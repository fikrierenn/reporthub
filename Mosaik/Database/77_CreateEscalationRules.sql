-- Migration 77: EscalationRules — Plan 54 M4 Dashboard→Alert
-- Rapor metriği (rs[ResultSet] üzerinde Aggregation(Column)) eşiği aştığında
-- EscalationSweeperJob (Hangfire, saatlik) INotificationService ile bildirim üretir.
-- Değerlendirme GLOBAL (user-data-filter yok). Dedup: Notifications.ExternalKey (migration 62).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EscalationRules')
BEGIN
    CREATE TABLE dbo.EscalationRules (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EscalationRules PRIMARY KEY,
        Name            NVARCHAR(150)  NOT NULL,
        ReportId        INT            NOT NULL,
        ResultSet       INT            NOT NULL CONSTRAINT DF_EscalationRules_ResultSet DEFAULT(0),
        [Column]        NVARCHAR(128)  NOT NULL,
        Aggregation     NVARCHAR(10)   NOT NULL CONSTRAINT DF_EscalationRules_Aggregation DEFAULT('first'),
        Operator        NVARCHAR(4)    NOT NULL CONSTRAINT DF_EscalationRules_Operator DEFAULT('gt'),
        Threshold       DECIMAL(18,4)  NOT NULL,
        NotifyUserIds   NVARCHAR(500)  NOT NULL,
        IsActive        BIT            NOT NULL CONSTRAINT DF_EscalationRules_IsActive DEFAULT(1),
        LastValue       DECIMAL(18,4)  NULL,
        LastEvaluatedAt DATETIME2      NULL,
        LastFiredAt     DATETIME2      NULL,
        LastError       NVARCHAR(500)  NULL,
        CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_EscalationRules_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CreatedBy       INT            NOT NULL CONSTRAINT DF_EscalationRules_CreatedBy DEFAULT(0),
        CONSTRAINT FK_EscalationRules_ReportCatalog FOREIGN KEY (ReportId)
            REFERENCES dbo.ReportCatalog(ReportId) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EscalationRules_Active' AND object_id = OBJECT_ID('dbo.EscalationRules'))
BEGIN
    CREATE INDEX IX_EscalationRules_Active ON dbo.EscalationRules(IsActive) INCLUDE (ReportId);
END
GO
