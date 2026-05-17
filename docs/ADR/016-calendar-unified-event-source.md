# ADR-016 — Calendar Unified Event Source

**Tarih:** 2026-05-17  
**Durum:** Kabul edildi  
**Yazan:** Claude

---

## Bağlam

Mosaik takvimine şu an yalnızca `ContractEvents` tablosu besleniyor (`CalendarController.Events()`). Plan 22 kapsamında `Holidays`, `HolidayOccurrences`, `ImportantDates` ekleniyor. Bunların hepsini tek bir takvim görünümünde göstermek için birleştirme stratejisi gerekiyor.

---

## Karar

**SQL View (`vw_CalendarUnified`) + kaynak renk kodlaması.**

### Unified view şeması

```sql
CREATE VIEW vw_CalendarUnified AS
-- 1. Manuel/AI sözleşme olayları
SELECT Id, FirmaId, Title, EventDate, 'ContractEvent' AS SourceType, ...
FROM ContractEvents
UNION ALL
-- 2. Yükümlülük son tarihleri
SELECT Id, FirmaId, Title, DueDate, 'Obligation', ...
FROM ContractObligations WHERE Status IN ('Pending','InProgress')
UNION ALL
-- 3. Resmi tatiller (FirmaId = 0 = sistem geneli)
SELECT h.Id, 0, h.Name, o.Date, 'Holiday', ...
FROM Holidays h JOIN HolidayOccurrences o ON h.Id = o.HolidayId
UNION ALL
-- 4. Önemli günler
SELECT Id, FirmaId, Title, EventDate, 'ImportantDate', ...
FROM ImportantDates
```

### EventSource enum genişletme

`ContractEnums.cs`'deki `EventSource` enum'una `Holiday` ve `ImportantDate` değerleri eklenir. Bu enum `ContractEvent.Source` için kullanılmaktayı; view `SourceType` string kolonu JSON'a direkt gider (enum değil).

### CalendarController değişikliği

`Events()` action'ı view'dan okur, `source` query param ile kaynak filtresi desteklenir:
```
GET /Calendar/Events?year=2026&month=5&sources=ContractEvent,Holiday
```
Boş `sources` → hepsini döner.

### Renk paleti (SourceType → backgroundColor)

| SourceType | Renk |
|---|---|
| ContractEvent | `var(--accent)` → `#5b6bff` |
| Obligation | `var(--danger)` → `#e63946` |
| Holiday | `#2d9e4a` |
| ImportantDate | `#e07b00` |

---

## Alternatifler

### A: Her kaynak için ayrı endpoint
Reddetme: 4 ayrı `/Calendar/ContractEvents`, `/Calendar/Holidays` vb. FullCalendar'a 4 eventSource yüklemek istemci-tarafı kargaşası, CORS güvensiz, filtre state çoğalır.

### B: Uygulama katmanında UNION (C# LINQ)
Reddetme: 4 farklı DbSet üzerinde IQueryable UNION yapılamaz (farklı tip). ToList() + concat = N+1 ve in-memory sort. Sayfalama imkânsız.

### C: Polymorphic tek tablo
Reddetme: ContractEvent zaten ayrı entity, FirmaId güvenlik sınırı var. Yeni tek tabloya migrate etmek Plan 25 sertleşmesiyle çelişir.

---

## Sonuçlar

- `vw_CalendarUnified` migration 58'de yaratılır.
- `CalendarController.Events()` raw SQL view query ile değiştirilir (`FromSqlRaw` veya `_db.Database.SqlQueryRaw`).
- Calendar Index view'da `source` toggle chip'leri eklenir (ADR-011 subnav pattern).
- Holiday FirmaId=0 → tüm firmalar görür. ImportantDate FirmaId kullanıcı firmasına ait.
- `EventSource` enum `ContractEvent.Source` için değil; gelecekte `ImportantDate.Source` (manuel vs. AI import) için kullanılacak. View `SourceType` string kolonunu JSON serialize eder.

---

## İlişkili

- Plan 22 — Resmi Tatiller + Önemli Günler implementasyonu
- ADR-006 — UTC datetime convention (holiday dates DateOnly, UTC saat yok)
- Plan 18 — Firma-bazlı mahalli tatil (ADR-016 extend edilecek)
