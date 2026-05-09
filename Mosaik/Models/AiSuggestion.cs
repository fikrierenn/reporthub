using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    public class AiSuggestion : BaseEntity<int>
    {
        [Required]
        [BindNever]
        public int FirmaId { get; set; }

        [Required]
        [BindNever]
        public int ExtractionId { get; set; }

        public SuggestionType SuggestionType { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public string? SuggestionDataJson { get; set; }

        public Confidence Confidence { get; set; } = Confidence.Medium;

        public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;

        [MaxLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public int? CreatedObligationId { get; set; }

        public int? CreatedEventId { get; set; }

        [BindNever, ValidateNever]
        public Firma Firma { get; set; } = null!;

        [BindNever, ValidateNever]
        public ContractAiExtraction Extraction { get; set; } = null!;
    }
}
