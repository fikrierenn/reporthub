---
paths:
  - "Mosaik/Database/**/*.sql"
  - "Mosaik/Models/DashboardConfig.cs"
  - "Mosaik/Services/DashboardConfigValidator.cs"
---

# Dashboard Config Seed Mimarisi

_Kapsam: `ReportCatalog.DashboardConfigJson` üreten seed/migration yazımı + mevcut config'leri koruma._

## TL;DR

Seed yazarken:
1. **Named contract binding** kullan (`result: "kpi"`, `"subeOzet"`) — `rs0/rs1` rsN pattern V2 builder default'udur, seed yolu DEĞİLDİR.
2. **Widget id YAZMA** — validator regex `^w_[a-z]+_[a-z0-9]{6,}$` strict, builder GUI üretir.
3. **`tableOptions.pageSize` ∈ {0,10,20,50,100}** — başka değer validator'dan geçmez.
4. **Mevcut config'i EZME** — `WHERE DashboardConfigJson IS NULL` koşulu zorunlu.
5. **Direkt `MCP UPDATE` yapacaksan da WHERE şart** — admin'in elle yaptığı + migration chain'inden geçmiş config kaybolur.

## Migration Zinciri (referans)

PDKS + Satış Pano `DashboardConfigJson` şu zincirden geçti — yeni seed yazmadan önce **bu zinciri tara**:

| # | Dosya | Ne yapar |
|---|---|---|
| 12 | `12_SeedPDKSDashboard.sql` | PDKS V1 config seed (`resultSet: int`, no IDs) |
| 14 | `14_SeedSatisDashboard.sql` | Satış V1 config seed (`resultSet: int`, no IDs) |
| 26 | `26_AddResultContract.sql` | `$.resultContract` named keys ekle (planFiili, kpi, subeOzet, ...) |
| 27 | `27_MigrateWidgetBinding.sql` | Her widget'ı `resultSet: 2` int → `result: "kpi"` named string'e migrate et + `resultSet` field'ı sil |

**V1 → V2 yolu:** seed (V1) → contract ekle (26) → binding migrate (27) → V2.

## Validator (DashboardConfigValidator.cs)

Validator çalışan path'lar:
- `PreviewDashboardV2` (admin builder preview)
- `DashboardValidate` (admin builder canlı validasyon)
- `ReportManagementService.ValidateDashboardConfigAsync` (admin form save)

**Çalışmayan path:** `/Reports/Run/{id}` runtime render — DB'deki config'i ne ise onu render eder, validator dokunmaz.

Sonuç: bozuk config DB'ye girerse render OK olur ama admin GUI'de açılan rapor save/preview'de hata verir.

### Strict regex (`DashboardConfigValidator.cs:17`)

```regex
^w_[a-z]+_[a-z0-9]{6,}$
```

Builder JS üretir (`builder-v2/builder.js:97`):
```javascript
'w_' + type + '_' + Math.random().toString(16).slice(2, 10)
```

Seed'de hardcoded ID yazma — pattern'e uydursan da insan-üretimi belli olur ve build/refactor sırasında çakışır.

### Page size whitelist

```csharp
KnownPageSizes = { 0, 10, 20, 50, 100 }
```

15, 30 gibi değerler hata üretir.

## Render Path Binding Çözümü (`DashboardConfig.ResolveResultSet`)

```
1. comp.Result -> ResultContract entry varsa entry.ResultSet
2. yoksa "rsN" regex int parse fallback (V2 builder default)
3. yoksa null -> placeholder render + audit
```

Seed için **birinci yolu** kullan (named contract). rsN fallback builder UX kolaylığıdır, kalıcı veri için stabil değildir (RS sırası SP refactor'da kayar, named binding semantik bağlar).

## Doğru Seed Şablonu

```sql
-- Mevcut config'i EZME — admin GUI ile elle düzenlenmiş olabilir
UPDATE dbo.ReportCatalog
SET DashboardConfigJson = N'{
  "schemaVersion": 2,
  "resultContract": {
    "kpi":      { "resultSet": 0, "required": true,  "shape": "row"   },
    "detay":    { "resultSet": 1, "required": true,  "shape": "table" },
    "trend":    { "resultSet": 2, "required": false, "shape": "table" }
  },
  "tabs": [{
    "title": "Genel",
    "components": [
      { "type": "kpi", "result": "kpi", "column": "Ciro", ... },
      { "type": "table", "result": "detay", "tableOptions": { "pageSize": 20 } }
    ]
  }]
}'
WHERE ProcName LIKE N'%sp_Xyz' AND DashboardConfigJson IS NULL;
```

Notlar:
- `id` field YOK — opsiyonel, builder ilk save'de üretir
- `result: "kpi"` named binding — RS sırası değişse bile contract semantik bağ kurar
- `WHERE ... IS NULL` — idempotent + mevcut config koruma
- `ProcName LIKE N'%sp_Xyz'` — schema (`bkm.` vs `dbo.`) DB'ye göre değişir, suffix match daha sağlam

## ProcName Schema Hassasiyeti

DB'ye göre değişiyor (bilinen):
- PDKS Pano: `bkm.sp_PdksPano` (PDKS DB)
- Satış Pano: `bkm.sp_SatisPano` install standardı, mevcut DB'de `dbo.sp_SatisPano` görülebilir

Migration WHERE clause'ında **`LIKE N'%sp_Foo'`** kullan, schema prefix hardcode etme (install ortamlarında uyumsuzluk üretir).

## Bozulan Config'i Onarma (rescue)

Yanlışlıkla `result: "rs2"` veya hardcoded ID'lerle ezilmiş config'i geri getirmek için:

1. `26_AddResultContract.sql`'i tekrar çalıştır → `resultContract` block'u geri gelir
2. Manuel UPDATE ile her widget'ın `result` alanını rsN'den named key'e çevir (migration 27 mantığı)

Veya daha basiti: doğru seed'i tekrar UPDATE et (migration 30 mevcut formu).

## İlişkili

- `Mosaik/Models/DashboardConfig.cs` — schema + ResolveResultSet
- `Mosaik/Services/DashboardConfigValidator.cs` — regex + whitelist
- `Mosaik/wwwroot/assets/js/builder-v2/builder.js:97` — widget ID generator
- `docs/ADR/007-named-result-binding.md` (planlı) — Faz 6 kararı
- `docs/ADR/008-schema-v2.md` (planlı)
