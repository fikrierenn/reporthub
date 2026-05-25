# Plan 53 — Mosaik vNext Unified Master Plan

**Durum:** ✅ ONAYLANDI 2026-05-25 (CFO & Mimari Ortak Kararı)
**Tier:** 3 (Büyük Dönüşüm: BKM Kitap Operasyonel Zeka Platformu)
**Tetik:** Kullanıcı strategic input 2026-05-25 — "tümünü topla ve geniş bir plan dosyası yaz"
**Effort:** ~300-380h (12 Faz, 3-4 ay)
**Aciliyet:** 🟢 **MOSAIK KUZEY YILDIZI** — Tüm sprintleri ve vNext modüllerini orkestre eden birleşik plan

---

## 1. STRATEJİK HEDEF VE KUZEY YILDIZI

Mosaik (*ReportHub*), basit bir SQL Stored Procedure çalıştırma portalından çıkıp, **BKM Kitap operasyonunun gerçek dijital beyni (Operational Intelligence Platform)** haline dönüştürülecektir. 

Bu birleşik master plan; yeni geliştirilen 8 bağımsız modül planını (Plan 44-51), **YonetIQ** (*yonet*) projesindeki pekiştirmeli semantik öğrenme ve veri kalite mimarisini ve **sqlserver-mcp-server** projesindeki BKM'ye özel kritik finansal/operasyonel DB guardrail'lerini **tek bir 12 haftalık yol haritasında birleştirir.**

### Dört Mimari Sütun (Core Pillars)

```
                       ┌────────────────────────────────────────────────┐
                       │ Mosaik: Şirket İçi Operasyonel Zeka Platformu │
                       └───────────────────────┬────────────────────────┘
                                               │
       ┌───────────────────────┬───────────────┴───────┬────────────────────────┐
       ▼                       ▼                       ▼                        ▼
┌───────────────┐       ┌───────────────┐       ┌───────────────┐        ┌───────────────┐
│  Pillar A:   │       │  Pillar B:   │       │  Pillar C:   │        │  Pillar D:   │
│  Yerel Güvenli│       │   Zero-UI &   │       │ Arayüzsüz     │        │ BKM Jargon &  │
│    AI & RAG   │       │ Mobil Saha    │       │ Süreç Derleme │        │  Veri Kalitesi│
└───────────────┘       └───────────────┘       └───────────────┘        └───────────────┘
```

* **Pillar A (Yerel Güvenli AI & RAG):** local Qwen 2.5 3B + E5 ONNX ile sıfır bulut bağımlılığı. `IRagAccessPolicy` ile SQL seviyesinde chunk elenmesi (Plan 44) ve dynamic differential privacy (Plan 51) ile veri bağışıklığı.
* **Pillar B (Zero-UI & Mobil Saha):** PWA manifest (Plan 46) + ZXing barkod okuyucu widget + local Whisper TR voice-to-task pipeline (Plan 49).
* **Pillar C (Arayüzsüz Süreç Derleme):** Excel-to-Process ingestion (Plan 45) + Executable SOP doğal dille süreç derleme (Plan 48) + Auto-Tuning süreç optimizasyonu (Plan 47).
* **Pillar D (BKM Jargon & Veri Kalitesi):** YonetIQ pekiştirmeli semantik öğrenme (SemanticEnricher) + BKM SQL Server standardizasyon kuralları + dynamic Data Quality Engine.

---

## 2. UYARLANACAK DIŞ VARLIKLAR ENVENTERİ

### 2.1 YonetIQ (*D:\Dev\yonet*) Projesinden Alınacaklar
1. **Data Quality Engine (Veri Kalite Kontrolü):** `sp_DataQuality_Check.sql` ve C# `DataQualityService` / `DataQualityReport` modelini adapte ederek `/Admin?tab=datasources` altında BKM veritabanlarının null, format ve mükerrerlik anomalilerini 0-100 puanlık bir karneyle görselleştireceğiz.
2. **Çok Sinyalli Pekiştirmeli Öğrenme (Multi-Signal Feedback Loop):** Thumbs up/down, sessiz kullanım, SQL elle düzeltme mesafesi (Levenshtein) sinyallerini toplayan `LearningSignalService.cs` ve `SemanticDiscoveryService.cs` sınıflarını AI Analiz modülümüze entegre edeceğiz. Güven puanı `≥ 8.0` olan başarılı pattern'leri **Golden Memory** olarak local Qwen modeline dynamic few-shot olarak enjekte edeceğiz.
3. **IIS Intranet Deploy Script (`publish.ps1`):** Mosaik'in local sunucuda kesintisiz build, app pool stop/start ve env koruma publish işlemlerini 5 saniyede tamamlayan PowerShell scriptini projemize aktaracağız.

### 2.2 sqlserver-mcp-server (*D:\Dev\sqlserver-mcp-server*) Projesinden Alınacaklar
1. **BKM Finansal & SQL Guardrail'leri:** BKM CFO'sunun (Fikri Bey) raporlarda sıklıkla karşılaştığı ve yapay zekanın kesinlikle uyması gereken kuralları PromptEngine'e dynamic markdown olarak enjekte edeceğiz:
   - EncoreMerkez compat 110 kısıtları (`STRING_AGG`, `TRIM`, `IIF` yasak).
   - `DocumentsTypeId = 3` (İade) belgelerinde `SUM` ve `AVG` hesaplarında negatif sign (-) / iade hariç tutma mantığı.
   - `SalesProducts` tablosunda `WHERE IsValid = 1` filtresinin zorunluluğu.
   - Join'lerde `COLLATE Turkish_CI_AS` zorunluluğu.
2. **BKM-Branded Excel Generation (Styled Excel):** Python scriptlerindeki kurumsal renk paleti şablonunu ClosedXML/EPPlus C# export motorumuza entegre edeceğiz (BKM Brand Blue `#0F172A` başlıklar, otomatik genişlik, para birimi formatları).
3. **BKM Kitap Trend Endeksi Modülü (Plan 52):** Adet yerine oransal baz (Baz hafta=100) trend endeksi, 13 haftalık hareketli ortalama, yeni çıkanlar için "Çıkış İvmesi Skoru" hesaplayan `analytics` şemasını ve `analytics.sp_haftalik_etl` stored procedure'ünü kuracağız.

---

## 3. 12 HAFTALIK BİRLEŞİK YOL HARİTASI

Tüm modüller, bağımlılık önceliklerine göre (SMTP -> Form Builder -> PWA -> Workflow -> Process Runtime -> AI Entegrasyonları) haftalık sprintlere bölünmüştür:

```
Hafta 1-2:   [Faz 0] Foundation, IIS Deploy Script, Data Quality Engine, Plan 32 SMTP.
Hafta 3-4:   [Faz 1] Plan 41 Form Builder (SurveyJS) + Plan 45 Excel-to-Process AI Ingestion.
Hafta 5-6:   [Faz 2] Plan 44 RAG Permission Guard + Plan 46 PWA Offline-First & ZXing Barcode.
Hafta 7-8:   [Faz 3] Plan 36 Workflow Designer (Stateless) + Plan 47 Auto-Tuning Process Advisor.
Hafta 9-10:  [Faz 4] Plan 42 Process Execution Runtime (Gotenberg & PdfSharp) + Plan 51 Differential Privacy.
Hafta 11-12: [Faz 5] Plan 48 Executable SOP (Prosedürden koda) + Plan 49 Zero-UI Voice Whisper Engine.
Hafta 13+:   [Faz 6] Plan 50 Shadow Org Graph + Plan 52 BKM Kitap Trend Endeksi Modülü.
```

---

## 4. DETAYLI FAZ PLANI VE ADIMLAR

### Faz 0 — Altyapı, Deploy & Veri Kalitesi (Hafta 1-2)
* **Adım 0.1 (SMTP):** Plan 32 scheduled reports & SMTP Caller altyapısını tamamla, `IEmailService` DI entegrasyonunu doğrula.
* **Adım 0.2 (Deploy):** YonetIQ `publish.ps1` betiğini BKM local IIS yapımıza göre Mosaik `scripts/iis-publish.ps1` olarak uyarla ve test et.
* **Adım 0.3 (SQL Rules):** BKM veritabanı ciro, iade ve collation guardrail'lerini `MarkdownSkillCatalog.cs` ve `ExtractionPrompts.cs` prompt dosyalarına enjekte et.
* **Adım 0.4 (Data Quality):** `74_DataQualityCheckProcedure.sql` migration dosyasını oluştur. `DataQualityService.cs` ile `/Admin?tab=datasources` altında BKM DB karne gösterimini canlıya al.

### Faz 1 — Form Çekirdeği ve Excel Giriş Kapısı (Hafta 3-4)
* **Adım 1.1 (Form v1):** Plan 41 Form Builder Faz 0-3 bitir. SurveyJS entegrasyonu, `FormDefinition` ve `FormSubmission` veritabanı tablolarını (Migration 70) oluştur.
* **Adım 1.2 (Excel Upload):** Plan 45 Excel Ingestion Faz 1-2 tamamla. ClosedXML wrapper'ı yaz. Drag-drop upload sayfasını `/Forms/ExcelImport` altında kur.
* **Adım 1.3 (AI Schema & Import):** Qwen ve Heuristic motor entegrasyonunu tamamla. SQL schema generator (`CREATE TABLE FormResponse_{slug}`) admin onaylı DDL editörünü ve Hangfire historical row batch importer'ını kur.

### Faz 2 — Güvenlik Bariyeri & Mobil Saha Entegrasyonu (Hafta 5-6)
* **Adım 2.1 (RAG Guard):** Plan 44 RAG Permission Guard Faz 1-4 bitir. `SopChunks` tablosuna `SecurityLevel`, `AllowedRoleIds` vb. kolonları ekle (Migration 73). `IRagAccessPolicy` interface'ini Mosaik.Core altında kur. SQL-side retrieval filtrelerini enjekte et.
* **Adım 2.2 (PWA):** Plan 46 PWA Faz 1-2 bitir. `manifest.json` ve `sw.js` (Workbox 7) cache stratejilerini (SOP read StaleWhileRevalidate, Form submission BackgroundSync IndexedDB queue) kur.
* **Adım 2.3 (Barcode Scan):** ZXing-js CDN/NuGet entegrasyonunu tamamla. SurveyJS custom "Barcode" field widget'ını ve HTTPS secure context kamera aktivasyonunu yap.

### Faz 3 — Workflow & Proaktif Optimizasyon (Hafta 7-8)
* **Adım 3.1 (Workflow):** Plan 36 Workflow Designer Faz A-C tamamla. `Stateless 5.20.1` motor entegrasyonu, sequential workflow database loglama ve dynamic designer UI'ı kur.
* **Adım 3.2 (Inbox):** Plan 37 Unified Action Inbox Faz 0-1 ile `IInboxProvider` ve `/Inbox` keyboard-friendly ekranlarını tamamla.
* **Adım 3.3 (Auto-Tuning):** Plan 47 Auto-Tuning Faz 1-4 tamamla. Daily Hangfire `ProcessAnalyzerJob` ve IPatternDetector sınıflarını (Bypass, Bottleneck) yaz. Qwen narator ile admin inbox'ına süreç optimizasyon önerilerini düşür. Immutable şablon versiyon güncelleme akışını kur.

### Faz 4 — Runtime & Veri Bağışıklığı (Hafta 9-10)
* **Adım 4.1 (Runtime):** Plan 42 Process Execution Runtime Faz 0-3 bitir. `ProcessInstance` polymorphic timeline ve 6 aspect veri ilişkilerini (Plan 38 EntityRelations reuse) kur.
* **Adım 4.2 (PDF Render):** Gotenberg Docker container ve `PdfSharp-MigraDoc` $0 maliyetli C# PDF/Word çıktı motorunu (ADR-021 rev 2) tamamla.
* **Adım 4.3 (Privacy Filter):** Plan 51 Differential Privacy Reporting Faz 1-4 bitir. `StoredProcedureExecutor` çıktısını RAM'de durdurup claims tabanlı dynamic masking, hashing ve MathNet.Numerics Laplace noise diferansiyel gizlilik mekanizmasını entegre et.

### Faz 5 — Süreç Derleme & Arayüzsüz Akış (Hafta 11-12)
* **Adım 5.1 (Executable SOP):** Plan 48 Executable SOP Faz 1-4 bitir. Doğal dille yazılan SOP metinlerini local Qwen ile JSON Intermediate Representation (IR) şemasına derleyen `SopSemanticParser.cs` ve Hangfire scheduler entegrasyonlarını tamamla.
* **Adım 5.2 (Zero-UI):** Plan 49 Zero-UI Operations Faz 1-4 bitir. Local ASR `Whisper.NET` (whisper-tiny-tr ONNX) motorunu süreç içi entegre et. Ses kaydını transient RAM bellekten anında silen (workplace surveillance korumalı) gizlilik proxy'sini ve push-to-talk widget'ını kur.

### Faz 6 — Gölge Hiyerarşi & Trend Endeksi (Hafta 13+)
* **Adım 6.1 (Shadow Graph):** Plan 50 Shadow Org Graph Faz 1-3 tamamla. `WorkflowInstanceLogs` ve `@mention` log ağırlıklarını Cytoscape.js force-directed grafik arayüzünde visual kılarak observed vs official organizasyon sapma raporlarını sun.
* **Adım 6.2 (Trend Index):** Plan 52 BKM Trend Endeksi modülünü kur. `analytics` şemasını Dim/Fact tablolarıyla oluştur. Pazartesi 03:00 `analytics.sp_haftalik_etl` Hangfire job'ını ve trend line-chart kamuya açık / şirket içi dashboard görünümlerini tamamla.

---

## 5. MİMARİ VE GÜVENLİK DEĞERLENDİRME ÇERÇEVESİ

* **Güvenlik Çekirdeği (Security Core):** Tüm dynamic tablo ve dynamic form response tabloları DDL Injection'a karşı aşırı katı sanitasyon regex'lerinden (`cleanSlug = Regex.Replace(..., @"[^a-z0-9_]")`) geçecektir.
* **Biometrik Ses Güvenliği:** Whisper ses notları sunucu diskine yazılmayacak, transkrip sonrası anında RAM'den yok edilecektir.
* **Diferansiyel Gizlilik (Laplace Noise):** Analistler ve düşük yetkili kullanıcılar için `ε = 1.0` Laplace mekanizmasıyla finansal range'ler gösterilecek, ham ciro rakamlarının repeated query'lerle sızmasını engellemek için cache ve rate limiting bypass korumaları devrede olacaktır.
* **Immutable Süreç Koruması:** Aktif onay akışları in-flight esnasında asla modifiye edilmeyecek, her optimizasyon şablon sürüm kontrolü (V1.0 -> V1.1) ve "Staging / Pending Activation" onay adımlarıyla devreye sokulacaktır.
