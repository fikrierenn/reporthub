# Plan 44 — RAG Chunk-Level Semantic Permission Guard

**Durum:** ⏳ TASLAK 2026-05-25 — onay bekliyor
**Tier:** 3 (schema + security + cross-modül + KVKK)
**Tetik:** Kullanıcı strategic input 2026-05-25 — "Mosaik dışarıdan 4 katman"
**Effort:** 12-18h (4 faz, ~1 hafta)
**Aciliyet:** 🔴 **CRITICAL HOT-FIX** — SOP modülü canlı, Documents Plan 27 RAG bağlanmadan önce kapatılmalı

---

## 1. Problem

SOP modülü Plan 34.1 ile cross-SOP RAG advisor canlı. `SopChunkRetriever` şu an **yalnızca FirmaId** scope filtreliyor:

```csharp
// SopChunkRetriever.cs:~80
.Where(c => c.FirmaId == userScope.FirmaId)
.OrderBy(c => Sql.Distance(c.Embedding, queryEmbed))
.Take(topK)
```

**Eksik filtre:**
- `AllowedRoleIds` — role-based access (admin/finans/hr/depo)
- `AllowedDepartmentIds` — department scope
- `MinSecurityLevel` — gizlilik seviyesi (public/internal/confidential/restricted)
- `AllowedUserIds` — explicit user whitelist (yönetim kurulu kararları için)

**Senaryo (Gerçek BKM riski):**

> Genel Müdür özel "2026 Maaş Politikası" PDF'i Documents modülüne yükledi. Plan 27 Faz E ile Documents RAG'a bağlandı. Depocu Ahmet kendi prosedürünü sorarken "şirket maaş ortalaması nedir" gibi yaratıcı bir soru sorar. RAG retrieval cosine similarity'ye göre Maaş Politikası chunk'ını yakalar → Qwen bağlama alır → cevap içinde maaş bilgisi sızar.

**Etki:**
- **KVKK 6698 m.12** — kişisel veri güvenlik ihlali 72h içinde Kurum'a bildirim zorunlu, idari ceza ~50K-1M TL
- **Ticari sır** — yönetim kurulu kararı, M&A, müşteri sözleşmeleri sızıntısı
- **İş hukuku** — performans değerlendirme, disiplin kararları erişim ihlali

**FK / izin yapısı zaten var:**
- `User.RoleIds` (junction)
- `Departments` + `UserDepartments`
- `Firmas` (multi-tenant)
- `ModuleRoleAccess` (Plan 32 ✅)

Eksik olan: **chunk-level metadata + retriever-side WHERE clause**.

---

## 2. Scope

### Kapsam içi (Faz 1-4)
- `SopChunks` tablosu metadata genişletme (4 yeni kolon + index)
- `DocumentChunks` (Plan 27 Faz E için hazır) aynı pattern
- `ContractChunks` (gelecek RAG için reserve)
- `SopChunkRetriever` SQL filter: user claims → WHERE clause
- `IRagAccessPolicy` Core abstraction (modüller arası ortak filter logic)
- Admin UI: SOP edit'te "Erişim Kapsamı" bölümü (rol/dept/level seç)
- Audit log: chunk yetkisiz erişim denemesi
- Default policy: yeni SOP `Internal` level + tüm role'lar (geriye uyumluluk)

### Kapsam dışı
- LLM prompt-injection koruması (ayrı plan — Plan 27 Faz F adayı)
- Differential privacy / vektör pertürbasyonu (overkill, ayrı araştırma)
- Watermarking chunk'lar (ileride)
- Cross-tenant federated RAG (BKM tek-tenant)

---

## 3. Mimari

### 3.1 Schema (Migration 73)

```sql
ALTER TABLE dbo.SopChunks ADD
    SecurityLevel TINYINT NOT NULL DEFAULT 1,  -- 0 public, 1 internal, 2 confidential, 3 restricted
    AllowedRoleIds NVARCHAR(500) NULL,         -- CSV "admin,hr,finans" veya NULL=tüm role
    AllowedDepartmentIds NVARCHAR(500) NULL,   -- CSV department ID'leri
    AllowedUserIds NVARCHAR(500) NULL;         -- CSV user ID'leri (explicit whitelist)

CREATE INDEX IX_SopChunks_AccessScope
    ON dbo.SopChunks (FirmaId, SecurityLevel)
    INCLUDE (AllowedRoleIds, AllowedDepartmentIds);
```

**Neden CSV string, junction tablosu değil?**
- Chunk sayısı SOP başına ~10-50, doc başına ~100. 10K SOP × 30 chunk × N junction = milyonlarca satır
- Junction join × top-K cosine her sorguda → vector search performance ölür
- CSV `STRING_SPLIT` ile filter: index seek + small scan kabul edilir
- CSV regex validation FilterValue gibi (security-principles m.8)

### 3.2 Core Abstraction

```csharp
// Mosaik.Core/Ai/Rag/IRagAccessPolicy.cs
public interface IRagAccessPolicy
{
    // SQL WHERE clause fragment + parameters
    (string WhereClause, IReadOnlyList<SqlParameter> Params)
        BuildChunkFilter(UserClaims claims);
}

public sealed class DefaultRagAccessPolicy : IRagAccessPolicy
{
    public (string, IReadOnlyList<SqlParameter>) BuildChunkFilter(UserClaims c)
    {
        // 1. FirmaId scope (mevcut)
        // 2. SecurityLevel <= max(claims.SecurityClearance)
        // 3. AllowedRoleIds IS NULL OR EXISTS(STRING_SPLIT INTERSECT claims.RoleIds)
        // 4. AllowedDepartmentIds IS NULL OR EXISTS(... INTERSECT claims.DepartmentIds)
        // 5. AllowedUserIds IS NULL OR claims.UserId IN STRING_SPLIT(...)
        ...
    }
}
```

### 3.3 Retriever entegrasyonu

```csharp
// SopChunkRetriever.cs (after Plan 44)
public async Task<IReadOnlyList<SopChunk>> RetrieveAsync(
    float[] queryEmbed, UserClaims claims, int topK, double minScore)
{
    var (where, parms) = _policy.BuildChunkFilter(claims);
    var sql = $@"
        SELECT TOP (@topK) c.*, Sql.Distance(c.Embedding, @q) AS Dist
        FROM SopChunks c
        WHERE {where}
        ORDER BY Dist
    ";
    // Eleme LLM'e gitmeden, SQL seviyesinde
}
```

### 3.4 Admin UI

SopController.Edit + Document/Contract Edit'te yeni bölüm:
- "Erişim Seviyesi" radio: Genel / Şirket İçi / Gizli / Kısıtlı
- "İzinli Roller" multi-select (CSV)
- "İzinli Departmanlar" multi-select (CSV)
- "Açık Kullanıcı Listesi" autocomplete (explicit whitelist, restricted için)
- Tooltip: "Gizli → sadece izinli rol/dept; Kısıtlı → sadece açık liste"

### 3.5 Audit

```csharp
// SopRagAdvisorService.AskAsync sonunda
var blocked = retrievalCandidates.Where(c => !claims.CanRead(c)).Count();
if (blocked > 0) {
    await _audit.LogAsync("rag_chunk_blocked", $"user={uid}, query={hash}, blocked={blocked}");
}
```

---

## 4. Alternatifler (5 lens analizi)

### 🔴 Contrarian: Bu plan fatal flaw'u ne?

**False sense of security.** Permission guard chunk metadata'ya bağımlı. Eğer kullanıcı (uploader) yanlış metadata girdiyse (default = herkese açık), gizli içerik yine sızar. **Real-world default'lar genelde permissive.** İK "şu an kullanmasam da herkes okusun" diye public bırakırsa Plan 44 çalışmaz.

**Mitigation:** Default `Internal` (sadece authenticated kullanıcılar), `Public` seçimi explicit confirm + audit. AI Integrity Checker (Plan 40 Faz 5 KVKK skill) chunk içeriğini tarayıp "bu chunk maaş/kişi/finans içeriyor, Internal yeterli değil" uyarısı.

### 🔵 First Principles: Gerçek problem nedir?

Gerçek problem chunk-level filter değil. **Document-level access control zaten Mosaik'te var** (DocumentPermissions tablo, Plan 27 Faz B). Eğer Documents zaten user'a göstermiyorsa, RAG da retrieve etmemeli — kaynak doc-level filter chunk'a propagate edilmeli.

**Mitigation:** Plan 44 v2 yaklaşımı — chunk metadata yerine `JOIN DocumentPermissions ON ChunkId.DocumentId`. Tek source of truth, drift yok.

**Tradeoff:** JOIN performansı vector search üstünde. Chunk-level cache (denormalized) yine gerekli. CSV pattern = pragmatik kabul.

**Karar:** Hibrit — chunk-level CSV (perf) + JOIN doc-level fallback (consistency). Edit sırasında document permission değişirse chunk metadata invalidate edilir.

### 🟢 Expansionist: Daha büyük fırsat ne?

**Tüm RAG modülleri (SOP + Documents + Contracts + ileride FAQ + chat history) ortak `IRagAccessPolicy`.** Plan 44 sadece SOP'ı düzeltmek için yazılsa, Documents/Contracts ayrı reinvent eder.

**Mitigation:** Plan 44 Core abstraction'ı önce yazılır, SOP referans implement, Documents/Contracts inherit. Plan 27 Faz E + Contracts RAG zorunlu olarak `IRagAccessPolicy` kullanır.

### ⚪ Outsider: Yabancı biri ne garip bulur?

"Niye chunk'ta security level var ama vector embedding'i değil?" — embed işlemini chunk content'i şifrelemeden yapıyoruz. Restricted chunk'lar embedding olarak DB'de duruyor. **Embedding-level inversion attack** (vektörden orijinal text geri kazanma) teorik mümkün.

**Mitigation:** Şimdilik kabul (Mosaik prod DB encrypted at rest, sadece DBA erişebilir). İleride: restricted chunk'lar için `EmbeddingEncrypted` + on-the-fly decrypt + cache (perf cost). Plan 44 scope dışı.

### 🟡 Executor: Pazartesi sabahı ne?

1. Migration 73 yaz (idempotent ALTER TABLE + index)
2. `IRagAccessPolicy` interface + `DefaultRagAccessPolicy` Mosaik.Core
3. `UserClaims` value object (UserId + RoleIds + DepartmentIds + SecurityClearance)
4. SopChunkRetriever wire-up (1 method change)
5. Backfill: tüm mevcut SopChunks → SecurityLevel=1 (Internal)
6. Admin UI Edit "Erişim Kapsamı" bölümü
7. Test: 3-4 senaryo (admin/depo/hr — kim ne görür)

---

## 5. Riskler

| Risk | Olasılık | Etki | Mitigation |
|---|---|---|---|
| Default permissive metadata → false security | yüksek | yüksek | Default `Internal`; `Public` explicit confirm + audit |
| Vector search perf 10x düşer (WHERE clause) | orta | yüksek | INCLUDE index + STRING_SPLIT optimize + small chunk count |
| Document permission drift (chunk metadata stale) | orta | orta | Permission update → chunk metadata invalidate (Hangfire) |
| Embedding inversion attack | düşük | yüksek | Encrypted at rest yeterli; Plan 44 v2'de embedding şifreleme |
| Migration 73 backfill timeout (10K chunk × N firma) | düşük | orta | Batch UPDATE 1000 chunk/transaction, idempotent |

---

## 6. Done Criteria

- [ ] Migration 73 (SopChunks + DocumentChunks placeholder) idempotent, fresh DB + apply test
- [ ] `IRagAccessPolicy` Mosaik.Core'da, unit test 8+ (her filter combination)
- [ ] `SopChunkRetriever` user claims param, WHERE clause SQL generate
- [ ] Admin SOP Edit "Erişim Kapsamı" UI, valid CSV regex
- [ ] Audit: `rag_chunk_blocked` event log
- [ ] Backfill: tüm mevcut SopChunks → Internal default
- [ ] Test: 3 senaryo (admin/depo/hr) farklı chunk set retrieve
- [ ] CONTRADICTIONS C-RAG-1 kapanır
- [ ] `dotnet test` ≥ 530 (516 + ~15 yeni test)

---

## 7. Rollback

Migration 73 ALTER ADD COLUMN — DROP COLUMN ile geri alınabilir. Backfill data kaybı yok (yalnızca metadata). Retriever eski path SQL'e revert (git revert).

---

## 8. Adımlar / Fazlar

### Faz 1 — Core abstraction + Migration (4h)
- [ ] `IRagAccessPolicy` + `UserClaims` + `DefaultRagAccessPolicy`
- [ ] Migration 73 `73_AddRagChunkPermissionFields.sql`
- [ ] Unit test 8 (filter combination)

### Faz 2 — SOP entegrasyonu (4h)
- [ ] `SopChunk` entity property + EF config
- [ ] `SopChunkRetriever.RetrieveAsync(UserClaims)` refactor
- [ ] `SopRagAdvisorService.AskAsync` user claims build
- [ ] Backfill script (tüm chunk → Internal default)

### Faz 3 — Admin UI (3h)
- [ ] SopController.Edit "Erişim Kapsamı" bölümü
- [ ] SopFormViewModel ek property'ler
- [ ] CSV validation server-side + JS

### Faz 4 — Audit + test + doc (3h)
- [ ] Audit `rag_chunk_blocked` event
- [ ] Senaryo testleri (admin/depo/hr)
- [ ] ADR-026 yazımı (RAG access policy mimarisi)
- [ ] CONTRADICTIONS doc güncelle

---

## 9. Bağımlılıklar

- **Bu plan blocker:** Plan 27 Faz E (Documents RAG) — Plan 44 önce bitmeli
- **Bu plan blocker:** Contracts RAG (gelecek plan) — aynı şart
- **Bağımsız:** Plan 41 (Form Builder), Plan 36 (Workflow), Plan 42 (Process Runtime)
- **Reuse:** `IUserDataScope` (Plan 14 Faz B), `ModuleRoleAccess` (Plan 32)

---

## 10. Açık Sorular

1. **SecurityClearance kullanıcıda ayrı mı, role'den mi türetilir?** — Önerim: role-based, "admin"→3 (restricted), "manager"→2 (confidential), "user"→1 (internal). Override gerekirse User entity'ye `SecurityClearance` byte eklenir.
2. **AI Integrity Checker chunk content scan zorunlu mu?** — Plan 40 Faz 5 KVKK skill (Microsoft Presidio) hazır. SOP edit/save'de chunk content scan + suggest security level (öneri, kullanıcı override).
3. **Restricted chunk LLM'e gitmesin mi, yoksa LLM'e git ama prompt-side ayrı işle mi?** — Önerim: SQL-side eliminate, LLM hiç görmesin (defense-in-depth).
4. **Documents Permissions tablosu chunk metadata'ya cache nasıl?** — Document permission update → Hangfire `DocumentChunkPermissionSyncJob` chunk metadata güncelle.
