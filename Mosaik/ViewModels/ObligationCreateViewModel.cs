using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mosaik.Models;

namespace Mosaik.ViewModels
{
    public class ObligationCreateViewModel
    {
        public int FirmaId { get; set; }
        public int? ContractId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public ObligationCategory Category { get; set; } = ObligationCategory.Finance;
        public ObligationType Type { get; set; } = ObligationType.Payment;

        [Required]
        public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

        public int ReminderDays { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Amount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "TRY";

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
