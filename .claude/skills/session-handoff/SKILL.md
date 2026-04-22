---
name: session-handoff
description: Oturum sonu ozet yazar. Bugun yapilanlari, build durumunu, yarim kalan islari, yarina baslangic noktasini docs/journal/YYYY-MM-DD.md dosyasina yazar. Kullanici "handoff", "oturum sonu", "iyi geceler", "kaydet ve kapat", "gunaydin ozet" gibi ifadeler kullandiginda veya /handoff calistirildiginda devreye gir.
allowed-tools: Read, Edit, Write, Bash, Grep, Glob
user-invocable: true
model: inherit
---

# Oturum Devir Skill'i

## Amac
Her oturum sonunda (veya baslangicinda ozet almak icin), gun icinde olanlari kalici bir journal dosyasina yazar. Boylece:
- CLAUDE.md'ye session log yazilmaz (temizlik korunur)
- Yarinki Claude ne olduguna bakar (SessionStart hook zaten okuyor)
- Gecmis kararlar grep'lenebilir

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
- dotnet build: yesil / kirmizi / calistirilmadi
- dotnet test: X yesil, Y kirmizi / calistirilmadi
- JS syntax check: OK / ERR (dosya adi)
- Smoke test (tarayici): yapildi / yapilmadi / kirildi

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
- <ADR'ye yazilmis mi? Yoksa henuz yaziilacak mi?>

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
# Bugunun tarihi (format: YYYY-MM-DD)
date +%Y-%m-%d

# Uncommitted dosya sayisi
git status --porcelain | wc -l

# Son commit'ler (bugun)
git log --since=midnight --oneline

# Son build durumu
# (Eger kullanici son build'i hatirliyorsa ona sor, yoksa "calistirilmadi" yaz)
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
- Dosya varsa: en altta `---` separator + yeni bolum `## Ikinci Oturum` gibi ekle.

### Adim 5 — Ozet Goster
Kullaniciya 5-10 satirlik kisa ozet:
```
Oturum kaydedildi: docs/journal/2026-04-22.md
- Tamamlanan: 4 madde
- Yarim kalan: 2 madde
- Uncommitted: 12 dosya (15 esigin altinda, iyi)
- Yarina baslangic: <ilk adim>
```

## Ornek Tetikleme Durumlari

- Kullanici "iyi geceler" dedi -> bu skill'i otomatik cagir, journal yaz, ozet ver.
- Kullanici "/handoff" yazdi -> aciklikla cagirdi.
- Kullanici "devam edecegiz" dedi -> mevcut durumu kaydet.
- Kullanici "gunaydin" dedi -> bu sefer ters yon: en son journal'i oku ve "nerede kaldik?" ozet ver.

## Dikkat

1. **Hic journal yoksa:** docs/journal/ olustur, ilk dosya bugunun tarihi.
2. **Commit edilmesine karar verme:** skill sadece yazmak icin, commit'i kullanici ister.
3. **CLAUDE.md'ye ekleme:** Session log CLAUDE.md'ye **yazilmamali** (200 satir esigi + 3 katman ayrimi kurali).
4. **Ust uste yazim:** Ayni gun ikinci kez cagrilirsa `## Oturum 2` eklenir, eski icerik silinmez.
5. **Turkce yaz:** UI metni ve journal icerigi Turkce, UTF-8 karakterler kullan ("Düzenle", "Bileşen").

## Iliskili Dosyalar
- SessionStart hook (`.claude/hooks/session-start.sh`): En son journal'i oturum basinda Claude'a enjekte eder.
- Baglam yonetimi anayasasi: `docs/CONTEXT_MANAGEMENT.md`.
