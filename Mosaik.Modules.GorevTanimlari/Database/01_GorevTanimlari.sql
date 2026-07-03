/* Görev Tanımları modülü şeması — idempotent.
   GorevDocuments + GorevVersions (SopVersions deseni) + GorevZirveMap (rol) + GorevPersonelMap (kişi).
   md → GorevDocuments/GorevVersions göçü: gorevtanimlari/bkm/scripts/migrate-to-mosaik.mjs. */

IF OBJECT_ID('dbo.GorevDocuments','U') IS NULL
CREATE TABLE dbo.GorevDocuments (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GorevDocuments PRIMARY KEY,
    PositionCode NVARCHAR(200) NOT NULL,
    Title NVARCHAR(300) NOT NULL,
    OrgPositionId INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_GorevDocuments_IsActive DEFAULT (1),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_GorevDocuments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy NVARCHAR(200) NULL, UpdatedAt DATETIME2 NULL, UpdatedBy NVARCHAR(200) NULL,
    CONSTRAINT UX_GorevDocuments_PositionCode UNIQUE (PositionCode),
    CONSTRAINT FK_GorevDocuments_OrgPositions FOREIGN KEY (OrgPositionId) REFERENCES dbo.OrgPositions (Id)
);
GO

IF OBJECT_ID('dbo.GorevVersions','U') IS NULL
CREATE TABLE dbo.GorevVersions (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GorevVersions PRIMARY KEY,
    GorevDocumentId INT NOT NULL, VersionNumber INT NOT NULL,
    ContentJson NVARCHAR(MAX) NOT NULL, PlainTextContent NVARCHAR(MAX) NULL,
    EffectiveDate DATETIME2 NULL, SupersededDate DATETIME2 NULL,
    Status TINYINT NOT NULL CONSTRAINT DF_GorevVersions_Status DEFAULT (0),
    CreatedBy NVARCHAR(200) NULL, CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_GorevVersions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_GorevVersions_GorevDocuments FOREIGN KEY (GorevDocumentId) REFERENCES dbo.GorevDocuments (Id),
    CONSTRAINT UX_GorevVersions_DocumentVersion UNIQUE (GorevDocumentId, VersionNumber)
);
GO

IF OBJECT_ID('dbo.GorevZirveMap','U') IS NULL
CREATE TABLE dbo.GorevZirveMap (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GorevZirveMap PRIMARY KEY,
    GorevDocumentId INT NOT NULL, ZirveDepartman NVARCHAR(100) NULL, ZirveUnvan NVARCHAR(100) NOT NULL,
    MatchType VARCHAR(10) NOT NULL CONSTRAINT DF_GorevZirveMap_MatchType DEFAULT ('manual'),
    Note NVARCHAR(500) NULL, MappedBy NVARCHAR(200) NULL, MappedAt DATETIME2 NOT NULL CONSTRAINT DF_GorevZirveMap_MappedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_GorevZirveMap_GorevDocuments FOREIGN KEY (GorevDocumentId) REFERENCES dbo.GorevDocuments (Id),
    CONSTRAINT UX_GorevZirveMap_ZirveRol UNIQUE (ZirveDepartman, ZirveUnvan)
);
GO

IF OBJECT_ID('dbo.GorevPersonelMap','U') IS NULL
CREATE TABLE dbo.GorevPersonelMap (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GorevPersonelMap PRIMARY KEY,
    Personelno NVARCHAR(50) NOT NULL, GorevDocumentId INT NOT NULL,
    Source VARCHAR(10) NOT NULL CONSTRAINT DF_GorevPersonelMap_Source DEFAULT ('manual'),
    Note NVARCHAR(500) NULL, MappedBy NVARCHAR(200) NULL, MappedAt DATETIME2 NOT NULL CONSTRAINT DF_GorevPersonelMap_MappedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UX_GorevPersonelMap_Personelno UNIQUE (Personelno),
    CONSTRAINT FK_GorevPersonelMap_GorevDocuments FOREIGN KEY (GorevDocumentId) REFERENCES dbo.GorevDocuments (Id)
);
GO

-- Menü/modül kaydı (AppModules) — idempotent. Fresh deploy'da modül menüde görünsün.
IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = N'gorevtanimlari')
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, AssemblyName, ModuleType, GroupKey)
    VALUES (N'gorevtanimlari', N'Görev Tanımları', 1, 15, N'Mosaik.Modules.GorevTanimlari', N'extension', N'org');
GO
