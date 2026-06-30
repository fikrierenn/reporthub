namespace Mosaik.Modules.Kvkk.Entities
{
    // Plan 40 (M6) — yurt dışı aktarım (KVKK m.9 yeni rejim). Pattern 6/9 denetimi için.
    public class CrossBorderTransfer
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public string RecipientName { get; set; } = string.Empty;  // "LinkedIn Inc."
        public string Country { get; set; } = string.Empty;        // "ABD"
        // 0 yeterlilik 1 SCC 2 BCR 3 taahhütname 4 arızi açık rıza 5 sözleşme ifası
        public byte Mechanism { get; set; }
        public string? LegalReference { get; set; }                // "m.9/2-a Standart Sözleşme"
        public string? DocumentLink { get; set; }                  // SCC PDF
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public KvkkProcess? Process { get; set; }
    }
}
