namespace Mosaik.Core.Comments
{
    // Plan 54 M5 — polymorphic yorum. EntityType (lookup "commentEntityType") + EntityId ile
    // herhangi bir varlığa (Contract/Circular/SOP...) bağlanır. FirmaId denormalize (ADR-012).
    // Body render-time DEĞİL kayıt-anında IContentSanitizer ile temizlenir. Flat — threading yok.
    public class Comment
    {
        public int Id { get; set; }

        // Denormalize firma sınırı — sorgu + güvenlik filtresi (ADR-012).
        public int FirmaId { get; set; }

        // Polymorphic hedef. EntityType lookup-validated, EntityId hedef PK.
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }

        // Sanitize edilmiş HTML gövde.
        public string Body { get; set; } = string.Empty;

        public int AuthorId { get; set; }

        // Denormalize gösterim adı — yazar silinse/değişse de yorum geçmişi okunur kalır.
        public string AuthorName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
