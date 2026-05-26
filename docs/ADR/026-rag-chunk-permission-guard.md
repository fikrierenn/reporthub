# ADR-026: RAG Chunk-Level Permission Guard

**Tarih:** 2026-05-26  
**Durum:** Onaylandı  
**Plan:** Plan 44  

---

## Karar

SOP RAG sisteminde chunk retrieval için 2 katmanlı defense-in-depth permission modeli.

---

## Bağlam

Plan 34.1 ile canlıya alınan SOP AI Danışman yalnızca `FirmaId` bazlı izolasyon yapıyordu. Türk hukuku (6698 KVKK madde 12) ve kurumsal güvenlik gereklilikleri, hassas prosedürlerin (İK, Finans, Kısıtlı) tüm firma kullanıcılarına açık olmadığını gerektirir. Örnek senaryo: depo personelinin LLM context'inde İK disiplin prosedürü görmesi.

---

## Karar

### Katman 1 — SQL Filtresi

`SopChunks.SecurityLevel <= userClearance` WHERE koşulu. Index'li sütun, DB tarafında hızlı eleme.

```sql
WHERE c.SecurityLevel <= @userClearance  -- tinyint, idx kullanır
```

### Katman 2 — In-Memory Policy

`IRagAccessPolicy.IsAccessible()` — 4 sıralı kontrol:
1. `SecurityLevel > clearance` → false (Layer 1 redundancy + in-memory doğrulama)
2. `AllowedRoleIds != null` → kullanıcı rollerinden en az biri listede değilse false
3. `AllowedDepartmentIds != null` → kullanıcının departman ID'lerinden en az biri listede değilse false
4. `AllowedUserIds != null` → kullanıcının ID'si listede değilse false
5. → true

NULL whitelist = kısıtlama yok.

### SecurityClearance Hiyerarşisi

| Seviye | Kime | Kod |
|---|---|---|
| 0 | Public | `SopChunk.Public` |
| 1 | Internal (default) | `SopChunk.Internal` |
| 2 | Confidential | `SopChunk.Confidential` |
| 3 | Restricted | `SopChunk.Restricted` |

Rol-türetme: `admin`→3, `hr`/`finans`/`manager`→2, `guest`→0, diğer→1. `Users.SecurityClearance` nullable override (Faz 3+).

### Eventual Consistency Senkronizasyonu

`SopDocument`'taki permission kolonları değiştiğinde `ChunkPermissionSyncJob` (Hangfire) tüm chunk'lara SQL bulk UPDATE ile ~1-2 sn içinde yansıtır.

```
SopController.Edit POST
  → _sop.UpdateAsync() (SopDocument permission kolları güncellenir)
  → BackgroundJob.Enqueue<ChunkPermissionSyncJob>(j => j.SyncAsync(id, "sop"))
  → SopChunks bulk UPDATE (SET SecurityLevel=sd.SecurityLevel, ...)
```

### RagUserContext

Controller katmanında claim'lerden inşa edilir, servis imzasına taşınır. DepartmentIds şu an boş (claim yok, Faz 3'te eklenecek).

---

## Alternatifler

### A — Prompt-level kısıtlama (Reddedildi)
Sistem promptuna "bu rolde olmayan kullanıcıya söyleme" eklemek. Jailbreak ile atlatılır; güvenlik guarantee yok.

### B — Sadece SQL filtresi (Reddedildi)
Layer 1 yeterli görünür ama in-process bypass riski (future code bug). Defense-in-depth daha güçlü.

### C — Vector store native ACL (Reddedildi)
pgvector, Qdrant, Milvus'ta yerleşik ACL. Şu an in-process cosine retrieval kullanıyoruz (ADR-022). Migration maliyeti > fayda. Vektör DB'ye geçilirse bu ADR revize edilir.

---

## Sonuçlar

- `SopChunks`: 4 yeni kolon (`SecurityLevel`, `AllowedRoleIds`, `AllowedDepartmentIds`, `AllowedUserIds`)
- `SopDocuments`: 4 kaynak kolon (ChunkPermissionSyncJob bu kolonları okur)
- `Users`: `SecurityClearance` nullable byte (override)
- `Mosaik.Core/AI/Rag/`: `RagUserContext`, `IRagAccessPolicy`, `DefaultRagAccessPolicy`
- `Mosaik.Modules.SOP/Services/`: `ChunkPermissionSyncJob`, `SopChunkRetriever` güncellendi
- Admin UI: SOP Edit "Erişim Kapsamı" section
- Testler: 5 yeni senaryo (`SopChunkRetrieverTests`)

---

## İlişkili

- ADR-022 — SOP AI Advisor stack (in-process LLM + embedding)
- Plan 44 — RAG Chunk-Level Permission Guard
- Plan 34.1 — SOP RAG Advisor
