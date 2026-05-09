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
    ('vergi', 'KDV Beyannamesi',            1, 1, 0, 26, NULL, 5,  'Aylık KDV beyannamesi gönderimi'),
    ('vergi', 'Muhtasar Beyannamesi',       1, 1, 0, 26, NULL, 5,  'Aylık muhtasar ve prim hizmetleri beyannamesi'),
    ('vergi', 'Kurumlar Vergisi Beyanı',    1, 1, 2, 25, 4,    14, 'Yıllık kurumlar vergisi beyannamesi (Nisan)'),
    ('vergi', 'Geçici Vergi Beyannamesi',   1, 1, 1, 17, NULL, 7,  'Çeyreklik geçici vergi beyannamesi'),
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
