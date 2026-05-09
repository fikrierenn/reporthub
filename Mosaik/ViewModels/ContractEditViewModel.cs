using Mosaik.Models;
using System.ComponentModel.DataAnnotations;

namespace Mosaik.ViewModels
{
    public class ContractEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Counterparty { get; set; }

        public ContractCategory Category { get; set; }
        public ContractStatus Status { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
