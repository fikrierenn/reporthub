using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.Entities
{
    // Plan 34.1 Faz 1 A-07 — SOP RAG retrieval birimi.
    // Bir Approved SopVersion → N chunk (512 token + 64 overlap, A-09 chunker).
    // EmbeddingJson: float[768] JSON (e5-base, L2 normalize). Cosine search dot product.
    // Plan 44: chunk-level permission guard — SecurityLevel + whitelist CSV kolonları.
    public class SopChunk
    {
        public int Id { get; set; }

        [Required]
        public int SopVersionId { get; set; }
        public SopVersion? SopVersion { get; set; }

        public int ChunkOrder { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        // float[768] JSON serialize. SQL VECTOR (2025) gelirse refactor.
        [Required]
        public string EmbeddingJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Plan 44 — chunk-level permission guard
        // SecurityLevel: 0=public, 1=internal, 2=confidential, 3=restricted
        public byte SecurityLevel { get; set; } = Internal;

        // CSV whitelist — null = tüm kullanıcılar erişebilir
        public string? AllowedRoleIds { get; set; }
        public string? AllowedDepartmentIds { get; set; }
        public string? AllowedUserIds { get; set; }

        // SecurityLevel sabit isimleri
        public const byte Public       = 0;
        public const byte Internal     = 1;
        public const byte Confidential = 2;
        public const byte Restricted   = 3;
    }
}
