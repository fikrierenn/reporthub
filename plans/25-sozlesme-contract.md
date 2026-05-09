# Plan 25 — Sözleşme Modülü (DikkatIQ Port)

**Durum:** Taslak  
**Tier:** 3 (yeni schema + 3+ klasör + kullanıcı-görünür)  
**Referans:** ADR-012 (PK=int, Scheduler=IHostedService, FirmaFilter=session claim)  
**Kaynak:** DikkatIQ.Core.Entities (Contract, Obligation, RecurrenceRule, Document, AiExtraction)

---

## Problem

BKMKİTAP grubunun 3 firması (BKMKİTAP, Bursa Kültür, Asiye Bingölbali) hukuki ve mali sözleşme
yükümlülüklerini takip edemiyor. DikkatIQ bu sorunu çözmüş bir uygulamadır; Mosaik'e port edilecek.

---

## Kapsam

| Bileşen | Açıklama |
|---|---|
| `Firma` entity | Ortak güvenlik sınırı (BKM_GENEL=1, BURSA_KÜLTÜR=2, ASİYE=3) |
| `Contract` | Sözleşme başlığı, karşı taraf, kategori, durum |
| `ContractObligation` | Yükümlülük (vade, tutar, tekrar, durum) |
| `ContractRecurrence` | Tekrar kuralı (aylık/çeyreklik/yıllık/özel) |
| `ContractFile` | Yüklenen dosya (PDF, DOCX) |
| `ContractAiExtraction` | AI çıkarma sonucu (durum, raw JSON, token sayısı) |
| `ICurrentUserService` | FirmaId + SubeId session claim'den |
| `AiPipelineQueue` | `Channel<int>` — on-demand AI extraction tetikleyici |
| `AiExtractionWorker` | `BackgroundService` — sıralı işleme + retry |

---

## Alternatifler (reddedilen)

- **Guid PK:** 14+ entity int PK üzerine kurulu, cross-join tip uyumsuzluğu → ADR-012 Karar 1
- **Hangfire AI pipeline:** Mosaik zaten Hangfire kullanıyor (Plan 17) ama 4 job için yeni dependency debt
  > Council verdict: `Channel<int>` + `BackgroundService` yeterli → ADR-012 Karar 2
- **EF Global Query Filter:** EF+SP dual filter sync yükü, test zorluğu → ADR-012 Karar 3
- **Ayrı Mosaik.Modules.Sozlesme projesi:** Tamim pattern, ama önce core entity'ler ana Mosaik'te;
  modül ayrımı Faz 2'de

---

## Riskler

- **FirmaId NULL mevcut User'larda:** Migration nullable, mevcut kullanıcılar FirmaId=NULL başlar;
  login sırasında Admin set eder
- **AiExtraction channel kapasitesi:** 100 kapasiteli bounded channel; taşarsa backpressure ile
  kullanıcıya hata dönülür (istenen: goroutine-leak değil, explicit hata)
- **PdfPig lisansı:** Apache 2.0 — ticari kullanımda sorun yok

---

## Done Criteria

- [ ] 6 entity DB'de oluştu (migration 42 + 43)
- [ ] ICurrentUserService.FirmaId login claim'den okunuyor
- [ ] AiPipelineQueue.EnqueueAsync çağrısı → AiExtractionWorker işliyor
- [ ] ContractAiExtraction.Status DB'ye yazılıyor (Processing → AwaitingReview)
- [ ] dotnet build && dotnet test — yeşil

---

## Rollback

Migration 42/43 manuel DROP — henüz prod data yok.

---

## Adımlar

### Faz A — Entity + Migration (bu session)
1. Enums: `ContractEnums.cs`
2. `Firma.cs` entity
3. `User.cs` → FirmaId nullable FK ekle
4. 5 Contract entity (BaseEntity<int> + FirmaId zorunlu)
5. Migration 42: Firma tablosu + seed (3 kayıt)
6. Migration 43: Contract, Obligation, Recurrence, File, AiExtraction tabloları
7. `MosaikContext` DbSet güncellemesi

### Faz B — Servis Katmanı (bu session)
1. `ICurrentUserService` + `CurrentUserService` (HttpContext claims)
2. `AiPipelineQueue` (Channel<int> singleton wrapper)
3. `AiExtractionWorker` (BackgroundService + retry 3x exponential)
4. `Program.cs` DI kayıtları
5. AuthController → login'de FirmaId claim ekle

### Faz C — UI (sonraki session)
1. Sözleşme listesi + detay view
2. Yükümlülük listesi + durum güncelleme
3. PDF yükleme + AI extraction tetikleme
4. Admin: firma atama (User edit)

---

## İlişkili

- ADR-012 — PK/Scheduler/FirmaFilter kararları
- Plan 16.5 — Mosaik.Core.AI (AiSummaryProvider hazır, kullanılacak)
- Plan 18B — HR Sync (FirmaId User'a eklendikten sonra sync mantığı)
