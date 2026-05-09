using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    // Plan 25 — AI metin çıkarma sonucu. AiExtractionWorker bu entity'yi günceller.
    // Status: Processing (iş başladı) → AwaitingReview (AI tamamladı) → Approved/Rejected (insan).
    // AiPipelineQueue: Channel<int> ile ContractFileId push edilir; worker sırayla işler.
    public class ContractAiExtraction : BaseEntity<int>
    {
        // Multi-tenant güvenlik sınırı. Form'dan bind edilmez.
        [Required]
        [BindNever]
        public int FirmaId { get; set; }

        [Required]
        [BindNever]
        public int ContractFileId { get; set; }

        [BindNever]
        public int? ContractId { get; set; }        // çıkarma onaylanınca set edilir

        public ExtractionStatus Status { get; set; } = ExtractionStatus.Processing;

        [MaxLength(50)]
        public string? PromptVersion { get; set; }  // "contract_v1" — prompt şablonu sürümü

        [MaxLength(100)]
        public string? ModelUsed { get; set; }      // AiSummaryResult.ModelUsed

        public string? RawText { get; set; }        // PDF'ten çıkan ham metin (PdfPig)
        public string? ExtractionResultJson { get; set; }   // AI'ın döndürdüğü JSON

        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        [MaxLength(100)]
        public string? ProgressStep { get; set; }   // "ocr", "ai_call", "parse" — debug için

        // Nav properties — form'dan bind/validate edilmez.
        [BindNever, ValidateNever]
        public Firma Firma { get; set; } = null!;

        [BindNever, ValidateNever]
        public ContractFile ContractFile { get; set; } = null!;

        [BindNever, ValidateNever]
        public Contract? Contract { get; set; }
    }
}
