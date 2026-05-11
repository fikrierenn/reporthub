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
                        using var processed = ApplyOtsuBinarization(skBitmap);
                        using var skData = processed.Encode(SKEncodedImageFormat.Png, 100);
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
        // Perf: GetPixelSpan (span okuma) + SKData.CreateCopy (managed write) — GetPixel/SetPixel virtual call yok.
        private static SKBitmap ApplyOtsuBinarization(SKBitmap source)
        {
            int width = source.Width;
            int height = source.Height;
            int total = width * height;
            int bpp = source.BytesPerPixel; // Bgra8888 = 4

            // 1) Grayscale + histogram — O(n) tek geçiş, span erişimi (no virtual dispatch)
            var srcSpan = source.GetPixelSpan(); // ReadOnlySpan<byte>
            var gray = new byte[total];
            var hist = new int[256];
            for (int i = 0; i < total; i++)
            {
                int off = i * bpp;
                // Bgra8888: off=B, off+1=G, off+2=R. Integer Rec.601: (R*77 + G*150 + B*29) >> 8
                byte g = (byte)((srcSpan[off + 2] * 77 + srcSpan[off + 1] * 150 + srcSpan[off] * 29) >> 8);
                gray[i] = g;
                hist[g]++;
            }

            // 2) Otsu — O(256), ihmal edilebilir
            double sum = 0;
            for (int t = 0; t < 256; t++) sum += t * hist[t];
            double sumB = 0, maxVar = 0;
            int wB = 0, threshold = 127;
            for (int t = 0; t < 256; t++)
            {
                wB += hist[t];
                if (wB == 0) continue;
                int wF = total - wB;
                if (wF == 0) break;
                sumB += t * hist[t];
                double mB = sumB / wB;
                double mF = (sum - sumB) / wF;
                double between = (double)wB * wF * (mB - mF) * (mB - mF);
                if (between > maxVar) { maxVar = between; threshold = t; }
            }

            // 3) Binary çıktı — managed byte[] + SKData.CreateCopy (SetPixel yok)
            var rawOut = new byte[total]; // Gray8: 1 byte/pixel
            for (int i = 0; i < total; i++)
                rawOut[i] = gray[i] < threshold ? (byte)0 : (byte)255;

            var outInfo = new SKImageInfo(width, height, SKColorType.Gray8, SKAlphaType.Opaque);
            using var outData = SKData.CreateCopy(rawOut);
            using var outImage = SKImage.FromPixels(outInfo, outData, width);
            return SKBitmap.FromImage(outImage) ?? new SKBitmap(width, height);
        }

        public bool HasMinimumText(string text) => text.Length >= MinTextLength;
    }

    // Plan 27 Faz A — bir PDF sayfasının OCR sonucu.
    public sealed record OcrPageResult(int PageIndex, string Text, float MeanConfidence, int CharCount);
}
