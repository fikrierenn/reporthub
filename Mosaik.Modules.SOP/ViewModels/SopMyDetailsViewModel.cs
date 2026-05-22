using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.ViewModels
{
    // Plan 34 Faz D — User detay (okuma sayfası).
    public class SopMyDetailsViewModel
    {
        public SopDocument Document { get; set; } = null!;
        public SopVersion Version { get; set; } = null!;
        public SopReadReceipt? Receipt { get; set; }                  // null → atanmamış (admin önizleme)
        public DateTime Deadline => Receipt?.AssignedAt.AddDays(Document.ReadDeadlineDays) ?? DateTime.UtcNow;
        public int DaysRemaining => (int)Math.Floor((Deadline - DateTime.UtcNow).TotalDays);
        public bool CanConfirm => Receipt != null && Receipt.ConfirmedAt == null;
        public bool IsConfirmed => Receipt?.ConfirmedAt != null;
    }
}
