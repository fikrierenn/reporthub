# Plan 49 — Zero-UI Operations (Arayüzsüz Kurumsal Akış)

**Durum:** 🔬 ARAŞTIRMA TASLAĞI 2026-05-25 — kullanıcı strategic input (radikal paradigm 2)
**Tier:** 3 (yeni paradigma + speech-to-text + computer vision + KVKK + cross-modül)
**Effort:** 80-120h (7 faz, 6-8 hafta) — tahmini, deep dive sonrası netleşir
**Aciliyet:** 🟣 Plan 46 (PWA) ✅ + Plan 41 (Form Builder) ✅ + Plan 44 (RAG Guard) ✅ sonrası

---

## 1. Vizyon

İnsanlar form doldurmaktan, menü gezinmekten, portal arayüzünden nefret eder. **En güzel arayüz, hiç var olmayan arayüzdür.**

**Radikal fikir:** Çalışan Mosaik portalına **girmesin**. Mosaik çalışanın doğal kanalları (ses + fotoğraf + e-posta + IM) dinleyen **görünmez orkestratör** olsun.

Çalışan reyonda yürürken cep telefonundaki Mosaik widget'ına basılı tutar: *"Reyon 4'teki raf kırılmış, kitaplar dökülüyor."* + fotoğraf. **1 saniye sonra:** hasar bildirim formu doldurulmuş + bakım personeline task atanmış + acil ise mağaza müdürüne push gitmiş.

---

## 2. Senaryo (BKM gerçek)

> Depocu Mehmet sayım yaparken hasarlı palet görür. Telefon çıkar, Mosaik widget basılı tut, konuş:
>
> *"Depo D2 koridorunda hasarlı palet var. Yaklaşık 15 kutu eziImış. Yağmurdan ıslanmış olabilir."*
>
> + 2 fotoğraf çek (palet + zemin)
>
> **Mosaik arka plan pipeline (~3 saniye):**
> 1. **Whisper (TR)** local ASR → ses → metin
> 2. **Qwen 3B intent parse** → action: "hasar_bildirimi", location: "Depo D2", count: 15, severity: "medium-high", water_damage: true
> 3. **Vision provider** (Gemini/OpenAI fallback Plan 27) fotoğraf → "yırtık ambalaj, 12 kutu görünür, su izi var" + risk_score 0.78
> 4. **Form Builder template match** → "Hasar Bildirim Formu" (Plan 41 DataElement ID 23) auto-fill
> 5. **Workflow trigger** → "yüksek risk hasar" template → bakım personeli + mağaza müdürü Task assign + push
> 6. **Audit log + Documents archive** ses + foto evidence
>
> Mehmet **portal açmadı, form görmedi**. Konuştu + fotoğraf çekti. Süreç başladı.

---

## 3. Mimari (ön araştırma)

### 3.1 Component'ler

```
Mosaik.Modules.ZeroUI/
├─ Services/
│  ├─ Speech/
│  │  ├─ IWhisperRunner.cs           — local ONNX Whisper TR model
│  │  └─ WhisperRunner.cs            — Microsoft.ML.OnnxRuntime + whisper-tiny-tr
│  ├─ Intent/
│  │  ├─ IIntentParser.cs            — Qwen prompt → action+entity+severity
│  │  └─ QwenIntentParser.cs
│  ├─ Vision/
│  │  └─ IDamageVisionAnalyzer.cs    — Plan 27 vision provider reuse
│  ├─ Orchestration/
│  │  ├─ ZeroUiOrchestrator.cs       — pipeline: ASR → Intent → Vision → FormFill → Workflow
│  │  ├─ FormTemplateMatcher.cs      — intent → best matching FormDefinition
│  │  └─ WorkflowTrigger.cs          — severity → workflow template
│  └─ Audit/
│     └─ ZeroUiAuditService.cs       — voice + photo evidence archive
├─ Areas/ZeroUI/
│  ├─ Controllers/CaptureController.cs  — POST /zeroui/capture (audio + photos)
│  └─ Views/Capture/                    — minimal trigger UI (basılı-tut mic + camera button)
└─ wwwroot/assets/js/zero-ui-widget.js  — PWA widget (Plan 46 entegrasyon)
```

### 3.2 Pipeline

```
Telefon: basılı-tut mic + photo
  ↓
POST /ZeroUI/Capture (audio.webm + photo[].jpg)
  ↓
ZeroUiOrchestrator.ProcessAsync()
  ├─ Whisper local ASR → text (TR)
  ├─ Qwen intent parse → IntentResult { action, entities[], severity, confidence }
  ├─ Vision provider photo analyze → VisionResult { risk_score, detected_items[] }
  ├─ FormTemplateMatcher → best FormDefinition (intent.action → form_id)
  ├─ FormResponse auto-fill (Plan 41) + status="ZeroUiSubmitted"
  ├─ WorkflowTrigger → severity threshold → assign tasks (Plan 36)
  ├─ Notification (Plan 17) → bakım + mağaza müdürü (push + email)
  └─ Audit archive (voice + photo → Documents Plan 27 retention)
  ↓
Response: "Hasar bildirimi kaydedildi (#12345). Bakım birimi yola çıktı."
  + opsiyonel: ses/metin confirmation TTS feedback
```

### 3.3 Confidence Gates

```
Low confidence (< 0.6) → Mosaik geri konuşur:
  "Anladığım: hasarlı palet, Depo D2, 15 kutu, su hasarı. Doğru mu? Evet/Düzelt."
  → kullanıcı sözlü onay veya manuel düzeltme
Medium (0.6-0.85) → form auto-fill + kullanıcıya "Onay" push (1 dk timeout sonra otomatik kabul)
High (>0.85) → otomatik submit + audit
```

---

## 4. KVKK + Teknik Riskler

| Risk | Etki | Mitigation |
|---|---|---|
| Ses kaydı KVKK aydınlatma + saklama | **CRITICAL** | Aydınlatma metni güncelle; ses sadece pipeline transit (max 1dk RAM) + transcription kalıcı; ham ses on-the-fly silinir (workplace surveillance riski) |
| Qwen intent yanlış parse → yanlış süreç | yüksek | Confidence gate + low confidence kullanıcıya geri sor + audit |
| Whisper TR model kalite (aksan, gürültü) | yüksek | whisper-tiny-tr → small/medium aşamalı; gürültülü ortam için noise suppression preprocessing |
| Vision provider cloud (Gemini/OpenAI) photo upload — KVKK | yüksek | Plan 44 chunk permission ile bağlı; "kişi yüzü/plaka detect" → local-only fallback |
| Yanlış kişiye task assign (location parse hatası) | orta | Lokasyon dictionary (Depo D2 = department_id 5 mapping); fuzzy match + onay |
| Saha gürültülü ortam ASR fail | orta | Yazılı text fallback (PWA Plan 46 textbox) |
| Ses tetikleme suistimal (şaka kayıtlar) | düşük | Per-user rate limit + admin review duplicate |

---

## 5. Bağımlılıklar (ön)

- **Hard prereq:** Plan 46 PWA (mobile widget host)
- **Hard prereq:** Plan 41 Form Builder (FormDefinition + auto-fill API)
- **Hard prereq:** Plan 44 RAG Permission Guard (vision provider chunk policy)
- **Hard prereq:** Plan 36 Workflow Designer (auto trigger)
- **Reuse:** Plan 27 vision provider (Gemini/OpenAI fallback)
- **Reuse:** Plan 34.1 Qwen + LLamaSharp + skill catalog
- **Reuse:** Plan 17 NotificationService

**Yeni NuGet/JS:**
- Whisper.NET (MIT) veya OpenAI Whisper ONNX export
- whisper-tiny TR fine-tune modeli (Hugging Face) — ~75MB
- MediaRecorder API (browser native) — yok

---

## 6. Deep Dive Sonraki Adımlar

Bu plan **araştırma taslağı**. Implementation öncesi:

1. **POC (2 hafta):** Sadece ses → text → intent → 1 form auto-fill (hasar bildirim). Vision skip. 5 örnek senaryo test.
2. **Whisper TR model benchmark:** tiny vs small vs medium accuracy on BKM saha gürültüsü
3. **5 lens deep tradeoff:** Contrarian ("portal değil chatbot olalım, Slack varken niye Mosaik?"), First Principles (saha personeli mobile-first), Expansionist (intent → SOP search "İSG eğitimi prosedürü nedir?"), Outsider (Alexa/Siri/Google Assistant niye corporate'de yok?), Executor (POC code)
4. **KVKK ön onay:** DPO consult — workplace surveillance + ses biometrik veri sayılır mı?
5. **ADR-031 adayı:** "Zero-UI orchestrator pipeline + ASR + intent + vision compose"

---

## 7. Bu plan ne zaman aktif olur?

Plan 46 PWA production + Plan 44 RAG Guard ✅ + Plan 41 v2 builder UI sonrası. **Erken implement = LLM hata maliyeti yüksek.**

**Tahmini başlangıç:** 2026 Q4 (Kasım-Aralık).

---

## 8. Açık Sorular

1. **Whisper local mi cloud mu?** — Önerim: local-first KVKK için, OpenAI Whisper cloud fallback opt-in (admin).
2. **Confidence gate'leri kim tunable?** — Önerim: admin per-form-type (hasar bildirim hassas → düşük threshold).
3. **Şive/aksan handling?** — Önerim: V1 standart TR; bölgesel aksan v2 (Adana/Karadeniz fine-tune).
4. **Multimodal (ses + foto + GPS aynı anda)?** — Önerim: V1 ses+foto; GPS Plan 49.1 opsiyonel (saha tracking KVKK ekstra dikkat).
5. **Push button vs always-listening?** — Önerim: **kesinlikle push-to-talk** (always-listening KVKK + pil felaketi).
