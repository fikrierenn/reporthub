# ADR-022 — SOP AI Advisor Stack: Qwen 2.5 3B + LLamaSharp + multilingual-e5 + QLoRA

**Tarih:** 2026-05-24
**Statü:** Önerildi (Plan 34.1 Faz 0 onayı ile aktif)
**Karar verenler:** Fikri / Claude
**Bağlam:** [Plan 34.1 SOP RAG Advisor](../../plans/34.1-sop-rag-advisor.md) — cross-SOP RAG advisor, in-process .NET inference, Ollama bağımlılığı yasak, LoRA-trainable, Türkçe destekli.

---

## 1. Bağlam

Plan 34 Faz F orijinal scope **single-SOP chat MVP** yetersiz. Kullanıcı kararı (2026-05-24): "ai ye sor o iligli sop ları bulmalı yorum yapmalı sopları ezbere bilmeli yorum çıkarım yapabilmeli". Cross-SOP RAG advisor + sürekli iyileşen (fine-tune) yapı.

**Ek şart:** Ollama bağımlılığı yasak (BKM kurumsal proxy + Windows servisi yönetim yükü + IPC overhead).

**Araştırma raporu:** Agent (general-purpose) 2026-05-24, hedef = küçük + eğitilebilir + Türkçe + in-process .NET + Ollama'sız. Detay [Plan 34.1 §10 Kararlar](../../plans/34.1-sop-rag-advisor.md#10-kararlar-varsayılan-kabul--kullanıcı-revize-edebilir).

---

## 2. Karar

**SOP AI Advisor stack:**

| Bileşen | Sürüm | Lisans | Rol | Boyut/Maliyet |
|---|---|---|---|---|
| **Qwen 2.5 3B Instruct** | GGUF Q4_K_M | **Apache 2.0** | LLM (cevap üretimi) | ~2.0 GB disk, ~2.4 GB RAM |
| **LLamaSharp** | 0.27 | **MIT** | llama.cpp .NET binding (in-process inference) | ~150 MB runtime overhead |
| **LLamaSharp.Backend.Cpu** | 0.27 | MIT | CPU backend (BKM laptop, GPU yok) | — |
| **Microsoft.ML.OnnxRuntime** | 1.20+ | **MIT** | Embedding runtime | ~50 MB |
| **intfloat/multilingual-e5-base** | ONNX FP32 (resmi, INT8 yok) | **MIT** | Embedding (RAG retrieval) | ~1.1 GB (FP32) |
| **Microsoft.Extensions.AI** | preview | **MIT** | `IChatClient` abstraction (model swap maliyeti 0) | — |
| **Unsloth + QLoRA** (Python, ayrı süreç) | latest | Apache 2.0 | Fine-tune pipeline (Colab T4 ücretsiz) | — |

**Toplam yıllık maliyet: $0.** Tüm bileşenler permissive lisans (MIT/Apache 2.0). KVKK uyumlu (lokal inference, yurt dışı aktarım yok). BKM kurumsal kullanım izin.

**Toplam advisor footprint:** ~3.5 GB RAM (Qwen Q4 2.4 + KV cache 0.2 + embedding 0.15 + runtime 0.15 + Mosaik base 0.5).

### 2.1 Use case mapping

| Use case | Bileşen | Pattern |
|---|---|---|
| Kullanıcı SOP sorusu | LLamaSharp + Qwen 2.5 3B | RAG context + system prompt |
| SOP chunk embedding | OnnxRuntime + e5-base | 512 token + 64 overlap |
| Cross-SOP retrieval | In-memory cosine | Top-K=4 + min similarity 0.5 |
| Domain fine-tune (3 ayda 1) | Unsloth + Colab + QLoRA | LoRA adapter GGUF |
| LoRA hot-swap | LLamaSharp `--lora-scaled` flag | Runtime (merge ETME) |
| Cloud fallback (opt-in) | Mevcut `IAiSummaryProvider` (z.ai/Gemini/Groq) | Default OFF, admin toggle |

---

## 3. Reddetme Gerekçeleri

### 3.1 Reddedilen LLM adayları

| Aday | Boyut Q4 | Sebep |
|---|---|---|
| **Phi-3.5-mini-instruct (3.8B)** | ~2.3 GB | İkinci sıra. TR multilingual var (MMLU 55.4 sub-8B liderlerinden) + MIT en temiz. Reddedilmedi — Plan B fallback. Qwen Türk fine-tune ekosistemi (Unsloth resmi Qwen pipeline) daha güçlü. |
| **Trendyol-LLM-8B-T1 / 7B** | ~4-5 GB | En iyi TR ama 4GB RAM hedefini aşar. v2 upgrade path (GPU sunucu eklenirse). |
| **Llama 3.2 1B / 3B** | ~1-2 GB | Resmi multilingual desteği EN/DE/FR/IT/PT/ES/HI/TH — **TR yok**. + Llama 3.2 Community License (700M MAU teorik limit + "Built with Llama" attribution). Apache 2.0 lehine reddedildi. |
| **Gemma 2 / 3** | ~1.6 GB | Gemma Terms (Google "Prohibited Use" listesi) Apache 2.0'dan kısıtlayıcı. TR de sınırlı. |
| **SmolLM2 1.7B** | ~1.1 GB | TR zayıf, eğitim seti EN ağırlıklı. |
| **TinyLlama 1.1B** | ~700 MB | Eski, TR yok. |
| **DeepSeek (3B-class)** | — | MIT iyi ama TR fine-tune ekosistem ince. |
| **Cosmos (ytu-ce-cosmos/Turkish-Llama-8b)** | ~5 GB | Llama 3 Community lisansı + 4GB üstü. Akademik TR LLM, ama lisans ve boyut iki sebepten reddedildi. |
| **KanarYa 2B** | ~1.3 GB | Pre-trained base, instruct değil. Sıfırdan SFT 50 SOP ile yetersiz. Apache 2.0 ama uygulamada elenir. |

### 3.2 Reddedilen runtime'lar

| Aday | Sebep |
|---|---|
| **Ollama** | Kullanıcı şartı — yasak. Ek process + IPC + Windows servisi yönetim yükü. |
| **vLLM / TGI** | Python ana, .NET'e REST. Mosaik in-process hedefine aykırı. GPU gerektirir. |
| **Microsoft.ML.OnnxRuntimeGenAI (sadece)** | LLamaSharp ile karşılaştırıldı. ONNX GenAI 0.13 stabil ama GGUF formatı yok — fine-tune sonrası GGUF dönüşüm/ONNX dönüşüm iki yol. GGUF + LLamaSharp daha basit. Yine de `IChatClient` arkasına ikincil adapter ekleme kapısı açık (model swap kararı). |
| **Python FastAPI mikroservis** | Ayrı deployment, dep yönetim, REST overhead. In-process kararına aykırı. |

### 3.3 Reddedilen yaklaşımlar

| Yaklaşım | Sebep |
|---|---|
| **Online learning (RLHF inference-time)** | Hiçbir small LLM yapmıyor. İmkansız. |
| **Cloud-only (Groq/Gemini default)** | KVKK risk + maliyet patlama. Opt-in fallback olarak kalır. |
| **Embed cloud + LLM local** | Yarı KVKK risk (soru cloud'a gider). Tutarsız. |
| **Single-SOP chat MVP (Plan 34 Faz F orijinal)** | Kullanıcı cross-SOP istedi. Scope yetersiz. |

---

## 4. Sonuçlar

### 4.1 Olumlu sonuçlar

- **$0 lisans maliyet.** Tüm bileşenler permissive.
- **KVKK uyumlu.** Yurt dışı aktarım yok (default), cloud fallback opt-in audit.
- **BKM laptop CPU yeterli.** ~3.5 GB RAM, 8GB+ kurumsal laptop standart.
- **Fine-tune ücretsiz.** Colab T4 16GB VRAM yeterli, 50-200 örnek için 30-90 dk.
- **Documents Plan 27 Faz E reuse path.** `Mosaik.Core.AI.Embed` + `Mosaik.Core.AI.Local` shared kit hazır pattern.
- **Model swap maliyeti 0.** `Microsoft.Extensions.AI.IChatClient` arkasında Qwen → Phi-3.5 değiştirme NuGet swap.

### 4.2 Olumsuz sonuçlar / kısıt

- **CPU latency 15-30 sn / cevap.** Background warm-up gizler ama kullanıcı tahammül eşiği test edilmeli.
- **Hallucination riski 3B model.** Sıkı RAG (top-K=4 + threshold 0.5) + system prompt "kaynak yok → cevap verme" zorunlu.
- **50 SOP corpus küçük.** Sentetik QA augmentation (Groq/Gemini, insan review) gerekli.
- **LoRA merge bug llama.cpp #7062.** Merge ETME — runtime `--lora-scaled` flag.
- **BKM proxy → HF Hub download yok.** Model dosyaları MSI / installer pre-bundle.
- **LLamaSharp .NET 10 forward-compat.** Resmi target net8 + netstandard2.0. Smoke test şart.

### 4.3 v2 Upgrade Path

| Sinyal | Aksiyon |
|---|---|
| CPU latency >30 sn kullanıcı şikayet | Ortak GPU sunucu (RTX 4060 ~$1000) + LLamaSharp.Backend.Cuda |
| Türkçe kalite Qwen 3B yetersiz | Trendyol-LLM-7B-chat-v4.1.0 Q4 (~4 GB) — RAM yeterse upgrade |
| 1000+ feedback toplandı | DPO (Direct Preference Optimization) → LoRA refresh |
| Documents Plan 27 Faz E başladı | `Mosaik.Core.AI.Embed` + `.Local` shared kit refactor |

---

## 5. Uygulama / Mosaik entegrasyon

### 5.1 Klasör yapısı

```
Mosaik.Core/AI/
  Embed/IMosaikEmbedder.cs
  Local/ILlmRunner.cs

Mosaik/Services/Ai/
  E5Embedder.cs
  LlamaSharpRunner.cs
  ModelWarmupHostedService.cs

Mosaik.Modules.SOP/Services/
  SopChunker.cs
  SopIndexer.cs
  SopRagAdvisorService.cs
  SopRateLimitGuard.cs
  SopAiHistoryCleanupJob.cs
  SopSyntheticQaGenerator.cs

App_Data/models/
  llm/qwen25-3b-instruct-q4_k_m.gguf       # ~2.0 GB, pre-bundle MSI
  embed/e5-base-int8.onnx                  # ~110 MB, pre-bundle
  lora/sop-vYYYY-MM.gguf                   # ~50 MB, fine-tune output
  tokenizer/                               # e5 tokenizer files

scripts/ai-train/
  sop-finetune.ipynb                       # Colab notebook
  export-gguf.py                           # LoRA → GGUF wrapper
  README.md
```

### 5.2 DI registrations (`Mosaik/Program.cs`)

```csharp
// Plan 34.1 — SOP AI Advisor stack (ADR-022)
builder.Services.AddSingleton<Mosaik.Core.AI.Embed.IMosaikEmbedder, Mosaik.Services.Ai.E5Embedder>();
builder.Services.AddSingleton<Mosaik.Core.AI.Local.ILlmRunner, Mosaik.Services.Ai.LlamaSharpRunner>();
builder.Services.AddHostedService<Mosaik.Services.Ai.ModelWarmupHostedService>();
```

### 5.3 Cold start mitigation

`ModelWarmupHostedService : IHostedService` uygulama başlangıcında arka plan thread'inde:
1. LLM GGUF mmap (3-6 sn)
2. Embedding ONNX yükle (1-2 sn)
3. Warm-up inference "Test" (5-10 sn)
4. Hazır flag set

Kullanıcı UI'a girdiğinde ilk istek <2 sn (sadece prompt + retrieve + generate).

### 5.4 Concurrency

LLamaSharp `LLamaContext` thread-safe değil. MVP için **tek-worker queue** (`Channel<Request>` + single consumer). Throughput yetmezse paralel context (RAM 1.5x) veya GPU sunucu v2.

---

## 6. İlişkili kararlar

- **Aşağı yönlü etki:** [Plan 34.1](../../plans/34.1-sop-rag-advisor.md) (bu ADR'in implementasyon planı)
- **Aşağı yönlü uyum:** [Plan 27 Faz E](../../plans/27-documents-vnext.md) — Documents RAG (aynı stack reuse)
- **AI altyapı parent:** [Plan 16.5](../../plans/16.5-mosaik-core-shared-kit.md) — Mosaik.Core.AI
- **Üst karar:** [ADR-002](002-modular-monolith.md) — modüler monolit (SOP modülü Mosaik.Core'a inject)
- **KVKK:** [Plan 40](../../plans/40-kvkk-process-backbone.md) — AI advisor envanteri (DataElement: prosedür içeriği, soru-cevap)
- **Görev entegrasyon:** [Plan 29](../../plans/29-talep-modulu.md) — "Cevap yok → görev oluştur"

---

## 7. Notlar

### Lisans tarama özet (KVKK envanter girişi için)

| Komponent | Lisans | Kurumsal kullanım | Attribution gerekli |
|---|---|---|---|
| Qwen 2.5 3B | Apache 2.0 | Sınırsız | Hayır |
| LLamaSharp | MIT | Sınırsız | Hayır (NOTICE dosyası tutulur) |
| Microsoft.ML.OnnxRuntime | MIT | Sınırsız | Hayır |
| multilingual-e5-base | MIT | Sınırsız | Hayır |
| Microsoft.Extensions.AI | MIT | Sınırsız | Hayır |
| Unsloth | Apache 2.0 | Sınırsız (Python pipeline, prod'da yok) | Hayır |

Tüm bileşenler **KVKK envanteri** açısından temiz: prosedür içeriği yurt dışı aktarımı yok (default config). Cloud fallback ON ise (admin toggle) ayrı işleme süreci VERBİS'e bildirilir.

### Karşılaştırma araştırma raporu

Detaylı agent araştırma çıktısı (Hugging Face indirme sayısı, son commit tarihi, MMLU skorları, Türkçe benchmark) [Plan 34.1 §10](../../plans/34.1-sop-rag-advisor.md#10-kararlar-varsayılan-kabul--kullanıcı-revize-edebilir) ve memory'de (`project_sop_rag_advisor_research.md`) bulunur.
