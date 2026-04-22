-- Add Category column to ReportCatalog
IF COL_LENGTH('dbo.ReportCatalog', 'Category') IS NULL
BEGIN
    ALTER TABLE [dbo].[ReportCatalog]
        ADD [Category] NVARCHAR(100) NULL;
END
