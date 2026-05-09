using System.ComponentModel.DataAnnotations;
using Mosaik.Models;

namespace Mosaik.ViewModels
{
    // Plan 25 — Contract entity'sini direkt bind etmek yerine ViewModel: nav property
    // implicit required + RecurringObligationInput[] çoklu satır binding cleanly çalışsın.
    public class ContractCreateViewModel
    {
        [Required]
        public int FirmaId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Counterparty { get; set; }

        public ContractCategory Category { get; set; } = ContractCategory.Other;
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        // Boş liste → recurring üretim yok. Liste varsa generator devreye girer.
        public List<RecurringObligationInput> Recurrences { get; set; } = new();
    }
}
