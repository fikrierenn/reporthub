# ADR-021 — Document Rendering Stack: QuestPDF Pro + OpenXml + OfficeIMO + PDFsharp + ClosedXML

**Tarih:** 2026-05-21
**Statü:** Önerildi (Plan 42 Faz 5 başlamadan onay + QuestPDF Professional lisans bütçesi)
**Karar verenler:** Fikri / Claude
**Bağlam:** Plan 42 Process Execution Runtime Faz 5 Result Rendering; OSS araştırma `docs/RESEARCH_OSS_VNEXT_2026-05-21.md`

---

## 1. Bağlam

Plan 42 Process Execution Runtime Faz 5 (Result Rendering) — DSAR cevap mektubu, ihbar soruşturma raporu, aday red/kabul iletişim, sertifika PDF (QR code), VERBİS taahhütname, Kurul ihlal bildirim, vs.

OSS araştırma .NET PDF/Word/Excel template engine ekosistemini tarayıp lisans + maliyet + .NET 10 uyumu + Türkçe karakter + complex template (table/image/header/footer/page break/watermark/digital signature) için karşılaştırma yaptı.

## 2. Karar

**Document rendering stack = QuestPDF Pro + DocumentFormat.OpenXml + OfficeIMO.Word + PDFsharp + ClosedXML + Playwright (opsiyonel).**

### 2.1 PDF — QuestPDF Professional (~$699/yıl 1 dev)

NuGet: `QuestPDF` 2026.5.0 (May 2026). Multi-target .NET 6/8/9/10.

**Lisans modeli:**
- Community MIT (free <$1M annual revenue)
- Professional $699/dev/yıl (≤10 dev)
- Enterprise (>10 dev quote-based)

**BKM durumu:** Revenue muhtemelen $1M üstü → **Professional zorunlu**.

**Use cases:**
- DSAR cevap mektubu (formal letter)
- Sertifika PDF + QR code (ZXing.Net entegrasyonu)
- VERBİS taahhütname görsel sürümü (Word'den dönüştürülürse)
- Kurul ihlal bildirim görsel rapor (opsiyonel)

**Özellikler:**
- Fluent C# Designer.cs paradigması
- Türkçe Unicode + RTL/bidi + ICU text shaping native
- QR code embed (ZXing.Net)
- Watermark built-in (background+header+content+footer+watermark stack)
- TOC, hyperlink, bookmark, page numbering hep var
- Digital signature **native YOK** — PDFsharp ile post-process

### 2.2 Word — DocumentFormat.OpenXml 3.5.1 (MIT, Microsoft official) + placeholder helper

**Lisans:** MIT, $0.

**Use cases:**
- VERBİS taahhütname template (`{{key}}` placeholder)
- Kurul ihlal bildirim taslağı (m.12/5 zorunlu format)
- Aday red/kabul iletişim mektubu
- Tedarikçi DPA template

**Pattern:** Word template (.docx) admin'de hazırla → `{{TalepSahibi}}`, `{{VerbisRef}}`, `{{ResponseDate}}` placeholder'ları → runtime'da regex/SDK ile replace. Plan 42 Result Rendering için **kanonik yaklaşım**.

**Helper:** Mosaik için ~200-300 satır C# (regex replace, image embed).

### 2.3 Word karmaşık (cover + TOC + section) — OfficeIMO.Word 1.0.34 (MIT)

**Lisans:** MIT, free for commercial usage with no limits.

**Use case:** İhbar soruşturma raporu — kapak + İçindekiler + multi-section.

**Pattern:** OpenXML SDK üzerinde fluent API — cover page, TOC, section/paragraph/comment.

**Tek geliştirici (Evotec), aktif** (v1.0.34 Mart 2026), OpenXML kadar olgun değil ama free + maintained.

### 2.4 Digital signature — PDFsharp 6.2 X509 (MIT) post-process

**Lisans:** MIT.

**Use case:** QuestPDF native signing YOK. PDFsharp X509 signature native — QuestPDF eksiğini kapatır.

**Sınırlama:** PAdES desteği yok (stagnant). KVKK/eIDAS uyumu için yeterli değil. Türk KEP/E-İmza entegre gerekirse ayrı plan. Plan 42 v1 görsel imza alanı + sonradan KEP yeterli.

### 2.5 Excel — ClosedXML korunsun (mevcut yatırım)

**Lisans:** MIT, $0.

**Mevcut kullanım:** Plan 17 Tamim export, Plan 40 KVKK xlsx import + VERBİS export.

**Karar:** EPPlus 7 geçişi gereksiz (Polyform Non-commercial / $557+ ticari).

### 2.6 HTML→PDF (opsiyonel, Plan 42 Faz 6+) — Playwright .NET (MIT)

**Lisans:** MIT, $0 (compute maliyeti var).

**Use case:** KPI dashboard PDF export, brand-heavy iletişim, Razor view → headless Chromium → PDF.

**Dezavantaj:** Chromium ~250MB disk, Hangfire background worker queue zorunlu.

**Plan 42 öncelik:** DÜŞÜK — DSAR/VERBİS için QuestPDF yeterli. Sadece "rapor PDF" (KPI/chart) için Plan 42 Faz 6+ konusu.

## 3. Reddedilen Alternatifler

| Aday | Lisans | Reddetme gerekçesi |
|---|---|---|
| **iText 7** | AGPL veya Commercial $4K-15K/yıl/site | AGPL closed-source Mosaik için yasak. Commercial aşırı pahalı. |
| **Aspose.PDF / Aspose.Words** | $1175+/dev başlangıç | Aşırı pahalı. |
| **DocX Xceed** | Ticari $852+, non-commercial only legacy | Free MIT alternatifler var. |
| **Spire.Doc / Spire.PDF** | Free <500 sayfa / 3 sheet limit | Üretim kullanılamaz. |
| **EPPlus 7** | Polyform Non-commercial / $557+/dev | ClosedXML zaten var. |
| **NPOI** | Apache-2.0 | ClosedXML kadar olgun değil, API tutarsız. |
| **Clippit (Open-XML-PowerTools fork)** | MIT | OpenXml + placeholder helper yeterli, Clippit fazla karmaşık (DocumentAssembler XML-driven). |

## 4. Maliyet

**Yıllık lisans:** ~$699 (QuestPDF Professional 1 dev). Tek paid item.

**Diğer hepsi:** $0 (MIT/Apache lisanslar).

**Kullanıcı bütçe onayı gerekir** QuestPDF Professional için. Reddedilirse alternatif: **PDFsharp + MigraDoc** (MIT, MigraDoc XML-vari API daha eski-stil, ama lisans sıfır + signature native var).

## 5. Stack Özet

| Use case | Library | Maliyet |
|---|---|---|
| DSAR cevap PDF formal letter | **QuestPDF Professional** | ~$699/yıl |
| VERBİS taahhütname Word | **OpenXml SDK** + placeholder helper | $0 |
| İhbar soruşturma raporu Word (multi-section) | **OfficeIMO.Word** | $0 |
| Sertifika PDF + QR | **QuestPDF + ZXing.Net** | (Pro içinde) |
| Kurul ihlal bildirim Word | **OpenXml SDK** template fill | $0 |
| Aday red/kabul iletişim | **OpenXml SDK** veya **QuestPDF** | $0/içinde |
| Digital signature | **PDFsharp X509** post-process | $0 |
| Excel raporlar | **ClosedXML** (mevcut) | $0 |
| KPI/chart PDF (Faz 6+) | **Playwright .NET** | $0 (compute) |

## 6. Implementation Sequence (Plan 42 Faz 5)

1. **5.1** — QuestPDF NuGet + lisans key config + `IPdfRenderer`/`IDocxRenderer` abstraction
2. **5.2** — OpenXml SDK template placeholder helper (`{{key}}` regex replace, image embed)
3. **5.3** — OfficeIMO opsiyonel — karmaşık rapor şablonu (ihbar raporu)
4. **5.4** — PDFsharp opsiyonel — digital signature post-process

## 7. Riskler

| Risk | Önlem |
|---|---|
| QuestPDF Professional lisans bütçesi $699/yıl | Kullanıcı bütçe onayı; reddedilirse PDFsharp+MigraDoc fallback |
| QuestPDF $1M revenue limiti yıllar içinde değişir | Yıllık lisans yenileme + lisans sayfası takip |
| PDFsharp PAdES yok (Türk KEP/E-İmza yetersiz) | Türk KEP entegre ayrı plan; v1 görsel imza yeterli |
| OfficeIMO tek geliştirici bakım | Karmaşık rapor için sadece, basit template OpenXml SDK |
| Scriban CVE-2024 sandbox escape (eski sürüm) | NuGet `>=7.2.0` pin (Plan 42 Faz 5 result template ayrı kullanım — ADR-021 kapsamı dışı, Plan 42 §5'te belirtilmiş) |

## 8. Sonuç

NuGet paketleri Plan 42 Faz 5 başlangıcında:
- `QuestPDF` 2026.5.0 (+ Pro lisans key config)
- `DocumentFormat.OpenXml` 3.5.1
- `OfficeIMO.Word` 1.0.34 (opsiyonel)
- `PDFsharp` 6.2.0 (opsiyonel signature)
- `ZXing.Net` (QuestPDF entegrasyonu QR code)
- `Microsoft.Playwright` (opsiyonel Faz 6+)

`ClosedXML` mevcut, korunsun.

Plan 42 §9 cross-reference güncellendi — bu ADR'ye referans verildi.
