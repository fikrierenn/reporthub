namespace Mosaik.Modules.Kvkk
{
    // Plan 40 Faz 2 — byte kod → Türkçe etiket (UI + test edilebilir saf eşleme).
    public static class KvkkLabels
    {
        public static string Risk(byte v) => v switch
        {
            0 => "Düşük", 1 => "Orta", 2 => "Yüksek", _ => "—"
        };

        public static string RiskClass(byte v) => v switch
        {
            0 => "ok", 1 => "warn", 2 => "err", _ => ""
        };

        public static string ReviewStatus(byte v) => v switch
        {
            0 => "Taslak", 1 => "Birim Onayı", 2 => "KVKK Onayı", 3 => "VERBİS Yayında", _ => "—"
        };

        public static string UsageType(byte v) => v switch
        {
            0 => "Toplar", 1 => "Saklar", 2 => "Aktarır", 3 => "Türetir", _ => "—"
        };

        public static string Mechanism(byte v) => v switch
        {
            0 => "Yeterlilik Kararı",
            1 => "Standart Sözleşme",
            2 => "Bağlayıcı Şirket Kuralları (BCR)",
            3 => "Taahhütname",
            4 => "Arızi Açık Rıza",
            5 => "Sözleşme İfası",
            _ => "—"
        };

        // Plan 40 Faz 5 — AI Integrity bulgu şiddeti.
        public static string Severity(byte v) => v switch
        {
            0 => "İdari", 1 => "İdari (Yüksek)", 2 => "Kritik", _ => "—"
        };

        public static string SeverityClass(byte v) => v switch
        {
            0 => "warn", 1 => "warn", 2 => "err", _ => ""
        };

        public static string PatternName(string code) => code switch
        {
            Services.KvkkIntegrityPatterns.CopyPastePurpose => "Kopyala-Yapıştır İşleme Amacı",
            Services.KvkkIntegrityPatterns.ConsentLegalConflict => "Açık Rıza ↔ Hukuki Yükümlülük Çakışması",
            Services.KvkkIntegrityPatterns.CctvRetentionOver60Days => "CCTV Saklama Süresi Aşımı",
            Services.KvkkIntegrityPatterns.DisclosureMismatch => "Envanter ↔ Aydınlatma Metni Uyumsuzluğu",
            Services.KvkkIntegrityPatterns.CandidateCvRetentionOver2Years => "Aday CV Saklama Süresi Aşımı",
            Services.KvkkIntegrityPatterns.CrossBorderHiddenTransfer => "Gizli Yurt Dışı Aktarım",
            Services.KvkkIntegrityPatterns.NoWhistleblowerProcess => "İhbar / Etik Hat Süreci Yok",
            Services.KvkkIntegrityPatterns.NoBreachProcess => "İhlal Yönetim Süreci Yok",
            _ => code
        };
    }
}
