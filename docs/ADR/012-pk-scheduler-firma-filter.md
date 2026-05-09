# ADR-012 — PK Tipi, Scheduler ve Firma Filtresi

**Tarih:** 2026-05-09  
**Durum:** Kabul Edildi  
**Bağlam:** DikkatIQ → Mosaik.Core.AI + Plan 25 port kararları (LLM Council oturumu)

---

## Karar 1 — PK Tipi: `int Identity`

**Karar:** Plan 25 ve sonraki tüm vNext entity'lerinde **`int Identity`** PK kullanılır. Guid'e geçilmez.

**Gerekçe:**
- Mosaik'in mevcut 14+ entity'si (User, Role, ReportCatalog, OrgPosition vb.) int PK üzerine kurulu
- BKMKİTAP grubu harici sisteme API expose etmiyor; Guid'in distributed/federation faydası geçersiz
- Hibrit int+Guid sistemi → cross-entity join'lerde kalıcı tip uyumsuzluğu
- Solo dev + 246 test + migration zinciri: Guid geçişi en az 2 günlük yatırım, YAGNI

**Reddedilen alternatif:** Guid (DikkatIQ pattern'i) — kaynak projenin multi-tenant/distributed gerekçesi Mosaik'e uygulanamaz.

---

## Karar 2 — Scheduler: `IHostedService + Channel<int>`, Hangfire yok

**Karar:** Mosaik'e Hangfire eklenmez. Background işler .NET native araçlarıyla çözülür.

| Job Tipi | Çözüm |
|---|---|
| Periyodik (günlük/aylık) | `BackgroundService + PeriodicTimer` |
| On-demand + retry | `BackgroundService + Channel<int>` |

**AiExtractionPipelineJob karşılığı:**
```csharp
// Singleton in-memory queue
services.AddSingleton<AiPipelineQueue>();      // Channel<int> wrapper
services.AddHostedService<AiExtractionWorker>(); // BackgroundService

// Retry: try/catch + exponential backoff, 3 deneme, 15 satır
// Persist: AiExtraction.Status=Processing DB'ye yazılır, restart recovery
//          timeout job (DailyCheck) 24 saat sonra stale'leri temizler
```

**Gerekçe:**
- Hangfire: SQL job tablosu + polling + dashboard = gereksiz bağımlılık, solo dev için overkill
- `Channel<T>` on-demand trigger + retry ihtiyacını karşılar
- `PeriodicTimer` (.NET 6+) cron alternatifsiz ve test edilebilir

**Reddedilen alternatif:** Hangfire — 4 job için dependency debt > fayda.

---

## Karar 3 — Firma Filtresi: Session Claim + Explicit `.Where()`

**Bağlam:** Mosaik aynı instance'ta 3 farklı tüzel kişiliğe hizmet eder:

| Firma | DB Kodu | Şubeler | Personel |
|---|---|---|---|
| BKMKİTAP | `BKM_GENEL` | FSM, ÖZLÜCE, İSTANBUL YOLU | 231 |
| Bursa Kültür | `BURSA_KÜLTÜR_MERKEZİ` | Heykel Şube | 28 |
| Asiye Bingölbali | `ASİYE_BİNGÖLBALİ` | Şura Şube | 12 |

Firma = **güvenlik sınırı** (izolasyon), Şube = operasyonel filtre.

**Karar:** EF Global Query Filter eklenmez. SP-only UserDataFilter yetmez. Doğru katman:

```csharp
// 1. ICurrentUserService genişletilir
public interface ICurrentUserService
{
    int UserId { get; }
    string Username { get; }
    int FirmaId { get; }    // YENİ — session claim'den
    int? SubeId { get; }    // YENİ — opsiyonel, şube bazlı erişimde
}

// 2. Yeni EF entity'lerde FirmaId zorunlu FK
public class Contract : BaseEntity
{
    public int FirmaId { get; set; }  // zorunlu
    // ...
}

// 3. EF LINQ sorgularında explicit filtre
var contracts = await _db.Set<Contract>()
    .Where(x => x.FirmaId == _currentUser.FirmaId)
    .AsNoTracking()
    .ToListAsync();

// 4. SP çağrılarında UserDataFilter'a firma key eklenir
// FilterKey="firmaId", FilterValue="1"  (BKM_GENEL=1)
```

**Gerekçe:**
- EF Global Query Filter: test izolasyonu zorlaşır, SP katmanıyla sync tutmak gerekir
- Explicit `.Where()`: okunabilir, debug edilebilir, her sorgu izlenebilir
- Session claim: login sırasında `FirmaId` claim olarak yazılır, her request'te mevcut

**FirmaId → int mapping (migration seed gerekli):**
```sql
-- Migration NN_AddFirmaTable.sql
INSERT INTO dbo.Firmas (FirmaId, Kod, Ad) VALUES
(1, 'BKM_GENEL',            'BKMKİTAP'),
(2, 'BURSA_KÜLTÜR_MERKEZİ', 'Bursa Kültür'),
(3, 'ASİYE_BİNGÖLBALİ',    'Asiye Bingölbali');
```

**Reddedilen alternatifler:**
- EF Global Query Filter — dual filter (EF+SP) sync yükü, test zorluğu
- SP-only UserDataFilter — EF LINQ sorgularını kapsamıyor

---

## İlişkili Planlar

- Plan 16.5 Faz C — Mosaik.Core.AI (bu ADR LLM provider kararlarını etkilemez)
- Plan 25 — Sözleşme modülü (FirmaId FK zorunlu, int PK, IHostedService AI pipeline)
- Plan 18B — HR Sync (FirmaId bazlı UserDataFilter, SubeId eşleme)

## Referanslar

- LLM Council oturumu: `docs/journal/2026-05-09.md`
- Firma yapısı: `memory/project_firma_yapisi.md`
- DikkatIQ analizi: 6 agent çıktısı (AI layer, Domain, Hangfire, Web, Tests, Settings)
