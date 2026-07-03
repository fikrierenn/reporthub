---
name: mosaik-forms-expert
description: Mosaik.Modules.Forms modülü uzmanı — form builder, submission pipeline, public token, DataElement/KVKK map, şifreleme, template. Forms modülüne (form tanımı, alan, submission, public link, şifreli alan, template, admin submission liste/export) dokunulurken tetiklenir. Modülün TAM mimarisi (entity/service/controller/veri-akışı file:line) + geliştirme pattern'leri + 3 bilinen gap'in tam insertion noktası. Yeni Forms kodu yazmadan önce oku.
---

# Mosaik.Modules.Forms — Uzman

_Kaynak: kılcal mimari dalış 2026-07-03 (Plan 56 M-A Faz 0). Forms işine dokunmadan önce bu + `mosaik-security`/`kvkk-veri-envanteri` cross-cutting._

## Modül mimarisi (ADR-002/015)
- `FormsModule.cs:14` IMosaikModule; `ModuleLoader` reflection ile keşfeder (`Program.cs:191`). **Kendi DbContext YOK** — base `DbContext` = aynı `MosaikContext`; `_db.Set<T>()` ile erişir. `ConfigureModelBuilder` (`FormsModule.cs:38-163`) MosaikContext model builder'a ToTable map'ler.
- csproj SADECE `Mosaik.Core` ref → KVKK/Documents'e compile ref YASAK (izolasyon). KVKK'ya erişim = `DataElementLookupService` raw SQL (`SqlQueryRaw` `dbo.KvkkDataElements`, `DbException`→graceful degrade).
- `ConfigureServices` (`:22-36`) 8 scoped service + MemoryCache. **`:35` `// FormEncryptionService` yorum-stub — kayıtlı DEĞİL (Gap 1).**

## Entity'ler (`Entities/`)
- **FormDefinition** — Slug/Name/Status(0 taslak/1 yayın/2 arşiv)/Version/`IsPublic`/`IsAnonymous`/**`IsEncrypted:30`**/`LinkedProcessId`/`TriggersWorkflowId`(soft-ref). Nav: Fields/Submissions/PublicTokens/Versions.
- **FormField** — Order/FieldKey(form-içi unique)/`FieldType`byte(`FormFieldType.cs` 13 sabit)/Options/ValidationRules/ConditionalLogic(raw JSON)/DataElementMaps.
- **FormFieldDataElementMap** — soft FK `DataElementId`(int, nav YOK)/UsageType.
- **FormDefinitionVersion** — immutable snapshot: Version/`SchemaJson`(survey-core). Silinmez (Restrict).
- **FormSubmission** — `FormVersionId` **required**/SubmittedById(null=anonim)/`PublicTokenId`/Status/`LinkedProcessInstanceId`/`WorkflowInstanceId`(soft). Nav: Values/Files.
- **FormSubmissionFieldValue** — FieldKey denorm + 4 tipli kolon (ValueText/Number/Date/Bool)/`ValueFileId`/**`IsEncrypted:32`** (write-only, hiç okunmuyor). Number `HasPrecision(18,4)`.
- **FormSubmissionFile** — izole disk (`DiskPath` ContentRoot-relative). FK `NoAction` (çoklu-cascade path guard, `FormsModule.cs:156-162`).
- **PublicFormToken** — `TokenHash` byte[] SHA256 (HMAC değil)/ExpiresAt/MaxUses/UsedCount.

## Service'ler
- **FormDefinitionService** — CRUD + `PublishAsync(:84-127)`: FormPublishValidator (blocking: HasSubmittableField/EmptyChoice; non-block: UnmappedWarnings) → `FormSchemaBuilder.BuildSchemaJson` → yeni FormDefinitionVersion insert → Status=1/Version=N. **Versiyonlama TEK burada; publish sonrası alan editi otomatik yeni versiyon YARATMAZ (republish gerek).**
- **FormSubmissionService(:23)** — submit pipeline (aşağıda). Deps: `db, FormValidationService, FormFileStorage`.
- **FormSchemaBuilder** — pure static, FormField[]→survey-core JSON (`MapType:81-96`).
- **FormValidationService**(orchestrator) + **FormFieldValidator**(pure) — server-authoritative, client survey-core'u tekrar doğrular (client güvenilmez). Malformed JSON→fail-closed.
- **PublicTokenService** — random+SHA256. `ConsumeAsync` atomik `ExecuteUpdateAsync`(:74-80); `RefundAsync`(:83-88) submit fail'de slot iade.
- **AntiSpamGuard**(pure honeypot→timing→rate) + **SubmitRateLimiter**(MemoryCache StrongBox+Interlocked).
- **FormFileStorage** — 2-faz decode(pure, magic-byte sniff `:126-133` PDF/PNG/JPG/DOCX)/write(path-traversal guard `:90-94`)/TryDeleteAll(rollback).
- **DataElementLookupService** — cross-module KVKK köprüsü, raw SQL, fail-closed exists.

## Controller'lar
- **FormDefinitionController** (`[Authorize(Roles=admin)]`) — CRUD/Publish/Archive/Restore/CreatePublicLink/*Field/*DataElement/**DownloadFile(:155-171 path-guard)**. Her mutasyon `IAuditLog.LogAsync` (`form_<verb>`). Firma/User claim'den.
- **FormController** (`[Authorize]`) `/Forms/f/{slug}` iç submit.
- **PublicFormController** (`[AllowAnonymous]`) `/Forms/p/{formId}/{token}` — Submit(:61-128): token doğrula→published+public→AntiSpam→değer topla→**consume ÖNCE**(:110)→SubmitAsync→fail'de Refund(:120).

## Submit pipeline (`FormSubmissionService.SubmitAsync:29-141`)
1. FormDefinition reload id+firma+Status==1 (caller state güvenilmez). 2. latestVersion çöz (yoksa fail). 3. ValidateAsync → alan hataları. 4. anonim/token gate. 5. **2-pass dosya**: pass1 decode(disk yok, orphan-avoid), pass2a `BuildFieldValue`(:143-166) non-file, pass2b disk write+SaveChanges tek try (fail→TryDeleteAll). 6. submission.Id döner.

## Konvansiyon (birebir taklit et)
`ServiceResult<T>` (business fail throw değil) · `AsNoTracking()` her read · `IAuditLog.LogAsync` her mutasyon (`form_<verb>`) · `[Authorize(Roles=admin)]`+`[ValidateAntiForgeryToken]` · ownership: her method FirmaId re-verify (child nav'da bile) · malformed JSON/crypto fail-closed, opsiyonel cross-module fail-open · pure static (Builder/Validator/Orderer/AntiSpam) ile DB-orchestrator ayrı.

## 3 GAP — tam insertion noktası (Plan 56 M-A)
**Gap 1 Şifreleme ✅ KAPANDI (encrypt-on-write, 2026-07-03):** `FormEncryptionService.cs` yazıldı (DataProtection `IDataProtector` purpose `Forms.FieldEncryption.v1`, Encrypt/TryDecrypt); `FormsModule.cs:34` `AddScoped<FormEncryptionService>` kayıtlı; `BuildFieldValue(:143-174)` `encrypt` param — form `IsEncrypted` ise ValueText'e şifreli blob + `IsEncrypted=true`, tipli kolonlar null; `SubmitAsync:107` `def.IsEncrypted` geçiyor. **KALAN:** decrypt tüketici **yetki-gated** (İhbar Komitesi rolü) — Gap 2 submission-detail ile gelecek; `FormEncryptionService.TryDecrypt` hazır.
**Gap 2 Submission admin ✅ KAPANDI (2026-07-03):** `FormSubmissionQueryService.cs` (List/Detail/Export) + `FormDefinitionController` Submissions/SubmissionDetail/ExportSubmissions (admin, audit) + Submissions.cshtml/SubmissionDetail.cshtml + Details "Yanıtlar" link + ClosedXML csproj. **Decrypt YETKİ-GATED:** `canDecrypt = IsInRole("ihbar-komitesi")||"admin"` → yetkisize "🔒 Şifreli" maske; ciphertext client'a gitmez. **Export şifreli alanı HER ZAMAN maskeler** (bulk-exfil yok, `ExportRowCap=5000` guard). Audit: view/decrypt(gerçek çözülünce)/export(sayılı). Firma-izolasyon submissionId+firmaId. security+code scan 0 CRIT/HIGH. **KALAN (tracked):** list pagination/filter-bar yok (5000 üstü form için); export UTC vs ekran local tarih.
**Gap 3 Template seed:** hiç `INSERT FormDefinitions` yok. Idempotent desen: KVKK `03_KvkkDataElementSeed.sql` (VALUES+WHERE NOT EXISTS). **KRİTİK:** Status=1 ama version snapshot yoksa SubmitAsync(:36-41) fail + Render 404 → template'i **Status=0 taslak** seed et (admin Publish'lesin, FormSchemaBuilder JSON'unu SQL'de tekrarlamaktan kaçın).

## İlişkili
- Plan 56 (program) + `mosaik-security` (Gap 1 AES key) + `kvkk-veri-envanteri` (ihbar/DSAR domain) + `mosaik-csharp-razor`/`ui-ux-pro-max` (Gap 2 view).
