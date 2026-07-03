# Mosaik — TODO

Bu dosya AKTIF işleri ve backlog'u takip eder. Tamamlanmış işler arşiv bölümünde, detay için `docs/journal/` ve `plans/archive/`.

---

## AKTIF (öncelik sırası)

### EN ÜST ÖNCELİK (2026-05-15 modül tamamlama + vNext yön kararı)

**Yön belgesi:** [`docs/VISION.md`](docs/VISION.md) — Mosaik özellik vizyonu + 6-9 haftalık vNext kalbi.
**Aktif meta-plan:** [`plans/33-modul-tamamlama-roadmap.md`](plans/33-modul-tamamlama-roadmap.md) — SOP öncesi zemini temizleme (4 faz, ~85-120 saat).

---

### 🔝 Görev Tanımları modülü (`Mosaik.Modules.GorevTanimlari`)
İçerik/karar repo: `D:\Dev\gorevtanimlari\bkm` (DECISIONS A/G/H/**I**). **Org işi öncesi ZORUNLU:** `.claude/rules/gorev-org-yapisi.md` + bkm DECISIONS.md oku. Canon: OrgPositions = bizim tasarım omurga (57), Zirve maps-in.
- [x] **T-16** ✅ 2026-07-02 — birim test + canonical class + ViewModels.
- [x] **Plan 55 Faz 1-5** ✅ 2026-07-03 (`035f467`/`70d9bd1`/`a4859f3`) — org.json → OrgPositions omurga import + `/GorevTanimlari/Sema` (bkm index.html chart birebir, DB-driven, pan/zoom/detay+KPI + admin sağ-tık düzenleme). Doküman-editör bu ekranda (sağ-tık görev tanımı+KPI → GorevVersions yeni versiyon). Full scan (3 agent, 1 CRIT XSS + 5 fix), 706/706 test, preview E2E.
- [ ] **Plan 55 Faz 6 kapanış** — sidebar Şema linki + eski /OrgChart redirect kararı + ADR + plan arşiv.

---

### 🔝 YARIN BAŞLANGIÇ — 2026-05-28
- [ ] **RoslynNavigator MCP smoke** — Claude Code restart sonrası `mcp__cwm-roslyn-navigator__*` aktif. 3 query: `find_callers AiSummaryProvider.CallLocalAsync` + `detect_antipatterns Mosaik` + `find_dead_code Mosaik`. Token kazancı ölç + Kaspersky AV çatışma kontrolü. Detay: `memory/reference_roslyn_navigator_mcp.md`, journal `docs/journal/2026-05-27.md`.
- [ ] **Yerleşik AI commit-split** — 6 uncommitted dosya 3 mantıksal commit'e (feat ai local + docs rules cherry-pick + docs claude referans).

---

#### Plan 33 Faz 1 — Plan stale + Kritik bugfix (~7-10 saat) ✅ **BAŞLADI 2026-05-15**

- [x] **R-01..R-07** — 7 plan stale temizlik (4 archive: 16.6, 17, 23, 26 + 4 durum güncel: 21, 25, 25.1, 27) ✅ 2026-05-15
- [x] **B-01** AiController.cs:446 CreatedObligationId data integrity bug ✅ 2026-05-15 — pair list pattern, ikinci SaveChanges ile cross-link
- [x] **B-02** Review.cshtml iframe src güvenlik fix ✅ 2026-05-15 — Url.Action Download endpoint
- [x] **B-03** DocumentsController App_Data + Download endpoint ✅ 2026-05-15 — wwwroot bypass kapatıldı, path traversal guard eklendi
- [x] **B-04** WizardExtractionService Tesseract OCR fallback ✅ 2026-05-15 — taranmış PDF artık crash etmez, OCR pipeline çalışır

Build: 0 hata 0 uyarı. Test: **337/337 geçti** (test-discipline.md kanıtlandı).

---

#### Plan 33 Faz 2 — Modül tamamlama (~37-50 saat) ⏳ SIRADA

- [x] **C-01** DailyReminderJob ✅ 2026-05-15 — `Services/DailyReminderJob.cs` + Program.cs cron 09:00, daysLeft sign fix 2026-05-18
- [x] **C-02** Plan 22 Holidays ✅ 2026-05-17 — entity + migration 58/59 + vw_CalendarUnified (SQL fix 2026-05-18) + CalendarController + filter chip
- [x] **C-03** TamimReminderJob ✅ 2026-05-17 — commit 37b6de0, Circular module 09:00 cron, AuditLog-based read tracking
- [x] **C-04** AI Wizard Faz 1 ✅ — WizardStart/Status endpoint + UI + hukuki risk bayrakları (commit 5a73fa3)
- [x] **C-05** ContractsController partial split ✅ (önceden yapılmıştı) — Files.cs + Ai.cs + partial keyword mevcut
- [x] **C-06** ObligationsController.Edit ✅ 2026-05-17 — commit 3a28fa0
- [x] **ADR-016** Calendar Unified Event Source ✅ 2026-05-17 — commit d32ffd3, `docs/ADR/016-calendar-unified-event-source.md`
- [x] **C-07** Plan 27 Faz A eksikleri ✅ 2026-05-19 — ContractExtractionValidator + Stage 3 hedefli alan retry (maks 3) + 7 field prompt. Build/test 349/349 yeşil. Post-review 3 agent: HasArray semantic + CT honor + ayrı catch (JsonException/HttpRequestException) + Warnings log + failedFields/skippedFields ErrorMessage'a. ContractExtractionValidator.cs + AiExtractionWorker.Stage3.cs (yeni partial) + ExtractionPrompts.GetStage3Prompt + Stage 1↔Stage 2 arası entegrasyon.
- [x] **C-08** Plan 25.1 Faz 5+6 ✅ — Task.Run→ApplicationStopping CT + inline style 0 + hook enabled (DISABLED=false)

---

#### Plan 33 Faz 3 — Mimari kararlar (~4-6 saat) ⏳

- [x] **ADR-016** Calendar Unified Event Source ✅ 2026-05-17 (commit d32ffd3, `docs/ADR/016-calendar-unified-event-source.md`)
- [x] **ADR-017** Compliance Scope Sınırı ✅ 2026-05-21 (`docs/ADR/017-compliance-scope-boundary.md`)
- [ ] **VISION update** — AI Generation karar notu (scope tut, modüllerde çıkar)

---

#### Plan 33 Faz 4 — Büyük borç (opsiyonel, ~38-52 saat)

- [ ] **D-01** Documents Plan 27 Faz C (20-28h) — versioning + FTS + metadata + permission + audit
- [x] **D-02** ✅ AI altyapı iyileştirme — audit log + token bütçe + rate limiting + LlmServiceAdapter rename. commit `2c04ebc` + `ed5a68a` (2026-05-22)
- [ ] **D-03** Vision çoklaştırma (4-6h) — Gemini + OpenAI vision fallback
- [ ] **D-04** Test coverage (6-8h) — Documents/Wizard/multi-firma integration
- [ ] **D-05** Plan 16.7 Tag Sistemi Faz A (8-10h) — bağımsız (AI önerici 16.5 Faz C bekliyor)
- [ ] **D-06** claudskills.com derin tarama (~2h) — 12 anahtar kelime, paralel WebFetch sub-agent. Bulgular `docs/SKILL_POOL.md` §3'e. Kullanıcı onayı 2026-05-21 "müsait olunca".

---

**vNext'in kalbi — Plan 33 sonrası (~6-9 hafta):**

- [ ] **[Plan 34 · SOP / Prosedür Yönetimi](plans/34-sop-prosedur-yonetimi.md)** ✅ **ONAYLANDI 2026-05-14 (varsayılan kabul)** — Plan 33 Faz 1+2 (+ önerilen D2) bittikten sonra Faz A başlar. Tamim altyapısı %80 reuse + AI Danışman MVP. 38-60 saat (2.5-3 hafta). Bağımlılık yok — bağımsız başlayabilir.
  - **Faz A** (4-6h, S-01..S-04) — Mosaik.Modules.SOP csproj + IMosaikModule iskelet + sidebar entry
  - **Faz B** (4-6h, S-05..S-08) — 5 entity (`SopDocument`, `SopVersion`, `SopReadReceipt`, `SopApprovalSubmission`, `SopAiConversation`) + migration 01-05
  - **Faz C** (12-16h, S-09..S-14) — Admin CRUD + Quill editor + 3-step onay flow (`ApprovalRequest` reuse) + departman ataması
  - **Faz D** (8-12h, S-15..S-19) — User-facing "Prosedürlerim" + SOP detay + okundu işaretleme + deadline countdown
  - **Faz E** (4-6h, S-20..S-22) — Bildirim push + Hangfire reminder job
  - **Faz F** (10-14h, S-23..S-30) — **AI Danışman MVP** — `SopAiAdvisorService` + drawer + KVKK banner + rate limit + thumbs-up/down + retention job
  - **Faz G** (4-8h, S-31, OPSİYONEL) — 27 BKM SOP migrate
- [ ] **Comment / Mention sistemi** (1-2 hafta + modül başına 1 gün) — Cross-cutting. Polymorphic `Comments` tablo + `EntityType`/`EntityId` + `@user` mention + `INotificationService` callback. Tamim/Doküman/Sözleşme/OrgChart entegrasyonu. **Bağımlılık:** Plan 31 SMTP caller (Plan 32 bekliyor). Plan yazılacak (Plan 35 adayı).
- [ ] **[Plan 40 · KVKK Veri Envanteri](plans/40-kvkk-process-backbone.md)** ⏳ **TASLAK 2026-05-21 rev 2** — Onay bekliyor. **DAR TUTULDU** — passive envanter + denetim + reverse search + AI integrity + VERBİS export. Execution kapsam dışı, Plan 42'ye devredildi. BKM Kitap v7 xlsx (361 süreç) Faz 1 seed. 50-65h, **7 faz**, 3-4 hafta. Bağımlılık: Plan 38 ✅, Plan 34/36/41/42 paralel.
  - **Faz 0** (6-8h) — Mosaik.Modules.Kvkk csproj + 14 entity + Migration 66/67/68
  - **Faz 1** (4-6h) — XlsxImporter + BKM Kitap v7 import (361 süreç, idempotent)
  - **Faz 2** (12-15h) — KVİE CRUD UI (Index filter + 20 sütun Edit + 6 aspect Detail — 5 aspect stub Plan 42 bekler)
  - **Faz 3** (6-8h) — SOP entegrasyonu (Plan 34 başladıktan sonra)
  - **Faz 4** (8-10h) — Global reverse search sidebar
  - **Faz 5** (12-15h) — AI Integrity Checker 8 pattern + Hangfire daily + email digest
  - **Faz 6** (6-8h) — VERBİS export ClosedXML + aydınlatma metni versioning
  - **Faz 7** (8-10h) — Risk dashboard (xlsx Risk Özeti parite, Plan 42 KvkkProcessingActivity log beslemesi)

- [ ] **[Plan 41 · Form Builder](plans/41-form-builder.md)** ⏳ **TASLAK 2026-05-21 — KRİTİK PATH** — Onay bekliyor. Portal form altyapısı. **Plan 42 prereq.** Hybrid yaklaşım: v1 JSON config server-side render + admin form CRUD; v2 drag-drop builder UI ileride. 12 field tipi (text/textarea/number/date/select/multiselect/radio/checkbox/file/signature/hidden/section). Public link + anonim submit + AntiSpam + Şifreli alan (ihbar için). DataElement mapping ZORUNLU Yayında geçişi için (KVKK Plan 40 entegrasyon). 8 template seed (DSAR/İhbar/Aday/İhlal/Rıza/Engelli/DPA/Review). 54-70h, **8 faz**, 4-5 hafta. Bağımlılık: Plan 38 ✅, Plan 36, Documents.
  - **Faz 0** (6-8h) — Mosaik.Modules.Forms csproj + 7 entity + Migration 70
  - **Faz 1** (10-12h) — Render + Submit pipeline (JSON config v1)
  - **Faz 2** (8-10h) — Admin Form CRUD
  - **Faz 3** (8-10h) — Public link + Anonim + AntiSpam (honeypot + rate limit + reCAPTCHA opsiyonel)
  - **Faz 4** (8-10h) — File upload (Documents reuse) + Signature pad + DataElement mapping admin UI
  - **Faz 5** (4-6h) — Şifreli alan AES-256 + Key Vault + İhbar template seed
  - **Faz 6** (6-8h) — Submission admin + Export ClosedXML
  - **Faz 7** (4-6h) — 8 template seed (Migration 71)

- [ ] **[Plan 42 · Process Execution Runtime](plans/42-process-execution-runtime.md)** ⏳ **TASLAK 2026-05-21 — BİRLEŞTİRİCİ** — Onay bekliyor. ProcessInstance runtime — Process'in N kez execute edildiği vaka. 6 aspect polymorphic timeline (Form / Workflow / Document / DecisionLog / Audit / KvkkContext). SLA timer (DSAR 30-gün, Breach 72h, custom). Result rendering (PDF QuestPDF + Word OpenXml). KvkkProcessingActivity log (her instance DataElement işleme kaydı). ICS feed. "Süreçlerim" inbox + admin dashboard. 64-84h, **9 faz**, 4-5 hafta. Bağımlılık: Plan 38 ✅, Plan 36, Plan 40, Plan 41 (KRİTİK), Plan 31, Documents.
  - **Faz 0** (6-8h) — Mosaik.Modules.ProcessRuntime csproj + 5 entity + Migration 72
  - **Faz 1** (8-10h) — ProcessExecutionService + manuel başlatma + Workflow Engine hookup
  - **Faz 2** (6-8h) — Public + Anonim başlatma + token validate
  - **Faz 3** (10-12h) — "Süreçlerim" inbox + Instance Detail 6 aspect timeline
  - **Faz 4** (8-10h) — SLA Timer Hangfire + Eskaltsiyon (Plan 37 §7.6 pattern)
  - **Faz 5** (6-8h) — Result Rendering (PDF QuestPDF + Word OpenXml) + Documents arşiv + email ek
  - **Faz 6** (6-8h) — KvkkProcessingActivity log + Retention Job (auto anonimleştir/sil)
  - **Faz 7** (4-6h) — Cron + Event trigger + ICS feed (Outlook/Google abone)
  - **Faz 8** (4-6h) — Process Owner Dashboard + Admin Tüm Instance'lar + Excel export
  - **Faz 9** (6-8h) — E2E entegrasyon test (DSAR/İhbar/Aday/İhlal/Review uçtan uca)

- [ ] **[Plan 44 · RAG Chunk-Level Permission Guard](plans/44-rag-permission-guard.md)** ✅ **ONAYLANDI 2026-05-25 revize v2 — 🔴 HOT-FIX** — Onay bekliyor. SOP RAG canlı, `SopChunkRetriever` sadece FirmaId scope; Documents Plan 27 Faz E bağlanmadan ÖNCE chunk-level `SecurityLevel/AllowedRoleIds/AllowedDepartmentIds/AllowedUserIds` + `IRagAccessPolicy` Core abstraction zorunlu. KVKK m.12 + ticari sır leak koruma. 12-18h, 4 faz, ~1 hafta.

- [ ] **[Plan 45 · Excel-to-Process AI Parser](plans/45-excel-to-process-ai-parser.md)** ✅ **ONAYLANDI 2026-05-25 revize v2** — Onay bekliyor. Excel upload → Qwen schema inference → SurveyJS template + SQL table autogen + historical import. Departman 3 hafta yazılım bekleme → 5dk AI işlemi. Plan 41 Faz 0-3 prereq. ClosedXML + Plan 40 Presidio scan reuse. 40-60h, 5 faz, 3-4 hafta.

- [ ] **[Plan 46 · PWA Offline-First + Native Camera/Barcode](plans/46-pwa-offline-camera.md)** ✅ **ONAYLANDI 2026-05-25 revize v2** — Onay bekliyor. Workbox 7 service worker + IndexedDB sync queue + ZXing-js barcode + getUserMedia camera + Web Push. BKM depo/mağaza saha personeli için kritik. Plan 41 Faz 0-3 prereq + Plan 17 Faz H NotificationService genişlet. 50-70h, 5 faz, 4-5 hafta.

- [ ] **[Plan 47 · Auto-Tuning Process Optimization Advisor](plans/47-auto-tuning-process-advisor.md)** ✅ **ONAYLANDI 2026-05-25 revize v2** — Onay bekliyor. Daily Hangfire job → 5 pattern detector (approval bypass / bottleneck / dead-end / duplicate / volume spike) → Qwen narrative + heuristic impact → Admin Inbox → Accept + workflow auto-edit + 30-gün post-accept impact tracking. **VISION §7 kuzey yıldızı — Operational Intelligence**. Plan 36+42 prereq. 25-35h, 5 faz, 2-3 hafta.

### Radikal Paradigmalar (VISION §10) — vNext + Plan 44-47 sonrası ~6+ ay olgun olunca

- [ ] **[Plan 48 · Executable SOP](plans/48-executable-sop.md)** 🔬 **ARAŞTIRMA TASLAĞI 2026-05-25** — Doküman → Canlı süreç paradigm. Yöneticinin yazdığı doğal dil prosedür → Qwen parse → Stateless State/Transition + Hangfire SLA timer + Task auto-register. **Prosedür yazmak = süreci canlıya almak**. Plan 34 + 36 prereq. 60-90h, 6 faz. Tahmini: 2026 Q3.

- [ ] **[Plan 49 · Zero-UI Operations](plans/49-zero-ui-ops.md)** ✅ **ONAYLANDI 2026-05-25 revize v2** — Arayüzsüz akış paradigm. Mosaik widget basılı-tut + konuş + foto → Whisper TR + Qwen intent + Vision → form auto-fill + workflow trigger. **1 saniyede konuş, süreç başlar**. Plan 46 + 41 + 44 prereq. 80-120h, 7 faz. Tahmini: 2026 Q4.

- [ ] **[Plan 50 · Shadow Organization Graph](plans/50-shadow-org-graph.md)** ✅ **ONAYLANDI 2026-05-25 revize v2** — Gerçek iş ağı paradigm. @mention + workflow + comment log → observed shadow graph + anomaly detect (resmi vs gerçek). AI önerisi "bilgi merkezi Y'ye danışıl". KVKK aggregate-only constraint. Plan 35 + 36 + 38 prereq. 40-60h, 5 faz. Tahmini: 2027 Q1.

- [ ] **[Plan 51 · Differential Privacy Reporting](plans/51-differential-privacy-reporting.md)** ✅ **ONAYLANDI 2026-05-25 revize v2** — Aktif veri bağışıklığı paradigm. App-side privacy filter: aynı SP 3 ayrı sanitize output (genel müdür raw / analist aggregate+Laplace noise / misafir mask). Presidio + Qwen sensitive column sniff. Plan 14 + 44 + 40 prereq. 50-80h, 6 faz. Tahmini: 2027 Q2.

- [ ] **KVKK skill import** ✅ 2026-05-21 — `.claude/skills/kvkk-veri-envanteri/SKILL.md` (374 satır, claudskills.com export). Plan 40 Faz 5 AI Integrity Checker prompt kaynağı. Commit `36d590e`.

- [ ] **OSS Reuse Araştırması — vNext altılı kalp** ✅ 2026-05-21 — `docs/RESEARCH_OSS_VNEXT_2026-05-21.md` (5 paralel agent). vNext altılı kalp toplam **~60-75h tasarruf** (~%23-25). Yıllık lisans tek kalem: **QuestPDF Pro ~$699/yıl 1 dev**. Diğer hepsi MIT/Apache/BSD ücretsiz.
  - **Plan 36 Workflow** — Stateless 5.20.1 (Apache-2.0, ~700 satır FSM) + Hangfire — Elsa/Camunda/Temporal/Workflow Core reddedildi. ~%20-25 tasarruf. **ADR-019** yazıldı.
  - **Plan 40 KVKK** — Microsoft Presidio (MIT, Docker yan-servis Türkçe spaCy `tr_core_news_trf`) + FuzzySharp (MIT, C# native fuzzy) + KVKK resmi xlsx referans + DikkatIQ AI Layer reuse. ~%25-30 tasarruf.
  - **Plan 41 Form Builder** — SurveyJS Form Library 3.x (MIT) renderer partial reuse + signature_pad 5.1.1 (MIT) + honeypot/time-based AntiSpam + Cloudflare Turnstile opsiyonel. **ADR-020** yazıldı. ~%30-35 tasarruf.
  - **Plan 42 Process Runtime** — Stateless backend (Plan 36 reuse) + vis-timeline 8.5.1 (Apache/MIT) Instance Detail 6-aspect UI + Ical.Net 5.2.2 (MIT, ical-org canonical) ICS feed + Scriban 7.2.0+ (BSD-2) result template + QuestPDF Pro + OpenXml + OfficeIMO + PDFsharp signature + Playwright opsiyonel. **ADR-021** yazıldı. ~%25 tasarruf.
  - **Plan 34 SOP** — Tamim altyapısı %80 reuse (mevcut) + EasyMDE opsiyonel markdown editor. ~%15-20 tasarruf.
  - **Reddedilenler:** Elsa 3.6, Camunda 8, Temporal, Workflow Core, Novu, iText AGPL, Aspose, DocX Xceed, EPPlus 7, Formbricks AGPL, LimeSurvey GPL/PHP, formio.js OSL-3.0, Survey Creator ticari, Bearer (C# yok), Privado (C# yok), OpenMetadata (overkill), Türk SaaS (proprietary abonelik), Wiki.js AGPL, Outline BSL, ProcessMaker AGPL/PHP, Flowable/Bonita/Camunda 7 JVM.
  - **3 yeni ADR:** ADR-019 (Workflow Stateless), ADR-020 (Form Builder Hybrid SurveyJS), ADR-021 (Document rendering QuestPDF+OpenXml+OfficeIMO).
  - **Aksiyon (Plan başlangıçlarında):** NuGet (`Stateless 5.20.1`, `FuzzySharp`, `QuestPDF 2026.5.0`, `DocumentFormat.OpenXml 3.5.1`, `OfficeIMO.Word 1.0.34`, `PDFsharp 6.2.0`, `Scriban >=7.2.0`, `Ical.Net 5.2.2`, `ZXing.Net`, opsiyonel `Microsoft.Playwright`) + JS assets (`wwwroot/lib/surveyjs/` UMD, `signature_pad`, `vis-timeline` standalone, opsiyonel `easymde`, `cookieconsent`) + KVKK referansları (`docs/kvkk-references/` gitignored telif) + Docker compose Presidio Analyzer+Anonymizer+Türkçe spaCy `tr_core_news_trf`.
  - **Bütçe onayı:** ~~QuestPDF Professional ~$699/yıl~~ **REV 3 REDDEDİLDİ 2026-05-21 (deep research ek agent).** Yerine **Gotenberg Docker (MIT) + Gotenberg.Sharp.API.Client (Apache 2.0) + PdfSharp/MigraDoc 6.2.4 (MIT) + QRCoder 1.8.0 (MIT) + Razor.Templating.Core (Apache 2.0)** hibrit stack — **$0 yıllık lisans + 3 yıl $2097 tasarruf**. ADR-021 rev 2. Razor view reuse Mosaik UI ile aynı Tailwind print CSS + digital signature native (MigraDoc PKCS#7) + Gotenberg fallback MigraDoc in-process. BKM Docker compose Plan 40 Presidio + Plan 42 Gotenberg <2Gi RAM toplam. Reddedilen ek alternatifler: Carbone CCL (third parties yasağı), DinkToPdf (wkhtmltopdf 2023 arşivlendi + CVE patch'siz), jsreport LGPL+JS SDK, Spire.PDF Free (10 sayfa limit), HiQPdf Free (5 sayfa limit), PdfReport.Core (LGPL + iTextSharp dep + bakım yavaş), iText AGPL.
- [x] **[Plan 36 · Workflow Designer + Onay Akışları](plans/36-workflow-designer-onay-akislari.md)** — Faz A+B+C ✅ TAMAMLANDI 2026-05-21 (W-01..W-17). Faz D (W-18..W-19, SOP entegrasyon) açık.
  - [x] **Faz A** ✅ W-01..W-08 — Core engine + designer UI
  - [x] **Faz B** ✅ W-09..W-13 — Hangfire job + bildirim + escalation + ICS feed
  - [x] **Faz C** ✅ W-14..W-17 — Obligations + Contracts entegrasyon + widget
  - [ ] **Faz D** (4-6h, W-18..W-19) — SOP entegrasyonu (Plan 34 sonrası)

**Paralel ikincil iş:**

- [ ] **Plan 18B · HR Sync** (büyük, ~16-24h) — Hangfire + Mosaik.User ek kolonlar + UserSyncService + Email/TC kararları + UserDataFilter otomatik atama. Haftada 1-2 gün, blok değil. (Kullanıcı 8 May "şimdilik durdur" demişti, vNext sıralaması sonrası yeniden değerlendir.)
- [ ] **Plan 32 · Scheduled Reports + Email Distribution** — 6 açık soru cevap bekliyor. **Comment/Mention'dan ÖNCE bitirilmesi gerek** — Plan 31 SMTP caller'ın ilk somut kullanımı bu. Comment/Mention'ın email bildirim path'i Plan 32 ile aynı SMTP altyapısını kullanır.

**Yapılmayacak (vNext kapsamı dışı, [VISION §6](docs/VISION.md#6)):**
- KPI / OKR modülü — Reports zaten karşılar
- Mesajlaşma — Slack/Teams varken marjinal
- Duyuru ayrı modül — Tamim'e `Type` enum yeterli (2-3 gün)
- ~~Form Builder kendi üretim — open-source integrate (LimeSurvey / Formbricks)~~ **REVİZE 2026-05-21:** Plan 41 ile değişti — SurveyJS renderer reuse + kendi modül (`Mosaik.Modules.Forms`). LimeSurvey GPL/PHP + Formbricks AGPL reddedildi. ADR-020.
- [x] **Plan 16.5 Faz C+D** ✅ — ILlmService + FallbackLlmService + IAiExtractionService + IAiSuggestionService + PromptBase. 337 test. commit d2fd745 + 1b195d4.
- [x] **Plan 17 (Tamim)** ✅ — Tüm fazlar tamamlandı. Faz H Bildirim: NotificationService + NotificationsController + sidebar badge + migration 41 + Circular wiring mevcut.
- [x] **Security altyapısı (3 katman)** ✅ 2026-05-13 — `.claude/agents/security-reviewer.md` + `.claude/skills/mosaik-security/SKILL.md` + `.claude/commands/security-check.md`. CLAUDE.md §2 + security-principles.md güncel. Tetik: yeni POST/SQL/email/JS fetch yazılırken proaktif, `/security-check` ile denetim.

### Code review backlog (oturum 2026-05-13 güvenlik denetimi)

3 paralel agent (code-reviewer + silent-failure-hunter + general-purpose security) son 35 commit'i (e2da9b6 → 0841080) taradı. 0 CRITICAL exploitable.

**2026-05-14 doğrulama sweep'i:** 11 HIGH listesi canlı koddan kontrol edildi. **7 HIGH ZATEN KAPALI** (a68378c "post-review hardening" commit'inde fix edilmiş), 2 HIGH gerçekten açıktı (`_ = ex;` log yok + bulk SMTP disabled warning yok), 2026-05-14 commit'iyle kapatıldı. TODO listesi güncellendi — tespit-fix asimetrisi giderildi.

- [x] **HIGH-1 · Route ambiguity** ✅ KAPALI — `CreateReport()`/`EditReport()` `private`, `*LegacyRedirect()` ayrı route attribute.
- [x] **HIGH-2 · SmtpEmailService exception swallow** ✅ KAPALI — `Task<EmailSendResult>` döner, 4 ayrı catch (SmtpException/SocketException/OperationCanceled/Exception) log + Failed result.
- [x] **HIGH-3 · EmailTemplates HtmlEncode** ✅ KAPALI — `E()` + `SafeUrl()` her interpolation'da, raw string güvenli.
- [x] **HIGH-4 · AntiForgery cache null-poison** ✅ KAPALI — `if (__aftCache) return __aftCache` truthy check, `_AppLayout.cshtml` global token.
- [x] **HIGH-5 · `_AdminOverview` query try/catch** ✅ KAPALI — `AdminController.cs:122-156` try/catch + OperationCanceled rethrow + SqlException + Exception ayrı log.
- [x] **HIGH-6 · OrgChart export catch** ✅ KAPALI — `catch (e) reportFailure` + Promise rejection handler + finally.
- [x] **HIGH-7 · EditReportLegacyRedirect id validation** ✅ KAPALI — `AnyAsync(r => r.ReportId == id)` check + SqlException + warning redirect.
- [x] **HIGH-a · `_ = ex;` log yok** ✅ KAPALI 2026-05-14 — `AdminController.Reports.cs:66`, `AdminController.cs:253`, `TestController.cs:39+72` → `_logger.LogError` + SqlException ayrımı.
- [x] **HIGH-d · Bulk SMTP disabled warning yok** ✅ KAPALI 2026-05-14 — `SmtpEmailService.cs:82` `_logger.LogWarning` Skipped count + Subject.
- [ ] **MEDIUM-b · SmtpEmailService `ex.Message` ErrorDetail'e** — Caller henüz yok (Plan 31 altyapı, Plan 32 caller bekliyor). `EmailSendResult` record yorumunda "UI'a YASAK" sözleşmesi var. Plan 32 caller eklerken bu sözleşme korunmalı (audit log + structured log → UI'a TempData generic mesaj). ~15dk Plan 32 ile.
- [ ] **MEDIUM-c · POST action try/catch yok** — `AdminController.Reports.cs:88,214` (POST CreateReport/EditReport). Service Result pattern dönüyor, async exception (deadlock/connection drop) production'da `app.UseExceptionHandler("/Home/Error")` ile karşılanıyor — connection string sızıntı yok. Defensive depth iyileştirme: per-action user-friendly mesaj. ~10dk her biri.
- [ ] **MEDIUM · SMTP password User Secrets uyarısı** — README/INSTALL.md notu + pre-commit hook'a non-empty `SmtpSettings.Password` tespit. ~30dk.
- [ ] **MEDIUM · `Database/56_AppModulesGroupKey.sql` CHECK constraint** — whitelist DB-level enforce. Yeni migration. ~10dk.

7+2 HIGH kapandı. Kalan 4 MEDIUM bağımsız + opsiyonel.

**2026-05-26 WorkflowInboxService fix (commit bu oturumda):**
- [x] **CRITICAL-W1 · WorkflowInboxService FirmaId leak + N+1** ✅ 2026-05-26 — FirmaId filtresi eklendi, N+1 batch preview ile çözüldü, CountPendingForUserAsync ayrı implementasyon.
- [x] **HIGH-W2 · WorkflowController.Respond try/catch** ✅ 2026-05-26 — AdvanceAsync başarısız olsa audit log artık yazılıyor.
- [x] **HIGH-W3 · ResolveFirmaId() hardcoded 1** ✅ 2026-05-26 — ICurrentUserService.FirmaIds[0] kullanıyor.
- [x] **HIGH-S1 · SopChunkRetriever magic number** ✅ 2026-05-26 — SopVersion.Approved sabit kullanıyor (SopService 3 yer dahil).

**2026-05-22 dış inceleme follow-up (Plan 45 sonrası backlog):**
- [ ] **LOW-F5 · AI günlük budget concurrent overshoot** — `Mosaik/Services/AiSummaryProvider.cs:47,103`. Check-and-add race; aynı provider'a paralel istekler bütçe aşılmadan içeri girip toplamda aşabilir. Fix: `Interlocked.Add` ile atomik check-and-add. Multi-instance issue zaten D-02-5 journal'da not (DB-backed counter sonraki iterasyon). ~30dk.
- [ ] **LOW-F6 · CSP inline JS debt** — Inline `onclick="..."` 50+ oluşum (örn. `CreateDataSource.cshtml:75`, `Documents/Index.cshtml:21`). CSP `script-src 'self'` eklemek için bloklayıcı. Sweep + agent batch refactor. ~4-6h.
- [ ] **LOW-F7 · Büyük dosya borcu** — `EditReportV2.cshtml` 1006 satır, `CreateReportV2.cshtml` 913 satır, `_AppLayout.cshtml` 444 satır. Reports modülü yeniden ele alındığında partial split. M-01 follow-up.
- [ ] **LOW · Voice command (annyang) araştırma** — Web Speech API wrapper (2KB MIT), KVKK riski (ses cloud'a — Chrome→Google, Edge→Azure). Geri açma koşulu: Plan 40 Faz 6 DataElement envanter + provider DPA + opt-in. Alternatif: Whisper.cpp WASM yerel STT. Detay: `docs/RESEARCH_ANNYANG_2026-05-22.md`.

### IK / HR — Zirve `vw_PersonelDepartman` ile

**Bağlam:** BKM Zirve `vw_PersonelDepartman` view 3 firma UNION (BKM_GENEL + BURSA_KÜLTÜR_MERKEZİ + ASİYE_BİNGÖLBALI), 4 seviye hiyerarşi (Lokasyon → AltLokasyon → Departman + Unvan), 272 aktif personel. IK DataSource zaten Mosaik'te kayıtlı. Detay: `memory/project_zirve_personel_discovery.md`.

**Hızlı kazanım (Plan 18 sync'i beklemez — sadece read-only rapor/dashboard):**
- [ ] **IK Personel Listesi raporu** — vw_PersonelDepartman üzerinden filtreli liste (Firma + Lokasyon + AltLokasyon + Departman + Unvan dropdown'ları). SP: `bkm.sp_IkPersonelListesi`. FilterDefinition `sube/IK` aktive.
- [ ] **Yeni Başlayanlar raporu** — `Igt >= @BasTarih AND Ict IS NULL`, son 30/90 gün filtreli. KPI: aylık trend.
- [ ] **İşten Ayrılanlar raporu** — `Ict BETWEEN @BasTarih AND @BitTarih`, ayrılma kodu (Icn) gruplu.
- [ ] **Mağaza Personel Yoğunluğu raporu** — AltLokasyon × Departman pivot. Toplam personel + ortalama kıdem.

**Plan 14 Faz B implementasyon (durmuş):**
- [ ] **IK FilterDefinition `(sube, IK)` aktivasyonu** — 3 OptionsQuery seçeneği hazır. Migration 33. Kullanıcı 8 May "şimdilik durdur" dedi.

**Plan 18 HR Sync (henüz yazılmadı, büyük yatırım):**
- [ ] **Mosaik.User entity ek kolonlar** — `ZirvePersonelNo` (UNIQUE), `Lokasyon`, `Sube`, `Departman`, `HireDate`, `Firma`. Migration NN.
- [ ] **Hangfire entegrasyonu** — Daily job altyapısı.
- [ ] **`UserSyncService`** — view → Mosaik.User insert/update (Personelno bazlı). Yeni: insert. Ict NOT NULL: IsActive=0. Sube değişti: UserDataFilter güncelle.
- [ ] **Email stratejisi karar** — AD lookup / placeholder / manuel admin GUI.
- [ ] **TC PII saklama karar** — hash mı raw mı skip mi.
- [ ] **UserDataFilter otomatik atama** — sync sırasında `User.Sube` → `UserDataFilter (sube/IK)`.
- [ ] **Plan 18 dosyası** `plans/18-hr-sync.md` yazılacak.

**Bilinen riskler:**
- `vw_PersonelDepartman` user-managed (Zirve schema değişirse kırılır). Plan 18 öncesi backup.
- `BKM_HEYKEL_GENEL` DB var ama view'da yok — netleştirilmeli.
- View raw PII (TC, IBAN, Maaş) içeriyor — sync mapping finansal alanları skip etmeli.

### Trivia / housekeeping (~30 dk)
- [ ] **NotebookLM re-login** — terminalde: `D:/Dev/reporthub/.venv/notebooklm/Scripts/notebooklm.exe login`

### Orta (2-4h)
- [ ] **G-09 · SP read-only login** ⚠️ CANLIYA CIKMADAN ZORUNLU — Rapor execution için ayrı read-only SQL login (db_datareader + EXECUTE). DataSource modeline `ReadOnlyConnString` ekle veya mevcut ConnString'i read-only login ile değiştir (kullanıcı kararı 2026-05-01).
- [ ] **SP mimarisi · sp_PdksPano → inline TVF refactor** — `fn_PdksDetay`, `fn_PdksKpiOzet`, `fn_PdksDepartmanKirilim` + orkestrator SP. ADR-004 adayı. Detay arşivde.
- [ ] **Dashboard P1 · Inline RS boyut limiti / lazy-load** (10K satır → 3MB HTML, ilk N + AJAX)

### Büyük (>4h, çok-fazlı)
- [ ] **Plan 05 · AST formula parser** (~6h+) — kendi recursive descent parser
- [ ] **Plan 13+ · vNext modüller** (Documents, Announcements, Calendar, Forms, Messages, Approvals — modül-modül ekleme, Plan 12 module infrastructure üzerine)
- [ ] **Plan 14 · Filter Production-Readiness** (`plans/14-filter-production-readiness.md`) — Faz A ✅. Faz B (IK sube OptionsQuery + migration 33) bekliyor. Faz C (Plan 16.5 Faz B sonrası IUserDataScope ile reportAccess deny-by-default). Faz D Plan 18 sonrasına ertelendi.
- [ ] **Plan 16 · vNext modül roadmap** (`plans/16-vnext-module-roadmap.md`) — 9 modül (17→26) port stratejisi. Onay bekliyor.
- [ ] **Plan 16.5 · Mosaik.Core shared kit** (`plans/16.5-shared-kit.md`) — Faz C+D bekliyor (AI Core).
- [ ] **Plan (eski 14) · Install/Deploy script** — master kurulum (DB CREATE + migration + seed + brand + admin user prompt). Plan 11 öğretisi: rapor metadata dump install kapsamına dahil. Numara çakışması var, yeni numara alacak.
- [ ] **context-mode esinlenmesi · 2 hook** — (1) `PreCompact` hook: `/compact` öncesi aktif TODO + son commitler otomatik journal'a append. (2) `PostToolUse` output-size uyarısı: MCP tool çıktısı >50 satırı geçince logla. Kaynak: https://github.com/mksglu/context-mode

### Stratejik / belirsiz vade (TARTIŞMA gerekli)
- [ ] Plan 04 (potansiyel) · Alpine.js + htmx adoption
- [ ] Plan 05 (potansiyel) · Scheduled Reports + Email Hangfire
- [ ] Yeni proje adi brainstorm
- [ ] vNext · Sirket içi portal architecture (Plan 06)
- [ ] Yetki revizyonu — granular roller (action-level [Authorize], sidebar conditional)

---

## DEVAM EDEN PROJE / TARTIŞMA

### YENİ PROJE ADI ARANIYOR (28 Nisan 2026)
Kullanıcı: "projeye reporthub demeyelim bir ara değiştirelim isim bulalım". Mevcut brand: "ReportHub" (geçici), kod adı "Mosaik" (klasör + namespace). Sidebar + AuthLayout'ta "BKM Kitap" + "Rapor Paneli" yer tutucu olarak güncellendi. Plan 06 vNext sırket içi portal hazırlığı sırasında (yeni feature seti netleşince) isim de finalize olabilir.

### MAJOR VISION — Sonraki Versiyon: Rapor Portali → Şirket İçi Portal (28 Nisan 2026)
Kullanıcı: "sonraki versiyonda rapor portalindan şirket içi portala doğru evireceğiz yapıyı". Hedef versiyon vNext: tam şirket içi portal.

**Eklenebilecekler (taslak):**
- Günlük tamim / sirküler / genelge — günlük resmi duyuru, departman/herkese, okundu-onaylı, arşiv, search
- Duyurular / haberler (announcement feed)
- Departman / takım dizini (org chart) — Plan 20 ✅ kısmen
- Doküman / dosya paylaşımı (intranet drive)
- Mesajlaşma / yorum (comment thread)
- Form / anket (form builder + survey)
- Prosedür / SOP yönetimi
- Takvim / etkinlik
- KPI / hedef takibi (OKR)
- Onay akışları (workflow / approval chains)

**Mimari etkiler:**
- Multi-tenant / departman izolasyonu — yetki revizyonu kritik
- Rol modeli granular (report_designer, hr_admin, doc_manager...)
- Sidebar conditional render
- Background services (Hangfire — duyuru bildirimi, takvim hatırlatıcı)
- Search global — Elasticsearch / SQL FTS / Meilisearch?
- Notification subsystem (red bell mevcut placeholder, gerçek olur)
- File storage strategy (MinIO/S3 veya disk)
- Real-time updates (SignalR — comment thread, notification push)

**Aksiyon:** R refactor + Plan 05 (cron/email) tamamlandıktan sonra **Plan 06 — vNext architecture** ayrı Tier 3 oturum.

---

## BACKLOG (henüz başlanmadı)

### User yönetimi P1-P3
- [ ] User modeline alan ekle: Phone (string?, 20), Department (string?, 100), Position/Title (string?, 100), ManagerUserId (int?), Notes (string?, 500). Migration + form alanları + admin liste kolonları.
- [ ] Admin user listesinde arama + filtreleme (Username/FullName/Email arama, rol/aktif/AD filtre).
- [ ] Son giriş zamanı gösterimi (LastLoginAt admin tablosuna, "2 gün önce" formatı).
- [ ] User audit alanları: PasswordChangedAt, FailedLoginCount, LockedUntil, MustChangePassword.
- [ ] Hesap kilitleme (5 başarısız → 15dk).
- [ ] Şifre karmaşıklığı kuralları (min uzunluk, harf+sayı zorunluluğu).
- [ ] Zorla şifre değiştirme flag (admin create'te "ilk girişte değiştir").
- [ ] Admin şifre sıfırlama (token ile).
- [ ] Toplu CSV import (ClosedXML).
- [ ] AD/LDAP senkronizasyon (LDAP search + tek tıkla ekleme).
- [ ] Kullanıcı kopyalama (rol + filtre kopyala).
- [ ] Soft delete (silmeden önce arşivleme önerisi).
- [ ] Avatar / profil resmi (upload + resize).
- [ ] Kullanıcı tercihleri (dil, tema, sayfa boyutu).
- [ ] Kullanıcı aktivite özeti (son N rapor, favori sayısı, toplam çalıştırma).

### ReportCatalog & Filtreleme
- [ ] **ReportCatalog.AllowedRoles CSV deprecate** — ADR-004 adayı. ReportAllowedRole junction birincil.

### Dashboard ileri özellikler (P2-P3)
- [ ] Tab sürükle-bırak sıralama
- [ ] Undo/redo (son 10 state, Ctrl+Z/Y)
- [ ] Raw JSON editor modu (Monaco/ace.js + schema validation)
- [ ] Component grubu / section ayraç
- [ ] Tarih kolon formatı (TableColumnDef format: date|datetime|number)
- [ ] Chart tooltip TR sayı formatı
- [ ] Runtime JS ayır (DashboardRenderer içinden inline JS → wwwroot/js/dashboard-runtime.js)
- [ ] DashboardRenderer static → DI (IDashboardRenderer interface)
- [ ] Stacked / area / mixed chart tipleri (M-12)
- [ ] Median / percentile / YoY agg fonksiyonları
- [ ] Text/markdown, gauge, progress bar component tipleri
- [ ] i18n: tr-TR hardcoded → appsettings
- [ ] Dashboard export: PDF, PNG (Playwright/Puppeteer)
- [ ] Dashboard paylaşım linki (tokenized URL)
- [ ] "Ana Dashboard" atama (Settings.DefaultDashboardReportId)
- [ ] Heatmap + Gauge widget'lar (M-12 disabled, sonraya)

### Mimari / kalite (FAZ 3)
- [ ] **M-06 · EF Core Migrations geçişi** (1 gün) — mevcut şemayı baseline yap, Database/legacy/ oluştur.
- [ ] **F-06 · CSP politikası** (1 gün) — opsiyonel; inline onclick/script temizle, header ekle.
- [ ] **Test coverage %30 hedefi** (1 hafta) — AdminController integration, ReportsController.Run, Admin SpPreview, PasswordHasher edge cases.
- [ ] AuditLog selektif eksikler — datasource/category delete log'lanmıyor (G-04 takip).
- [ ] CSS karışıklığı — `@apply` ile components tanımı.
- [ ] Database scriptleri klasör ayırma (Schema/Seed/Migrations/StoredProcedures).
- [ ] JavaScript bundle/build (esbuild/vite minify, prod için).

### Mimari tutarsızlıklar (kalan YÜKSEK / ORTA)
- [ ] **Data access stratejisi dokümantasyonu** — ADR-001 yazıldı ✅ ama AGENT.md gibi yanıltıcı manifesto kalanları gözden geçir.
- [ ] **ViewModel → DTO pattern** (mass assignment riski, AutoMapper veya manuel projection).
- [ ] **Form syntax tutarlılığı** (raw `<form>` standart, EditReport vb. düzelt).

### Performans & Operasyon
- [ ] Rapor sonuçları için caching, büyük sonuç setleri için pagination.
- [ ] Connection pooling/timeout ayarları gözden geçirme.
- [ ] Rate limiting (brute force koruması), HTTPS zorunluluğu, session timeout.
- [ ] Integration testleri artırma, UI test otomasyonu (Selenium), load testing.
- [ ] CI/CD pipeline, otomatik deploy, monitoring/alerting, backup stratejisi.

---

## TAMAMLANDI ARŞİV

### Plan'lar — kapanmış (detay `plans/archive/`)
- ✅ **Plan 03** — M-13 Project-Wide Design Harmonization (28 Nisan 2026, 7 commit, 17 view + 4 CSS/JS, ~1850 satır azalma)
- ✅ **Plan 07** — Yetki/Filter revizyon (4-6 Mayıs 2026): FilterDefinition master + UserDataFilters dinamik UI + deny-by-default + raporGrubu rename + Reports/Index liste filtresi + Admin Filtreler CRUD. Migration 20-24.
- ✅ **Plan 09** — Designer ↔ Run görsel parite (6 Mayıs 2026, 4 faz)
- ✅ **Plan 11** — Mosaik Foundation (rebrand + DB reset + UTC) (7 May 2026)
- ✅ **Plan 12** — Admin GUI + Brand+Module parametric (7 Mayıs 2026)
- ✅ **Plan 14 Faz A** — UserDataFilter diff audit (commit `27714c5`, 2026-05-07)
- ✅ **Plan 14 Faz C1** — SpInjectionScope + ReportAccessScope (commit `4e52a05`, 2026-05-08). C2/C3 ROI düşük.
- ✅ **Plan 16.5 Faz A+B** — Domain primitives + Workflow + Lookup + DataScope. Migration 33+34. 43 test (2026-05-08).
- ✅ **Plan 18A** — IK Quick Reports + Dashboard (commit `a972b66`+`e5b442e`, 2026-05-11). 6 SP + ReportCatalog seed + İK Pano + Alpine bug fix.
- ✅ **Plan 20 Faz A+B+C** — Org Chart (3 görünüm + sağ-tık modal + Zirve canlı incumbent + PNG export, 2026-05-08).
- ✅ **M-11 Plan 02** — Dashboard Builder UX Redesign (13 faz F-0..F-12, 6 Mayıs 2026 KAPANIŞ). Plan 09 paritesi dahil.

### FAZ 0 — KAPANDI (22 Nisan 2026)
- ✅ **G-01** Hardcoded SA şifresi (commit `8de22fd`)
- ✅ **Bağlam yönetimi** rituel + rules + hooks (commit `e59e3a9`)
- ✅ **F-01** SP Önizle click handler (commit `07f4b91`)
- ✅ **32-dosyalık backlog commit-split** (16 commit `64259ed`..`7a7b81d`)
- ✅ **Deprecated artifacts** (Views/Auth/AGENT.md silindi, `7a7b81d`)
- ✅ **Pre/Post-commit hook'lar** (commit `59888db`)

### FAZ 1 — KAPANDI (22 Nisan 2026)
- ✅ **M-02** Exception handling sanitize (`b6ff43a` + `a047957`)
- ✅ **G-02** Open redirect fix (`4c40f61`)
- ✅ **G-03** UserDataFilter whitelist + regex (`4c40f61`)
- ✅ **F-02** SP Önizle default parametre + admin override (`b6ff43a` + `816c8c2`)
- ✅ **M-03 Faz A+B** User.Roles CSV deprecate kod-düzey + nullable (`2d0c3fd`, `bf922ae`)
- ✅ **M-04** DashboardRenderer + UserDataFilter + UserRole sync unit tests (`6c70b1e`, `b714916`)
- ✅ **session-handoff skill auto-commit** (`5df75ff`)
- ✅ **dashboard-builder.js spPreviewReady + kolon datalist** (`b3ae747`)

### FAZ 2 — KAPANDI
- ✅ **M-01** AdminController service extraction (5 adım: Category/Role/DataSource/Report/User Management Services + UserRoleSyncService)
- ✅ **G-04** Audit log genişletme — 10 CRUD audit (`effa7b5`)
- ✅ **G-05** Cookie HttpOnly/Secure/SameSite/ExpireTimeSpan (`fdc97ca`)
- ✅ **G-06** TestController authorize + antiforgery (`fdc97ca`)
- ✅ **G-07** Dashboard iframe policy review (4 Mayıs 2026 audit)
- ✅ **G-08** DashboardRenderer JSON escape regresyon test (6 escape testi)
- ✅ **M-03 Faz C** User.Roles kolon drop (Migration 19, 4 Mayıs 2026)
- ✅ **M-05** DashboardHtml legacy retirement (3 faz, ADR-005, Migration 17)
- ✅ **M-07** ViewModel BindNever (4 Mayıs 2026)
- ✅ **M-08** Async tutarlılık (4 Mayıs 2026)
- ✅ **M-09** AsNoTracking sweep (4 Mayıs 2026)
- ✅ **M-10** Named Result Contract (Faz 1-6, ADR-007, Migration 18+26+27, 6 Mayıs 2026)
- ✅ **F-03** dashboard-builder.js memory leak (F-7 split'te çözüldü)
- ✅ **F-04** AGENT.md silindi (`7a7b81d`)
- ✅ **F-05** Türkçe UTF-8 normalize (4 Mayıs 2026)
- ✅ **dashboard-builder.js V1 split** (F-7 modülerleştirme, 7 modül)
- ✅ **builder-v2/builder-drawer.js split** (4 Mayıs 2026: 511 → 269 + 259)
- ✅ **Hesaplı kolon autocomplete** (commit `36dd2f3`, 4 Mayıs 2026)
- ✅ **DateTime sweep** Faz A-E (UtcNow + Migration 25 + Plan 11 reset)
- ✅ **Dashboard P0/P1 audits** — Config deserialize, RS index validation, Mobile responsive, Tailwind local serve
- ✅ **CreateUser veri filtresi bölümü** (zaten _AdminUserDataFilterPanel partial'ı çağırıyordu)
- ✅ **ADR yazımı** — ADR-001 data-access ✅, ADR-002 → ADR-005 dashboard-architecture ✅, ADR-003 role-model ✅, ADR-004 skill-design ✅
- ✅ **CSV İndir butonu** (commit `143e1d9`)
- ✅ **Plan 03/04/06.B arşivle**
- ✅ **M-13 sub-nav** işaretle

### Mimari tutarsızlıklar — düzeltilenler (21 Nisan denetimi)
- ✅ User.Roles CSV + UserRole ikili sistem → Migration 19 + drop
- ✅ DashboardHtml dual storage → M-05 Faz C (`0f73478`)
- ✅ AsNoTracking eksikleri → 4 Mayıs sweep
- ✅ ex.Message → user'a JSON dönme (AdminController.Filters.cs:158, vb.)
- ✅ IsDashboard ölü property silindi
- ✅ CSS eski class'lar (form-card/btn-brand) → M-13 Plan 03 R2 sonrası temiz
- ✅ [ValidateAntiForgeryToken] tüm POST'larda
- ✅ async void hiç yok

### İlk dönem yapılanlar (özet)
Kullanıcı tablosu + PBKDF2, raporlar liste/çalıştırma ayrımı, Excel export, parametre üretici, ortak `_AppLayout`, navbar/footer sticky, Türkçe normalize, admin user CRUD, profil, rol checkbox, dashboard canlı veri, sticky table header, server-side filtreleme, Logs ayrılması, AuditLog merkezi servis, otomatik testler (PasswordHasher + AuditLog), manuel smoke testler.

---

## ARŞİV — Eski Tartışma Notları

### SP MIMARISI TARTIŞMASI (21 Nisan 2026)
**Kararlar plans/'a evrildi:**
- Karar 1 (SP'den vazgeçme?) → **Hibrit, SP kal.** ADR-001 (`docs/ADR/001-data-access.md`) yazıldı: rapor/dashboard data = SP, app metadata = EF Core.
- Karar 2 (sp_PdksPano parçalama) → **Inline TVF + orkestrator SP.** Aktif madde olarak yukarıda "SP mimarisi · sp_PdksPano → inline TVF refactor" başlığında. ADR-004 adayı.

Detay analiz (kazanımlar/dikkat/anti-pattern listesi) gerekirse git history'de `TODO.md` 21 Nisan revizyonuna bak.

### BUG: SP Önizle handler bağlı değil (21 Nisan 2026)
✅ **Çözüldü** — F-01 / commit `07f4b91`. `initSpHelpers()` outer IIFE'den çıkarıldı, top-level IIFE oldu.

### MIMARI TUTARSIZLIKLAR audit (21 Nisan 2026)
Çoğu kapandı (yukarıda). Kalan düşük-öncelik maddeler "BACKLOG → Mimari tutarsızlıklar" bölümünde.
