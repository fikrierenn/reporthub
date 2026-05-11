# Büyük Değişiklik Öncesi Zorunlu Çek-Liste

_Kapsam: Silme, rename, refactor, route attribute kaldırma, kolon drop, view silme, controller action silme._
_Tetik: Bir feature 30+ satır etkileniyor veya kullanıcı-görünür davranış değişiyor._

## Neden bu dosya var

**2026-05-12:** Kullanıcı V2 Builder zaten aktif olduğu halde V1 EditReport/CreateReport view'larını refactor ettim. Sonra silinmeleri gerekti, refactor boşa gitti. Sebep: "hangi view canonical, hangi deprecated" sorgusu yapmadım. Tahmin ettim.

Kullanıcı kararı: **"Yapıya hakim olman için ne gerekiyorsa al."**

Önlem = bu çek-liste + `docs/ARCHITECTURE_MAP.md` + CLAUDE.md oturum başı 5. adım.

---

## Silme / Rename / Refactor öncesi adımlar

Her adım **sırayla** ve **atlanmadan**:

### 1. `docs/ARCHITECTURE_MAP.md` oku
- Canonical/deprecated tablosu (Bölüm 3) — silmek üzere olduğun şey **deprecated** olarak işaretli mi?
- Sidebar → route → view tablosu (Bölüm 2) — bu view sidebar'dan link veriyor mu?
- POST endpoint asimetrisi (Bölüm 4) — GET'i silersen POST kırılır mı?

### 2. Referans tara (zorunlu, atlanırsa hata yapılır)
```bash
# Tüm Razor + C# + JS + CSS'te isim ara
grep -rn "<view-veya-method-adı>" Mosaik/ Mosaik.Core/ Mosaik.Modules.*/ \
    --include="*.cshtml" --include="*.cs" --include="*.js" --include="*.css"

# Sadece linkler (href, asp-action, RedirectToAction):
grep -rn "<viewName>\|<actionName>" Mosaik/Views Mosaik.Modules.*/Areas
```

Sonuç **sıfır** olsa bile şüphelen — bir route attribute farklı string'le yazılmış olabilir.

### 3. Route conflict kontrol
- `[Route("…")]` attribute kaldırıyorsan → başka method o route'u yutuyor mu?
- `[HttpGet]` / `[HttpPost]` ayrı route'tan gidiyor mu?
- Convention-based default route `{controller}/{action}/{id?}` ile çakışma var mı?

### 4. Sidebar / Tablo / EmptyState / Overview link kontrol
- `Mosaik/Views/Shared/_AppLayout.cshtml` — sidebar render
- `Mosaik/Views/Admin/_Admin*Tab*.cshtml` — admin tab partial'ları
- `Mosaik/Views/Admin/_AdminOverview.cshtml` — admin dashboard hızlı eylem
- `Mosaik/Views/Shared/_EmptyState.cshtml` — boş state CTA'ları

### 5. Kullanıcıya net soru sor (TEHLİKELİ SİLME ÖNCESİ ZORUNLU)
Eğer şu durumlardan biri varsa **kullanıcı onayı olmadan SİLME**:
- View dosyası silme (kullanıcı kaybı yaşar)
- Controller action route'unu değiştirme (mevcut bookmark'lar kırılır)
- DB kolon drop (data loss)
- Migration rollback

Soru kalıbı: **"X canonical mi yoksa Y mi? X'i silersem Y aktif kalır mı, yoksa ikisi de gerekli mi?"**

### 6. Plan ekle (Tier 3 ise)
3+ klasör / yeni pattern / kullanıcı-görünür değişiklik → `.claude/rules/plan-first.md` Tier 3 ZORUNLU plan. Plansız refactor yasak.

### 7. Refactor sırasında parallel agent disipline
İş 5+ dosyaya yayılıyorsa → paralel `general-purpose` agent'lar (`feedback_subagent_skill_ana_prensip.md`). Tek tek elle yapma.

### 8. Refactor sonrası
- `dotnet build` → 0 hata 0 uyarı
- Preview start + smoke test (kritik path)
- `grep -c 'style="' <changed-view>` → inline style yasak
- `docs/ARCHITECTURE_MAP.md` GÜNCELLE — silinen/eklenen satırları yansıt, "2026-MM-DD silindi" notu
- Commit mesajında net belirt: "<X> silindi — <Y> canonical, eski URL <Z>'ye redirect"

---

## Anti-pattern (yaparsan kullanıcı haklı olarak kızar)

1. **"Sanırım bu kullanılmıyor" → silme.** Sanmak yetmez, **grep ile doğrula**.
2. **V1 var, V2 var → ikisi de canonical sandım** → ikisini refactor ettim. Yanlış. Önce ARCHITECTURE_MAP'e bak.
3. **GET silinince POST otomatik silinir** → yanlış. POST ayrı endpoint, başka yerden çağrılıyor olabilir.
4. **Sidebar link kontrol etmeden silme** → 404 yaratır.
5. **Plan yazmadan refactor** → kapsam patlar, geri alma zor.
6. **Tek başına refactor (5+ dosya)** → paralel agent kullan.

---

## Hata yaptığında

1. Kabul et — savunma yok.
2. `git revert` veya manuel restore.
3. Bu dosyaya örnek olarak ekle (yeni anti-pattern keşfedildiyse).
4. `docs/ARCHITECTURE_MAP.md`'ye stale ya da yanlış satır varsa düzelt.
5. Memory (`feedback_*.md`) ekle: aynı hatayı tekrarlamamak için.

---

## İlişkili

- `docs/ARCHITECTURE_MAP.md` — single source of truth, refactor öncesi 0. adım
- `CLAUDE.md` § 0 — oturum başı ritüel (5. madde bu dosyaya işaret eder)
- `.claude/rules/plan-first.md` — Tier 3 plan zorunlu
- `.claude/rules/session-protocol.md` — Adım 5 compliance scan (refactor sonrası)
- `.claude/rules/coding-discipline.md` — surgical changes, simplicity first
