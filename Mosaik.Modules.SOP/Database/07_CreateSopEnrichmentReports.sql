-- Migration 07 — Plan 34.1 Save-time AI Enrichment: review report tablosu (2026-05-24)
-- SOP version save sonrası LLM otomatik tarama sonucu (tags, missing sections, jargon, KVKK, saklama).
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SopEnrichmentReports')
BEGIN
    CREATE TABLE dbo.SopEnrichmentReports (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        SopVersionId        INT          NOT NULL,
        ReportJson          NVARCHAR(MAX) NOT NULL,
        Status              TINYINT      NOT NULL DEFAULT 0,                   -- 0 Pending 1 Completed 2 Failed 3 Accepted
        FailureReason       NVARCHAR(500) NULL,
        InjectedSkillIds    NVARCHAR(500) NULL,
        CreatedAt           DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        CompletedAt         DATETIME2    NULL,
        AcceptedAt          DATETIME2    NULL,
        AcceptedByUserId    INT          NULL,
        CONSTRAINT FK_SopEnrichmentReports_SopVersion
            FOREIGN KEY (SopVersionId) REFERENCES dbo.SopVersions(Id) ON DELETE CASCADE,
        CONSTRAINT CK_SopEnrichmentReports_Status CHECK (Status IN (0, 1, 2, 3))
    );
    CREATE INDEX IX_SopEnrichmentReports_SopVersionId_CreatedAt
        ON dbo.SopEnrichmentReports(SopVersionId, CreatedAt DESC);
    PRINT 'Migration 07 — SopEnrichmentReports created.';
END
ELSE
    PRINT 'SopEnrichmentReports zaten mevcut, atlandı.';
GO
