using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Core.Domain;

namespace Mosaik.Modules.Tamim.Models
{
    // Plan 17 Faz D — okundu kaydı (kim, ne zaman). Faz B'de tablo oluşturulur,
    // insert mantığı Faz D'de eklenir. Unique: (TamimId, UserId).
    public class TamimReadLog : BaseEntity
    {
        [Key]
        [BindNever]
        public int Id { get; set; }

        public int TamimId { get; set; }

        public int UserId { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        // Manuel "Okudum" butonu basıldı mı (otomatik görüntüleme vs explicit ack)
        public bool IsAcknowledged { get; set; }
    }
}
