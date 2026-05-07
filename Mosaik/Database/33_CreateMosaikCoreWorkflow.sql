-- Migration 33: Plan 16.5 Faz A — Mosaik.Core.Workflow tabloları
-- Tarih: 2026-05-08
-- Plan: plans/16.5-shared-kit.md (Faz A — Domain primitives + Workflow)
-- İdempotent: çoklu çalıştırma güvenli

SET NOCOUNT ON;
GO

-- 1. ApprovalRequests — generic onay isteği (EntityType + EntityId ile herhangi bir entity'ye bağlanır)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApprovalRequests' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ApprovalRequests
    (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalRequests PRIMARY KEY,
        EntityType   NVARCHAR(50)  NOT NULL,
        EntityId     INT           NOT NULL,
        Subject      NVARCHAR(200) NULL,
        Description  NVARCHAR(1000) NULL,
        Status       INT           NOT NULL CONSTRAINT DF_ApprovalRequests_Status DEFAULT 0, -- Pending=0
        CompletedAt  DATETIME2(0)  NULL,
        CreatedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_ApprovalRequests_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy    NVARCHAR(100) NULL,
        UpdatedAt    DATETIME2(0)  NULL,
        UpdatedBy    NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.ApprovalRequests olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.ApprovalRequests zaten mevcut, atlandi.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalRequests_Entity')
    CREATE NONCLUSTERED INDEX IX_ApprovalRequests_Entity
        ON dbo.ApprovalRequests (EntityType, EntityId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalRequests_Status')
    CREATE NONCLUSTERED INDEX IX_ApprovalRequests_Status
        ON dbo.ApprovalRequests (Status);
GO

-- 2. ApprovalSteps — sıralı onay adımları (StepOrder bazlı, MIN sıradaki onayçı)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApprovalSteps' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ApprovalSteps
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalSteps PRIMARY KEY,
        RequestId       INT          NOT NULL,
        StepOrder       INT          NOT NULL,
        ApproverUserId  INT          NULL,
        ApproverRole    NVARCHAR(50) NULL,
        Status          INT          NOT NULL CONSTRAINT DF_ApprovalSteps_Status DEFAULT 0,
        DecidedAt       DATETIME2(0) NULL,
        Comment         NVARCHAR(500) NULL,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_ApprovalSteps_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0) NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT FK_ApprovalSteps_Request FOREIGN KEY (RequestId)
            REFERENCES dbo.ApprovalRequests(Id) ON DELETE CASCADE
    );
    PRINT 'Tablo dbo.ApprovalSteps olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.ApprovalSteps zaten mevcut, atlandi.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalSteps_RequestStep')
    CREATE NONCLUSTERED INDEX IX_ApprovalSteps_RequestStep
        ON dbo.ApprovalSteps (RequestId, StepOrder);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalSteps_Status')
    CREATE NONCLUSTERED INDEX IX_ApprovalSteps_Status
        ON dbo.ApprovalSteps (Status);
GO

-- 3. StatusTransitions — DB-driven izinli durum geçişleri (Operax pattern)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StatusTransitions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.StatusTransitions
    (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StatusTransitions PRIMARY KEY,
        EntityType    NVARCHAR(50)  NOT NULL,
        FromStatus    NVARCHAR(30)  NOT NULL,
        ToStatus      NVARCHAR(30)  NOT NULL,
        AllowedRoles  NVARCHAR(200) NULL,  -- CSV: "admin,manager"
        IsActive      BIT           NOT NULL CONSTRAINT DF_StatusTransitions_IsActive DEFAULT 1
    );
    PRINT 'Tablo dbo.StatusTransitions olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.StatusTransitions zaten mevcut, atlandi.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StatusTransitions_Triplet')
    CREATE UNIQUE NONCLUSTERED INDEX UX_StatusTransitions_Triplet
        ON dbo.StatusTransitions (EntityType, FromStatus, ToStatus);
GO

PRINT 'Migration 33 tamamlandi: ApprovalRequests + ApprovalSteps + StatusTransitions.';
