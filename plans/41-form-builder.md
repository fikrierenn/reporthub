# Plan 41 — Form Builder

**Tarih:** 2026-05-21
**Yazan:** Fikri / Claude
**Durum:** `Taslak` (rev 2 — 2026-07-01, implementasyon-hazır, kullanıcı onayı bekliyor)
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

### A: Hybrid (SEÇİLEN — survey-core renderer reuse + DIY liste-builder v1, drag-drop v2 opsiyonel)

**v1 (revize 2026-07-01 — 2 tur OSS araştırması sonrası):**
- **Renderer:** `survey-core` + `survey-js-ui` (MIT, ücretsiz, vanilla-JS uyumlu, framework bağımsız) reuse edilir — kendi renderer YAZILMAZ. JSON şema motoru + validation + tüm field tipleri (text/select/file/signature/vb) hazır, olgun (SurveyJS ekosistemi büyük, iyi dokümante).
- **Builder (admin form tasarım UI):** **Mosaik DIY, liste-tabanlı** (drag-drop YOK) — alan ekle/sil/sırala (yukarı/aşağı buton)/tip seç/validation kuralı gir. survey-core JSON şemasını (`elements: [{type,name,title,...}]`) hedefler. **$0, üçüncü-parti risk yok.**
- **Drag-drop builder (v2, opsiyonel, gerekirse):** Şimdi COMMIT edilmiyor. Gerekirse `Formeo` (MIT, vanilla, editor+renderer aynı paket) adapte edilir veya native HTML5 drag-drop (`draggable`+`dragstart`/`dragover`/`drop`, js-conventions.md pattern) ile liste-builder'a drag-reorder eklenir — **ayrı küçük iş, footprint-ladder**.

**v3 (ileride):** Conditional logic + multi-page + advanced validation.

**2 tur OSS araştırması sonucu (reddedilen "daha iyi" adaylar):**
- **Form.io** — lisans belirsizliği (repo MIT ama kurumsal doküman/3.parti kaynaklar OSL-3.0/copyleft diyor, çelişkili) + MongoDB zorunluluğu + enterprise özellikler paywall. KVKK aracı için lisans riski kabul edilemez.
- **Formeo** — MIT temiz, vanilla, drag-drop dahil ama JSON şema resmi dokümante değil (adapter yazımı gerekir), 80 açık issue, tek-maintainer riski. v2 drag-drop adayı olarak not edildi, v1'e commit edilmedi.
- **JSON Forms** — builder tarafı deprecated/terk edilmiş.
- **kevinchappell/formBuilder** — MIT ama jQuery zorunlu (Mosaik "jQuery yok" ihlali).
- **Formily, GrapesJS** — framework kilitli (React/Vue) veya form-özel değil.

**Sebep:**
- KVKK Plan 40 (kapandı) ve Process Execution Plan 42 bekliyor. v1 (renderer reuse + DIY builder) fonksiyonel olarak tam — sadece drag-drop kozmetik eksik.
- survey-core reuse = ~12h → ~4h tasarruf (renderer yazmaktan), sıfır lisans riski (MIT, mature).
- DIY builder = DataElement mapping native + audit hooks tam + multi-firma izole + Türkçe UI + KVKK uyum + üçüncü-parti bağımlılık yok.
- Risk düşük — incremental, drag-drop sonradan eklenebilir (builder'ın altındaki veri modeli/API değişmez).

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

### OSS reuse stack (2026-05-21 ilk araştırma + 2026-07-01 rev 2 — 2 tur derinleşme)

| Bileşen | OSS | Lisans | Konum | Tasarruf |
|---|---|---|---|---|
| Renderer | **survey-core + survey-js-ui** (SurveyJS Form Library, MIT) | MIT | `wwwroot/lib/surveyjs/` UMD, vanilla embed (script+CSS, iframe yok) | %70 (~12h → 4h) — JSON schema + `Serializer.addProperty()` ile KVKK DataElement custom metadata |
| Builder UI (v1) | **Mosaik DIY — liste-tabanlı** (drag-drop YOK, Survey Creator $579+$229/yıl ticari — reddedildi, ücretsiz kalınacak) | İç | Plan 41 Faz 2 | 0 (kapsamda kalır, ~20-25h — drag-drop'suz basitleşmiş) |
| Builder UI (v2, opsiyonel) | **Formeo adapte** (MIT, vanilla, drag-drop) VEYA native HTML5 drag-drop | MIT/İç | **Commit edilmedi** — gerekirse ayrı küçük iş | Drag-drop istenirse ~10-15h (Formeo adapter) |
| Signature pad | **signature_pad 5.1.1** (MIT, ~5KB UMD) | MIT | `wwwroot/assets/js/form-signature.js` IIFE wrapper | %85 (~6h → 1h) |
| AntiSpam katmanlı (2026-06-29 düzeltme) | **Honeypot → timing-check → IP-rate-limit → CAPTCHA (son)** | Pattern | controller side, sıralı early-exit | %50 (~4h → 2h) — CAPTCHA sadece üst katmanlar geçilirse tetiklenir (Cloudflare Turnstile önerilir, Google reCAPTCHA KVKK riski) |
| DataElement KVKK metadata | Plan 40 entegrasyon, **OPTIONAL** (2026-06-29 düzeltme — Yayında geçişini bloklamaz) | İç | Plan 41 Faz 4 | 0 |
| Workflow trigger | Plan 36 Stateless callback | İç | Plan 41 Faz 7 | 0 |

**Reddedilenler (2026-05-21):** Formbricks (AGPLv3 viral), LimeSurvey (GPL+PHP), Survey Creator (ücretli, bütçe onayı yok), reCAPTCHA v3 (KVKK riski — Google), Blazor formlar (stack uyumsuz), json-editor/Alpaca (Bootstrap/jQuery UI legacy), Tripetto (proprietary), Syncfusion (proprietary).

**Reddedilenler (2026-07-01 rev 2 — 2. tur "daha iyi ücretsiz drag-drop var mı" araştırması):**
- **Form.io / formio.js** — lisans belirsiz (repo LICENSE.txt MIT ama form.io kurumsal dokümantasyon + 3.parti kaynaklar OSL-3.0/copyleft anlatıyor, çelişkili) + self-host MongoDB zorunlu + enterprise özellik paywall. KVKK aracı için lisans riski kabul edilemez.
- **Formeo** (Draggable/formeo) — MIT temiz, vanilla, drag-drop dahil (editor+renderer aynı paket) — **v2 adayı olarak not edildi**, ama JSON şema resmi dokümante değil (adapter yazımı gerekir) + 80 açık issue + tek-maintainer riski → v1'e commit edilmedi.
- **JSON Forms** (@jsonforms/core) — renderer MIT ve olgun ama resmi drag-drop builder (`@jsonforms/editor`) deprecated/terk edilmiş.
- **kevinchappell/formBuilder** — MIT, aktif bakımlı, ama **jQuery zorunlu bağımlılık** — Mosaik "jQuery yok" kuralını ihlal eder.
- **Formily + Designable** (Alibaba) — MIT ama React/Vue'ya sıkı bağlı, framework-agnostic çekirdek yok — vanilla JS/Razor MVC entegrasyonu pratik değil.
- **GrapesJS + grapesjs-plugin-forms** — MIT ama genel sayfa builder'ı (form-özel değil), form-plugin 3 yıl güncellenmemiş, çıktı HTML/CSS (JSON şema değil) — DataElement mapping'e uymuyor.

**Net Plan 41 effort tasarrufu: ~%25-30** (~62h → ~44-47h, drag-drop v1 kapsamından çıktığı için tasarruf biraz düştü ama üçüncü-parti risk sıfıra indi).

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
    FormVersionId INT NOT NULL FK FormDefinitionVersions,  -- 2026-06-29 düzeltme: ZORUNLU — submission hangi
                                                            -- şema versiyonuna göre render/validate edildiyse o versiyon (§4.6)
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

### 4.3 KVKK uyum — DataElement mapping OPTIONAL (2026-06-29 düzeltme)

Plan 40 KVKK envanteri için form field'lar DataElement'e bağlanabilir ama **zorunlu değil** — `Yayında` geçişini bloklamaz (ilk taslak "ZORUNLU + blocker" kararı gerçek kullanımda çok katı bulundu, düzeltildi). `DataElementMapValidator` **uyarı** üretir, hata değil:

```csharp
public Task<ValidationResult> ValidateAsync(FormDefinition def, CancellationToken ct)
{
    var unmappedFields = def.Fields
        .Where(f => f.FieldType != FieldType.SectionHeader && f.FieldType != FieldType.Hidden)
        .Where(f => !_db.FormFieldDataElementMaps.Any(m => m.FormFieldId == f.Id))
        .ToList();

    // OPTIONAL — Warning döner, IsSuccess=true kalır. Admin UI "X alan KVKK'ya bağlı değil" banner gösterir,
    // ama Yayında geçişini bloklamaz (Faz 4 done criteria: warning, blocker değil).
    return ValidationResult.OkWithWarnings(
        unmappedFields.Select(f => $"Field '{f.Label}' bir KVKK DataElement'e bağlı değil (opsiyonel)."));
}
```

Bağlanmış alanlar KVKK reverse-search'e (Plan 40 Faz 4 pattern) dahil olur; bağlanmamış alanlar sadece envanterde görünmez (veri kaybı değil, görünürlük kaybı — kabul edilebilir risk).

### 4.4 Şifreli alan (ihbar formu için)

`FormDefinition.IsEncrypted = 1` ise her `FormSubmissionFieldValue.ValueText` AES-256 ile şifrelenir. Anahtar Azure Key Vault veya `appsettings` (env override) — Plan 40 secret discipline. Sadece "İhbar Komitesi" rolü çözebilir (DB level RLS değil, service level guard).

### 4.5 Anti-spam — katmanlı sıra (2026-06-29 düzeltme)

Her katman early-exit; sadece önceki katman(lar) geçilirse sıradaki çalışır (maliyet artan sırada):

1. **Honeypot:** hidden field `_hp` — bot doldurursa **anında reject** (CAPTCHA'ya bile gitmeden).
2. **Timing check:** form load → submit arası < 2 saniye → bot şüphesi, reject.
3. **IP rate-limit:** IP başına dakikada 5 submit (ASP.NET Core rate limiter middleware) — **IP yazma zorunlu** (audit + rate-limit paylaşır).
4. **CAPTCHA (son katman, en maliyetli):** Cloudflare Turnstile (SaaS ücretsiz, "no PII collection" — reCAPTCHA v3 Google veri paylaşımı KVKK riski, reddedildi). Yukarıdaki 3 katman geçilirse tetiklenir; `FormDefinition.UseCaptcha` toggle, ihbar/DSAR default açık.

### 4.6 Sürüm yönetimi (2026-06-29 düzeltme — ZORUNLU FK)

Form yayınlandığında `FormDefinitionVersions` snapshot alır (SchemaJson tam kopya). `FormSubmission.FormVersionId` **ZORUNLU FK** (§4.1) — submission her zaman render edildiği versiyona bağlı kalır, eski submission'lar eski şema ile render edilir (geriye uyumluluk garantisi, "hangi soruları gördü" denetim sorusu her zaman cevaplanabilir). Yeni submit'ler aktif (en son yayınlanan) Version'a gider.

---

## 5. Riskler

| Risk | Olasılık | Etki | Önlem |
|---|---|---|---|
| v1 admin form yorucu, kullanıcı sevmez (drag-drop yok) | Orta | Orta | 8-10 hazır template seed; çoğu form template'ten klon. Drag-drop v2'ye deferred (Formeo adayı). |
| DataElement mapping unutulur, KVKK envanteri eksik | Orta | Orta | **(2026-07-01 düzeltme: OPTIONAL, blocker değil)** — Admin UI uyarı banner, denetim raporunda "eşlenmemiş alan" listesi. Görünürlük kaybı, veri kaybı değil. |
| Anonim form abuse (ihbar spam) | Orta | Orta | Katmanlı anti-spam (honeypot→timing→rate-limit→Turnstile CAPTCHA) + IP audit + manuel review queue |
| Şifreli ihbar key management | Düşük | Yüksek | Key Vault zorunlu prod; appsettings dev only |
| Form sürüm değişikliği aktif submission'ları kırar | Düşük | Orta | `FormSubmission.FormVersionId` ZORUNLU FK; eski şema arşiv (§4.6) |
| survey-core/survey-js-ui üçüncü-parti bakım riski | Düşük | Orta | SurveyJS ekosistemi büyük+aktif (ticari Survey Creator'ı besleyen şirket sürdürüyor); MIT fork edilebilir. Wrapper ince tutulur (Faz 1) — üst kütüphane değişirse adapter katmanı izole eder. |
| Public token leak | Orta | Yüksek | HMAC + ExpiresAt + MaxUses; tek-kullanımlık seçenek |
| File upload payload bomb | Orta | Orta | Documents reuse — magic byte + MaxFileSize + virus scan stub |
| Workflow trigger başarısız → submission orphan | Orta | Orta | Transaction wrap; retry queue Hangfire; admin re-trigger UI |
| Multi-firma izolasyon ihlali | Düşük | Yüksek | `IUserDataScope` her query'de; FirmaId her tablo |
| Conditional logic XSS riski | Düşük | Yüksek | Server-side authoritative; client logic sadece UX; sanitize zorunlu |

---

## 6. Done Criteria

### Faz 0 — Veri modeli + scaffold ✅ KAPANDI (retroaktif — 2026-05-21 onay öncesi scaffold edilmiş, rev 2'de tamamlandı 2026-07-01)
- [x] `Mosaik.Modules.Forms` csproj + `FormsModule.cs` IMosaikModule — commit `315aa5c`, `c26d7f8`, `d0b81ee`, `6430cd7` (2026-05-21, plan onay beklerken)
- [x] Migration 70/72/73 schema (idempotent) + **74 rev 2 eklendi (2026-07-01):** `FormSubmissions.FormVersionId` ZORUNLU FK
- [x] 7 entity + DbSet — `FormSubmission.FormVersionId` rev 2'de eklendi (tablo boştu, NOT NULL güvenli)
- [x] Build yeşil

### Faz 1 — survey-core render + Submit ✅ KAPANDI 2026-07-01
- [x] `wwwroot/lib/surveyjs/` — survey-core + survey-js-ui UMD (MIT, vendored, unpkg@2.5.32)
- [x] `FormRendererService` — `FormField[]` → survey-core JSON şema dönüşümü (`FormSchemaBuilder`, saf/testable)
- [x] `FormValidationService` — server-side authoritative (`FormFieldValidator`, saf/testable) — **fail-closed** (bozuk kural = reddet, fail-open DEĞİL, silent-failure-hunter CRITICAL fix)
- [x] `FormSubmissionService` — save + `FormVersionId` bind + workflow trigger stub + alan-bazlı hata dict (`ErrorCode="field_validation"`)
- [x] `/Forms/{slug}` GET + `/Forms/{slug}/Submit` POST + `/Forms/{slug}/Submitted` GET
- [x] form-render.js (survey-core init + Alpine glue, field-level error → `question.addError`) — Alpine factory pattern
- [x] AntiForgery + AsNoTracking + firma-izolasyonu (her sorguda `CurrentFirmaId`)
- [x] 18 unit test (FormSchemaBuilder 8 + FormFieldValidator 10) — field type→schema dönüşüm, validation, malformed-JSON fail-closed
- **Full-scan bulguları kapatıldı:** code-reviewer/security-reviewer/inline-style temiz; silent-failure-hunter CRITICAL (fail-open validation) + HIGH (alan-bazlı hata) + MEDIUM (renderer log asimetrisi) fix edildi.
- **Preview'de 2 gerçek bug bulundu/düzeltildi (full-scan sonrası, canlı test sırasında):** (1) `FormsModule.cs` Faz 0 scaffold'da `.ToTable()` eksik — EF "FormDefinition" (tekil) arıyordu, DB'de "FormDefinitions" (çoğul) — runtime `Invalid object name` hatası, sadece Faz 1 gerçek sorgu çalıştırınca ortaya çıktı. (2) Render.cshtml JSON'u `x-data` HTML attribute'una `Html.Raw` ile gömüyordu — JSON içindeki çift-tırnak attribute'u kırıyordu (Alpine "Unexpected token" hatası); tüm dinamik değerler script-context'e (`window.__mosaikForm*`) taşındı.
- Preview: manuel seed veri (FormDefinition+2 FormField+FormDefinitionVersion) ile tam render→doldur→submit→DB doğrulama akışı çalıştı (FormVersionId doğru bağlandı, ValueText/ValueNumber tip-doğru kaydedildi). Test verisi temizlendi.
- Test: 664/664 (18 yeni).

### Faz 2 — Admin Form CRUD, liste-tabanlı builder ✅ KAPANDI 2026-07-01
- [x] `FormDefinitionController` — Index/Create/Edit/Details/Publish/Archive/Restore + AddField/EditField/RemoveField/MoveField
- [x] FormField ekle/sil/sırala (yukarı/aşağı buton, `FormFieldOrderer` saf/testable, drag-drop YOK v1)
- [x] Status Taslak→Yayında transition — DataElement mapping check **warning banner, blocker DEĞİL** (§4.3) + **boş-seçenek Select/Radio blocker** (silent-failure-hunter MEDIUM — canlı seçilemez dropdown önlendi)
- [x] `FormDefinitionVersion` snapshot — Yayına alma anında SchemaJson snapshot (`FormSchemaBuilder` reuse) + Version++
- [x] WCAG: scope th, aria-label, crumbs+aria-current; inline-style temiz (tarama doğruladı, 28 CSS class tanımlı)
- [x] 11 unit test (FormFieldOrderer 5 + FormPublishValidator 6, boş-choice + malformed-JSON dahil)
- **Full-scan bulguları kapatıldı:** güvenlik temiz (multi-tenant IDOR, CSRF, XSS, mass-assignment, ReDoS hepsi PASS), inline-style temiz, CSS class'lar tanımlı. silent-failure-hunter CRITICAL (MoveField sonuç yutuluyordu + audit yok) + MEDIUM (publish warnings gösterilmiyordu, boş-choice select) fix. code-reviewer redundant ternary + `.row-actions` `.dt`-dışı no-op fix.
- **Preview'de 1 gerçek bug bulundu/düzeltildi:** `FormFieldService.AddAsync` + `FormDefinitionService.PublishAsync` `.DefaultIfEmpty(0).MaxAsync()` EF SQL'e çevrilemiyordu (500 hata) — `MaxAsync(v => (int?)...)  ?? 0` pattern'ine çevrildi. Faz 1'deki `Contains(string,StringComparison)` gibi build-yeşil-runtime-kırık sınıfından — preview yakaladı.
- **Preview:** create→addField(boş Select)→publish BLOCKED→editField(seçenek ekle)→publish SUCCESS (Status=1, Version=1, SchemaJson dropdown+choices snapshot) end-to-end doğrulandı. Test verisi temizlendi.

### Faz 3 — Public link + Anonim + AntiSpam katmanlı ✅ KAPANDI 2026-07-01
- [x] `PublicTokenService` — **HMAC+secret DEĞİL** (random 256-bit token + SHA256 hash-storage, reset-password pattern; secret yönetimi yok, leak-safe). ConsumeAsync/RefundAsync atomik `ExecuteUpdateAsync`, FixedTimeEquals timing-safe.
- [x] `PublicFormController` ([AllowAnonymous]) — token validate → render → submit; `/Forms/p/{formId}/{token}` route (admin `/Forms/FormDefinition/*` ile çakışma çözüldü; Faz 1'de `/Forms/{slug}`→`/Forms/f/{slug}` de aynı sebeple).
- [x] AntiSpam katmanlı sıra (§4.5): honeypot → timing → IP-rate-limit (formId+IP key). CAPTCHA son-katman opsiyonel deferred (plan §4.5, açık internete açılmadan önce şart).
- [x] Submission audit (IP, UA) + spam-blocked + token-exhausted audit
- [x] Anonim submit — FormSubmissionService anonim-check public-token için gevşetildi (token = erişim kanıtı, PublicTokenId server-side ValidateAsync sonucundan, client-spoof imkansız)
- [x] `AntiSpamGuard` 6 unit test (katman sırası, negative-elapsed skip)
- **Full-scan hardening:** güvenlik 0 CRITICAL (token/multi-tenant/CSRF/XSS/anonim-spoof PASS). silent-failure-hunter CRITICAL: **over-use** — submission consume'dan ÖNCE kaydediliyordu + ConsumeAsync bool yutuluyordu → **consume-first gate + submit-fail'de RefundAsync** (over-use imkansız). HIGH: rate-limiter non-atomik race → StrongBox+Interlocked; rejection path'leri audit'siz → token-exhausted audit. code-reviewer: `.hp-field` honeypot CSS **tanımsızdı** (honeypot görünür + bot'a ipucu) → off-screen CSS eklendi.
- **Preview'de 1 gerçek bug (canlı 3-senaryo testi):** too-fast **geçti** (elapsed=0.5) — tr-TR culture noktayı binlik-ayraç sanıp `0.5→5` parse ediyordu (TooFast bypass). `InvariantCulture` + parse-fail→-1 fix. Sonra: honeypot→400, too-fast→400, temiz→200 (anonim submission, PublicTokenId bağlı, UsedCount=1 sadece temiz; spam DB'ye girmedi) doğrulandı.
- **Not:** Chrome extension localhost'u kurumsal AV policy'yle blokluyor — public akış preview MCP ([AllowAnonymous] endpoint oturumdan bağımsız) ile doğrulandı.

### Faz 4 — File upload + Signature + DataElement mapping (opsiyonel) (8-10h) ✅ KAPANDI (commit 3a3c72e, 2026-07-01)
- [x] File field — Forms **kendi** App_Data/forms storage (ADR-002: Documents ana projeye compile bağlanamaz → magic-byte+guard kopyalandı). FormSubmissionFile entity + migration 75.
- [x] Signature — survey-core **native signaturepad** (bundled, ayrı signature_pad lib GEREKMEDİ) → PNG dosya (inline base64 değil, advisor conf 72). Değişiklik: plan "signature_pad 5.1.1" yerine survey-core native.
- [x] `FormFieldDataElementMap` admin UI (Details sayfası eşleme bölümü) + cross-modül DataElementLookupService (KvkkDataElements SqlQueryRaw + graceful degrade)
- [x] Unmapped warning — `FormPublishValidator.GetUnmappedFieldWarnings` (ayrı DataElementMapValidator YARATILMADI, advisor conf 90). Yayında bloklamaz.
- [x] 12 yeni test (697/697). Preview E2E: file+signature+number(3.5 locale)+magic-byte reddi+refund+map+admin download.
- **Full scan:** 0 CRIT/0 HIGH; 3 MED fix (path trailing-sep, mid-batch orphan cleanup, InvariantCulture locale, DbException log).

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

## 10. Karar gerekiyor (rev 2 — çözülenler işaretli)

1. ~~v1 admin form mu, hafif inline builder mı?~~ **ÇÖZÜLDÜ (2026-07-01):** survey-core renderer reuse + Mosaik DIY liste-tabanlı builder (drag-drop yok). 2 tur OSS araştırması sonrası — Form.io lisans riski, Formeo olgunlaşmamış, Survey Creator ücretli. Drag-drop v2'ye (opsiyonel, gerekirse Formeo adaptasyonu).
2. **Şifreli ihbar key storage** — Azure Key Vault zorunlu prod mu, sadece appsettings yeterli mi? Önerim: KV zorunlu (KVKK m.12/5 yüksek güvenlik gereksinimi). **Açık — onay bekliyor.**
3. ~~reCAPTCHA opt-in mi default mu?~~ **ÇÖZÜLDÜ (2026-06-29):** Google reCAPTCHA reddedildi (KVKK veri paylaşımı riski) → Cloudflare Turnstile, katmanlı anti-spam'in son adımı (§4.5).
4. **Conditional logic v1'de mi v2'de mi?** — v1 basit (if A=X show B), v2 advanced (multi-condition AND/OR). Önerim: v2'ye. **Açık — onay bekliyor.**
5. **Form template marketplace** — Şimdilik 8 BKM template + admin CRUD yeterli mi? Önerim: yeterli, marketplace ileride. **Açık — onay bekliyor (düşük öncelik).**

---

## 11. Sürüm

- **2026-05-21:** Taslak. Onay bekliyor.
- **2026-06-29 (Plan 54 / research düzeltme, önceki plan sonunda not olarak vardı, rev 2'de plan gövdesine işlendi):** `FormVersionId` submission'da ZORUNLU FK. SurveyJS engine (survey-core+survey-js-ui, MIT) reuse — builder AYRI değerlendirilecek (rev 2'de çözüldü). Anti-spam katmanlı sıra. DataElement mapping OPTIONAL (blocker değil).
- **2026-07-01 rev 2 — 2 tur OSS araştırması + kullanıcı onayı:** Builder kararı kilitlendi — survey-core renderer + Mosaik DIY liste-builder (v1, $0, üçüncü-parti risksiz), drag-drop v2'ye deferred (Formeo MIT adayı not edildi, commit edilmedi). Survey Creator ($579+$229/yıl ticari) kullanıcı kararıyla reddedildi ("ücretsiz kullan"). OSS reddedilenler listesi genişletildi (Form.io lisans riski, Formeo olgunluk riski, JSON Forms/formBuilder/Formily/GrapesJS uyumsuzluk). **Plan implementasyon-hazır — kullanıcı onayı bekliyor (Faz 0 başlangıcı).**


---
> **DÜZELTME 2026-06-29 (Plan 54 / research):** `FormVersion` snapshot (FK Form + version + tam SchemaJson + publishedAt) + `submission.FormVersionId` ZORUNLU — submission render edildiği versiyona göre validate. SurveyJS engine+builder REUSE (kendi renderer yazma); sadece ince Mosaik wrapper (entity/version/DataElement-map/HMAC/encryption/anti-spam). Anti-spam katmanlı: honeypot→timing→IP-rate-limit(IP yazma)→CAPTCHA-son. field→DataElement map OPTIONAL. Not: `FormDefinitionVersion.cs` zaten var (§4.6).
