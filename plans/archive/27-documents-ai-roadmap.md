# Plan 27 — Documents & AI Roadmap

**Tarih:** 2026-05-10
**Yazan:** Fikri / Claude
**Durum:** ⚠️ **KISMI IMPLEMENT** — 2026-05-14 Plan 33 keşfinde doğrulandı:
- **Faz A (Akıllı Extraction) %80** — PageImportanceScorer ✅, Tesseract preprocessing ✅, DPI 300 + tur+eng ✅, per-page confidence gate ✅, smart page seçim + vision rescue ✅. **Eksik:** A-06 field-specific 10 prompt registry, A-07 ContractExtractionValidator, A-08 Stage 3 doğrulama → Plan 33 Faz 2 C7
- **Faz B (Quick AI Wins) %70** — DocumentInsightService (B-01+B-03 merge) ✅, upload entegrasyon ✅, AskDocument + DocumentChatService MVP ✅. **Eksik:** B-01/B-03 ayrı servis, map-reduce uzun dokümanlar
- **Faz C (DMS Foundation) %0** — versioning, App_Data taşıma, FTS, metadata, permission, audit → Plan 33 Faz 4 D1 (büyük borç, 20-28sa)
- **Faz D (Compliance) %0** — retention, legal hold, e-imza, KEP — uzun vadeli backlog
- **Faz E (RAG) %0** — vector store, cross-doc chat — uzun vadeli

**Kritik güvenlik açığı:** DocumentsController.Upload wwwroot'a yazıyor, firma sınırı aşılabilir → Plan 33 BUGFIX-3 ile fix edilecek.

Plan 19 (Documents v2) bu plana absorbe edildi (Faz C). Plan 26 (OCR fallback) bu plana absorbe edildi (Faz A).
**Tier:** 3 (meta plan — kapsam 5+ klasör, schema değişiklikleri, harici dep, kullanıcı-görünür)

---

## 1. Problem

Bugünkü oturumda iki temel açık tespit edildi:

1. **Mosaik sözleşme analiz pipeline'ı eksik:** Plan 26 OCR fallback bitti ama 22 sayfalık Turkcell sözleşmesinde Tesseract `MaxPages=8` sınırı yüzünden §5 Süre/Fesih/Devir/Mücbir/Yetkili mahkeme tamamen kaçırıldı. AI ayrıca adres halüsinasyonu yaptı (Fatih, İstanbul → gerçek Bursa Osmangazi), Ticaret Sicil No'yu Vergi No olarak işaretledi, Sözleşme No alanını boş bıraktı (Abonelik No: 5320380620 görünür halde).

2. **Mosaik Documents modülü klasik DMS özelliklerinden yoksun:** Versiyonlama yok, full-text search yok, custom metadata yok, doküman-bazlı permission yok, retention/legal hold yok, e-imza/KEP yok, audit trail dar, share link yok, PII redaction yok. Plan 19 var ama dar scope (sadece versiyonlama + güvenli serve).

Bu iki açık tek bir koordineli yol haritasında çözülmeli — birçok pattern (auto-tag, summary, RAG) sözleşme pipeline'ını da besler.

## 2. Scope

### Kapsam dahili (5 faz)

**Faz A — Akıllı Sözleşme Extraction** (sözleşme pipeline iyileştirme)
- Page importance scoring (regex + TOC, kritik sayfalar garanti)
- Tesseract preprocessing (Otsu binarization + dilation, Türkçe karakter doğruluğu)
- DPI 200→300, dil `tur+eng` (İngilizce terimler için)
- Per-page OCR confidence gate → o sayfa için vision fallback
- Field-specific extractor registry (10 ayrı clause prompt'u, CUAD pattern)
- Stage 3 doğrulama çağrısı (null alanlar için targeted retry)
- JSON schema validation + 1 retry

**Faz B — Quick AI Wins** (1 günde implement)
- Auto-tagging (z.ai prompt → DocumentType/Year/Dept/Tags)
- Executive summary (map-reduce, uzun sözleşmeler için)
- Single-doc chat (PDF metni <32K token ise direkt drawer, RAG'sız MVP)

**Faz C — DMS Foundation** (klasik DMS taban)
- Versiyonlama + check-in/check-out (Plan 19 absorbe edilir)
- App_Data'ya taşıma + controller serve (Plan 25.1 Faz 1 ile birleştir)
- Full-text search (SQL Server FT index, Türkçe stoplist)
- Custom metadata (EAV light: `DocumentMetadataField` + `DocumentMetadataValue`)
- Doküman/klasör seviyesi permission (`DocumentPermission(Subject, Doc/Folder, Level, ValidUntil?)`)
- Audit trail extend (`doc_view`, `doc_download`, `doc_print`, `doc_share` event tipleri)

**Faz D — Compliance** (TR mevzuat + KVKK)
- Retention policy + auto-dispose (`RetentionPolicy(EventType, Months, Action)`)
- Legal hold (`LegalHold(DocId, Reason, HoldUntil)` — delete blok)
- E-imza entegrasyonu (TÜBİTAK Kamu SM PKCS#7 + RFC 3161 timestamp)
- KEP entegrasyonu (PTT REST API outbound + alındı belgesi arşivi)
- PII redaction (Microsoft Presidio Python sidecar + custom TR recognizer)
- Dinamik watermark (kullanıcı-spesifik PDF overlay)
- Share link (token + expiry + opsiyonel password)

**Faz E — Advanced AI** (RAG + cross-doc analitik)
- Microsoft Kernel Memory + vector store (pgvector veya SQL Server 2025 native)
- Cross-doc chat ("geçen yıl X firmasıyla yapılan sözleşmelerin yükümlülükleri")
- Document diff + AI redline (DiffPlex + z.ai, "değişiklik kimin lehine?")
- Anomaly detection (Luminance pattern — standart sözleşme dilinden sapan madde)
- Hybrid search (BM25 + vector)

### Kapsam dışı

- **Real-time co-edit** (OnlyOffice lisans + karmaşık, ROI zayıf)
- **Outlook plugin** (yan iş, kapsam dışı)
- **Mobil scan/capture** (PWA + MediaDevices, ayrı plan)
- **GraphRAG / Neo4j knowledge graph** (Mosaik scale'inde overkill, Plan 25 olgunlaşınca tekrar değerlendirilir)
- **Annotation/comment** (Faz C+1, ayrı plan)
- **Email-in (mail→klasör)** (Faz D+1, ayrı plan)
- **e-Fatura/e-Defter** (muhasebe paketinin işi, sadece arşivleme Faz D kapsamında)

### Etkilenen dosyalar (tahmin — faz başına)

**Faz A** — 6 dosya / ~600 satır
- `Mosaik/Services/Ai/PageImportanceScorer.cs` (yeni, ~120 satır)
- `Mosaik/Services/Ai/TesseractOcrExtractor.cs` (preprocessing + per-page conf, +150 satır)
- `Mosaik/Services/Ai/AiExtractionWorker.cs` (smart page selection + schema validation, +100 satır)
- `Mosaik/Services/Ai/ExtractionPrompts.cs` (10 field-specific prompt, ~200 satır)
- `Mosaik/Services/Ai/ContractExtractionValidator.cs` (yeni, ~80 satır)
- `Mosaik/Mosaik.csproj` (NuGet: JsonSchema.Net opsiyonel)

**Faz B** — 4 dosya / ~250 satır
- `Mosaik/Services/Ai/DocumentClassifierService.cs` (yeni)
- `Mosaik/Services/Ai/DocumentSummaryService.cs` (yeni)
- `Mosaik/Controllers/DocumentsController.cs` (upload akışına entegrasyon)
- `Mosaik/Views/Ai/_DocumentChatDrawer.cshtml` (yeni, RAG'sız MVP)

**Faz C** — 12 dosya / ~1200 satır
- Yeni entity'ler: `DocumentVersion`, `DocumentMetadataField`, `DocumentMetadataValue`, `DocumentPermission`
- Migration 52-55
- Yeni controller endpoint'leri (check-in/out, version history, permission CRUD)
- `_AppLayout.cshtml` sidebar — doküman alt-menü

**Faz D** — 10 dosya / ~1000 satır
- E-imza: `Mosaik.Services.ESignature` (TÜBİTAK API client)
- KEP: `Mosaik.Services.Kep` (PTT API client)
- Presidio sidecar: `docker-compose.yml` + Python proje
- `RetentionPolicy`, `LegalHold` entity + service

**Faz E** — 8 dosya / ~800 satır
- Kernel Memory entegrasyon
- Vector store provider (pgvector veya SQL Server 2025)
- Chat UI + cross-doc

**Toplam tahmini:** 40 dosya / ~3900 satır / 4-6 hafta solo dev.

## 3. Alternatifler

### A: Tek monolitik plan (her şey bir branch)
**Açıklama:** Plan 27 altında tüm fazlar tek büyük PR.
**Reddetme sebebi:** Test edilemez, geri alınamaz, kullanıcı 4 hafta görmez.

### B: Her faz ayrı Plan dosyası (27.A, 27.B, 27.C, 27.D, 27.E)
**Açıklama:** Meta Plan 27 sadece roadmap, her faz için ayrı detay plan.
**Reddetme sebebi:** Plan-First overhead, tek geliştirici için aşırı bürokrasi. Plan 25 zaten içinde fazlar var.

### C: Tek Plan 27 + faz başına commit-split + faz tamamlanınca journal kararı (SEÇİLEN)
**Açıklama:** Tek plan, fazlar sıralı, her faz kendi sprint'i + commit-split + journal handoff. Faz biti = plan içinde ✅ işaretle.
**Sebep:** Plan 25 ile paralel pattern, kullanıcı her faz sonu somut çıktı görüyor. Plan dosya çoğalmıyor.

### D: Faz E (RAG) öncelikli — AI native ilk
**Açıklama:** Önce büyük AI sıçraması (Kernel Memory + cross-doc chat) yapılsın, klasik DMS sonra.
**Reddetme sebebi:** Foundation (versiyonlama, permission, audit) yokken RAG'ı production'a almak güvenlik açığı (yetkisiz kullanıcı tüm dokümanları sorgulayabilir). Faz C öncelikli.

> 🔴 **Contrarian:** "5 faz çok büyük plan, ortada bırakılır. 1-2 faz odaklan." Geçerli — Faz A+B (1 hafta) commit'le, gerisi backlog'a yaz.
> 🔵 **First Principles:** Asıl sorun "sözleşme pipeline'ı eksik" mi yoksa "kullanıcı her gün doküman bulamıyor mu?" Cevap: ikincisi. Faz C (search + metadata) Faz A'dan daha kullanıcı-değerli olabilir. Ama A bugünkü bug'ı çözüyor, öncelik korunuyor.
> 🟢 **Expansionist:** Bu plan Mosaik'i Türkiye'nin açık-kaynak DMS+AI ürünü yapmaya yakın. Plan 22 (toplantı) + Plan 28 (mesajlaşma) ile bağlanır, gerçek kurumsal portal olur. Brain vault'a synthesis dosyası yaz.
> ⚪ **Outsider:** Yabancı bir geliştirici Mosaik'e baksa "Neden e-imza/KEP/KVKK her yere yayılmış, ayrı modül değil?" derdi. Faz D'yi belki ayrı `Mosaik.Modules.Compliance` modülü olarak çıkar.
> 🟡 **Executor:** Pazartesi sabahı somut adım — `PageImportanceScorer.cs` yaz + Turkcell PDF'ini retry'la + 22 sayfanın hepsi kapsanıyor mu doğrula.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Faz A page scoring her sözleşme tipine uymaz | Orta | Orta | Fallback: skor algoritması başarısızsa eski "tüm sayfalar" yolu aktif. MaxPages 8→30 zaten kazanç |
| Tesseract preprocessing SkiaSharp Windows-only quirk | Düşük | Orta | CA1416 warning zaten var, prod Windows-only kabul |
| Field-specific 10 ayrı LLM çağrısı = rate limit | Yüksek | Yüksek | Queue mantığı zaten var. Birden fazla provider fallback (Plan 25.1 hardening) |
| Faz C: full-text Türkçe stoplist yetersiz | Düşük | Yüksek | Custom stoplist + lemmatization (Zemberek-NLP) opsiyonel |
| Faz D: e-imza entegrasyonu kurum sertifikası gerektirir | Yüksek | Yüksek | TÜBİTAK Kamu SM hesabı + test ortamı önce. PoC ile başla |
| Faz D: KEP outbound tek-tenant, multi-firma karmaşık | Orta | Düşük | İlk versiyonda tek KEP hesabı kabul |
| Faz E: pgvector eklemek = ek DB | Yüksek | Yüksek | Önce SQL Server 2025 native vector dene (Mosaik DB içinde kalır). Yetmezse pgvector sidecar |
| Faz E: embedding modeli token maliyeti | Düşük | Orta | z.ai embedding ucuz, multilingual-e5-large yerel de host edilebilir |
| Plan kapsamı 4-6 hafta = ortada bırakılma | Yüksek | Orta | **Faz A + B + C sprint** öncelikli — gerisi backlog. Her faz biti commit + journal |

## 5. Done Criteria

### Faz A
- [ ] Turkcell 22 sayfalık PDF'i retry'la — §5 maddeleri (süre/fesih/devir/mücbir/mahkeme) çıkarımda görünüyor
- [ ] PageImportanceScorer unit test'i (>5 farklı sözleşme tipi)
- [ ] Tesseract preprocessing açma/kapama config option
- [ ] Per-page confidence gate metric (kaç sayfa vision'a düşüyor)
- [ ] JSON schema validation: null alan için 1 retry, hâlâ boşsa null + audit

### Faz B
- [ ] Doküman yüklenince otomatik tag çıkıyor (DocumentType, Year, Dept, Tags)
- [ ] Liste tooltip executive summary gösteriyor
- [ ] Sözleşme detay sayfasında "Soru sor" drawer çalışıyor (RAG'sız, single-doc context)

### Faz C
- [ ] Versiyonlama: aynı dosyanın yeni hali yüklenince Version++, eski kayıt `IsCurrentVersion=0`
- [ ] App_Data'ya taşındı, wwwroot doğrudan erişim **kapalı**
- [ ] Full-text search: "fesih" sorgusu 100ms'de N sözleşme döndürüyor
- [ ] Custom metadata: kullanıcı yeni alan tanımlıyor (TextField, NumberField, DateField, LookupField)
- [ ] Doküman permission: rol+kullanıcı bazlı, tarih sınırlı erişim
- [ ] Audit: download/print/share event'leri ayrı kategoride loglanıyor

### Faz D
- [ ] Retention: 10 yıl sonra otomatik dispose (Hangfire job)
- [ ] Legal hold: hold süresince delete blok
- [ ] E-imza: TÜBİTAK Kamu SM test cert ile bir doc imzalanıyor
- [ ] KEP: PTT test ortamına outbound gönderim
- [ ] PII redaction: TC kimlik/IBAN/telefon maskeleniyor
- [ ] Watermark: kullanıcı-spesifik overlay

### Faz E
- [ ] Cross-doc chat: "geçen yıl X ile yapılan sözleşmeler" sorusu doğru cevap
- [ ] Hybrid search: BM25 + vector skoru harmanlanmış
- [ ] Document diff: iki versiyon arasındaki değişiklikler + AI yorumu
- [ ] Anomaly: standart dilden sapan madde işaretleniyor

## 6. Rollback Planı

- Her faz ayrı commit grubu → `git revert <commit>` ile geri al
- Faz A: `TesseractOcrExtractor` MaxPages=8 + smart paging kapatma config
- Faz B: AI servis kayıtlarını DI'dan kaldır, controller method'larını sil
- Faz C: Migration rollback script (kolon DROP), entity sil
- Faz D: Sidecar Docker stop, entegrasyon servisleri devre dışı
- Faz E: Kernel Memory init kapatma, vector store opsiyonel

## 7. Adımlar (öncelik sırası)

### Sprint 1 — Faz A (1 hafta) — **EN ÖNCELIKLI**

1. [ ] **A-01** PageImportanceScorer iskelet — keyword + skor algoritması, unit test
2. [ ] **A-02** TesseractOcrExtractor preprocessing — SkiaSharp Otsu binarization
3. [ ] **A-03** TesseractOcrExtractor DPI 300 + dil `tur+eng` + MaxPages 30 (geçici fallback)
4. [ ] **A-04** Per-page confidence gate — Tesseract `conf` skor + threshold
5. [ ] **A-05** AiExtractionWorker smart page seçim — scorer + low-conf sayfalar vision'a
6. [x] **A-06** ExtractionPrompts: field-specific prompt registry ✅ 2026-05-19 — 7 alan (counterparty, startDate, endDate, contractValue, jurisdiction, parties, obligations). `GetStage3Prompt(fieldName)` switch
7. [x] **A-07** ContractExtractionValidator — null kritik alan tespiti ✅ 2026-05-19 — 7 kritik alan; `NullFields` + `Warnings`. HasArray `>=` semantic
8. [x] **A-08** Stage 3 hedefli alan retry ✅ 2026-05-19 — maks 3 alan, JsonNode merge (null override izin), CT honor, ayrı catch, failedFields+skipped ErrorMessage'a
9. [ ] **A-09** Turkcell PDF smoke test — §5 maddeleri çıkıyor mu

### Sprint 2 — Faz B (2-3 gün)

10. [ ] **B-01** DocumentClassifierService — z.ai prompt + JSON schema (auto-tag)
11. [ ] **B-02** Upload akışına entegrasyon — yüklenince otomatik etiket
12. [ ] **B-03** DocumentSummaryService — single-pass (küçük) + map-reduce (büyük)
13. [ ] **B-04** Liste tooltip + detay sayfa özet
14. [ ] **B-05** Single-doc chat drawer — token <32K kontrol + direkt z.ai

### Sprint 3 — Faz C (1 hafta)

15. [ ] **C-01** DocumentVersion entity + migration
16. [ ] **C-02** Check-in/out servisi + UI
17. [ ] **C-03** App_Data taşıma + Download controller serve
18. [ ] **C-04** SQL Server FT index + search endpoint
19. [ ] **C-05** Metadata field/value entity + admin CRUD
20. [ ] **C-06** DocumentPermission entity + middleware
21. [ ] **C-07** Audit event tipleri extend

### Sprint 4 — Faz D (1-2 hafta)

22. [ ] **D-01** RetentionPolicy + Hangfire dispose job
23. [ ] **D-02** LegalHold entity + delete blok middleware
24. [ ] **D-03** TÜBİTAK Kamu SM e-imza PoC
25. [ ] **D-04** PTT KEP API client + outbound endpoint
26. [ ] **D-05** Presidio Python sidecar + Docker
27. [ ] **D-06** Watermark service (QuestPDF)
28. [ ] **D-07** Share link entity + token endpoint

### Sprint 5 — Faz E (2 hafta)

29. [ ] **E-01** Microsoft.KernelMemory NuGet + DI
30. [ ] **E-02** Vector store karar: SQL Server 2025 vs pgvector (PoC)
31. [ ] **E-03** Embedding pipeline (yüklenen doc → embed → store)
32. [ ] **E-04** Cross-doc chat UI
33. [ ] **E-05** Hybrid search (BM25 + vector skoru)
34. [ ] **E-06** Document diff servisi + redline UI
35. [ ] **E-07** Anomaly detection (standart sözleşme playbook + sapma skoru)

## 8. İlişkili Planlar

- **Plan 19** (Documents v2 — versiyonlama) → **Faz C absorbe eder**, Plan 19 arşive
- **Plan 25** (Sözleşme) — kaynak modül, Faz A+B sözleşme pipeline'ını besler
- **Plan 25.1** (Contract security hardening) — App_Data taşıma Faz C ile birleşir
- **Plan 26** (OCR fallback) — bugün tamamlandı, Faz A bunun üzerine
- **Plan 16.5** (Mosaik.Core shared kit) — `ApprovalStep` Faz C workflow için, `IUserDataScope` Faz C permission için
- **Plan 16.7** (Tag sistemi) — Faz B auto-tag bağımlı, paralel ilerlemeli

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Geri bildirim alındı
- [ ] Onay alındı: —

---

## 10. Önerilen Başlangıç Noktası

**Bugün/yarın:** Faz A — A-01 (PageImportanceScorer) + A-03 (MaxPages 30 + DPI 300 + dil) → Turkcell PDF retry → §5 görünüyor mu doğrula.

**Bu hafta sonu:** Faz A komple bit, commit-split, journal.

**Önümüzdeki hafta:** Faz B (3 quick AI win) → kullanıcı görünür değer.

**Sonra:** Faz C — DMS Foundation. Faz D + E backlog'a.

> **Arşiv notu (2026-07-05 stale-triage):** Kısmi-implement kapanış: versioning UI + permission enforce Plan 56 M-B'de bitti (7a43a94). Kalan (FTS+metadata+RAG Faz E) TODO D-01 + Plan 44 prereq olarak izleniyor.
