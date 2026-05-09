# Plan 22 — Resmi Tatiller + Önemli Günler + Reminder Engine

**Tarih:** 2026-05-10
**Yazan:** Claude
**Durum:** `Taslak`

---

## 1. Problem

Mosaik'te resmi tatil / önemli gün entity'si yok. ContractObligation/ContractEvent'in `ReminderDays` kolonu var ama hiçbir background job bunları işleyip Notifications tablosuna yazmıyor. Hangfire altyapısı kurulu (`/hangfire`), Notifications tablosu generic olarak hazır (`Database/41_CreateNotifications.sql`) — sadece Holiday entity + DailyReminderJob eksik. Kullanıcı "hazır önemli günler ile ilgili bir şeyler de vardı sanki" dedi — gerçekte yoktu, eklemek gerek.

## 2. Scope

### Kapsam dahili
- `Holidays` tablosu: sistem-tanımlı resmi tatiller + dini bayramlar (`IsSystemDefined=1`).
- `HolidayOccurrences` tablosu: lunar bayramların yıl-yıl çözülmüş tarihleri (Ramazan/Kurban 2026-2030 seed).
- `ImportantDates` tablosu: kullanıcı/firma tanımlı önemli günler (yıldönümü, denetim, etkinlik).
- 9 sabit Türkiye resmi tatili seed: Yılbaşı, 23 Nisan, 1 Mayıs, 19 Mayıs, 15 Temmuz, 30 Ağustos, 29 Ekim (yarım günler dahil).
- `vw_CalendarUnified` view: Holiday + ImportantDate + ContractEvent + ContractObligation birleşik.
- `CalendarController.Events()` filter chip: kaynak (tatil/önemli/sözleşme/yükümlülük) toggle.
- `DailyReminderJob` Hangfire RecurringJob — her gün 08:00 TR — bugün+ReminderDays içinde olan event'lere `Notifications` insert.
- Idempotency UNIQUE index: `(NotificationType, EntityType, EntityId, UserId, CAST(CreatedAt AS DATE))`.
- Admin CRUD: `/Admin/Holidays` (sistem read-only edit, dini bayram yıl ekleme), `/Admin/ImportantDates` (full CRUD).

### Kapsam dışı
- Diyanet API entegrasyonu (lunar tarih otomatik çekme) — manuel yıllık seed.
- Email reminder channel — Plan 16.5 Faz F adayı, şu an in-app Notification yeterli.
- Firma-bazlı tatil farkı (BKM Bursa şubesinde mahalli tatil) — Plan 18 Branch Master ile birleşir.
- PDKS/izin hesaplama Holiday okur — Plan 18B HR Sync sonrası.

### Etkilenen dosyalar
- `Mosaik/Models/Holiday.cs`, `HolidayOccurrence.cs`, `ImportantDate.cs`
- `Mosaik/Database/52_CreateHolidays.sql` — 3 tablo + view + seed
- `Mosaik/Database/53_SeedTurkishHolidays.sql` — 9 sabit tatil + dini bayram 2026-2030
- `Mosaik/Services/IHolidayService.cs`, `IImportantDateService.cs`
- `Mosaik/Services/Jobs/DailyReminderJob.cs` — Hangfire job
- `Mosaik/Program.cs` — `RecurringJob.AddOrUpdate<DailyReminderJob>` (Turkey Standard Time)
- `Mosaik/Controllers/HolidaysController.cs` (admin), `ImportantDatesController.cs`
- `Mosaik/Controllers/CalendarController.cs` — vw_CalendarUnified entegrasyonu
- `Mosaik/Views/Admin/Holidays/*.cshtml`, `ImportantDates/*.cshtml`
- `Mosaik/Views/Calendar/Index.cshtml` — kaynak filter chip + renk

**Tahmini boyut:** 14 dosya / ~700 satır.

## 3. Alternatifler

### A: Tek tablo `CalendarItems` polymorphic
**Açıklama:** Holiday + ImportantDate + ContractEvent hepsi tek tabloda, `Source` kolonu ayırır.
**Reddetme sebebi:** ContractEvent zaten ayrı entity, security-hardening Plan 25.1 var. Polymorphic delete-guard her yere yayılır. Ayrı tablo + view union daha temiz.

### B: IHostedService + Timer (Hangfire'dan ayrı)
**Açıklama:** Custom BackgroundService + cron parser.
**Reddetme sebebi:** Hangfire zaten kurulu, dashboard var, retry/persistence çözülmüş. İkinci paralel sistem kurma.

### C: Ayrı tablolar + view union + Hangfire RecurringJob (SEÇİLEN)
**Açıklama:** Holidays/HolidayOccurrences/ImportantDates ayrı, vw_CalendarUnified union, DailyReminderJob Hangfire'da.
**Sebep:** Mevcut altyapıyla maksimum uyum, schema bulanıklığı yok, idempotency UNIQUE index ile çözülür.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Lunar bayram tarihi yanlış seed (Ramazan/Kurban) | orta | orta | Diyanet 5 yıllık takvimi PDF'ten manuel doğrulama, seed yorum satırında kaynak. |
| DailyReminderJob duplicate notification | yüksek | orta | UNIQUE index `(NotificationType, EntityType, EntityId, UserId, Date)` — INSERT IGNORE. |
| TimeZone yanlış (UTC olarak çalışıp 11:00 TR'de tetikler) | orta | orta | `RecurringJobOptions { TimeZone = "Turkey Standard Time" }` explicit. |
| 2031'de seed bitince tatil görünmez | düşük | yüksek (zamanla) | Admin "Lunar Bayram Yıl Ekle" buton — yıl seçer, manuel tarih girer. |
| Hangfire job restart sonrası enqueue kaybolur | düşük | düşük | Hangfire SqlServerStorage zaten persistent, recurring job DB'de kayıtlı. |

## 5. Done Criteria

- [ ] Migration 52+53 uygulanır, 9 resmi tatil + 2026-2030 dini bayram seed.
- [ ] `/Calendar` ekranında tatil chip filtre çalışır, renk farklı.
- [ ] `/Admin/Holidays` admin sistem tatili Label edit edebilir, Code lock.
- [ ] `/Admin/ImportantDates` full CRUD.
- [ ] `DailyReminderJob` test ortamında manuel trigger ile 1 obligation için notification yazar.
- [ ] UNIQUE index duplicate notification atar.
- [ ] Hangfire dashboard'da `daily-reminder-check` recurring görünür, next exec 08:00 TR.

## 6. Rollback Planı

- Git revert.
- Migration down: `DROP VIEW vw_CalendarUnified; DROP TABLE ImportantDates, HolidayOccurrences, Holidays`.
- `RecurringJob.RemoveIfExists("daily-reminder-check")` Program.cs'den.

## 7. Adımlar

1. [ ] 3 entity + migration 52 + EF config + DbSets
2. [ ] Migration 53: 9 sabit + 2026-2030 dini seed
3. [ ] `vw_CalendarUnified` view
4. [ ] `IHolidayService` + `IImportantDateService`
5. [ ] HolidaysController + ImportantDatesController + 4 admin view
6. [ ] CalendarController vw_CalendarUnified entegrasyonu + filter chip
7. [ ] `DailyReminderJob` + UNIQUE index migration + Program.cs RecurringJob
8. [ ] Test: idempotency + TZ + manual trigger

## 8. İlişkili

- DikkatIQ `DailyObligationCheckJob` referans pattern (D:\Dev\DikkatIQ)
- Plan 16.5 Faz E ileride Core'a port (HR/PDKS Holiday okur)
- Plan 18 Branch Master — firma-bazlı mahalli tatil
- Plan 25.1 ReminderDays validation pattern miras

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Onay alındı: ___
