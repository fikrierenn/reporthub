## Plan 13 — G-09: SP Execution Read-Only SQL Login

**Tarih:** 2026-05-07  
**Yazan:** Claude (Fikri yönetiminde)  
**Durum:** Taslak — onay bekleniyor  
**Tier:** 3 (DB migration + model değişikliği + birden fazla servis/controller + admin UI + yeni SQL script)  
**Tahmini süre:** ~2–3 saat

---

### 1. Problem

Mevcut `DataSource.ConnString` muhtemelen yüksek yetkili bir SQL login kullanıyor (SA/dbo). Admin bir SP bağladığında o SP içinde `UPDATE/INSERT/DELETE` bile olsa uygulama engelleyemiyor. Kullanıcı "canlıya almadan mutlaka yapalım" dedi (2026-05-01).

---

### 2. Çözüm Yaklaşımı

`DataSource` modeline `ReadOnlyConnString` (nullable) eklenir. Rapor execution path'ı bu alanı kullanır; boşsa `ConnString` fallback yapar. Admin keşif operasyonları (SpExplorer, OptionsQuery test) her zaman `ConnString` kullanmaya devam eder.

**Hangi path read-only kullanır:**
- `ReportsController.Run` → `_spExecutor.ExecuteMultipleAsync`
- `FilterOptionsService.GetOptionsAsync` (kullanıcıya dönen filtre seçenekleri)

**Hangi path ana ConnString'de kalır:**
- `SpExplorerService` (admin SP browse, metadata fetch)
- `FilterOptionsService` admin test endpoint
- `DataSourceManagementService.TestConnectionAsync` (admin bağlantı testi)

---

### 3. Alternatifler

**A — ReadOnlyConnString alanı ekle (seçilen):** Ayırma net, admin konfigürasyona bağlı, boş bırakılabilir (geçiş dönemi için fallback). Kod minimal.

**B — ConnString'i read-only login'e zorla, ayrı admin bağlantısı için `AdminConnString` ekle:** Daha güvenli (uygulama hiçbir zaman admin bağlantısıyla SP çalıştırmaz) ama 2 alan adı değişir, migration+UI daha büyük, mevcut DataSource'ların ConnString'ini admin versiyona rename etmek kafa karıştırır.

**C — Application layer'da SP parse et, DML varsa reddet:** Güvenilmez (dynamic SQL, EXEC içinde EXEC). Efor yüksek, güvenlik garanti değil.

**5 Lens:**
- 🔴 Contrarian: ReadOnlyConnString boş bırakılırsa hiçbir güvenlik sağlanmıyor. Fallback mantığı yanlış his verebilir.
- 🔵 First Principles: Gerçek risk = kullanıcı SP içinde veri değiştirilebilir. En basit bariyer = DB login'e yetki verme.
- 🟢 Expansionist: Tüm DataSource'lar için standart "rapor execution login" politikası dokümante edilebilir.
- ⚪ Outsider: İki farklı ConnString alanı kafa karıştırabilir — UI'da net etiket şart.
- 🟡 Executor: İlk iş SQL script + migration. Sonra model + migration. Sonra kod. UI en son.

---

### 4. Riskler

- Mevcut DataSource'ların `ReadOnlyConnString` boş kalması → fallback `ConnString` çalışır, eski davranış korunur — GEÇİŞ DÖNEM KABUL EDİLEBİLİR
- Admin "ReadOnlyConnString nedir" anlamayabilir → UI açıklaması + help text şart
- DB login oluşturma DB admin yetkisi gerektirir — SQL script + dokümantasyon yeterli, kod bu kısma dokunmaz

---

### 5. Done Criteria

- [ ] Migration 28: `DataSources.ReadOnlyConnString NVARCHAR(500) NULL`
- [ ] `DataSource.cs` model güncellendi
- [ ] `ReportsController.Run.cs` → `ReadOnlyConnString ?? ConnString`
- [ ] `FilterOptionsService.cs` kullanıcı path → `ReadOnlyConnString ?? ConnString`
- [ ] Admin DataSource Create/Edit formlarına `ReadOnlyConnString` alanı eklendi
- [ ] `Database/28_AddReadOnlyConnString.sql` migration dosyası
- [ ] `Database/scripts/create-readonly-login.sql` örnek SQL (çalıştırılmaz, template)
- [ ] Smoke test: ReportsController.Run read-only login ile SP çalışıyor, admin SpExplorer ana login ile çalışıyor

---

### 6. Rollback

Migration geri alınabilir: `ALTER TABLE DataSources DROP COLUMN ReadOnlyConnString`. Kod değişikliği: `ReadOnlyConnString ?? ConnString` → `ConnString` (tek satır).

---

### 7. Adımlar

**Faz 1 — DB + Model (~30 dk)**
1. `Database/28_AddReadOnlyConnString.sql` yaz
2. `Mosaik/Models/DataSource.cs` → `ReadOnlyConnString` nullable property ekle
3. `MosaikContext` varsa fluent config (yoksa convention yeterli)

**Faz 2 — Servis katmanı (~30 dk)**
4. `ReportsController.Run.cs:114,256` → `context.SelectedReport.DataSource.ReadOnlyConnString ?? context.SelectedReport.DataSource.ConnString`
5. `FilterOptionsService.cs:108` kullanıcı path → aynı pattern
6. `AdminController.Filters.cs:141` (admin test endpoint) → `ConnString` kalır

**Faz 3 — Admin UI (~30 dk)**
7. `Views/Admin/CreateDataSource.cshtml` + `EditDataSource.cshtml` → ReadOnlyConnString alanı ekle
8. `DataSourceManagementService.cs` → Create/Update ReadOnlyConnString parametre ekle
9. `AdminController.DataSources.cs` → form binding güncelle

**Faz 4 — SQL Template + Smoke (~30 dk)**
10. `Database/scripts/create-readonly-login.sql` template (yorum açıklamalı)
11. Smoke: `/Reports/Run/{id}` read-only login ile çalışıyor, `/Admin/SpList` ana login ile çalışıyor
12. Commit + plan arşivle
