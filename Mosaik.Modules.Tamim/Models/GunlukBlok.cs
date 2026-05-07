using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Tamim.Models
{
    public enum BlokDurum
    {
        Taslak = 0,        // GM çalışanı yazmış, henüz onaylanmamış
        Onayli = 1,        // Editor onayladı, 17:00 cron'da Tamim'e dahil olacak
        Reddedildi = 2,    // Editor reddetti
        Yayinda = 3        // Tamim yayınlandı (TamimBlok junction kuruldu)
    }

    // Plan 17 v2 — Genel Müdürlük çalışanlarının gün içinde girdiği duyuru/karar/uyarı.
    // 17:00'de cron job onaylı blokları Tamim zarfına derleyip yayınlar.
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

        public BlokDurum Durum { get; set; } = BlokDurum.Taslak;

        public bool Acil { get; set; }

        [MaxLength(500)]
        public string? RedSebebi { get; set; }

        public int? OnaylayanId { get; set; }

        public DateTime? OnayTarihi { get; set; }
    }
}
