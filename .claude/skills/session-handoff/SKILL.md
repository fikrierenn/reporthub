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

### Adim 1.5 — ARCHITECTURE_MAP.md refresh (ZORUNLU)

Karar 2026-05-12 (kullanici): "Bu dosyayi da surekli besle".

```bash
bash scripts/refresh-arch-map.sh
```

Script § 7 (AppModules live state — sqlcli), § 12 (Admin views envanteri
+ inline-style sayimi), § 13 (Controller routes), Last refresh tarihi
guncellenir. El yazimi bolumler (§ 2-6, 8-11) etkilenmez.

Diff varsa kullanici-degisikligine eklenmis sayilir; **journal commit'inde
ayri commit at**:

```bash
if ! git diff --quiet -- docs/ARCHITECTURE_MAP.md; then
    git add docs/ARCHITECTURE_MAP.md
    git commit -m "docs(map): ARCHITECTURE_MAP auto-refresh $(date +%Y-%m-%d)"
fi
```

Yapi degisiklikleri (yeni AppModule, view ekleme, controller action) varsa
handoff icin journal'da "Mimari etkileri" bolumu ekle.

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

### Adim 6 — Memory Kaydet

Oturum boyunca ortaya cikan bilgileri auto-memory'ye yaz/guncelle:

- **feedback** — duzeltilen veya onaylanan yaklasimlar
- **project** — devam eden is, hedefler, tarihler
- **user** — kullanicinin rolu, tercihleri hakkinda yeni bilgi
- **reference** — referans verilen harici kaynaklar, araclar

Kurallar:
- Mevcut memory'yi tekrarlama — guncelle
- Koddan veya git history'den cikarilabileni kaydetme
- Goreli tarihleri mutlak tarihe cevir (orn. "yarin" → "2026-05-02")
- feedback/project icin **Why:** ve **How to apply:** satirlari ekle
- Kaydedecek anlamli sey yoksa bu adimi atla

### Adim 7 — Obsidian Brain Vault'a Push (ZORUNLU)

Brain vault `D:/Dev/brain` — cross-project Karpathy LLM Wiki. ReportHub journal'i + onemli kararlari brain'e de yansit. Memory referansi: `reference_brain_obsidian.md`.

**A) `log.md`'ye one-liner append (her oturumda zorunlu):**

```bash
BRAIN="D:/Dev/brain"
DATE=$(date +%Y-%m-%d)
# Ornek: "- 2026-05-06 [reporthub] Plan 12 commit-split: Brand+Modules sistemi 2 commit'te kapandi (f4f8748, 15a3164)."
echo "- $DATE [reporthub] <bir cumle ozet>" >> "$BRAIN/log.md"
```

**B) Synthesis dosyasi (sadece onemli oturumlarda — Tier 3 kapanis, mimari karar, pattern, lesson):**

`D:/Dev/brain/synthesis/YYYY-MM-DD-reporthub-<slug>.md` olustur. Sablon:

```markdown
---
date: YYYY-MM-DD
project: reporthub
tags: [<plan-NN>, <konu>]
---

# <Baslik>

## Ne yapildi (1-2 cumle)

## Neden (karar gerekcesi)

## Cross-link
- Plan: `D:/Dev/reporthub/plans/NN-*.md`
- Journal: `D:/Dev/reporthub/docs/journal/YYYY-MM-DD.md`
- Entity/concept: `[[entities/projects/reporthub]]`

## Lesson (varsa pattern/anti-pattern)
```

**C) Brain'i COMMIT ETME** — `D:/Dev/brain/.git/` ayri repo, kullanici review eder. Yazilan `log.md` + varsa `synthesis/*.md` staged birak. `git add` / `git commit` brain icinde calistirma.

**Atlanma kosulu:** Vault yoksa (`D:/Dev/brain/CLAUDE.md` mevcut degil) sessizce atla — kullaniciya bildir.

### Adim 7.5 — NotebookLM Brain'e Push (opsiyonel)

NotebookLM CLI kuruluysa oturum ozetini AI Brain notebook'una ekle:

1. Memory'de `reference_brain_notebook.md` var mi kontrol et (notebook ID: `f2407372`)
2. ID yoksa: `notebooklm list --json` ile "AI Brain" notebook ara; bulunamazsa kullaniciya sor
3. Ozet dosyasini olustur: `$TEMP/session-summary-YYYY-MM-DD.md` (journal iceriginin kisa versiyonu)
4. Push et: `notebooklm source add "$TEMP/session-summary-YYYY-MM-DD.md" --notebook <ID>`

**Auth basarisizsa veya CLI kurulu degilse:** Bu adimi sessizce atla, kullaniciya bilgi ver. Memory'ler, journal ve Obsidian Brain zaten kaydedildi.

### Adim 8 — Ozet Goster
Kullaniciya 5-10 satirlik kisa ozet:
```
Oturum kaydedildi: docs/journal/2026-04-22.md
- Tamamlanan: 4 madde
- Yarim kalan: 2 madde
- Commit: abc1234 docs(journal): 2026-04-22 handoff
- Uncommitted: N dosya (15 esigin altinda, iyi)
- Memory: 2 kaydedildi, 1 guncellendi
- Obsidian Brain: log.md'ye satir eklendi (+ synthesis varsa)
- NotebookLM: push edildi / atlanildi (CLI yok / auth expired)
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
