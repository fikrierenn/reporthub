# Plan 45 — Excel-to-Process AI Adaptation Engine

**Durum:** ✅ **ONAYLANDI 2026-05-25** — revize v2 (3 teknik kör nokta + 5 açık soru kapatma)
**Tier:** 3 (yeni modül + AI + schema autogen + UX)
**Tetik:** Kullanıcı strategic input 2026-05-25 — "Excel düşman değil giriş kapısı"
**Effort:** 40-60h (5 faz, 3-4 hafta)
**Aciliyet:** 🟢 Plan 41 (Form Builder) Faz 0-3 sonrası — Form Builder prereq

---

## 1. Problem

BKM Kitap operasyonunda her departman onlarca takip Excel'i kullanıyor:
- Depo: hasar takibi, sayım, sevkıyat, iade
- Satın Alma: tedarikçi puanı, satınalma talebi, fatura takip
- Mağaza: müşteri şikayet, eksik ürün, gün sonu raporu
- İK: izin formu, performans, mülakat değerlendirme
- Kalite: hijyen denetim, ekipman check, sertifika takip

**Problem:** Departman yöneticisi "bu Excel'i sisteme alalım" derken yazılımcı bekleme süresi 2-3 hafta:
- Form Builder UI'da 12 field tipi tek tek tanımla
- DB tablo schema yaz, migration apply
- Geçmiş Excel verisini import et (manuel CSV import)
- Test + canlı

**Sonuç:** Excel'den geçiş yapılmıyor. Mosaik adoption düşük kalıyor. Kullanıcı Excel'i bırakmıyor.

**Stratejik soru:** Excel'i düşman ilan etmek yerine **platforma giriş kapısı** yapmak — 3 hafta yazılım eforu → 5 dakika AI işlemi.

---

## 2. Scope

### Kapsam içi (Faz 1-5)
- Excel upload UI (xlsx/xlsm/csv) + ClosedXML parse
- Qwen 2.5 schema inference: kolon adları → field type + label + validation
- SurveyJS JSON template autogen + admin review/edit UI
- SQL table schema autogen (idempotent migration) + Plan 41 FormSubmission entity reuse
- Geçmiş veri import (Excel row → FormSubmission)
- AI confidence scoring + low-confidence flag (kullanıcı review)
- Audit log: Excel import provenance (source file hash + import user + timestamp)

### Kapsam dışı
- Excel formula parsing (VLOOKUP/INDEX/MATCH) — manuel mapping önerisi
- Pivot table → dashboard widget — Plan 02 dashboard builder ayrı
- Macro/VBA execution — güvenlik riski, kategorik red
- Çoklu sayfa Excel — v1'de tek sayfa, v2 çoklu sayfa Form Group
- Two-way sync (Excel ↔ DB) — tek yönlü (Excel → DB), Excel sonradan reference değil

---

## 3. Mimari

### 3.1 Workflow

```
1. User: /Forms/ExcelImport
   └─ Drag-drop xlsx upload (max 10MB)

2. ExcelParserService.AnalyzeAsync(stream)
   ├─ ClosedXML: WorkbookPart → Sheet1 → Header row + first 10 data rows
   ├─ Qwen prompt: "Bu kolonları SurveyJS field'a dönüştür..."
   ├─ Output: List<DetectedField> { Name, Label, Type, Required, Validation, Confidence }
   └─ Heuristics fallback: pattern match (Tarih, Sayı, Email regex)

3. User Review UI: /Forms/ExcelImportReview/{jobId}
   ├─ Sol pane: Excel orijinal (DataTables read-only)
   ├─ Sağ pane: önerilen SurveyJS template + field edit
   ├─ Low-confidence field'lar sarı highlight ("AI emin değil, kontrol et")
   └─ "Onayla + Form Oluştur" butonu

4. ExcelImportService.CommitAsync(jobId, approvedTemplate)
   ├─ FormDefinition entity create (Plan 41) + SurveyJS JSON kaydet
   ├─ Migration autogen: CREATE TABLE FormResponse_{slug} (Id, FirmaId, ...kolonlar)
   ├─ sqlcli apply
   ├─ Excel data rows → FormResponse rows (batch insert)
   └─ Audit + notification

5. Form artık /Forms/{slug} altında canlı
```

### 3.2 Component'ler

```
Mosaik.Modules.Forms/
├─ Services/
│  ├─ Excel/
│  │  ├─ ExcelParserService.cs         — ClosedXML parse + Qwen analyze
│  │  ├─ FieldInferenceService.cs      — Qwen prompt + heuristic fallback
│  │  ├─ SqlSchemaGenerator.cs         — DetectedField → CREATE TABLE
│  │  ├─ ExcelImportJobStore.cs        — IMemoryCache + IndexedDB için job state
│  │  └─ HistoricalDataImporter.cs     — Excel row → FormResponse
│  └─ ...
├─ Areas/Forms/
│  ├─ Controllers/ExcelImportController.cs
│  └─ Views/ExcelImport/
│     ├─ Index.cshtml      — upload
│     ├─ Review.cshtml     — split-pane review
│     └─ Result.cshtml     — onay sonrası özet
└─ ViewModels/ExcelImportReviewViewModel.cs
```

### 3.3 Qwen Prompt Tasarımı

```
Sistem: BKM Mosaik Excel-to-Form analist. Kolon başlıklarını + ilk 10 satırı ver.
Her kolon için JSON döndür:
{
  "name": "snake_case_camel",         // SQL kolon
  "label": "Türkçe etiket",           // UI label
  "type": "text|number|date|select|email|phone|textarea|checkbox|file",
  "required": true/false,
  "validation": "regex/range/min/max",
  "options": ["seçenek1", "seçenek2"], // select için
  "confidence": 0.0-1.0
}

Excel kolon: {column_name}
Örnek değerler: {samples joined comma}
```

### 3.4 Heuristic Fallback (LLM kapalı / unavailable)

```csharp
public DetectedField InferHeuristic(string header, IReadOnlyList<string> samples)
{
    // Header pattern: "Tarih", "Date", "Tarih_*" → date
    // Header pattern: "Email", "Eposta", "E-posta" → email
    // Samples: tümü Regex(@"^\d+$") → number
    // Samples: tümü Regex(@"^\d{4}-\d{2}-\d{2}") → date
    // ...
}
```

### 3.5 SQL Schema Autogen (revize v2 — güvenlik + veri kaybı koruma)

**DDL Injection Koruma (CRITICAL — slug sanitization):**

```csharp
// SqlSchemaGenerator.SanitizeSlug
string cleanSlug = Regex.Replace(
    sheetName.ToLower(new CultureInfo("tr-TR")),
    @"[^a-z0-9_]",
    "");
if (cleanSlug.Length > 50) cleanSlug = cleanSlug.Substring(0, 50);
// Empty veya numerik-başlangıç → safe default
if (string.IsNullOrEmpty(cleanSlug) || char.IsDigit(cleanSlug[0]))
    cleanSlug = "form_" + Guid.NewGuid().ToString("N").Substring(0, 8);
// SQL reserved word check (USER, TABLE, ORDER, vs.) → suffix _f
if (SqlReservedWords.Contains(cleanSlug))
    cleanSlug = cleanSlug + "_f";
```

ASP.NET Core DB user'ının DDL yetkisi var (CREATE TABLE) — kullanıcı sheet adı `"DROP TABLE Users; --"` girerse string concat ile felaket. Whitelist regex zorunlu.

**Type-Tolerant String Fallback (CRITICAL — data loss koruma):**

Sayısal/tarih kolonlar autogen aşamasında `NVARCHAR(MAX)` olarak yaratılır. AI ilk 10 satıra bakıp `INT` derse bile 100. satırda `"15A"` → `CastException` → tüm import ölür. Çözüm:

```csharp
// İlk pass: TÜM kolonlar NVARCHAR(MAX), import kesinlikle başarılı
// İkinci pass (post-import analysis): "Bu kolondaki 10K satırın tümü int parse oluyor.
//   Kolonu INT'e dönüştürmek ister misin?" → admin onaylı `ALTER TABLE`
// Confidence + tahmin admin UI'da görünür, downgrade asla otomatik değil
```

**Output örneği (revize):**

```sql
CREATE TABLE dbo.FormResponse_depo_hasar_takip (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FormDefinitionId INT NOT NULL,
    FirmaId INT NOT NULL,
    SubmittedBy INT NULL,
    SubmittedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    -- Excel'den autogen — v1 string-first, post-import upgrade
    tarih NVARCHAR(MAX) NULL,
    urun_kodu NVARCHAR(MAX) NULL,
    miktar NVARCHAR(MAX) NULL,
    hasar_tipi NVARCHAR(MAX) NULL,
    aciklama NVARCHAR(MAX) NULL,
    fotograf NVARCHAR(500) NULL,             -- dosya path zaten string

    -- Provenance + offline marker
    ImportedFromExcelHash NVARCHAR(64) NULL,
    OriginalExcelRowNumber INT NULL,
    ImportedOffline BIT NOT NULL DEFAULT 0
);

-- Mosaik global standard: FirmaId composite index (Plan 14 pattern)
CREATE NONCLUSTERED INDEX IX_FormResponse_depo_hasar_takip_FirmaId
    ON dbo.FormResponse_depo_hasar_takip (FirmaId, SubmittedAt DESC);
```

**Index + Disk Bloat (Mosaik standardı):**
- Clustered index `Id` (default, Mosaik global pattern)
- Non-clustered `(FirmaId, SubmittedAt DESC)` zorunlu — multi-tenant scope query
- Eski/kullanılmayan FormDefinition arşiv edildiğinde admin `DROP TABLE FormResponse_{slug}` opsiyonu (audit log)
- Yüzlerce dinamik tablo DB metadata bloat → monthly `sp_helpdb` review job (Plan 47 advisor adayı)

**Naming:** Excel sheet adı → snake_case slug → table suffix. Conflict varsa `_2`, `_3` suffix + SQL reserved word check.

### 3.6 Schema Drift Koruma (form edit sonrası)

Admin Plan 41 Form Builder ile FormDefinition'ı sonradan düzenlerse SurveyJS field eklenir/silinir:

- **Field eklendi:** `ALTER TABLE FormResponse_{slug} ADD {col} NVARCHAR(MAX) NULL` otomatik
- **Field silindi:** SQL kolon **silinmez** (veri kaybı yasak). Sadece SurveyJS template'ten kaldır, kolon NULL kalır (geçmiş veri korunur)
- **Field rename:** Önce yeni kolon ekle + data migrate + eski deprecated flag. Plan 45.1 iş.
- **Field type change:** Type-tolerant fallback aktif — kolonu ALTER TABLE etmiyoruz, app-level cast.

### 3.6 Confidence Scoring

```
Confidence kaynak                            | Ağırlık
---------------------------------------------|---------
Qwen prompt confidence (LLM kendisi)         | 0.5
Heuristic regex match                        | 0.3
Sample data uniformity (variance düşük)      | 0.2

< 0.6 → sarı highlight "kontrol et"
< 0.4 → kırmızı + kullanıcı manuel düzeltme zorunlu
```

---

## 4. Alternatifler (5 lens)

### 🔴 Contrarian: Fatal flaw?

**Schema autogen tehlikeli.** AI yanlış kolon tipi (örn. "12345" → number ama aslında ürün kodu → string) → SQL'de int → import sırasında overflow/cast hatası → data loss.

Daha kötü: AI kolon adını yanlış normalize edebilir ("Ürün Kodu" → `urun_kodu`, ama başka Excel'de "Ürün ID" → `urun_id` — aynı semantik, ayrı tablo).

**Mitigation:**
- Tip downgrade'ı asla: AI `number` dese bile string fallback ile import + post-import "tipi number'a çevir?" wizard
- Schema review aşaması zorunlu (Faz 3 split-pane UI)
- Naming dictionary: BKM yaygın kolon adları map (ürün kodu, sku, barkod → kanonik)

### 🔵 First Principles: Gerçek problem?

Excel parse + AI inference değil. Gerçek problem: **departman yöneticisi yazılımcı bekliyor.** Bypass yolu — Form Builder'ı kendisi kullanabilse Excel'e gerek yok.

**Mitigation v2:** Plan 41 Form Builder UI'ı **drag-drop self-service** seviyesinde olduktan sonra Excel parser secondary path. Plan 41 v1 (JSON config) yetersiz, v2 builder UI önce. Plan 45 Plan 41 v2 sonrası daha mantıklı.

**Karar:** Plan 45 ŞART, ama Plan 41 v2 ile paralel — kullanıcı 2 path: (a) form builder UI sıfırdan, (b) Excel upload + AI parse + review.

### 🟢 Expansionist: Daha büyük fırsat?

Excel sadece form değil — **mevcut süreç bilgisi.** Excel'in kolon adlarından + comment'lardan + formül'den şirketin gerçek operasyon dilini öğrenmek mümkün.

**Mitigation:** Plan 45 Faz 5+: Excel ingestion → BKM Operation Dictionary build. "Ürün kodu" 50 Excel'de geçiyor → kanonik lookup. "Hasar tipi" 12 Excel'de → DictionaryType+Value seed önerisi. **Excel = şirketin gerçek operasyon ontolojisi**.

Plan 45 scope dışı (ayrı plan adayı — Plan 48 BKM Ontology), ama unutulmamalı.

### ⚪ Outsider: Garip ne?

Yabancı geliştirici: "Niye Excel? Excel zaten ETL/BI'de var (Power Query, Tableau Prep). Kendi sürümünüzü niye yapıyorsunuz?"

**Cevap:** Bizim kullanım operasyonel form değil ETL değil. ETL = "veriyi taşı". Bizim = "Excel'i öldür + canlı form'a geç". Mosaik form + workflow + permission + audit ekler. Power Query bunu yapmıyor.

### 🟡 Executor: Pazartesi sabahı?

1. ClosedXML NuGet (zaten OSS reuse — kabul)
2. `ExcelParserService.AnalyzeAsync` skeleton + Sheet1 first 10 rows extract
3. Hard-coded Qwen prompt + JSON output parse
4. Heuristic fallback (date/email/number regex)
5. UI: 1 sayfa upload + result table (Edit'siz, basic)
6. v1 MVP: schema autogen DEVRE DIŞI, sadece SurveyJS template autogen
7. Test: 3 örnek Excel (depo hasar, izin formu, satınalma talep)

---

## 5. Riskler

| Risk | Olasılık | Etki | Mitigation |
|---|---|---|---|
| AI yanlış field type → data loss import | yüksek | yüksek | Tip downgrade yasak, string fallback, post-import wizard |
| Yanlış kolon naming → duplicate table | orta | orta | Naming dictionary + conflict detection + suffix |
| Excel macro / formula → execution risk | orta | yüksek | ClosedXML formula NOT evaluate; macro strip; file scanner (ClamAV future) |
| Çok büyük Excel (10K+ satır) → timeout | düşük | orta | Streaming parse + Hangfire background import |
| KVKK: Excel'de kişisel veri sızıntısı | orta | yüksek | Presidio scan (Plan 40 Faz 5 reuse) — kişisel veri tespit → uyarı |
| LLM cost (Qwen local OK, OpenAI fallback maliyetli) | düşük | düşük | Sadece Qwen local; OpenAI fallback opt-in |

---

## 6. Done Criteria

- [ ] `ExcelParserService` ClosedXML + Qwen prompt + heuristic fallback
- [ ] 3 örnek Excel (depo/izin/satınalma) %80+ field type doğru
- [ ] Split-pane review UI (sol Excel, sağ SurveyJS preview)
- [ ] Low/medium/high confidence color coding
- [ ] FormDefinition create (Plan 41 entity reuse) + SurveyJS JSON kaydet
- [ ] SQL schema autogen idempotent migration generator + admin apply onay
- [ ] Historical data import: Excel row → FormResponse (10K satır ≤ 30sn)
- [ ] Provenance: `ImportedFromExcelHash`, `OriginalExcelRowNumber` her satır
- [ ] Presidio scan (Plan 40 reuse) Excel'de kişisel veri tespit
- [ ] ADR-027 yazımı (Excel-to-Process pattern + LLM inference riskleri)

---

## 7. Rollback

- FormDefinition create geri al: SoftDelete (Plan 41 pattern)
- Autogen migration: DROP TABLE FormResponse_{slug} (manual onay)
- Import edilen FormResponse satırlar: WHERE ImportedFromExcelHash=@hash DELETE

---

## 8. Adımlar / Fazlar

### Faz 1 — Excel parser + AI inference (12h)
- ClosedXML wrapper, sheet/header/sample extract
- Qwen prompt + response parser
- Heuristic fallback regex set

### Faz 2 — Review UI (10h)
- Upload page, drag-drop
- Split-pane review (orijinal vs öneri)
- Field-level edit + confidence highlight

### Faz 3 — SurveyJS template commit (8h)
- FormDefinition entity create (Plan 41 reuse)
- SurveyJS JSON generate from DetectedFields
- Validation + tests

### Faz 4 — SQL schema + historical import (12h)
- SqlSchemaGenerator → idempotent migration string
- Admin apply UI ("önce DB review")
- Hangfire job: Excel rows → FormResponse batch

### Faz 5 — KVKK + audit + test (8h)
- Presidio scan integration (Plan 40 reuse)
- Audit log: import event + provenance
- 3 örnek Excel end-to-end test

---

## 9. Bağımlılıklar

- **Prereq:** Plan 41 Form Builder Faz 0-3 ✅ (FormDefinition entity + SurveyJS render)
- **Reuse:** ClosedXML (OSS), Plan 40 Faz 5 (Presidio Türkçe spaCy)
- **Bağımsız:** Plan 36, 42, 44

---

## 10. Açık Sorular — KAPATILDI 2026-05-25 (revize v2 kararları)

1. **AI vs Heuristic önceliği ne olmalı?**
   **Karar:** **Hibrit (AI First + Heuristic Backup).** İlk pass Qwen yapar. Per-kolon confidence kontrol. `< 0.7` veya LLM çevrimdışı → `InferHeuristic` (Regex) devreye. İki motor çelişirse sarı highlight (Medium Confidence) + manuel onay.

2. **Schema Autogen otomatik mi admin onaylı mı?**
   **Karar:** **Kesinlikle Admin Manuel Onaylı.** Arka planda DDL script üretilir, Review ekranında salt-okunur SQL editörde gösterilir. Admin "Şemayı Veritabanına Uygula" butonu olmadan DB'de hiçbir tablo yaratılmaz. DDL injection + slug sanitization Faz 4'te zorunlu.

3. **Excel formülleri evaluate edilsin mi?**
   **Karar:** **HAYIR. Sadece Cached Value.** ClosedXML formula execute YASAK — sunucu kilitlenir (özellikle TR formüller `DÜŞEYARA`/`EĞER`). Hücre `CachedValue` alınır. Formula tespit edilirse Review ekranında uyarı: "Bu kolon Excel'de formül içeriyordu. Portalda dinamik hesaplama için Hesaplanan Alan (AST Formula Parser) tanımlayabilirsiniz." Plan 45.2 adayı (hesaplanan alan parser).

4. **Multi-Sheet (Çok Sayfalı) Excel?**
   **Karar:** **v1 Sadece Tek Sayfa (Sheet 1).** Birden fazla sheet varsa upload ekranında dropdown: "Çalışma kitabında N sayfa bulundu. Hangisini aktarmak istersiniz?" v2 (Plan 45.1) çoklu sheet → Form Group dönüşümü.

5. **SurveyJS edit edildiğinde Schema Drift nasıl çözülür?**
   **Karar:** **Safe ALTER TABLE & Warning.** Yeni field eklenirse otomatik `ALTER TABLE ADD {col} NVARCHAR(MAX) NULL`. Field silinirse SQL kolonu **silinmez** (veri kaybı yasak), sadece SurveyJS template'ten kaldırılır. Rename → yeni kolon + data migrate + eski deprecated (Plan 45.1).

---

## 11. Onay + Revize Notu

**ONAYLANDI 2026-05-25** — Kullanıcı strategic review:
- 3 teknik kör nokta plan'a eklendi: DDL injection sanitization + type-tolerant string-first + index/disk bloat
- 5 açık soru cevaplandı + plan'a karar olarak gömüldü
- §3.5 SQL Schema Autogen genişletildi + §3.6 Schema Drift bölümü eklendi
- Implementation Plan 41 Faz 0-3 tamamlandığında başlar
