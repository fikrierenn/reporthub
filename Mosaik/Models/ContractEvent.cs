using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    public class ContractEvent : BaseEntity<int>
    {
        [Required]
        [BindNever]
        public int FirmaId { get; set; }

        [BindNever]
        public int? ContractId { get; set; }

        [BindNever]
        public int? ObligationId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }

        public EventType EventType { get; set; } = EventType.Deadline;

        public EventStatus Status { get; set; } = EventStatus.Upcoming;

        public int ReminderDays { get; set; } = 7;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public EventSource Source { get; set; } = EventSource.Manual;

        [BindNever, ValidateNever]
        public Firma Firma { get; set; } = null!;

        [BindNever, ValidateNever]
        public Contract? Contract { get; set; }

        [BindNever, ValidateNever]
        public ContractObligation? Obligation { get; set; }
    }
}
