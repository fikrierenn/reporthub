using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.SOP.Entities
{
    // Plan 34 §2.1 — Okundu disiplini.
    // AssignedAt: kullanıcıya atama (SOP yayınlanıp departmanına atandığında).
    //             Read deadline countdown buradan başlar (AssignedAt + ReadDeadlineDays).
    // ReadAt: SOP detay ilk açıldığında set.
    // ConfirmedAt: "Okudum + onayladım" tıklandığında set (audit + departman yöneticisine bildirim).
    // ReminderSentCount: 7 gün kala + 1 gün kala sayacı (Faz E Hangfire).
    public class SopReadReceipt
    {
        public int Id { get; set; }

        [Required]
        public int SopVersionId { get; set; }
        public SopVersion? SopVersion { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public int ReminderSentCount { get; set; }
    }
}
