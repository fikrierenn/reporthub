namespace Mosaik.Modules.Kvkk.Entities
{
    // Plan 40 (M6) Faz 3 — SOP↔Process bağı. Junction sahibi KVKK modülü (ADR-002: SOP
    // modülü Kvkk'ya csproj referansı veremez). SopDocumentId FK'siz (cross-modül) —
    // SopTitle snapshot link anında yazılır (SOP rename'de eskiyebilir, kabul edilen risk).
    public class SopProcessLink
    {
        public int Id { get; set; }
        public int FirmaId { get; set; }
        public int ProcessId { get; set; }
        public int SopDocumentId { get; set; }
        public string SopTitle { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public KvkkProcess? Process { get; set; }
    }
}
