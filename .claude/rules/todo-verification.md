# TODO Doğrulama Disiplini

_Kapsam: `TODO.md` + `docs/journal/*` + memory'deki HIGH/CRITICAL bulgu listeleri. Action almadan önce **canlı kod** ile karşılaştırılır._

## Neden bu dosya var

**2026-05-14:** Kullanıcı 12 Mayıs analizini paylaştı — 11 HIGH bulgu listesi açık görünüyordu. Body'sine inanıp fix'lere başlamak üzereydim. Sweep çalıştırınca **7 madde zaten kapanmış** çıktı (a68378c "post-review hardening" commit'i). Sadece 2 HIGH gerçekten açıktı.

Kullanıcı uyardı: "O kadar tespit için scriptler yazdın, skiller oluşturdun ama tespit edemedin."

Sistem boşluğu: `security-reviewer` + `mosaik-security` + `/security-check` **yeni yazılan kodu** denetliyor. **Hiçbiri eski TODO/journal/memory iddialarını canlı kodla karşılaştırmıyor.** Stale-claim filtre yok. Kullanıcı söylemese sweep yapmayacaktım.

Bu dosya o önlemdir.

## Mutlak Kurallar

1. **TODO listesi bilgi değildir, hipotez tahtasıdır.** `TODO.md` veya journal'da "HIGH-X açık" yazılı olması, X'in **şu an gerçekten açık** olduğu anlamına gelmez. Yazıldığı tarihte açıktı. Bugün hâlâ açık olduğunu kanıtlamak senin işin.

2. **Action almadan önce file:line ile doğrula.** Madde `AdminController.Reports.cs:128 id validation yok` diyorsa:
   - Önce `Read` ile o satırı oku.
   - `AnyAsync` / `if (id ...)` / `Validate` gibi anahtar kelimeler var mı bak.
   - Yoksa açık, varsa kapalı — kullanıcıya bildir, fix etme.

3. **"Post-review hardening" commit'leri kırmızı bayraktır.** `git log --grep='hardening\|fix(security)\|post-review'` çıkıyorsa TODO Code review backlog'u **muhtemelen stale**. Action öncesi tam sweep zorunlu.

4. **Fix'lemeden önce sweep, fix sırasında değil.** 11 madde fix'liyormuş gibi başlayıp 5. maddede "bu zaten kapalı" demek **maliyetli**. Önce tüm 11'i paralel `Read`/`Grep` ile doğrula, **sonra** açık olanları sırayla fix et.

## Workflow

### Adım 1 — TODO maddesini oku
`TODO.md` veya journal'da bir bulgu listesi varsa, her madde için:
- file:line referansı var mı?
- "Fix: ..." snippet'i var mı?

Yoksa → maddeyi reddet, kullanıcıya net file:line iste.

### Adım 2 — Paralel doğrulama (tek mesajda)
HIGH/CRITICAL maddelerin tümü için **paralel** `Read` + `Grep` çağrısı. Tek tek sıralı yapma.

```
Read(AdminController.Reports.cs:60-90)
Read(SmtpEmailService.cs)
Grep(_ = ex, Mosaik/)
Read(EmailTemplates.cs)
Grep(IEmailService, Mosaik/)
...
```

### Adım 3 — "Gerçek açık" listesi çıkar
| # | İddia | Kod kanıtı | Durum |
|---|---|---|---|
| HIGH-1 | Route ambiguity | `CreateReport()` `private` | ✅ KAPALI |
| HIGH-a | `_ = ex;` log yok | `_ = ex;` var, `LogError` yok | ❌ AÇIK |

Kullanıcıya bu tabloyu göster. **Onay almadan fix etme.**

### Adım 4 — TODO.md güncelle
Kapanmış maddeleri `[ ]` → `[x] ✅ KAPALI <tarih> — <kod kanıtı>` yap. Kaynak commit hash'i ekle. Bu kalıcı kayıt — sonraki oturum da aynı stale'i okumasın.

### Adım 5 — Sadece gerçek açık olanları fix et
2 maddeyi fix etmek 11'i fix etmekten 5x hızlı. Yanlış maddeye dokunma — git diff'te gereksiz değişiklik = code review yükü.

## Tetikleyiciler

Bu kural şu durumlarda **otomatik** uygulanır:

- Kullanıcı "TODO'daki HIGH'ları fix et" / "güvenlik bulgularını kapat" / "borç temizle" diyor
- Kullanıcı dışarıdan bir analiz/review paylaşıyor (LLM Council, code review, audit raporu)
- `docs/journal/*` veya `TODO.md`'de 3+ "HIGH" / "CRITICAL" / "FIX BEKLİYOR" işareti var
- Son 7 günde `fix(security)` veya `hardening` commit'i geçtiyse

## Anti-pattern (yaparsan kullanıcı haklı olarak kızar)

1. **"TODO'da açık yazıyor, demek ki açık" varsayımı** — En sık hata. Yazılı her şey kanıt değil.
2. **Sweep yapmadan fix'e başlamak** — 7 maddeyi gereksiz "fix"leyip git diff şişirmek.
3. **Stale TODO'yu güncellemeden kapatmak** — Sonraki oturum aynı hipotezi okur, döngü.
4. **Kullanıcı analizi paylaştığında doğrudan uygulamak** — Önce doğrula. Dış analiz de kaynak değil, hipotez.

## İlişkili

- `.claude/rules/before-major-change.md` — silme/rename öncesi grep + ARCHITECTURE_MAP (kardeş kural — refactor için bu, TODO için yukarıdaki)
- `.claude/rules/session-protocol.md` Adım 5 — compliance scan (TODO doğrulama bunun pre-step'i olabilir)
- `.claude/agents/security-reviewer.md` — kod denetler, TODO denetlemez (kapsam farkı)
- `memory/feedback_todo_stale_claim.md` — bu hatanın spesifik kayıt
