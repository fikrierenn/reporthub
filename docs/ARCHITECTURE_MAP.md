# Mosaik — Mimari Haritası (Single Source of Truth)

> **Bu dosya her büyük değişiklik öncesi OKUNMALI.**
> Silme / rename / refactor öncesi: hangi route hangi view'a gidiyor, V1/V2/legacy var mı,
> hangi sidebar linki canonical entry point — hepsi burada.
> Tahmin etme. **Önce buraya bak.**

Son güncelleme: **2026-05-12**
Tutucu kural: Repo'da gerçekle eşleşmeyen satır görürsen, **kod yerine bu dosyayı güncelle** ya da
en azından "stale!" işareti ekle.

---

## 1. Stack & Konum

| Katman | Yer | Not |
|---|---|---|
| Web (host) | `Mosaik/` (cs proj) | ASP.NET Core MVC + Razor (Pages değil) |
| Core | `Mosaik.Core/` | Cross-modül contracts (Email, Notification, Workflow, Module, AI, Logging, …) |
| Module: Tamim | `Mosaik.Modules.Circular/` | ModuleKey `circular`, AssemblyName Mosaik.Modules.Tamim ile yüklenir |
| Test | `Mosaik.Tests/` | xUnit, 246+ test |
| DB | SQL Server `Mosaik` veritabanı | LocalDB değil — `Server=BT-FIKRI\SQLEXPRESS` |
| Migration | `Mosaik/Database/NN_*.sql` | sqlcli `script` ile uygulanır, EF Migrations YOK |
| Profil | `D:/Dev/reporthub/sqlcli.json` Profiles.mosaik | sqlcli'yi `dotnet run --project D:/Dev/sqlcli -- script <file>` ile çağır |

---

## 2. Sidebar → Route → View (canonical map)

Sidebar (Plan 23) 5 grup × N link. **Her satır = canonical entry point.** Direkt URL'lerden gelmeyin, sidebar'a bakın.

### Ana
| Sidebar | URL | Controller#Action | View |
|---|---|---|---|
| Genel Bakış | `/Dashboard` | `DashboardController#Index` | `Views/Dashboard/Index.cshtml` |

### Çalışma Alanı (DB-driven, AppModules.GroupKey='workspace')
| Sidebar | URL | Controller#Action | View | Not |
|---|---|---|---|---|
| Raporlar | `/Reports` | `ReportsController#Index` | `Views/Reports/Index.cshtml` | |
| Takvim | `/Calendar` | `CalendarController#Index` | `Views/Calendar/Index.cshtml` | |
| Dokümanlar | `/Documents` | `DocumentsController#Index` | `Views/Documents/Index.cshtml` | |
| Tamim & Sirküler | `/Circular/Circular` | `Mosaik.Modules.Circular.CircularController#Index` | `Areas/Circular/Views/Circular/Index.cshtml` | Area route — Tamim modülü |
| (atlanır) | `/Dashboard` çakışma | "Panolar" modülü sidebar render'da SKIP edilir (Genel Bakış zaten Dashboard) | — | |

### Sözleşmeler & Uyum (GroupKey='contracts')
| Sidebar | URL | Controller#Action | View |
|---|---|---|---|
| Uyum | `/Compliance` | `ComplianceController#Index` | `Views/Compliance/Index.cshtml` |
| Sözleşmeler | `/Contracts` | `ContractsController#Index` | `Views/Contracts/Index.cshtml` |
| Yükümlülükler | `/Obligations` | `ObligationsController#Index` | `Views/Obligations/Index.cshtml` |

### Yapı & Yapay Zeka (GroupKey='structure')
| Sidebar | URL | Controller#Action | View |
|---|---|---|---|
| AI Analiz | `/Ai` | `AiController#Index` | `Views/Ai/Index.cshtml` |
| Organizasyon | `/OrgChart` | `OrgChartController#Index` | `Views/OrgChart/Index.cshtml` |

### Sistem (admin-only, sidebar Plan 23 alt-section'larla bölünmüş)
| Sidebar | URL | Controller#Action | View | Not |
|---|---|---|---|---|
| **Yönetim Paneli** (default) | `/Admin` | `AdminController#Index` (tab=overview) | `Views/Admin/Index.cshtml` → `_AdminOverview` partial | overview dashboard |
| _İçerik & Veri_ | | | | |
| Veri Kaynakları | `/Admin?tab=datasources` | `AdminController#Index` | `_AdminTabDataSources` partial | |
| Rapor Kataloğu | `/Admin?tab=reports` | `AdminController#Index` | `_AdminTabReports` partial | "Yeni" linki **V2**'ye |
| Filtreler | `/Admin?tab=filters` | `AdminController#Index` | `_AdminTabFilters` partial | |
| Lookup | `/Admin/Lookup` | `AdminController#Lookup` | `Views/Admin/Lookup.cshtml` | |
| _Erişim & Yetki_ | | | | |
| Kullanıcılar | `/Admin?tab=users` | `AdminController#Index` | `_AdminTabUsers` partial | |
| Roller | `/Admin?tab=roles` | `AdminController#Index` | `_AdminTabRoles` partial | |
| Rapor Grupları | `/Admin?tab=groups` | `AdminController#Index` | `_AdminTabGroups` partial | |
| _Yapı_ | | | | |
| Organizasyon | `/Admin/OrgChart` | `AdminController#OrgChart` | `Views/Admin/OrgChart.cshtml` | (≠ public `/OrgChart`) |
| _Görünüm & Ayar_ | | | | |
| Marka Ayarları | `/Admin/BrandSettings` | `AdminController#BrandSettings` | `Views/Admin/BrandSettings.cshtml` | |
| Modüller | `/Admin/Modules` | `AdminController#Modules` | `Views/Admin/Modules.cshtml` | |
| AI Ayarları | `/Admin/AiSettings` | `AdminController#AiSettings` | `Views/Admin/AiSettings.cshtml` + `AiSettingsEdit.cshtml` |
| _İzleme_ | | | | |
| Geçmiş / Loglar | `/Logs` | `LogsController#Index` | `Views/Logs/Index.cshtml` | |
| Hangfire ↗ | `/hangfire` | (Hangfire Dashboard) | — | new tab |

### Avatar bölümü (sidebar alt)
| | URL | Not |
|---|---|---|
| Bildirimler | `/Notifications` | bell icon |
| Profil | `/Profile` | gear icon |
| Çıkış | `/Login?logout=1` | danger icon |

---

## 3. V1 vs V2 ve Deprecated Görünümler

Bu tablo **her silme/refactor öncesi kontrol edilmeli.** Sidebar/tablo/empty-state linkleri burada işaretli.

| Konu | Canonical (V2/yeni) | Deprecated/Eski | Durum |
|---|---|---|---|
| Rapor Düzenleyici | `CreateReportV2.cshtml` + `EditReportV2.cshtml` (URL: `/Admin/CreateReportV2`, `/Admin/EditReportV2/{id}`) | `CreateReport.cshtml` + `EditReport.cshtml` | **2026-05-12 silindi**. Eski URL'ler `CreateReportLegacyRedirect` / `EditReportLegacyRedirect` ile V2'ye 302 redirect. GET method'ları `AdminController.Reports.cs` içinde route'suz public kalır (V2 data load için). POST endpoint'ler (`POST /Admin/CreateReport`, `POST /Admin/EditReport/{id}`) **AKTİF**, V2 form'ları oraya submit ediyor. |
| Yönetim ana sayfa | `_AdminOverview.cshtml` partial (overview dashboard) | Yatay `<nav class="subnav">` + `_AdminSubnav.cshtml` | **2026-05-12 silindi**. Sidebar Sistem grubu navigasyon sağlar. |
| Tamim ana view | `Mosaik.Modules.Circular/Areas/Circular/Views/Circular/Index.cshtml` | `Mosaik/Views/Tamim/*` | Modül adı kod-içi `Circular` (EN), UI etiketi "Tamim" (TR). ModuleKey `circular`, AppModules.GroupKey `workspace`. |
| BrandService cache | `Singleton + Invalidate()` | DB direct read | Brand singleton, BrandSettings POST sonrası `IBrandService.Invalidate()` çağırır. |

**Kontrol komutu** (refactor öncesi): `grep -rn "<view-adı>" Mosaik/Views Mosaik/Controllers Mosaik.Modules.*/` → kaç yerden link veriliyor?

---

## 4. POST endpoint asimetrisi (önemli)

Bazı GET action'lar route'suz (V2'den çağrılır), POST action'ları route'lu (form submit). Karıştırma:

- `AdminController.Reports.cs`:
  - `CreateReport()` GET — **route YOK** (V2'den çağrılır)
  - `CreateReport(ReportCatalog)` POST — `[Route("Admin/CreateReport")]` **var**
  - `EditReport(int)` GET — **route YOK** (V2'den çağrılır)
  - `EditReport(int, ReportCatalog)` POST — `[Route("Admin/EditReport/{id}")]` **var**
  - `CreateReportLegacyRedirect` / `EditReportLegacyRedirect` — eski URL'leri V2'ye yönlendirir.

Bunları silersen V2 form submit kırılır.

---

## 5. Migration & DB

- **Migration runner:** sqlcli `script` komutu. EF Migrations YOK.
- **Yeni migration:** `Mosaik/Database/NN_<ad>.sql` — N+1 ile başla, idempotent (`IF NOT EXISTS`, `IF NOT EXISTS SELECT … FROM sys.columns`).
- **Migration log tablosu:** YOK — uygulama startup'ta run etmez. **Manuel** sqlcli ile çalıştırılır.
- **Mosaik DB:** `Server=BT-FIKRI\SQLEXPRESS;Database=Mosaik`. sqlcli profil `mosaik` default.
- **MCP allowlist:** master, DerinSIS*, BKMDATA, EncoreMerkez, BKM. **Mosaik DB MCP allowlist'te değil** — sqlcli kullan, MCP `mcp__sqlserver__*` çağırma.

Son migration numarası (2026-05-12): **56** (`56_AppModulesGroupKey.sql`).

---

## 6. Servis kayıtları (Program.cs)

| Servis | Lifetime | Not |
|---|---|---|
| `MosaikContext` | Scoped (AddDbContext) | EF |
| `IBrandService` / `IModuleService` | **Singleton** | DB cache, `Invalidate()` POST sonrası |
| `StoredProcedureExecutor` | Scoped | Multi-RS ADO.NET |
| `INotificationService` | Scoped | Cross-modül |
| `IEmailService` (Plan 31) | Scoped | Mosaik.Core.Email.IEmailService → SmtpEmailService. **Enabled=false default**, prod'da appsettings'e doğru SMTP girilmeli. |
| `AiPipelineQueue` | Singleton | Channel<int> |
| `IAuditLog` | Scoped (host AuditLogService impl) | |

---

## 7. AppModules (DB-driven sidebar listesi)

`mcp__sqlserver__*` Mosaik allowlist'te değil — **sqlcli ile sorgulanır**:
```bash
dotnet run --project D:/Dev/sqlcli -- query "SELECT ModuleKey, DisplayName, GroupKey, IsEnabled, ModuleType FROM dbo.AppModules ORDER BY SortOrder"
```

Mevcut (2026-05-12, 10 modül):
```
reports       Raporlar           workspace   core
dashboards    Panolar            workspace   core   (sidebar render'da SKIP — Dashboard hardcoded)
calendar      Takvim             workspace   extension
documents     Dokümanlar         workspace   core
compliance    Uyum               contracts   extension
ai            AI Analiz          structure   extension
contracts     Sözleşmeler        contracts   core
obligations   Yükümlülükler      contracts   core
orgchart      Organizasyon       structure   core
circular      Tamim & Sirküler   workspace   extension (Mosaik.Modules.Tamim assembly)
```

**Yeni modül eklerken:** Migration → seed (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType, **GroupKey**) → `_AppLayout.cshtml` `moduleIcons` dict'e ikon ekle → `ModuleUrl` switch'ine route mapping ekle (gerekiyorsa).

---

## 8. Aktif Plan'lar (durum)

| Plan | Konu | Durum |
|---|---|---|
| 23 | Sidebar + Overview Dashboard | ✅ Tamamlandı (2026-05-12) |
| 25.1 Faz 1 | Inline style refactor | ⏳ Devam — ~640 admin module'de |
| 31 | Email/SMTP altyapısı | ✅ Tamamlandı (2026-05-12) |
| 32 | Scheduled Reports + Email Distribution | 📝 Taslak — 6 açık soru, kullanıcı onayı bekliyor |
| 33 | Admin views standardizasyon | 📝 Taslak — V1 Builder silindi (kullanıcı kararı), 14 view kaldı |

Tüm plan'lar: `plans/NN-<slug>.md` (aktif), `plans/archive/` (tamamlanan).

---

## 9. Refactor / Silme öncesi ZORUNLU çek-listesi

Bir view/method/file silmeden veya rename etmeden önce **hepsini sırayla** yap:

1. **Bu dosyaya bak.** Canonical mı, deprecated mı?
2. **Referans tara:** `grep -rn "<isim>" Mosaik/ Mosaik.Core/ Mosaik.Modules.*/` (Views + Controllers + JS + CSS)
3. **Sidebar/Tablo/EmptyState link tara:** `_AppLayout.cshtml`, `_Admin*Tab*.cshtml`, `_EmptyState.cshtml`, Razor `href=` veya `asp-action=`
4. **Route conflict kontrol:** Eğer route attribute kaldırılıyorsa, başka method route'u yutuyor mu? (`[Route]` uniqueness)
5. **POST endpoint farkındalığı:** GET'i silmek POST'u kırabilir, POST'u silmek form submit kırar. **GET ≠ POST**.
6. **Build kontrol:** `dotnet build Mosaik/Mosaik.csproj --nologo` 0 hata 0 uyarı
7. **Smoke test:** preview start + manuel test (kritik path)
8. **Doc güncelle:** Bu dosyada satır ekle/güncelle ("X silindi 2026-MM-DD, V2'ye redirect").
9. **Commit mesajına net not:** "X silindi — Y canonical, eski URL Z'ye redirect"

**Bu adımlardan biri atlanırsa**, "V1 var sandım refactor ettim, V2 zaten varmış" tarzı kayıp oluşur.

---

## 10. Bilinen sapmalar (refactor sırasında dikkat)

- `Views/Admin/OrgChart.cshtml` — chart canvas + drag-drop hibrit, refactor zor (Plan 33.1 candidate)
- `Views/Admin/CreateReportV2.cshtml` + `EditReportV2.cshtml` — kendine özgü canvas/drawer UI, standard form pattern uygulanamaz (Plan 33 scope DIŞI)
- `Views/Auth/*` — login layout farklı (`Layout=null` veya `_AuthLayout`?), `_AppLayout` değil
- Anonim sayfalar (Login, Error) `_AppLayout` kullanmaz, sidebar/topbar render etmez.

---

## 11. Memory / Kural referansları

- `.claude/rules/architecture.md` — kalıcı mimari kural (bu dosyanın özeti)
- `.claude/rules/known-issues.md` — Kaspersky, AGENT.md vs
- `.claude/rules/session-protocol.md` — oturum başı ritüel (bu dosya artık 0. adım)
- `memory/project_architecture_map.md` — machine-local özet (auto-memory)
- `docs/MOSAIK_DESIGN_PROMPT.md` — UI standardı promptu (tasarım üretimi için)
