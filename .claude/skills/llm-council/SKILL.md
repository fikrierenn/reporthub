# LLM Council Skill

_Karpathy'nin LLM Council metodolojisi. Gerçek belirsizlik + yüksek maliyet kararlar için._

## Tetikleyiciler

**Kesin tetikleyiciler:** "council this", "war room this", "pressure-test this", "stress-test this", "debate this", "/council-this"

**Güçlü tetikleyiciler** (gerçek bir karar tradeoff'u varsa):
- "şunu mu yapayım bunu mu", "hangi seçenek", "doğru hamle mi", "validate et", "kararsız kaldım"

**Tetikleme — YAPMA:** Basit evet/hayır, tek doğru cevabı olan, ya da "markdown kullanalım mı" gibi stakes'siz sorularda.

## Ne Zaman Kullan

- Mimari kararlar: "EF Core migrations mı, raw SQL mi?"
- Önceliklendirme: "Plan 13'te hangi modül önce?"
- Scope kararları: "AdminController'ı bu sprint mi servis layer'a ayıralım?"
- Teknoloji seçimi: "Alpine.js mi, vanilla JS mi?"

**KULLANMA:** Zaten scopelanmış, onaylanmış planlarda. Plan-First sistemi onları yönetir.

---

## Süreç (4 adım)

### Adım 1 — Soruyu çerçevele

Kullanıcı sorusunu + CLAUDE.md / journal / plan bağlamını birleştirerek **tarafsız, net bir karar çerçevesi** yaz. Yönlendirme yok, kendi görüşünü ekleme.

### Adım 2 — 5 Danışmanı paralel çalıştır

**Hepsini aynı anda** spawn et (sequential değil). Her biri 150-300 kelime, kendi lensinden — hedge etmeden, dengeleymeye çalışmadan.

| Danışman | Lens |
|---|---|
| **Contrarian** | Fatal flaw'u bul. Plan başarısız olursa neden olur? |
| **First Principles** | Yanlış soru mu soruyoruz? Gerçek problem ne? |
| **Expansionist** | Kaçırılan upside ne? Scope çok mu dar? |
| **Outsider** | Koda/projeye yabancı biri ne garip bulur? |
| **Executor** | İlk somut adım ne? Pazartesi ne yapılır? |

**Her danışmana prompt:**
```
Sen bir LLM Council'da [Danışman Adı] rolündesin.

Düşünce lensin: [yukarıdaki lens açıklaması]

Karar sorusu:
---
[çerçevelenmiş soru]
---

Perspektifinden doğrudan yaz. Hedge etme, dengelemeye çalışma. 
Diğer danışmanlar diğer açıları kapatacak.
150-300 kelime. Başlık yok, direkt analiz.
```

### Adım 3 — Anonim peer review (paralel)

5 yanıtı A-E olarak anonim et (rastgele harfle eşleştir). 5 reviewer'ı aynı anda spawn et, her biri şunu yanıtlar:

1. En güçlü yanıt hangisi ve neden?
2. En büyük blind spot nerede?
3. Hepsinin kaçırdığı nedir?

**Her reviewer'a prompt:**
```
LLM Council çıktılarını review ediyorsun.

Soru: [çerçevelenmiş soru]

Yanıtlar:
**A:** [yanıt]
**B:** [yanıt]  
**C:** [yanıt]
**D:** [yanıt]
**E:** [yanıt]

1. En güçlü yanıt hangisi, neden?
2. En büyük blind spot hangi yanıtta, nedir?
3. Hepsinin kaçırdığı nedir?

200 kelime altı, direkt.
```

### Adım 4 — Chairman sentezi

Tüm yanıtlar + peer review'ları alan chairman şu formatı üretir:

```markdown
## Council Verdict: {kısa konu}

### Konseyin Uzlaştığı Noktalar
[Birden fazla danışmanın bağımsız ulaştığı sonuçlar — yüksek güven sinyalleri]

### Konseyin Çatıştığı Noktalar  
[Gerçek anlaşmazlıklar — her iki tarafı da göster, neden ayrıştıklarını açıkla]

### Yakalanan Blind Spot'lar
[Sadece peer review'da ortaya çıkan şeyler — bireysel danışmanların gözden kaçırdığı]

### Öneri
[Net, eyleme geçirilebilir öneri. "Bağlıdır" değil. Chairman çoğunlukla ayrı düşünebilir.]

### İlk Adım
[Tek bir somut adım. Liste değil. Bir şey.]
```

Chairman'a not: Çoğunluk 4-1 "yap" dese bile 1'in gerekçesi güçlüyse 1'e taraf ol ve açıkla.

---

## Mosaik Projesine Özel Bağlam

Council öncesi şu dosyaları oku (30 sn max):
- `CLAUDE.md` — proje kimliği + kısıtlar
- `TODO.md` — aktif sprint + backlog
- Son journal: `docs/journal/YYYY-MM-DD.md`
- Varsa ilgili plan: `plans/NN-*.md`

Bu bağlam olmadan danışmanlar genelgeçer tavsiye üretir.

---

## Sonucu Kaydet

Transcript istenirse: `docs/journal/YYYY-MM-DD.md`'ye append et (ayrı dosya değil).
