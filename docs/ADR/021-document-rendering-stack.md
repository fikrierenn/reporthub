# ADR-021 — Document Rendering Stack: Gotenberg + PdfSharp/MigraDoc + OfficeIMO + OpenXml + ClosedXML

**Tarih:** 2026-05-21 (rev 2 — QuestPDF Pro reddedildi, Gotenberg + MigraDoc hibrit)
**Statü:** Önerildi (Plan 42 Faz 5 başlamadan onay — bütçe gerekmez)
**Karar verenler:** Fikri / Claude
**Bağlam:** Plan 42 Process Execution Runtime Faz 5 Result Rendering; OSS araştırma `docs/RESEARCH_OSS_VNEXT_2026-05-21.md` + ek paralel deep research (2026-05-21 gece).

---

## 1. Bağlam

Plan 42 Faz 5 Result Rendering — DSAR cevap mektubu, ihbar soruşturma raporu, aday red/kabul, sertifika PDF (QR code), VERBİS taahhütname, KVKK Kurul ihlal bildirim.

**İlk öneri (rev 1):** QuestPDF Professional ~$699/yıl 1 dev (BKM revenue >$1M, Community izin yok).

**Kullanıcı kararı (2026-05-21):** "questpdf yerine bence repo vardır iyi bak" — ücretsiz OSS alternatif zorunlu.

Deep research (ek paralel agent) sonucu **Gotenberg + PdfSharp/MigraDoc hibrit stack** QuestPDF Pro'yu kapsam + lisans + maliyet + Mosaik stack uyum açısından yener.

## 2. Karar (rev 2)

**Document rendering stack:**

| Bileşen | Lisans | Rol | Maliyet |
|---|---|---|---|
| **Gotenberg 8.32** (Docker microservice) | **MIT** | Birincil HTML→PDF motoru (Razor view → headless Chromium) | $0 |
| **Gotenberg.Sharp.API.Client 3.0.0** | **Apache 2.0** | .NET 10 fluent client (`AddGotenbergSharpClient`) | $0 |
| **PdfSharp + MigraDoc 6.2.4** | **MIT** | Yedek + digital signature (PKCS#7 native) + bulk in-process | $0 |
| **QRCoder 1.8.0** | **MIT** | QR code embed (sertifika) | $0 |
| **RazorLight veya Razor.Templating.Core** | **Apache 2.0** | Razor view → HTML string (controller'sız render) | $0 |
| **DocumentFormat.OpenXml 3.5.1** | MIT (Microsoft) | Word template placeholder VERBİS taahhütname | $0 |
| **OfficeIMO.Word 1.0.34** | MIT | Karmaşık Word rapor (ihbar soruşturma cover+TOC+section) | $0 |
| **ClosedXML** (mevcut) | MIT | Excel raporlar — korunsun | $0 |
| **Microsoft.Playwright** (opsiyonel) | MIT | Gotenberg yerine in-process alternatif (BKM Docker yoksa) | $0 |

**Toplam yıllık maliyet: $0.** Tüm bileşenler permissive lisans (MIT/Apache 2.0).

### 2.1 Use case dağılımı

| Use case | Birincil yol | Motor |
|---|---|---|
| DSAR cevap mektubu (formal letter) | Razor view → Gotenberg | Chromium HTML |
| Sertifika + QR code | Razor + QRCoder PNG data-URI → Gotenberg | Chromium |
| KVKK ihlal bildirim rapor (multi-page tablo) | Razor + Tailwind print CSS → Gotenberg | Chromium |
| Aday red/kabul iletişim | Razor view → Gotenberg | Chromium |
| VERBİS görsel rapor (Chart.js embed) | Razor + Chart.js JS execute → Gotenberg | Chromium |
| Bulk job (1000+ aday red) | MigraDoc in-process | PdfSharp |
| Digital signature **şart** DSAR | MigraDoc + PKCS#7 | PdfSharp |
| Gotenberg container down fallback | MigraDoc | PdfSharp |
| VERBİS taahhütname Word | OpenXml + placeholder helper | — |
| İhbar soruşturma raporu Word (cover+TOC+section) | OfficeIMO.Word | — |
| Excel raporlar | ClosedXML (mevcut) | — |

## 3. QuestPDF Pro Reddetme Gerekçesi

| Kriter | QuestPDF Pro (rev 1) | Gotenberg + MigraDoc (rev 2) |
|---|---|---|
| Yıllık lisans | **$699/yıl 1 dev** | **$0** |
| 3 yıl maliyet | **$2097** | **$0** |
| Lisans modeli | Hybrid commercial ($1M revenue threshold) | MIT + Apache 2.0 permissive |
| Razor view reuse | **Yok** (Fluent C# DSL ayrı dil) | **Var** (Mosaik UI ile aynı template) |
| Digital signature | **Yok** (PDFsharp ile post-process gerekli) | **Var** (MigraDoc PKCS#7 native) |
| Tailwind/Chart.js render | Yok (manuel re-implementation) | **Var** (Chromium native) |
| Plan 42 Faz 5 effort | 21h | **19h** (-2h) |
| Vendor lock | Var ($1M revenue threshold ilerideki risk) | Yok |
| BKM Docker compose entegrasyon | İlgisiz | **Plan 40 Presidio ile birleşir** (+1 container) |
| Throughput | ~50 PDF/s in-process | ~20-40 PDF/s HTTP (Gotenberg pool) — BKM ölçeği için yeter |

**Sonuç:** QuestPDF Pro net dezavantajlı. Kapsam (digital signature eksik), lisans ($699/yıl), öğrenme borcu (Fluent DSL), Razor reuse yoksunluğu (template duplication). **Reddedildi.**

## 4. Diğer Reddedilen Alternatifler (deep research)

| Aday | Lisans | Reddetme gerekçesi |
|---|---|---|
| **iText 7 / iTextSharp** | AGPL (modern), LGPL (5.5.13 son EOL 2018) | AGPL closed-source Mosaik için yasak. iText 5.5.13 EOL. |
| **Aspose.PDF / Aspose.Words** | $1175+/dev başlangıç | Aşırı pahalı. |
| **DocX Xceed** | Ticari $852+ | Free MIT alternatif var. |
| **Spire.PDF Free / HiQPdf Free** | Proprietary, free <5-10 sayfa | Üretim kullanılamaz. |
| **Carbone Community** | CCL (kısıtlı) | "third parties" yasağı belirsiz, .NET SDK yok, REST + JS. |
| **DinkToPdf / wkhtmltopdf** | MIT wrapper / LGPL | wkhtmltopdf 2023'te arşivlendi, CVE-2022-35583 SSRF açığı patch'siz. Production'da yasak. |
| **jsreport** | LGPL | LGPL viral değil ama JS template engine, .NET SDK marjinal. |
| **PdfReport.Core (VahidN)** | LGPL-2.0 | Niche fit VERBİS tablo rapor için ama bakım yavaş, iTextSharp.LGPL bağımlı. |
| **OpenPDF** | LGPL/MPL | Java-native, .NET portu marjinal. |
| **PhantomJS** | EOL 2018 | — |
| **EPPlus 7** | Polyform Non-commercial / $557+/dev | ClosedXML zaten var. |
| **NPOI** | Apache-2.0 | ClosedXML kadar olgun değil, API tutarsız. |
| **Clippit (Open-XML-PowerTools fork)** | MIT | OpenXml + placeholder helper yeterli, Clippit fazla karmaşık. |

## 5. Implementation Sequence (Plan 42 Faz 5)

```yaml
# docker-compose.yml (Plan 40 Presidio ile birleştir)
services:
  gotenberg:
    image: gotenberg/gotenberg:8.32
    ports: ["3000:3000"]
    command:
      - "gotenberg"
      - "--api-timeout=60s"
      - "--chromium-disable-javascript=false"
    deploy:
      resources:
        limits: { memory: 1G }
```

```bash
# NuGet
dotnet add Mosaik package Gotenberg.Sharp.API.Client --version 3.0.0
dotnet add Mosaik package PDFsharp-MigraDoc --version 6.2.4
dotnet add Mosaik package QRCoder --version 1.8.0
dotnet add Mosaik package Razor.Templating.Core
dotnet add Mosaik package DocumentFormat.OpenXml --version 3.5.1
dotnet add Mosaik package OfficeIMO.Word --version 1.0.34
```

```csharp
// Program.cs
builder.Services.AddGotenbergSharpClient(o =>
    o.ServiceUrl = builder.Configuration["Gotenberg:Url"]);

// Mosaik.Modules.ProcessRuntime/Services/PdfRenderer.cs
public class PdfRenderer(
    IGotenbergSharpClient gotenberg,
    IRazorViewRenderer razor) : IPdfRenderer
{
    public async Task<byte[]> RenderAsync<TModel>(string viewPath, TModel model, CancellationToken ct)
    {
        var html = await razor.RenderToStringAsync(viewPath, model);
        var req = new HtmlRequestBuilder()
            .ContainsHtml(b => b.SetBody(html))
            .ConfigureRequest(b => b
                .SetMarginsTopBot(40, 40)
                .AddCustomHeader(headerHtml)
                .AddCustomFooter("Sayfa <span class=\"pageNumber\"></span> / <span class=\"totalPages\"></span>")
                .SetPdfFormat(PdfFormats.A2B))
            .Build();
        return await gotenberg.HtmlToPdfAsync(req, ct).ToByteArrayAsync(ct);
    }
}

// Bulk + signature path
public class MigraDocRenderer(IQrCodeService qr) : IBulkPdfRenderer
{
    // MigraDoc Document → PdfSharp render → PKCS#7 sign
}
```

**Faz 5 adımları:**
1. **5.1** — Docker compose Gotenberg ekle (Plan 40 Presidio container ile birleştir) + NuGet paketleri
2. **5.2** — `IPdfRenderer` abstraction + `GotenbergPdfRenderer` impl + RazorLight/Razor.Templating.Core entegrasyon
3. **5.3** — Razor template'leri yaz (DSAR mektubu, sertifika+QR, KVKK rapor) — Mosaik UI ile aynı Tailwind reuse
4. **5.4** — OpenXml SDK placeholder helper (`{{key}}` regex replace, image embed) — VERBİS Word
5. **5.5** — OfficeIMO Word — ihbar soruşturma raporu (cover+TOC+section)
6. **5.6** — MigraDoc fallback + digital signature path (PKCS#7) — opsiyonel ihtiyaca göre

## 6. Stack Özet (rev 2)

| Use case | Library | Maliyet |
|---|---|---|
| DSAR cevap PDF formal letter | **Gotenberg + Razor view** | $0 |
| VERBİS taahhütname Word | **OpenXml SDK + placeholder helper** | $0 |
| İhbar soruşturma raporu Word | **OfficeIMO.Word** | $0 |
| Sertifika PDF + QR | **Gotenberg + Razor + QRCoder** | $0 |
| Kurul ihlal bildirim Word | **OpenXml SDK** template fill | $0 |
| Aday red/kabul iletişim | **Gotenberg + Razor** veya **OpenXml** | $0 |
| Digital signature | **MigraDoc PKCS#7** in-process | $0 |
| Bulk job (1000+ doc) | **MigraDoc** in-process | $0 |
| Excel raporlar | **ClosedXML** (mevcut) | $0 |
| Gotenberg fallback | **MigraDoc** | $0 |

**Toplam yıllık maliyet: $0.** 3 yılda $2097 tasarruf (QuestPDF Pro'ya karşı).

## 7. Effort Etkisi

| Adım | QuestPDF (rev 1) | Gotenberg + MigraDoc (rev 2) | Δ |
|---|---|---|---|
| Kurulum + DI | 1h | 2h (Docker compose + NuGet) | +1 |
| Razor → HTML pipeline (RazorLight) | 0 (DSL native) | 3h | +3 |
| Template (DSAR, sertifika, rapor) | 12h (Fluent DSL) | 6h (mevcut Razor + Tailwind reuse) | **−6** |
| QR embed | 1h | 1h | 0 |
| Digital signature | 4h (PDFsharp post-process) | 4h (MigraDoc path) | 0 |
| Test (Türkçe, page break, watermark) | 3h | 3h | 0 |
| **Toplam Plan 42 Faz 5** | **21h** | **19h** | **−2h** |

**Bonus:** Razor template'leri Mosaik UI ile aynı source-of-truth — Tailwind reuse, single template. QuestPDF DSL ayrı dil öğrenme borcu yok.

## 8. Riskler

| Risk | Önlem |
|---|---|
| Gotenberg container down → PDF üretimi durur | MigraDoc fallback path her senaryo için hazır (bulk + signature path ile aynı kod) |
| Docker compose RAM yükü | Plan 40 Presidio (+512Mi-1Gi) + Gotenberg (+1Gi) = compose <2GB toplam, BKM IT için kabul |
| Chromium binary güvenlik patches | Gotenberg upstream Chromium tag — `8.32+` daima son LTS |
| Razor view template `@inject` HtmlContext yoksunluğu (RazorLight) | Razor.Templating.Core daha yakın ASP.NET Core kontekst |
| MigraDoc API "eski-stil" eleştirisi | Fluent değil DOM-stil ama doc-gen için yeterli + signature native + kullanım sınırlı (fallback) |
| BKM ileride $1M revenue threshold geçince QuestPDF maliyeti yükselir | Geçerli değil — Gotenberg MIT, vendor lock yok |

## 9. ADR Sürümleri

- **2026-05-21 rev 1:** QuestPDF Pro + DocumentFormat.OpenXml + OfficeIMO + PDFsharp + ClosedXML. Yıllık lisans ~$699.
- **2026-05-21 rev 2:** QuestPDF Pro **reddedildi**. Gotenberg + PdfSharp/MigraDoc + QRCoder + Razor.Templating.Core + DocumentFormat.OpenXml + OfficeIMO + ClosedXML. **Yıllık lisans $0**. 3 yıl $2097 tasarruf. Razor reuse + digital signature native + 2h ek tasarruf.

## 10. Sonuç

ADR-021 onaylandığında NuGet + Docker compose adımları Plan 42 Faz 5 başlangıcında. **Bütçe gerekmez** ($0 stack).

Plan 42 §3 OSS reuse stack tablosu güncellendi (rev 2).

Plan 42 toplam effort: 84h baseline → ~62-64h OSS reuse (önceden 64h, Gotenberg +2h tasarruf).
