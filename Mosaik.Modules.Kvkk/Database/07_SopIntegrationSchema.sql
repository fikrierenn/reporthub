-- Plan 54 M6 / Plan 40 Faz 3 — SOP<->Process bag tablolari (idempotent).
-- SopDocumentId FK'siz (cross-modul, ADR-002) -- junction sahibi KVKK.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KvkkSopProcessLinks' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.KvkkSopProcessLinks (
        Id INT IDENTITY PRIMARY KEY,
        FirmaId INT NOT NULL,
        ProcessId INT NOT NULL,
        SopDocumentId INT NOT NULL,
        SopTitle NVARCHAR(300) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_KvkkSopProcessLinks_Process FOREIGN KEY (ProcessId)
            REFERENCES dbo.KvkkProcesses(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_KvkkSopProcessLinks UNIQUE (ProcessId, SopDocumentId)
    );
    CREATE INDEX IX_KvkkSopProcessLinks_Sop ON dbo.KvkkSopProcessLinks (SopDocumentId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KvkkSopScannedElements' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.KvkkSopScannedElements (
        Id INT IDENTITY PRIMARY KEY,
        FirmaId INT NOT NULL,
        SopDocumentId INT NOT NULL,
        DataElementId INT NOT NULL,
        ScannedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_KvkkSopScannedElements_DataElement FOREIGN KEY (DataElementId)
            REFERENCES dbo.KvkkDataElements(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_KvkkSopScannedElements_Sop ON dbo.KvkkSopScannedElements (SopDocumentId);
END
GO

PRINT 'KVKK Faz 3: KvkkSopProcessLinks + KvkkSopScannedElements tablolari (07) hazir.';
