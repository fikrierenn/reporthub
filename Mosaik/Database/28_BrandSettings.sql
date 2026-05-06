-- 28_BrandSettings.sql
-- Plan 12 Faz 2a: Marka ayarları tablosu (tek satır, upsert pattern).
-- DB-driven brand: site adı, slogan, logo path, birincil renk.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BrandSettings')
BEGIN
    CREATE TABLE dbo.BrandSettings (
        Id          INT             NOT NULL CONSTRAINT PK_BrandSettings PRIMARY KEY DEFAULT 1,
        SiteTitle   NVARCHAR(100)   NOT NULL DEFAULT N'Mosaik',
        Slogan      NVARCHAR(200)   NULL,
        LogoPath    NVARCHAR(500)   NULL,
        PrimaryColor NVARCHAR(7)   NOT NULL DEFAULT N'#6366f1',
        UpdatedAt   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Tek varsayılan satır — yoksa ekle
IF NOT EXISTS (SELECT 1 FROM dbo.BrandSettings WHERE Id = 1)
BEGIN
    INSERT INTO dbo.BrandSettings (Id, SiteTitle, Slogan, LogoPath, PrimaryColor, UpdatedAt)
    VALUES (1, N'Mosaik', NULL, NULL, N'#6366f1', GETUTCDATE());
END
GO
