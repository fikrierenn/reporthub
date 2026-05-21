# vNext Kalbi Bağımlılık Grafiği + İmplementasyon Sırası

**Tarih:** 2026-05-21
**Kapsam:** Plan 32, 34, 35, 36, 37, 38, 40, 41, 42 + ADR-018/019/020/021
**Kaynak:** `docs/RESEARCH_OSS_VNEXT_2026-05-21.md` + plan dosyaları + `docs/VISION.md` §4.2 altılı kalp

---

## 1. Bağımlılık Grafiği (Yönlü DAG)

```
                          ┌────────────────────────────────────┐
                          │ Plan 38 EntityRelations + Decision │ ✅ ONAYLI
                          │ (2026-05-21)                       │
                          └─────────┬──────────────────────────┘
                                    │ tüm yeni planlara polymorphic linker
        ┌───────────────┬───────────┼──────────┬──────────────┐
        ▼               ▼           ▼          ▼              ▼
┌──────────────┐ ┌─────────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐
│ Plan 34 SOP  │ │ Plan 36     │ │ Plan 40  │ │ Plan 41  │ │ Plan 35      │
│ ✅ ONAYLI    │ │ Workflow    │ │ KVKK     │ │ Form     │ │ Comment/     │
│              │ │ + Stateless │ │ Envanter │ │ Builder  │ │ Mention      │
│ Tamim reuse  │ │ ✅ ONAYLI   │ │ (rev 2)  │ │ ⚠️ KRİTİK│ │ (taslak)     │
│ %80          │ │ rev 5-21    │ │ ⏳ TASLAK│ │ PATH     │ │              │
└──────┬───────┘ └──────┬──────┘ └────┬─────┘ └────┬─────┘ └──────┬───────┘
       │                │             │            │              │
       │ Plan 40 Faz 3  │             │ form       │ Plan 42     │ Plan 31
       │ entegrasyon    │ Plan 41+42  │ aspect     │ form        │ SMTP
       │                │ trigger     │ stub→typed │ altyapı     │ caller
       │                │             │            │              │
       └──────────┬─────┴─────┬───────┴──────┬─────┴──────────────┘
                  ▼           ▼              ▼
                  ┌──────────────────────────────────┐
                  │ Plan 42 Process Execution        │
                  │ Runtime (BİRLEŞTİRİCİ)           │
                  │ ⏳ TASLAK                        │
                  │ gerektirir: 34 + 36 + 40 + 41    │
                  │           + 38 + 31 + Documents  │
                  └────────┬─────────────────────────┘
                           │ Plan 42 büyük tüketici
                           ▼
                  ┌──────────────────────────────────┐
                  │ Plan 37 Unified Inbox            │
                  │ ⏳ TASLAK (ADR-018 bağımlı)      │
                  └──────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│ Plan 31 SMTP altyapısı ✅ + Plan 32 caller ⏳ 6 açık soru│
│ → Plan 35 Comment + Plan 42 result email + Plan 40 digest│
└──────────────────────────────────────────────────────────┘
```

## 2. Bağımlılık Matrisi

| Plan | Bağımlı olduğu | Bağımlı olan | Bağımsız Faz'lar | Blocking Faz'lar |
|---|---|---|---|---|
| **Plan 38** EntityRelations | — | 34, 35, 36, 40, 41, 42, 37 | tümü (✅ onaylı) | — |
| **Plan 31** SMTP altyapı | — | 32, 35, 40 Faz 5, 42 | tümü (✅) | — |
| **Plan 32** SMTP caller | 31 (✅) | 35, 40 Faz 5 digest, 42 result email | — | tüm Faz'lar 6 açık soru bekler |
| **Plan 34** SOP | 38 (✅), Tamim altyapı (✅) | 40 Faz 3 | Faz A-D (bağımsız) | — |
| **Plan 35** Comment/Mention | 38 (✅), 31+32, INotificationService | — | Polymorphic Comments tablo (38 hazır) | Plan 32 caller bekler |
| **Plan 36** Workflow + Stateless | 38 (✅), Hangfire (✅) | 41 Faz 7, 42 Faz 1, 40 stub | Faz A (Stateless + designer) | — |
| **Plan 37** Unified Inbox | ADR-018, 36 ApprovalRequest, IInboxProvider | 42 (büyük tüketici) | ADR-018 onay sonrası | ADR-018 + 36 |
| **Plan 40** KVKK (rev 2 dar) | 38 (✅) | 42 KvkkContext aspect | Faz 0-2 (model + xlsx + CRUD) | Faz 3 → 34, Faz 4 → 38 reverse search, Faz 5 → 31+32+DikkatIQ, Faz 7 → 42 KvkkProcessingActivity |
| **Plan 41** Form Builder ⚠️ | 38 (✅), 36 trigger, Documents file upload | 40 form aspect, 42 form aspect | Faz 0-3 (scaffold + render + admin + public) | Faz 4 file → Documents (✅), Faz 7 trigger → 36 |
| **Plan 42** Process Runtime | 34, 36, 40 tanım, 41 ⚠️, 38 (✅), 31, Documents | 37 IInboxProvider | Faz 0 (scaffold) | Faz 1 → 36, Faz 2 → 41 public token, Faz 3 → 41 inbox, Faz 5 → 36, Faz 6 → 40 |

## 3. Kritik Path Analizi

**Kritik path:** Plan 41 Form Builder. En uzun + en çok bağımlı olan.

```
Plan 38 ✅ → Plan 41 Faz 0-3 (~32-40h) → Plan 42 Faz 0-3 (~30-40h) → Plan 42 Faz 4-9 (~34-44h)
                                       ↑
                               Plan 36 Faz A (~8h) gerekir
                                       ↑
                               Plan 40 Faz 0-2 (~22-29h) paralel
```

**Kritik path toplam:** ~94-124h (Plan 41 başlangıçtan Plan 42 bitişe).

**Paralel hatlar (kritik path'i blok etmez):**
- Plan 34 SOP Faz A-D — bağımsız (Tamim reuse, ~30-50h)
- Plan 35 Comment/Mention — Plan 32 caller bittiğinde başlar
- Plan 36 Faz B-D (designer UI + entegre) — Plan 41 Faz 3 sonrası
- Plan 40 Faz 5-7 (AI Integrity + VERBİS + dashboard) — Plan 42 Faz 6 sonrası

## 4. İmplementasyon Sırası (Hafta Hafta)

### Önce — Bütçe + Onay (Hafta 0)

Bekleyen kararlar:
- [ ] **Plan 40 onay** (5 madde §10)
- [ ] **Plan 41 onay** (5 madde §10)
- [ ] **Plan 42 onay** (5 madde §10)
- [ ] **ADR-018 onay** (IMosaikModule capability evrim)
- [ ] **ADR-019 onay** (Stateless workflow)
- [ ] **ADR-020 onay** (SurveyJS Form Builder Hybrid)
- [ ] **ADR-021 onay rev 2** (Gotenberg + MigraDoc + QRCoder hibrit — $0 lisans, QuestPDF Pro reddedildi)
- [ ] **Plan 32 SMTP caller** 6 açık soru cevap (Email Distribution scope)
- [ ] **ReportType obsolete cleanup** karar (Migration 19?)

Aksiyon paketi:
1. NuGet paketleri ekle: `Stateless 5.20.1` · `FuzzySharp` · `QuestPDF 2026.5.0` (+ Pro lisans key) · `DocumentFormat.OpenXml 3.5.1` · `OfficeIMO.Word 1.0.34` · `PDFsharp 6.2.0` · `Scriban >=7.2.0` · `Ical.Net 5.2.2` · `ZXing.Net` · opsiyonel `Microsoft.Playwright`
2. JS assets indir: `wwwroot/lib/surveyjs/` UMD v3.x · `signature_pad` v5.1.1 · `vis-timeline` v8.5.1 standalone · opsiyonel `easymde`, `cookieconsent`
3. KVKK referansları indir: `docs/kvkk-references/` (gitignored telif)
   - KVKK envanter xlsx (`kvkk.gov.tr/.../b5fe209d-...xlsx`)
   - Mart 2025 Rehber PDF (Yayın No 61)
   - VERBİS Kılavuz PDF
   - 6698 Kanun PDF (mevzuat.gov.tr)
4. Docker compose Presidio dev profil:
   - Analyzer + Anonymizer servis
   - Türkçe spaCy `tr_core_news_trf` Dockerfile bake
   - Custom recognizer: `TcKimlikRecognizer.py` (11-hane + Mod10/Mod11), `IbanTrRecognizer.py` (`^TR\d{24}$` + checksum)
5. `Mosaik.Core.AI/Privacy/PresidioClient.cs` HttpClient + DTO scaffold

### Hafta 1-2 — Foundation + Kritik Path Başlangıç

**Paralel başlat (3 hat):**

| Hat | Plan | Faz | Süre | Notlar |
|---|---|---|---|---|
| A | **Plan 32** SMTP caller | tam | 8-12h | Comment/Mention + Plan 42 result email + Plan 40 digest tetikleyici. Diğer planları blok ediyor. |
| B | **Plan 41** Form Builder | Faz 0-1 | 16-20h | KRİTİK PATH. Scaffold + JSON config v1 render+submit. SurveyJS bundle + signature_pad entegrasyon. |
| C | **Plan 36** Workflow | Faz A | 8h | Stateless WorkflowEngine + sequential-workflow-designer scaffold. Plan 41 Faz 7 + Plan 42 Faz 1 prereq. |

**Tamamlanması beklenen:** Plan 32 ✓, Plan 41 Faz 0-1 ✓, Plan 36 Faz A ✓.

### Hafta 2-3 — Plan 40 + Plan 34 paralel

| Hat | Plan | Faz | Süre | Notlar |
|---|---|---|---|---|
| A | **Plan 34** SOP | Faz A-B | 8-14h | Tamim altyapı %80 reuse. Bağımsız. |
| B | **Plan 41** Form Builder | Faz 2-3 | 16-20h | Admin CRUD + Public link + Anonim + AntiSpam. |
| C | **Plan 40** KVKK | Faz 0-1 | 10-14h | Mosaik.Modules.Kvkk + Migration 66/67/68 + xlsx import (BKM v7 361 süreç). |
| D | **Plan 35** Comment/Mention | başla | 8-12h | Plan 32 caller hazır artık. Polymorphic Comments tablo + INotificationService callback. |

### Hafta 3-4 — Plan 41 finalize + Plan 40 CRUD + Plan 36 designer

| Hat | Plan | Faz | Süre | Notlar |
|---|---|---|---|---|
| A | **Plan 41** Form Builder | Faz 4-5 | 12-16h | File upload + Signature + DataElement mapping + Şifreli alan AES |
| B | **Plan 40** KVKK | Faz 2 | 12-15h | KVİE CRUD UI (Index filter + 20 sütun Edit + 6 aspect Detail) |
| C | **Plan 36** Workflow | Faz B-C | 14-18h | Designer UI + Obligations/Contracts entegrasyon |
| D | **Plan 35** Comment/Mention | tamam | 12-16h | Tamim/Doküman/Sözleşme/OrgChart entegrasyon her biri 1 gün |

### Hafta 4-5 — Plan 41 tamamla + Plan 40 SOP entegre + ADR-018

| Hat | Plan | Faz | Süre | Notlar |
|---|---|---|---|---|
| A | **Plan 41** Form Builder | Faz 6-7 | 10-14h | Submission admin + Export + 8 template seed (DSAR/İhbar/Aday/İhlal/Rıza/Engelli/DPA/Review) |
| B | **Plan 40** KVKK | Faz 3-4 | 14-18h | SOP entegrasyonu (Plan 34 hazır) + Global reverse search sidebar |
| C | **Plan 36** Workflow | Faz D | 4-6h | SOP entegrasyon (Plan 34 sonrası) |
| D | **Plan 34** SOP | Faz C-D | 22-36h | Tam SOP CRUD + version + onay + okundu disiplini |
| E | **ADR-018** onay sonrası → Plan 37 Inbox başla | Faz 0-1 | 8-12h | IInboxProvider + IWidgetProvider + ICatalogProvider opt-in |

### Hafta 6-8 — Plan 42 Process Execution Runtime (BİRLEŞTİRİCİ)

**Kritik moment:** Plan 41 ✓, Plan 36 ✓, Plan 40 Faz 0-4 ✓, Plan 34 ✓ — birleştirici başlar.

| Hat | Plan | Faz | Süre | Notlar |
|---|---|---|---|---|
| A | **Plan 42** Process Runtime | Faz 0-1 | 14-18h | Mosaik.Modules.ProcessRuntime scaffold + ProcessExecutionService + Workflow hookup (Plan 36 Stateless callback) |
| B | **Plan 42** Process Runtime | Faz 2-3 | 16-20h | Public + Anonim başlatma + Inbox 4 sekme + Instance Detail 6 aspect timeline (vis-timeline) |
| C | **Plan 42** Process Runtime | Faz 4-5 | 14-18h | SLA Timer Hangfire + Eskaltsiyon + Result Rendering (Gotenberg + MigraDoc + QRCoder + OpenXml + OfficeIMO) |

### Hafta 8-9 — Plan 42 finalize + Plan 40 KVKK closure

| Hat | Plan | Faz | Süre | Notlar |
|---|---|---|---|---|
| A | **Plan 42** Process Runtime | Faz 6-7 | 10-14h | KvkkProcessingActivity log + Retention sweep (Hangfire daily anonimleştir/sil) + ICS feed (Ical.Net) |
| B | **Plan 42** Process Runtime | Faz 8-9 | 10-14h | Process Owner Dashboard + Admin + E2E entegrasyon test (DSAR/İhbar/Aday/İhlal/Review) |
| C | **Plan 40** KVKK | Faz 5 | 12-15h | AI Integrity Checker 8 pattern (Plan 42 KvkkProcessingActivity tablosundan beslenir) + Hangfire daily + email digest |
| D | **Plan 37** Unified Inbox | Faz 2-3 | 10-14h | Plan 42 ProcessInstance büyük tüketici. |

### Hafta 10-11 — KVKK closure + borç temizliği

| Hat | Plan | Faz | Süre | Notlar |
|---|---|---|---|---|
| A | **Plan 40** KVKK | Faz 6-7 | 14-18h | VERBİS export ClosedXML (Mart 2025 rehber format) + aydınlatma metni versioning + Risk dashboard (xlsx Risk Özeti parite) |
| B | **Plan 33 Faz 4** D-01..D-06 | aktif | 38-52h | Documents Plan 27 Faz C + AI altyapı iyileştirme + Vision çoklaştırma + Test coverage + Tag Sistemi |

### Paralel (haftada 1-2 gün, blok değil)

- **Plan 18B** HR Sync (Zirve `vw_PersonelDepartman` günlük senkron, ~16-24h)

### Hafta 12+ — Yeni vNext

- Form/Anket Builder genişletme (Plan 41 v2 drag-drop builder UI)
- Documents iki-plan çakışma çözüm (Plan 27 vs Plan 19)
- VISION §7 Operational Intelligence layer pilot

---

## 5. Toplam Effort Özeti

| Plan | OSS reuse sonrası | Hafta |
|---|---|---|
| Plan 32 SMTP caller | 8-12h | 1 |
| Plan 34 SOP | 30-50h | 1-5 (paralel) |
| Plan 35 Comment/Mention | 12-16h | 2-4 |
| Plan 36 Workflow + Stateless | 22-32h | 1-5 |
| Plan 37 Unified Inbox | 18-26h | 5-9 (ADR-018 sonrası) |
| Plan 40 KVKK | 38-50h | 2-11 (faz'lara yayılı) |
| Plan 41 Form Builder | 38-48h | 1-5 (kritik path) |
| Plan 42 Process Runtime | 48-64h | 6-9 (birleştirici) |
| **vNext kalbi toplam** | **214-298h** | **11 hafta** |

(Önceki tahmin 236-319h baseline → 176-244h OSS reuse → 214-298h tam vNext + Plan 32+35+37 dahil)

**Paralel implementasyon yapıldığında:** ~11 hafta wall-clock.

---

## 6. Risk + Kritik Düğümler

### Yüksek risk düğümler

1. **Plan 41 Form Builder Faz 0-3 (~32-40h)** — Plan 42 başlamadan tamam olmalı. Kritik path.
   - **Önlem:** Hafta 1 başlamak şart, paralel Plan 32+36 yürümeli, gerekirse en az 3 hafta blok.

2. ~~**QuestPDF Pro lisans bütçesi $699/yıl**~~ **REV 3 ÇÖZÜLDÜ 2026-05-21:** Deep research ek agent → QuestPDF Pro reddedildi → **Gotenberg + MigraDoc + QRCoder + Razor.Templating.Core hibrit** kabul. $0 lisans + 3 yıl $2097 tasarruf + Razor reuse + digital signature native. ADR-021 rev 2. **Blocker yok.**

3. **Plan 32 SMTP caller 6 açık soru** — Plan 35 Comment/Mention + Plan 40 digest + Plan 42 result email hepsi buna bekler.
   - **Önlem:** Plan 32 sorular Hafta 0'da kapansın.

4. **ADR-018 IMosaikModule capability** — Plan 37 Unified Inbox unlock.
   - **Önlem:** Plan 37 kritik path değil; sonradan ekle.

5. **Plan 36 + Plan 42 birleşik Stateless mapper** — Designer JSON ↔ Stateless config drift.
   - **Önlem:** Mapper unit test Plan 36 Faz A done criteria; Plan 42 Faz 1 Workflow hookup smoke test.

### Düşük risk paralel hatlar

- **Plan 18B HR Sync** — bağımsız, haftada 1-2 gün
- **Plan 33 Faz 4** D-01..D-06 — Hafta 10+ borç temizliği
- **Plan 34 SOP Faz A-D** — Plan 40 Faz 3 dışında tamamen bağımsız

---

## 7. Önerilen Yarına Başlangıç Noktası

**Hafta 0 (bugün/yarın):**

1. **3 plan onayı:** Plan 40 (5 madde) + Plan 41 (5 madde) + Plan 42 (5 madde)
2. **3 ADR onayı:** ADR-019 (Stateless) + ADR-020 (SurveyJS) + ADR-021 rev 2 (Gotenberg + MigraDoc hibrit — $0 lisans)
3. **ADR-018 onayı:** IMosaikModule capability evrim
4. **Plan 32 SMTP caller** 6 açık soru cevap
5. **ReportType obsolete cleanup karar** (Migration 19 yazılsın mı)
6. **NuGet + JS + KVKK referansları indirme** (Hafta 0 sonu)
7. **Docker Presidio dev profil hazırlama**

**Hafta 1 başlangıç (paralel 3 hat):**
- Hat A: Plan 32 SMTP caller bitir
- Hat B: Plan 41 Form Builder Faz 0 başla (Mosaik.Modules.Forms scaffold + Migration 70)
- Hat C: Plan 36 Workflow Faz A başla (Stateless + sequential-workflow-designer scaffold)

---

## 8. Sürüm

- **2026-05-21:** İlk sürüm. 5 paralel agent OSS araştırması + 3 ADR + Plan 36/40/41/42 düzenlemesi sonrası bağımlılık + sıralama analizi.
