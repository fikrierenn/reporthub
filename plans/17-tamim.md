# Plan 17 — Tamim & Sirküler Modülü (v2 — gerçek mimari)

**Durum:** Faz A iskelet ✅, v1 yanlış pattern revert edildi 2026-05-08, v2 implement bekliyor
**Tier:** 3 (yeni modül + DB + UI + cron job + iş akışı)
**Tarih:** 2026-05-08 (revize)
**Tahmini süre:** ~8-10 saat (4 faz, oturum bölünebilir)
**Önkoşul:** Plan 16.5 ✅ + Plan 16.6 ✅ tamamlandı, Hangfire ekleme bu plan içinde

## 1. Problem

BKM içi günlük operasyonel haberleşme için yapılandırılmış sistem. Mevcut: e-posta + ilan tahtası + WhatsApp grupları → "görmedim/duymadım" bahanesi, takip yok, bilgi kaybı.

**Kullanım modeli (kullanıcı netleştirdi):**
- **Genel Müdürlük çalışanları (mağaza personeli DEĞİL)** gün içinde duyuru/karar/uyarı/hadise kaydı (`GunlukBlok`) girer — kendi departmanları adına
- **Editör** rolü (gerekirse) blokları derler/düzenler/onaylar
- **Akşam 17:00**'de sistem otomatik **bugünün bloklarını derleyip** günlük `Tamim` olarak yayınlar (TamimBlok junction)
- **Tüm Mosaik kullanıcıları (mağaza personeli + GM)** ertesi gün/akşam tamimi okur
- **Okundu logu otomatik** — kim okudu, ne zaman, sayfa ziyaretiyle veya manuel "Okudum" ack
- **Admin** kim okumadı raporu görür

Kaynak: `D:/Dev/tamim/` Next.js + Prisma + node-cron prototipi. Schema 25+ tablo, kullanım patternı netleşti.

## 2. Scope

**Plan 17 kapsamı (v2):**
- `Mosaik.Modules.Tamim` ayrı csproj (Plan 16.6 ✅ kuruldu)
- 4 ana entity: `GunlukBlok`, `Tamim`, `TamimBlok` (junction), `TamimOkudu`
- 3 lookup (`Mosaik.Core.Lookup` üzerinden): `BlokTuru`, `TamimDurum`, `BildirimTuru`
- BlokController CRUD (GM çalışanları)
- TamimController read-only + ack (tüm kullanıcılar)
- AdminController OkumaRaporu (kim okudu/okumadı)
- **17:00 günlük tamim derleme cron job** (Hangfire)
- Bildirim push (Mosaik kullanıcılarına yeni tamim için)

**Kapsam dışı:**
- Mobile push notification (out of scope)
- Mağaza personeli **blok yazamaz** (sadece okur) — kullanıcı kararı
- Direktorluk/Departman master verisi — basit string Department alanı (Plan 18B HR sync ile gerçek hiyerarşi gelir)
- Multi-firma desteği (ASIYE_BINGOLBALI/HEYKEL ayrı tamim) — ileri faz, şimdilik tek firma
- Etiket sistemi — Faz D opsiyonel
- E-posta bildirimi — Plan 18B sonrası SMTP entegrasyon

## 3. Mimari (gerçek pattern)

```
GÜN İÇİ (08:30 - 17:00):
  GM çalışanı → /Tamim/Blok/Create
    GunlukBlok yaratır:
      - Konu (string 200), Aciklama (text)
      - BlokTuru (DictionaryValue ref: Duyuru/Karar/Hadise/Uyarı/Bilgi)
      - DepartmanAdi (string, basit — HR sync sonrası FK olur)
      - AciliyetFlag (bool)
      - EkDosyalar (BlokDosyasi tablo, opsiyonel Faz C)
      - Durum (enum: Taslak / Onayli / Reddedildi)

EDITOR (gerekirse, gün içi):
  /Tamim/Blok → tüm "Taslak" blokların listesi (sadece editor rolü)
  Düzenle / Onayla / Reddet aksiyonları

17:00 CRON (Hangfire RecurringJob):
  TamimDerleyiciJob.RunDailyAsync():
    1. Bugün tarihinde "Onayli" GunlukBlok'ları topla
    2. Eğer ≥1 onaylı blok varsa:
       a. Tamim kaydı oluştur (TamimNo="TAM-20260508", Baslik="08 Mayıs 2026 Günlük Tamim", YayinTarihi=now())
       b. TamimBlok junction ile her bloğu zarfa ekle (siraNo = blok yaratım sırası)
       c. Tamim.Durum = "Yayında"
       d. Tüm aktif Mosaik kullanıcılarına Bildirim insert (yeni tamim push)
       e. Audit log: tamim_published (TamimId, BlokSayisi)
    3. Onaylı blok yoksa: tamim oluşturma (boş tamim olmaz). Audit log: tamim_skipped_no_blocks

KULLANICILAR (mağaza + GM):
  /Tamim → bugünün ve son 7 günün tamim listesi
  /Tamim/{id} → detay sayfası (zarf + bağlı bloklar listesi)
    → ziyaret edince TamimOkudu insert (UserId+TamimId UNIQUE, idempotent)
    → "Okudum" buton → IsAcknowledged=1 set
  /Tamim/Bildirim → okumadığım tamim sayısı badge

ADMIN:
  /Tamim/Admin/OkumaRaporu/{tamimId}
    → Aktif kullanıcılar listesi (User.IsActive=1)
    → Solunda yeşil tik (okudu) veya kırmızı x (okumadı)
    → IsAcknowledged ayrımı (sadece açtı vs okudum dedi)
    → Filtreleme: tüm kullanıcılar, sadece okumayan, sadece okuyan
```

## 4. Entity haritası

### `GunlukBlok` (ana giriş entity'si)
```csharp
public class GunlukBlok : BaseEntity
{
    [Key, BindNever] public int Id { get; set; }
    [Required, MaxLength(50), BindNever] public string BlokNo { get; set; } = "";  // BLK-20260508-001 (otomatik)
    public int OlusturanId { get; set; }              // User.UserId FK
    [MaxLength(100)] public string DepartmanAdi { get; set; } = "";  // basit string (HR sync sonrası FK)
    [Required, MaxLength(200)] public string Konu { get; set; } = "";
    [Required] public string Aciklama { get; set; } = "";  // text/HTML
    public DateTime BlokTarihi { get; set; } = DateTime.UtcNow.Date;
    public int BlokTuruId { get; set; }               // Mosaik.Core.Lookup.DictionaryValue.Id
    public BlokDurum Durum { get; set; } = BlokDurum.Taslak;
    public bool Acil { get; set; }
    [MaxLength(500)] public string? RedSebebi { get; set; }  // editor reddederse
    public int? OnaylayanId { get; set; }
    public DateTime? OnayTarihi { get; set; }
}
public enum BlokDurum { Taslak = 0, Onayli = 1, Reddedildi = 2, Yayinda = 3 }
```

### `Tamim` (zarf — Body yok!)
```csharp
public class Tamim : BaseEntity
{
    [Key, BindNever] public int Id { get; set; }
    [Required, MaxLength(50), BindNever] public string TamimNo { get; set; } = "";  // TAM-20260508 (cron set)
    [Required, MaxLength(200)] public string Baslik { get; set; } = "";  // "08 Mayıs 2026 Günlük Tamim"
    public DateTime TamimTarihi { get; set; }         // Hangi günün tamimi
    public DateTime YayinTarihi { get; set; }         // Cron yayın anı
    public int BlokSayisi { get; set; }               // denormalized (tablo yansıma için)
    public bool Acil { get; set; }                    // herhangi bir blok acil mi
}
```

### `TamimBlok` (junction)
```csharp
public class TamimBlok : BaseEntity
{
    [Key, BindNever] public int Id { get; set; }
    public int TamimId { get; set; }
    public int BlokId { get; set; }
    public int SiraNo { get; set; }
}
```

### `TamimOkudu` (read log)
```csharp
public class TamimOkudu : BaseEntity
{
    [Key, BindNever] public int Id { get; set; }
    public int TamimId { get; set; }
    public int UserId { get; set; }                   // Mosaik User.UserId FK
    public DateTime IlkGorulme { get; set; } = DateTime.UtcNow;  // sayfa ziyaret
    public bool Okundu { get; set; }                  // "Okudum" buton
    public DateTime? OkumaZamani { get; set; }
    // Unique: (TamimId, UserId)
}
```

## 5. Faz Planı (revize)

### Faz A — İskelet ✅ TAMAM (commit 38d6efe)
Mevcut yapı korunuyor: `Mosaik.Modules.Tamim` csproj + `IMosaikModule` impl + boş Index sayfası.

### Faz B — Hangfire + Lookup seed + Entity scaffold (~3h) ⭐ SIRADAKİ
- B1. **Hangfire entegrasyonu** (Mosaik'te yok):
  - `Mosaik.csproj`'a `Hangfire.AspNetCore` + `Hangfire.SqlServer` paketi
  - `Program.cs`: `services.AddHangfire(...)` + `app.UseHangfireDashboard("/hangfire")` (admin-only)
  - DB: `HANGFIRE` schema otomatik yaratılır
  - Migration 38: Hangfire schema doğrulama (eğer otomatik yaratılmadıysa)
- B2. **Lookup seed** (Mosaik.Core.DictionaryType + Value):
  - Migration 39: `BlokTuru` DictionaryType yaratıp 5 değer (Duyuru, Karar, Hadise, Uyarı, Bilgi) seed
  - `BildirimTuru` 3 değer (TamimYayinlandi, BlokOnayBekliyor, BlokReddedildi)
- B3. **Entity'leri yaz** (Mosaik.Modules.Tamim/Models/):
  - GunlukBlok.cs, BlokDurum enum
  - Tamim.cs (zarf, Body YOK)
  - TamimBlok.cs (junction)
  - TamimOkudu.cs (read log)
- B4. **TamimModule.ConfigureModelBuilder** entity config + index
- B5. Migration 40 (modul-içi): `Mosaik.Modules.Tamim/Database/01_CreateTamimV2Tables.sql`
  - `dbo.GunlukBlok` (BlokNo UNIQUE)
  - `dbo.Tamim` (TamimNo UNIQUE)
  - `dbo.TamimBlok` (TamimId+BlokId UNIQUE)
  - `dbo.TamimOkudu` (TamimId+UserId UNIQUE)
- B6. Build + test korundu (273 yeşil)

### Faz C — Servis + Controller + UI (~3h)
- C1. `IBlokService` + `BlokService` (CRUD + Onayla + Reddet)
- C2. `ITamimService` + `TamimService` (Read + Listele + OkumaKaydet)
- C3. `BlokController` (Areas Tamim/Blok): Index/Details/Create/Edit/Delete/Onayla/Reddet
- C4. `TamimController` (Areas Tamim/Tamim): Index (son 30 gün)/Details (oto okuma kaydı)
- C5. ViewModels: BlokFormViewModel, TamimListItemViewModel, OkumaRaporuViewModel
- C6. Views (modern CSS pattern):
  - Blok/Index, Blok/Details, Blok/Create, Blok/Edit
  - Tamim/Index (kart liste), Tamim/Details (zarf + bloklar listesi)
- C7. Sidebar entegrasyonu — Tamim modülü açıkken görünür (Plan 12)

### Faz D — Hangfire job + bildirim + okuma raporu (~2h)
- D1. `TamimDerleyiciJob` (Hangfire RecurringJob, cron `0 17 * * *`):
  - Bugünün onaylı bloklarını topla
  - Tamim oluştur + TamimBlok junction
  - Bildirim insert (her aktif kullanıcıya)
  - Audit log: tamim_published
- D2. Manuel tetikleme endpoint: admin için (`/Tamim/Admin/DerleSimdi` — test amaçlı, audit log)
- D3. `BildirimController` minimal: kullanıcının okumadığı bildirim badge sayısı
- D4. `AdminController.OkumaRaporu/{tamimId}`:
  - Aktif kullanıcı listesi vs TamimOkudu join
  - Filter: tümü / okuyan / okumayan
  - Export CSV (opsiyonel)
- D5. Sidebar bildirim badge entegrasyonu

## 6. Mimari kararlar

**Hangfire seçimi:**
- Mosaik'te şu an background job altyapısı yok. Quartz.NET veya BackgroundService alternatifleri var ama Hangfire en yaygın + dashboard hazır + DB-backed (server restart safe).
- DB'de `HANGFIRE` schema yaratılır, ana Mosaik DB'sinde (ayrı DB değil — basitlik).

**Cross-modül FK pattern (Plan 16.6'dan):**
- `GunlukBlok.OlusturanId`, `TamimOkudu.UserId` → `Mosaik.Models.User.UserId` cross-csproj FK
- Navigation property YOK, sadece int FK
- Service layer User lookup yapar (`_db.Set<User>().Find(id)` veya host'tan via DI sağlanır)

**BlokTuru — neden Lookup, neden enum değil:**
- Admin yeni blok türü ekleyebilmeli (örn. "Etkinlik Duyurusu") build redeploy olmadan
- Mosaik.Core.Lookup zaten kuruldu (Plan 16.5 Faz B)
- DictionaryType.Code = "blokTuru", her DictionaryValue = bir tür
- Migration 39 ilk 5 değer seed eder

**Mağaza personeli neden blok yazamaz:**
- Kullanıcı kararı: GM iletişim merkezi, mağaza personeli alıcı
- BlokController.Create [Authorize(Roles = "admin,editor,gm")] — mağaza rolü dışında
- Mosaik mevcut rol yapısı: User.Roles CSV (deprecated) + UserRole tablo. Mevcut roller incelenip uyumlu olanlar kullanılır.

**TamimNo formatı:**
- `TAM-YYYYMMDD` günlük (cron tek tamim oluşturur)
- Eğer aynı gün çoklu tamim gerekirse `TAM-YYYYMMDD-001` (faz E ileri)

## 7. Riskler

| Risk | Mitigation |
|---|---|
| **Hangfire DB schema** Mosaik DB'sine yaratılıyor — performans/güvenlik | Hangfire schema separate kalır, küçük yük (~10 tablo). Production'da ayrı DB önerilebilir (Plan 17.5) |
| **17:00 cron timezone** — UTC vs Turkey | `DateTime.UtcNow` yerine TZConvert ile İstanbul saati. Cron expression `0 14 * * *` (UTC 14:00 = TR 17:00) |
| **Boş gün** (hiç onaylı blok yok) | Tamim oluşturma, audit "tamim_skipped_no_blocks" + admin bildirim |
| **Editor onay bottleneck** | Faz E opsiyonel: GM çalışanı kendi bloğunu **doğrudan onaylı** yazsın (editor onay opsiyonel). Şimdilik manuel onay zorunlu |
| **Cross-modül FK validation** — User silindiğinde GunlukBlok orphan | Soft-delete User pattern (mevcut Mosaik User.IsActive). FK NOT cascade. Service layer null check |
| **TamimOkudu büyür** (300 user × 365 tamim × 5 yıl ≈ 550K satır) | Index doğru kurulu, partition gerek yok. Faz E'de archive job |
| **Plan 12 IModuleService cache** — yeni AppModule değişikliği reflect olmazsa | Singleton service `Invalidate()` çağrısı seed sonrası |

## 8. Done Criteria

**Faz A:** ✅ tamam (commit 38d6efe)

**Faz B (BU OTURUMDA):**
- [ ] Hangfire entegre, `/hangfire` dashboard erişilebilir (admin)
- [ ] Migration 38 (Hangfire), 39 (Lookup seed), 40 (Tamim v2 tabloları) uygulandı
- [ ] 4 entity dosyası + TamimModule.ConfigureModelBuilder dolu
- [ ] Build 0 warning, 273 test korundu

**Faz C:**
- [ ] BlokController + TamimController CRUD/read aksiyonları
- [ ] 8+ view dosyası (Blok ve Tamim için)
- [ ] Sidebar'da Tamim görünüyor
- [ ] Smoke: editor blok yarat → onayla → manuel tamim derle → liste/detay göster

**Faz D:**
- [ ] TamimDerleyiciJob Hangfire RecurringJob kayıtlı
- [ ] Manuel "Derle Şimdi" buton çalışıyor (audit log)
- [ ] OkumaRaporu sayfası (admin) okuyan/okumayan listesi
- [ ] Bildirim badge sidebar'da

## 9. Sıralama

1. **Faz B** (~3h) — Hangfire + Lookup + Entity + Migration
2. **Faz C** (~3h) — UI ve service
3. **Faz D** (~2h) — Cron + bildirim + okuma raporu

Toplam ~8h. 1-2 oturumda biter.

## 10. Rollback

- Faz B: Migration 38/39/40 revert + entity dosyaları sil + Hangfire paket geri al
- Faz C: Controller + view + service dosyaları sil
- Faz D: Hangfire RecurringJob `RecurringJob.RemoveIfExists("tamim-derleyici")`

## 11. v1 yanlış pattern revert notu (2026-05-08)

İlk implement (commit 69e4196) `Title + Body` generic blog pattern'iyle yapıldı — kullanıcı "uydurma mı" sorduğunda netleşti, kaynak `tamim/` projesi tamamen farklı pattern (operasyonel günlük blok + zarf + cron). Manuel revert:
- v1 dosyaları (Tamim+TamimReadLog entity, TamimService, HomeController CRUD, Create/Edit/Details views) silindi
- DB tabloları DROP (Migration 37)
- TamimModule + HomeController + Index.cshtml Faz A iskelet hâline geri döndü
- Plan 16.6 altyapısı (cross-csproj DbContext factory + ModelBuilder hook) korundu

Saf kayıp ~1 saatlik kod, ama Plan 16.6 mimarisi validate edildi (RCL views + Areas + ProjectReference + DI factory + ModelBuilder hook = hepsi çalışıyor).
