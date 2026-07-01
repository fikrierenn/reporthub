using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    // Plan 41 Faz 3 — public link token. HMAC+secret yerine random-token + hash-storage
    // (reset-password pattern): yüksek-entropili rastgele token üret, SHA256 hash'ini DB'ye
    // yaz, plaintext'i URL'e ver. DB leak olsa bile token geri üretilemez, secret yönetimi yok.
    public class PublicTokenService(DbContext db)
    {
        private const int TokenBytes = 32; // 256-bit entropi

        public sealed record CreatedToken(string PlainToken, int TokenId);

        public async Task<ServiceResult<CreatedToken>> CreateAsync(
            int formDefinitionId, int firmaId, int createdBy, DateTime expiresAt, int maxUses,
            string? recipientEmail, CancellationToken ct = default)
        {
            var owned = await db.Set<FormDefinition>().AsNoTracking()
                .AnyAsync(d => d.Id == formDefinitionId && d.FirmaId == firmaId && d.IsPublic, ct);
            if (!owned)
                return ServiceResult<CreatedToken>.Failure("Form bulunamadı veya public erişime kapalı.");

            if (maxUses < 1)
                return ServiceResult<CreatedToken>.Failure("Kullanım sayısı en az 1 olmalı.");
            if (expiresAt <= DateTime.UtcNow)
                return ServiceResult<CreatedToken>.Failure("Son kullanma tarihi gelecekte olmalı.");

            var plain = ToUrlToken(RandomNumberGenerator.GetBytes(TokenBytes));
            var token = new PublicFormToken
            {
                FormDefinitionId = formDefinitionId,
                TokenHash = Hash(plain),
                RecipientEmail = recipientEmail,
                ExpiresAt = expiresAt,
                MaxUses = maxUses,
                CreatedBy = createdBy
            };
            db.Set<PublicFormToken>().Add(token);
            await db.SaveChangesAsync(ct);
            return ServiceResult<CreatedToken>.Ok(new CreatedToken(plain, token.Id));
        }

        // Token geçerli mi (form eşleşiyor + süresi geçmemiş + kullanım hakkı var). Timing-safe eşleme.
        public async Task<PublicFormToken?> ValidateAsync(int formDefinitionId, string plainToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(plainToken))
                return null;

            var hash = Hash(plainToken);
            // Hash unique — tek eşleşme; timing-safe compare DB tarafında değil, hash eşitliğiyle.
            var candidates = await db.Set<PublicFormToken>()
                .Where(t => t.FormDefinitionId == formDefinitionId)
                .ToListAsync(ct);

            foreach (var t in candidates)
            {
                if (!CryptographicOperations.FixedTimeEquals(t.TokenHash, hash))
                    continue;
                if (t.ExpiresAt <= DateTime.UtcNow)
                    return null; // süresi geçmiş
                if (t.UsedCount >= t.MaxUses)
                    return null; // kullanım hakkı bitmiş
                return t;
            }
            return null;
        }

        // Kullanım hakkını ATOMİK rezerve et (submit'ten ÖNCE çağrılır — over-use imkansız).
        // WHERE UsedCount<MaxUses tek UPDATE'te; concurrent submit'te yalnız biri kazanır.
        // Dönüş false = slot dolu/expired (render→submit arası tükenmiş) → submit reddedilir.
        public async Task<bool> ConsumeAsync(int tokenId, CancellationToken ct = default)
        {
            var affected = await db.Set<PublicFormToken>()
                .Where(t => t.Id == tokenId && t.UsedCount < t.MaxUses && t.ExpiresAt > DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedCount, t => t.UsedCount + 1), ct);
            return affected > 0;
        }

        // Rezerve edildi ama submit başarısız oldu (validation hatası) → slot iade (kullanıcı tekrar denesin).
        public async Task RefundAsync(int tokenId, CancellationToken ct = default)
        {
            await db.Set<PublicFormToken>()
                .Where(t => t.Id == tokenId && t.UsedCount > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedCount, t => t.UsedCount - 1), ct);
        }

        private static byte[] Hash(string plain) =>
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plain));

        // URL-safe base64 (padding/+/ temizlenir).
        private static string ToUrlToken(byte[] bytes) =>
            Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
