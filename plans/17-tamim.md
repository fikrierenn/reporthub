# Plan 17 — Tamim & Sirküler Modülü (v2 minimal — sadeleştirilmiş)

**Durum:** Faz A iskelet ✅, v1 yanlış pattern revert ✅, Faz B v2 minimal implement ✅ 2026-05-08
**Tier:** 3 (yeni modül + DB + UI + cron job + iş akışı)
**Tarih:** 2026-05-08 (revize 2: minimal pattern)
**Tahmini süre kalan:** ~5 saat (Faz C+D)
**Önkoşul:** Plan 16.5 ✅ + Plan 16.6 ✅, Hangfire ✅ kuruldu

**KAVRAM NETLEŞMESİ (2026-05-08, kullanıcı: "iç iletişim ile tamimi karıştırıyorsun"):**

**Tamim NEDİR:** Bağlayıcı, kalıcı, "haberim yoktu" diyilemeyecek konular. Politika/prosedür/uyumluluk/karar dokümanları. Okuma logu yasal kanıt değerinde.

**Tamim NE DEĞİLDİR:** Sosyal duyuru (doğum günü, yeni personel kutlama), geçici operasyonel hadise (asansör arızası, klima sorunu), iç iletişim (toplantı çağrısı, kahvaltı duyurusu), informal mesajlaşma. Bunlar için **ayrı modül** (Plan 27 — İç İletişim / Duyuru Akışı) gerekli.

**Hedefleme:** Tamim default = TÜM kullanıcılar (bağlayıcı, herkesi etkiler). Hedefleme istisna (müdür-only politika gibi nadir durum) — Faz E opsiyonel kapsam, çoğu kullanım gerek duymaz.

---

**SADELEŞTİRME NOTU (2026-05-08, kullanıcı: "gereksiz fazla olanları kaldırabiliriz"):**
- **TamimBlok junction kaldırıldı** — 1:N için gereksiz, `GunlukBlok.TamimId` FK doğrudan bağ
- **TamimOkudu kaldırıldı** — mevcut `Mosaik.Models.AuditLog` (`EventType="tamim_okundu"`) kullanılır
- **Tamim.BlokSayisi + Acil denormalized kolonları kaldırıldı** — COUNT/EXISTS ile hesaplanır
- **GunlukBlok.Durum enum + RedSebebi/OnaylayanId/OnayTarihi kaldırıldı** — `TamimId IS NULL`=bekleyen / `IS NOT NULL`=yayında, onay akışı yok (cron otomatik)
- **GunlukBlok.IsActive eklendi** — soft-delete
- **`IAuditLog` interface Mosaik.Core'a eklendi** — modüller cross-csproj audit yazabilir
- **4 entity → 2 entity** (~%50 schema sadeleşmesi)

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

## 4. Entity haritası (SADELEŞTİRİLMİŞ — 2 entity)

### `GunlukBlok` (ana giriş entity'si)
```csharp
public class GunlukBlok : BaseEntity
{
    [Key, BindNever] public int Id { get; set; }
    [Required, MaxLength(50), BindNever] public string BlokNo { get; set; } = "";  // BLK-20260508-001 (otomatik)
    [BindNever] public int OlusturanId { get; set; }    // User.UserId cross-csproj FK
    [Required, MaxLength(100)] public string DepartmanAdi { get; set; } = "";
    [Required, MaxLength(200)] public string Konu { get; set; } = "";
    [Required, MaxLength(500)] public string Aciklama { get; set; } = "";  // kısa özet
    [Required] public string Icerik { get; set; } = "";  // NVARCHAR(MAX) zengin metin (AI özet kaynağı)
    public DateTime BlokTarihi { get; set; } = DateTime.UtcNow.Date;
    public int BlokTuruId { get; set; }                 // Mosaik.Core.Lookup.DictionaryValue.Id
    public bool Acil { get; set; }
    public int? TamimId { get; set; }                   // NULL=bekleyen, NOT NULL=yayında
    public bool IsActive { get; set; } = true;          // soft-delete
}
```
**Durum bilgisi**: `TamimId IS NULL` = bekleyen, `IS NOT NULL` = yayında.
**Onay akışı YOK** (cron otomatik). Editor onayı ihtiyacı doğarsa Plan 17.1.

### `Tamim` (zarf — Body yok!)
```csharp
public class Tamim : BaseEntity
{
    [Key, BindNever] public int Id { get; set; }
    [Required, MaxLength(50), BindNever] public string TamimNo { get; set; } = "";  // TAM-20260508
    [Required, MaxLength(200)] public string Baslik { get; set; } = "";  // "08 Mayıs 2026 Günlük Tamim"
    public DateTime TamimTarihi { get; set; }    // Hangi günün tamimi
    public DateTime YayinTarihi { get; set; }    // Cron yayın anı
    // BlokSayisi + Acil DENORMALIZED kaldırıldı — COUNT/EXISTS ile
}
```

**İçerik:** `GunlukBlok.TamimId` FK üzerinden bağlı bloklar (`SELECT * FROM GunlukBlok WHERE TamimId=@id ORDER BY Id`).

### Okuma logu — `Mosaik.Models.AuditLog` (mevcut, ekstra tablo yok)

```csharp
await _audit.LogAsync(
    eventType: "tamim_okundu",
    targetType: "tamim",
    targetKey: tamimId.ToString());
```

`IAuditLog` interface (Mosaik.Core.Logging) ile cross-csproj erişim. AuditLogService implement.

**"Okundu mu?" sorgusu:** `EXISTS WHERE EventType='tamim_okundu' AND TargetKey=@id AND Username=@user`
**"Kim okumadı?" raporu:** `Users LEFT JOIN AuditLog ON ... WHERE AuditLog.AuditId IS NULL`

## 4.5. tamim2 Derin Keşif Yansımaları (2026-05-08)

İki subagent code-explorer keşfi sonrası schema + süreç kararları. Detaylı bulgu memory'de (`project_tamim_app_kesif.md`).

**Schema (Faz B'ye eklenecek):**
- `GunlukBlok.Icerik NVARCHAR(MAX) NOT NULL` — tamim2'de yok ama AI sorgular, Mosaik için zorunlu
- Mevcut alanlar (`Konu`, `Aciklama`) yerine kullanılacak değil, ek olarak — `Konu` 200 char başlık, `Aciklama` kısa, `Icerik` zengin metin

**Lookup kararı (kesin):**
- Tamim2: 7 ayrı tablo. Mosaik.Core.Lookup tek tablo (DictionaryType+Value) ✓ — Mosaik pattern korunur
- DictionaryValue'da Color/Icon/SiraNo metadata kontrolü (Faz B1 önkoşulu)

**Reddedilen tamim2 patternleri (Mosaik'te tekrarlanmasın):**
- Self-call AI (GET → POST localhost) → Hangfire job içi tek çağrı
- Cache'siz Gemini → `Tamim.AiOzetJson` immutable kaydı yeter
- Hiç yetki kontrolü → her durum geçişi action'ı `[Authorize(Roles="...")]`
- Hardcoded password → User Secrets + cookie auth (Mosaik default)

**Onaylanan tamim2 referansları:**
- `tamim/services/cronJobs.js` — node-cron pattern (Faz D Hangfire için niyet referans)
- `tamim/services/ai/geminiService.generateTamimSummary` — prompt yapısı (Faz F birebir)
- `src/app/api/dashboard/stats/route.ts` — istatistik SQL (Faz I birebir port)
- `spBlokNoOlustur` / `spTamimNoOlustur` — numara üretim formatı (BLK-YYYYMMDD-NNN, TAM-YYYYMMDD)

## 5. Faz Planı (revize 2 — fark yaratan özellikler eklendi)

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

### Faz E — Dosya Ekleme (~3h) ⭐ Yüksek değer
- E1. `BlokDosyasi` entity (`Mosaik.Modules.Tamim/Models/`)
  - BlokId FK, DosyaAdi, DosyaYolu (server path), DosyaTuru (uzantı), MimeType, Boyut, YuklemeTarihi, IsActive
- E2. `DosyaTuru` lookup (Mosaik.Core.Lookup üzerinden, Migration 39 ek): `pdf, docx, xlsx, jpg, png, txt`. Her tür için mimeType + maxBoyutMB.
- E3. Modül-içi migration `02_AddBlokDosyalari.sql`
- E4. `BlokDosyaService` — upload validation (whitelist + max 10MB), sanitize filename, GUID-based klasör pattern (`/wwwroot/uploads/bloklar/{yyyy}/{MM}/{guid}.{uzantı}`)
- E5. `BlokController.UploadDosya` (POST, [Authorize]) + `DownloadDosya` (GET, yetki kontrolü)
- E6. Blok Create/Edit view'ında multi-file input (HTML5 `<input type="file" multiple>`)
- E7. Blok Details view'ında dosya listesi (icon + tıklanabilir indirme link)
- E8. Tamim Details view'ında bağlı blokların dosyaları toplu görünür

### Faz F — AI Özet (~4h)
- F1. **Plan 16.5 Faz C+D AI Core önkoşulu** — şu an yok, ilk yapılmalı (YonetIQ AiProviderService + PromptEngine)
- F2. AI Core hazır olduğunda: `TamimAiOzetService` (PromptEngine ile)
  - Girdi: Tamim'e bağlı `GunlukBlok[]` (Konu + Aciklama'lar)
  - Çıktı: 5 maddelik kurum-içi özet (paragraf veya bullet)
- F3. `Tamim.AiOzetJson` (NVARCHAR(MAX)) kolonu ekle (Migration 03)
- F4. Tamim Details sayfası başında **AI özet kartı** (önyukar görünüm — "5 saniyede tamamı")
- F5. Admin "Yeniden Üret" buton (önyaklaşık değişimde)
- F6. Maliyet kontrolü: cache TTL 24h (özet bir kez üretilir, değişmiyor zaten — çünkü Tamim immutable)

### Faz G — Export PDF/Excel (~3h)
- G1. **PuppeteerSharp** veya **IronPDF** (NuGet) — Razor → HTML → PDF
- G2. **ClosedXML** zaten Mosaik.csproj'da kurulu (Excel export için)
- G3. Endpoint'ler:
  - `GET /Tamim/Tamim/{id}/Export?format=pdf` → tam tamim PDF (zarf + bloklar + AI özet)
  - `GET /Tamim/Tamim/{id}/Export?format=excel` → blok listesi tablosu
  - `GET /Tamim/Admin/{id}/OkumayanlariExport` → kim okumadı CSV/Excel
- G4. Print-friendly Razor view (`Views/Tamim/Print.cshtml`) — sade, başlık+bloklar+sayfa numarası
- G5. Audit log: `tamim_export_pdf` event (kim ne zaman çıktı aldı)

### Faz H — Bildirim Sistemi (~3h)
- H1. `Bildirim` entity (modül-içi VEYA Mosaik.Core'a — cross-modül kullanım için Core öneri)
  - UserId FK, Baslik, Mesaj, BildirimTuru (lookup), TargetUrl, Okundu, OlusturmaTarihi
- H2. Migration: `Bildirim` tablosu + index `(UserId, Okundu, OlusturmaTarihi)`
- H3. `IBildirimService` (Mosaik.Core.Notification) — cross-modül abstraction (HR, Approval da kullanır)
- H4. Sidebar bildirim badge (okunmamış sayısı, polling 60sn veya SignalR)
- H5. Cron 17:00 sonrası: yeni Tamim yayınlandığında **tüm aktif kullanıcılara** Bildirim insert
- H6. E-posta entegrasyonu (opsiyonel Plan 17.1): `SmtpClient` veya `MailKit` — Tamim acil ise email gönder
- H7. Hatırlatma cron 09:00: önceki gün Tamim'i okumayanlara in-app bildirim (Hatırlatma Bildirimi)

### Faz I — İstatistik Dashboard (~2h)
- I1. `TamimDashboardController` (Mosaik.Modules.Tamim) — Admin-only
- I2. Metrikler (Mosaik mevcut Dashboard motoru kullanılır):
  - **SeenRate**: Aktif kullanıcılar arasında kaç kişi tamim'e en az 1 kez baktı (AuditLog `tamim_okundu` distinct UserId / Toplam aktif user)
  - **AckRate**: Açık ack varsa (Plan 17.x'te `TamimOkudu.Okundu=1` opsiyonel)
  - **Departman bazı giriş tamamlama oranı**: Hangi departmanlar her gün blok yazıyor
  - **Eksik giriş**: Bugün blok yazmamış departmanlar
  - **Onay bekleyen tamim sayısı** (mevcut sistemde editor onay yok, bu metrik şu an 0)
- I3. Chart.js 4 zaten kurulu — mevcut dashboard builder pattern
- I4. `/Tamim/Admin/Dashboard` route, sidebar admin menüsünde

**Faz E-I toplam:** ~15h. Faz D + E-I birleşik **Plan 17 Faz 2** (~17h, 2-3 oturum) olarak planlanabilir. Faz F (AI Özet) Plan 16.5 Faz C+D (AI Core) bağımlı — onlar bitmeden başlanamaz.

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
