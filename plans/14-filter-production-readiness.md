## Plan 14 — Filter Sistemi Production-Readiness

**Tarih:** 2026-05-07 (gece taslağı + sabah revize)
**Yazan:** Claude (Fikri yönetiminde)
**Durum:** Faz A ✅ + Faz B keşif ✅ + Faz C ✅ fiilen kapalı (2026-05-08). Faz B implement (IK sube OptionsQuery aktivasyonu) kullanıcı kararıyla "şimdilik durdur" — Plan 18 HR Sync sonrasına ertelendi. Faz D dropped (overkill). **Plan büyük ölçüde tamam**, kalan tek iş Faz B implement.
**Tier:** 3 (admin UI + servis + audit + DB seed + test, 4-6 dosya)
**Tahmini süre:** ~4 saat (Faz D düşüldü, Faz C 16.5'a bağlandı)

**Revizyon notu (2026-05-07 sabah):** D:/Dev derin keşif sonrası Plan 16 (vNext roadmap) + Plan 16.5 (Mosaik.Core shared kit) yazıldı. Plan 14 Faz C'deki `IFilterScope` interface artık **Plan 16.5 Faz B'de `IUserDataScope` olarak yaşıyor**. Plan 14 Faz C revize: kendi interface'ini yazmıyor, 16.5'tan kullanıyor. Faz D (bulk UI) Plan 18 (HR sync) sonrasına ertelendi. Faz A 10 öneri ile genişletildi (~60dk → ~90dk).

---

### 1. Problem

Bugün (2026-05-07) FilterDefinition seed (migration 31) + activate + UserDataFilter backfill (migration 32) tamamlandı. 3 filter aktif: PDKS sube, DER sube, DER urunKategori. 2 user (admin, ik) için `*` (tümü) varsayılan atandı.

**Eksik / risk noktaları (code-explorer 7 May audit):**

A. **UserDataFilter assignment audit log eksik** — `UserManagementService.SyncDataFiltersAsync` (`UserManagementService.cs:204-222`) audit'e dokunmuyor. Kim hangi kullanıcıya hangi filtreyi atadı, izlenemez. Production blocker.

B. **EditUser POST hata kurtarma'da DataFilters boş** — `AdminController.Users.cs:120-127` validation hatası dönerse partial ViewModel inline kuruluyor; `DataFilters/DataSources/FilterDefinitions` set edilmiyor → form panel boşalıyor.

C. **IK sube FilterDefinition placeholder** — migration 22'de `IsActive=0`, `OptionsQuery=NULL`. IK sistemi raporlarında sube filtresi çalışmıyor; aktive edilirse 403 başlar.

D. **`reportAccess` NativeSources kod-bağımlı** — `FilterOptionsService.NativeSources` dict yalnızca `raporGrubu` içeriyor. Yeni reportAccess filtresi (örn. IK rapor erişimi) admin GUI'den yönetilemiyor, kod ekleme şart.

E. **Bulk assign UI yok** — 5 kullanıcıya farklı kapsam atamak için 5 ayrı EditUser sayfası. Friction yüksek.

F. **`raporGrubu` reportAccess deny-by-default değil** — `InjectAsync` yalnızca `spInjection` scope'ları için zorunlu kontrol yapıyor. `raporGrubu` aktifken kullanıcının kaydı yoksa "tümünü gör" davranışı. Semantik tutarsızlık.

G. **FilterDefinition IsActive toggle uyarısı yok** — admin pasife çekerse sessiz 403 başlar; UI uyarmıyor.

H. **`ik` kullanıcısı `*` (tümü) atandı** — IK demosu için yeterli ama production'da kapsam daraltılmalı (örn. sadece İstanbul mağazaları).

---

### 2. Çözüm Yaklaşımı

**4 faz, incremental, her faz commit edilebilir.**

**Faz A — Audit + Bug fix + güçlendirilmiş hijyen (~90 dk):**

- **A1. Diff-based audit log** (öneri #1) — `UserManagementService.SyncDataFiltersAsync`:
  - Snapshot before delete, compute `{ added: [], removed: [], changed: [] }`
  - `EventType="user_data_filter_sync"`, `OldValuesJson` + `NewValuesJson` + diff summary
  - Audit reader "ne değişti?" sorusunu doğrudan cevaplar
- **A2. No-change detection** (öneri #2) — eski/yeni listeyi normalize compare; eşitse audit atma. Pattern: `if (oldHash == newHash) return;` (sırasız set comparison). Gereksiz event noise engellenir.
- **A3. Ortak `BuildAdminUserFormAsync` helper** (öneri #3) — `AdminController.Users.cs`:
  - Mevcut `BuildCreateUserFormAsync` (satır 156-194) **rename + generalize**
  - Tek helper signature: `BuildAdminUserFormAsync(User user, HashSet<int> selectedRoleIds, List<UserDataFilter> postedFilters, string? message, string? messageType)`
  - **3 çağrı noktası:** EditUser GET (78-100 inline), EditUser POST error (118-127), CreateUser POST error (mevcut)
  - Drift riski sıfır
- **A4. ModelState.IsValid kontrolü** (öneri #8) — EditUser POST (`AdminController.Users.cs:106-128`):
  - `if (!ModelState.IsValid) return View(await BuildAdminUserFormAsync(...));` ekle
  - Servis-only validation çift validation'a yer veriyor; annotation hatalarını UI'a doğru sızdır
- **A5. raporGrubu tanı sorgusu** (öneri #4) — Faz C riskini erkene taşı:
  ```sql
  SELECT COUNT(DISTINCT UserId) AS UsersWithRaporGrubu FROM UserDataFilters WHERE FilterKey='raporGrubu';
  SELECT COUNT(*) AS ActiveUsers FROM Users WHERE IsActive=1;
  ```
  İki sayı eşit değilse Faz C migration 34 backfill zorunlu — commit message'a notu düş
- **A6. Unit test** (öneri #7) — `Mosaik.Tests/UserManagementServiceTests.cs`:
  - `SyncDataFiltersAsync_DiffEvent_LogsAddedAndRemoved` (mock IAuditLogService, EF InMemory)
  - `SyncDataFiltersAsync_NoChange_SkipsAudit`
  - `BuildAdminUserFormAsync_PopulatesFiltersOnPostError`
- **A7. Smoke test:**
  - `/Admin/EditUser/7` → DER sube'yi `*` yerine `1,4477` yap → Kaydet
  - `dbo.AuditLog WHERE EventType='user_data_filter_sync'` → diff JSON'da `removed:["*"]` + `added:["1","4477"]`
  - Aynı filtreyi tekrar Kaydet → audit'e yeni kayıt **yok** (no-change skip doğrulandı)
  - EditUser POST hatası simüle (eksik Username) → form dönüyor + filter paneli dolu + ModelState hata mesajı görünür
- **A8. Commit:** `feat(admin): UserDataFilter diff audit + ortak form helper + ModelState (plan: 14)`

**Faz B — IK sube + ik user kapsam (~60 dk) — KEŞİF TAMAM, IMPLEMENT BEKLİYOR:**

**2026-05-08 keşif sonucu (memory: `project_zirve_personel_discovery.md`):**
- IK DataSource zaten Mosaik'te kayıtlı: `Server=192.168.40.25\ZRVSQL2008;Database=BKM_GENEL;Integrated Security=true;...`
- Canonical kaynak: `dbo.vw_PersonelDepartman` (kullanıcının kendisi yazdığı 3 firma UNION view: BKM_GENEL + BURSA_KÜLTÜR_MERKEZİ + ASİYE_BİNGÖLBALI)
- 4 seviye hiyerarşi: Firma → Lokasyon (3) → AltLokasyon (8 mağaza/birim) → Departman (24+)
- 272 aktif personel (BKM 232 / Bursa KM 28 / Asiye 12)
- Aktif filtre: `Ict > 2023-12-31 OR Ict IS NULL`
- View, boş SGK kolonlarına (`SeriNo`, `Ckn`, `Meslekilcesi`) text doldurma pattern'iyle hiyerarşi taşıyor

**Aday OptionsQuery (kullanıcı kararı bekliyor):**

A. **Tek seviye (en pratik):**
```sql
SELECT DISTINCT Id = AltLokasyon, Label = AltLokasyon
  FROM dbo.vw_PersonelDepartman
 WHERE AltLokasyon IS NOT NULL AND Ict IS NULL
 ORDER BY Id;
```

B. **Hiyerarşik etiket:**
```sql
SELECT DISTINCT Id = AltLokasyon, Label = Lokasyon + ' / ' + AltLokasyon ...
```

C. **3 ayrı FilterDefinition:** `lokasyon/IK`, `sube/IK` (AltLokasyon), `departman/IK`

**Implement edilmedi — kullanıcı "şimdilik durdur" dedi (2026-05-08).** Plan 18 (HR Sync) yazımıyla birleşik karar alınacak.

**KRİTİK BİLGİ — BKM şube heterojenliği** (`memory/project_bkm_sube_heterogen.md`):
BKM ekosisteminde "şube" her programda **farklı tablo + farklı kod sistemi** ile yaşıyor. PDKS şube kodları, IK şube kodları, satış sistemi mağaza kodları **birbirinden bağımsız**. Tek "global şube tablosu" yok. Bu yüzden:
- `FilterDefinition (FilterKey="sube", DataSourceKey="IK")` ayrı satır
- `FilterDefinition (FilterKey="sube", DataSourceKey="PDKS")` ayrı satır
- `FilterDefinition (FilterKey="sube", DataSourceKey="DER")` ayrı satır
- Her birinin OptionsQuery farklı, kullanıcının atadığı değerler birbirine uymaz

- **B1. BKM_GENEL DB keşfi** (subagent paralel — Faz A çalışırken):
  - Subagent prompt: "BKM_GENEL DB'sinde IK için şube/personel master tablolarını bul. Aday tablo adları + kolon listesi + 5 örnek satır + öneri (hangi tablo IK sube canonical) + kısa rapor"
  - Subagent kullanımı zorunlu (ana çalışma prensibi)
  - Aday sorgular:
    ```sql
    SELECT name, OBJECT_SCHEMA_NAME(object_id)+'.'+name AS qname
      FROM BKM_GENEL.sys.tables
     WHERE name LIKE '%Sube%' OR name LIKE '%Magaza%' OR name LIKE '%Personel%' OR name LIKE '%Birim%';
    ```
  - **Asla varsayma:** Tablo adı/kolon adı tahmini yapma, gerçek schema'yı subagent ile doğrula
- **B2. IK sube FilterDefinition.OptionsQuery** yaz — kanonik `(Id, Label)` projection, BKM_GENEL DB üzerinden
- **B3. Migration 33** — IK sube FilterDefinition güncelle, IsActive=1, OptionsQuery dolu, DataSourceKey="IK"
- **B4. PDKS ve IK arası mapping kontrolü:** ik kullanıcısı PDKS Pano açtığında PDKS sube ID'leri kullanılıyor. IK rapor için IK sube ID'leri. **Aynı kullanıcı için iki ayrı UserDataFilter satırı**. Kullanıcı bunu fark etmeli — admin GUI EditUser sayfasında DataSource sütunu zaten ayrı (`_AdminUserDataFilterPanel.cshtml`), test et.
- **B5. ik kullanıcısı kapsam daraltma** — admin GUI'den iki ayrı atama (PDKS sube + IK sube). Plan dışı manuel adım, smoke test parçası.
- **B6. Smoke test:** ik login → PDKS Pano → sadece atanmış PDKS şubeleri. IK rapor → sadece atanmış IK şubeleri. **Kross-domain karışmamalı** (IK şube ID'si PDKS sorgusuna gitmemeli).

**Faz C — `IUserDataScope` ile reportAccess deny-by-default + IsActive uyarısı (~90 dk):**

**Önkoşul:** Plan 16.5 Faz B tamamlanmış olmalı (`IUserDataScope` interface + `DataScopeRegistry` mevcut). ✅ TAMAM (commit Plan 16.5 Faz B).

**Faz C1 ✅ TAMAM — commit `4e52a05` (2026-05-08):**
- `Mosaik/Services/SpInjectionScope.cs` — UserDataFilter SP-side enforcement (`*` magic + CSV expand + DataSourceKey eşleşme)
- `Mosaik/Services/ReportAccessScope.cs` — raporGrubu EF-side enforcement (deny-by-default, `*` magic, GroupId list match)
- DI kayıt (Program.cs)
- 12 unit test (UserDataScopeTests): SpInjection + ReportAccess + Registry kapsama
- **Mevcut UserDataFilterInjector + ReportsController.Index dokunulmadı** — pragmatik karar (riski düşük tutma).

**Tanı sorgusu sonucu (2026-05-08, sqlcli):**
- `raporGrubu` FilterDefinition **AKTİF DEĞİL** (`IsActive=0` veya kayıt yok)
- 2 active user var, raporGrubu kaydı 0 — backfill **GEREKSİZ**
- Migration 35 ŞU AN yazılmıyor. raporGrubu admin GUI'den aktive edilirken Migration 32 logic'i (`*` backfill) çalıştırılmalı.

**Faz C2/C3 — ertelendi (ayrı oturum, riski yüksek, ROI düşük):**
- `UserDataFilterInjector` registry'ye refactor (raw EF query yerine `IUserDataScope.HasAccessAsync`)
- `ReportsController.Index` raporGrubu inline mantığı `ReportAccessScope.ListAccessibleValuesAsync` çağrısına geçir
- 298+ test full regression
- **DRY** kazancı, vNext modüller için altyapı temizliği. Şu an mevcut işlevsellik doğru çalıştığı için ertelendi.

**Faz C4-C6 — fiilen gereksiz (2026-05-08 değerlendirme):**
- **C4** (raporGrubu backfill migration): tanı sorgusu sonucu `raporGrubu` FilterDefinition **inactive + 0 kullanıcı**. Backfill yapılacak veri yok. Migration yazılmaz; raporGrubu admin GUI'den aktive edildiğinde Migration 32 logic'i (`*` backfill) tetiklenir.
- **C5** (FilterDefinition IsActive impact preview AJAX): raporGrubu inactive olduğundan AJAX uyarısının pratik tetiği yok. Aktive edildiğinde tek satır ek mesaj yeterli, ayrı endpoint overkill. **Erteleme.**
- **C6** (yeni user create'te raporGrubu default `*` zorunlu): raporGrubu inactive olduğundan şu an enforce gereksiz. Aktive edildiğinde Migration 32 backfill yeterli; yeni user için CreateUser servis akışına ek 1 satır gerekir.

**Sonuç (2026-05-08): Faz C fiilen kapalı.** C1 commit'lendi (4e52a05), C2/C3 ayrı oturum bekliyor, C4-C6 raporGrubu hâlâ inactive olduğu için gereksiz. raporGrubu aktive edilirse C4-C6 yeniden değerlendirilir.

- **C1. `IUserDataScope` implementasyonları** (öneri #5 + 16.5 bridge):
  - `SpInjectionScope : IUserDataScope` — mevcut `UserDataFilterInjector` mantığını sarmala
  - `ReportAccessScope : IUserDataScope` — `raporGrubu` deny-by-default
  - DataScopeRegistry'ye DI ile kayıt
- **C2. `UserDataFilterInjector` refactor** — sert kodlu `spInjection`-only kontrolü kaldır; tüm scope'lar registry üzerinden enforcement
- **C3. `ReportsController.cs:209-251`** (reportAccess path) → `IUserDataScope.HasAccessAsync` çağrısına geç (DRY: tek deny-by-default mantığı)
- **C4. Migration 34 — raporGrubu backfill** (Faz A'daki tanı sorgusu eksik göstermişse):
  - `INSERT INTO UserDataFilters (UserId, FilterKey='raporGrubu', FilterValue='*') WHERE NOT EXISTS ...`
  - Mevcut active user'lar `*` (tümü) ile backfill — sessiz 403 önlenir
- **C5. FilterDefinition Edit form** — IsActive toggle değişiminde AJAX preview:
  - "Bu filtre pasife alınırsa N kullanıcı etkilenecek" uyarısı
  - `GET /Admin/Filters/{id}/Impact` endpoint'i
- **C6. Test:** yeni user oluşturulduğunda raporGrubu için en az 1 kayıt zorunlu (varsayılan `*`)

**Faz D — Bulk assign UI: DROPPED** (öneri #6)

Mevcut user sayısı 2-3, bulk UI overkill. Plan 18 (HR sync) sonrası 50+ user olunca kritik olur. Plan 14'ten **çıkarıldı**, TODO.md'ye `[after-plan-18]` etiketiyle taşınacak (Faz A commit'inde TODO update).

---

### 3. Alternatifler

**A — Faz A+B sadece (seçilen):** Production blocker'lar (audit gap + EditUser bug + IK sube hazırlığı) çözülür. ik kullanıcısı kapsamı manuel admin GUI ile daraltılır. Faz C+D ileri sürülür.

**B — Tam paket (A+B+C+D):** Tüm tutarsızlıklar bir oturumda kapatılır. ~5-6 saat, plan 13 (G-09) öncesi gereksiz uzayabilir.

**C — Sadece bug fix (A):** En hızlı, ama IK kullanılamaz halde kalır.

**5 Lens:**
- 🔴 **Contrarian:** Faz C `reportAccess` deny-by-default tutarlılığı kullanıcıyı kıracak — sessiz davranış değişikliği. Backfill migration zorunlu, atlanırsa prod'da kullanıcı 403 yer.
- 🔵 **First Principles:** Filtre sistemi = "kullanıcı sadece yetkili olduğu veriyi görsün". Audit gap (A1) + IK eksiği (B1) gerçek güvenlik sorunu. Faz D bulk UI sadece UX.
- 🟢 **Expansionist:** Bulk assign + role-based filter (Plan 08 RoleDataFilters?) gelecek için altyapı. Şu an scope dışı bırak.
- ⚪ **Outsider:** İkinci kullanıcı (ik) için manuel kapsam atamadan production'a alınamaz. Faz B'nin son adımı (admin GUI manuel) plan dışı ama zorunlu.
- 🟡 **Executor:** İlk iş audit log + EditUser bug. Test edilebilir, geri dönülebilir, immediate value. Faz A tek başına 60dk.

---

### 4. Riskler

- **`raporGrubu` deny-by-default** (Faz C1): mevcut user'larda backfill yapılmadıysa rapor listesi boşalır → migration kritik. Ön kontrol: `SELECT COUNT(*) FROM UserDataFilters WHERE FilterKey='raporGrubu'`.
- **Audit log volümü** (Faz A1): SyncDataFiltersAsync delete-all-then-insert; her atama 1 user_data_filter_sync event üretir. Kabul edilebilir (admin işlemi, sık değil).
- **EditUser BuildEditUserFormAsync** (A2): EditUser ile CreateUser arasında subtle fark var (ekstra alanlar: roller, password reset). DRY iken kafa karıştırmasın.
- **IK OptionsQuery** (B1): doğru tabloyu bulmak için BKM_GENEL DB'de keşif gerekir. Yedek plan: `OptionsQuery=NULL` + `IsActive=0` bırak, ileri taşı.
- **Bulk assign UI** (Faz D): yeni endpoint = yeni güvenlik yüzeyi. Faz D'yi onay aşamasında detaylandır.

---

### 5. Done Criteria

**Faz A:**
- [ ] `SyncDataFiltersAsync` diff audit log (added/removed/changed JSON)
- [ ] No-change detection (eşitse audit skip)
- [ ] `BuildAdminUserFormAsync` ortak helper (3 çağrı noktası)
- [ ] EditUser POST `ModelState.IsValid` kontrolü
- [ ] raporGrubu tanı sorgusu çıktısı commit message'da
- [ ] Unit test: 3 senaryo (diff event, no-change, helper populate)
- [ ] Smoke: diff JSON doğru, no-change skip çalışıyor, ModelState hatası UI'a düşüyor

**Faz B:**
- [ ] BKM_GENEL keşif subagent çıktısı (Faz A paralel)
- [ ] IK sube FilterDefinition.OptionsQuery dolu + IsActive=1
- [ ] Migration 33 yazıldı + uygulandı
- [ ] Smoke: ik kullanıcısı sadece atanmış şubeleri görüyor

**Faz C (Plan 16.5 Faz B önkoşul):**
- [ ] `SpInjectionScope` + `ReportAccessScope` `IUserDataScope` implement
- [ ] `UserDataFilterInjector` registry-based refactor
- [ ] `ReportsController` reportAccess path scope çağrısına geçti
- [ ] Migration 34 raporGrubu backfill (gerekirse — Faz A tanı sorgusuna göre)
- [ ] FilterDefinition Edit form IsActive impact uyarı UI
- [ ] CreateUser default `raporGrubu='*'` kaydı

**Faz D — DROPPED** (Plan 18 sonrasına ertelendi)

---

### 6. Rollback

- Faz A: kod commit'i revert (audit log + helper helper). DB değişikliği yok.
- Faz B: Migration 33 → `UPDATE FilterDefinition SET IsActive=0, OptionsQuery=NULL WHERE FilterKey='sube' AND DataSourceKey='IK'`.
- Faz C: kod commit revert + migration 34 backfill geri alma (`DELETE UserDataFilters WHERE FilterKey='raporGrubu' AND FilterValue='*' AND CreatedAt > X`).
- Faz D: yeni dosyalar siler, route remove.

---

### 7. Adımlar (Faz A — sabah ilk iş, ~90 dk)

**1. raporGrubu tanı sorgusu** (5 dk):
```sql
SELECT COUNT(DISTINCT UserId) AS UsersWithRaporGrubu FROM UserDataFilters WHERE FilterKey='raporGrubu';
SELECT COUNT(*) AS ActiveUsers FROM Users WHERE IsActive=1;
```
Sonucu commit message'a not olarak ekle. Faz C için backfill gerekli mi karar burada.

**2. `Mosaik/Services/UserManagementService.cs:204-222`** SyncDataFiltersAsync diff audit (~25 dk):
- Snapshot before delete (`existing`'i normalize tuple list'e çevir: `(Key, Value, DataSourceKey)`)
- Compute diff: `added = newSet - oldSet`, `removed = oldSet - newSet`, `changed` (DataSourceKey değişen aynı Key+Value)
- No-change short-circuit: `if (added.Count == 0 && removed.Count == 0 && changed.Count == 0) return;`
- Mevcut delete-all-insert davranışı korunur
- Audit log: `EventType="user_data_filter_sync"`, `OldValuesJson=existing`, `NewValuesJson=filters`, diff özeti `Description`'a

**3. `Mosaik/Controllers/AdminController.Users.cs`** ortak helper refactor (~25 dk):
- `BuildCreateUserFormAsync` → `BuildAdminUserFormAsync(User user, HashSet<int> selectedRoleIds, List<UserDataFilter> postedFilters, string? message, string? messageType)`
- `postedFilters` parametresi: caller `Request.Form` parsing'i kendisi yapar veya null geçerse helper post-form okur
- 3 çağrı noktası: EditUser GET (78-100 inline kodu kaldır), EditUser POST error (118-127), CreateUser POST error
- EditUser POST'a `if (!ModelState.IsValid)` ekle: `View(await BuildAdminUserFormAsync(user, input.SelectedRoleIds, postedFilters: null, "Form geçersiz, hataları düzeltin.", "error"));`

**4. Unit test** (`Mosaik.Tests/UserManagementServiceTests.cs`, ~20 dk):
- `SyncDataFiltersAsync_DiffEvent_LogsAddedAndRemoved` — 2 mevcut filter, 1'i kaldırılır + 1 yeni eklenir → audit'e diff JSON
- `SyncDataFiltersAsync_NoChange_SkipsAudit` — aynı liste tekrar → audit çağrısı **yok**
- (Helper testi controller test gerektirir, opsiyonel — smoke yeterli)

**5. Smoke test** (~10 dk):
- `/Admin/EditUser/7` → DER sube `*` → `1,4477` → Kaydet → audit'te `removed:["*"]` + `added:["1","4477"]`
- Aynı sayfa Kaydet (değişiklik yok) → yeni audit kaydı **yok**
- EditUser POST eksik Username → form dönüyor, filter paneli dolu, validation mesajı UI'da

**6. Commit:** `feat(admin): UserDataFilter diff audit + ortak form helper + ModelState (plan: 14)`

Mesaj gövdesinde:
- Adım 1 tanı sorgusu sonucu
- Faz C backfill gerekli mi?
- Faz D Plan 18 sonrasına ertelendi notu (TODO.md güncellemesi commit'in parçası)

Faz B Faz A'yla paralel (subagent BKM_GENEL keşfi). Faz C Plan 16.5 Faz B sonrası.

---

### 8. Bağlam Referansları

**Sistem haritası (code-explorer 7 May 2026):**
- Modeller: `Models/FilterDefinition.cs`, `Models/UserDataFilter.cs`
- Servis: `Services/UserDataFilterInjector.cs:29-116`, `Services/UserDataFilterValidator.cs`, `Services/FilterDefinitionService.cs`, `Services/FilterOptionsService.cs:32-53`
- Controller: `Controllers/AdminController.Filters.cs`, `Controllers/AdminController.Users.cs:103-194`, `Controllers/ReportsController.Run.cs:86-101,240-253`, `Controllers/ReportsController.V2Preview.cs:317-325`
- View: `Views/Admin/_AdminUserDataFilterPanel.cshtml`
- Migration: `Database/13_CreateUserDataFilter.sql`, `Database/20_AddFilterDefinition.sql`, `Database/22_DropSubeCanonical_AddDataSourceFilterUnique.sql`, `Database/31_SeedFilterDefinitions.sql`, `Database/32_ActivateFilterDefinitions.sql`
- Test: `Mosaik.Tests/UserDataFilterInjectorTests.cs`, `Mosaik.Tests/UserDataFilterValidatorTests.cs`, `Mosaik.Tests/FilterOptionsServiceTests.cs`

**İlişkili kararlar:**
- ADR-006 (planlanmış) — AllowedRoles CSV deprecate
- Plan 07 (arşiv) — yetki filter Faz 4 (`raporGrubu` rename, deny-by-default ilk hali)
- Plan 13 (taslak) — G-09 SP read-only login (filter sonrası canlı blocker)

**InjectAsync deny-by-default kuralı (referans):**
`UserDataFilterInjector.cs:57-65` — aktif her FilterDefinition için kullanıcının en az 1 UserDataFilter kaydı zorunlu, yoksa `UserDataFilterDeniedException`. Şu an yalnızca `spInjection` scope kontrol ediyor.

---

### 9. Çalışma Sırası (revize)

1. **Faz A** (~90 dk) — bu plan, sabah ilk iş
2. **Faz B paralel başlat** — Faz A devam ederken BKM_GENEL keşif subagent
3. **Plan 16.5 Faz A+B** (~7h) — domain primitives + workflow + lookup + `IUserDataScope`
4. **Plan 14 Faz C** (~90 dk) — 16.5'tan `IUserDataScope` al, scope-driven enforcement
5. **Plan 14 Faz B kapanışı** — Faz A keşfi + Faz C scope refactor sonrası IK sube migration 33 + ik kapsam
6. (Plan 16.5 Faz C+D + Plan 17+ — ayrı oturumlar)

**Plan 14 toplam:** ~4 saat (Faz A 1.5h + B 0.75h + C 1.5h + D dropped). Önceki tahmin 4-6 saat.
