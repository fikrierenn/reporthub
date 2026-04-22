-- 11_AddDashboardConfigJson.sql
-- Dashboard yapılandırılmış mod için config JSON kolonu ekler

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ReportCatalog') AND name = 'DashboardConfigJson')
BEGIN
    ALTER TABLE ReportCatalog ADD DashboardConfigJson NVARCHAR(MAX) NULL;
END
GO
