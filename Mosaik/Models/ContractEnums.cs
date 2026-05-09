namespace Mosaik.Models
{
    public enum ContractCategory
    {
        Lease,      // Kira
        Service,    // Hizmet
        Supply,     // Tedarik
        Employment, // İş
        License,    // Lisans
        Insurance,  // Sigorta
        Other
    }

    public enum ContractStatus
    {
        Draft,      // Taslak
        Active,     // Aktif
        Expired,    // Süresi doldu
        Terminated  // Feshedildi
    }

    public enum ObligationCategory
    {
        Finance,    // Finans
        Tax,        // Vergi
        Hr,         // İK
        Operation,  // Operasyon
        It,         // BT
        Legal       // Hukuk
    }

    public enum ObligationType
    {
        Payment,    // Ödeme
        Tax,        // Vergi
        Compliance, // Uyumluluk
        Renewal,    // Yenileme
        Deadline,   // Son tarih
        Audit       // Denetim
    }

    public enum ObligationStatus
    {
        Pending,    // Bekliyor
        Completed,  // Tamamlandı
        Overdue,    // Gecikmiş
        Cancelled   // İptal
    }

    public enum ObligationSource
    {
        Manual,       // Manuel girildi
        AiSuggested   // AI önerdi
    }

    public enum RecurrenceType
    {
        Monthly,    // Aylık
        Quarterly,  // Çeyreklik
        Yearly,     // Yıllık
        Custom      // Özel aralık
    }

    public enum ExtractionStatus
    {
        Processing,     // İşleniyor
        AwaitingReview, // İnceleme bekliyor
        Approved,       // Onaylandı
        Rejected,       // Reddedildi
        Failed          // Hata
    }

    public enum EventType
    {
        Payment,    // Ödeme
        Deadline,   // Son tarih
        Renewal,    // Yenileme
        Tax,        // Vergi
        Compliance, // Uyumluluk
        Operation   // Operasyon
    }

    public enum EventStatus
    {
        Upcoming,   // Yaklaşan
        Completed,  // Tamamlandı
        Cancelled   // İptal
    }

    public enum EventSource
    {
        Manual,       // Manuel girildi
        AiSuggested   // AI önerdi
    }

    public enum SuggestionType
    {
        Obligation,    // Yükümlülük önerisi
        ContractEvent, // Takvim etkinliği önerisi
        RiskWarning    // Risk uyarısı
    }

    public enum SuggestionStatus
    {
        Pending,   // Bekliyor
        Approved,  // Onaylandı
        Rejected   // Reddedildi
    }

    public enum Confidence
    {
        High,   // Yüksek
        Medium, // Orta
        Low     // Düşük
    }
}
