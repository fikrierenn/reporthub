using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Ai;
using Mosaik.Core.Logging;
using Mosaik.Modules.Circular.Models;

namespace Mosaik.Modules.Circular.Services
{
    // Plan 17 Faz F — Tamim için Türkçe AI özet üretici.
    // Kullanılan sistem prompt: kurum-içi yöneticiye 5 madde özet, JSON çıktı.
    // Sonuç Circular.AiSummaryJson alanına yazılır.
    public class CircularSummaryService
    {
        private const string SystemPrompt = @"Sen kurumsal tamim özetleyicisisin. Bağlayıcı resmi bir belge özetliyorsun — yasal kanıt değerinde, mağaza ve genel müdürlük personeline duyurulacak.

YAZIM ÜSLUBU (kurumsal/resmi Türkçe):
- Edilgen yapı tercih edilir: ""yapılacaktır"", ""uygulanacaktır"", ""yürürlüğe girecektir"", ""tamamlanması gerekmektedir"", ""dikkate alınmalıdır"".
- Cümleler tam, fiil net, sonu noktalı. ""dir/dır"" eki uygun yerlerde.
- Konuşma dili, argo, samimi ifadeler YASAK (""olacak"" değil ""olacaktır"", ""yapılacak"" yerine ""yapılacaktır"" tercih edilmeli).
- ""Tarafından"", ""itibarıyla"", ""bağlamında"", ""kapsamında"" gibi resmi bağlayıcılar kullanılabilir.
- Robotik 3. tekil emir cümleleri yasak (""yapar"", ""uygular"", ""tamamlar"" gibi tek başına bitişler değil — edilgen veya gelecek zaman).
- İngilizce kelime kullanma; Türkçe terminoloji.
- Her madde 1 cümle, en fazla 22 kelime.

İÇERİK:
- 3-7 madde özet. Acil olanlar (isUrgent=true) başa alınır ve satır başına 🔴 konur.
- Tarih, saat, tutar, oran, kişi/departman/mağaza adları AYNEN korunur.
- Uydurma yasak — yalnızca blok metninde yer alan bilgi özetlenir.
- topics: 1-3 kelimelik 3-5 anahtar konu başlığı (örn: ""e-Fatura"", ""Yıllık İzin"").

ÖRNEK (referans, kurumsal üslup):
{
  ""summary"": [
    ""🔴 9 Mayıs 2026 Cumartesi 09:00-10:00 saatleri arasında Heykel mağaza POS sistemleri bakım nedeniyle hizmet dışı bırakılacaktır."",
    ""1 Haziran 2026 tarihinden itibaren tüm izin talepleri yalnızca Mosaik portalı üzerinden alınacaktır."",
    ""Mayıs 2026 dönemi muhtasar beyanname için son verilme tarihi 26 Mayıs 2026 saat 23:59'dur."",
    ""KVKK eğitimi tüm personel tarafından bu hafta sonuna kadar tamamlanmalıdır."",
    ""Ay sonu kasiyer prosedürü 3.2 maddesi güncellenmiş olup, iki kişilik imza zorunluluğu getirilmiştir.""
  ],
  ""topics"": [""POS Bakım"", ""İzin Talebi"", ""Muhtasar Beyan"", ""KVKK"", ""Kasa Sayım""],
  ""urgentCount"": 1
}

ÇIKTI: Yalnızca JSON. Markdown, önek, açıklama yazma.";

        private readonly DbContext _db;
        private readonly IAiSummaryProvider _ai;
        private readonly IAuditLog _audit;

        public CircularSummaryService(DbContext db, IAiSummaryProvider ai, IAuditLog audit)
        {
            _db = db;
            _ai = ai;
            _audit = audit;
        }

        public async Task<AiSummaryResult> GenerateAsync(int circularId, CancellationToken ct = default)
        {
            var circular = await _db.Set<Models.Circular>().FirstOrDefaultAsync(c => c.Id == circularId, ct);
            if (circular == null)
                return new AiSummaryResult(false, null, "Circular bulunamadı.");

            var blocks = await _db.Set<DailyBlock>().AsNoTracking()
                .Where(b => b.CircularId == circularId && b.IsActive)
                .OrderByDescending(b => b.IsUrgent).ThenBy(b => b.Id)
                .ToListAsync(ct);

            if (blocks.Count == 0)
                return new AiSummaryResult(false, null, "Tamime bağlı blok yok.");

            var userPayload = new
            {
                circular = new { circular.CircularNumber, circular.Title, date = circular.CircularDate.ToString("yyyy-MM-dd") },
                blocks = blocks.Select(b => new
                {
                    b.BlockNumber,
                    b.Department,
                    b.Subject,
                    b.IsUrgent,
                    contentText = StripHtml(b.Content)
                }).ToList()
            };
            var userPrompt = JsonSerializer.Serialize(userPayload, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            var result = await _ai.GenerateAsync(new AiRequest(
                SystemPrompt: SystemPrompt,
                UserPrompt: userPrompt,
                RequireJson: true,
                Purpose: "circular_summary",
                // Türkçe doğal cümleler için 0.3-0.5 ideal — kullanıcı genel ayarda
                // farklı seçmiş olsa bile özet için deterministik kalsın
                OverrideTemperature: 0.4
            ), ct);
            if (!result.IsSuccess) return result;

            // JSON validate (uyumsuzsa ham metni saklamayız)
            try
            {
                using var _ = JsonDocument.Parse(result.RawJson ?? "{}");
            }
            catch (Exception ex)
            {
                return new AiSummaryResult(false, result.RawJson, $"AI yanıtı geçersiz JSON: {ex.Message}", result.InputTokens, result.OutputTokens, result.ModelUsed);
            }

            circular.AiSummaryJson = result.RawJson;
            circular.AiSummaryAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                eventType: "circular_summary_generated",
                targetType: "circular",
                targetKey: circularId.ToString(),
                description: $"AI özet üretildi (model={result.ModelUsed}, in={result.InputTokens}, out={result.OutputTokens}).");

            return result;
        }

        // Quill HTML → plain text (AI prompt için temiz)
        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            var t = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
            t = System.Net.WebUtility.HtmlDecode(t);
            t = System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ").Trim();
            return t.Length > 4000 ? t.Substring(0, 4000) : t;
        }
    }
}
