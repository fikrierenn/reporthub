using System.Runtime.Versioning;
using Microsoft.AspNetCore.Hosting;

namespace Mosaik.Services.Ai
{
    public sealed partial class AiExtractionWorker
    {
        [SupportedOSPlatform("windows")]
        private async Task<(Dictionary<int, string> byPage, string? diag)> TryRescueLowConfPagesAsync(
            string filePath, HashSet<int> pageIndices, CancellationToken ct)
        {
            var byPage = new Dictionary<int, string>();
            if (pageIndices.Count == 0) return (byPage, null);

            using var scope = _scopeFactory.CreateScope();
            var vision = scope.ServiceProvider.GetRequiredService<Mosaik.Core.Ai.IAiVisionProvider>();
            var absolute = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().WebRootPath,
                    filePath.TrimStart('/', '\\'));

            if (!File.Exists(absolute))
                return (byPage, $"Vision rescue: dosya yok ({absolute})");

            var pdfBytes = await File.ReadAllBytesAsync(absolute, ct);
            int idx = 0;
            int failedCount = 0;
            using var stream = new MemoryStream(pdfBytes);
            foreach (var bmp in PDFtoImage.Conversion.ToImages(stream, options: new(Dpi: 200)))
            {
                if (pageIndices.Contains(idx))
                {
                    try
                    {
                        using (bmp)
                        using (var jpgData = bmp.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 90))
                        {
                            var b64 = Convert.ToBase64String(jpgData.ToArray());
                            var req = new Mosaik.Core.Ai.AiVisionRequest(
                                SystemPrompt: "Bu sözleşme sayfasındaki Türkçe metni eksiksiz oku. " +
                                              "Sadece gördüğün metni döndür, yorum/özet yapma. " +
                                              "Madde numaralarını ve paragraf yapısını koru.",
                                UserPrompt: $"Sayfa {idx + 1} metnini düz Türkçe olarak ver.",
                                Base64Images: new[] { b64 },
                                MimeType: "image/jpeg",
                                RequireJson: false,
                                Purpose: "contract_ocr_per_page_rescue");
                            var result = await vision.GenerateFromImagesAsync(req, ct);
                            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.RawJson))
                                byPage[idx] = result.RawJson!;
                            else
                            {
                                failedCount++;
                                _logger.LogWarning("Vision rescue sayfa {Page} başarısız: {Err}", idx + 1, result.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogWarning(ex, "Vision rescue sayfa {Page} exception", idx + 1);
                    }
                }
                else
                {
                    bmp.Dispose();
                }
                idx++;
            }

            string? diag = failedCount == 0 ? null
                : failedCount == pageIndices.Count ? $"Vision rescue: {failedCount}/{pageIndices.Count} sayfa başarısız (tümü)"
                : $"Vision rescue: {failedCount}/{pageIndices.Count} sayfa başarısız";
            return (byPage, diag);
        }

        [SupportedOSPlatform("windows")]
        private async Task<(string text, string? diag)> TryFullDocumentVisionAsync(string filePath, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var vision = scope.ServiceProvider.GetRequiredService<Mosaik.Core.Ai.IAiVisionProvider>();
            var absolute = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().WebRootPath,
                    filePath.TrimStart('/', '\\'));

            if (!File.Exists(absolute))
                return (string.Empty, $"Vision full: dosya yok ({absolute})");

            var b64Images = new List<string>();
            var pdfBytes = await File.ReadAllBytesAsync(absolute, ct);
            using (var stream = new MemoryStream(pdfBytes))
            {
                int p = 0;
                foreach (var bmp in PDFtoImage.Conversion.ToImages(stream, options: new(Dpi: 150)))
                {
                    if (p >= 3) { bmp.Dispose(); break; }
                    using (bmp)
                    using (var jpgData = bmp.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 85))
                        b64Images.Add(Convert.ToBase64String(jpgData.ToArray()));
                    p++;
                }
            }

            if (b64Images.Count == 0)
                return (string.Empty, "Vision full: PDF sayfası görüntüye çevrilemedi");

            var req = new Mosaik.Core.Ai.AiVisionRequest(
                SystemPrompt: ExtractionPrompts.Stage1SystemPrompt,
                UserPrompt: "Bu sözleşme sayfalarındaki tüm metni oku ve düz metin olarak döndür.",
                Base64Images: b64Images,
                MimeType: "image/jpeg",
                RequireJson: false,
                Purpose: "contract_ocr_vision_full");
            var result = await vision.GenerateFromImagesAsync(req, ct);
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.RawJson))
                return (result.RawJson!, null);
            return (string.Empty, $"Vision full: IsSuccess={result.IsSuccess}, Hata={result.Error}");
        }
    }
}
