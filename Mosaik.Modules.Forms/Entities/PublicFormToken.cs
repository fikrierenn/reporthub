using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.1 — Public link token. Random 256-bit token (reset-password pattern);
    // DB'de SADECE SHA256 hash saklanır (HMAC+secret DEĞİL — secret yönetimi yok, leak-safe).
    public class PublicFormToken
    {
        public int Id { get; set; }

        [Required]
        public int FormDefinitionId { get; set; }
        public FormDefinition? FormDefinition { get; set; }

        [Required]
        public byte[] TokenHash { get; set; } = Array.Empty<byte>();   // SHA256(random-token)

        [MaxLength(200)]
        public string? RecipientEmail { get; set; }

        public DateTime ExpiresAt { get; set; }
        public int MaxUses { get; set; } = 1;
        public int UsedCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int CreatedBy { get; set; }
    }
}
