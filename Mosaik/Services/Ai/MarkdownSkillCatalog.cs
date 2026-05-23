using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mosaik.Core.AI.Skills;

namespace Mosaik.Services.Ai
{
    // Plan 34.2 — Mosaik/App_Data/ai-skills/*.md markdown skill loader.
    // YAML frontmatter parse (id/name/description/category/tags[]/token_estimate/chains[]).
    // Singleton — startup'ta tüm skill'ler taranır, manifest cache'lenir, içerik lazy.
    // File watcher yok (dev'de restart yeterli, prod'da deploy zaten restart).
    public sealed class MarkdownSkillCatalog : ISkillCatalog
    {
        private readonly ILogger<MarkdownSkillCatalog> _logger;
        private readonly string _skillsRoot;
        private readonly ConcurrentDictionary<string, SkillManifest> _manifests = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _contentCache = new(StringComparer.OrdinalIgnoreCase);
        private volatile bool _initialized;
        private readonly object _initLock = new();

        public MarkdownSkillCatalog(ILogger<MarkdownSkillCatalog> logger, string contentRoot)
        {
            _logger = logger;
            _skillsRoot = Path.Combine(contentRoot, "App_Data", "ai-skills");
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;

                if (!Directory.Exists(_skillsRoot))
                {
                    _logger.LogWarning("SkillCatalog: dizin yok {Path} — boş catalog.", _skillsRoot);
                    _initialized = true;
                    return;
                }

                foreach (var file in Directory.EnumerateFiles(_skillsRoot, "*.md", SearchOption.TopDirectoryOnly))
                {
                    if (Path.GetFileName(file).StartsWith("_")) continue;       // _README.md vb. atla
                    try
                    {
                        var manifest = ParseManifest(file);
                        if (manifest != null) _manifests[manifest.Id] = manifest;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "SkillCatalog: parse hatası {File}", file);
                    }
                }

                _logger.LogInformation("SkillCatalog: {Count} skill yüklendi ({Path})", _manifests.Count, _skillsRoot);
                _initialized = true;
            }
        }

        public void Reload()
        {
            lock (_initLock)
            {
                _manifests.Clear();
                _contentCache.Clear();
                _initialized = false;
            }
            EnsureInitialized();
            _logger.LogInformation("SkillCatalog: manuel reload — {Count} skill yüklendi", _manifests.Count);
        }

        public Task<IReadOnlyList<SkillManifest>> ListAsync(CancellationToken ct = default)
        {
            EnsureInitialized();
            IReadOnlyList<SkillManifest> list = _manifests.Values.OrderBy(m => m.Name).ToList();
            return Task.FromResult(list);
        }

        public Task<Skill?> GetAsync(string id, CancellationToken ct = default)
        {
            EnsureInitialized();
            if (!_manifests.TryGetValue(id, out var manifest)) return Task.FromResult<Skill?>(null);

            var content = _contentCache.GetOrAdd(manifest.FilePath, path => File.ReadAllText(path));
            // Frontmatter'ı kes — sadece markdown body kalır (LLM'e bu gider).
            var body = StripFrontmatter(content);
            return Task.FromResult<Skill?>(new Skill(manifest, body));
        }

        public Task<IReadOnlyList<SkillManifest>> MatchAsync(SkillMatchContext ctx, int top = 3, CancellationToken ct = default)
        {
            EnsureInitialized();
            var tagSet = (ctx.Tags ?? Array.Empty<string>()).Select(t => t.Trim().ToLowerInvariant()).ToHashSet();
            var question = ctx.Question?.ToLowerInvariant() ?? string.Empty;

            var scored = _manifests.Values
                .Select(m =>
                {
                    double score = 0;
                    // Category match: 5 puan
                    if (!string.IsNullOrEmpty(ctx.Category)
                        && string.Equals(m.Category, ctx.Category, StringComparison.OrdinalIgnoreCase))
                        score += 5;

                    // Tag overlap: her ortak tag 2 puan
                    foreach (var t in m.Tags)
                    {
                        if (tagSet.Contains(t.ToLowerInvariant())) score += 2;
                    }

                    // Question keyword match: tag içeriyor 1 puan
                    if (question.Length > 0)
                    {
                        foreach (var t in m.Tags)
                        {
                            if (question.Contains(t.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
                                score += 1;
                        }

                        // Description keyword match: 0.5 puan/kelime (5+ char anlamlı keyword)
                        var descLower = m.Description.ToLowerInvariant();
                        foreach (var word in question.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (word.Length < 5) continue;
                            if (descLower.Contains(word)) score += 0.5;
                        }

                        // Name keyword: 2 puan
                        var nameLower = m.Name.ToLowerInvariant();
                        foreach (var word in question.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (word.Length < 5) continue;
                            if (nameLower.Contains(word)) score += 2;
                        }
                    }

                    return (Manifest: m, Score: score);
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(top)
                .Select(x => x.Manifest)
                .ToList();

            return Task.FromResult<IReadOnlyList<SkillManifest>>(scored);
        }

        // --- frontmatter parser (basit, sadece bilinen alanlar) ---

        private static readonly Regex FrontmatterRegex =
            new(@"^---\s*\r?\n(.*?)\r?\n---\s*\r?\n", RegexOptions.Singleline | RegexOptions.Compiled);

        private static SkillManifest? ParseManifest(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var match = FrontmatterRegex.Match(content);
            if (!match.Success) return null;                                  // frontmatter yoksa atla

            var fm = ParseFrontmatter(match.Groups[1].Value);

            var id = fm.GetValueOrDefault("id", Path.GetFileNameWithoutExtension(filePath));
            var name = fm.GetValueOrDefault("name", id);
            var description = fm.GetValueOrDefault("description", "");
            var category = fm.TryGetValue("category", out var c) && !string.IsNullOrWhiteSpace(c) ? c : null;
            var tags = ParseList(fm.GetValueOrDefault("tags", ""));
            int.TryParse(fm.GetValueOrDefault("token_estimate", "0"), out var tokenEst);
            var chains = ParseList(fm.GetValueOrDefault("chains", ""));

            return new SkillManifest(id, name, description, category, tags, tokenEst, chains, filePath);
        }

        private static Dictionary<string, string> ParseFrontmatter(string yaml)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? currentKey = null;
            var sb = new System.Text.StringBuilder();

            foreach (var raw in yaml.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) { Flush(); continue; }

                // "key: value" pattern
                var colonIdx = line.IndexOf(':');
                if (colonIdx > 0 && !char.IsWhiteSpace(line[0]))
                {
                    Flush();
                    currentKey = line[..colonIdx].Trim();
                    var val = line[(colonIdx + 1)..].Trim();
                    sb.Append(val);
                }
                else if (currentKey != null)
                {
                    // multi-line devamı
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(line.Trim());
                }
            }
            Flush();

            void Flush()
            {
                if (currentKey != null && sb.Length > 0)
                    dict[currentKey] = sb.ToString();
                sb.Clear();
                currentKey = null;
            }

            return dict;
        }

        // "[a, b, c]" veya "a, b, c" formatı
        private static string[] ParseList(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return Array.Empty<string>();
            val = val.Trim('[', ']').Trim();
            return val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private static string StripFrontmatter(string content)
        {
            var match = FrontmatterRegex.Match(content);
            return match.Success ? content[match.Length..].TrimStart() : content;
        }
    }
}
