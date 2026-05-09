# Plan 23 — Sidebar Reorganizasyon (Collapsible Groups + Modern Admin Layout)

**Tarih:** 2026-05-10
**Yazan:** Claude
**Durum:** `Taslak`

---

## 1. Problem

Mevcut sidebar (`_AppLayout.cshtml:78-235`) hibrit ve eksik:
- 3 link hardcoded (`Dashboard`, `OrgChart`, `Contracts`) — `AppModules` toggle etkilemiyor.
- Mevcut controller'lar sidebar'da YOK: `Obligations`, `Documents`, `Notifications`. Kullanıcı bu sayfalara doğrudan URL ile gidiyor.
- Düz liste — gruplama yok. 9-10 modül olunca dikey scroll ihtiyacı.
- Açılır/kapanır section yok. Vanilla JS, Alpine.js sidebar'da kullanılmıyor.
- Admin subnav uyumsuz (`Index.cshtml:29-68` ile `_AdminSubnav.cshtml` farklı link sayısı).

Kullanıcı kararı: "modern bir admin sayfası olmalı, açılır kapanır menü olabilir, menüde olmayan bir sürü şeyler var".

## 2. Scope

### Kapsam dahili
- 5 sidebar grubu: **Ana**, **Çalışma Alanı** (Reports/Tamim/Calendar/Documents), **Sözleşmeler & Uyum** (Contracts/Obligations/Compliance), **Yapı & Yapay Zeka** (OrgChart/AI), **Sistem** (Logs/Admin).
- AppModules tablosuna eksik 4 seed: `contracts`, `obligations`, `orgchart`, `documents`. Mevcut hardcoded 3 link kaldırılır → tam DB-driven.
- AppModules tablosuna `GroupKey NVARCHAR(50)` kolonu — modül hangi grupta gösterilecek.
- Alpine.js `x-data` + `x-collapse` plugin (CDN) ile collapsible group. localStorage persistence per-grup (`sidebar.group.<key>`).
- Collapsed (icon-only) sidebar modunda group header'ları gizlenir, sadece ikonlar (mevcut CSS pattern korunur).
- Mobile drawer'da gruplar açık başlar (collapsed state ignore).
- `_AdminSubnav.cshtml` ile `Admin/Index.cshtml` subnav uyumlu hale getirilir — tek source-of-truth.
- AppModule add/edit ekranına `GroupKey` dropdown.

### Kapsam dışı
- Multi-level nested menü (sub-menü içinde sub-menü).
- Drag-drop sıralama admin'de — SortOrder edit yeterli.
- Custom group ekleme — sabit 5 group enum/lookup.
- Tema (dark mode) — ayrı ileri sprint.
- Sidebar arama — 9-10 modül için gereksiz.

### Etkilenen dosyalar
- `Mosaik/Database/54_AppModulesGroupKey.sql` — kolon ekleme + 4 yeni seed + mevcut 6 seed güncelleme
- `Mosaik/Models/AppModule.cs` — GroupKey property
- `Mosaik/Services/ModuleService.cs` — GetGroupedAsync()
- `Mosaik/Views/Shared/_AppLayout.cshtml` — sidebar render (hardcoded 3 link kaldırılır)
- `Mosaik/wwwroot/assets/js/app-shell.js` — Alpine entegrasyon (mevcut vanilla collapse korunur)
- `Mosaik/wwwroot/assets/css/app-shell.css` — collapsible group stil (chevron rotation, indent)
- `Mosaik/Views/Admin/_AdminSubnav.cshtml` + `Index.cshtml` — uyumlu hale getir
- `Mosaik/Views/Admin/Modules.cshtml` — GroupKey dropdown
- Alpine Collapse plugin CDN: `_AppLayout.cshtml` head ekleme

**Tahmini boyut:** 8 dosya / ~300 satır.

## 3. Alternatifler

### A: Vanilla JS `<details>/<summary>`
**Açıklama:** Native HTML disclosure widget, JS gerekmez.
**Reddetme sebebi:** localStorage persistence + collapsed icon-only mode hâlâ JS şart. Animation yok. Alpine zaten projede var, ek dependency değil.

### B: Tam hardcoded grouping (DB değişikliği yok)
**Açıklama:** Razor switch-case ile ModuleKey'e göre group'a yerleştir.
**Reddetme sebebi:** Yeni modül eklenince Razor switch güncellemek gerek — admin DB'den yapamaz. AppModules.GroupKey kolonu DRY ve esneklik.

### C: AppModules.GroupKey + Alpine x-collapse + 5 sabit group (SEÇİLEN)
**Açıklama:** DB-driven group atama, Alpine animasyon + localStorage, 5 sabit group key (admin custom group ekleyemez).
**Sebep:** Maksimum esneklik (admin yeni modülü gruba atayabilir) + minimum DB schema değişiklik (1 kolon) + zaten mevcut Alpine ekosistemi.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Mevcut hardcoded link kaldırılınca AppModules seed eksikse Dashboard'a erişim kalmaz | yüksek | düşük | `Dashboard` zaten hardcoded "Ana" grupta kalır (sistem link). Migration 54 önce 4 seed yazar, sonra hardcoded blok kaldırılır. |
| Alpine Collapse plugin CDN çakışması | düşük | düşük | Mevcut `alpinejs@3.x` ile uyumlu plugin versiyonu (`@alpinejs/collapse@3.x`). |
| localStorage stale (kullanıcı eski state'le açar) | düşük | yüksek | Group key'ler stable, default `open=true`. Stale durumda zarar yok. |
| Mobile responsive bozulur | orta | orta | Mevcut `app-shell.js` mobile drawer logic korunur, sadece group HTML eklenir. Test gerekir. |
| Inline style yasağı ihlali | orta | orta | Tüm yeni CSS `app-shell.css`'e, hiç `style="..."` attribute yok. |

## 5. Done Criteria

- [ ] Migration 54 uygulanır, 10 modül AppModules'te (`reports`, `dashboards`, `tamim`, `calendar`, `compliance`, `ai`, `contracts`, `obligations`, `orgchart`, `documents`).
- [ ] Sidebar 5 grup gösterir, her grup chevron ikonu ile aç/kapat.
- [ ] localStorage'a state yazar, sayfa reload'da korur.
- [ ] Collapsed (icon-only) modda group header gizlenir, ikonlar kalır.
- [ ] Mobile <1024px drawer'da gruplar açık başlar.
- [ ] Admin AppModule edit ekranında GroupKey dropdown çalışır.
- [ ] `_AdminSubnav.cshtml` ile `Admin/Index.cshtml` aynı 10 link.
- [ ] Hardcoded `Dashboard`/`OrgChart`/`Contracts` linkleri `_AppLayout.cshtml`'den temizlenir.

## 6. Rollback Planı

- Git revert.
- Migration 54 down: `ALTER TABLE AppModules DROP COLUMN GroupKey; DELETE FROM AppModules WHERE ModuleKey IN ('contracts','obligations','orgchart','documents')`.
- `_AppLayout.cshtml` hardcoded blok geri eklenir.

## 7. Adımlar

1. [ ] Migration 54: AppModules.GroupKey kolonu + 4 yeni seed + 6 mevcut update
2. [ ] AppModule entity + ModuleService.GetGroupedAsync()
3. [ ] _AppLayout.cshtml sidebar render: 5 group loop, Alpine x-data
4. [ ] Alpine Collapse CDN + app-shell.css collapsible stil
5. [ ] app-shell.js mobile drawer + group state etkileşimi
6. [ ] Admin Modules ekranında GroupKey dropdown
7. [ ] _AdminSubnav + Admin/Index uyumlulaştırma (10 link, tek source)
8. [ ] Test: collapsed mode, mobile drawer, localStorage persistence

## 8. İlişkili

- Plan 12 (Brand+Modules) — AppModules tablosu zaten kuruldu, GroupKey ek kolon
- Plan 17 (Tamim) — `tamim` ModuleKey GroupKey'e atanır
- Plan 25 (Sözleşme) — `contracts`/`obligations` GroupKey
- ADR planı: `docs/ADR/011-sidebar-grouping.md` (yazılacak)

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Onay alındı: ___
