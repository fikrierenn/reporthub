# Mosaik — Library Radar

**Oluşturulma:** 2026-05-20
**Kaynak:** 5 paralel agent araştırması (Dashboard + DMS + AI + Communication + SOP/Forms/HR)
**Amaç:** Mevcut ve planlanan modülleri güçlendirecek open-source kütüphane envanteri. Plan yazılmadan önce buraya bak.

---

## Nasıl Okunur

- **Öncelik:** 🔴 Hemen (CDN/NuGet, sıfır altyapı) · 🟡 Orta vadeli (küçük setup) · 🟢 Uzun vadeli (sidecar/altyapı)
- **Durum:** `İncelendi` · `Planlandı` · `Uygulamada` · `Reddedildi`
- Her kütüphane için: ne yapar, neden seçildi, nasıl entegre edilir, risk var mı

---

## 1. Dashboard + Reports Modülü

### 1.1 Apache ECharts
- **GitHub:** https://github.com/apache/echarts — 66k★ — Apache 2.0
- **Ne yapar:** Chart.js'in desteklemediği chart tipleri: treemap, sankey, heatmap calendar, Gantt (custom series), candlestick, scatter, radar, geo map. 100k+ nokta performansı, canvas-first.
- **Neden:** Tek CDN ile Chart.js'in tüm açıklarını kapatır. Dashboard builder'da "chart tipi" dropdown'ına 10+ yeni tip eklenebilir.
- **Entegrasyon:** CDN `<script>` tag, `echarts.init(container)` + JSON config. Mevcut `DashboardRenderer.cs`'e yeni `ChartRenderer` case eklenir. Chart.js ile yan yana yaşayabilir (replace gerekmez).
- **Risk:** Chart.js'den farklı config formatı — mevcut dashboard JSON'ları etkilenmez, sadece yeni widget tipleri ECharts ile yazılır.
- **Öncelik:** 🔴 | **Durum:** İncelendi
- **Hedef plan:** Dashboard Builder V3 veya Plan 08 revizyonu

### 1.2 Frappe Gantt
- **GitHub:** https://github.com/frappe/gantt — 4k★ — MIT
- **Ne yapar:** SVG tabanlı hafif Gantt chart, drag-resize bar, bağımlılık okları.
- **Neden:** Proje/görev/yükümlülük timeline görünümü. Plan 36 Workflow timeline için aday.
- **Entegrasyon:** CDN, `new Gantt("#gantt", tasks)`. Task objesi: `{id, name, start, end, progress, dependencies}`.
- **Risk:** Yok. ECharts ile çakışmaz, farklı use case.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 36 Faz B (instance dashboard timeline)

### 1.3 Tabulator
- **GitHub:** https://github.com/olifolkerd/tabulator — 6.5k★ — MIT
- **Ne yapar:** Virtual scroll, column resize, sort/filter, grouping, built-in XLSX + CSV + PDF export. Zero jQuery.
- **Neden:** Mevcut `<table class="dt">` manuel render'ı büyük result set'lerde yavaş. Tabulator virtual scroll ile 10k+ satır sorunsuz.
- **Entegrasyon:** CDN, `new Tabulator("#table", { data: [...], columns: [...] })`. Mevcut SP result JSON'u doğrudan beslenir.
- **Risk:** Mevcut `.dt` CSS override gerekebilir — izole `<div>` içinde kullan, class çakışması olmaz.
- **Öncelik:** 🔴 | **Durum:** İncelendi
- **Hedef plan:** Reports Run.cshtml tablo widget upgrade

### 1.4 SheetJS CE (Community Edition)
- **GitHub:** https://github.com/SheetJS/sheetjs — 35k★ — Apache 2.0
- **Ne yapar:** Browser'da JSON array → XLSX dosyası oluştur, download et. Server-side export controller'a gerek kalmaz.
- **Neden:** Mevcut export controller SP'yi tekrar çalıştırıyor. Client-side export zaten render edilmiş datayı kullanır → daha hızlı, controller yükü sıfır.
- **Entegrasyon:** CDN, `XLSX.writeFile(XLSX.utils.book_new(), "export.xlsx")`. Dashboard widget'a "Excel İndir" butonu.
- **Risk:** Community edition'da bazı gelişmiş format özellikleri yok (Pro ile). Temel export için yeterli.
- **Öncelik:** 🔴 | **Durum:** İncelendi

### 1.5 jsPDF + jspdf-autotable
- **GitHub:** https://github.com/parallax/jsPDF — 29k★ — MIT
- **Ne yapar:** Browser'da tablo → PDF oluştur, download. `autoTable` plugin ile grid-to-PDF 5 satır.
- **Neden:** Rapor sonuçlarının PDF export'u için sunucu roundtrip gerektirmez.
- **Entegrasyon:** CDN, `doc.autoTable({ head: cols, body: rows }); doc.save("rapor.pdf")`.
- **Risk:** Türkçe font embed gerekir (jsPDF'de TR karakter desteği için custom font yükle).
- **Öncelik:** 🟡 | **Durum:** İncelendi

### 1.6 Native EventSource (SSE)
- **Ne yapar:** Server-Sent Events — server'dan client'a tek yönlü push. Hangfire job tamamlandığında dashboard'u otomatik yeniler.
- **Neden:** SignalR'dan hafif, sadece server→client bildirim yeterli dashboard için.
- **Entegrasyon:** `Response.ContentType = "text/event-stream"` endpoint + `new EventSource("/api/dashboard/live")` client. Sıfır library.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** AI Wizard status polling (mevcut `setInterval` replace)

---

## 2. Documents + DMS Modülü

### 2.1 Mozilla PDF.js
- **GitHub:** https://github.com/mozilla/pdf.js — 49k★ — Apache 2.0
- **Ne yapar:** Browser'da PDF render — download gerektirmez, inline viewer.
- **Neden:** Documents detay sayfasında PDF preview. Server-side Gotenberg'e gerek yok.
- **Entegrasyon:** `wwwroot/assets/vendor/pdfjs/` prebuilt viewer. `<iframe src="/assets/vendor/pdfjs/web/viewer.html?file=/Documents/Download/123">` — Download endpoint'i zaten var (Plan 33 BUGFIX-3).
- **Risk:** Yok. Statik dosyalar, sıfır backend değişiklik.
- **Öncelik:** 🔴 | **Durum:** İncelendi
- **Hedef plan:** Documents/Details.cshtml

### 2.2 PDFtoImage (sungaila)
- **GitHub:** https://github.com/sungaila/PDFtoImage — MIT — NuGet 5.2.1 — 3.5M download
- **Ne yapar:** PDF sayfası → `SKBitmap`/PNG/JPEG. PDFium tabanlı, .NET 10 destekli.
- **Neden:** Doküman liste sayfasında thumbnail, AI extraction önizleme için sayfa görseli.
- **Entegrasyon:** `NuGet: PDFtoImage` + `Conversion.ToImage(stream, page: 0)`. `DocumentsController`'a `GET /Documents/Thumbnail/{id}` endpoint ekle.
- **Risk:** `Sdcb.PDFium.runtime.*` native paket de gerekiyor (platform-specific NuGet).
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 27 Faz C (DMS Foundation)

### 2.3 Gotenberg
- **GitHub:** https://github.com/gotenberg/gotenberg — 8k★ — MIT
- **Ne yapar:** Docker sidecar — Word/Excel/PPT → PDF dönüşümü. LibreOffice + Chromium wrap eder. `HttpClient` ile multipart POST.
- **Neden:** Documents modülünde Word dosyaları PDF olarak görüntülenebilir.
- **Entegrasyon:** `docker run gotenberg/gotenberg:8`. ASP.NET'ten `HttpClient.PostAsync("http://gotenberg/forms/libreoffice/convert")`.
- **Risk:** BKM on-premise'de Docker izni var mı? Yok ise LibreOffice headless fallback.
- **Öncelik:** 🟢 | **Durum:** İncelendi — Docker ortamı doğrulanınca

### 2.4 diff2html
- **GitHub:** https://github.com/rtfpessoa/diff2html — 9k★ — MIT
- **Ne yapar:** Unified diff string → side-by-side HTML render. Versiyon karşılaştırma UI.
- **Neden:** Plan 27 Faz C versiyonlama — iki versiyon metni diff olarak göster.
- **Entegrasyon:** CDN. Server: PdfPig ile iki versiyonun metnini çek, `diff` algoritması uygula (LibGit2Sharp veya basit Myers diff), string gönder. Client: `diff2html.html(diffString)`.
- **Risk:** Binary DOCX için text extraction kalitesine bağlı.
- **Öncelik:** 🟢 | **Durum:** İncelendi
- **Hedef plan:** Plan 27 Faz C

### 2.5 Mark.js
- **GitHub:** https://markjs.io — 7k★ — MIT
- **Ne yapar:** DOM'da keyword highlight. SQL Server FTS sonuçlarını sayfada işaretle.
- **Neden:** FTS search sonuçlarında arama terimi vurgulama — UX standardı.
- **Entegrasyon:** CDN, `new Mark(".content").mark("arama terimi")`. Search endpoint'ten highlight term dön, client'ta uygula.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 27 Faz C (FTS UI)

### 2.6 iText 7 for .NET — ⚠️ AGPL LİSANS
- **Ne yapar:** PAdES/PKCS#7 dijital imza, TSA timestamp — TÜBİTAK KamuSM uyumlu.
- **Neden:** E-imza (Plan 27 Faz D) için tek olgun .NET seçeneği.
- **Risk:** **AGPL — Mosaik closed-source ise ticari lisans zorunlu.** Fiyat araştırılacak. Alternatif: PdfSharpCore (MIT, temel imza) veya ESYA API (devlet kütüphanesi).
- **Durum:** Reddedildi (lisans) — alternatif araştırılacak
- **Öncelik:** 🟢 | **Hedef plan:** Plan 27 Faz D

---

## 3. AI + LLM Altyapısı

### 3.1 Docling (IBM)
- **GitHub:** https://github.com/docling-project/docling — 58k★ — MIT — Python
- **Ne yapar:** Computer vision tabanlı PDF layout analizi. Tablo, başlık, sütun, header/footer ayrımı. JSON/Markdown çıktı. PdfPig'den çok daha akıllı.
- **Neden:** Plan 27 Faz A'da Tesseract + PdfPig pipeline'ı karmaşık sayfaları kaçırıyor. Docling bunu çözer.
- **Entegrasyon:** FastAPI sidecar (`uvicorn`). ASP.NET'ten `HttpClient.PostAsync("/parse", pdfBytes)` → JSON. Mevcut `AiExtractionWorker`'da `IPdfParser` interface'i soyutla, Docling implementasyonu yaz.
- **Risk:** Python runtime gerekli. Sidecar tek process'te pdfplumber + spaCy ile birleştirilebilir.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 27 Faz A upgrade / Plan D-02 AI altyapı iyileştirme

### 3.2 PaddleSharp (Sdcb.PaddleOCR)
- **GitHub:** https://github.com/sdcb/PaddleSharp — MIT — NuGet — .NET 10 uyumlu
- **Ne yapar:** PP-OCRv4/v5 motoru, karmaşık layout + Türkçe metin. Tesseract'tan belirgin üstün.
- **Neden:** Taranmış PDF'lerde Tesseract başarısız olduğu durumlarda (eğik, düşük DPI, el yazısı).
- **Entegrasyon:** NuGet: `Sdcb.PaddleOCR` + `Sdcb.PaddleOCR.runtime.win64`. `OcrAll(mat)` → `OcrResult`. Mevcut `TesseractOcrExtractor`'ın yanına alternatif olarak ekle, confidence'a göre seç.
- **Risk:** Native Windows x64 runtime paketi (Linux için ayrı paket). BKM sunucusu Windows ise sorunsuz.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan D-02 AI altyapı iyileştirme

### 3.3 spaCy tr_core_news_trf (Türkçe NER)
- **GitHub:** https://github.com/turkish-nlp-suite/turkish-spacy-models — MIT — Python sidecar
- **Ne yapar:** Türkçe transformer-based NER: PER (kişi), ORG (firma), LOC (yer), DATE (tarih), MONEY (tutar). Sözleşme tarafı, imza tarihi, bedel otomatik bulunur.
- **Neden:** Mevcut extraction LLM'e sorarak yapıyor — NER ile pre-fill yapılırsa LLM prompt kısalır, maliyet düşer, doğruluk artar.
- **Entegrasyon:** FastAPI sidecar `POST /ner` → `{text} → [{text, label, start, end}]`. Docling sidecar'ıyla aynı servis içinde çalıştır. `AiExtractionWorker`'da Stage 1 öncesi NER run et, sonuçları prompt context'e ekle.
- **Risk:** Transformer model ~500MB, ilk yüklemede yavaş. Singleton pattern ile singleton instance.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 27 Faz A (extraction kalite artışı)

### 3.4 BGE-M3 + SQL Server 2025 VECTOR
- **HuggingFace:** https://huggingface.co/BAAI/bge-m3 — MIT
- **Ne yapar:** 100+ dil, 8192 token, 1024 boyutlu dense vector. SQL Server 2025 `VECTOR` tipi + EF Core 10 `SqlVector<float>` ile native integration.
- **Neden:** Plan 27 Faz E RAG pipeline — doküman embedding'leri DB'de sakla, semantic search yap.
- **Entegrasyon:**
  ```csharp
  // EF Core 10 entity
  public SqlVector<float>? Embedding { get; set; }
  // Insert
  entity.Embedding = new SqlVector<float>(await embeddingApi.GetAsync(text));
  // Search
  context.Documents.OrderBy(d => EF.Functions.VectorDistance("cosine", d.Embedding, queryVector)).Take(5)
  ```
  Sidecar: `sentence-transformers` + FastAPI `POST /embed` → `float[]`.
- **Risk:** SQL Server 2025 gerekiyor (şu an SQL Server sürümü doğrulanmalı).
- **Öncelik:** 🟢 | **Durum:** İncelendi
- **Hedef plan:** Plan 27 Faz E (RAG)

### 3.5 LiteLLM Proxy
- **GitHub:** https://github.com/BerriAI/litellm — 20k★ — MIT — Python
- **Ne yapar:** Groq/Gemini/Claude/OpenAI için unified proxy. Redis-backed semantic cache — aynı prompt tekrar gelirse LLM çağrısı yapmaz.
- **Neden:** Mevcut `FallbackLlmService` doğrudan HTTP çağırıyor. LiteLLM önüne koyunca: cache hit = maliyet sıfır, fallback mantığı LiteLLM'de.
- **Entegrasyon:** Docker `litellm --config config.yaml`. `FallbackLlmService`'teki base URL'leri LiteLLM proxy'ye yönlendir. Config'de model priority + fallback tanımla.
- **Risk:** Redis dependency (GPTCache için). Sıfır cache ile de çalışır (sadece unified routing).
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan D-02 AI altyapı iyileştirme

### 3.6 pdfplumber
- **GitHub:** https://github.com/jsvine/pdfplumber — 6k★ — MIT — Python
- **Ne yapar:** PDF'den tablo extraction (borderless dahil). Fatura, bordro, mali tablo için Camelot'tan iyi.
- **Entegrasyon:** Docling sidecar içinde `/extract-tables` route. `pdfplumber.open(file).pages[n].extract_tables()`.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 27 Faz A (tablo extraction)

### 3.7 Microsoft Presidio + Turkish spaCy
- **GitHub:** https://github.com/microsoft/presidio — 4k★ — MIT — Python
- **Ne yapar:** PII detection + redaction. Türkçe: `tr_core_news_trf` engine + custom TCKN regex recognizer ekle.
- **Neden:** KVKK — sözleşmede TC kimlik no, telefon, e-posta redact.
- **Entegrasyon:** `presidio-analyzer` REST mode. `POST /analyze` → span listesi. `POST /anonymize` → redacted text. spaCy NER sidecar ile aynı servis.
- **Öncelik:** 🟢 | **Durum:** İncelendi
- **Hedef plan:** Plan 27 Faz D (Compliance)

---

## 4. Communication + Notification Modülü

### 4.1 Quill.js v2 + Tribute.js
- **Quill:** https://github.com/quilljs/quill — 42k★ — BSD
- **Tribute:** https://github.com/zurb/tribute — 3.5k★ — MIT
- **Ne yapar:** Quill = delta-format rich text editor. Tribute = `@mention` autocomplete — herhangi bir contenteditable/textarea'ya eklenir.
- **Neden:** Plan 35 Comment/Mention sistemi. Tamim editörü zaten Quill kullanıyor — aynı stack, tutarlılık.
- **Entegrasyon:**
  - Tribute: `new Tribute({ values: fetch("/api/users/search?q="), selectTemplate: (item) => "@" + item.original.username })`. Quill blur event'inde Tribute attach.
  - Backend: `GET /api/users/search?q=ali` → `[{id, username, displayName}]`
- **Risk:** Quill v2 ile Tribute uyumu test edilmeli.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 35 (Comment/Mention)

### 4.2 Ical.Net
- **GitHub:** https://github.com/rianjs/ical.net — 0.9k★ — MIT — .NET
- **Ne yapar:** RFC 5545 uyumlu `.ics` dosyası/feed oluşturur. Outlook + Google Calendar + Apple Calendar okur.
- **Neden:** Plan 36 Faz B ICS endpoint — yükümlülük deadline'larını kullanıcının takvimine push et.
- **Entegrasyon:**
  ```csharp
  var calendar = new Calendar();
  calendar.Events.Add(new CalendarEvent {
      DtStart = new CalDateTime(dueDate),
      Summary = obligation.Title,
      Alarms = { new Alarm { Duration = Duration.FromDays(-7) } }
  });
  return File(IcsSerializer.SerializeToByteArray(calendar), "text/calendar", "obligations.ics");
  ```
- **Risk:** Yok. NuGet: `Ical.Net`.
- **Öncelik:** 🔴 | **Durum:** İncelendi
- **Hedef plan:** Plan 36 Faz B (W-12)

### 4.3 FluentEmail + MailKit
- **FluentEmail:** https://github.com/lukencode/FluentEmail — 3.1k★ — MIT
- **MailKit:** https://github.com/jstedfast/MailKit — 6.3k★ — MIT
- **Ne yapar:** FluentEmail = fluent builder + Razor template renderer. MailKit = async SMTP (Microsoft SmtpClient deprecated). Hangfire ile queue.
- **Neden:** Plan 32 SMTP altyapısı. `IEmailService` interface'i zaten var.
- **Entegrasyon:**
  ```csharp
  services.AddFluentEmail("noreply@bkm.com")
          .AddRazorRenderer()
          .AddMailKitSender(smtpSettings);
  // Hangfire job
  await _mailer.To(user.Email).UsingTemplate("Templates/Notification.cshtml", model).SendAsync();
  ```
- **Risk:** Razor email template'leri `Views/EmailTemplates/` altında — HttpContext gerektirmez (RazorLight alternatif).
- **Öncelik:** 🔴 | **Durum:** İncelendi
- **Hedef plan:** Plan 32

### 4.4 ASP.NET Core SignalR (built-in)
- **Ne yapar:** WebSocket hub — per-user channel, otomatik reconnect. Bildirim badge'ini server'dan push et.
- **Neden:** Yeni bildirim gelince sayfa yenileme yerine badge anında güncellenir.
- **Entegrasyon:** `services.AddSignalR()`. `NotificationHub : Hub`. `_AppLayout.cshtml`'e `<script src="/signalr/dist/browser/signalr.js">` + `connection.on("NewNotification", count => updateBadge(count))`. `INotificationService.SendAsync`'te `hubContext.Clients.User(userId).SendAsync("NewNotification", unreadCount)`.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Notifications modülü upgrade

### 4.5 RazorLight
- **GitHub:** https://github.com/toddams/RazorLight — 1.5k★ — Apache 2.0 — .NET
- **Ne yapar:** HttpContext olmadan `.cshtml` template render — e-posta template'leri Razor syntax'ında yaz.
- **Neden:** FluentEmail + Razor renderer için. E-posta şablonları mevcut UI pattern'larıyla tutarlı kalır.
- **Entegrasyon:** `new RazorLightEngineBuilder().UseFileSystemProject("Views/EmailTemplates").Build()`. FluentEmail `.AddRazorRenderer(Path.Combine(env.ContentRootPath, "Views/EmailTemplates"))`.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 32

### 4.6 WebPush-CSharp (Opsiyonel)
- **GitHub:** https://github.com/web-push-libs/web-push-csharp — 0.5k★ — MIT
- **Ne yapar:** RFC 8030 / VAPID browser push notification. Kullanıcı browser'ı kapalıyken bile bildirim.
- **Neden:** "Görmedim" mazaretini ortadan kaldıran "no-excuse" platform için kritik.
- **Entegrasyon:** VAPID key pair generate → DB'de `PushSubscription` tablo → `webPushClient.SendNotificationAsync(subscription, payload)`.
- **Risk:** Browser izin gerektirir. Kurumsal Chrome policy'de kısıtlı olabilir.
- **Öncelik:** 🟢 | **Durum:** İncelendi

---

## 5. SOP + Forms + OrgChart + Genel

### 5.1 TipTap
- **GitHub:** https://github.com/ueberdosis/tiptap — 30k★ — MIT
- **Ne yapar:** ProseMirror tabanlı extensible rich text editor. Framework-agnostic vanilla JS bundle (`@tiptap/core`). Tablo, resim upload, code block, işbirliği (opsiyonel).
- **Neden:** Quill'den daha modern API, daha aktif geliştirme. SOP + Tamim + Comment editörü için tek standart.
- **Entegrasyon:** CDN bundle veya `npm build` → `wwwroot`. `new Editor({ element: container, extensions: [StarterKit, Table, Image] })`. Quill'den geçiş: Delta format → HTML dönüşüm gerekir (mevcut Tamim içerikleri için migration).
- **Risk:** Mevcut Tamim modülü Quill kullanıyor — geçiş için içerik migration planı gerekir. Yeni modüller TipTap ile başlar, Tamim sonradan geçer.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan 34 SOP + Plan 35 Comment

### 5.2 SurveyJS
- **GitHub:** https://github.com/surveyjs/survey-library — 4k★ — MIT (runtime)
- **Ne yapar:** JSON tanımlı form builder. CDN ile çalışır. React gerektirmez. Vanilla JS renderer mevcut. Builder UI ayrı (ticari), ama runtime MIT.
- **Neden:** VISION.md'de "Form Builder kendi üretim değil — open-source integrate" olarak işaretli. SurveyJS tam oturuyor.
- **Entegrasyon:** CDN `survey.core.js` + `survey.ui.css`. JSON form definition → `new Survey.Model(json)`. Submit → `POST /Forms/Submit` ile `{formId, answers: {...}}`.
- **Risk:** Builder UI ticari — sadece runtime kullanılacaksa JSON'u elle veya admin UI ile oluştur.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Form/Anket modülü (VISION.md sırası)

### 5.3 QuestPDF
- **GitHub:** https://github.com/QuestPDF/QuestPDF — 14k★ — MIT (community, <1M€ gelir)
- **Ne yapar:** Fluent C# API ile pixel-perfect PDF. Chromium yok, Razor yok — saf .NET.
- **Neden:** Rapor export, sözleşme özeti, SOP print için. Playwright'tan daha hızlı, daha öngörülebilir.
- **Entegrasyon:**
  ```csharp
  Document.Create(container => {
      container.Page(page => {
          page.Content().Table(table => { ... });
      });
  }).GeneratePdf("output.pdf");
  ```
  `DocumentsController`'a `GET /Documents/ExportPdf/{id}` endpoint. `return File(pdfBytes, "application/pdf")`.
- **Risk:** BKM < 1M€ gelir → MIT ücretsiz. Aşılırsa Community lisans ücretli olur (çok düşük ihtimal).
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Documents + Reports export

### 5.4 Playwright .NET
- **GitHub:** https://github.com/microsoft/playwright-dotnet — 4k★ — MIT
- **Ne yapar:** Headless Chromium → Razor view'ı tam render edip PDF al. CSS + Tailwind + tüm stil korunur.
- **Neden:** QuestPDF'in karşılayamadığı karmaşık layout (dashboard screenshot, grafik içeren rapor) için.
- **Entegrasyon:** `await playwright.Chromium.LaunchAsync()`. `await page.GotoAsync(url)`. `await page.PdfAsync(new() { Format = PaperFormat.A4 })`. Hangfire job olarak çalıştır.
- **Risk:** Chromium binary (~150MB). Server'da Chromium kurulu olmalı.
- **Öncelik:** 🟢 | **Durum:** İncelendi

### 5.5 Audit.NET
- **GitHub:** https://github.com/thepirat000/Audit.NET — 2.5k★ — MIT — .NET
- **Ne yapar:** EF Core interceptor ile entity-level otomatik change tracking. Her `SaveChanges`'ta Before/After snapshot + user/timestamp.
- **Neden:** Mevcut `AuditLogService` manuel — her action'a ayrı log çağrısı. Audit.NET ile EF seviyesinde otomatik olur, hiçbir şey kaçmaz.
- **Entegrasyon:** `services.AddDbContext<MosaikContext>(o => o.UseAuditEntityFramework())`. `AuditDataProvider` → `AuditLog` tablosuna yaz. Mevcut `AuditLogService` ile birlikte çalışabilir (farklı scope).
- **Risk:** Her entity değişikliğini log'lar → log hacmi artabilir. Whitelist/blacklist entity konfigürasyonu şart.
- **Öncelik:** 🟡 | **Durum:** İncelendi
- **Hedef plan:** Plan D-01 veya ayrı plan

### 5.6 D3.js Collapsible Tree
- **GitHub:** https://github.com/d3/d3 — 110k★ — ISC
- **Ne yapar:** SVG tabanlı, 10k+ node OrgChart, expand/collapse, zoom, SVG export.
- **Neden:** Mevcut dabeng/OrgChart 500+ node'da yavaşlıyor. D3 ile sanal render + lazy expand.
- **Entegrasyon:** Mevcut `json-digger` veri formatı korunabilir. D3 `d3.hierarchy()` + `d3.tree()` layout. Custom implementation ~300-400 satır IIFE.
- **Risk:** D3 öğrenme eğrisi yüksek. Tek geliştirici için zaman yatırımı büyük. Sadece 500+ node ihtiyacı belirginleşince geçilsin.
- **Öncelik:** 🟢 | **Durum:** İncelendi
- **Hedef plan:** OrgChart v2 (gerektiğinde)

---

## Özet Öncelik Tablosu

### 🔴 Hemen (CDN/NuGet, sıfır altyapı)

| Library | Modül | NuGet/CDN | İş büyüklüğü |
|---|---|---|---|
| Apache ECharts | Dashboard | CDN | 1-2 gün |
| Tabulator | Reports/Documents | CDN | 1 gün |
| PDF.js | Documents | wwwroot static | 0.5 gün |
| Ical.Net | Workflow/Obligations | NuGet | 0.5 gün |
| FluentEmail + MailKit | Notifications/Email | NuGet | 1-2 gün |
| SheetJS CE | Reports export | CDN | 0.5 gün |

### 🟡 Orta vadeli (küçük setup, NuGet veya npm build)

| Library | Modül | Setup | İş büyüklüğü |
|---|---|---|---|
| TipTap | SOP/Comment/Tamim | npm bundle | 2-3 gün |
| SurveyJS | Form/Anket | CDN | 1-2 gün |
| QuestPDF | Documents/Reports | NuGet | 1 gün |
| PaddleSharp | AI/OCR | NuGet (native) | 1 gün |
| Quill + Tribute.js | Comment/Mention | CDN | 1 gün |
| SignalR | Notifications | Built-in | 1 gün |
| Audit.NET | Audit | NuGet | 1 gün |
| Mark.js | Documents FTS | CDN | 0.5 gün |
| LiteLLM proxy | AI | Docker | 1-2 gün |

### 🟢 Uzun vadeli (sidecar/altyapı/Docker)

| Library | Modül | Setup | İş büyüklüğü |
|---|---|---|---|
| Docling | AI/Documents | Python sidecar | 2-3 gün |
| spaCy TR NER | AI/Contracts | Python sidecar | 1-2 gün |
| BGE-M3 + VECTOR | AI/RAG | Python + SQL 2025 | 3-5 gün |
| Presidio + TR | AI/KVKK | Python sidecar | 2-3 gün |
| Gotenberg | Documents | Docker | 1 gün |
| Playwright .NET | PDF gen | Chromium | 1-2 gün |
| WebPush-CSharp | Notifications | NuGet | 1 gün |
| D3 OrgChart | OrgChart | CDN + custom | 3-5 gün |

---

## Python Sidecar Konsolidasyonu

Docling + pdfplumber + spaCy NER + Presidio + BGE-M3 = **tek FastAPI servisi**:

```
POST /parse          → Docling (PDF layout)
POST /extract-tables → pdfplumber (tablo)
POST /ner            → spaCy TR NER
POST /embed          → BGE-M3 (vector)
POST /redact         → Presidio (PII)
POST /classify       → bart-large-mnli (doc type)
```

Tek port, tek Docker container. `IDocumentAiService` interface ile ASP.NET Core'dan çağrılır.

---

## İlişkili Dosyalar

- `docs/VISION.md` — hangi modüller planlı
- `plans/27-documents-ai-roadmap.md` — Documents + AI planı
- `plans/34-sop-prosedur-yonetimi.md` — SOP planı
- `plans/36-workflow-designer-onay-akislari.md` — Workflow planı
- `TODO.md` — aktif sprint
- `.claude/rules/architecture.md` — stack kısıtları (ADR-014: vanilla JS + Alpine, no React/Vue)
