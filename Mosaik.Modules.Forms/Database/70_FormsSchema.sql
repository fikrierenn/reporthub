-- Migration 70 — Plan 41 Faz 0 scaffold (21 Mayıs 2026)
-- Forms modülü çekirdek schema. Plan 41 §4.1.
-- ADR-002 modular monolit + ADR-020 SurveyJS Form Library Hybrid.
--
-- 7 tablo: FormDefinitions, FormFields, FormFieldDataElementMaps,
-- FormSubmissions, FormSubmissionFieldValues, PublicFormTokens, FormDefinitionVersions.
--
-- DataElementId (Plan 40 KVKK) + LinkedProcessId (Plan 40) + WorkflowInstanceId (Plan 36) +
-- LinkedProcessInstanceId (Plan 42) — şimdilik soft reference (FK YOK), Plan 40/42 tablo'ları
-- gelince ALTER TABLE ADD FK ayrı migration.
--
-- ROLLBACK: Tablolar bağımsız (cross-modül FK yok); DROP order: child->parent.

USE [Mosaik];
GO

------------------------------------------------------------------------------
-- 1. FormDefinitions
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FormDefinitions')
BEGIN
    CREATE TABLE dbo.FormDefinitions (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId               INT          NOT NULL,
        Slug                  NVARCHAR(120) NOT NULL,
        Name                  NVARCHAR(200) NOT NULL,
        Description           NVARCHAR(MAX) NULL,
        Category              NVARCHAR(80)  NULL,
        IsPublic              BIT          NOT NULL DEFAULT 0,
        IsAnonymous           BIT          NOT NULL DEFAULT 0,
        IsEncrypted           BIT          NOT NULL DEFAULT 0,
        Status                TINYINT      NOT NULL DEFAULT 0,
        Version               INT          NOT NULL DEFAULT 1,
        LinkedProcessId       INT          NULL,
        TriggersWorkflowId    INT          NULL,
        CreatedBy             INT          NOT NULL,
        CreatedAt             DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt             DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX IX_FormDefinitions_Slug ON dbo.FormDefinitions(Slug);
    CREATE INDEX IX_FormDefinitions_FirmaStatus ON dbo.FormDefinitions(FirmaId, Status);
    PRINT 'Migration 70 — FormDefinitions created.';
END
GO

------------------------------------------------------------------------------
-- 2. FormFields
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FormFields')
BEGIN
    CREATE TABLE dbo.FormFields (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        FormDefinitionId      INT          NOT NULL,
        [Order]               INT          NOT NULL,
        FieldKey              NVARCHAR(80) NOT NULL,
        Label                 NVARCHAR(300) NOT NULL,
        HelpText              NVARCHAR(500) NULL,
        FieldType             TINYINT      NOT NULL,
        IsRequired            BIT          NOT NULL DEFAULT 0,
        ValidationRules       NVARCHAR(MAX) NULL,
        Options               NVARCHAR(MAX) NULL,
        ConditionalLogic      NVARCHAR(MAX) NULL,
        DefaultValue          NVARCHAR(MAX) NULL,
        Placeholder           NVARCHAR(200) NULL,
        CONSTRAINT FK_FormFields_FormDefinitions FOREIGN KEY (FormDefinitionId)
            REFERENCES dbo.FormDefinitions(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_FormFields_DefField ON dbo.FormFields(FormDefinitionId, FieldKey);
    CREATE INDEX IX_FormFields_DefOrder ON dbo.FormFields(FormDefinitionId, [Order]);
    PRINT 'Migration 70 — FormFields created.';
END
GO

------------------------------------------------------------------------------
-- 3. FormFieldDataElementMaps (KVKK Plan 40 entegrasyon)
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FormFieldDataElementMaps')
BEGIN
    CREATE TABLE dbo.FormFieldDataElementMaps (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        FormFieldId           INT          NOT NULL,
        DataElementId         INT          NOT NULL,
        UsageType             TINYINT      NOT NULL DEFAULT 0,
        Notes                 NVARCHAR(500) NULL,
        CONSTRAINT FK_FFDEM_FormFields FOREIGN KEY (FormFieldId)
            REFERENCES dbo.FormFields(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_FFDEM_FieldElement ON dbo.FormFieldDataElementMaps(FormFieldId, DataElementId);
    PRINT 'Migration 70 — FormFieldDataElementMaps created.';
END
GO

------------------------------------------------------------------------------
-- 4. FormSubmissions
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FormSubmissions')
BEGIN
    CREATE TABLE dbo.FormSubmissions (
        Id                       INT IDENTITY(1,1) PRIMARY KEY,
        FormDefinitionId         INT          NOT NULL,
        FirmaId                  INT          NOT NULL,
        SubmittedById            INT          NULL,
        SubmitterEmail           NVARCHAR(200) NULL,
        SubmitterPhone           NVARCHAR(40) NULL,
        SubmitterIp              NVARCHAR(45) NULL,
        SubmitterUserAgent       NVARCHAR(500) NULL,
        PublicTokenId            INT          NULL,
        Status                   TINYINT      NOT NULL DEFAULT 0,
        LinkedProcessInstanceId  INT          NULL,
        WorkflowInstanceId       INT          NULL,
        SubmittedAt              DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_FormSubmissions_FormDefinitions FOREIGN KEY (FormDefinitionId)
            REFERENCES dbo.FormDefinitions(Id)
    );
    CREATE INDEX IX_FormSubmissions_FormDef ON dbo.FormSubmissions(FormDefinitionId, SubmittedAt);
    CREATE INDEX IX_FormSubmissions_User ON dbo.FormSubmissions(SubmittedById, SubmittedAt);
    PRINT 'Migration 70 — FormSubmissions created.';
END
GO

------------------------------------------------------------------------------
-- 5. FormSubmissionFieldValues
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FormSubmissionFieldValues')
BEGIN
    CREATE TABLE dbo.FormSubmissionFieldValues (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        FormSubmissionId      INT          NOT NULL,
        FormFieldId           INT          NOT NULL,
        FieldKey              NVARCHAR(80) NOT NULL,
        ValueText             NVARCHAR(MAX) NULL,
        ValueNumber           DECIMAL(18,4) NULL,
        ValueDate             DATETIME2    NULL,
        ValueBool             BIT          NULL,
        ValueFileId           INT          NULL,
        IsEncrypted           BIT          NOT NULL DEFAULT 0,
        CONSTRAINT FK_FSFV_FormSubmissions FOREIGN KEY (FormSubmissionId)
            REFERENCES dbo.FormSubmissions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_FSFV_FormFields FOREIGN KEY (FormFieldId)
            REFERENCES dbo.FormFields(Id)
    );
    CREATE INDEX IX_FSFV_Submission ON dbo.FormSubmissionFieldValues(FormSubmissionId, FieldKey);
    PRINT 'Migration 70 — FormSubmissionFieldValues created.';
END
GO

------------------------------------------------------------------------------
-- 6. PublicFormTokens
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PublicFormTokens')
BEGIN
    CREATE TABLE dbo.PublicFormTokens (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        FormDefinitionId      INT          NOT NULL,
        TokenHash             VARBINARY(64) NOT NULL,
        RecipientEmail        NVARCHAR(200) NULL,
        ExpiresAt             DATETIME2    NOT NULL,
        MaxUses               INT          NOT NULL DEFAULT 1,
        UsedCount             INT          NOT NULL DEFAULT 0,
        CreatedAt             DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy             INT          NOT NULL,
        CONSTRAINT FK_PublicFormTokens_FormDefinitions FOREIGN KEY (FormDefinitionId)
            REFERENCES dbo.FormDefinitions(Id)
    );
    CREATE UNIQUE INDEX IX_PublicFormTokens_Hash ON dbo.PublicFormTokens(TokenHash);
    PRINT 'Migration 70 — PublicFormTokens created.';
END
GO

------------------------------------------------------------------------------
-- 7. FormDefinitionVersions
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'FormDefinitionVersions')
BEGIN
    CREATE TABLE dbo.FormDefinitionVersions (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        FormDefinitionId      INT          NOT NULL,
        Version               INT          NOT NULL,
        SchemaJson            NVARCHAR(MAX) NOT NULL,
        PublishedAt           DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        PublishedBy           INT          NOT NULL,
        CONSTRAINT FK_FDV_FormDefinitions FOREIGN KEY (FormDefinitionId)
            REFERENCES dbo.FormDefinitions(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_FDV_DefVersion ON dbo.FormDefinitionVersions(FormDefinitionId, Version);
    PRINT 'Migration 70 — FormDefinitionVersions created.';
END
GO

-- AuditLog
INSERT INTO dbo.AuditLog (Id, Username, EventType, TargetType, TargetKey, Description, NewValuesJson, Success, Timestamp)
VALUES (NEWID(), 'system', 'schema_migration', 'module', 'forms',
        'Migration 70: Forms module schema scaffold (Plan 41 Faz 0)',
        '{"Tables":["FormDefinitions","FormFields","FormFieldDataElementMaps","FormSubmissions","FormSubmissionFieldValues","PublicFormTokens","FormDefinitionVersions"]}',
        1, SYSUTCDATETIME());
GO
