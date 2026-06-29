# Plan 21 — Lookup Table (Enum → DB)

**Tarih:** 2026-05-09
**Yazan:** Claude
**Durum:** ⚠️ **KISMI — SPEC GÜNCELLENDİ 2026-06-29.** Altyapı canlı ama bu planın öngördüğü tek `Lookups` tablosu **SUPERSEDED**: gerçekte `DictionaryType`/`DictionaryValue` (migration `34_CreateMosaikCoreLookup.sql`) + `LookupService` (`GetValuesAsync(typeCode)`, IMemoryCache 10dk) kullanılıyor. Tamim (`38_SeedTamimLookups.sql`) + SOP (`08`) seed'lemiş; **Contracts/Obligations etmemiş** — 5 enum hâlâ view'larda hardcoded `<option>` (`Contracts/Create.cshtml:140-146` vb.), `/Admin/Lookups` CRUD **YOK**. Bu plan o eksiği DictionaryType altyapısıyla kapatır (yeni tablo YOK).

### Güncel pattern (34 + LookupService)
- `DictionaryTypes(Id, Code, Name, IsActive)` + `DictionaryValues(TypeId, Code, Label, DisplayOrder, IsActive)`. Code = camelCase EN, Label = TR (turkish-ui).
- View tüketim: `await _lookup.GetValuesAsync("contractCategory")` → dropdown.
- **Enum↔lookup eşleme:** enum kolonu (`ContractCategory Category` int) KALIR. `DictionaryValue.Code` = enum üye camelCase'i (örn. `ContractCategory.Rent`→`"rent"`). POST'ta Code→enum parse, render'da enum→Code→Label. **Admin sadece Label/DisplayOrder/IsActive düzenler** (Code lock, enum bütünlüğü korunur); admin yeni-kategori-ekleme bu fazda YOK (enum üyesi olmadan int gelmez — kapsam dışı, Risk tablosu).

---

## 1. Problem

Mosaik'te 16 enum var. 5 tanesi (`ContractCategory`, `ObligationCategory`, `ObligationType`, `RecurrenceType`, `EventType`) admin'in düzenleyebilmesi gereken iş kategorileri — şirket farkı, sektör farkı, yeni ihtiyaç durumunda kod değişikliği + deploy gerektiriyor. Şu an form dropdown'larında ~35 hardcoded `<option>` satırı var, Türkçe etiket Razor'da inline.

Kullanıcı kararı: "varsa başka yerlerde de enumları normal db ye çevir".

## 2. Scope

### Kapsam dahili
- Tek `Lookups` tablosu (`GroupKey`, `Code`, `Label`, `SortOrder`, `IsActive`, `IsSystemDefined`).
- 5 enum DB lookup'a geçer (29 seed row): ContractCategory, ObligationCategory, ObligationType, RecurrenceType, EventType.
- `ILookupService` cache'li (5dk TTL, IModuleService pattern), `Invalidate()` admin save sonrası.
- Admin `/Admin/Lookups` CRUD ekranı (group + label + order + active toggle).
- Form dropdown'ları `ViewBag.Lookups` ile beslenir (5 view).
- **Enum kolonu INT kalır** — DB'de saklanan değer değişmez, Lookup sadece Türkçe etiket + sıra. Mevcut switch/match kodları bozulmaz.

### Kapsam dışı
- Lookup'a aday OLMAYAN 11 enum (ContractStatus, ObligationStatus, ExtractionStatus, SuggestionStatus, Confidence, EventStatus, ObligationSource, EventSource, ApprovalStatus, TokenType, SuggestionType) — kod-içi state machine, dokunulmaz.
- Multi-language (TR + EN ikinci kolon) — şu an TR-only.
- User-defined custom group ekleme — sadece sabit 5 group.
- Migration ile mevcut data değiştirme (DB'deki INT değerler enum ordinal'iyle eşleşiyor zaten).

### Etkilenen dosyalar (güncel — DictionaryType altyapısı, yeni tablo YOK)

**Faz 1 — seed + view wiring (asıl kullanıcı kazancı, ~6 dosya):**
- `Mosaik/Database/75_SeedContractLookups.sql` — 5 DictionaryType (`contractCategory`, `obligationCategory`, `obligationType`, `recurrenceType`, `eventType`) + ~29 DictionaryValue (Code = enum üye camelCase). İdempotent, 38 pattern.
- `Mosaik/Controllers/ContractsController.cs` + `ObligationsController.cs` — GET'te `ViewBag.X = await _lookup.GetValuesAsync(...)`, POST'ta Code→enum parse fallback.
- `Mosaik/Views/Contracts/Create.cshtml` + `Edit.cshtml` — hardcoded `<option>` → lookup loop.
- `Mosaik/Views/Obligations/Create.cshtml` — aynı.
- `Mosaik/Views/Compliance/Preview.cshtml` + `Calendar/Index.cshtml` — RecurrenceType/EventType label lookup.

**Faz 2 — admin CRUD (opsiyonel, daha ağır):**
- `Mosaik/Controllers/AdminController.Lookups.cs` + `Views/Admin/Lookups.cshtml` — DictionaryType/Value CRUD (Label/DisplayOrder/IsActive edit, Code read-only). `LookupService` cache invalidation save sonrası.

**Tahmini boyut:** Faz 1 ~6 dosya/~200 satır; Faz 2 ~2 dosya/~200 satır.

> ⚠️ `Lookup.cs` entity + `51_CreateLookups.sql` + ayrı `LookupService` (eski spec) **YAZILMAYACAK** — DictionaryType/Value + mevcut `LookupService` kullanılır.

## 3. Alternatifler

### A: Her enum için ayrı tablo (`ContractCategories`, `ObligationCategories`, ...)
**Açıklama:** 5 ayrı tablo, her biri için ayrı admin CRUD endpoint.
**Reddetme sebebi:** 5 admin ekranı + 5 EF konfig + 5 controller partial = solo-dev için orantısız. YAGNI.

### B: Resource file (.resx) ile sadece Türkçe label
**Açıklama:** Enum kod'da kalır, etiketler `Resources/Enums.tr.resx`'te.
**Reddetme sebebi:** Admin DB'den düzenleyemez — kod değişikliği + deploy şart. Kullanıcı talebine ters.

### C: Tek `Lookups` tablo + group kolonu (SEÇİLEN)
**Açıklama:** YonetIQ pattern, 1 tablo + GroupKey filter, tek admin ekranı + group dropdown.
**Sebep:** Minimum schema overhead, admin tek noktadan yönetir, EF tek DbSet, cache bir tane.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Admin yanlış Code değiştirir → enum int eşlemesi bozulur | yüksek | orta | `Code` field admin UI'da read-only (sadece Label/SortOrder düzenlenebilir). `IsSystemDefined=1` ise Code lock. |
| Lookup eklenince enum'a yeni değer gelmez (DB-only) | orta | yüksek | Admin yeni row ekleyince enum INT yeni değer olmaz — bu özellikle "yeni kategori sadece DB'de" tasarımı. UI gösterir, kod switch'lerinde default fallback olmalı. Doc'a yaz. |
| Cache stale (admin save sonrası eski label görünür) | düşük | orta | `LookupService.Invalidate()` save endpoint'inde çağrılır. |
| 5 view'da dropdown migration sırasında uyumsuzluk | orta | orta | Faz B view'larda hardcoded `<option>` blokları + ViewBag.Lookups paralel kalır, fallback `??` operatörü ile. |

## 5. Done Criteria

- [ ] Migration 51 uygulanır, 29 seed row görünür.
- [ ] `/Admin/Lookups` ekranında 5 group filtre, edit/create/delete çalışır.
- [ ] Contracts Create/Edit, Obligations Create, Compliance Preview, Calendar Index — tüm dropdown'lar Lookups'tan beslenir.
- [ ] Admin yeni "Bayilik" row eklerse Contracts Create dropdown'ında anında görünür (cache invalidation).
- [ ] 246 test geçer, build yeşil.
- [ ] Code field read-only, IsSystemDefined=1 row'lar silinemez.

## 6. Rollback Planı

- Git revert.
- Migration down: `DROP TABLE Lookups`.
- View'lar hardcoded `<option>` bloklarına geri döner (paralel kaldığı için zaten çalışır).

## 7. Adımlar

1. [ ] Lookup entity + migration 51 + 29 seed row
2. [ ] `LookupService` singleton + cache (IModuleService pattern)
3. [ ] AdminController.Lookups partial + ViewModel
4. [ ] Lookups admin CRUD view (modern .field/.lab/.inp pattern, inline style YASAK)
5. [ ] Contracts Create/Edit dropdown migration
6. [ ] Obligations Create dropdown migration
7. [ ] Compliance Preview + Calendar Index label migration
8. [ ] Test: cache invalidation + IsSystemDefined lock + Code read-only

## 8. İlişkili

- Plan 16.5 (Mosaik.Core shared kit) — Lookup ileride Core'a port edilebilir
- Plan 20 (Org Chart) — UnvanLookup için aynı pattern kullanılabilir
- DikkatIQ + YonetIQ'da Lookup pattern yok (her ikisi de kod enum) — Mosaik öncü

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Onay alındı: ___
