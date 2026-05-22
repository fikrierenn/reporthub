-- Migration 01 — Plan 34 Faz B: SOP entity tabloları (2026-05-22)
-- 4 tablo: SopDocuments, SopVersions, SopReadReceipts, SopApprovalSubmissions.
-- SopAiConversation Faz F'de (Migration 05).
-- Idempotent: tablolar zaten varsa atlanır.

SET NOCOUNT ON;
GO

------------------------------------------------------------------------------
-- 1. SopDocuments — master record
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SopDocuments')
BEGIN
    CREATE TABLE dbo.SopDocuments (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId               INT          NOT NULL,
        Title                 NVARCHAR(200) NOT NULL,
        Description           NVARCHAR(MAX) NULL,
        Category              NVARCHAR(80)  NULL,
        DepartmentIds         NVARCHAR(500) NULL,                     -- CSV başlangıçta
        IsCompanyWide         BIT          NOT NULL DEFAULT 0,
        OwnerUserId           INT          NOT NULL,
        IsActive              BIT          NOT NULL DEFAULT 1,
        ReadDeadlineDays      INT          NOT NULL DEFAULT 30,
        RequiresIKApproval    BIT          NOT NULL DEFAULT 1,
        AiAdvisorEnabled      BIT          NOT NULL DEFAULT 1,
        CreatedBy             INT          NOT NULL,
        CreatedAt             DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt             DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_SopDocuments_ReadDeadline CHECK (ReadDeadlineDays BETWEEN 1 AND 365)
    );
    CREATE INDEX IX_SopDocuments_FirmaActive ON dbo.SopDocuments(FirmaId, IsActive);
    CREATE INDEX IX_SopDocuments_Owner ON dbo.SopDocuments(OwnerUserId);
    PRINT 'Migration 01 — SopDocuments created.';
END
ELSE
    PRINT 'SopDocuments zaten mevcut, atlandı.';
GO

------------------------------------------------------------------------------
-- 2. SopVersions — version snapshot zinciri
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SopVersions')
BEGIN
    CREATE TABLE dbo.SopVersions (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        SopDocumentId         INT          NOT NULL,
        VersionNumber         INT          NOT NULL,
        ContentJson           NVARCHAR(MAX) NOT NULL,
        PlainTextContent      NVARCHAR(MAX) NULL,                    -- AI context cache (Faz F)
        EffectiveDate         DATETIME2    NULL,
        SupersededDate        DATETIME2    NULL,
        Status                TINYINT      NOT NULL DEFAULT 0,        -- 0 Draft 1 Pending 2 Approved 3 Archived
        CreatedBy             INT          NOT NULL,
        CreatedAt             DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SopVersions_SopDocuments FOREIGN KEY (SopDocumentId)
            REFERENCES dbo.SopDocuments(Id) ON DELETE CASCADE,
        CONSTRAINT CK_SopVersions_Status CHECK (Status BETWEEN 0 AND 3),
        CONSTRAINT CK_SopVersions_VersionNumber CHECK (VersionNumber >= 1)
    );
    CREATE UNIQUE INDEX IX_SopVersions_DocumentVersion ON dbo.SopVersions(SopDocumentId, VersionNumber);
    CREATE INDEX IX_SopVersions_Status ON dbo.SopVersions(Status);
    PRINT 'Migration 01 — SopVersions created.';
END
ELSE
    PRINT 'SopVersions zaten mevcut, atlandı.';
GO

------------------------------------------------------------------------------
-- 3. SopReadReceipts — okundu disiplini
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SopReadReceipts')
BEGIN
    CREATE TABLE dbo.SopReadReceipts (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        SopVersionId          INT          NOT NULL,
        UserId                INT          NOT NULL,
        AssignedAt            DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        ReadAt                DATETIME2    NULL,
        ConfirmedAt           DATETIME2    NULL,
        ReminderSentCount     INT          NOT NULL DEFAULT 0,
        CONSTRAINT FK_SopReadReceipts_SopVersions FOREIGN KEY (SopVersionId)
            REFERENCES dbo.SopVersions(Id) ON DELETE CASCADE,
        CONSTRAINT CK_SopReadReceipts_Reminder CHECK (ReminderSentCount >= 0)
    );
    CREATE UNIQUE INDEX IX_SopReadReceipts_VersionUser ON dbo.SopReadReceipts(SopVersionId, UserId);
    CREATE INDEX IX_SopReadReceipts_UserPending
        ON dbo.SopReadReceipts(UserId, ConfirmedAt)
        WHERE ConfirmedAt IS NULL;                                    -- "Prosedürlerim" hızlı query
    PRINT 'Migration 01 — SopReadReceipts created.';
END
ELSE
    PRINT 'SopReadReceipts zaten mevcut, atlandı.';
GO

------------------------------------------------------------------------------
-- 4. SopApprovalSubmissions — Mosaik.Core ApprovalRequest cross-assembly link
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SopApprovalSubmissions')
BEGIN
    CREATE TABLE dbo.SopApprovalSubmissions (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        SopVersionId          INT          NOT NULL,
        ApprovalRequestId     INT          NOT NULL,                  -- ApprovalRequests(Id), cross-modül abstraction
        CreatedBy             INT          NOT NULL,
        CreatedAt             DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SopApprovalSubmissions_SopVersions FOREIGN KEY (SopVersionId)
            REFERENCES dbo.SopVersions(Id) ON DELETE CASCADE
        -- ApprovalRequestId için FK eklemiyoruz (Mosaik.Core tablosu, modüler bağımsızlık).
        -- Soft reference: ApprovalService.GetByIdAsync ile çözülür.
    );
    CREATE INDEX IX_SopApprovalSubmissions_Version ON dbo.SopApprovalSubmissions(SopVersionId);
    CREATE INDEX IX_SopApprovalSubmissions_Request ON dbo.SopApprovalSubmissions(ApprovalRequestId);
    PRINT 'Migration 01 — SopApprovalSubmissions created.';
END
ELSE
    PRINT 'SopApprovalSubmissions zaten mevcut, atlandı.';
GO

PRINT 'Migration 01 tamamlandi.';
