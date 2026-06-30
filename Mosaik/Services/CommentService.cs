using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Comments;
using Mosaik.Core.Domain;
using Mosaik.Core.Html;
using Mosaik.Core.Notification;
using Mosaik.Core.Users;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Plan 54 M5 — ICommentService implementasyonu (ana proje).
    // entityType lookup doğrula → Body sanitize → kaydet → @mention parse → fan-out.
    public class CommentService : ICommentService
    {
        public const string EntityTypeLookup = "commentEntityType";
        private const int MaxBodyLength = 4000;

        // @kullanici — harf/rakam/nokta/alt-çizgi/tire, 2-50 karakter.
        private static readonly Regex MentionRegex =
            new(@"@([A-Za-z0-9._-]{2,50})", RegexOptions.Compiled);

        private readonly MosaikContext _db;
        private readonly IContentSanitizer _sanitizer;
        private readonly INotificationService _notifications;
        private readonly IActiveUserDirectory _userDirectory;
        private readonly Mosaik.Core.Lookup.ILookupService _lookup;
        private readonly ILogger<CommentService> _logger;

        public CommentService(
            MosaikContext db,
            IContentSanitizer sanitizer,
            INotificationService notifications,
            IActiveUserDirectory userDirectory,
            Mosaik.Core.Lookup.ILookupService lookup,
            ILogger<CommentService> logger)
        {
            _db = db;
            _sanitizer = sanitizer;
            _notifications = notifications;
            _userDirectory = userDirectory;
            _lookup = lookup;
            _logger = logger;
        }

        // entityType → firma kolonu taşıyan tablo (SABİT whitelist — user-input DEĞİL → SQL-injection yok).
        private static readonly Dictionary<string, string> FirmaTableByEntityType = new(StringComparer.Ordinal)
        {
            ["contract"] = "Contracts",
            ["sopDocument"] = "SopDocuments"
        };

        public async Task<ServiceResult<Comment>> AddAsync(
            string entityType, int entityId, IReadOnlyCollection<int> allowedFirmaIds,
            int authorId, string authorName, string rawBody, string targetUrl,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
                return ServiceResult<Comment>.Failure("Yorum boş olamaz.");
            if (rawBody.Length > MaxBodyLength)
                return ServiceResult<Comment>.Failure($"Yorum en fazla {MaxBodyLength} karakter olabilir.");

            var valid = await _lookup.GetByCodeAsync(EntityTypeLookup, entityType);
            if (valid == null)
                return ServiceResult<Comment>.Failure("Geçersiz varlık türü.");

            // SUNUCU-TARAFI firma çözümü: client'tan gelen firmaId'ye güvenme (multi-tenant write-path).
            var firmaId = await ResolveEntityFirmaAsync(entityType, entityId, ct);
            if (firmaId == null)
                return ServiceResult<Comment>.Failure("Hedef kayıt bulunamadı.");
            if (!allowedFirmaIds.Contains(firmaId.Value))
                return ServiceResult<Comment>.Failure("Bu kayda erişim yetkiniz yok.");

            var sanitized = _sanitizer.Sanitize(rawBody);
            if (string.IsNullOrWhiteSpace(sanitized))
                return ServiceResult<Comment>.Failure("Yorum geçerli içerik barındırmıyor.");
            // Sanitize tag normalize ederken uzayabilir → kolon sınırını aşmadan kes/reddet.
            if (sanitized.Length > MaxBodyLength)
                return ServiceResult<Comment>.Failure($"Yorum en fazla {MaxBodyLength} karakter olabilir.");

            var comment = new Comment
            {
                FirmaId = firmaId.Value,
                EntityType = entityType,
                EntityId = entityId,
                Body = sanitized,
                AuthorId = authorId,
                AuthorName = authorName,
                CreatedAt = DateTime.UtcNow
            };
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync(ct);

            await FanOutMentionsAsync(comment, rawBody, firmaId.Value, targetUrl, ct);

            return ServiceResult<Comment>.Ok(comment);
        }

        // Hedef varlığın gerçek FirmaId'sini DB'den çöz. Tablo adı sabit whitelist'ten,
        // entityId parametreli — SQL-injection yok. Bilinmeyen tip/kayıt → null.
        private async Task<int?> ResolveEntityFirmaAsync(string entityType, int entityId, CancellationToken ct)
        {
            if (!FirmaTableByEntityType.TryGetValue(entityType, out var table))
                return null;
            var sql = $"SELECT FirmaId AS Value FROM dbo.{table} WHERE Id = {{0}}";
            var firmas = await _db.Database.SqlQueryRaw<int>(sql, entityId).ToListAsync(ct);
            return firmas.Count > 0 ? firmas[0] : null;
        }

        // @mention kullanıcı adlarını ham gövdeden çıkarır (case-insensitive distinct).
        public static List<string> ExtractMentions(string rawBody)
        {
            if (string.IsNullOrEmpty(rawBody))
                return new List<string>();
            return MentionRegex.Matches(rawBody)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public Task<List<Comment>> GetForEntityAsync(
            string entityType, int entityId, int firmaId, CancellationToken ct = default) =>
            _db.Comments.AsNoTracking()
                .Where(c => c.EntityType == entityType && c.EntityId == entityId && c.FirmaId == firmaId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);

        // @mention → aktif kullanıcı → bildirim. Yazar kendini mention'lasa atlanır.
        // Bildirim hatası yorumu geçersiz kılmamalı (yorum zaten kaydedildi) — yut + logla.
        private async Task FanOutMentionsAsync(
            Comment comment, string rawBody, int firmaId, string targetUrl, CancellationToken ct)
        {
            try
            {
                var usernames = ExtractMentions(rawBody);
                if (usernames.Count == 0)
                    return;

                var users = await _userDirectory.GetActiveUsersAsync(firmaId);
                var byName = users.ToDictionary(u => u.Username, u => u.UserId, StringComparer.OrdinalIgnoreCase);

                var targetIds = usernames
                    .Where(byName.ContainsKey)
                    .Select(n => byName[n])
                    .Where(id => id != comment.AuthorId)
                    .Distinct()
                    .ToList();
                if (targetIds.Count == 0)
                    return;

                await _notifications.CreateBulkIfNotExistsAsync(
                    externalKeyPrefix: $"mention:{comment.Id}",
                    userIds: targetIds,
                    entityType: comment.EntityType,
                    entityId: comment.EntityId,
                    title: $"{comment.AuthorName} sizden bir yorumda bahsetti",
                    message: null,
                    targetUrl: targetUrl,
                    notificationType: "mention",
                    createdBy: comment.AuthorId.ToString());
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // gerçek iptal/shutdown — best-effort fan-out hatası olarak yutma (error-handling #4)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CommentService: CommentId={Id} mention fan-out hatası (yorum kaydedildi).", comment.Id);
            }
        }
    }
}
