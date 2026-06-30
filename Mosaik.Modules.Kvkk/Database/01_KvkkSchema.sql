-- Plan 54 M6 / Plan 40 Faz 0 — KVKK veri modeli omurgası (idempotent).
-- REF lookuplar + core (Process / DataElement / junction / cross-border). EF config ile birebir.

-- ===== REF lookuplar =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkDataCategories')
CREATE TABLE dbo.KvkkDataCategories (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkDataCategories PRIMARY KEY,
    Code NVARCHAR(60) NOT NULL, Name NVARCHAR(150) NOT NULL,
    IsSpecialCategory BIT NOT NULL DEFAULT(0), SortOrder INT NOT NULL DEFAULT(0), IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkDataCategories_Code UNIQUE (Code));
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkLegalBases')
CREATE TABLE dbo.KvkkLegalBases (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkLegalBases PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL, Name NVARCHAR(300) NOT NULL, Article NVARCHAR(20) NOT NULL,
    IsSpecialCategoryBasis BIT NOT NULL DEFAULT(0), SortOrder INT NOT NULL DEFAULT(0), IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkLegalBases_Code UNIQUE (Code));
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkPersonGroups')
CREATE TABLE dbo.KvkkPersonGroups (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkPersonGroups PRIMARY KEY,
    Code NVARCHAR(60) NOT NULL, Name NVARCHAR(150) NOT NULL,
    SortOrder INT NOT NULL DEFAULT(0), IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkPersonGroups_Code UNIQUE (Code));
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkRetentionRules')
CREATE TABLE dbo.KvkkRetentionRules (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkRetentionRules PRIMARY KEY,
    Code NVARCHAR(60) NOT NULL, Name NVARCHAR(200) NOT NULL, DurationText NVARCHAR(80) NOT NULL,
    LegalReference NVARCHAR(200) NULL, SortOrder INT NOT NULL DEFAULT(0), IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkRetentionRules_Code UNIQUE (Code));
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkDisposalMethods')
CREATE TABLE dbo.KvkkDisposalMethods (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkDisposalMethods PRIMARY KEY,
    Code NVARCHAR(60) NOT NULL, Name NVARCHAR(150) NOT NULL,
    SortOrder INT NOT NULL DEFAULT(0), IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkDisposalMethods_Code UNIQUE (Code));
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkMeasureStandards')
CREATE TABLE dbo.KvkkMeasureStandards (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkMeasureStandards PRIMARY KEY,
    Code NVARCHAR(60) NOT NULL, Name NVARCHAR(250) NOT NULL, MeasureType TINYINT NOT NULL DEFAULT(0),
    SortOrder INT NOT NULL DEFAULT(0), IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkMeasureStandards_Code UNIQUE (Code));
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkProcessingPurposes')
CREATE TABLE dbo.KvkkProcessingPurposes (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkProcessingPurposes PRIMARY KEY,
    Code NVARCHAR(60) NOT NULL, Name NVARCHAR(250) NOT NULL,
    SortOrder INT NOT NULL DEFAULT(0), IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkProcessingPurposes_Code UNIQUE (Code));
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkRecipients')
CREATE TABLE dbo.KvkkRecipients (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkRecipients PRIMARY KEY,
    Code NVARCHAR(60) NOT NULL, Name NVARCHAR(200) NOT NULL, IsCrossBorder BIT NOT NULL DEFAULT(0),
    SortOrder INT NOT NULL DEFAULT(0), IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkRecipients_Code UNIQUE (Code));
GO

-- ===== Core =====
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkProcesses')
CREATE TABLE dbo.KvkkProcesses (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkProcesses PRIMARY KEY,
    FirmaId INT NOT NULL,
    Department NVARCHAR(120) NOT NULL, Unit NVARCHAR(120) NULL, Owner NVARCHAR(200) NULL,
    Name NVARCHAR(300) NOT NULL, Purpose NVARCHAR(MAX) NOT NULL,
    LegalBasisId INT NOT NULL, ProcessingPurposeId INT NULL,
    DataSource NVARCHAR(500) NULL, StorageMedium NVARCHAR(500) NULL,
    AccessAuthority NVARCHAR(500) NULL, RecipientGroups NVARCHAR(500) NULL,
    RetentionRuleId INT NULL, DisposalMethodId INT NULL,
    RiskLevel TINYINT NOT NULL DEFAULT(0), ReviewStatus TINYINT NOT NULL DEFAULT(0),
    LastReviewedAt DATETIME2 NULL, LastReviewedBy INT NULL, IsActive BIT NOT NULL DEFAULT(1),
    CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), UpdatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT FK_KvkkProcesses_LegalBasis FOREIGN KEY (LegalBasisId) REFERENCES dbo.KvkkLegalBases(Id),
    CONSTRAINT FK_KvkkProcesses_Retention FOREIGN KEY (RetentionRuleId) REFERENCES dbo.KvkkRetentionRules(Id) ON DELETE SET NULL,
    CONSTRAINT FK_KvkkProcesses_Disposal FOREIGN KEY (DisposalMethodId) REFERENCES dbo.KvkkDisposalMethods(Id) ON DELETE SET NULL,
    CONSTRAINT FK_KvkkProcesses_Purpose FOREIGN KEY (ProcessingPurposeId) REFERENCES dbo.KvkkProcessingPurposes(Id) ON DELETE SET NULL);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_KvkkProcesses_FirmaDept' AND object_id=OBJECT_ID('dbo.KvkkProcesses'))
    CREATE INDEX IX_KvkkProcesses_FirmaDept ON dbo.KvkkProcesses(FirmaId, Department);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_KvkkProcesses_Risk' AND object_id=OBJECT_ID('dbo.KvkkProcesses'))
    CREATE INDEX IX_KvkkProcesses_Risk ON dbo.KvkkProcesses(FirmaId, RiskLevel);
GO
-- Natural key (idempotent UPSERT garantisi + concurrency koruması): FirmaId+Department+Name
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_KvkkProcesses_Natural' AND object_id=OBJECT_ID('dbo.KvkkProcesses'))
    CREATE UNIQUE INDEX UQ_KvkkProcesses_Natural ON dbo.KvkkProcesses(FirmaId, Department, Name);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkDataElements')
CREATE TABLE dbo.KvkkDataElements (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkDataElements PRIMARY KEY,
    ElementCode NVARCHAR(80) NOT NULL, DisplayName NVARCHAR(200) NOT NULL,
    DataCategoryId INT NOT NULL, IsSpecialCategory BIT NOT NULL DEFAULT(0),
    DefaultRetentionHint NVARCHAR(200) NULL, DefaultLegalBasisHintId INT NULL,
    Aliases NVARCHAR(500) NULL, IsActive BIT NOT NULL DEFAULT(1),
    CONSTRAINT UQ_KvkkDataElements_Code UNIQUE (ElementCode),
    CONSTRAINT FK_KvkkDataElements_Category FOREIGN KEY (DataCategoryId) REFERENCES dbo.KvkkDataCategories(Id));
GO
-- DefaultLegalBasisHintId FK (idempotent — tablo önceden yaratılmışsa da ekler)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_KvkkDataElements_LegalBasisHint')
    ALTER TABLE dbo.KvkkDataElements ADD CONSTRAINT FK_KvkkDataElements_LegalBasisHint
        FOREIGN KEY (DefaultLegalBasisHintId) REFERENCES dbo.KvkkLegalBases(Id) ON DELETE SET NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkProcessDataLinks')
CREATE TABLE dbo.KvkkProcessDataLinks (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkProcessDataLinks PRIMARY KEY,
    ProcessId INT NOT NULL, DataElementId INT NOT NULL, UsageType TINYINT NOT NULL DEFAULT(0), Notes NVARCHAR(500) NULL,
    CONSTRAINT UQ_KvkkPDL UNIQUE (ProcessId, DataElementId, UsageType),
    CONSTRAINT FK_KvkkPDL_Process FOREIGN KEY (ProcessId) REFERENCES dbo.KvkkProcesses(Id) ON DELETE CASCADE,
    CONSTRAINT FK_KvkkPDL_Element FOREIGN KEY (DataElementId) REFERENCES dbo.KvkkDataElements(Id));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_KvkkPDL_Element' AND object_id=OBJECT_ID('dbo.KvkkProcessDataLinks'))
    CREATE INDEX IX_KvkkPDL_Element ON dbo.KvkkProcessDataLinks(DataElementId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='KvkkCrossBorderTransfers')
CREATE TABLE dbo.KvkkCrossBorderTransfers (
    Id INT IDENTITY(1,1) CONSTRAINT PK_KvkkCrossBorderTransfers PRIMARY KEY,
    ProcessId INT NOT NULL, RecipientName NVARCHAR(300) NOT NULL, Country NVARCHAR(120) NOT NULL,
    Mechanism TINYINT NOT NULL DEFAULT(0), LegalReference NVARCHAR(200) NULL, DocumentLink NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT FK_KvkkCBT_Process FOREIGN KEY (ProcessId) REFERENCES dbo.KvkkProcesses(Id) ON DELETE CASCADE);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_KvkkCBT_Process' AND object_id=OBJECT_ID('dbo.KvkkCrossBorderTransfers'))
    CREATE INDEX IX_KvkkCBT_Process ON dbo.KvkkCrossBorderTransfers(ProcessId);
GO

PRINT 'KVKK Faz 0 schema (01) tamamlandi.';
