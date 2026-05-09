using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mosaik.Models
{
    // ADR-012 — firma = güvenlik sınırı. Tüm vNext entity'lerde FirmaId FK zorunlu.
    // Seed: BKM_GENEL=1, BURSA_KÜLTÜR_MERKEZİ=2, ASİYE_BİNGÖLBALİ=3 (migration 42).
    public class Firma
    {
        [Key]
        [BindNever]
        public int FirmaId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;   // "BKM_GENEL" — Zirve DB kodu

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;   // "BKMKİTAP"

        public bool IsActive { get; set; } = true;
    }
}
