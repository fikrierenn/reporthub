# ADR-005 · Dashboard mimarisi: Config-driven JSON tek source-of-truth, DashboardHtml drop

- **Durum:** Kabul edildi (22 Nisan 2026)
- **Etkilenen:** `ReportCatalog` entity + DB şeması, `DashboardRenderer`, `ReportsController.Run`, admin rapor form'u (Create/EditReport.cshtml), `ReportManagementService`, dashboard builder JS
- **İlgili TODO:** M-05 (BIRLESIK ONCELIK SIRASI Faz 2)

## Bağlam

Dashboard modülü iki ayrı yazım yoluyla doğdu:

1. **Inline HTML template** — `ReportCatalog.DashboardHtml` (NVARCHAR(MAX)). Admin bir `<html>` şablonu yazar, renderer `{{RESULT_0}}` gibi placeholder'ları SP sonuçlarıyla replace eder, iframe `srcdoc` ile render edilir. Migration: `10_AddDashboardColumns.sql`.
2. **Config-driven JSON** — `ReportCatalog.DashboardConfigJson` (NVARCHAR(MAX)). Drag-drop builder'da KPI/Chart/Table bileşenleri seçilir, JSON config'e serialize edilir, server-side `DashboardRenderer` config'i DOM API (`createElement` + `textContent`) ile güvenli HTML'e çevirir. Migration: `11_AddDashboardConfigJson.sql`, seed'ler `12_`/`14_`.

İki alan yan yana yaşadı:

- `ReportsController.Run`: hem HasConfig hem HasHtml kontrol edip ConfigJson'u tercih ediyordu, config yoksa HTML fallback yapıyordu.
- Admin form'unda mode switcher vardı (`switchDashMode('builder')` / `switchDashMode('html')`) — iki ayrı textarea.
- `ReportManagementService.CreateAsync/UpdateAsync` ikisini de bir DTO'da taşıyordu.

**Sorun:**
- **Güvenlik:** `DashboardHtml` inline template path'i admin-authored HTML → iframe sandbox sınırlı XSS koruması. `DashboardRenderer` (config path) DOM API + `textContent` ile çok daha sıkı.
- **Karmaşıklık:** Dual storage = belirsizlik. Hangisi source-of-truth? İkisi dolu olursa? Renderer tercih etmeli (yapılmış) ama admin kafası karışıyor.
- **Builder'ı köreltiyor:** Drag-drop builder config üretiyor, HTML textarea paralel mode olarak varken iki yolu da korumak gerekiyor.
- **Dokümantasyon yükü:** iki yol = iki set kural, iki set test.

## Karar

**`DashboardConfigJson` dashboard tanımının tek resmî kaynağıdır.** `DashboardHtml` kolonu, entity property'si ve renderer fallback kodu kaldırılır. Geçiş üç fazlıdır:

### Faz A — Builder'ın canonical hale getirilmesi (önceki iş, Nisan 2026 başı)

- `wwwroot/assets/js/dashboard-builder.js` drag-drop + component toolbox + `DashboardConfigJson` textarea'yı populate eder.
- `DashboardRenderer` config'i DOM API ile güvenli HTML'e çevirir (XSS fix'leriyle: `createElement` + `textContent`, `eval()` yok, `innerHTML` yok).
- Admin form'unda iki mode paralel yaşar.

### Faz B — Legacy retirement (commit `a2feb5d`, 22 Nisan 2026)

- `Models/ReportCatalog.DashboardHtml` → `[Obsolete]` + nullable XML doc.
- `Models/ReportPanelContext` → `#pragma warning disable CS0618` + `entity.Property(e => e.DashboardHtml)` map'i korundu.
- `Services/ReportManagementService.ReportFormInput`'tan `DashboardHtml` parametresi kaldırıldı. Create/Update artık yalnızca `DashboardConfigJson` yazar.
- `Controllers/AdminController.BuildReportFormInput`'ta `DashboardHtml` okuma kaldırıldı.
- `Controllers/ReportsController.Run`: legacy fallback path korundu **ama** her tetiklendiğinde `dashboard_html_legacy_render` audit log event'i yazılıyor. Amaç: Faz C öncesi "gerçek kullanım var mı?" sorusunu ölçülebilir kılmak.
- `Views/Admin/{Create,Edit}Report.cshtml`: mode switcher (`switchDashMode`) + HTML textarea kaldırıldı. Yalnızca `#dashboardBuilder` görünür (ama wrapper id hâlâ `dashboardHtmlSection` — Faz C'de yeniden adlandırılır).
- `Database/16_DeprecateDashboardHtml.sql`: idempotent orphan check (HTML-only aktif dashboard sayısını PRINT eder), **DDL yok**.

### Faz C — Kolonu drop + property sil (bu iş, 22 Nisan 2026)

Faz C için iki kriter net olmalıydı:

1. **Orphan check = 0.** `DashboardHtml` dolu ama `DashboardConfigJson` boş bir aktif dashboard kalmamış. `mcp__lokaldb__sql_query` PortalHUB snapshot (22 Nisan öğleden sonra):

   | Total | HasDashboardHtml | HasDashboardConfigJson | HtmlOnlyOrphans | BothPresent |
   |---|---|---|---|---|
   | 4 | 0 | 2 | **0** | 0 |

   → Yol açık. DashboardHtml kolonu tüm satırlarda NULL/empty.

2. **`dashboard_html_legacy_render` audit event = 0.** Faz B'den bu yana (22 Nisan öğle) audit log'ta bu event'ten hiç oluşmamış. Zaman penceresi kısa (aynı gün) ama orphan = 0 olduğu için legacy code path **yapısal olarak tetiklenemez**; zaman penceresi burada ikincil bir güvence.

Faz C aksiyonları:

- `Database/17_DropDashboardHtml.sql`: idempotent DROP COLUMN. Orphan check Güvenlik ağı olarak önce çalışır, >0 ise RAISERROR ile abort.
- `Models/ReportCatalog`: `DashboardHtml` property silindi.
- `Models/ReportPanelContext`: pragma + property map satırları silindi.
- `Controllers/ReportsController.Run`: legacy fallback branch silindi. Dashboard raporu `DashboardConfigJson` olmadan çalıştırılırsa `dashboard_config_missing` audit event'i yazılır ve boş config ile render edilir (invalid JSON ile aynı davranış).
- `Controllers/ReportsController.RenderDashboardTemplate` method silindi.
- `Views/Admin/{Create,Edit}Report.cshtml`: wrapper id `dashboardHtmlSection` → `dashboardConfigSection`. M-05 yorum satırları temizlendi.

**Veri etkisi:** Yok. Orphan = 0 garantisi sağlandı.

## Alternatifler

- **(A) Dual storage'ı koru** — mevcut Faz A durumu. Sürekli "hangisi source?" sorusu, builder'ın tek yol olması avantajını zayıflatır. **Red.**
- **(B) HTML'i koru, ConfigJson'u drop et** — admin inline HTML = güvenlik sürtüşmesi (iframe sandbox'a rağmen CSP, XSS yönetimi), drag-drop builder avantajı kaybedilir. **Red.**
- **(C) ConfigJson birincil, HTML drop (seçilen)** — bu ADR. Güvenli renderer, tek builder, dokümantasyon tek yol, admin zihin yükü düşer. Üç fazlı geçiş orphan riski sıfırladı. **Kabul.**

## Sonuçlar

**Olumlu:**
- Dashboard authoring tek yol: drag-drop builder → ConfigJson. Admin için kafa karışıklığı yok.
- XSS surface daralır: `DashboardRenderer` DOM API, admin-authored HTML template yok.
- `ReportsController.Run` dashboard branch'i sadeleşir (bir if, iki değil).
- `RenderDashboardTemplate` (~60 satır regex + string manipulation) silinir — maintenance yükü düşer.
- Migration 17 tek seferlik idempotent.

**Olumsuz / dikkat:**
- Geri dönüş pahalı: kolon drop geri alınamaz (migration rollback script yok; ihtiyaç doğarsa restore-from-backup + migration 16 revert).
- `DashboardRenderer` bir raporun builder-in-üretim config'ini okuyamazsa (bozuk JSON) `dashboard_config_invalid` log'u + boş dashboard — kullanıcı experience kötü ama sessiz 500 değil. Bu davranış Faz B'den beri var.
- `dashboardHtmlSection` id rename (Faz C içinde) eski CSS/JS referansı varsa kırar — regresyon: `grep -r dashboardHtmlSection` temiz olmalı.

## Referanslar

- `TODO.md` → "BIRLESIK ONCELIK SIRASI" → Faz 2 M-05
- `.claude/rules/architecture.md` → "Dashboard Mimarisi"
- `Database/16_DeprecateDashboardHtml.sql` (Faz B orphan check), `Database/17_DropDashboardHtml.sql` (Faz C DROP)
- İlgili commit'ler: Faz B `a2feb5d`, Faz C bu PR.
- Dashboard render güvenliği: `Services/DashboardRenderer.cs` + `.claude/rules/security-principles.md` #2.
