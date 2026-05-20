-- Migration 65: Plan 36 Workflow Designer + Onay Akışları Faz A
-- WorkflowTemplates: designer ile çizilen şablon (sequential-workflow-designer JSON)
-- WorkflowInstances: template'dan başlatılan iş; CurrentStepId log projeksiyon, opsiyonel cache
-- WorkflowInstanceLogs: append-only event sourcing (UPDATE yok)

-- ============================================================
-- WorkflowTemplates
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkflowTemplates')
BEGIN
    CREATE TABLE dbo.WorkflowTemplates (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId         INT NOT NULL,
        Name            NVARCHAR(200) NOT NULL,
        EntityType      NVARCHAR(50) NOT NULL,   -- hangi modül (Contract, Obligation, Circular, ...)
        DefinitionJson  NVARCHAR(MAX) NOT NULL,  -- sequential-workflow-designer toJSON çıktısı
        IsActive        BIT NOT NULL CONSTRAINT DF_WorkflowTemplates_IsActive DEFAULT 1,
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_WorkflowTemplates_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy       INT NULL,
        UpdatedAt       DATETIME2 NULL,
        UpdatedBy       INT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkflowTemplates_EntityType')
BEGIN
    CREATE INDEX IX_WorkflowTemplates_EntityType
        ON dbo.WorkflowTemplates(FirmaId, EntityType, IsActive);
END
GO

-- ============================================================
-- WorkflowInstances
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkflowInstances')
BEGIN
    CREATE TABLE dbo.WorkflowInstances (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId         INT NOT NULL,
        TemplateId      INT NOT NULL REFERENCES dbo.WorkflowTemplates(Id),
        EntityType      NVARCHAR(50) NOT NULL,
        EntityId        INT NOT NULL,
        CurrentStepId   NVARCHAR(100) NULL,      -- projeksiyon: latest StepEntered log'tan
        Status          INT NOT NULL CONSTRAINT DF_WorkflowInstances_Status DEFAULT 0,  -- 0=Active 1=Completed 2=Cancelled
        StartedAt       DATETIME2 NOT NULL CONSTRAINT DF_WorkflowInstances_StartedAt DEFAULT SYSUTCDATETIME(),
        StartedBy       INT NOT NULL,
        CompletedAt     DATETIME2 NULL,
        PayloadJson     NVARCHAR(MAX) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkflowInstances_Entity')
BEGIN
    CREATE INDEX IX_WorkflowInstances_Entity
        ON dbo.WorkflowInstances(FirmaId, EntityType, EntityId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkflowInstances_Active')
BEGIN
    CREATE INDEX IX_WorkflowInstances_Active
        ON dbo.WorkflowInstances(FirmaId, Status)
        INCLUDE (TemplateId, EntityType, EntityId, CurrentStepId);
END
GO

-- ============================================================
-- WorkflowInstanceLogs (append-only event sourcing)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WorkflowInstanceLogs')
BEGIN
    CREATE TABLE dbo.WorkflowInstanceLogs (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        InstanceId      INT NOT NULL REFERENCES dbo.WorkflowInstances(Id),
        StepId          NVARCHAR(100) NULL,
        EventType       NVARCHAR(50) NOT NULL,
        ActorId         INT NULL,
        OccurredAt      DATETIME2 NOT NULL CONSTRAINT DF_WorkflowInstanceLogs_OccurredAt DEFAULT SYSUTCDATETIME(),
        PayloadJson     NVARCHAR(MAX) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkflowInstanceLogs_Instance')
BEGIN
    CREATE INDEX IX_WorkflowInstanceLogs_Instance
        ON dbo.WorkflowInstanceLogs(InstanceId, OccurredAt);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkflowInstanceLogs_StepEvent')
BEGIN
    -- Friction Heatmap + Digital Twin what-if query'leri
    CREATE INDEX IX_WorkflowInstanceLogs_StepEvent
        ON dbo.WorkflowInstanceLogs(StepId, EventType)
        INCLUDE (InstanceId, OccurredAt);
END
GO
