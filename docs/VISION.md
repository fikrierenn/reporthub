# Mosaik · Ürün Vizyonu

**Statü:** Canlı belge. Yön kararları burada yaşar — implementasyon detayı `plans/NN-*.md`'de, kararın gerekçesi `docs/ADR/`'da. Bu dosya **ne** ve **neden** sorularını cevaplar; **nasıl** sorusu plana havale edilir.

**Son güncelleme:** 2026-05-14 (analiz oturumu).

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

### vNext'in kalbi — Üçlü kombinasyon

| # | Modül | Effort | Bağımlılık | Değer |
|---|---|---|---|---|
| 1 | **SOP / Prosedür** | 2-3 hafta | Tamim altyapısı (mevcut) | BKM'de gün-1 kullanılır |
| 2 | **Comment / Mention** | 1-2 hafta | INotificationService + Plan 31 caller | Portal "konuşma yerine" döner |
| 3 | **Workflow Designer** | 3-4 hafta | IWorkflow (Core mevcut) | Geri kalan modüllerin bağımlılığı |

**Toplam: 6-9 hafta**. Bu üçlü tamamlandığında Mosaik gerçek "iç portal" iddiasını taşır. Mevcut altyapıyı reuse eder, BKM Kitap'ta bugün kullanılır.

### Önerilen sıra (3 ay perspektif)

```
Hafta 1-3:  Plan 32 Email caller bitir → SOP modülü (Tamim altyapısı reuse)
Hafta 4-5:  Comment / Mention (cross-cutting, modül başına 1 gün entegrasyon)
Hafta 6-9:  Workflow Designer (engine + designer UI + her modüle entegre)
Paralel:    Plan 18B HR Sync (haftada 1-2 gün, blok değil)
Hafta 10+:  Form/Anket (integrate karar), Documents iki-plan çakışmasını çöz
```

### Yapılmayacaklar (vNext kapsamı dışı)

- KPI / OKR modülü — Reports zaten karşılar, OKR culture fit yok
- Mesajlaşma — Slack/Teams varken marjinal
- Duyuru ayrı modül — Tamim'e `Type` enum yeterli
- Form Builder kendi üretim — open-source entegre et

---

## 7. Cross-reference

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
