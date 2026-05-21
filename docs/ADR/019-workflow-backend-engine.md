# ADR-019 — Workflow Backend Engine: Stateless + Hangfire

**Tarih:** 2026-05-21
**Statü:** Önerildi (Plan 36 Faz A başlamadan onay)
**Karar verenler:** Fikri / Claude
**Bağlam:** Plan 36 Workflow Designer + Plan 42 Process Execution Runtime; OSS araştırma `docs/RESEARCH_OSS_VNEXT_2026-05-21.md`

---

## 1. Bağlam

Plan 36 onaylandı (2026-05-19) `sequential-workflow-designer` (MIT, zero-dep frontend) + custom backend engine yaklaşımıyla. Backend engine somut isim olarak belirsizdi ("custom WorkflowEngine"). Plan 42 Process Execution Runtime (2026-05-21) ProcessInstance + 6 aspect polymorphic timeline ekledi — workflow engine onun bir aspect'i.

OSS araştırma (5 paralel agent, `docs/RESEARCH_OSS_VNEXT_2026-05-21.md`) sonucu .NET workflow engine ekosistemini detay tarama yapıldı.

## 2. Karar

**Workflow backend engine = Stateless 5.20.1 (Apache-2.0) + Hangfire (mevcut) + custom WorkflowEngine wrapper.**

Stateless 5.x:
- Hierarchical Finite State Machine
- Async entry/exit actions
- External state persistence (Mosaik DB'ye `WorkflowInstance.StateJson`)
- ~700 satır C# kütüphane
- API 5 method: `Configure`, `Permit`, `OnEntry`, `OnExit`, `Fire`
- .NET 6/8/9/10 multi-target

Hangfire (mevcut, projede kullanımda):
- Cron + scheduled jobs
- Persistence Mosaik SQL Server
- Dashboard `/hangfire`
- Plan 36 step deadline + Plan 42 SLA timer kanalı

Mapper: `sequential-workflow-designer` JSON workflow definition → Stateless `Configure(stateName).Permit(trigger, nextState)` config (~50-80 satır C#).

## 3. Reddedilen Alternatifler

| Aday | Lisans | Reddetme gerekçesi |
|---|---|---|
| **Elsa 3.6** | MIT, 7.4k★ | Designer embed yok (Elsa Studio Blazor WASM ayrı), Plan 42 ProcessInstance + 6 aspect Elsa entity'leri ile çakışıyor, "10-25 workflows incredibly inefficient" GitHub şikayet, dokümantasyon v2/v3 karışıklığı, 1-2 hafta öğrenme. Net +2 hafta zarar. |
| **Workflow Core 3.17** | MIT, 5.4k★ | Yarı aktif (son NuGet Eki 2025), designer yok — sequential-workflow-designer ile birlikte kullanılırsa Stateless ile fark kapanıyor. Stateless daha hafif. |
| **Camunda 8 / Zeebe** | Source-available, prod lisans zorunlu | Multi-component (Zeebe + Tasklist + Operate + Identity + Keycloak), BKM kurumsal IT operasyonel yük, 6-12 ay lisans satın alma süreci, BKM 5-10 workflow × N×1000 instance ölçeği için aşırı. |
| **Temporal.io** | MIT engine, Cloud $200+/ay | Cassandra cluster + Elasticsearch self-host SRE yükü, 4 kişilik BKM IT karşılayamaz, BKM ölçeği için aşırı. |
| **MassTransit Saga** | Apache-2.0 | Saga = distributed transaction pattern, Mosaik tek-DB monolit, yanlış pattern. |
| **DIY pure Hangfire + state JSON** | İç | 200-300 satır boilerplate (state transition validation, guard clauses, hierarchical state, sync/async entry actions). Stateless zaten çözüyor. |

## 4. Gerekçe

- **Persistence-agnostic:** Stateless DB'ye state yazmaz, Mosaik kontrolünde — multi-tenant `FirmaId` her query pattern'i değişmeden çalışır.
- **Plan 42 uyumlu:** Stateless state machine WorkflowInstance seviyesinde yaşar, ProcessInstance üst entity polymorphic kalır. Plan 36 + Plan 42 entity şemaları çakışmaz.
- **sequential-workflow-designer uyumlu:** Designer JSON'ı Stateless config'ine mapper trivial.
- **Hangfire mevcut:** Yeni scheduler dep yok.
- **Hiyerarşik state desteği:** Plan 36 Faz 2 koşullu branch için hazır.
- **Öğrenme eğrisi:** 1 gün (API 5 method).
- **Test edilebilir:** State machine in-memory build, unit test hızlı.
- **Maliyet:** $0 (Apache-2.0).
- **Risk düşük:** ~700 satır → gerekirse `Mosaik.Core/Workflow/` altına internal copy + attribution.

## 5. Effort Etkisi

| Faz | Baseline | Stateless ile | Tasarruf |
|---|---|---|---|
| Plan 36 Faz A WorkflowEngine | 12h | 8h | -33% |
| Plan 36 Faz 2 conditional branch | 16h | 10h | -38% |
| Plan 42 Faz 1 Workflow hookup | 8-10h | 5-7h | -30% |
| Plan 42 Faz 4 SLA Timer | 8-10h | 6-8h | -20% |
| **Toplam** | **~44-48h** | **~29-33h** | **~%30, -15-20h** |

## 6. Riskler

| Risk | Önlem |
|---|---|
| Stateless tek bakıcı (Nicholas Blumhardt + DotNet State Machine org) bakım yavaşlar | ~700 satır → internal copy Mosaik.Core/Workflow/ + Apache-2.0 attribution |
| State JSON migration (template revise aktif instance kırar) | Plan 36 §10: instance immutable snapshot; yeni template yeni instance |
| Hangfire job kaybı (server restart) | Plan 42 §5: failsafe daily sweep |
| Designer JSON ↔ Stateless config drift | Mapper unit test, Plan 36 Faz A done criteria |

## 7. Sonuç

NuGet: `dotnet add package Stateless` (sürüm `5.20.1` pin'le).

Plan 36 §3 Alternatif C metni güncellendi — Stateless somut isim eklendi.

Plan 42 §9 cross-reference güncellendi — "Stateless OnEntry callback → ProcessInstance.AssignedToId update".

Toplam vNext kalbi içinde ~15-20h tasarruf + risk azalır + Stateless vendor lock-in yok.
