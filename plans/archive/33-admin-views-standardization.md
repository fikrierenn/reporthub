# Plan 33 — Yönetim Paneli Ekranları Standardizasyonu

**Tarih:** 2026-05-12 (yazıldı) → 2026-05-13 (tamamlandı)
**Yazan:** Claude
**Durum:** ✅ **TAMAMLANDI** (296 inline silindi, 12 view standardize, 5 commit)
**İlişkili:** Plan 23 (Sidebar + Overview Dashboard), Plan 25.1 Faz 1 (Inline Style Refactor)
**Follow-up:** Plan 33.2 (V2 Dashboard Builder — CreateReportV2 + EditReportV2, scope dışı bırakılmıştı)

---

## 1. Problem

Plan 23 ile `/Admin` overview dashboard standart UI'a oturdu (`_AdminOverview.cshtml`, inline=0).
Ama **`Mosaik/Views/Admin/` altındaki 19 ekran** birbirinden farklı:

| Ekran                  | Layout | Hero | TopActions | form-section-card | Inline `style="` |
|------------------------|:------:|:----:|:----------:|:-----------------:|:----------------:|
| CreateUser ⭐          | ✓ | ✓ | ✓ | ✓ | **0** |
| EditUser ⭐            | ✓ | ✓ | ✓ | ✓ | **0** |
| EditPosition ⭐        | ✓ | ✓ | ✓ | ✓ | **0** |
| _AdminOverview ⭐ (yeni)| ✓ | (n/a) | (n/a) | (overview-panel) | **0** |
| CreateFilter           | ✓ | ✓ | ✓ | ✗ | 7   |
| EditFilter             | ✓ | ✓ | ✓ | ✗ | 7   |
| EditGroup              | ✓ | ✓ | ✓ | ✗ | 16  |
| EditRole               | ✓ | ✓ | ✓ | ✗ | 16  |
| Lookup                 | ✓ | ✓ | ✓ | ✗ | 16  |
| Modules                | ✓ | ✓ | ✓ | ✗ | 16  |
| BrandSettings          | ✓ | ✓ | ✓ | ✗ | 21  |
| AiSettings             | ✓ | ✓ | ✓ | ✗ | 30  |
| CreateDataSource       | ✓ | ✓ | ✓ | ✗ | 36  |
| EditDataSource         | ✓ | ✓ | ✓ | ✗ | 37  |
| OrgChart               | ✓ | ✓ | ✓ | ✗ | 39  |
| AiSettingsEdit         | ✓ | ✓ | ✓ | ✗ | 55  |
| CreateReport           | ✓ | ✓ | ✓ | ✗ | 71  |
| EditReport             | ✓ | ✓ | ✓ | ✗ | 75  |
| CreateReportV2         | ✓ | ✗ | ✓ | ✗ | 81  |
| EditReportV2           | ✓ | ✗ | ✓ | ✗ | 97  |

**Toplam:** ~640 inline style oluşumu admin module'de. Plan 25.1 Faz 1 borcunun büyük kısmı.

**Belirti:** Yeni kullanıcı bir admin ekranından diğerine geçince stil/tutarlılık eksikliği hissediyor (kullanıcı geri bildirimi 2026-05-12: "hepsi aynı değil").

## 2. Scope

### Kapsam dahili
- 16 admin view'ı **Form A pattern**'e (CreateUser/EditUser golden reference) uyumlu hale getir:
  - `form-section-card` + `form-section-head` + `form-section-body.stack` + `action-row`
  - Inline `style="…"` sıfıra indir → `.field` / `.lab` / `.inp` / `.btn` / utility class
  - Razor `@(cond ? "x:y" : "x:z")` style → class modifier (`.pill.ok` vb)
- CreateReportV2 + EditReportV2 (Dashboard Builder V2) **scope DIŞI** — kendine özgü canvas/drawer UI, ayrı plan (M-11 follow-up)
- BrandSettings + Modules — `Html.BeginForm` → raw `<form method="post">` (mevcut sapma)
- OrgChart — özel hibrit (chart canvas + form), inline=39, refactor zor → ayrı sub-task
- AiSettings + AiSettingsEdit — sözleşme önizleme bölümü inline grid'ler, custom pattern (.preview-grid?) ihtiyacı

### Kapsam dışı
- Yeni feature ekleme (sadece visual standardization)
- Server-side controller refactor (data load aynı)
- Tablo refactor — admin tab partial'ları (_AdminTabUsers vb) bu plan dışı (sonraki sprint)
- V2 Builder layout refactor (ayrı plan)
- Mobile responsive optimizasyon (mevcut breakpoint'ler korunur)

### Etkilenen dosyalar (16 view + opsiyonel components.css ekleri)
- Mosaik/Views/Admin/{AiSettings, AiSettingsEdit, BrandSettings, CreateDataSource, CreateFilter, CreateReport, EditDataSource, EditFilter, EditGroup, EditReport, EditRole, Lookup, Modules, OrgChart}.cshtml
- Mosaik/wwwroot/assets/css/components.css — yeni utility ihtiyacı (preview-grid, json-viewer vb)

**Tahmini boyut:** 14 view refactor + ~2-3 yeni utility class. Tier 3.

## 3. Alternatifler

### A: Tek tek elle refactor (1 oturum / 1 ekran)
**Reddetme sebebi:** 14 view × 30-90 dk = 1-2 hafta. Çok yavaş.

### B: Paralel agent dağıtımı (SEÇİLEN)
**Açıklama:** Önce shared CSS utility paketi (1 commit), sonra 4-7 paralel `general-purpose` agent her biri 1-2 view refactor. Çakışma yok (farklı dosyalar).
**Sebep:** `feedback_inline_css_refactor_pending.md` + `inline-style-guard.md` kuralı: "HIGH bulgular birden fazla view'a yayılırsa tek başına refactor yapma — agent'lara dağıt".

### C: Otomatik dönüştürme (regex)
**Reddetme sebebi:** Inline style'ların yarısı semantik karar gerektirir (örn. `style="color:red"` → `.text-danger` mi `.dot.err` mi context-dependent). AI yargısı şart.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Refactor sırasında ekran kırılır | yüksek | düşük | Her view'ı paralel build et + smoke test (preview_eval ile sayfa render kontrol) |
| Yeni utility class chaos (her agent kendi class adı uydurur) | orta | yüksek | Plan 33 öncesi tek "shared CSS" commit'i, agent'lara fixed class listesi verilir |
| Agent yüzeysel refactor — sadece görsel, semantik bozuk | orta | orta | Agent prompt'unda "form alanları aria-label korunsun" + golden reference (CreateUser.cshtml) zorunlu okuma |
| Inline=0 ama görsel regresyon | orta | düşük | preview_screenshot before/after karşılaştır (kritik ekranlar: BrandSettings, OrgChart) |
| OrgChart (chart canvas + drag-drop) refactor zor | yüksek | yüksek | OrgChart bu plandan ayırın, ayrı sub-plan (Plan 33.1) |
| CreateReport/EditReport (75 inline, dashboard config builder) — V1 builder, hâlâ kullanılıyor mu? | orta | düşük | Önce kullanıcıya sor: V1 builder hâlâ kullanılıyor mu yoksa V2 yetiyor mu? Eğer deprecate edilmişse refactor değil delete |

## 5. Done Criteria

- [x] 12 view inline style **= 0** (AiSettingsEdit 55, CreateDataSource 36, EditDataSource 37, AiSettings 30, BrandSettings 21, EditGroup 16, EditRole 16, Lookup 16, Modules 16, CreateFilter 7, EditFilter 7, OrgChart 39)
- [x] Form view'lar `form-section-card` + `form-section-head` + `form-section-body` yapısında
- [x] `action-row` (sol Geri Dön ghost + sağ Kaydet primary) tüm form'larda
- [x] `_AlertMessage` partial kullanılır (inline alert div'ler kaldırıldı)
- [x] `Html.BeginForm` → raw `<form>` (BrandSettings + Modules)
- [x] Razor `@(...)` style expression yok — class modifier kullanılır
- [x] Build temiz (0 hata 0 uyarı — her commit)
- [x] Smoke test: preview start + 12 endpoint cevap (auth redirect, render hatası yok)
- [x] OrgChart Plan 33 kapsamında refactor edildi (sub-plan 33.1 gerekmedi; org-chart.css'e .oc-* utility paketi eklendi)
- [x] V1 Report Builder silindi (kullanıcı kararı: "v2 var zaten") — 567 satır net silim
- [ ] CreateReportV2 + EditReportV2 → **Plan 33.2** ayrı plan (178 inline toplam, canvas/drawer/qb widget UI)

## 6. Rollback Planı

- Git revert (commit-per-view yapı, 14 commit beklenir)
- Eski view'lar restore (`git checkout HEAD~N Mosaik/Views/Admin/<file>.cshtml`)
- components.css'e eklenen utility'ler ayrı commit'te → ayrı revert

## 7. Adımlar

1. [ ] **Faz 0 — Karar:** V1 Builder (CreateReport/EditReport) hâlâ kullanılıyor mu? Yoksa delete (kullanıcı yanıtına göre)
2. [ ] **Faz 1 — Shared CSS:** `components.css`'e gerekli utility'ler (preview-grid, json-viewer, settings-card vb) tek commit'te
3. [ ] **Faz 2 — Paralel refactor (3-4 batch):**
   - Batch A (kolay, 7-16 inline): CreateFilter, EditFilter, EditGroup, EditRole, Lookup, Modules → 1 agent batch
   - Batch B (orta, 21-36): BrandSettings, AiSettings, CreateDataSource, EditDataSource → 1 agent batch
   - Batch C (zor, 55-75): AiSettingsEdit, CreateReport, EditReport → 1 agent + manuel review
   - Batch D (özel, OrgChart): Plan 33.1'e ertelenir
4. [ ] **Faz 3 — Smoke test:**
   - Build temiz
   - Preview her ekran açılır
   - Form submit çalışır (en az 1 örnek field doldurup post)
   - Mobile breakpoint kontrol (sidebar drawer + form)
5. [ ] **Faz 4 — Cleanup + commit-split:** 14 commit (1 view = 1 commit) + 1 shared CSS commit + 1 doc commit
6. [ ] **Faz 5 — pre-commit inline-style hook enable:** Plan 25.1 ile bağlantılı

## 8. Agent Prompt Şablonu (paralel dağıtım için)

```
Görev: Mosaik/Views/Admin/<X>.cshtml dosyasını Plan 33 standardına refactor et.

Scope: Sadece bu tek dosya. Başka dosyaya dokunma.
Etiket: Mosaik UI standardı

Zorunlu okumalar (sırayla):
1. docs/MOSAIK_DESIGN_PROMPT.md — tüm utility class referansı
2. Mosaik/Views/Admin/CreateUser.cshtml — golden reference (inline=0, form-section-card kullanır)
3. .claude/rules/inline-style-guard.md — inline style yasak detayları
4. .claude/rules/ui-patterns.md — pattern standardı

Yapacaklarn:
- Inline `style="…"` attribute'larını → utility class veya yeni class (gerekirse `components.css`'e ekle, ama once mevcut class'lari tara)
- `form-section-card` + `form-section-head` + `form-section-body.stack` yapısına sok
- `action-row` ile sol Geri + sağ Kaydet
- `Html.BeginForm` → raw `<form method="post">` + `@Html.AntiForgeryToken()`
- Razor `style="@(c ? "x" : "y")"` → class modifier
- Alert div → `@await Html.PartialAsync("_AlertMessage", (type, message))`

YAPMAYACAKLAR:
- Yeni özellik / form alanı ekleme
- ViewModel veya Controller değiştirme
- Diğer view'lara dokunma
- Inline JS taşımak/değiştirmek (gerekiyorsa "wwwroot/assets/js/ taşı" notu bırak)

Done tanımı:
- `grep -c 'style="' <file>` → 0 (istisnalar: display:contents, --w:%)
- `dotnet build Mosaik/Mosaik.csproj --nologo` → 0 hata 0 uyarı
- Görsel regresyon yok (preview_screenshot karşılaştırması iste)

Raporla:
- Hangi class'ları kullandın (yeni eklenen varsa neden)
- Kaldırılan inline style sayısı (önce / sonra)
- Karşılaştığın belirsiz karar varsa not olarak bırak
```

## 9. Açık Sorular

1. **V1 Builder (CreateReport + EditReport — 146 inline toplam):** Hâlâ kullanılıyor mu yoksa V2 yetiyor mu? Deprecate edebilirsek refactor yerine **delete** çok daha hızlı.
2. **OrgChart:** Bu plan içinde mi yoksa Plan 33.1 olarak ayrı mı? (Önerim: ayrı — chart canvas refactor kapsamı farklı)
3. **AiSettings + AiSettingsEdit** preview grid'leri (LLM provider listesi) — yeni `.settings-card` pattern mı, mevcut `.form-section-card` yeterli mi?
4. **Smoke test ölçütü:** Hangi 3-5 kritik ekran mutlaka preview ile manuel test edilmeli? (Önerim: BrandSettings, EditUser, EditRole, EditReport, OrgChart)
5. **Paralel agent sayısı:** Tek mesajda 4-7 agent çağrılır — sistemin OK mi, yoksa 2-3 batch sequential mı?

## 10. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Soru 1-5 cevaplandı
- [ ] Onay alındı: ___
- [ ] Faz 1 başladı (shared CSS)
