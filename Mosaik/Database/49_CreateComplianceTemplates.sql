-- Migration 49: ComplianceTemplate tablosu + seed (Uyum modülü)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComplianceTemplate')
BEGIN
    CREATE TABLE dbo.ComplianceTemplate (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        PackageName  NVARCHAR(100) NOT NULL,
        Title        NVARCHAR(200) NOT NULL,
        Category     INT NOT NULL DEFAULT 0,  -- ObligationCategory enum
        Type         INT NOT NULL DEFAULT 0,  -- ObligationType enum
        Recurrence   INT NOT NULL DEFAULT 0,  -- RecurrenceType enum
        DayOfMonth   INT NULL,
        MonthOfYear  INT NULL,
        ReminderDays INT NOT NULL DEFAULT 7,
        Description  NVARCHAR(500) NULL,
        IsActive     BIT NOT NULL DEFAULT 1,
        CreatedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_ComplianceTemplate_PackageName ON dbo.ComplianceTemplate (PackageName);
    CREATE INDEX IX_ComplianceTemplate_IsActive     ON dbo.ComplianceTemplate (IsActive);
END
GO

-- Seed: Temel uyum şablonları
-- Category: 1=Tax,2=Hr,3=Finance,4=Legal / Type: 0=Payment,1=Tax,2=Compliance,3=Renewal,4=Deadline,5=Audit
-- Recurrence: 0=Monthly,1=Quarterly,2=Yearly,3=Custom
IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceTemplate WHERE PackageName = 'vergi')
BEGIN
    INSERT INTO dbo.ComplianceTemplate (PackageName, Title, Category, Type, Recurrence, DayOfMonth, MonthOfYear, ReminderDays, Description)
    VALUES
    -- KDV: takip eden ayın 28'i (VUK Md.41)
    ('vergi', 'KDV Beyannamesi',            1, 1, 0, 28, NULL, 5,  'Aylık KDV beyannamesi — takip eden ayın 28''i'),
    -- Muhtasar: takip eden ayın 26'sı
    ('vergi', 'Muhtasar Beyannamesi',       1, 1, 0, 26, NULL, 5,  'Aylık muhtasar ve prim hizmetleri beyannamesi — takip eden ayın 26''sı'),
    -- Kurumlar Vergisi: 30 Nisan (yıllık)
    ('vergi', 'Kurumlar Vergisi Beyanı',    1, 1, 2, 30, 4,    14, 'Yıllık kurumlar vergisi beyannamesi — 30 Nisan'),
    -- Gelir Vergisi: 31 Mart (yıllık, gerçek kişi)
    ('vergi', 'Gelir Vergisi Beyannamesi',  1, 1, 2, 31, 3,    14, 'Yıllık gelir vergisi beyannamesi — 31 Mart'),
    -- Geçici Vergi: dönem+2. ayın 17'si (Q1→17 Mayıs, Q2→17 Ağustos, Q3→17 Kasım); hafta sonu → ertesi iş günü
    ('vergi', 'Geçici Vergi Beyannamesi',   1, 1, 1, 17, NULL, 7,  'Çeyreklik geçici vergi beyannamesi — dönem+2. ayın 17''si'),
    ('ik',    'SGK Aylık Bildirgesi',       2, 2, 0, 23, NULL, 5,  'Aylık SGK prim bildirgesi gönderimi'),
    ('ik',    'İşe Giriş / Çıkış Bildirimi',2,2, 0, NULL,NULL, 1,  'Personel değişikliklerinde SGK bildirimi'),
    ('ik',    'Yıllık İzin Takibi',         2, 2, 2, 1,  1,    30, 'Yıllık izin hakları güncellenmesi'),
    ('finans','Banka Ekstresi Mutabakatı',  0, 0, 0, 1,  NULL, 3,  'Aylık banka hesapları mutabakatı'),
    ('finans','Sigorta Poliçe Yenileme',    0, 3, 2, NULL,NULL,30, 'Yıllık sigorta poliçe yenileme kontrolü'),
    ('hukuk', 'Sözleşme Son Tarih Kontrolü',5, 4, 1, NULL,NULL,30, 'Çeyreklik sözleşme bitiş tarihi revizyonu'),
    ('hukuk', 'Lisans Yenileme',            5, 3, 2, NULL,NULL,30, 'Yıllık yazılım lisansları yenileme'),
    ('it',    'Güvenlik Güncellemesi',      4, 2, 0, NULL,NULL, 7, 'Aylık sistem güvenlik güncellemeleri'),
    ('it',    'Yedekleme Kontrolü',         4, 2, 0, NULL,NULL, 1, 'Aylık yedekleme doğrulaması');
END
GO

-- Seed: Calendar + AI modüllerini AppModules tablosuna ekle
IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'calendar')
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType)
    VALUES ('calendar', 'Takvim', 1, 25, 'extension');

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'compliance')
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType)
    VALUES ('compliance', 'Uyum', 1, 30, 'extension');

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'ai')
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType)
    VALUES ('ai', 'AI Analiz', 1, 35, 'extension');
GO
