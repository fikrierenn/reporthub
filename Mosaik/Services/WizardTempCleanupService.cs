namespace Mosaik.Services
{
    // N-3 — App_Data/wizard_temp/ altındaki TTL'i geçmiş geçici PDF dosyalarını temizler.
    // WizardExtractionService finally bloğu normal akışta siler; bu servis crash/abort artıklarını yakalar.
    public sealed class WizardTempCleanupService : BackgroundService
    {
        private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan FileTtl = TimeSpan.FromMinutes(20);

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<WizardTempCleanupService> _logger;

        public WizardTempCleanupService(IWebHostEnvironment env, ILogger<WizardTempCleanupService> logger)
        {
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WizardTempCleanupService başladı.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(ScanInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                CleanupOldFiles();
            }
            _logger.LogInformation("WizardTempCleanupService durdu.");
        }

        private void CleanupOldFiles()
        {
            var tempDir = Path.Combine(_env.ContentRootPath, "App_Data", "wizard_temp");
            if (!Directory.Exists(tempDir)) return;

            var cutoff = DateTime.UtcNow - FileTtl;
            int deleted = 0, failed = 0;

            foreach (var file in Directory.EnumerateFiles(tempDir, "*.pdf"))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                    {
                        File.Delete(file);
                        deleted++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(ex, "WizardTempCleanup: {File} silinemedi", Path.GetFileName(file));
                }
            }

            if (deleted > 0 || failed > 0)
                _logger.LogInformation("WizardTempCleanup: {Deleted} silindi, {Failed} başarısız", deleted, failed);
        }
    }
}
