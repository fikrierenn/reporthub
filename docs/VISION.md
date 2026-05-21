# Mosaik · Ürün Vizyonu

**Statü:** Canlı belge. Yön kararları burada yaşar — implementasyon detayı `plans/NN-*.md`'de, kararın gerekçesi `docs/ADR/`'da. Bu dosya **ne** ve **neden** sorularını cevaplar; **nasıl** sorusu plana havale edilir.

**Son güncelleme:** 2026-05-21 (KVKK Process Backbone + Form Builder + Process Execution Runtime — vNext kalbi altılı; portal = execution platform).

---

## 1. Mosaik nedir, ne olacak?

Mosaik **modüler şirket içi portal**. "Rapor portali" olarak başladı, son 3-4 ayda "iç portal" yönüne döndü. Migration 56'daki sidebar gruplama (5 grup, 11+ modül) bu evrimin somut ifadesi. Brand metaforu: her modül bir taş, birlikte mozaiği oluşturur.

**Hedef kullanıcı:** BKM Kitap operasyonu (~200-300 kişi tahmini). Cross-DB (DerinSIS / BKMDATA / BKM_GENEL / EncoreMerkez) entegrasyonu ile günlük operasyonel veriyi tek arayüzde toplar.

**İki olgunluk ölçüsü:**
- **Mevcut özellik seti:** ~%75 olgun (CLAUDE.md kaydı). Rapor + dashboard + sözleşme + tamim + orgchart canlı kullanıma hazır.
- **vNext "iç portal" vaadi:** ~%40-50 ilerleme. 10 modül planlanmış (TODO.md "MAJOR VISION"), 3'ü tamam (Reports, Dashboard, Tamim), 1'i kısmen (Documents), 6'sı henüz yok.

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

### Calendar %50 — entegrasyonsuz takvim Google Calendar yanında ölü

Var, çalışıyor ([`CalendarController.cs`](../Mosaik/Controllers/CalendarController.cs) 5.4KB), ama **diğer modüllerle entegrasyonu net değil**. Sözleşme son tarih, yükümlülük due date, tamim deadline, doğum günü, resmi tatil (Plan 22 — Holidays Important Dates Reminder, aktif backlog) — bunlar otomatik takvime yansıyor mu? Aksi halde "ikinci takvim" oluyor, Google Calendar varken kimse bakmaz. **Karar:** Calendar'ı **unified event source** olarak konumlandır (her modül CalendarEvent yazsın) veya tamamen sil.

### Compliance %50 — Contract ile sınırı belirsiz

[`ComplianceTemplate`](../Mosaik/Models/) model var, ComplianceController 5KB, ama [Plan 25.1 (contract security hardening)](../plans/) tek planı. **Compliance ile Contract sınırı net değil** — sözleşme tipinin compliance gereği mi, ayrı bir takvim/checklist mi? KVKK, ISO 27001 gibi şirket içi uyum dosyaları burada mı yaşayacak? Karar yok.

### Approval altyapısı vs UI — generic engine yarıda

[`ApprovalService`](../Mosaik/Services/) + `ApprovalRequest` + `IWorkflow` interface mevcut. Ama **kullanım sahası neresi?** Sözleşme onayında otomatik mi, manuel mi? Tamim onayında? İK izin talebinde? Generic workflow engine yarısına kadar yapılmış, **designer UI yok**.

**Üst düzey karar:** Ya engine'i tamamla + her modüle entegre et (Tier 3, 4-6 hafta), ya her modüle özel approval yaz (her modülde tekrar, daha pratik, daha çirkin). Şu an ikisini de yapmamış görünüyor — **en kötü senaryo**.

### Notification %40 — Plan 31/32 takılı, gerçek mail atılmıyor

Plan 17 Faz H bitti, sidebar badge var, ama **sadece tamim ile entegre**. Sözleşme yükümlülük due-date hatırlatması notification atıyor mu? Plan 31 SMTP altyapısı var ama **caller yok** (Plan 32 caller olacaktı, Plan 32 takıldı). **Gerçek mail hâlâ atılmıyor.** Email + Notification + Reminder üçlüsü bir sonraki büyük iş — bittiğinde sözleşme/yükümlülük modülünün gerçek değeri ortaya çıkar.

### AI generation / chat — extraction güçlü, generation yok

Extraction (PDF → data) güçlü. Ama [`DocumentChatService`](../Mosaik/Services/Ai/DocumentChatService.cs) 3KB, [`DocumentInsightService`](../Mosaik/Services/Ai/DocumentInsightService.cs) 5.5KB — **bu iki servis çok küçük**. AI'la sohbet, doküman üzerinde Q&A, üretken yazım yardımı bu kadar küçük dosyada olmaz. Ya henüz ürünleşmedi ya da scope tutuldu. **Karar gerekli.**

---

## 4. Yok olan vNext modülleri — değer sıralı

### 1. SOP / Prosedür Yönetimi (Plan yok) — **EN HIZLI KAZANIM**

BKM'nin 27 İK prosedürü + 44 form şu anda Word'de. Mosaik'e taşıyıp **version control + onay akışı + okundu disiplini** koyduğunda bugün BKM'de gerçek kullanılır. Tamim altyapısı (Block-based content, dosya ek, notification) %80 reuse edilebilir.

- **Effort:** 2-3 hafta
- **ROI:** Yatırım 1 ay içinde geri döner — prosedürler zaten yazılı, sadece taşınacak
- **Bağımlılık:** Tamim altyapısı (mevcut), Workflow Designer (sıradaki #4 ile zincirleme)
- **Karar:** Bu sıradaki en mantıklı iş. ROI değerlendirme ihtiyacı yok.

### 2. Comment / Mention sistemi (Plan yok) — **CROSS-CUTTING, KÜÇÜK, YÜKSEK DEĞER**

Tamim'e, dokümana, sözleşmeye, organizasyon kaydına yorum yapılabilsin. `@user` mention notification tetiklesin. Tek bir `Comments` tablo + polymorphic `EntityType` + `EntityId` + `INotificationService` callback.

- **Effort:** 1-2 hafta + modül başına 1 günlük entegrasyon
- **Etki:** Eklendiği gün portal "okuma yeri" olmaktan **"konuşma yeri"ne** döner
- **Bağımlılık:** `INotificationService` (mevcut), Plan 31 SMTP caller (Plan 32 bekliyor)
- **Karar:** Tek ekonomik iş. SOP'un altında değil, paralelinde gider.

### 3. Form / Anket Builder (Plan yok) — **BÜYÜK AMA DEĞERLİ**

İK için memnuniyet anketi, eğitim sonu değerlendirme, çıkış mülakatı. Müşteri için memnuniyet, ürün geri bildirim. Operasyon için olay bildirimi, ekipman arıza talebi.

- **Effort:** 6-8 hafta (own) veya 2-3 hafta (integrate)
- **Açık kaynak alternatifler:** LimeSurvey, Formbricks self-hosted
- **Karar gerekli:** Own vs integrate. Önerim: **integrate** — Mosaik'in core competence'i form builder değil, kullanım

### 4. Workflow Designer + Onay Akışları (Plan yok) — **GENERİC ENGINE**

`IWorkflow` altyapısı zaten `Mosaik.Core`'da. Üzerine bir designer UI + chain configuration + assignment + reminder.

- **Effort:** 3-4 hafta generic engine + 1 hafta her modüle entegrasyon
- **Etki:** Bu modül var olduktan sonra SOP onayı, sözleşme onayı, satın alma onayı, izin talebi onayı **hepsi aynı engine'i** kullanır
- **Bağımlılık:** Yok (Core abstraction hazır)
- **Karar:** 2'den sonra **en yüksek leverage olan iş** — geri kalan tüm modüllerin bağımlılığı

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

### Önerilen sıra (3 ay perspektif — 2026-05-21 rev 2 altılı kalp)

```
Hafta 1-3:  Plan 34 SOP modülü (Tamim altyapısı reuse) + Plan 32 SMTP caller
            Plan 40 KVKK Faz 0-2 paralel (veri modeli + xlsx import + envanter CRUD)
            Plan 38 EntityRelations sözleşmesi ilk büyük canlı test
            Plan 36 Workflow Designer Faz A (engine)
Hafta 2-6:  Plan 41 Form Builder (KRİTİK PATH — Plan 42 prereq)
            Faz 0-1 (scaffold + render+submit) hafta 2-3
            Faz 2-3 (admin CRUD + public anonim) hafta 4-5
            Faz 4-7 (file/signature/encrypted/template seed) hafta 5-6
Hafta 4-5:  Plan 35 Comment / Mention (cross-cutting, paralel)
Hafta 5-7:  Plan 36 Workflow Designer Faz B-D (designer UI + entegre)
Hafta 6-7:  Plan 40 KVKK Faz 3-4 (SOP entegrasyon + global reverse search)
Hafta 7-10: Plan 42 Process Execution Runtime (ProcessInstance + 6 aspect + SLA + result)
            Faz 0-3 (scaffold + execution + public + inbox/detail) hafta 7-8
            Faz 4-6 (SLA + render + KvkkProcessingActivity + retention) hafta 8-9
            Faz 7-9 (cron + admin + e2e entegrasyon test) hafta 9-10
Hafta 9-11: Plan 40 KVKK Faz 5-7 (AI integrity + VERBİS export + risk dashboard)
            Plan 42 KvkkProcessingActivity log Plan 40 dashboard'unu besler
Paralel:    Plan 18B HR Sync (haftada 1-2 gün, blok değil)
Hafta 12+:  Documents Plan 27 Faz C, Plan 33 Faz 4 D-01..D-05 borç temizliği
```

**Kritik path:** Plan 41 Form Builder. Olmadan Plan 42 başlamaz, Plan 40 form aspect stub kalır. En önce Faz 0-3 (4 hafta) çıkması şart.

### Yapılmayacaklar (vNext kapsamı dışı)

- KPI / OKR modülü — Reports zaten karşılar, OKR culture fit yok
- Mesajlaşma — Slack/Teams varken marjinal
- Duyuru ayrı modül — Tamim'e `Type` enum yeterli
- Form Builder kendi üretim — open-source entegre et

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

## 8. Cross-reference

- **Implementasyon planları:** [`plans/`](../plans/) (Tier 3 işler için zorunlu, [ADR-010](ADR/010-plan-first-tier-system.md))
  - Plan 16 — vNext modül roadmap (modül listesi + port stratejisi, bu vizyonun **implementasyon havalandırması**)
  - Plan 16.5 — Mosaik.Core shared kit (cross-modül abstraction)
  - Plan 16.6 — [ADR-002](ADR/002-modular-monolith.md) modüler monolit
- **Aktif sprint:** [`TODO.md`](../TODO.md) → "EN ÜST ÖNCELİK" bölümü
- **Mimari kararlar:** [`docs/ADR/`](ADR/) — 13 ADR, ADR-001 ile ADR-013 arası
- **Mevcut özellikler haritası:** [`docs/ARCHITECTURE_MAP.md`](ARCHITECTURE_MAP.md) (auto-refresh)

---

## 8. Bu belge nasıl güncellenir

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
