---
name: notebooklm
description: Google NotebookLM CLI entegrasyonu — notebook oluştur, kaynak ekle (URL/YouTube/PDF), soru sor, podcast/video/rapor/quiz üret, indir. "/notebooklm" veya "podcast oluştur", "quiz üret", "notebooklm kur" gibi ifadelerde tetikle.
allowed-tools: Read, Edit, Write, Bash, Grep, Glob
user-invocable: true
model: inherit
---

# NotebookLM Otomasyon

Google NotebookLM'e tam programatik erişim. Notebook oluştur, kaynak ekle (URL, YouTube, PDF, ses, video, görsel), içerikle sohbet et, artifact üret (podcast, video, rapor, quiz, flashcard, infographic, mind map, slide deck), sonuçları indir.

**Platform:** Windows 11 (Python 3.10+ gerekli).

---

## Adım 0: Kurulum (İlk Kullanımda Otomatik)

### Python Kontrolü

```bash
python --version
```

Python 3.10 altındaysa: [python.org](https://www.python.org/downloads/) üzerinden 3.12+ kur, PATH'e ekle.

### CLI Kur

```bash
python -m venv "$USERPROFILE/.notebooklm-venv"
source "$USERPROFILE/.notebooklm-venv/Scripts/activate"
pip install "notebooklm-py[browser]"
playwright install chromium
```

PATH'e ekle:
```bash
mkdir -p ~/bin
ln -sf "$USERPROFILE/.notebooklm-venv/Scripts/notebooklm.exe" ~/bin/notebooklm
export PATH="$HOME/bin:$PATH"
```

Doğrula:
```bash
notebooklm --help
```

### Kimlik Doğrulama

`notebooklm login` interaktif terminal gerektirir — Claude Code'da çalışmaz. Bunun yerine:

Kullanıcıya söyle:
> Tarayıcı açacağım — Google hesabınla giriş yap ve notebooklm.google.com'a git. Hazır olunca bana söyle.

Login scripti:
```bash
cat > /tmp/nlm_login.py << 'PYEOF'
import json, os, time
from pathlib import Path
from playwright.sync_api import sync_playwright

STORAGE_PATH = Path.home() / ".notebooklm" / "storage_state.json"
PROFILE_PATH = Path.home() / ".notebooklm" / "browser_profile"
SIGNAL_FILE = Path(os.environ.get("TEMP", "/tmp")) / "nlm_save_signal"

SIGNAL_FILE.unlink(missing_ok=True)
STORAGE_PATH.parent.mkdir(parents=True, exist_ok=True)

print("Tarayıcı açılıyor — Google ile giriş yapın...")

with sync_playwright() as p:
    browser = p.chromium.launch_persistent_context(
        user_data_dir=str(PROFILE_PATH),
        headless=False,
        args=["--disable-blink-features=AutomationControlled"],
    )
    page = browser.pages[0] if browser.pages else browser.new_page()
    page.goto("https://notebooklm.google.com/")

    print("Tarayıcı açık. Kaydetme sinyali bekleniyor...")
    while not SIGNAL_FILE.exists():
        time.sleep(1)

    print("Sinyal alındı — oturum kaydediliyor...")
    storage = browser.storage_state()
    with open(STORAGE_PATH, "w") as f:
        json.dump(storage, f)

    cookie_names = [c["name"] for c in storage.get("cookies", [])]
    print(f"{len(cookie_names)} cookie kaydedildi: {cookie_names}")
    browser.close()

SIGNAL_FILE.unlink(missing_ok=True)
print(f"Kimlik doğrulama kaydedildi: {STORAGE_PATH}")
PYEOF

source "$USERPROFILE/.notebooklm-venv/Scripts/activate"
python /tmp/nlm_login.py > /tmp/nlm_login_output.txt 2>&1 &
echo "Login başlatıldı (PID=$!). Tarayıcı birkaç saniye içinde açılacak..."
```

Kullanıcı giriş yaptığını onaylayınca:
```bash
touch "${TEMP:-/tmp}/nlm_save_signal"
sleep 8
cat /tmp/nlm_login_output.txt
```

Doğrulama:
```bash
notebooklm auth check
notebooklm list
```

---

## Otonomi Kuralları

**Onaysız çalıştır:**
- `list`, `source list`, `artifact list`, `auth check`, `status`, `use <id>`, `create`, `ask "..."`, `source add`, `history`

**Onay al:**
- `delete` — yıkıcı
- `generate *` — uzun sürer, başarısız olabilir
- `download *` — dosya sisteme yazar
- `ask "..." --save-as-note` — not oluşturur

---

## Hızlı Referans

| Görev | Komut |
|---|---|
| Notebook listele | `notebooklm list` |
| Notebook oluştur | `notebooklm create "Başlık"` |
| Bağlam ayarla | `notebooklm use <notebook_id>` |
| URL kaynak ekle | `notebooklm source add "https://..."` |
| Dosya ekle | `notebooklm source add ./dosya.pdf` |
| YouTube ekle | `notebooklm source add "https://youtube.com/..."` |
| Soru sor | `notebooklm ask "soru"` |
| Podcast üret | `notebooklm generate audio "talimatlar"` |
| Video üret | `notebooklm generate video "talimatlar"` |
| Rapor üret | `notebooklm generate report --format briefing-doc` |
| Quiz üret | `notebooklm generate quiz` |
| Flashcard üret | `notebooklm generate flashcards` |
| İnfografik üret | `notebooklm generate infographic` |
| Mind map üret | `notebooklm generate mind-map` |
| Slide deck üret | `notebooklm generate slide-deck` |
| Artifact durumu | `notebooklm artifact list` |
| Ses indir | `notebooklm download audio ./output.mp3` |
| Video indir | `notebooklm download video ./output.mp4` |
| Slide indir (PPTX) | `notebooklm download slide-deck ./slides.pptx --format pptx` |
| Rapor indir | `notebooklm download report ./report.md` |

## Üretim Tipleri

| Tip | Komut | Seçenekler | Format |
|---|---|---|---|
| Podcast | `generate audio` | `--format [deep-dive\|brief\|critique\|debate]`, `--length [short\|default\|long]` | .mp3 |
| Video | `generate video` | `--format [explainer\|brief]`, `--style [auto\|classic\|whiteboard\|kawaii\|...]` | .mp4 |
| Slide Deck | `generate slide-deck` | `--format [detailed\|presenter]`, `--length [default\|short]` | .pdf/.pptx |
| Rapor | `generate report` | `--format [briefing-doc\|study-guide\|blog-post\|custom]` | .md |
| Quiz | `generate quiz` | `--difficulty [easy\|medium\|hard]`, `--quantity [fewer\|standard\|more]` | .json/.md |
| Flashcard | `generate flashcards` | `--difficulty`, `--quantity` | .json/.md |
| İnfografik | `generate infographic` | `--orientation [landscape\|portrait\|square]` | .png |
| Mind Map | `generate mind-map` | (anlık) | .json |

## Yaygın İş Akışları

### Araştırma → Podcast
1. `notebooklm create "Araştırma: [konu]"`
2. Her URL/belge için `notebooklm source add`
3. Kaynaklar hazır olana kadar bekle: `notebooklm source list --json`
4. `notebooklm generate audio "Odak: [belirli açı]"`
5. `notebooklm artifact list` ile durumu takip et
6. `notebooklm download audio ./podcast.mp3`

### Belge Analizi
1. `notebooklm create "Analiz: [proje]"`
2. `notebooklm source add ./belge.pdf`
3. `notebooklm ask "Temel noktaları özetle"`

## Hata Yönetimi

| Hata | Sebep | Çözüm |
|---|---|---|
| Auth/cookie hatası | Oturum süresi doldu | Login scriptini tekrar çalıştır |
| "No notebook context" | Bağlam ayarlanmamış | `notebooklm use <id>` |
| Rate limiting | Google kısıtlaması | 5-10 dk bekle, tekrar dene |
| Download başarısız | Üretim tamamlanmamış | `artifact list` ile durum kontrol |

## Bilinen Kısıtlamalar

- Üretim süreleri: ses 10-20 dk, video 15-45 dk, quiz/flashcard 5-15 dk
- Google rate limit'e takılabilir
- Gayri resmi API — Google habersiz değiştirebilir
