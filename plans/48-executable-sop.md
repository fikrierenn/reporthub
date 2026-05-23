# Plan 48 — Executable SOP (Doküman → Canlı Süreç)

**Durum:** 🔬 ARAŞTIRMA TASLAĞI 2026-05-25 — kullanıcı strategic input (radikal paradigm 1)
**Tier:** 3 (yeni paradigma + AI parse + workflow auto-gen + cross-modül)
**Effort:** 60-90h (6 faz, 5-7 hafta) — tahmini, deep dive sonrası netleşir
**Aciliyet:** 🟣 Plan 36 (Workflow Designer) ✅ + Plan 34 SOP ✅ sonrası — vNext kalbinin **üst katmanı**

---

## 1. Vizyon

SOP dokümanı = pasif metin. Açıp okuyup imzalanıp tozlanır. Aktör değil.

**Radikal fikir:** SOP dokümanı = **executable code**. Yöneticinin yazdığı doğal dil prosedürü kaydederken **arka planda** Stateless onay motoruna State/Transition tanımlanır + Hangfire SLA timer'lar register edilir + Task assignment kuralları doğar.

Süreç tasarlamak = prosedürü yazmak. Form Builder/Workflow Designer manuel UI'a gerek kalmaz.

---

## 2. Senaryo (BKM gerçek)

> İK yöneticisi Mosaik SOP editöründe yazar:
>
> *"Yeni işe giren personele 3 gün içinde İSG eğitimi atanmalı. Personel eğitimi 7 gün içinde tamamlamazsa yöneticisine uyarı maili atılmalı."*
>
> **Kaydet** → Mosaik Qwen 3B semantik parse:
> - **Entity:** yeni personel (User created event subscription)
> - **Action 1:** İSG eğitimi assign (Task entity) — trigger: HiredAt + 0 gün, deadline: HiredAt + 3 gün
> - **State Machine:** [Assigned → InProgress → Completed]
> - **Timer 1:** Hangfire job "İSG eğitimi assign" cron daily 09:00, query User WHERE HiredAt within last 0 day
> - **Timer 2:** Hangfire SLA timer "Eğitim 7 gün overdue check" — query Task WHERE Status != Completed AND HiredAt + 7 day < now
> - **Action 2:** Yöneticiye uyarı mail (INotificationService) — trigger: Timer 2 fired
>
> Yönetici prosedürü "3 gün" → "5 gün" diye günceller → Mosaik diff parse → Timer parametresi **dinamik yeniden register**.

---

## 3. Mimari (ön araştırma)

### 3.1 Component'ler

```
Mosaik.Modules.SOP/Services/Executable/
├─ SopSemanticParser.cs        — Qwen + skill catalog → IntermediateRepresentation (IR)
├─ IrToWorkflowCompiler.cs     — IR → Stateless StateMachine definition
├─ IrToTimerScheduler.cs       — IR → Hangfire RecurringJob/SLA timer specs
├─ IrToTaskTemplate.cs         — IR → Task entity templates (assign rule)
├─ SopExecutionRegistry.cs     — SopVersion → workflow/timer/task mapping kaydı
└─ SopExecutionMigrator.cs     — Diff old IR vs new IR → ALTER (re-register, cancel obsolete)
```

### 3.2 Intermediate Representation (IR)

```json
{
  "sop_id": 42,
  "version": 3,
  "triggers": [
    { "type": "entity_created", "entity": "User", "event": "Hired" }
  ],
  "actions": [
    {
      "id": "isg_assign",
      "type": "create_task",
      "target": { "entity": "User", "ref": "trigger.entity" },
      "assign_to": "isg_sorumlusu_role",
      "deadline_offset": "P3D",
      "title": "İSG Eğitimi"
    }
  ],
  "timers": [
    {
      "id": "isg_overdue",
      "type": "sla_overdue",
      "watch_task": "isg_assign",
      "threshold": "P7D",
      "on_fire": {
        "type": "notification",
        "target": "trigger.entity.manager",
        "channel": "email",
        "template": "isg_overdue_warning"
      }
    }
  ],
  "state_machine": {
    "states": ["Assigned", "InProgress", "Completed", "Overdue"],
    "initial": "Assigned",
    "transitions": [...]
  }
}
```

### 3.3 Risk haritası (deep dive bekleyen)

| Risk | Etki |
|---|---|
| LLM yanlış parse → yanlış otomatik süreç → operasyonel felç | **CRITICAL** — preview + onay zorunlu |
| Doğal dil belirsizliği ("3 gün" = takvim mi iş günü mü?) | Yüksek — TR business calendar context |
| Diff parse yanlış cancel + re-register → timer gap (downtime) | Yüksek — versioning + dual-run window |
| Yönetici farkında olmadan production workflow değiştirir | Yüksek — staging + approval flow zorunlu |
| Skill catalog güncel olmayınca semantic drift | Orta — versioned skill + audit |
| Hangfire job orphan (cancel olmadı, prosedür silindi) | Orta — Registry GC daily job |

---

## 4. Deep Dive Sonraki Adımlar

Bu plan **araştırma taslağı**. Implementation öncesi gerekli ön çalışma:

1. **POC (1 hafta):** Tek pattern (SLA timer) — yöneticinin yazdığı 5 örnek prosedürü manuel IR'a çevir, IR'dan Hangfire job auto-gen, sonuç beklendiği gibi mi?
2. **5 lens deep tradeoff:** Contrarian (LLM hallucinate operasyonu kilitler), First Principles (asıl problem: süreç tasarımı), Expansionist (sadece SOP değil → Form Builder + Compliance), Outsider (Power Automate/Zapier neden başaramadı), Executor (POC code)
3. **Bağımlılık değerlendirme:** Plan 34 SOP ✅, Plan 36 Workflow ✅, Plan 17 Notification ✅, Plan 38 EntityRelations ✅ — temel hazır
4. **ADR-030 adayı:** "Executable SOP IR + diff migration mimarisi"

---

## 5. Bağımlılıklar (ön)

- **Hard prereq:** Plan 36 Workflow Designer (Stateless backend)
- **Hard prereq:** Plan 34 SOP modülü (entity + versioning)
- **Reuse:** Plan 34.1 LLamaSharp + Qwen + skill catalog
- **Reuse:** Plan 38 EntityRelations (process ↔ entity bağlama)
- **Reuse:** Plan 17 NotificationService
- **Bağımsız:** Plan 41-47

---

## 6. Bu plan ne zaman aktif olur?

vNext kalbi (Plan 36+40+41+42+44) production'da 3 ay stabil olduktan sonra. **Erken implement = LLM hallucinate operasyonu kilitler.** Log birikmesi + güven inşası + skill catalog olgunlaşma gerek.

**Tahmini başlangıç:** 2026 Q3 (Eylül-Ekim).

---

## 7. Açık Sorular

1. **POC scope ne?** — Önerim: SLA timer + Task auto-assign (en basit pattern).
2. **IR format JSON mı, AST mı, DSL mi?** — Önerim: JSON IR (debugging kolay) + opsiyonel görsel DSL Plan 48.1.
3. **LLM parse'ı her save'de mi, manuel "Süreçleştir" butonu ile mi?** — Önerim: manuel buton (hata maliyeti yüksek + transparency).
4. **Versioning: eski versiyon prosedür → eski IR'lar canlı kalır mı?** — Önerim: SopVersion approve → yeni IR aktif, eski IR cancel + audit. Dual-run window 24h.
5. **Override: LLM IR yanlış parse ederse manuel düzeltme?** — Önerim: IR editor (JSON edit UI) admin için + audit log.
