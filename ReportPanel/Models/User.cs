using System.ComponentModel.DataAnnotations;

namespace ReportPanel.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// DEPRECATED (M-03 Faz B). Rol bilgisi artık UserRole junction tablosundan okunur.
        /// Bu alan Faz C'de DB'den drop edilecek; o zamana kadar legacy audit history
        /// okumalar için korunuyor ama yeni yazım yolları burada string.Empty bırakır
        /// veya NULL geçer. Migration: Database/15_NullableUserRolesCsv.sql. ADR: docs/ADR/003-role-model.md.
        /// </summary>
        [Obsolete("User.Roles CSV deprecate (M-03). UserRole junction kullan. Faz C'de drop edilecek.")]
        [MaxLength(200)]
        public string? Roles { get; set; }

        public bool IsAdUser { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
