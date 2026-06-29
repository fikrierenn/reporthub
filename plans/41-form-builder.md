# Plan 41 — Form Builder

**Tarih:** 2026-05-21
**Yazan:** Fikri / Claude
**Durum:** `Taslak` (onay bekliyor)
**Bağımlılık:** Plan 38 EntityRelations (✅), Plan 36 Workflow Engine (✅ onaylı, başlamadı), Documents (var — file upload reuse)
**Kritik path:** Plan 42 Process Execution Runtime'ın prereq'i. Plan 40 KVKK Faz 3 form aspect typed bağlama için zorunlu.

---

## 1. Problem

Mosaik'te **kullanıcı-doldurulan form** sistemi yok. Mevcut form'lar (Login, Admin CRUD, Tamim oluştur) hard-coded Razor view'lar — her yeni form için Controller + View + ViewModel + Migration yazılır. Şu an dış kullanıma açılabilir public form (DSAR başvuru, ihbar, aday başvuru) imkansız.

**Kullanıcı netleştirmesi (2026-05-21):**

> "bu süreçlerin ve kvkk kısımlarının tamamının işleyişi formları akışı mümkün olduğunca portal üstünden olmalı"

Portal **runtime execution platform** olarak konumlandı. Form Builder bu vizyonun en büyük missing piece. KVKK envanteri (Plan 40), Process Execution Runtime (Plan 42), SOP (Plan 34), Workflow Designer (Plan 36) hepsi form çağırıyor:

| Senaryo | Form ihtiyacı |
|---|---|
| Aday başvuru | Public link + anonim submit + CV upload |
| İhbar (whistleblower) | Public + anonim + şifreli storage |
| DSAR (m.13 ilgili kişi) | Public + kimlik doğrulama + 30 gün SLA timer |
| Veri ihlali bildirimi (m.12) | Çalışan portal + dosya ek + 72h timer |
| Açık rıza yenileme | Çalışan portal + signature + DisclosureNotice link |
| Engelli belgesi yükleme | Çalışan portal + file upload + İSG onay |
| Tedarikçi DPA | Tedarikçi portal + e-signature stub |
| Yıllık envanter review | Birim müdürü inbox + onay + yorum |
| PDKS biyometrik rıza | Yeni işe giriş + alternatif yöntem seçim |
| Eğitim katılım | Çalışan portal + quiz + sertifika üretimi |

**Tek tek hard-coded yapmak imkansız.** Generic form builder gerek.

---

## 2. Scope

### Kapsam dahili

**Çekirdek özellikler:**
- `FormDefinition` — form metadata (ad, açıklama, public/internal, version)
- `FormField` — alan tanımı (type, label, required, validation, order)
- `FormSubmission` — submit edilmiş veri (JSON storage)
- `FormFieldDataElementMap` — KVKK için her field'ı DataElement'e bağlar (zorunlu — Plan 40)
- `FormDefinitionPublishedVersion` — form sürüm versioning

**Field tipleri (v1):**
- text (single line)
- textarea (multi line)
- number (int/decimal)
- date / datetime
- select (dropdown, single)
- multiselect (checkboxes)
- radio
- checkbox (boolean)
- file (Documents modülü reuse, magic byte validation)
- signature (Canvas → PNG base64)
- hidden (system field)
- section header (UI only, not data)

**Doğrulama:**
- Required, MinLength, MaxLength, Min, Max, Regex, Custom validator
- Conditional logic — if field A == X then show field B (v2)

**Public link:**
- `/Form/Public/{slug}/{tokenId}` — kimlik doğrulamasız erişim
- HMAC token (RecipientEmail + ExpiryDate + Salt)
- AntiSpam: honeypot + rate limit + reCAPTCHA opsiyonel (v2)

**Anonim submit:**
- IP log (KVKK m.5/2/f meşru menfaat — şüpheli işlem)
- Şifreli storage (ihbar formu için Field-level encryption)
- Submitter identity opsiyonel (DSAR için email + telefon zorunlu)

**Workflow trigger:**
- Form submit → `ProcessInstance` oluştur (Plan 42)
- `WorkflowInstance` başlat (Plan 36)
- INotificationService callback

**Submission UI:**
- Çalışan inbox: doldurulması beklenen + submit ettiklerim
- Admin view: tüm submission'lar, filter + export
- Re-submission edit (DSAR cevap düzeltme için — admin only)

**EntityRelations entegrasyonu (Plan 38):**
- `FormDefinition` ↔ `Process` (RelationType: `consumes`)
- `FormSubmission` ↔ `ProcessInstance` (RelationType: `produces`)
- `FormField` ↔ `DataElement` (RelationType: `maps`)

### Kapsam dışı

- **Drag-drop visual builder UI** — v2 (Plan 41.2). v1 sadece JSON config + admin form.
- E-signature dijital sertifika (e-imza yasal) — manuel imza + scan upload yeterli v1
- Payment field (Stripe/iyzico) — kapsam dışı, ileride
- Multi-page wizard form — v2 (v1 tek sayfa)
- Form sürüm karşılaştırma diff view — v2
- Form template marketplace — v3+
- A/B testing form variantları — kapsam dışı
- Mobile native form (PWA optimize sadece responsive)

### Etkilenen dosyalar

```
Mosaik.Modules.Forms/                           (yeni csproj — ADR-015)
├── Mosaik.Modules.Forms.csproj
├── FormsModule.cs                              (IMosaikModule)
├── Areas/Forms/
│   ├── Controllers/
│   │   ├── FormDefinitionController.cs         (admin CRUD)
│   │   ├── FormController.cs                   (kullanıcı doldurur)
│   │   ├── PublicFormController.cs             (anonim/public)
│   │   ├── SubmissionController.cs             (admin submission liste)
│   │   └── FormBuilderApiController.cs         (JSON CRUD — v2 builder backend)
│   ├── Views/Definition/{Index,Edit,Details}.cshtml
│   ├── Views/Form/{Render,Submitted}.cshtml
│   ├── Views/Public/{Render,Submitted,Expired}.cshtml
│   ├── Views/Submission/{Index,Details}.cshtml
│   └── ViewModels/
├── Entities/
│   ├── FormDefinition.cs
│   ├── FormField.cs
│   ├── FormSubmission.cs
│   ├── FormSubmissionFieldValue.cs
│   ├── FormFieldDataElementMap.cs
│   ├── PublicFormToken.cs
│   └── FormDefinitionVersion.cs
├── Services/
│   ├── FormRendererService.cs                  (JSON → HTML render)
│   ├── FormValidationService.cs                (server-side validate)
│   ├── FormSubmissionService.cs                (save + workflow trigger)
│   ├── PublicTokenService.cs                   (HMAC + expiry)
│   ├── FormEncryptionService.cs                (field-level encrypt for ihbar)
│   └── DataElementMapValidator.cs              (KVKK: her field DataElement'e bağlı mı?)
└── Database/
    ├── 70_FormsSchema.sql
    └── 71_FormsSeedTemplates.sql              (DSAR/İhbar/Engelli/Rıza/Review başlangıç template'ler)
```

**wwwroot** assetler:
```
wwwroot/assets/js/form-builder/
├── form-render.js               v1 — JSON → DOM (vanilla IIFE + Alpine)
├── form-validate.js             client-side validation
├── form-signature.js            Canvas signature pad
├── form-file-upload.js          Documents endpoint upload
└── form-conditional.js          v2 — if/then field visibility

wwwroot/assets/css/components-forms.css        modül-özel CSS
```

**Tahmini boyut:** ~35-45 dosya, ~3000-4000 satır C# + ~1500 satır JS + ~600 satır CSS.

---

## 3. Alternatifler — Own vs Integrate vs Hybrid

### A: Hybrid (SEÇİLEN — server-side v1, builder UI v2)

**v1 (4 hafta):** JSON config-driven server-side render. Admin form ile `FormField` CRUD. Drag-drop builder yok — alan tek tek eklenir. KVKK ihtiyacının %70'i karşılanır.

**v2 (sonra ~2 hafta):** Vanilla JS drag-drop builder UI. JSON şema ile uyumlu — v1 form'lar otomatik geçer.

**v3 (ileride):** Conditional logic + multi-page + advanced validation.

**Sebep:**
- KVKK Plan 40 ve Process Execution Plan 42 bekliyor. v1 yetiyor.
- Own kontrol = DataElement mapping native + audit hooks tam + multi-firma izole + Türkçe UI + KVKK uyum
- Builder UI ertelenebilir; KVKK senaryoları admin form ile çözülür
- Risk düşük — incremental

### B: Own (Mosaik full builder, drag-drop dahil)

**Reddetme:** 6-8 hafta. Kritik path Plan 40+42'yi geciktirir. v1 hybrid yeterli, v2 ile aynı yere varır.

### C: Integrate (Formbricks / LimeSurvey self-host)

**Reddetme:**
- Formbricks: open-source ama enterprise feature (multi-tenant, SSO) kilitli
- LimeSurvey: PHP stack, .NET portal ile entegrasyon iframe + webhook
- DataElement mapping custom plugin yazımı (Plan 40 KVKK için kritik)
- Audit hooks zayıf — KVKK denetim için yetersiz
- Multi-firma izole çözüm yok (BKM Kitap + Belinza ayrı veri)
- Türkçe UI eksik
- Embed iframe Mosaik design system bozuluyor
- Toplam effort ~3-4 hafta integrate + 2 hafta KVKK adapt = own v1 ile aynı

### OSS reuse stack (2026-05-21 araştırma sonrası — `docs/RESEARCH_OSS_VNEXT_2026-05-21.md`)

| Bileşen | OSS | Lisans | Konum | Tasarruf |
|---|---|---|---|---|
| Renderer | **SurveyJS Form Library 3.x** (MIT) | MIT | `wwwroot/lib/surveyjs/` UMD | %70 (~12h → 4h) — JSON schema + `Serializer.addProperty()` ile KVKK DataElement custom metadata |
| Builder UI | **Mosaik DIY** (Survey Creator £422/yıl ticari değil) | İç | Plan 41 Faz B-C drag-drop | 0 (kapsamda kalır, ~30h korunur) |
| Signature pad | **signature_pad 5.1.1** (MIT, ~5KB UMD) | MIT | `wwwroot/assets/js/form-signature.js` IIFE wrapper | %85 (~6h → 1h) |
| AntiSpam (zorunlu) | **Honeypot + time-based** (5 satır pattern) | Pattern | controller side | %50 (~4h → 2h) |
| AntiSpam (opsiyonel public) | **Cloudflare Turnstile** SaaS ücretsiz | SaaS | DSAR/ihbar yüksek risk endpoint | KVKK-friendly, "no PII collection" |
| DataElement KVKK metadata | Plan 40 entegrasyon, **kapsamda kalır** | İç | Plan 41 Faz D | 0 |
| Workflow trigger | Plan 36 Stateless callback | İç | Plan 41 Faz G | 0 |

**Reddedilenler:** Formbricks (AGPLv3 viral), LimeSurvey (GPL+PHP), formio.js (OSL-3.0), Survey Creator (£422/dev/yıl ticari), reCAPTCHA v3 (KVKK riski — Google), Blazor formlar (stack uyumsuz), kevinchappell/formBuilder (jQuery), json-editor/Alpaca (Bootstrap/jQuery UI legacy), Tripetto (proprietary), Syncfusion (proprietary).

**Net Plan 41 effort tasarrufu: ~%30-35** (~62h → ~42h).

### Lens kontrolü

- 🔴 **Contrarian:** v1 drag-drop builder yok = admin form yorucu. **Cevap:** v1 BKM iç kullanım için 8-10 form template seed (DSAR, ihbar, başvuru, rıza, engelli, vs) — bunlar admin form ile yapılır, son kullanıcı bir form template seçer + doldurur.
- 🔵 **First Principles:** Asıl ihtiyaç **runtime form doldurma + submission storage + workflow trigger + DataElement mapping**. Builder UI cosmetic. v1 yeterli.
- 🟢 **Expansionist:** Form Builder sadece KVKK değil — eğitim quiz, müşteri memnuniyet anketi, satın alma talep formu, vs. v2 builder ile expand edilir.
- ⚪ **Outsider:** Bir BKM çalışanı "engelli belgesi yükle" linki tıklar, form açılır, doldurur, submit eder, "İSG'ye gitti" mesajı görür. Bu yeterli. Drag-drop builder konsantrasyon dağıtır.
- 🟡 **Executor:** Pazartesi sabahı Faz 0 — `Mosaik.Modules.Forms` csproj scaffold + `FormDefinition`/`FormField` entity + Migration 70. 4-6 saat.

---

## 4. Mimari

### 4.1 Veri modeli

```sql
CREATE TABLE FormDefinitions (
    Id INT IDENTITY PK,
    FirmaId INT NOT NULL,                          -- multi-tenant
    Slug NVARCHAR(120) NOT NULL UNIQUE,            -- "dsar-basvuru", "ihbar-formu"
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Category NVARCHAR(80) NULL,                    -- "KVKK", "İK", "Müşteri", "İç süreç"
    IsPublic BIT NOT NULL DEFAULT 0,
    IsAnonymous BIT NOT NULL DEFAULT 0,            -- anonim submit izin (ihbar)
    IsEncrypted BIT NOT NULL DEFAULT 0,            -- field-level encrypt (ihbar)
    Status TINYINT NOT NULL DEFAULT 0,             -- 0 Taslak 1 Yayında 2 Arşiv
    Version INT NOT NULL DEFAULT 1,
    LinkedProcessId INT NULL FK Processes,         -- Plan 40 KVKK Process
    TriggersWorkflowId INT NULL FK WorkflowDefs,   -- Plan 36 trigger
    CreatedBy INT NOT NULL FK Users,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    INDEX IX_FormDef_Slug (Slug),
    INDEX IX_FormDef_Status (FirmaId, Status)
);

CREATE TABLE FormFields (
    Id INT IDENTITY PK,
    FormDefinitionId INT NOT NULL FK,
    [Order] INT NOT NULL,
    FieldKey NVARCHAR(80) NOT NULL,                -- "ad_soyad", "tc_kimlik"
    Label NVARCHAR(300) NOT NULL,
    HelpText NVARCHAR(500) NULL,
    FieldType TINYINT NOT NULL,                    -- 0 text 1 textarea 2 number ... 10 signature 11 hidden 12 section
    IsRequired BIT NOT NULL DEFAULT 0,
    ValidationRules NVARCHAR(MAX) NULL,            -- JSON: { minLength, maxLength, regex, custom }
    Options NVARCHAR(MAX) NULL,                    -- JSON: select/radio/checkbox seçenekler
    ConditionalLogic NVARCHAR(MAX) NULL,           -- v2 — JSON: { showIf: [{ fieldKey, op, value }] }
    DefaultValue NVARCHAR(MAX) NULL,
    Placeholder NVARCHAR(200) NULL,
    UNIQUE (FormDefinitionId, FieldKey)
);

CREATE TABLE FormFieldDataElementMaps (
    Id INT IDENTITY PK,
    FormFieldId INT NOT NULL FK,
    DataElementId INT NOT NULL FK,                 -- Plan 40 KVKK
    UsageType TINYINT NOT NULL DEFAULT 0,          -- 0 collects 1 derives
    Notes NVARCHAR(500) NULL,
    UNIQUE (FormFieldId, DataElementId)
);

CREATE TABLE FormSubmissions (
    Id INT IDENTITY PK,
    FormDefinitionId INT NOT NULL FK,
    FirmaId INT NOT NULL,
    SubmittedBy INT NULL FK Users,                 -- NULL ise anonim
    SubmitterEmail NVARCHAR(200) NULL,             -- anonim için iletişim
    SubmitterPhone NVARCHAR(40) NULL,
    SubmitterIp NVARCHAR(45) NULL,                 -- audit
    SubmitterUserAgent NVARCHAR(500) NULL,
    PublicTokenId INT NULL FK PublicFormTokens,    -- public link ile gelmişse
    Status TINYINT NOT NULL DEFAULT 0,             -- 0 Submitted 1 InReview 2 Completed 3 Rejected
    LinkedProcessInstanceId INT NULL FK,           -- Plan 42 ProcessInstance
    WorkflowInstanceId INT NULL FK,                -- Plan 36
    SubmittedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    INDEX IX_Sub_Form (FormDefinitionId, SubmittedAt),
    INDEX IX_Sub_User (SubmittedBy, SubmittedAt)
);

CREATE TABLE FormSubmissionFieldValues (
    Id INT IDENTITY PK,
    FormSubmissionId INT NOT NULL FK,
    FormFieldId INT NOT NULL FK,
    FieldKey NVARCHAR(80) NOT NULL,                -- denormalized for query
    ValueText NVARCHAR(MAX) NULL,                  -- text/textarea/select/etc
    ValueNumber DECIMAL(18,4) NULL,
    ValueDate DATETIME2 NULL,
    ValueBool BIT NULL,
    ValueFileId INT NULL FK Documents,             -- file upload
    IsEncrypted BIT NOT NULL DEFAULT 0,            -- ihbar formu için
    INDEX IX_FSFV_Submission (FormSubmissionId, FieldKey)
);

CREATE TABLE PublicFormTokens (
    Id INT IDENTITY PK,
    FormDefinitionId INT NOT NULL FK,
    TokenHash VARBINARY(64) NOT NULL UNIQUE,       -- HMAC-SHA256
    RecipientEmail NVARCHAR(200) NULL,
    ExpiresAt DATETIME2 NOT NULL,
    MaxUses INT NOT NULL DEFAULT 1,
    UsedCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy INT NOT NULL FK Users
);

CREATE TABLE FormDefinitionVersions (
    Id INT IDENTITY PK,
    FormDefinitionId INT NOT NULL FK,
    Version INT NOT NULL,
    SchemaJson NVARCHAR(MAX) NOT NULL,             -- snapshot
    PublishedAt DATETIME2 NOT NULL,
    PublishedBy INT NOT NULL FK Users,
    UNIQUE (FormDefinitionId, Version)
);
```

### 4.2 Form render pipeline

```
GET /Forms/{slug}                    → kullanıcı render
GET /Forms/Public/{slug}/{token}    → public render (token validate)
POST /Forms/Submit                   → submission

Render:
  1. FormDefinitionService.Get(slug)
  2. FormRendererService.Render(definition) → Razor view + JSON schema
  3. Client-side: form-render.js + form-validate.js + (file/signature/conditional)
  4. Output: <form> with x-data Alpine state

Submit:
  1. AntiForgery + (public ise token validate)
  2. FormValidationService.Validate(definition, payload) — server-side authoritative
  3. FormEncryptionService.EncryptIfNeeded(definition, payload)
  4. FormSubmissionService.Save(submission) — transaction
  5. Plan 42: ProcessInstanceService.Create(submission)
  6. Plan 36: WorkflowEngine.Start(workflowId, instance)
  7. INotificationService.Notify(definition.OnSubmitNotifyRoles)
  8. AuditLog: form_submitted
  9. Plan 38: EntityRelations.Add(submission ↔ process_instance, ↔ workflow_instance)
 10. Response: 200 + redirect /Forms/Submitted/{id}
```

### 4.3 KVKK uyum — DataElement mapping ZORUNLU

Plan 40 KVKK envanteri için **her form field bir DataElement'e bağlı olmalı**. `DataElementMapValidator`:

```csharp
public Task<ValidationResult> ValidateAsync(FormDefinition def, CancellationToken ct)
{
    var unmappedFields = def.Fields
        .Where(f => f.FieldType != FieldType.SectionHeader && f.FieldType != FieldType.Hidden)
        .Where(f => !_db.FormFieldDataElementMaps.Any(m => m.FormFieldId == f.Id))
        .ToList();

    if (unmappedFields.Any())
        return new ValidationResult(
            valid: false,
            errors: unmappedFields.Select(f =>
                $"Field '{f.Label}' bir KVKK DataElement'e bağlı değil. KVKK envanteri için zorunlu."));

    return ValidationResult.Ok();
}
```

`FormDefinition.Status = 0 Taslak` iken mapping eksik OK. **`Status = 1 Yayında` geçişi için tam mapping zorunlu** — service guard.

### 4.4 Şifreli alan (ihbar formu için)

`FormDefinition.IsEncrypted = 1` ise her `FormSubmissionFieldValue.ValueText` AES-256 ile şifrelenir. Anahtar Azure Key Vault veya `appsettings` (env override) — Plan 40 secret discipline. Sadece "İhbar Komitesi" rolü çözebilir (DB level RLS değil, service level guard).

### 4.5 Anti-spam

- **Honeypot:** hidden field `_hp` — bot doldurursa reject
- **Rate limit:** IP başına dakikada 5 submit (ASP.NET Core rate limiter middleware)
- **Time check:** form load → submit arası < 2 saniye → bot şüphesi
- **reCAPTCHA v3** (v2): public form'larda opsiyonel toggle (FormDefinition.UseRecaptcha)

### 4.6 Sürüm yönetimi

Form yayınlandığında `FormDefinitionVersions` snapshot alır. Submission `FormDefinitionId + Version` referansı saklar — eski submission'lar eski şema ile render edilir (geriye uyumluluk). Yeni submit'ler aktif Version'a gider.

---

## 5. Riskler

| Risk | Olasılık | Etki | Önlem |
|---|---|---|---|
| v1 admin form yorucu, kullanıcı sevmez | Orta | Orta | 8-10 hazır template seed; çoğu form template'ten klon. v2 builder ile çöz. |
| DataElement mapping unutulur, KVKK envanteri eksik | Yüksek | Yüksek | `Yayında` geçişi mapping eksikse blok. Admin UI uyarı banner. |
| Anonim form abuse (ihbar spam) | Orta | Orta | Rate limit + reCAPTCHA + IP audit + manuel review queue |
| Şifreli ihbar key management | Düşük | Yüksek | Key Vault zorunlu prod; appsettings dev only |
| Form sürüm değişikliği aktif submission'ları kırar | Düşük | Orta | Submission `Version` referansı; eski şema arşiv |
| Public token leak | Orta | Yüksek | HMAC + ExpiresAt + MaxUses; tek-kullanımlık seçenek |
| File upload payload bomb | Orta | Orta | Documents reuse — magic byte + MaxFileSize + virus scan stub |
| Workflow trigger başarısız → submission orphan | Orta | Orta | Transaction wrap; retry queue Hangfire; admin re-trigger UI |
| Multi-firma izolasyon ihlali | Düşük | Yüksek | `IUserDataScope` her query'de; FirmaId her tablo |
| Conditional logic XSS riski | Düşük | Yüksek | Server-side authoritative; client logic sadece UX; sanitize zorunlu |

---

## 6. Done Criteria

### Faz 0 — Veri modeli + scaffold (6-8h)
- [ ] `Mosaik.Modules.Forms` csproj + `FormsModule.cs` IMosaikModule
- [ ] Migration 70 schema (idempotent)
- [ ] 7 entity + DbSet
- [ ] Build yeşil + 3+ unit test (entity validation)

### Faz 1 — JSON config v1 (Render + Submit) (10-12h)
- [ ] `FormRendererService` — JSON → Razor view
- [ ] `FormValidationService` — server-side auth
- [ ] `FormSubmissionService` — save + workflow trigger stub
- [ ] `/Forms/{slug}` GET + POST endpoint
- [ ] form-render.js + form-validate.js (vanilla IIFE)
- [ ] AntiForgery + AsNoTracking
- [ ] 5+ unit test (field type render, validation, submit happy path)

### Faz 2 — Admin Form CRUD (8-10h)
- [ ] `FormDefinitionController` — Index/Edit/Details/Create/Delete
- [ ] FormField ekle/sil/sırala admin form
- [ ] Status Taslak→Yayında transition (DataElement mapping check)
- [ ] WCAG focus trap, scope th, aria-label
- [ ] 5+ unit test

### Faz 3 — Public link + Anonim + AntiSpam (8-10h)
- [ ] `PublicTokenService` HMAC + expiry
- [ ] `PublicFormController` — token validate, render, submit
- [ ] Honeypot + rate limit middleware
- [ ] Submission audit (IP, UA, timing)
- [ ] 5+ unit test (token validate, expired, max-uses)

### Faz 4 — File upload + Signature + DataElement mapping (8-10h)
- [ ] File field — Documents modülü reuse (Plan 33 B-03 App_Data pattern)
- [ ] Signature pad — Canvas → base64 PNG → Documents
- [ ] `FormFieldDataElementMap` admin UI
- [ ] `DataElementMapValidator` — Yayında geçişi blocker
- [ ] 6+ unit test

### Faz 5 — Şifreli alan + İhbar template (4-6h)
- [ ] `FormEncryptionService` AES-256
- [ ] Key Vault entegrasyonu (dev appsettings, prod KV)
- [ ] İhbar template seed (Migration 71)
- [ ] "İhbar Komitesi" role only decrypt
- [ ] 4+ unit test

### Faz 6 — Submission admin + Export (6-8h)
- [ ] `SubmissionController` — Index filter, Detail
- [ ] Excel export ClosedXML
- [ ] Re-submission edit (admin only)
- [ ] AuditLog her görüntülemede `form_submission_viewed`
- [ ] 4+ unit test

### Faz 7 — Template seed + İlk 8 form (4-6h)
- [ ] Migration 71 — 8 form template seed:
  1. DSAR (m.13) — public + kimlik doğrulama
  2. İhbar (whistleblower) — anonim + şifreli
  3. Aday başvuru — public + CV upload
  4. Veri ihlali bildirim (m.12) — çalışan + dosya
  5. Açık rıza yenileme — çalışan + signature
  6. Engelli belgesi yükleme — çalışan + file + İSG
  7. Tedarikçi DPA — tedarikçi + signature
  8. Yıllık envanter review — birim müdürü inbox
- [ ] Her template DataElement mapping seed
- [ ] Her template TriggersWorkflowId set (Plan 36 + 42 hookup)

### Genel
- [ ] Build: 0 uyarı 0 hata
- [ ] Test: tüm yeni testler yeşil + mevcut 387 bozulmamış
- [ ] ARCHITECTURE_MAP refresh
- [ ] CLAUDE.md modül listesine `Forms` ekle
- [ ] VISION.md §7 entegrasyon notu
- [ ] ADR-020 yazılır: "Form Builder — Hybrid (v1 JSON config, v2 builder UI)"

---

## 7. Rollback

- Migration 70 idempotent + reverse migration hazır (DROP TABLE)
- `Mosaik.Modules.Forms` csproj reference Mosaik.csproj'dan kaldırılırsa modül devre dışı (ADR-002)
- Şifreli submission'lar key kaybı durumunda kurtarılamaz — KV backup zorunlu

---

## 8. Adımlar

```
Faz 0 — Scaffold (6-8h)
Faz 1 — Render + Submit pipeline (10-12h)
Faz 2 — Admin CRUD (8-10h)
Faz 3 — Public + Anonim + AntiSpam (8-10h)
Faz 4 — File + Signature + DataElement map (8-10h)
Faz 5 — Encrypted + İhbar (4-6h)
Faz 6 — Submission admin + Export (6-8h)
Faz 7 — Template seed 8 form (4-6h)

Toplam: 54-70h, 4-5 hafta
```

---

## 9. Cross-reference

- **Plan 40** — KVKK envanter, DataElement mapping bu plana bağımlı (Form Builder olmadan KVKK Faz 3 form aspect stub kalır)
- **Plan 42** — Process Execution Runtime, Form ↔ ProcessInstance binding bu plana bağımlı
- **Plan 36** — Workflow Engine, Form submit trigger
- **Plan 38** — EntityRelations, FormDefinition ↔ Process polymorphic
- **Plan 31** — SMTP, submission notification kanalı
- **Documents** — File field reuse (App_Data + Download endpoint pattern)
- **ADR-002** — Modular monolit
- **ADR-014** — Vanilla IIFE + Alpine (no React/Vue)
- **ADR-015** — Yeni modüller ayrı assembly
- **ADR-020 (yeni)** — Form Builder Hybrid yaklaşım gerekçesi

---

## 10. Karar gerekiyor

1. **v1 admin form mu, hafif inline builder mı?** — v1 katı admin form CRUD (4-5 hafta), v1.5 inline builder (~+1 hafta). Önerim: katı v1, v2 ile drag-drop.
2. **Şifreli ihbar key storage** — Azure Key Vault zorunlu prod mu, sadece appsettings yeterli mi? Önerim: KV zorunlu (KVKK m.12/5 yüksek güvenlik gereksinimi).
3. **reCAPTCHA opt-in mi default mu?** — Public form'larda Google reCAPTCHA v3 zorunlu mu? Önerim: opsiyonel `FormDefinition.UseRecaptcha`; ihbar/DSAR default açık.
4. **Conditional logic v1'de mi v2'de mi?** — v1 basit (if A=X show B), v2 advanced (multi-condition AND/OR). Önerim: v2'ye.
5. **Form template marketplace** — Şimdilik 8 BKM template + admin CRUD yeterli mi? Önerim: yeterli, marketplace ileride.

---

## 11. Sürüm

- **2026-05-21:** Taslak. Onay bekliyor.


---
> **DÜZELTME 2026-06-29 (Plan 54 / research):** `FormVersion` snapshot (FK Form + version + tam SchemaJson + publishedAt) + `submission.FormVersionId` ZORUNLU — submission render edildiği versiyona göre validate. SurveyJS engine+builder REUSE (kendi renderer yazma); sadece ince Mosaik wrapper (entity/version/DataElement-map/HMAC/encryption/anti-spam). Anti-spam katmanlı: honeypot→timing→IP-rate-limit(IP yazma)→CAPTCHA-son. field→DataElement map OPTIONAL. Not: `FormDefinitionVersion.cs` zaten var (§4.6).
