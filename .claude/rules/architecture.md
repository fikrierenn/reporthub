# Mimari Kurallar ve Bilinen Tutarsızlıklar

_Kapsam: Projenin genel mimarisi, karar kayıtları (ADR'lere dönüşene kadar özet burada)._

## Veri Erişim

- **Rapor/dashboard verisi** → SQL Server **stored procedure**. `CommandType.StoredProcedure` + `SqlParameter` zorunlu. String concat YOK.
- **Metadata CRUD** (User, Role, ReportCatalog, AuditLog, Category, Favorite, UserDataFilter) → **EF Core 10**, `ReportPanelContext`.
- **Dapper yok**, eklenmeyecek (tek geliştirici için gereksiz katman).
- **SP execution helper:** `ReportsController.ExecuteStoredProcedureMultiResultSets` — ileride `Services/IStoredProcedureExecutor.cs`'e taşınacak (TODO M-01 service extraction ile birlikte).

## Dashboard Mimarisi

- **Config-driven JSON.** `ReportCatalog.DashboardConfigJson` birincil. `DashboardHtml` (legacy) kaldırılacak (TODO M-05).
- **Renderer:** `Services/DashboardRenderer.cs` — statik, StringBuilder ile HTML + inline JS emits.
- **Güvenlik:** iframe sandbox (`allow-scripts` only, `allow-same-origin` yok) — XSS büyük ölçüde izole.
- **XSS önlemleri (hepsi uygulanmış):**
  - DOM API: `createElement` + `textContent` (asla `innerHTML`).
  - `eval()` **yasak** — `window.__RS[rs]` array indexing kullan.
  - `onclick` inline attribute yasak — `addEventListener` + closure.
  - `</script>` break-out case-insensitive regex ile, `<!--` de kaçırıldı.

## Auth + Yetkilendirme

- **Cookie auth** (ASP.NET Core default). JWT ekleme yok (multi-client değiliz).
- **Rol modeli:** `UserRole` junction tablosu **birincil**. `User.Roles` CSV kolonu **kaldırılıyor** (TODO M-03, migration 15). AuthController CSV fallback **kaldırılacak**.
- **`[Authorize(Roles="admin")]`** tüm admin controller'larda class-level.
- **`[ValidateAntiForgeryToken]`** POST action'larda zorunlu. (TestController exception — TODO G-06.)

## Bilinen Tutarsızlıklar (YÜKSEK risk)

1. **User.Roles CSV + UserRole tablo ikili sistem** — TODO M-03, bu hafta.
2. **ReportCatalog.DashboardHtml + DashboardConfigJson dual storage** — TODO M-05, bu ay.
3. **AdminController 1736 satır** — service layer yok. TODO M-01, bu ay.
4. **Exception handling stack trace sızdırıyor** (`ex.Message` user'a gösteriliyor) — TODO M-02, bu hafta.

## ORTA risk

- **Async/await tutarsız** — bazı GET action'larda `.ToList()` sync. TODO M-08.
- **`.AsNoTracking()` eksik** — 15+ read query. TODO M-09.
- **ViewModel entity direkt mapping** — mass assignment riski. TODO M-07.
- **Form syntax karışık** — `Html.BeginForm` vs raw `<form>`. Standart: `Html.BeginForm`.

## DÜŞÜK risk

- **AuditLog selektif** — datasource/category delete log'lanmıyor. TODO G-04.
- **CSS Tailwind utility + custom karışık** — PostCSS build pipeline ilerde.
- **DB script organizasyonu** — `Database/` alt klasörlere ayrılacak. TODO M-06.
- **Test coverage <%10** — öncelik: DashboardRenderer, UserDataFilter, UserRole sync.

## Kararlar (kronolojik — küçük notlar, büyük kararlar ADR'lere)

**22 Nisan 2026:**

- **Dev launch config `Development` zorunlu.** `dotnet run --no-launch-profile` Production default'a düşüyor → `appsettings.Development.json` (gitignored local SA şifresi) yüklenmez → DB connection kırılır. Çözüm: `.claude/launch.json` → `--launch-profile http` geç (launchSettings'teki profil `ASPNETCORE_ENVIRONMENT=Development` ayarlar).
- **Dev-only şifreler için rotate şart değil** (G-01 kapsamı). Kullanıcı kararı: "dev ortamı zaten lokal, git history'deki eski SA şifresi önemsiz". Fix sadece leak'i durdurur, history'yi dokunmaz. Prod/staging şifreleri için aynı karar **geçerli değil** — rotation zorunlu.
- **AdminController 1736 satır anti-pattern kabul.** M-01 (Faz 2, 2 gün) service extraction planlandı ama commit-split sırasında `feat(admin): consolidated admin panel` bundled commit (`07f4b91`) olarak kabul edildi. "Known technical debt" commit mesajında açıkça belirtildi. Refactor ayrı ADR konusu.
- **User.Roles CSV deprecate 3 faz**: Faz A (kod-düzeyi temizlik ✅ `2d0c3fd`), Faz B (kolon nullable + `[Obsolete]`), Faz C (kolon drop + field sil). Detay: [ADR-003](../../docs/ADR/003-role-model.md).
- **Skill commit davranışı 3 kademeli.** session-handoff (tam otomatik, tek path), plan-tracker (kod commit'iyle birlikte), commit-splitter (her bucket için onay). Detay: [ADR-004](../../docs/ADR/004-skill-design-principles.md).
- **Pragmatik commit-split** (65 dosya → 16 commit): yeni dosyalar feature-başına, modified controller/view dosyaları scope-based consolidated. Hunk-level split 2-3 saat maliyet, solo-dev için yatırım değil. Pattern kaydı: `claude-context-template/docs/PATTERNS.md` P-1.
- **Pre-commit antipattern hook** commit-split süresince disable edildi (mevcut M-02 ihlalleri blok ediyordu), sonda re-enable ayrı commit (`59888db`). Pattern: `claude-context-template/docs/PATTERNS.md` P-2.
- **Deprecated dosya silme > banner.** `Views/Auth/AGENT.md` banner yerine silindi (`7a7b81d`). Banner kafa karıştırır.
- **Koşulsuz SessionStart hook kuralı.** `.claude/rules/session-protocol.md` — context'te hook çıktısı görünse bile `bash` elle tekrar çalıştırılır. Aksi varsayım iki kez hata üretti.

## Proje Durumu (snapshot)

- **Olgunluk:** %78 (Faz 0 + Faz 1 öncelikli üçlü + M-03 Faz A kapandı).
- **Uncommitted:** tipik olarak 0 (post-commit hook journal append'leri hariç).
- **Aktif modüller:** rol sistemi, kategori, favori, AD user, user data filter, dashboard motoru, SP önizleme, Claude tooling (hook'lar + skill'ler + agent'lar).

## Referanslar

- ADR'ler: [003-role-model](../../docs/ADR/003-role-model.md) ✅, [004-skill-design-principles](../../docs/ADR/004-skill-design-principles.md) ✅. Yazılacak: `001-data-access.md`, `002-dashboard-architecture.md`, `005-sp-modularization.md`, `006-allowed-roles-csv-deprecate.md`.
- Bağlam yönetimi: `docs/CONTEXT_MANAGEMENT.md`.
- Kapsamlı TODO: `TODO.md` → "BIRLESIK ONCELIK SIRASI".
- Real-world pattern'ler: `claude-context-template/docs/PATTERNS.md` (P-1..P-10).
