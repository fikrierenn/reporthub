-- Migration 43: Plan 25 Sözleşme modülü tabloları
-- Bağımlılık: Migration 42 (Firmas tablosu zorunlu)

-- ContractRecurrences (bağımlılık yok)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractRecurrences')
BEGIN
    CREATE TABLE dbo.ContractRecurrences (
        Id              INT NOT NULL IDENTITY(1,1),
        RecurrenceType  INT NOT NULL DEFAULT 0,     -- 0=Monthly, 1=Quarterly, 2=Yearly, 3=Custom
        IntervalValue   INT NOT NULL DEFAULT 1,
        DayOfMonth      INT NULL,
        MonthOfYear     INT NULL,
        StartDate       DATE NOT NULL,
        EndDate         DATE NULL,
        MaxOccurrences  INT NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2 NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT PK_ContractRecurrences PRIMARY KEY (Id)
    );
    PRINT 'ContractRecurrences tablosu oluşturuldu.';
END

-- Contracts
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Contracts')
BEGIN
    CREATE TABLE dbo.Contracts (
        Id              INT NOT NULL IDENTITY(1,1),
        FirmaId         INT NOT NULL,
        Title           NVARCHAR(200) NOT NULL,
        Counterparty    NVARCHAR(200) NULL,
        Category        INT NOT NULL DEFAULT 6,     -- 6=Other
        Status          INT NOT NULL DEFAULT 0,     -- 0=Draft
        StartDate       DATE NULL,
        EndDate         DATE NULL,
        Notes           NVARCHAR(2000) NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2 NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT PK_Contracts PRIMARY KEY (Id),
        CONSTRAINT FK_Contracts_Firmas FOREIGN KEY (FirmaId) REFERENCES dbo.Firmas(FirmaId)
    );
    CREATE INDEX IX_Contracts_FirmaId ON dbo.Contracts (FirmaId);
    CREATE INDEX IX_Contracts_Status  ON dbo.Contracts (Status);
    PRINT 'Contracts tablosu oluşturuldu.';
END

-- ContractObligations
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractObligations')
BEGIN
    CREATE TABLE dbo.ContractObligations (
        Id                  INT NOT NULL IDENTITY(1,1),
        FirmaId             INT NOT NULL,
        ContractId          INT NULL,
        Title               NVARCHAR(200) NOT NULL,
        Category            INT NOT NULL DEFAULT 0,     -- 0=Finance
        Type                INT NOT NULL DEFAULT 0,     -- 0=Payment
        Amount              DECIMAL(18,2) NULL,
        Currency            NVARCHAR(3) NOT NULL DEFAULT N'TRY',
        DueDate             DATE NOT NULL,
        ReminderDays        INT NULL,
        Status              INT NOT NULL DEFAULT 0,     -- 0=Pending
        Source              INT NOT NULL DEFAULT 0,     -- 0=Manual
        RecurrenceId        INT NULL,
        ParentObligationId  INT NULL,
        Notes               NVARCHAR(2000) NULL,
        CompletedAt         DATETIME2 NULL,
        CompletedBy         NVARCHAR(100) NULL,
        CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy           NVARCHAR(100) NULL,
        UpdatedAt           DATETIME2 NULL,
        UpdatedBy           NVARCHAR(100) NULL,
        CONSTRAINT PK_ContractObligations PRIMARY KEY (Id),
        CONSTRAINT FK_ContractObl_Firmas FOREIGN KEY (FirmaId) REFERENCES dbo.Firmas(FirmaId),
        CONSTRAINT FK_ContractObl_Contracts FOREIGN KEY (ContractId) REFERENCES dbo.Contracts(Id) ON DELETE SET NULL,
        CONSTRAINT FK_ContractObl_Recurrences FOREIGN KEY (RecurrenceId) REFERENCES dbo.ContractRecurrences(Id) ON DELETE SET NULL,
        CONSTRAINT FK_ContractObl_Parent FOREIGN KEY (ParentObligationId) REFERENCES dbo.ContractObligations(Id)
    );
    CREATE INDEX IX_ContractObl_FirmaId_Status ON dbo.ContractObligations (FirmaId, Status);
    CREATE INDEX IX_ContractObl_DueDate ON dbo.ContractObligations (DueDate);
    PRINT 'ContractObligations tablosu oluşturuldu.';
END

-- ContractFiles
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractFiles')
BEGIN
    CREATE TABLE dbo.ContractFiles (
        Id              INT NOT NULL IDENTITY(1,1),
        FirmaId         INT NOT NULL,
        ContractId      INT NULL,
        ObligationId    INT NULL,
        FileName        NVARCHAR(260) NOT NULL,
        FilePath        NVARCHAR(500) NOT NULL,
        FileSize        BIGINT NOT NULL DEFAULT 0,
        MimeType        NVARCHAR(100) NOT NULL DEFAULT N'',
        Version         INT NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2 NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT PK_ContractFiles PRIMARY KEY (Id),
        CONSTRAINT FK_ContractFiles_Firmas FOREIGN KEY (FirmaId) REFERENCES dbo.Firmas(FirmaId),
        CONSTRAINT FK_ContractFiles_Contracts FOREIGN KEY (ContractId) REFERENCES dbo.Contracts(Id) ON DELETE SET NULL,
        CONSTRAINT FK_ContractFiles_Obligations FOREIGN KEY (ObligationId) REFERENCES dbo.ContractObligations(Id) ON DELETE SET NULL
    );
    PRINT 'ContractFiles tablosu oluşturuldu.';
END

-- ContractAiExtractions
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractAiExtractions')
BEGIN
    CREATE TABLE dbo.ContractAiExtractions (
        Id                      INT NOT NULL IDENTITY(1,1),
        FirmaId                 INT NOT NULL,
        ContractFileId          INT NOT NULL,
        ContractId              INT NULL,
        Status                  INT NOT NULL DEFAULT 0,     -- 0=Processing
        PromptVersion           NVARCHAR(50) NULL,
        ModelUsed               NVARCHAR(100) NULL,
        RawText                 NVARCHAR(MAX) NULL,
        ExtractionResultJson    NVARCHAR(MAX) NULL,
        InputTokens             INT NOT NULL DEFAULT 0,
        OutputTokens            INT NOT NULL DEFAULT 0,
        ProcessedAt             DATETIME2 NULL,
        ReviewedBy              NVARCHAR(100) NULL,
        ReviewedAt              DATETIME2 NULL,
        ErrorMessage            NVARCHAR(1000) NULL,
        ProgressStep            NVARCHAR(100) NULL,
        CreatedAt               DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy               NVARCHAR(100) NULL,
        UpdatedAt               DATETIME2 NULL,
        UpdatedBy               NVARCHAR(100) NULL,
        CONSTRAINT PK_ContractAiExtractions PRIMARY KEY (Id),
        CONSTRAINT FK_ContractAiExt_Firmas FOREIGN KEY (FirmaId) REFERENCES dbo.Firmas(FirmaId),
        CONSTRAINT FK_ContractAiExt_Files FOREIGN KEY (ContractFileId) REFERENCES dbo.ContractFiles(Id),
        CONSTRAINT FK_ContractAiExt_Contracts FOREIGN KEY (ContractId) REFERENCES dbo.Contracts(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_ContractAiExt_Status ON dbo.ContractAiExtractions (Status);
    PRINT 'ContractAiExtractions tablosu oluşturuldu.';
END

PRINT 'Migration 43 tamamlandı.';
