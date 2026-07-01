namespace Mosaik.Modules.Kvkk.Entities
{
    // Plan 40 (M6) Faz 3 — SOP içeriği taranınca (DataElementMatcher) bulunan veri öğeleri.
    // KvkkIntegrityChecker Pattern 8 bu tabloyu ProcessDataLink'le karşılaştırır (cross-modül
    // SopDocument entity'sine hiç dokunmadan — Faz 4 precedent).
    public class SopScannedElement
    {
        public int Id { get; set; }
        public int FirmaId { get; set; }
        public int SopDocumentId { get; set; }
        public int DataElementId { get; set; }
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

        public DataElement? DataElement { get; set; }
    }
}
