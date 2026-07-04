# Plan 42 — Process Execution Runtime

**Tarih:** 2026-05-21
**Yazan:** Fikri / Claude
**Durum:** `Taslak` (onay bekliyor) · ⚠️ **REVİZYON ŞART (2026-07-04, Plan 57 §4.5 Council Verdict):** ProcessInstance İKİNCİ state machine OLAMAZ. `IProcessExecutionService` kendi başlat/ilerlet/kapat + `ProcessInstanceTransition` state-log İPTAL → ProcessInstance = ince VAKA KONTEYNERİ (türetilmiş status + EntityRelations timeline), "ilerlet" = `IWorkflowService.AdvanceAsync`'e delege. Timer = mevcut WorkflowEngine delay-step + Hangfire (yeni TimerStep yok). Tek motor = WorkflowEngine; ProcessInstance onu WRAP eder, duplicate etmez. Bu revizyon yapılmadan onaya girmez. Detay: [`plans/57-forms-workflow-bridge.md`](57-forms-workflow-bridge.md) §4.5.
**Bağımlılık:** Plan 38 EntityRelations (✅), Plan 36 Workflow Engine (✅), Plan 40 KVKK Process tanım (Taslak), Plan 41 Form Builder (Taslak — kritik prereq), Plan 34 SOP (Taslak), Plan 31 SMTP (✅)
**Konumu:** vNext kalbinin son birleştirici taşı. SOP + Comment + Workflow + Form + KVKK = bu plan ile **portal execution platform** olur.

---

## 1. Problem

Kullanıcı netleştirmesi (2026-05-21):

> "süreç sonucunda workflow ile formlar vs vs"
> "bu süreçlerin ve kvkk kısımlarının tamamının işleyişi formları akışı mümkün olduğunca portal üstünden olmalı"

Plan 40 KVKK Process'i **tanım** (KVİE template) seviyesinde kuruyor. Plan 41 Form Builder **form altyapısını** veriyor. Plan 36 Workflow Engine **state machine'i** veriyor. Ama **kim tetikler, kim takip eder, sonuç ne?** yok.

**Eksik katman:** `ProcessInstance` — bir Process'in N defa execute edildiği vaka. Her vaka:

- Hangi form'lar dolduruldu
- Hangi workflow adımında
- Kim üstünde (atanan)
- Hangi dokümanlar üretildi
- Hangi KVKK DataElement'leri işlendi
- Hangi kararlar verildi (Plan 38 DecisionLog)
- Audit timeline

Bu eksiklik kapanmadan **"portal üzerinden işleyiş"** yarım kalır.

**Senaryolar (Plan 40 + 41 birleşimi):**

| Tetikleyici | Process | ProcessInstance hayatı |
|---|---|---|
| Public link tıklanır | "Aday Havuzu" | Form submit → Instance create → İK ön eleme → mülakat planla → karar → arşivle |
| Anonim portal | "İhbar Yönetimi" | Şifreli form submit → Instance create → İhbar Komitesi inbox → soruşturma → kapatma |
| Vatandaş başvuru | "DSAR (m.13)" | Public form + kimlik → Instance + 30-gün timer → KVKK Sorumlusu inceleme → cevap maili |
| Çalışan portal | "Veri İhlali Bildirim (m.12)" | Form + dosya → Instance + 72h timer → KVKK Sorumlusu → Kurul taslak |
| Yıllık review | "KVKK Envanter Review" | Birim müdürü inbox → her Process satırını onayla → VERBİS hazır flag |
| Otomatik (Hangfire) | "Süresi dolan veri imha" | Process tetikler → 6-aylık imha job → tutanak üret → KVKK Sorumlusu arşivle |
| Çalışan portal | "Eğitim Katılım" | Form + quiz → Instance → 80% pass → sertifika PDF üret → DocumentLink + KVKK eğitim kaydı |

**Her instance bir audit + KVKK + EntityRelations otomatik kayıt zinciri üretir.** Excel manuel kayıt biter.

---

## 2. Scope

### Kapsam dahili

**Çekirdek:**
- `ProcessInstance` entity — Process'ten N kez execute
- `ProcessInstanceAspect` — instance'a bağlı 6 aspect (Form / Workflow / Document / DecisionLog / Audit / KvkkContext)
- `ProcessInstanceTransition` — state geçiş log (Plan 36 reuse)
- `IProcessExecutionService` — başlat / ilerlet / iptal / kapat
- `ProcessTrigger` — manuel (UI) / public link / cron (Hangfire) / event (başka instance tamamlanınca)

**UI:**
- **"Süreç Başlat"** — `/Process/Start/{slug}` admin/kullanıcı tetikler
- **Public başlatma** — `/Process/Public/{slug}/{token}` anonim (DSAR/ihbar/aday)
- **Instance Detail** — 6 aspect timeline view (form data + workflow position + decisions + documents + audit + KVKK)
- **"Süreçlerim" Inbox** — atanan + başlattığım + bekleyen aksiyon
- **Admin "Tüm İnstance'lar"** — filter (status, atanan, tarih, process) + export
- **Süreç sahibi dashboard** — kendi sürecinde aktif instance'lar + SLA aşımı uyarıları

**Sonuç üretimi:**
- PDF/Word template engine (ClosedXML kullanıyoruz Excel için, **PDF için QuestPDF** veya **DocumentFormat.OpenXml** Word)
- Template'e instance data inject (form values + workflow decisions)
- Documents modülüne otomatik arşiv
- Email gönderim (Plan 31 SMTP) — DSAR cevabı, sertifika, vs

**Workflow entegrasyonu:**
- Workflow step tamamlanınca callback → ProcessInstance state güncelle
- Step "Form Doldur" tipi — kullanıcı atanır + form link gönderilir
- Step "Karar Ver" tipi — onay/red + DecisionLog
- Step "Sonuç Üret" tipi — template + Documents arşiv
- Step "Bildir" tipi — INotificationService callback

**KVKK entegrasyonu:**
- Her instance başlatınca → ilgili Process'in DataElement'leri otomatik kayıt (`KvkkProcessingActivity` log)
- Saklama süresi timer otomatik başlar (Process.RetentionRule)
- Saklama bitince Hangfire job → imha + tutanak üret + İmha Process tetikle (recursion)

**ICS Calendar feed:**
- `/Process/Calendar.ics?token=...` — atananın süreçleri Outlook/Google Calendar abone
- SLA timer'lar → takvim event

**EntityRelations (Plan 38):**
- `ProcessInstance` ↔ `Process` (RelationType: `instanceOf`)
- `ProcessInstance` ↔ `FormSubmission` (RelationType: `produces`)
- `ProcessInstance` ↔ `WorkflowInstance` (RelationType: `executedBy`)
- `ProcessInstance` ↔ `Document` (RelationType: `documents`)
- `ProcessInstance` ↔ `DecisionLog` (RelationType: `decidedBy`)
- `ProcessInstance` ↔ `User` (RelationType: `assignedTo`)
- `ProcessInstance` ↔ `User` (RelationType: `initiatedBy`)

### Kapsam dışı

- **E-imza yasal** (KEP, e-İmza Türkiye) — manuel scan upload v1, e-imza ileride
- **Real-time co-editing** (multiple users aynı anda) — kapsam dışı
- **Versiyon snapshot** — instance immutable kayıt; revize yeni instance
- **Cross-firma instance transfer** — kapsam dışı (multi-tenant izole)
- **AI assisted form doldurma** — kapsam dışı v1 (Plan 16.5 AI Core sonrası)
- **Process kütüphanesi marketplace** — Plan 40 seed yeterli
- **Görsel BPMN diyagram** — Plan 36 sequential-workflow-designer yeterli, BPMN overkill

### Etkilenen dosyalar

```
Mosaik.Modules.ProcessRuntime/                   (yeni csproj)
├── Mosaik.Modules.ProcessRuntime.csproj
├── ProcessRuntimeModule.cs                      (IMosaikModule)
├── Areas/Process/
│   ├── Controllers/
│   │   ├── ProcessExecutionController.cs        (Start, Cancel, Complete)
│   │   ├── PublicProcessController.cs           (anonim start)
│   │   ├── InstanceController.cs                (Index "Süreçlerim", Detail, Admin filter)
│   │   ├── ProcessCalendarController.cs         (ICS feed)
│   │   └── ResultRenderingController.cs         (PDF/Word üret + Download)
│   ├── Views/
│   │   ├── Execution/{Start,Started,Cancelled}.cshtml
│   │   ├── Public/{Start,Submitted,Expired}.cshtml
│   │   ├── Instance/{Index,Details,AssignedToMe,InitiatedByMe}.cshtml
│   │   └── Admin/{AllInstances,ProcessOwnerDashboard}.cshtml
│   └── ViewModels/
├── Entities/
│   ├── ProcessInstance.cs
│   ├── ProcessInstanceAspect.cs                 (polymorphic — Form/Workflow/Doc/Decision/Audit/KvkkContext)
│   ├── ProcessInstanceTransition.cs             (state log)
│   ├── ProcessTriggerLog.cs                     (kim başlattı, neden)
│   └── ProcessSlaTimer.cs                       (30-gün DSAR, 72h breach, custom)
├── Services/
│   ├── ProcessExecutionService.cs               (Start/Advance/Cancel/Complete)
│   ├── ProcessInstanceQueryService.cs           (Inbox, dashboard)
│   ├── ProcessSlaTimerService.cs                (Hangfire timer)
│   ├── ResultRenderingService.cs                (PDF/Word template inject)
│   ├── ProcessTriggerListener.cs                (cron + event)
│   └── KvkkProcessingActivityLogger.cs          (Plan 40 DataElement kayıt)
└── Database/
    ├── 72_ProcessRuntimeSchema.sql
    └── 73_ProcessRuntimeSeed.sql               (DSAR/İhbar/Aday/İhlal template instance pattern'leri)
```

**Tahmini boyut:** ~30-40 dosya, ~2500-3500 satır C# + ~500 satır JS + ~300 satır CSS.

---

## 3. Alternatifler

### A: Tek `ProcessInstance` + polymorphic Aspect (SEÇİLEN)

`ProcessInstance` (1 vaka) + `ProcessInstanceAspect { InstanceId, AspectType, AspectId }` polymorphic. Form/Workflow/Doc/Decision/Audit/Kvkk her biri ayrı entity, aspect tablosu ile bağlı.

**Sebep:** EntityRelations pattern (Plan 38) ile tutarlı. Yeni aspect tipi eklenince schema değişmez (örnek: ileride "Approval Comment" eklenir → AspectType = 'Comment').

### B: Her aspect için ProcessInstance üstünde direkt FK

**Reddetme:** Schema rigid — yeni aspect = migration. Genericlik kaybı.

### C: Workflow Engine her şeyi taşır (ProcessInstance ayrı entity yok)

**Reddetme:** Workflow = state machine, **Process Instance = vaka kimliği**. Workflow değişebilir (revize), aynı instance hâlâ var. Karıştırma yanlış soyut katman.

### OSS reuse stack (2026-05-21 araştırma — `docs/RESEARCH_OSS_VNEXT_2026-05-21.md`)

| Bileşen | OSS | Lisans | Faz | Tasarruf |
|---|---|---|---|---|
| State machine | **Stateless 5.20.1** (Apache-2.0, ~700 satır) | Apache-2.0 | Faz 1 (Plan 36 reuse) | %30 hookup (-3-5h) |
| ProcessInstance entity + 6 aspect | **DIY** (Mosaik native) | İç | Faz 0 | 0 — OSS BPM engine reddedildi |
| Timeline UI (Instance Detail 6 aspect) | **vis-timeline 8.5.1** (Apache/MIT, vanilla) | Apache/MIT | Faz 3 | ~%50 (-12-16h) groups API ile 6 aspect ayrı row |
| ICS feed (Outlook/Google abone) | **Ical.Net 5.2.2** (MIT, ical-org canonical) | MIT | Faz 7 | ~%80 (-8-12h) RFC 5545 + RRULE + NodaTime TZ |
| Result PDF (DSAR + sertifika + KVKK rapor) | **Gotenberg 8.32 Docker + Gotenberg.Sharp.API.Client 3.0.0 + RazorLight/Razor.Templating.Core** (MIT + Apache 2.0) — Razor view → HTML → headless Chromium | MIT/Apache | Faz 5 | ~%70 (12h→6h) Razor template Mosaik UI reuse Tailwind print CSS |
| PDF in-process fallback + digital signature + bulk | **PdfSharp + MigraDoc 6.2.4** (MIT) — PKCS#7 native | MIT | Faz 5 | Gotenberg down fallback + 1000+ doc bulk + signature şart DSAR |
| QR code (sertifika) | **QRCoder 1.8.0** (MIT) → PNG data-URI Razor embed | MIT | Faz 5 | ZXing.Net yerine, daha hafif |
| Result template (VERBİS taahhütname Word) | **DocumentFormat.OpenXml 3.5.1** (MIT, Microsoft) + placeholder helper | MIT | Faz 5 | ~%40 (-2-3h) placeholder regex replace |
| Result template (İhbar soruşturma raporu Word) | **OfficeIMO.Word 1.0.34** (MIT, fluent OpenXml wrapper) | MIT | Faz 5 | ~%60 (-3-4h) cover+TOC+section |
| Result template (Liquid sandbox — opsiyonel) | **Scriban 7.2.0+** (BSD-2, AOT-safe) | BSD-2 | Faz 5 | Gotenberg+Razor yeterli, Scriban yedek niş |
| KPI/chart PDF (Faz 6+) | **Playwright .NET** Razor→Chromium | MIT | Faz 8 (opsiyonel) | $0 |
| Excel raporlar | **ClosedXML** (mevcut, MIT) | MIT | Faz 8 | 0 (korunsun) |
| Notification | Plan 31 SMTP + Plan 37 inbox DIY (Novu reddedildi) | İç | Faz 4 | 0 (Novu = Node+Postgres+Redis+MongoDB+RabbitMQ ek altyapı) |
| BPMN visual designer | ❌ **bpmn-js ERTELE** — Plan 42 polymorphic aspect modelini kısıtlar | — | — | — |
| State machine v2 | ❌ **xstate OPSIYONEL** — Stateless ile fark az | — | — | — |
| Process discovery | ❌ **Apromore ERTELE** — VISION §7.0.1 ileride ayrı plan | — | — | — |

**Reddedilenler:** Elsa 3.6 (designer embed yok, ProcessInstance ile entity çakışma), Camunda 8 (multi-component + lisans), Temporal (Cassandra+ES SRE), Workflow Core (yarı aktif + designer yok), Novu (5 ek container), iText (AGPL), Aspose ($1175+/dev), DocX Xceed ($852+), Spire (free <500 sayfa), EPPlus 7 (Polyform Non-commercial), Wiki.js (AGPL), Outline (BSL), ProcessMaker (AGPL+PHP), Flowable/Bonita/Camunda 7 (JVM), **QuestPDF Professional (~$699/yıl rev 1'de önerildi, rev 2'de reddedildi — Razor reuse yok + DSL öğrenme borcu + digital signature yok)**, Carbone CCL (third parties yasağı), DinkToPdf/wkhtmltopdf (2023'te arşivlendi + CVE patch'siz), Spire.PDF Free (10 sayfa limit), HiQPdf Free (5 sayfa limit).

**Yıllık lisans:** **$0** (rev 2 — Gotenberg + MigraDoc + QRCoder hepsi MIT/Apache/BSD). 3 yıl $2097 tasarruf (QuestPDF Pro'ya karşı).

**Net Plan 42 effort tasarrufu: ~%25** (~84h → ~64h).

### Lens kontrolü

- 🔴 **Contrarian:** ProcessInstance + WorkflowInstance + FormSubmission = 3 tablo, gereksiz mi? **Cevap:** Her birinin life-cycle farklı. WorkflowInstance Plan 36 mevcut. FormSubmission Plan 41 mevcut. ProcessInstance bunları **iş anlamlı vaka** olarak birleştirir.
- 🔵 **First Principles:** Sorun "süreç çalıştığında ne oluyor?" — kayıt + atama + sonuç. ProcessInstance bunu kavramsal olarak ifade eder.
- 🟢 **Expansionist:** ProcessInstance KVKK ile sınırlı değil — SOP execution, Tamim onay, sözleşme imza, satın alma talep hepsi instance üretir. Tek omurga.
- ⚪ **Outsider:** Bir BKM çalışanı "Süreçlerim" sayfasında açık vakalar görür: 3 DSAR cevap bekliyor + 1 ihbar inceleme + 2 envanter review. Bu iş listesi. Önceden Excel + email + Slack idi; artık tek yer.
- 🟡 **Executor:** Pazartesi sabahı Faz 0 — `ProcessInstance` entity + Migration 72 + en basit Start/View. 4-6 saat.

---

## 4. Mimari

### 4.1 Veri modeli

```sql
CREATE TABLE ProcessInstances (
    Id INT IDENTITY PK,
    InstanceCode NVARCHAR(40) NOT NULL UNIQUE,    -- "PI-20260521-00001" insan dostu
    FirmaId INT NOT NULL,
    ProcessId INT NOT NULL FK Processes,          -- Plan 40 KVKK Process tanım
    Status TINYINT NOT NULL DEFAULT 0,            -- 0 Active 1 Completed 2 Cancelled 3 Failed 4 Suspended
    Priority TINYINT NOT NULL DEFAULT 1,          -- 0 Low 1 Normal 2 High 3 Urgent
    Title NVARCHAR(300) NOT NULL,                 -- "Aday başvuru — Ahmet Yılmaz" otomatik veya form
    InitiatedById INT NULL FK Users,              -- NULL ise anonim/cron
    InitiatorEmail NVARCHAR(200) NULL,            -- anonim için
    InitiatorIp NVARCHAR(45) NULL,
    AssignedToId INT NULL FK Users,               -- current step assignee
    AssignedToRole NVARCHAR(80) NULL,             -- role-based (e.g. "KVKK Sorumlusu")
    DueAt DATETIME2 NULL,                         -- SLA timer
    StartedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2 NULL,
    ResultCode NVARCHAR(40) NULL,                 -- "Approved", "Rejected", "Closed"
    ResultDocumentId INT NULL FK Documents,       -- sonuç PDF/Word
    SourceTrigger NVARCHAR(40) NOT NULL,          -- "manual" / "public" / "cron" / "event"
    SourceTriggerRefId INT NULL,                  -- public token, cron job, parent instance
    INDEX IX_PI_FirmaStatus (FirmaId, Status),
    INDEX IX_PI_Assigned (AssignedToId, Status, DueAt),
    INDEX IX_PI_Initiator (InitiatedById, StartedAt),
    INDEX IX_PI_Process (ProcessId, Status)
);

CREATE TABLE ProcessInstanceAspects (
    Id INT IDENTITY PK,
    ProcessInstanceId INT NOT NULL FK,
    AspectType TINYINT NOT NULL,                  -- 0 Form 1 Workflow 2 Document 3 Decision 4 Audit 5 KvkkContext 6 Comment 7 SubInstance
    AspectId INT NOT NULL,                        -- polymorphic (FormSubmission.Id, WorkflowInstance.Id, Document.Id, DecisionLog.Id, ...)
    RelationLabel NVARCHAR(80) NULL,              -- "initial submission", "approval decision", "result document"
    AddedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    AddedBy INT NULL FK Users,
    INDEX IX_PIA_Instance (ProcessInstanceId, AspectType),
    INDEX IX_PIA_Aspect (AspectType, AspectId)    -- reverse: bu form/workflow/doc hangi instance'a bağlı?
);

CREATE TABLE ProcessInstanceTransitions (
    Id INT IDENTITY PK,
    ProcessInstanceId INT NOT NULL FK,
    FromStatus TINYINT NOT NULL,
    ToStatus TINYINT NOT NULL,
    FromAssignedToId INT NULL,
    ToAssignedToId INT NULL,
    StepName NVARCHAR(200) NULL,                  -- workflow step adı
    Reason NVARCHAR(MAX) NULL,                    -- karar gerekçesi
    TransitionedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TransitionedBy INT NULL FK Users,
    INDEX IX_PIT_Instance (ProcessInstanceId, TransitionedAt)
);

CREATE TABLE ProcessSlaTimers (
    Id INT IDENTITY PK,
    ProcessInstanceId INT NOT NULL FK,
    TimerType NVARCHAR(40) NOT NULL,              -- "dsar_30day", "breach_72h", "custom"
    StartsAt DATETIME2 NOT NULL,
    DueAt DATETIME2 NOT NULL,
    HangfireJobId NVARCHAR(80) NULL,
    Status TINYINT NOT NULL DEFAULT 0,            -- 0 Active 1 Triggered 2 Cancelled 3 Resolved
    INDEX IX_PST_Active (Status, DueAt)
);

CREATE TABLE ProcessTriggerLogs (
    Id INT IDENTITY PK,
    ProcessId INT NOT NULL FK,
    ProcessInstanceId INT NULL FK,                -- başarılı ise instance
    TriggerType NVARCHAR(40) NOT NULL,
    TriggerSource NVARCHAR(200) NULL,             -- IP, user, cron name
    Payload NVARCHAR(MAX) NULL,                   -- form data snapshot
    Status TINYINT NOT NULL,                      -- 0 Pending 1 Started 2 Failed
    Error NVARCHAR(MAX) NULL,
    TriggeredAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

### 4.2 Yaşam döngüsü

```
Trigger             Status      Workflow              Aspects
─────────────────────────────────────────────────────────────
1. Tetiklenme        Active     Started (Step 1)      Form (initial submission)
                                                       Audit (start)
                                                       KvkkContext (DataElement log)
                                                       EntityRelations.Add(process_instance → form_submission)
2. İlerleyiş         Active     Step 2 → Step 3       Audit (step transition)
                                                       DecisionLog (her onay)
                                                       Document (ek dosyalar)
3. Tamamlanma        Completed  Final step            Document (sonuç PDF)
                                                       Audit (complete)
                                                       Notification (Plan 31 SMTP)
4. İptal             Cancelled  -                     Audit + reason
5. SLA aşımı         Active     Escalation            Notification (eskaltsiyon zinciri)
                                                       Audit (overdue)
```

### 4.3 ProcessTrigger tipleri

| Tip | Tetikleyici | Örnek |
|---|---|---|
| `manual` | Kullanıcı UI'dan `/Process/Start/{slug}` | İK çalışanı "Yeni Aday Ekle" tıklar |
| `public` | Anonim public link | DSAR vatandaş başvurusu |
| `cron` | Hangfire job (Process.RetentionRule sonu) | 6-aylık veri imha tetiklenir |
| `event` | Başka instance tamamlanınca | "Aday işe alındı" → "Özlük dosyası oluştur" instance |
| `api` | Dış sistem webhook (v2) | ERP'den fatura → "Fatura Onay" instance |

### 4.4 Public başlatma akışı

```
1. Admin: PublicFormToken oluştur (FormDefinitionId, ExpiresAt, MaxUses=1)
2. Link gönder: /Process/Public/{slug}/{token}
3. Anonim user form doldurur (Plan 41)
4. POST submit → PublicTokenService validate
5. FormSubmissionService.Save → FormSubmission insert
6. ProcessExecutionService.Start(processId, formSubmission)
7. ProcessInstance create + Aspect (Form)
8. WorkflowEngine.Start (Plan 36)
9. AssignedTo → KVKK Sorumlusu / İK Müdürü
10. Notification → atanana email + InApp
11. KvkkContext log → DataElement işleme kayıt
12. EntityRelations çoklu insert
13. AuditLog
14. Response: anonim user'a "Başvurunuz alındı, kod: PI-20260521-00001"
```

### 4.5 Sonuç üretimi (PDF/Word)

`ResultRenderingService`:

- Template registry: `Database/Templates/` — Word docx with `{{placeholder}}` (DocumentFormat.OpenXml) veya PDF QuestPDF C# code
- Process tanımında `ResultTemplateKey` → template seç
- Instance verisi (form values + workflow decisions + Process metadata) inject
- Documents modülüne `ApplicationName=ProcessResult` ile arşivle
- Email ek olarak gönder (Plan 31 SMTP)

Örnek template'ler:
- DSAR cevap mektubu (KVKK m.13 formal)
- İhbar soruşturma raporu (gizli, sadece İhbar Komitesi)
- Aday başvuru iletişim mektubu (red/kabul)
- Eğitim sertifikası (PDF, QR code with verify link)
- VERBİS uyumluluk taahhütnamesi

### 4.6 SLA Timer + Eskaltsiyon

`ProcessSlaTimerService`:
- ProcessInstance create → Process tanımındaki SLA'lar üzerinden ProcessSlaTimer'lar oluştur
- Hangfire job ScheduleAt = DueAt
- Job tetiklenince: ProcessInstance check, hâlâ Active ise eskaltsiyon zinciri (Plan 31 SMTP + Plan 37 Inbox)

SLA tipleri:
- `dsar_30day` — KVKK m.13 cevap 30 gün
- `breach_72h` — KVKK m.12 ihlal bildirim 72 saat
- `internal_5day` — iç süreç default 5 iş günü
- `custom` — Process tanımında set

Eskaltsiyon zinciri (Plan 37 §7.6 No-Excuse pattern):
- DueAt − 7 gün: assignee email + InApp
- DueAt − 1 gün: assignee + manager
- DueAt + 24h: manager + departman başkanı
- DueAt + 72h: admin

### 4.7 Inbox UI

`/Process/My` — `ProcessInstanceQueryService`:

- **Aksiyonum bekliyor:** AssignedToId=me + Status=Active + DueAt soonest first
- **Başlattıklarım:** InitiatedById=me + Status (filter all/active/completed)
- **Yaklaşan SLA:** DueAt < +7 gün
- **Süreç sahibi olarak:** ProcessOwner=me + tüm aktif instance'lar (overview)

Tablo: Kod / Title / Process / Status badge / Atanan / DueAt (countdown chip) / Last action / Aksiyonlar

Keyboard nav: `j/k` next/prev, `Enter` detail, `Space` toggle done (Plan 37 pattern).

### 4.8 Instance Detail — 6 Aspect Timeline

```
PI-20260521-00001  · DSAR Başvuru — ali@example.com  · Active · KVKK Sorumlusu  · 28 gün kaldı
─────────────────────────────────────────────────────────────────────────────────
[Timeline]
2026-05-21 14:32  📝 Form submit                            ali@example.com (anonim)
                  "İlgili kişi başvurusu" — DSAR formu v2
                  Fields: ad-soyad, tc, email, talep türü, açıklama
                  [Form aspect] → /Forms/Submission/4521
2026-05-21 14:32  🎯 Instance created                        system
                  Process: "DSAR (m.13) Yönetimi"
                  Trigger: public link, IP 81.x.x.x
2026-05-21 14:32  ⏰ SLA timer started                       system
                  DSAR 30 gün → due 2026-06-20
2026-05-21 14:33  📨 Notification sent                       system
                  → KVKK Sorumlusu (email + InApp)
2026-05-21 15:05  👀 Instance viewed                         M. Yılmaz (KVKK Sorumlusu)
2026-05-21 15:12  ✅ Decision: Talep kabul                    M. Yılmaz
                  "Kullanıcı verilerinin silinmesi onaylandı"
                  [Decision aspect] → DecisionLog.Id=782
2026-05-22 09:00  🔄 Workflow step → Veri silme              system
                  Assigned: IT Sorumlusu
2026-05-22 11:45  📎 Document attached                       T. Demir (IT)
                  "User_4521_DeletionEvidence.pdf"
                  [Document aspect]
2026-05-22 11:46  🏁 Instance completed                       T. Demir
                  Result: Veri silindi, cevap mektubu üretildi
                  [Result document] → DSAR_Cevap_20260522.pdf
                  Email gönderildi: ali@example.com
─────────────────────────────────────────────────────────────────────────────────
[Aspect tabs]  Form (1)  ·  Workflow (3 step)  ·  Document (2)  ·  Decision (1)  ·  Audit (8)  ·  KVKK Context (5 DataElement)
```

### 4.9 KVKK Context — Otomatik DataElement kaydı

Process.ProcessDataLinks → her instance başlatıldığında bir snapshot olarak `KvkkProcessingActivity` tablosuna log düşer (Plan 40):

```sql
CREATE TABLE KvkkProcessingActivities (
    Id INT IDENTITY PK,
    ProcessInstanceId INT NOT NULL FK,
    DataElementId INT NOT NULL FK,
    UsageType TINYINT NOT NULL,                   -- collects/stores/transfers/derives
    ProcessedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LegalBasisId INT NOT NULL FK,                 -- snapshot
    RetentionExpiresAt DATETIME2 NOT NULL,        -- imha timer
    AnonymizedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    INDEX IX_KPA_DataElement (DataElementId, ProcessedAt),
    INDEX IX_KPA_Retention (RetentionExpiresAt, DeletedAt)
);
```

Bu tablo:
- KVKK denetim için **hangi veri, ne zaman, hangi sebeple işlendi** kanıtı
- Reverse search "ad-soyad nerelerde işlendi?" — sadece tanım değil **gerçek işlem** seviyesinde
- Hangfire job: RetentionExpiresAt < NOW → otomatik anonimleştirme/silme tetikler (KVKK Mart 2025 rehber zorunluluğu)

---

## 5. Riskler

| Risk | Olasılık | Etki | Önlem |
|---|---|---|---|
| ProcessInstance + Workflow + Form üçlüsü karmaşık | Yüksek | Orta | Aspect tablosu polymorphic = generic, schema rigid değil. UI 6-tab clear separation. |
| Public anonim trigger abuse | Yüksek | Yüksek | Plan 41 rate limit + token expiry + manuel review queue + IP audit |
| SLA timer Hangfire job kayıp | Düşük | Yüksek | DB-backed timer + retry; Hangfire dashboard izleme; failsafe daily sweep |
| Result template inject XSS | Orta | Orta | OpenXml/QuestPDF text-only inject; HTML render yok; sanitize zorunlu |
| Audit timeline performans (1000+ entry) | Düşük | Orta | Pagination + lazy load aspect details |
| Multi-firma izolasyon ihlali | Düşük | Yüksek | `IUserDataScope` her query + FirmaId her tablo |
| KvkkProcessingActivity tablo şişer (her instance 5-10 entry) | Yüksek | Düşük | INDEX + retention-based partitioning; arşiv tablosu sonra |
| Cron trigger sonsuz döngü (instance tamamlanınca event → yeni instance → ...) | Orta | Yüksek | Depth guard (max 3 chain) + audit visualization |
| Anonim DSAR kimlik doğrulama eksik | Orta | Yüksek | Email + telefon zorunlu + token public link + opsiyonel KEP doğrulama |
| Workflow değişikliği aktif instance'ları kırar | Orta | Orta | Instance Workflow snapshot (Plan 36 Faz B) |

---

## 6. Done Criteria

### Faz 0 — Veri modeli + scaffold (6-8h)
- [ ] `Mosaik.Modules.ProcessRuntime` csproj + IMosaikModule
- [ ] Migration 72 schema (idempotent)
- [ ] 5 entity (ProcessInstance, Aspect, Transition, SlaTimer, TriggerLog)
- [ ] DbSet + EF konfigürasyon
- [ ] Build yeşil + 3+ unit test

### Faz 1 — ProcessExecutionService + manuel başlatma (8-10h)
- [ ] `ProcessExecutionService.Start/Advance/Cancel/Complete`
- [ ] `/Process/Start/{slug}` UI — kullanıcı tetikler
- [ ] Workflow Engine hookup (Plan 36 Start callback)
- [ ] EntityRelations çoklu insert
- [ ] AuditLog her aksiyon
- [ ] 6+ unit test

### Faz 2 — Public + Anonim başlatma (6-8h)
- [ ] `/Process/Public/{slug}/{token}` endpoint
- [ ] PublicTokenService entegrasyon (Plan 41 reuse)
- [ ] Anonim instance Title otomatik üretim ("DSAR — ali@example.com")
- [ ] IP audit
- [ ] 4+ unit test

### Faz 3 — Inbox + Detail UI (10-12h)
- [ ] `/Process/My` — 4 sekme (aksiyon bekliyor / başlattıklarım / yaklaşan SLA / süreç sahibi)
- [ ] `/Process/Instance/{code}` — 6 aspect timeline detail
- [ ] Filter + search + sort
- [ ] WCAG focus trap + scope th + aria
- [ ] Keyboard nav (j/k/Enter/Space)
- [ ] 6+ unit test + UI integration test

### Faz 4 — SLA Timer + Eskaltsiyon (8-10h)
- [ ] `ProcessSlaTimerService` Hangfire
- [ ] Eskaltsiyon zinciri (Plan 31 SMTP caller)
- [ ] DSAR 30-gün + Breach 72h + custom timer
- [ ] Daily sweep failsafe (kayıp job için)
- [ ] 6+ unit test (timer trigger, escalation chain)

### Faz 5 — Result Rendering (PDF/Word) (6-8h)
- [ ] `ResultRenderingService` — QuestPDF + DocumentFormat.OpenXml
- [ ] Template registry (5 başlangıç template seed)
- [ ] Documents modülüne otomatik arşiv
- [ ] Email ek olarak gönder (Plan 31)
- [ ] 5+ unit test

### Faz 6 — KvkkProcessingActivity log + Retention Job (6-8h)
- [ ] `KvkkProcessingActivityLogger` — instance start ProcessDataLink snapshot
- [ ] Migration 72 ek (KvkkProcessingActivities tablosu)
- [ ] Hangfire daily retention sweep (RetentionExpiresAt < NOW → anonimleştir/sil)
- [ ] İmha tutanak üret + İmha Process tetikle (recursion)
- [ ] 5+ unit test

### Faz 7 — Cron + Event trigger + ICS feed (4-6h)
- [ ] `ProcessTriggerListener` cron + event
- [ ] Sonsuz döngü guard (max chain 3)
- [ ] ICS feed `/Process/Calendar.ics?token=...` (Plan 37 §7.6 pattern)
- [ ] HMAC token Outlook/Google abone
- [ ] 4+ unit test

### Faz 8 — Process Owner Dashboard + Admin (4-6h)
- [ ] `/Process/Admin/All` — admin tüm instance'lar
- [ ] `/Process/Owner` — süreç sahibi kendi process'lerindeki instance overview
- [ ] Excel export ClosedXML
- [ ] KPI: cycle time, SLA aşım oranı, completion rate (Reports modülü reuse)

### Faz 9 — Plan 40 KVKK + Plan 41 Form + Plan 34 SOP entegrasyon test (6-8h)
- [ ] DSAR end-to-end: public link → form → instance → KVKK Sorumlusu → silme → cevap mektubu
- [ ] İhbar end-to-end: anonim form (şifreli) → İhbar Komitesi → soruşturma → kapatma
- [ ] Aday başvuru end-to-end: public form + CV → İK ön eleme → mülakat → karar → arşiv
- [ ] Veri ihlali end-to-end: çalışan form → 72h timer → KVKK Sorumlusu → Kurul taslak
- [ ] Yıllık review: birim müdürü inbox → her Process onayla → VERBİS hazır

### Genel
- [ ] Build: 0 uyarı 0 hata
- [ ] Test: tüm yeni testler yeşil + 387 mevcut bozulmamış
- [ ] ARCHITECTURE_MAP refresh
- [ ] CLAUDE.md modül listesine `ProcessRuntime` ekle
- [ ] VISION.md §7 entegrasyon notu
- [ ] ADR-021 yazılır: "Process Execution Runtime — ProcessInstance polymorphic aspect omurgası"

---

## 7. Rollback

- Migration 72 idempotent + reverse migration
- `Mosaik.Modules.ProcessRuntime` csproj reference kaldırılırsa modül devre dışı (ADR-002)
- Aktif instance'lar status='Suspended' → KVKK Sorumlusu manuel müdahale gerekir
- Hangfire job'lar BackgroundJob.Delete ile silinebilir

---

## 8. Adımlar

```
Faz 0 — Scaffold (6-8h)
Faz 1 — ProcessExecutionService + manuel (8-10h)
Faz 2 — Public + Anonim (6-8h)
Faz 3 — Inbox + Detail (10-12h)
Faz 4 — SLA + Eskaltsiyon (8-10h)
Faz 5 — Result Rendering (6-8h)
Faz 6 — KvkkProcessingActivity + Retention (6-8h)
Faz 7 — Cron + Event + ICS (4-6h)
Faz 8 — Owner Dashboard + Admin (4-6h)
Faz 9 — E2E entegrasyon test (6-8h)

Toplam: 64-84h, 4-5 hafta
```

---

## 9. Cross-reference

- **Plan 38** — EntityRelations + DecisionLog (omurga, ✅ onaylı)
- **Plan 36** — Workflow Engine (state machine, ✅ onaylı)
- **Plan 40** — KVKK Process tanım + DataElement (taslak)
- **Plan 41** — Form Builder (taslak, kritik prereq)
- **Plan 34** — SOP (taslak)
- **Plan 31** — SMTP (✅ altyapı, caller Plan 42'de tüketici)
- **Plan 37** — Unified Inbox (Plan 42 ProcessInstance büyük IInboxProvider tüketici)
- **Documents** — file ek + result arşiv
- **ADR-002** — Modular monolit
- **ADR-014** — Vanilla IIFE + Alpine
- **ADR-015** — Yeni modül ayrı assembly
- **ADR-018** — IMosaikModule capability (TASLAK, opt-in)
- **ADR-021 (yeni)** — ProcessInstance polymorphic aspect omurgası

---

## 10. Karar gerekiyor

1. **PDF template engine** — QuestPDF (C# kod) vs DocumentFormat.OpenXml (Word docx template) vs ikisi de? Önerim: ikisi de (Word formal yazışma, QuestPDF sertifika/PDF görsel).
2. **Anonim DSAR kimlik doğrulama** — Sadece email + telefon mu, KEP doğrulama da mı zorunlu? Önerim: v1 email+telefon yeterli, KEP v2 (KVKK Mart 2025 rehber öneri).
3. **Cron event döngü guard depth** — Max 3 chain mi yeterli? Önerim: 3 default, FirmaId bazlı config override.
4. **ICS feed token rotasyon** — Token süresi 1 yıl mı, kullanıcı manuel revoke mu? Önerim: 6 ay default + manuel revoke.
5. **Plan 42 sıralama** — Plan 40+41 bittikten sonra mı, Faz 1-3'leri paralel mi? Önerim: Plan 41 Faz 0-3 bitince Plan 42 Faz 0-1 başlar (form altyapı şart). Plan 40 paralel.

---

## 11. Sürüm

- **2026-05-21:** Taslak. Onay bekliyor.


---
> **DÜZELTME 2026-06-29 (Plan 54 / research — KRİTİK):** 6 fiziksel aspect tablosu YERİNE **tek `ProcessActivity` stream** (ActivityType lookup + PayloadJson + RefEntityType/RefEntityId; aspect = projeksiyon WHERE ActivityType=...). Tek-sorgu timeline, 6-yollu UNION yok, modül izolasyonu korunur. Engine: `.NET Stateless` transition guard, persistence EF Core, SLA: DueAt/EscalationLevel + Hangfire sweeper + IBusinessClock. BPMN/Elsa/event-sourcing YOK. Kaynak: docs/RESEARCH_VISION §2.
