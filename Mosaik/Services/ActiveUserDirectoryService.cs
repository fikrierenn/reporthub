using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Users;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Plan 34 Faz E S-20 — modül-erişimli aktif kullanıcı listesi.
    // SOP (ve sonradan diğer modüller) IActiveUserDirectory üzerinden User
    // entity'sine ulaşmadan aktif kullanıcı ID listesini çeker.
    public class ActiveUserDirectoryService : IActiveUserDirectory
    {
        private readonly MosaikContext _db;

        public ActiveUserDirectoryService(MosaikContext db) => _db = db;

        public async Task<List<int>> GetActiveUserIdsAsync(int? firmaId = null)
        {
            if (firmaId is null or 0)
            {
                return await _db.Users.AsNoTracking()
                    .Where(u => u.IsActive)
                    .Select(u => u.UserId)
                    .ToListAsync();
            }

            // FirmaIds CSV pattern (DailyReminderJob ile aynı) — in-memory parse:
            // EF'in CSV split desteği yok, raw filter sonrası in-memory Contains.
            var firmaToken = firmaId.Value;
            var candidates = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.FirmaIds != null)
                .Select(u => new { u.UserId, u.FirmaIds })
                .ToListAsync();

            return candidates
                .Where(u => ParseFirmaIds(u.FirmaIds!).Contains(firmaToken))
                .Select(u => u.UserId)
                .ToList();
        }

        public async Task<Dictionary<int, string>> GetUserEmailsAsync(IEnumerable<int> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, string>();

            var rows = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.Email != null && u.Email != "" && ids.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Email })
                .ToListAsync();

            return rows.ToDictionary(r => r.UserId, r => r.Email!);
        }

        private static IReadOnlySet<int> ParseFirmaIds(string csv)
        {
            var set = new HashSet<int>();
            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var id)) set.Add(id);
            }
            return set;
        }
    }
}
