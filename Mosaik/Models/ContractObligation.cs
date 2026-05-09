using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    // Plan 25 — Yükümlülük. Sözleşmeye bağlı veya bağımsız olabilir.
    // ParentObligationId: tekrar üretilen yükümlülüğün orijinaline işaret eder.
    public class ContractObligation : BaseEntity<int>
    {
        // Multi-tenant güvenlik sınırı. Form'dan bind edilmez.
        [Required]
        [BindNever]
        public int FirmaId { get; set; }

        [BindNever]
        public int? ContractId { get; set; }    // null = sözleşmesiz bağımsız yükümlülük

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public ObligationCategory Category { get; set; } = ObligationCategory.Finance;
        public ObligationType Type { get; set; } = ObligationType.Payment;

        public decimal? Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "TRY";

        public DateOnly DueDate { get; set; }
        public int? ReminderDays { get; set; }   // vade öncesi kaç gün bildirim

        public ObligationStatus Status { get; set; } = ObligationStatus.Pending;
        public ObligationSource Source { get; set; } = ObligationSource.Manual;

        [BindNever]
        public int? RecurrenceId { get; set; }

        [BindNever]
        public int? ParentObligationId { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [BindNever]
        public DateTime? CompletedAt { get; set; }

        [BindNever]
        public string? CompletedBy { get; set; }

        // Nav properties — form'dan bind/validate edilmez (EF Include yükler).
        [BindNever, ValidateNever]
        public Firma Firma { get; set; } = null!;
        [BindNever, ValidateNever]
        public Contract? Contract { get; set; }
        [BindNever, ValidateNever]
        public ContractRecurrence? Recurrence { get; set; }
        [BindNever, ValidateNever]
        public ContractObligation? ParentObligation { get; set; }
        [BindNever, ValidateNever]
        public ICollection<ContractObligation> ChildObligations { get; set; } = new List<ContractObligation>();
    }
}
