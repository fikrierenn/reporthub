using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.1 — Public link HMAC token.
    public class PublicFormToken
    {
        public int Id { get; set; }

        [Required]
        public int FormDefinitionId { get; set; }
        public FormDefinition? FormDefinition { get; set; }

        [Required]
        public byte[] TokenHash { get; set; } = Array.Empty<byte>();   // HMAC-SHA256

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
