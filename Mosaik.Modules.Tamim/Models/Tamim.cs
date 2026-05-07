using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Tamim.Models
{
    // Plan 17 v2 — Tamim ZARFI (envelope). Body içermez.
    // İçerik = TamimBlok junction üzerinden bağlı bir veya birden fazla GunlukBlok.
    // 17:00 cron job tarafından otomatik oluşturulur.
    public class Tamim : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        // TAM-YYYYMMDD (cron job set eder, günde bir tamim)
        [Required]
        [MaxLength(50)]
        [BindNever]
        public string TamimNo { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Baslik { get; set; } = string.Empty;  // "08 Mayıs 2026 Günlük Tamim"

        public DateTime TamimTarihi { get; set; }   // Hangi günün tamimi (00:00:00 baz)

        public DateTime YayinTarihi { get; set; }   // Cron yayın anı

        public int BlokSayisi { get; set; }         // denormalized — TamimBlok COUNT

        public bool Acil { get; set; }              // bağlı bloklardan biri Acil ise true
    }
}
