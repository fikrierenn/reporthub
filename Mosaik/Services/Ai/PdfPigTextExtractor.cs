using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Mosaik.Services.Ai
{
    // Plan 25 Faz D — PdfPig (Apache 2.0) ile PDF metin çıkarma.
    // Sayfa sırası korunur; kelimeler arası boşluk normalize edilir.
    // OCR değil — metin tabanlı PDF gerektirir. Taranmış PDF için harici OCR gerekir.
    public class PdfPigTextExtractor : IPdfTextExtractor
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PdfPigTextExtractor> _logger;

        public PdfPigTextExtractor(IWebHostEnvironment env, ILogger<PdfPigTextExtractor> logger)
        {
            _env = env;
            _logger = logger;
        }

        public Task<string> ExtractAsync(string filePath, CancellationToken ct = default)
        {
            // Absolute path gelirse direkt kullan; relative ise ContentRoot (App_Data) altında çöz
            var absolute = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(_env.ContentRootPath, filePath.TrimStart('/', '\\'));

            if (!File.Exists(absolute))
            {
                _logger.LogWarning("PDF dosyası bulunamadı: {Path}", absolute);
                return Task.FromResult(string.Empty);
            }

            try
            {
                using var doc = PdfDocument.Open(absolute);
                var pages = new System.Text.StringBuilder();

                foreach (var page in doc.GetPages())
                {
                    // Kelimeleri sıralı al (soldan sağa, yukarıdan aşağıya)
                    var words = page.GetWords()
                        .OrderBy(w => -w.BoundingBox.Bottom)   // yukarıdan aşağıya
                        .ThenBy(w => w.BoundingBox.Left)       // soldan sağa
                        .Select(w => w.Text);

                    pages.AppendLine(string.Join(" ", words));
                    pages.AppendLine();
                }

                return Task.FromResult(pages.ToString().Trim());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF metni çıkarılamadı: {Path}", absolute);
                return Task.FromResult(string.Empty);
            }
        }
    }
}
