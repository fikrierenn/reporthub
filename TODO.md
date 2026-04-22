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

#### FAZ 1 — KALAN
9. ⏳ **M-04 kismi · DashboardRenderer + UserDataFilter + UserRole sync unit tests** — DashboardRenderer XSS (9 test) ve UserDataFilterValidator (17 test) ✅ yazildi. Kalan: UserRole sync idempotency testi (AdminController.SyncUserRoles private method, refactor + test ayri is).
10. ✅ **dashboard-builder.js: spPreviewReady event listener + kolon datalist** — (commit sonradan doldurulacak). document.addEventListener('spPreviewReady'), populateColumnDatalist, attachListAttribute (compColumn, compLabelCol, ds-col, col-key). Browser dogrulandi: 7 result set -> 51 distinct kolon datalist'te.
11. **SP Onizle admin-override panel** (1-2h) — F-02 tamamlama. Admin default parametreleri override edebilsin (date picker, int input, vb.).
12. **M-02 devam** (30dk-1h) — AuthController + Services + diger controller'larda kalan ex.Message leak'ler (pre-commit hook yakalar, ayrica tara).
13. **M-03 Faz B · User.Roles nullable + [Obsolete]** (2h) — `15_NullableUserRolesCsv.sql` migration + model isaret. Faz C (kolonu drop) sonraki PR.
14. **UserRole sync idempotency testi** — `AdminController.SyncUserRoles`'u testable helper'a cikar (Services/UserRoleSyncService), EF in-memory + 3 test (yeni user, rol degisimi, remove all). Ayri commit, yaklasik 2h.
15. **dashboard-builder.js split** (Faz 2, 2h) — dosya 567 satir (yeni csharp-conventions 500 kirmizi cizgi kurali uygulanmali). Mantikli split: `dashboard-builder-core.js` (state + render + events) + `dashboard-builder-forms.js` (component forms + validators). JS'de de ayni 500 satir kurali.

#### FAZ 2 — BU AY (4 hafta, orta oncelik)
14. **M-01 · AdminController service extraction** (2 gun) — UserManagementService, ReportManagementService, DataSourceService. Controller endpoint'e inisin, 1736 → ~400 satir.
15. **G-04 · Audit log genisletme** (2h) — datasource delete, category delete, role delete icin `_auditLog.LogAsync`. OldValuesJson snapshot.
16. **G-05 · Cookie HttpOnly/Secure/SameSite/ExpireTimeSpan** (30dk) — Program.cs:27.
17. **G-06 · TestController [Authorize(Roles="admin")]** (15dk) — class-level + POST'a [ValidateAntiForgeryToken].
18. **M-05 · DashboardHtml legacy retirement** (1 gun) — kolonu archive tablo'ya tasi, DashboardConfigJson mandatory, Form'dan DashboardHtml input kaldir.
19. **F-03 · dashboard-builder.js memory leak** (1h) — event delegation veya AbortController. Drag-drop listener re-attach sorunu.
20. ✅ **F-04 · AGENT.md yaniltici icerik** — commit `7a7b81d` (silindi).
21. **SP mimarisi · sp_PdksPano → inline TVF refactor** (3h) — `fn_PdksDetay`, `fn_PdksKpiOzet`, `fn_PdksDepartmanKirilim` + orkestrator SP. ADR-004.
22. **Dashboard canli onizleme iframe** (4h) — builder'da gercek render preview.
23. **User P1 · Admin listesi arama + filtre + son giris** (1 gun) — admin user tab'a arama kutusu, rol/aktif/AD filtresi, LastLoginAt gosterimi.
24. **User P1 · User modeline Phone/Department/Position** (4h) — migration 16 + form alanlari.
25. **M-03 Faz C · User.Roles kolon drop** (30dk-1h) — `16_DropUserRolesCsv.sql` + model field sil. Faz B'den sonra, veri validation sonra.
26. **ReportCatalog.AllowedRoles CSV deprecate** (1 gun) — ADR-004 adayi. ReportAllowedRole junction birincil, CSV kaldir.

#### FAZ 3 — BU CEYREK (3 ay, dusuk oncelik / temizlik)
27. **M-06 · EF Core Migrations gecisi** (1 gun) — mevcut semayi baseline yap, yeni degisiklikler migration. Database/legacy/ olustur.
28. **M-07 · ViewModel BindNever + DTO pattern** (4h) — mass assignment riski. UserId, PasswordHash gibi kritik alanlar bind edilmesin.
29. **M-08 · Async/await tutarlilik** (2h) — AdminController.cs:1087-1091 vb. `.ToList()` → `.ToListAsync()`.
30. **M-09 · AsNoTracking tutarlilik** (2h) — 15+ read-only query.
31. **F-05 · Turkce UTF-8 normalize** (3h) — `turkish-ui-normalizer` skill'i ile tum "Duzenle"/"Bilesen" → "Düzenle"/"Bileşen".
32. **F-06 · CSP politikasi** (1 gun) — opsiyonel; inline onclick/script temizle, header ekle.
33. **Test coverage %30 hedefi** (1 hafta) — AdminController integration, ReportsController.Run, Admin SpPreview, PasswordHasher edge cases.
34. **G-07 · Dashboard iframe policy sikisitirma** (30dk) — Referrer-policy + sandbox kombinasyonu gozden gecir.
35. **G-08 · DashboardRenderer JSON escape regresyon testi** (1h) — `</script>`, `<!--`, case-insensitive bypass test.
36. **ADR yazimi** (1h) — ADR-001 data-access, ADR-002 dashboard-architecture. (ADR-003 role-model ✅ yazildi bugun.)

#### Toplam efor tahmini
- Faz 0 (bugun): 3 saat — blocker'lari kaldir
- Faz 1 (hafta): ~5 gun dagitilmis
- Faz 2 (ay): ~10 gun dagitilmis
- Faz 3 (ceyrek): ~15 gun dagitilmis

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
- [ ] **DashboardHtml vs DashboardConfigJson dual storage**: Hangisi source of truth belirsiz, ikisi de doldurulabiliyor. **Coz:** DashboardConfigJson'u birincil yap, DashboardHtml'i legacy isaretle veya kaldir. Migration script hazirla.
  - ReportPanel/Models/ReportCatalog.cs (iki kolon)
  - Database/10_AddDashboardColumns.sql + 11_AddDashboardConfigJson.sql
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
- [ ] **Legacy DashboardHtml path**: ReportCatalog.DashboardHtml + RenderDashboardTemplate kullaniliyor mu? Kullanilmiyorsa kolonu ve code path'i kaldir (ReportsController.cs:247-248).
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
