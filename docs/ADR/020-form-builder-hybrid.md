# ADR-020 — Form Builder Hybrid: SurveyJS Form Library Renderer + DIY Builder UI

**Tarih:** 2026-05-21
**Statü:** Önerildi (Plan 41 Faz 0 başlamadan onay)
**Karar verenler:** Fikri / Claude
**Bağlam:** Plan 41 Form Builder; OSS araştırma `docs/RESEARCH_OSS_VNEXT_2026-05-21.md`

---

## 1. Bağlam

Plan 41 Form Builder (Hybrid yaklaşım: v1 JSON config + v2 builder UI) yazıldı. Form renderer + builder UI altyapısı için OSS reuse araştırması yapıldı (Form Builder agent — 1500 kelime brief).

## 2. Karar

**Form Builder = SurveyJS Form Library (MIT) renderer partial reuse + DIY drag-drop builder UI + signature_pad (MIT) doğrudan reuse + honeypot/time-based AntiSpam pattern.**

### 2.1 SurveyJS Form Library (MIT) — Renderer kısmı
- NuGet/JS olarak `wwwroot/lib/surveyjs/` UMD bundle (CDN değil — Kaspersky/SSL riski + offline kurum env)
- Versiyon pin (v3.x)
- Knockout legacy ~250kb minified → ayrı bundle (load-on-demand)
- `Serializer.addProperty("question", { name: "dataElementId", category: "kvkk" })` ile KVKK DataElement custom field metadata (Plan 40 entegrasyon)
- AES encryption flag (`encrypted: true`) custom property
- Türkçe lokalizasyon dahili (`surveyLocalization.defaultLocale = "tr"`)
- Conditional logic (`visibleIf`, `enableIf`), validation, multi-step wizard, dosya upload widget'ı kutudan çıkar

### 2.2 DIY Drag-drop Builder UI (Mosaik)
- Plan 41 Faz B-C kapsamında
- SurveyJS JSON producer formatına yazar
- Survey Creator ticari (~£422/dev/yıl) ALINMIYOR
- Tam Mosaik tarzı: tokens.css + components.css canonical

### 2.3 signature_pad 5.1.1 (MIT, szimek)
- UMD ~5KB, sıfır bağımlılık
- `wwwroot/assets/js/form-signature.js` IIFE wrapper
- Mobile touch + desktop mouse + Apple Pencil/Wacom
- Canvas → PNG base64 → Documents upload pipeline (App_Data private + path guard, BUGFIX-3 pattern)
- ~2 saat entegrasyon

### 2.4 AntiSpam 3 katman
1. **Honeypot field** (zorunlu, 5 satır Razor + server-side check) — `<input type="text" name="hp_url" tabindex="-1" autocomplete="off" style="position:absolute;left:-9999px">`
2. **Time-based check** (zorunlu, 5 satır kod) — `submittedAt - openedAt < 3sn → bot`
3. **Cloudflare Turnstile** (opsiyonel, yüksek risk endpoint için) — `FormDefinition.UseRecaptcha` toggle, "no PII collection" KVKK-friendly

## 3. Reddedilen Alternatifler

| Aday | Lisans | Reddetme gerekçesi |
|---|---|---|
| **Survey Creator (ticari)** | ~£422/dev/yıl | Builder UI Mosaik'te kendi yazılır (Plan 41 zaten kapsamda) — UI tam Mosaik tarzı (tokens.css canonical) |
| **Formbricks** | AGPLv3, 12.3k★ | Viral lisans → Mosaik'in tüm kodu AGPLv3 olur (multi-firma SaaS riski kabul edilemez), stack Next.js |
| **LimeSurvey** | GPL-2.0, 4k★ | PHP+MySQL ayrı uygulama, stack uyumsuz |
| **formio.js** | OSL-3.0 (copyleft kaynaklı) | Lisans riski (copyleft) |
| **Tripetto FormBuilder SDK** | Ticari kapalı | Proprietary |
| **BlazorForms / Whyvra.Blazor.Forms** | MIT | Blazor only, Mosaik MVC+Razor+Vanilla JS uyumsuz |
| **kevinchappell/formBuilder** | MIT, 9.2k★ | jQuery bağımlı, Mosaik vanilla IIFE+Alpine (jQuery yok) |
| **json-editor / Alpaca** | MIT/Apache | Bootstrap/jQuery UI legacy stack |
| **Syncfusion Dynamic Form** | Proprietary | Ticari |
| **reCAPTCHA v3** | Google SaaS | KVKK riski, Google veri aktarımı |

## 4. Effort Etkisi

| Bileşen | Baseline | Reuse ile | Tasarruf |
|---|---|---|---|
| Renderer (Faz 1 Render + Submit) | ~12h | ~4h | %70 |
| Signature widget (Faz 4) | ~6h | ~1h | %85 |
| AntiSpam (Faz 3) | ~4h | ~2h | %50 |
| Builder UI (Faz 2) | ~30h korunur | ~30h | 0 (kendi yazılır) |
| KVKK DataElement entegrasyonu (Faz 4) | ~10h korunur | ~10h | 0 |
| Workflow trigger (Faz 6/7) | korunur | korunur | 0 |

**Net Plan 41 effort tasarrufu: ~%30-35** (~62h → ~42h)

## 5. Riskler

| Risk | Önlem |
|---|---|
| SurveyJS Knockout legacy ~250kb bundle | Ayrı load-on-demand bundle; dashboard iframe gibi |
| SurveyJS sürüm breaking | v3.x pin + smoke test |
| `Serializer.addProperty` runtime hata custom property tip mismatch | Mapper unit test (form Definition → SurveyJS JSON) |
| Cloudflare Turnstile bağımlılık dış servis | DSAR/ihbar yüksek risk endpoint sadece; admin toggle ile kapat |

## 6. Sonuç

JS asset listesi (`wwwroot/lib/`):
- SurveyJS Form Library UMD v3.x
- signature_pad UMD v5.1.1

Plan 41 §3 Alternatifler bölümüne OSS reuse stack tablosu eklendi.

Plan 41 effort 54-70h → ~42h (en olası, %30-35 tasarruf).
