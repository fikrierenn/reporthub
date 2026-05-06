# ADR-006 · Uygulama kodu UTC, SQL default'ları legacy local — geçiş zamanla

- **Durum:** Kabul edildi (22 Nisan 2026)
- **Etkilenen:** Tüm `DateTime.Now` kullanımları, `DbContext` default'ları, SP'ler, ReportRunLog filtreleri, seed'ler.
- **İlgili TODO:** "DateTime.Now → DateTime.UtcNow sweep" (Faz 2 madde 28).

## Bağlam

Proje kısmen UTC'ye geçmiş durumda:

- **UTC kullananlar** — `UserManagementService`, `UserRoleSyncService`, `ReportManagementService`. Yeni kayıt yazarken `DateTime.UtcNow`.
- **Local kullananlar** — `AuthController`, `ProfileController`, `DashboardController`, `AuditLogService` ve tüm entity default property'leri (`public DateTime CreatedAt { get; set; } = DateTime.Now;`).
- **SQL DEFAULT'ları** — `Database/02_CreateTables.sql` ve `Migrations/*` dosyalarında 14+ `GETDATE()` (local). `03_SeedData.sql` seed'leri `GETDATE()` kullanıyor.

**Veri tarafı:**

- Server Windows TR/Istanbul (+3). `GETDATE() - GETUTCDATE() = 180 dk`.
- Eski satırlar local (2025-12-19T10:01:43Z kolonda — aslında local 10:01 TR, UTC 07:01 olarak olması gerekirken UTC olarak işaretlenmeden yazılmış, semantik "naive local").
- Yeni servislerden gelen kayıtlar UTC (semantik "naive UTC"). Aynı sütunda iki semantik yan yana.

**Sorun:**

- Karışık timezone veri: bir `WHERE CreatedAt >= DATEADD(DAY, -7, GETDATE())` sorgusu eski satırları doğru dönerken UTC yazılan yeni satırları ±3 saat kaydırır.
- Dashboard ve ReportRunLog filtreleri (örn. `DashboardController.cs:49` "bu ayki koşumlar") local gerekiyor ama `DateTime.Now` default ileride UTC'ye dönerse sessizce bozulur.
- `DateTime.Now` antipatter'ı pre-commit hook'ta bloklanıyor ama entity default'ları ve AuditLog yazımları hâlâ local.

## Karar

**Uygulama kodu tarafı UTC standardına taşınıyor; DB default'ları ve eski veriler geçiş/migration ile daha sonra hizalanacak.**

### Kural (yeni kod)

- **Uygulama kodu her yerde `DateTime.UtcNow`** — controller, service, entity property default, audit log yazımı.
- `DateTime.Now`, `DateTime.Today` **yeni kodda yasak** (pre-commit hook zaten blokluyor).
- **Filename / log prefix** gibi UI-görünür tarihler de UTC — admin farkeder ama ISO timestamp universal.

### Kural (legacy/SQL tarafı — kapsam dışı, follow-up)

- DB DEFAULT `GETDATE()` → `GETUTCDATE()` migration'ı **bu ADR'ın kapsamı dışı**. Ayrı migration + backup gerekir.
- Eski satırların timezone shift'i (naive-local → naive-UTC, `-180` dk) **bu ADR'ın kapsamı dışı**. Ayrı migration + veri doğrulama gerekir.
- SP'lerde `GETDATE()` kullanımı (ör. sp_PdksPano legacy tarih) **kapsam dışı**. SP refactor sırasında (TODO madde 21) eş zamanlı düzeltilecek.

### Geçiş süreci

| Faz | Kapsam | Durum |
|---|---|---|
| A — Yeni servisler UTC | UserManagementService, UserRoleSyncService, ReportManagementService | ✅ tamamlanmış (önceki PR'lar) |
| B — ReportHub ana dosyalar UTC | ReportCatalog:41, ReportsController:89/382/384 | ✅ commit `0f73478` (M-05 Faz C cleanup) |
| **C — Kalan app kodu UTC** | AuthController (4), ProfileController (1), DashboardController (1), AuditLogService (1), 11 model default | **bu ADR ile sweep** |
| D — DB DEFAULT'ları | 14+ `GETDATE()` → `GETUTCDATE()`, migration + data shift | ⏳ ayrı TODO, backup gerekir |
| E — SP ve seed'ler | sp_PdksPano vb., 03_SeedData.sql | ⏳ SP refactor (madde 21) ile |

### Display layer

- Razor view katmanı UTC → local dönüşümden sorumlu. Şu an projede `ToString("dd.MM.yyyy HH:mm")` çağrıları naive kabul ediyor; **view'larda UTC görüntülense de kullanıcı fark etmeyecek (dev ortamı + tek TZ)**. Follow-up: view helper `ToLocalDisplay()` extension.

### DashboardController.cs:49 özel vaka

```csharp
var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
```

UTC'ye çevrildiğinde ay başı UTC 00:00'da başlar. Ay sonu edge case: yerel 01:00'da ilk gün UTC'de hâlâ önceki ay son günü görünür. **Bu query ReportRunLog filtresi; ReportRunLog.RunAt DB DEFAULT `GETDATE()` yazıyor** → eski satırlar local, yeni C#-tarafı yazımlar UTC. Karışık veri üzerinde çalıştığı için edge case bugünden bozuk. Faz D migration'ı ile düzelecek.

## Alternatif düşünüldü

- **Her şeyi local'e geri çevir** — reddedildi. .NET best practice UTC. Yeni servisler zaten UTC. Geri dönüş büyük commit + bugları kalıcılaştırır.
- **Tüm migration'ı tek atomic PR** — reddedildi. DB backup + data shift + SP regresyon riski 2+ saat/ayrı bir iş. Tek PR'da yapmak compile-and-pray. Fazları bölmek daha güvenli.
- **UTC-aware entity base class (BaseEntity.CreatedUtc)** — ertelendi. Tüm entity'leri base'e taşımak büyük refactor, bu ADR'ın scope'u dışı.

## Sonuç

Faz C bu ADR ile yapılıyor. Faz D ve E ayrı TODO maddeleri, ayrı commit, ayrı backup.
