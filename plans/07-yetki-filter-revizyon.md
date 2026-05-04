# Plan 07 — Yetki + Veri Filter Revizyonu (FilterDefinition + User Dinamik Field)

**Tarih:** 2026-05-04
**Yazan:** Claude (Fikri yönetiminde)
**Durum:** Onay bekliyor
**Branch:** `feature/m-11-dashboard-builder-redesign` (devam) veya yeni `feature/yetki-filter`
**İlişkili TODO:** TODO.md → "YETKILENDIRME REVIZYONU" (28 Nisan 2026), "P0 CreateUser veri filtresi" (audit ile zaten kapali)
**Tier:** 3 (3+ klasör, schema/security, kullanıcı-görünür, plan-first zorunlu)

---

## 1. Problem

Mevcut `UserDataFilters` mekaniği çalışıyor ama **3 ciddi UX/güvenlik sorunu** var:

1. **FilterKey listesi cshtml hardcoded** (`_AdminUserDataFilterPanel.cshtml:59-63`): `sube/bolum/bolge/kategori/maliyet_merkezi`. Yeni filter eklemek için kod değişikliği gerek. `FilterOptionsService.cs:47-52` switch-case da hardcoded — sadece `sube` ve `bolum` için SQL var; UI'da `bolge/kategori/maliyet_merkezi` seçilebilir ama "Liste boş" döner.
2. **Default semantik güvensiz** (`UserDataFilterInjector` — 0 satır = SP parametresi NULL = tümü): Yeni user açıldı, admin filter atadığını unuttu → kullanıcı **tüm şubeleri ve tüm kategorileri görür**. Bu kullanıcı yetkilendirme silentleri için tehlikeli default.
3. **User formunda soyut "filter row" UX** — `[+ Filtre Ekle]` butonu, generic FilterKey dropdown + FilterValue dropdown. Admin "X şubeli + Y kategorili" tanımlamak için 2 satır eklemek zorunda. Anlamsız soyutlama. Hedef: User formunda **dinamik field section** (her aktif FilterDefinition için kalıcı alan).

**Ölçek bağlamı (kullanıcı 4 Mayıs 2026):**
- GM ~50 kişi, şube ortalama 40-50 kişi × N şube, bölüm başına ~5 kişi → toplam **300-500+ kullanıcı**
- Manuel atama imkansız → otomasyon (default "Hepsi" + admin daraltır) zorunlu

---

## 2. Scope

### Kapsam dahili

- **DB:** Yeni tablo `FilterDefinition` (master), `UserDataFilters` schema değişmez
- **Backend:** `FilterDefinition` EF model + `FilterDefinitionService` + `FilterOptionsService` refactor + `UserDataFilterInjector` "0 kayıt = deny" mantığı
- **UI:** `_AdminUserDataFilterPanel.cshtml` dinamik field rendering (FilterDefinition loop), her field "Hepsi" toggle + multi-select. Admin Panel'de yeni "Filtreler" alt-sayfası CRUD
- **Migration:** Migration 20 — FilterDefinition tablosu + 2 seed (sube, kategori) + backfill mevcut user'lara `*`
- **Audit:** `data_filter_denied` event 0 kayıt durumunda

### Kapsam dışı (sonraki plan)

- **Rol-bazlı filter atama** (`RoleDataFilters` junction) — kullanıcı sayısı 300+ olduğunda atomasyon için zorunlu ama bu plan'da yok. Plan 08'e bırakıldı.
- **Granular role + permission sistemi** — Plan 06 vNext (şirket içi portal) kapsamında.
- (Faz 7 plan-dahili: Reports/Index liste filtresi rapor kategorisi için aktif — kullanıcı sadece atanmış kategorideki raporları görür)

### Etkilenen dosyalar (tahmin)

**Yeni:**
- `ReportPanel/Database/20_AddFilterDefinition.sql`
- `ReportPanel/Models/FilterDefinition.cs`
- `ReportPanel/Services/FilterDefinitionService.cs`
- `ReportPanel/ViewModels/AdminFilterDefinitionFormViewModel.cs`
- `ReportPanel/Views/Admin/EditFilterDefinition.cshtml` (CRUD)
- `ReportPanel/Views/Admin/_AdminTabFilters.cshtml` (Filtreler liste)

**Değiştirilen:**
- `ReportPanel/Models/ReportPanelContext.cs` (DbSet ekle)
- `ReportPanel/Services/FilterOptionsService.cs` (switch-case kalkar, DB-driven)
- `ReportPanel/Services/UserDataFilterInjector.cs` (0 kayıt = deny mantığı)
- `ReportPanel/Views/Admin/_AdminUserDataFilterPanel.cshtml` (dinamik field, "Hepsi" toggle)
- `ReportPanel/Views/Admin/Index.cshtml` (sub-nav: yeni "Filtreler" tab)
- `ReportPanel/Controllers/AdminController.cs` veya yeni partial `AdminController.Filters.cs`
- `ReportPanel/Controllers/ReportsController.Run.cs` (rapor erişim 403 mantığı)

---

## 3. Alternatifler

### A: Mevcut soyut "filter row" UX kal, sadece FilterKey listesi DB-driven yap (en küçük scope)

**Açıklama:** `FilterDefinition` ekle, `_AdminUserDataFilterPanel`'deki hardcoded `<option>` listesi DB'den gelir, geri kalan UI aynı kalır.

**Reddetme sebebi:** Kullanıcının asıl şikayeti UX — "filter satırı" soyutlaması anlamsız. Bu çözüm yarısı.

### B: User entity'ye concrete `BranchCode` + `AllowedCategoryIds` field'ları ekle (concrete schema)

**Açıklama:** User modeline doğrudan kolonlar ekle, multi-tenant logic User entity üzerinden.

**Reddetme sebebi:**
- "Sonradan farklı şeyler eklenebilir" gerekliği — yeni filter = yeni User kolonu = migration + form rebuild.
- N:M ilişkiler için ya CSV (kötü pattern, M-03'te yeni temizlendi) ya junction tablosu — junction zaten UserDataFilters yapıyor.
- Genişletilemez.

### C (SEÇİLEN): Soyut FilterDefinition + UserDataFilters mevcut + UI dinamik field (her aktif FilterDefinition için kalıcı alan)

**Açıklama:**
- Master `FilterDefinition` tablosu (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder).
- UserDataFilters mevcut — schema değişmez. `FilterValue='*'` magic string = "Hepsi" (explicit kayıt).
- User formunda her aktif FilterDefinition için **kalıcı field** (label + multi-select + "Hepsi" toggle).
- 0 kayıt = deny (yeni user atlanırsa rapor 403 döner).
- Yeni filter = sadece DB'de FilterDefinition satırı + form otomatik yeni field render.

**Avantajlar:**
- Schema değişmiyor (UserDataFilters mevcut), geri uyum garantili.
- Yeni filter eklemek için kod değişikliği yok (sadece admin master CRUD).
- UX: her filter kalıcı field — admin "Şube + Kategori + ..." görür, anlam net.
- Güvenlik: deny by default — atlanan user'a varsayılan "tümü" verilmez.

### D: Rol-bazlı filter template + auto-clone yeni user'a (300+ user otomasyonu)

**Açıklama:** `RoleDataFilters` junction tablosu, kullanıcı role bağlandığında filter'lar otomatik UserDataFilters'a kopyalanır.

**Reddetme sebebi (şimdilik):** Plan 08 olarak bırakıldı. C tamamlanmadan rol-bazlı atama tasarımı eksik kalır (rol-vs-user override mantığı, hiyerarşi). Önce C → 50 user manuel test → sonra D.

---

## 4. Riskler

- **Mevcut 7 kullanıcı erişimi kırılır** eğer migration backfill atlanırsa (deny by default + 0 kayıt = 403). **Mitigation:** Migration script `INSERT * kayıtları` zorunlu adım.
- **OptionsQuery güvenliği** — admin yazdığı serbest SQL (admin-only path). SQL injection riski **yok** (admin yazıyor) ama yanlış SQL DB yorabilir veya yanlış kolon adı boş döner. **Mitigation:**
  - Admin form'unda "Test" butonu — kayıt etmeden önce SQL'i deneyip 1-N satır gösterir
  - OptionsQuery sadece SELECT izinli — `EXEC`, `INSERT`, `DELETE` tespiti regex ile reddedilir
  - Read-only DB connection kullan (G-09 SP read-only login ile birlikte)
- **300+ kullanıcı için manuel atama** — hala büyük yük. Plan 08 (rol-bazlı) kritik. C tamamlandığında **somut UX problemini test edip** D'nin scope'u gerçekleşir.
- **`Scope='reportAccess'` uygulaması karmaşık** — Reports/Index listesi sorgusu kategori-filtreli olmalı, mevcut LINQ chain (ReportsController.cs:184) revize. **Mitigation:** Faz 7 opsiyonel, ilk implementation'da `Scope='spInjection'` (sube davranışı) yeterli.
- **`*` magic string** — Future expansion'da `*` literal değer gelirse çakışır. **Mitigation:** Yorumda belgelenir; alternatif `IsAll bit` kolonu — schema değişikliği gerek, magic string seçildi (basit).

---

## 5. Done Criteria

### Faz 1 — DB schema + seed + backfill
- [ ] `Database/20_AddFilterDefinition.sql` (idempotent, FilterDefinition tablosu + PK + FK→DataSources)
- [ ] Seed: `sube` (spInjection, OptionsQuery vrd.SubeListe) + `kategori` (reportAccess, ReportCategories SELECT)
- [ ] Backfill: tüm aktif kullanıcılara her aktif FilterDefinition için `FilterValue='*'` kayıt
- [ ] DB'de çalıştırıldı, idempotency doğrulandı (yeniden çalıştırma patlamaz)

### Faz 2 — EF model + service
- [ ] `Models/FilterDefinition.cs` ([Key], [BindNever] kritik alanlarda)
- [ ] `ReportPanelContext.FilterDefinitions` DbSet
- [ ] `Services/FilterDefinitionService.cs` (CRUD, OptionsQuery doğrulama)
- [ ] Build + 217 test yeşil

### Faz 3 — FilterOptionsService refactor
- [ ] Switch-case kalktı, `OptionsQuery` DB'den okunuyor
- [ ] Geri uyum: mevcut sube/bolum kayıtları yeni FilterDefinition.OptionsQuery ile aynı sonuç
- [ ] Test: `FilterOptionsServiceTests.cs` yeni — DB-driven OptionsQuery exec

### Faz 4 — UserDataFilterInjector "0 kayıt = deny"
- [ ] Kullanıcı için aktif FilterKey'lerden herhangi birine kayıtsız ise → exception veya special return
- [ ] ReportsController.Run + RunJsonV2 + Export catch eder, 403 döner + audit `data_filter_denied`
- [ ] Test: 0 kayıtlı user için Run çağrısı 403

### Faz 5 — UI dinamik field
- [ ] `_AdminUserDataFilterPanel.cshtml` rewrite — FilterDefinition loop'u, her aktif key için label + multi-select + "Hepsi" toggle
- [ ] "Hepsi" işaretliyse multi-select disabled, kaydederken `FilterValue='*'` tek satır
- [ ] Yeni user formunda default tüm filter'lar "Hepsi" işaretli (atlama tolere eder)
- [ ] Browser smoke: yeni user oluştur → DB'de doğru kayıtlar (her filter için `*`)

### Faz 6 — Admin "Filtreler" alt-sayfası
- [ ] `Views/Admin/_AdminTabFilters.cshtml` (liste)
- [ ] `Views/Admin/EditFilterDefinition.cshtml` (CRUD form)
- [ ] OptionsQuery test butonu (kayıt etmeden 1-N satır göster)
- [ ] Admin/Index sub-nav'a "Filtreler" tab
- [ ] AdminController.Filters partial veya FilterDefinitionService endpoint'leri

### Faz 7 — Reports/Index kategori filtresi (Scope='reportAccess')
- [ ] ReportsController.Index sorgusu: kullanıcının kategori filtresini join et
- [ ] `*` veya 0 kayıt mantığı: 0 kayıt → reddet, `*` → tümünü gör

### Faz 8 — Test + smoke
- [ ] Unit: FilterDefinitionServiceTests, FilterOptionsServiceTests, UserDataFilterInjectorTests (deny path)
- [ ] Smoke: Admin Filtre CRUD + User formunda dinamik field + Run 403/200 senaryoları
- [ ] 217 + ~10 yeni test yeşil
- [ ] Plan dosyasını arşive taşı: `git mv plans/07-*.md plans/archive/`

---

## 6. Rollback Planı

Plan 07 implementasyonu rollback edilirse:

1. **DB:** Migration 20 reverse — `DROP TABLE FilterDefinition;` (UserDataFilters'taki `*` kayıtları silinir veya `IsActive=0` yapılır)
2. **Code:** Faz 5'in tek-revert: `_AdminUserDataFilterPanel.cshtml` mevcut hardcoded `<option>` listesine geri dön
3. **Service:** `FilterOptionsService` switch-case'e geri dön
4. **Veri kaybı:** 0 (UserDataFilters mevcut kayıtlar korunur)

Eğer sadece Faz 4 (deny by default) sorun çıkarırsa: `UserDataFilterInjector`'da "0 kayıt = NULL" davranışına geri dön (mevcut + tehlikeli ama hızlı geri açma).

---

## 7. Adımlar

### Faz 1 — DB (~2h)
1. [ ] `Database/20_AddFilterDefinition.sql` yaz: idempotent CREATE + PK + FK + 2 seed (sube spInjection, kategori reportAccess)
2. [ ] Backfill INSERT script: `INSERT UserDataFilters (UserId, FilterKey, FilterValue, ...) SELECT u.UserId, fd.FilterKey, '*', ... CROSS JOIN ... NOT EXISTS`
3. [ ] DB'de çalıştır + idempotency test (2x çalıştır, hata yok)
4. [ ] Commit: `feat(filter): Migration 20 FilterDefinition + backfill (plan: 07)`

### Faz 2 — EF + service (~2h)
5. [ ] `Models/FilterDefinition.cs` + `ReportPanelContext.FilterDefinitions`
6. [ ] `Services/FilterDefinitionService.cs` CRUD
7. [ ] Build + test
8. [ ] Commit: `feat(filter): EF model + FilterDefinitionService (plan: 07)`

### Faz 3 — FilterOptionsService (~1h)
9. [ ] Switch-case kaldır, FilterDefinition.OptionsQuery'yi exec et
10. [ ] Test: `FilterOptionsServiceTests.cs` (sube + kategori OptionsQuery)
11. [ ] Commit: `refactor(filter): FilterOptionsService DB-driven (plan: 07)`

### Faz 4 — Injector deny (~1h)
12. [ ] `UserDataFilterInjector.InjectAsync` — 0 kayıt = throw veya bool dönüş
13. [ ] ReportsController.Run/Export/RunJsonV2 catch → 403 + audit
14. [ ] Test: deny path
15. [ ] Commit: `feat(filter): deny-by-default (plan: 07)`

### Faz 5 — UI dinamik field (~3h)
16. [ ] `_AdminUserDataFilterPanel.cshtml` rewrite — FilterDefinition loop, "Hepsi" toggle
17. [ ] CreateUser/EditUser formunda default "Hepsi" işaretli
18. [ ] Browser smoke: yeni user → DB kayıt
19. [ ] Commit: `feat(filter): user form dinamik field (plan: 07)`

### Faz 6 — Admin CRUD (~3h)
20. [ ] `_AdminTabFilters.cshtml` + `EditFilterDefinition.cshtml`
21. [ ] AdminController.Filters partial
22. [ ] OptionsQuery "Test" butonu
23. [ ] Commit: `feat(filter): admin Filtreler CRUD (plan: 07)`

### Faz 7 — Reports kategori filtresi (~1.5h)
24. [ ] ReportsController.Index → kategori filter join
25. [ ] Smoke: kullanıcı kategori kısıtı doğru çalışıyor
26. [ ] Commit: `feat(filter): reportAccess scope reports/index (plan: 07)`

### Faz 8 — Test + arşivle (~1.5h)
27. [ ] Yeni unit testler (10+ test)
28. [ ] Tam smoke: 5 senaryo (yeni user, mevcut user, deny, hepsi, spesifik)
29. [ ] Commit: `test(filter): unit + smoke (plan: 07)`
30. [ ] `git mv plans/07-*.md plans/archive/` + TODO.md kapanış

**Tahmini boyut toplam:** ~15 saat, 7-8 commit, ~25 dosya etkilenir.

---

## 8. İlişkili

- TODO.md → "YETKILENDIRME REVIZYONU" (28 Nisan 2026)
- `.claude/rules/security-principles.md` → multi-tenant data filter (madde 8)
- `.claude/rules/architecture.md` → mevcut UserDataFilter mekaniği
- ADR-003 (rol modeli) — UserRole junction birincil
- Plan 06 vNext (şirket içi portal) — granular role + permission, bu plan'ın üstüne oturur
- Plan 08 (potansiyel) — RoleDataFilters + auto-clone, bu plan tamamlandıktan sonra

---

## 9. Onay

> Kullanıcı onay verene kadar implement edilmez.

- [x] Plan kullanıcıya gösterildi — 2026-05-04 oturumu
- [x] Geri bildirim alındı + plan güncellendi
- [x] Onay alındı: 2026-05-04 (Fikri)
- [ ] Implement edildi: <commit serisi>
- [ ] Tamamlandı: <tarih>

**Onaylanmış kararlar (4 Mayıs 2026):**
1. **0 kayıt → 403 reddet** ✅ (a) — açık feedback, sessiz boş döndürme yok. Audit `data_filter_denied`.
2. **Migration backfill ✅** — mevcut 7 user için tek seferde tüm aktif FilterDefinition'lara `*` kaydı ekle. Geçişte kimse kapı dışı kalmaz, herşeye yetki ile başlar, admin sonradan daraltır.
3. **Yeni user formunda default boş ✅** (b) — admin mutlaka karar vermeli. Atlanırsa user 0 kayıt = 403 (madde 1 ile tutarlı).
4. **Faz 7 dahili ✅** — Reports/Index kategori filtresi (Scope=reportAccess) plan kapsamında.

**Naming netleştirme:**
- `sube` (Scope=spInjection, OptionsQuery=vrd.SubeListe) — mevcut, korunur
- `raporKategori` (Scope=reportAccess, OptionsQuery=ReportCategories SELECT) — YENİ, /Reports listesi filtresi
- **`urunKategori`** (Scope=spInjection, satış raporlarındaki kitap/kırtasiye/mağaza/satınalma) — Plan 07 seed dahil DEĞİL; admin sonradan FilterDefinition CRUD ile ekler (ürün master tablosu/SQL netleşince)

---

**Önemli not:** Plan 06 vNext ile çakışma — vNext "granular role + permission" sistemi getirir, Plan 07 onun **alt katmanı** olarak oturur (FilterDefinition + UserDataFilters infrastructure değişmez). Plan 06 başlamadan Plan 07'yi tamamlamak doğru sıra.
