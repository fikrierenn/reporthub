using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Mosaik.Core.Domain;

namespace Mosaik.Models
{
    // Plan 25 — Sözleşme. ADR-012: int PK, FirmaId zorunlu güvenlik sınırı.
    public class Contract : BaseEntity<int>
    {
        // Multi-tenant güvenlik sınırı. Form'dan bind edilmez (Edit POST'ta value korunur).
        [Required]
        [BindNever]
        public int FirmaId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Counterparty { get; set; }       // Karşı taraf

        public ContractCategory Category { get; set; } = ContractCategory.Other;

        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        // Plan 33 — AI wizard çıkardığı alanlar (migration 61).
        public decimal? ContractValue { get; set; }

        [MaxLength(8)]
        public string? Currency { get; set; }

        [MaxLength(200)]
        public string? GoverningLaw { get; set; }

        public bool? AutoRenewal { get; set; }

        public bool? KvkkInvolved { get; set; }

        // Nav properties — [ValidateNever] + [BindNever]: form bind etmez, validator non-nullable
        // reference type için implicit required üretmez.
        [BindNever, ValidateNever]
        public Firma Firma { get; set; } = null!;

        [BindNever, ValidateNever]
        public ICollection<ContractObligation> Obligations { get; set; } = new List<ContractObligation>();

        [BindNever, ValidateNever]
        public ICollection<ContractFile> Files { get; set; } = new List<ContractFile>();
    }
}
