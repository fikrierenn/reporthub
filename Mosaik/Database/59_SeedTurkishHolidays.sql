-- Migration 59: Türkiye resmi tatilleri seed (Plan 22)
-- 9 sabit tatil + Ramazan/Kurban Bayramı 2026-2030
-- Kaynak: Cumhurbaşkanlığı resmi takvim + Diyanet İşleri Başkanlığı

-- Sabit tatiller
INSERT INTO Holidays (Name, Code, Type, FixedMonth, FixedDay, IsHalfDay, IsSystemDefined, IsActive) VALUES
('Yılbaşı',                    'TR_NEW_YEAR',     'National', 1,  1,  0, 1, 1),
('Ulusal Egemenlik ve Çocuk Bayramı', 'TR_APR23', 'National', 4,  23, 0, 1, 1),
('Emek ve Dayanışma Günü',     'TR_MAY1',         'National', 5,  1,  0, 1, 1),
('Atatürk''ü Anma / Gençlik ve Spor Bayramı', 'TR_MAY19', 'National', 5, 19, 0, 1, 1),
('Demokrasi ve Millî Birlik Günü', 'TR_JUL15',    'National', 7,  15, 0, 1, 1),
('Zafer Bayramı',              'TR_AUG30',        'National', 8,  30, 0, 1, 1),
('Cumhuriyet Bayramı Öncesi',  'TR_OCT28_HALF',   'HalfDay',  10, 28, 1, 1, 1),
('Cumhuriyet Bayramı',         'TR_OCT29',        'National', 10, 29, 0, 1, 1);

-- Dini bayramlar (FixedMonth/Day NULL — lunar, yıllık HolidayOccurrences ile çözülür)
INSERT INTO Holidays (Name, Code, Type, FixedMonth, FixedDay, IsHalfDay, IsSystemDefined, IsActive) VALUES
('Ramazan Bayramı',            'TR_RAMAZAN',      'Religious', NULL, NULL, 0, 1, 1),
('Kurban Bayramı',             'TR_KURBAN',       'Religious', NULL, NULL, 0, 1, 1);

-- Sabit tatiller için 2026-2030 occurrences (otomatik)
DECLARE @yr INT = 2026;
WHILE @yr <= 2030
BEGIN
    INSERT INTO HolidayOccurrences (HolidayId, Year, Date)
    SELECT h.Id, @yr, DATEFROMPARTS(@yr, h.FixedMonth, h.FixedDay)
    FROM Holidays h
    WHERE h.FixedMonth IS NOT NULL AND h.IsActive = 1;
    SET @yr = @yr + 1;
END;

-- Ramazan Bayramı tarihleri 2026-2030 (Diyanet takvimi)
-- Arefe dahil değil — sadece bayram 1. günü. Tatil günleri: 3 gün.
DECLARE @ramazan INT = (SELECT Id FROM Holidays WHERE Code = 'TR_RAMAZAN');
DECLARE @kurban  INT = (SELECT Id FROM Holidays WHERE Code = 'TR_KURBAN');

INSERT INTO HolidayOccurrences (HolidayId, Year, Date) VALUES
(@ramazan, 2026, '2026-03-20'),
(@ramazan, 2027, '2027-03-09'),
(@ramazan, 2028, '2028-02-27'),
(@ramazan, 2029, '2029-02-15'),
(@ramazan, 2030, '2030-02-04');

-- Kurban Bayramı tarihleri 2026-2030
INSERT INTO HolidayOccurrences (HolidayId, Year, Date) VALUES
(@kurban, 2026, '2026-05-27'),
(@kurban, 2027, '2027-05-16'),
(@kurban, 2028, '2028-05-04'),
(@kurban, 2029, '2029-04-24'),
(@kurban, 2030, '2030-04-13');
