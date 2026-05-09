# Plan 20 — Organizasyon Şeması (İK Görev Hiyerarşisi)

> Tier 3 plan. Tier 1 (yok) ve Tier 2 (TODO satırı) için kullanma.

**Tarih:** 2026-05-08
**Yazan:** Claude (Fikri yönetiminde)
**Durum:** `Tamamlandı` (2026-05-08, Faz A+B+C — sağ-tık modal + Zirve canlı incumbent + PNG export)
**Tier:** 3 (yeni domain entity + migration + admin UI + görsel render + cross-DB join)

---

## 1. Problem

Mosaik'te organizasyon şeması yok. Zirve `BKM_GENEL.dbo.vw_PersonelDepartman` view'ında 271 aktif personelin Lokasyon + AltLokasyon + Departman + Unvan'ı tutuluyor — ama **Unvan'lar arası hiyerarşi (kim kime bağlı) yok**.

İhtiyaç: Şirket görev hiyerarşisi (CEO → Operasyon → Bölge Müdürü → Mağaza Müdürü → Kasiyer gibi) Mosaik'te tanımlanabilsin. Üzerine Zirve canlı personeli her node'a otomatik dağılsın. Sonuç: tek görsel org chart, tıklanabilir, herkes hangi pozisyonun altında çalışıyor görür.

## 2. Scope

### Kapsam dahili

- **Domain:** `OrgPosition` entity (Mosaik tarafında, Zirve'ye dokunmaz). `Code` = Unvan adı (Zirve `Unvan` ile eşleşir), `ParentPositionId`, `DisplayOrder`, `IsActive`.
- **Migration:** Tek tablo `OrgPositions` + index'ler.
- **Servis:** `IOrgChartService` — pozisyon CRUD, hiyerarşi yöneten + Zirve personel join (cross-DB SELECT).
- **Admin UI:** Pozisyon CRUD + **drag-drop nested tree editor** (Faz B). SortableJS (~30KB CDN) + HTMX (reorder AJAX) + Alpine (form state). Vanilla IIFE yazılmaz.
- **Görsel render:** Tıklanabilir tree (D3.js veya basit nested CSS — karar Faz C'de). Her node'da pozisyon adı + incumbent sayısı + tıklayınca personel listesi.
- **Cross-DB join:** Mosaik DB → Zirve `BKM_GENEL.dbo.vw_PersonelDepartman` linked-server veya 4-part naming. Yalnızca SELECT.
- **Türkçe UI:** "Organizasyon Şeması", "Görev", "Bağlı Olduğu Görev", "Çalışanlar".
- **Yetki:** Görüntüleme tüm aktif kullanıcılara açık. CRUD admin-only.

### Kapsam dışı

- **Lokasyon × Unvan (Model 2)** — kullanıcı kararı: tek node ile başla. Aynı unvan farklı mağazalardaysa tek node, çoklu incumbent.
- **Manager-direct report (kişi bazlı)** — pozisyon bazlı yeterli; X kişi Y kişiye bağlı değil, X pozisyonu Y pozisyonuna bağlı.
- **Reorg history / vakansi takibi** — Faz D adayı (vNext).
- **Position permission** ("bu pozisyondaki kişi raporları görsün") — Plan 14 Faz B + UserDataFilter scope dışı.
- **Zirve'ye yazma** — kesinlikle SELECT-only. Yeni Unvan / personel atama Zirve tarafında yapılır, Mosaik mirror.

### Etkilenen dosyalar (tahmin)

**Yeni:**
- `Mosaik.Core/Domain/OrgPosition.cs` — entity
- `Mosaik/Services/IOrgChartService.cs` + `OrgChartService.cs`
- `Mosaik/Database/NN_CreateOrgPositions.sql` — migration
- `Mosaik/Controllers/AdminController.OrgChart.cs` — admin partial
- `Mosaik/Controllers/OrgChartController.cs` — public read (görsel sayfa)
- `Mosaik/Views/Admin/OrgChart.cshtml`, `EditPosition.cshtml`
- `Mosaik/Views/OrgChart/Index.cshtml` — `/OrgChart` görsel sayfa
- `Mosaik/ViewModels/Admin/AdminOrgPositionFormViewModel.cs`
- `Mosaik/wwwroot/assets/js/org-chart-render.js` — görsel render
- `Mosaik.Tests/OrgChartServiceTests.cs`

**Modify:**
- `Mosaik/Models/MosaikContext.cs` — DbSet<OrgPosition>
- `Mosaik/Program.cs` — DI kayıt
- `Mosaik/Views/Shared/_AppLayout.cshtml` — sidebar nav (gerekirse)

**Tahmini boyut:** ~12 yeni dosya, ~3 modify, ~600 satır + ~100 satır migration. ~8-10h toplam (3 faz).

## 3. Alternatifler

### A: Mosaik'te entity, Zirve'den join

**Açıklama:** OrgPosition Mosaik'te durur (hiyerarşi + display order). Personel atama Zirve view'dan canlı çekilir, `Unvan` field'ı match key. Cross-DB query.

**Reddetme sebebi:** Yok — bu seçildi.

### B: Tüm yapı Zirve'de, Mosaik sadece görsel

**Açıklama:** Zirve view'a yeni kolon (`ParentUnvan`) ekleyip hiyerarşiyi orada tanımla.

**Reddetme sebebi:** Zirve corporate production DB; schema değişikliği için BT/IK departman onayı + downstream sistem analizi gerekir. Read-only kuralı (kullanıcı: "özellikle zirve tarafına sadece select"). Mosaik tarafında entity = düşük blast radius.

### C: Personel-level hierarchy (kişi bazlı manager)

**Açıklama:** Her personelin "manager" alanı Mosaik'te. CEO için NULL, herkes için bir parent person.

**Reddetme sebebi:** 272 kişi × manager atama = 272 manuel kayıt. Reorg her zaman pozisyon bazlı düşünülür, kişi gelse de gitse de pozisyon kalır. Position-based daha doğru abstraction.

### D: External org chart tool (BambooHR / Pingboard / Lucidchart embed)

**Açıklama:** Üçüncü parti SaaS, Mosaik iframe.

**Reddetme sebebi:** Şirket içi data dış servise gönderilemez (kurumsal politika), Zirve bağlantısı yok, lisans ücreti.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Zirve `Unvan` text farklılıkları (whitespace, case, tipo) — match'lerken kayıp | yüksek | yüksek | Mosaik `OrgPosition.Code` unique + Zirve canlı SELECT'te `LTRIM(RTRIM(Unvan))` + case-insensitive compare. Eşleşmeyen personeli "tanımsız" bucket'a düşür, admin'e uyarı |
| Cross-DB query izin/performans (`BKM_GENEL.dbo.vw_PersonelDepartman` 4-part name) | orta | orta | Mosaik service account'unun BKM_GENEL + ASİYE_BİNGÖLBALI_GENEL + BKM_2_GENEL'e SELECT yetkisi olduğunu doğrula (G-09 read-only login bağlamı). View 3 firma DB'sini UNION ettiği için her birine erişim şart. Sorgu dakikada bir cache'lenir, IMemoryCache 5dk TTL |
| Hiyerarşi döngüsü (A → B → A) | orta | düşük | Servis tarafında parent set ederken cycle detect (DFS); admin UI uyarısı |
| Görsel render kütüphane bağımlılığı (D3 ~70KB) | düşük | düşük | Faz C'de karar; basit nested CSS yeterliyse D3 atlanabilir. CDN-first, yerel fallback |
| Zirve'deki yeni unvan ortaya çıkması | orta | orta | Hiyerarşi otomatik güncellenmez. Admin GUI'de "Zirve'de var ama Mosaik'te tanımsız" listesi → tek tık ekle |
| 272 personel sayısı büyürse render yavaşlar | düşük | düşük | Tree lazy expand (default 2 seviye açık), incumbent listesi tıklanınca yüklenir |
| Aynı Unvan farklı firmalarda farklı anlam (BKM "Müdür" ≠ ASİYE "Müdür") | orta | orta | İlk fazda **tek hiyerarşi** (Firma boyutu yok). İhtiyaç çıkarsa Faz D'de Firma × Unvan kombinasyon |

## 5. Done Criteria

### Faz A — Backend altyapı (~2h)

- [ ] `OrgPosition` entity (Mosaik.Core/Domain), [BindNever] Id, MaxLength constraint'ler, navigation property (self-ref Parent + Children)
- [ ] Migration NN — `OrgPositions` tablosu (Id PK, Code UNIQUE, ParentPositionId FK self, DisplayOrder, IsActive, CreatedAt/By, UpdatedAt/By)
- [ ] `MosaikContext.DbSet<OrgPosition>` + EF mapping (self-ref FK)
- [ ] `IOrgChartService` skeleton + `OrgChartService` impl (CRUD + GetTree + ZirveJoin yer tutucu)
- [ ] DI kayıt
- [x] Zirve keşif (2026-05-09): `SELECT DISTINCT LTRIM(RTRIM(Unvan)) FROM BKM_GENEL.dbo.vw_PersonelDepartman WHERE Ict IS NULL` → **61 farklı Unvan / 271 aktif personel** (bkz. §10 Zirve Keşif Çıktısı)
- [ ] xUnit: 4 test (Create/Update/Delete + Cycle detect)

### Faz B — Admin UI (~6h, scope drag-drop'tan sağ-tık modala pivot etti)

- [x] **Migration 40** — 61 unvan + 6 katmanlı default hiyerarşi seed (idempotent)
- [x] **`/Admin/OrgChart?view=...`** — 3 görünüm: `list` (nested ul + SortableJS sibling reorder), `tree` (klasik kutucuklu top-down, dabeng/OrgChart v5), `horizontal` (sol-sağ aynı lib)
- [x] **`/Admin/EditPosition`** — Code + Title + Parent + DisplayOrder + IsActive + Description form
- [x] **`POST /Admin/OrgChart/Reorder`** — JSON body, server cycle detect + sibling parent guard
- [x] **`POST /Admin/OrgChart/ImportFromZirve`** — Tanımsız unvan tek-tık import
- [x] **Tanımsız Unvan widget** — Zirve cross-DB cached canlı keşif
- [x] **Sağ-tık "Üst görevi değiştir" modal** (Alpine, tüm view'larda) — drag-drop yerine, kullanıcı kararı (drag-drop UX güvenilmez bulundu)
- [x] **PNG export** — tree/horizontal view'larında `oc.export()` butonu (html2canvas)
- [x] **Audit:** `org_position_create/update/delete/reorder/import` events
- [x] **Türkçe UI** (Görev / Bağlı Olduğu Görev / Sıra / Aktif / Açıklama)
- [x] **xUnit:** 5 yeni test (ReorderAsync × 3, ImportFromZirveCodeAsync × 2). 17/17 OrgChart, 315/315 toplam
- [x] **Sidebar nav** Yönetim → Organizasyon linki (`fa-sitemap`)

**Pivotlar (Faz B sırasında):**
- D3 → OrgChart.js v5 (jQuery, klasik kutucuklu görünüm + json-digger + html2canvas dependency)
- Drag-drop → sağ-tık modal (cross-parent + JSONDigger quirks güvenilmez)

### Faz C — Görsel + Personel Atama (~3-4h)

- [ ] `IOrgChartService.GetChartWithIncumbentsAsync()` — pozisyon ağacı + Zirve canlı join (cache 5dk)
- [ ] `/OrgChart` public sayfa (auth gerek, herkese açık)
- [ ] Görsel render (D3 veya nested HTML — karar burada)
- [ ] Tıklanan node'da personel listesi (ad + lokasyon + altlokasyon)
- [ ] "Tanımsız Unvan" bucket — Zirve'de var Mosaik'te yok kişiler ayrı listede
- [ ] Smoke test: Zirve canlı + Mosaik hiyerarşi → ekranda gerçek 272 personel doğru node'lara dağıldı

### Faz D — vNext (kapsam dışı)

- Reorg history (audit-level zaten var, dedicated diff UI)
- Vakansi (boş pozisyon) takibi
- Position-based permission (UserDataFilter / role bağlama)
- Firma × Unvan boyutu (BKM/BURSA_KÜLTÜR_MERKEZİ/ASİYE ayrı hiyerarşi)
- Manager-direct report (kişi bazlı override)

## 6. Rollback Planı

- **Migration:** `DROP TABLE OrgPositions` (FK cascade self-ref). Veri kaybı = sadece hiyerarşi tanımları (Zirve mirror'a etki yok).
- **Git revert:** Her faz ayrı commit, kademeli revert mümkün.
- **Cache invalidation:** Sorun çıkarsa servis çalışmaz duruma geçirilir, Zirve'ye dokunulmaz.

## 7. Adımlar / TODO maddeleri

### Faz A (bu oturum hedef)

1. [ ] **OC-A1** OrgPosition entity (Mosaik.Core/Domain)
2. [ ] **OC-A2** Migration NN — OrgPositions tablosu (idempotent)
3. [ ] **OC-A3** MosaikContext DbSet + self-ref FK config
4. [ ] **OC-A4** IOrgChartService + OrgChartService skeleton (CRUD + Cycle detect)
5. [ ] **OC-A5** DI Program.cs
6. [x] **OC-A6** Zirve distinct Unvan keşif (2026-05-09 ✅ — 61 unvan, 271 personel, §10'da tam liste)
7. [ ] **OC-A7** xUnit OrgChartServiceTests (4+ test)

### Faz B (bu oturum hedef — drag-drop scope, ~4-5h)

1. [ ] **OC-B1** Migration 40 — 61 unvan + 6 katmanlı default hiyerarşi seed (idempotent)
2. [ ] **OC-B2** AdminController.OrgChart.cs + AdminOrgPositionFormViewModel
3. [ ] **OC-B3** OrgChartService.ReorderAsync + ImportFromZirveCodeAsync (cycle-safe)
4. [ ] **OC-B4** Views/Admin/OrgChart.cshtml — nested ul tree + SortableJS + HTMX
5. [ ] **OC-B5** Views/Admin/EditPosition.cshtml — form
6. [ ] **OC-B6** "Tanımsız unvan" widget partial (Zirve cross-DB SELECT cached)
7. [ ] **OC-B7** Audit events (create/update/delete/reorder/import)
8. [ ] **OC-B8** xUnit (Reorder/Cycle/Import) + smoke test
9. [ ] **OC-B9** Sidebar nav linki (`Admin → Organizasyon Şeması`)

### Faz C (sonraki oturum)

1. [ ] **OC-C1** GetChartWithIncumbentsAsync (Zirve cross-DB cached)
2. [ ] **OC-C2** OrgChartController + Views/OrgChart/Index.cshtml
3. [ ] **OC-C3** org-chart-render.js (D3 veya nested HTML)
4. [ ] **OC-C4** Smoke test live data

## 8. İlişkili

- ADR: `docs/ADR/NN-org-chart-architecture.md` (Faz A sonrası yazılır)
- Memory: `project_zirve_personel_discovery.md` — Zirve view yapısı
- Plan 16 vNext roadmap: "Departman dizini" satırı bu plana çözüm
- Plan 18B HR Sync: gelecekte Mosaik.User ↔ Zirve sync sırasında OrgPosition.Code üzerinden personel-pozisyon eşleşme query optimizasyonu

## 9. Onay

> Kullanıcı "tek node ile başlayalım" → Faz A başlatma onayı verildi (2026-05-08). Faz B/C için ayrı onay gerek.

- [x] Plan kullanıcıya gösterildi (taslak çıktısıyla, 2026-05-08)
- [x] Tek node modeli seçildi (Lokasyon × Unvan reddedildi)
- [x] Faz B onayı (2026-05-09): seed + drag-drop tree + SortableJS+HTMX+Alpine stack onaylandı
- [ ] Faz C onayı (Görsel + canlı join)

### Açık sorular (Faz B/C öncesi cevap bekleyenler)

1. **Görsel kütüphane:** D3.js (esnek, ~70KB) vs nested CSS tree (sıfır bağımlılık) vs cytoscape.js (graph odaklı, ~250KB). Faz C başlarken karar.
2. **Tanımsız unvan UX:** Admin görünce uyarı (toast) mu, ayrı widget mu, otomatik import mu?
3. **Görsel sayfada yetki:** /OrgChart tüm aktif kullanıcılar görsün mü, yoksa rol bazlı (örn. "yönetim" rolü) mü?
4. **Çoklu firma boyutu:** Tek hiyerarşi mi (BKM, ASİYE, BKM_KÜLTÜR_MERKEZİ aynı ağaçta), yoksa firma seçici mi?
5. **Personel kartı:** Tıklanan node'da personel isim + email mi, yoksa profil sayfası link mi?

---

## 10. Zirve Keşif Çıktısı (OC-A6 ✅ 2026-05-09)

**Sorgu:**
```sql
SELECT TOP 500 LTRIM(RTRIM(Unvan)) AS Unvan, COUNT(*) AS PersonelSayisi
FROM dbo.vw_PersonelDepartman
WHERE Ict IS NULL AND Unvan IS NOT NULL AND LTRIM(RTRIM(Unvan)) <> ''
GROUP BY LTRIM(RTRIM(Unvan))
ORDER BY COUNT(*) DESC, LTRIM(RTRIM(Unvan));
```

**Sonuç:** 61 distinct Unvan / 271 aktif personel (Ict IS NULL).

### Hiyerarşi taslağı (default seed önerisi — Faz B'de admin onaylayıp düzenler)

**1. Üst Yönetim (Direktör seviyesi)**
- YÖNETİM KURULU BAŞKANI (1)
  - YÖNETİM KURULU BAŞKAN YARD. (1)
  - İŞVEREN VEKİLİ (1)
  - SAHA OPERASYON DİREKTÖRÜ (1)
  - MALİ İŞLER DİREKTÖRÜ (1)
  - FİNANS DİREKTÖRÜ (1)
  - MALİ İŞLER YÖN.SİS. DİREKTÖRÜ (1)
  - İÇ DENETİM VE İK MÜDÜRÜ (1)

**2. Orta Yönetim (Müdür/Yönetici)**
- KAFE BÖLGE MÜDÜRÜ (1) → KAFE MÜDÜRÜ (1) → KAFE YÖNETİCİSİ (1)
- MAĞAZA MÜDÜRÜ (5) → MAĞAZA MÜDÜR YRD. (6) → MAĞAZA MÜDÜR YRD. ADAYI (1)
- BİLGİ TEKNOLOJİLERİ YÖNETİCİSİ (1)
- İDARİ İŞLER YÖNETİCİSİ (1)
- SATIN ALMA YÖNETİCİSİ (1)

**3. Şef / Sorumlu**
- REYON ŞEFİ (18) → REYON ŞEFİ ADAYI (1)
- KASA ŞEFİ (3)
- MUTFAK ŞEFİ (3) → ŞEF GARSON (6)
- DEPO ŞEFİ (1) → DEPO SORUMLUSU (1) → DEPO PERSONELİ (4)
- MUHASEBE ŞEFİ (1) → MUHASEBE GÖREVLİSİ (1) → ÖN MUHASEBE GÖREVLİSİ (2)
- MAL KABUL SORUMLUSU (1) → MAL KABUL PERSONELİ (7)
- SATIN ALMA SORUMLUSU (2) → SATIN ALMA UZMANI (1) → SATIN ALMA GÖREVLİSİ (1)
- FİNANS SORUMLUSU (1)

**4. Operasyonel personel (en kalabalık)**
- SATIŞ DANIŞMANI (91), SATIŞ DESTEK PERSONELİ (1)
- KASİYER (17)
- GARSON (15), AŞÇI YARDIMCISI (10), BARİSTA (7), BULAŞIKÇI (4), YEMEKHANE PERSONELİ (1)
- TEMİZLİK PERSONELİ (11), BEKÇİ (2), ŞOFÖR (1), MAKAM ŞOFÖRÜ (1), FORKLİFT OPERATÖRÜ (1)
- ÇAĞRI MERKEZİ TEMSİLCİSİ (5)
- STOK KONTROL GÖREVLİSİ (1)

**5. Uzman / Bireysel katkıcı (genelde doğrudan yönetime bağlı)**
- İNSAN KAYNAKLARI UZMANI (2)
- GRAFİK TASARIM UZMANI (3), SOSYAL MEDYA UZMANI (3), VİDEOGRAPHER UZMANI (2), METİN YAZARI (2)
- İŞ ANALİSTİ (1), TEKNİK İŞLER UZMANI (1), ÖLÇME VE DEĞERLENDİRME UZMANI (1)
- ETKİNLİK ORGANİZASYON SORUML. (3)
- RİSK YÖNETİM VE DENETİM PERS. (1)
- YÖNETİCİ ASİSTANI (1)

**6. Stajyer / Aday**
- STAJYER (3)

### Notlar
- **271 vs 272 sayı farkı:** Memory'de 272 yazıyordu; bugünkü COUNT 271. Yeni ayrılma olmuş olabilir; sayı zamanla değişir, soft assertion.
- **Cross-DB UNION:** View 3 firma DB'sini birleştiriyor (BKM_GENEL + ASİYE_BİNGÖLBALI_GENEL + BKM_2_GENEL). `rapor_readonly`'ye 3'üne de SELECT verildi.
- **Hiyerarşi varsayım:** Yukarıdaki yapı **Claude'un tahmini** — Faz B Admin UI'da kullanıcı confirme/düzeltir. Code field'ı her unvanın tam metni (büyük harf, Türkçe karakterli).
- **"YÖN." / "YRD." / "YÖN.SİS." kısaltmalar:** View'da olduğu gibi seed et, normalize etme. Match key = LTRIM(RTRIM) + case-insensitive compare.
