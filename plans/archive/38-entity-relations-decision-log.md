# Plan 38 — EntityRelations + DecisionLog Core Tablolar

**Tarih:** 2026-05-20
**Yazan:** Fikri / Claude
**Durum:** `Onaylandı` (2026-05-21)

---

## 1. Problem

VISION §7 "Operational Intelligence Platform" kuzey yıldızının iki temel yapı taşı:

1. **EntityRelations** — modüller arası ilişki ağı (Living Org Map). Şu an her FK ayrı tabloda (`UserDepartments`, `ContractVendor`, `ObligationContract`...). Cross-modül "X'e bağlı tüm Y'ler" sorgusu zor.
2. **DecisionLog** — kim, neden, ne karar verdi (Decision Memory). AuditLog **ne** olduğunu kayıt eder, **neden + alternatifler + sonuç** yok.

Bu iki tablo yokken vizyon hayal. **Eski FK'lar refactor edilmez** — sadece yeni iş yazılırken (Plan 36 Workflow approve/reject, Plan 34 SOP, Plan 35 Comment) bu tablolara da kayıt düşer. Üst yapı kendiliğinden çıkar.

---

## 2. Scope

### Kapsam dahili
- 2 yeni tablo + EF entity + servis interface
- `IEntityRelationService.Add/Get/Remove`
- `IDecisionLogService.LogAsync`
- Plan 36 W-13 (Approve/Reject) DecisionLog yazıcı
- Plan 38 sonrası yeni modüller (Plan 34 SOP, Plan 35 Comment) sözleşmeye uyar

### Kapsam dışı
- Eski FK tabloları migrate etme (UserDepartments → EntityRelations) — refactor yok
- Cytoscape.js graph visualization — VISION §7, ileride ayrı plan
- N-hop recursive CTE query'leri — şimdilik sadece 1-hop direct lookup
- RAG embedding'i DecisionLog üzerinde — ileride AI COO ile

### Etkilenen dosyalar
- `Mosaik/Database/64_EntityRelationsDecisionLog.sql` — yeni migration
- `Mosaik/Models/EntityRelation.cs` — yeni entity
- `Mosaik/Models/DecisionLog.cs` — yeni entity
- `Mosaik/Models/MosaikContext.cs` — DbSet kayıt
- `Mosaik.Core/Intelligence/IEntityRelationService.cs` — interface
- `Mosaik.Core/Intelligence/IDecisionLogService.cs` — interface
- `Mosaik/Services/Intelligence/EntityRelationService.cs` — impl
- `Mosaik/Services/Intelligence/DecisionLogService.cs` — impl
- `Mosaik/Program.cs` — DI kayıt

**Tahmini boyut:** ~8-10 dosya / 400-500 satır.

---

## 3. Alternatifler

### A: Tek tablo, polymorphic (SEÇİLEN)
`EntityRelations { SourceType, SourceId, RelationType, TargetType, TargetId, Weight, ValidFrom, ValidTo, SourceSystem }`. FK yok — type+id pair "soft reference". Plan 35 Comment'in `Comments { EntityType, EntityId }` pattern'ine eşdeğer.

**Sebep:** Cross-modül ilişki için tek tablo gerek. Type-safe FK her ilişki için ayrı tablo demek = patlama. Soft reference güvenliği `IEntityRelationService` katmanında — validation orada.

### B: Her ilişki için strongly-typed FK tablosu
**Reddetme sebebi:** `UserDepartments`, `UserContracts`, `UserSOPs`, `UserComments`... patlama. 10 modül × 5 ilişki = 50 tablo. Anti-pattern.

### C: Neo4j sidecar
**Reddetme sebebi:** ADR-013 (multi-DB topology) SQL Server only der. 200 kişilik şirkette graph DB overkill. Recursive CTE 3-hop yeterli.

---

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Polymorphic FK → orphan row | Orta | Orta | `IEntityRelationService.Add` source/target type whitelist kontrolü |
| `EntityType` string enum'a karşı stringly-typed | Düşük | Yüksek | `EntityType` static const class (`EntityType.User`, `EntityType.Contract`) |
| `RelationType` çeşidi patlar | Düşük | Orta | İlk faz: `manages | owns | signed | assigned_to | references` — 5 tipi başla, gerekirse genişlet |
| Eski FK'lara dokunmayız ama duplicate ilişki kayıt riski | Düşük | Düşük | `IEntityRelationService` her zaman idempotent: aynı (source,target,type) varsa update timestamp |
| DecisionLog büyük JSON Payload — sorgu yavaş | Düşük | Düşük | `AlternativesJson` NVARCHAR(MAX), index'lenmiyor; sadece DisplayedTo user için lazy load |

---

## 5. Done Criteria

- [ ] Migration 64 yeşil — 2 tablo + index
- [ ] EF entity'ler `dotnet build` yeşil
- [ ] `IEntityRelationService.AddAsync` + `GetBySourceAsync` + `GetByTargetAsync` çalışıyor (5 unit test)
- [ ] `IDecisionLogService.LogAsync` + `GetByEntityAsync` çalışıyor (3 unit test)
- [ ] Plan 36 W-13 (Approve/Reject) DecisionLog'a yazıyor (entegrasyon noktası)
- [ ] `EntityType` static const class enum'lardan tip güvenli erişim
- [ ] `RelationType` static const class (5 tipi başlangıç)
- [ ] dotnet test yeşil
- [ ] İnline style yok (CRUD UI bu planda yok zaten)

---

## 6. Rollback Planı

- Migration 64 drop: 2 tablo bağımsız, FK yok (polymorphic) — `DROP TABLE EntityRelations; DROP TABLE DecisionLogs;`
- Servis interface kullanılmıyorsa yeni modüller fallback null-guard ile sessiz skip
- `git revert` + migration down: veri kaybı yok

---

## 7. Adımlar

### Faz A — Core Tablo + Servis (4-6h) ✅ TAMAMLANDI 2026-05-21

1. [x] **E-01** ✅ `Mosaik.Core/Intelligence/IEntityRelationService` + `IDecisionLogService` interface'leri + DTO record'ları
2. [x] **E-02** ✅ `EntityType.cs` (10 tip) + `RelationType.cs` (8 tip) static const + `IsValid`
3. [x] **E-03** ✅ Migration 64 — `EntityRelations` + `DecisionLogs` tabloları + 4 index, DB'ye uygulandı
4. [x] **E-04** ✅ `Mosaik/Models/Intelligence/EntityRelation.cs` + `DecisionLog.cs` EF entity + `MosaikContext` DbSet + `OnModelCreating` konfig
5. [x] **E-05** ✅ `EntityRelationService` impl — idempotent Add (mevcut tuple → update), Remove, GetBySource/Target
6. [x] **E-06** ✅ `DecisionLogService` impl — LogAsync + GetByEntityAsync (MadeAt DESC)
7. [x] **E-07** ✅ `Program.cs` DI kayıt — `AddScoped<IEntityRelationService, ...>` + `IDecisionLogService`
8. [x] **E-08** ✅ Unit test — 7 EntityRelation + 4 DecisionLog = 11 test, hepsi yeşil (full regression: 360/360)

### Faz B — Plan 36 Entegrasyonu (1-2h)

9. [ ] **E-09** Plan 36 W-13 (Approve/Reject action) DecisionLog yazıcı bağlantısı:
   ```csharp
   await _decisionLog.LogAsync(new DecisionLogEntry {
       FirmaId = firmaId,
       Title = $"Workflow {step.Name} {(approved ? "approved" : "rejected")}",
       Rationale = comment,
       MadeBy = userId,
       RelatedEntityType = EntityType.WorkflowInstance,
       RelatedEntityId = instanceId
   });
   ```

**Toplam tahmin:** 5-8 saat (1 gün).

---

## 8. Şema (referans)

```sql
CREATE TABLE EntityRelations (
    Id INT IDENTITY PRIMARY KEY,
    FirmaId INT NOT NULL,
    SourceType NVARCHAR(50) NOT NULL,
    SourceId INT NOT NULL,
    RelationType NVARCHAR(50) NOT NULL,
    TargetType NVARCHAR(50) NOT NULL,
    TargetId INT NOT NULL,
    Weight DECIMAL(5,2) NULL,
    ValidFrom DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2 NULL,
    SourceSystem NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy INT NULL
);
CREATE INDEX IX_EntityRelations_Source ON EntityRelations(SourceType, SourceId);
CREATE INDEX IX_EntityRelations_Target ON EntityRelations(TargetType, TargetId);
CREATE INDEX IX_EntityRelations_Firma ON EntityRelations(FirmaId);

CREATE TABLE DecisionLogs (
    Id INT IDENTITY PRIMARY KEY,
    FirmaId INT NOT NULL,
    Title NVARCHAR(300) NOT NULL,
    Rationale NVARCHAR(MAX) NULL,
    MadeBy INT NOT NULL,
    MadeAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    AlternativesJson NVARCHAR(MAX) NULL,
    ExpectedOutcome NVARCHAR(MAX) NULL,
    ActualOutcome NVARCHAR(MAX) NULL,
    KpiImpactJson NVARCHAR(MAX) NULL,
    RelatedEntityType NVARCHAR(50) NULL,
    RelatedEntityId INT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active'
);
CREATE INDEX IX_DecisionLogs_Entity ON DecisionLogs(RelatedEntityType, RelatedEntityId);
CREATE INDEX IX_DecisionLogs_MadeBy ON DecisionLogs(MadeBy, MadeAt DESC);
```

---

## 8.1 Mevcut yapıya uyumlandırma — C hibrit (2026-05-20 karar, 2026-05-21 netleştirildi)

**Karar:** Eski FK'lar (UserDepartments, ContractVendor, ObligationContract, ...) **yerinde kalır** — global refactor yok. AMA: **dokunduğumuz yeni iş paralel yazar.** Yeni feature yazarken o feature'ın yazdığı FK aynı zamanda `EntityRelations`'a da kayıt düşer.

**Üç katmanlı kural:**

1. **Eski okuma kodu** → eski FK'ya bakmaya devam eder, dokunulmaz.
2. **Yeni okuma kodu** → `IEntityRelationService.GetBySourceAsync` aggregator (iki kaynağı birleştirir: EntityRelations + eski FK adapter).
3. **Yazma (yeni veya değiştirilen feature)** → her zaman çift yazar:
   - Eski FK'ya (mevcut UI ve okuma kodu kırılmasın)
   - `EntityRelations`'a (yeni okuma + analitik + Living Org Map için)

**Faz B (sonraki sprint — Plan 38 kapsamı dışı, taslak not):**
- `IEntityRelationService.GetBySourceAsync` aggregator iki kaynak:
  1. `EntityRelations` tablosundan polymorphic kayıtlar
  2. Eski FK tablolarından adapter ile çevrilmiş kayıtlar (örn: `UserDepartments` → `(User, manages|member_of, Department)`)
- 3 ana ilişki ile başla: User↔Department, Contract↔Vendor, Obligation↔Contract
- Adapter `IEntityRelationSource` interface — her FK tablosu için küçük mapping metod

**Çift-yazma pattern (Plan 36/34/35 yazıcısı):**
```csharp
// Örnek: Plan 36 W-13 Approve
_db.WorkflowInstanceLogs.Add(log);        // mevcut FK / log tablosu
await _entityRelations.AddAsync(new {     // yeni paralel kayıt
    SourceType = EntityType.User, SourceId = userId,
    RelationType = RelationType.Approved,
    TargetType = EntityType.WorkflowInstance, TargetId = instanceId
});
await _decisionLog.LogAsync(...);
```

Mevcut UI eskisini okur, kırılmaz. Yeni analitik / Living Org Map / Friction Heatmap yenisini okur. Aggregator ikisini birleştirir.

**İleride (Plan 38 kapsamı dışı, opsiyonel):**
- Hangfire sync job (15-30dk): eski FK → EntityRelations idempotent upsert (denormalize read model). Yük artarsa değerlendir; şu an aggregator yeterli.

**DecisionLog ↔ AuditLog link:**
- `DecisionLog.RelatedAuditId INT NULL` kolonu eklenir (Migration 64'te baştan)
- Yeni karar action'ı (Plan 36 W-13 Approve/Reject) hem AuditLog hem DecisionLog'a yazar; DecisionLog `RelatedAuditId` üzerinden audit kaydına link
- Eski AuditLog kayıtları yerinde kalır, backfill yok (rationale bilgisi geriye gidip yazılamaz)

## 9. İlişkili

- **VISION §7** — Living Org Map + Decision Memory Engine (kuzey yıldızı)
- **Plan 36** — W-13 ilk DecisionLog yazıcısı
- **Plan 34** — SOP onay → DecisionLog yazıcı (Faz D)
- **Plan 35** — Comment/Mention `EntityType+EntityId` aynı polymorphic pattern
- **ADR-013** — SQL Server only (Neo4j yok)
- **ADR-002** — Modular monolith, `Mosaik.Core/Intelligence/` cross-modül abstraction
- **TODO ID'leri:** E-01..E-09

---

## 10. Onay

> Kullanıcı onay verene kadar implement edilmez.

- [x] Plan kullanıcıya gösterildi
- [x] Geri bildirim alındı (çift-yazma kuralı 8.1'e eklendi)
- [x] Onay alındı (2026-05-21)
