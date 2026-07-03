# Plan 56 — Modül Tamamlama Programı (audit-driven)

**Tier:** 3 (çok-modül, schema+security+UX, kullanıcı-görünür)
**Durum:** ⏳ TASLAK 2026-07-03 — onay bekliyor
**Kaynak:** 20-modül uçtan-uca tamamlanmışlık denetimi (7 paralel agent, 2026-07-03).
**Yöntem:** Modül-modül. Her önemli modül geliştirmeden ÖNCE **Faz 0: kılcal mimari dalış → uzman skill**:
1. `code-explorer` agent ile o modülü entry→data→pattern→gap kılcalına kadar trace.
2. → **`.claude/skills/mosaik-<modül>-expert/SKILL.md`** üret (dalış çıktısından): kod mimarisi (file:line entry→data) + içerik/domain mimarisi + modül mimarisi (IMosaikModule/DI/cross-module sınır) + geliştirme pattern'leri (o modülün konvansiyonu) + bilinen gapّ'ler. **Auto-trigger:** o modüle dokununca yüklenir.
3. SONRA: **danış (uzman skill + cross-cutting) → kod → full scan (4 agent) → preview → commit** → sonraki modül.

**Advisor kararı (kullanıcı 2026-07-03):** Her önemli modülün **kendi uzman skill'i** var (`mosaik-forms-expert`, `mosaik-documents-expert`, `mosaik-workflow-expert`, ... ~8 önemli modül). Gerekçe: skill auto-trigger — o modülde çalışınca derin bağlam otomatik yüklenir, doc gibi elle okuma gerekmez. Sınır: **~8 önemli modül**, 20'si değil (footprint dengesi). Cross-cutting advisor'lar (mosaik-security/csharp-razor/css/js/portal-danismani/kvkk/ui-ux) "nasıl yazılır" için korunur; uzman skill "bu modül nasıl kurulu + nasıl geliştirilir" için.

Bir modül bitmeden sonrakine geçme. TAM modüllere kod-DOKUNMA (coding-discipline) — ama uzman skill + kılcal anlama yine üretilir (gelecek geliştirme + gap kapatma için).

## Denetim özeti (temel)

**18 TAM + temiz (DOKUNULMAZ):** Reports, Dashboard, Contracts, Obligations, Calendar, Tamim/Circular, Compliance, OrgChart, GorevTanimlari (Sema+Mapping), Comments, Escalation, Ai, Search, Inbox, Notifications, SOP, KVKK, Workflow (çekirdek).

**Çalışma gerektiren:** aşağıdaki M-A..M-E. Öncelik risk/değer sırasında.

---

## M-A — Forms tamamlama 🔴 (en yüksek — veri-koruma riski + işlevsel boşluk)

**Modül:** `Mosaik.Modules.Forms`. Çekirdek (12 alan tipi, versiyon, public link, dosya/imza, DataElement map, anti-spam) TAM. Eksik 3 faz:

- **Gap 1 (🔴 YÜKSEK — yanlış vaat):** `IsEncrypted` flag var (`FormDefinition.cs:9,30`, `FormSubmissionFieldValue.cs:6,32`) + CRUD round-trip, ama `FormSubmissionService.BuildFieldValue` (`FormSubmissionService.cs:143-166`) flag'e bakmadan **düz metin** yazıyor. `FormEncryptionService` yorum-stub (`FormsModule.cs:35`). Admin "şifreli ihbar formu" işaretler → plaintext saklanır.
- **Gap 2 (🔴 işlevsel boşluk):** Faz 6 YOK — submission admin liste + Detay + Excel export hiç yazılmamış (`FormDefinitionController` grep 0). **Admin toplanan yanıtları göremiyor/dışa aktaramıyor.**
- **Gap 3 (🟠):** Faz 7 YOK — template seed SQL yok (8 hazır form: DSAR/İhbar/Aday/İhlal/Rıza/Engelli/DPA/Review).

**KAPSAM GENİŞLEDİ (audit + rakip-analiz + wiring-kontrol 2026-07-03):** Forms şu an "topla ama görme/yönlendirme yok" seviyesinde. Rakip-baseline + KVKK-bağlam önceliği:
- 🔴 **G1 Şifreleme** ✅ (encrypt-on-write; decrypt Gap 2'de)
- 🔴 **G2 Submission admin liste/detay/export** — yanıt görüntüleme+Excel; **decrypt yetki-gated** (İhbar Komitesi rol → çöz, yoksa 🔒 maske). = şifreleme E2E kapanışı.
- 🔴 **G3 ConditionalLogic bağlama** — `FormField.ConditionalLogic` DB'de ölü alan; survey-core `visibleIf` + server required-if. Branching = table-stakes (KVKK/DSAR/ihbar).
- 🔴 **G4 Submit-sonrası bildirim** — SMTP(M1) var, köprü yok; submit sessiz. Form sahibine/role email+notification.
- 🟠 **G5 Template seed** (8 form: DSAR/İhbar/Aday/İhlal/Rıza/Engelli/DPA/Review, Status=0 taslak).
- 🟠 **G6 Multi-page/wizard** (uzun form) — DEFER.
- 🟠 **G7 İhbar anonim iki-yönlü diyalog** (case-no, EU Whistleblowing Directive) — ihbar ciddi use-case ise ayrı iş.
- ⚪ **YAPMA (consumer-polish):** ödeme, calculation, quiz, i18n, prefill, Zapier connector, Typeform konuşmalı UX — on-prem iç portalda değersiz.

**Sıra:** G2 (submission-admin, şifreleme E2E kapatır) → G3 (conditional) → G4 (bildirim) → G5 (template). G6/G7 defer, M-D workflow-trigger ayrı.

**Danış (kod öncesi):**
- G1 ✅ → `mosaik-security` (DataProtection, yapıldı).
- G2 → decrypt-gate `mosaik-security` (rol-gate + maske + audit); export `VerbisExporter` deseni (ClosedXML+Safe); view `ui-patterns`+`mosaik-csharp-razor`.
- G3 → `mosaik-forms-expert` (FormSchemaBuilder visibleIf) + `mosaik-js-expert`.
- G4 → `mosaik-security` (email XSS/injection) + Core `INotificationService`/`IEmailService`.
- **Her G sonrası `mosaik-forms-expert` skill güncelle** (Gap 1'de stale kaldı dersi).

**Fazlar:**
1. **FormEncryptionService** (AES-256-GCM veya DataProtection) → `BuildFieldValue` `IsEncrypted` ise şifrele; submission okuma decrypt (yetki-gated). Read-path (SemaService yok — Forms render/detail) plaintext göstermez, sadece yetkili. Migration: ValueText yeterli (nvarchar), IsEncrypted zaten var.
2. **Submission admin:** `FormDefinitionController.Submissions(formId)` liste + `SubmissionDetail(id)` + `ExportExcel(formId)` (ClosedXML) + `form_submission_viewed` audit. Şifreli alan yetkisiz için maskeli.
3. **8 template seed** (`Database/06_SeedFormTemplates.sql`, idempotent).

**Done:** İhbar formu submit → DB'de ValueText **şifreli** (SELECT ile doğrula) · admin submission listesi + Excel indir çalışır · 8 template görünür · full scan 0 CRIT/HIGH · preview E2E · test (encrypt round-trip + export).

---

## M-B — Documents tamamlama 🟠

**Modül:** `Documents` (ana proje). Çekirdek CRUD TAM. Eksik:
- **Gap 1:** `DocumentPermission` ölü kod — `Models/DocumentPermission.cs` + `Services/DocumentPermissionService.cs` + `MosaikContext.cs:65` DbSet + migration `74` var, `DocumentsController` 0 referans. **Karar:** ya belge-bazlı izni controller/UI'a bağla, ya ölü kodu sil (footprint-ladder).
- **Gap 2:** Versiyon geçmişi write-only — re-upload arşivliyor (`DocumentsController.cs:156-167`) ama listeleme/restore UI yok. Version history görünümü + restore action.

**Danış:** `mosaik-portal-danismani` (permission modeli gerçekten gerekli mi yoksa sil mi — kapsam kararı) + `mosaik-csharp-razor`.

**Fazlar:** (1) Permission kararı (bağla/sil) — danış sonrası. (2) Version history UI + restore.

**Done:** karar uygulandı (ölü kod yok) · versiyon geçmişi görünür + restore çalışır · scan+preview+test.

---

## M-C — Ölü kod temizliği 🟡

- **Workflow `ApprovalService`/`ApprovalRequest`/`ApprovalStep`** — DI+7 test var, hiç çağrılmıyor (WorkflowEngine yerinden etti). **Karar:** sil (test dahil) veya "deprecated" işaretle. before-major-change çek-listesi + grep doğrula.
- **KVKK `XlsxImporter` orphan** — `ImportAsync` tetikleyici yok; view `--import-kvkk` CLI'sine atıf yapıyor ama komut yok. **Karar:** ya CLI/admin action ekle (re-import lazımsa), ya view metnini düzelt + orphan not.
- **SOP `sop-editor` rolü** — `[Authorize(Roles="admin,sop-editor")]` ama rol seed yok. Seed ekle veya attribute'tan çıkar.

**Danış:** `mosaik-portal-danismani` (sil vs koru kararları). **Done:** grep temiz, dead-ref yok, build+test yeşil.

---

## M-D — Cross-module workflow oto-tetikleme 🟡 (feature)

`FormSubmissionService.cs:138` "workflow/ProcessInstance trigger stub" — Forms/KVKK süreçleri workflow'u otomatik başlatmıyor (sadece manuel `/Workflow/Trigger`). Form submit → ilgili WorkflowDefinition varsa `WorkflowEngine.StartAsync` otomatik.
**Danış:** `mosaik-portal-danismani` (tetikleme modeli, ADR-002 cross-module — Forms modülü WorkflowEngine'i nasıl çağırır: Core abstraction `IWorkflowTrigger`?). **Not:** Plan 42 (Process Runtime) ile örtüşür — önce onunla hizala.

---

## M-E — Belge/plan/vizyon hijyeni 🟢 (ucuz, kod-dışı)

`consolidate-mosaik` skill ile:
- **Arşiv:** Plan 44 (kod bitti), 27/32/48/14 (superseded/park).
- **Park işareti:** 45/46/47.
- **Stale-claim düzelt:** Plan 41 "onay bekliyor"→"Faz 0-4 ✅, 5-7 M-A'da"; Plan 36 "onaylandı"→"motor canlı, designer var".
- **VISION senkron:** KVKK canlı · GorevTanimlari modülü ekle · RESEARCH 3 düzeltmesini Plan 40/41/42'ye fold · Plan 49/50/51 CUT-vs-onaylı karara bağla · QuestPDF stale temizle.
- **CLAUDE.md:** test sayısı 564→573, controller listesi güncel.

---

## Sıra (öneri)

**M-A (Forms) → M-B (Documents) → M-C (ölü kod) → M-E (hijyen, ucuz araya) → M-D (cross-trigger, Plan 42 ile).**
Her modül tam bitip commit'lenmeden sonrakine geçilmez. Her modül sonu: plan bu dosyada ✅ + journal.

## Riskler
- Çalışan koda dokunmak (M-B permission, M-C sil) → before-major-change grep + backup + test zorunlu.
- Şifreleme key yönetimi (M-A) → prod secret out-of-source (security-principles #5).
- Cross-module (M-D) → ADR-002 izolasyon, Core abstraction şart.

## Done (program)
- [ ] M-A Forms (encrypt + submission admin + template)
- [ ] M-B Documents (permission karar + version UI)
- [ ] M-C ölü kod temizliği
- [ ] M-D cross-trigger (Plan 42 ile)
- [ ] M-E hijyen
