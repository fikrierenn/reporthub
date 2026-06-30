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
    }
}
