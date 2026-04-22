# ADR-007 · Named result contract — widget'ları result-set index'inden ayır

- **Durum:** Kabul edildi (22 Nisan 2026)
- **Etkilenen:** `DashboardConfig` modeli, `DashboardRenderer`, `dashboard-builder.js`, admin save validation, mevcut PDKS + Satış ConfigJson'ları.
- **İlgili TODO:** M-10 (6 faz).
- **İlgili ADR:** ADR-005 (config-driven dashboard — tek source-of-truth ConfigJson).

## Bağlam

Mevcut dashboard config widget'ları result-set'e **index** ile bağlıyor:

```json
{ "type": "chart", "resultSet": 2, "labelColumn": "date" }
```

Sorunlar:

- **Silent break:** SP'ye başta yeni `SELECT` eklenirse index'ler kayıyor, widget'lar yanlış veri gösteriyor. Hiçbir fail-fast yok.
- **Self-documentation yok:** `resultSet: 2` ne demek? Kod okumadan bilinmiyor.
- **Refactoring zor:** SP'yi yeniden düzenlemek ConfigJson rewrite gerektiriyor.
- **Çoklu widget aynı veri:** Aynı SELECT'i 3 widget kullanıyorsa 3 yerde index tutuluyor. Değişiklik 3 yeri vurmak demek.

## Karar

**Widget'lar `result: "chartData"` (isim) ile bağlanır.** Config root'unda `resultContract` sözlüğü isim → index map'ini tutar. Admin save-time validator isim tutarlılığını zorlar.

```json
{
  "schemaVersion": 1,
  "resultContract": {
    "summary":  { "resultSet": 0, "required": true,  "shape": "row" },
    "detail":   { "resultSet": 1, "required": false, "shape": "table" },
    "chart":    { "resultSet": 2, "required": true,  "shape": "table" }
  },
  "tabs": [{
    "components": [
      { "id": "w_chart_abc123", "type": "chart", "result": "chart", "config": { ... } }
    ]
  }]
}
```

### Kural: "Declare now, enforce later"

- `shape` ve `required` schema'da **şimdi** tanımlı — ileride runtime validation eklendiğinde config rewrite gerekmez.
- Runtime enforcement **Faz 4**'te devreye girer (bkz. faz tablosu).

### Precedence (backward-compat + bounds check)

Renderer resolve kuralı — `DashboardConfig.ResolveResultSet(comp, resultSetCount)` nullable int döner:

```csharp
public int? ResolveResultSet(DashboardComponent comp, int resultSetCount)
{
    // 1. name-based binding
    if (!string.IsNullOrEmpty(comp.Result))
    {
        if (ResultContract == null || !ResultContract.TryGetValue(comp.Result, out var entry))
            return null; // unknown name
        if (entry.ResultSet < 0 || entry.ResultSet >= resultSetCount)
            return null; // out of bounds
        return entry.ResultSet;
    }

    // 2. legacy index binding
    if (comp.ResultSet.HasValue)
    {
        var idx = comp.ResultSet.Value;
        if (idx < 0 || idx >= resultSetCount) return null;
        return idx;
    }

    // 3. no binding
    return null;
}
```

**`null` döndüğünde** renderer `RenderMissingResultPlaceholder` basar (throw yok). `-1` sentinel **kullanılmaz** — contract UI davranışına bağlı kalmasın. Faz 4'te null case `dashboard_result_unresolved` audit event tetikleyecek.

Yeni config'ler `result` kullanır. Eski `resultSet: N` yazan config'ler Faz 5 migration'ı öncesine kadar çalışmaya devam eder. Faz 6'da legacy fallback kaldırılır.

### `required` detect (enforce Faz 4)

Render başında `ResultContract`'taki `required: true` entry'ler kontrol edilir — ilgili result set SP'den dönmedi veya 0 satırsa dashboard üstüne sarı uyarı banner'i ("Eksik zorunlu veri: X, Y") basılır. Dashboard bütünüyle çökmez. Faz 4'te audit event + kullanıcı mesajı sertleştirilir.

### Unknown widget type → placeholder (Codaxy pattern)

Renderer `switch (comp.Type)` ile eşleşmeyen bir type görürse throw etmez — "Bu widget tipi kaldırıldı" placeholder div render eder. Dashboard bütünüyle çökmesin.

### schemaVersion + forward-compat (Kibana pattern)

Root'a `schemaVersion: 1`. System.Text.Json default'u zaten bilinmeyen property'leri sessiz atlıyor → forward-compat ücretsiz geliyor. İleride breaking schema değişikliği gerekirse `schemaVersion` artırılır + migration yazılır.

### Stabil widget id (Grafana anti-lesson)

Her `DashboardComponent`'a `id` string field. İlk save'de otomatik üret (örn. `w_<type>_<hash8>`). Bir kez üretildikten sonra asla değişmez. Bu future'da layout patch, drag-drop undo, cross-widget event targeting için gerekli.

**Grafana refId hatası:** Grafana `A`/`B`/`C` refId'lerini auto-generate ediyor, transformasyonlar arasında mutasyon olabiliyor ([grafana#103955](https://github.com/grafana/grafana/issues/103955)). Bizde id **user-visible değil, kod-üretir, immutable**.

## Alternatifler (reddedildi)

- **Metadata-first SELECT** (RESULT 0 = isim map): tüm SP'leri değiştirmek gerekir, invasive. Config-declared yapı strictly better. OSS'de emsali yok (Evidence.dev, Lightdash, Kibana hiçbiri bu yolda değil).
- **Frontend rewrite (4-module: State/Renderer/Interaction/Data):** 2 dashboard için 2000+ satır JS + build toolchain + iframe sandbox güvenlik kaybı. Scope disproportionate. LobsterBoard tek renderer ile çalışıyor; bu ölçekte doğru altitude.
- **Event bus (chart→table filter):** kullanıcı talep etmemiş, YAGNI.
- **Edit/view ayrı route:** zaten URL route'u ayrım yapıyor (`/Admin/EditReport/{id}` vs `/Reports/Run/{id}`). LobsterBoard bile tek renderer + Ctrl+E toggle ile çözüyor.
- **DB surrogate key'i isim olarak kullanmak** (Redash `query_49588` pattern): admin-authored human label zorunlu. `rs_2` gibi otomatik isim yasak.

## OSS emsalleri

- **Evidence.dev** — named queries + `data={queryName}` widget binding + compile-time validator ([docs](https://docs.evidence.dev/core-concepts/queries)). **ADR-007'nin birebir olgun kardeşi.**
- **Kibana** — `references[]` + `panelRefName` indirection ([PR #39387](https://github.com/elastic/kibana/pull/39387/files)). schemaVersion + forward-compat.
- **Lightdash** — [JSON Schema publish](https://raw.githubusercontent.com/lightdash/lightdash/refs/heads/main/packages/common/src/schemas/json/model-as-code-1.0.json) (Monaco/ACE autocomplete).
- **Codaxy** — widget type registry + "removed widget" fallback ([app/widgets/index.js](https://raw.githubusercontent.com/codaxy/dashboards/master/app/widgets/index.js)).
- **Grafana** (anti-emsali) — refId auto-gen instability ([#103955](https://github.com/grafana/grafana/issues/103955)).

## Geçiş fazları

| Faz | Kapsam | Süre |
|---|---|---|
| **1** | ADR + `DashboardConfig` model (SchemaVersion + ResultContract + Component.Result + Component.Id) + `DashboardRenderer` resolver (precedence + unknown-type placeholder) + JSON Schema dosyası | ~2h |
| 2 | `dashboard-builder.js` + admin form UI name-based binding | ~3h |
| 3 | Admin save validation (hard: name unique, index valid, widget.result resolve. soft: required-unused warning) | ~2h |
| 4 | Runtime soft-fail + `dashboard_required_result_missing` audit event | ~1h |
| 5 | Migration 18 — PDKS + Satış ConfigJson rewrite (`resultContract` + widget id + `result` field). Idempotent. `Explore` agent isim önerisi + manuel onay. | ~2h |
| 6 | Legacy `resultSet: N` binding deprecate + renderer fallback kaldır | ~1h |

**Toplam:** ~11 saat (1.5 gün). Faz 1 + 5 → render yolu. Faz 2 + 3 → admin UI yolu. Faz 4 + 6 → hardening.

## Scope dışı (ayrı TODO / feature PR)

- **Master-detail** (`detail.relation: "orderId"`) — legitimate feature ama contract kararından bağımsız. İsim kontratı olduktan sonra widget tipi eklemek ucuz.
- **Pivot widget** — 3. parti lib gerektirir, feature PR.
- **Multi-view widget** (aynı kart chart/table toggle) — widget tipi eklemesi.
- **Event bus** — YAGNI, kullanıcı talep etmedi.

## Kabul kriteri

1. Mevcut PDKS + Satış dashboardları smoke test'te bozulmaz (Faz 5 migration sonrası).
2. `DashboardConfigJson` root'unda `resultContract` ve `schemaVersion` olan her config için `result` kullanan widget'lar çalışır.
3. Legacy `resultSet: N` binding Faz 6'ya kadar çalışmaya devam eder.
4. Admin save'de duplicate name veya eksik widget.result referansı → hata mesajı (Faz 3).
5. Runtime required result eksikse placeholder + audit event (Faz 4).
