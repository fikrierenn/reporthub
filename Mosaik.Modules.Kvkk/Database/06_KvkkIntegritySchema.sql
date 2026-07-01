-- Plan 54 M6 / Plan 40 Faz 5 — AI Integrity Checker bulgu tablosu (idempotent).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KvkkIntegrityFindings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.KvkkIntegrityFindings (
        Id INT IDENTITY PRIMARY KEY,
        FirmaId INT NOT NULL,
        PatternCode NVARCHAR(60) NOT NULL,
        Severity TINYINT NOT NULL,               -- 0 İdari 1 İdari-Yüksek 2 Kritik
        ProcessId INT NULL,
        Description NVARCHAR(500) NOT NULL,
        FirstDetectedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        LastDetectedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsDismissed BIT NOT NULL DEFAULT 0,
        DismissedBy INT NULL,
        DismissedAt DATETIME2 NULL,
        DismissReason NVARCHAR(500) NULL,
        CONSTRAINT FK_KvkkIntegrityFindings_Process FOREIGN KEY (ProcessId)
            REFERENCES dbo.KvkkProcesses(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_KvkkIntegrityFindings_Dedup ON dbo.KvkkIntegrityFindings (FirmaId, PatternCode, ProcessId);
    CREATE INDEX IX_KvkkIntegrityFindings_Open ON dbo.KvkkIntegrityFindings (FirmaId, IsDismissed, Severity);
END
GO

PRINT 'KVKK Faz 5: KvkkIntegrityFindings tablosu (06) hazir.';
