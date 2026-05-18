-- Migration 60: ComplianceTemplate ek paketler — taşıt, emlak, belediye + vergi eksikleri
-- (vergi paketi migration 49'da var; burası yeni paketler + eksik satırlar)

-- Damga Vergisi (vergi paketine ekle)
-- Idempotency: PackageName + Title composite key (Title aynı ama farklı paketler için seperate insert garantili)
IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceTemplate WHERE PackageName = 'vergi' AND Title = 'Damga Vergisi Beyannamesi')
BEGIN
    INSERT INTO dbo.ComplianceTemplate (PackageName, Title, Category, Type, Recurrence, DayOfMonth, MonthOfYear, ReminderDays, Description)
    VALUES ('vergi', 'Damga Vergisi Beyannamesi', 1, 1, 0, 26, NULL, 3, 'Aylık damga vergisi beyannamesi — takip eden ayın 26''sı');
END
GO

-- KDV Tevkifat 2 No'lu
IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceTemplate WHERE PackageName = 'vergi' AND Title = 'KDV Tevkifat Beyannamesi (2 No''lu)')
BEGIN
    INSERT INTO dbo.ComplianceTemplate (PackageName, Title, Category, Type, Recurrence, DayOfMonth, MonthOfYear, ReminderDays, Description)
    VALUES ('vergi', 'KDV Tevkifat Beyannamesi (2 No''lu)', 1, 1, 0, 26, NULL, 3, 'Aylık KDV tevkifat beyannamesi — takip eden ayın 26''sı');
END
GO

-- Gelir Vergisi 2. Taksit (yıllık — beyan Mart'ta, 2. ödeme Temmuz)
IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceTemplate WHERE PackageName = 'vergi' AND Title = 'Gelir Vergisi 2. Taksit Ödemesi')
BEGIN
    INSERT INTO dbo.ComplianceTemplate (PackageName, Title, Category, Type, Recurrence, DayOfMonth, MonthOfYear, ReminderDays, Description)
    VALUES ('vergi', 'Gelir Vergisi 2. Taksit Ödemesi', 1, 0, 2, 31, 7, 5, 'Yıllık gelir vergisi 2. taksit ödemesi — 31 Temmuz');
END
GO

-- SGK Prim Ödemesi (ik paketine ekle — bildirge 23, ödeme 26 farklı)
IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceTemplate WHERE PackageName = 'ik' AND Title = 'SGK Prim Ödemesi')
BEGIN
    INSERT INTO dbo.ComplianceTemplate (PackageName, Title, Category, Type, Recurrence, DayOfMonth, MonthOfYear, ReminderDays, Description)
    VALUES ('ik', 'SGK Prim Ödemesi', 2, 0, 0, 26, NULL, 3, 'Aylık SGK prim ödemesi — takip eden ayın 26''sı (bildirgeden ayrı)');
END
GO

-- Motorlu Taşıtlar Vergisi paketi
IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceTemplate WHERE PackageName = 'tasit')
BEGIN
    INSERT INTO dbo.ComplianceTemplate (PackageName, Title, Category, Type, Recurrence, DayOfMonth, MonthOfYear, ReminderDays, Description)
    VALUES
    ('tasit', 'MTV 1. Taksit', 1, 0, 2, 31, 1, 5, 'Motorlu taşıtlar vergisi 1. taksit ödemesi — Ocak sonu'),
    ('tasit', 'MTV 2. Taksit', 1, 0, 2, 31, 7, 5, 'Motorlu taşıtlar vergisi 2. taksit ödemesi — Temmuz sonu');
END
GO

-- Emlak & Arazi Vergisi paketi
IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceTemplate WHERE PackageName = 'emlak')
BEGIN
    INSERT INTO dbo.ComplianceTemplate (PackageName, Title, Category, Type, Recurrence, DayOfMonth, MonthOfYear, ReminderDays, Description)
    VALUES
    ('emlak', 'Emlak Vergisi 1. Taksit',  1, 0, 2, 31, 5,  5, 'Bina/arsa emlak vergisi 1. taksit — Mayıs sonu'),
    ('emlak', 'Emlak Vergisi 2. Taksit',  1, 0, 2, 30, 11, 5, 'Bina/arsa emlak vergisi 2. taksit — Kasım sonu'),
    ('emlak', 'Arazi Vergisi 1. Taksit',  1, 0, 2, 31, 5,  5, 'Arazi/tarla vergisi 1. taksit — Mayıs sonu'),
    ('emlak', 'Arazi Vergisi 2. Taksit',  1, 0, 2, 30, 11, 5, 'Arazi/tarla vergisi 2. taksit — Kasım sonu');
END
GO

-- Belediye vergileri paketi
IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceTemplate WHERE PackageName = 'belediye')
BEGIN
    INSERT INTO dbo.ComplianceTemplate (PackageName, Title, Category, Type, Recurrence, DayOfMonth, MonthOfYear, ReminderDays, Description)
    VALUES
    ('belediye', 'İlan ve Reklam Vergisi', 1, 1, 2, 31, 1, 7, 'Yıllık ilan ve reklam vergisi beyannamesi — Ocak sonu');
END
GO

-- AppModules: yeni paketler için ek kayıt gerekmez (compliance modülü zaten var)
