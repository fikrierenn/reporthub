-- M-05 Faz C (22 Nisan 2026)
-- DashboardHtml kolonu DROP. DashboardConfigJson artik tek source-of-truth.
-- ADR: docs/ADR/005-dashboard-architecture.md
--
-- Guvenlik agi: orphan check (HtmlOnly aktif dashboard) > 0 ise abort.
-- Faz B'de (migration 16) orphan sayisi PRINT ile raporlanmisti, burada ayni
-- check RAISERROR ile ile drop'u engeller. Idempotent: kolon yoksa sessizce atlanir.

USE [PortalHUB];
GO

-- 1) Orphan check — kolon var iken calisir.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ReportCatalog')
      AND name = 'DashboardHtml'
)
BEGIN
    DECLARE @OrphanCount INT;

    -- Dynamic SQL: kolon yoksa parse error patlayabilir; EXISTS branch garantisi altinda.
    EXEC sp_executesql
        N'SELECT @cnt = COUNT(1)
          FROM [dbo].[ReportCatalog]
          WHERE ReportType = ''dashboard''
            AND IsActive = 1
            AND DashboardHtml IS NOT NULL
            AND LEN(LTRIM(RTRIM(DashboardHtml))) > 0
            AND (DashboardConfigJson IS NULL OR LEN(LTRIM(RTRIM(DashboardConfigJson))) = 0)',
        N'@cnt INT OUTPUT',
        @cnt = @OrphanCount OUTPUT;

    IF @OrphanCount > 0
    BEGIN
        DECLARE @msg NVARCHAR(400) = CONCAT(
            'ABORT: ', @OrphanCount,
            ' aktif dashboard raporu yalnizca DashboardHtml kullaniyor. ',
            'Once bu raporlar DashboardConfigJson''a migrate edilmeli. ',
            'Liste icin Database/16_DeprecateDashboardHtml.sql SELECT''unu calistirin.'
        );
        RAISERROR(@msg, 16, 1);
        RETURN;
    END

    PRINT 'OK — orphan yok. DashboardHtml kolonu drop ediliyor.';

    ALTER TABLE [dbo].[ReportCatalog] DROP COLUMN [DashboardHtml];

    PRINT 'OK — DashboardHtml kolonu drop edildi.';
END
ELSE
BEGIN
    PRINT 'NOOP — DashboardHtml kolonu zaten yok (migration zaten calismis).';
END
GO
