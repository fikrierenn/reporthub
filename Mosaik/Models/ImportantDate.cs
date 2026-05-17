using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Mosaik.Models
{
    public class ImportantDate
    {
        public int Id { get; set; }

        [Required]
        [BindNever]
        public int FirmaId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int ReminderDays { get; set; } = 7;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever, ValidateNever]
        public Firma Firma { get; set; } = null!;
    }
}
