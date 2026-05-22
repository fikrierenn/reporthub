using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.ViewModels
{
    // Plan 34 Faz D — "Prosedürlerim" liste item.
    // DaysRemaining: pozitif → süre kaldı; sıfır/negatif → geçti (kırmızı uyarı).
    public class SopMyListItemViewModel
    {
        public int ReceiptId { get; set; }
        public int VersionId { get; set; }
        public int DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public int DaysRemaining => (int)Math.Floor((Deadline - DateTime.UtcNow).TotalDays);
        public bool IsOverdue => ConfirmedAt == null && DaysRemaining < 0;
        public bool IsConfirmed => ConfirmedAt != null;

        public static SopMyListItemViewModel FromReceipt(SopReadReceipt r)
        {
            var doc = r.SopVersion?.SopDocument;
            var deadlineDays = doc?.ReadDeadlineDays ?? 30;
            return new SopMyListItemViewModel
            {
                ReceiptId = r.Id,
                VersionId = r.SopVersionId,
                DocumentId = doc?.Id ?? 0,
                Title = doc?.Title ?? "—",
                VersionNumber = r.SopVersion?.VersionNumber ?? 0,
                AssignedAt = r.AssignedAt,
                Deadline = r.AssignedAt.AddDays(deadlineDays),
                ReadAt = r.ReadAt,
                ConfirmedAt = r.ConfirmedAt
            };
        }
    }
}
