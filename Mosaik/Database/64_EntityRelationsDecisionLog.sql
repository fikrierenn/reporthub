-- Migration 64: Plan 38 EntityRelations + DecisionLogs
-- Polymorphic ilişki tablosu (Living Org Map) + karar kayıt (Decision Memory).
-- VISION §7 kuzey yıldızı altyapısı. Eski FK'lar yerinde kalır, çift-yazma pattern.

-- ============================================================
-- EntityRelations: cross-modül polymorphic ilişki
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'EntityRelations'
)
BEGIN
    CREATE TABLE dbo.EntityRelations (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId         INT NOT NULL,
        SourceType      NVARCHAR(50) NOT NULL,
        SourceId        INT NOT NULL,
        RelationType    NVARCHAR(50) NOT NULL,
        TargetType      NVARCHAR(50) NOT NULL,
        TargetId        INT NOT NULL,
        Weight          DECIMAL(5,2) NULL,
        ValidFrom       DATETIME2 NOT NULL CONSTRAINT DF_EntityRelations_ValidFrom DEFAULT SYSUTCDATETIME(),
        ValidTo         DATETIME2 NULL,
        SourceSystem    NVARCHAR(50) NULL,
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_EntityRelations_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy       INT NULL,
        CONSTRAINT UQ_EntityRelations_Tuple
            UNIQUE (FirmaId, SourceType, SourceId, RelationType, TargetType, TargetId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EntityRelations_Source')
BEGIN
    CREATE INDEX IX_EntityRelations_Source
        ON dbo.EntityRelations(FirmaId, SourceType, SourceId)
        INCLUDE (RelationType, TargetType, TargetId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EntityRelations_Target')
BEGIN
    CREATE INDEX IX_EntityRelations_Target
        ON dbo.EntityRelations(FirmaId, TargetType, TargetId)
        INCLUDE (RelationType, SourceType, SourceId);
END
GO

-- ============================================================
-- DecisionLogs: kim, neden, ne karar verdi (AuditLog'un "neden" katmanı)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'DecisionLogs'
)
BEGIN
    CREATE TABLE dbo.DecisionLogs (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId             INT NOT NULL,
        Title               NVARCHAR(300) NOT NULL,
        Rationale           NVARCHAR(MAX) NULL,
        MadeBy              INT NOT NULL,
        MadeAt              DATETIME2 NOT NULL CONSTRAINT DF_DecisionLogs_MadeAt DEFAULT SYSUTCDATETIME(),
        AlternativesJson    NVARCHAR(MAX) NULL,
        ExpectedOutcome     NVARCHAR(MAX) NULL,
        ActualOutcome       NVARCHAR(MAX) NULL,
        KpiImpactJson       NVARCHAR(MAX) NULL,
        RelatedEntityType   NVARCHAR(50) NULL,
        RelatedEntityId     INT NULL,
        RelatedAuditId      BIGINT NULL,
        Status              NVARCHAR(20) NOT NULL CONSTRAINT DF_DecisionLogs_Status DEFAULT 'Active'
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DecisionLogs_Entity')
BEGIN
    CREATE INDEX IX_DecisionLogs_Entity
        ON dbo.DecisionLogs(FirmaId, RelatedEntityType, RelatedEntityId)
        INCLUDE (Title, MadeBy, MadeAt);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DecisionLogs_MadeBy')
BEGIN
    CREATE INDEX IX_DecisionLogs_MadeBy
        ON dbo.DecisionLogs(FirmaId, MadeBy, MadeAt DESC);
END
GO
