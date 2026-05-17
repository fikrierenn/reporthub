-- Migration 58: Resmi Tatiller + Önemli Günler + CalendarUnified view (ADR-016, Plan 22)
-- 2026-05-17

-- 1. Holidays
CREATE TABLE Holidays (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    Name           NVARCHAR(100) NOT NULL,
    Code           NVARCHAR(50)  NOT NULL,
    Type           NVARCHAR(20)  NOT NULL DEFAULT 'National',   -- National | Religious | HalfDay
    FixedMonth     INT NULL,
    FixedDay       INT NULL,
    IsHalfDay      BIT NOT NULL DEFAULT 0,
    IsSystemDefined BIT NOT NULL DEFAULT 1,
    IsActive       BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Holidays_Code UNIQUE (Code)
);

-- 2. HolidayOccurrences (fixed + lunar dates resolved per year)
CREATE TABLE HolidayOccurrences (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    HolidayId  INT NOT NULL REFERENCES Holidays(Id) ON DELETE CASCADE,
    Year       INT NOT NULL,
    Date       DATE NOT NULL,
    CONSTRAINT UQ_HolidayOccurrences_HolidayYear UNIQUE (HolidayId, Year)
);
CREATE INDEX IX_HolidayOccurrences_Date ON HolidayOccurrences(Date);

-- 3. ImportantDates (firma-level custom events)
CREATE TABLE ImportantDates (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    FirmaId      INT NOT NULL REFERENCES Firmas(Id) ON DELETE RESTRICT,
    Title        NVARCHAR(200) NOT NULL,
    EventDate    DATE NOT NULL,
    Notes        NVARCHAR(500) NULL,
    ReminderDays INT NOT NULL DEFAULT 7,
    IsActive     BIT NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_ImportantDates_FirmaDate ON ImportantDates(FirmaId, EventDate);

-- 4. vw_CalendarUnified — ADR-016 birleşik takvim görünümü
CREATE OR ALTER VIEW vw_CalendarUnified AS
-- Sözleşme olayları
SELECT
    ce.Id           AS EventId,
    ce.FirmaId,
    ce.Title,
    ce.EventDate,
    'ContractEvent' AS SourceType,
    ce.Id           AS SourceId,
    ce.Notes,
    ce.ReminderDays,
    NULL            AS HolidayType
FROM ContractEvents ce WHERE ce.EventDate >= DATEADD(year, -1, GETDATE())
UNION ALL
-- Yükümlülük son tarihleri
SELECT
    co.Id,
    co.FirmaId,
    co.Title,
    co.DueDate,
    'Obligation',
    co.Id,
    co.Notes,
    co.ReminderDays,
    NULL
FROM ContractObligations co
WHERE co.Status IN ('Pending','InProgress') AND co.DueDate >= DATEADD(year, -1, GETDATE())
UNION ALL
-- Resmi tatiller (FirmaId = 0 = sistem geneli, tüm firmalar görür)
SELECT
    ho.Id,
    0,
    h.Name,
    ho.Date,
    'Holiday',
    h.Id,
    NULL,
    0,
    h.Type
FROM HolidayOccurrences ho
JOIN Holidays h ON h.Id = ho.HolidayId
WHERE h.IsActive = 1 AND ho.Date >= DATEADD(year, -1, GETDATE())
UNION ALL
-- Önemli günler
SELECT
    id2.Id,
    id2.FirmaId,
    id2.Title,
    id2.EventDate,
    'ImportantDate',
    id2.Id,
    id2.Notes,
    id2.ReminderDays,
    NULL
FROM ImportantDates id2
WHERE id2.IsActive = 1 AND id2.EventDate >= DATEADD(year, -1, GETDATE());
