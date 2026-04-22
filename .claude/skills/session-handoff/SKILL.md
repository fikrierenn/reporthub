---
name: session-handoff
description: Oturum sonu ozet yazar. Bugun yapilanlari, build durumunu, yarim kalan islari, yarina baslangic noktasini docs/journal/YYYY-MM-DD.md dosyasina yazar. Yazim sonunda journal'i otomatik commit eder (sadece journal dosyasi — baska dosyaya dokunmaz). Kullanici "handoff", "oturum sonu", "iyi geceler", "kaydet ve kapat", "gunaydin ozet" gibi ifadeler kullandiginda veya /handoff calistirildiginda devreye gir.
allowed-tools: Read, Edit, Write, Bash, Grep, Glob
user-invocable: true
model: inherit
---

# Oturum Devir Skill'i

## Amac
Her oturum sonunda (veya baslangicinda ozet almak icin), gun icinde olanlari kalici bir journal dosyasina yazar **ve journal'i otomatik commit eder**. Boylece:
- CLAUDE.md'ye session log yazilmaz (temizlik korunur)
- Yarinki Claude ne olduguna bakar (SessionStart hook zaten okuyor)
- Gecmis kararlar grep'lenebilir
- Journal surekli uncommitted durumda asili kalmaz (pre-commit hook tekrarlayan gurultu yapmaz)

## Kaynak Dosya
`docs/journal/YYYY-MM-DD.md` — tarih format'i `%Y-%m-%d`. Eger dosya yoksa olustur, varsa append.

## Cikti Sablonu

```markdown
# Oturum Gunlugu — YYYY-MM-DD

## Ana Konu
<1-2 cumle: bu oturumda asil hedef neydi>

## Tamamlananlar
- Madde 1 (dosya:line referansi varsa ekle)
- Madde 2
- ...

## Build / Test Durumu
- Build: yesil / kirmizi / calistirilmadi
- Test: X yesil, Y kirmizi / calistirilmadi
- Smoke test: yapildi / yapilmadi / kirildi

## Commit Durumu
- Uncommitted dosya sayisi: N
- Yeni commit'ler: <varsa liste>
- Commit beklemede: <varsa>

## Yarim Kalan / Yarin'a Birakilan Isler
- Madde 1 — neden yarim, nereden devam
- Madde 2
- ...

## Kararlar
- <Bu oturumda alinan mimari/UX/teknik kararlar>
- <ADR'ye yazilmis mi? Yoksa henuz yazilacak mi?>

## Dikkat Edilmesi Gerekenler
- <Memory hatasi, yanlis varsayim, duzeltme gerektirecek noktalar>

## Yarina Baslangic Noktasi
1. <En kritik 1. adim>
2. <2. adim>
3. <3. adim>
```

## Adim Adim

### Adim 1 — Bilgi Topla
Asagidaki komutlari cagir, sonuclarini kullan:

```bash
date +%Y-%m-%d
git status --porcelain | wc -l
git log --since=midnight --oneline
```

### Adim 2 — Mevcut Journal'i Kontrol Et
```bash
JOURNAL="docs/journal/$(date +%Y-%m-%d).md"
# Dosya varsa: append (eski icerigi koru, yeni bolum ekle)
# Dosya yoksa: sablondan olustur
```

### Adim 3 — Konusma Baglamini Oku
Bu oturumdaki:
- Kullanici mesajlarinin ozeti
- Senin yaptigin degisiklikler (`git diff --name-only`)
- Todo list durumu (hangi task'lar complete edildi)

Bu bilgilerden "Tamamlananlar", "Yarim Kalan", "Kararlar" bolumlerini cikar.

### Adim 4 — Dosyaya Yaz
- Dosya yoksa: sablondan yeni dosya.
- Dosya varsa: en altta `---` separator + yeni bolum `## Oturum 2` gibi ekle.

### Adim 5 — Journal'i OTOMATIK commit et

**Yeni (onceki kural "commit etmez" degistirildi):** journal dosyasi
handoff sonunda tek dosya scope ile commit'lenir. Disiplin:

- **Sadece** `docs/journal/YYYY-MM-DD.md` stage'lenir. `git add .` /
  `-A` **yasak**.
- Baska bir degisiklik varsa (kullanici uzerinde calisiyorsa)
  dokunulmaz — o dosyalar uncommitted kalir.
- Eger journal dosyasinda hic degisiklik yoksa (idempotent ikinci
  call), commit atlanir.

```bash
JOURNAL="docs/journal/$(date +%Y-%m-%d).md"
if ! git diff --quiet -- "$JOURNAL" || git ls-files --others --exclude-standard -- "$JOURNAL" | grep -q .; then
  git add "$JOURNAL"
  git commit -m "docs(journal): $(date +%Y-%m-%d) handoff"
fi
```

Commit mesaji format:
```
docs(journal): YYYY-MM-DD handoff

<opsiyonel: 1-2 cumle oturum ozeti>

Co-Authored-By: <agent> <...>
```

### Adim 6 — Ozet Goster
Kullaniciya 5-10 satirlik kisa ozet:
```
Oturum kaydedildi: docs/journal/2026-04-22.md
- Tamamlanan: 4 madde
- Yarim kalan: 2 madde
- Commit: abc1234 docs(journal): 2026-04-22 handoff
- Uncommitted: N dosya (15 esigin altinda, iyi)
- Yarina baslangic: <ilk adim>
```

## Ornek Tetikleme Durumlari

- Kullanici "iyi geceler" dedi → bu skill'i otomatik cagir, journal yaz, commit et, ozet ver.
- Kullanici "/handoff" yazdi → aciklikla cagirdi.
- Kullanici "devam edecegiz" dedi → mevcut durumu kaydet.
- Kullanici "gunaydin" dedi → bu sefer ters yon: en son journal'i oku ve "nerede kaldik?" ozet ver (commit yapma — sadece okuma).

## Dikkat

1. **Hic journal yoksa:** docs/journal/ olustur, ilk dosya bugunun tarihi.
2. **Auto-commit SADECE journal dosyasi icin:** `git add docs/journal/YYYY-MM-DD.md` — baska path yasak. `commit-discipline.md` kuralinin bir istisnasidir; gerekcesi journal handoff artifactinin sistemik yer almasi.
3. **Baska dosya uncommitted ise dokunma:** kullanici icinde olan is icin ayri commit bekliyor olabilir.
4. **CLAUDE.md'ye ekleme:** Session log CLAUDE.md'ye **yazilmamali** (200 satir esigi + 3 katman ayrimi kurali).
5. **Ust uste yazim:** Ayni gun ikinci kez cagrilirsa `## Oturum 2` eklenir, eski icerik silinmez. Ikinci commit'te mesaji `docs(journal): YYYY-MM-DD handoff (oturum N)` yazabilirsin.
6. **Turkce yaz:** UI metni ve journal icerigi Turkce, UTF-8 karakterler kullan ("Düzenle", "Bileşen").
7. **Pre-commit hook:** Journal `.md` dosyasi — pre-commit antipattern hook Markdown taramiyor, bu commit asla bloklanmaz.

## Iliskili Dosyalar
- SessionStart hook (`.claude/hooks/session-start.sh`): En son journal'i oturum basinda Claude'a enjekte eder.
- Post-commit journal hook (`.claude/hooks/post-commit-journal.sh`): Baska commit'lerde otomatik journal append ediyor; handoff skill bu append'leri de commit'leyecek.
- Commit disiplini: `.claude/rules/commit-discipline.md` — journal auto-commit istisnasini belirtir.
- Baglam yonetimi anayasasi: `docs/CONTEXT_MANAGEMENT.md`.
