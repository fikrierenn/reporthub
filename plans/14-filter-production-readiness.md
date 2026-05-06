## Plan 14 — Filter Sistemi Production-Readiness

**Tarih:** 2026-05-07 (gece taslağı, sabah onay)
**Yazan:** Claude (Fikri yönetiminde)
**Durum:** Taslak — onay bekleniyor
**Tier:** 3 (admin UI + servis + audit + DB seed + test, 4-6 dosya)
**Tahmini süre:** ~4-6 saat (faz bazlı incremental)

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

**Faz A — Audit + Bug fix (~60 dk):**
- A1. `UserManagementService.SyncDataFiltersAsync`'e audit log ekle (`user_data_filter_sync` event, before/after diff)
- A2. `AdminController.Users.cs:103-127` EditUser POST error recovery → `BuildEditUserFormAsync` helper oluştur, DataFilters/DataSources/FilterDefinitions doldur
- A3. Smoke test: admin'in UserDataFilter ataması audit log'a düşüyor mu, EditUser form hatasında panel doluyor mu

**Faz B — IK sube + ik user kapsam (~45 dk):**
- B1. IK sistem için `sube` FilterDefinition.OptionsQuery yaz (BKM_GENEL DB'de uygun tablo bul → `bkm.SubeListe` veya benzeri)
- B2. Migration 33 — IK sube FilterDefinition güncelle, IsActive=1
- B3. ik kullanıcısı için PDKS sube + DER sube kapsamlarını gerçek mağaza ID'lerine çek (admin GUI'den yapılır, plan'da değil — manuel adım)
- B4. Smoke test: ik kullanıcısı login → /Reports/Run/10 (PDKS Pano) → sadece atanmış şubeler gelmeli

**Faz C — `raporGrubu` deny-by-default + IsActive uyarısı (~90 dk):**
- C1. `UserDataFilterInjector` içine `reportAccess` scope deny-by-default kontrolü ekle (currently `spInjection`-only). Mevcut migration 32 backfill `raporGrubu` için de `*` ekledi mi kontrol et — yoksa migration 34 backfill
- C2. `ReportsController.cs:209-251` (reportAccess path) → InjectAsync'tan denetim alacak şekilde refactor (DRY: tek deny-by-default mantığı)
- C3. FilterDefinition Edit form → IsActive toggle değişiminde kullanıcı sayısı + etki uyarısı (AJAX preview)
- C4. Test: yeni user oluşturulduğunda raporGrubu için en az 1 kayıt zorunlu (varsayılan `*`)

**Faz D — Bulk assign UI (~120 dk, OPSİYONEL, sabah onaya göre):**
- D1. `/Admin/Filters/{id}/Assign` toplu atama sayfası — FilterDefinition seç → tüm kullanıcılar grid → her kullanıcı için "Hepsi" / multi-select / kayıt YOK seçenekleri
- D2. Tek POST ile tümünü kaydet
- D3. Ön kontrol: kaç kullanıcı etkilenecek + audit log toplu giriş

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
- [ ] `UserManagementService.SyncDataFiltersAsync` audit log çağırıyor
- [ ] `AdminController.Users.cs` EditUser POST error → DataFilters/DataSources/FilterDefinitions dolu ViewModel
- [ ] Smoke: admin/EditUser/7 → filter değiştir → audit log'da `user_data_filter_sync` event var

**Faz B:**
- [ ] IK sube FilterDefinition.OptionsQuery dolu + IsActive=1
- [ ] Migration 33 yazıldı + uygulandı
- [ ] Smoke: ik kullanıcısı PDKS Pano açıyor, sadece atanmış şubeler dönüyor

**Faz C (opsiyonel):**
- [ ] `UserDataFilterInjector` reportAccess scope deny-by-default kontrolü
- [ ] Migration 34 raporGrubu backfill (zorunluysa)
- [ ] FilterDefinition Edit form IsActive toggle uyarı UI

**Faz D (opsiyonel):**
- [ ] `/Admin/Filters/{id}/Assign` toplu atama sayfası
- [ ] Test coverage

---

### 6. Rollback

- Faz A: kod commit'i revert (audit log + helper helper). DB değişikliği yok.
- Faz B: Migration 33 → `UPDATE FilterDefinition SET IsActive=0, OptionsQuery=NULL WHERE FilterKey='sube' AND DataSourceKey='IK'`.
- Faz C: kod commit revert + migration 34 backfill geri alma (`DELETE UserDataFilters WHERE FilterKey='raporGrubu' AND FilterValue='*' AND CreatedAt > X`).
- Faz D: yeni dosyalar siler, route remove.

---

### 7. Adımlar (Faz A — sabah ilk iş)

**1. AuditLogEntry desteği kontrol** — `EventType="user_data_filter_sync"` standart pattern'e uyuyor mu? `OldValuesJson` + `NewValuesJson` filter listesi serialize edilir.

**2. `Mosaik/Services/UserManagementService.cs:204-222`** SyncDataFiltersAsync'i güncelle:
   - Mevcut filter listesini oku (delete öncesi snapshot)
   - Yeni listeyi yaz (mevcut delete-all-insert davranışı)
   - Audit log: `{old: [...], new: [...]}` JSON

**3. `Mosaik/Controllers/AdminController.Users.cs:156-194`** `BuildCreateUserFormAsync` paraleli olarak `BuildEditUserFormAsync` (ya da BuildAdminUserFormAsync ortak helper) — DataFilters + DataSources + FilterDefinitions doldur.

**4. EditUser POST error recovery** (`AdminController.Users.cs:120-127`) → yeni helper'ı çağır.

**5. Smoke test:**
   - `/Admin/EditUser/7` → DER sube'yi `*` yerine `1,4477` yap → Kaydet
   - `dbo.AuditLog WHERE EventType='user_data_filter_sync'` → kayıt var mı, OldValuesJson + NewValuesJson dolu mu
   - EditUser POST hatası simüle et (örn. eksik Username) → form dönüyor + filter paneli dolu

**6. Commit:** `feat(admin): UserDataFilter sync audit log + EditUser error recovery (plan: 14)`

Faz B+C+D ayrı commit'ler halinde, plan'a göre.

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

### 9. Sabah Başlangıç Noktası

1. **TODO.md güncelle:** Plan 14 satırı ekle (`BIRLESIK ONCELIK SIRASI` → Faz B yerine Faz A).
2. **Plan onayı:** kullanıcıya 4 faz incremental yaklaşımı + Faz A'dan başlama kararını onaylat.
3. **Faz A 1-6. adım** sırayla.
4. Faz B IK sube OptionsQuery için `BKM_GENEL` DB keşfi (`mcp__sqlserver__sql_query`).
