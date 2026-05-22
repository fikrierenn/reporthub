using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    // Plan 25 — Sözleşmeye/yükümlülüğe ekli dosya.
    // "ContractFile" adı: "Document" Plan 19 Doküman modülüyle çakışıyor.
    // Depolama: wwwroot/uploads/contracts/{FirmaId}/{ContractId}/ (Plan 25.1: App_Data'ya taşınacak)
    public class ContractFile : BaseEntity<int>
    {
        // Multi-tenant güvenlik sınırı. Form'dan bind edilmez.
        [Required]
        [BindNever]
        public int FirmaId { get; set; }

        [BindNever]
        public int? ContractId { get; set; }

        [BindNever]
        public int? ObligationId { get; set; }

        [Required]
        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;    // ContentRoot-relative (App_Data/contracts/...)

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        public int Version { get; set; } = 1;

        // Plan 27 Faz C — PDF'den çıkarılan metin; FTS + ContentText LIKE arama için.
        public string? ContentText { get; set; }

        // Plan 27 Faz B — AI auto-classification + executive summary (yükleme sonrası).
        [MaxLength(2000)]
        public string? AiSummary { get; set; }
        public string? AiTagsJson { get; set; }     // {"documentType","year","department","tags":[...]}
        public DateTime? AiClassifiedAt { get; set; }

        // Nav properties — form'dan bind/validate edilmez (EF Include yükler).
        [BindNever, ValidateNever]
        public Firma Firma { get; set; } = null!;

        [BindNever, ValidateNever]
        public Contract? Contract { get; set; }

        [BindNever, ValidateNever]
        public ContractObligation? Obligation { get; set; }

        [BindNever, ValidateNever]
        public ICollection<ContractAiExtraction> AiExtractions { get; set; } = new List<ContractAiExtraction>();

        // Plan 27 Faz C — versiyonlama.
        [BindNever, ValidateNever]
        public ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();
    }
}
