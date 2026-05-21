# Plan 39 — Audit Hardening Batch (2026-05-21 sonrası)

## Problem
2026-05-20 oturum 6 sonu 5 paralel agent audit raporu: 1 CRITICAL + 14 HIGH. Aynı oturumda 7 fix yapıldı (CRITICAL Dashboard ILogger, HIGH AdminCreate authorize, AI ex.Message x3, AI JsonException x2, Documents Task.Run CT + XSS, Dashboard DateTime UTC). **Geri kalan 7 HIGH madde** saatler süren cerrahi gerektiriyor — bu plan onları batch'liyor.

## Scope
**Tier 3** — 4+ klasör, schema/security/UX, kullanıcı görünür, harici dep (HtmlSanitizer NuGet).

## Tahmin
~18-26 saat. 4 faz.

## Açık Maddeler

### Faz A — DashboardController CSV → junction (M-03 son kapanış) [2h]
- `DashboardController.cs:339-354` `AllowedForUser(r.AllowedRoles, userRolesCsv)` CSV → `r.ReportAllowedRoles.Any(ar => userRoleIds.Contains(ar.RoleId))` junction.
- `Recent Reports` listesi (line 163) aynı yönle.
- `AllowedForUser` helper sil.
- Smoke: admin + sıradan kullanıcı dashboard'da rapor listesi senkronize.
- `architecture.md` Bilinen Tutarsızlıklar #1'i kapat işareti.

### Faz B — HtmlSanitizer Block.Content stored XSS koruma [4h]
- NuGet: `HtmlSanitizer` 9.0.892 (zaten Mosaik.Modules.Circular csproj'da var, kullanılmıyor — kontrol).
- Render-time sanitize (save-time DEĞİL — backward compat mevcut DB kayıtları için):
  - `Mosaik.Modules.Circular/Areas/Circular/Views/Block/Edit.cshtml:100`
  - `Block/Details.cshtml:95` (varsa)
  - `Block/Create.cshtml`
  - `Circular/Print.cshtml:171`
  - `Circular/Details.cshtml:204`
- Razor helper veya partial `_SafeHtml.cshtml`: `@await Html.PartialAsync("_SafeHtml", Model.Content)`.
- Whitelist: `<p>`, `<br>`, `<strong>`, `<em>`, `<ul>`, `<ol>`, `<li>`, `<a href>`, `<img src>` (data: + javascript: URI block).

### Faz C — Hard-limit dosya split [6-8h]
1. **`Mosaik/Models/ReportPanelContext.cs` (591)** — file-class isim eşitle (`MosaikContext.cs` rename) + `OnModelCreating` partial method'lara böl (`MosaikContext.Reports.cs`, `MosaikContext.Workflow.cs`, `MosaikContext.Contracts.cs`, `MosaikContext.Tamim.cs`).
2. **`Mosaik/Controllers/AiController.cs` (587)** — zaten `AiController.Wizard.cs` partial mevcut. `ApplySuggestions` + OCR + suggestion mantığı `AiController.Suggestions.cs` partial'a.
3. **`Mosaik/wwwroot/assets/js/workflow-designer.js` (392)** — `workflow-designer-canvas.js` (drag/drop) + `workflow-designer-step-form.js` (step config panel) + `workflow-designer-validator.js`.

### Faz D — A11y batch (paralel agent ile) [6-8h]
4 paralel agent, her biri farklı view grubu:
1. **`<th scope="col">` ekle** — 91 case / 17 file. Global find/replace (`<th>` → `<th scope="col">`, satır başlığı için `scope="row"` manuel kontrol).
2. **`<label class="lab" for="X">` + `<input id="X">` bağı** — 93 case / 13 file. Form view'lar (CreateUser, EditReportV2, Contracts/Edit vb.).
3. **`<a href="#" class="icon-btn">` → `<button type="button">` + aria-label** — 15+ yer (yardım butonları).
4. **Tab pattern role="tablist"** — Notifications referansını 6 view'a replicate (Ai/Detail, Calendar, CreateReportV2, EditReportV2, OrgChart x2).
5. **Modal/Drawer `role="dialog" aria-modal`** — Documents, Calendar, OrgChart 3 view + focus trap.
6. **Decorative `<i class="fa-*">` aria-hidden="true"** — 380 case. Buton içeriği varsa zorunlu (regex find/replace).

## Reddedilen Alternatifler
- **HtmlSanitizer save-time**: backward compat eski DB kayıtları sanitize kalmaz, render-time her zaman çalışır.
- **A11y refactor manuel**: 200+ değişiklik, paralel agent şart.
- **Tüm Faz'ları tek commit'te**: rollback imkânsız.

## Riskler
- Faz B sanitizer agresif whitelist → mevcut tamim content rendering bozulur. Önce staging test.
- Faz C `MosaikContext` rename refactor → 50+ using statement (Mosaik.Modules.Circular dahil). Build kırılma riski.

## Done Criteria
- 7 HIGH audit maddesi kapalı (architecture I-1/I-3/I-4/I-6, security M-3/M-4 = security workflow auth-test full pass).
- 376 test yeşil her faz sonunda.
- A11y baseline: 0 `<th scope=>` eksik (en az 91 fix), label-for bağı 196 →186 (10 kalan acceptable nice-to-have).
- `architecture.md` Bilinen Tutarsızlıklar bölümü canlı durumla senkron.

## Rollback
Her faz ayrı PR. Faz B HtmlSanitizer false positive → render-time geri al + `@Html.Raw` korunur. Faz C MosaikContext kırılırsa git revert.

## Adımlar Özet
| # | Faz | Süre | Bağımlılık |
|---|---|---|---|
| 1 | A — Dashboard CSV junction | 2h | yok |
| 2 | B — HtmlSanitizer Block | 4h | yok |
| 3 | C-1 — MosaikContext rename + split | 3h | yok |
| 4 | C-2 — AiController split | 2h | yok |
| 5 | C-3 — workflow-designer.js split | 2h | Plan 36 stable |
| 6 | D — A11y batch (4-6 paralel agent) | 6-8h | yok |
| 7 | architecture.md + memory + TODO stale-claim sweep | 1h | Faz A-D bitince |

## Status: TASLAK (kullanıcı onayı bekliyor)
2026-05-20 yazıldı. Plan 36 Faz D (Documents/Obligations widget) tamamlandıktan sonra başlanabilir veya öncelikli sıraya alınabilir.
