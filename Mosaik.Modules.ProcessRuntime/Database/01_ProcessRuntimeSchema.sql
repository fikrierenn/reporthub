-- =============================================================================
-- ProcessRuntime 01 — vaka konteyneri şeması (Plan 42 REV 3, council §4.5 uyumlu)
-- =============================================================================
-- ProcessInstances = İNCE konteyner: state machine YOK (WorkflowInstance otorite),
-- Transitions tablosu YOK (WorkflowInstanceLogs tek log), AssignedTo YOK (drift).
-- Status = kaba projeksiyon (0 Açık 1 Tamamlandı 2 İptal). İdempotent.

IF OBJECT_ID(N'dbo.ProcessInstances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessInstances (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProcessInstances PRIMARY KEY,
        InstanceCode NVARCHAR(40) NOT NULL,
        FirmaId INT NOT NULL,
        ProcessId INT NULL,                 -- KvkkProcesses soft-ref (modül opsiyonel, FK yok)
        Status TINYINT NOT NULL CONSTRAINT DF_PI_Status DEFAULT 0,
        Priority TINYINT NOT NULL CONSTRAINT DF_PI_Priority DEFAULT 1,
        Title NVARCHAR(300) NOT NULL,
        InitiatedById INT NULL,
        InitiatorEmail NVARCHAR(200) NULL,
        WorkflowInstanceId INT NULL,        -- WorkflowInstances soft-ref (ana yürütücü)
        DueAt DATETIME2 NULL,
        StartedAt DATETIME2 NOT NULL CONSTRAINT DF_PI_StartedAt DEFAULT SYSUTCDATETIME(),
        CompletedAt DATETIME2 NULL,
        ResultCode NVARCHAR(40) NULL,
        SourceTrigger NVARCHAR(40) NOT NULL CONSTRAINT DF_PI_Source DEFAULT N'manual',
        SourceTriggerRefId INT NULL,
        CONSTRAINT UQ_PI_InstanceCode UNIQUE (InstanceCode),
        CONSTRAINT CK_PI_Status CHECK (Status IN (0,1,2)),
        CONSTRAINT CK_PI_Source CHECK (SourceTrigger IN (N'manual', N'public', N'cron', N'event'))
    );
    CREATE INDEX IX_PI_FirmaStatus ON dbo.ProcessInstances (FirmaId, Status);
    CREATE INDEX IX_PI_Initiator ON dbo.ProcessInstances (InitiatedById, StartedAt);
    CREATE INDEX IX_PI_Workflow ON dbo.ProcessInstances (WorkflowInstanceId) WHERE WorkflowInstanceId IS NOT NULL;
    PRINT 'ProcessInstances olusturuldu.';
END
ELSE PRINT 'ProcessInstances zaten var.';
GO

IF OBJECT_ID(N'dbo.ProcessInstanceAspects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessInstanceAspects (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProcessInstanceAspects PRIMARY KEY,
        ProcessInstanceId INT NOT NULL
            CONSTRAINT FK_PIA_Instance REFERENCES dbo.ProcessInstances(Id) ON DELETE CASCADE,
        AspectType TINYINT NOT NULL,
        AspectId INT NOT NULL,
        RelationLabel NVARCHAR(80) NULL,
        AddedAt DATETIME2 NOT NULL CONSTRAINT DF_PIA_AddedAt DEFAULT SYSUTCDATETIME(),
        AddedBy INT NULL,
        CONSTRAINT CK_PIA_Type CHECK (AspectType BETWEEN 0 AND 7)
    );
    CREATE INDEX IX_PIA_Instance ON dbo.ProcessInstanceAspects (ProcessInstanceId, AspectType);
    CREATE INDEX IX_PIA_Reverse ON dbo.ProcessInstanceAspects (AspectType, AspectId);
    PRINT 'ProcessInstanceAspects olusturuldu.';
END
ELSE PRINT 'ProcessInstanceAspects zaten var.';
