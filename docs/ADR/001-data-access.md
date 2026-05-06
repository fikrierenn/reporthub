# ADR-001 · Veri erişimi: SP rapor/dashboard, EF Core metadata (hibrit)

- **Durum:** Kabul edildi (4 Mayıs 2026, kayıt; pratik 2025'ten beri uygulanıyor)
- **Etkilenen:** `ReportsController` (SP yürütme), `DashboardRenderer`, tüm `*ManagementService`'ler (EF), `ReportPanelContext`, `Database/sp_*.sql`
- **İlgili TODO:** TODO.md "SP MIMARISI TARTISMASI" özeti (21 Nisan 2026 gecesi tartışıldı, karar dosyaya geçirilmedi — bu ADR o eksiği kapatır)

## Bağlam

Erken aşamada iki veri-erişim aracı yan yana büyüdü:

1. **EF Core 10** — kullanıcı/rol/rapor metadata, kategori, favori, audit log, user data filter (`ReportPanelContext`).
2. **Stored Procedure (ADO.NET, multi-result-set)** — rapor verisi ve dashboard kaynakları (`sp_PdksPano`, `sp_SatisPano`). `ReportsController.ExecuteStoredProcedureMultiResultSets` üzerinden `SqlCommand` + `SqlDataReader` ile ham `Dictionary<string, object>` listelerine okunur.

İki yıllık çalışma sırasında "her şey EF'e mi, her şey SP'ye mi, Dapper ekleyelim mi?" soruları periyodik olarak gündeme geldi (özellikle 21 Nisan 2026 gecesi: "Her şeyi SP yapmaktan vaz mı geçsem?", "monolitik SP'leri parçalayalım mı?"). Karar konuşulmuş ama hiçbir resmî yere yazılmamıştı; yeni gelen kişi (veya 6 ay sonra Claude) bağlamı yeniden inşa etmek zorunda kalıyor.

**Mevcut sorunlar (karar yazılmadığı için):**

- `Views/Auth/AGENT.md` (silindi 22 Nisan, `7a7b81d`) "Razor Pages + Dapper" manifestosu içeriyordu — yanlış mimari iddiası, Claude oturumlarını yanıltıyordu.
- Yeni rapor eklerken "EF FromSqlRaw mu, SP mi?" tereddütü her seferinde yeniden çözülüyor.
- Dashboard bileşenleri için "SP yerine view oluşturalım mı?" tartışması bir önceki oturumda da çıktı (22 Nisan, sp_PdksPano modülerleştirme).

## Karar

**Hibrit, tek bir kural ile:**

- **Rapor + dashboard verisi → Stored Procedure.** `ADO.NET` üzerinden `CommandType.StoredProcedure` + `SqlParameter`. EF Core'un `FromSqlRaw` / `FromSqlInterpolated` yolları **kullanılmaz** (rapor projection'ı keyless entity mapping ile uyuşmaz, multi-result-set EF tarafında awkward). Helper: `ReportsController.ExecuteStoredProcedureMultiResultSets` (ileride `Services/IStoredProcedureExecutor.cs`'e taşınacak — TODO M-01 ile başlandı).
- **Uygulama metadata'sı → EF Core 10.** `User`, `Role`, `UserRole`, `ReportCatalog`, `ReportAllowedRole`, `ReportCategory`, `ReportCategoryLink`, `ReportFavorite`, `UserDataFilter`, `AuditLog`, `DataSource`. CRUD `*ManagementService`'lerde, read query'lerde `.AsNoTracking()`, write'larda `SaveChangesAsync()`.
- **Dapper kullanılmaz.** Solo geliştirici için ekstra katman (Dapper SP'leri çağırmak için tasarlanmıştı; ADO.NET helper'ı projeye yetiyor, multi-result-set zaten elle dispatch ediliyor).
- **Dashboard SP modülerleştirme:** monolitik SP'ler reuse edilebilir parçalar gerekirse **inline Table-Valued Function (`fn_*`)** ile parçalanır; orkestrator SP `SELECT * FROM fn_*` ile result set'leri toplar. Multi-statement TVF **yasak** (optimizer killer). Ayrıntı: TODO FAZ 2 madde 21 (sp_PdksPano refactor, ADR-004 adayıydı; karar bu ADR'de konsolide).

## Alternatifler

- **(A) EF Core her şey için** — `FromSqlInterpolated` ile SQL'i kodda yaz, SP'leri sil. Karmaşık aggregation/CTE/window function path'leri için EF projection awkward; multi-result-set desteği zayıf; SP üzerindeki DBA operasyonel esnekliği (production'da dakikalar içinde fix, deploy yok) kaybolur. **Red.**
- **(B) Tüm raporları view'a çevir** — view parametresiz, parametre gerekirse TVF'e düşmek gerekir, zaten orkestratör SP gerekecek. Yarım çözüm. **Red.**
- **(C) Dapper ekle** — hibrit ekosistem, ama ADO.NET helper zaten yetiyor; Dapper'ın "SQL → POCO" özelliği bizim multi-result-set + dynamic-shape (`Dictionary<string, object>`) ihtiyacımıza uymuyor. Ekstra dependency, yeni geliştirici onboarding yükü. **Red.**
- **(D) Hibrit, SP rapor + EF metadata (seçilen)** — bu ADR. Pratiğin kayıt altına alınması.
- **(E) Her KPI için ayrı SP** (`sp_Kpi_ToplamPersonel`, vb.) — her dashboard için 6+ round-trip, performans patlaması. SP fonksiyon gibi davranamaz (`FROM sp_*` mümkün değil). **Red.** Bunun yerine inline TVF reuse pattern'i (yukarıda).

## Sonuçlar

**Olumlu:**
- DBA / operasyonel esneklik korunur (SP production'da anlık değiştirilebilir, deploy yok).
- Karmaşık aggregation SQL'de en verimli yerde yazılır.
- Metadata + audit log için EF Core'un migration/tracking/DI desteğinden faydalanılır.
- Yeni rapor eklerken net karar: "rapor verisi mi metadata mı?" → SP / EF.
- Reuse: ortak KPI'lar `fn_*` inline TVF'lerden çağrılır, kod duplikasyonu sıfır.

**Olumsuz / dikkat:**
- İki erişim modeli => iki test stratejisi (EF in-memory mümkün, SP integration test gerek). Test coverage <%10 sorunu (TODO Faz 3 madde 35) bu hibrit yapıdan bağımsız ama kompleksite katar.
- SP'ler `Database/*.sql` dosyalarında; migration disiplini elle (M-06 EF Core Migrations geçişi, FAZ 3 madde 29).
- `string procName` admin-input'undan EF tarafından enforce edilen güvenlik yok — `ReportCatalog.ProcName` whitelist + admin-only yazma + `[ValidateAntiForgeryToken]` ile kapatılır (security-principles.md madde 1).
- `ADO.NET` helper hâlâ `ReportsController` içinde (1736 satır → service split sonrası daha az). M-01 tamamlandığında `IStoredProcedureExecutor` olarak DI'a alınacak.

## Referanslar

- `CLAUDE.md` § 1 Tech stack
- `.claude/rules/architecture.md` → "Veri Erişim"
- `.claude/rules/sql-conventions.md` → SP şablonu, inline TVF kuralı
- `.claude/rules/csharp-conventions.md` → SP yürütme şablonu
- `TODO.md` → "SP MIMARISI TARTISMASI" (21 Nisan 2026, karar bu ADR'de yazılı)
- ADR-005 (dashboard-architecture) — config-driven JSON path, bu ADR'de SP rolünü belirledikten sonra dashboard render'ın işleyişi
- TODO FAZ 2 madde 21 — sp_PdksPano → inline TVF refactor (bu ADR'in pratik uygulaması)
