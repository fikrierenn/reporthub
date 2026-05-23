# SOP AI Advisor — Model Dosyaları

Plan 34.1 + ADR-022 — bu klasör SOP RAG advisor için gerekli LLM + embedding + LoRA dosyalarını barındırır.

**Önemli:** Model binary'leri (`.gguf` / `.onnx` / `.bin` / `.safetensors`) **`.gitignore`** ile dışlanmıştır. Kullanıcılar manuel indirir (BKM kurumsal proxy + GitHub LFS limiti).

## Klasör yapısı

```
App_Data/models/
├── README.md                            # bu dosya (tracked)
├── llm/
│   └── qwen25-3b-instruct-q4_k_m.gguf   # ~2.0 GB  ❌ gitignored
├── embed/
│   └── e5-base.onnx                # ~1.1 GB FP32 (intfloat resmi ONNX, INT8 yok) ❌ gitignored
├── lora/
│   └── sop-vYYYY-MM.gguf                # ~50 MB   ❌ gitignored (fine-tune output)
└── tokenizer/
    ├── tokenizer.json                   # e5 tokenizer config
    ├── tokenizer_config.json
    └── special_tokens_map.json
```

## İndirme talimatı (manuel — BKM laptop)

### 1. LLM — Qwen 2.5 3B Instruct Q4_K_M

```powershell
# Hugging Face web UI'dan indir (BKM proxy izin verirse):
# https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf

# Veya huggingface-cli (Python pip install huggingface_hub gerekli):
huggingface-cli download Qwen/Qwen2.5-3B-Instruct-GGUF qwen2.5-3b-instruct-q4_k_m.gguf --local-dir Mosaik/App_Data/models/llm

# Dosya adını ADR-022 referansına çevir:
mv qwen2.5-3b-instruct-q4_k_m.gguf qwen25-3b-instruct-q4_k_m.gguf
```

**Lisans:** Apache 2.0 — kurumsal kullanım izin, attribution gerekmez.

### 2. Embedding — multilingual-e5-base ONNX INT8

```powershell
# ONNX export: optimum-cli (Python pip install optimum[onnxruntime])
optimum-cli export onnx \
    --model intfloat/multilingual-e5-base \
    --task feature-extraction \
    --quantization int8 \
    Mosaik/App_Data/models/embed/

# Çıktı: model.onnx + tokenizer.json + tokenizer_config.json + special_tokens_map.json
# model.onnx → embed/e5-base.onnx olarak rename
mv Mosaik/App_Data/models/embed/model.onnx Mosaik/App_Data/models/embed/e5-base.onnx

# Tokenizer dosyalarını tokenizer/ klasörüne taşı:
mv Mosaik/App_Data/models/embed/tokenizer*.json Mosaik/App_Data/models/tokenizer/
mv Mosaik/App_Data/models/embed/special_tokens_map.json Mosaik/App_Data/models/tokenizer/
```

**Alternatif (önerilen — hazır ONNX):**
```
https://huggingface.co/intfloat/multilingual-e5-base/tree/main
```
Hugging Face üzerinde resmi ONNX variant yok — `optimum-cli` export şart.

**Lisans:** MIT.

### 3. LoRA adapter (fine-tune sonrası, opsiyonel)

`scripts/ai-train/sop-finetune.ipynb` Colab notebook'u çalıştır → LoRA adapter GGUF olarak export et → `Mosaik/App_Data/models/lora/` altına kopyala.

Dosya adı versionlı: `sop-v2026-08.gguf` (yıl-ay). Mosaik runtime'da `--lora-scaled` flag ile uygular (merge ETME — llama.cpp issue #7062).

## BKM proxy sorunu

`huggingface-cli` veya `optimum-cli` BKM proxy arkasında çalışmazsa:

1. Ev / başka bir bilgisayardan indir
2. USB / network share ile BKM laptop'una kopyala
3. `App_Data/models/` altındaki ilgili klasöre yerleştir

İleride **MSI installer** ile pre-bundle yapılacak (Plan 34.1 v2).

## Smoke test

Model dosyaları yerleştirildikten sonra:

```bash
cd D:/Dev/reporthub
dotnet test --filter "FullyQualifiedName~LlamaSharpSmokeTest"
```

Bu test `App_Data/models/llm/qwen25-3b-instruct-q4_k_m.gguf` mevcut değilse **skip** olur. Mevcutsa "Merhaba" promptuna Türkçe cevap üretir, console'a yazdırır.

## Memory footprint

| Bileşen | RAM |
|---|---|
| Qwen 2.5 3B Q4 (ctx=2048) | ~2.4 GB |
| KV cache (ctx=2048, batch=1) | ~200 MB |
| e5-base ONNX INT8 | ~150 MB |
| LLamaSharp + ORT overhead | ~150 MB |
| **Toplam advisor** | **~2.9 GB** |
| Mosaik base (Kestrel + EF) | ~400-600 MB |
| **Pratik total** | **~3.5 GB** |

BKM 8GB+ kurumsal laptop standart — rahat çalışır.

## İlgili

- [Plan 34.1](../../../plans/34.1-sop-rag-advisor.md) — implementasyon planı
- [ADR-022](../../../docs/ADR/022-sop-ai-advisor-stack.md) — stack karar
- [scripts/ai-train/README.md](../../../scripts/ai-train/README.md) — fine-tune workflow (yazılacak)
