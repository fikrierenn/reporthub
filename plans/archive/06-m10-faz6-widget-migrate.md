# Plan 06 — M-10 Faz 6: Widget binding migrate + legacy fallback kaldır

**Tarih:** 2026-05-06
**Yazan:** Fikri / Claude
**Durum:** Tamamlandı (2026-05-06)

---

## 1. Problem

ADR-007 Named Result Contract Faz 1-5 ✅ tamamlandı. Faz 4 ile runtime soft-fail audit devreye girdi, Faz 5 (Migration 26) ile PDKS + Satış raporlarına `resultContract` (RS isimleri) idempotent eklendi. Ama widget binding'leri hala legacy `resultSet: N` int index kullanıyor:

| Rapor | Widget | Binding tipi |
|---|---:|---|
| PDKS Pano (13) | 15 | `resultSet: N` legacy index |
| Satış Pano (14) | 16 | `resultSet: N` legacy index |
| KPI Test sil (16) | 3+1 | `result: "rsN"` pattern + 1 binding-yok |

`DashboardConfig.ResolveResultSet`'te 3 katmanlı fallback (named → "rsN" regex → legacy `comp.ResultSet`) hala canlı. Faz 6'nın amacı: widget'ları named binding'e migrate edip 3. fallback dalını (legacy index) kaldırmak. Böylece "ne kullanıldığı" tek source-of-truth (`result: "<name>"`) ile belli olur, RS sırası değişirse widget'lar bozulmaz.

## 2. Scope

### Kapsam dahili
- **Migration 27** — PDKS 15 + Satış 16 widget için `resultSet: N` → `result: "<contractName>"` mapping. `resultSet` field'ı sil. Idempotent (re-run safe).
- **DashboardConfig.ResolveResultSet** — 3. dal (`comp.ResultSet.HasValue`) ve `DashboardComponent.ResultSet` property'si sil. Tek source: `comp.Result`.
- **PlaceholderRenderer.cs:16-17** — legacy `resultSet: N` branch sil.
- **DashboardConfigValidator** — `comp.ResultSet` referans eden hard rule'ları (negatif index, out-of-bounds) güncelle. Validator'da `result` zorunlu kuralı sertleşir (NoBinding artık hata).
- **DashboardComponent model** — `ResultSet` property `[Obsolete]` veya tamamen sil (kullanım yok kaldıktan sonra).
- **Smoke test** — PDKS Pano + Satış Pano /Reports/Run render ✓; widget'lar veriyi name-based bağlamış olmalı.
- **Unit test** — `DashboardConfigTests.ResolveResultSet`, `DashboardConfigValidatorTests` legacy index branch testleri silinir/güncellenir.

### Kapsam dışı
- **"rsN" regex fallback (2. dal)** — KORUNUR. V2 builder yeni widget eklediğinde `result: "rs0"` üretmeye devam ediyor; bu fallback olmadan builder UI'sı bozulur. Migration ileri bir aşamada (Faz 7?) ele alınabilir.
- **builder-drawer.js / builder-render.js davranışı** — `comp.result && /^rs\d+$/.test(comp.result)` kullanımı korunur (Senaryo B kapsamı, ayrı oturum).
- **KPI Test rapor 16** — geliştirme/test artığı. Migration 27'nin scope'u canlı 2 rapor. ID 15/16 manuel veya silme tercihi sonra.

### Etkilenen dosyalar (tahmin)
- `ReportPanel/Database/27_MigrateWidgetBinding.sql` — yeni (idempotent JSON_MODIFY)
- `ReportPanel/Models/DashboardConfig.cs` — ResolveResultSet 3. dal sil, `DashboardComponent.ResultSet` property sil/Obsolete
- `ReportPanel/Services/Rendering/PlaceholderRenderer.cs:16-17` — legacy branch sil
- `ReportPanel/Services/DashboardConfigValidator.cs` — legacy resultSet validation kaldır, NoBinding artık hard error
- `ReportPanel.Tests/DashboardConfigTests.cs` — ResolveResultSet legacy testleri update
- `ReportPanel.Tests/DashboardConfigValidatorTests.cs` — legacy index validation testleri update
- `TODO.md` — Faz 6 [x] işaretle, kapsam dışı kalan "rsN fallback" yeni satır
- `docs/ADR/007-named-result-contract.md` — Faz 6 sonuç notu (varsa)

**Tahmini boyut:** ~6-7 dosya / ~150 satır net (test güncelleme dahil).

## 3. Alternatifler

### A: Migration C# seeder (DashboardSeeder)
**Açıklama:** SQL yerine `Database/Seeders/DashboardWidgetMigrator.cs` — DB'den ConfigJson oku, parse et, her component'in `resultSet` index'ine resultContract entry ile lookup yap, `result: "<name>"` set et + `resultSet` sil, geri yaz. Migration runner script (Program.cs IF DEBUG flag).
**Reddetme sebebi:** Mevcut migration disiplini düz SQL (`Database/NN_*.sql`). C# seeder pattern projede yok — yeni pattern + runner mekanizması = scope creep. Tek seferlik veri dönüşümü için fazla.

### B: Manuel SSMS UPDATE (kullanıcı eli ile)
**Açıklama:** Migration 27 yazılmaz. 31 widget için kullanıcı SSMS'te tek tek UPDATE.
**Reddetme sebebi:** İdempotent değil, audit yok, hatalı çalışmaya açık. Plan 07 cascade fix sırasında manuel SQL kabul edildi (1 satır) ama 31 widget farklı ölçek. Migration script audit'lenebilir + re-run güvenli + tek dosya.

### C (seçilen): Migration 27 idempotent SQL — JSON_MODIFY + OPENJSON + UPDATE
**Açıklama:** `Database/27_MigrateWidgetBinding.sql` — Migration 26 pattern'i: WHERE clause'da "henüz migrate edilmemiş" filtresi (`JSON_VALUE(component, '$.result') IS NULL AND JSON_VALUE(component, '$.resultSet') IS NOT NULL`). Her widget için resultContract entry'sinin index'ini lookup yap, `result` set et, `resultSet` sil. PDKS + Satış için ayrı blok (resultContract isimleri farklı).
**Sebep:** Mevcut migration disiplinine uygun, idempotent, audit'lenebilir. Migration 26 zaten benzer pattern (idempotent JSON_MODIFY) — proven.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| JSON_MODIFY array path syntax karmaşık (`$.tabs[0].components[3].result`) | orta | yüksek | Önce SELECT ile path'leri çıkar, sonra UPDATE. WHILE loop veya OPENJSON CROSS APPLY ile her satırda doğru index. Test DB'de dry-run önce. |
| ResultContract isimleri yanlış map'lenirse widget'lar boş veri gösterir (silent) | yüksek | düşük | Migration sonrası SELECT ile her widget'ın `result` değeri + RS index'in örnek 1 satır verisini (column header'lar) yan yana göster. PDKS + Satış preview manuel kontrol. Audit log `dashboard_required_result_missing` event'i Faz 4'ten beri uyarır. |
| Validator sertleştirmesi mevcut kayıtlı raporları "geçersiz" gösterir (admin save bozulur) | orta | orta | Validator değişikliği Migration 27'den **sonra** deploy edilir. Önce migrate, sonra kod. (Tek deploy step için: kod migrate'i tetikleyen seed satırı sırasıyla çalışırsa tek atomik). |
| Renderer 3. dal kaldırınca PDKS/Satış dışı raporlarda regresyon | düşük | düşük | DB'de PDKS/Satış dışında `resultSet` legacy binding kullanan başka aktif rapor yok (yukarıdaki bulgu). Test 16/15 geliştirme artıkları, IsActive=1 ama scope dışı. |
| Geri alma maliyeti: Migration 27 down script yok (JSON_MODIFY destruktif) | orta | düşük | DB backup zorunlu (BKM SSMS routine). Rollback path: yedekten ConfigJson geri yükle (5 satır SQL). Detay §6. |

## 5. Done Criteria

- [x] Migration 27 yazıldı, idempotent re-run ✓
- [x] PDKS Pano + Satış Pano widget'larında `resultSet` field yok, `result: "<contractName>"` var (DB SELECT doğrulama: PDKS 15/15 NamedBinding, Satış 16/16 NamedBinding)
- [x] `DashboardConfig.ResolveResultSet` 3. dal silindi; `DashboardComponent.ResultSet` property silindi
- [x] `PlaceholderRenderer.cs:16-17` legacy branch silindi
- [x] `DashboardConfigValidator` legacy resultSet rule'ları kaldırıldı; NoBinding artık hard error
- [x] Build yeşil, 228/228 test yeşil (önceki 229'dan -1: legacy negatif resultSet testi silindi)
- [x] Smoke (browser): PDKS Pano render ✓, Satış Pano render ✓ (UserDataFilter atama sonrası — Plan 07 deny-by-default)
- [x] TODO.md M-10 Faz 6 [x] işaretlendi, "rsN fallback ileride" notu eklendi
- [x] Journal entry yazıldı

## 6. Rollback Planı

**Migration 27 sorun yarattıysa:**
1. SSMS'te BKM yedekten DashboardConfigJson geri yükle:
   ```sql
   UPDATE dbo.ReportCatalog
   SET DashboardConfigJson = (SELECT DashboardConfigJson FROM <backup_db>.dbo.ReportCatalog WHERE ReportId = source.ReportId)
   FROM dbo.ReportCatalog source
   WHERE ReportId IN (13, 14);
   ```
2. `git revert <commit>` — kod değişikliklerini geri al (renderer 3. dal restore, validator restore).
3. App restart.

**Migration başarılı, kod regresyon:**
- `git revert <code-commit>` yeterli; Migration 27 verisini geri alma gerekmez (named binding 1. dal hala çalışır).

## 7. Adımlar

1. [ ] **F6.1** Migration 27 yazıldı + dry-run (test DB veya `BEGIN TRAN ... ROLLBACK`)
2. [ ] **F6.2** Kullanıcı PortalHUB'da Migration 27 çalıştırdı + doğrulama SELECT ✓
3. [ ] **F6.3** `DashboardConfig.cs` ResolveResultSet 3. dal sil, `DashboardComponent.ResultSet` property sil/Obsolete
4. [ ] **F6.4** `PlaceholderRenderer.cs` legacy branch sil
5. [ ] **F6.5** `DashboardConfigValidator.cs` legacy rules güncelle, NoBinding hard error
6. [ ] **F6.6** Unit test'leri güncelle/sil (ResolveResultSet legacy + Validator legacy)
7. [ ] **F6.7** `dotnet test` yeşil
8. [ ] **F6.8** Browser smoke: PDKS Pano + Satış Pano render, console error yok
9. [ ] **F6.9** TODO.md güncelle + journal entry + commit (`feat(dashboard): M-10 Faz 6 — widget binding migrate + legacy fallback kaldır (plan: 06)`)

## 8. İlişkili

- ADR: `docs/ADR/007-named-result-contract.md`
- Önceki migration: `Database/26_AddResultContract.sql` (Faz 5)
- TODO: `TODO.md` satır 95 (M-10 Faz 6 BEKLEMEDE → AKTIF)
- Konuşma: `docs/journal/2026-05-05.md` (Faz 4-5 kapanış + Faz 6 ön koşul notu)

## 9. Onay

- [x] Plan kullanıcıya gösterildi
- [x] Geri bildirim alındı (varsa düzeltildi) — itiraz yok, "uygundur"
- [x] Onay alındı: 2026-05-06 Fikri
