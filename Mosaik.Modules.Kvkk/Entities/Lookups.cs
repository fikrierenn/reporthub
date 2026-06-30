namespace Mosaik.Modules.Kvkk.Entities
{
    // Plan 40 (M6) Faz 0 — KVKK REF lookup tabloları. xlsx REF sheet + skill karşılığı.
    // Hepsi: Id + Code(unique) + Name + SortOrder + IsActive ortak; tipe özel ek alanlar.

    // VERBİS 22 standart kategori (m.5 genel + m.6 özel nitelikli).
    public class DataCategory
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;       // "kimlik", "saglik", "biyometrik"
        public string Name { get; set; } = string.Empty;       // "Kimlik"
        public bool IsSpecialCategory { get; set; }            // m.6 özel nitelikli mi
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // KVKK m.5/2 + m.5/1 + m.6/3 hukuki sebepleri.
    public class LegalBasis
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;       // "m5-2-a", "m6-3-a"
        public string Name { get; set; } = string.Empty;       // "Kanunlarda açıkça öngörülme"
        public string Article { get; set; } = string.Empty;    // "5/2-a"
        public bool IsSpecialCategoryBasis { get; set; }       // m.6 sebebi mi
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // Standart kişi grupları (çalışan, müşteri, ziyaretçi...).
    public class PersonGroup
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // Saklama süresi + yasal dayanak matrisi.
    public class RetentionRule
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;        // "Bordro / ücret hesabı"
        public string DurationText { get; set; } = string.Empty;// "10 yıl"
        public string? LegalReference { get; set; }             // "İş K. m.32, TBK m.146"
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // İmha yöntemi (silme / yok etme / anonimleştirme...).
    public class DisposalMethod
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // İdari + teknik tedbir standardı.
    public class MeasureStandard
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public byte MeasureType { get; set; }                   // 0 idari, 1 teknik
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // İşleme amacı (KVKK standart amaçlar — kopyala-yapıştır Pattern 1 önler).
    public class ProcessingPurpose
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // Alıcı / alıcı grubu (aktarım hedefi). IsCrossBorder = yurt dışı SaaS/kurum.
    public class Recipient
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsCrossBorder { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
