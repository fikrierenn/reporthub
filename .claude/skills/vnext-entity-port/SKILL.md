---
name: vnext-entity-port
description: Plan 17+ vNext modüllerinin (Tamim, HR, Doküman, Calendar, KPI, Audit, Approval, Form, Sözleşme) Mosaik'e MODÜLER MONOLİT pattern'iyle (Plan 16.6) port edilmesini standart şablonlarla yapar. Her modül = ayrı csproj + IMosaikModule self-register. Entity → EF migration → Controller → ViewModel → Views (Index/Edit/Details) iskelet üretir.
---

# vNext Entity Port (Modular Monolith)

## Ne zaman tetiklenir

- "Plan 17 (Tamim) implement et"
- "tamim/ klasöründen Mosaik'e port"
- "X modülü için iskelet kur"
- vNext modül roadmap (`plans/16-vnext-module-roadmap.md`) sırasındaki herhangi bir modül

## Önkoşullar (her zaman)

1. **Plan 16.5 Faz A+B tamamlanmış** — Mosaik.Core domain primitives + Lookup + IUserDataScope (commit `0394220`)
2. **Plan 16.6 tamamlanmış** — Mosaik.Core ayrı csproj + IMosaikModule + ModuleLoader (commit `5fd7330`)
3. **Kaynak klasör keşfi** yapılmış (örn. tamim/ Prisma schema okunmuş, entity haritası çıkarılmış)
4. **Plan dosyası** ilgili modül için yazılmış (`plans/17-tamim.md` vs.)
5. **Mosaik build yeşil** — kırık dosya/test üstüne ekleme yok

## Modül anatomisi (Plan 16.6 pattern)

```
Mosaik.Modules.<X>/                    ← yeni csproj, .NET 10 RCL (Razor Class Library)
├── Mosaik.Modules.<X>.csproj          ← FrameworkReference Microsoft.AspNetCore.App, ProjectReference Mosaik.Core
├── <X>Module.cs                        ← IMosaikModule impl (ConfigureServices, ConfigureModelBuilder, MigrationFolder, MapEndpoints)
├── Models/                             ← entity'ler (BaseEntity inherit, [BindNever] PK)
│   └── <X>.cs
├── ViewModels/                         ← form ViewModel'leri (entity referansı YASAK, M-07)
├── Controllers/                        ← Areas pattern: [Area("<X>")]
│   └── <X>Controller.cs
├── Views/                              ← Razor RCL (host'a gömülü)
│   └── <X>/
│       ├── Index.cshtml
│       ├── Details.cshtml
│       ├── Create.cshtml
│       └── Edit.cshtml
├── Services/                           ← modül-içi servisler (DbContext bağımlı olabilir)
│   └── <X>Service.cs
└── Database/                           ← modül-içi migration'lar
    ├── 01_Create<X>Tables.sql
    └── 02_Seed<X>.sql
```

**Csproj template:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Mosaik.Modules.<X></RootNamespace>
    <AssemblyName>Mosaik.Modules.<X></AssemblyName>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\Mosaik.Core\Mosaik.Core.csproj" />
  </ItemGroup>
</Project>
```

## Standart adımlar (her modül için)

### 1. Plan + onay
- `plans/<NN>-<modül>.md` yazılı + onaylı (Tier 3 zorunlu, ADR-010)
- Modül scope (Faz A iskelet / B entity+CRUD / C iş mantığı / D entegrasyon) ayrılı

### 2. Modül csproj
- `Mosaik.Modules.<X>/<X>.csproj` (RCL, Mosaik.Core ProjectReference)
- `Mosaik.sln`'e ekle (`dotnet sln add`)
- Mosaik.csproj'a ProjectReference (host modülü yükler)

### 3. IMosaikModule impl
```csharp
public class <X>Module : IMosaikModule
{
    public string ModuleKey => "<modul-key>";    // Plan 12 AppModule.ModuleKey
    public string DisplayName => "<Tablo Adı>";
    public string? Icon => "fas fa-<icon>";
    public int DisplayOrder => 100;              // sidebar sırası
    public string? MigrationFolder => "Database";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<I<X>Service, <X>Service>();
    }

    public void ConfigureModelBuilder(ModelBuilder mb)
    {
        mb.Entity<<X>>(e => { /* HasKey, MaxLength, HasIndex, FK */ });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAreaControllerRoute(
            name: "<modul-key>",
            areaName: "<X>",
            pattern: "<X>/{controller=Home}/{action=Index}/{id?}");
    }
}
```

### 4. Entity tasarımı
- `Models/<X>.cs` — `Mosaik.Core.Domain.BaseEntity` inherit (CreatedAt/UpdatedAt/CreatedBy/UpdatedBy)
- `[BindNever]` PK + timestamp (M-07 mass assignment koruması)
- Türkçe UI label property'leri ayrı ViewModel'e (entity Türkçe olmaz)
- Nullability disiplini (`<Nullable>enable</Nullable>`)
- vNext entity'leri MosaikContext'e DbSet eklenir mi? **HAYIR** — `ConfigureModelBuilder` yeterli, EF DbSet generic `Set<T>()` ile erişilir. Veya modül kendi DbContext'ini kullanır (gelişmiş, `IMosaikDbContext` gerekirse).

### 5. EF migration (modül-içi)
- `<X>/Database/01_Create<X>Tables.sql` (idempotent)
- `IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '<Tablo>') CREATE TABLE ...`
- Constraint adı pattern: `PK_<Tablo>`, `FK_<Cocuk>_<Ata>`, `DF_<Tablo>_<Kolon>`, `IX_<Tablo>_<Kolon>`
- `GETUTCDATE()` (Migration 25 standartı), `DATETIME2(0)`, `NVARCHAR` Türkçe
- Tablo adı **çakışma riski:** `Tamim`, `TamimBlock` gibi ana modül DB'sinde olmayan adlar
- `sql-migration-writer` skill kurallarına uy

### 6. Controller (Areas pattern)
- `Controllers/<X>Controller.cs` partial class değil — modül izole
- `[Area("<X>")]` zorunlu
- 5 action: `Index`, `Details(id)`, `Create [GET/POST]`, `Edit [GET/POST]`, `Delete [POST]`
- POST: `[ValidateAntiForgeryToken]` + route attribute, route-id'den explicit al
- ModelState.IsValid kontrolü her POST'ta
- Service katmanı: `<X>Service`, `ServiceResult<T>` döner (Mosaik.Core.Domain.ServiceResult)
- AuditLog: `EventType="<modul>_create/update/delete"`, before/after JSON

### 7. ViewModel
- `ViewModels/<X>FormViewModel.cs`
- Entity referansı **yasak** (M-07)
- DataAnnotations validation
- `Message` + `MessageType` (string, default empty)

### 8. Views (modern CSS pattern)
- `Views/<X>/{Index,Details,Create,Edit}.cshtml`
- Layout: `Views/_ViewStart.cshtml` `Layout = "_AppLayout";` (host layout'unu kullanır)
- CSS: `.field` / `.lab` / `.inp` / `.btn` / `.btn.primary` / inline `var(--paper)` card (eski `form-card`/`btn-brand` SİLİNDİ)
- Form: raw `<form method="post">` + `@Html.AntiForgeryToken()` (Html.BeginForm değil)
- Türkçe metin UTF-8 (`Düzenle`, `İşlem` — ASCII'leştirme YASAK)
- Font Awesome 6 icon'lar (`fas fa-pen`, `fas fa-trash`)

### 9. Mosaik.sln + Mosaik.csproj wiring
```bash
dotnet sln Mosaik.sln add Mosaik.Modules.<X>/Mosaik.Modules.<X>.csproj
```
Mosaik.csproj'a:
```xml
<ProjectReference Include="..\Mosaik.Modules.<X>\Mosaik.Modules.<X>.csproj" />
```
ModuleLoader assembly tarama otomatik bulur — extra config gereksiz.

### 10. AppModule seed (Plan 12 + 16.6)
Migration script'inde veya manuel admin GUI'den:
```sql
IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = '<modul-key>')
INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, AssemblyName, ModuleType)
VALUES ('<modul-key>', '<Görünen Ad>', 1, 100, 'Mosaik.Modules.<X>', 'extension');
```

### 11. Test
- Modül-içi test gerekiyorsa: `Mosaik.Modules.<X>.Tests/` (opsiyonel csproj)
- Veya `Mosaik.Tests/<X>ServiceTests.cs` (mevcut test projesinde, basit modüller için)
- EF InMemory + DefaultHttpContext pattern

### 12. Build + smoke
- `dotnet build` 0 warning + 0 error
- `dotnet test` 100% pass
- Browser smoke: `http://localhost:5197/<X>` → CRUD tam çalışıyor mu
- Sidebar'da DisplayName görünüyor mu (Plan 12 AppModule)

## Modül-specifik notlar (Plan 16'dan)

| Plan | Modül | Kaynak | Özel dikkat |
|---|---|---|---|
| 17 | Tamim | tamim/ Prisma | **İlk modül = referans implementation.** ReadLog tablo eklenmeli (kaynakta yok), TaskItem.SourceEntity pattern (YonetIQ) |
| 18A | IK Quick Reports | Zirve `vw_PersonelDepartman` | Read-only, **modül değil** — Mosaik core'a SP+rapor olarak eklenir (hızlı kazanım) |
| 18B | HR Sync | hrrepo/ + hrreport/ | Lookup-based Phone/Position/Sube (Mosaik.Core.Lookup), AD GetCurrentWindowsUserAsync port |
| 19 | Doküman | katalog/ + DikkatIQ AI | IPdfTextExtractor + AiSuggestion pattern (Plan 16.5 Faz D bağımlı) |
| 20 | KPI Widget | cashflow/ + Tower | **Modül değil widget tipi** — Mosaik.Core dashboard'a yeni widget eklenir |
| 21 | Audit/Risk/DOF | BkmArgus/ | DerinSISBkm dependency BkmArgus'ta kalır, sadece entity port |
| 22 | Calendar | meet/ NLP | Türkçe NLP bonus, EF entity sıfırdan |
| 23 | Approval | esign/ UI + Mosaik.Core.Workflow.ApprovalRequest | StepOrder bazlı sıralı onay (mevcut altyapı) |
| 24 | Form Builder | from-scratch + FLYX referans | conditional_display pattern |
| 25 | Sözleşme | DikkatIQ/ full | AI Core (16.5) zorunlu önkoşul |

## Çıktı format şablonu

```markdown
## Plan NN — <Modul> Port Raporu

### Eklenen csproj
- Mosaik.Modules.<X>/Mosaik.Modules.<X>.csproj
- ProjectReference: Mosaik.Core
- Mosaik.csproj + Mosaik.sln'e eklendi

### Eklenen dosyalar
- <X>Module.cs (IMosaikModule impl, X satır)
- Models/<X>.cs (entity, X satır)
- Database/01_Create<X>Tables.sql (Y satır migration)
- Controllers/<X>Controller.cs (Z action)
- ViewModels/<X>FormViewModel.cs
- Views/<X>/{Index,Details,Create,Edit}.cshtml
- Services/<X>Service.cs (ServiceResult pattern)
- (opsiyonel) Mosaik.Modules.<X>.Tests/<X>ServiceTests.cs (W test)

### Build/test durumu
- Build: 0 hata, 0 warning ✓
- Test: önceki sayım → önceki+W, hepsi yeşil ✓

### Smoke test ipuçları
- /<modul-key> → list açılıyor mu
- Sidebar DisplayName görünüyor mu (AppModule)
- Create → form çalışıyor, audit log düştü mü
- Edit → ModelState hata UI'da görünüyor mu
- Delete → cascade doğru mu
- ModuleLoader.DiscoverModules() → modül liste içinde

### Bilinen kısıtlamalar / TODO
- ...
```

## Yaygın hatalar (KAÇIN)

- ❌ Modül-içi başka modüle ProjectReference (cross-modül izolasyon ihlali). Cross-modül iletişim yalnızca Mosaik.Core üzerinden.
- ❌ MosaikContext'e modül DbSet ekleme — `ConfigureModelBuilder` yeterli, modül `Set<T>()` ile erişir
- ❌ Modül migration'ında host migration adlandırması (`35_*.sql` host'ta, modülde `01_*.sql`)
- ❌ Sidebar AppModule seed'i unutmak — modül load edilir ama görünmez
- ❌ Areas attribute'unu unutmak — route eşleşmez
- ❌ Mosaik.csproj ProjectReference unutmak — modül DLL'i bin/'de olmaz, ModuleLoader bulamaz

## İlişkili

- `plans/16-vnext-module-roadmap.md` — modül sıralaması + reuse skoru
- `plans/16.5-shared-kit.md` — Mosaik.Core önkoşul
- `plans/16.6-module-extension-architecture.md` — Modular Monolith mimarisi
- `.claude/rules/csharp-conventions.md` — 300/500 satır eşiği, async, AsNoTracking
- `.claude/rules/razor-conventions.md` — modern CSS, raw form, antiforgery
- `.claude/rules/sql-conventions.md` — idempotent migration
- `.claude/rules/turkish-ui.md` — UTF-8, sözlük
- `.claude/skills/sql-migration-writer/SKILL.md` — migration template kütüphanesi
- `.claude/skills/bkm-db-explorer/SKILL.md` — kaynak DB keşif (HR/IK/Bordro modülleri)

## Tools

Read, Edit, Write, Glob, Grep, Bash (build + test), `mcp__sqlserver__*` (DB schema doğrulama), TodoWrite (faz tracking).
