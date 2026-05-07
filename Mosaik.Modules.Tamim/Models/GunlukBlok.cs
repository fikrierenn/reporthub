using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Tamim.Models
{
    // Plan 17 v2 (sadeleştirme 2026-05-08) — Genel Müdürlük çalışanlarının gün
    // içinde girdiği duyuru/karar/uyarı/hadise kaydı. 17:00 cron job o günkü
    // blokları toplayıp Tamim zarfına bağlar (TamimId set).
    //
    // Durum bilgisi: TamimId IS NULL = bekliyor, IS NOT NULL = yayında.
    // Onay akışı YOK (basitlik). Editor onayı Plan 17.1'e ertelendi.
    public class GunlukBlok : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // BLK-YYYYMMDD-NNN otomatik (BlokService set eder)
        [Required]
        [MaxLength(50)]
        [BindNever]
        public string BlokNo { get; set; } = string.Empty;

        [BindNever]
        public int OlusturanId { get; set; }   // User.UserId cross-csproj FK

        [Required]
        [MaxLength(100)]
        public string DepartmanAdi { get; set; } = string.Empty;  // Plan 18B sonrası FK

        [Required]
        [MaxLength(200)]
        public string Konu { get; set; } = string.Empty;

        [Required]
        public string Aciklama { get; set; } = string.Empty;

        public DateTime BlokTarihi { get; set; } = DateTime.UtcNow.Date;

        // DictionaryValue.Id (Mosaik.Core.Lookup, Code="blokTuru")
        public int BlokTuruId { get; set; }

        public bool Acil { get; set; }

        // Tamim'e bağlandı mı? NULL=bekliyor, NOT NULL=yayında.
        public int? TamimId { get; set; }

        // Soft-delete (yayınlanmamış blokları kaldır)
        public bool IsActive { get; set; } = true;
    }
}
