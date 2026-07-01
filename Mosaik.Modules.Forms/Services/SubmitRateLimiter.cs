using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 41 Faz 3 §4.5 — IP başına dakikalık submit sayacı (public form abuse önleme).
    // Fixed-window (sliding DEĞİL — cache entry bir kez oluşturulur, expire olana dek sabit pencere).
    // Sayaç StrongBox + Interlocked ile ATOMİK (silent-failure-hunter HIGH — non-atomik
    // get-modify-set concurrent burst'te count kaybediyordu). Tek-instance BKM portalı için yeterli.
    public class SubmitRateLimiter(IMemoryCache cache)
    {
        private const int MaxPerMinute = 5;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        // true = limit aşıldı (reddet). Entry ilk oluşturulduğunda pencere sabitlenir; her çağrı atomik artırır.
        // Key = formId + IP (security-reviewer H-1: bir formun abuse'u başka formun bucket'ını tüketmesin).
        public bool IsRateLimited(int formId, string clientIp)
        {
            if (string.IsNullOrWhiteSpace(clientIp))
                clientIp = "unknown";
            var key = $"forms_submit_rl_{formId}_{clientIp}";
            var counter = cache.GetOrCreate(key, e =>
            {
                e.AbsoluteExpirationRelativeToNow = Window; // ilk yazımda sabit pencere — her çağrıda YENİLENMEZ
                return new StrongBox<int>(0);
            })!;
            var count = Interlocked.Increment(ref counter.Value);
            return count > MaxPerMinute;
        }
    }
}
