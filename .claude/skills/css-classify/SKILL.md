---
name: css-classify
description: Yeni CSS utility class eklemeden ÖNCE zorunlu karar skill'i. "Bu sınıf components.css (generic, modüller-arası) mi yoksa components-<modül>.css (modül-özel) mi yoksa varolan bir sınıf yeniden mi kullanılır?" sorusunu 3 adımda cevaplar. Yeni view yazarken, inline-style refactor ederken her CSS ekleme kararında çağrılır.
---

# CSS Sınıflandırma Skill'i

## Amaç

Her yeni CSS pattern ihtiyacı doğduğunda — yeni view yazarken, inline-style
refactor ederken, agent yeni utility önerirken — **standart karar akışı** ile
sınıfın doğru CSS dosyasına gitmesini garanti eder.

Önlemek istediğimiz şey: agent veya elle eklenen `.tamim-filterbar`,
`.tamim-content-card`, `.tcc-*` gibi modül-prefix taşıyan ama gerçekte generic olan
sınıflar. Bunlar modül CSS'ini şişirir, başka modülde reuse edilemez, duplikasyon
yaratır. Plan 25.1 Batch B'de bu hata yapıldı, kural tekrar etmesin.

## Ne zaman tetiklenir

- Yeni `.cshtml` view yazarken / refactor ederken yeni CSS sınıfı eklemek üzere olduğunda
- Agent'a verilen prompt'ta "yeni utility ekleyebilirsin" denildiğinde — bu karar skill'i agent çağırır
- Refactor sonrası "yeni eklediğim sınıfları doğru yere mi koydum?" denetim için

## 3 adımlı karar akışı

### Adım 1 — Mevcut sınıfı ara

Bu pattern (örn. "tablo cell muted gri", "filter-bar search ikon", "content card body label")
zaten tanımlı mı? **Önce arama** yapılır:

```bash
# Önce components.css'te ara (generic)
grep -nE "^\.<pattern-kelimesi>" Mosaik/wwwroot/assets/css/components.css

# Sonra modül CSS'lerinde ara (rename gerek olabilir)
grep -rnE "^\.<pattern-kelimesi>" Mosaik/wwwroot/assets/css/components-*.css

# Üst sınıf da bul (örn. `.filter-bar` varsa onun child'larını da)
grep -nE "^\.<üst-sınıf>" Mosaik/wwwroot/assets/css/components.css
```

**Varsa:** Use it. Yeni class yaratma. Dur.

### Adım 2 — Modüller-arası reuse testi

Bu pattern başka bir modülde de işe yarar mı?
- Filter bar? **Evet** — Reports, Documents, Tamim, HR hepsi filtreli liste yapacak.
- Tablo cell muted? **Evet** — her tabloda olur.
- Content card (label + body + ekler)? **Evet** — Tamim, Documents, Compliance, vb.
- Status pill, badge? **Evet** — her yerde.
- Form section card? **Evet** — admin + modül form'ları.
- Modal overlay? **Evet** — her modülde modal olur.

→ **Generic** kategorisi. `components.css`'e gider. Class adı **modül-prefix olmadan**:
`.filter-bar`, `.content-card`, `.info-card`, `.btn-count`, `.cell-muted`.

### Adım 3 — Sadece modüle özgü mü?

Şu özelliklere bakar:
- Modülün kendine has görsel öğesi mi (Tamim'de Quill rich-text wrapper, OrgChart'ta chart node, Compliance'ta paket kartı)?
- Modüle özgü iş kuralı stilini taşıyor mu (Tamim "acil" işareti, Tamim "zarf/envelope" görünüm)?
- Başka modülün aynı pattern'i ihtiyacı olabilir mi? **Hayır.**

→ **Modül-özel** kategorisi. `components-<modül>.css`'e gider. Class adı **modül-prefix ile**:
`.tamim-envelope`, `.tamim-quill`, `.oc-pub-row`, `.builder-rs-preview`.

## Karar tablosu

| Soru | Cevap | Hedef |
|---|---|---|
| Zaten components.css'te var mı? | Evet | Use it (yeni yaratma) |
| Zaten modül CSS'te var ama generic mi? | Evet | **Taşı** components.css'e + rename |
| Başka modül de kullanır mı? | Evet | components.css (prefix-siz) |
| Sadece bu modülün özel görsel öğesi mi? | Evet | components-`<modül>`.css (prefix'li) |
| Şüphedeyim — generic mi modül-özel mi? | — | components.css (varsayılan generic; sonradan modül-özel olduğu anlaşılırsa taşı) |

## Anti-pattern'ler

❌ `.tamim-filterbar` — `filter-bar` zaten components.css'te var, prefix gereksiz
❌ `.tcc-label`, `.tcc-body` — content-card'ın parçası, generic
❌ `.btn-count` modül CSS'te tanımlı — her butonda olur, generic
❌ `.cell-muted`, `.cell-right`, `.row-actions` modül CSS'te — her tabloda olur
❌ `.tamim-info-card` — info-card generic, prefix gereksiz
❌ Yeni dosyaya inline `<style>` block — refactor borcu yaratır, hemen utility'e taşı

✅ `.tamim-envelope` — Tamim'in kendine özgü "zarf" üst başlık görünümü
✅ `.tamim-quill` — Quill rich-text editor wrapper (Tamim'e özel)
✅ `.tamim-urgent-toggle` — Tamim "acil" toggle
✅ `.oc-pub-row` — OrgChart public read-only row
✅ `.builder-rs-preview` — V2 Dashboard Builder result-set preview

## Class adı önerisi standartı

| Tür | Pattern | Örnek |
|---|---|---|
| Generic | BEM benzeri, prefix yok | `.content-card`, `.content-card__label`, `.filter-bar__field--grow` |
| Modül | `<modül>-` prefix + BEM | `.tamim-envelope__title`, `.oc-pub-row__count` |
| State/modifier | `is-` veya `--` | `.is-selected`, `.is-error`, `.btn--primary` (Mosaik'te `.btn.primary` zaten) |

## Refactor sırası — eski hatayı düzelt

Mevcut Tamim CSS'inde (Plan 25.1 Batch B'de yanlış eklendi) **şu generic'ler**
modül CSS'te kalmış — tespit edildiğinde sırayla taşı:

1. `.tamim-filterbar` + `.tfb-*` → `.filter-bar` varyantı (zaten var) + `.fb-icon/-grow`
2. `.tamim-content-card`, `.tcc-*` → `.content-card`, `.content-card__*`
3. `.tamim-info-card` → `.info-card`
4. `.tamim-inline-form` → `.inline-form` (zaten var)
5. `.tamim-subject-link` → `.subject-link`
6. `.tamim-urgent-icon` → `.urgent-icon` (acil işareti her listede olabilir)
7. `.btn-count`, `.cell-muted`, `.cell-right`, `.col-right`, `.row-actions` (zaten components.css'e taşındı, modülden sil)
8. `.paper-card`, `.dashed-card`, `.mini-chart`, `.count-suffix`, `.value-sm`, `.row-name`, `.chip-row`, `.hint-row` (taşı)
9. `.file-pill`, `.fp-ext-generic`, `.fp-size-dot` (Documents + Tamim ortak, taşı)

Tamim CSS'te kalacaklar (gerçekten modül-özel):
- `.tamim-envelope`, `.te-no`, `.te-no-sep`, `.te-no-urgent`, `.te-meta`
- `.tamim-quill`, `.tamim-quill__editor`
- `.tamim-urgent-toggle`
- `.tamim-upload-zone`, `.tamim-file-list/__row/__main/__ext/__name/__size`, `.tamim-empty-files`
- `.tamim-form-row-type-subject`

## Bu skill'i nasıl çağırırım

Yeni view/refactor sırasında:

```
1. İhtiyacın olan CSS pattern'i tarif et (1 cümle)
2. Adım 1: grep ile mevcut sınıfı ara (components.css → modül CSS)
3. Bulunduysa → use it, dur.
4. Bulunmadıysa → Adım 2 (reuse testi) ve Adım 3 (modül-özel testi).
5. Sonuç: hangi dosyaya, hangi class adıyla?
6. CSS ekle (doğru dosyaya).
7. View'da kullan.
```

Agent'a verilen prompt'ta da bu skill'in özeti olmalı — "yeni utility ekleyeceksen
**önce components.css'i tara**, generic'i orada yarat, modül-prefix sadece gerçek
modül-özel öğeler için".

## İlişkili

- `.claude/rules/inline-style-guard.md` § "CSS dosyası seçimi" — bu skill'in özeti
- `.claude/rules/ui-patterns.md` — UI standardı (utility class referansı)
- `docs/MOSAIK_DESIGN_PROMPT.md` — design üretme promptu (utility tablosu)
- `Mosaik/wwwroot/assets/css/components.css` — generic utility'ler (single source)
- `Mosaik/wwwroot/assets/css/components-tamim.css`, `org-chart.css`, `builder-v2.css` — modül-özel
