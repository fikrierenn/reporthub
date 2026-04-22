# ADR-004 · Skill tasarım prensipleri (commit sınırları, kapsam disiplini)

- **Durum:** Kabul edildi (22 Nisan 2026)
- **Etkilenen:** `.claude/skills/*`, `.claude/agents/*`, commit-discipline kuralı
- **İlgili commit'ler:** `5df75ff` (session-handoff auto-commit), `b272926` (plan-tracker), commit-splitter subagent `e59e3a9` içinde

## Bağlam

ReportHub'da üç skill/agent sürüme girdi:
- `session-handoff` — oturum sonu journal yazar
- `plan-tracker` — TodoWrite ↔ TODO.md senkron
- `commit-splitter` — uncommitted çalışmayı bucket'lara böl, ardışık commit et

Her biri dosyaya yazma + potansiyel git etkileşimi içerdiği için **"skill nereye kadar otonom davranır, nerede kullanıcıdan onay ister"** sorusu ortaya çıktı. Üç somut tercih örnek oldu:

1. `session-handoff`: başlangıçta "commit etmez, kullanıcı karar versin" olarak yazıldı. Pratikte journal'ı her oturumda uncommitted bıraktığı için aşağıdaki sorunlar üretti:
   - 15-dosya eşiği uyarısı anlamsız gürültü oldu (aslında 3 dosya iş + 12 satır journal'dı)
   - Post-commit-journal hook'u her sonraki commit'te append ettiği için uncommitted diff git gittikçe büyüdü
   - Yeni oturumda `git status` sessizce gösterdi ama kimse commit etmedi

2. `plan-tracker`: TODO.md'yi güncellemek istiyor ama commit etmek için kod değişiklikleriyle birleşmek istiyor (plan guncel, feature commit'iyle ship). Auto-commit uygunsuz.

3. `commit-splitter`: tam tersi — commit yapmak zaten işin özü. Ama **her bucket** için kullanıcı onayı şart.

**Sorun:** "Skill commit eder mi etmez mi?" ikili karar değil. Ne kadar, nasıl, hangi kapsamda farklı.

## Karar

**Skill commit davranışı üç kademeli:**

| Kademe | Kapsam | Örnek |
|---|---|---|
| **Tam otomatik, tek dosya** | Skill kendi ürettiği tek dosyayı commit eder, başka path'e dokunmaz | `session-handoff` → sadece `docs/journal/YYYY-MM-DD.md` |
| **Otomatik değil, kullanıcı commit'i** | Skill dosyayı yazar, commit bir sonraki feature commit'ine dahil edilir | `plan-tracker` → `TODO.md` güncellenir, feature commit'iyle gider |
| **Her adımda onay** | Skill/agent her commit öncesi kullanıcıdan onay bekler | `commit-splitter` → her bucket için "onayla/reddet" |

### Seçim kriteri (hangi kademe?)

**1. kademe (tam otomatik) şartları:**
- Yazılan dosya skill'in **kendi artifactı** (başka kaynağı yok)
- Dosya **tek** ve **statik path** (`docs/journal/YYYY-MM-DD.md` gibi)
- Commit bozuk state bırakmaz (build kırmaz, test patlatmaz — sadece meta dosya)
- Kullanıcı niyetinin aksine iş yapmaz (handoff istendi → journal yazıldı → commit beklenen sonuç)

**2. kademe (kullanıcı commit'i) şartları:**
- Dosya **user kodunun parçası** (TODO.md, ADR, kural dosyası)
- İlgili **kod değişikliğiyle** birlikte commit'lenmeli (tutarlılık)
- Skill dosyayı yazar ama **commit yetkisi kullanıcıda**

**3. kademe (onay) şartları:**
- **Çoklu** path / **büyük** değişiklik
- **Geri alınması maliyetli** (commit sonrası rebase gerekir)
- Kullanıcının **bucket'lama kararı verdiği** iş (commit-splitter)

### Genel kurallar (tüm skill'ler için)

1. **`git add .` / `git add -A` yasak.** Skill hangi path'leri değiştiriyorsa sadece onları stage'ler.
2. **Skill mesajında açık scope** — `session-handoff`: "sadece journal", `plan-tracker`: "sadece TODO.md, commit'i sen yap".
3. **Hata durumunda sessiz geçme yok.** Skill commit yaparken hata alırsa kullanıcıya bildirir (pre-commit hook blok örneği gibi).
4. **Commit-discipline.md istisna olarak** belgelenir (1. kademe skill'ler için — ADR-004 referanslı).
5. **Skill'in "commit yapar mı" davranışı frontmatter'da veya başlıkta açık** (`session-handoff` description: "Yazim sonunda journal'i otomatik commit eder").

## Alternatifler

- **(A) Hiçbir skill commit yapmaz** (ilk tasarım). Temiz güvenli ama pratikte journal sürekli uncommitted → gürültü. **Reddedildi.**
- **(B) Tüm skill'ler serbestçe commit yapar.** Kontrolsüz, güvensiz, `plan-tracker` TODO.md'yi kod değişikliği ile birlikte commit'leyemeyecek. **Reddedildi.**
- **(C) Üç kademeli model** (seçilen). Her skill kendi durumuna göre doğru kademeye oturur, davranış açık belgelenir.

## Sonuçlar

**Olumlu:**
- Skill'lerin davranış sınırları net — kullanıcı ne zaman onay beklemesi gerektiğini biliyor
- Commit-discipline ihlali değil, istisna ve gerekçeli
- Yeni skill yazarken kademe seçimi standart soru setiyle yapılır

**Olumsuz / dikkat:**
- Yeni bir skill yazan geliştirici bu ADR'i okumalı; yoksa yanlış kademe seçebilir (örn. büyük refactor skill'i tam otomatik commit yaparsa felaket)
- Kademe geçişi mümkün ama commit disiplini gerektirir (`session-handoff` başlangıçta kademe 2, şimdi kademe 1 — değişiklik bir ADR notu ile yapıldı)
- `commit-splitter` subagent, skill'den farklı olarak Agent tool ile çağrıldığı için direktifi alıp çalışır — onay mekanizması konuşma üstünden işler. Skill (slash komut) ile subagent (Agent tool) arasındaki fark da ayrı bir notta toplanmalı.

## Referanslar

- [commit-discipline.md](../../.claude/rules/commit-discipline.md) — istisna bloğu (rule #1 altında)
- [session-handoff/SKILL.md](../../.claude/skills/session-handoff/SKILL.md) — kademe 1 uygulaması
- [plan-tracker/SKILL.md](../../.claude/skills/plan-tracker/SKILL.md) — kademe 2 uygulaması
- [commit-splitter.md](../../.claude/agents/commit-splitter.md) — kademe 3 uygulaması
