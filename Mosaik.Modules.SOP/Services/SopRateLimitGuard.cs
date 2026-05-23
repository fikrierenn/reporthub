using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34.1 Faz 3 A-18 — Kullanıcı başına 20 soru/saat rate limit.
    // Admin bypass: isAdmin=true → CanAsk=true, RemainingQuota=int.MaxValue.
    // Sayım: SopAiConversation count WHERE UserId = u AND CreatedAt > NOW()-1h
    // (sliding 1-saat penceresi). Index: IX_SopAiConversations_UserId_CreatedAt (Migration 06).
    //
    // Plan 34 §10 madde 8 + Plan 34.1 §10 madde 10.
    public class SopRateLimitGuard
    {
        public const int HourlyLimit = 20;
        private static readonly TimeSpan Window = TimeSpan.FromHours(1);

        private readonly DbContext _db;

        public SopRateLimitGuard(DbContext db)
        {
            _db = db;
        }

        public async Task<RateLimitCheckResult> CheckAsync(int userId, bool isAdmin, CancellationToken ct = default)
        {
            if (isAdmin)
            {
                return new RateLimitCheckResult(
                    CanAsk: true,
                    RemainingQuota: int.MaxValue,
                    WindowResetAt: null,
                    UsedInWindow: 0);
            }

            var cutoff = DateTime.UtcNow - Window;

            // Pencere içindeki en eski + sayı: tek query.
            var rows = await _db.Set<SopAiConversation>()
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.CreatedAt > cutoff)
                .Select(c => c.CreatedAt)
                .ToListAsync(ct);

            var used = rows.Count;
            var remaining = HourlyLimit - used;
            if (remaining < 0) remaining = 0;

            DateTime? resetAt = null;
            if (used > 0)
            {
                // Pencere "kayar" — en eski kayıt + 1 saat = quota dolu kullanıcı için reset.
                resetAt = rows.Min().Add(Window);
            }

            return new RateLimitCheckResult(
                CanAsk: used < HourlyLimit,
                RemainingQuota: remaining,
                WindowResetAt: resetAt,
                UsedInWindow: used);
        }
    }

    public sealed record RateLimitCheckResult(
        bool CanAsk,
        int RemainingQuota,
        DateTime? WindowResetAt,
        int UsedInWindow);
}
