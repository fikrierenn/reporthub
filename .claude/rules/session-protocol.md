# Oturum Protokolü

_Kapsam: Her Claude oturumunun başı / ortası / sonu ritüelleri._

## Neden bu dosya var

22 Nisan 2026 sabahı bir oturumda SessionStart hook Cowork modunda fire etmedi → Claude "nerede kaldık?" sorusuna hafızadan cevap verdi, journal + TODO'yu elle okumayı atladı → kullanıcı fark etti ve önlem istedi. Aynı gün öğleden sonra ikinci benzer hata: hook çıktısı context'te görünüyor diye `bash` çalıştırmayı atladı → kullanıcı koşulsuz kural istedi. Bu dosya o önlemdir. Tekrarı kabul edilmez.

## Oturum Başı (İlk yanıttan önce ZORUNLU)

### Adım 1 — Hook'u KOŞULSUZ çalıştır

```bash
bash .claude/hooks/session-start.sh
```

**Her oturumda, istisnasız.** Context'te hook çıktısı görünse bile tekrar çalıştır — fresh çıktı farklı olabilir, context stale olabilir. "Hook fire etti, atla" varsayımı **yasak**. 22 Nisan 2026'da bu varsayım iki kez hata üretti; kural koşulsuz hale getirildi.

Çıktı: son 3 gün commit'leri, uncommitted sayısı, 15-eşik uyarısı, aktif TODO başlıkları, son journal'in son 40 satırı.

### Adım 2 — Son 2 journal dosyasını oku

```bash
ls -t docs/journal/*.md | head -2
```

Her ikisini de `Read` et. Özellikle bak:
- **Tamamlananlar** — dün neyi bitirdik
- **Yarım kalan işler** — nereden devam edilecek
- **Düzeltme notları** — hangi memory hatası yapıldı (aynı hatayı tekrarlama)

### Adım 3 — TODO.md aktif öncelikleri oku

`TODO.md` → **"BIRLESIK ONCELIK SIRASI"** bölümü. En az Faz 0 (bugün) + Faz 1'in ilk 3 maddesi. Aktif bug başlıkları (SP Önizle vb).

### Adım 4 — Uncommitted durumu bil

`git status --porcelain | wc -l` — 15 üstüyse yeni iş yasak, önce commit-split.

### Adım 5 — Pre-session Compliance Scan (ZORUNLU — uncommitted varsa)

**Karar 2026-05-10 (kullanıcı):** "Session sonundaki 3 aşamalı kod taramasını session başına alalım, kapanırken yapmıyorsun." Tarama oturum **başında** yapılır, sona bırakılmaz — çünkü uygulamada handoff sırasında bypass ediliyordu, yarın (yani bugün) yapılan iş borç olarak handoff'a giren oturumdan kalan dosyalarla başlıyordu.

**2026-05-10 ek karar:** Inline style taraması da bu adıma dahil ("başlangıca inline stil taramasını da eklemelisin"). 4. paralel agent inline style envanter çıkarır.

Eğer uncommitted dosya varsa (handoff'tan devreden iş ya da yarım kalan kod), oturum başında **tek mesajda 4 paralel agent** çalıştır:

```
Agent(subagent_type="code-reviewer")           — CLAUDE.md + mimari uyumluluk
Agent(subagent_type="silent-failure-hunter")    — exception handling + silent catch + fallback
Agent(subagent_type="general-purpose")          — security-review checklist (.claude/rules/security-principles.md)
Agent(subagent_type="general-purpose")          — inline style envanter (.claude/rules/ui-patterns.md §0)
```

Scope: `git diff --name-only HEAD` + untracked (`git ls-files --others --exclude-standard`). Inline style scan için scope: tüm `.cshtml` (yeni ve değiştirilmiş).

- **Bulgu yok** → kullanıcının istediği işe geç (örn. commit-split).
- **Bulgu var** → önce fix et, sonra commit-split / yeni iş.
- **Inline style HIGH** (Plan 25 / yeni view) → utility class'a refactor zorunlu, commit-split öncesi.
- **Inline style MEDIUM/LOW** (admin module / legacy) → envanter Plan 25.1 / sonraki sprint borç listesine eklenir.

**Önemli:** Bulgular fix edilmeden commit-split yapma. Aksi takdirde HIGH/MEDIUM bulgular commit'lere gömülür ve sonraki oturumda kaybolur.

**Refactor agent'lara dağıtım:** Inline style HIGH bulgular birden fazla view'a yayılırsa **tek başına yapma** — kullanıcı 2026-05-10'da net söyledi: "agentlara dağıttın mı". Her bağımsız view için paralel agent (general-purpose veya frontend-design skill ile), shared CSS önce eklenir. 8+ view = 4-8 paralel agent.

**Override:** Kullanıcı açıkça "tarama yapma" / "atla" derse atlanır. Aksi default = tarama zorunlu.

**Uncommitted yoksa bile:** Bu oturumda kod yazıldıysa (git log ile son commit bu oturuma aitse) Adım 5 çalıştırılır — scope olarak son commit'teki dosyalar kullanılır. Gerçekten hiç kod yazılmamışsa atlanır.

### Kullanıcıya cevap

Yukarıdaki 5 adım **sessizce** yapılır — kullanıcıya "şunu okudum şunu okudum" demeye gerek yok. Cevap sadece bu okumalara dayanır, hafıza tahminine değil. Adım 5 (compliance scan) sonuçları varsa kullanıcıya özet ver: "X HIGH, Y MEDIUM bulgu — düzelttikten sonra başlıyorum."

---

## Oturum Ortası

### 15 dosya eşiği
`git status` ile uncommitted > 15 → **yeni iş yasak**, önce `commit-discipline.md` → "32-Dosyalık Backlog — Planlı Split" planına göre böl.

### 3 paralel feature eşiği
Aynı anda 3'ten fazla feature branch açıksa birini bitirmeden yenisine geçme. Context kayıyor.

### Kural değişikliği → dosyaya yaz
Kullanıcı yeni bir kural / tercih söylüyorsa konuşmada kalmaz, hemen ilgili `.claude/rules/*.md` dosyasına eklenir. "Aklında tut" demez — Claude konuşma hafızasından kural çekemez.

### Mimari karar → ADR
Mimari bir karar alındıysa `docs/ADR/NNN-konu.md` yaz (veya en azından TODO'ya "ADR-X yaz" kaydı düş).

### 3+ adımlı plan → TodoWrite + plan-tracker (OTOMATIK, HATIRLATMASIZ)

Kullanıcı 3+ adımlı bir iş tanımladığı ya da Claude kendisi çok fazlı iş planlıyorsa (faz A, B, C / madde 1-2-3 / önce X sonra Y sonra Z), **kullanıcı demeden, reminder beklemeden**:

1. **TodoWrite** çağır — her faz/adım ayrı item, ilki `in_progress`.
2. **`plan-tracker` skill** çağır (Skill tool) — TODO.md'deki aktif faz bölümüne maddeleri yazsın. İş bittikçe ✅ + commit hash.

Kullanıcının "TodoWrite kullanmadın" / "planı dosyaya yazmadın" demesi bu kuralın ihlalidir. 22 Nisan 2026 akşam: ADR-007 6-fazlı plan konuşmada kaldı, kullanıcı hatırlatmak zorunda kaldı. Tekrarı yasak.

**Skill çağırma dili netleştirme** (Claude'ın sık kaçırdığı):
- TodoWrite **yetmez** — sadece in-session bellek. Compact / `/clear` / oturum sonu = uçar.
- plan-tracker **dosyaya yazar** — kalıcı. Plan + ilerleme + commit hash.
- İkisi **paralel** kullanılır, biri diğerinin yerine değil.

### Skill/Agent/MCP proaktif kullanım — ANA ÇALIŞMA PRENSİBİ

**Kullanıcı kararı (2026-05-07):** "İşleri mutlaka subagent ve skill kullanarak yapmalısın senin ana çalışma prensibin olmalı." Subagent + skill kullanımı **default**, manuel iş **istisnai**. Kullanıcı "agent çağır" / "skill kullan" demek zorunda kalmasın — proaktif tetikle.

| Tetik | Tool | Ne zaman |
|---|---|---|
| 3+ dosya/klasör keşif | `Explore` agent | Pattern nerede / N config tara |
| Derin analiz / "yüzeysel geçme" | `code-explorer` agent | file:line referans, 1500-2000 kelime brief |
| Mimari karar + alternatif | `code-architect` veya `Plan` agent | Refactor tradeoff |
| **Birden fazla bağımsız klasör** | **Paralel subagent batch** | **Tek mesajda N tool call** |
| 3+ adımlı iş | TodoWrite + `plan-tracker` | İkisi farklı amaç, biri yetmez |
| Oturum sonu | `session-handoff` | "iyi geceler" / "handoff" |
| Multi-LLM danışma | `llm-council` skill | Mimari "hangi yol" belirsizliği |
| UI/UX değişiklik (HER .cshtml edit) | `accessibility-compliance` + `ui-ux-pro-max` | **Otomatik:** view düzenlerken WCAG contrast, ARIA, touch target, focus-visible kontrol |
| Yeni sayfa / layout değişiklik | `frontend-design` + `visual-design-foundations` + `responsive-design` | Yeni view, layout migration, hero/card/table pattern |
| Design system değişiklik | `design-system-patterns` + `interaction-design` | Token ekleme, animasyon, theming |
| BKM kurumsal DB sorgu | `mcp__sqlserver__*` | Allowlist: master, DerinSIS*, BKMDATA, EncoreMerkez, BKM. **Mosaik DB allowlist DIŞINDA** — uygulama içi DB için MCP yerine SSMS/sqlcmd |
| Dashboard/UI regresyon | `mcp__Claude_Preview__*` | Render smoke test, screenshot |
| Yeni feature implement | `/feature-dev` slash | 7 fazlı guided |
| PR review | `/review-pr` slash | Multi-agent comprehensive |
| AI Brain push | `notebooklm` + `wiki-keeper` | Cross-project bilgi |
| BI/dashboard işi | `anthropic-skills:bi-dashboard` | Power BI/Metabase |
| SQL Server uzmanlık | `anthropic-skills:sql-server-uzmani` | T-SQL, SP, view, performans |

**Anti-pattern (yapma):**
- 8 klasörü Read tool ile tek tek okumak → **subagent batch**
- 3 bağımsız dosyayı sıralı edit → **paralel batch tool call** (tek mesajda)
- Plan yazarken alternatifleri kafadan üretmek → `llm-council` veya `code-architect`
- Kullanıcı "skill kullan" hatırlatmasını beklemek → proaktif
- Manuel keşfederken context kıyısına yer almak → Explore agent + max kelime cap

**Subagent prompt disiplini:**
- Hedef + scope (file/klasör listesi) + format + max kelime cap
- Kalite gardiyanı: "yüzeysel geçme" / "boş ise net söyle"
- Çıktı şekli: "X paragraf, Y bölüm, max Z kelime"
- Boş klasör veya kod yoksa subagent **sustur**, dolduruşa girme

**Override:** Kullanıcı açıkça "manuel yap, agent gönderme" derse subagent durdurulur. Aksi default = subagent + skill.

Detay memory: `feedback_subagent_skill_ana_prensip.md`.

---

## Oturum Sonu

### Tetikler

Kullanıcı "iyi geceler" / "handoff" / "kaydet ve kapat" / "/handoff" / "devam edeceğiz" dediğinde `.claude/skills/session-handoff/SKILL.md` devreye girer.

### Compliance Scan — oturum başında VE sonunda ZORUNLU

**Karar 2026-05-11 (kullanıcı):** "her kapanışta ya da açılışta yapmalısın." Artık iki yönde zorunlu:

**Oturum başında (Adım 5):** Uncommitted veya bu oturumda yazılan kod varsa 4 paralel agent.

**Oturum sonunda (handoff öncesi):** Bu oturumda commit edilen/değiştirilen dosyalar üzerinde aynı 4 paralel agent:
```
Agent(subagent_type="code-reviewer")
Agent(subagent_type="silent-failure-hunter")
Agent(subagent_type="general-purpose")   — security-review
Agent(subagent_type="general-purpose")   — inline style envanter
```
Scope: `git diff HEAD~N..HEAD --name-only` (bu oturumda yapılan commit'ler).

- Bulgu yok → handoff yaz.
- CRITICAL/HIGH bulgu → düzelt + commit → sonra handoff.
- MEDIUM/LOW → handoff'ta "bilinen borç" olarak kaydet.

**Override:** Kullanıcı "tarama yapma" / "atla" derse atlanır.

### Ne yapar

`docs/journal/YYYY-MM-DD.md` dosyasına yazar (yoksa oluştur, varsa append):
- **Ana konu** — bu oturumda asıl hedef
- **Tamamlananlar** — dosya:line referanslı
- **Build / test durumu** — yeşil / kırmızı / çalıştırılmadı
- **Commit durumu** — uncommitted sayısı, yeni commit'ler
- **Yarım kalan işler** — nereden devam
- **Kararlar** — ADR'ye gidecek mi
- **Dikkat edilmesi gerekenler** — memory hatası, yanlış varsayım
- **Yarına başlangıç noktası** — 1-3 somut adım

### CLAUDE.md'ye session log yazma
Session log **CLAUDE.md'ye yazılmaz** (200 satır eşiği + 3 katman ayrımı kuralı). Sadece journal'a.

### Commit kararı
Skill commit **etmez**. Kullanıcı açıkça isteyene kadar commit yok (`commit-discipline.md`).

---

## Ritüel atlandığında

1. **Kabul et.** Hata savunmaya gitme — "hook fire etmedi" / "context'te vardı" mazeret değil, elle okuma sorumluluğu vardır.
2. **Anında kapat.** Hook'u manuel çalıştır, journal'i oku, TODO'yu gözden geçir.
3. **Önlemini dosyaya yaz.** Aynı türde hata tekrar olmasın diye kural güçlendir (bu dosya örneği).
4. **Journal'a süreç notu düş.** Düzeltme notları bölümüne: "Süreç hatası: X atladı. Önlem: Y eklendi."

---

## Cowork vs Claude Code farkları

| Özellik | Claude Code | Cowork |
|---|---|---|
| CLAUDE.md enjeksiyonu | ✅ | ✅ |
| `.claude/rules/*.md` enjeksiyonu | ✅ (CLAUDE.md'de referans edilenler) | ✅ (aynı — doğrulandı 22 Nisan) |
| SessionStart hook | ✅ otomatik tetikler | ⚠️ bazen fire etmiyor — elle çalıştır |
| `.claude/` yazma izni | ✅ | ✅ (22 Nisan 2026 testinde doğrulandı, önceki varsayım yanlıştı) |
| `.claude/skills/` kullanımı | ✅ | ✅ |
| `docs/` yazma izni | ✅ | ✅ |
| Subagent (Task tool) | ✅ | ✅ (Agent tool) |
| MCP araçları | ✅ | ✅ |

**Sonuç:** Cowork'te her ritüel elle yapılmalı. Hook otomasyonuna güvenme — ama `.claude/` yazma kısıtlaması da varsayım çıktı, doğrulamadan kural yazma.

---

## İlişkili Dosyalar

- `docs/CONTEXT_MANAGEMENT.md` — bağlam yönetimi anayasası (ilkeler bütünü).
- `.claude/hooks/session-start.sh` — oturum başı bilgi toplayan script.
- `.claude/skills/session-handoff/SKILL.md` — oturum sonu journal yazar.
- `.claude/rules/commit-discipline.md` — 15 dosya eşiği, branch-per-ask.
- `docs/journal/` — tarihli oturum kayıtları.
