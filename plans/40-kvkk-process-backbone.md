# Plan 40 — KVKK Veri Envanteri + Process Backbone

**Tarih:** 2026-05-21
**Yazan:** Fikri / Claude
**Durum:** `Taslak` (onay bekliyor)
**Bağımlılık:** Plan 38 EntityRelations (✅ onaylandı), Plan 34 SOP (✅ onaylandı, henüz başlamadı), Plan 36 Workflow Engine (✅ onaylandı), Plan 16.5 AI Core (yarım), Plan 31 SMTP (✅ altyapı)

---

## 1. Problem

KVKK 6698 envanter yükümlülüğü Mosaik'te yok. BKM Kitap için **361 süreç × 20 sütun** envanter Excel'de tutuluyor (`BKM_KVKK_Veri_Envanteri_v7_DENETIM_HAZIR.xlsx`, 17 departman, 5 REF lookup, Risk Özeti dashboard). Excel:

- Versiyonlanmıyor (her güncelleme yeni dosya kopyası)
- Çapraz arama yok ("ad-soyad nerelerde işleniyor?" sorusu cevapsız)
- SOP / Form / Sözleşme ile bağlantı yok
- Birim müdürü doğrulama akışı manuel
- VERBİS beyanı manuel kopyala-yapıştır
- Aydınlatma metni ↔ envanter çapraz check yok
- Kopyala-yapıştır işleme amaçları (Pattern 1) tespitsiz
- Yurt dışı SaaS aktarımları (Microsoft 365 / Google / AWS) gözden kaçıyor

**Daha derin problem (kullanıcı fikri 2026-05-21):**

Bir süreç sadece envanter satırı değildir. Süreç **uygulamak için** workflow + form + SOP + audit + kitapçık üretir. Bu 6 aspect şu an Mosaik'te dağınık (Tamim, Documents, Workflow Engine taslak, SOP yok, Form Builder yok). **Tek omurga yok.**

**Çözüm:** `Process` central entity + `DataElement` granular envanter + `EntityRelations` polymorphic linker. Süreç → 6 aspect derived. Reverse navigation ("parmak izi nerede?") DataElement seviyesinde.

---

## 2. Scope

### Kapsam dahili

**Veri modeli:**
- `Process` — süreç tanımı (KVİE satırı, central entity)
- `DataElement` — atomic veri öğesi (ad-soyad, TC, parmak izi, CCTV, IBAN, ~60 seed)
- `DataCategory` — KVKK 22 standart kategori (REF lookup seed)
- `PersonGroup` — standart kişi grupları (13+ seed)
- `LegalBasis` — KVKK m.5/2 + m.6/3 (12+ seed)
- `RetentionRule` — saklama süresi + yasal dayanak matrisi (24 seed)
- `DisposalMethod` — imha yöntemi (6 seed)
- `MeasureStandard` — idari + teknik tedbir (23 seed)
- `ProcessDataLink` — Process × DataElement junction (M-M)
- `CrossBorderTransfer` — yurt dışı aktarım (alıcı + ülke + mekanizma)
- `DisclosureNotice` — aydınlatma metni versioning (m.10)
- `DsarRequest` — KVKK m.13 ilgili kişi başvuru
- `DataBreachIncident` — m.12/5 72h ihlal bildirim
- `KvkkIntegrityFinding` — AI checker çıktısı

**Servis + iş:**
- `IProcessService` — CRUD + audit
- `IDataElementService` — reverse lookup ("ad-soyad nerede?")
- `IKvkkIntegrityChecker` — 8 pattern detector
- `IVerbisExporter` — Mart 2025 rehber formatında Excel
- `IKvkkAdvisor` — AI prompt orchestration (skill'in runtime karşılığı)
- Hangfire cron: 6h imha takvimi reminder, breach 72h timer, yıllık review

**UI:**
- KVİE CRUD (Index departman+risk filter, Edit 20 sütun, Detail 6 aspect tab)
- Global reverse search (sidebar üst arama: DataElement adı yaz → modal)
- DSAR + Breach workflow (Plan 36 engine reuse)
- VERBİS export butonu
- Risk dashboard (Risk Özeti xlsx karşılığı)
- AI integrity findings panel

**Veri:**
- BKM Kitap v7 xlsx → DB seed (361 süreç, 5 REF, 17 departman)
- DataElement seed ~60 öğe
- Re-import job (xlsx merge, duplicate koruma)

**EntityRelations entegrasyonu (Plan 38):**
- `Process` ↔ `Sop` (RelationType: `derivedFrom`)
- `Process` ↔ `Form` (RelationType: `consumes`)
- `Process` ↔ `WorkflowDefinition` (RelationType: `executedBy`)
- `Process` ↔ `Document` (RelationType: `documentedBy`)
- `Process` ↔ `DataElement` (RelationType: `processes`) — junction'a paralel polymorphic kayıt da yazılır (Plan 38 omurga için)

### Kapsam dışı

- **Form Builder modülü** — KVKK Faz 3 form bağlama, **Form Builder ayrı plan** (Plan 41 adayı). Şimdilik Process ↔ Form link manuel/string, builder geldiğinde otomatik mapping.
- Aydınlatma metni AI üretimi — manuel yazılır, sadece versiyonlanır
- VERBİS web giriş otomasyonu — Excel export yeterli, manuel yükleme
- Multi-firma KVİE template kütüphanesi — BKM Kitap'a özel, sonradan Belinza vb. yayılma ayrı plan
- KVKK skill içeriği değiştirme — `.claude/skills/kvkk-veri-envanteri/SKILL.md` Claude oturum tarafı, runtime'a kopyalanmaz; sadece AI prompt kaynağı olarak referans verilir
- Eski FK refactor — Plan 38 ilkesi: yeni iş yazıcı, eski FK'lar dokunulmaz

### Etkilenen dosyalar

```
Mosaik.Modules.Kvkk/                            (yeni csproj — ADR-015 ayrı assembly)
├── Mosaik.Modules.Kvkk.csproj
├── KvkkModule.cs                               (IMosaikModule)
├── Areas/Kvkk/
│   ├── Controllers/
│   │   ├── ProcessController.cs                (CRUD)
│   │   ├── DataElementController.cs            (reverse lookup)
│   │   ├── DsarController.cs                   (m.13 başvuru workflow)
│   │   ├── BreachController.cs                 (m.12 72h timer)
│   │   ├── VerbisExportController.cs
│   │   └── IntegrityFindingController.cs
│   ├── Views/Process/{Index,Edit,Details}.cshtml
│   ├── Views/DataElement/{Index,Details,ReverseSearch}.cshtml
│   ├── Views/Dashboard/{Risk,Findings}.cshtml
│   └── ViewModels/
├── Entities/
│   ├── Process.cs / ProcessDataLink.cs
│   ├── DataElement.cs / DataCategory.cs
│   ├── PersonGroup.cs / LegalBasis.cs
│   ├── RetentionRule.cs / DisposalMethod.cs / MeasureStandard.cs
│   ├── CrossBorderTransfer.cs / DisclosureNotice.cs
│   ├── DsarRequest.cs / DataBreachIncident.cs
│   └── KvkkIntegrityFinding.cs
├── Services/
│   ├── ProcessService.cs
│   ├── DataElementService.cs
│   ├── KvkkIntegrityChecker.cs                 (8 pattern detector)
│   ├── VerbisExporter.cs                       (ClosedXML)
│   ├── KvkkAdvisor.cs                          (FallbackLlmService + skill prompt)
│   ├── BreachNotificationJob.cs                (Hangfire 72h)
│   └── XlsxImporter.cs                         (BKM v7 → DB)
└── Database/
    ├── 66_KvkkSchema.sql                       (entity tablolar + index)
    ├── 67_KvkkLookupSeed.sql                   (REF: kategori/sebep/saklama/imha/tedbir)
    ├── 68_KvkkDataElementSeed.sql              (~60 atomic veri öğesi)
    └── 69_KvkkBkmKitapImport.sql               (xlsx → 361 süreç, idempotent)
```

**Tahmini boyut:** ~40-50 dosya, ~3500-4500 satır C# + ~800-1200 satır SQL.

---

## 3. Alternatifler

### A: `Process` central entity + 6 aspect derived (SEÇİLEN)

Tek `Process` tablosu KVİE satırını taşır. Workflow / Form / SOP / Doküman / Audit / Kitapçık aspect'leri EntityRelations üzerinden polymorphic. DataElement granular, junction tablo.

**Sebep:** Kullanıcı 2026-05-21 fikri "süreç sonucunda workflow + form vs vs". Tek omurga = navigation tutarlı. Plan 38 EntityRelations omurgayı taşır. Reverse navigation maliyeti düşük (single index lookup).

### B: KVİE bağımsız modül, SOP/Form/Workflow ayrı yaşar

**Reddetme:** Mevcut Excel pattern'i tekrar eder. Reverse navigation imkansız. Aydınlatma metni / envanter / VERBİS uyumsuzluğu pattern 4 baş ağrısı devam eder. Tek artısı: scope küçük (~30h). Uzun vade kayıp daha büyük.

### C: KVİE'yi sadece skill olarak tut, modül yazma

**Reddetme:** Excel manuel kalır. Audit gap (BKM Kitap 200+ kişi, denetimde defansif olamaz). VERBİS export manuel. Comment/Mention pattern envantere dokunamaz. AI integrity check sadece tek seferlik skill kullanımı (sürekli izleme yok).

### D: Açık kaynak KVKK aracı entegrasyon (OneTrust / Privado / OpenComply)

**Reddetme:** OneTrust $50K+ lisans, self-host yok. Privado AI-first ama enterprise pricing. OpenComply LGPD odaklı (Brezilya), KVKK adapt etmek effort. Türkçe + KVKK Mart 2025 rehber + VERBİS + BKM-özgü hukuki sebep eşleşmeleri kütüphaneye gömülü değil.

### Lens kontrolü

- 🔴 **Contrarian:** Process omurga fatal flaw: eski modüller (Tamim, Documents, Contracts) refactor olmadan bu omurgaya nasıl bağlanır? **Cevap:** Bağlanmaz — Plan 38 ilkesi. Yeni iş omurgaya kayıt düşer. Eski FK'lar dokunmaz. Üst yapı kendiliğinden çıkar.
- 🔵 **First Principles:** Gerçek problem KVKK uyum değil, **operasyonel veri haritası**. KVKK yasal vesile; asıl değer "ad-soyad nerelerde işleniyor" reverse navigation. Aynı pattern: "Hangi süreç IBAN'a dokunuyor?" finansal kontrol için de değer.
- 🟢 **Expansionist:** Process omurga sadece KVKK değil, **Operational Intelligence Layer** §7.3 EntityRelations'ın ilk büyük canlı testi. SOP, Form, Comment, Workflow, KVKK hepsi aynı omurga. KVKK = pilot.
- ⚪ **Outsider:** Bir denetçi gelirse Excel açar, 20 sütun bakar, "VERBİS'le uyumlu mu" sorar. Mosaik UI'da bu denetçi 5 dk içinde aynı bilgi + bonus reverse search + integrity check + audit log görür. Bu kazanım.
- 🟡 **Executor:** Pazartesi sabahı Faz 0 — Plan 38 EntityRelations entegrasyon dosyalarını oku, Process entity skeleton + Migration 66 yaz. 4-6 saat.

---

## 4. Mimari

### 4.1 Veri modeli — Core entity'ler

```sql
-- Central
CREATE TABLE Processes (
    Id INT IDENTITY PK,
    FirmaId INT NOT NULL,                      -- multi-tenant
    Department NVARCHAR(120) NOT NULL,
    Unit NVARCHAR(120) NULL,
    Owner NVARCHAR(200) NULL,                  -- Süreç Sahibi (rol/kişi)
    Name NVARCHAR(300) NOT NULL,               -- Faaliyet Adı
    Purpose NVARCHAR(MAX) NOT NULL,            -- İşleme Amacı
    LegalBasisId INT NOT NULL FK,
    DataSource NVARCHAR(500) NULL,             -- Veri Kaynağı
    StorageMedium NVARCHAR(500) NULL,          -- Saklandığı Ortam
    AccessAuthority NVARCHAR(500) NULL,        -- Erişim Yetkisi
    RecipientGroups NVARCHAR(500) NULL,        -- Aktarılan Alıcı Grupları
    RetentionRuleId INT NULL FK,
    DisposalMethodId INT NULL FK,
    RiskLevel TINYINT NOT NULL DEFAULT 1,      -- 0 Düşük 1 Orta 2 Yüksek
    ReviewStatus TINYINT NOT NULL DEFAULT 0,   -- 0 Taslak 1 Birim onayı 2 KVKK onay 3 VERBİS yayında
    LastReviewedAt DATETIME2 NULL,
    LastReviewedBy INT NULL FK Users,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    INDEX IX_Processes_FirmaDept (FirmaId, Department),
    INDEX IX_Processes_Risk (FirmaId, RiskLevel)
);

CREATE TABLE DataElements (
    Id INT IDENTITY PK,
    ElementCode NVARCHAR(80) NOT NULL UNIQUE,  -- "person.fullname", "biometric.fingerprint"
    DisplayName NVARCHAR(200) NOT NULL,        -- "Ad-Soyad"
    DataCategoryId INT NOT NULL FK,
    IsSpecialCategory BIT NOT NULL,            -- m.6 mı
    DefaultRetentionHint NVARCHAR(200) NULL,
    DefaultLegalBasisHint INT NULL FK LegalBasis,
    Aliases NVARCHAR(500) NULL,                -- arama için ("isim,ad,name,...")
    IsActive BIT NOT NULL DEFAULT 1,
    INDEX IX_DataElements_Search (DisplayName, Aliases)
);

CREATE TABLE ProcessDataLinks (
    Id INT IDENTITY PK,
    ProcessId INT NOT NULL FK Processes,
    DataElementId INT NOT NULL FK DataElements,
    UsageType TINYINT NOT NULL DEFAULT 0,      -- 0 collects 1 stores 2 transfers 3 derives
    Notes NVARCHAR(500) NULL,
    UNIQUE (ProcessId, DataElementId, UsageType),
    INDEX IX_PDL_Element (DataElementId)        -- reverse lookup hot path
);

CREATE TABLE CrossBorderTransfers (
    Id INT IDENTITY PK,
    ProcessId INT NOT NULL FK Processes,
    RecipientName NVARCHAR(300) NOT NULL,      -- "LinkedIn Inc."
    Country NVARCHAR(120) NOT NULL,            -- "ABD"
    Mechanism TINYINT NOT NULL,                -- 0 yeterlilik 1 SCC 2 BCR 3 taahhütname 4 arızi açık rıza 5 sözleşme ifası
    LegalReference NVARCHAR(200) NULL,         -- "m.9/2-a Standart Sözleşme"
    DocumentLink NVARCHAR(500) NULL,           -- SCC PDF
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

**REF lookup tabloları** (DataCategory, PersonGroup, LegalBasis, RetentionRule, DisposalMethod, MeasureStandard) — xlsx REF sheet'lerinden seed.

**DSAR + Breach** (m.13, m.12/5) Plan 36 Workflow Engine üzerinden — ayrı entity'ler ama state machine engine'e devredilir.

### 4.2 EntityRelations entegrasyonu (Plan 38)

Her `Process` aşağıdaki RelationType'lar ile EntityRelations'a kayıt düşer:

```
Process(SourceType="KvkkProcess", SourceId=N)
  → DataElement     RelationType="processes"      (junction'a paralel)
  → Sop             RelationType="derivedFrom"    (Plan 34)
  → WorkflowDef     RelationType="executedBy"     (Plan 36)
  → Form            RelationType="consumes"       (Plan 41 adayı, geçici string)
  → Document        RelationType="documentedBy"   (Documents)
  → User            RelationType="ownedBy"        (Süreç Sahibi)
  → Department      RelationType="appliesTo"
  → DisclosureNotice RelationType="discloses"     (Aydınlatma Metni)
```

Reverse query örneği:

```sql
-- "person.fullname" nerelerde işleniyor?
SELECT er.SourceType, er.SourceId, /* + entity name lookup */
FROM EntityRelations er
WHERE er.TargetType='DataElement' AND er.TargetId=@adSoyadId
  AND er.RelationType='processes';
```

### 4.3 AI Integrity Checker — 8 Pattern

Skill'in **Pattern Library** bölümü runtime `KvkkIntegrityChecker` servisine pattern olarak gömülür:

| Pattern | Detection | Severity |
|---|---|---|
| 1 Kopyala-yapıştır amaç | 3+ süreçte aynı `Purpose` string (fuzzy match >85%) | İdari |
| 2 Açık rıza ↔ kanuni yükümlülük çakışması | Bordro/SGK süreçlerinde `LegalBasisId == m.5/1` | İdari (yüksek) |
| 3 CCTV > 60 gün | `DataElement.code='cctv.video'` & saklama > 60 gün | İdari |
| 4 Envanter ↔ Aydınlatma metni uyumsuzluğu | Process.Purpose içeriği DisclosureNotice'da yok | Kritik |
| 5 Aday CV > 2 yıl | PersonGroup="ÇalışanAdayı" & saklama > 2 yıl | İdari |
| 6 Yurt dışı SaaS gizli aktarım | StorageMedium "Microsoft 365 / Google / AWS / Azure" var & CrossBorderTransfers boş | Kritik |
| 7 Etik hat / ihbar süreci yok | Hukuk departmanında "ihbar" / "whistleblower" / "etik hat" geçen süreç yoksa | İdari |
| 8 İhlal yönetim süreci yok | Hukuk departmanında "ihlal" / "breach" süreç yoksa | İdari (yüksek) |

Cron job (Hangfire): günlük tarama → `KvkkIntegrityFinding` tablo → dashboard widget + email alert (Plan 31 SMTP caller).

### 4.4 Xlsx import (Faz 1)

`XlsxImporter` servis (ClosedXML — zaten kullanılıyor):

1. Workbook açar (`Başla Burada` sheet header validation)
2. REF sheet'leri → lookup tablolarına idempotent UPSERT (`MERGE`)
3. `Tüm Envanter` sheet → `Processes` tablosuna UPSERT (`Name+Department+FirmaId` natural key)
4. Her satırda 20 sütun parse → entity fields
5. Veri Türü Detay parse → DataElement detect (alias match) → `ProcessDataLinks` insert
6. Yurt Dışı Aktarım parse → `CrossBorderTransfers` insert
7. EntityRelations kayıt (8 RelationType)
8. AuditLog: `kvkk_xlsx_import` event
9. Sonuç: insert/update/skip sayıları + error listesi

### 4.5 UI özellikleri

**KVİE Process Index:**
- Filter: Departman, Risk seviyesi, Özel nitelikli (yes/no), Yurt dışı aktarım (yes/no), Review status
- Search: Süreç adı, amaç, sahip
- Sütunlar: Sıra, Departman, Süreç Adı, Risk badge, Veri kategori count, Hukuki sebep, Saklama, Review status, Son inceleme tarihi
- Bulk action: Birim onayı toplu gönder, VERBİS yayına al

**Process Detail (6 aspect tab):**
1. **KVKK** — 20 sütun (xlsx parite)
2. **Workflow** — Plan 36 WorkflowDefinition link + step preview
3. **Form** — bağlı formlar (geçici string, Form Builder gelince typed)
4. **SOP** — bağlı SOP procedure'lar (Plan 34)
5. **Audit** — son 30 gün AuditLog entries
6. **Kitapçık** — bağlı Documents

**Sidebar Reverse Search:**
- Üst arama kutusu: "ad soyad" / "parmak izi" / "iban" / "tc kimlik" / "cctv"
- Modal:
  - Sol: DataElement kartı (kategori, özel nitelikli badge, varsayılan sebep, alias listesi)
  - Sağ: bağlı kayıtlar gruplu (SOP / Form / Süreç / Doküman / Sözleşme), her satır link
  - Üst: uyumluluk skoru ("18 süreçte; 3'ünde hukuki sebep eksik, 1'inde aydınlatma metni atlamış")

**Risk Dashboard (xlsx Risk Özeti karşılığı):**
- Departman bazlı Yüksek/Orta/Düşük sayım (Risk Özeti sheet 17 satır parite)
- DataElement bazlı yayılım haritası (en çok işlenen 10 veri öğesi)
- Açık integrity findings count (severity bazlı)
- 72h breach timer (aktif breach varsa)
- KVKK m.13 başvuru SLA dashboard

---

## 5. Riskler

| Risk | Olasılık | Etki | Önlem |
|---|---|---|---|
| Plan 38 EntityRelations omurgası sözleşmesi yanlış | Düşük | Yüksek | Plan 38 onaylı + ilk büyük tüketici; sözleşmeyi KVKK ile sertifiye et |
| Xlsx import sırasında karakter encoding bozulma | Orta | Orta | ClosedXML default UTF-8, test split data sample önce import |
| DataElement alias match false-positive (kategori yanlış bağlama) | Orta | Düşük | Import sonrası review screen, kullanıcı confirm; manual override |
| Form Builder yokken Process ↔ Form bağı zayıf | Yüksek | Düşük | Geçici string link (Plan 41 Form Builder gelince typed migration) |
| AI integrity checker false-positive yorgunluğu | Orta | Orta | Severity threshold + dismiss + per-pattern enable/disable + retest scheduling |
| BKM Kitap v7 xlsx içeriği hassas | Orta | Yüksek | Repo'ya commit ETME; sadece `/d/Downloads/`'tan import; DB seed encrypt değil ama prod backup şifreli |
| VERBİS export format değişimi | Düşük | Düşük | Kurul rehber sürümü versiyonu seed; rehber değişince template güncelle |
| KvkkAdvisor LLM token maliyeti | Orta | Düşük | FallbackLlmService rate limit (Plan 16.5 D-02 mevzu); cache prompt; on-demand only |
| Multi-firma KVİE template farklılaşması | Düşük | Orta | FirmaId per-row; Belinza kendi envanteri ayrı |
| Process omurgası SOP/Form/Workflow modüllerinin onaylanma sürelerine bağımlı | Yüksek | Orta | Faz 2 (CRUD UI) Workflow/Form/SOP olmadan da çalışır; aspect tab'ları "Henüz bağlı yok" gösterir |

---

## 6. Done Criteria

### Faz 0 — Veri modeli omurgası
- [ ] Migration 66 (schema) idempotent, build yeşil
- [ ] Migration 67 (REF lookup seed) — 5 REF sheet → tablo karşılığı
- [ ] Migration 68 (DataElement seed ~60 öğe)
- [ ] `Process`, `DataElement`, `ProcessDataLink`, `CrossBorderTransfer` entity + DbSet
- [ ] EntityRelations ilk yazıcı pattern dokuman (Plan 38 sözleşme test)
- [ ] Smoke test: en az 5 Process insert + reverse lookup query
- [ ] Test: 5+ unit test (entity validation, reverse lookup, EntityRelations hook)

### Faz 1 — Xlsx import
- [ ] `XlsxImporter` servis: idempotent UPSERT
- [ ] BKM Kitap v7 xlsx → 361 süreç import (test environment)
- [ ] AuditLog `kvkk_xlsx_import` event
- [ ] Import sonuç raporu (insert/update/skip/error)
- [ ] Re-import: aynı dosya 2. kez çalıştırıldığında 0 değişiklik
- [ ] Test: en az 3 integration test (sample xlsx)

### Faz 2 — KVİE CRUD UI
- [ ] ProcessController: Index/Edit/Details/Create/Delete
- [ ] Index filter + search çalışır
- [ ] Edit 20 sütun form, AntiForgery + AsNoTracking standard
- [ ] Detail 6 aspect tab (KVKK + 5 stub "Henüz bağlı yok")
- [ ] Birim müdürü onay workflow (Plan 36 reuse) — `ReviewStatus` 0→1→2→3 state machine
- [ ] WCAG: dialog focus trap, scope th, aria-label
- [ ] Multi-firma `IUserDataScope` filter
- [ ] Test: 8+ unit test (controller, service, workflow trigger)

### Faz 3 — Workflow + Form bağlama
- [ ] Workflow aspect tab: WorkflowDefinition picker + step preview
- [ ] Form aspect tab: geçici string link + DataElement mapping uyarısı
- [ ] EntityRelations kayıt her bağlamada
- [ ] DSAR (m.13) Plan 36 engine üzerinde — 30 gün SLA timer
- [ ] Breach (m.12/5) Plan 36 + Hangfire 72h timer

### Faz 4 — SOP entegrasyonu (Plan 34 bağımlılık)
- [ ] SOP detail page Process picker zorunlu (yeni SOP)
- [ ] SOP content AI scan → DataElement detect + öneri panel
- [ ] EntityRelations otomatik bağlama (`derivedFrom`)
- [ ] Bütünlük check: SOP'ta veri öğesi geçiyor ama Process'te yok → uyarı

### Faz 5 — Global reverse search
- [ ] Sidebar üst arama kutusu (her sayfada görünür)
- [ ] DataElementController.ReverseSearch endpoint
- [ ] Modal: sol DataElement kart, sağ tüketici listesi gruplu
- [ ] EntityRelations query 1-hop (3-hop versiyonu Plan 38 v2)
- [ ] Test: en az 5 DataElement için reverse query unit test

### Faz 6 — AI Integrity Checker
- [ ] `KvkkIntegrityChecker` 8 pattern detector
- [ ] Hangfire daily job
- [ ] `KvkkIntegrityFinding` entity + dashboard widget
- [ ] Severity threshold + per-pattern enable/disable
- [ ] Plan 31 SMTP caller — kritik findings için email digest
- [ ] Test: 8 pattern için unit test (positive + negative case)

### Faz 7 — VERBİS export + aydınlatma metni versioning
- [ ] `VerbisExporter` ClosedXML — Mart 2025 rehber 20 sütun template
- [ ] Aktif + birim onaylı süreçler export edilir
- [ ] `DisclosureNotice` versioning entity
- [ ] Sürüm karşılaştırma (diff view)
- [ ] Periyodik 6 ay review reminder + sign-off log

### Faz 8 — Risk dashboard
- [ ] Departman bazlı Yüksek/Orta/Düşük chart (xlsx Risk Özeti parite)
- [ ] DataElement yayılım haritası (top 10)
- [ ] Açık findings sayım
- [ ] 72h breach + DSAR SLA widget'ları
- [ ] Reports modülü reuse — DashboardConfigJson template

### Genel
- [ ] Build: 0 uyarı 0 hata
- [ ] Test: tüm yeni test'ler yeşil + 387 mevcut bozulmamış
- [ ] ARCHITECTURE_MAP refresh
- [ ] CLAUDE.md modül listesine `Kvkk` ekle
- [ ] VISION.md §7 entegrasyon notu
- [ ] ADR-019 yazılır: "KVKK Process Backbone — Plan 38 EntityRelations ilk büyük tüketici"

---

## 7. Rollback

- Plan 38 EntityRelations dokunulmaz (omurga sözleşmesi)
- Migration 66-69 idempotent + reverse migration hazır (DROP TABLE'ler)
- Mosaik.Modules.Kvkk csproj reference Mosaik.csproj'dan kaldırılırsa modül devre dışı (ADR-002 modular monolit)
- Xlsx import dataset'i ayrı `IsImported=1` flag — rollback için bulk delete

---

## 8. Adımlar (granüler)

```
Faz 0 — Veri modeli omurgası (6-8h)
  0.1 Mosaik.Modules.Kvkk csproj + KvkkModule.cs scaffold
  0.2 Entity 14 dosya (Process, DataElement, junction, lookups, breach/dsar/finding)
  0.3 Migration 66 schema (idempotent IF NOT EXISTS)
  0.4 Migration 67 REF lookup seed (xlsx REF sheet 5 set)
  0.5 Migration 68 DataElement seed (~60 öğe + alias)
  0.6 IEntityRelationService hook (Plan 38 sözleşme test)
  0.7 5+ unit test
  0.8 Build + test yeşil + commit

Faz 1 — Xlsx import (4-6h)
  1.1 XlsxImporter servis (ClosedXML)
  1.2 REF + Tüm Envanter parse
  1.3 DataElement alias match (fuzzy)
  1.4 CrossBorderTransfer extract
  1.5 EntityRelations 8 RelationType insert
  1.6 Idempotent UPSERT + audit log
  1.7 Migration 69 (BKM v7 ilk yükleme)
  1.8 3+ integration test
  1.9 Commit

Faz 2 — KVİE CRUD UI (12-15h)
  2.1 ProcessController + view'lar
  2.2 Index filter + search
  2.3 Edit form + 20 alan
  2.4 Detail 6 aspect tab (5 stub)
  2.5 ReviewStatus workflow (Plan 36)
  2.6 IUserDataScope filter
  2.7 WCAG + a11y
  2.8 8+ unit test
  2.9 Commit

Faz 3 — Workflow + Form bağlama (10-12h)
  3.1 WorkflowDefinition picker
  3.2 Form string link (geçici)
  3.3 EntityRelations otomatik
  3.4 DSAR controller + 30-gün SLA
  3.5 Breach controller + 72h Hangfire
  3.6 Plan 31 SMTP caller (DSAR + Breach kanalı)
  3.7 Commit

Faz 4 — SOP entegrasyonu (6-8h, Plan 34 başlamasını bekler)
  4.1 SOP detail Process picker
  4.2 AI scan DataElement detect
  4.3 EntityRelations otomatik
  4.4 Bütünlük uyarısı
  4.5 Commit

Faz 5 — Global reverse search (8-10h)
  5.1 DataElementController.ReverseSearch
  5.2 Sidebar üst arama kutusu (Layout edit)
  5.3 Modal UI
  5.4 EntityRelations 1-hop query
  5.5 5+ unit test
  5.6 Commit

Faz 6 — AI Integrity Checker (12-15h)
  6.1 KvkkIntegrityChecker 8 pattern
  6.2 Hangfire daily job
  6.3 KvkkIntegrityFinding entity + dashboard
  6.4 Severity + dismiss + per-pattern toggle
  6.5 Email digest (Plan 31 caller)
  6.6 16+ unit test (8 pattern × 2 case)
  6.7 Commit

Faz 7 — VERBİS export + aydınlatma metni (6-8h)
  7.1 VerbisExporter ClosedXML template
  7.2 DisclosureNotice versioning + diff view
  7.3 6-ay review reminder cron
  7.4 Commit

Faz 8 — Risk dashboard (8-10h)
  8.1 Departman risk chart (Reports modülü reuse)
  8.2 DataElement yayılım
  8.3 Findings + breach + DSAR widget'ları
  8.4 Commit

Genel kapanış
  9.1 ARCHITECTURE_MAP refresh
  9.2 CLAUDE.md modül listesine Kvkk
  9.3 VISION.md §7 entegrasyon notu
  9.4 ADR-019 yaz
  9.5 Plan 40 arşivle
```

---

## 9. Cross-reference

- **Skill kaynak:** [`.claude/skills/kvkk-veri-envanteri/SKILL.md`](../.claude/skills/kvkk-veri-envanteri/SKILL.md) — 374 satır, 14 bölüm, KVKK Mart 2025 rehber + Pattern Library 8 madde
- **Veri kaynak:** `D:\Downloads\BKM_KVKK_Veri_Envanteri_v7_DENETIM_HAZIR.xlsx` — 361 süreç × 20 sütun, 17 departman, 5 REF, Risk Özeti dashboard
- **Plan 38** — `plans/38-entity-relations-decision-log.md` (omurga, ✅ onaylı)
- **Plan 34** — `plans/34-sop-prosedur-yonetimi.md` (SOP, ✅ onaylı, başlamadı)
- **Plan 36** — `plans/36-workflow-designer-onay-akislari.md` (Workflow Engine, ✅ onaylı)
- **Plan 31** — SMTP altyapı (✅ var, caller bekliyor — Plan 32)
- **Plan 16.5** — `Mosaik.Core` AI kit (yarım, D-02 SOP öncesi temizlik)
- **ADR-002** — Modular monolit (ayrı csproj zorunlu)
- **ADR-014** — Frontend stack (Alpine + Vanilla, htmx ertelenmiş)
- **ADR-015** — Yeni modüller ayrı assembly
- **ADR-018** — IMosaikModule capability evrim (TASLAK, Plan 37 ilk somut kullanım)
- **VISION §4.2** — Comment / Mention (paralel iş, KVKK process'ine yorum)
- **VISION §7.3** — Company Memory (EntityRelations omurga, KVKK ilk büyük tüketici)
- **VISION §7.5** — Dynamic Dashboard (Risk Özeti widget'ları)
- **VISION §7.6** — No-Excuse Platform (DSAR + Breach SLA timer)

---

## 10. Karar gerekiyor (onay öncesi)

1. **Form Builder ayrı plan adayı (Plan 41)** — Faz 3 form link şimdilik string, Form Builder yazılınca typed. Sıralama: SOP (Plan 34) → KVKK (Plan 40) → Form Builder (Plan 41) → Comment/Mention (Plan 35)?
2. **BKM Kitap v7 xlsx repo'ya commit?** — Hassas içerik. **Önerim: ETME.** Sadece `D:\Downloads`'tan import; import script seed migration olarak DB'de kalır.
3. **Multi-firma template** — Belinza ayrı envanter, BKM Kitap ayrı. `FirmaId` zorunlu, seed sadece BKM Kitap için. Belinza yeni firma seed sırasında yazılır.
4. **Kvkk modülü ad** — `Mosaik.Modules.Kvkk` (kısa) vs `Mosaik.Modules.DataProtection` (i18n için soyut). **Önerim: Kvkk** — BKM iç portal, Türkçe semantik daha doğru.
5. **Plan 40 sıralaması** — Plan 34 SOP'tan **sonra** mı, **paralel** mi? Bağımlılık tek yönlü (KVKK→SOP entegrasyonu Faz 4). KVKK Faz 0-3 SOP'sız çalışır. **Önerim: paralel başla**, Faz 4'te SOP olmazsa stub kalır.

---

## 11. Sürüm

- **2026-05-21:** Taslak. Onay bekliyor.
