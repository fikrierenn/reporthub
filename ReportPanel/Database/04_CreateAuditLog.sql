-- Create AuditLog table and backfill from ReportRunLog
IF OBJECT_ID('dbo.AuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditLog] (
        [AuditId] UNIQUEIDENTIFIER NOT NULL,
        [Username] NVARCHAR(100) NOT NULL,
        [EventType] NVARCHAR(50) NOT NULL,
        [TargetType] NVARCHAR(50) NULL,
        [TargetKey] NVARCHAR(200) NULL,
        [Description] NVARCHAR(500) NULL,
        [OldValuesJson] NVARCHAR(MAX) NULL,
        [NewValuesJson] NVARCHAR(MAX) NULL,
        [IsSuccess] BIT NOT NULL CONSTRAINT [DF_AuditLog_IsSuccess] DEFAULT (1),
        [ErrorMessage] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME NOT NULL CONSTRAINT [DF_AuditLog_CreatedAt] DEFAULT (GETDATE()),
        [ReportId] INT NULL,
        [DataSourceKey] NVARCHAR(50) NULL,
        [ParamsJson] NVARCHAR(MAX) NULL,
        [DurationMs] INT NULL,
        [ResultRowCount] INT NULL,
        [IpAddress] NVARCHAR(45) NULL,
        [UserAgent] NVARCHAR(300) NULL,
        CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([AuditId] ASC)
    );
END
GO

-- Backfill existing report run logs
IF OBJECT_ID('dbo.ReportRunLog', 'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM [dbo].[AuditLog])
BEGIN
    INSERT INTO [dbo].[AuditLog] (
        [AuditId],
        [Username],
        [EventType],
        [TargetType],
        [TargetKey],
        [Description],
        [OldValuesJson],
        [NewValuesJson],
        [IsSuccess],
        [ErrorMessage],
        [CreatedAt],
        [ReportId],
        [DataSourceKey],
        [ParamsJson],
        [DurationMs],
        [ResultRowCount],
        [IpAddress],
        [UserAgent]
    )
    SELECT
        NEWID() AS AuditId,
        [Username],
        N'report_run' AS EventType,
        N'report' AS TargetType,
        CAST([ReportId] AS NVARCHAR(200)) AS TargetKey,
        CASE WHEN [IsSuccess] = 1 THEN N'Run OK' ELSE N'Run failed' END AS Description,
        NULL AS OldValuesJson,
        NULL AS NewValuesJson,
        [IsSuccess],
        [ErrorMessage],
        [RunAt] AS CreatedAt,
        [ReportId],
        [DataSourceKey],
        [ParamsJson],
        [DurationMs],
        [ResultRowCount],
        NULL AS IpAddress,
        NULL AS UserAgent
    FROM [dbo].[ReportRunLog];
END
