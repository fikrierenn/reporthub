-- Migration 47: ContractEvent tablosu (Calendar modülü)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractEvent')
BEGIN
    CREATE TABLE dbo.ContractEvent (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId       INT NOT NULL,
        ContractId    INT NULL,
        ObligationId  INT NULL,
        Title         NVARCHAR(200) NOT NULL,
        EventDate     DATE NOT NULL,
        EventType     INT NOT NULL DEFAULT 1,   -- 0=Payment,1=Deadline,2=Renewal,3=Tax,4=Compliance,5=Operation
        Status        INT NOT NULL DEFAULT 0,   -- 0=Upcoming,1=Completed,2=Cancelled
        ReminderDays  INT NOT NULL DEFAULT 7,
        Notes         NVARCHAR(500) NULL,
        Source        INT NOT NULL DEFAULT 0,   -- 0=Manual,1=AiSuggested
        CreatedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_ContractEvent_Firma      FOREIGN KEY (FirmaId)      REFERENCES dbo.Firmas(FirmaId),
        CONSTRAINT FK_ContractEvent_Contract   FOREIGN KEY (ContractId)   REFERENCES dbo.Contracts(Id),
        CONSTRAINT FK_ContractEvent_Obligation FOREIGN KEY (ObligationId) REFERENCES dbo.ContractObligations(Id)
    );

    CREATE INDEX IX_ContractEvent_FirmaId_EventDate ON dbo.ContractEvent (FirmaId, EventDate);
    CREATE INDEX IX_ContractEvent_Status            ON dbo.ContractEvent (Status);
END
GO
