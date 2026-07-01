namespace Mosaik.Modules.Kvkk.Entities
{
    // Plan 40 (M6) Faz 5 — AI Integrity Checker çıktısı. 8 saf-C# pattern (Presidio ertelendi).
    // ProcessId NULL = firma-seviyesi bulgu (örn. "ihlal yönetim süreci yok").
    public class KvkkIntegrityFinding
    {
        public int Id { get; set; }
        public int FirmaId { get; set; }

        public string PatternCode { get; set; } = string.Empty;  // sabit detector kimliği (KvkkIntegrityPatterns.*)
        public byte Severity { get; set; }                       // 0 İdari 1 İdari-Yüksek 2 Kritik
        public int? ProcessId { get; set; }
        public string Description { get; set; } = string.Empty;

        public DateTime FirstDetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastDetectedAt { get; set; } = DateTime.UtcNow;

        public bool IsDismissed { get; set; }
        public int? DismissedBy { get; set; }
        public DateTime? DismissedAt { get; set; }
        public string? DismissReason { get; set; }

        public KvkkProcess? Process { get; set; }
    }
}
