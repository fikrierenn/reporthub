# ReportHub — Claude Araçları ve Mimari Öneri Paketi
_Araştırma tarihi: 22 Nisan 2026 — 3 paralel agent ile derlendi, tek belge haline getirildi._

## Amaç
ReportHub'ı (ASP.NET Core 10 MVC, SQL Server SP-ağırlıklı rapor portalı, tek geliştirici, Türkçe UI, BKM) **uzun vadede Claude Code ile verimli bakılabilir** hale getirmek. Dosya 4 ana kısımdan oluşuyor:

1. **Mimari Yol Haritası** — kod tabanı için sprint bazlı tavsiyeler
2. **Claude Skill Önerileri** — projede kurulacak özel skill'ler
3. **Claude Subagent Önerileri** — özelleşmiş alt-agent'lar
4. **Hook'lar + CLAUDE.md Organizasyonu** — `settings.json` ve bellek disiplini

Her bölüm **YÜKSEK / ORTA / DÜŞÜK önceliklerle** bitiyor. Dosyanın sonunda **3 sprint'lik uygulama planı** ve **nereden başlamalı** özeti var.

---

# 1. Mimari Yol Haritası

## 1.1 Veri erişim katmanı
- **Mevcut durum:** `ReportsController.cs` içinde statik `ExecuteStoredProcedure` + `ExecuteStoredProcedureMultiResultSets` (ADO.NET). CRUD için EF Core. Repository katmanı yok.
- **Tavsiye:** `Services/IStoredProcedureExecutor.cs` interface'i + `StoredProcedureExecutor` implementasyonu oluştur, Controller'dan çıkar, `AddScoped` olarak DI'a kaydet. Dapper EKLEME — mevcut ADO.NET + EF hibriti tek geliştirici için yeterli.
- **Öncelik / Süre:** ORTA / 2 saat
- **İnaksiyon riski:** SP çalıştırma test edilemiyor, reuse zor, Controller şişiyor.

## 1.2 Dashboard motoru
- **Mevcut durum:** `DashboardRenderer.cs` 400 satırlık statik StringBuilder — hem HTML hem satır içi JS üretiyor. XSS güvenlik düzeltmeleri zaten yapıldı.
- **Tavsiye (3 adımda):**
  - (a) ~120 satırlık runtime JS'i (`switchTab`, `showDetail`, chart init) **`wwwroot/js/dashboard-runtime.js`** dosyasına çıkar. Renderer sadece `<script src>` + config embed etsin.
  - (b) Renderer statik kalsın — DI overhead'i bu iş için değmez. Ama `ReportCatalog.DashboardHtml` (legacy) **kullanımdan kaldır işareti al**, sadece `DashboardConfigJson` birincil olsun.
  - (c) İstemci-taraf framework (React/Vue) EKLEME — vanilla JS + HTML5 KPI/chart/tablo için fazlasıyla yeter.
- **Öncelik / Süre:** YÜKSEK / 3 saat
- **İnaksiyon riski:** Satır içi JS bakımı zor, güvenlik denetiminde tekrar gündeme gelir.

## 1.3 API yüzeyi
- **Mevcut durum:** Razor view + birkaç JSON endpoint (FilterOptions, SpList, SpPreview).
- **Tavsiye:** Kullanıcı tarafı server-rendered Razor kalsın. Admin operasyonları için **Minimal API** (`app.MapPost("/api/admin/reports/{id}", ...)`) yavaş yavaş eklenebilir. OpenAPI/Swagger tek geliştirici için abartı — CLAUDE.md'de dokümante et yeter.
- **Öncelik / Süre:** DÜŞÜK / 1 gün (sadece external client ihtiyacı doğarsa)
- **İnaksiyon riski:** Mobile / CLI istemcisi ekleme ihtiyacı doğarsa zorluk çıkar.

## 1.4 Auth & Yetkilendirme
- **Mevcut durum:** Cookie auth. Rol = `User.Roles` CSV **+** `UserRole` normalize tablo (ÇİFT KAYNAK, TODO'da flagli).
- **Tavsiye:** **Önce CSV/tablo ikiliğini çöz**: `User.Roles` kolonunu drop et, tüm rol kontrolü `UserRole` join'e geçsin. Migration:
  ```sql
  -- 15_MigrateRolesFromCsv.sql
  INSERT INTO UserRole (UserId, RoleId)
  SELECT u.UserId, r.RoleId
  FROM Users u
  CROSS APPLY STRING_SPLIT(u.Roles, ',') s
  JOIN Roles r ON LTRIM(RTRIM(s.value)) = r.Name
  WHERE NOT EXISTS (SELECT 1 FROM UserRole x WHERE x.UserId=u.UserId AND x.RoleId=r.RoleId);
  ALTER TABLE Users DROP COLUMN Roles;
  ```
  Kod tarafı: `DashboardController.cs:68-82` (CSV parse) ve `ReportsController.cs:73-75` ikisini de `UserRole` join'e çevir. JWT ekleme — cookie yeterli.
- **Öncelik / Süre:** YÜKSEK / 1 gün
- **İnaksiyon riski:** Rol kontrolleri tutarsız, yetkisiz erişim potansiyeli.

## 1.5 Frontend mimarisi
- **Mevcut durum:** Vanilla JS IIFE, Tailwind CDN, Chart.js CDN, `btn-brand` custom CSS.
- **Tavsiye:**
  - Vanilla JS + IIFE kalsın (iyi çalışıyor).
  - Tailwind CDN tek geliştirici için yeter. PostCSS build geçişi Sprint 2'ye bırakılabilir.
  - Chart.js 4.4.x'e güncelle. Yeni görselleştirme türü gerekirse **Apex Charts** eklenebilir (framework gerektirmez).
  - **JS bundle ilerde:** `.js` dosya sayısı 3'ü aşarsa npm'siz esbuild pipeline: `npx esbuild wwwroot/assets/js/*.js --bundle --minify --outdir=wwwroot/dist`.
- **Öncelik / Süre:** DÜŞÜK / 2 saat (sadece gerekirse)
- **İnaksiyon riski:** Performans platosu, büyüdükçe CSS duplikasyonu.

## 1.6 Test stratejisi
- **Mevcut durum:** 2 test sınıfı (PasswordHasher + AuditLogService) — %10'un altı coverage.
- **Tavsiye (öncelik sırası):**
  1. **P0 — Integration test:** `ReportsController.Run(int)` — seed PDKS + SatisPano raporlarıyla gerçek DB'ye karşı. Parametre doğrulama, dashboard config deserialize, SP hata yakalama. ~4 test.
  2. **P1 — Unit test:** `DashboardRenderer.Render()` — mock resultSet, XSS payload'ları (`<script>`, `<img onerror>`), multi-tab, grid layout. ~6 test.
  3. **P2 — Noktasal:** `AdminController.CreateUser()` rol atama, `SpPreview()` farklı SP imzalarıyla.
  4. **ATLA:** DbContext mock testleri (abartı), UI automation (manuel smoke yeter).
- **Öncelik / Süre:** ORTA / 3-4 gün (kritik yollarda %30 coverage hedefi)
- **İnaksiyon riski:** Rapor run + dashboard render'da sessiz regresyon.

## 1.7 CI/CD
- **Mevcut durum:** Sıfır otomasyon.
- **Tavsiye:** GitHub Actions (ya da Azure DevOps). Minimal workflow:
  ```yaml
  # .github/workflows/build.yml
  name: Build & Test
  on: [push, pull_request]
  jobs:
    build:
      runs-on: windows-latest  # SQL Server LocalDB için
      steps:
        - uses: actions/checkout@v4
        - uses: actions/setup-dotnet@v4
          with: { dotnet-version: '10.0.x' }
        - run: dotnet restore
        - run: dotnet build --no-restore --configuration Release
        - run: dotnet test --no-build --configuration Release
        - run: dotnet publish ReportPanel/ReportPanel.csproj -c Release -o ./publish
        - uses: actions/upload-artifact@v4
          with: { name: publish, path: ./publish }
  ```
  Secret olarak connection string'i `ConnectionStrings__MainDb` env var'ına koy, staging deploy'da kullan.
- **Öncelik / Süre:** ORTA / 4 saat
- **İnaksiyon riski:** Manuel build hatası, geri alma mekanizması yok.

## 1.8 DB Migration disiplini
- **Mevcut durum:** 14 numaralı SQL (01-14), SP'ler aynı klasörde karışık.
- **Tavsiye:** EF Core Migrations'a GEÇME (SP'ler çoğunluk, gereksiz). Mevcut SQL'leri **alt klasörlere ayır**:
  ```
  Database/
    Schema/            01_CreateDatabase.sql, 02_CreateTables.sql
    Migrations/        06_CreateReportFavorites.sql, 07_AddReportCategory.sql, ...
    StoredProcedures/  sp_PdksPano.sql, sp_SatisPano.sql
    Functions/         fn_PdksKpiOzet.sql (yeni, TODO SP mimarisi gereği)
    Seed/              03_SeedData.sql, 12_SeedPDKSDashboard.sql, 14_SeedSatisDashboard.sql
  ```
  Numaralandırma semantik: `YYYY-MM-DD_KisaAciklama.sql`. Migration runner için basit bash script ya da .NET console app: `DbMigrate.csproj`. Flyway KULLANMA — tek geliştirici için abartı.
- **Öncelik / Süre:** DÜŞÜK / 1 gün
- **İnaksiyon riski:** Schema dokümantasyonsuz kalır, sıfırdan kurmak zorlaşır.

## 1.9 Gözlemlenebilirlik (Observability)
- **Mevcut durum:** `AuditLog` tablosu dışında hiçbir şey.
- **Tavsiye:**
  - **Aşama 1 (şimdi):** `Serilog` + `Serilog.AspNetCore` + `Serilog.Sinks.File` + `Serilog.Sinks.Console`. `Program.cs`'de bootstrap:
    ```csharp
    builder.Host.UseSerilog((ctx, lc) => lc
      .WriteTo.Console()
      .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));
    ```
    Controller'larda `ILogger<ReportsController>` inject et, kritik olayları logla.
  - **Aşama 2 (opsiyonel):** Seq (self-hosted, 30GB/ay ücretsiz) VEYA ApplicationInsights (Azure). Tek geliştirici için Seq local'de çok rahat.
  - **ATLA:** OpenTelemetry (multi-service mimari olmadıkça gereksiz).
- **Öncelik / Süre:** ORTA / 2 saat
- **İnaksiyon riski:** Production sorunları kör uçar, audit trail eksik kalır.

## 1.10 Dokümantasyon / ADR (Mimari Karar Kaydı)
- **Mevcut durum:** CLAUDE.md (süreç), TODO.md (özellikler), satır içi yorum az.
- **Tavsiye:** `docs/ADR/` klasörü + 3 kritik karar:
  1. **ADR-001-data-access.md** — "Rapor/dashboard verisi = SP. Metadata CRUD = EF Core."
  2. **ADR-002-dashboard-architecture.md** — "Config-driven JSON. iframe sandbox + DOM API (`eval` yok)."
  3. **ADR-003-role-model.md** — "UserRole junction tablo — CSV normalize edildi."
  
  Format: **Durum** (Öneri/Kabul/Geçersiz), **Bağlam**, **Karar**, **Sonuçlar**. README.md'den link.
- **Öncelik / Süre:** ORTA / 2 saat
- **İnaksiyon riski:** Tribal knowledge, gelecekteki sen aynı tartışmaları yeniden yapar.

---

# 2. Claude Skill Önerileri

**Skill nedir?** `.claude/skills/<ad>/SKILL.md` dosyası — ilgili bir konu geçince Claude otomatik okur (`description` alanındaki tetikleyiciye göre). Mevcut `security-review`, `review`, `simplify`, `init` skill'leri zaten aktif. Aşağıdakiler **ReportHub'a özel** ek skill'ler.

## 2.1 `sp-preview-extractor` (skill)
- **Tetikleyici:** "SP", "stored procedure", "önizle", "kolonlar" kelimeleri
- **Ne yapar:** Mevcut `/Admin/SpPreview` endpoint'ini sarar. SP adı verilince parametre imzası (akıllı default: date→bugün, int→0), tüm result set'leri, kolon tipleri listeler. İstenirse `.md` SP referans kartı üretir.
- **Neden:** Dashboard builder kolon auto-detect + SP dokümantasyonu için temel taş.
- **Öncelik:** YÜKSEK

## 2.2 `dashboard-config-validator` (skill)
- **Tetikleyici:** `DashboardConfig.cs`, `DashboardRenderer.cs` veya "DashboardConfigJson" içeren dosya düzenlemesi
- **Ne yapar:** `DashboardConfig` JSON round-trip testi, `createElement`/`textContent` zorunluluğu, `eval()` yasağı, Chart.js config sanitize kontrolü, iframe sandbox attribute doğrulama, mini CSP kuralı.
- **Neden:** Dashboard motoru en riskli kod yolu, sürekli büyüyor.
- **Öncelik:** YÜKSEK

## 2.3 `turkish-ui-normalizer` (skill)
- **Tetikleyici:** `.cshtml` içinde ASCII'ye düşürülmüş Türkçe ("Duzenle", "Bilesen", "Islem")
- **Ne yapar:** UTF-8'e normalize eder ("Düzenle", "Bileşen", "İşlem"). Kod identifier'larına dokunmaz. `<meta charset>` + `_AppLayout` kontrolü. Opsiyonel olarak `<html lang="tr">` eksikse uyarır.
- **Neden:** CLAUDE.md §2.5'te zorunlu, tutarlılık için.
- **Öncelik:** ORTA

## 2.4 `migration-script-generator` (skill)
- **Tetikleyici:** manuel `/new-migration <açıklama>` veya `Models/*.cs` değişimi
- **Ne yapar:** Senin `Database/NN_*.sql` konvansiyonunla sıradaki numaralı migration yazar: `IF NOT EXISTS` guard, GO ayırıcılar, rollback bölümü yorum olarak. EF Core migration DEĞİL.
- **Neden:** 14 migration'ın disiplinini koru, yazım hatasından kurtul.
- **Öncelik:** ORTA

## 2.5 `audit-log-coverage-checker` (skill)
- **Tetikleyici:** manuel, `[HttpPost]` admin action içeren commit öncesi
- **Ne yapar:** Admin-scope POST action'ları listeler, `AuditLogService.LogAsync` çağırıyor mu kontrol eder. CreateUser/CreateReport/DeleteX/EditX hepsi log'lu olmalı.
- **Neden:** CLAUDE.md'de "AuditLog selektif" açık eksiklik.
- **Öncelik:** ORTA

## 2.6 `razor-form-consistency-linter` (skill)
- **Tetikleyici:** manuel `/razor-lint`
- **Ne yapar:** `Views/` altını tarar — form pattern karışıklığı (`Html.BeginForm` vs raw `<form>`), eksik `@Html.AntiForgeryToken`, eksik `asp-validation-for`, karışık `form-input-brand` vs Tailwind utility.
- **Neden:** TODO'daki "form syntax karışık" ORTA riskli maddeyi sistematikleştirir.
- **Öncelik:** DÜŞÜK

## 2.7 `session-handoff-writer` (skill)
- **Tetikleyici:** manuel `/handoff`
- **Ne yapar:** CLAUDE.md §5 "Bu Oturumda Olanlar" formatında özet yazar: tamamlananlar, build durumu, yarım kalanlar, yarına başlangıç noktası. Türkçe UI kuralına uyar.
- **Neden:** Zaten manuel yapıyorsun, otomatize et.
- **Öncelik:** DÜŞÜK

---

# 3. Claude Subagent Önerileri

**Agent nedir?** `.claude/agents/<ad>.md` dosyası — izole bir context window'da çalışan özelleşmiş Claude. `description` alanına göre otomatik devredilebilir. Araç allowlist'i kısıtlanabilir.

## 3.1 `sp-inventory-auditor` (subagent)
- **Tetikleyici:** manuel
- **Araçlar:** `Read, Grep, Glob, mcp__sqlserver__sql_browse_schema, mcp__sqlserver__sql_query`
- **Ne yapar:** DB'deki tüm SP'leri enumerate eder (`sys.procedures`), `ReportCatalog.ProcName` ile eşleştirir, `.cs` dosyalarındaki `SqlCommand` literal'larını tarar. Flagler:
  - **Orphan SP**: DB'de var, kod çağırmıyor
  - **Dangling SP**: Kod çağırıyor, DB'de yok
  - **Undocumented SP**: DB'de var, `ReportCatalog`'da satırı yok
- **Neden:** ReportHub'ın DNA'sı SP — bugün kontrat dokümanı yok. Bu agent eksik katalog.
- **Öncelik:** YÜKSEK

## 3.2 `razor-xss-auditor` (subagent)
- **Tetikleyici:** manuel veya `.cshtml` düzenlemesi
- **Araçlar:** `Read, Grep`
- **Ne yapar:** Razor view'larda `@Html.Raw`, satır içi `<script>` içinde `innerHTML`, POST formlarda eksik `@Html.AntiForgeryToken`, JS çıktısında string concat tarar. `DashboardRenderer` gibi server-generated HTML de kapsam dahilinde. Dosya:satır + çözüm önerisi raporlar.
- **Neden:** `DashboardRenderer` + tekrarlı `innerHTML` pattern'i — jenerik skill'ler yakalamaz.
- **Öncelik:** YÜKSEK

## 3.3 `ef-sp-hybrid-reviewer` (subagent)
- **Tetikleyici:** manuel, Controller'da data erişimi değişen commit öncesi
- **Araçlar:** `Read, Grep`
- **Ne yapar:** Her yeni veri erişim noktası için tavsiye verir:
  - Basit tek-entity CRUD → EF LINQ
  - Okuma ağırlıklı mevcut SP → `FromSql`
  - Parametreli çok-resultset SP → parametreli ham `SqlCommand`
  
  SP kullanıldığında `CommandType.StoredProcedure` + `SqlParameter` zorunluluğu. `.AsNoTracking()` tutarsızlığını flagler.
- **Neden:** TODO'daki "SP mi EF Core mu" kararı için günlük asistan.
- **Öncelik:** ORTA

## 3.4 `user-data-filter-guard` (subagent)
- **Tetikleyici:** `ReportsController.cs` veya `UserDataFilter` ilgili dosya düzenlemesi
- **Araçlar:** `Read, Grep`
- **Ne yapar:** Her SP çalıştırma yolunda kullanıcının data filter'ı uygulanıyor mu doğrular (satır 875 anchor). `SqlCommand.ExecuteReader` filter injection'ı bypass eden çağrı varsa flagler.
- **Neden:** Multi-tenant veri sızıntısı rapor uygulamasında #1 sessiz hata kaynağı. Generic OWASP yakalamaz.
- **Öncelik:** YÜKSEK

## 3.5 `commit-splitter` (subagent, bir defalık)
- **Tetikleyici:** manuel `/commit-splitter`
- **Araçlar:** `Bash(git *), Read`
- **Ne yapar:** TODO.md'deki 7 fazlı commit stratejisini okur, 32 uncommitted dosyayı bucket'lara grupla, her bucket için commit mesajı taslağı hazırla, onay alarak sıralı stage yap.
- **Neden:** 32 dosyalık backlog'u çözmek için tek seferlik araç. İşi biter, emekli olur.
- **Öncelik:** YÜKSEK (bir defalık)

## 3.6 `dashboard-runtime-extractor` (subagent, bir defalık)
- **Tetikleyici:** manuel
- **Araçlar:** `Read, Edit, Write, Bash(dotnet build *)`
- **Ne yapar:** `DashboardRenderer.cs` içindeki satır içi JS'i `wwwroot/js/dashboard-runtime.js`'e taşır. Renderer sadece config + `<script src>` üretsin. Build + mevcut dashboard smoke test.
- **Neden:** Mimari 1.2 sprint 1 maddesini otomatize eder, izole iş — subagent'a iyi uyar.
- **Öncelik:** YÜKSEK (sprint 1)

## 3.7 `sp-to-tvf-refactorer` (subagent)
- **Tetikleyici:** manuel
- **Araçlar:** `Read, Write, mcp__sqlserver__sql_query`
- **Ne yapar:** Monolitik SP alır (örn. `sp_PdksPano`), her result set query'sini ayrı **inline TVF**'e böler (`fn_PdksDetay`, `fn_PdksKpiOzet`, `fn_PdksDepartmanKirilim`). Yeni SP orkestrator olur. Refactor öncesi/sonrası sonuç karşılaştırma testi.
- **Neden:** TODO "SP MIMARISI TARTISMASI → Karar 2" için iş gücü.
- **Öncelik:** ORTA

---

# 4. Hook'lar + CLAUDE.md Organizasyonu

## 4.1 settings.json hook'ları

`.claude/settings.json`'a eklenecek hook'lar:

### H1 — PostToolUse: değişen dosyayı dotnet format
```json
{
  "hooks": {
    "PostToolUse": [{
      "matcher": "Edit|Write",
      "hooks": [{
        "type": "command",
        "command": "pwsh -Command \"if ($env:CLAUDE_FILE_PATH -match '\\.(cs|cshtml)$') { dotnet format D:/Dev/reporthub/ReportPanel/ReportPanel.csproj --include $env:CLAUDE_FILE_PATH --verify-no-changes 2>$null; if ($LASTEXITCODE -ne 0) { dotnet format D:/Dev/reporthub/ReportPanel/ReportPanel.csproj --include $env:CLAUDE_FILE_PATH } }\""
      }]
    }]
  }
}
```
**Fayda:** Her `.cs` / `.cshtml` düzenlemesi sonrası otomatik format. Universal.

### H2 — PreToolUse: git commit antipattern taraması
```json
{
  "hooks": {
    "PreToolUse": [{
      "matcher": "Bash",
      "hooks": [{
        "type": "command",
        "command": "bash D:/Dev/reporthub/.claude/hooks/pre-commit-antipattern.sh"
      }]
    }]
  }
}
```
`pre-commit-antipattern.sh` içeriği:
```bash
#!/usr/bin/env bash
# Sadece git commit komutlarında çalış
if ! echo "$CLAUDE_BASH_COMMAND" | grep -q "^git commit"; then exit 0; fi
staged=$(git diff --cached --name-only --diff-filter=ACM | grep -E '\.cs$')
[ -z "$staged" ] && exit 0
for f in $staged; do
  grep -Hn "DateTime\.Now\b" "$f" && { echo "❌ DateTime.Now → DateTime.UtcNow kullan" >&2; exit 2; }
  grep -Hn "async void\b" "$f" | grep -v "event" && { echo "❌ async void (event handler hariç)" >&2; exit 2; }
  grep -Hn "new HttpClient()" "$f" && { echo "❌ new HttpClient() → IHttpClientFactory" >&2; exit 2; }
  grep -HnE "Password=[^\"';]+" "$f" | grep -v 'Password=\"\"' && { echo "❌ Hardcoded şifre tespit edildi" >&2; exit 2; }
done
exit 0
```
**Fayda:** `TestController.cs:50` SA şifre sızıntısı + klasik .NET antipattern'leri.

### H3 — Stop: build + JS syntax doğrulaması
```json
{
  "hooks": {
    "Stop": [{
      "hooks": [{
        "type": "command",
        "command": "bash D:/Dev/reporthub/.claude/hooks/verify.sh"
      }]
    }]
  }
}
```
`verify.sh`:
```bash
#!/usr/bin/env bash
cd D:/Dev/reporthub/ReportPanel
dotnet build --nologo -clp:ErrorsOnly || exit 2
# JS syntax check
for js in wwwroot/assets/js/*.js; do
  node -e "new Function(require('fs').readFileSync('$js','utf8'))" 2>&1 || { echo "JS hatası: $js" >&2; exit 2; }
done
exit 0
```
**Fayda:** "Bitti" demeden önce build yeşil + JS parse OK zorunluluğu.

## 4.2 CLAUDE.md organizasyonu

Mevcut CLAUDE.md ~250 satır — sınırda. Best practice §200 satır altı. Önerilen split:

```
D:/Dev/reporthub/
├── CLAUDE.md                              # ~100 satır, genel rehber + import'lar
├── .claude/
│   ├── rules/
│   │   ├── sql-sp.md                      # paths: **/*.sql — SP yazım konvansiyonu
│   │   ├── razor.md                       # paths: **/*.cshtml — view kuralları
│   │   ├── js.md                          # paths: wwwroot/assets/js/** — IIFE, vanilla
│   │   └── commit.md                      # commit stratejisi detayı
│   ├── agents/                            # 7 subagent
│   ├── skills/                            # 7 skill
│   ├── hooks/                             # hook shell scriptleri
│   └── settings.json                      # hook kayıtları + permission
├── docs/
│   ├── ADR/
│   │   ├── 001-data-access.md
│   │   ├── 002-dashboard-architecture.md
│   │   └── 003-role-model.md
│   └── CLAUDE_TOOLING_PROPOSAL.md         # BU DOSYA
└── TODO.md                                # özellik TODO'ları + mimari tartışmalar
```

CLAUDE.md başına import'lar:
```markdown
# CLAUDE.md — ReportHub

@README.md
@docs/ADR/001-data-access.md
@docs/ADR/002-dashboard-architecture.md
@docs/ADR/003-role-model.md

## Kurallar
- SQL/SP: @.claude/rules/sql-sp.md
- Razor: @.claude/rules/razor.md
- JavaScript: @.claude/rules/js.md
- Commit: @.claude/rules/commit.md

## Kullanıcı direktifleri
[§2 mevcut içerik kalsın]

...
```

`§5 Bu Oturumda Olanlar` bölümünü **silme**, **`~/.claude/projects/D--Dev-reporthub/memory/MEMORY.md`** dosyasına taşı — auto memory bu iş için tasarlanmış.

## 4.3 Skill yazım şablonu (referans)

```markdown
---
name: sp-preview-extractor
description: Bir SQL Server stored procedure'u önizle — parametreleri akıllı default'larla listele (date→bugün, int→0), tüm result set'leri enumerate et, kolon metadata'sı döndür. "SP", "stored procedure", "önizle", "kolonlar" veya dashboard builder işleri söz konusu olduğunda kullan.
allowed-tools: Bash, Read, Grep, mcp__sqlserver__sql_query, mcp__sqlserver__sql_browse_schema
user-invocable: true
model: inherit
---

# SP Önizleme Çıkarıcı

## Amaç
...

## Adımlar
1. ...

## Örnek kullanım
...
```

## 4.4 Subagent yazım şablonu (referans)

```markdown
---
name: sp-inventory-auditor
description: Bu projenin SQL Server kataloğundaki stored procedure'ları denetler. sys.procedures ile ReportCatalog.ProcName ve C# SqlCommand literal'larını çapraz referans alarak orphan, dangling ve undocumented SP'leri raporlar. Kullanıcı SP kapsamını veya stored procedure gözden geçirmeyi sorduğunda proaktif kullan.
tools: Read, Grep, Glob, mcp__sqlserver__sql_query, mcp__sqlserver__sql_browse_schema
model: sonnet
color: purple
---

ReportHub için kıdemli bir veritabanı denetçisisin...
```

---

# 5. Sprint Planı

## Sprint 1 — Hafta 1 (güvenlik + yapı temelleri)
**Hedef:** Kritik mimari borçları kapat + en faydalı 3 Claude aracını kur.

- [ ] Mimari 1.1 — `IStoredProcedureExecutor` servisi (2h)
- [ ] Mimari 1.2 — Dashboard runtime JS extraction (3h)
- [ ] Mimari 1.4 — `User.Roles` CSV normalize migration (1g)
- [ ] Subagent 3.5 — `commit-splitter` → 32 dosyayı 7 commit'e böl (1g, bir defalık)
- [ ] Subagent 3.6 — `dashboard-runtime-extractor` → 1.2'yi otomatize et
- [ ] Hook H1 — dotnet format auto
- [ ] Hook H3 — Stop verify (build + JS)
- [ ] Skill 2.2 — `dashboard-config-validator`
- [ ] Skill 2.1 — `sp-preview-extractor` (SpList/SpPreview endpoint'leri zaten hazır)

**Toplam efor:** ~3 gün

## Sprint 2 — Ay 1 (kalite + süreklilik)
**Hedef:** CI/CD + observability + doküman + test temelleri.

- [ ] Mimari 1.7 — GitHub Actions workflow (4h)
- [ ] Mimari 1.9 — Serilog wiring (2h)
- [ ] Mimari 1.10 — 3 ADR yazımı (2h)
- [ ] Mimari 1.8 — Database/ klasör reorganizasyonu (1g)
- [ ] SP Mimarisi Kararı — `sp_PdksPano` → inline TVF parçalaması (TODO'da detay, 2-3h)
- [ ] Subagent 3.1 — `sp-inventory-auditor`
- [ ] Subagent 3.2 — `razor-xss-auditor`
- [ ] Subagent 3.4 — `user-data-filter-guard`
- [ ] Hook H2 — pre-commit antipattern
- [ ] Skill 2.5 — `audit-log-coverage-checker`
- [ ] Test P0 — `ReportsController.Run()` integration testi
- [ ] Test P1 — `DashboardRenderer` unit testleri (XSS payload dahil)

**Toplam efor:** ~5 gün dağıtılmış

## Sprint 3 — Çeyrek 1 (cila + opsiyonel)
**Hedef:** Kullanıcı deneyimi + kapsam genişletme.

- [ ] User P1 — Admin listesi arama + filtre + son giriş
- [ ] User P1 — User modeline Phone/Department/Position (migration 15)
- [ ] Dashboard UX — Canlı önizleme iframe
- [ ] Dashboard UX — SP kolon auto-detect JS (yarım kalan — TODO'da)
- [ ] Subagent 3.3 — `ef-sp-hybrid-reviewer`
- [ ] Subagent 3.7 — `sp-to-tvf-refactorer`
- [ ] Skill 2.3 — `turkish-ui-normalizer`
- [ ] Skill 2.4 — `migration-script-generator`
- [ ] Skill 2.6 — `razor-form-consistency-linter`
- [ ] Skill 2.7 — `session-handoff-writer`
- [ ] Test coverage → %30 (diğer controller'lar)
- [ ] Opsiyonel: esbuild JS bundle pipeline (JS >5 dosya olursa)

**Toplam efor:** ~5 gün dağıtılmış

---

# 6. Hızlı Referans

## Önerilen dosya konumları
| Amaç | Konum |
|---|---|
| Proje rehberi | `CLAUDE.md` (repo root) |
| Topical kurallar | `.claude/rules/*.md` (commit'lenmiş) |
| Özel skill'ler | `.claude/skills/<ad>/SKILL.md` (commit'lenmiş) |
| Özel agent'lar | `.claude/agents/<ad>.md` (commit'lenmiş) |
| Hook scriptleri | `.claude/hooks/*.sh` (commit'lenmiş) |
| Permission | `.claude/settings.json` (commit'lenmiş) + `.claude/settings.local.json` (gitignore) |
| ADR | `docs/ADR/NNN-*.md` |
| Oturum hafızası (auto) | `~/.claude/projects/D--Dev-reporthub/memory/MEMORY.md` |

## Araştırma kaynakları
- **Claude Code dokümanları:**
  - https://docs.claude.com/en/docs/claude-code/sub-agents
  - https://docs.claude.com/en/docs/claude-code/skills
  - https://docs.claude.com/en/docs/claude-code/memory
  - https://docs.claude.com/en/docs/claude-code/settings
- **.NET ekosisteminden örnekler:**
  - [codewithmukesh/dotnet-claude-kit](https://github.com/codewithmukesh/dotnet-claude-kit) — 47 skill, 10 agent, 7 hook
  - [VoltAgent/awesome-claude-code-subagents](https://github.com/VoltAgent/awesome-claude-code-subagents) — 167 skill / 16 agent
  - [Aaronontheweb/dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) — EF Core + Aspire odaklı
  - [wshobson/agents](https://github.com/wshobson/agents) — production-seviyesi agent örnekleri
  - [lowtouch.ai 12-subagent DB migration pipeline](https://www.lowtouch.ai/claude-code-cli-vs-github-copilot-agentic-workflows-database-migration/) — SP-ağırlıklı için en iyi referans
- **.NET'e özel rehberler:**
  - [codewithmukesh — CLAUDE.md for .NET Developers](https://codewithmukesh.com/blog/claude-md-mastery-dotnet/)
  - [Platform.Uno — Configuring Claude Code for Real .NET Projects](https://platform.uno/blog/configuring-claude-code-for-real-net-projects/)
  - [Atomic Object — Claude Code Sub-Agent For .NET and Angular](https://spin.atomicobject.com/claude-code-sub-agent/)

---

# 7. Kapanış: Nereden Başlamalı?

**1 saatin varsa:**
1. **Hook H3 (Stop: verify)** — en hızlı ROI, her "bitti" dediğinde build + JS doğrulanır
2. **Subagent `commit-splitter`** — 32 dosyalık backlog'u temiz commit'lere böl
3. **Skill `dashboard-config-validator`** — aktif güvenlik koruması

**1 günün varsa:** Sprint 1'in tamamı.

**1 haftan varsa:** Sprint 1 + Sprint 2'nin yarısı — proje production-hazır, profesyonel bir yapıya kavuşur.

İyi geceler, sabah buradan başlayabilirsin. 🌙
