using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Domain;
using Mosaik.Modules.GorevTanimlari.Entities;

namespace Mosaik.Modules.GorevTanimlari.Services;

// Org şeması verisi — OrgPositions omurgası (bizim tasarım, DECISIONS I) + GorevVersions görev-tanımı
// içeriği → index.html org-chart'ının beklediği {org:[...], advisors:[]} JSON şekline dönüştürür.
// Ana MosaikContext modülden görünmez → base DbContext inject (OrgPosition Mosaik.Core'da, base'de map'li).
public class SemaService
{
    private readonly DbContext _db;
    private readonly ILogger<SemaService> _logger;
    public SemaService(DbContext db, ILogger<SemaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> BuildTreeJsonAsync(CancellationToken ct = default)
    {
        var positions = await _db.Set<OrgPosition>().AsNoTracking()
            .Where(p => p.IsActive).ToListAsync(ct);

        var docs = await _db.Set<GorevDocument>().AsNoTracking()
            .Where(d => d.OrgPositionId != null).ToListAsync(ct);
        var docByPos = docs
            .GroupBy(d => d.OrgPositionId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var docIds = docs.Select(d => d.Id).ToHashSet();
        var versions = await _db.Set<GorevVersion>().AsNoTracking()
            .Where(v => v.Status == 1).ToListAsync(ct);
        var contentByDoc = versions
            .Where(v => docIds.Contains(v.GorevDocumentId))
            .GroupBy(v => v.GorevDocumentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.VersionNumber).First().ContentJson);

        var byParent = positions.ToLookup(p => p.ParentPositionId);

        object Node(OrgPosition p)
        {
            object? def = null;
            if (docByPos.TryGetValue(p.Id, out var docId)
                && contentByDoc.TryGetValue(docId, out var cj)
                && !string.IsNullOrWhiteSpace(cj))
            {
                try { def = JsonSerializer.Deserialize<JsonElement>(cj); }
                catch (JsonException ex)
                {
                    // Bozuk/eski-şema ContentJson → tanım boş gösterilir; sessiz kalmasın (operasyonel iz).
                    _logger.LogWarning(ex, "OrgPosition {PosId} görev tanımı JSON parse edilemedi, boş gösteriliyor.", p.Id);
                    def = null;
                }
            }
            return new
            {
                id = "op" + p.Id,
                position = p.Title,
                name = p.HolderName ?? "",
                role = Role(p.Title),
                directToManager = false,
                def,
                children = byParent[p.Id]
                    .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Title)
                    .Select(Node).ToList()
            };
        }

        var roots = byParent[null]
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Title)
            .Select(Node).ToList();

        var data = new { org = roots, advisors = new List<object>() };
        // Default encoder — '<'/'>' → </> kaçar. Bu JSON <script id="sema-data"> içine
        // @Html.Raw ile gömülüyor; UnsafeRelaxedJsonEscaping kullanılsa "</script>" breakout → stored XSS.
        // TR karakterler \uXXXX olur, JSON.parse doğru çözer (güvenli).
        return JsonSerializer.Serialize(data);
    }

    // Ünvandan rol katmanı türet (renk + badge için). org.json role'üyle uyumlu sıralama.
    private static string Role(string title)
    {
        var t = (title ?? string.Empty).ToLowerInvariant();
        // asistan/temsilci/analist önce — "YKB Asistanı" ünvanı "yönetim kurulu" içerse de üst yönetim değil.
        if (t.Contains("asistan") || t.Contains("temsilci") || t.Contains("analist")) return "uzman";
        if (t.Contains("yönetim kurulu") || t.Contains("genel müdür")) return "ust";
        if (t.Contains("danışman")) return "danisman";
        if (t.Contains("müdür")) return "mudur";
        if (t.Contains("takım lideri") || t.Contains("lider")) return "lider";
        if (t.Contains("uzman") || t.Contains("sorumlu") || t.Contains("görevli")) return "uzman";
        return "diger";
    }
}
