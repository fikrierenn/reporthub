-- 20_AddFilterDefinition.sql
-- Plan 07 Faz 1 — FilterDefinition master tablosu + 2 seed + backfill.
--
-- Mimari:
-- - FilterDefinition: aktif filter anahtarlarinin master listesi (sube, raporKategori, ...).
-- - UserDataFilters (mevcut): kullanici-bazli atanmis filter degerleri. Schema degismez.
-- - FilterValue='*' magic string = "Hepsi" (deny-by-default'ta gerekli explicit kayit).
-- - Scope='spInjection': SP'ye parametre enjekte (sube), DataSourceKey + OptionsQuery zorunlu.
-- - Scope='reportAccess': Reports/Index liste filtresi (raporKategori), native EF source — OptionsQuery NULL.
--
-- Backfill:
-- - Mevcut 7 aktif user × 2 aktif FilterDefinition icin her FilterKey'e '*' kayit (NOT EXISTS).
-- - Mevcut spesifik kayitlar (admin'in 2 sube kaydi) korunur — sadece kayitsiz user'lara eklenir.
-- - Gecis sonrasi: kimse kapi disinda kalmaz, herseye yetkili baslar, admin sonradan daraltir.
--
-- Plan: plans/07-yetki-filter-revizyon.md (4 Mayis 2026 onayli)
-- ROLLBACK: Database/20_AddFilterDefinition_rollback.sql (DROP TABLE FilterDefinition + '*' kayit cleanup).

USE [PortalHUB];
GO

-- 1. FilterDefinition tablosu
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FilterDefinition')
BEGIN
    CREATE TABLE [dbo].[FilterDefinition] (
        FilterDefinitionId  INT IDENTITY(1,1) NOT NULL,
        FilterKey           NVARCHAR(50)  NOT NULL,
        Label               NVARCHAR(100) NOT NULL,
        Scope               NVARCHAR(20)  NOT NULL,
        DataSourceKey       NVARCHAR(50)  NULL,
        OptionsQuery        NVARCHAR(MAX) NULL,
        IsActive            BIT           NOT NULL CONSTRAINT DF_FilterDefinition_IsActive DEFAULT (1),
        DisplayOrder        INT           NOT NULL CONSTRAINT DF_FilterDefinition_DisplayOrder DEFAULT (0),
        CreatedAt           DATETIME2     NOT NULL CONSTRAINT DF_FilterDefinition_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt           DATETIME2     NULL,

        CONSTRAINT PK_FilterDefinition PRIMARY KEY CLUSTERED (FilterDefinitionId),
        CONSTRAINT UQ_FilterDefinition_Key UNIQUE (FilterKey),
        CONSTRAINT FK_FilterDefinition_DataSource FOREIGN KEY (DataSourceKey)
            REFERENCES [dbo].[DataSources] (DataSourceKey),
        CONSTRAINT CK_FilterDefinition_Scope CHECK (Scope IN ('spInjection', 'reportAccess'))
    );

    CREATE INDEX IX_FilterDefinition_IsActive ON [dbo].[FilterDefinition] (IsActive, DisplayOrder);
    PRINT 'FilterDefinition tablosu olusturuldu.';
END
ELSE
BEGIN
    PRINT 'FilterDefinition tablosu zaten var (migration idempotent).';
END
GO

-- 2. Seed: sube (spInjection, PDKS DataSource)
IF NOT EXISTS (SELECT 1 FROM dbo.FilterDefinition WHERE FilterKey = N'sube')
BEGIN
    INSERT INTO dbo.FilterDefinition (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder)
    VALUES (
        N'sube',
        N'Şube',
        N'spInjection',
        N'PDKS',
        N'SELECT CAST(SubeNo AS varchar(10)) AS Value, SubeAd AS Label FROM vrd.SubeListe',
        1,
        10
    );
    PRINT 'Seed: sube eklendi.';
END
ELSE
BEGIN
    PRINT 'Seed: sube zaten var.';
END
GO

-- 3. Seed: raporKategori (reportAccess, native EF — OptionsQuery NULL)
IF NOT EXISTS (SELECT 1 FROM dbo.FilterDefinition WHERE FilterKey = N'raporKategori')
BEGIN
    INSERT INTO dbo.FilterDefinition (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder)
    VALUES (
        N'raporKategori',
        N'Rapor Kategorisi',
        N'reportAccess',
        NULL,
        NULL,
        1,
        20
    );
    PRINT 'Seed: raporKategori eklendi.';
END
ELSE
BEGIN
    PRINT 'Seed: raporKategori zaten var.';
END
GO

-- 4. Backfill: aktif user'lara her aktif FilterKey icin '*' kayit (kayitsizlara)
INSERT INTO dbo.UserDataFilters (UserId, FilterKey, FilterValue, DataSourceKey, ReportId, CreatedAt)
SELECT u.UserId, fd.FilterKey, N'*', NULL, NULL, GETDATE()
FROM dbo.Users u
CROSS JOIN dbo.FilterDefinition fd
WHERE u.IsActive = 1
  AND fd.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UserDataFilters x
      WHERE x.UserId = u.UserId AND x.FilterKey = fd.FilterKey
  );

DECLARE @BackfillCount INT = @@ROWCOUNT;
PRINT 'Backfill: ' + CAST(@BackfillCount AS VARCHAR(10)) + ' yeni UserDataFilters kaydi (* magic).';
GO

-- 5. Dogrulama (idempotency check icin sonuc raporu)
SELECT
    'FilterDefinition' AS Tablo,
    COUNT(*) AS Adet
FROM dbo.FilterDefinition
UNION ALL
SELECT 'UserDataFilters_total', COUNT(*) FROM dbo.UserDataFilters
UNION ALL
SELECT 'UserDataFilters_with_*', COUNT(*) FROM dbo.UserDataFilters WHERE FilterValue = N'*';
GO
