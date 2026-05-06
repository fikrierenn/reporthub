# Git / Commit Disiplini

_Kapsam: Commit/branch/merge pratikleri. `paths:` yok — compact sonrası bile kalmalı._

## Commit Kuralları

1. **Kullanıcı açıkça istemedikçe commit etme.** `"commit et"`, `"commit'le"`, `"git commit"` gibi net komut gelmeden commit yok.
   - **İstisna:** `session-handoff` skill'i, yalnızca `docs/journal/YYYY-MM-DD.md` dosyasını otomatik commit eder (başka path'e dokunmaz). Gerekçe: handoff artifactı dosyaya yazılıp bırakılırsa her oturum başında uncommitted olarak görünür ve pre-commit hook'u gürültü yapar.

2. **Bir commit = bir konu.** AI 3 katman birden çıkarırsa → 3 ayrı commit'e böl.

3. **Save-point commit.** Test yeşil → hemen commit, iş yarım olsa bile. `WIP: <konu>` prefix'li olabilir, undo stack için.

4. **15 dosya eşiği.** `git status` ile uncommitted > 15 → **yeni iş yasak**, önce commit-split.

5. **Commit mesajı formatı (konvansiyon):**
   ```
   <tip>: <kısa özet>

   <detay — opsiyonel>
   ```
   Tipler: `feat` (yeni özellik), `fix` (bug), `refactor`, `docs`, `test`, `chore`, `perf`, `style`, `build`.

6. **Türkçe veya İngilizce** (tutarlı kal). Mevcut commit'ler İngilizce (`Add install guide`, `Update README`) — devamı öyle.

## Branch Stratejisi

- **main** → production.
- **feature branch** → bir kullanıcı talebi = bir branch (`feature/dashboard-builder`, `fix/sp-preview-handler`).
- İş bitince **squash merge** main'e. Geri alma kolay olur.
- **Branch-per-ask:** Yeni talep → yeni branch. Mevcut dalı kirletme.

## 32-Dosyalık Backlog — Planlı Split

```
1. feat: rol sistemi
   - Role.cs, UserRole.cs, ReportAllowedRole.cs
   - Database/07_AddReportCategory.sql, 08_MigrateRolesAndCategories.sql
   - AdminController rol CRUD + Views/Admin/EditRole.cshtml
   - ViewModels/AdminRoleFormViewModel.cs

2. feat: rapor kategorileri
   - ReportCategory.cs, ReportCategoryLink.cs
   - Database/07 katkısı
   - AdminController kategori CRUD + Views/Admin/EditCategory.cshtml
   - ViewModels/AdminCategoryFormViewModel.cs

3. feat: rapor favorileri
   - ReportFavorite.cs
   - Database/06_CreateReportFavorites.sql
   - ReportsController favori ekleme/kaldırma
   - Views/Reports/Index.cshtml favori UI

4. feat: AD user desteği
   - Database/09_AddIsAdUser.sql
   - User.IsAdUser
   - AuthController AD login path
   - CreateUser.cshtml / EditUser.cshtml AD toggle

5. feat: user data filter
   - UserDataFilter.cs
   - Database/13_CreateUserDataFilter.sql
   - ReportsController.InjectUserDataFilters
   - CreateUser.cshtml / EditUser.cshtml filtre bölümü

6. feat: dashboard motoru
   - DashboardConfig.cs
   - Services/DashboardRenderer.cs
   - wwwroot/assets/js/dashboard-builder.js
   - Database/10_AddDashboardColumns.sql, 11_AddDashboardConfigJson.sql, 12_SeedPDKSDashboard.sql, 14_SeedSatisDashboard.sql
   - Database/sp_PdksPano.sql, sp_SatisPano.sql
   - Views/Admin/EditReport.cshtml dashboard bölümü
   - Views/Reports/Run.cshtml iframe render
   - wwwroot/assets/js/admin-report-form.js SP önizleme entegrasyonu
   - AdminController SpList, SpPreview endpoint'leri

7. docs: kullanıcı kılavuzu ve install notları
   - KULLANICI_KLAVUZU.md
   - INSTALL.md + README.md güncellemeleri
   - global.json
   - docs/ (bağlam yönetimi, araç önerileri, journal)
```

## Commit-Split Yardımcıları

- **Plan:** `commit-splitter` subagent (TODO FAZ 1 madde 5).
- **Komut listesi:**
  ```bash
  git status                           # ne değişmiş
  git diff --stat                      # kaç satır değişmiş
  git add <file1> <file2>              # bucket bazlı stage
  git commit -m "feat: <konu>"
  git log --oneline -10                # sonucu gör
  ```

## Zararlı Komutlar (kullanıcı onayı olmadan YASAK)

- `git push --force` / `git push -f` — history yeniden yaz.
- `git reset --hard` — uncommitted iş yok olur.
- `git clean -fd` — untracked dosyalar siler.
- `git rebase -i` — interactive rebase (otomatik olmaz).
- `git checkout .` / `git restore .` — tüm değişiklikleri at.

Bu komutlar gerekirse **açık onay** al: "Bu komutu çalıştırmam emin misin? Mevcut X dosya değişikliği kaybolacak."

## Git Hook'ları (planlı)

- **pre-commit (antipattern scan):** `DateTime.Now`, `async void`, `new HttpClient()`, hardcoded password tespiti.
- **post-commit (journal güncelleme):** `docs/journal/YYYY-MM-DD.md`'ye commit özeti.

Bunlar `.claude/hooks/` altında, `.claude/settings.json`'da kayıtlı.

## Plan-First Referansı (ADR-010)

**Tier 3 commit'lerde plan referansı zorunlu:**

```
feat(m-11): F-7 dashboard builder split-pane Razor (plan: 02)
```

Tier 1 ve Tier 2 commit'lerde plan referansı gereksiz.

Tier tespiti için: `.claude/rules/plan-first.md` Tier sinyalleri (3+ klasör, schema/security/UX, harici dep, kullanıcı-görünür).

Plan yoksa ama Tier 3 sinyali varsa:
- Kullanıcıya sor (mini-plan veya bypass)
- BYPASS: commit message'a `(plan: BYPASS-<tarih>)` + retro plan `plans/archive/`'a

Detay: `.claude/rules/plan-first.md`, `plans/README.md`, `docs/ADR/010-plan-first-tier-system.md`.
