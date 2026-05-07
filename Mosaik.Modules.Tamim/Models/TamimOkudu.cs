using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Tamim.Models
{
    // Plan 17 v2 — Tamim okuma logu. "Görmedim/duymadım" bahanesini bitirir.
    // UNIQUE: (TamimId, UserId) — bir kullanıcı bir tamim için tek kayıt.
    // Detail sayfa ziyaretinde IlkGorulme set, "Okudum" buton IsAcknowledged set.
    public class TamimOkudu : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int TamimId { get; set; }

        public int UserId { get; set; }    // cross-csproj FK Mosaik.Models.User.UserId

        public DateTime IlkGorulme { get; set; } = DateTime.UtcNow;

        public bool Okundu { get; set; }   // explicit "Okudum" buton

        public DateTime? OkumaZamani { get; set; }
    }
}
