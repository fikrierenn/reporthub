using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Core.Domain
{
    // Plan 16.5 Faz A — tüm vNext entity'leri için ortak audit alanları.
    // YonetIQ port. Mass assignment koruması: PK + timestamp [BindNever].
    public abstract class BaseEntity
    {
        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever]
        public string? CreatedBy { get; set; }

        [BindNever]
        public DateTime? UpdatedAt { get; set; }

        [BindNever]
        public string? UpdatedBy { get; set; }
    }

    public abstract class BaseEntity<TKey> : BaseEntity where TKey : notnull
    {
        [BindNever]
        public TKey Id { get; set; } = default!;
    }
}
