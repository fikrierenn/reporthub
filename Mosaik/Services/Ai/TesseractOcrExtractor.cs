using PDFtoImage;
using SkiaSharp;
using Tesseract;
using Microsoft.AspNetCore.Hosting;
using System.Runtime.Versioning;

namespace Mosaik.Services.Ai
{
    // Plan 26 — Taranmış PDF için Tesseract OCR (offline, ücretsiz).
    // Plan 27 Faz A — per-page metadata + Otsu binarization + confidence.
    // Akış: PDF sayfaları → SKBitmap (PDFtoImage) → grayscale + Otsu → PNG → Tesseract.
    public sealed class TesseractOcrExtractor
    {
        // Plan 27 Faz A-03: kapsam artırıldı — taranmış uzun sözleşmelerde §5 fesih/devir/mücbir
        // maddeleri sayfa 13-15'te oluyor, eski 8 limiti yarım analiz üretiyordu.
        private const int MaxPages = 30;
        private const int MinTextLength = 50;
        private const int Dpi = 300;
        private const string Languages = "tur+eng";

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TesseractOcrExtractor> _logger;

        public string? LastDiagnostic { get; private set; }

        public TesseractOcrExtractor(IWebHostEnvironment env, ILogger<TesseractOcrExtractor> logger)
        {
            _env = env;
            _logger = logger;
        }

        [SupportedOSPlatform("windows")]
        public string ExtractFromPdf(string filePath)
        {
            var pages = ExtractPages(filePath);
            if (pages.Count == 0) return string.Empty;
            return string.Join("\n", pages.Where(p => !string.IsNullOrWhiteSpace(p.Text)).Select(p => p.Text)).Trim();
        }

        [SupportedOSPlatform("windows")]
        public IReadOnlyList<OcrPageResult> ExtractPages(string filePath, CancellationToken ct = default)
        {
            LastDiagnostic = null;
            var absolute = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(_env.WebRootPath, filePath.TrimStart('/', '\\'));

            if (!File.Exists(absolute))
            {
                LastDiagnostic = $"Dosya bulunamadı: {absolute}";
                _logger.LogWarning("TesseractOcr: {Diag}", LastDiagnostic);
                return Array.Empty<OcrPageResult>();
            }

            var tessDataPath = Path.Combine(_env.ContentRootPath, "tessdata");
            _logger.LogInformation("TesseractOcr: ContentRootPath={Root}, tessdata={TessPath}, PDF={Pdf}, DPI={Dpi}, Lang={Lang}",
                _env.ContentRootPath, tessDataPath, absolute, Dpi, Languages);
            if (!Directory.Exists(tessDataPath) || !File.Exists(Path.Combine(tessDataPath, "tur.traineddata")))
            {
                LastDiagnostic = $"tessdata/tur.traineddata bulunamadı: {tessDataPath}";
                _logger.LogWarning("TesseractOcr: {Diag}", LastDiagnostic);
                return Array.Empty<OcrPageResult>();
            }

            var results = new List<OcrPageResult>();
            try
            {
                var pdfFileBytes = File.ReadAllBytes(absolute);
                int pageCount;
                using (var pcStream = new MemoryStream(pdfFileBytes))
                    pageCount = Conversion.GetPageCount(pcStream);
                var pagesToProcess = Math.Min(pageCount, MaxPages);

                using var engine = new TesseractEngine(tessDataPath, Languages, EngineMode.Default);
                using var imgStream = new MemoryStream(pdfFileBytes);

                int pageIndex = 0;
                foreach (var skBitmap in Conversion.ToImages(imgStream, options: new(Dpi: Dpi)))
                {
                    ct.ThrowIfCancellationRequested();
                    if (pageIndex >= pagesToProcess) break;

                    using (skBitmap)
                    {
                        // Plan 27 Faz A-02 — Otsu preprocessing geçici olarak devre dışı.
                        // GetPixel/SetPixel her sayfa için 8M+ sanal call üretiyordu (22 sayfa × 5+ dk).
                        // Tesseract'ın internal preprocessing'i çoğu durumda yeterli.
                        // Hızlı versiyon için SKBitmap.GetPixelSpan() unsafe erişim gerekiyor — sonraki sprint.
                        using var skData = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
                        var pngBytes = skData.ToArray();

                        using var pix = Pix.LoadFromMemory(pngBytes);
                        using var ocrPage = engine.Process(pix);
                        var text = ocrPage.GetText() ?? string.Empty;
                        var meanConfidence = ocrPage.GetMeanConfidence();

                        results.Add(new OcrPageResult(pageIndex, text.Trim(), meanConfidence, text.Trim().Length));
                    }

                    pageIndex++;
                }

                var totalChars = results.Sum(r => r.CharCount);
                var avgConf = results.Count > 0 ? results.Average(r => r.MeanConfidence) : 0;
                _logger.LogInformation(
                    "TesseractOcr: {Chars} karakter, {Pages}/{Total} sayfa, ortalama conf={Conf:F2}",
                    totalChars, pagesToProcess, pageCount, avgConf);

                return results;
            }
            catch (Exception ex)
            {
                LastDiagnostic = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogError(ex, "TesseractOcr başarısız: {Path}", absolute);
                return results;
            }
        }

        // Plan 27 Faz A-02 — grayscale + Otsu adaptive threshold.
        // Tarayıcı çıkışında zayıf kontrast/gölge varsa Tesseract Türkçe diakritikleri kaçırır.
        // Otsu pixel histogramından optimum threshold bulup binary'e çevirir — kontrast keskinleşir.
        private static SKBitmap ApplyOtsuBinarization(SKBitmap source)
        {
            int width = source.Width;
            int height = source.Height;

            // 1) Grayscale histogram + gri değerler
            var grayValues = new byte[width * height];
            var histogram = new int[256];
            int idx = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var px = source.GetPixel(x, y);
                    // luminance (Rec. 601)
                    byte gray = (byte)((px.Red * 0.299) + (px.Green * 0.587) + (px.Blue * 0.114));
                    grayValues[idx++] = gray;
                    histogram[gray]++;
                }
            }

            // 2) Otsu — sınıflar arası varyansı maksimize eden eşik
            int total = width * height;
            double sum = 0;
            for (int t = 0; t < 256; t++) sum += t * histogram[t];

            double sumB = 0;
            int wB = 0;
            double maxVar = 0;
            int threshold = 127;
            for (int t = 0; t < 256; t++)
            {
                wB += histogram[t];
                if (wB == 0) continue;
                int wF = total - wB;
                if (wF == 0) break;
                sumB += t * histogram[t];
                double mB = sumB / wB;
                double mF = (sum - sumB) / wF;
                double varBetween = (double)wB * wF * (mB - mF) * (mB - mF);
                if (varBetween > maxVar)
                {
                    maxVar = varBetween;
                    threshold = t;
                }
            }

            // 3) Threshold uygula → binary bitmap
            var output = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            idx = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte v = grayValues[idx++] < threshold ? (byte)0 : (byte)255;
                    output.SetPixel(x, y, new SKColor(v, v, v));
                }
            }
            return output;
        }

        public bool HasMinimumText(string text) => text.Length >= MinTextLength;
    }

    // Plan 27 Faz A — bir PDF sayfasının OCR sonucu.
    public sealed record OcrPageResult(int PageIndex, string Text, float MeanConfidence, int CharCount);
}
