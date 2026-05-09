# Plan 05 — Tablo Widget: Kolon Checkbox + Hesaplı Kolonlar (AST whitelist)

**Tarih:** 2026-04-30
**Yazan:** Fikri / Claude
**Durum:** `Tamamlandı` (2026-04-30)

**Yön değişikliği notu:** Faz 2 başlangıçta `DashboardConfig.CalculatedFields`
top-level + `TableColumnDef.Computed` flag yaklaşımıyla yazıldı (commit `99084cb`).
Sonra "tablo komple JSON" niyetiyle revize edildi: Faz 2 mini-revert + tablo widget'a
özel `TableColumnDef.Formula` (commit `6d5ca5c`) + `JsonExtensionData` esneklik
mekanizması (commit `9f8cca0`). Sonuç: tek state path (kolon nesnesinde Formula),
schema-by-default + Extra bypass valve.

---

## 1. Problem

V2 builder'da tablo widget'ın kolon yönetimi pratik değil:

- **05.A:** Tablo seçildiğinde Setup tab kolon listelemiyor — `comp.Columns` (TableColumnDef[]) raw JSON'da kalıyor, kullanıcı checkbox'la kolon eklenip çıkarılamıyor, sıralama drag-drop ile değiştirilemiyor. Bağlı RS'in tüm kolonları otomatik gelse de, kullanıcı **alt küme** seçemiyor.
- **05.B:** Hesaplı kolon (örn. `IIF(satis > 100, 'YÜKSEK', 'DÜŞÜK')`, `BugunCiro - GecenYilBugun`, `CASE WHEN col = 'X' THEN 1 ELSE 0 END`) eklenemez. Schema **hazır** (`DashboardConfig.CalculatedFields: List<CalculatedField>`, ADR-008 v2'de declare edilmiş) ama parser yazılmamış — yorum: _"AST-sandbox parser F-8'de"_.

Kullanıcı tablo widget'ını gerçek BI deneyimine yaklaştırmak istiyor: kolon seç + sırala + hesaplı kolon ekle.

## 2. Scope

### Kapsam dahili

**Plan 05.A — Tablo kolon checkbox + drag-drop:**
- Tablo widget Setup tab'da bağlı RS'in tüm kolonları checkbox listesi (✓ = `Columns`'da var)
- Drag-drop reorder (HTML5 native veya Sortable.js benzeri)
- Kolon başlığı (label) inline edit (alias)
- Hizalama (sol/sağ/orta) kolon başına buton

**Plan 05.B — Hesaplı kolon + AST evaluator:**
- Setup tab tablo seçiliyken "+ Yeni Hesaplı Kolon" buton → form (alias + formula textarea + format)
- `CalculatedField` (top-level, mevcut schema) kullanarak ekle, tablo `Columns`'a `key="cf:<name>"` referansı
- Server-side `Services/Eval/FormulaEvaluator.cs` — AST whitelist parser + evaluator
- DashboardRenderer satır işleme: her satırda formula evaluate edip `row[name] = value` ekleme (tablo body client-side JS satır JSON'unu okuyor, doğal olarak yeni anahtar görür)
- Token whitelist: literal sayı/string, kolon ref (`[ColAdı]` veya bareword), `+ - * /`, parantez, karşılaştırma (`= != < > <= >=`), mantık (`AND OR NOT`), `IF(cond, t, f)`, `IIF(cond, t, f)`, `CASE WHEN ... THEN ... [ELSE ...] END`
- Whitelist dışı her token → Türkçe hata mesajı, formula reddedilir
- Unit testler (`ReportPanel.Tests/FormulaEvaluatorTests.cs`)

### Kapsam dışı

- **Client-side eval** — kesinlikle yok (security-principles.md: `eval()` yasak, JS Function() yasak)
- **Toplam satırı (totalRow) hesaplı kolon desteği** — Faz 6'da, ayrı plan
- **Conditional format computed kolon üzerinde** — Faz 6'da
- **String concat fonksiyonları (CONCAT, SUBSTRING)** — V1 için yok, sadece sayısal/koşullu
- **Tarih fonksiyonları (DATEDIFF, DATEPART)** — V1 için yok
- **Aggregate fonksiyonları formula içinde (SUM, AVG)** — KPI/Chart `Agg` zaten yapıyor, tabloda satır-bazlı eval, aggregate yok
- **Cross-RS column reference** — formula sadece kendi tablo widget'ının bağlı RS'indeki kolonlara erişir
- **V1 dashboard-builder.js / V1 view'lar** — sadece V2 (EditReportV2 + CreateReportV2)

### Etkilenen dosyalar (tahmin)

| Dosya | Değişiklik |
|---|---|
| `Services/Eval/FormulaEvaluator.cs` | **YENİ** — tokenizer + AST parser + evaluator (~300 satır) |
| `Services/Eval/FormulaToken.cs` | **YENİ** — token tipi enum + record (~50 satır) |
| `Services/Eval/FormulaNode.cs` | **YENİ** — AST node tipleri (binary/unary/literal/column/if/case) (~80 satır) |
| `Services/DashboardConfigValidator.cs` | + computedColumns formula whitelist validation çağrısı |
| `Services/Rendering/TableRenderer.cs` | + `data-tbl` JSON'a computed kolon meta + row pre-eval |
| `Services/DashboardRenderer.cs` | + result set satırlarına computed kolon evaluate + row dict'e ekleme |
| `Models/DashboardConfig.cs` | (gerekiyorsa) `TableColumnDef.Computed: bool?` flag — UI cf: prefix yerine semantik |
| `wwwroot/assets/js/builder-v2/builder-drawer.js` | + tablo Setup tab kolon list mixin (renderColumnPicker, addCalcCol, removeCalcCol, reorderCol) |
| `Views/Admin/EditReportV2.cshtml` | + Setup tab `<template x-if="selected.type === 'table'">` checkbox grid + calc cols section |
| `Views/Admin/CreateReportV2.cshtml` | aynı |
| `wwwroot/assets/css/builder-v2.css` | + `.col-picker`, `.col-row` (drag handle + checkbox + label + align), `.calc-col-form` |
| `ReportPanel.Tests/FormulaEvaluatorTests.cs` | **YENİ** — 25-40 unit test |

**Tahmini boyut:** ~14 dosya / ~1100 satır net (tests dahil).

## 3. Alternatifler

### A: SQL-tarafı hesaplı kolon (CASE WHEN x THEN y END AS NewCol)
**Açıklama:** Admin SP'yi düzenler, yeni alias kolonu SP'den döndürür.
**Reddetme sebebi:**
- Admin SP'ye dokunmak istemiyor (rapor metadata'sı, kullanıcı self-service istiyor)
- SP değişikliği prod deploy gerektirir, dashboard builder UI'ında anlık olmaz
- Plan 05'in tüm motivasyonu UI tarafı — SQL tarafına atmak işi taşımak

### B: JavaScript eval / `new Function(formula)`
**Açıklama:** Client-side JS eval ile formula çalıştır, hızlı + kolay.
**Reddetme sebebi:**
- `.claude/rules/security-principles.md`: _"`eval()` **yasak**"_, _"`onclick` inline yasak"_ — XSS izolasyonu (iframe sandbox) iç tarafta da ihlal edilmemeli
- Dashboard config kullanıcıdan geliyor (admin), JS'de eval zincirleme XSS vektörü
- Server-side eval daha controlled ortam (tip kontrol, sandbox, exception)

### C: ANTLR / Sprache benzeri 3rd-party parser kütüphanesi
**Açıklama:** Hazır parser kütüphanesi (ANTLR runtime, Sprache, Pidgin) ile grammar yaz.
**Reddetme sebebi:**
- Sprache (~100KB) / ANTLR runtime (~500KB) yeni dependency
- Mini-DSL (~10 token tip), recursive descent parser ~250 satır C# ile yazılır — fazla mühendislik
- 3rd-party parser bug'ı = bizim güvenlik açığımız (audit yapmak zor)
- Sprache çok hafif ama yine de paket eklenmesi build/CI değişikliği

### D (seçilen): Kendi recursive descent parser + token whitelist
**Açıklama:** Tokenizer (string → token akışı, whitelist'le filtre) + recursive descent parser (token → AST) + tree walker evaluator (AST + row dict → değer). Saf C#, sıfır dependency.
**Sebep:**
- Whitelist explicit + auditable (token enum'da ne varsa o, başka token reddedilir)
- Parser ~250 satır, evaluator ~150 satır, test edilebilir
- Performance: AST cache per request (config statik), satır eval'i sadece tree walking
- Solo dev için yatırım/getiri optimal

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| AST parser security açığı (whitelist atlama) | yüksek | orta | Token enum kapalı liste; tokenizer regex değil char-by-char + state machine; parser yalnızca enum'daki token'ları kabul; identifier whitelist (kolon adı a-zA-Z0-9_); 3rd-party izoation testi (`security-review` skill) |
| Performance: 1000 satır × 10 hesaplı kolon = 10000 evaluation | orta | orta | AST tree per-formula 1 kez parse (config statik); evaluator stateless; literal kolon ref `Dictionary<string,object>` lookup O(1); benchmark testi (1000 row × 10 col < 50ms hedef) |
| Mevcut config geriye uyumluluk | düşük | düşük | `CalculatedFields` zaten v2 schema'da nullable, config'lerde yok = no-op; tablo `Columns`'a yeni `key="cf:..."` eklenirse opt-in; eski config'ler dokunulmuyor |
| Türkçe karakter (kolon adı `Şube`, `Çıkış`) tokenizer'da kırılır | orta | yüksek | Identifier regex `[\p{L}_][\p{L}\p{N}_]*` (Unicode letter category); test case'leri Türkçe kolon adıyla |
| Format çakışması (`CalculatedField.Format` vs `TableColumnDef.Format`) | düşük | düşük | Tablo kolon `Format` öncelikli (UI level); CalculatedField.Format default fallback |
| Drag-drop browser uyumluluğu | düşük | düşük | HTML5 native drag/drop API kullan (jQuery yok kuralı); Chromium-based hedef tarayıcı, kabul; mobile fallback Faz 7+ |

## 5. Done Criteria

- [x] **05.A:** Tablo widget seçilince Setup tab'da checkbox listesi (RS bağlıysa, RS yoksa info mesajı)
- [x] **05.A:** ✓ kaldırınca `comp.Columns`'tan o kolon çıkar; ekleyince eklenir; sıra `Columns` listesine yansır
- [x] **05.A:** Drag-drop ile sıra değişir → `comp.Columns` array sırası güncellenir → `syncConfig()` tetiklenir
- [x] **05.A:** Kolon başına 3-state hizalama butonu (sol/orta/sağ) `TableColumnDef.Align` günceller
- [x] **05.B:** "+ Yeni Hesaplı Kolon" formu açılır (alias + formula + format), kaydedince tablo `Columns` listesine `{key, label, align, format, formula}` eklenir _(yön değişikliği: top-level CalculatedFields yerine TableColumnDef.Formula tek state path)_
- [x] **05.B:** Geçerli formula örnekleri çalışır (44 unit test + 4 entegrasyon):
  - `BugunCiro - GecenYilBugun` (aritmetik)
  - `IIF(satis > 100, 'Yüksek', 'Düşük')` (koşul, IIF alias)
  - `IF(stok < 10, 'Az', IF(stok < 50, 'Orta', 'Bol'))` (nested IF)
  - `CASE WHEN durum = 'A' THEN 1 WHEN durum = 'B' THEN 2 ELSE 0 END`
  - `(satis - maliyet) / maliyet * 100` (yüzde)
- [x] **05.B:** Geçersiz formülde Türkçe hata mesajı kullanıcıya gösterilir, save bloklanır + UI'da live görünür
- [x] **05.B:** Server-side validator (`DashboardConfigValidator`) computed kolon formula'sını save'de doğrular + `AdminController.ValidateFormula` live endpoint
- [x] **Smoke:** PDKS Pano/13 → tablo widget + `IIF([Sube]='Ankara', 'Merkez', 'Sube')` eklendi, render OK; `eval('x')` reddedildi (pos 5 + Türkçe hata)
- [x] **Test:** `FormulaEvaluatorTests` 44 test, `DashboardRendererTests` 4 entegrasyon, `DashboardConfigExtensionDataTests` 7 round-trip → toplam 217 test, sıfır regression
- [x] **Build OK** + **smoke OK** (browser preview + curl-style preview_eval)
- [x] **Bonus:** `JsonExtensionData` esneklik mekanizması — DashboardConfig/Component/Tab/TableColumnDef üzerine `Extra: Dictionary<string, JsonElement>?` eklendi, frontend yeni alan ekleme schema değişikliği gerektirmez (round-trip otomatik)

## 6. Rollback Planı

- **Git revert:** Plan 05 birden fazla commit (faz başına 1) — atomic revert per faz
- **Feature flag yok** (config opt-in: `CalculatedFields` boş = pre-existing davranış)
- **Mevcut config'ler dokunulmuyor:** schema additive, eski tablo widget'lar `Columns` raw kullanmaya devam eder
- **DB migration yok** (DashboardConfigJson nvarchar(max), schema flexible)
- **Eğer parser bug → prod hata:** `TableRenderer` try-catch zaten var, computed kolon evaluate fail → cell `null` → "—" render, dashboard çökmez

## 7. Adımlar

> **Tamamlandı (2026-04-30) — Commit zinciri:**
> - `c836301` Faz 1: FormulaToken/Node/Tokenizer/Parser/Evaluator + 44 test
> - `99084cb` Faz 2 v1: top-level CalculatedFields global enrichment _(yön değişikliği ile revert edildi)_
> - `6d5ca5c` Faz 2 v2 + Faz 3: tablo-özel `TableColumnDef.Formula` + Setup UI checkbox/drag-drop
> - `9f8cca0` Bonus: `JsonExtensionData` esneklik (Extra dict round-trip)
> - `5ee0e96` Faz 4: hesaplı kolon UI + AdminController.ValidateFormula
>
> Aşağıdaki adım tablosu tarihsel referans (orijinal plan).

### Faz 1 — AST parser çekirdeği (server-side, test-first)
1. [ ] **05.1.1** `Services/Eval/FormulaToken.cs` — TokenType enum + Token record (lexeme + position)
2. [ ] **05.1.2** `Services/Eval/FormulaNode.cs` — AST node tipleri: LiteralNode, ColumnNode, BinaryOpNode, UnaryOpNode, IfNode, CaseNode
3. [ ] **05.1.3** `Services/Eval/FormulaTokenizer.cs` — char-by-char state machine, Unicode identifier, whitelist
4. [ ] **05.1.4** `Services/Eval/FormulaParser.cs` — recursive descent (precedence: OR < AND < NOT < cmp < +- < */ < unary < primary)
5. [ ] **05.1.5** `Services/Eval/FormulaEvaluator.cs` — AST + IDictionary<string, object?> row → object?
6. [ ] **05.1.6** `ReportPanel.Tests/FormulaEvaluatorTests.cs` — token + parser + evaluator + edge cases (~30 test)
7. [ ] Commit: `feat(m-11 plan-05): formula evaluator (AST + whitelist + tests) (plan: 05)`

### Faz 2 — Validator + Renderer entegrasyonu
8. [ ] **05.2.1** `DashboardConfigValidator.cs` — `ValidateCalculatedFields(config)` → `Parser.TryParse(formula)` her CalculatedField için
9. [ ] **05.2.2** `DashboardRenderer.cs` — render-time row enrichment: bağlı RS satırlarına computed kolon eval, `Dictionary<string, object?> row[cf.Name] = value`
10. [ ] **05.2.3** `TableRenderer.cs` — `comp.Columns` içinde `key="cf:<name>"` veya `Computed=true` flag'li olanlar için meta'yı normal kolonla aynı işle
11. [ ] **05.2.4** Smoke: PDKS Pano'da elle DashboardConfigJson'a 1 CalculatedField ekle, EditReportV2/13 → preview → tablo widget'ta görün
12. [ ] Commit: `feat(m-11 plan-05): renderer + validator computed kolon entegrasyonu (plan: 05)`

### Faz 3 — V2 Builder UI: tablo kolon checkbox + drag-drop (Plan 05.A)
13. [ ] **05.3.1** `builder-drawer.js` — `tableColumnsAvailable()` (bağlı RS kolonları), `tableColumnsSelected()` (comp.Columns), `toggleTableColumn(colName)`, `reorderTableColumn(fromIdx, toIdx)`, `setColumnAlign(colName, align)`
14. [ ] **05.3.2** `EditReportV2.cshtml` Setup tab — `<template x-if="selected && selected.type === 'table'">` blok: kolon checkbox grid + drag-drop
15. [ ] **05.3.3** `CreateReportV2.cshtml` aynı
16. [ ] **05.3.4** `builder-v2.css` — `.col-picker`, `.col-row` (drag handle + checkbox + label + align toggle)
17. [ ] **05.3.5** Smoke: tabloya 5 kolon, 2'sini kapat, kalanı drag-drop ile sırala, kaydet, reload, sıra korunur
18. [ ] Commit: `feat(m-11 plan-05.A): tablo Setup kolon checkbox + drag-drop (plan: 05)`

### Faz 4 — V2 Builder UI: hesaplı kolon ekle (Plan 05.B)
19. [ ] **05.4.1** `builder-drawer.js` — `addCalcColumn()` (alias + formula + format → CalculatedFields'a + Columns'a), `removeCalcColumn(name)`, `editCalcColumn(name, ...)`
20. [ ] **05.4.2** `EditReportV2.cshtml` Setup tab tablo bloğu — "+ Yeni Hesaplı Kolon" butonu + inline form (alias input + formula textarea + format select)
21. [ ] **05.4.3** Client-side syntax preview: textarea blur → fetch `POST /Admin/ValidateFormula` → Türkçe hata varsa göster (server tek source of truth)
22. [ ] **05.4.4** `AdminController.ValidateFormula(string formula, List<string> availableColumns)` — admin-only, 200 OK / 400 Türkçe hata
23. [ ] **05.4.5** Smoke: PDKS Pano tablo widget → "+ Yeni Hesaplı Kolon" → `IIF(KadroToplam > 50, 'Büyük', 'Küçük')` → kaydet → preview'da kolon görün
24. [ ] **05.4.6** Smoke negatif: `eval('x')` formülü save → 400 + Türkçe hata
25. [ ] Commit: `feat(m-11 plan-05.B): hesaplı kolon UI + ValidateFormula endpoint (plan: 05)`

### Faz 5 — Cleanup + dokümantasyon
26. [ ] **05.5.1** `docs/dashboard-engine-architecture.md` — formula evaluator bölümü ekle (whitelist + sözdizimi referansı)
27. [ ] **05.5.2** ADR-008 v2 dosyasında F-8 maddesini "tamamlandı" işaretle
28. [ ] **05.5.3** `docs/user-guide/V2-builder-rehberi.html` — hesaplı kolon bölümü (örnek formüller)
29. [ ] Commit: `docs(m-11 plan-05): formula evaluator dokümantasyonu (plan: 05)`

## 8. İlişkili

- ADR-008 v2 schema (CalculatedField declared, F-8 parser şu plan'da implement ediliyor)
- `.claude/rules/security-principles.md` — eval yasak, AST whitelist
- `.claude/rules/csharp-conventions.md` — 300 satır eşiği (parser/evaluator ayrı dosyalar)
- Plan 04 (V2 redesign, kapandı) — Setup tab pattern bu plana baz
- Önceki tablo widget render: `Services/Rendering/TableRenderer.cs`

## 9. Sözdizimi referansı (formula DSL v1)

```ebnf
formula     = orExpr ;
orExpr      = andExpr { "OR" andExpr } ;
andExpr     = notExpr { "AND" notExpr } ;
notExpr     = [ "NOT" ] cmpExpr ;
cmpExpr     = addExpr [ ("=" | "!=" | "<>" | "<" | ">" | "<=" | ">=") addExpr ] ;
addExpr     = mulExpr { ("+" | "-") mulExpr } ;
mulExpr     = unaryExpr { ("*" | "/") unaryExpr } ;
unaryExpr   = [ "-" ] primary ;
primary     = literal | column | "(" formula ")" | ifCall | iifCall | caseExpr ;
ifCall      = "IF" "(" formula "," formula "," formula ")" ;
iifCall     = "IIF" "(" formula "," formula "," formula ")" ;
caseExpr    = "CASE" { "WHEN" formula "THEN" formula } [ "ELSE" formula ] "END" ;
column      = identifier | "[" identifier "]" ;
literal     = number | string | "TRUE" | "FALSE" | "NULL" ;
identifier  = letter { letter | digit | "_" } ;
```

**Whitelist token'ları (TokenType enum):**
`Number, String, Bool, Null, Ident, LBracket, RBracket, LParen, RParen, Comma, Plus, Minus, Star, Slash, Eq, Neq, Lt, Gt, Lte, Gte, And, Or, Not, If, Iif, Case, When, Then, Else, End, EOF`

Bunun dışındaki her şey `FormulaParseException` ("Bilinmeyen sembol: ...").
