---
description: "Mosaik güvenlik denetimi — security-reviewer + silent-failure-hunter + Anthropic security-review paralel"
argument-hint: "[range] (örn. HEAD~5..HEAD veya boş = uncommitted)"
allowed-tools: ["Bash", "Glob", "Grep", "Read", "Task", "TodoWrite"]
---

# /security-check — Mosaik Güvenlik Denetimi

3 paralel agent ile son değişiklikleri `security-principles.md` checklist'ine göre tarar. Range parametresi opsiyonel — verilmezse `git diff HEAD~1..HEAD` + uncommitted scope alır.

**Range:** "$ARGUMENTS"

## Workflow

### 1. Range tespiti

```bash
# Argüman varsa kullan
RANGE="$ARGUMENTS"

# Yoksa default: son commit + uncommitted
if [ -z "$RANGE" ]; then
    RANGE="HEAD~1..HEAD"
fi

git log --oneline "$RANGE" 2>/dev/null
git diff "$RANGE" --name-only
git status --short
```

Çıktı:
- Commit listesi (kaç commit, hangi konular)
- Değişen dosya listesi (kategorize: controller / service / view / JS / SQL / config)
- Uncommitted dosyalar

Eğer scope **0 dosya** ise: "Taranacak değişiklik yok." dön + çık.

Eğer scope **>150 dosya** ise: "Scope çok büyük (N dosya). Daraltma öner:" + son 3 commit yaklaşımı sun, kullanıcıya sor.

### 2. Üç paralel agent — TEK MESAJDA

Tek mesajda 3 Agent tool call paralel başlat:

#### Agent 1 — `security-reviewer`

```
subagent_type: security-reviewer
description: Security audit
prompt:
  Mosaik son değişiklikleri range '<RANGE>' kapsamında güvenlik audit'i yap.
  Scope: `git diff <RANGE>` çıktısı.

  Odak (security-principles.md 10 kural):
  1. SQL injection (SP + SqlParameter, string concat yasak)
  2. XSS (HtmlEncode, DOM API, @Html.Raw user input)
  3. CSRF ([ValidateAntiForgeryToken] + @Html.AntiForgeryToken())
  4. Open redirect (Url.IsLocalUrl + StartsWith)
  5. Secret yönetimi (appsettings.json plain-text)
  6. Cookie sertleştirme
  7. Exception (ex.Message user'a YASAK)
  8. Multi-tenant filter bypass
  9. Password hashing
  10. Iframe sandbox

  Output: file:line + saldırı senaryosu + kanıt + fix snippet.
  Format: agent kendi rapor formatını kullanır.
  Confidence ≥ 75 olan CRITICAL/HIGH/MEDIUM döndür.
  0 bulgu varsa "0 CRITICAL / 0 HIGH / 0 MEDIUM" yaz.
```

#### Agent 2 — `silent-failure-hunter`

```
subagent_type: silent-failure-hunter
description: Silent failure audit
prompt:
  Mosaik son değişiklikleri range '<RANGE>' kapsamında silent failure
  ve uygunsuz error handling ara.

  Odak:
  - Boş catch block
  - catch swallow + log only (caller'a fail bilgisi yok)
  - .catch(() => {}) JS sessiz fail
  - Task döner ama exception yutar (caller başarı sanır)
  - Fallback / retry kullanıcıya bilgi vermeden çalışıyor
  - SqlException catch ama specific exception yok
  - ex.Message user'a yansıyor

  Kural referansı: .claude/rules/security-principles.md §7

  Output: file:line + Hidden Errors listesi + User Impact + fix snippet.
  Format: agent kendi rapor formatını kullanır.
```

#### Agent 3 — Anthropic `security-review` skill (general-purpose ile)

```
subagent_type: general-purpose
description: OWASP checklist sweep
prompt:
  Mosaik son değişiklikleri range '<RANGE>' için OWASP Top 10 ve
  defansif security pratik checklist'i ile tara.

  Anthropic security-review skill mantığını uygula (current branch
  pending changes), ancak Mosaik-spesifik scope kullan:

  Range: git diff <RANGE>
  Read these first:
  - .claude/rules/security-principles.md (10 mutlak kural)
  - .claude/skills/mosaik-security/SKILL.md (proaktif kurallar)

  Mosaik özelinde dikkat:
  - V1 legacy redirect intentional (ARCHITECTURE_MAP §3 — yanlış flag yok)
  - DashboardController CSV legacy known debt (architecture.md)
  - TestController #if DEBUG sarılı
  - Inline style ayrı tarama scope'ta (bu agent değil)

  Output:
  - CRITICAL (exploitable, kanıt + saldırgan profili + sömürü adımı)
  - HIGH (defense-in-depth)
  - MEDIUM (best practice)
  - POSITIVE (3-5 örnek)
  - Genel postür 1-3 cümle
  Max 1200 kelime. Spekülasyon yok, kanıt yok ise rapor yok.
```

### 3. Bulguları birleştir

3 agent dönünce kullanıcıya **tek özet** yaz:

```markdown
# Security Check Özeti — <range>

**Scope:** N dosya, K commit
**Tarih:** YYYY-MM-DD

## Kesişen bulgular (2/3 veya 3/3 agent doğruladı)

### HIGH-1 · <başlık> (3/3)
- **File:line:** ...
- **Kanıt:** ...
- **Fix:** ...

## Tek agent bulguları (ikinci görüş yok ama net)

### HIGH-N · <başlık> (security-reviewer)
...

## MEDIUM

- file:line — kısa not

## POSITIVE

- file:line — doğru pratik (3-5 örnek)

## Karar noktası

- **A)** En kritik N HIGH'i hemen fix et — ~Xh
- **B)** Sadece <kritik bir/iki> acil — diğerleri sonra
- **C)** Tüm bulguları TODO.md'ye kaydet, başka iş yap

Hangisi?
```

### 4. TodoWrite ile takip

Kullanıcı bir bulguyu fix etmeye karar verirse `TodoWrite` ile takip et — her bulgu ayrı item.

## Kullanım örnekleri

```
/security-check
# default: HEAD~1..HEAD + uncommitted

/security-check HEAD~5..HEAD
# son 5 commit

/security-check e2da9b6^..0841080
# explicit range

/security-check origin/main..HEAD
# branch'in main'den farkı (PR review öncesi)
```

## İlişkili

- `.claude/agents/security-reviewer.md` — denetleyici agent (proje-spesifik)
- `.claude/skills/mosaik-security/SKILL.md` — proaktif uygulayıcı skill
- `.claude/agents/silent-failure-hunter.md` — error handling scope
- `.claude/rules/security-principles.md` — 10 mutlak kural (anayasa)
- `.claude/commands/review-pr.md` — geniş PR review (bu daha dar, sadece security)
- Anthropic hazır `security-review` skill — current branch pending changes (built-in)

## Notlar

- **Paralel zorunlu.** 3 agent tek mesajda Task tool ile başlat.
- **Range zorunlu doğrula.** Boş scope → çık. Çok büyük → kullanıcıya sor.
- **Compliance scan ile karıştırma.** `session-protocol.md` Adım 5 (oturum başı tarama) daha geniştir — security + silent-failure + inline-style 4 agent. Bu komut **sadece security odaklı**, 3 agent.
- **Sonuç birleştirilmeden çıkma.** 3 agent ayrı ayrı rapor verir; sen sentezler + kullanıcıya tek özet + karar noktası sunarsın.
