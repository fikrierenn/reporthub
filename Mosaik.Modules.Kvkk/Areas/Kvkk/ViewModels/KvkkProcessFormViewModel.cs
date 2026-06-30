using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Kvkk.Areas.Kvkk.ViewModels
{
    // Plan 40 Faz 2b — KVİE süreç oluştur/düzenle form DTO (mass-assignment koruması:
    // FirmaId/ReviewStatus/CreatedAt/CreatedBy form'da YOK — sunucu atar).
    public class KvkkProcessFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Süreç adı zorunlu.")]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departman zorunlu.")]
        [MaxLength(120)]
        public string Department { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? Unit { get; set; }

        [MaxLength(200)]
        public string? Owner { get; set; }

        [Required(ErrorMessage = "İşleme amacı zorunlu.")]
        public string Purpose { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Hukuki sebep seçilmeli.")]
        public int LegalBasisId { get; set; }

        public int? ProcessingPurposeId { get; set; }
        public int? RetentionRuleId { get; set; }
        public int? DisposalMethodId { get; set; }

        [MaxLength(500)] public string? DataSource { get; set; }
        [MaxLength(500)] public string? StorageMedium { get; set; }
        [MaxLength(500)] public string? AccessAuthority { get; set; }
        [MaxLength(500)] public string? RecipientGroups { get; set; }

        [Range(0, 2)] public byte RiskLevel { get; set; } = 1;

        // --- Form doldurma seçenekleri (POST'ta kullanılmaz) ---
        public List<Opt> LegalBases { get; set; } = new();
        public List<Opt> ProcessingPurposes { get; set; } = new();
        public List<Opt> RetentionRules { get; set; } = new();
        public List<Opt> DisposalMethods { get; set; } = new();

        public record Opt(int Id, string Name);
    }
}
