namespace Mosaik.Modules.Forms.Services
{
    public enum AntiSpamResult { Ok, HoneypotTripped, TooFast, RateLimited }

    // Plan 41 Faz 3 §4.5 — katmanlı anti-spam, early-exit sıralı (maliyet artan sırada):
    // honeypot → timing → IP-rate-limit → (CAPTCHA controller'da, en son). Saf/testable —
    // rate-limit state'i çağırana ait (bu helper sadece kararı verir).
    public static class AntiSpamGuard
    {
        public const int MinFillSeconds = 2; // form load → submit < 2sn = bot şüphesi

        // honeypotValue: gizli _hp alanı — bot doldurursa dolu gelir (insan boş bırakır).
        // elapsedSeconds: form render → submit arası saniye (client hidden timestamp'ten).
        // rateLimited: IP başına dakikalık submit limiti aşıldı mı (çağıran hesaplar).
        public static AntiSpamResult Evaluate(string? honeypotValue, double elapsedSeconds, bool rateLimited)
        {
            if (!string.IsNullOrEmpty(honeypotValue))
                return AntiSpamResult.HoneypotTripped;
            if (elapsedSeconds >= 0 && elapsedSeconds < MinFillSeconds)
                return AntiSpamResult.TooFast;
            if (rateLimited)
                return AntiSpamResult.RateLimited;
            return AntiSpamResult.Ok;
        }
    }
}
