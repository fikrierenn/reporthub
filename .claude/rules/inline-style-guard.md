# Inline Style Yasağı

_Kullanıcı kararı 2026-05-10. Bu kural `.cshtml` view yazarken / düzenlerken zorunludur. UI patterns ana referansı: `.claude/rules/ui-patterns.md`._

## Kural

**`style="..."` HTML attribute kullanımı sıfır toleranslı yasaktır.** Yeni veya düzenlenen `.cshtml` dosyalarında inline style attribute eklemek **kabul edilmez**.

## Tek istisnalar

- `style="display:contents"` form-as-grid pattern (max 3 yerde, hâlihazırda standart)
- `style="--w: @pct%"` veri-driven CSS variable (örn. dinamik bar grafik genişliği — `_DashMiniCharts.cshtml`)
- `Print.cshtml` view-local `<style>` block (Layout=null A4 print template)
- **Runtime/dynamic style** — GridStack drag-drop pozisyon/boyut attribute'ları, Chart.js canvas `width`/`height` runtime hesaplaması, Alpine `:style="cond ? 'right' : ''"` runtime evaluasyonu **istisna kapsamındadır** (Plan 33.2 kararı 2026-05-13). Sadece widget library/JS tarafından runtime yazılan stiller — manuel inline yazma yasak.

## Razor expression style de yasak

`style="@(condition ? "..." : "...")"` yerine class modifier kullan: `.pill.ok / .pill.error / .dot.ok / .dot.err`.

## Neden

- Kullanıcı 2026-05-09'da iki kez uyardı, 2026-05-10'da net karar: "satır için style kullanımı minimum yasak seviyesinde olmalı".
- Inline style refactor borcu zaten 30+ admin view'da ~1500+ oluşum (memory: `inline_css_refactor_pending`). Yeni view eklerken borca ekleme yasak.
- Tema değişikliği tüm view'larda yayılamıyor → Plan 12 Brand+Modules etkilenir.
- CSP (Content Security Policy) eklersek inline style kırılır.

## Doğru yol

1. Mevcut utility class kullan: `.btn.sm`, `.field`, `.lab`, `.inp`, `.form-section-card`, `.action-row`, `.grid-2col`, `.text-muted`, `.text-xs`, `.dot.ok`, `.pill.error`, `.alert.success`, `.help-text` vb.
2. Yoksa `Mosaik/wwwroot/assets/css/components.css`'e ekle (utility-first), tek tanım N view'da kullanılır.
3. Pattern doğru utility yoksa: `code-architect` ile yeni utility tartış, sonra ekle.

## CSS dosyası seçimi — ortak vs modül-özel (2026-05-13 kuralı)

| Pattern türü | Hedef CSS | Örnek |
|---|---|---|
| Generic (her modülde reuse edilebilir) | `Mosaik/wwwroot/assets/css/components.css` | filter-bar, table-card, cell-muted, info-card, content-card, paper-card, btn-count, status-pill, dashed-card |
| Sayfa-tipi pattern (admin dashboard, liste, form) | `components.css` | overview-*, kpi-tile, admin-* |
| Modül-spesifik (sadece o modülün özel bileşeni) | `components-<modül>.css` | tamim-envelope, tamim-quill, tamim-upload-zone, oc-pub-* (orgchart) |

**Kural:** Yeni utility eklerken kendine sor — "Başka bir modül de bu pattern'i kullanabilir mi?" Cevap **evet** → `components.css`. **Hayır, sadece bu modülün özel görsel öğesi** → modül CSS'i.

**Anti-pattern:** `.tamim-filterbar`, `.tamim-content-card`, `.tcc-*` gibi modül-prefixli ama generic olan sınıflar. Bunlar `components.css`'te `filter-bar`, `content-card` olarak yaşamalı (zaten varlardır veya buraya gelmeliler).

**Refactor agent prompt'una eklenecek satır:**
> Yeni utility eklerken — generic (başka modülde reuse edilebilir) ise `components.css`'e, modül-spesifik ise `components-<modül>.css`'e ekle. Mevcut generic'i modül CSS'inde yaratma; varsa onu kullan.

## Tarama

`Bash` ile `grep -rn 'style="' Mosaik/Views/` ile mevcut envanter görülür. Tam scan komutu için `general-purpose` agent çağır (envanter 2026-05-10: 70 dosya / 1369 oluşum). Inline style scan, oturum başı compliance scan'ın 4. agent'ı olarak otomatik çalışır (`session-protocol.md` Adım 5).

## Refactor agent'lara dağıtım (kullanıcı kararı 2026-05-10)

HIGH bulgular birden fazla view'a yayılırsa **tek başına refactor yapma**. Her bağımsız view için paralel agent (general-purpose veya frontend-design skill ile). Önce `components.css`'e utility paketi eklenir (shared dependency), sonra 4-8 paralel agent her biri 1 view dosyası refactor eder. Çakışma yok (farklı dosyalar).

## Borç durumu

Plan 25.1 Faz 1 → HIGH 181 oluşum (Plan 25 view'lar + bu oturumda dokunulanlar). Sonraki sprint MEDIUM ~700 (admin module). LOW ~417 → en son.

## Pre-commit hook

`.claude/hooks/inline-style-guard.sh` taslak yazıldı (DISABLED). Plan 25.1 Faz 1 refactor sonrası enable. Staged `.cshtml` dosyalarda yeni `style="` ekleme tespit ederse commit'i bloklar.
