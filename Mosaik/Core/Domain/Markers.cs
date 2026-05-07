namespace Mosaik.Core.Domain
{
    // Marker interface'leri. Servis katmanı bunlara göre branch alır
    // (örn. ISoftDeletable repository'sinde Where(x => !x.IsDeleted)).

    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        string? CreatedBy { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }
    }

    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
    }

    // Cross-modül izlenebilirlik (YonetIQ TaskItem.SourceEntityType pattern):
    // örn. Tamim'den çıkarılan görev kaynağını taşır.
    public interface ISourceTraceable
    {
        string? SourceEntityType { get; set; }
        int? SourceEntityId { get; set; }
    }
}
