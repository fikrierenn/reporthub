# Footprint Ladder — Yeni Yetenek En Dar Basamakta

_Rule katmanı: core (her oturum geçerli). `paths:` YOK — compact sonrası survive. Pusula footprint-ladder (Hermes narrow-waist) uyarlaması, 2026-06-29._

## Neden bu dosya var

Mosaik çatısı şişti ve **uyarıyor ama budamıyor**:
- MEMORY.md limit aştı (25.5KB > 24.4KB, index kesiliyor).
- 30 aktif plan, 23 ADR, 24 skill, 19 rule — büyük kısmı 35-53 gün dokunulmamış.
- session-start her oturum "29 plan stale, ARCHITECTURE_MAP 25 gün stale" diyor.

Sebep: "yeni ihtiyaç = yeni dosya" refleksi. Her yeni kalıcı yapı (rule/skill/agent/ADR/plan/sayfa) **bakım yükü + her-oturum bağlam maliyeti** getirir. Bu kural o refleksi frenler. Kardeş budama mekanizması: `consolidate-mosaik` skill.

## Temel İlke

**Çekirdek dar bel; yetenek kenarda.** Bir ihtiyaç çıktığında merdivenin EN ALT (en dar, en ucuz) basamağında çöz. Üst basamağa **ancak alt basamak yetmezse** çık.

## Merdiven (alttan üste — alt = dar/ucuz)

| # | Basamak | Ne zaman | Maliyet |
|---|---|---|---|
| 1 | **Mevcut rule/skill/view/service'i genişlet** | Var olana 1 satır/fonksiyon/bölüm eklemek çözüyorsa | ~0 yeni yüzey |
| 2 | **Yeni skill** | Tekrarlanan iş akışı; tetik-bazlı yüklenir (her zaman bağlamda değil) | Düşük — sadece tetiklenince |
| 3 | **Yeni rule** | Kalıcı davranış kuralı | Orta — core ise her oturum bağlamda |
| 4 | **Yeni agent** | Özelleşmiş salt-okuma/denetim alt-ajan | Orta — tanım + model tier seçimi |
| 5 | **Yeni ADR** | Geri-alınması zor mimari karar (kalıcı kayıt) | Orta — kalıcı referans |
| 6 | **Yeni plan (Tier 3)** | 3+ klasör / schema-security-UX / kullanıcı-görünür | Yüksek — onay + implement + arşiv döngüsü |
| 7 | **Yeni modül/sayfa (SON ÇARE)** | Kullanıcı-görünür yeni yüzey; başka basamak çözemiyor | En yüksek — UI+EF+SP+test+nav+A11y+i18n |

## Kurallar

1. **Aşağıdan yukarı sor:** "Bunu mevcut X'i genişleterek çözebilir miyim?" → hayırsa bir üst basamak.
2. **Atlama yapma:** 7. basamağa (yeni sayfa/modül) çıkmadan önce 1-6 elendi mi?
3. **Şüphede aşağıda kal.** Dar çözüm yetmezse büyütmek kolay; geniş çözümü küçültmek zor.
4. **Yeni skill/agent/rule ÖNCESİ mevcut listeyi kontrol et.** `.claude/skills/` + `.claude/agents/` + `.claude/rules/` + global + plugin'lerde aynı isim/işlev varsa **GENİŞLET, yaratma.** (Pusula 17.06 dersi: `bkm-sunum` proje-local yaratıldı, global versiyonu varmış → dup.)
5. **Dış repodan "esin"** (awesome-X, superpowers, claudeskills) diye Mosaik'te zaten olanı tekrar kurma → önce mevcut rule/skill ile kıyasla.
6. **Yeni dosya = onay.** footprint maliyeti olan her kalıcı yapı kullanıcı onayı + git-revert'lenebilir olmalı.

## Anti-pattern

- ❌ "Yeni özellik = yeni sayfa/modül" refleksi → önce mevcut sayfaya bölüm/bileşen eklenebilir mi?
- ❌ Tek-kullanımlık iş için yeni skill/agent → mevcut akışta inline çöz.
- ❌ Her tasarım/konu için ayrı generic Anthropic skill import etmek (frontend-design + visual-design + responsive + interaction + ... 8 design skill yığını → biri yeter).
- ❌ "İleride lazım olur" geniş soyutlama (bkz. `coding-discipline.md` simplicity-first).
- ❌ Aynı bilgiyi ikinci rule'a yazmak (`session-memory.md` "aynı bilgi iki yerde yaşamaz").

## İlişkili

- `.claude/skills/consolidate-mosaik/SKILL.md` — budama (bu kuralın yürütücüsü; biriken yapıyı archive eder).
- `.claude/rules/coding-discipline.md` — simplicity-first (aynı damar, kod tarafı).
- `.claude/rules/plan-first.md` — Tier sistemi (6. basamak = Tier 3 plan).
- `.claude/rules/before-major-change.md` — silme/refactor öncesi grep + ARCHITECTURE_MAP.
- `.claude/rules/session-memory.md` — katman ayrımı + eşikler.
