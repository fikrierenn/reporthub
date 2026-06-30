namespace Mosaik.Modules.Kvkk.Entities
{
    // Plan 40 (M6) — KVKK süreç tanımı (KVİE satırı, central entity). TANIM tarafı;
    // runtime execution Plan 42 (ProcessInstance). FirmaId denormalize (ADR-012).
    public class KvkkProcess
    {
        public int Id { get; set; }
        public int FirmaId { get; set; }

        public string Department { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string? Owner { get; set; }                  // Süreç Sahibi (rol/kişi)
        public string Name { get; set; } = string.Empty;    // Faaliyet Adı
        public string Purpose { get; set; } = string.Empty; // İşleme Amacı (serbest metin)

        public int LegalBasisId { get; set; }
        public int? ProcessingPurposeId { get; set; }       // standart amaç (opsiyonel eşleme)
        public string? DataSource { get; set; }             // Veri Kaynağı
        public string? StorageMedium { get; set; }          // Saklandığı Ortam
        public string? AccessAuthority { get; set; }        // Erişim Yetkisi
        public string? RecipientGroups { get; set; }        // Aktarılan Alıcı Grupları (serbest)
        public int? RetentionRuleId { get; set; }
        public int? DisposalMethodId { get; set; }

        public byte RiskLevel { get; set; }                 // 0 Düşük 1 Orta 2 Yüksek
        public byte ReviewStatus { get; set; }              // 0 Taslak 1 Birim onayı 2 KVKK onay 3 VERBİS yayında
        public DateTime? LastReviewedAt { get; set; }
        public int? LastReviewedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public LegalBasis? LegalBasis { get; set; }
        public RetentionRule? RetentionRule { get; set; }
        public DisposalMethod? DisposalMethod { get; set; }
        public ProcessingPurpose? ProcessingPurpose { get; set; }
        public ICollection<ProcessDataLink> DataLinks { get; set; } = new List<ProcessDataLink>();
        public ICollection<CrossBorderTransfer> CrossBorderTransfers { get; set; } = new List<CrossBorderTransfer>();
    }
}
