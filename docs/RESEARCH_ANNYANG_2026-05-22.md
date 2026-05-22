# Annyang JS Speech Recognition — Research Notu

**Tarih:** 2026-05-22
**Kaynak:** `https://github.com/TalAter/annyang`
**Tetikleyici:** Kullanıcı paylaşımı, oturum içi pivot

## Özet

Annyang, browser tabanlı voice command kütüphanesi. **Wrapper** — gerçek STT (Speech-to-Text) tarayıcının `webkitSpeechRecognition` / `SpeechRecognition` API'sine bağlı, **annyang kendi backend tutmaz**.

| Özellik | Değer |
|---|---|
| Boyut | 2KB minified |
| Lisans | MIT |
| Dependencies | 0 |
| Aktiflik | v3.0.0 (2026-03-11), 810 commit |
| API | `addCommands` + `start()` + `setLanguage('tr-TR')` |
| Türkçe | `setLanguage('tr-TR')` — Web Speech API'nin desteklediği kadar |
| Offline | **HAYIR** — browser STT cloud'a bağımlı |
| Tarayıcı | Chrome/Edge tam (Google STT cloud); Safari kısmi; Firefox **yok** |

## Veri akışı (kritik)

```
Microphone (browser)
  → SpeechRecognition.start()
  → tarayıcı ses verisini bulut STT'ye gönderir
      Chrome  → Google Cloud Speech
      Edge    → Microsoft Speech (Azure)
      Safari  → Apple's STT (yerel olabilir, doc belirsiz)
  → transcribed text döner
  → annyang `commands` regex match
```

**KVKK risk:** Mikrofondan alınan ses BKM kurumsal ağ dışına çıkar. Kullanıcı kişisel veri (isim, TC, telefon) sesli söylerse provider'ın DPA'sına tabi olur.

## Mosaik kullanım senaryoları

| Senaryo | Risk | Karar |
|---|---|---|
| SOP "Okudum + Onayladım" minimal komut | Düşük (tek kelime, kişisel veri yok) | Plan 34.x opt-in adayı |
| Genel form alanı doldurma (ad/email/adres sesli) | **YÜKSEK** — kişisel veri cloud'a | Reddedildi |
| Search bar voice navigation | Orta — arama metni içerik içerir | Plan ileri |
| Rapor filtreleme komutu ("Mayıs raporu aç") | Düşük | Faz ileri opsiyon |
| Toplantı notu otomatik transkript | **YÜKSEK** — KVKK + saklama süresi | Plan dışı |

## Karar (2026-05-22)

**Şu an entegrasyon yok.** Sebep:
1. KVKK Plan 40 henüz tamamlanmadı — ses verisi data element envanteri dışında
2. Provider DPA (Google/Microsoft) yazılı kontrol gerek
3. Web Speech API browser bağımlılığı (Firefox kullanan user dışlanır)
4. Plan 34 Faz E+F öncelikli (bildirim + AI Danışman)

**Geri açma koşulu:**
- KVKK Plan 40 Faz 6 tamamlandı (DataElement envanter)
- Provider DPA imzalandı (Google Workspace Enterprise veya Azure Speech)
- BKM IT onayı (microphone permission policy)
- Opt-in kullanıcı (varsayılan kapalı)
- Banner: "Sesli komut etkin — bilgisayar mikrofonu Google'a/Microsoft'a gönderiyor"

## Önerilen minimal POC (geri açıldığında)

`/SOP/My/Read` view'da opsiyonel buton:

```html
<button x-data="{ on: false }" x-on:click="
    if (!annyang) return alert('Tarayıcı desteklemiyor.');
    annyang.setLanguage('tr-TR');
    annyang.addCommands({ 'onayla': () => document.querySelector('#confirm-form').submit() });
    annyang.start();
    on = true;
" :class="on ? 'btn primary' : 'btn ghost'">
    🎤 Sesle Onayla
</button>
```

Sadece **"onayla"** tek kelime → form submit. Kişisel veri sözlü söylenmez. KVKK risk minimal.

Production öncesi:
- annyang.js wwwroot'a indir (CDN bağımlılığı yok)
- `Authorize` user opt-in flag (`User.VoiceCommandEnabled`)
- Audit log: `voice_command_used` event

## Alternatifler (KVKK açısından daha iyi)

1. **Whisper.cpp WASM** — yerel browser STT, model dosyası 75-150MB, ses cloud'a gitmez. Ama yavaş + bundle ağır.
2. **Vosk Browser** — Web Speech API alternatifi, model browser'da çalışır. Türkçe model var.
3. **Kuruma yerel STT sunucu** — Whisper API self-hosted (Mosaik içinde Docker). KVKK ideal ama altyapı yatırımı.

vNext kalbinin **sonraki** halkası için (Plan 50+ aday).

## Backlog satırı

TODO.md'ye eklenecek:

```markdown
- [ ] **LOW · Voice command araştırma** — annyang Web Speech API wrapper, KVKK riski (ses cloud). Geri açma koşulu: Plan 40 Faz 6 + provider DPA. Alternatif: Whisper.cpp WASM (yerel). Detay: docs/RESEARCH_ANNYANG_2026-05-22.md.
```

## İlişkili

- `plans/40-kvkk-process-backbone.md` — DataElement envanter prereq
- `.claude/rules/security-principles.md` — kişisel veri sızıntı koruma
- `docs/VISION.md` § Accessibility (a11y için voice opsiyonu uzun vade)
