namespace Mosaik.Modules.GorevTanimlari.Entities;

// Pozisyon başına 1 görev-tanımı dokümanı (Mosaik DB, md'den göç edildi).
public class GorevDocument
{
    public int Id { get; set; }
    public string PositionCode { get; set; } = "";
    public string Title { get; set; } = "";
    public int? OrgPositionId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

// Rol-düzeyi eşleme (Zirve Departman+Unvan → doküman). Varsayılan öneri kaynağı.
public class GorevZirveMap
{
    public int Id { get; set; }
    public int GorevDocumentId { get; set; }
    public string? ZirveDepartman { get; set; }
    public string ZirveUnvan { get; set; } = "";
    public string MatchType { get; set; } = "manual";
    public string? Note { get; set; }
    public string? MappedBy { get; set; }
    public DateTime MappedAt { get; set; }
}

// Kişi-düzeyi eşleme (Zirve Personelno → doküman). Rol varsayılanını EZER.
// Gerekçe: aynı ünvan farklı tanıma gidebilir (ör. Satın Alma Görevlisi → biri Kırtasiye, biri Oyuncak).
public class GorevPersonelMap
{
    public int Id { get; set; }
    public string Personelno { get; set; } = ""; // Zirve alfanümerik personel kodu (ör. "4634-BKM")
    public int GorevDocumentId { get; set; }
    public string Source { get; set; } = "manual";
    public string? Note { get; set; }
    public string? MappedBy { get; set; }
    public DateTime MappedAt { get; set; }
}

// Versiyonlu görev-tanımı içeriği (SopVersions deseni). ContentJson = {paragraphs[], kpi[{text,active}]}.
// Status: 0=taslak, 1=yayın, 2=süperse. Şema ekranı yayın (Status=1) en yüksek VersionNumber'ı gösterir.
public class GorevVersion
{
    public int Id { get; set; }
    public int GorevDocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string ContentJson { get; set; } = "";
    public string? PlainTextContent { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? SupersededDate { get; set; }
    public byte Status { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
