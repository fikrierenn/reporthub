# Plan 34 — SOP / Prosedür Yönetimi (Mosaik.Modules.SOP)

**Tarih:** 2026-05-14
**Yazan:** Fikri / Claude
**Durum:** `Taslak` — kullanıcı onayı bekliyor

**Tier:** 3 (yeni modül + ayrı csproj + schema + UI + kullanıcı-görünür)
**Tahmini süre:** 2-3 hafta (~50-80 saat, faz F opsiyonel hariç)
**VISION sırası:** [vNext kalbi #1](../docs/VISION.md#1-sop--prosedür-yönetimi-plan-yok--en-hızlı-kazanım) (en hızlı kazanım, ROI 1 ay)

---

## 1. Problem

BKM'de SOP / prosedür yönetimi şu an Word dosyalarıyla yapılıyor:

- **27 İK prosedürü + 44 form** NAS, kişisel klasörler, email ekleri arasında dağılmış.
- **Versiyon kontrolü yok** — "hangisi güncel?" sorusu her ay tekrarlanıyor. Eski versiyonun aktif olarak okunması yaygın.
- **Okundu disiplini yok** — yeni çalışan eski versiyon okur, mevcut çalışan değişikliklerden haberdar olmaz. Yasal/operasyonel risk.
- **Onay akışı manuel** — email zinciri ile yönetiliyor, denetim izi zayıf.
- **Geriye dönük denetim yok** — "Şubat 2025'te hangi prosedür geçerliydi?" sorusuna kimse cevap veremiyor.

Mosaik'te 3 olgun altyapı var: **Tamim** (block-based content + dosya ek + notification), **Approval** (yarım — designer UI yok ama `ApprovalRequest+ApprovalStep` entity'leri Mosaik.Core'da), **OrgChart + UserDataFilter** (departman ataması). SOP bu üçünün ortak müşterisi — yeni altyapı çoğunlukla **mevcudu reuse**, sıfırdan değil.

---

## 2. Scope

### Kapsam dahili

**Modül iskeleti (ADR-015 uyumlu):**
- Yeni csproj: `Mosaik.Modules.SOP` ([ADR-015](../docs/ADR/015-new-modules-separate-assembly.md) — yeni modül zorunlu ayrı assembly)
- `SopModule : IMosaikModule` self-register
- `Mosaik.Modules.SOP/Database/` migration zinciri (modül-içi)
- AppModules tablosuna SOP kaydı (sidebar enable/disable Plan 12)

**Domain entity'leri (4 yeni):**
- `SopDocument` — Title, Description, Department(FK), Category, OwnerUserId, IsActive
- `SopVersion` — SopDocumentId(FK), VersionNumber, ContentJson (block-based, Tamim quill reuse), AttachmentFiles, EffectiveDate, SupersededDate, Status (Draft/Pending/Approved/Archived)
- `SopReadReceipt` — SopVersionId(FK), UserId(FK), ReadAt, ConfirmedAt, ReminderSentCount
- `SopApprovalSubmission` — SopVersionId(FK), ApprovalRequestId(FK to `Mosaik.Core.Workflow.ApprovalRequest`) — Workflow Designer (Plan 36) hazır olduğunda upgrade noktası

**Admin CRUD:**
- SopController.cs (admin endpoints): Create, Edit, NewVersion, Submit-for-approval, Approve, Archive
- Block content editor — Tamim'in Quill setup'ı reuse (cross-modül asset paylaşımı yerine `Mosaik.Modules.SOP/wwwroot/quill-setup.js` kopyası — modül izolasyonu için kabul edilen tekrar)
- Versiyon yönetimi: yeni onaylanan versiyon → öncekinin `SupersededDate` set, 3 ay sonra `Status=Archived`
- Departman ataması: manuel multi-select (Plan 18B HR sync sonrası otomatik öneri eklenir)

**Onay akışı (basic 3-adımlı, Plan 36 öncesi):**
- Sıralı 3 adım: Yazan → Departman Yöneticisi → İK Yetkilisi
- `ApprovalRequest` entity'si kullan (Mosaik.Core'da hazır), `StepOrder` ile
- Bildirim: her adım için sıradaki onaylayıcıya `INotificationService` (Plan 17 Faz H altyapısı)
- Email: Plan 31 SMTP caller (Plan 32 hazır olduğunda — şimdilik in-app notification yeterli)
- **Plan 36 Workflow Designer hazır olduğunda:** SOP `ApprovalRequest`'i okumaya devam eder, sadece designer UI ile akış değiştirilebilir hale gelir (refactor değil, **interface uyumlu upgrade**)

**Okundu disiplini:**
- Yeni onaylanan SOP atandığı departman üyelerine zorunlu okuma görevi
- 30 günlük read deadline (admin tarafında configurable — `SopDocument.ReadDeadlineDays`)
- Reminder: 7 gün kala notification + 1 gün kala email
- "Okudum + onayladım" butonu → `SopReadReceipt.ConfirmedAt` set + departman yöneticisine bildirim

**User-facing:**
- "Prosedürlerim" sayfası — atanmış SOP'lar, read deadline countdown, okundu durumu
- "Tüm SOP'lar" arşiv arayüzü — filtre (departman/kategori/durum) + arama
- SOP detay sayfası — block content render + dosya ek listesi + version history popover
- Sidebar entry: "Prosedürler" (admin'de SOP yönetimi, user'da okumalarım)

**Audit:**
- Tüm event'ler `IAuditLog` ile: `sop_created`, `sop_version_submitted`, `sop_approved`, `sop_archived`, `sop_read`, `sop_confirmed`, `sop_reminder_sent`

### Kapsam dışı

- **Workflow Designer entegrasyonu** — Plan 36 yapıldığında upgrade (interface uyumlu)
- **AI özet/summary** — Plan 16.5 caller olarak ileride eklenir, MVP'de manuel özet alanı
- **Form Builder entegrasyonu** — SOP sonu test/sınav, Plan ileri
- **Comment / Mention** — Plan 35 hazır olunca SOP detayda yorum açılır (cross-modül zincirleme)
- **Tam BKM SOP migrate** — Faz F opsiyonel, isteğe bağlı
- **SOP-bazlı eğitim modülü** — ayrı plan adayı (Plan 41+)
- **Mobile app** — web-only

### Etkilenen dosyalar (tahmin)

**Yeni csproj (Mosaik.Modules.SOP/):**
- `Mosaik.Modules.SOP.csproj` (~30 satır)
- `SopModule.cs` (`IMosaikModule` impl, ~80 satır)
- `Controllers/SopController.cs` (admin CRUD, ~400 satır — hard-limit altında)
- `Controllers/SopMyController.cs` (user-facing "okumalarım", ~150 satır)
- `Models/SopDocument.cs`, `SopVersion.cs`, `SopReadReceipt.cs`, `SopApprovalSubmission.cs` (her biri ~40-60 satır)
- `Services/SopService.cs` (~250 satır), `SopReadReceiptService.cs` (~150 satır), `SopApprovalService.cs` (~200 satır)
- `ViewModels/Sop*ViewModel.cs` (4-5 dosya, her biri 30-60 satır)
- `Views/Sop/` Admin (Index, Edit, NewVersion, ApprovalQueue) + Areas/Sop/Views/My (Index, Detail)
- `Database/01_CreateSopTables.sql`, `02_SeedSopCategories.sql`, `03_AddSopAppModule.sql`, `04_SopReadDeadlineDefault.sql`

**Ana proje (mosaik):**
- `Mosaik.sln` — yeni proje ekleme
- `Mosaik/Mosaik.csproj` — `ProjectReference` ekleme (ADR-002 modül kuralı)
- `Mosaik/Views/Shared/_AppLayout.cshtml` — sidebar SOP entry (otomatik, Plan 12 AppModule ile)

**Test (Mosaik.Tests):**
- `SopServiceTests.cs` — version chain logic, supersede, archive timing
- `SopReadReceiptServiceTests.cs` — reminder schedule, deadline calc
- `SopApprovalServiceTests.cs` — 3-step flow, step transitions

**Tahmini boyut:** ~25-30 dosya / ~3000-3500 satır (en büyük dosya 400 satır, hard-limit içinde).

---

## 3. 5 lens (plan-first.md disiplini)

🔴 **Contrarian:** Plan 36 Workflow Designer hazır olmadan SOP'ye basic flow ekleyip sonra refactor etmek 2x iş riski. **Cevap:** `ApprovalRequest` entity Mosaik.Core'da zaten var. Basic flow sadece 3 hard-coded step. Plan 36 designer UI eklendiğinde **interface aynı**, sadece step'ler configurable olur — SOP kodu refactor olmaz, `ApprovalRequest` okumaya devam eder.

🔵 **First Principles:** Gerçek problem "SOP nerede?" değil **"hangi SOP geçerli ve okundu mu?"** — versiyonlama + read receipt + audit en yüksek değer. AI özet, form, gelişmiş arama vs. ikincil. Plan bu önceliği yansıtıyor (Faz B+C+D Core; Faz E bildirim; Faz F opsiyonel migration).

🟢 **Expansionist:** SOP yapısı (block content + version + audit + onay + read receipt) **tüm policy belgelerine** uygundur — KVKK metni, kalite el kitabı, ISO 27001 dokümanları, etik kurallar. SOP modülünü ileride "Policy Documents" olarak genişletme imkanı var; `SopDocument.Category` enum bunu destekler ama scope dışı (ayrı plan).

⚪ **Outsider:** "Documents modülü versiyonlama yapıyor (Plan 27), neden ayrı SOP?" sorusu gelir. **Cevap:** Documents jenerik DMS (yükle, paylaş, indir). SOP **policy enforcement** — okundu zorunlu, departmana bağlı, onay zinciri, audit yasal denetim için. Farklı domain, farklı lifecycle (yıllık review, supersede zinciri). Yan yana yaşar, çakışmaz.

🟡 **Executor:** Pazartesi sabahı 09:00:
1. `dotnet new classlib -n Mosaik.Modules.SOP -o Mosaik.Modules.SOP`
2. `Mosaik.sln` ekleme + `Mosaik.Modules.SOP.csproj` `<ProjectReference Include="../Mosaik.Core/..." />`
3. `SopModule.cs` `IMosaikModule` iskelet (Circular'dan kopyala-uyarla)
4. İlk commit: `feat(sop): module scaffold (plan: 34)`

---

## 4. Alternatifler

### A: Documents modülünün altında SOP type

**Açıklama:** SOP'ı Documents'in özel bir tipi (`DocumentType.SOP`) yap, ayrı modül yapma.
**Reddetme sebebi:** Policy enforcement (okundu zorunlu, onay zinciri, departman ataması) Documents'in jenerik DMS yapısını kirletir. Documents lifecycle: yükle → indir → arşivle. SOP lifecycle: yaz → onayla → ata → okundu izle → versiyon güncelle → eskiyi supersede et. Farklı lifecycle = farklı modül.

### B: Tamim modülüne SOP type ekle

**Açıklama:** Tamim'in `Type` enum'una "SOP" ekle, aynı modülde yaşat.
**Reddetme sebebi:** Tamim resmi yazı/duyuru — tek seferlik, kısa ömürlü, "okundu" yeterli. SOP prosedür — uzun ömürlü, versiyon zinciri, yıllık review, onay akışı. Tamim modülü block content + notification veriyor (reuse hedefli) ama lifecycle ortak değil. VISION'da Tamim'in Type enum'u **Duyuru** için ayrıldı (formal vs informal), SOP değil.

### C: Plan 36 Workflow Designer'ı önce yap, sonra SOP

**Açıklama:** Generic engine + designer önce, SOP onun ilk müşterisi.
**Reddetme sebebi:** VISION değer sıralaması SOP > Comment > Workflow Designer (en hızlı kazanım önce). Workflow Designer 3-4 hafta + designer UI, SOP onsuz da basic 3-step flow ile çalışır (ApprovalRequest entity hazır). Sıra: SOP basic flow → Plan 36 designer → SOP designer'a upgrade (interface uyumlu, refactor değil).

### D: Yeni modül + basic flow (seçilen)

**Açıklama:** Mosaik.Modules.SOP ayrı csproj, ApprovalRequest entity ile basic 3-step flow, Plan 36 hazır olduğunda upgrade.
**Sebep:** VISION önerisi. ROI 1 ay (prosedürler zaten yazılı, sadece taşınacak). Tamim altyapısı %80 reuse. Plan 36'ya bağımlılık sıfır.

---

## 5. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| BKM 27 SOP migration'da Word format kayıpları | orta | yüksek | Faz F opsiyonel, MVP'de manuel yükleme yeterli. Format dönüşümü için Pandoc kullanım planı ileri. |
| Departman ataması yanlış (HR sync Plan 18B beklemede) | düşük | yüksek | Manuel multi-select ile başla. Plan 18B sonrası "departmandaki herkes" auto-suggest. |
| 30 günlük read deadline çok sıkı | düşük | orta | `ReadDeadlineDays` admin tarafında configurable, varsayılan 30, SOP bazında override edilebilir. |
| Plan 36 sonrası refactor ihtiyacı | düşük | düşük | `ApprovalRequest` interface uyumlu — designer UI step'leri configurable yapar, SOP kodu okumaya devam eder. |
| Cross-modül asset paylaşımı (Quill) | düşük | orta | Modül izolasyonu için Quill setup'ı SOP modülüne kopyala (kabul edilen tekrar). Ortak Quill base library Plan 16.5'a ekleme adayı (ayrı iş). |
| Sidebar entry yanlış izinlerle açılır (her user görür) | yüksek | orta | `[Authorize(Roles="admin,sop-editor,sop-reader")]` net rol ayrımı. SOP reader/editor role'leri migration 03'te seed edilir. |
| Test coverage <%10 (mevcut sorun + yeni modül) | orta | orta | Faz B+C için unit test zorunlu (test-discipline.md). Done criteria: en az 10 test. |

---

## 6. Done Criteria

- [ ] `Mosaik.Modules.SOP` csproj build temiz (0 hata 0 uyarı)
- [ ] `IMosaikModule` self-register çalışıyor (sidebar entry görünür, AppModules.IsEnabled=1)
- [ ] Migration idempotent (`Database/01-04_*.sql` 2x çalıştırılınca hata yok)
- [ ] Admin SOP CRUD smoke test geçiyor (create → version ekle → ata → onayla → arşivle)
- [ ] User "Prosedürlerim" sayfası atanmış SOP listesini gösteriyor + deadline countdown
- [ ] "Okudum + onayladım" işaretleme → `SopReadReceipt` + audit log
- [ ] Notification: yeni SOP yayınlanınca departman üyelerine push (`INotificationService`)
- [ ] **Test:** en az 10 unit test (`SopServiceTests`, `SopReadReceiptServiceTests`, `SopApprovalServiceTests`)
- [ ] **Test çalıştırıldı:** `dotnet test --filter "FullyQualifiedName~Sop"` → 0 başarısız ([test-discipline.md](../.claude/rules/test-discipline.md) zorunlu)
- [ ] `vnext-entity-port` skill pattern'ine uygun ([SKILL.md](../.claude/skills/vnext-entity-port/SKILL.md))
- [ ] [ARCHITECTURE_MAP.md](../docs/ARCHITECTURE_MAP.md) module ayrımı bölümü güncellendi
- [ ] Plan dosyası `plans/archive/` taşındı + journal'da özet

---

## 7. Rollback Planı

**Modül kapatma (DB-driven, Plan 12):**
```sql
UPDATE AppModules SET IsEnabled = 0 WHERE ModuleKey = 'sop'
```
Sidebar entry kaybolur, kullanıcı erişimi engellenir. Veri kaybı yok.

**Modül silme (geri dönüş):**
```bash
# Migration revert (manuel SQL):
DROP TABLE SopApprovalSubmissions, SopReadReceipts, SopVersions, SopDocuments;
DELETE FROM AppModules WHERE ModuleKey = 'sop';

# Csproj kaldırma:
dotnet sln Mosaik.sln remove Mosaik.Modules.SOP/Mosaik.Modules.SOP.csproj
rm -rf Mosaik.Modules.SOP/
```

**Git rollback:**
```bash
git revert <plan-34-merge-commit>
# veya tek tek revert: feat(sop): Faz A, B, C, D, E
```

---

## 8. Adımlar / Fazlar

### Faz A — Modül iskeleti (4-6 saat) — S-01..S-04
1. [ ] **S-01** `Mosaik.Modules.SOP` csproj + sln ekleme
2. [ ] **S-02** `SopModule : IMosaikModule` impl (Circular'dan template)
3. [ ] **S-03** `Mosaik.csproj` ProjectReference ekle + build smoke
4. [ ] **S-04** `Database/03_AddSopAppModule.sql` — AppModules.SOP kaydı, sidebar visible

### Faz B — Entity + Migration (4-6 saat) — S-05..S-08
5. [ ] **S-05** 4 entity (`SopDocument`, `SopVersion`, `SopReadReceipt`, `SopApprovalSubmission`)
6. [ ] **S-06** `Database/01_CreateSopTables.sql` (idempotent, FK + index)
7. [ ] **S-07** `Database/02_SeedSopCategories.sql` (İK, Operasyon, IT, Finans, Kalite, Etik default kategoriler)
8. [ ] **S-08** `ConfigureModelBuilder` (modül `IMosaikModule`) + `dotnet build` test

### Faz C — Admin CRUD (12-16 saat) — S-09..S-14
9. [ ] **S-09** `SopService.cs` — Create/Update/NewVersion/SupersedeAndArchive
10. [ ] **S-10** `SopController` admin endpoints + AntiForgery + Authorize
11. [ ] **S-11** Admin views (Index, Edit, NewVersion form + Quill editor)
12. [ ] **S-12** `SopApprovalService.cs` — 3-adımlı flow + `ApprovalRequest` entegrasyonu
13. [ ] **S-13** Departman ataması UI (multi-select)
14. [ ] **S-14** Unit test: `SopServiceTests` (version chain, supersede logic) — **çalıştır + 0 başarısız**

### Faz D — User-facing (8-12 saat) — S-15..S-19
15. [ ] **S-15** `SopMyController` — "Prosedürlerim" + "Tüm SOP'lar"
16. [ ] **S-16** SOP detay view (block render + dosya ek + version history popover)
17. [ ] **S-17** "Okudum + onayladım" akışı (`SopReadReceipt`)
18. [ ] **S-18** Read deadline countdown UI (`x-data` Alpine)
19. [ ] **S-19** Unit test: `SopReadReceiptServiceTests` — **çalıştır + 0 başarısız**

### Faz E — Bildirim (4-6 saat) — S-20..S-22
20. [ ] **S-20** Yeni SOP onaylanınca departman üyelerine `INotificationService` push
21. [ ] **S-21** Reminder background job (7 gün kala + 1 gün kala) — Hangfire RecurringJob
22. [ ] **S-22** Email caller (Plan 31 SMTP — Plan 32 caller hazır olursa entegre, değilse skip)

### Faz F — BKM SOP migrate (4-8 saat, OPSİYONEL) — S-23
23. [ ] **S-23** 27 Word dokümanını ilk versiyon olarak import (manuel veya basit Pandoc script)

**Toplam:** ~32-54 saat (Faz F hariç 28-46 saat). 2-3 hafta paralel iş.

> TODO.md'ye Faz A-E adımları S-01..S-22 olarak eklenecek.

---

## 9. İlişkili

- **ADR:** [ADR-002](../docs/ADR/002-modular-monolith.md) (modüler monolit), [ADR-015](../docs/ADR/015-new-modules-separate-assembly.md) (yeni modül ayrı assembly)
- **Yön belgesi:** [VISION.md §4 #1](../docs/VISION.md#1-sop--prosedür-yönetimi-plan-yok--en-hızlı-kazanım)
- **Referans modül:** [Plan 17 (Tamim)](17-tamim.md) — Quill editor + block content + notification altyapısı reuse kaynağı
- **Sonraki bağımlı planlar:**
  - [Plan 35 (Comment/Mention)](#) — SOP detayda yorum açma (cross-modül zincirleme)
  - [Plan 36 (Workflow Designer)](#) — SOP basic 3-step flow → configurable designer upgrade
- **Skill:** [`vnext-entity-port`](../.claude/skills/vnext-entity-port/SKILL.md) — bu planın implementasyon şablonu
- **Test disiplini:** [`.claude/rules/test-discipline.md`](../.claude/rules/test-discipline.md)
- **Konuşma referans:** `docs/journal/2026-05-14.md` (yazılınca)

---

## 10. Onay

> [Plan-first kuralı](../.claude/rules/plan-first.md): kullanıcı onay verene kadar implement edilmez.

- [ ] Plan kullanıcıya gösterildi: 2026-05-14
- [ ] Geri bildirim alındı (varsa düzeltildi)
- [ ] Onay alındı: `<tarih, kullanıcı>`

**Onay öncesi açık sorular (kullanıcı kararı bekliyor):**

1. **Read deadline varsayılan değer** — 30 gün önerildi. BKM'de farklı politika var mı?
2. **3-adımlı onay flow** — Yazan → Departman Yöneticisi → İK Yetkilisi. Doğru mu? Bazı SOP tipleri için (örn. IT prosedürleri) farklı zincir gerekli mi?
3. **Departman ataması zorunluluğu** — Bir SOP yayınlanırken **en az 1 departman seçilmesi zorunlu** mu, yoksa "tüm şirket"e bırakılabilir mi?
4. **Faz F (BKM SOP migration)** — Bu plan kapsamında yapılsın mı, ayrı plan mı? 27 SOP migrate eforu 4-8 saat ama Word→block dönüşümü format kaybı yaratabilir.
5. **SOP reader/editor role'leri** — `sop-editor` (yeni SOP yazabilir) + `sop-reader` (sadece okur) ayrımı yapılsın mı, yoksa `admin` + `<user>` yeterli mi?
6. **Quill editor tekrar** — Tamim'deki Quill setup'ı SOP modülüne **kopyalanacak** (modül izolasyonu için kabul edilen tekrar). Alternatif: Plan 16.5'a "Mosaik.Core.Editor" base library eklemek (ayrı iş). Kabul ediyor musunuz?
