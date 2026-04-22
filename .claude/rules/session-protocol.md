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

### Kullanıcıya cevap

Yukarıdaki 4 adım **sessizce** yapılır — kullanıcıya "şunu okudum şunu okudum" demeye gerek yok. Cevap sadece bu okumalara dayanır, hafıza tahminine değil.

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

---

## Oturum Sonu

### Tetikler

Kullanıcı "iyi geceler" / "handoff" / "kaydet ve kapat" / "/handoff" / "devam edeceğiz" dediğinde `.claude/skills/session-handoff/SKILL.md` devreye girer.

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
