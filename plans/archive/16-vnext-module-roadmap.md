# Plan 16 — vNext Modül Yol Haritası (D:/Dev keşif sonrası)

**Durum:** Aktif — **2026-05-14 [VISION.md](../docs/VISION.md) ile revize edildi.** Bu plan **port stratejisi** (D:/Dev kaynak haritası), VISION ise **değer önceliği**. İkisi birlikte okunur.
**Tier:** 3 (kullanıcı-görünür, 6+ modül, schema/UI/auth değişiklikleri)
**Tarih:** 2026-05-07 (orijinal taslak), 2026-05-14 (VISION uyumu)
**Bağlam:** D:/Dev altında 8 paralel subagent keşfi tamamlandı. Bu plan, Mosaik vNext modüllerinin **port stratejisini ve sırasını** kayıt altına alır. Her bir modül kendi alt-planına (Plan 17, 18, ...) sahip olacak; bu plan **roadmap + kaynak haritası**.

---

## ⚠️ VISION uyum notu (2026-05-14)

[`docs/VISION.md`](../docs/VISION.md) **vNext kalbi** olarak **3 modüllük üçlü** belirledi (~6-9 hafta):

| Yeni Tier 3 plan adayı | Effort | VISION sıra |
|---|---|---|
| Plan 34 — SOP / Prosedür Yönetimi | 2-3 hafta | #1 (en hızlı kazanım) |
| Plan 35 — Comment / Mention | 1-2 hafta + entegrasyon | #2 |
| Plan 36 — Workflow Designer | 3-4 hafta + entegrasyon | #4 (en yüksek leverage) |

Plan 16'daki **D:/Dev keşif çıktıları** (YonetIQ ApprovalService, DikkatIQ AI core, Tower KPI entity vs.) bu üçlüye **port kaynağı** olarak hizmet eder. Örneğin Workflow Designer için YonetIQ `ApprovalRequest+ApprovalStep` pattern + Plan 17 Faz H notification altyapısı + IWorkflow Core abstraction = referans implementation.

### VISION'da "yapılmayacak" olarak işaretlenenler

Plan 16'da geçen ama [VISION §4](../docs/VISION.md#4-yok-olan-vnext-modülleri--değer-sıralı) iptal eden modüller:

- **KPI / OKR ayrı modülü** — Reports modülü zaten dashboard üretiyor, Mosaik ölçeğinde (200-300 kişi) OKR culture fit yok. Onun yerine 2-3 KPI dashboard template'i Reports altında üret.
- **Mesajlaşma (real-time SignalR)** — Slack/Teams varken marjinal. Comment/Mention (Plan 35) yeterli.
- **Duyuru ayrı modülü** — Tamim'e `Type` enum (Formal vs Informal) eklemek 2-3 gün, ayrı modül 2-3 hafta. Yeni modül yapma.
- **Form/Anket Builder kendi üretim** — open-source integrate (LimeSurvey / Formbricks self-hosted) önerildi.

Bu maddeleri Plan 16'da görürsen → **VISION kararıyla iptal**, port stratejisi geçersiz.

### Plan 16 alt-planlarında durum

| Alt-plan | Durum | VISION uyum |
|---|---|---|
| Plan 17 (Tamim) | ✅ Tamamlandı | Uyumlu (Tamim altyapısı SOP'a reuse) |
| Plan 18 (HR Sync) | ⏸ Bekliyor (Plan 18B) | "Paralel ikincil" — vNext kalbi sonrası |
| Plan 19 (Documents v2) | ⚠ Plan 27 ile çakışıyor | VISION §3 — birleştir veya birini reddet |
| Plan 20 (OrgChart) | ✅ Tamamlandı | Uyumlu |
| Plan 21 (Lookup table) | Bekliyor | OK (cross-cutting altyapı) |
| Plan 22 (Holidays/dates) | Bekliyor | Calendar entegrasyonu açıklığı, VISION §3 işaretli |
| Plan 23 (Sidebar layout) | ✅ Tamamlandı | Uyumlu |
| Plan 24 (sqlcli web UI) | Bekliyor | Düşük öncelik |
| Plan 25 (Sözleşme) | ✅ Faz A+B+C kısmen | VISION'da olgun ürün (LegalTech kalbi) |
| Plan 25.1 (Contract security) | Bekliyor | OK |
| Plan 26 (OCR fallback) | Bekliyor | OK (AI altyapı) |
| Plan 27 (Documents AI) | ⚠ Plan 19 ile çakışıyor | VISION §3 — birleştir veya birini reddet |
| Plan 32 (Scheduled Reports + Email) | ⏸ 6 açık soru | "Paralel ikincil + Comment/Mention'dan ÖNCE" |

---

---

## 1. Problem

Mosaik şu an %80 olgun (rapor + dashboard + user/role + audit + brand + modules). Şirket içi portal vizyonu için 7+ vNext modülü gerekli: tamim, HR sync, doküman, takvim/toplantı, KPI/forecast, onay/e-imza, form/anket. **Karar:** sıfırdan yazmak yerine D:/Dev'deki 60+ yarım projenin hangileri reuse edilebilir? Hangi sırayla?

## 2. Scope

**Bu plan kapsamında:**
- 8 keşif raporunun sentezi
- Modül-modül port stratejisi (full-port / referans-al / skip)
- Plan 17+ alt-plan başlıkları + sıralaması
- D:/Dev kaynak klasörlerin Mosaik-içi karşılığı

**Bu plan kapsamı dışında (alt-planlara bırakılır):**
- Her modülün entity şeması detayı (alt-plan içinde)
- Migration script yazımı
- Implementation

---

## 3. Keşif Sentezi — 8 Subagent Bulguları

### 3.0 DikkatIQ (`D:/Dev/DikkatIQ/`) — AI Core + Yeni Modül (3.5/5, ama AI 5/5)

- **Stack:** ASP.NET Core 10 Razor Pages + EF Core + Hangfire + 3-layer LLM fallback (Ollama → Gemini → Claude). Anthropic.SDK 5.10.0 + Mscc.GenerativeAI 3.1.0. PdfPig + OCRmyPDF (Türkçe).
- **Domain:** AI-powered **sözleşme & yükümlülük yönetimi** — PDF upload → AI extract → suggestion → human approval → Contract/Obligation/Event records. Multi-company (CompanyId global filter).
- **13 entity:** Company, Users, UserCompany, Contracts, ContractEvents, Obligations, RecurrenceRules, Documents, AiExtractions, AiSuggestions, Notifications, AuditLogs, ComplianceTemplates.
- **Karar:** **İki ayrı kullanım:**
  1. **AI layer'ı `Mosaik.Core.AI` shared kütüphanesine port et (Plan 16.5'a EKLE)** — `FallbackLlmService`, `OllamaLlmService`, `GeminiLlmService`, `ClaudeLlmService`, `PdfPigTextExtractor`, ExtractionPrompts, `AiExtraction`+`AiSuggestion` entity pattern. Tüm vNext modüllere değer katar (Tamim AI özet, Doküman auto-tag, KPI AI insight, Audit risk extract, Calendar NLP).
  2. **Plan 25 — Sözleşme/Yükümlülük modülü (YENİ)** — DikkatIQ'in domain'i Mosaik'te yok. Legal/finance için kullanışlı. Sıralamada en sonda (24'ten sonra).
- **Önemli:** AI **asla doğrudan yazmaz**, hep suggestion → user review → approval. Bu pattern Plan 16.5'ta `IAiSuggestion<TEntity>` interface'i olarak çıkarılabilir.

### 3.1 YonetIQ (`D:/Dev/yonet/`) — AI Core + Pattern Treasury (4.5/5 derin keşif)

**İlk keşif yüzeyseldi — 2026-05-07 derin keşif (code-explorer) ile aşağıdaki 3 büyük katman ortaya çıktı:**

- **Stack:** .NET 10 Blazor Server + Dapper (EF değil) + SQL Server. AI-heavy ama **6 katmanlı orkestre edilmiş** mimari (40 dağınık servis değil).
- **6 AI katmanı:**
  1. **AiProviderService** — Gemini → OpenAI → Anthropic fallback, SSE streaming, model chain (6 Gemini), `IHttpClientFactory` refactor önerisi var
  2. **SkillRegistry** — DB override, `SkillPersistenceService` ile admin panelden temperature/prompt değiştir (deploy gerekmez)
  3. **PromptEngine** — `.md` disk dosyaları + FileSystemWatcher hot-reload + `_system_rules.md` global prefix + `{{variable}}` template, 70+ prompt
  4. **AiOrchestrationService** — MetricEngine→NLtoSQL→SkillRouting→Executor + self-correction (1 retry) + name resolution (posMagaza/UrunBilgi LIKE)
  5. **MetricEngineService** — DB'de saklanan `MetricDefinitions+DimensionDefinitions+JoinDefinitions` üzerinden **deterministik NL→SQL**. LLM sadece intent parse, SQL kod tarafında. Guardrails: DROP/DELETE blacklist, 10K satır limit, slow query 5s log. Başarılı sorgu `AiPatterns` golden_query'e otomatik kayıt.
  6. **LearningSignalService** — ThumbsUp +3, Decay -0.3, Levenshtein similarity, threshold-based auto-approve (≥8.0 oto, 4.0-7.9 admin, <4.0 gürültü). Vektör DB gerekmez.

- **Domain pattern hazineleri:**
  - **`ServiceResult<T>` + `BaseService.ExecuteServiceAsync`** — DB error handling, exception sızdırma riskini azaltıyor (security-principles #7 ile uyumlu — ama `ex.Message` BaseService'te expose ediliyor, port sırasında düzeltilmeli)
  - **Lookup pattern** (`Group + Value` ikili sistem + `IsActive` soft-delete) — departman/pozisyon/şube/durum/öncelik tek tabloda
  - **ApprovalRequest + ApprovalStep** — `StepOrder` bazlı sıralı onay, `MIN(StepOrder)` ile sıradaki onayçı, `EntityType+EntityId` generic
  - **TaskItem.SourceEntityType + SourceEntityId** — cross-modül izlenebilirlik (toplantı kararı → görev)
  - **ConversationContextService** — SQL tabanlı 6-tur sliding window, vektör DB gerekmez
  - **MetaSkill + SkillStudio** — AI'ın kendi skill'ini yazması (`meta.analyzer` + `meta.writer` + `meta.refiner`) — şimdilik Tier 3

- **Karar (revize):** **Hibrit port — domain pattern'leri Plan 16.5 + 18 + 23'e, AI Core Plan 16.5'a, MetricEngine Plan 26'ya.** Blazor UI skip, Dapper→EF/ADO.NET dönüşümü her port'ta zorunlu.

- **DikkatIQ ile ilişki:** YonetIQ AI Core daha olgun (streaming, hot-reload, self-correction). DikkatIQ'in PDF extraction + suggestion-approval pattern'i bunun **üstüne** kurulur. DikkatIQ `FallbackLlmService` → YonetIQ `AiProviderService` ile değiştirilir. Plan 16.5'ta tek AI Core.

- **Kritik dosyalar (port öncelikli):**
  - `Data/Infrastructure/ServiceResult.cs` (44 satır)
  - `Data/Services/BaseService.cs` (80 satır, ex.Message fix gerekli)
  - `Data/Models/Lookup.cs` + `Data/Infrastructure/LookupConstants.cs` + `Data/Services/LookupService.cs`
  - `Data/Models/ApprovalRequest.cs` + `Data/Services/ApprovalService.cs`
  - `Data/Models/TaskItem.cs` (SourceEntity FK pattern)
  - `Data/Services/AI/AiProviderService.cs` + `PromptEngine.cs` + `AiOrchestrationService.cs` + `ConversationContextService.cs` + `LearningSignalService.cs`
  - `Data/Services/AI/MetricEngineService.cs` + `MetricIntentParser.cs` + `MetricSqlBuilder.cs` + `JoinGraph.cs`
  - `YONETIQ_UNIFIED_MASTER.md` + `YONETIQ_LEARNING_SYSTEM.md` + `YONETIQ_SCHEMA_DISCOVERY.md` (referans dökümanlar)

### 3.2 Tower (`D:/Dev/tower/`) — Entity Port (4/5)

- **Stack:** Next.js 14 + Shadcn/Tailwind + raw MSSQL (Prisma yok).
- **Modüller:** KPI, Dashboard (widget grid), Tasks, Notes, Finance/HR/Inventory/IT/Stores dashboard'ları.
- **AI hooks:** KPI metadata fields (`aiBusinessQuestion`, `aiNormalBehaviorHint`, `aiRiskHint`). AI_Logs tablosu. **OpenAI stub** — gerçek LLM kodu yok, sadece interface.
- **Karar:** **Entity şemasını + business logic'i port et, UI'yı yeniden çiz.**
  - SQL şeması (`02-create-tables.sql`) → Mosaik EF migration
  - `KPIService.ts` → C# `KpiService` (status calc + threshold)
  - Dashboard widget grid modeli → Mosaik dashboard config'e ek widget tipi (`kpi`)
  - **Önemli:** Mosaik zaten dashboard widget sistemine sahip. Tower'ın grid sistemini ezmeye gerek yok; KPI **yeni widget tipi** olarak eklenebilir.

### 3.3 BkmArgus (`D:/Dev/BkmArgus/`) — Parça Port (3.5/5)

- **Stack:** .NET 10 Razor Pages + Dapper (SP-first) + 4 katmanlı AI (LM Rules → Semantic Memory → LLM Chain → Agent Pipeline).
- **Modüller:** Account, Audit (alan denetim + foto), Risk (11-flag), DOF (Findings/Actions kanban), AI queue, Ref (8 tab), Yonetim, Correlation, Urun, McpServer.
- **Karar:** **Audit + Risk + DOF entity'lerini port et (5/5).** UI ve auth'u skip.
  - `audit.Audits` + `AuditItems` + `AuditResults` + `AuditPhotos` → Mosaik EF
  - `ref.RiskParam` + `RiskSkorAgirlik` → Mosaik
  - `dof.Findings` + `Actions` (kanban state machine) → Mosaik workflow engine
  - `LmRules.cs` → `RiskScoringService` (rule engine pattern)
  - **Skip:** Custom auth (Identity kullan), Razor Pages → MVC view, ERP `src.*` views (DerinSISBkm dependency)

### 3.4 Tamim (`D:/Dev/tamim/`) — Direkt Port (5/5 — birebir)

- **Stack:** Next.js 15 + Prisma + SQL Server. Auth: NextAuth + bcrypt.
- **Entity:** `Tamim`, `TamimDurum`, `TamimBlok`, `Bildirim` (mesaj history). **Eksik:** `ReadStatus` (kim okudu).
- **UI:** /tamimler, /tamimler/[id], /tamimler/olustur, /ayarlar/tanimlar/etiketler.
- **Karar:** **Direkt port.** Entity 1:1 taşınabilir, Razor'a 5 view yazılır.
  - 8-12 saat solo dev tahmin
  - **Eklenecek:** `TamimReadLog` tablosu (Tamim'de yok)
  - **Plan 17 adayı**

### 3.5 HR Sync (`hrreport/` + `hrrepo/` + `hrreport_electron/`) — Karma (Plan 16/HR)

- **En olgun:** `hrreport/` (Node.js Express, 14 backend dosyası, bcrypt + JWT).
- **`hrrepo/`** — .NET 8 Blazor + EF Core, `GetCurrentWindowsUserAsync()` AD entegrasyonu var.
- **User alanları (mevcut):** Username, FullName, Email, Role, IsActive, LastLoginAt, Theme, Language, Notifications, MaxConnections, Department, CreatedAt, UpdatedAt.
- **EKSIK alanlar (Plan 16 ekleyecek):** Phone, Position, ManagerId, SubeId.
- **Karar:** **hrrepo `GetCurrentWindowsUserAsync()` + UserService scaffold'unu port et.** Plan 16 (zaten planda) bu işi yapacak.

### 3.6 Meet (`D:/Dev/meet/`) — NLP Motoru Port (3/5)

- **Stack:** ASP.NET Core 10 Razor Pages, **in-memory mock**, EF yok.
- **Değer:** Türkçe NLP regex motoru — toplantı notlarından action/decision/blocker/question çıkarımı. AiDtos + MockAiService 600 LoC.
- **`toplanti/`** — boş iskelet, skip.
- **Karar:** **NLP motorunu çıkar, EF entity'leri ekle.** Calendar modülü iskeletinin **AI bonus özelliği** olarak entegre.

### 3.7 Form/Doc/Approval (`talep/` + `katalog/` + `esign/`)

- **`katalog/`** — Vanilla HTML+JS+IndexedDB+PDF.js+Claude API. **4/5 reuse.** Document management modülü için **full port** adayı (entity model + AI extraction pattern).
- **`esign/`** — Tailwind static site, **UI skeleton sadece**, backend yok. **2/5.** Approval modülü için **UI referans**, logic from-scratch.
- **`talep/`** — boş klasör. **0/5, skip.**
- **Karar:** Doküman modülü öncelik (Plan 19), Approval orta (Plan 20), Form en geç (yeni form builder).

### 3.9 Operax (`D:/Dev/Operax/`) — Pattern Treasury (3/5)

- **Stack:** .NET 10 Razor Pages + **Dapper** (EF değil) + ASP.NET Identity + Hangfire + NCalc 1.3.8 (formül motoru) + TailwindCSS v4. Solution: `Operax.Web` + `Operax.PrintServer` + `Operax.Cli`.
- **Domain:** WMS + Üretim + Ticari ERP/operasyon platformu (17 modül planı, M00-M10 ekran var, M11-M17 yok). **Mosaik domain'inden uzak** — port aday değil.
- **AI yok.** YonetIQ benzeri NL-to-SQL, semantic learning, prompt engine **hiçbiri yok**. NCalc sadece Parametrik BOM formülü için (ölçüye göre hammadde miktarı).
- **Build durumu:** **Şu an derlemiyor** — 19 build hatası açık (`SPRINT_0.md`). Newtonsoft.Json güvenlik açığı (GHSA-5crp-9r3c-p9vr).
- **Mosaik için 3 Tier 2 pattern (Plan 16.5'a katkı):**
  - **`DictionaryType + DictionaryValue + StatusTransition`** — soft enum + state machine. YonetIQ Lookup ile birleşince Plan 16.5 Faz B'nin temeli. Operax'ınki daha olgun (DB-driven, admin CRUD, StatusTransition matrisi).
  - **Multi-company şema standardı** (`CompanyId + CreatedAt/By + UpdatedAt/By + IsDeleted/DeletedAt/By`) — Plan 11 Faz 6 reset sonrası multi-tenant geçiş düşünülürse referans
  - **EventQueue + Idempotency pattern** — Mosaik bildirim altyapısı için tasarım referansı (Tamim gönderim, onay bildirimi, e-posta retry)
- **Karar:** Domain port etme. Plan 16.5 Faz A-B'de YonetIQ Lookup + Operax DictionaryType birleşik tasarım.

### 3.10 FrameOS (`D:/Dev/FrameOS/`) — Skip (Tier 3)

- **Stack:** Next.js 14 + Supabase (PostgreSQL + JSONB) + OpenAI Whisper + Google Generative AI + fluent-ffmpeg + yt-dlp.
- **Domain:** AI multi-modal medya analiz (video/foto/ses) — 7 modüllü plan (FramePilot, FrameVision, FrameScript, FrameAudio, FrameFlow, FrameCut, FrameStudio).
- **Implementasyon durumu:** **Sıfır kod.** Master plan + DEVELOPER_GUIDE.md kapsamlı yazılmış, `src/modules/` boş. Sadece `src/types/database.ts` ve `layout.tsx` var. Git repo bile yok.
- **Karar:** **Skip.** Mosaik domain'i değil, kod port edilemez (sıfır implementasyon), stack tamamen farklı (Next.js/TS/Supabase). Konsept değer (job state machine, provider abstraction, modül izolasyon standardı) Plan 16.5'a zaten YonetIQ'dan geliyor.

### 3.11 FLYX (`D:/Dev/FLYX/`) — Skip (Tier 3, Plan 24 için referans)

- **Stack:** Node.js 20 + TypeScript + Turborepo + Chevrotain (parser kütüphanesi) — **plan**, kod yok.
- **FSL DSL nedir:** "FLYX Script Language" — aslında Claude Code'a verip iskelet üretmek için yazılmış **prompt dokümantasyon dilidir**. Parser yok, runtime yok, UI yok.
- **Implementasyon durumu:** **Sıfır kod.** `packages/*/src/` boş, `apps/` boş. Son commit Nisan 2025, dokunulmamış.
- **Mosaik için tek değer:** FSL Quick Reference'daki **form field type sözlüğü** (Text/Number/Decimal/Boolean/Date/DateTime/Email/Enum/Relation/File/Image/Array) + **`conditional_display` pattern** (show_if/disable_if) Plan 24 Form Builder JSON schema kararı için yarım günde okunup referans alınabilir.
- **Karar:** **Skip.** Plan 24 (Form Builder) yazılırken FSL Quick Reference'a 30dk göz at, JSON schema'ya esinlen, kod port etme.

### 3.8 KPI/Finans (`butce/` + `cashflow/` + `tahmin/` + `tahminleme/`)

- **`cashflow/`** — .NET 10 Blazor + EF Core, **olgun**, multi-tenant nakit akış (gider/borç/çek/kredi/maaş). 13 tablo, 6 SP.
- **`butce/`** — Python Flask + Pandas, prototip (Excel-tabanlı satış tahmin).
- **`tahminleme/`** — Python sklearn (ML), DerinSIS bağlı.
- **`tahmin/`** — boş.
- **Karar:** **Cashflow entity'lerini + SP'leri port et.** Mosaik dashboard'una **yeni KPI widget tipi** olarak entegre — ayrı modül değil, dashboard kategorisi. Tahmin algoritmaları Python kalır, API wrapper ile çağrılır (veya ML.NET'e port — Plan 21).

---

## 4. vNext Modül Sıralaması (Plan 17+)

| # | Modül | Kaynak | Reuse | Effort | Plan |
|---|---|---|---|---|---|
| **17** | **Tamim/Sirküler** | tamim/ (direkt port) | 5/5 | 8-12h | Yeni |
| **18A** | **IK Quick Reports + Dashboard** (read-only, view-driven, hızlı kazanım) | Zirve `vw_PersonelDepartman` (3 firma UNION + 4 seviye hiyerarşi, 272 aktif) | 5/5 | 4-6h | Yeni |
| **18B** | **HR Sync + User alanları** (User provisioning, Hangfire job, AD lookup) | vw_PersonelDepartman + hrrepo/ + hrreport/ AD pattern | 4/5 | 16-24h | **Plan 18 (yeni)** |
| **19** | **Doküman Yönetimi** | katalog/ + DikkatIQ AI extraction | 4.5/5 | 16-20h | Yeni |
| **20** | **KPI/Forecast Widget** | cashflow/ (entity + SP) + Tower (KPI logic) | 4/5 | 12-16h | Yeni |
| **21** | **Audit/Risk/DOF** | BkmArgus/ (entity + scoring) | 4/5 | 24-32h | Yeni (büyük) |
| **22** | **Calendar/Toplantı** | meet/ (NLP) + yeni EF | 3/5 | 16-20h | Yeni |
| **23** | **Onay/E-imza** | esign/ (UI ref) + from-scratch logic | 2/5 | 20-24h | Yeni |
| **24** | **Form/Anket Builder** | from-scratch | 0/5 | 24-32h | En son |
| **25** | **Sözleşme/Yükümlülük (legal/finance)** | DikkatIQ/ (full domain) | 5/5 | 20-24h | Yeni |
| **26** | **NL-to-SQL / MetricEngine** | YonetIQ MetricEngine adaptasyonu | 4/5 (kavramsal) | 30-40h | Yeni (büyük) |
| **27** | **Duyurular** (sosyal/operasyonel akış) | from-scratch + tamim/ özellik referansı | 0/5 | 12-16h | Yeni |
| **28** | **İç İletişim / Mesajlaşma** | from-scratch (SignalR/realtime) | 0/5 | 24-30h | Yeni — gerçek zamanlı, Plan 27'den ayrı |
| **29** | **Talep/İstek Yönetimi** | talep/ + katalog/ referansı + Plan 23 (esign) onay zinciri | 4/5 | 16-20h | **Yeni — kullanıcı 2026-05-07 ekledi** |
| **30** | **Görev/Task Yönetimi** | YonetIQ task entity referansı + Plan 22 (toplantı kararları) cross-link | 3/5 | 20-24h | **Yeni — Plan 29 ve Plan 22 birleşim noktası** |

### Plan 29 Talep + Plan 30 Görev — entegrasyon notu (kullanıcı 2026-05-07)

**Talep → Görev dönüşümü** ortak akış:
- Kullanıcı bir talep gönderir (örn. "ofis için yeni klima")
- Onaylanan talep otomatik **göreve dönüşür** (atanan kişi, son tarih, durum takibi)
- Talep onay zinciri (Plan 23) ile bağ — onaylanan adımda Görev tetiklenir

**Toplantı kararı → Görev dönüşümü** (Plan 22 ↔ Plan 30):
- YonetIQ'da bu pattern var: toplantı sonunda alınan her karar → atanmış göreve dönüşür
- Calendar/Toplantı modülünde (Plan 22) "karar" entity'si Görev tablosuna FK üretir
- Görev kaynağı: ManualEntry / FromRequest (Plan 29) / FromMeeting (Plan 22) / FromApproval (Plan 23)

**Plan 30 Görev — Mosaik.Core seviyesinde mi?**
- Karar yarına: Görev cross-modül concept (Tamim'den de görev üretilebilir, "blok yaz" görevi vb)
- Mosaik.Core'a `ITask` abstraction yerleştirilirse her modül kendi domain'inden görev üretebilir
- Aksi halde Plan 30 ayrı modül, Plan 29/22/23 explicit ProjectReference

**Plan 17 vs Plan 27 vs Plan 28 ayrımı (kullanıcı 2026-05-08 netleştirdi):**

| Modül | Tip | Bağlayıcı | Özellikler |
|---|---|---|---|
| **Plan 17 Tamim** | Resmi günlük tamim | ✅ Yasal kanıt | Politika/prosedür/uyumluluk/karar. 17:00 cron derleme. AuditLog "tamim_okundu". Dosya ekleme + AI özet + Export PDF/Excel + Bildirim + Dashboard (Faz E-I). |
| **Plan 27 Duyurular** | Sosyal/operasyonel akış | ❌ Bağlayıcı değil | Doğum günü kutlaması, geçici hadise (klima arızası), kampanya başlangıcı, yeni personel duyurusu, etkinlik bilgilendirme. Bağlayıcı değil, ack zorunlu değil, daha kısa ömürlü. Tamim'in dosya/bildirim/export özellikleri reuse. |
| **Plan 28 İç İletişim** | Mesajlaşma / sohbet | — | Gerçek zamanlı (SignalR). WhatsApp/Slack grup yerine. Departman kanalları, doğrudan mesaj, mağaza-içi koordinasyon. Tamim'den ortak altyapı yok. |

**Kritik:** `D:/Dev/tamim/` projesinde fark yaratan özellikler (dosya ekleme, AI özet, çıktı, bildirim, dashboard) **planlanmış ama hiç implement edilmemiş**. Mosaik tarafında sıfırdan yazılacak. Sadece Prisma şeması + organizasyon dokümanları referans değer taşıyor.

**Sıralama gerekçesi:**
1. Tamim — düşük effort, yüksek görünür değer (kullanıcılar hemen kullanır)
2. **18A IK Quick Reports** — 4-6h hızlı kazanım, mevcut Mosaik rapor pipeline'ı (SP+ReportCatalog+DashboardConfig). Plan 18B sync'i beklemez. Kullanıcının `vw_PersonelDepartman` view'ı canonical, 272 aktif personel. Bağlam: `memory/project_zirve_personel_discovery.md`.
3. **18B HR Sync** — User provisioning için Hangfire + Mosaik.User ek kolonlar + Email/TC kararları. 18A'dan sonra (read-only önce, write sonra).
4. Doküman — orta effort, AI extraction zaten tasarlanmış
5. KPI Widget — dashboard motoru zaten var, **modül değil widget tipi**
6. Audit — büyük ama BkmArgus entity'leri 70% port edilebilir
7. Calendar — NLP bonus
8. Approval — pattern olgun, business logic tasarımı gerek
9. Form — en sonda, builder UI gerekli

---

## 5. Önemli Pattern Bulguları (tüm modüllere ortak)

1. **`BaseEntity` pattern (YonetIQ'dan)** — Tüm yeni vNext entity'lere uygula. Id + CreatedAt + CreatedBy + UpdatedAt.
2. **`ServiceResult<T>` (YonetIQ'dan)** — Error handling standartı.
3. **Status workflow** — Tamim/Approval/DOF/Form hepsi state machine: Draft → InReview → Approved/Rejected → Published. Ortak `IWorkflow` interface çıkarılabilir.
4. **Attachment pattern** — Tamim, Doküman, Approval, Form, Audit hepsi attachment kullanır. Tek `AttachmentService` + ortak tablo.
5. **ReadLog pattern** — Tamim, Duyuru, Doküman okundu bilgisi. Tek `ReadLog<TEntity>` polymorphic tablo.

**Kararı:** Plan 17 başlamadan önce **`Mosaik.Core/` shared kit** çıkar. **2026-05-07 derin keşif sonrası genişletilmiş içerik:**

**Plan 16.5 — `Mosaik.Core` (shared kit) içeriği:**

**Domain primitives** (3+ modül paylaşımlı):
- `BaseEntity` (Id + CreatedAt + CreatedBy + UpdatedAt + UpdatedBy) — YonetIQ pattern
- `ServiceResult<T>` — YonetIQ `ServiceResult.cs:44 satır` (port + `ex.Message` fix)
- `BaseService.ExecuteAsync` — DB error handling (sec-principles #7 uyumlu generic mesaj)
- `Lookup` + `LookupConstants` + `LookupService` — YonetIQ pattern (HR, Brand, Module, Status için soft enum)
- `IWorkflow<TStatus>` — Tamim/Approval/DOF/Form ortak state machine
- `IAttachable` + `AttachmentService` — 5+ modül ortak
- `IReadLog<TEntity>` — Tamim/Duyuru/Doküman okundu kaydı
- `ISourceTraceable` — TaskItem.SourceEntityType+SourceEntityId pattern (cross-modül izlenebilirlik)

**`Mosaik.Core.AI` katmanı** (YonetIQ + DikkatIQ hibrit):
- `IAiProvider` + `AiProviderService` — YonetIQ multi-provider fallback (Gemini→OpenAI→Anthropic, SSE, model chain)
- `PromptEngine` — YonetIQ `.md` + FileSystemWatcher hot-reload + `_system_rules.md` global prefix + `{{var}}` template
- `IConversationContext` — YonetIQ SQL multi-turn (6 tur sliding, vektör DB gerekmez)
- `ILearningSignal` — YonetIQ Levenshtein + signal weights + decay (NL-to-SQL feature için)
- `IPdfTextExtractor` + `PdfPigExtractor` + OCRmyPDF fallback — DikkatIQ port (Türkçe pack)
- `AiExtraction` + `AiSuggestion` entity pattern — DikkatIQ "AI asla doğrudan yazmaz" ilkesi
- `IAiSuggestion<TEntity>` interface — suggestion-then-approval generic pattern

**Filter scope abstraction** (Plan 14 Faz C ile birleşir):
- `IUserDataScope` — `Scope` + `RequiresExplicitGrant` + `HasAccessAsync(userId, value)` — vNext modüller (`tamimRead`, `documentAccess` vs.) bunu implement eder

AI Core'u: Plan 17 (Tamim AI özet), Plan 19 (Doküman auto-tag), Plan 21 (Audit risk extract), Plan 22 (Calendar NLP), Plan 25 (Sözleşme), Plan 26 (MetricEngine NL-to-SQL) hepsi paylaşır.

---

## 6. Alternatifler (5 Lens)

- 🔴 **Contrarian:** "vNext modülleri ekleme, mevcut rapor sistemini olgunlaştır." Geçerli — ama kullanıcı vizyonu portal, sadece rapor değil.
- 🔵 **First Principles:** "Hangi modül en çok değer üretir?" Tamim + HR + Doküman top 3 (günlük kullanım frekansı yüksek).
- 🟢 **Expansionist:** "Tüm modülleri tek monorepo'da değil, plugin olarak yaz." Plan 12 module infrastructure tam buna izin veriyor — her modül `IModule` implement.
- ⚪ **Outsider:** "8 modül 1 dev için çok. 6 ay sürer." Doğru — bu plan **sıralama**, hepsi aynı sprint'te değil.
- 🟡 **Executor:** "Pazartesi ne yaparız?" Plan 17 (Tamim) başla, çünkü en hızlı sonuç + en az risk.

---

## 7. Riskler

- **Scope creep:** 8 modül = 6+ ay iş. Mosaik'in mevcut kullanıcıları öncelik kaybedebilir. **Mitigation:** Her modül için 2 hafta hard-cap.
- **Auth uyumsuzluğu:** Kaynak klasörlerin auth'ları farklı. Mosaik Identity'ye standartlaşma maliyeti her modülde. **Mitigation:** Plan 16.5 shared kit'te `IUserContext` interface.
- **DB connection bağımlılığı:** BkmArgus DerinSISBkm bağımlı. Cashflow multi-tenant. Mosaik tek DB. **Mitigation:** Modül-bazlı `DbConnection` registry (Plan 12 datasource sistemi zaten var).

---

## 8. Done Criteria (bu plan)

- [ ] Kullanıcı sıralamayı onaylar (veya değiştirir)
- [ ] Plan 17 (Tamim) ayrı dosya olarak yazılır
- [ ] Plan 16.5 (shared kit) ayrı dosya olarak yazılır
- [ ] TODO.md "BIRLESIK ONCELIK SIRASI" bölümüne vNext modül sırası eklenir
- [ ] Memory'de `project_vnext_module_roadmap.md` referansı oluşur

---

## 9. Rollback

Plan onaylanmazsa: bu dosya `plans/archive/`'a taşınır, TODO'ya "vNext keşif iptal" notu düşülür. Mevcut Plan 14 (filter prod-readiness) yola devam eder.

---

## 10. Sonraki Adım

**Kullanıcıya sunulacak karar noktaları:**

1. Bu sıralama (Tamim → HR → Doküman → KPI → Audit → Calendar → Approval → Form) onay?
2. Plan 16.5 (shared kit) önce mi yoksa Plan 17 (Tamim) içinde mi yapılsın?
3. Plan 14 Faz A (audit log fix, ~60 dk) bu modüllerden önce mi sonra mı?
4. Kullanıcının asıl sürpriz beklediği modül var mı (BkmArgus risk, KPI?)


---
> **SUPERSEDED 2026-06-29 → Plan 54** (modül-modül seri + denetim kapısı, kapsam-disiplinli). Scope-creep (49/50/51) CUT + rakip table-stakes eklendi. git history korur.
