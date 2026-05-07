using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Tamim.Models
{
    // Plan 17 v2 — Tamim ↔ GunlukBlok junction (zarf-içerik bağı).
    // UNIQUE: (TamimId, BlokId)
    public class TamimBlok : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int TamimId { get; set; }

        public int BlokId { get; set; }

        public int SiraNo { get; set; }
    }
}
