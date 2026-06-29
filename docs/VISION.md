# Mosaik · Ürün Vizyonu

**Statü:** Canlı belge. Yön kararları burada yaşar — implementasyon detayı `plans/NN-*.md`'de, kararın gerekçesi `docs/ADR/`'da. Bu dosya **ne** ve **neden** sorularını cevaplar; **nasıl** sorusu plana havale edilir.

**Son güncelleme:** 2026-05-21 (KVKK Process Backbone + Form Builder + Process Execution Runtime — vNext kalbi altılı; portal = execution platform).

> **Kapsam sınırı + rakip benchmark (2026-06-29):** [`docs/COMPETITIVE_SCOPE_2026-06-29.md`](COMPETITIVE_SCOPE_2026-06-29.md) — portalda ne OLMALI/OLMAMALI (CORE/ADJACENT/OUT) + global/TR rakip parity gap. Eksik must-have: unified inbox + cross-module search + dashboard→alert (CORE bağ-dokusu) · e-imza + İK self-servis + KVKK tamamlama (DSAR/VERBİS/ihlal) (TR table-stakes). CUT review: Plan 49 biyometrik / 51 differential-privacy / 50 shadow-org (onaylı ama scope-creep). Differentiator/wedge: **local-LLM KVKK-safe on-prem AI** (rakip yapısal eşleşemez). OUT/integrate: ERP/CRM/HRIS-bordro/e-Devlet.
>
> **Dış best-practice doğrulaması (2026-06-29):** vNext yönü 5 paralel web-research + sistem taraması ile sektör pratiğine oturtuldu → [`docs/RESEARCH_VISION_2026-06-29.md`](RESEARCH_VISION_2026-06-29.md). Özet: yön DOĞRU (modüler monolit + OI north star literatürle uyumlu). **3 düzeltme planlara fold bekliyor:** (1) Plan 42 — 6 fiziksel aspect tablosu yerine **tek `ProcessActivity` stream** (discriminator + PayloadJson + RefEntityType/Id); (2) Plan 41 — **`FormVersion` snapshot** + SurveyJS reuse; (3) Plan 40 — ProcessingPurpose/Recipient/VerbisRegistration lookup + Pattern 9. Platform boşluğu = cross-module search + unified inbox + dashboard→alert (yeni modül değil, bağlayıcı doku). EntityRelations generic tablo genişletilmeden typed-junction kararı verilmeli.

---

## 1. Mosaik nedir, ne olacak?

Mosaik **modüler şirket içi portal**. "Rapor portali" olarak başladı, son 3-4 ayda "iç portal" yönüne döndü. Migration 56'daki sidebar gruplama (5 grup, 11+ modül) bu evrimin somut ifadesi. Brand metaforu: her modül bir taş, birlikte mozaiği oluşturur.

**Hedef kullanıcı:** BKM Kitap operasyonu (~200-300 kişi tahmini). Cross-DB (DerinSIS / BKMDATA / BKM_GENEL / EncoreMerkez) entegrasyonu ile günlük operasyonel veriyi tek arayüzde toplar.

**İki olgunluk ölçüsü:**
- **Mevcut özellik seti:** ~%80 olgun (2026-05-25 revize). Rapor + dashboard + sözleşme + tamim + orgchart + SOP (Plan 34+34.1 ✅) + Workflow engine canlı kullanıma hazır.
- **vNext "iç portal" vaadi:** ~%55-60 ilerleme (2026-05-25 revize). 10 modül planlanmış, **5 tam** (Reports, Dashboard, Tamim/Circular, OrgChart, **SOP** ✅ 2026-05-23), **2 yarım** (Documents Plan 27 Faz B, Workflow engine var designer eksik — Plan 36), **3 yok** (Comment/Mention Plan 35, Form Builder Plan 41, KVKK/ProcessRuntime Plan 40+42).

Bu iki ölçüyü karıştırma — "Mosaik %75 hazır" demek **mevcut iddiası** için doğru, **vNext iddiası** için yanıltıcı.

---

## 2. Olgun ve değerli özellikler

### Rapor + Dashboard — core competence

SP-driven execution, V2 builder (drag-drop dashboard tasarımı), 10 chart tipi, named result contract ([ADR-007](ADR/007-named-result-contract.md) — sofistike tasarım), [`UserDataFilter`](../Mosaik/Services/UserDataFilterInjector.cs) ile çoklu kiracı / şube bazlı izolasyon, role-based access, CSV/Excel export. Bu özellik tek başına **Power BI Embedded'ın iç versiyon alternatifi** olarak değer üretiyor. BKM Kitap operasyonunun günlük rapor ihtiyacının %80+'ı buradan beslenebilir.

### Sözleşme + Yükümlülük + Compliance üçlüsü — gizli LegalTech

Beklendiğinden çok daha güçlü. [`ContractsController.cs`](../Mosaik/Controllers/ContractsController.cs) 577 satır (M-01 split adayı), magic byte upload validation, `AccessibleFirmaIds` ile firma izolasyonu. [`ObligationContractGenerator`](../Mosaik/Services/) + AI extraction pipeline (PDF → LLM → structured data → wizard onayı) gerçek bir **LegalTech ürünü**. Bu özellik bağımsız bir SaaS olabilir — DikkatIQ projesinde tasarlanan ürünün canlı versiyonu zaten Mosaik'te. PivotEra portföy ürünlerinden biri buraya gömülmüş.

### AI altyapısı — extraction tarafı production-grade

[`AiExtractionWorker.cs`](../Mosaik/Services/Ai/) (19KB) + WizardExtractionService + ExtractionPrompts + PageImportanceScorer + TesseractOcrExtractor + ZaiVisionProvider + FallbackLlmService. Plan 16.5 (shared AI kit) Core'a taşımış, modül-bağımsız. OCR fallback var, vision provider abstract, LLM swap edilebilir. Bu altyapı **sadece "sözleşme okuma" değil** — dilekçe, fatura, sözleşme, tutanak, herhangi bir text-extraction iş yükünü taşıyabilir.

### Tamim modülü — modüler mimarinin canlı testi

[`Mosaik.Modules.Circular`](../Mosaik.Modules.Circular/) ayrı assembly, kendi `Areas/`, kendi `Database/`, `IMosaikModule` üzerinden yüklenmiş ([ADR-002](ADR/002-modular-monolith.md)). Plan 17 tüm fazları kapatılmış. Block-based content (Quill editör), dosya ekleri, AI summary, notification, compile job. Plan 16 vNext modül roadmap'inin şablonu zaten elinde — gelecek modüller için **kopyala-genişlet pattern'i hazır** ([`vnext-entity-port`](../.claude/skills/vnext-entity-port/SKILL.md) skill).

### OrgChart — Plan 20 ile bitti

PNG export'lu, görsel hiyerarşi. Plan 18B HR Sync (Zirve `vw_PersonelDepartman` view'inden günlük senkron) bu modülün tamamlayıcısı — şu an statik veri, sync ile dinamik olacak.

### UserDataFilter / IUserDataScope — kritik cross-cutting

Multi-DB allowlist (DerinSIS, BKMDATA, EncoreMerkez vs) + şube/firma bazlı erişim kısıtlaması + filtre enjekte etme ([ADR-013](ADR/013-multi-db-topology.md)). Bu özellik bir iç portal için pazarlık konusu değil — varlığı + Plan 14'ün production-readiness odağı doğru iş.

### AuditLog + Notification + Approval — Core'a çıkarılmış cross-cutting

Üç enterprise-grade kavram `Mosaik.Core` altında abstraction olarak duruyor: [`IEmailService`](../Mosaik.Core/Email/IEmailService.cs), [`INotificationService`](../Mosaik.Core/Notification/), [`IWorkflow`](../Mosaik.Core/Workflow/), [`IAuditLog`](../Mosaik.Core/Logging/). Bu doğru istikamet.

---

## 3. Belirsiz veya yarım kalmış

### Documents %60 — iki plan, tek yön yok

AI extraction güçlü ama **paylaşım + permission UX'i belirsiz**. Doküman yükledikten sonra kim görür, kim indirir, versiyon kontrolü var mı, link paylaşılır mı — bunlar [Plan 27 (documents AI roadmap)](../plans/27-documents-ai-roadmap.md) içinde mi yoksa [Plan 19 (documents-v2)](../plans/) içinde mi anlaşılmıyor. **İki plan aktif, biri reset edilmeli ya da birleştirilmeli.** Aksi halde documents modülü iki yöne çekiliyor.

### Calendar %75 — unified event source (ADR-016 sonrası)

[`CalendarController.cs`](../Mosaik/Controllers/CalendarController.cs) + `vw_CalendarUnified` view + ADR-016 (IMosaikModule opt-in event provider). Plan 33 C-02 ✅ ile **diğer modüllerden takvime yansıma** çalışıyor: Contracts due date + Compliance deadline + Holidays + Birthday + Circular published date.
**Eksik:** Drag-drop event create UI, recurring event UX. Plan 22 Holidays/Birthdays/Important Dates altyapısı var, UI optimizasyonu kaldı.

### Compliance %60 — Contract ile sınır netleşti

[`ComplianceTemplate`](../Mosaik/Models/) + ComplianceController + `vw_ObligationDueWithCompliance`. Plan 25 (Sözleşme) Faz B+C tamam, Compliance template KVKK/ISO 27001 dosyaları için kullanılabilir hale geldi. Compliance = template definitions + due date schedule; Contract = imzalı belge instance. Sınır net.
**Eksik:** UI tarafı az (5KB controller), Compliance dashboard yok. Plan 25 Faz D adayı.

### Approval altyapısı — **DAĞINIK, Plan 36 birleştirici**

3 paralel motor (CONTRADICTIONS C-2):
- `Mosaik.Services.ApprovalService` (host) — generic `ApprovalRequest`/`ApprovalStep`
- `SopApprovalService` (SOP modülü) — host inject etmiyor, DbContext duplicate logic
- `WorkflowEngine` (`Mosaik.Core`) — Plan 36 ONAYLANDI, Stateless 5.20 + Hangfire + designer UI Faz A-C [x]

**Karar (Plan 36):** Tek engine. Stateless backend + Hangfire reminder + visual designer. ADR-019 yazıldı.
**Mevcut borç:** SOP duplicate logic Plan 36 implement tamamlanınca `IApprovalService` Core abstraction'a refactor (ADR-024 adayı). `IApprovalService` interface henüz repo'da yok — sadece ADR-002'de geçiyor.

### Notification %70 — Caller var, SMTP default kapalı

Mail caller'lar canlı:
- `DailyReminderJob` (Hangfire 09:00 TR) — Compliance/Obligation due-date reminder
- `SopReadReminderJob` (Hangfire 09:00 TR, Plan 34) — SOP okuma 7-gün/1-gün hatırlatma
- `WorkflowNotifier` (Plan 36 Faz B) — workflow step assignment notification
- Plan 17 Faz H — Tamim notification + sidebar badge

**Config:** `SmtpSettings.Enabled: false` default → prod'da mail göndermek için ayar gerekiyor.
**Plan 31:** SMTP altyapı ✅. **Plan 32:** Scheduled Reports + Email Distribution onay bekliyor (6 açık soru).
**Sonuç:** Mail caller'lar yazıldı, config aktif edilince mail gider. "Hiç mail atılmıyor" stale.

### AI generation / chat — extraction güçlü, generation yok

Extraction (PDF → data) güçlü. Ama [`DocumentChatService`](../Mosaik/Services/Ai/DocumentChatService.cs) 3KB, [`DocumentInsightService`](../Mosaik/Services/Ai/DocumentInsightService.cs) 5.5KB — **bu iki servis çok küçük**. AI'la sohbet, doküman üzerinde Q&A, üretken yazım yardımı bu kadar küçük dosyada olmaz. Ya henüz ürünleşmedi ya da scope tutuldu. **Karar gerekli.**

---

## 4. Yok olan vNext modülleri — değer sıralı

### 1. SOP / Prosedür Yönetimi ✅ TAMAMLANDI 2026-05-23 — **EN HIZLI KAZANIM**

BKM'nin 27 İK prosedürü + 44 form için **version control + onay akışı + okundu disiplini** altyapısı canlı:
- [Plan 34](../plans/34-sop-prosedur-yonetimi.md) Faz A-E ✅ (modül + entity + admin CRUD + user-facing + bildirim)
- [Plan 34.1](../plans/34.1-sop-rag-advisor.md) Faz 0-6 + 8 ✅ (cross-SOP RAG advisor + AI enrichment + ADR-022)
- 516/516 test geçiyor, Mosaik.Modules.SOP modüler monolith.

**Kalan:** Plan 34 Faz G (BKM 27 SOP içerik migration, manuel yükleme yeterli — opsiyonel). Plan 34.1 Faz 7 (LoRA fine-tune, kullanıcı feedback datası birikince).

### 2. Comment / Mention sistemi (Plan yok) — **CROSS-CUTTING, KÜÇÜK, YÜKSEK DEĞER**

Tamim'e, dokümana, sözleşmeye, organizasyon kaydına yorum yapılabilsin. `@user` mention notification tetiklesin. Tek bir `Comments` tablo + polymorphic `EntityType` + `EntityId` + `INotificationService` callback.

- **Effort:** 1-2 hafta + modül başına 1 günlük entegrasyon
- **Etki:** Eklendiği gün portal "okuma yeri" olmaktan **"konuşma yeri"ne** döner
- **Bağımlılık:** `INotificationService` (mevcut), Plan 31 SMTP caller (Plan 32 bekliyor)
- **Karar:** Tek ekonomik iş. SOP'un altında değil, paralelinde gider.

### 3. Form / Anket Builder ([Plan 41](../plans/41-form-builder.md) TASLAK) — **BÜYÜK AMA DEĞERLİ — HIBRİT**

İK için memnuniyet anketi, eğitim sonu değerlendirme, çıkış mülakatı. Müşteri için memnuniyet, ürün geri bildirim. Operasyon için olay bildirimi, ekipman arıza talebi. KVKK için DSAR/İhbar/Rıza formları.

- **Effort:** 54-70h (8 faz, 4-5 hafta) — Plan 41 taslak 2026-05-21
- **Karar:** **Kendi modül (`Mosaik.Modules.Forms`) + SurveyJS renderer reuse** (ADR-020). LimeSurvey GPL/PHP + Formbricks AGPL reddedildi (lisans + dil + integrate maliyet). v1 JSON config server-side render + admin CRUD; v2 drag-drop builder UI ileride.
- **Bağımlılık:** Plan 38 ✅, Plan 36, Documents
- **Plan 42 prereq:** KRİTİK PATH. ProcessExecution runtime form input bekliyor.

### 4. Workflow Designer + Onay Akışları ([Plan 36](../plans/36-workflow-designer.md) ONAYLANDI) — **GENERİC ENGINE — STATELESS BACKBONE**

`IWorkflow` altyapısı zaten `Mosaik.Core`'da. Plan 36 onaylandı — Stateless 5.20.1 (Apache-2.0, ~700 satır FSM) backend + Hangfire reminder + designer UI canvas. Elsa/Camunda/Temporal/Workflow Core reddedildi (overkill, ~%20-25 tasarruf).

- **Effort:** 3-4 hafta generic engine + 1 hafta her modüle entegrasyon
- **Etki:** Bu modül var olduktan sonra SOP onayı, sözleşme onayı, satın alma onayı, izin talebi onayı **hepsi aynı engine'i** kullanır
- **Bağımlılık:** Plan 38 ✅ (EntityRelations + DecisionLog)
- **Karar:** 2'den sonra **en yüksek leverage olan iş** — geri kalan tüm modüllerin bağımlılığı. **ADR-019** yazıldı.

### 5. Duyuru / Announcement Feed (Plan yok) — **TAMIM'E TYPE EKLE, AYRI MODÜL YAPMA**

Tamim varken eklemeyebilirsin. **Tamim modülüne `Severity` veya `Type` enum** ekleyerek çatallat:
- **Formal Tamim:** okundu-onaylı, departmana zorunlu, audit'li
- **Informal Duyuru:** feed'de görünür, opsiyonel okuma, like/comment

- **Effort:** 2-3 gün
- **Karar:** Yeni modül yapma. Tamim'e type ekle.

### 6. KPI / OKR (Plan yok) — **ŞIMDI YAPMA**

Mosaik ölçeğinde (BKM Kitap, 200-300 kişi) KPI dashboard zaten Reports modülünde yapılabilir. OKR (Objectives + Key Results, quarterly tracking) Google kültürü için tasarlanmış, **Türkiye perakendecisi için aşırı**.

- **Karar:** Önce 2-3 KPI dashboard template'i Reports altında üret. Gerçek talep doğarsa ayrı OKR modülü düşün. **Şu an yapma.**

### 7. Mesajlaşma (Plan yok) — **SLACK/TEAMS VARKEN YAPMA**

Real-time messaging (SignalR, online presence, typing indicator) çok geniş scope. Slack/Teams varken portal içi messaging'in yaratacağı değer marjinal. Comment + Mention sistemi (madde 2) varsa messaging'e ihtiyaç düşer.

- **Karar:** Yapma, ya da çok sonraya.

---

## 5. Fazla / belirsiz olanlar (temizlik adayı)

### TestController production'da

[`TestController.cs`](../Mosaik/Controllers/TestController.cs) `#if DEBUG` ile sarılı (kontrol edildi 14 May), prod build'de excluded. **OK ama** dosyanın varlığı CLAUDE.md'de "TestController exception — TODO G-06" diye anılıyor. Karar: prod'da kapalı, kalsın.

### htmx + Alpine + Vanilla JS — net karar yok

CLAUDE.md "Vanilla JS IIFE" diyor. Ama `_AppLayout.cshtml`'a htmx CDN eklenmiş. `htmx-expert` ve `alpine-js` skill'leri var. **3 frontend stack** var ama hangisinin ana stack olduğu yazılmamış.

**Karar gerekli:** Vanilla + Alpine = standart, htmx = **sadece şu use case'ler için** (form swap + sidebar update + partial refresh mesela). Yoksa 1-2 yıl sonra "burada Alpine, burada htmx, burada vanilla" karmaşası kaçınılmaz. ADR yazılmalı (ADR-014 adayı).

### Module ayrımı tutarsız

[`Mosaik.Modules.Circular`](../Mosaik.Modules.Circular/) ayrı assembly. Documents, Contracts, OrgChart, Calendar **ana projede**. Strateji nedir? Hepsi modül olacak mı, Circular özel mi, hangi kriter?

[ADR-002](ADR/002-modular-monolith.md) "compile-time modular monolith" demiş ama mevcut 4 modül henüz çıkarılmamış. **Önerim:**
- Yeni eklenen tüm modüller `Mosaik.Modules.<Name>` ayrı assembly (kural)
- Mevcut 4 modülü (Documents, Contracts, OrgChart, Calendar) **1 modül / ay** tempoda çıkar
- Reports + Dashboard core olarak ana projede kalır (cross-modül kullanım)

### Toplu temizlik backlog'u

- TestController dokümantasyonu
- 14 admin view standardization (Plan 33)
- Yarım kalan plan dosyaları (Plan 32 6 açık soru, Plan 27 iki dosya çakışma)

**Hafta 1 (ufak ama moral)** olarak ele alınabilir.

---

## 6. Yön önerisi — Bir sonraki 3 ay

### Mevcut backlog (şu sırada düşünülen)
- Plan 32 — Scheduled Reports + Email Distribution (6 açık soru bekliyor)
- Plan 18B — HR Sync (Zirve `vw_PersonelDepartman`, ~16-24h, kullanıcı 8 May durdurdu)

**Değerli ama vNext'in kalbi değil.** Plan 32 raporun caller'ı + Plan 18B HR sync — operasyonel ek özellikler. Ana iddiayı taşımıyorlar.

### vNext'in kalbi — Altılı kombinasyon (2026-05-21 rev 2 — portal execution platform)

| # | Modül | Effort | Bağımlılık | Değer |
|---|---|---|---|---|
| 1 | **SOP / Prosedür** (Plan 34) | 2-3 hafta | Tamim altyapısı (mevcut) | BKM'de gün-1 kullanılır |
| 2 | **Comment / Mention** (Plan 35) | 1-2 hafta | INotificationService + Plan 31 caller | Portal "konuşma yerine" döner |
| 3 | **Workflow Designer** (Plan 36) | 3-4 hafta | IWorkflow (Core mevcut) | Geri kalan modüllerin bağımlılığı |
| 4 | **KVKK Envanter** (Plan 40 dar tutuldu) | 3-4 hafta | Plan 38 ✅ | Yasal envanter + DataElement granular + reverse navigation + VERBİS export + AI integrity |
| 5 | **Form Builder** (Plan 41 — KRİTİK PREREQ) | 4-5 hafta | Plan 38 ✅, Plan 36 | Tüm portal form altyapısı (DSAR/İhbar/Aday/Rıza/Engelli/Tedarikçi/Review/PDKS) |
| 6 | **Process Execution Runtime** (Plan 42 — BİRLEŞTİRİCİ) | 4-5 hafta | Plan 40+41+36+38 | ProcessInstance runtime, 6 aspect timeline, SLA timer, KvkkProcessingActivity log, sonuç PDF/Word üretimi |

**Toplam: 11-13 hafta** (paralel başlatılabilir). Bu altılı tamamlandığında Mosaik **portal execution platform**:

> "Excel manuel kayıt biter. Tüm süreç + KVKK + form + workflow + audit + sonuç doküman portal'da yaşar."

**Süreç omurgası fikri (kullanıcı 2026-05-21):**
- Bir süreç sadece envanter satırı değil — uygulamak için **workflow + form + SOP + audit + kitapçık + KVKK context** üretir.
- `Process` (tanım, Plan 40) → `ProcessInstance` (vaka, Plan 42 N kez) → 6 aspect derived.
- EntityRelations polymorphic linker (Plan 38 omurga).
- DataElement granular reverse navigation: "ad-soyad / parmak izi / IBAN / CCTV nerelerde işleniyor?"

**Portal execution boyutu (kullanıcı 2026-05-21 rev 2):**
- DSAR public link → form → KVKK Sorumlusu inceleme → 30-gün SLA → cevap mektubu otomatik PDF → email
- İhbar anonim form (şifreli) → İhbar Komitesi → soruşturma → kapatma
- Aday başvuru public + CV upload → İK ön eleme → mülakat → karar → arşiv
- Veri ihlali çalışan formu → 72h Hangfire timer → KVKK Sorumlusu → Kurul taslak
- Açık rıza yenileme çalışan portal → İK → DisclosureNotice version
- Engelli belgesi yükleme → İSG onay → özlük dosyası
- Tedarikçi DPA → Hukuk onay → arşiv
- Yıllık envanter review → birim müdürü inbox → VERBİS hazır flag
- PDKS biyometrik rıza → alternatif yöntem → onay
- Eğitim katılım + quiz → otomatik sertifika PDF

**Detay:** [Plan 40](../plans/40-kvkk-process-backbone.md) KVKK Envanter (dar), [Plan 41](../plans/41-form-builder.md) Form Builder (Hybrid v1 JSON + v2 builder), [Plan 42](../plans/42-process-execution-runtime.md) Process Execution Runtime (ProcessInstance + 6 aspect timeline + KvkkProcessingActivity log).

### Önerilen sıra (3 ay perspektif — 2026-05-21 rev 3 bağımlılık + OSS reuse)

> **Detaylı bağımlılık grafiği + matris + hafta hafta plan:** [`docs/VNEXT_DEPENDENCY_ORDER_2026-05-21.md`](VNEXT_DEPENDENCY_ORDER_2026-05-21.md).

```
Hafta 0:    Onay + bütçe + aksiyon paketi
            - 3 plan onay (40/41/42 §10 5 madde her biri)
            - 4 ADR onay (018/019/020/021)
            - ~~QuestPDF Pro ~$699/yıl bütçe~~ REV 3: gerekmez (Gotenberg + MigraDoc hibrit $0)
            - Plan 32 SMTP caller 6 açık soru cevap
            - NuGet + JS + KVKK referansları indir (+ Gotenberg.Sharp.API.Client + PdfSharp-MigraDoc + QRCoder + Razor.Templating.Core)
            - Docker Presidio Türkçe spaCy bake + Gotenberg 8.32 container

Hafta 1-2:  Foundation + kritik path başlangıç (3 paralel hat)
            A: Plan 32 SMTP caller bitir (~8-12h) — Plan 35/40/42 unlocker
            B: Plan 41 Form Builder Faz 0-1 (~16-20h) — KRİTİK PATH
            C: Plan 36 Workflow Faz A — Stateless 5.20.1 + designer scaffold (~8h)

Hafta 2-3:  Plan 40 + Plan 34 + Plan 35 paralel
            A: Plan 34 SOP Faz A-B — Tamim altyapı reuse (~8-14h)
            B: Plan 41 Form Builder Faz 2-3 — admin CRUD + public anonim (~16-20h)
            C: Plan 40 KVKK Faz 0-1 — Mosaik.Modules.Kvkk + xlsx import 361 süreç (~10-14h)
            D: Plan 35 Comment/Mention başla (~8-12h)

Hafta 3-4:  Plan 41 finalize + Plan 40 CRUD + Plan 36 designer
            A: Plan 41 Form Builder Faz 4-5 — file/signature/encrypted (~12-16h)
            B: Plan 40 KVKK Faz 2 — KVİE CRUD UI 20 sütun (~12-15h)
            C: Plan 36 Workflow Faz B-C — designer UI + Obligations/Contracts (~14-18h)
            D: Plan 35 Comment/Mention tamam (~12-16h)

Hafta 4-5:  Plan 41 tamamla + Plan 40 SOP entegre + ADR-018 sonrası Plan 37
            A: Plan 41 Form Builder Faz 6-7 — submission admin + 8 template seed (~10-14h)
            B: Plan 40 KVKK Faz 3-4 — SOP entegrasyon + global reverse search (~14-18h)
            C: Plan 36 Workflow Faz D — SOP entegrasyon (~4-6h)
            D: Plan 34 SOP Faz C-D — tam SOP CRUD + version + onay (~22-36h)
            E: Plan 37 Unified Inbox Faz 0-1 — ADR-018 onay sonrası (~8-12h)

Hafta 6-8:  Plan 42 Process Execution Runtime (BİRLEŞTİRİCİ)
            Tüm prereq (34+36+40 Faz 0-4+41+38) bittikten sonra.
            A: Plan 42 Faz 0-1 — scaffold + ProcessExecutionService + Stateless hookup (~14-18h)
            B: Plan 42 Faz 2-3 — public/anonim + inbox + 6 aspect timeline vis-timeline (~16-20h)
            C: Plan 42 Faz 4-5 — SLA Hangfire + result rendering QuestPDF/OpenXml/OfficeIMO (~14-18h)

Hafta 8-9:  Plan 42 finalize + Plan 40 KVKK AI closure
            A: Plan 42 Faz 6-7 — KvkkProcessingActivity log + retention + ICS Ical.Net (~10-14h)
            B: Plan 42 Faz 8-9 — owner dashboard + admin + E2E test (~10-14h)
            C: Plan 40 KVKK Faz 5 — AI Integrity Checker 8 pattern Presidio+FuzzySharp (~12-15h)
            D: Plan 37 Unified Inbox Faz 2-3 — Plan 42 tüketici (~10-14h)

Hafta 10-11: KVKK closure + borç temizliği
            A: Plan 40 KVKK Faz 6-7 — VERBİS export + aydınlatma versioning + Risk dashboard (~14-18h)
            B: Plan 33 Faz 4 D-01..D-06 — Documents/AI/Vision/Test/Tag borç (~38-52h)

Paralel:    Plan 18B HR Sync — bağımsız, haftada 1-2 gün (~16-24h)

Hafta 12+:  Plan 41 v2 drag-drop builder UI, Documents çakışma çözüm, VISION §7 OI pilot
```

**Kritik path:** Plan 41 Form Builder Faz 0-3 (~32-40h Hafta 1-3). Olmadan Plan 42 başlamaz. **vNext kalbi toplam ~11 hafta wall-clock paralel.**

**Effort özet (OSS reuse + Plan 32+35+37 dahil):** Plan 32 (~8-12h) + 34 (~30-50h) + 35 (~12-16h) + 36 (~22-32h) + 37 (~18-26h) + 40 (~38-50h) + 41 (~38-48h) + 42 (~48-64h) = **~214-298h** sequential. Paralel ~11 hafta.

### Yapılmayacaklar (vNext kapsamı dışı)

- KPI / OKR modülü — Reports zaten karşılar, OKR culture fit yok
- Mesajlaşma — Slack/Teams varken marjinal
- Duyuru ayrı modül — Tamim'e `Type` enum yeterli
- ~~Form Builder kendi üretim — open-source entegre et~~ **REVİZE 2026-05-21:** Plan 41 ile değişti (SurveyJS hibrit + kendi modül, LimeSurvey/Formbricks reddedildi, ADR-020)

---

## 7. Büyük Vizyon — Operational Intelligence Platform

> **Bu bölüm kuzey yıldızıdır, plan değildir.** Aşağıdaki katmanlar (Living Org Map, Decision Memory, Digital Twin, AI COO, Autonomous Ops, Invisible ERP) **uzun vade hedef**. Yarın implement edilmez, refactor borcu üretmez.
>
> **Pragmatik bağlantı:** Yeni iş yazarken (Plan 36, 37, 38, 34) bu vizyona doğru **temel atılır** — eski kodu yeniden yazmak yok, sadece yeni yazılan kodun event sourcing / EntityRelations / DecisionLog / Inbox sözleşmesine uyması. Üst yapı kendiliğinden çıkar.
>
> Somut yakın-vade müdahaleler:
> - **Plan 36 Migration 62** — `WorkflowInstanceLogs` append-only event sourcing (EventType enum). Friction Heatmap + Digital Twin what-if query bedava gelir.
> - **Plan 38 (yeni mini-plan)** — `EntityRelations` + `DecisionLog` core tablolar. Yeni modüller (SOP, Comment, Workflow approve/reject) doğal yazıcı. Eski FK'lar dokunmaz.
> - **Plan 37 (Unified Inbox)** — `IInboxProvider` + `IWidgetProvider` sözleşmesi. Yeni modüller kayıt olur, mevcut dashboard ayrı yaşar.
> - **ADR-016 (IMosaikModule evrim)** — opt-in arama/katalog/widget kayıt. Eski modüller zorlanmaz.

### 7.0 Asıl Problem Nedir?

Şirketlerde **bilgi eksikliği** yok. Problem şu:

> **Şirketler düşünemiyor.**

Çünkü:
- Bilgi parçalı (ERP'de, Slack'te, Excel'de, kafada)
- Kararlar görünmez (kim, ne zaman, neden, sonucu ne?)
- Operasyon reaktif (sorun çıkınca fark ediliyor)
- **İnsan süreç taşıyor, sistem taşımıyor**

ERP kayıt tutar. Ticket sistemi iş takip eder. Slack konuşma depolar. Ama hiçbiri şirketin **çalışma modelini** göremez, ölçemez, iyileştiremez.

### 7.0.1 Mosaik'in Büyük Oyunu

```
"Operational Intelligence Layer"

ERP'nin üstüne oturan:
→ düşünen
→ ilişki kuran
→ öneren
→ orchestration yapan
katman.
```

**Slogan:** *"See how your company actually works."*

Şirketlerin çoğu süreçlerini bilmiyor. Darboğazlarını bilmiyor. Karar maliyetlerini bilmiyor. Operasyonel sürtünmeyi ölçemiyor. Burada gerçek ürün fırsatı var — portal değil, **Operational Intelligence Platform**.

Celonis bu sorunu $5-10M lisans bedeline çözüyor. Mosaik bunu 200-300 kişilik şirket için affordable ve self-hosted yapabilir.

---

### 7.1 Living Organization Map

Sadece org chart değil — şirketin **canlı haritası**:

```
İnsan → Süreç → Karar → KPI → Workflow → Risk → Maliyet → Doküman
```

Hepsi bağlı. Örnek görünüm:
> Bir satın alma talebi gecikti.
> Mosaik: "Ahmet üzerinde 17 approval var. Bu süreçte SLA aşımı %42. Finans overload. Aynı vendor son 3 süreçte de gecikmiş."

Bu **organizational observability**. ERP bunu yapamaz.

**Teknik temel:**
- `EntityRelations { SourceType, SourceId, RelationType, TargetType, TargetId, Weight, ValidFrom, ValidTo }`
- SQL Server recursive CTE ile N-hop traversal (3-hop yeterli 200 kişilik şirkette)
- **Visualization:** Cytoscape.js (MIT, vanilla JS, ADR-014 uyumlu) — Linkurious/Neo4j Bloom $50K+ lisans, Mosaik'e uygunsuz
- "Overloaded kişi" → kırmızı node (`task_count > threshold`), "geciken süreç" → kalın kenar (`weight`)
- Timeline slider: `ValidFrom/ValidTo` ile herhangi bir tarihe snapshot

### 7.2 Decision Memory Engine

Şirketler aynı kararları tekrar tekrar alır. Mosaik her kararı saklar:

```
Karar
→ neden alındı (Rationale)
→ kim aldı (UserId)
→ reddedilen alternatifler (JSON)
→ beklenen sonuç
→ gerçekleşen sonuç (sonradan girilir)
→ KPI etkisi: { metric: "approval_time", before: 8, after: 3, unit: "days" }
```

AI şunu söyleyebilir: *"Bu karar modeli geçen yıl maliyet artışına yol açmıştı."*

İşte burada **kurumsal hafıza** oluşur. RAG: geçmiş kararlar embedding'lenir, yeni karar bağlamında semantik arama.

```sql
DecisionLog { Id, FirmaId, Title, Rationale, MadeBy, MadeAt,
              AlternativesConsidered (JSON), ExpectedOutcome, ActualOutcome,
              KpiImpact (JSON), RelatedEntityType, RelatedEntityId,
              Status (Active | Superseded | Reversed) }
```

### 7.3 Organizational Digital Twin

*"Bu approval katmanını kaldırırsak ne olur?"*

Gerçek simülasyon değil — **deterministik what-if**:
```sql
-- Mevcut: tüm adımların ortalama süresi
AVG(DATEDIFF(HOUR, EnteredAt, ExitedAt)) FROM WorkflowInstanceLogs

-- What-if: LegalApproval adımını çıkar, kalan adımların toplamı
-- → Kullanıcıya: "Bu adımı kaldırırsanız cycle time 8.2 gün → 5.1 güne iner"
```

MVP bu kadar yeterli. Monte carlo simülasyonu ileride.

### 7.4 Friction Heatmap

Şirket içi sürtünme haritası — ERP bunu yapamaz:

- En yavaş süreçler (adım bazlı `AVG bekleme`)
- En çok bekleten kişiler (overload detection)
- Approval bottleneck'leri (step SLA aşımı)
- **Ping-pong detection:** aynı item 2+ kez aynı kişiye döner
- Ticket bounce: aynı varlık 3+ kez farklı departmana geçer

```sql
-- Ping-pong: aynı InstanceId'de aynı AssignedToId 2+ kez gelir
SELECT InstanceId, AssignedToId, COUNT(*) AS Appearances
FROM WorkflowInstanceLogs
GROUP BY InstanceId, AssignedToId
HAVING COUNT(*) > 1
```

Anomali tespiti: `PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY DurationHours)` — medyanın 2x üzeri outlier.

### 7.5 AI COO (Operasyon Copilotu)

Doğal dil ile operasyonel sorular:
- *"Son 30 gündeki darboğazları özetle."*
- *"Hangi süreçleri otomasyona almalıyız?"*
- *"En çok yönetici bağımlılığı olan süreçler?"*
- *"Bu ay hangi sözleşmeler bitiyor?"*
- *"Ali Bey bu ay kaç onay görevi aldı?"*

Mimari: **NL-to-Tool** (NL-to-SQL değil — güvenlik sınırı korunur):
```
Soru → IntentClassifier (LLM) → ToolRouter
     → getOverdueObligations | getBottlenecks | getDepartmentLoad | summarizeDecisions
     → FallbackLlmService → Yanıt + kaynak citation
```

UserDataFilter her tool çağrısında enjekte edilir — AI firma sınırını asla geçemez.

### 7.6 Work Graph

Şirket departman değil, **iş ağıdır**:

```
people ↔ work ↔ decisions ↔ systems
```

LinkedIn Economic Graph'ın şirket içi versiyonu. Her düğüm: Person, Task, Contract, Document, SOP, Decision. Her kenar: manages, owns, signed, blocked_by, derived_from, applies_to.

Atlassian Teamwork Graph 150 milyar bağlantıyı (node, edge, weight, timestamp) dörtlüsü ile yönetiyor. Mosaik'te aynı mantık 200 kişilik şirkette `EntityRelations` tablosuyla çalışır.

### 7.7 Autonomous Operations

Fatura senaryosu:
```
Fatura geldi
→ AI sınıflandırdı (AiExtractionWorker — MEVCUT)
→ Risk analizi (VendorHistory tablosu — YOK, eklenmeli)
→ Vendor geçmişi kontrolü (aynı vendor geçmişte gecikmiş mi?)
→ Onay akışını optimize etti (Plan 36 WorkflowEngine — GELECEK)
→ Anormallik yoksa: otomatik işle
→ Anormallik varsa: escalation (EscalationRule — §7 Oturum 2)
```

İnsan sadece exception handling yapar. Mosaik'in mevcut altyapısı (AiExtractionWorker + Plan 36) bu pipeline'ın %60'ını zaten karşılıyor.

Eksik: `VendorRiskProfile { VendorId, DelayCount, AvgDelayDays, LastAnomalyAt }` + basit `DecisionRule { Condition, Action }` kural tablosu.

### 7.8 Invisible ERP Layer

Kimse ERP kullanmak istemiyor. Mosaik SAP/Logo/Netsis/Mikro/Excel/Mail **üstünde** çalışan görünmez katman:

- Kullanıcı ERP'ye girmez — Mosaik'e girer
- ERP'den veri çeken connectorlar (webhook veya DB polling)
- n8n pattern: Logo Tiger webhook trigger → Mosaik Hangfire job
- Türk ERP: Logo GO/Tiger REST API var; Netsis web service tabanlı

Bu katman kurulduğunda Mosaik artık "portal" değil, **operasyonel merkez** olur.

---

### 7.9 Stratejik Faz Planı

**Faz 1 — "Visible" (Mevcut çalışmalar):** İnsanların gördüklerini görünür yapma. Reports, Dashboard, Contracts, SOP, Workflow. *"Neye sahibiz?"* sorusunu cevaplar.

**Faz 2 — "Measurable" (~2026 Q3-Q4):** Süreçleri ölçme. Friction Heatmap, WorkflowInstanceLogs, Org Intelligence dashboard. *"Nasıl çalışıyoruz?"* sorusunu cevaplar.

**Faz 3 — "Intelligent" (~2027):** AI COO, Decision Memory, Work Graph, what-if simülasyonu. *"Nasıl daha iyi çalışabiliriz?"* sorusunu cevaplar.

**Faz 4 — "Autonomous" (uzun vade):** Autonomous Operations, Invisible ERP connectorları. *"Sistem kendisi optimize edebilir mi?"* sorusunu cevaplar.

---

## 8. Yeni Vizyon Katmanları — 2026-05-20 Genişlemesi

7 platform araştırması (Backstage, Appsmith, NocoBase, n8n, Twenty CRM, Plane, FlowiseAI) + stratejik konsept analizi (Unified Inbox, AI Danışman, Company Memory, Org Intelligence, Dynamic Dashboard, No-excuse Platform) sonucunda aşağıdaki katmanlar Mosaik vizyonuna eklendi.

### 7.1 Unified Action Inbox — Plan 37 (yeni)

Tek `/Inbox` sayfasında tüm aksiyon gerektiren öğeler. **Aksiyon ≠ Bildirim** ayrımı kritik — pasif log sidebar'da, Inbox sadece "senin yapman gereken".

```
InboxItem { UserId, Type, EntityType, EntityId, Priority, DueAt, IsRead, IsDone, Reason }
Type: Approval | Task | Alert | AIRecommendation | KPIAlert | Mention | Deadline
Reason: "assigned" | "mentioned" | "approval_required" | "deadline_approaching"
```

**IInboxProvider** interface — her modül kendi item'larını Inbox'a bildirir (ContractApprovalInboxProvider, ObligationDeadlineInboxProvider, AIRecommendationInboxProvider). Plan 36 Faz A tamamlanmadan Plan 37 başlamaz (ApprovalRequest entity bağımlılığı).

UX: keyboard-first (`j/k/Space`), "neden buradasın" chip, optimistic done + undo, boş state motivasyon.

### 7.2 AI Process Assistant — NL-to-Tool (Plan 34 genişlemesi)

"Bu ay hangi sözleşmem bitiyor?", "En çok geciktiren departman hangisi?" soruları doğal dille yanıtlanır. **NL-to-SQL değil NL-to-Tool** — güvenlik sınırı korunur.

```
Soru → IntentClassifier (LLM) → ToolRouter → ToolResult → FallbackLlmService → Yanıt + kaynak
```

5 MVP aracı: `getExpiringContracts`, `getOverdueObligations`, `getDepartmentLoad`, `searchDocuments`, `runReport`. Her araç UserDataFilterInjector'dan geçer — AI izole firma verisini asla göremez.

### 7.3 Company Memory — EntityRelation (migration ~65)

```sql
EntityRelations { FirmaId, SourceType, SourceId, RelationType, TargetType, TargetId, CreatedAt }
```

Önce eklenmesi gereken 5 ilişki: Contract→Obligation (HasObligation), Document→Contract (AttachedTo), Obligation→User (AssignedTo), SOP→Department (AppliesTo), Task→Contract (DerivedFrom). Neo4j overkill — SQL pivot table yeterli. UI: her detay sayfasında collapsible "Bağlı Öğeler" section.

### 7.4 Org Intelligence — Bottleneck Detection

```sql
WorkflowInstanceLogs { FirmaId, InstanceId, EntityType, EntityId, StepName, StepOrder,
                       Status, AssignedToId, EnteredAt, ExitedAt }
```

5 temel metrik SQL aggregation ile hesaplanır: adım bekleme süresi, kişi bazlı yük, overdue rate, lead time, sözleşme bitiş uyarısı. 200 kişilik şirkette ML gereksiz — `PERCENTILE_CONT` anomali tespiti yeterli. **Faz 0:** `ContractObligationsController` SaveChanges noktasına log kaydı (5 satır). **Faz 1:** Admin "Yük Raporu" sayfası.

### 7.5 Dynamic Dashboard Engine — Per-Role Widget

```csharp
interface IWidgetProvider {
    string WidgetType { get; }
    string DisplayName { get; }
    Task<object> GetDataAsync(int userId, int reportId, CancellationToken ct);
    string RenderConfigSchema();  // builder form otomatik üretimi
}
```

Rol template'leri: `ceo` (4 KPI + trend), `hr` (personel + dağılım), `operations` (PDKS + sözleşme expiry), `default` (son 3 rapor). Öncelik: user layout > rol template > sistem default. `IWidgetProvider` interface ~3 saatlik iş, mevcut DashboardRenderer'ı kırmaz.

### 7.6 No-Excuse Platform — EscalationRule + Multi-Channel

Kullanıcıya "Görmedim/atladım" mazaretini kapatacak kanal zinciri:

| T | Tetik | Kanal | Hedef |
|---|-------|-------|-------|
| DueDate − 7 gün | Hangfire | InApp + Email | Atanan |
| DueDate − 1 gün | Hangfire | InApp + Email | Atanan |
| T+24h | Hangfire kontrol | InApp + Email | Atanan |
| T+72h | EscalationRule #1 | Email | Yönetici |
| T+120h | EscalationRule #2 | Email | Departman Başkanı |
| T+168h | EscalationRule #3 | Email | Admin |

```csharp
class EscalationRule { TriggerAfterHours, EscalateTo (enum), Channel (flags enum) }
enum NotificationChannel { InApp, Email, Push, Sms }  // Sms = stub şimdilik
```

ICS feed: `GET /Obligations/Calendar.ics?token={hmacToken}` — Outlook/Google Calendar aboneliği ile yükümlülükler kişisel takvime düşer. RRULE ile periyodik yükümlülük tekrarı (Q-due → `FREQ=YEARLY;BYMONTH=2,5,8,11`).

### 7.6.b KVKK + Process Execution — Plan 40 + 41 + 42 (2026-05-21 rev 2)

EntityRelations'ın ilk büyük canlı tüketicisi. KVKK 6698 envanter yükümlülüğü vesilesiyle **`Process` central entity** kurulur. Her süreç 6 aspect derived:

```
[Process / KVİE Satırı]
   │
   ├── DataElement(s)       atomic veri öğesi (ad-soyad, parmak izi, IBAN…)
   ├── WorkflowDefinition   Plan 36 onay akışı
   ├── Form(lar)            Plan 41 Form Builder (henüz yok, geçici string)
   ├── Sop                  Plan 34 prosedür dokümanı
   ├── AuditLog             standart audit (var)
   └── Document/Kitapçık    Documents modülü
```

Tüm 6 aspect aynı `ProcessId`'ye bağlı + EntityRelations polymorphic kayıt. Reverse navigation:

```sql
-- "person.fullname" nerelerde işleniyor?
SELECT * FROM EntityRelations
WHERE TargetType='DataElement' AND TargetId=@adSoyadId;
```

**BKM Kitap v7 xlsx envanteri (361 süreç × 20 sütun, 17 departman, 5 REF, Risk Özeti dashboard) Faz 1'de DB'ye seed olarak yüklenir.** AI Integrity Checker (8 pattern — kopyala-yapıştır amaç, CCTV>60gün, gizli yurt dışı SaaS aktarımı, vs) günlük Hangfire job. VERBİS export ClosedXML Mart 2025 rehber formatında.

**Üç plan birleşimi (2026-05-21 rev 2):**

| Plan | Görev | Effort | Konum |
|---|---|---|---|
| **Plan 40** | KVKK envanter (tanım, integrity, VERBİS, reverse search) | 50-65h, 3-4 hafta | passive registry + denetim |
| **Plan 41** | Form Builder altyapı (Hybrid v1 JSON + v2 builder UI) | 54-70h, 4-5 hafta | tüm portal form çekirdeği |
| **Plan 42** | Process Execution Runtime (ProcessInstance + 6 aspect timeline + SLA + result PDF/Word + KvkkProcessingActivity log) | 64-84h, 4-5 hafta | runtime birleştirici |

**Portal execution platform vizyonu (kullanıcı 2026-05-21):**

> "bu süreçlerin ve kvkk kısımlarının tamamının işleyişi formları akışı mümkün olduğunca portal üstünden olmalı"

Excel manuel kayıt biter. DSAR vatandaş başvurusu → public form → 30-gün SLA → cevap PDF email; ihbar anonim form (şifreli) → İhbar Komitesi → soruşturma; aday başvuru + CV → İK ön eleme → karar; veri ihlali 72h timer; yıllık envanter review birim müdürü inbox; PDKS biyometrik rıza alternatif yöntem; eğitim katılım quiz → sertifika otomatik. **Her instance KVKK DataElement işleme kaydı düşer, EntityRelations otomatik dolar, saklama timer çalışır, retention sonu otomatik anonimleştirme/silme tetiklenir.**

**Plan detayları:** [`plans/40-kvkk-process-backbone.md`](../plans/40-kvkk-process-backbone.md), [`plans/41-form-builder.md`](../plans/41-form-builder.md), [`plans/42-process-execution-runtime.md`](../plans/42-process-execution-runtime.md).

### 7.7 IMosaikModule Evrim — Backstage + Appsmith Dersleri

Mevcut `IMosaikModule.RegisterServices(IServiceCollection)` yeterli değil. Eklenecek:

```csharp
interface IMosaikModule {
    void RegisterServices(IServiceCollection services);
    IEnumerable<SearchDocument> ProvideSearchDocuments();    // cross-modül unified search
    IEnumerable<CatalogEntity> ProvideCatalogEntities();     // varlık kataloğu
    IEnumerable<WidgetDefinition> GetWidgetDefinitions();    // dashboard widget tipleri
    IEnumerable<InboxItemType> GetInboxItemTypes();          // Inbox provider bildirimi
}
```

**ADR-018 taslak yazıldı (2026-05-20):** [`docs/ADR/018-imosaikmodule-capability-evolution.md`](ADR/018-imosaikmodule-capability-evolution.md). Opt-in capability interfaces (IInboxProvider, IWidgetProvider, ISearchDocumentProvider, ICatalogEntityProvider, IEntityWorkflowProvider). `IMosaikModule` çekirdek değişmez — eski modüller dokunulmaz. İlk implementasyon Plan 37 (Unified Inbox).

---

## 9. Strategic Extension Layers — 2026-05-25 (CEO + Saha + KVKK lens)

vNext kalbinin 11 haftalık plan'ı operasyonel temel. Üzerine **Mosaik'i piyasadaki binlerce standart portaldan ayıracak** 4 strategic katman tasarlandı (kullanıcı strategic input 2026-05-25). Her biri ayrı plan, gerçek BKM risk + fırsat.

### 9.1 [Plan 44 — RAG Chunk-Level Permission Guard](../plans/44-rag-permission-guard.md) 🔴 HOT-FIX

**Lens:** KVKK denetçi
**Risk:** SOP RAG canlı, `SopChunkRetriever` sadece FirmaId scope. Documents Plan 27 Faz E RAG'a bağlandığında: yönetim kurulu kararı / maaş politikası chunk'ları düşük yetkili user sorusuna sızabilir. KVKK m.12 + ticari sır.
**Çözüm:** Chunk-level `SecurityLevel + AllowedRoleIds + AllowedDepartmentIds + AllowedUserIds`. SQL-side WHERE filter, LLM hiç görmesin. `IRagAccessPolicy` Core abstraction (SOP/Documents/Contracts reuse).
**Effort:** 12-18h | **Bağımlılık:** SOP canlı (acil) | **Blocker:** Documents/Contracts RAG bağlanmadan ÖNCE

### 9.2 [Plan 45 — Excel-to-Process AI Adaptation Engine](../plans/45-excel-to-process-ai-parser.md)

**Lens:** Operasyonel hız
**Risk:** Her departman onlarca takip Excel'i; yazılımcı bekleme 2-3 hafta. Mosaik adoption düşük, Excel ölmüyor.
**Çözüm:** Excel upload → Qwen schema inference → SurveyJS template autogen + SQL table autogen + historical import. 3 hafta → 5 dakika.
**Effort:** 40-60h | **Prereq:** Plan 41 Faz 0-3 ✅ | **Reuse:** ClosedXML (OSS), Plan 40 Faz 5 Presidio scan

### 9.3 [Plan 46 — PWA Offline-First + Native Camera/Barcode](../plans/46-pwa-offline-camera.md)

**Lens:** Saha operasyon
**Risk:** Depo/mağaza saha personeli portalı kullanamaz. Sinyalsiz koridor + el-yazımı barkod + sayım data kaybı. BKM perakende + lojistik için kritik.
**Çözüm:** PWA (Workbox 7) + IndexedDB sync queue + ZXing-js barcode + getUserMedia camera + Web Push. SurveyJS custom barcode widget.
**Effort:** 50-70h | **Prereq:** Plan 41 Faz 0-3 ✅ | **OSS:** Workbox + idb + ZXing-js + WebPush.NET

### 9.4 [Plan 47 — Auto-Tuning Process Optimization Advisor](../plans/47-auto-tuning-process-advisor.md)

**Lens:** CEO / yönetim
**Fırsat:** Mevcut "Friction Heatmap" pasif (yönetici dashboard'a bakacak). Proaktif öneri = "kendi verisini okuyan canlı kurumsal beyin" = standart portal'dan kopuş.
**Çözüm:** Daily Hangfire job → 5 pattern detector (approval bypass / bottleneck / dead-end / duplicate / volume spike) → Qwen narrative + heuristic impact → Admin Inbox → Accept/Reject + workflow auto-edit + post-accept tracking.
**Effort:** 25-35h | **Prereq:** Plan 36 Faz B+C log + Plan 42 instance data | **VISION §7 ile uyumlu** (Operational Intelligence kuzey yıldızı)

### Toplam Strategic Extension

| Plan | Aciliyet | Effort | Sıra |
|---|---|---|---|
| 44 RAG Guard | 🔴 HOT-FIX | 12-18h | Plan 27 Faz E öncesi (~Hafta 3-4) |
| 47 Auto-Tuning | 🟡 Differentiator | 25-35h | Plan 36+42 sonrası (~Hafta 10+) |
| 45 Excel Parser | 🟢 Adoption | 40-60h | Plan 41 Faz 4+ (~Hafta 6-9) |
| 46 PWA + Camera | 🟢 Saha | 50-70h | Plan 41 Faz 4+ (~Hafta 7-11) |

**Toplam ekstra effort:** ~130-180h (3-4 hafta paralel + 2 ek sprint).
**Stratejik etki:** Mosaik "intranet portal"dan **"canlı kurumsal beyin + saha-uyumlu + adoption-hızlandırıcı + KVKK-emniyetli"** seviyesine taşınır.

---

## 10. Radikal Paradigmalar — 2026-05-25 (Enterprise OS Vizyonu)

vNext kalbi + 4 strategic extension (Plan 44-47) Mosaik'i "saha-uyumlu + KVKK-emniyetli + adoption-hızlandırıcı + canlı kurumsal beyin" seviyesine taşır. Üzerine **dünya'da var olmayan, devrimsel "Enterprise Operating System"** kategorisi için 4 radikal paradigma araştırma taslağı yazıldı.

Bu paradigmalar **vNext kalbi production'da 6+ ay olgun olunca aktif olur** (data + skill catalog + güven inşası gerek). Erken implement = LLM hata maliyeti yüksek + KVKK risk.

### 10.1 [Plan 48 — Executable SOP](../plans/48-executable-sop.md) (Doküman → Canlı Süreç)

**Paradigm:** Dokümantasyon = kod. Yöneticinin yazdığı doğal dil prosedürü → arka planda Stateless State/Transition + Hangfire SLA timer + Task assignment auto-register. Form Builder/Workflow Designer manuel UI'a gerek kalmaz — **prosedür yazmak = süreci canlıya almak**.

**Effort:** 60-90h | **Bağımlılık:** Plan 34 SOP ✅ + Plan 36 Workflow ✅ | **Tahmini:** 2026 Q3

### 10.2 [Plan 49 — Zero-UI Operations](../plans/49-zero-ui-ops.md) (Arayüzsüz Akış)

**Paradigm:** Çalışan portala girmesin. Mosaik widget basılı tut → konuş + fotoğraf çek → Whisper TR ASR + Qwen intent + Vision provider → form auto-fill + workflow trigger + task assign. **1 saniyede konuş, süreç başlar — sıfır arayüz etkileşimi**.

**Effort:** 80-120h | **Bağımlılık:** Plan 46 PWA ✅ + Plan 41 Form ✅ + Plan 44 RAG Guard ✅ | **Tahmini:** 2026 Q4

### 10.3 [Plan 50 — Shadow Organization Graph](../plans/50-shadow-org-graph.md) (Gerçek İş Ağı)

**Paradigm:** Resmi Org Chart yalan. Onay logları + @mention + comment thread + workflow bypass pattern → **observed shadow graph**. Resmi vs gerçek bilgi merkezi karşılaştırma. AI önerisi: "X konusunda kritik kararlarda gerçek bilgi merkezi Y'ye danışıl, %15-20 verimlilik artışı".

**Effort:** 40-60h | **Bağımlılık:** Plan 35 Comment ✅ + Plan 36 Workflow ✅ + Plan 38 EntityRelations ✅ | **Tahmini:** 2027 Q1

### 10.4 [Plan 51 — Differential Privacy Reporting](../plans/51-differential-privacy-reporting.md) (Aktif Veri Bağışıklığı)

**Paradigm:** SP sonucunu user'a olduğu gibi gösterme. App-side privacy filter: aynı SP **3 ayrı sanitize output** (genel müdür raw, analist aggregate+Laplace noise, misafir mask). 50+ DB yetki tablosu yerine **centralized policy + AI sensitive column sniff** (Presidio + Qwen). **App-side defense-in-depth**.

**Effort:** 50-80h | **Bağımlılık:** Plan 14 UserDataScope + Plan 44 RAG Guard ✅ + Plan 40 Presidio ✅ | **Tahmini:** 2027 Q2

### Toplam Radikal Paradigm

| Plan | Paradigm | Effort | Tahmini |
|---|---|---|---|
| 48 Executable SOP | Doküman → Kod | 60-90h | 2026 Q3 |
| 49 Zero-UI Ops | Arayüzsüz akış | 80-120h | 2026 Q4 |
| 50 Shadow Graph | Gerçek hiyerarşi | 40-60h | 2027 Q1 |
| 51 DP Reporting | Aktif gizlilik | 50-80h | 2027 Q2 |

**Toplam:** ~230-350h (12-16 hafta). vNext kalbi + Plan 44-47 sonrası **yıl boyu** dağıtık geliştirme.

**Stratejik konum:** Bu 4 paradigm Mosaik'i:
- Salesforce/Microsoft 365/Notion/Monday.com seviyesinden **bir kategori üste** taşır
- "Kurumsal İşletim Sistemi" (Enterprise OS) tanımı
- Kendi kendini yazan + kendi kendini yöneten + kendi kendini koruyan **canlı kurumsal organizma**

**Pre-implementation şart:** Her plan için POC + 5-lens deep tradeoff + KVKK/DPO ön onay + ADR. Bu plan dosyaları **araştırma taslakları** — Tier 3 implementation öncesi deep dive.

---

## 11. Cross-reference

- **Implementasyon planları:** [`plans/`](../plans/) (Tier 3 işler için zorunlu, [ADR-010](ADR/010-plan-first-tier-system.md))
  - Plan 16 — vNext modül roadmap (modül listesi + port stratejisi, bu vizyonun **implementasyon havalandırması**)
  - Plan 16.5 — Mosaik.Core shared kit (cross-modül abstraction)
  - Plan 16.6 — [ADR-002](ADR/002-modular-monolith.md) modüler monolit
  - **Plan 44-47** — Strategic extension layers (RAG Guard, Excel Parser, PWA, Auto-Tuning)
- **Aktif sprint:** [`TODO.md`](../TODO.md) → "EN ÜST ÖNCELİK" bölümü
- **Mimari kararlar:** [`docs/ADR/`](ADR/) — ADR-001 ile ADR-022 arası
- **Mevcut özellikler haritası:** [`docs/ARCHITECTURE_MAP.md`](ARCHITECTURE_MAP.md) (auto-refresh)
- **Çelişki audit:** [`docs/CONTRADICTIONS_2026-05-25.md`](CONTRADICTIONS_2026-05-25.md) (19 bulgu)

---

## 12. Bu belge nasıl güncellenir

**Kim güncelleyebilir:** Kullanıcı + Claude oturumlarında stratejik karar alındığında.

**Ne zaman:**
- Yeni modül kararı (yapılacak / yapılmayacak)
- Mevcut modülün scope'u değiştiğinde
- vNext sırası değiştiğinde
- Effort tahmini gerçekleştiğinde (geriye dönük revize)

**Ne zaman değil:**
- Implementasyon detayı değişti → plan dosyasını güncelle, buraya değil
- Yeni mimari karar → ADR yaz, buraya **referans ekle**
- Günlük iş → TODO.md + journal

**Sürüm disiplini:** Her güncellemede üstteki "Son güncelleme" tarihini yenile + kısa değişiklik notu ekle.

### Sürüm geçmişi

- **2026-05-14:** İlk sürüm. Mevcut özellik olgunluğu + vNext değer sıralı modül listesi + 6-9 haftalık SOP+Comment+Workflow Designer üçlüsü önerisi.
- **2026-05-20:** §7 eklendi — 7 platform araştırması (Backstage, Appsmith, NocoBase, n8n, Twenty CRM, Plane, FlowiseAI) + 6 stratejik vizyon katmanı (Unified Inbox, AI Danışman, Company Memory, Org Intelligence, Dynamic Dashboard, No-excuse). IMosaikModule evrim önerisi. Plan 37 (Unified Inbox) adayı tanımlandı.
- **2026-05-21:** vNext kalbi üçlüden dörtlüye genişletildi (KVKK Process Backbone Plan 40 eklendi). Süreç omurgası fikri: `Process` central entity → 6 aspect derived (DataElement / Workflow / Form / SOP / Audit / Doküman) + EntityRelations polymorphic linker. §7.6.b yeni alt bölüm. BKM Kitap v7 KVKK envanter xlsx (361 süreç) Faz 1 seed kaynağı. AI Integrity Checker 8 pattern + VERBİS export + global reverse search ("ad-soyad nerede işleniyor?"). KVKK skill (`.claude/skills/kvkk-veri-envanteri/`) repo'ya port edildi (374 satır, claudskills.com export).
- **2026-05-21 rev 2:** Kullanıcı netleştirmesi "bu süreçlerin ve kvkk kısımlarının tamamının işleyişi formları akışı mümkün olduğunca portal üstünden olmalı" sonrası. Plan 40 **dar tutuldu** — execution kapsam dışına çıkarıldı, Plan 42'ye devredildi. **Plan 41 Form Builder** (Hybrid v1 JSON + v2 builder UI, 54-70h) ve **Plan 42 Process Execution Runtime** (ProcessInstance + 6 aspect timeline + SLA timer + sonuç PDF/Word + KvkkProcessingActivity log, 64-84h) eklendi. vNext kalbi **altılı**: SOP + Comment + Workflow + KVKK + Form Builder + Process Execution Runtime. Toplam 11-13 hafta. Plan 41 = kritik path (Plan 42 prereq, Plan 40 form aspect typed bağlama). Portal **runtime execution platform** olarak konumlandı.
- **2026-05-21 rev 3:** Kullanıcı "questpdf yerine bence repo vardır iyi bak" → ek deep research agent. **QuestPDF Pro reddedildi** (3 yıl $2097 maliyet + DSL öğrenme borcu + Razor reuse yok + digital signature native yok + vendor lock). **Gotenberg + PdfSharp/MigraDoc + QRCoder + Razor.Templating.Core hibrit stack kabul edildi.** ADR-021 rev 2 yazıldı. Yıllık lisans **$0**. vNext kalbi toplam lisans maliyeti: 0. Plan 42 Faz 5 effort 21h → 19h (-2h Razor template reuse). BKM Docker compose Plan 40 Presidio + Plan 42 Gotenberg birleşik <2Gi RAM toplam. Reddedilen ek alternatifler: Carbone (CCL), DinkToPdf (wkhtmltopdf arşiv), jsreport (LGPL), Spire.PDF Free (10 sayfa limit), HiQPdf Free (5 sayfa limit), PdfReport.Core (LGPL+iTextSharp dep), iText AGPL.
