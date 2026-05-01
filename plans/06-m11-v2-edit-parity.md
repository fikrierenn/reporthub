# Plan 06.B — V2 Drawer: Edit Parity (Roles, Categories, Description, IsActive)

**Tarih:** 2026-05-01
**Yazan:** Fikri / Claude
**Durum:** `Tamamlandı`

---

## 1. Problem

V2 builder drawer'da rapor metadata alanlarının bir kısmı düzenlenemiyor:

- **Description:** form'a hardcoded hidden input olarak gidiyor, UI'da görünmüyor.
- **IsActive:** read-only `x-text` ile gösteriliyor, toggle yok.
- **SelectedRoles:** Razor `@foreach` ile hardcoded hidden input'lar, UI'da checkbox yok.
- **SelectedCategories:** aynı pattern, hardcoded hidden input'lar.

Kullanıcı bu alanları düzenlemek için "Klasik formda düzenle" linkiyle V1'e geçmek zorunda.
Bu UX kırılması dashboard builder bağlamından çıkma gerektiriyor — V2 kullanıcı akışını bozuyor.

## 2. Scope

### Kapsam dahili

**06.B-1:** Description textarea alanı — drawer "Rapor Ayarları" bölümüne editable textarea.
**06.B-2:** IsActive toggle — read-only gösterimden checkbox/switch'e çevir.
**06.B-3:** Roles checklist — `AvailableRoles` + `SelectedRoleIds` checkbox listesi.
**06.B-4:** Categories checklist — `AvailableCategories` + `SelectedCategoryIds` checkbox listesi.
**06.B-5:** "Klasik formda düzenle" linkini kaldır (artık gereksiz).
**06.B-6:** Form hidden input'ları reactive hale getir (hardcoded Razor → Alpine `x-model`).

### Kapsam dışı

- V2-specific POST endpoint (mevcut `/Admin/EditReport/{id}` yeterli, form field'ları aynı name'lerle gidiyor).
- Roles/Categories CRUD (yeni rol/kategori ekleme V2'den yapılmaz, Admin panelinden yapılır).
- ParamSchema visual editor (Plan 06.A'da zaten editable textarea yapıldı).
- Drag-drop role/category sıralama.

### Etkilenen dosyalar

| Dosya | Değişiklik |
|---|---|
| `Views/Admin/EditReportV2.cshtml` | `window.__reportMeta`'ya roles + categories JSON ekle, hardcoded hidden input'ları kaldır, drawer'a 4 yeni alan ekle |
| `wwwroot/assets/js/builder-v2/builder-settings.js` | `description`, `isActive`, `selectedRoles`, `selectedCategories` reactive state + init |

**Tahmini boyut:** 2 dosya / ~80 satır net ekleme.

## 3. Alternatifler

### A: Ayrı V2-specific POST endpoint
**Açıklama:** `/Admin/EditReportV2/{id}` yeni endpoint, sadece V2 field'larını kabul eder.
**Reddetme sebebi:** Mevcut endpoint zaten tüm field'ları kabul ediyor (`ReportFormInput` record). İkinci endpoint gereksiz duplikasyon + ayrı validation/audit mantığı.

### B: Modal popup ile role/category seçimi
**Açıklama:** Drawer'da yer kaplamaması için roles/categories modal'da açılsın.
**Reddetme sebebi:** Overengineering. Drawer zaten scroll edebilir, 5-10 rol/kategori için modal gereksiz UX katmanı.

### C: Drawer inline — seçilen yaklaşım
**Açıklama:** Tüm alanlar drawer'ın "Rapor Ayarları" bölümüne inline eklenir. Alpine reactive state + hidden form input'lar.
**Sebep:** En basit, mevcut pattern'le tutarlı (Title/DataSource/ProcName zaten bu şekilde çalışıyor). Tek form, tek POST endpoint.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Roles/Categories checkbox değişmediğinde boş gitmesi | yüksek | orta | Hidden input'ları Alpine x-effect ile senkronize tut, form submit öncesi kontrol |
| IsActive false gönderilememesi (checkbox unchecked → form'da yok) | orta | yüksek | Alpine ile hidden input value="true/false" bind et, checkbox yerine toggle pattern |
| builder-settings.js 250 satır soft limit aşımı | düşük | düşük | Mevcut 97 satır + ~50 ekleme = ~147, limit altında |

## 5. Done Criteria

- [ ] Description textarea V2 drawer'da görünür ve form POST'a dahil
- [ ] IsActive toggle çalışır, true/false doğru gider
- [ ] Roles checklist tüm available role'ları gösterir, seçili olanlar checked
- [ ] Categories checklist aynı şekilde çalışır
- [ ] "Klasik formda düzenle" linki kaldırılmış
- [ ] Form POST sonrası tüm alanlar doğru kaydedilir (smoke test)
- [ ] Mevcut V2 builder fonksiyonları kırılmamış (widget ekleme, taşıma, kaydetme)

## 6. Rollback Planı

- `git revert <commit>` — tek commit, 2 dosya. Hardcoded hidden input'lar geri gelir, "Klasik formda düzenle" linki geri gelir. Veri kaybı yok.

## 7. Adımlar

1. [ ] **06.B-1** `EditReportV2.cshtml`: `window.__reportMeta`'ya `availableRoles`, `selectedRoleIds`, `availableCategories`, `selectedCategoryIds` ekle
2. [ ] **06.B-2** `EditReportV2.cshtml`: Hardcoded Description/IsActive/Roles/Categories hidden input'ları kaldır
3. [ ] **06.B-3** `builder-settings.js`: `description`, `isActive`, `selectedRoleIds`, `selectedCategoryIds` reactive state + `initSettings()` init
4. [ ] **06.B-4** `EditReportV2.cshtml`: Drawer "Rapor Ayarları"na Description textarea + IsActive toggle + Roles checklist + Categories checklist ekle
5. [ ] **06.B-5** `EditReportV2.cshtml`: Alpine reactive hidden input'lar ekle (form dışı div'de, `form="reportEditFormV2"`)
6. [ ] **06.B-6** `EditReportV2.cshtml`: "Klasik formda düzenle" linkini kaldır
7. [ ] **06.B-7** Smoke test: V2'den kaydet → DB'de doğru değerler, V1'e geçip kontrol

## 8. İlişkili

- Plan 06.A: `52e0077` — ParamSchema editor (read-only → editable textarea) — tamamlandı
- Plan 04: `plans/04-m11-v2-builder-ux-redesign.md` — V2 builder UX tasarımı
- `AdminController.EditReport` POST handler (line ~569)
- `ReportManagementService.UpdateAsync` — form input → DB update
- V1 `EditReport.cshtml` — roles/categories checkbox pattern referansı

## 9. Onay

> Kullanıcı onay verene kadar implement edilmez.

- [ ] Plan kullanıcıya gösterildi
- [ ] Geri bildirim alındı
- [ ] Onay alındı: _tarih_
