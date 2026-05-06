-- Drop legacy ReportRunLog table after AuditLog migration
IF OBJECT_ID('dbo.ReportRunLog', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[ReportRunLog];
END
