---
name: vnext-entity-port
description: Plan 17+ vNext modüllerinin (Tamim, HR, Doküman, Calendar, KPI, Audit, Approval, Form, Sözleşme) D:/Dev kaynak projelerinden Mosaik'e port edilmesini standart şablonlarla yapar. Entity → EF migration → Controller (CRUD) → ViewModel → Views (Index/Edit/Details) iskelet üretir.
---

# vNext Entity Port

## Ne zaman tetiklenir

- "Plan 17 (Tamim) implement et"
- "tamim/ klasöründen Mosaik'e port"
- "X entity'sini Mosaik EF'e ekle + admin CRUD"
- vNext modül roadmap (`plans/16-vnext-module-roadmap.md`) sırasındaki herhangi bir modül implement aşaması

## Önkoşullar

1. **Plan 16.5 Faz A+B tamamlanmış** — `Mosaik.Core` shared kit (BaseEntity, ServiceResult, Lookup, IUserDataScope) hazır
2. **Kaynak klasör keşfi** yapılmış (örn. tamim/ Prisma schema okunmuş, entity haritası çıkarılmış)
3. **Plan dosyası** ilgili modül için yazılmış (`plans/17-tamim.md` vs.)
4. **Mosaik build yeşil** — kırık dosya/test üstüne ekleme yok

## Standart adımlar (her modül için)

### 1. Entity tasarımı
- Kaynak şema (Prisma/EF/SQL) → Mosaik EF entity
- `Mosaik.Core.BaseEntity` inherit (Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
- `[BindNever]` PK + `CreatedAt` + `UpdatedAt` (mass assignment koruması, M-07)
- Türkçe UI label property'leri ayrı ViewModel'e (entity Türkçe olmaz)
- Nullability disiplini (`<Nullable>enable</Nullable>` zaten var)

### 2. EF migration
- `Mosaik/Database/NN_Create<Modul>.sql` (idempotent)
- `IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '<Tablo>') CREATE TABLE ...`
- FK constraint `WITH NOCHECK` ekle, sonra `CHECK` (büyük tablolarda lock azaltır)
- Default `GETUTCDATE()` (Migration 25 standartı)
- Audit trigger gerekirse ayrı dosya

### 3. Controller (CRUD partial)
- `AdminController.<Modul>.cs` partial class (eğer admin altıysa) veya `<Modul>Controller.cs` (kullanıcı tarafı)
- 5 action: `Index`, `Details(id)`, `Create [GET/POST]`, `Edit [GET/POST]`, `Delete [POST]`
- POST action: `[ValidateAntiForgeryToken]` zorunlu, route attribute, route-id'den explicit al (`User.UserId [BindNever]` örneği)
- ModelState.IsValid kontrolü her POST'ta
- Service katmanı: `<Modul>Service.cs`, `ServiceResult<T>` döner
- AuditLog: `EventType="<modul>_create/update/delete"`, before/after JSON

### 4. ViewModel
- `Mosaik/ViewModels/<Modul>FormViewModel.cs`
- Entity referansı **yasak** (M-07) — flat property'ler
- DataAnnotations validation
- `Message` + `MessageType` (string, default empty)

### 5. Views (modern CSS pattern)
- `Views/<Modul>/Index.cshtml` — list + filter + create button
- `Views/<Modul>/Details.cshtml` — read-only detail
- `Views/<Modul>/Create.cshtml` + `Edit.cshtml` — form
- Layout: `_AppLayout.cshtml`
- CSS: `.field` / `.lab` / `.inp` / `.btn` / `.btn.primary` / inline `var(--paper)` card (eski `form-card`/`btn-brand` SİLİNDİ)
- Form: raw `<form method="post">` + `@Html.AntiForgeryToken()` (Html.BeginForm değil)
- Türkçe metin UTF-8 (`Düzenle`, `İşlem` — ASCII'leştirme YASAK)
- Font Awesome 6 icon'lar (`fas fa-pen`, `fas fa-trash`)

### 6. Module registration (Plan 12)
- `AppModule` tablosuna entry (Modules sistemi DB-driven)
- Sidebar görünüm `IModuleService` cache invalidation

### 7. Test
- `Mosaik.Tests/<Modul>ServiceTests.cs` — EF InMemory + DefaultHttpContext
- Min 3 senaryo: create happy path, update with audit, delete with cascade
- 250+ test toplamı bozulmamalı

### 8. Build + smoke
- `dotnet build` 0 warning + 0 error
- `dotnet test` 100% pass
- Browser smoke: `http://localhost:5197/<Modul>` → CRUD tam çalışıyor mu

## Modül-specifik notlar (Plan 16'dan)

| Plan | Modül | Kaynak | Özel dikkat |
|---|---|---|---|
| 17 | Tamim | tamim/ Prisma | ReadStatus tablo eklenmeli (kaynakta yok), TaskItem.SourceEntity pattern |
| 18 | HR sync | hrrepo/ + hrreport/ | Lookup-based Phone/Position/Sube alanları, AD GetCurrentWindowsUserAsync port |
| 19 | Doküman | katalog/ + DikkatIQ AI | IPdfTextExtractor + AiSuggestion pattern (Plan 16.5 Faz D bağımlı) |
| 20 | KPI Widget | cashflow/ + Tower | Modül değil, dashboard widget tipi |
| 21 | Audit/Risk/DOF | BkmArgus/ | DerinSISBkm dependency BkmArgus'ta kalır, sadece entity port |
| 22 | Calendar | meet/ NLP | Türkçe NLP bonus, EF entity sıfırdan |
| 23 | Approval | esign/ UI + YonetIQ ApprovalRequest | StepOrder bazlı sıralı onay |
| 24 | Form Builder | from-scratch + FLYX referans | conditional_display pattern |
| 25 | Sözleşme | DikkatIQ/ full | AI Core (16.5) zorunlu önkoşul |

## Çıktı format şablonu

```markdown
## Plan NN — <Modul> Port Raporu

### Eklenen dosyalar
- `Models/<Entity>.cs` (X satır)
- `Database/NN_Create<Modul>.sql` (Y satır migration)
- `Controllers/<Controller>.cs` (Z action)
- `ViewModels/<Form>ViewModel.cs`
- `Views/<Modul>/{Index,Details,Create,Edit}.cshtml`
- `Services/<Service>.cs` (ServiceResult pattern)
- `Mosaik.Tests/<Modul>ServiceTests.cs` (W test)

### Build/test durumu
- Build: 0 hata, 0 warning ✓
- Test: 250 → 250+W, hepsi yeşil ✓

### Smoke test ipuçları
- /<modul> → list açılıyor mu
- Create → form çalışıyor, audit log düştü mü
- Edit → ModelState hata UI'da görünüyor mu
- Delete → cascade doğru mu

### Bilinen kısıtlamalar / TODO
- ...
```

## İlişkili

- `plans/16-vnext-module-roadmap.md` — modül sıralaması + reuse skoru
- `plans/16.5-shared-kit.md` — `Mosaik.Core` önkoşul
- `.claude/rules/csharp-conventions.md` — 300/500 satır eşiği, async, AsNoTracking
- `.claude/rules/razor-conventions.md` — modern CSS, raw form, antiforgery
- `.claude/rules/sql-conventions.md` — idempotent migration
- `.claude/rules/turkish-ui.md` — UTF-8, sözlük

## Tools

Read, Edit, Write, Glob, Grep, Bash (build + test), `mcp__sqlserver__*` (DB schema doğrulama), TodoWrite (faz tracking).
