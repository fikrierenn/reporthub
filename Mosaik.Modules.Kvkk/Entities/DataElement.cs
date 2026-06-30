namespace Mosaik.Modules.Kvkk.Entities
{
    // Plan 40 (M6) — atomic veri öğesi (ad-soyad, TC, parmak izi, CCTV, IBAN...).
    // Reverse navigation çekirdeği: "bu veri nerede işleniyor?" → ProcessDataLink.
    public class DataElement
    {
        public int Id { get; set; }
        public string ElementCode { get; set; } = string.Empty;  // "person.fullname", "biometric.fingerprint"
        public string DisplayName { get; set; } = string.Empty;  // "Ad-Soyad"
        public int DataCategoryId { get; set; }
        public bool IsSpecialCategory { get; set; }              // m.6 mı (kategoriyle tutarlı)
        public string? DefaultRetentionHint { get; set; }
        public int? DefaultLegalBasisHintId { get; set; }
        public string? Aliases { get; set; }                    // arama için CSV ("isim,ad,name")
        public bool IsActive { get; set; } = true;

        public DataCategory? DataCategory { get; set; }
        public ICollection<ProcessDataLink> ProcessLinks { get; set; } = new List<ProcessDataLink>();
    }
}
