# vNext Vizyon Araştırması — Dış Best-Practice Doğrulaması (2026-06-29)

_5 paralel WebSearch agent + `mosaik-portal-danismani` sistem taraması. Amaç: vNext yönünü (VISION §7 + Plan 40/41/42) iç dokümana değil, sektör best-practice'ine oturtmak. Pattern: `memory/feedback_5_paralel_agent_oss_research_pattern.md`._

## Çapraz-doğrulanmış ana tema

İki bağımsız agent (process + collab) aynı uyarıyı verdi: **polymorphic tabloyu aşırı-genelleme**. Üç bağımsız agent (portal + form + process) aynı reçeteyi verdi: **OSS engine'i reuse et, sadece ince Mosaik wrapper yaz; kendi engine'ini kurma**. Bu iki ilke aşağıdaki tüm kararların belkemiği.

---

## 1. Portal / Operational Intelligence (VISION §7) — yön DOĞRU

**Bulgu:** Mosaik zaten 2025 best-practice yolunda. Modüler monolit + per-modül assembly (ADR-002/015) = literatür konsensüsü ("~%70 kuruluş well-designed modular monolith ile daha iyi"; 1 dev → asla microservices). OI ≠ BI: OI = canlı izleme + eşik → aktöre yönlendir + platform içinde aksiyon. Bu tam olarak Mosaik'in Reports→Workflow→Notifications birleşimi.

**"Modül yığını" → "platform" boşluğu (yeni modül DEĞİL, bağlayıcı doku):**
1. **Cross-module search** — sahip olmadığın tek gerçek platform yeteneği (rapor+SOP+sözleşme+tamim tek arama).
2. **Unified inbox / notification center** — OI'nin "exception → aktör" yüzeyi. Plan 42 inbox'ı workflow-only feature değil, **platform servisi** yap.
3. **Dashboard → aksiyon alert** — eşik aşınca notification fire eden dashboard = OI'nin BI'dan farkının literal tanımı. Küçük ekleme, büyük kategori-uyumu.
4. Centralized permission "kim neyi görür" auditable görünüm.

**YAPMA (tek-takım 250-kullanıcı için overkill):** microservices, low-code/modül marketplace (Retool/Appsmith tarzı), real-time streaming/event-bus (Kafka/CEP), ayrı data-warehouse tier, 3rd-party plugin SDK. DB-driven config (BrandSettings/Modules) zaten doğru miktarda "low-code".

---

## 2. Process Execution Runtime (Plan 42) — 6 aspect tablosu YERİNE 1 stream

**Doğrulanan spine:** `Process` (versiyonlu tanım) → `ProcessInstance` (vaka) → aktivite kayıtları → scoped variables. Camunda/Temporal/ServiceNow/Pega hepsi bu şekil.

**KRİTİK DÜZELTME — Plan 42'nin 6 ayrı fiziksel aspect tablosu (Form/Workflow/Document/Decision/Audit/Kvkk) bir anti-pattern.** Yerine:
- **Tek `ProcessActivity` stream tablosu:** `Id, ProcessInstanceId, ActivityType (lookup: form_submitted/workflow_step/document_attached/decision_logged/audit/kvkk_event), OccurredAt, ActorId, PayloadJson, RefEntityType, RefEntityId`.
- 6 "aspect" → ayrı tablo değil, **projeksiyon** (`WHERE ActivityType=...`). Timeline = tek sıralı sorgu (6-yollu UNION yok).
- Gerçek Form/Document/Decision satırı kendi modülünde kalır; stream sadece `RefEntityType/Id` ile işaret eder (modül izolasyonu korunur, ADR-002).

**Engine kararı:** `.NET Stateless` ile `ProcessInstance` lifecycle transition guard (Draft→InProgress→AwaitingApproval→Closed); **persistence EF Core'da kalır**. **Elsa/Camunda/BPMN ADOPT ETME** (paralel persistence + designer + ADR-002 ihlali). **Event sourcing ADOPT ETME** (audit ihtiyacı ES gerektirmez; append-only `AuditLogService` yeter).

**SLA/timer:** BPM "non-interrupting recurring timer" yerine → `DueAt/EscalationLevel/EscalatedAt` kolonu + Hangfire sweeper + `IBusinessClock` (iş-takvimi farkında, `ComplianceDueCalculator` pattern). $0 eşdeğer.

---

## 3. Form Builder (Plan 41) — hybrid DOĞRU, FormVersion EKLE, SurveyJS reuse

**Doğrulanan:** v1 JSON-config + v2 visual builder (aynı JSON üstünde) = SurveyJS/Form.io modeli. **Dual JSON-Schema+UI-Schema (JSONForms) ADOPT ETME** — branching'de kırılır (empirik).

**En yüksek-risk eksik (eklenmeli):** schema versioning + submission bütünlüğü.
- `FormVersion` (FK Form, version int, **tam snapshot JSON**, status, publishedAt) + `FormSubmission.FormVersionId` damgası.
- Submission, render edildiği versiyona göre validate edilir (en son değil). Yeni alanlar tanıtıldıkları versiyonda optional/nullable. Versiyon **publish'te** (her draft'ta değil).
- KVKK/ihbar için zorunlu: kişinin ne gördüğü/onayladığı kanıtlanmalı.

**Reuse-vs-build:** SurveyJS (OSS stack'te kabul) render + Survey Creator builder + 12 field + conditional logic + cross-version `clearIncorrectValues()` hepsini verir. **Sadece ince Mosaik katmanı yaz:** Form/FormVersion/FormSubmission entity + versioning + field↔DataElement map + HMAC/encryption/anti-spam + Plan 42 process trigger.

**Anonim hassas intake güvenliği:** field-level encryption (ihbar) ✅ standart. Anti-spam katmanlı ucuz-önce: honeypot → timing → IP rate-limit (**IP'yi submission'a yazmadan**) → CAPTCHA son. HMAC link `{formId, versionId, exp}` imzala. field→DataElement map **optional** (zorunlu yapma — builder kullanılabilirliği ölür; ama RoPA self-populate eden gerçek differentiator).

**Form→process:** submission = event → `IProcessLauncher` (Core abstraction) → `ProcessInstance` spawn. Modül modülü çağırmaz (ADR-002).

---

## 4. Collaboration + polymorphic (Comment/Mention + EntityRelations Plan 38)

**Polymorphic Comments → SHIP ET** (Rails/Discourse/GitHub pattern, blanket anti-pattern değil). Eksik DB-FK için telafi:
- `entity_type` → **lookup-FK** (DictionaryType; typo/junk engeller, integrity'nin ucuz %80'i).
- Denormalize `firma_id` comment satırında (firma=güvenlik sınırı join'e bağlı olmasın).
- Composite index `(entity_type, entity_id, created_at)` + ayrı `(author_user_id)` + `parent_comment_id` (threading).
- Orphan stratejisi: soft-delete parent veya scheduled sweep (cascade imkânsız).

**EntityRelations (Plan 38) generic tablo = ASIL anti-pattern riski.** Tek "her şeyi her şeye bağla" tablosu over-generalized association. Comment'in tek polymorphic parent'ı savunulabilir; serbest ilişki grafiği değil. **Öneri:** gerçek ilişkiler için **typed junction tablolar** (`contract_obligations`, `circular_documents` — iki tarafta FK, temiz index). Generic tabloyu yalnız "link anything to anything" gerçek bir UI feature'ı ise + `relation_kind` + her iki `*_type` lookup-FK kısıtlı tut. **Kod-öncesi `mosaik-portal-danismani`'ya danış.** (Not: Plan 38 altyapısı shipped + tek tüketici WorkflowEngine — genişletmeden önce bu kararı ver.)

**@mention + notification:** **fan-out-on-write** (Mosaik ölçeği için doğru — bounded recipient). `INotificationService` (ADR-002) üzerinden, modüller doğrudan çağırmaz. `@user` server-side parse (client'a güvenme). Read receipt = `is_read/read_at` kolon (ayrı tablo değil). Digest = Hangfire daily batch ("12 yeni yorum").

**ADOPT ETME:** W3C ActivityStreams (federation standardı, tek-tenant'a gereksiz), event sourcing (append-only `AuditLogService` zaten yeter).

---

## 5. KVKK RoPA/VERBİS (Plan 40) — backbone DOĞRULANDI, 3 ekleme + sadeleştir

`Process` + granular `DataElement` + `ProcessDataLink` junction = Fides (fideslang) System/declaration/taxonomy + OneTrust data-mapping şekli. Çoğu platformdan **daha zengin** (granular DataElement).

**3 model eklemesi (gerçek KVKK boşluğu kapatır):**
1. **`ProcessingPurpose` lookup** (KVKK 10 standart amaç) — `Purpose` free-text elaboration kalır. Kopya-yapıştır tespiti (Pattern 1) FuzzySharp yerine ucuz GROUP BY olur, VERBİS export sadık.
2. **`Recipient` lookup** (alıcı grubu) — domestic + `CrossBorderTransfer.RecipientName` ortak. VERBİS "alıcı grupları" + integrity cross-check için free-text kırılgan.
3. **`VerbisRegistration`** (FirmaId başına 1 satır) — controller header (unvan/MERSİS/KEP/irtibat/yurt dışı temsilci); export process satırlarına join. `CrossBorderTransfer`'a `SccSignedAt + KurulNotifiedAt` (SCC 5 iş günü bildirim yükümlülüğü).

**Güncellik (2024-2025):** açık rıza yurt dışı aktarımda **birincil yol DEĞİL** artık (adequacy → uygun güvence/SCC → arızi). **Pattern 9 ekle:** açık-rıza-only cross-border = risk flag. DSAR (Plan 42): **clock-pause-on-verification** açıkça modellə (en sık atlanan SLA nüansı; KVKK m.13 = 30 gün).

**Reverse mapping** ("ad-soyad nerede işleniyor?") = inverted-index. `IX_PDL_Element` doğru hot-path; EntityRelations dual-write SOP/Form/Document'a ulaşmak için **haklı** (junction tek başına bunları kaçırır). İkisini de tut.

**Sadeleştir (tek şirket, ~361 süreç — GRC SaaS değil):** DisclosureNotice diff/versioning UI → basit version int + sign-off; per-pattern enable/disable matrisi → tek severity threshold; AI integrity daily cron → **weekly** (token maliyeti); KvkkAdvisor LLM → opsiyonel enrichment (8-9 deterministik SQL/regex pattern %90 değeri verir).

---

## 6. Canlı modelleme borçları (`mosaik-portal-danismani` taraması)

| Borç | Durum | Aksiyon |
|---|---|---|
| **Forms status lookup seed eksik** | ⚠️ ucuz | byte var ama `formDefinitionStatus/formSubmissionStatus` DictionaryType seed yok → SOP `08_SeedSopStatusLookups.sql` kopyala |
| **DataElement soft-ref dangling** | ⚠️ Plan 40 prereq | `FormFieldDataElementMap.DataElementId` int soft-FK, gerçek entity yok → Plan 40 sıraya |
| **EntityRelations tek-tüketici** | ⚠️ | sadece WorkflowEngine yazıyor; §4 typed-junction kararı verilmeden genişletme |
| **Process/ProcessInstance/DataElement yok** | ❌ taslak | vNext birleştirici (Plan 40→42); §2 tek-stream tasarımıyla |
| SOP status→lookup + modül izolasyonu | ✅ | referans implementasyon, korunsun |

---

## Net aksiyon sırası (kuzey yıldızı disiplini: eski kod refactor yok, yeni iş bu pattern'lerle)

1. **Plan 41 (Form)** kritik path: + `FormVersion` snapshot, SurveyJS reuse, anti-spam katmanı, field→DataElement optional.
2. **Plan 40 (KVKK)** prereq: + ProcessingPurpose/Recipient/VerbisRegistration lookup + Pattern 9; Faz 5-7 sadeleştir.
3. **Plan 42 (ProcessExec)** birleştirici: **6 aspect → 1 `ProcessActivity` stream**, Stateless guard, Hangfire SLA sweeper, clock-pause DSAR.
4. **Platform dokusu** (VISION §7): cross-module search + unified inbox + dashboard alert (3 küçük, yüksek kategori-uyumu).
5. **Comment/Mention** (Plan 35): polymorphic + entity_type lookup-FK + denormalize firma_id + fan-out-on-write.
6. **EntityRelations**: genişletmeden önce typed-junction vs generic kararı (`mosaik-portal-danismani`).

> Bu doküman 40/41/42 planlarına ve VISION §7'ye foldlanmalı (belge-hizalama-disiplini). Planlar Taslak — onay + fold ayrı oturum.
