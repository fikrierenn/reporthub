using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.Entities
{
    // Plan 34.1 Faz 2 A-12 — SOP RAG advisor soru-cevap geçmişi.
    // KVKK retention: 1 yıl (Faz 6 Hangfire cleanup). Audit log ayrı 5-yıl.
    // SourceSopVersionIds: retrieval'da kullanılan SOP version'ları CSV ("12,34,56"),
    // top-K hit (Faz 2 sınırı: ≤4). Plan 34.1 §2 "Kaynaklar listesi" gösterimi için.
    public class SopAiConversation
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? FirmaId { get; set; }

        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        // CSV "12,34,56" — Plan 18B ↔ many-to-many refactor adayı.
        [MaxLength(500)]
        public string? SourceSopVersionIds { get; set; }

        public int? TokensIn { get; set; }
        public int? TokensOut { get; set; }

        // Lookup typeCode "sopAiFeedback": 0 None | 1 ThumbsUp | 2 ThumbsDown
        // Faz 5 feedback endpoint set eder.
        public byte UserFeedback { get; set; }

        [MaxLength(500)]
        public string? FeedbackNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FeedbackAt { get; set; }
    }
}
