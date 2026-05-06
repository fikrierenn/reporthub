-- 10_AddDashboardColumns.sql
-- Dashboard rapor tipi desteği için ReportCatalog tablosuna yeni kolonlar ekler

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ReportCatalog') AND name = 'ReportType')
BEGIN
    ALTER TABLE ReportCatalog ADD ReportType NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportCatalog_ReportType DEFAULT 'table';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ReportCatalog') AND name = 'DashboardHtml')
BEGIN
    ALTER TABLE ReportCatalog ADD DashboardHtml NVARCHAR(MAX) NULL;
END
GO
