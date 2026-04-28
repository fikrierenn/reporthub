# Plan 03 — Project-wide Design System Harmonization

**Tarih:** 2026-04-27
**Yazan:** Claude (Cowork oturumu, Fikri yönetiminde)
**Durum:** `Onay bekliyor`
**Branch:** `feature/m-11-dashboard-builder-redesign` üstüne ya da yeni `feature/m-13-app-shell-redesign`
**Mockup referansları:**
- [app-shell-v1.html](../ReportPanel/wwwroot/mockups/app-shell-v1.html) — Genel Bakış sayfası (sidebar + ana içerik)
- [app-shell-builder-v1.html](../ReportPanel/wwwroot/mockups/app-shell-builder-v1.html) — Builder + sidebar shell birleşimi
- [dashboard-builder-v3.html](../ReportPanel/wwwroot/mockups/dashboard-builder-v3.html) — orijinal builder mockup (token kaynağı)

---

## 1. Problem

M-11 F-7 alt-3a sonrası **builder tek başına BKM brand** kazandı, ama proje geri kalanı (Dashboard/Reports/Admin/Profile/Auth/Logs) hala eski `_AppLayout` topbar + max-w-7xl center main pattern. Builder modern split-pane görünüyor, üç tık ötedeki rapor liste sayfası 2018 vibe veriyor — **görsel ada / kırılma**.

Kullanıcı verbatim:
> "tüm tasarımı bu builder ile uyumlu hale getirmek gerekiyor bence"
> "mockup renkleri değil tasarım bütünlüğü de uygulanmalı"

Plan 02'de A patikası onaylandı: Plan 02 önce → Plan 03 sonra. M-11 F-7 alt-1+2+3a tamam (3 commit, brand canlıda). Şimdi sıra Plan 03'te.

İki onaylı mockup hazır — pattern referansı net. Plan 03 = bu shell'i `_AppLayout.cshtml`'e port et + 13 view'i yeni layout'a taşı.

## 2. Scope

### Kapsam dahili

#### A. Layout shell (`_AppLayout.cshtml` rewrite)
- Sol sidebar (240px, Ctrl+B ile 60px collapse)
- Sticky topbar (56px) — breadcrumb + search + actions slot
- Main scroll area (`@RenderBody()`)
- Mobile responsive (<1024px sidebar overlay drawer)

#### B. Sidebar nav (server-side render)
- 3 section: Ana / Sistem / Yönetim (rol bazlı `if isAdmin` ile filtreleme)
- Aktif state: `ViewContext.RouteData.Values["controller"/"action"]` ile karşılaştırma
- Badge sayıları: aktif rapor sayısı, dashboard sayısı, favori sayısı (ViewModel'den)
- Mobile collapse + collapse state localStorage

#### C. Sayfa-by-sayfa migration (13 view)
Her view yeni shell altında:
1. **`Dashboard/Index`** — "Genel Bakış" — mockup app-shell-v1 birebir port (KPI grid + grafik + favoriler + aktivite)
2. **`Reports/Index`** — Rapor listesi — qcard grid pattern + filtre/arama bar (mockup'tan favoriler bölümü kalıbı)
3. **`Reports/Run`** — Rapor çalıştır — param form üstte + sonuç (tablo veya dashboard iframe) altta
4. **`Admin/Index`** — Admin panel — sub-nav (Raporlar/Kullanıcılar/Roller/Kategoriler/DataSources) + içerik
5. **`Admin/EditReport`** — Builder — mockup app-shell-builder-v1 port (M-11 F-7'den uyum)
6. **`Admin/CreateReport`** — Builder — aynı pattern
7. **`Admin/EditUser`** + `Admin/CreateUser` — Form pattern (.field/.lab/.inp)
8. **`Admin/EditRole`** + `Admin/EditCategory` — Form pattern
9. **`Admin/DataSources`** — Liste + form pattern
10. **`Profile/Index`** — Form pattern
11. **`Logs/Index`** — Tablo pattern (mockup'taki .activity .dt birebir)
12. **`Auth/Login`** — **Sidebar'sız** varyant (`Layout = "_AuthLayout"` ya da `_AppLayout`'ta `BodyClass="login"` ile sidebar gizle)

#### D. Component pattern adaptation
- Tüm form'lar: `.field/.lab/.inp/.seg-full` (eski Tailwind utility karışımından temizle)
- Tüm butonlar: `.btn/.btn.primary/.btn.ghost`
- Tüm tablolar: `.dt` pattern (sticky thead + line-2 row border + status chip)
- Kartlar: `.qcard/.kpi-card` pattern (paper bg + line border + 10px radius)
- Type chip'ler: `.type-kpi/.type-chart/.type-table/.type-dash` admin sayfalarında rapor liste gibi yerlerde

#### E. Builder topbar consolidation (M-11 F-7 sonrası)
- M-11 F-7 alt-2/3a'da Razor topbar eklendi (breadcrumb + dirty chip + Önizle/Geri Al)
- Plan 03 sonrası `_AppLayout` topbar bunları zaten içerecek (slot olarak)
- EditReport/CreateReport `<div class="builder-topbar">` BLOK SİL — yerini topbar slot doldurur
- mode segmented (Düzenle/Önizle), dirty chip, JSON, Kaydet butonları topbar slot'ta

#### F. Theme + Font policy
- Inter sans **tüm proje** (eski default sans-serif değişir — risk: Tailwind text-* class'larıyla overlap)
- JetBrains Mono **mono numeric için** (`.mono` class — tarih, sayı, ID, hash gösterimleri)
- Font CDN `_AppLayout` head'inde (her sayfada yüklenir, tek-istek cache)

#### G. Brand CSS reorganization
- `style.css` (proje wide) — token sistemi + temel layout (.btn/.field/.dt/.qcard/.kpi-card vb.)
- `dashboard-builder.css` (builder-only) — sadece builder-spesifik (.builder-shell/.palette/.canvas/.drawer/widget/.w-head/.w-body)
- Token'lar zaten alt-3a'da style.css'e eklendi, mevcut alt-3a CSS dosyalarından genel kalıpları **dashboard-builder.css'ten style.css'e taşı** (genelleştir)

### Kapsam dışı (M-12'ye veya başka plan)
- Dark mode (`color-scheme: dark` varyantı) — M-12 listede
- i18n (multi-language) — ileride
- Custom Tailwind config (PostCSS build pipeline) — şu an CDN, kalır
- Server-side notification system (kırmızı dot bildirim count'u API'den) — şimdilik static
- Cmd+K fuzzy search backend — şimdilik UI placeholder, F-10 sonrası
- Search functionality (rapor/dashboard/kullanıcı ara) — şimdilik UI, backend index gerekir

### Etkilenen dosyalar (tahmin)

**Faz A — Shell skeleton (~5 dosya):**
- `Views/Shared/_AppLayout.cshtml` (rewrite, ~250 → ~180 satır)
- `Views/Shared/_AuthLayout.cshtml` (yeni, ~40 satır — sidebar'sız Login için)
- `wwwroot/assets/css/style.css` (token + base pattern genişletme, +200 satır)
- `wwwroot/assets/css/app-shell.css` (yeni, ~300 satır — sidebar/topbar/nav/sayfalama component'ler)
- `wwwroot/assets/js/app-shell.js` (yeni, ~80 satır — Ctrl+B collapse + active nav + mobile drawer)

**Faz B — Sayfa migration (~13 dosya):**
- 12 admin/reports/dashboard/profile/logs view'i — her birinde topbar slot + content yeniden düzenleme

**Faz C — Form/table pattern (~10 dosya overlap):**
- Faz B sayfalarında `.field/.lab/.inp/.dt` adaptation ile aynı dosyalar

**Faz D — Builder topbar consolidation (~3 dosya):**
- `EditReport.cshtml` + `CreateReport.cshtml` — builder-topbar BLOK SİL, topbar slot kullan
- `dashboard-builder.css` — builder-topbar stiller temizle (yeni `_AppLayout` topbar'a devir)
- `wwwroot/assets/js/dashboard-builder/builder-core.js` — topbar slot'a Önizle/Geri Al/Kaydet bind (Razor inline yerine)

**Faz E — Auth (~2 dosya):**
- `Views/Auth/Login.cshtml` — `Layout = "_AuthLayout"` veya body class ayarı
- `_AuthLayout.cshtml` (yeni)

**Faz F — Test + journal (~3 dosya):**
- `tests/ReportPanel.Tests/Views/AppShellLayoutTests.cs` (yeni, smoke layout test)
- `docs/screenshots/m13-shell-{home,reports,builder,login}.png` (4 screenshot)
- `docs/journal/<tarih>.md` (handoff entry)

**Tahmini boyut toplam:** 30-35 dosya, ~3500-4500 satır net (eski layout/view kalıpları silinecek, yenisi gelir).

## 3. Alternatifler

### A: Mockup'ları aynen kabul, _AppLayout direkt rewrite
**Açıklama:** Tek seferde `_AppLayout.cshtml`'i sidebar shell ile değiştir, sonra her view'i yeni shell altına oynat.
**Reddetme sebebi:** "Big bang" — 13 view aynı anda kırılır, hata bulunca rollback büyük commit. Geçiş sırasında sayfalar bozuk durumda. Smoke imkansız.

### B: Eski + yeni layout paralel (BodyClass switch)
**Açıklama:** `_AppLayout` korur, yeni `_ShellLayout` ekler. Sayfa-by-sayfa geçirilir. Tamamlanan sayfalar `_ShellLayout`, eskiler `_AppLayout`.
**Reddetme sebebi:** İki layout paralel = iki nav sistemi = iki user header = code duplication + UX kafa karışıklığı (kullanıcı bir sayfada sidebar, diğerinde topbar görür). Geçici çözüm uzun yaşayan teknik borç olur.

### C: SEÇİLEN — Aşamalı migration tek layout
**Açıklama:** `_AppLayout.cshtml`'i yeniden yaz (sidebar shell), tüm view'ler aynı layout altında. Faz B sayfa-by-sayfa **content adaptation** (yeni shell zaten render ediyor, her view sadece kendi içeriğini pattern'e uyduruyor).
**Sebep:** Tek source of truth, tek pass. Faz A bitince tüm sayfalar yeni shell altında — bazıları ham içerikli (eski form/tablo), Faz B'de incele incele kalıba sokulur. Smoke her aşamada mümkün (yeni shell + eski içerik = çalışır + çirkin; iyi tarafa migration ile düzelir).

### D: Mockup'ları zayıflat (sidebar opsiyonel)
**Açıklama:** Mevcut topbar + max-w-7xl korunsun, sidebar sadece admin sayfalarında.
**Reddetme sebebi:** Kullanıcı isteği "tasarım bütünlüğü" — kısmi bütünlük bütünlük değildir. Mockup onayı bu yaklaşımı dışlıyor.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| `_AppLayout` rewrite tüm sayfaları kırar | Çok yüksek | Yüksek | Faz A öncesi 2 ek pre-step: (1) `_AppLayout` backup branch (`backup/pre-shell-2026-04-27`), (2) **statik HTML smoke** — yeni layout sadece `<h1>` body ile test, sonra sayfa-content gelir |
| Tailwind utility class'larıyla `.field/.lab/.inp` çakışma | Orta | Yüksek | Custom class'lar `!important` değil, **scope edici** kullan (`.builder-shell .inp` veya body-class scope). View'larda eski Tailwind class'ları temizlenirken tek tek değiştir |
| Builder topbar consolidation `dashboard-builder.css`'i bozar | Orta | Orta | Faz D'yi en sona koy. Faz A-C bittikten sonra builder hala çalışıyor olmalı (Razor topbar duplicate ama fonksiyonel). Faz D temizlik passes |
| Reports/Run iframe pattern shell ile çakışır | Orta | Orta | Reports/Run sayfası `BodyClass="report-fullwidth"` veya custom layout slot — sidebar daralmış, iframe kalan alanı doldurur |
| Inter font tüm projede yüklenince Tailwind text-* boyutları kayar | Düşük | Orta | Inter ve default font baseline benzer (her ikisi de sans-serif, ortalama metrics yakın). Smoke ile font kayma kontrol — gerekirse `body { font-family: Inter; }` yerine `.app-shell { font-family: Inter; }` scope'la |
| Mobile responsive (<1024px) sidebar overlay JS kompleks | Orta | Orta | Faz A'da mobile pattern minimum (sidebar `position: fixed` + backdrop). Detay polish Faz F sonu veya M-13 alt-iş |
| Login sayfası `_AuthLayout` ayrı dosya gerek | Düşük | Yüksek | Faz E ayrı, yeni `_AuthLayout.cshtml` 40 satır, mockup brand kart-pattern (paper card ortada + brand-mark + form) |
| Auth/Login redirect sonrası "tüm sayfalar bozuk" panik | Düşük (dev only) | Orta | Branch izole çalışılır, main'e merge öncesi 4 saat smoke. Yarı yolda commit dökümanı: "uncommitted çalışmayı bitir, merge öncesi" notu |
| Solo-dev efor tahmin altı (~13-16h) → 25h+ olabilir | Orta | Orta | Faz B'de "P2 nice-to-have" sayfalar var (Logs, DataSources): mocup uygun ama tam pattern adaptation ileride yapılabilir. Acil pattern: Dashboard/Index + Reports/Index + Admin/EditReport (P0). Kalan 9 sayfa P1 |
| Plan 02 alt-3b (Gridstack canvas) Plan 03 ile çakışır | Orta | Orta | Plan 03 başlamadan önce Plan 02 alt-3b ya bitir ya ertele. Tercih: alt-3b'yi Plan 03 Faz D içinde "builder topbar consolidation + canvas Gridstack" birleşik yap (tek pass) |

## 5. Done Criteria

### Faz A (Shell skeleton)
- [ ] `_AppLayout.cshtml` yeni shell — sidebar + topbar + main slot, mockup app-shell-v1 birebir
- [ ] `app-shell.css` + `app-shell.js` yüklü, Ctrl+B collapse çalışır
- [ ] Mevcut `Dashboard/Index` yeni shell altında render ediyor (eski içerik korundu, layout yeni)
- [ ] Brand kırmızı (#dc2626) tüm sayfalarda aktif (sidebar active, button primary, type-chip)
- [ ] 0 console error, 0 layout warning

### Faz B (Sayfa migration P0)
- [ ] `Dashboard/Index` mockup app-shell-v1 birebir (KPI grid + chart + favoriler + aktivite)
- [ ] `Reports/Index` qcard grid pattern + filtre bar
- [ ] `Admin/EditReport` (Builder) mockup app-shell-builder-v1 birebir (mode segmented + drawer Veri/Görünüm)

### Faz B (Sayfa migration P1)
- [ ] `Reports/Run` param form + sonuç bölümü
- [ ] `Admin/Index` sub-nav + içerik
- [ ] `Admin/EditUser` + `CreateUser` form pattern
- [ ] `Admin/EditRole` + `EditCategory` form pattern
- [ ] `Admin/DataSources` liste + form
- [ ] `Profile/Index` form pattern
- [ ] `Logs/Index` tablo pattern (.dt + status chip)

### Faz C (Component adaptation)
- [ ] Tüm form'larda `.field/.lab/.inp/.seg-full` (Tailwind utility temizlenmiş)
- [ ] Tüm butonlar `.btn/.btn.primary/.btn.ghost`
- [ ] Tüm tablolar `.dt` pattern
- [ ] Kart'lar `.qcard/.kpi-card` pattern

### Faz D (Builder topbar consolidation)
- [ ] EditReport/CreateReport `<div class="builder-topbar">` blok SİLİNDİ
- [ ] Topbar slot mode segmented + dirty chip + actions doldu
- [ ] dashboard-builder.css'te topbar stiller TEMİZ (devir yapıldı)
- [ ] Plan 02 alt-3b Gridstack canvas birleştirildi (eğer paralel)

### Faz E (Auth)
- [ ] `Auth/Login` `_AuthLayout` veya `BodyClass="login"` ile sidebar'sız
- [ ] Login form mockup brand pattern (paper card + brand-mark + .field/.inp)

### Faz F (Test + journal)
- [ ] 122/122 mevcut test yeşil + 3-5 yeni layout smoke test
- [ ] 4 screenshot (`docs/screenshots/m13-shell-*.png`)
- [ ] Handoff journal entry
- [ ] Bütün ana navigasyon yolları manuel smoke (Login → Genel Bakış → Raporlar → Detay → EditReport → Logs → Profile)

## 6. Rollback Planı

### Faz başına revert
- Her faz commit'i bağımsız revertable
- Faz A revert → eski `_AppLayout` geri gelir, tüm sayfalar eski pattern
- Faz B-C-D-E-F revert → kademeli geri dönüş

### Branch reset (acil)
- `git reset --hard <pre-Plan03-commit>` — Plan 03 öncesi snapshot
- **AÇIK ONAY GEREKİR** (destructive)

### Pre-step backup
- Plan 03 başlamadan: `git branch backup/pre-shell-2026-04-XX HEAD` — yedek branch
- Plan 03 commit'leri ile main divergence olursa: backup branch'e dön + diff incele

### Production rollback (uzak ihtimal)
- `_AppLayout` bozarsa **tüm sayfalar** etkilenir → production rollback git revert + redeploy
- Mitigation: Plan 03 PR uzun açık tut, dev DB + dev preview'da 1 hafta yaşat

## 7. Adımlar / İçerdiği TODO maddeleri

### Pre-step (~30dk)
1. [ ] Backup branch oluştur: `git branch backup/pre-shell-2026-04-XX`
2. [ ] Plan 02 alt-3b durumu netleştir: ya bu plan'a entegre et, ya bekle, ya hemen bitir
3. [ ] Mevcut `_AppLayout.cshtml` 250 satır incele — özel davranış var mı (`@RenderSection`, body class logic, role-based hide)
4. [ ] ADR-011 yaz: "Sidebar shell layout + project-wide brand harmonization" — alternatifler/sebep/sonuç
5. [ ] Branch karar: `feature/m-11-dashboard-builder-redesign` üstüne mi (M-13 alt-fazı) yoksa yeni `feature/m-13-app-shell-redesign` mi

### Faz A — Shell skeleton (~3h, 1-2 commit)
6. [ ] `wwwroot/assets/css/app-shell.css` yeni — mockup-v1 sidebar/topbar/nav/avatar stiller (~300 sat)
7. [ ] `wwwroot/assets/js/app-shell.js` yeni — collapse toggle (Ctrl+B) + active nav + localStorage state (~80 sat)
8. [ ] `style.css` token+base genişletme — qcard/kpi-card/dt/btn/field genel pattern (~+200 sat)
9. [ ] `_AppLayout.cshtml` rewrite — sidebar + topbar + main slot
10. [ ] Eski `_AppLayout` backup taşı (`_AppLayout.legacy.cshtml.bak` veya git history yeterli, dosya sil)
11. [ ] Smoke: Dashboard/Index açılır mı, sidebar görünür mü, console error yok mu
12. [ ] Commit: `feat(m-13 a): app-shell layout skeleton + sidebar nav (plan: 03)`

### Faz B P0 — Kritik 3 sayfa (~3h, 1 commit)
13. [ ] `Dashboard/Index` mockup app-shell-v1 birebir port — KPI grid + chart + favoriler + aktivite (mock data → ViewModel'e bağla)
14. [ ] `Reports/Index` qcard grid + filtre bar — mevcut tablo görünümünü kart grid'ine dönüştür
15. [ ] `Admin/EditReport` builder topbar slot test (henüz consolidation yok, çift bar olabilir — Faz D'de temizlenir)
16. [ ] Smoke 3 sayfa
17. [ ] Commit: `feat(m-13 b-p0): Dashboard + Reports + EditReport mockup pattern (plan: 03)`

### Faz B P1 — 9 sayfa (~5h, 3 commit)
18. [ ] **B.1 admin form'ları**: Admin/EditUser/CreateUser/EditRole/EditCategory — `.field/.lab/.inp/.seg-full` pattern
19. [ ] Commit: `feat(m-13 b1): admin form pattern uyumu (plan: 03)`
20. [ ] **B.2 admin liste'leri**: Admin/Index/DataSources — sub-nav + qcard veya .dt pattern
21. [ ] Commit: `feat(m-13 b2): admin liste pattern uyumu (plan: 03)`
22. [ ] **B.3 user-facing**: Reports/Run + Profile/Index + Logs/Index — param form + result block + tablo pattern
23. [ ] Commit: `feat(m-13 b3): user sayfalari pattern uyumu (plan: 03)`

### Faz C — Component pattern (~2h, 1 commit)
24. [ ] Tüm view'larda Tailwind utility class'larını `.field/.lab/.inp` ile değiştir (script veya manuel grep+replace)
25. [ ] Tablo'ları `.dt` pattern'e geçir
26. [ ] Buton sınıfları `btn-brand` → `.btn.primary` map (eski class'lar kalsın `style.css`'te alias olarak)
27. [ ] Smoke: tüm sayfalarda görsel tutarlılık
28. [ ] Commit: `refactor(m-13 c): component pattern adaptation (plan: 03)`

### Faz D — Builder topbar consolidation (~2h, 1-2 commit)
29. [ ] EditReport/CreateReport `<div class="builder-topbar">` BLOK SİL
30. [ ] `_AppLayout` topbar slot'a builder-spesifik içerik (mode segmented + dirty chip + Önizle/Geri Al/JSON/Kaydet) **section** Razor pattern ile inject
31. [ ] `dashboard-builder.css`'te topbar stilleri SİL (style.css/app-shell.css'e devir)
32. [ ] **Opsiyonel**: Plan 02 alt-3b Gridstack canvas implement (canvas widget render)
33. [ ] Smoke: builder tek-bar görünür mu, mode/dirty/butonlar çalışır mı
34. [ ] Commit: `feat(m-13 d): builder topbar consolidation + alt-3b gridstack (plan: 03)`

### Faz E — Auth (~30dk, 1 commit)
35. [ ] `_AuthLayout.cshtml` yeni — sidebar'sız, brand-mark logo + form ortada paper card
36. [ ] `Auth/Login` `Layout = "_AuthLayout"` set
37. [ ] Login form .field/.inp pattern + brand-mark başlık
38. [ ] Smoke: logout + login round-trip
39. [ ] Commit: `feat(m-13 e): auth shell ayrim (plan: 03)`

### Faz F — Test + journal (~1h, 1 commit)
40. [ ] `AppShellLayoutTests.cs` yeni: 3-5 smoke test (sidebar render, active nav, breadcrumb)
41. [ ] 4 screenshot (`docs/screenshots/m13-shell-*.png`)
42. [ ] Handoff journal entry — Plan 03 toplam commit özeti
43. [ ] Plan 03'ü arşive taşı: `git mv plans/03-*.md plans/archive/`
44. [ ] PR check: 130+ test yeşil, 0 build error/warning, manuel UI tüm yollar
45. [ ] Commit: `test(m-13 f): smoke + screenshots + handoff (plan: 03)`

## 8. İlişkili

- ADR: ADR-011 yazılacak (pre-step #4) — sidebar shell mimari kararı
- Önceki plan: [Plan 02](02-m11-dashboard-builder-finalize.md) (M-11 builder finalize)
- Mockup'lar:
  - [app-shell-v1.html](../ReportPanel/wwwroot/mockups/app-shell-v1.html)
  - [app-shell-builder-v1.html](../ReportPanel/wwwroot/mockups/app-shell-builder-v1.html)
  - [dashboard-builder-v3.html](../ReportPanel/wwwroot/mockups/dashboard-builder-v3.html)
- TODO ID'leri: yeni — F-13.A..F-13.F (Faz başına)
- Konuşma referans: `docs/journal/2026-04-27.md`

## 9. Onay

> Kullanıcı onay verene kadar implement edilmez.

- [x] Mockup app-shell-v1 onaylandı (2026-04-27, "evet onaylıyorum mockuplar tamamdır")
- [x] Mockup app-shell-builder-v1 onaylandı (2026-04-27)
- [ ] Plan 03 detayı kullanıcıya gösterildi — bekliyor
- [ ] Faz/branch/commit stratejisi onayı — bekliyor
- [ ] Son onay: <tarih, kullanıcı imzası>

---

## 10. Karar Noktaları (kullanıcıdan input gerek)

Plan implement öncesi şu 5 nokta netleşmeli:

1. **Branch:** Mevcut `feature/m-11-dashboard-builder-redesign` üstüne mi (M-13 alt-fazı, M-11 PR'a dahil) yoksa yeni `feature/m-13-app-shell-redesign` (ayrı PR)?
   - **Önerim:** Yeni branch — Plan 03 büyük scope, M-11'le karışmamalı. M-11 alt-3b Plan 03 Faz D'ye taşınır.

2. **Plan 02 alt-3b (Gridstack canvas):** Plan 03'ten önce bitsin mi yoksa Plan 03 Faz D ile birleşsin mi?
   - **Önerim:** Birleşik (Faz D) — Gridstack zaten builder spesifik, topbar consolidation ile aynı pass.

3. **Inter font scope:** Tüm proje mi yoksa sadece `.app-shell` scope mu?
   - **Önerim:** Tüm proje (`<body>` font-family). Mockup tutarlılığı için. Risk: minimum (Inter ve default sans-serif yakın metrics).

4. **`_AuthLayout` mı yoksa `BodyClass="login"` mu?**
   - **Önerim:** `_AuthLayout` — daha temiz separation, login sayfası body class hack'i değil farklı layout file.

5. **Faz B P0/P1 ayrımı kabul mü?** P0 = Dashboard + Reports + EditReport (kritik 3). P1 = kalan 9 sayfa.
   - **Önerim:** Kabul. P0 bittikten sonra kullanıcı görsel kontrolü yapsın, P1 sonrasında commit.

Bu 5 nokta + plan onayı geldikten sonra Pre-step ile başlanır.
