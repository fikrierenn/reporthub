# BkmArgus → Mosaik Adaptasyon Kılavuzu

**Kaynak:** `D:/Dev/BkmArgus`  
**Tarih:** 2026-05-25  
**Amaç:** BkmArgus'tan Mosaik vNext'e port edilecek bileşenlerin referans kılavuzu.

---

## 1. LM Rules — Heuristic-First AI Gate

**Kaynak:** `BkmArgus/src/BkmArgus.AiWorker/LmRules.cs` (66 satır)  
**Pipeline:** `BkmArgus/src/BkmArgus.AiWorker/AiWorkerService.cs` (936 satır)

### Nasıl çalışıyor

`LmRules.Decide(RiskSummaryRow)` — LLM'e hiç dokunmadan karar üretir.

- Girdi: stok risk flag'lerinin boolean kombinasyonu
- Çıktı: `RuleDecision { RootCauseClass, EvidencePlan, LlmRequired, PriorityScore }`
- `LlmRequired = true` yalnızca 3 durumda: skor 90+, data quality flag, count adjustment flag
- Diğer tüm vakalar kural motoruyla kapanır → LLM maliyeti %60-70 azalır

Pipeline sırası:
```
NEW/BEKLEMEDE → LmRules.Decide() → ai.RuleResults MERGE →
  LlmRequired ? "LLM_QUEUED" : "LM_DONE"
```
LLM sırası ayrı `ProcessLlmQueueAsync` loop'unda işlenir.

### Mosaik entegrasyon hedefi

**Plan 47 Auto-Tuning Process Advisor Faz 1:**

```csharp
// Mosaik.Modules.DOF/Services/DofRuleEngine.cs
public class DofRuleEngine
{
    public DofRuleDecision Decide(DofRiskContext ctx)
    {
        // Risk seviyesi 1-2 → sadece şablon aksiyon önerisi, LLM gerek yok
        if (ctx.RiskLevel <= 2 && !ctx.HasDataQualityFlag)
            return new DofRuleDecision { LlmRequired = false, TemplateSuggestion = ... };

        return new DofRuleDecision { LlmRequired = true };
    }
}
```

**Mosaik.Core.AI'ya eklenecek abstraction:**
```csharp
public interface IHeuristicGate<TContext, TDecision>
{
    TDecision Decide(TContext ctx);
}
```

**Bağlı plan:** Plan 47 + Mosaik.Core.AI (D-02)

---

## 2. DOF (Düzeltici Önleyici Faaliyet) Şeması

**Kaynak:** `BkmArgus/sql/38_dof_state_machine.sql`  
**Eski şema:** `BkmArgus/src/BkmArgus.Installer/sql/02_tables.sql:311-406` (Türkçe alan — port etme)

### Yeni şema (port edilecek)

```sql
-- Port edilecek tablo yapısı
dof.Findings          -- Ana kayıt (FindingSignature: DOF-00001 pattern)
dof.StatusHistory     -- Durum geçiş log'u (from/to + reason)
dof.StatusRules       -- DB-driven geçiş kuralları (RequiredRole)
dof.Comments          -- Tartışma thread
```

`StatusRules` özellikle kıymetli: Workflow Designer olmadan DB-driven state machine.  
`dof.sp_Finding_Transition` — C# rol geçiyor, SP DB'de kontrol ediyor, başarısız ise RAISERROR.

Kanıt tablosu:
```
KanitTuru: SQL / PNG / PDF / CSV / NOT
KanitYolu: nullable dosya yolu
KanitMetni: nullable küçük metin kanıt
```

### Mosaik entegrasyon hedefi

**Plan 42 ProcessExecution Faz 3 (DOF Aspect):**

- `dof.Findings` → ProcessInstance Aspect = "DOF" ile polymorphic *veya* ayrı `dof` schema  
- `dof.StatusRules` → Mosaik `WorkflowTransition` tablosunun hafif alternatifi  
- `FindingSignature` pattern → Mosaik genel `ReferenceNumber` generator (DOF-YYMMDD-NNNN)
- Kanıt tablosu → Plan 42 Document aspect'i ile örtüşüyor, birleştir

---

## 3. SqlDb — Slow-Query Log Pattern

**Kaynak:** `BkmArgus/src/BkmArgus.Web/Data/SqlDb.cs` (101 satır)

### Pattern özeti

```csharp
// BkmArgus pattern — Mosaik'e adapte edilecek kısım
var sw = Stopwatch.StartNew();
var result = await conn.QueryAsync<T>(spName, param, commandType: CommandType.StoredProcedure);
sw.Stop();
if (sw.ElapsedMilliseconds > 500)
    _logger.LogWarning("[SLOW SP] {SpName} {ElapsedMs}ms", spName, sw.ElapsedMilliseconds);
```

### Mosaik entegrasyon hedefi

**Mosaik/Services/StoredProcedureExecutor.cs:**  
Mosaik Dapper kullanmayacak (ADR kararı), ama slow-query log eklenebilir.

```csharp
// StoredProcedureExecutor.ExecuteMultipleAsync içine eklenecek
if (elapsed > TimeSpan.FromMilliseconds(500))
    _logger.LogWarning("[SLOW_SP] {ProcName} {ElapsedMs}ms FirmaId={FirmaId}",
        procName, elapsed.TotalMilliseconds, firmaId);
```

**Bağlı görev:** D-02 AI altyapı iyileştirme → `StoredProcedureExecutor` hardening

---

## 4. Queue Lock Pattern (Distributed Worker)

**Kaynak:** `BkmArgus/src/BkmArgus.AiWorker/AiWorkerService.cs`  
**SQL:** `WITH (UPDLOCK, READPAST, ROWLOCK)` CTE'si

### Pattern

```sql
-- Birden fazla worker instance'ı çalışırken double-processing önler
WITH q AS (
    SELECT TOP(1) RequestId, ...
    FROM ai.AnalysisQueue WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE Status = 'NEW'
    ORDER BY CreatedAt
)
UPDATE q SET Status = 'PROCESSING', ...
OUTPUT INSERTED.*
```

### Mosaik entegrasyon hedefi

Mosaik Hangfire kullanıyor, bu pattern gerekmez — ama `SopEnrichmentService` Hangfire job'larında double-processing riski var. Hangfire distributed lock yetersizse bu SP pattern geri açılabilir.

---

## 5. SkillRegistry Pattern (Bonus)

**Kaynak:** `BkmArgus/src/BkmArgus.AiWorker/Skills/SkillRegistry.cs` + `SkillDefinition.cs`

Her skill: `SystemPromptTemplate` + `UserPromptTemplate` + `RequiredContext[]` + `Temperature` + `MaxTokens`.  
`SkillExecutor.ExecuteAsync(skillId, variables, token)` — template interpolasyon + LLM call.

**Mosaik durumu:** Plan 34.1 `SopEnrichmentService` bu pattern'in domain-specific versiyonu. Ayrıca `ISkillCatalog` + `MarkdownSkillCatalog` zaten mevcut (`App_Data/ai-skills/*.md`). BkmArgus'tan alma gerekmez — ama `SkillDefinition`'daki `RequiredContext[]` liste yaklaşımı Mosaik skill'lerine eklenebilir (hangi skill hangi context'e ihtiyaç duyuyor?).

---

## Öncelik Sırası

| # | Bileşen | Hedef Plan | Effort | Öncelik |
|---|---|---|---|---|
| 1 | DOF state machine şeması | Plan 42 Faz 3 | 4-6h | YÜKSEK |
| 2 | LM Rules heuristic gate | Plan 47 Faz 1 | 3-4h | YÜKSEK |
| 3 | Slow-query log | D-02 | 1h | ORTA |
| 4 | Queue lock pattern | Hangfire review | 0h (Hangfire yeterli) | DÜŞÜK |
| 5 | SkillRegistry enrich | Plan 34.1 follow-up | 2h | DÜŞÜK |

---

## Karar: vw_CalendarUnified

BkmArgus'ta bu view **yok**. Mosaik'in `vw_CalendarUnified` (SOP deadline + Obligation + Circular + Holiday birleşik takvim) ayrıca tasarlanacak. BkmArgus bağımlılık değil.
