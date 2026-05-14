# Plan 26 — OCR Fallback (Taranmış PDF Desteği)

**Tarih:** 2026-05-10
**Yazan:** Fikri / Claude
**Durum:** ⛔ **SUPERSEDED 2026-05-14** — [Plan 27 Faz A](27-documents-ai-roadmap.md) "Akıllı Sözleşme Extraction" bu planı absorbe etti. `TesseractOcrExtractor` + `PageImportanceScorer` + per-page confidence gate + vision rescue tüm `AiExtractionWorker` pipeline'ında implement edildi (2026-05-10..2026-05-12 commits). Kalan eksik **`WizardExtractionService` Tesseract chain entegrasyonu** — Plan 33 BUGFIX-4 altında ele alınıyor. Plan 33 R-07 ile `plans/archive/`'a taşınır.

---

## 1. Problem

`AiExtractionWorker` taranmış PDF yüklendiğinde "PDF'ten metin çıkarılamadı" hatası vererek `Failed` durumuna düşüyor. PdfPig yalnızca text-layer'lı PDF'leri okuyabiliyor; taranmış belgeler (görüntü tabanlı) için OCR gerekiyor. Kullanıcıların büyük çoğunluğunun sözleşmeleri taranmış PDF olarak elinde bulunuyor.

## 2. Scope

### Kapsam dahili
- Taranmış PDF → Tesseract OCR → text → mevcut AI analiz zinciri
- Tesseract başarısız / boş dönerse → z.ai vision fallback
- Türkçe tessdata desteği
- `AiExtractionWorker` fallback zinciri

### Kapsam dışı
- El yazısı tanıma (Tesseract desteklemiyor, z.ai vision dener)
- Word/Excel OCR (zaten hata veriyor, değişmiyor)
- Wizard akışı (ayrı servis, dokunulmayacak)
- UI değişikliği (progress step'lere "ocr" zaten var)

### Etkilenen dosyalar
- `Mosaik/Mosaik.csproj` — 2 NuGet: `Tesseract`, `PDFtoImage`
- `Mosaik/Services/Ai/TesseractOcrExtractor.cs` — yeni servis
- `Mosaik/Services/Ai/AiExtractionWorker.cs` — fallback zinciri (~20 satır)
- `Mosaik/Services/Ai/IPdfTextExtractor.cs` — gerek yok (ayrı interface)
- `Mosaik/Program.cs` — `TesseractOcrExtractor` DI kaydı
- `Mosaik/tessdata/tur.traineddata` — Türkçe dil dosyası

**Tahmini boyut:** 5 dosya / ~120 satır.

## 3. Alternatifler

### A: Sadece z.ai Vision
**Açıklama:** PDF sayfaları görüntüye çevrilip z.ai GLM-4V'ye gönderilir, OCR + analiz tek adımda.
**Reddetme sebebi:** Her sayfa API çağrısı = maliyet. Uzun sözleşmelerde (10+ sayfa) çok token harcar. Kullanıcı maliyet odaklı tercih yaptı.

### B: Azure Document Intelligence
**Açıklama:** Microsoft'un managed OCR + form extraction servisi, Türkçe destekliyor.
**Reddetme sebebi:** Ek SaaS bağımlılığı, subscription gerekiyor, basit use case için overkill.

### C: Tesseract önce, z.ai vision fallback (seçilen)
**Açıklama:** Tesseract offline ve ücretsiz; çoğu taranmış sözleşmeyi okur. Tesseract boş / düşük güven döndürürse z.ai vision devreye girer.
**Sebep:** Maliyet öncelikli ama hız da korunuyor. Zaten mevcut z.ai altyapısı kurtarıcı olarak çalışır.

> 🔴 **Contrarian:** Tesseract Türkçe karakter kalitesi değişken; kötü taramada hem Tesseract hem z.ai başarısız olabilir — kullanıcıya "okunaksız belge" mesajı gösterilmeli.
> 🔵 **First Principles:** Gerçek sorun kullanıcının taranmış PDF yüklemesi — upload sırasında "bu PDF metin içermiyor, OCR uygulanacak, süre uzayabilir" uyarısı daha iyi UX olabilir.
> 🟢 **Expansionist:** Tesseract + vision zinciri wizard akışına da eklenebilir (şu an wizard da aynı sorunla karşılaşıyor).
> ⚪ **Outsider:** tessdata dosyası repoya girmeli mi? 15MB binary — `.gitignore` + kurulum scripti daha temiz.
> 🟡 **Executor:** İlk somut adım NuGet ekle + `dotnet build` geçiyor mu kontrol et.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Tesseract native library (leptonica) Windows path sorunu | yüksek | orta | NuGet `Tesseract` Windows binary dahil eder, test et |
| tessdata dosyası repoya girmesi (15MB) | düşük | yüksek | `.gitignore` + `docs/INSTALL.md`'e kurulum notu |
| PDFtoImage PDFium Windows native DLL | orta | düşük | `runtimes/` klasörü NuGet ile gelir |
| Türkçe OCR kalitesi düşük | orta | orta | z.ai fallback zaten var; threshold ayarlanabilir |
| z.ai vision token maliyeti artışı | orta | düşük | Sadece Tesseract başarısız olunca devreye girer |

## 5. Done Criteria

- [ ] Taranmış PDF yüklendiğinde `AiExtractionWorker` Tesseract ile metni okuyabiliyor
- [ ] Tesseract boş/kısa metin dönünce z.ai vision fallback devreye giriyor
- [ ] Türkçe sözleşme örneğiyle smoke test geçiyor
- [ ] `Failed` durumundaki extraction "Retry" ile başarılı sonuçlanıyor
- [ ] Build yeşil, mevcut 321 test geçiyor
- [ ] `.gitignore` güncellendi (tessdata/*.traineddata hariç)
- [ ] `docs/INSTALL.md`'e tessdata kurulum notu eklendi

## 6. Rollback Planı

- `git revert <commit>` — NuGet ref + yeni dosyalar geri alınır
- `TesseractOcrExtractor` DI kaydı `Program.cs`'den kaldırılır
- Worker eski haliyle sadece PdfPig + "PDF metin içermiyor" hatasıyla devam eder
- DB'deki Failed extraction'lar etkilenmez

## 7. Adımlar

1. [ ] **F-01** `Tesseract` + `PDFtoImage` NuGet ekle, `dotnet build` geçiyor mu doğrula
2. [ ] **F-02** `tessdata/tur.traineddata` indir, `.gitignore` güncelle
3. [ ] **F-03** `TesseractOcrExtractor.cs` yaz — PDF → page images → Tesseract → text
4. [ ] **F-04** `Program.cs` DI kaydı
5. [ ] **F-05** `AiExtractionWorker` fallback zinciri: PdfPig boşsa Tesseract, Tesseract boşsa z.ai vision
6. [ ] **F-06** Smoke test — taranmış PDF ile Retry, başarılı extraction doğrula
7. [ ] **F-07** `docs/INSTALL.md` kurulum notu

## 8. İlişkili

- Plan 25: `plans/25-sozlesme-contract.md` — AI extraction pipeline kaynak
- Plan 25.1: `plans/25.1-contract-security-hardening.md` — worker hardening
- Kaynak servis: `Mosaik/Services/Ai/AiExtractionWorker.cs`
- Vision altyapı: `Mosaik/Services/Ai/ZaiVisionProvider.cs`
- Konuşma referans: `docs/journal/2026-05-10.md`

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Geri bildirim alındı
- [ ] Onay alındı: —
