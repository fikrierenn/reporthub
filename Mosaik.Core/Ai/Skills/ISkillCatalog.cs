namespace Mosaik.Core.AI.Skills
{
    // Plan 34.2 — Mosaik runtime LLM domain expertise skill catalog.
    // App_Data/ai-skills/*.md markdown dosyaları YAML frontmatter ile.
    // SopRagAdvisorService kategori bazlı otomatik match veya manuel select ile inject.
    public interface ISkillCatalog
    {
        // Tüm skill manifest'leri (içerik yüklenmez, hafif).
        Task<IReadOnlyList<SkillManifest>> ListAsync(CancellationToken ct = default);

        // Tek skill (content dahil) — id markdown dosyasının frontmatter `id` alanı.
        Task<Skill?> GetAsync(string id, CancellationToken ct = default);

        // Context'e göre uygun skill'leri öner (Category + Tags match).
        // Sıralama: tag overlap × score + category match × score.
        Task<IReadOnlyList<SkillManifest>> MatchAsync(SkillMatchContext ctx, int top = 3, CancellationToken ct = default);

        // Manuel cache invalidation — admin "Yeniden Yükle" buton için.
        void Reload();
    }

    public sealed record SkillManifest(
        string Id,
        string Name,
        string Description,
        string? Category,
        string[] Tags,
        int TokenEstimate,
        string[] Chains,
        string FilePath);

    public sealed record Skill(
        SkillManifest Manifest,
        string MarkdownContent);

    public sealed record SkillMatchContext(
        string? Category = null,
        IEnumerable<string>? Tags = null,
        string? Question = null);
}
