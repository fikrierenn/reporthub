# Plan 19 — Documents v2 (Versiyonlama + Güvenli Serve + UploadedBy)

**Tarih:** 2026-05-09 (orijinal taslak)
**Yazan:** Claude
**Durum:** ⛔ **SUPERSEDED 2026-05-14** — [Plan 27 Faz C](27-documents-ai-roadmap.md) "DMS Foundation" bu planı absorbe etti (satır 39: "Versiyonlama + check-in/check-out (Plan 19 absorbe edilir)"). Bu plan dosyası referans için kalır; **implementasyon Plan 27 Faz C üzerinden yürütülür**. [VISION.md](../docs/VISION.md) §3 — Documents çakışması bu kararla çözüldü.

---

## 1. Problem

Mevcut `DocumentsController` + `ContractFile` entity yetersiz: (a) `Version` alanı sabit `=1`, aynı dosyanın yeni versiyonunu yükleme yok, (b) dosyalar `wwwroot/uploads/` altında doğrudan erişilebilir (Plan 25.1 Faz 1'de App_Data'ya taşıma TODO'su var), (c) `UploadedBy` alanı yok — kim yükledi denetlenemiyor, (d) Upload form'da Obligation seçici yok, (e) PDF upload sonrası `/Ai/Review/{id}` redirect yok (DikkatIQ pattern).

## 2. Scope

### Kapsam dahili
- `ContractFile` entity'ye `UploadedBy` (string) + `IsCurrentVersion` (bit) + parent `OriginalFileId` (int? self-FK) ekle.
- Versiyonlama akışı: aynı `(ContractId, FileName)` kombinasyonu varsa `Version++`, eski kayıt `IsCurrentVersion=0`.
- `GET /Documents/Download/{id}` controller endpoint — Content-Disposition + FileStream serve. wwwroot doğrudan erişim **kapatılır** (App_Data'ya taşı, Plan 25.1 Faz 1 ile birleştirilir).
- Upload form'una Obligation dropdown (contractId seçilince filtrelenmiş).
- PDF upload sonrası `Ai/Review/{id}` redirect.
- Index sayfasına versiyon geçmişi popover (aynı OriginalFileId'ye sahip kayıtlar listesi).

### Kapsam dışı
- Tag sistemi (Plan 16.7 ayrı plan, dependency).
- Bağımsız Plan 19 "Doküman modülü" (sözleşme dışı genel doküman) — ayrı plan slot'una düşer (ileride 19.1).
- OCR / full-text search.
- Paylaşım linki, izin yönetimi.
- Dosya önizleme (PDF inline).

### Etkilenen dosyalar (tahmin)
- `Mosaik/Models/ContractFile.cs` — yeni alanlar
- `Mosaik/Database/50_ContractFileVersioning.sql` — migration
- `Mosaik/Controllers/DocumentsController.cs` — Upload + Download + DeleteVersion
- `Mosaik/Views/Documents/Index.cshtml` — Obligation dropdown + version popover
- `Mosaik/Models/MosaikContext.cs` (veya `ReportPanelContext.cs`) — yeni FK + index

**Tahmini boyut:** 5 dosya / ~250 satır.

## 3. Alternatifler

### A: Versiyon ayrı tablo (`ContractFileVersion`)
**Açıklama:** Her versiyonu ayrı kayıt yerine ayrı tabloda topla.
**Reddetme sebebi:** İki tablo join + EF konfigürasyon overhead. DikkatIQ pattern de tek tablo + Version int.

### B: Wwwroot'ta kal, sadece `[Authorize]` middleware ile koru
**Açıklama:** Static file'lara ASP.NET middleware gate.
**Reddetme sebebi:** Static file middleware'i kullanıcıya kim hangi dosyaya erişebilir kararını veremez (firma + role kontrolü). Controller serve şart.

### C: App_Data + Controller serve + tek tablo Version int (SEÇİLEN)
**Açıklama:** Dosyalar `App_Data/contracts/{firmaId}/`, `Download/{id}` controller serve, `Version` kolonu artar, `IsCurrentVersion` flag.
**Sebep:** DikkatIQ pattern ile uyum, Plan 25.1 Faz 1 ile doğal birleşme, audit log için single-source.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Mevcut `wwwroot/uploads/` dosyalarının App_Data'ya migrate edilmesi | yüksek | yüksek | Migration script yazma — `Database/Migrate-FilesToAppData.sql` + manuel `Move-Item` PowerShell. Backup şart. |
| Production'da yüklü dosya zaten varken `IsCurrentVersion=1` flag set | orta | yüksek | Migration `UPDATE ContractFiles SET IsCurrentVersion=1 WHERE IsCurrentVersion IS NULL` idempotent. |
| Download endpoint'inde firma erişim kontrolü atlanır | yüksek | düşük | `firmas.Contains(file.FirmaId)` kontrolü her endpoint'te (Index/Upload/Download/Delete). |

## 5. Done Criteria

- [ ] Migration 50 uygulanır, 246 test geçer.
- [ ] Aynı dosya tekrar yüklenince Version=2 olur, eski kayıt `IsCurrentVersion=0`.
- [ ] `wwwroot/uploads/` altında yeni dosya **oluşmaz** (App_Data'ya yazar).
- [ ] `/Documents/Download/{id}` indirme çalışır (tarayıcıda Content-Disposition: attachment).
- [ ] Upload form Obligation dropdown contractId değişince ajax filtrelenir.
- [ ] PDF upload → `/Ai/Review/{id}` redirect.

## 6. Rollback Planı

- Git revert commit.
- Migration 50 down: `ALTER TABLE ContractFiles DROP COLUMN UploadedBy, IsCurrentVersion, OriginalFileId`.
- Static file middleware tekrar `wwwroot/uploads/` serve.

## 7. Adımlar

1. [ ] Entity + migration 50 + EF config
2. [ ] App_Data taşıma + `wwwroot/uploads` legacy okuma fallback
3. [ ] Upload akışı (versiyon mantığı + UploadedBy + Obligation FK)
4. [ ] `Download/{id}` controller serve
5. [ ] Index view Obligation dropdown + version popover
6. [ ] PDF upload → AI Review redirect
7. [ ] Test (xUnit) — versioning + access control

## 8. İlişkili

- Plan 25.1 (Sözleşme security hardening) Faz 1 ile birleşir
- Plan 16.7 (Tag sistemi) sonradan bağlanır
- DikkatIQ `IDocumentService` referans pattern

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Onay alındı: ___
