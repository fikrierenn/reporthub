using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Models
{
    // M-07: kritik alanlara [BindNever] — mass assignment koruması (defansif).
    // Form binding ile UserId/PasswordHash/timestamp set edilemez; service layer set eder.
    public class User
    {
        [Key]
        [BindNever]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        // [BindNever] — form'dan gelmez, UserManagementService.PasswordHasher set eder.
        // [Required] kaldırıldı: ModelState validation'ı çalışıyor ama field bind edilmediği
        // için empty default değer her zaman fail veriyordu. DB NOT NULL constraint zaten var.
        [MaxLength(255)]
        [BindNever]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsAdUser { get; set; }

        public bool IsActive { get; set; } = true;

        // Plan 57 Part C — Zirve personel kodu köprüsü ("4634-BKM"). Amir çözümleme
        // (IManagerResolver) OrgPositions.HolderPersonelno ↔ bu alan üzerinden User bulur.
        // Trim'lenmiş saklanır; filtered-unique index (bir personel = bir user).
        [MaxLength(50)]
        public string? Personelno { get; set; }

        // ADR-012 — firma güvenlik sınırı (çoklu erişim CSV).
        // Format: "1,2,3" veya "1" veya NULL. NULL/boş = modül kapalı.
        // Login'de claim'lere parse edilir.
        // [BindNever]: form'da FirmaIds checkbox grup olarak gelir (multi-value);
        // string'e bind çakışır → service layer'da BuildUserFormInput manuel okur.
        [BindNever]
        public string? FirmaIds { get; set; }

        [BindNever]
        public DateTime? LastLoginAt { get; set; }

        [BindNever]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
