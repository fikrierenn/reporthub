# Mosaik — TODO

Bu dosya AKTIF işleri ve backlog'u takip eder. Tamamlanmış işler arşiv bölümünde, detay için `docs/journal/` ve `plans/archive/`.

---

## AKTIF (öncelik sırası)

### EN ÜST ÖNCELİK (2026-05-08 sonrası)

Sıra: Plan 14 + Plan 16.5 + IK quick wins birleşik.

- [ ] **Plan 18B · HR Sync** (büyük, ~16-24h) — Hangfire + Mosaik.User ek kolonlar + UserSyncService + Email/TC kararları + UserDataFilter otomatik atama. 18A bittikten sonra.
- [ ] **Plan 16.5 Faz C+D** — AI Core (YonetIQ providers + DikkatIQ extraction). Plan 19 Doküman öncesi zorunlu.
- [ ] **Plan 17 (Tamim)** — vNext modül roadmap'in ilk üyesi (~10h, tamim/ direkt port). Faz H (Bildirim) en sona.

### IK / HR — Zirve `vw_PersonelDepartman` ile

**Bağlam:** BKM Zirve `vw_PersonelDepartman` view 3 firma UNION (BKM_GENEL + BURSA_KÜLTÜR_MERKEZİ + ASİYE_BİNGÖLBALI), 4 seviye hiyerarşi (Lokasyon → AltLokasyon → Departman + Unvan), 272 aktif personel. IK DataSource zaten Mosaik'te kayıtlı. Detay: `memory/project_zirve_personel_discovery.md`.

**Hızlı kazanım (Plan 18 sync'i beklemez — sadece read-only rapor/dashboard):**
- [ ] **IK Personel Listesi raporu** — vw_PersonelDepartman üzerinden filtreli liste (Firma + Lokasyon + AltLokasyon + Departman + Unvan dropdown'ları). SP: `bkm.sp_IkPersonelListesi`. FilterDefinition `sube/IK` aktive.
- [ ] **Yeni Başlayanlar raporu** — `Igt >= @BasTarih AND Ict IS NULL`, son 30/90 gün filtreli. KPI: aylık trend.
- [ ] **İşten Ayrılanlar raporu** — `Ict BETWEEN @BasTarih AND @BitTarih`, ayrılma kodu (Icn) gruplu.
- [ ] **Mağaza Personel Yoğunluğu raporu** — AltLokasyon × Departman pivot. Toplam personel + ortalama kıdem.

**Plan 14 Faz B implementasyon (durmuş):**
- [ ] **IK FilterDefinition `(sube, IK)` aktivasyonu** — 3 OptionsQuery seçeneği hazır. Migration 33. Kullanıcı 8 May "şimdilik durdur" dedi.

**Plan 18 HR Sync (henüz yazılmadı, büyük yatırım):**
- [ ] **Mosaik.User entity ek kolonlar** — `ZirvePersonelNo` (UNIQUE), `Lokasyon`, `Sube`, `Departman`, `HireDate`, `Firma`. Migration NN.
- [ ] **Hangfire entegrasyonu** — Daily job altyapısı.
- [ ] **`UserSyncService`** — view → Mosaik.User insert/update (Personelno bazlı). Yeni: insert. Ict NOT NULL: IsActive=0. Sube değişti: UserDataFilter güncelle.
- [ ] **Email stratejisi karar** — AD lookup / placeholder / manuel admin GUI.
- [ ] **TC PII saklama karar** — hash mı raw mı skip mi.
- [ ] **UserDataFilter otomatik atama** — sync sırasında `User.Sube` → `UserDataFilter (sube/IK)`.
- [ ] **Plan 18 dosyası** `plans/18-hr-sync.md` yazılacak.

**Bilinen riskler:**
- `vw_PersonelDepartman` user-managed (Zirve schema değişirse kırılır). Plan 18 öncesi backup.
- `BKM_HEYKEL_GENEL` DB var ama view'da yok — netleştirilmeli.
- View raw PII (TC, IBAN, Maaş) içeriyor — sync mapping finansal alanları skip etmeli.

### Trivia / housekeeping (~30 dk)
- [ ] **NotebookLM re-login** — terminalde: `D:/Dev/reporthub/.venv/notebooklm/Scripts/notebooklm.exe login`

### Orta (2-4h)
- [ ] **G-09 · SP read-only login** ⚠️ CANLIYA CIKMADAN ZORUNLU — Rapor execution için ayrı read-only SQL login (db_datareader + EXECUTE). DataSource modeline `ReadOnlyConnString` ekle veya mevcut ConnString'i read-only login ile değiştir (kullanıcı kararı 2026-05-01).
- [ ] **SP mimarisi · sp_PdksPano → inline TVF refactor** — `fn_PdksDetay`, `fn_PdksKpiOzet`, `fn_PdksDepartmanKirilim` + orkestrator SP. ADR-004 adayı. Detay arşivde.
- [ ] **Dashboard P1 · Inline RS boyut limiti / lazy-load** (10K satır → 3MB HTML, ilk N + AJAX)

### Büyük (>4h, çok-fazlı)
- [ ] **Plan 05 · AST formula parser** (~6h+) — kendi recursive descent parser
- [ ] **Plan 13+ · vNext modüller** (Documents, Announcements, Calendar, Forms, Messages, Approvals — modül-modül ekleme, Plan 12 module infrastructure üzerine)
- [ ] **Plan 14 · Filter Production-Readiness** (`plans/14-filter-production-readiness.md`) — Faz A ✅. Faz B (IK sube OptionsQuery + migration 33) bekliyor. Faz C (Plan 16.5 Faz B sonrası IUserDataScope ile reportAccess deny-by-default). Faz D Plan 18 sonrasına ertelendi.
- [ ] **Plan 16 · vNext modül roadmap** (`plans/16-vnext-module-roadmap.md`) — 9 modül (17→26) port stratejisi. Onay bekliyor.
- [ ] **Plan 16.5 · Mosaik.Core shared kit** (`plans/16.5-shared-kit.md`) — Faz C+D bekliyor (AI Core).
- [ ] **Plan (eski 14) · Install/Deploy script** — master kurulum (DB CREATE + migration + seed + brand + admin user prompt). Plan 11 öğretisi: rapor metadata dump install kapsamına dahil. Numara çakışması var, yeni numara alacak.
- [ ] **context-mode esinlenmesi · 2 hook** — (1) `PreCompact` hook: `/compact` öncesi aktif TODO + son commitler otomatik journal'a append. (2) `PostToolUse` output-size uyarısı: MCP tool çıktısı >50 satırı geçince logla. Kaynak: https://github.com/mksglu/context-mode

### Stratejik / belirsiz vade (TARTIŞMA gerekli)
- [ ] Plan 04 (potansiyel) · Alpine.js + htmx adoption
- [ ] Plan 05 (potansiyel) · Scheduled Reports + Email Hangfire
- [ ] Yeni proje adi brainstorm
- [ ] vNext · Sirket içi portal architecture (Plan 06)
- [ ] Yetki revizyonu — granular roller (action-level [Authorize], sidebar conditional)

---

## DEVAM EDEN PROJE / TARTIŞMA

### YENİ PROJE ADI ARANIYOR (28 Nisan 2026)
Kullanıcı: "projeye reporthub demeyelim bir ara değiştirelim isim bulalım". Mevcut brand: "ReportHub" (geçici), kod adı "Mosaik" (klasör + namespace). Sidebar + AuthLayout'ta "BKM Kitap" + "Rapor Paneli" yer tutucu olarak güncellendi. Plan 06 vNext sırket içi portal hazırlığı sırasında (yeni feature seti netleşince) isim de finalize olabilir.

### MAJOR VISION — Sonraki Versiyon: Rapor Portali → Şirket İçi Portal (28 Nisan 2026)
Kullanıcı: "sonraki versiyonda rapor portalindan şirket içi portala doğru evireceğiz yapıyı". Hedef versiyon vNext: tam şirket içi portal.

**Eklenebilecekler (taslak):**
- Günlük tamim / sirküler / genelge — günlük resmi duyuru, departman/herkese, okundu-onaylı, arşiv, search
- Duyurular / haberler (announcement feed)
- Departman / takım dizini (org chart) — Plan 20 ✅ kısmen
- Doküman / dosya paylaşımı (intranet drive)
- Mesajlaşma / yorum (comment thread)
- Form / anket (form builder + survey)
- Prosedür / SOP yönetimi
- Takvim / etkinlik
- KPI / hedef takibi (OKR)
- Onay akışları (workflow / approval chains)

**Mimari etkiler:**
- Multi-tenant / departman izolasyonu — yetki revizyonu kritik
- Rol modeli granular (report_designer, hr_admin, doc_manager...)
- Sidebar conditional render
- Background services (Hangfire — duyuru bildirimi, takvim hatırlatıcı)
- Search global — Elasticsearch / SQL FTS / Meilisearch?
- Notification subsystem (red bell mevcut placeholder, gerçek olur)
- File storage strategy (MinIO/S3 veya disk)
- Real-time updates (SignalR — comment thread, notification push)

**Aksiyon:** R refactor + Plan 05 (cron/email) tamamlandıktan sonra **Plan 06 — vNext architecture** ayrı Tier 3 oturum.

---

## BACKLOG (henüz başlanmadı)

### User yönetimi P1-P3
- [ ] User modeline alan ekle: Phone (string?, 20), Department (string?, 100), Position/Title (string?, 100), ManagerUserId (int?), Notes (string?, 500). Migration + form alanları + admin liste kolonları.
- [ ] Admin user listesinde arama + filtreleme (Username/FullName/Email arama, rol/aktif/AD filtre).
- [ ] Son giriş zamanı gösterimi (LastLoginAt admin tablosuna, "2 gün önce" formatı).
- [ ] User audit alanları: PasswordChangedAt, FailedLoginCount, LockedUntil, MustChangePassword.
- [ ] Hesap kilitleme (5 başarısız → 15dk).
- [ ] Şifre karmaşıklığı kuralları (min uzunluk, harf+sayı zorunluluğu).
- [ ] Zorla şifre değiştirme flag (admin create'te "ilk girişte değiştir").
- [ ] Admin şifre sıfırlama (token ile).
- [ ] Toplu CSV import (ClosedXML).
- [ ] AD/LDAP senkronizasyon (LDAP search + tek tıkla ekleme).
- [ ] Kullanıcı kopyalama (rol + filtre kopyala).
- [ ] Soft delete (silmeden önce arşivleme önerisi).
- [ ] Avatar / profil resmi (upload + resize).
- [ ] Kullanıcı tercihleri (dil, tema, sayfa boyutu).
- [ ] Kullanıcı aktivite özeti (son N rapor, favori sayısı, toplam çalıştırma).

### ReportCatalog & Filtreleme
- [ ] **ReportCatalog.AllowedRoles CSV deprecate** — ADR-004 adayı. ReportAllowedRole junction birincil.

### Dashboard ileri özellikler (P2-P3)
- [ ] Tab sürükle-bırak sıralama
- [ ] Undo/redo (son 10 state, Ctrl+Z/Y)
- [ ] Raw JSON editor modu (Monaco/ace.js + schema validation)
- [ ] Component grubu / section ayraç
- [ ] Tarih kolon formatı (TableColumnDef format: date|datetime|number)
- [ ] Chart tooltip TR sayı formatı
- [ ] Runtime JS ayır (DashboardRenderer içinden inline JS → wwwroot/js/dashboard-runtime.js)
- [ ] DashboardRenderer static → DI (IDashboardRenderer interface)
- [ ] Stacked / area / mixed chart tipleri (M-12)
- [ ] Median / percentile / YoY agg fonksiyonları
- [ ] Text/markdown, gauge, progress bar component tipleri
- [ ] i18n: tr-TR hardcoded → appsettings
- [ ] Dashboard export: PDF, PNG (Playwright/Puppeteer)
- [ ] Dashboard paylaşım linki (tokenized URL)
- [ ] "Ana Dashboard" atama (Settings.DefaultDashboardReportId)
- [ ] Heatmap + Gauge widget'lar (M-12 disabled, sonraya)

### Mimari / kalite (FAZ 3)
- [ ] **M-06 · EF Core Migrations geçişi** (1 gün) — mevcut şemayı baseline yap, Database/legacy/ oluştur.
- [ ] **F-06 · CSP politikası** (1 gün) — opsiyonel; inline onclick/script temizle, header ekle.
- [ ] **Test coverage %30 hedefi** (1 hafta) — AdminController integration, ReportsController.Run, Admin SpPreview, PasswordHasher edge cases.
- [ ] AuditLog selektif eksikler — datasource/category delete log'lanmıyor (G-04 takip).
- [ ] CSS karışıklığı — `@apply` ile components tanımı.
- [ ] Database scriptleri klasör ayırma (Schema/Seed/Migrations/StoredProcedures).
- [ ] JavaScript bundle/build (esbuild/vite minify, prod için).

### Mimari tutarsızlıklar (kalan YÜKSEK / ORTA)
- [ ] **Data access stratejisi dokümantasyonu** — ADR-001 yazıldı ✅ ama AGENT.md gibi yanıltıcı manifesto kalanları gözden geçir.
- [ ] **ViewModel → DTO pattern** (mass assignment riski, AutoMapper veya manuel projection).
- [ ] **Form syntax tutarlılığı** (raw `<form>` standart, EditReport vb. düzelt).

### Performans & Operasyon
- [ ] Rapor sonuçları için caching, büyük sonuç setleri için pagination.
- [ ] Connection pooling/timeout ayarları gözden geçirme.
- [ ] Rate limiting (brute force koruması), HTTPS zorunluluğu, session timeout.
- [ ] Integration testleri artırma, UI test otomasyonu (Selenium), load testing.
- [ ] CI/CD pipeline, otomatik deploy, monitoring/alerting, backup stratejisi.

---

## TAMAMLANDI ARŞİV

### Plan'lar — kapanmış (detay `plans/archive/`)
- ✅ **Plan 03** — M-13 Project-Wide Design Harmonization (28 Nisan 2026, 7 commit, 17 view + 4 CSS/JS, ~1850 satır azalma)
- ✅ **Plan 07** — Yetki/Filter revizyon (4-6 Mayıs 2026): FilterDefinition master + UserDataFilters dinamik UI + deny-by-default + raporGrubu rename + Reports/Index liste filtresi + Admin Filtreler CRUD. Migration 20-24.
- ✅ **Plan 09** — Designer ↔ Run görsel parite (6 Mayıs 2026, 4 faz)
- ✅ **Plan 11** — Mosaik Foundation (rebrand + DB reset + UTC) (7 May 2026)
- ✅ **Plan 12** — Admin GUI + Brand+Module parametric (7 Mayıs 2026)
- ✅ **Plan 14 Faz A** — UserDataFilter diff audit (commit `27714c5`, 2026-05-07)
- ✅ **Plan 14 Faz C1** — SpInjectionScope + ReportAccessScope (commit `4e52a05`, 2026-05-08). C2/C3 ROI düşük.
- ✅ **Plan 16.5 Faz A+B** — Domain primitives + Workflow + Lookup + DataScope. Migration 33+34. 43 test (2026-05-08).
- ✅ **Plan 18A** — IK Quick Reports + Dashboard (commit `a972b66`+`e5b442e`, 2026-05-11). 6 SP + ReportCatalog seed + İK Pano + Alpine bug fix.
- ✅ **Plan 20 Faz A+B+C** — Org Chart (3 görünüm + sağ-tık modal + Zirve canlı incumbent + PNG export, 2026-05-08).
- ✅ **M-11 Plan 02** — Dashboard Builder UX Redesign (13 faz F-0..F-12, 6 Mayıs 2026 KAPANIŞ). Plan 09 paritesi dahil.

### FAZ 0 — KAPANDI (22 Nisan 2026)
- ✅ **G-01** Hardcoded SA şifresi (commit `8de22fd`)
- ✅ **Bağlam yönetimi** rituel + rules + hooks (commit `e59e3a9`)
- ✅ **F-01** SP Önizle click handler (commit `07f4b91`)
- ✅ **32-dosyalık backlog commit-split** (16 commit `64259ed`..`7a7b81d`)
- ✅ **Deprecated artifacts** (Views/Auth/AGENT.md silindi, `7a7b81d`)
- ✅ **Pre/Post-commit hook'lar** (commit `59888db`)

### FAZ 1 — KAPANDI (22 Nisan 2026)
- ✅ **M-02** Exception handling sanitize (`b6ff43a` + `a047957`)
- ✅ **G-02** Open redirect fix (`4c40f61`)
- ✅ **G-03** UserDataFilter whitelist + regex (`4c40f61`)
- ✅ **F-02** SP Önizle default parametre + admin override (`b6ff43a` + `816c8c2`)
- ✅ **M-03 Faz A+B** User.Roles CSV deprecate kod-düzey + nullable (`2d0c3fd`, `bf922ae`)
- ✅ **M-04** DashboardRenderer + UserDataFilter + UserRole sync unit tests (`6c70b1e`, `b714916`)
- ✅ **session-handoff skill auto-commit** (`5df75ff`)
- ✅ **dashboard-builder.js spPreviewReady + kolon datalist** (`b3ae747`)

### FAZ 2 — KAPANDI
- ✅ **M-01** AdminController service extraction (5 adım: Category/Role/DataSource/Report/User Management Services + UserRoleSyncService)
- ✅ **G-04** Audit log genişletme — 10 CRUD audit (`effa7b5`)
- ✅ **G-05** Cookie HttpOnly/Secure/SameSite/ExpireTimeSpan (`fdc97ca`)
- ✅ **G-06** TestController authorize + antiforgery (`fdc97ca`)
- ✅ **G-07** Dashboard iframe policy review (4 Mayıs 2026 audit)
- ✅ **G-08** DashboardRenderer JSON escape regresyon test (6 escape testi)
- ✅ **M-03 Faz C** User.Roles kolon drop (Migration 19, 4 Mayıs 2026)
- ✅ **M-05** DashboardHtml legacy retirement (3 faz, ADR-005, Migration 17)
- ✅ **M-07** ViewModel BindNever (4 Mayıs 2026)
- ✅ **M-08** Async tutarlılık (4 Mayıs 2026)
- ✅ **M-09** AsNoTracking sweep (4 Mayıs 2026)
- ✅ **M-10** Named Result Contract (Faz 1-6, ADR-007, Migration 18+26+27, 6 Mayıs 2026)
- ✅ **F-03** dashboard-builder.js memory leak (F-7 split'te çözüldü)
- ✅ **F-04** AGENT.md silindi (`7a7b81d`)
- ✅ **F-05** Türkçe UTF-8 normalize (4 Mayıs 2026)
- ✅ **dashboard-builder.js V1 split** (F-7 modülerleştirme, 7 modül)
- ✅ **builder-v2/builder-drawer.js split** (4 Mayıs 2026: 511 → 269 + 259)
- ✅ **Hesaplı kolon autocomplete** (commit `36dd2f3`, 4 Mayıs 2026)
- ✅ **DateTime sweep** Faz A-E (UtcNow + Migration 25 + Plan 11 reset)
- ✅ **Dashboard P0/P1 audits** — Config deserialize, RS index validation, Mobile responsive, Tailwind local serve
- ✅ **CreateUser veri filtresi bölümü** (zaten _AdminUserDataFilterPanel partial'ı çağırıyordu)
- ✅ **ADR yazımı** — ADR-001 data-access ✅, ADR-002 → ADR-005 dashboard-architecture ✅, ADR-003 role-model ✅, ADR-004 skill-design ✅
- ✅ **CSV İndir butonu** (commit `143e1d9`)
- ✅ **Plan 03/04/06.B arşivle**
- ✅ **M-13 sub-nav** işaretle

### Mimari tutarsızlıklar — düzeltilenler (21 Nisan denetimi)
- ✅ User.Roles CSV + UserRole ikili sistem → Migration 19 + drop
- ✅ DashboardHtml dual storage → M-05 Faz C (`0f73478`)
- ✅ AsNoTracking eksikleri → 4 Mayıs sweep
- ✅ ex.Message → user'a JSON dönme (AdminController.Filters.cs:158, vb.)
- ✅ IsDashboard ölü property silindi
- ✅ CSS eski class'lar (form-card/btn-brand) → M-13 Plan 03 R2 sonrası temiz
- ✅ [ValidateAntiForgeryToken] tüm POST'larda
- ✅ async void hiç yok

### İlk dönem yapılanlar (özet)
Kullanıcı tablosu + PBKDF2, raporlar liste/çalıştırma ayrımı, Excel export, parametre üretici, ortak `_AppLayout`, navbar/footer sticky, Türkçe normalize, admin user CRUD, profil, rol checkbox, dashboard canlı veri, sticky table header, server-side filtreleme, Logs ayrılması, AuditLog merkezi servis, otomatik testler (PasswordHasher + AuditLog), manuel smoke testler.

---

## ARŞİV — Eski Tartışma Notları

### SP MIMARISI TARTIŞMASI (21 Nisan 2026)
**Kararlar plans/'a evrildi:**
- Karar 1 (SP'den vazgeçme?) → **Hibrit, SP kal.** ADR-001 (`docs/ADR/001-data-access.md`) yazıldı: rapor/dashboard data = SP, app metadata = EF Core.
- Karar 2 (sp_PdksPano parçalama) → **Inline TVF + orkestrator SP.** Aktif madde olarak yukarıda "SP mimarisi · sp_PdksPano → inline TVF refactor" başlığında. ADR-004 adayı.

Detay analiz (kazanımlar/dikkat/anti-pattern listesi) gerekirse git history'de `TODO.md` 21 Nisan revizyonuna bak.

### BUG: SP Önizle handler bağlı değil (21 Nisan 2026)
✅ **Çözüldü** — F-01 / commit `07f4b91`. `initSpHelpers()` outer IIFE'den çıkarıldı, top-level IIFE oldu.

### MIMARI TUTARSIZLIKLAR audit (21 Nisan 2026)
Çoğu kapandı (yukarıda). Kalan düşük-öncelik maddeler "BACKLOG → Mimari tutarsızlıklar" bölümünde.
