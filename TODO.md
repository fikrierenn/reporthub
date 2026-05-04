# TODO

Bu dosya yapilanlari ve kalanlari detayli takip icin kullanilir.

## Yapilanlar (tamam)
- Kullanici tablosu ve PBKDF2 sifreleme eklendi.
- PortalHUB tablolarina uygun seed scriptler duzeltildi.
- Raporlar liste + calistirma sayfasi olarak ayrildi.
- Excel disari aktarim eklendi (.xlsx).
- Parametre olusturucu ve SP parametrelerini getirme araci eklendi.
- Parametre alanlarinda placeholder/help/default destekleri eklendi.
- Ortak navbar yapisi icin `_AppLayout.cshtml` olusturuldu.
- Reports liste ve run sayfalari layout'a alindi.
- Dashboard sayfasi layout'a alindi.
- Admin ana sayfasi layout'a alindi.
- Admin CreateReport ve EditReport sayfalari layout'a alindi.
- Admin CreateDataSource ve EditDataSource sayfalari layout'a alindi.
- Home Index ve Privacy sayfalari layout'a alindi.
- Login sayfasi layout'a alindi (navbar uyumlu).
- Tekrarlanan header/footer kaldirildi.
- EditReport form action duzeltildi.
- EditReport form tag helper sorunu icin BeginForm kullanildi.
- Test sayfasi layout'a alindi.
- Navbar ve footer sabit (sticky) hale getirildi.
- Login sayfasi layout disi yapildi (navbar kaldirildi).
- Dashboard verileri canli log ve rapor verilerinden alinmaya baslandi.
- Dashboard metinleri Turkce karakterlerle guncellendi.
- Login sayfasi ve ortak layout metinleri Turkce karakterlerle guncellendi.
- Rapor calistirma sayfasinda genis gorunum anahtari ve tablo rahatligi eklendi.
- Run sayfasinda tam ekran ve tablo odak modlari kontrol edildi.
- Sonuc tablosunda arama (client-side) eklendi.
- Login ve layoutta Giris yazimi duzeltildi.
- Tum sayfalarda Turkce karakter taramasi yapildi.
- Login sayfasi ve sticky navbar/footer uyumu kontrol edildi.
- Admin rolune kullanici yonetimi eklendi (ekle/duzenle/sil).
- Rapor rol secimi checkbox listesine cevrildi.
- Profil ekrani eklendi (ad soyad, email, sifre guncelleme).
- Rol listesi hardcode olarak AdminController icinde tutuluyor.
- Admin sayfalarinda kalan bozuk Turkce metinleri temizlendi.
- Rapor rol secimi checkbox UX iyilestirmeleri tamamlandi.
- Excel export icerigine rapor bilgileri ve bos sonuc uyari metni eklendi.
- Parametre UX iyilestirmeleri tamamlandi (required vurgusu, varsayilan tarih).
- Rapor calistirma sayfasinda parametre hatalari form ustune alindi.
- Rapor calistirma tablosu icin genis gorunum/sticky header iyilestirmesi tamamlandi.
- Run sayfasinda JS kaldirildi, gorunum ve arama server-side hale getirildi.
- Sistem loglari Admin'den ayrildi, /Logs sayfasina tasindi.
- Admin sayfasindan log sorgusu kaldirildi, navbar loglara tasindi.
- Sistem loglari icin filtreleme server-side hale getirildi (JS yok).
- ViewBag kullanimlarini kritik sayfalarda ViewModel'e tasima (tamamlandi).
- Gereksiz inline CSS/HTML temizligi (tarandi, inline style bloklari tasindi).
- Script bloklarini sayfa disina (wwwroot/js) tasima.
- AuditLog tablosu ve migration scriptleri eklendi, ReportRunLog drop edildi.
- Audit log: tek log tablosu uzerinden calisildi.
- Audit log: log kapsami (login/logout, sifre, profil, kullanici/rol, rapor, veri kaynagi, export, test) eklendi.
- Audit log: alanlar (olay tipi, hedef, eski/yeni, zaman, ip/user-agent) eklendi.
- Audit log: log ekrani sadece adminde, filtre/arama aktif.
- Audit log: log listeleme UX (combo/select, arama, sayfalama) tamamlandi.
- Audit log: log yazma mekanizmasi merkezi servis ile eklendi.
- Otomatik testler eklendi (PasswordHasher, AuditLogService).
- Login, rapor calistirma, export akisi manuel smoke test tamamlandi.
- Admin rapor ekleme ve parametre uretme akisi manuel test tamamlandi.

## Devam eden isler (aktif)

### AKTIF SIRA — 2026-05-04 SENTEZI (kolaydan zora)

Bu liste 5 plan dosyasi (`plans/02-06`) + asagidaki FAZ 1-3 + son 5 journal'in yarim kalan kismi taranarak cikarildi. Madde sirasi: efor (kolay → zor). Detay icin ilgili FAZ/plan dosyasina bak.

#### Trivia / housekeeping (~30 dk toplam, en kolay)
- [ ] **CSV İndir butonu commit** — 4 dosya uncommitted (oturum 2026-05-04, TableRenderer + DashboardClientScripts + Run.cshtml + Edit/CreateReportV2)
- [ ] **Plan 04 arsivle** — commit `108560e` ile tamamlandi: `git mv plans/04-*.md plans/archive/`
- [ ] **Plan 06.B arsivle** — Done criteria ✓ (`caf8822`+`2b62d39`): `git mv plans/06-*.md plans/archive/`
- [ ] **Plan 03 durumu kontrol** — Done criteria check, kapaliysa arsivle
- [ ] **M-13 sub-nav unchecked'lar isaretle** — commit'leri var (`7bc8cb0` `831319b` `9ba3c61` `fc55063` `4ada9e6` `4d2f5d2` `c8ce59f`), [x] yap (asagidaki Plan 03 cizelgesi satir 279-317)
- [ ] **NotebookLM re-login** — kullanici terminalde: `D:/Dev/reporthub/.venv/notebooklm/Scripts/notebooklm.exe login`

#### Küçük işler (~1h)
- [ ] **F-03 · dashboard-builder.js memory leak** (FAZ 2 madde 19) — event delegation veya AbortController
- [ ] **M-03 Faz C · User.Roles kolon drop** (FAZ 2 madde 25) — `16_DropUserRolesCsv.sql` + model field sil
- [ ] **G-07 · Dashboard iframe policy review** (FAZ 3 madde 36)
- [ ] **G-08 · DashboardRenderer JSON escape regresyon test** (FAZ 3 madde 37)
- [ ] **Hesaplı kolon autocomplete** — SP preview kolonlarindan datalist (M-11 builder UX)
- [ ] **M-02 son grep** (FAZ 1 kalan) — AuthController + servisler ex.Message leak
- [ ] **F-02 admin override UI tamamla** (FAZ 1 yarim) — SP Onizle parametre override
- [ ] **ADR-001 + ADR-002 yazimi** (FAZ 3 madde 38) — data-access + dashboard-architecture
- [ ] **ReportParamValidator Run path kök fix** — `{"fields":[...]}` parse Run yolunda (PreviewDashboardV2'de cozuldu, Run karsiligi)

#### Orta (2-4h)
- [ ] **G-09 · SP read-only login** ⚠️ CANLIYA CIKMADAN ZORUNLU (FAZ 2 madde 28.5)
- [ ] **dashboard-builder.js split** (FAZ 2 madde 27) — 567 satir
- [ ] **SP mimarisi · sp_PdksPano → inline TVF refactor** (FAZ 2 madde 21) — ADR-004 adayi
- [ ] **M-10 Faz 4-6 · Named Result Contract** (FAZ 2 madde 29)
- [ ] **DateTime Faz D · DB DEFAULT GETDATE → GETUTCDATE** (FAZ 2 madde 28)
- [ ] **M-08 Async tutarlilik** (FAZ 3 madde 31)
- [ ] **M-09 AsNoTracking sweep** (FAZ 3 madde 32)
- [ ] **M-07 ViewModel BindNever + DTO** (FAZ 3 madde 30)
- [ ] **Dashboard P0 · Config deserialize try/catch** — `ReportsController.cs:240`
- [ ] **Dashboard P1 · Result set index validation** server-side
- [ ] **Dashboard P1 · Mobile responsive grid** — `grid-cols-1 sm:grid-cols-2 lg:grid-cols-4`
- [ ] **Dashboard P1 · Tailwind/Chart.js local serve** (production)
- [ ] **Dashboard P1 · Inline RS boyut limiti / lazy-load** (10K satir)
- [ ] **F-05 · Türkçe UTF-8 normalize** (FAZ 3 madde 33) — turkish-ui-normalizer skill
- [ ] **CreateUser veri filtresi bölümü (P0)** — EditUser'da var, Create'te yok

#### Büyük (>4h, çok-fazlı)
- [ ] **M-11 Plan 02 · F-9 live preview endpoint** (~5h)
- [ ] **M-11 Plan 02 · F-10 sablon + kbd shortcuts** (~3h)
- [ ] **M-11 Plan 02 · F-11 smart defaults** (~3h)
- [ ] **M-11 Plan 02 · F-12 e2e + screenshot + journal** (~3h)
- [ ] **Plan 05 · AST formula parser** (~6h+) — kendi recursive descent parser
- [ ] **DateTime Faz E · Veri shift + SP/seed hizalama** (yarim gun, FAZ 2 madde 28)
- [ ] **User P1 · Phone/Dept/Position alanlari** (FAZ 2 madde 24)
- [ ] **User P1 · Admin liste arama+filtre+son giris** (FAZ 2 madde 23)
- [ ] **ReportCatalog.AllowedRoles CSV deprecate** (FAZ 2 madde 26)
- [ ] **F-06 · CSP politikasi** (FAZ 3 madde 34)
- [ ] **M-06 · EF Core Migrations gecisi** (FAZ 3 madde 29)
- [ ] **Test coverage %30 hedefi** (FAZ 3 madde 35)

#### User yönetimi P2 (operasyonel iyileştirme — TODO satır 525-536)
- [ ] Hesap kilitleme (5 basarisiz → 15dk)
- [ ] Sifre karmasikligi kurallari
- [ ] Zorla sifre degistirme flag
- [ ] Admin sifre sifirlama (token ile)
- [ ] Toplu CSV import (ClosedXML)
- [ ] AD/LDAP senkronizasyon
- [ ] Kullanici kopyalama
- [ ] Avatar / tercihler / aktivite ozeti (P3)

#### Stratejik / belirsiz vade (TARTIŞMA gerekli)
- [ ] Plan 04 (potansiyel) · Alpine.js + htmx adoption (FAZ 3 madde 40)
- [ ] Plan 05 (potansiyel) · Scheduled Reports + Email Hangfire (FAZ 3 madde 41)
- [ ] Yeni proje adi brainstorm (TODO satir 207)
- [ ] vNext · Sirket ici portal architecture (TODO satir 215)
- [ ] Yetki revizyonu — granular roller (TODO satir 193, TODO madde 39)

---

### BIRLESIK ONCELIK SIRASI (22 Nisan 2026 sabah)
Bu liste asagidakilerin sentezidir:
- `docs/CONTEXT_MANAGEMENT.md` — bagalm yonetimi anayasasi (bugunku arastirma)
- `C:\Users\fikri.eren\.claude\plans\scheduled-task-name-code-reveiw-file-c-twinkling-graham.md` — kod review raporu (26 bulgu: 1 kritik + 7 yuksek + 10 orta + 8 dusuk)
- Onceki `DASHBOARD OZELLIGI`, `DETAYLI KULLANICI YONETIMI`, `MIMARI TUTARSIZLIKLAR` bolumleri
- Dun geceki `SP MIMARISI TARTISMASI`

---

#### FAZ 0 — ✅ TAMAMLANDI (22 Nisan 2026)
1. ✅ **G-01 · Hardcoded SA sifresi** — commit `8de22fd` (7 dosya: appsettings.json, Staging.json, TestController, AdminController.GetTemplateConnectionString, CreateDataSource.cshtml, deploy-staging.ps1, CLAUDE_TOOLING_PROPOSAL.md). Development.json gitignored, SA sifresi lokalde kaliyor.
2. ✅ **Baglam yonetimi** — commit `e59e3a9` (CLAUDE.md yeniden duzenlendi §0 oturum basi rituel eklendi, `.claude/rules/` 10 dosya, `.claude/hooks/` 3 script, `.claude/agents/commit-splitter.md`, `.claude/skills/session-handoff/`, `docs/CONTEXT_MANAGEMENT.md`, `docs/CLAUDE_TOOLING_PROPOSAL.md`).
3. ✅ **F-01 · SP Onizle click handler** — commit `07f4b91` (admin-report-form.js initSpHelpers outer IIFE'den cikarildi, top-level IIFE oldu). Browser'da dogrulandi.
4. ✅ **32-dosyalik backlog commit-split** — 16 commit (`64259ed`..`7a7b81d`) ile bolundu. `commit-splitter` subagent yazildi.
5. ✅ **Deprecated artifacts** — `7a7b81d` (Views/Auth/AGENT.md silindi, bos dizinler temizlendi).
6. ✅ **Pre-commit/Post-commit hook'lar** — commit `59888db` ve onceki (antipattern scan + journal auto-update).

#### FAZ 1 — İLERLEYIS (bu hafta)
✅ **M-02 kismi · Exception handling sanitize** — commit `b6ff43a` (AdminController 7 ex.Message leak) + `a047957` (ReportsController, TestController, AdminController SpList/SpPreview). Kalan: AuthController veya baska servisler varsa grep tara.
✅ **G-02 · Open redirect fix** — commit `4c40f61` (AuthController.cs:285, ReportsController.cs:991 — `Url.IsLocalUrl` + `StartsWith("/")` + `!StartsWith("//")` + `!StartsWith("/\\")`).
✅ **G-03 · UserDataFilter whitelist + regex** — commit `4c40f61` (ReportsController.cs:18-23 static regex fields, `InjectUserDataFilters` whitelist check, reject audit log `user_filter_rejected`).
✅ **F-02 kismi · SP Onizle default parametre destegi** — commit `b6ff43a` (AdminController.SpPreview DATA_TYPE switch: date→today, int→0, string→'', bool→false, vb.). Browser dogrulandi: "7 result set dondu". Admin override UI hala yapilmadi (yarim).
✅ **M-03 Faz A · User.Roles CSV deprecate (kod-duzeyi)** — commit `2d0c3fd` + `docs/ADR/003-role-model.md`. Auth/Profile/Reports/Admin de-sync noktalari temizlendi. Faz B (nullable kolon) + Faz C (drop) ileride.
✅ **session-handoff skill auto-commit** — commit `5df75ff` (skill artik docs/journal dosyasini tek-path commit ediyor, commit-discipline.md istisna eklendi).

#### FAZ 1 — ✅ KAPANDI (22 Nisan 2026)
9. ✅ **M-04 · DashboardRenderer + UserDataFilter + UserRole sync unit tests** — DashboardRenderer XSS (9 test) + UserDataFilterValidator (17 test) commit `6c70b1e`. UserRoleSyncService + 5 idempotency test commit `b714916`. Tum suite: 66/66 yesil.
10. ✅ **dashboard-builder.js: spPreviewReady event listener + kolon datalist** — commit `b3ae747`. `document.addEventListener('spPreviewReady')`, populateColumnDatalist, attachListAttribute. Browser dogrulandi: 7 result set -> 51 distinct kolon.
11. ✅ **SP Onizle admin-override panel** — F-02 tamamlandi, commit `816c8c2`. ProcParams short-name destegi, SpPreview paramsJson override, typed inputs (date/number/text/checkbox).
12. ✅ **M-02 devam · ex.Message sanitize** — commit `a047957` + `b6ff43a`. Oturum 4'te yeniden tarama: AuthController/Reports/Profile/DataSourceService'de user-facing leak YOK. `async void`, `new HttpClient()` yok. Kalan `.Message` kullanimlari audit log alanlarinda (ErrorMessage, LogRun param) — amacli.
13. ✅ **M-03 Faz B · User.Roles nullable + [Obsolete]** — commit `bf922ae`. `Database/15_NullableUserRolesCsv.sql` (idempotent, UserRole orphan check + ALTER NULL); [User.cs:31](ReportPanel/Models/User.cs:31) `string?` + `[Obsolete]`; [ReportPanelContext.cs:94-97](ReportPanel/Models/ReportPanelContext.cs:94) pragma; [UserManagementService.cs:54](ReportPanel/Services/UserManagementService.cs:54) yeni create'te Roles yazilmaz. Faz C ADR-003'e gore "cok sonra" — Faz 2 madde 25'te bekliyor.

#### FAZ 2 — BU AY (4 hafta, orta oncelik)
14. ✅ **M-01 · AdminController service extraction** — 5 adimda tamamlandi:
    - step 1 CategoryManagementService — commit `f0e6412`
    - step 2 RoleManagementService — commit `d2228df`
    - step 3 DataSourceManagementService — commit `175f743`
    - step 4 ReportManagementService — commit `44d8a5f`
    - step 5 UserManagementService — commit `212de63`
    AdminController 1736 satirdan service'lere bolundu. UserRoleSyncService de ayri (madde 9).
15. ✅ **G-04 · Audit log genisletme** — 10 CRUD audit eklendi, commit `effa7b5`. `AuditCrudAsync` private helper.
16. ✅ **G-05 · Cookie HttpOnly/Secure/SameSite/ExpireTimeSpan** — commit `fdc97ca`. Program.cs AddCookie.
17. ✅ **G-06 · TestController [Authorize(Roles="admin")] + [ValidateAntiForgeryToken]** — commit `fdc97ca` (G-05 ile birlesik).
18. ✅ **M-05 · DashboardHtml legacy retirement** — 3 faz tamamlandi: Faz A (CSV kaldir ~ ADR-005), Faz B (`a2feb5d`) legacy retirement + audit event, Faz C (`0f73478`) DB DROP COLUMN + model sil. Migration `17_DropDashboardHtml.sql`, ADR-005.
19. **F-03 · dashboard-builder.js memory leak** (1h) — event delegation veya AbortController. Drag-drop listener re-attach sorunu. **Siradaki is — 23 Nisan ilk.**
20. ✅ **F-04 · AGENT.md yaniltici icerik** — commit `7a7b81d` (silindi).
21. **SP mimarisi · sp_PdksPano → inline TVF refactor** (3h) — `fn_PdksDetay`, `fn_PdksKpiOzet`, `fn_PdksDepartmanKirilim` + orkestrator SP. ADR-004.
22. **Dashboard canli onizleme iframe** (4h) — builder'da gercek render preview.
23. **User P1 · Admin listesi arama + filtre + son giris** (1 gun) — admin user tab'a arama kutusu, rol/aktif/AD filtresi, LastLoginAt gosterimi.
24. **User P1 · User modeline Phone/Department/Position** (4h) — migration 16 + form alanlari.
25. **M-03 Faz C · User.Roles kolon drop** (30dk-1h) — `16_DropUserRolesCsv.sql` + model field sil. ADR-003 "cok sonra" — veri validation + Faz B'nin DB'de yayginlasmasi sonra.
26. **ReportCatalog.AllowedRoles CSV deprecate** (1 gun) — ADR-004 adayi. ReportAllowedRole junction birincil, CSV kaldir.
27. **dashboard-builder.js split** (2h) — dosya 567 satir (500 kirmizi cizgi). Mantikli split: `dashboard-builder-core.js` (state + render + events) + `dashboard-builder-forms.js` (component forms + validators).
28. ✅ **DateTime.Now → DateTime.UtcNow sweep (app kodu / ADR-006 Faz C)** — tum 19 usage UtcNow'a cevrildi. ADR-006 yazildi. Takip eden iki ayri is:
    - **Faz D · DB DEFAULT `GETDATE()` → `GETUTCDATE()`** (2-3h) — 14+ DEFAULT constraint (02_CreateTables.sql + Migrations/*). ALTER TABLE DROP CONSTRAINT + ADD CONSTRAINT migration + backup gerekli.
    - **Faz E · Veri shift + SP/seed hizalama** (yarim gun) — eski "naive-local" satirlari UtcNow ile hizala (tum tarih kolonlarinda `-180 dk`). SP'lerde `GETDATE()` → `GETUTCDATE()` audit. 03_SeedData.sql guncelleme. Backup/rollback plani sart.
28.5. **G-09 · SP execution read-only login** (CANLI ÖNCESİ ZORUNLU, 2h) — Rapor çalıştırma SP'leri geniş yetkili connection string ile çalışıyor. SP içinde UPDATE/INSERT/DELETE varsa kullanıcı farkında olmadan DML tetikleyebilir. **Çözüm:** Rapor execution için ayrı read-only SQL login (db_datareader + EXECUTE). DataSource modeline `ReadOnlyConnString` ekle veya mevcut ConnString'i read-only login ile değiştir. Canlıya almadan önce mutlaka yapılmalı (kullanıcı kararı 2026-05-01).

29. **M-10 · ADR-007 Named Result Contract** (6 faz, ~1.5 gun toplam) — dashboard widget'larda index bagimliligini kaldir. `resultSet: N` → `result: "chartData"` name-based binding. Scope daraltildi (frontend rewrite / event bus / metadata-first SP REDDEDILDI). Kural: **declare now, enforce later** — shape field schema'da, enforcement Faz 4. Naming: camelCase. Fazlar:
    - **Faz 1** (~2h) · ADR-007 doc + `DashboardConfig.ResultContract` dictionary + `DashboardComponent.Result` field + `DashboardRenderer` resolver (precedence: `Result > ResultSet`, legacy fallback).
    - **Faz 2** (~3h) · `dashboard-builder.js` + admin form UI name-based binding. Widget editor'da result dropdown (isim listesi).
    - ✅ **Faz 3** · Admin save validation — hard: name unique, resultSet index valid, widget.result resolve. Soft: required-ama-kullanilmayan uyari. 10 hard + 3 soft rule, admin-dostu TR mesajlar. `DashboardConfigValidator` + 19 unit test. 96/96 yesil.
    - **Faz 4** (~1h) · Runtime **soft-fail** (direkt throw DEGIL): required result eksik/bos → kullaniciya "Veri bulunamadi" + `dashboard_required_result_missing` audit event.
    - **Faz 5** (~2h) · Migration 18 — PDKS (7 RS) + Satis (7 RS) ConfigJson rewrite. **Idempotent**: `resultContract` yoksa uret, varsa atla. `Explore` agent ile her resultSet icin camelCase isim onerisi.
    - **Faz 6** (~1h) · Legacy `resultSet: N` binding deprecate + renderer fallback kaldir (ayri PR, tum configler migrate edildikten sonra).

29.5. **M-11 · Dashboard Builder UX Redesign + Chart Expansion** (13 faz, ~60h toplam / 1.5-2 hafta solo) — Apache Superset'ten esinlenen modern admin builder. Plan dosyasi: `C:/Users/fikri.eren/.claude/plans/imdi-planlama-yap-bu-optimized-hippo.md`. Mockup: `ReportPanel/wwwroot/mockups/dashboard-builder-v3.html` (Gridstack + Chart.js canli, BKM renkleri tasarim referansi — implementasyon `_AppLayout` + `style.css` custom class'lari ile ayri port edilir). ADR-008 + ADR-009 kabul edildi (24 Nisan). Branch: `feature/m-11-dashboard-builder-redesign`.

    **Guncellemeler (24 Nisan, ADR-008/009 sonrasi):**
    - **Gridstack.js kabul** — builder-only CDN, runtime (Reports/Run) CSS-grid inline-style. Onceki "Gridstack reddet" karari tersine cevrildi.
    - **preview = Reports/Run tek renderer** — builder onizleme iframe `/Reports/Run/{id}?preview=1&configOverride=<draft>` ile. Admin-only override. ADR-008 karar A patikasi.
    - **ReportType kolonu SIL** — tum raporlar dashboard. F-1.5 alt-commit 3 ile migration 19. Run.cshtml tamamen yeniden yazilir (tablo render path'i drop). ADR-009.
    - **Font Inter + JetBrains Mono** builder-only, `_AppLayout`'a eklenmez.
    - **Calculated fields** (turetilmis alanlar) — AST-based sandbox parser, `eval()` yasak (F-8).
    - **Tasarim uyum kurali** — mockup birebir kopyalanmayacak; proje `.btn-brand` / `.card-brand` / `.form-input-brand` + max-w-7xl + `_AppLayout` header/footer icinde.

    **Superset'ten alinan 10 pattern:** Veri|Gorunum drawer tab'i · kategorili chart gallery (arama + chip filter) · Native Filter Bar (param chip'leri) · 3-tab inspect preview (Cikti|Sorgu|JSON) · widget hover menu · loading/empty/error state · kisayol modali (?) · sablon market · dark mode (M-12'ye) · duplicate action.

    **v2 tasarimdan alinan gorsel kit:** topbar kbd hints · collapsible contract bar · component-card · type badge · tab-strip · swatch/icon grid · span toggle · suggest-pill · section-hd · preview panel animasyonu.

    **Widget matrisi:** KPI 4 varyant (basic/delta/sparkline/progress), Chart 10 tip (line/area/bar/hbar/stacked/pie/doughnut/radar/polar/scatter), Table kosullu format (veri bari/renk skalasi/ikon/negatif kirmizi) + ayarlar (satir detay/toplam/cizgili/sticky/arama/sayfalama). Heatmap + Gauge M-12 disabled.

    **YENI unsurlar** (plan disi, 23 Nisan eklendi):
    - **Turetilmis Alanlar** (formul → yeni kolon) — admin `DeltaCiro = BugunCiro - GecenYilBugun` gibi client-side hesaplanan alan tanimlar, suggest pill'lere `fx DeltaCiro` olarak girer. Fonksiyonlar: `+ - * /`, SUM, AVG, ROUND, IF, COALESCE, CONCAT. Hatali formul inline error banner.
    - **Veri Kaynagi bar** (drawer ustunde) — Baglanti (DerinSIS) · SP · RS sayisi · son calisma suresi · Onizle + Ayar butonlari.

    **Fazlar:**
    - **F-0** (~1h) · M-10 Faz 3 bekleyen commit + ADR-008 yaz + branch kurulum.
    - **F-1** (~3h) · Migration 18 v1→v2 schema + table→dashboard auto-convert (idempotent, audit log'lu).
    - **F-1.5** (~4h) · `ReportCatalog.ReportType` [Obsolete] → SIL. `isDashboard` dallanmasi 5+ yerden temizle (ReportsController/ReportManagementService/Run.cshtml/EditReport.cshtml). Migration 19 drop.
    - **F-2** (~5h) · `DashboardRenderer.cs` (422) → `Rendering/` altinda IWidgetRenderer + Kpi/Chart/Table/Shell + Factory + DI refactor.
    - **F-3** (~4h) · Schema v2 model (variant + numberFormat + axisOptions + tableOptions + calculatedFields) + validator genisletme.
    - **F-4** (~5h) · 10 chart tipi renderer (Chart.js native; plugin YOK) + axisOptions + numberFormat emit.
    - **F-5** (~4h) · 4 KPI variant renderer (basic/delta/sparkline/progress).
    - **F-6** (~4h) · Tablo kosullu format (dataBar/colorScale/iconUpDown/negativeRed) + tableOptions.
    - **F-7** (~10h) · UI redesign — `dashboard-builder.js` (775) → 6 modul (core/list/drawer/contract/preview/templates) + split-pane Razor + BKM brand CSS.
    - **F-8** (~6h) · Drawer form — type switcher, variant/chart picker, swatch+icon grid, span toggle, suggest pills, **Turetilmis Alanlar editoru**.
    - **F-9** (~5h) · Live preview endpoint + validation banner + dirty chip + toast + Geri Al (last-save snapshot).
    - **F-10** (~3h) · Sablon sistemi (KPI Trio/Trend Grafik/Detay Tablo preset) + kbd shortcuts (Ctrl+S/P, Esc, Delete, ?).
    - **F-11** (~3h) · Smart defaults (SP preview → suggest pills + chart tipi onerisi).
    - **F-12** (~3h) · E2E smoke + screenshot + journal.

    **Commit sayisi:** ~19. Her commit <15 dosya (commit-discipline.md).

    **M-12'ye ertelenenler:** dark mode · Cmd+K fuzzy palette · structure widgets (markdown/iframe/divider) · heatmap/gauge/treemap/sankey/funnel/bubble/combo ileri chart tipleri · tam undo/redo stack · cross-chart filter · Chart.js plugin'leri (datalabels/zoom/annotation/matrix). ~~Gridstack~~ 24 Nisan'da geri alindi, F-5 aktif.

#### FAZ 3 — BU CEYREK (3 ay, dusuk oncelik / temizlik)
29. **M-06 · EF Core Migrations gecisi** (1 gun) — mevcut semayi baseline yap, yeni degisiklikler migration. Database/legacy/ olustur.
30. **M-07 · ViewModel BindNever + DTO pattern** (4h) — mass assignment riski. UserId, PasswordHash gibi kritik alanlar bind edilmesin.
31. **M-08 · Async/await tutarlilik** (2h) — AdminController.cs:1087-1091 vb. `.ToList()` → `.ToListAsync()`.
32. **M-09 · AsNoTracking tutarlilik** (2h) — 15+ read-only query.
33. **F-05 · Turkce UTF-8 normalize** (3h) — `turkish-ui-normalizer` skill'i ile tum "Duzenle"/"Bilesen" → "Düzenle"/"Bileşen".
34. **F-06 · CSP politikasi** (1 gun) — opsiyonel; inline onclick/script temizle, header ekle.
35. **Test coverage %30 hedefi** (1 hafta) — AdminController integration, ReportsController.Run, Admin SpPreview, PasswordHasher edge cases.
36. **G-07 · Dashboard iframe policy sikisitirma** (30dk) — Referrer-policy + sandbox kombinasyonu gozden gecir.
37. **G-08 · DashboardRenderer JSON escape regresyon testi** (1h) — `</script>`, `<!--`, case-insensitive bypass test.
38. **ADR yazimi** (1h) — ADR-001 data-access, ADR-002 dashboard-architecture. (ADR-003 role-model ✅ yazildi 22 Nisan, ADR-004 skill-design ✅ yazildi 22 Nisan.)
41. **PLAN 05 (potansiyel) — Scheduled Reports + Email Delivery** (kullanici 28 Nisan 2026 oturum 4: "sisteme cron job ekleyip raporlari mail olarak belirlenen zamanda gonderme sansimiz olabilir mi") — Tier 3, R refactor + Plan 04 sonrasi.
   - **Yol**: Hangfire (cron + dashboard + persistence) + MailKit (SMTP). Vanilla BackgroundService + Cronos alternatif (minimal).
   - **Schema**: `ScheduledReport` (Id, ReportId, CronExpression, Recipients, Format html/xlsx, IsActive, NextRunAt, LastRunAt). Migration `18_CreateScheduledReports.sql`.
   - **UI**: `Admin/ScheduledReports/{Index,Create,Edit}` — Admin/Index'e yeni subnav tab veya ayri route.
   - **Service**: `IScheduledReportRunner` Hangfire job — cron tetik → rapor calistir (UserDataFilter dahil) → mail gonder (HTML inline veya Excel attachment) → AuditLog.
   - **Config**: `appsettings.json:Smtp` (Host/Port/User/Pass/From) — env var prod icin.
   - **Tahmini**: ~2-3 gun, 8-12 commit, 1 migration, 2 yeni dep (Hangfire + MailKit), 4-5 yeni view, 2 service, 1 ADR.
   - **Risk**: Mail spam, cron drift (server timezone), HTML email client uyumu (Outlook/Gmail farkli render).

40. **PLAN 04 (potansiyel) — Alpine.js + htmx adoption** (TARTISMA gerekli, R1-R5 refactor sonrasi) — Kullanici 28 Nisan 2026 oturum 4: "kod kisaltmasi da yapmak daha efektif kodlamalar yazmak lazim js yerine daha iyi bir js framework kullanmak isi hizlandirabilir mi". Vanilla JS DOM API kodu kalabaligi (~531 sat admin-report-form, ~333 sat builder-drawer). Aday kombinasyon: **Alpine.js** (form state, mode segmented, dirty chip, opt-card secim) + **htmx** (filter row add/remove, SP Onizle swap, datasource list reload). Build step yok (CDN), Razor MVC + Tailwind ile uyumlu. Kademeli adoption (sayfa basi). PLAN-FIRST: ayri oturum, Tier 3 plan, before/after metrik (satir basina kazanc), regresyon test.

39. **YETKILENDIRME REVIZYONU — rapor + alan gorme** (TARTISMA gerekli, Plan 03 sonrasi) — Kullanici 28 Nisan 2026: "kullanici yetki tanimlama mantigi pratik olmamis ve tam istedigim gibi de degil. Rapor yetki + alan gorme konusunu konusalim bir ara, en sonda olabilir is bitince."
   - **Mevcut model:** UserRole (junction) + ReportAllowedRole (rapor-rol) + UserDataFilter (kullanici-bazli WHERE injection). Tek role "admin" + ek custom roller var ama AdminController class-level `[Authorize(Roles="admin")]` — granular yetki YOK.
   - **Sikayet noktalari (bekleniyor):** rapor yetkisi UX, alan/satir/sutun gorme granulariteci, admin-friendly tanimlama akisi.
   - **Sidebar UX bagi (28 Nisan, Faz D):** Plan 03 Faz D'de YONETIM section 6 → 1 linke indi (Admin/Index subnav 5 tab handle ediyor). Granular yetki gelirse: sidebar'a alt-link'ler conditional render edilir (`@if (User.IsInRole("report_designer"))`). Yetki revizyonu sirasinda sidebar konusunu tekrar gozden gecir.
   - **Aksiyon:** Plan 03 (M-13) tamamen kapaninca ayri oturum ac. Once mevcut akisi haritalayan sayfa-sayfa screenshot + sikayet detayini topla, sonra mimari karar (Tier 3 plan + ADR). Granular role tanimlari + AdminController action-level [Authorize] + sidebar conditional render birlikte degerlendirilmeli.

#### Toplam efor tahmini
- Faz 0 (bugun): 3 saat — blocker'lari kaldir
- Faz 1 (hafta): ~5 gun dagitilmis
- Faz 2 (ay): ~10 gun dagitilmis
- Faz 3 (ceyrek): ~15 gun dagitilmis

---

### YENI PROJE ADI ARANIYOR (28 Nisan 2026)
Kullanici 28 Nisan 2026 oturum 4: "projeye reporthub demeyelim bir ara degistirelim isim bulalim". 
Mevcut iceride brand: "ReportHub" (geçici), kod adi "ReportPanel" (klasor + namespace). 
Sidebar + AuthLayout'ta "ReportHub" gectigi yerler "BKM Kitap" + "Rapor Paneli" olarak guncelendi 
(R3.2, 28 Nisan), ama bu da yer tutucu. Yeni isim adaylari + brand-mark logosu (mevcut bkm-logo.svg 
disinda alternatif logo asetleri) icin ayri brainstorm gerekli. Plan 06 vNext sirket ici portal 
hazirligi sirasinda (yeni feature seti netlesince) isim de finalize olabilir.

### MAJOR VISION — Sonraki Versiyon: Rapor Portali → Sirket Ici Portal (28 Nisan 2026)
Kullanici 28 Nisan 2026 oturum 4: "sonraki versiyonda rapor portalindan sirket ici portala dogru evireceğiz yapiyi". 
Mevcut: rapor + dashboard portal. Hedef versiyon vNext: tam sirket ici portal.

**Eklenebilecekler (taslak):**
- **Günlük tamim / sirküler / genelge** (28 Nisan 2026 kullanici eklemesi) — günlük resmi duyuru, departman/herkese, okundu-onayli, arsiv, search
- Duyurular / haberler (announcement feed — günlük tamim ile birlikte ama serbest formatli)
- Departman / takim dizini (org chart)
- Doküman / dosya paylasimi (intranet drive)
- Mesajlasma / yorum (comment thread)
- Form / anket (form builder + survey)
- Prosedür / SOP yonetimi (knowledge base)
- Takvim / etkinlik (event calendar)
- KPI / hedef takibi (OKR pano)
- Onay akislari (workflow / approval chains)

**Mimari etkiler (degerlendirilmesi gereken):**
- Multi-tenant / departman izolasyonu — yetki revizyonu (TODO #39) BURADA kritik
- Rol modeli granular hale gelmeli (sadece admin yerine: report_designer, hr_admin, doc_manager, vb.)
- Sidebar conditional render — kullanicinin yetki olduğu modul/menüleri sadece görür
- Background services çoğalir (Plan 05 cron+email burada genisler — duyuru bildirimi, takvim hatirlatici, vb.)
- Search global — Elasticsearch / SQL FTS / Meilisearch?
- Notification subsystem (red bell icon mevcut placeholder, gerçek olur)
- File storage strategy (MinIO/S3 veya disk)
- Real-time updates (SignalR — comment thread, notification push)

**Aksiyon:** Plan 03 (M-13 design system) + Plan 04 (Alpine/htmx) + R refactor + Plan 05 (cron/email) tamamlandiktan sonra **Plan 06 — vNext architecture** ayri Tier 3 oturum: skopla alimlanacak (öncelikli modüller, faz planlamasi, breaking change yönetimi). Mevcut url + session + role infrastructure korunmali, modül-by-modül opt-in.

---

### PLAN 03 — M-13 PROJECT-WIDE DESIGN HARMONIZATION ✅ KAPANDI (28 Nisan 2026)
Plan dosyasi: [plans/archive/03-project-wide-design-system-harmonization.md](plans/archive/03-project-wide-design-system-harmonization.md). Backup branch: `backup/pre-shell-2026-04-27`.

**Sonuc (oturum 4, 28 Nisan):** Faz A+B (Plan 03 ilk yarisi, oturum 3) + Faz C+D+E+F1 (Plan 03 ikinci yarisi, oturum 4) tamamlandi. 17 view dosyasi proje genelinde tek pattern'a alindi: hero + paper card + .field/.lab/.inp + .btn + .dt + subnav + opt-card. Builder topbar Razor blok silinip _AppLayout topbar slot'a tasindi (mode segmented + dirty chip + 4 buton). Auth shell ayrildi (_AuthLayout yeni). _Layout.cshtml legacy Bootstrap silindi.

**Net commit'ler oturum 4:**
- `7bc8cb0` _FooterHint partial Dashboard model mismatch fix (smoke test sırasinda yakalandi)
- `831319b` C1: AccessDenied + Reports/Run + Test/Index pattern adaptation
- `9ba3c61` C2: 6 admin form'u + .opt-card pattern (style.css) + admin-datasource-form.js JS toggle kaldirildi (CSS :has())
- `fc55063` C3: Admin/Index sub-nav + .dt hub (.subnav pattern style.css'e eklendi, 652 → 430 satir, %34 azalma)
- `4ada9e6` D: EditReport + CreateReport pattern + builder-topbar consolidation, sidebar Yonetim section 6→1 link sadelestirme
- `4d2f5d2` E: _AuthLayout YENI + Login + Error pattern + _Layout.cshtml legacy SİLİNDİ
- `c8ce59f` F1: ui-ux-pro-max audit fix (44px touch target mobile + prefers-reduced-motion) + sidebar collapse buton fix (60px wide içinde sığmıyordu)

**Net etki:** ~1850 satir azalma, 7 yeni commit, 17 view + 4 CSS/JS dosyasi etkilendi.

**Detay Faz A/B/C/D/E/F çizelgesi:** [plans/archive/03-project-wide-design-system-harmonization.md](plans/archive/03-project-wide-design-system-harmonization.md). Tum fazlar ✅ kapandi (commit'ler yukarida).

---

### BUG: SP Onizle butonu click handler bagli degil (21 Nisan 2026 gecesi)
**Belirti:** `/Admin/EditReport/13` sayfasinda "SP Onizle" butonuna tiklandiginda ekrana hicbir sey gelmiyor, panel bos kaliyor.

**Teshis (canli test edildi):**
- Backend endpoint **calisiyor**: `/Admin/SpPreview?dataSourceKey=PDKS&procName=sp_PdksPano` → HTTP 200 JSON donuyor. Response: `{success:false, error:"SQL hatasi: TOP veya OFFSET yan tumcesi gecersiz bir deger iceriyor", resultSets:[...kolonlarla birlikte...]}`. Yani kolon metadata bile geliyor, sadece SP'nin parametresi NULL kabul etmedigi icin row bazinda patlamis.
- DOM elementleri **mevcut**: `#spPreviewBtn`, `#spPreviewPanel`, `#ProcName`, `select[name=DataSourceKey]`. ProcName=`sp_PdksPano`, DataSourceKey=`PDKS`. Script yuklu: `/assets/js/admin-report-form.js` (19416 byte, icinde spPreviewBtn + initSpHelpers + /Admin/SpList stringleri var).
- **Tiklama sonrasi panel hala `.hidden` ve innerHTML=0** → click handler **bagli degil**. `markerAttached: true` testi ile manuel `addEventListener` eklenebildi, yani element canli.

**Muhtemel sebepler:**
1. `admin-report-form.js`'in dis IIFE'si `(() => { ... if (!paramListEl || !paramSchemaEl) return; ... })();` — EditReport sayfasinda paramListEl ve paramSchemaEl var (Parametre Olusturucu panel'i goruluyor) yani erken return olmamasi gerek AMA yine de kontrol etmek lazim.
2. Benim ekledigim `initSpHelpers()` IIFE dis IIFE'nin son `})();`den ONCE mi SONRA mi konuldu? Eger sonra konulduysa outer scope degil, ama o zaman da calismasi gerekiyordu. Belki parse hatasiyla tum ikinci yari durdu. `Read` ile dosyanin son satirlarini netlestir.
3. Arrow IIFE icinde `function` keyword'u kullanmak (`(function initSpHelpers() {...})()`) sorun olmamali ama bazi minifier'lar veya ES feature detection'larda takilabilir.
4. Browser console'a bir syntax/runtime error dusmus olabilir — `mcp__Claude_Preview__preview_console_logs` ile kontrol et.

**Cozum adimlari (sabah):**
1. `mcp__Claude_Preview__preview_console_logs` ile console error var mi bak.
2. `admin-report-form.js`'in son ~50 satirini Read et, IIFE'lerin dogru kapanip kapanmadigini gor. Ozellikle outer `})();` ile inner `})();` ayrimi.
3. Gerekiyorsa **initSpHelpers'i outer IIFE'nin DISINA cikart** — ayri bir top-level IIFE yap, paramList kontrolunden bagimsiz olsun. Bu temizlik hem bug'i cozer hem de mantiksal ayrim getirir (parametre olusturucu != SP onizleme).
4. Test: reload et, butona tikla, panel.hidden kalkti mi kontrol et.

**Ek not:** sp_PdksPano parametresiz calistiramiyoruz cunku `@Tarih` gibi parametreleri NULL kabul etmiyor (TOP yan tumcesi hatasi). SpPreview endpoint'ine **varsayilan parametre degeri** ekleme secenegi dusunulmeli (ornegin tarih parametresine `GETDATE()` default, ya da admin builder UI'da "Onizleme parametreleri" alani). Bunu da "Builder UX" bolumune ekledim:

- [ ] SP Onizleme: parametreli SP'ler icin builder'da kucuk bir "Onizleme parametreleri" input seti ekle. Default olarak: date -> bugun, int -> 0, string -> '', bool -> 0. Admin override edebilsin.

---

### SP MIMARISI TARTISMASI (21 Nisan 2026 gecesi - yarina)
Kullanici iki farkli soru sordu:
1. **"Her seyi SP yapmaktan vaz mi gecsem?"** — tum SP'leri EF Core'a cevirmek.
2. **"sp_PdksPano 6 result set donuyor, KPI'lari tek tek SELECT mi yapsam, baska yerlerde de reuse edebilir miyim?"** — monolitik SP vs. modulerlestirme.

---

#### Karar 1: SP'den tamamen vazgec(me)
**Tavsiye: Hibrit, SP kal.**
- Rapor/dashboard datasi = SP kal. Bu projenin DNA'si: admin SP adi + parametre schema girer, kullanici calistirir. SP'yi kaldirirsan her rapor icin C# deploy gerek.
- Karmasik dashboard (6 RS, CTE, window function, aggregation) EF Core'da ya FromSqlRaw'a dusersin (zaten SQL yaziyorsun) ya da N+1 problemi.
- SP'de DBA operasyonel esneklik kazanir (production'da dakikalar icinde fix, deploy yok).
- Guvenlik: DB user'a sadece spesifik SP EXEC izni verilebilir.

**EF Core'a gecmesi gerekenler (zaten gecmis durumda):**
- CRUD + admin paneli (User, Role, Category, DataSource, ReportCatalog) — EF Core'da kalsin.
- Basit listeler (Logs, Favoriler, UserDataFilter) — zaten LINQ.

**Yazilacak karar dokumani:** CLAUDE.md veya `docs/ADR-001-data-access.md` olarak "Rapor/dashboard data = SP. Uygulama metadata'si = EF Core." diye netlestir. Views/Auth/AGENT.md'deki yaniltici "Dapper + Razor Pages" manifestosunu kaldir.

---

#### Karar 2: Monolitik sp_PdksPano'yu parcalama stratejisi
**Tavsiye: KPI'lari TEK TEK SP yapma, SQL View / inline TVF yap.**

**Neden SP degil de view/fn:**
- SP sadece cagrilabilir, baska query'nin `FROM`'una giremez.
- View / inline TVF her yerden SELECT edilebilir — baska SP'den, direkt admin panelden, EF Core'dan.

**Somut plan — sp_PdksPano'yu boyle kir:**

```sql
-- Atomik, reuse edilebilir parcalar (inline TVF)
CREATE FUNCTION fn_PdksDetay(@tarih date) RETURNS TABLE AS RETURN (...)
CREATE FUNCTION fn_PdksKpiOzet(@tarih date) RETURNS TABLE AS RETURN (...)   -- toplam, eksik, gec, fazla
CREATE FUNCTION fn_PdksDepartmanKirilim(@tarih date) RETURNS TABLE AS RETURN (...)

-- Dashboard orkestratoru: sadece sonuclari toparlar, hic is yapmaz
CREATE PROCEDURE sp_PdksPano(@tarih date) AS
BEGIN
    SELECT * FROM fn_PdksDetay(@tarih);
    SELECT * FROM fn_PdksKpiOzet(@tarih);
    SELECT * FROM fn_PdksDepartmanKirilim(@tarih);
END
```

**Kazanimlar:**
1. **Reuse:** Baska dashboard "eksik giris sayisi" isterse -> `SELECT EksikGiris FROM fn_PdksKpiOzet(@tarih)`. Kod duplikasyonu sifir.
2. **Bakim:** KPI formulu degisirken sadece o TVF'e dokunuyorsun, buyuk SP bozulmuyor.
3. **Test:** TVF'i izole test edersin — `SELECT * FROM fn_PdksKpiOzet('2026-04-21')` calistirip bakarsin.
4. **Performans ayni:** Inline TVF SQL Server icin SP govdesinde satir-ici (inline) genisler, round-trip tek.
5. **Widget / baska sayfa:** Profil sayfasinda "bugun eksik giris: 3" gostermek istersen ayni TVF'ten tek satir cekersin.
6. **EF Core'dan da cagirilabilir:** `_context.Set<PdksKpi>().FromSqlInterpolated($"SELECT * FROM fn_PdksKpiOzet({tarih})")`.

**Dikkat edilecekler:**
- **Inline TVF kullan** (`RETURNS TABLE AS RETURN (...)`) — multi-statement TVF (`RETURNS @t TABLE ... BEGIN ... END`) performans katili, SQL Server onlari optimize edemiyor.
- View'lar parametresiz — parametre gerekiyorsa TVF kullan.
- Yan etkili operasyonlarda (insert/update/audit yazim) SP kal — view/fn readonly.
- TVF'lere standart prefix: `fn_*`; atomik KPI'lar icin belki `fn_Kpi_*`, detay veriler icin `fn_*` ayri konvansiyon.

**Kacinilmasi gerekenler:**
- **Her KPI icin ayri SP** (`sp_Kpi_ToplamPersonel`, `sp_Kpi_EksikGiris`): 6 KPI = 6 round-trip. Performans patlamasi. SP fonksiyon gibi davranamaz.
- **Multi-statement TVF**: optimizer karacutuya dusurur, inline'dan cok yavas.
- **SP icinden SP cagirip result set birlestirme**: kirligi yuksek, test etmesi imkansiz.
- **Parametreli universal SP** (`sp_Kpi(@name, @date)` + CASE WHEN): anti-pattern, statement cache bozulur.

**Uygulama adimlari (yarina):**
1. `sp_PdksPano.sql` ac, icindeki her result set query'sini parca parca tespit et.
2. Her biri icin `fn_Pdks*` inline TVF olustur (Database/16_PdksFunctions.sql).
3. `sp_PdksPano`'yu orkestrator'e indir (sadece 3-6 satir SELECT FROM fn_).
4. Smoke test: PDKS panosu browser'da acilinca onceki ile ayni sonuclari dondurdugunden emin ol.
5. Ayni pattern'i `sp_SatisPano`'ya uygula (Database/17_SatisFunctions.sql).
6. **Bonus:** `fn_PdksKpiOzet`'i kullanan basit "Gunluk Ozet" widget'i ekle (Profile veya Dashboard sayfasina). Reuse'un canli ornegi olur.
7. Toplam sure: ~2-3 saat iki SP icin.

**Karar kriterleri (sabah onay icin):**
- Takim: Tek gelistiricisin (BKM). DBA/backend ayrimi yok -> hibrit mantikli.
- Deploy: SP production'da anlik degistirilebiliyor. SP/TVF parcalama bunu bozmaz.
- Rapor sikligi: Yeni KPI eklemek ayda bir ise, TVF reuse buyuk kazanim.

---

#### Tartisilacak diger sorular (sabaha)
- `sp_PdksPano` su anda kac result set donuyor? Her biri ne? (kod acilip gorulecek)
- Hangi KPI'lar "birden fazla dashboard'da" potansiyel olarak kullanilabilir? (PDKS'te hesaplanan kisi sayisi Satis panosunda da yardimci olur mu?)
- Ortak bir "fn_Kisi_Ozet" gibi cross-domain TVF olusturulmali mi, yoksa PDKS/Satis ayri kalsin mi?
- EF Core tarafinda `DbSet<KpiOzet>` gibi keyless entity tanimi yapilip TVF'leri LINQ'e acmak ister misin?

---

**Mevcut durum:**
- Tum raporlar SP uzerinden calisiyor (`sp_PdksPano`, `sp_SatisPano`, SeedData'daki digerleri).
- ReportCatalog.ProcName string, admin SP adini girip parametre schema'si tanimliyor.
- `ExecuteStoredProcedure` / `ExecuteStoredProcedureMultiResultSets` (ReportsController) ADO.NET ile cagriyor.
- Dashboard seed'leri SP + JSON config ikilisi uzerine kurulu.

**SP'den vazgecerse avantajlar:**
- LINQ / EF Core ile tip-guvenli query'ler; compile-time kontrol.
- Refactor kolayligi: kolon adi degisince derleyici gosterir.
- Test edilebilirlik: in-memory DB veya SQLite ile unit test.
- SQL'i C# kodu icinde gormek (ama inline SQL'in dezavantajlari da var).
- Migration / schema evolution EF Core migration'lari ile.

**SP kalmaya devam avantajlari:**
- DBA'lar SP'leri optimize edebilir, plan cache'i lehtar.
- Complex aggregation / window function / CTE'ler SQL'de cok daha verimli.
- Uygulama deploy etmeden SP'yi degistirip davranis degistirebilir (operasyonel esneklik).
- PDKS / Satis Panosu gibi dashboard'larin karmasik cok-result-set yapisi SP ile kolay.
- Guvenlik: schema-level izinler SP'ye verilebilir, tablelara direkt erisim gerekmez.

**Hibrit secenekler (orta yol):**
- **Opsiyon A:** Basit CRUD + liste ekranlari EF Core LINQ, karmasik rapor/dashboard SP kalir. En dengeli.
- **Opsiyon B:** EF Core `FromSqlRaw` / `FromSqlInterpolated` ile SQL'i kodda yaz; SP yok ama SQL gucu korunur.
- **Opsiyon C:** Dapper ekle, SP'ler Dapper'dan cagrilsin, CRUD EF Core'da kalsin. AGENT.md'deki vizyon bununla uyumlu.
- **Opsiyon D:** Hepsi SP kalsin ama SP tanimi ve migration'lari repo'ya versiyonlu girsin (zaten kismen var).

**Karar kriterleri (sabah uzerinde konusulacak):**
- Takim buyuklugu: tek gelistirici mi, yoksa DBA/backend ayri mi?
- Deploy disiplini: SP'yi production'da degistirme yetkisi kimde?
- Rapor olusturma sikligi: yeni rapor haftada bir mi, ayda bir mi?
- Dashboard karmasikligi: mevcut 2 panodan onesinin karmasikligina bak (sp_PdksPano.sql 6 RS donuyor).
- Test kulturu: unit test agirlikli mi, integration test mi?

**Yarin baslangic:** Kullaniciya bu kriterleri tek tek sor, sonra net tavsiyede bulun (muhtemelen Opsiyon A veya C). TODO.md'ye karar notu yaz. Karar "vazgecelim" yonunde olursa: migration plani (hangi SP ilk gidecek, ikame LINQ query, test senaryolari).

---

### MIMARI TUTARSIZLIKLAR - Tum proje denetimi
(Audit tarihi: 21 Nisan 2026. Explore agent ile kapsamli tarama yapildi.)

#### Yapildi (bu oturumda)
- [x] Mimari tutarsizlik taramasi yapildi, bulgular asagida.
- [x] dotnet build basariyla gecti (uyari yok, exit 0).

#### YUKSEK risk
- [ ] **User.Roles CSV + UserRole tablosu ikili sistem**: User modelinde `public string Roles` hala var, UserRole normalize tablosu da var. Admin CreateUser'da User.Roles yaziliyor AMA UserRole eklenmeyebiliyor. DashboardController CSV parse ederken ReportsController UserRole join yapiyor. **Coz:** User.Roles field'ini kaldir, tum role kontrolu UserRole uzerinden. Migration ile CSV'yi normalize et.
  - ReportPanel/Models/User.cs:27
  - ReportPanel/Controllers/DashboardController.cs:68-82 (CSV parse)
  - ReportPanel/Controllers/ReportsController.cs:73-75 (normalize)
- [x] ~~**DashboardHtml vs DashboardConfigJson dual storage**~~ → ✅ M-05 3 faz kapandi (Faz C commit `0f73478`, migration 17, ADR-005). DashboardConfigJson tek source-of-truth.
  - ReportsController.cs:238-249 (iki render path)
- [ ] **Data access stratejisi belirsiz**: EF Core var, Dapper hic kullanilmamis (Program.cs'de register yok) ama SP'ler var (sp_PdksPano.sql, sp_SatisPano.sql). SP'ler ADO.NET ile mi, EF FromSqlRaw ile mi cagriliyor? **Coz:** Tek strateji sec - ya EF Core + SP'leri repository'ye al, ya Dapper ekle. Dokumante et.

#### ORTA risk
- [ ] **Async/await karisik**: HomeController sync (`IActionResult Index()`), digerleri async. Bazi yerler `.ToList()` yerine `.ToListAsync()` kullanmali. **Coz:** Tum controller action'lari `async Task<IActionResult>`, tum EF query'ler `*Async`.
- [ ] **AsNoTracking() eksik**: AdminController.Index:36, DashboardController:36 tracking'siz read yapiyor ama `.AsNoTracking()` yok. ReportsController:72 ve ProfileController:31'de var. **Coz:** Read-only query'lere her yerde `.AsNoTracking()`.
- [ ] **ViewModel'lar sadece wrapper, Entity view'a dogrudan gidiyor**: AdminIndexViewModel.Reports = List<ReportCatalog> (Entity). **Coz:** DTO pattern'e gec, View'a Entity gonderme. Otomatik mapping (AutoMapper) ya da manuel projection.
- [ ] **Form syntax karisik**: EditReport `Html.BeginForm`, digerleri raw `<form method="post">`. **Coz:** Tek pattern sec (tercihen `Html.BeginForm` + `asp-action`), hepsini donustur.

#### DUSUK risk
- [ ] **AuditLog selektif**: Profile update/password_change log'lu, CreateUser log'lu, ama EditUser ve bazi datasource action'lari degil. **Coz:** Tum CRUD action'lara `_auditLog.LogAsync` ekle (create/update/delete).
- [ ] **CSS: Tailwind utility + custom brand class'lar karisik**: `btn-brand`, `form-input-brand` gibi custom class'lar var, Tailwind utility de var. **Coz:** Custom class'lari tailwind config'de `@layer components` ile `@apply` kullanarak tanimla.
- [ ] **Naming karisik**: Tablo/kolonlar Ingilizce, UI Turkce. sp_PdksPano Turkce param (`@Tarih`), sp_SatisPano Ingilizce. **Coz:** Kural netle: code Ingilizce, UI Turkce, SP'ler tutarli olsun.
- [ ] **Database scriptleri karisik**: 05_DropReportRunLog destructive, 12/14 seed, diger schema — hepsi ayni klasorde numaralanmis. **Coz:** `Database/Schema/`, `Database/Seed/`, `Database/Migrations/`, `Database/StoredProcedures/` alt klasorleri.
- [ ] **Test coverage dusuk**: Sadece PasswordHasher + AuditLog test'leri var. **Coz:** ReportsController.Run, AdminController.CreateUser, DashboardRenderer ve kritik business logic icin integration + unit test ekle.
- [ ] **ReportPanel/Views/Auth/AGENT.md projeye uymuyor**: "MVC yok, Razor Pages + Dapper" manifestosu, oysa proje MVC + EF Core. **Coz:** Dosyayi sil veya "hedef mimari" olarak dokumante et ve migration plani yaz.
- [ ] **JavaScript bundle/build yok**: Tum JS ayri dosyalar, runtime'da yukleniyor, minify yok. **Coz:** Kucuk proje icin abartmaya gerek yok ama esbuild/vite ile minify + bundle production icin.

---

### DETAYLI KULLANICI YONETIMI - Eksikler
(Not: Mevcut durumda User modeli + EditUser.cshtml + CreateUser.cshtml var; roller checkbox, AD flag, aktif toggle, sifre, veri filtreleri zaten destekleniyor. Asagidakiler eksik.)

#### P0 - Tutarsizlik (kritik)
- [ ] **CreateUser.cshtml'e veri filtresi bolumu ekle**: Su an sadece EditUser'da var. Yeni kullanici olusturulurken filtre tanimlanamiyor, iki adimli ack: once kaydet sonra duzenle. EditUser'daki "Veri Filtreleri" section + JS'i CreateUser'a tasi, AdminController.CreateUser POST handler'inda FilterKeys/FilterValues/FilterDataSources array'lerini al ve UserDataFilter kayitlari olustur.

#### P1 - Detay alanlari (kullanicinin sordugu "detayli" icin)
- [ ] **User modeline alan ekle**: Phone (string?, 20 char), Department (string?, 100), Position/Title (string?, 100), ManagerUserId (int?), Notes (string?, 500).
  - SQL migration: `15_AddUserDetailColumns.sql`
  - Create + Edit formlarina ilgili input'lar
  - Admin listesinde (Views/Admin/Index.cshtml user tablosu) departman ve pozisyon kolonu
- [ ] **Admin user listesinde arama + filtreleme**: Username/FullName/Email'de arama, rol filtrelemesi, aktif/pasif filtresi, AD/local filtresi.
- [ ] **Son giris zamani gosterimi**: LastLoginAt var ama admin tablosunda gozukmuyor. "Son giris: 2 gun once" formatinda.

#### P2 - Guvenlik ve audit
- [ ] **User modeline audit alanlari**: PasswordChangedAt, FailedLoginCount, LockedUntil, MustChangePassword flag.
- [ ] **Hesap kilitleme mekanizmasi**: 5 basarisiz girisde 15 dakika kilit, AuthController'da kontrol.
- [ ] **Sifre karmasikligi**: Minimum uzunluk, harf+sayi zorunlulugu Admin ayarlarinda.
- [ ] **Zorla sifre degistirme**: Admin create ederken "Ilk giriste sifre degistir" flag.
- [ ] **Kullanici sifre sifirlama**: Admin'den "Sifre sifirla" linki, token ile kullanici yeni sifre belirler.

#### P2 - Operasyonel
- [ ] **Toplu kullanici import**: Excel/CSV'den tek seferde coklu kullanici olusturma. ClosedXML ile parse.
- [ ] **AD senkronizasyonu**: Domain'den kullanici arama (LDAP search), bulunan kullaniciyi tek tikla sisteme ekleme.
- [ ] **Kullanici kopyalama**: Mevcut kullanicinin rol ve filtrelerini kopyalayarak yeni kullanici olustur.
- [ ] **Kullanici silme yerine pasif yapma**: Admin silmeden once "Gercekten sil? Arsivlenmesini oner" tercihi.

#### P3 - Profil iyilestirme
- [ ] **Avatar/profil resmi**: Upload + resize (wwwroot/avatars/).
- [ ] **Kullanici tercihleri**: Dil, tema, varsayilan sayfa boyutu.
- [ ] **Kullanici aktivite ozeti**: Profilde son N rapor, favori sayisi, toplam calistirma.

---

### DASHBOARD OZELLIGI - Iyilestirme & Gelistirme Plani
(Son guncelleme: 21 Nisan 2026. main branch'te 32 dosya uncommitted. DashboardRenderer.cs, dashboard-builder.js, EditReport.cshtml, PDKS+Satis seed dashboard'lari.)

#### Yapildi (bu oturumda)
- [x] Builder "Duzenle" butonu bug fix: `_dbTypeChange` editIndex'i reset ediyordu, artik edit modu korunuyor (dashboard-builder.js:303).
- [x] Builder "Duzenle"ye tiklaninca forma smooth scroll + ring flash feedback; edit modunda form arka plani mavi; "Duzenleniyor: {title}" banner + cancel link (dashboard-builder.js `_dbEditComp` ve `render`).
- [x] Drag-drop component siralama: HTML5 native API, grip handle, dragover highlight, editIndex korumasi (dashboard-builder.js `attachDragDrop`).

#### Builder UX - Basitlestirme yol haritasi (oncelik sirali)
**P1 - Kullanim akisi:**
- [ ] **Liste + Form yan yana layout**: Su an liste ustte form altta, scroll gerekli. Liste sol %35, form sag %65 olarak grid. Mobilde stack.
- [ ] **Inline bilesen card edit**: "Duzenle" butonu yerine card'a tiklaninca card genisleyip form icine gomulu acilsin (accordion tarzi).
- [ ] **SP kolon auto-detect**: "Kolon adi" text input'lari dropdown olsun. Admin SP'yi "Onizle" ile calistiririr, donen kolon listesi builder'daki tum column field'larda datalist/autocomplete olarak kullanilsin.
- [ ] **Component copy/duplicate**: Bir KPI'yi kopyalayip ufak degisiklikle yenisini olusturma. List'te "Kopyala" butonu.
- [ ] **Hazir sablon galerisi**: "KPI: Sayac", "KPI: Toplam", "Grafik: Aylik Trend", "Tablo: Detay Liste" gibi hazir preset'ler. "+ Bilesen Ekle" yerine "Sablondan Sec" + "Sifirdan Olustur" iki buton.

**P1 - Gorsel feedback:**
- [ ] **Canli onizleme iframe**: Builder'in sag tarafinda/altinda iframe ile gercek render. Her degisiklikte (throttle 300ms) iframe yenile. Test SP run endpoint'i gerekecek.
- [ ] **Validation feedback**: Bos title, gecersiz RS index, eksik dataset column'u -> kaydetmeden once uyari (red border + tooltip).
- [ ] **Unsaved changes warning**: Form dolu + "Iptal" -> onay modali. Sayfadan cikma -> beforeunload warning.

**P2 - Ileri ozellikler:**
- [ ] **Tab surukle-birak siralama**: Su an sadece en sona ekleniyor.
- [ ] **Undo/redo**: Son 10 config state'i hafizada tut, Ctrl+Z / Ctrl+Y.
- [ ] **Keyboard shortcuts**: Del = sil, Enter = kaydet, Esc = iptal, Ctrl+D = duplicate.
- [ ] **Raw JSON editor modu**: Builder bozulursa kurtarma icin Monaco/ace.js'li JSON editor + schema validation.
- [ ] **Component grubu / section**: Tab icinde section ayraci (baslik + aciklama).

#### Dashboard Render motoru - Guvenlik & Bug (audit sonucu)
**P0 - Kritik:**
- [ ] **Config deserialize try/catch** (ReportsController.cs:240-243): Bozuk JSON -> 500 hata. Fallback + audit log.
- [ ] **Modal innerHTML XSS** (DashboardRenderer.cs:135-144): `modalBody.innerHTML` satir degerleriyle dolduruluyor, HTML tag icerirse XSS. DOM API + textContent.
- [ ] **Table onclick inline JSON XSS** (DashboardRenderer.cs:214): `onclick="showDetail(JSON)"` attribute'a goemmek yerine `data-row-index` + addEventListener.
- [ ] **eval() kaldir** (DashboardRenderer.cs:163, 186, 208): `eval('RS_' + rs)` yerine `window.__RS[rs]` array indexing.

**P1 - Onemli:**
- [ ] **Result set index validation**: Config'de `resultSet: 5` ama SP 3 dondurdu -> sessizce bos. Server-side validation + render'da fallback uyarisi.
- [ ] **Builder submit validation**: Bos title, negatif rs, eksik column -> submit engelle.
- [ ] **Mobile responsive grid**: DashboardRenderer.cs:80 `grid-cols-4` sabit; `grid-cols-1 sm:grid-cols-2 lg:grid-cols-4`.
- [ ] **Dashboard Excel export davranisi**: Su an dashboard raporda ne oluyor netlestir; export butonunu gizle ya da multi-sheet destekle.
- [ ] **Unit test eksikligi**: ReportPanel.Tests'te DashboardRenderer hic test edilmemis. Smoke + XSS payload + invalid RS + JSON round-trip testleri.

**P1 - Performans:**
- [ ] **Inline RS boyut limiti**: 10K satir -> 3MB HTML. Ilk N satiri embed et + uyari, ya da AJAX lazy-load endpoint.
- [ ] **Tailwind/Chart.js local**: Production'da CDN yerine wwwroot/lib'den serve et.

**P2 - Veri & UX:**
- [ ] **Tarih kolon formati**: TableColumnDef'e `format: date|datetime|number`, renderer'da switch.
- [ ] **Chart tooltip Turkce sayi formati**: `options.plugins.tooltip.callbacks.label` + fmtNum.
- [ ] **Parametreli dashboard SP**: sp_PdksPano `@Tarih` aliyorsa Run'da parametre formu gorunsun, POST'a eklensin. (Dogrulama gerekli - zaten calisiyor olabilir.)

**P2 - Mimari:**
- [ ] **Runtime JS ayir**: DashboardRenderer.cs icinden ~100 satir inline JS'i wwwroot/js/dashboard-runtime.js'e cikar, render sadece config + script src uretsin.
- [x] ~~**Legacy DashboardHtml path**~~ → ✅ M-05 Faz C (`0f73478`) RenderDashboardTemplate silindi, kolon DROP.
- [ ] **DashboardRenderer static -> DI**: Test edilebilirlik icin IDashboardRenderer interface.

#### P3 - Sonra (nice-to-have)
- [ ] Stacked / area / mixed chart tipleri
- [ ] Median / percentile / yuzde degisim (YoY) agg fonksiyonlari
- [ ] Text/markdown, gauge, progress bar component tipleri
- [ ] Drag-drop ile component span (4 kolonluk grid icinde dinamik span)
- [ ] i18n: tr-TR hardcoded kaldirilsin, appsettings'den okusun
- [ ] Dashboard export: PDF, PNG (Playwright/Puppeteer ile)
- [ ] Dashboard paylasim linki (tokenized URL)
- [ ] "Ana Dashboard" atama: Settings'e DefaultDashboardReportId, login sonrasi otomatik calistir

#### Commit stratejisi (32 dosya uncommitted)
Anlamli parcalara bolunecek:
1. **feat: rol sistemi** (Role.cs, UserRole.cs, ReportAllowedRole.cs, SQL 07-08, admin rol yonetimi)
2. **feat: rapor kategorileri** (ReportCategory, SQL 07-08, AdminCategoryFormViewModel, EditCategory.cshtml)
3. **feat: rapor favorileri** (ReportFavorite, SQL 06, ReportsController)
4. **feat: AD user destegi** (User.IsAdUser, SQL 09, AuthController)
5. **feat: user data filter** (UserDataFilter, SQL 13, InjectUserDataFilters)
6. **feat: dashboard motoru** (DashboardConfig, DashboardRenderer, dashboard-builder.js, SQL 10-12, 14, sp_PdksPano, sp_SatisPano, EditReport/CreateReport dashboard UI)
7. **docs: kullanici kilavuzu ve install notlari**

## Yapilacaklar (detayli)
### 8) Iyilestirme onerileri
- Performance: rapor sonuclari icin caching, buyuk sonuc setleri icin pagination.
- Performance: connection pooling/timeout ayarlari gozenlemesi.
- Guvenlik: rate limiting (brute force korumasi), HTTPS zorunlulugu, CSP, session timeout.
- Ozellikler: scheduled reports, email export, dashboard grafiklerini zenginlestirme, rapor favorileri.
- Test: integration testleri artirma, UI test otomasyonu (Selenium), load testing.
- DevOps: CI/CD pipeline, otomatik deploy, monitoring/alerting, backup stratejisi.
### 7) Test ve kontrol
