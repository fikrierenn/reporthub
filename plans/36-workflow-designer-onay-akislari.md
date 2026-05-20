# Plan 36 — Workflow Designer + Onay Akışları

**Tarih:** 2026-05-19
**Yazan:** Fikri / Claude
**Durum:** `Onaylandı`

---

## 1. Problem

Mosaik'te yükümlülük, SOP adımı, sözleşme onayı, tamim onayı gibi işler "kimin yapacağını" tanımlamıyor. Görev atanıyor ama kim onaylayacak, sıra ne, gecikirse ne olacak belli değil. Kullanıcı "görmedim / unuttum / bana gelmedi" mazeret üretiyor çünkü sistem bunu engelleyecek zincir mekanizmasına sahip değil. Tüm modüllerde onay akışı ihtiyacı var (SOP → tamim → sözleşme → satın alma → izin talebi) ama her biri farklı kodlanırsa bakım felaketi. Tek shared engine gerekiyor.

---

## 2. Scope

### Kapsam dahili
- Visual workflow designer (drag-drop canvas) — adım tanımlama
- Adım tipleri: Onay, Bildirim, Görev, Koşul (if/else branch)
- Her adıma: sorumlu kullanıcı/rol, deadline offset, escalation kuralı
- Workflow template → DB kayıt (JSON)
- Workflow instance — template'den tetikleme, aktif adım takibi
- Hangfire job — aktif adımları işler, bildirim + escalation gönderir
- `IWorkflow` abstraction (Mosaik.Core'da zaten iskelet var) → implement
- Entegrasyon: Obligations, Contracts, SOP (Plan 34), Tamim (Plan 17)

### Kapsam dışı
- BPMN 2.0 standart uyumu — aşırı komplex, YAGNI
- Harici workflow engine (Elsa, Camunda) entegrasyonu
- Mobil uygulama push notification
- SLA raporlama dashboard'u (Phase 2)
- E-imza / dijital onay damgası

### Etkilenen dosyalar (tahmin)
- `Mosaik.Core/Workflow/` — `IWorkflow`, `IWorkflowStep`, `WorkflowInstance` interface'leri (mevcut iskelet genişletilecek)
- `Mosaik/Models/WorkflowTemplate.cs` — yeni
- `Mosaik/Models/WorkflowInstance.cs` — yeni
- `Mosaik/Models/WorkflowStep.cs` — yeni
- `Mosaik/Controllers/WorkflowController.cs` — designer CRUD + instance API
- `Mosaik/Services/WorkflowEngine.cs` — instance yürütme motoru
- `Mosaik/Services/WorkflowNotifier.cs` — bildirim + escalation
- `Mosaik/Jobs/WorkflowStepProcessor.cs` — Hangfire recurring job
- `Mosaik/Views/Workflow/` — designer view + instance dashboard
- `Mosaik/Database/62_WorkflowTables.sql` — yeni migration
- `wwwroot/assets/js/workflow-designer.js` — sequential-workflow-designer wrapper
- `Mosaik/Controllers/ObligationsController.cs` — workflow tetikleme eklenir
- `Mosaik/Controllers/ContractsController.cs` — workflow tetikleme eklenir

**Tahmini boyut:** ~20-25 dosya / 1500-2000 satır

---

## 3. Alternatifler

### A: Her modüle özel onay mantığı
**Açıklama:** SOP'a ayrı onay tablosu, Sözleşmeye ayrı, Tamim'e ayrı — her biri kendi controller'ında.
**Reddetme sebebi:** 5 modül × onay kodu = 5 ayrı bug yatağı. Kural değişince 5 yerde güncelleme. Plan 34 SOP, Plan 25 Contracts, Plan 17 Tamim birbirini bilmez. Anti-pattern.

### B: Hazır workflow engine (Elsa Workflows / Camunda)
**Açıklama:** Elsa (MIT, .NET native) veya Camunda 8 self-hosted entegre et.
**Reddetme sebebi:** Elsa 3.x hâlâ instabil, heavy dependency, öğrenme eğrisi yüksek. Camunda harici servis + JVM = operasyonel yük. Mosaik'in ihtiyacı Elsa'nın sunduğunun %20'si — YAGNI. Bağımlılık riskini almaya değmez.

### C: `sequential-workflow-designer` + custom engine (SEÇİLEN)
**Açıklama:** Frontend'de `sequential-workflow-designer` (MIT, zero-dep, Razor-embeddable) ile görsel tasarım. Backend'de lightweight custom `WorkflowEngine` — template JSON'dan instance üretir, Hangfire ile adım işler, `IWorkflow` Core abstraction üzerinden modüllere bağlanır.
**Sebep:** Kontrolümüzde, tam fit, sıfır harici runtime bağımlılığı. Mosaik.Core'daki `IWorkflow` iskelet zaten bu yönü işaret ediyor. Modüller sadece `IWorkflowService.StartAsync(templateId, entityId)` çağırır — engine detayını bilmez.

---

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| WorkflowEngine complexity patlar | Yüksek | Orta | Faz A sadece sequential (no branch) ile başla, koşullu branch Phase 2 |
| Hangfire job race condition | Orta | Düşük | Optimistic lock + `WorkflowInstance.LockedAt` kolonu |
| sequential-workflow-designer UX öğrenme eğrisi | Düşük | Düşük | Demo JSON ile pre-built şablonlar sun |
| Modül entegrasyonu kapsam patlar | Yüksek | Orta | Faz B'de sadece Obligations + Contracts — SOP entegrasyonu Faz C |
| Migration sayısı artıyor (şu an 62) | Düşük | Kesin | Tek migration, 3 tablo toplu |

---

## 5. Done Criteria

- [ ] Workflow template oluşturulabilir (sequential-workflow-designer canvas)
- [ ] Template adımlarına sorumlu kullanıcı / rol atanabilir
- [ ] Template kaydedilince DB'ye JSON serialize edilir
- [ ] Obligations veya Contracts kaydında workflow instance tetiklenebilir
- [ ] Aktif instance'ın hangi adımda olduğu görülebilir (instance dashboard)
- [ ] Hangfire job her dakika aktif adımları kontrol eder, deadline geçmişse escalation bildirimi gönderir
- [ ] Onaylayan kullanıcı adımı approve/reject edebilir
- [ ] Reject → instance durur, başlatıcıya bildirim gider
- [ ] dotnet test yeşil (WorkflowEngine unit test — sequential adımlar için)
- [ ] İnline style yok, ui-patterns.md uyumlu

---

## 6. Rollback Planı

- Migration: `62_WorkflowTables.sql` drop script hazır (tablolar bağımsız, FK yok diğer core tablolara)
- WorkflowController sadece yeni route — mevcut controller'lar dokunulmaz
- Modül entegrasyonu `IWorkflowService` null-guard ile: engine yoksa silent skip
- `git revert` + migration down: veri kaybı yok (hiç production instance yok)

---

## 7. Adımlar

### Faz A — Core Engine + Designer UI (12-16h)

> **Mimari karar (2026-05-20):** `WorkflowInstanceLogs` **append-only event sourcing** olarak baştan tasarlanır. UPDATE yok — her state değişimi yeni satır. `WorkflowInstance.CurrentStepId` sadece **projeksiyon** (latest log'tan türetilir, opsiyonel cache). Bu pattern Friction Heatmap + Digital Twin what-if query'lerini ücretsiz açar (VISION §7 yakın-vade müdahale).

> **Backend altyapı tamamlandı 2026-05-21** (W-01..W-04 + W-08). UI bloğu (W-05..W-07) sonraki batch — sequential-workflow-designer CDN + Razor view.

1. [x] **W-01** ✅ `Mosaik.Core/Workflow/IWorkflowService` + `WorkflowEventType` (8 sabit) + `WorkflowInstanceStatus` + DTO record'ları
2. [x] **W-02** ✅ Migration 65 — `WorkflowTemplates`, `WorkflowInstances`, `WorkflowInstanceLogs` event sourcing + 5 index (Entity, Active, Instance, StepEvent + EntityType). DB'ye uygulandı.
3. [x] **W-03** ✅ `Mosaik/Models/Workflow/` 3 EF entity + `MosaikContext` DbSet + `OnModelCreating` konfig
4. [x] **W-04** ✅ `WorkflowEngine.cs` — StartAsync (InstanceStarted + StepEntered), AdvanceAsync (Approve → next step / last → Completed; Reject → Cancelled), CancelAsync, GetLogsAsync. `WorkflowDefinition.Parse` sequential-workflow-designer JSON.
5. [ ] **W-05** `sequential-workflow-designer` CDN entegrasyonu — `workflow-designer.js` wrapper IIFE
6. [ ] **W-06** `WorkflowController` — `Index` (liste) + `Create/Edit` (designer canvas) + `Instance` (aktif adım görünümü)
7. [ ] **W-07** Workflow designer Razor view — canvas + adım property panel + kaydet
8. [x] **W-08** ✅ Unit test — 8 WorkflowEngine test (Start/Advance/Approve/Reject/Cancel/Complete/Logs ordering). Full regression: 368/368 yeşil.

### Faz B — Hangfire Job + Bildirim + Escalation (8-10h)

9. [ ] **W-09** `WorkflowStepProcessor` Hangfire job — her dakika aktif adım kontrol
10. [ ] **W-10** `WorkflowNotifier.cs` — atanan kullanıcıya in-app + email bildirim
11. [ ] **W-11** Escalation logic — deadline+1 gün geçmişse yöneticiye bildirim
12. [ ] **W-12** ICS endpoint — `/Workflow/Instance/{id}.ics` — deadline'ları Outlook'a export
13. [ ] **W-13** Approve/Reject action — `POST /Workflow/Instance/{id}/Step/{stepId}/Respond`

### Faz C — Modül Entegrasyonu (6-8h)

14. [ ] **W-14** `ObligationsController` — yükümlülük oluşturulunca workflow tetikle (opsiyonel template seçimi)
15. [ ] **W-15** `ContractsController` — sözleşme onay workflow tetikleme
16. [ ] **W-16** Instance dashboard widget — ana sayfada "bekleyen onaylarım" paneli
17. [ ] **W-17** Smoke test — end-to-end: template oluştur → instance başlat → onayla → tamamlandı

### Faz D — SOP Entegrasyonu (Plan 34 sonrası, 4-6h)

18. [ ] **W-18** SOP adımları → workflow instance bağlantısı
19. [ ] **W-19** Tamim onay zinciri (Plan 17 Faz H — bildirim — bu faz kapanır)

**Toplam tahmin:** 30-40 saat (4-5 gün)

---

## 8. İlişkili

- **Mosaik.Core:** `Mosaik.Core/Workflow/IWorkflow.cs` (mevcut iskelet)
- **Plan 34:** [SOP/Prosedür Yönetimi](34-sop-prosedur-yonetimi.md) — Faz D bağımlılığı
- **Plan 32:** [Scheduled Reports Email](32-scheduled-reports-email-distribution.md) — SMTP altyapısı (W-10 bağımlılığı)
- **Plan 33:** [Modül Tamamlama Roadmap](33-modul-tamamlama-roadmap.md) — zemin temizliği
- **VISION.md:** "Workflow Designer + Onay Akışları — en yüksek leverage iş"
- **ADR-002:** Modular monolith — `IWorkflow` Core abstraction cross-modül iletişim kuralı
- **TODO ID'leri:** Plan 36 / W-01..W-19
- **Journal:** `docs/journal/2026-05-19.md`
- **Library:** [sequential-workflow-designer](https://github.com/nocode-js/sequential-workflow-designer) (MIT)

---

## 9. Onay

> Kullanıcı onay verene kadar implement edilmez.

- [x] Plan kullanıcıya gösterildi
- [x] Geri bildirim alındı — sequential-workflow-designer onaylandı, eksikler proje içinde genişletilecek
- [x] Onay alındı: 2026-05-19, Fikri
