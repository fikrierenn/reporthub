# OSS Kaynak Araştırması — vNext Altılı Kalp (2026-05-21)

**Tarih:** 2026-05-21
**Kapsam:** Plan 34 SOP + Plan 36 Workflow + Plan 40 KVKK + Plan 41 Form Builder + Plan 42 Process Execution Runtime
**Yöntem:** 5 paralel `general-purpose` agent — WebSearch + WebFetch ile dibine kadar tarama
**Hedef:** Hazır OSS parçaları reuse ile her plandan %20-50 effort tasarrufu, baştan yazımı sıfıra indir
**Kullanıcı kararı (2026-05-21):** "bu işleri yapacak parça parçada olsa repolar kaynaklar varsa dibine kadar kullanmalısın kaynak araştırması yapmalısın"

---

## 1. Net Karar Paketi — Mosaik için kabul edilen OSS stack

### Plan 36 Workflow Engine + Plan 42 Workflow hookup

| Aday | Lisans | Karar | Gerekçe |
|---|---|---|---|
| **Stateless 5.20.1** | Apache-2.0 | ✅ **KABUL** | ~700 satır C# hierarchical FSM + async actions + extern state persistence. Persistence-agnostic → Mosaik DB. ~$0. |
| Elsa 3.6 | MIT | ❌ Red | Designer embed yok (Elsa Studio Blazor WASM ayrı), Plan 42 ProcessInstance + 6 aspect Elsa entity'leri ile çakışıyor, "10-25 workflows incredibly inefficient" şikayet, doc gap. Net +2 hafta zarar. |
| Workflow Core 3.17 | MIT | ❌ Red | Designer yok, yarı aktif. Stateless ile fark kapanıyor. |
| Camunda 8 / Zeebe | Source-available | ❌ Red | Prod lisansı zorunlu, multi-component (Zeebe+Tasklist+Operate+Keycloak), BKM IT yükü, 6-12 ay satın alma. |
| Temporal | MIT engine | ❌ Red | Cassandra+ES SRE yükü, Cloud $200+/ay, 4 kişilik IT karşılayamaz. |
| MassTransit Saga | Apache-2.0 | ❌ Red | Saga = distributed, Mosaik monolit. |

**ADR-019 yazılır** — "Workflow backend engine — Stateless + Hangfire".

### Plan 41 Form Builder

| Aday | Lisans | Karar | Gerekçe |
|---|---|---|---|
| **SurveyJS Form Library (renderer)** | MIT | ✅ **PARTIAL REUSE** | JSON schema vanilla embed + `Serializer.addProperty()` KVKK DataElement custom metadata + Türkçe lokalizasyon. ~250kb minified Knockout legacy → ayrı bundle. |
| **signature_pad 5.1.1** | MIT, 9.5k★ | ✅ **DOĞRUDAN AL** | UMD ~5KB, sıfır dep, mobile touch + desktop. ~2h wrapper IIFE. |
| **Honeypot + time-based AntiSpam** | Pattern | ✅ **KOD YOK, PATTERN** | 5 satır Razor + server-side check. KVKK-uyumlu (PII toplamaz). |
| Cloudflare Turnstile | SaaS ücretsiz | ✅ **OPSIYONEL** | Yüksek-risk public formlarda (DSAR/ihbar) toggle. "No PII collection". |
| SurveyJS Survey Creator | Ticari ~£422/dev/yıl | ❌ Red | Builder UI Mosaik'te kendi yazılır (Plan 41 zaten kapsamda). |
| Formbricks 12.3k★ | AGPLv3 | ❌ Red | Viral lisans → Mosaik'in tüm kodu AGPLv3 olur. Stack Next.js. |
| LimeSurvey | GPL-2.0 | ❌ Red | PHP+MySQL, ayrı uygulama. |
| formio.js | OSL-3.0 | ❌ Red | Copyleft lisans riski. |
| Tripetto | Ticari kapalı | ❌ Red | |
| BlazorForms / Whyvra | MIT | ❌ Red | Blazor only, Mosaik MVC+Razor+Vanilla. |
| kevinchappell/formBuilder 9.2k★ | MIT | ❌ Red | jQuery bağımlı, Mosaik vanilla. |
| json-editor / Alpaca | MIT/Apache | ❌ Red | Bootstrap/jQuery UI legacy. |
| Syncfusion Dynamic Form | Ticari | ❌ Red | Proprietary. |
| reCAPTCHA v3 | Google SaaS | ❌ Red | KVKK riski, Google veri aktarımı. |

**ADR-020 yazılır** — "Form Builder Hybrid: SurveyJS Form Library renderer + DIY drag-drop builder UI".

### Plan 42 Document Rendering (Faz 5) — rev 2 (2026-05-21 gece deep research)

**Kullanıcı kararı:** "questpdf yerine bence repo vardır iyi bak" → ek deep research agent → **QuestPDF Pro reddedildi**, Gotenberg + MigraDoc hibrit stack kabul edildi. $699/yıl → $0, 3 yıl $2097 tasarruf.

| Use case | Aday | Maliyet | Karar |
|---|---|---|---|
| DSAR cevap PDF formal letter | **Gotenberg Docker + Razor view** | $0 (MIT + Apache 2.0) | ✅ |
| VERBİS taahhütname Word | **DocumentFormat.OpenXml** + placeholder helper | $0 (MIT) | ✅ |
| İhbar soruşturma raporu Word (cover+TOC+section) | **OfficeIMO.Word** v1.0.34 | $0 (MIT fluent OpenXml wrapper) | ✅ |
| Sertifika PDF + QR code | **Gotenberg + Razor + QRCoder 1.8.0** PNG data-URI | $0 (MIT) | ✅ |
| Digital signature + bulk in-process + Gotenberg fallback | **PdfSharp + MigraDoc 6.2.4** (PKCS#7 native) | $0 (MIT) | ✅ |
| Razor view → HTML string render | **RazorLight veya Razor.Templating.Core** | $0 (Apache 2.0) | ✅ |
| KPI/chart PDF (Plan 42 Faz 6+) | **Playwright .NET** (Gotenberg yedek alternatif) | $0 (compute) | ✅ Opsiyonel |
| Excel raporlar | **ClosedXML** (mevcut) | $0 | ✅ Korunsun |
| **QuestPDF Professional** | Hybrid ~$699/yıl 1 dev | ❌ **REV 2 REDDEDİLDİ** | Razor reuse yok + DSL öğrenme borcu + digital signature yok + vendor lock + 3 yıl $2097 |
| iText 7 | AGPL | ❌ Red | Closed-source Mosaik için yasak. |
| Aspose.PDF/Words | Ticari $1175+/dev | ❌ Red | Aşırı pahalı. |
| DocX Xceed | Ticari $852+ | ❌ Red | Free MIT alternatif var. |
| Spire.Doc/Spire.PDF | Free <500 sayfa/3 sheet | ❌ Red | Üretim kullanılamaz. |
| EPPlus 7 | Polyform Non-commercial / $557+ | ❌ Red | ClosedXML zaten var. |
| NPOI | Apache-2.0 | ❌ Red | ClosedXML kadar olgun değil. |
| Clippit (Open-XML-PowerTools fork) | MIT | ❌ Red | OpenXML + placeholder helper yeterli, Clippit fazla karmaşık. |
| **Carbone Community** | CCL kısıtlı | ❌ Red | "third parties" yasağı belirsiz, .NET SDK yok |
| **DinkToPdf / wkhtmltopdf** | MIT wrapper / LGPL | ❌ Red | wkhtmltopdf 2023'te arşivlendi, CVE-2022-35583 SSRF açığı patch'siz |
| **jsreport** | LGPL | ❌ Red | JS template engine, .NET SDK marjinal |
| **PdfReport.Core (VahidN)** | LGPL-2.0 | ❌ Red | iTextSharp.LGPL bağımlı, bakım yavaş |
| **OpenPDF** | LGPL/MPL | ❌ Red | Java-native, .NET portu marjinal |
| **PhantomJS** | EOL 2018 | ❌ Red | — |
| **HiQPdf Free** | Proprietary | ❌ Red | 5 sayfa limit |
| **Spire.PDF Free** | Proprietary | ❌ Red | 10 sayfa limit |

**Gotenberg avantajları:**
- Docker MIT yan-servis (Plan 40 Presidio compose ile birleşir, +1 container <1Gi RAM)
- Türkçe Noto font stack v8.30+ tam Unicode
- Watermark + Header/Footer + Page numbering + PDF/A native
- Razor template'leri Mosaik UI ile **aynı source-of-truth** (Tailwind + Türkçe + Chart.js reuse)
- DSL öğrenme borcu yok
- Vendor lock yok (MIT)

**MigraDoc avantajları:**
- In-process (HTTP overhead yok) — bulk 1000+ doc için ideal
- Digital signature PKCS#7 native
- Gotenberg down fallback path aynı kod

**ADR-021 rev 2 yazıldı** — "Document rendering stack — Gotenberg + PdfSharp/MigraDoc + QRCoder + OpenXml + OfficeIMO + ClosedXML. QuestPDF Pro reddedildi."

### Plan 42 Process Runtime UI + ICS + Template

| Aday | Lisans | Karar | Gerekçe |
|---|---|---|---|
| **Scriban 7.2.0** | BSD-2 | ✅ **AL** | Plan 42 result template Liquid sandbox, AOT-safe. CVE-2024 v7.0.0+ fixed. **≥7.2.0 pin.** |
| **Ical.Net 5.2.2** | MIT, ical-org canonical | ✅ **AL** | Plan 42 ICS feed RFC 5545 tam uyum + RRULE + NodaTime TZ. rianjs fork terkli. |
| **vis-timeline 8.5.1** | Apache-2.0 / MIT | ✅ **AL** | Plan 42 Instance Detail 6-aspect timeline UI. Vanilla JS ADR-014 uyum. groups API ile 6 aspect ayrı row. moment.js peer (~120kb gzip). |
| Fluid (sebastienros) | MIT | 🔄 YEDEK | Scriban'a alternatif (tek motor seç). Scriban'dan %40 hızlı. |
| Novu .NET SDK 3.12.0 | MIT | ❌ Red | Node.js + Postgres + Redis + MongoDB + RabbitMQ ek altyapı. Plan 37 DIY yeterli. |
| bpmn-js | MIT | 🔄 ERTELE | BPMN XML zorlar, Plan 42 polymorphic aspect modelini kısıtlar. Plan 42 sonrası "visual designer" istenirse aç. |
| xstate | MIT | 🔄 OPSIYONEL | Stateless ile fark az. Timer/parallel state ihtiyacı doğarsa al. |

### Plan 40 KVKK Veri Envanteri

| Aday | Lisans | Karar | Gerekçe |
|---|---|---|---|
| **Microsoft Presidio** | MIT, 8.2k★ | ✅ **AL (yan-servis)** | PII detection + anonymization Docker. Türkçe için `turkish-nlp-suite/tr_core_news_trf` spaCy. TC Kimlik + IBAN custom recognizer ~100 satır. Pattern 4 + retention sweep anonimleştirme. |
| **FuzzySharp (JakeBayer)** | MIT, C# native | ✅ **AL** | Token-Set Ratio fuzzy similarity. Pattern 1 kopyala-yapıştır amaç tespiti. ⚠️ Türkçe için `PreprocessMode.None` + manual i↔I/ç↔c normalize. |
| **KVKK resmi envanter Excel** | Resmi | ✅ **REFERANS** | `kvkk.gov.tr/.../b5fe209d-...xlsx` Plan 40 Faz 6 VERBİS export birebir format kaynağı. |
| **KVKK 6698 + Mart 2025 Rehber + VERBİS Kılavuz PDF** | Resmi | ✅ **REFERANS** | `docs/kvkk-references/` (gitignored, telif). |
| **DikkatIQ AI Layer (Mosaik.Core.AI)** | İç | ✅ **REUSE** | 3-katman LLM fallback (Ollama→Gemini→Claude) hazır. Plan 40 AI Integrity Checker prompt orchestration %30 tasarruf. |
| **CookieConsent v3 (orestbida)** | MIT, vanilla ~24kb | 🔄 OPSIYONEL | Plan 40 scope dışı ama Mosaik `/Privacy` sayfası için 1 saatte entegre. WCAG keyboard. |
| Bearer CLI | Elastic License 2.0 | ❌ Red | C# desteklemiyor (Go/Java/JS/TS/PHP/Python/Ruby). Mosaik C# kodbase tarayamaz. |
| Privado-Inc/privado | Apache-2.0 | ❌ Red | C# yok. |
| OpenMetadata / DataHub | Apache-2.0 | 🔄 ERTELE | Airflow + Elasticsearch overkill stack. Plan 42+ retention + DB column-level scan için tekrar değerlendir. |
| Lucene.Net TurkishAnalyzer | Apache-2.0 | 🔄 OPSIYONEL | P6 veri minimizasyon için. FuzzySharp+regex çoğu için yeter. |
| Türk SaaS (kvkkasistan/nesil) | Proprietary | ❌ Red | Kapalı kaynak, ₺15-50k/yıl abonelik. |
| TÜBİTAK BİLGEM | — | — | Yayınlanmış OSS yok. |

### Plan 34 SOP

| Aday | Lisans | Karar | Gerekçe |
|---|---|---|---|
| **Tamim altyapısı reuse** (Mosaik.Modules.Circular) | İç | ✅ **REUSE %80** | Block-based content, file ek, notification, compile job. SOP procedure entity üstüne kopyala-genişlet. |
| **EasyMDE** (Markdown editor) | MIT, vanilla ~80kb | 🔄 OPSIYONEL | Gelişmiş markdown editor istenirse 2-3h entegrasyon. |
| Wiki.js | AGPL-3.0 | ❌ Red | Node.js + AGPL viral. |
| Outline | BSL 1.1 | ❌ Red | "AI PR yasak" non-OSI. |
| ProcessMaker | AGPL-3.0 | ❌ Red | PHP/Laravel monolith. |
| Flowable / Bonita / Camunda 7 | Apache/LGPL | ❌ Red | JVM bağımlılığı. |
| Frappe Quality Procedure | MIT/GPL | 🔄 PATTERN REFERANS | Python ERPNext, stack mismatch ama UX pattern referansı. |
| Apromore | Hibrit | 🔄 ERTELE | Process mining ileride VISION §7.0.1 Celonis benzer ihtiyaç ayrı plan. |
| Plane | AGPL | ❌ Red | Önceden Plan 16 araştırılmıştı, AGPL. |
| Twenty CRM | AGPL | ❌ Red | Önceden Plan 16 araştırılmıştı. |

---

## 2. Toplam Effort Tasarrufu

| Plan | Baseline | OSS reuse sonrası | Tasarruf |
|---|---|---|---|
| Plan 36 Workflow | 30-40h | 22-32h (Stateless) | **-8h ~%20-25** |
| Plan 40 KVKK | 50-65h | 38-50h (Presidio + FuzzySharp + DikkatIQ + KVKK resmi xlsx) | **-12-15h ~%25-30** |
| Plan 41 Form Builder | 54-70h | 38-48h (SurveyJS + signature_pad + honeypot) | **-16-22h ~%30-35** |
| Plan 42 Process Runtime | 64-84h | 48-64h (Scriban + Ical.Net + vis-timeline + QuestPDF + OpenXml + OfficeIMO + PDFsharp) | **-16-20h ~%25** |
| Plan 34 SOP | 38-60h | 30-50h (Tamim reuse %80 + EasyMDE opsiyonel) | **-8-10h ~%15-20** |
| **Toplam vNext kalbi** | **236-319h** | **176-244h** | **~60-75h ~%23-25 tasarruf** |

**Yıllık lisans maliyeti (rev 2):** **$0**. QuestPDF Pro reddedildi → Gotenberg + MigraDoc + QRCoder hibrit. Tüm bileşenler MIT/Apache/BSD permissive. **3 yıl $2097 tasarruf.**

---

## 3. Yeni NuGet / JS Bağımlılıkları

### NuGet (C# .NET 10)

| Paket | Sürüm | Lisans | Modül |
|---|---|---|---|
| `Stateless` | 5.20.1 | Apache-2.0 | Mosaik.Core / Plan 36 WorkflowEngine |
| `FuzzySharp` | latest | MIT | Mosaik.Modules.Kvkk / Plan 40 Pattern 1 |
| `Gotenberg.Sharp.API.Client` | 3.0.0 | Apache 2.0 | Mosaik.Modules.ProcessRuntime / Plan 42 Faz 5 (Docker server MIT) |
| `Razor.Templating.Core` | latest | Apache 2.0 | Mosaik.Modules.ProcessRuntime / Plan 42 Faz 5 (Razor → HTML string) |
| `QRCoder` | 1.8.0 | MIT | Mosaik.Modules.ProcessRuntime / Plan 42 Faz 5 (QR code) |
| ~~`QuestPDF`~~ | ~~2026.5.0~~ | ~~Hybrid ($699/yr)~~ | ❌ rev 2 reddedildi — Gotenberg + MigraDoc hibrit |
| `DocumentFormat.OpenXml` | 3.5.1 | MIT | Mosaik.Modules.ProcessRuntime / Plan 42 |
| `OfficeIMO.Word` | 1.0.34 | MIT | Mosaik.Modules.ProcessRuntime / Plan 42 |
| `PDFsharp` | 6.2.0 | MIT | Mosaik.Modules.ProcessRuntime / Plan 42 (signature) |
| `Scriban` | 7.2.0+ | BSD-2 | Mosaik.Modules.ProcessRuntime / Plan 42 result template |
| `Ical.Net` | 5.2.2 | MIT | Mosaik.Modules.ProcessRuntime / Plan 42 ICS feed |
| `ZXing.Net` | latest | Apache-2.0 | Mosaik.Modules.ProcessRuntime / Plan 42 QR code (QuestPDF entegrasyon) |
| `Microsoft.Playwright` | latest | MIT | Mosaik.Modules.ProcessRuntime / Plan 42 Faz 6+ (KPI PDF) — opsiyonel |
| `ClosedXML` | mevcut | MIT | (zaten kullanımda, korunsun) |

### Vanilla JS / static assets

| Paket | Sürüm | Lisans | Konum |
|---|---|---|---|
| **SurveyJS Form Library** | 3.x UMD bundle | MIT | `wwwroot/lib/surveyjs/` — CDN değil, offline kurum env için |
| **signature_pad** | 5.1.1 UMD | MIT | `wwwroot/assets/js/form-signature.js` IIFE wrapper |
| **vis-timeline** | 8.5.1 standalone bundle | Apache/MIT | `wwwroot/lib/vis-timeline/` |
| **EasyMDE** (opsiyonel) | latest | MIT | `wwwroot/lib/easymde/` (Plan 34 SOP markdown editor) |
| **CookieConsent v3** (opsiyonel) | 3.x | MIT | `wwwroot/lib/cookieconsent/` (Privacy sayfası) |

### Docker yan-servisler

| Servis | Image | Modül | Profil |
|---|---|---|---|
| **Microsoft Presidio Analyzer** | `mcr.microsoft.com/presidio-analyzer` | Plan 40 KVKK | dev + prod |
| **Microsoft Presidio Anonymizer** | `mcr.microsoft.com/presidio-anonymizer` | Plan 40 KVKK + Plan 42 retention sweep | dev + prod |
| **spaCy `tr_core_news_trf`** | Custom Dockerfile bake (HuggingFace download) | Presidio NlpEngine | dev + prod |
| **Gotenberg 8.32** | `gotenberg/gotenberg:8.32` | Plan 42 Faz 5 PDF üretimi | dev + prod (RAM ~1Gi) |

---

## 4.1 Bağımsız doğrulama — awesome-dotnet-pdf-libraries-2025 (2026-05-21 ek tarama)

Kullanıcı `https://github.com/csharp-pdf-libraries/awesome-dotnet-pdf-libraries-2025` referansı paylaştı. 73+ kütüphane envanteri ile mevcut kararlarımız **bağımsız doğrulandı**:

- **Gotenberg MIT** kabul: "Self-hosted Docker API, multiple conversion engines"
- **PDFSharp + MigraDoc MIT**: "Actively maintained, document object model"
- **Bootstrap homepage test:** Sadece IronPDF/PuppeteerSharp/Playwright/Gotenberg-Chromium modern CSS3 (Flexbox/Grid) destekliyor → **Mosaik Tailwind reuse için Gotenberg seçimi kritik onay**
- **iText AGPL, Aspose $1199, wkhtmltopdf deprecated** reddetmelerimiz doğrulandı
- **QuestPDF** reddetmesi rev 2'de yapıldı (DSL + lisans + Razor reuse yok)

Awesome-list'te keşfedilen 13 yeni aday değerlendirildi, **0'ı stack'i değiştirir**:

| Aday | Lisans | Sonuç |
|---|---|---|
| Scryber.core | LGPL | XML XSL-FO, Razor HTML yaklaşımına alakasız |
| pdfpig | Apache 2.0 | Mosaik'te zaten kullanılıyor (DikkatIQ PDF extract reading) |
| PeachPDF / ZetPDF / VectSharp | Free / GPL | Niş, Gotenberg kapsıyor |
| Fluid (sebastienros) | Apache 2.0 | RESEARCH'te zaten yedek (Scriban alternatifi) |
| Docotic.Pdf / GemBox.Pdf | Free/Commercial dual | MIT pure (PdfSharp+MigraDoc) zaten karşılıyor |
| FastReport.NET | Free/Commercial dual | Reporting engine, Reports modülü zaten var |
| Rotativa / NReco / TuesPechkin | MIT/Free | wkhtmltopdf bağımlı, 2023 arşiv |
| IronPDF | Proprietary $749+ | Pahalı, alternatif var |
| Syncfusion PDF Framework | Proprietary $395/ay | Aşırı pahalı |
| Apryse / PSPDFKit / Foxit / Adobe / GdPicture / ABCPDF / DynamicPDF / Telerik | Proprietary | Enterprise scope dışı |
| Api2pdf / PDFBolt / CraftMyPDF / pdforge | SaaS | Cloud bağımlılık + KVKK riski |
| HTMLDOC | GPL 2011 | Abandoned |

**Net karar:** Stack değişmez. **Gotenberg + PdfSharp/MigraDoc + QRCoder + Razor.Templating.Core** rev 2 doğrulandı.

## 4. Kaynak Referansları

### KVKK Resmi (indirilecek `docs/kvkk-references/` gitignored)
- [KVKK Envanter Excel](https://kvkk.gov.tr/SharedFolderServer/CMSFiles/b5fe209d-3c12-4c70-8dac-a2eaa5742ae6.xlsx) — Plan 40 Faz 6 VERBİS export reference format
- [KVKK Mart 2025 Rehberi (Yayın No 61)](https://www.kvkk.gov.tr/Icerik/5446/Kisisel-Veri-Isleme-Envanteri-Hazirlama-Rehberi)
- [VERBİS Kılavuz PDF](https://verbis.kvkk.gov.tr/sharedFolder/veri-sorumlulari-sicil-bilgi-sistemi-kilavuzu.pdf)
- [KVKK 6698 Kanun Tam Metni](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf) — Resmi Gazete 7 Nisan 2016, No: 29677
- [6698 Konsolide Metin (LexPera)](https://www.lexpera.com.tr/mevzuat/kanunlar/kisisel-verilerin-korunmasi-kanunu-6698)

### Awesome-list referansı (bağımsız doğrulama 2026-05-21)
- [awesome-dotnet-pdf-libraries-2025](https://github.com/csharp-pdf-libraries/awesome-dotnet-pdf-libraries-2025) — 73+ kütüphane envanteri. Mevcut kararlarımız bağımsız doğrulandı: Gotenberg + PdfSharp/MigraDoc seçimi onay, iText AGPL + Aspose + wkhtmltopdf reddetmeleri doğru, QuestPDF reddi doğru (Bootstrap CSS3 test geçen 4'ten biri ama lisans/DSL/Razor reuse dezavantajları kalır).

### GitHub repolar
- [Stateless](https://github.com/dotnet-state-machine/stateless)
- [SurveyJS Form Library](https://github.com/surveyjs/survey-library)
- [signature_pad](https://github.com/szimek/signature_pad)
- [QuestPDF](https://github.com/QuestPDF/QuestPDF)
- [PDFsharp](https://github.com/empira/PDFsharp)
- [OfficeIMO](https://github.com/EvotecIt/OfficeIMO)
- [Microsoft Presidio](https://github.com/microsoft/presidio)
- [FuzzySharp](https://github.com/JakeBayer/FuzzySharp)
- [Scriban](https://github.com/scriban/scriban)
- [Ical.Net](https://github.com/ical-org/ical.net)
- [vis-timeline](https://github.com/visjs/vis-timeline)
- [CookieConsent](https://github.com/orestbida/cookieconsent) (opsiyonel)
- [EasyMDE](https://github.com/Ionaru/easy-markdown-editor) (opsiyonel)

### Türkçe NLP
- [turkish-nlp-suite/tr_core_news_trf](https://huggingface.co/turkish-nlp-suite/tr_core_news_trf) — spaCy Türkçe transformer NER (Presidio NlpEngine)

---

## 5. Aksiyon Listesi (Plan'ları başlatmadan önce)

1. **NuGet paketleri ekle** (Plan 36/40/41/42 başladığında):
   - Stateless 5.20.1, FuzzySharp, QuestPDF, DocumentFormat.OpenXml, OfficeIMO.Word, PDFsharp, Scriban (≥7.2.0), Ical.Net 5.2.2, ZXing.Net
2. **JS assets indir** (`wwwroot/lib/`):
   - SurveyJS Form Library UMD bundle, signature_pad UMD, vis-timeline standalone bundle
3. **KVKK referansları indir** (`docs/kvkk-references/` gitignored):
   - KVKK envanter xlsx, Mart 2025 Rehber PDF, VERBİS Kılavuz PDF, 6698 Kanun PDF
4. **Docker compose Presidio** dev profil:
   - Analyzer + Anonymizer servis, Türkçe spaCy `tr_core_news_trf` Dockerfile bake
   - Custom recognizer Python: `TcKimlikRecognizer.py` (11-hane + Mod10/Mod11), `IbanTrRecognizer.py` (`^TR\d{24}$` + checksum)
5. **C# adapter:** `Mosaik.Core.AI/Privacy/PresidioClient.cs` HttpClient + DTO
6. ~~QuestPDF Professional lisans satın al~~ → **REV 2: GEREKMEZ** ($0 stack). Gotenberg Docker container + NuGet `Gotenberg.Sharp.API.Client 3.0.0` + `PDFsharp-MigraDoc 6.2.4` + `QRCoder 1.8.0` + `Razor.Templating.Core`. **Lisans bütçesi yok.**
7. **3 ADR yaz** (Plan implementation öncesi):
   - ADR-019: Workflow backend engine — Stateless + Hangfire
   - ADR-020: Form Builder Hybrid — SurveyJS Form Library renderer + DIY drag-drop builder
   - ADR-021: Document rendering stack — QuestPDF Pro + OpenXml + OfficeIMO + PDFsharp + ClosedXML

---

## 6. Bilinen Riskler

| Risk | Önlem |
|---|---|
| ~~**QuestPDF Professional lisans bütçesi** ~$699/yıl~~ | **REV 2 ÇÖZÜLDÜ** — Gotenberg + MigraDoc hibrit stack, $0 lisans, Razor reuse |
| **Gotenberg container down → PDF üretimi durur** | MigraDoc in-process fallback path aynı kod (bulk + signature ile birleşik) |
| **Docker compose RAM yükü** | Plan 40 Presidio (~1Gi) + Gotenberg (~1Gi) = compose <2GB toplam BKM IT için kabul |
| **Stateless tek bakıcı** | ~700 satır → gerekirse Mosaik.Core/Workflow/ altına internal copy + Apache-2.0 attribution |
| **vis-timeline moment.js bağımlılığı** | dayjs migration future-proof değil; standalone bundle 1 dep kabul edilebilir |
| **Scriban v7 breaking** | `>=7.2.0` lock; eski örnek kodları v6 syntax olabilir |
| **Ical.Net v5 breaking** | rianjs fork v4 örnek kod stack overflow'da bol; ical-org v5 doc yenisi al |
| **SurveyJS Knockout legacy** ~250kb minified | Ayrı bundle, dashboard iframe gibi load-on-demand |
| **Presidio Türkçe paketi resmi değil** | turkish-nlp-suite community model — version pin + smoke test |
| **CVE-2024 Scriban sandbox escape** (CVSS 9.1) | v7.0.0+ fix, NuGet `>=7.2.0` pin |
| **vis-timeline gelecekte dayjs zorunluluğu** | Mosaik IIFE wrapper, lib swap kolay |
| **VERBİS API yok** | Manuel upload, Excel export yeterli |
| **Türk SaaS abonelik** ₺15-50k/yıl | Reddedildi — in-house Plan 40 |

---

## 7. Sonuç

Hazır OSS parçalar ile vNext altılı kalp **~62-77h tasarruf** edilir (~%24-26 — Gotenberg Razor reuse +2h ek tasarruf). **Yıllık lisans maliyeti: $0** (rev 2). Tüm paketler MIT/Apache 2.0/BSD permissive. **3 yıl $2097 tasarruf** (QuestPDF Pro'ya karşı).

**Sıralama** (kullanıcı onayı sonrası):
1. NuGet + JS assets + KVKK referansları indir
2. ADR-019/020/021 yaz
3. Plan 36 Faz A başla (Stateless + Hangfire WorkflowEngine)
4. Plan 41 Faz 0 paralel (Mosaik.Modules.Forms scaffold + SurveyJS bundle entegrasyon)
5. Plan 40 Faz 0 paralel (Mosaik.Modules.Kvkk + Presidio Docker dev profil)
6. Plan 42 Faz 0 Plan 41 Faz 3 bitince başlar

Excel manuel kayıt günlüğü kapanır, portal runtime execution platform olur. Hiçbir tekerleği baştan icat etmeden.
