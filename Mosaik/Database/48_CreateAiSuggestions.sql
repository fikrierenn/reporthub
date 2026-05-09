-- Migration 48: AiSuggestion tablosu (AI Review ekranı)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AiSuggestion')
BEGIN
    CREATE TABLE dbo.AiSuggestion (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        FirmaId             INT NOT NULL,
        ExtractionId        INT NOT NULL,
        SuggestionType      INT NOT NULL DEFAULT 0,  -- 0=Obligation,1=ContractEvent,2=RiskWarning
        Title               NVARCHAR(200) NOT NULL,
        Description         NVARCHAR(2000) NULL,
        SuggestionDataJson  NVARCHAR(MAX) NULL,
        Confidence          INT NOT NULL DEFAULT 1,  -- 0=High,1=Medium,2=Low
        Status              INT NOT NULL DEFAULT 0,  -- 0=Pending,1=Approved,2=Rejected
        ApprovedBy          NVARCHAR(100) NULL,
        ApprovedAt          DATETIME2 NULL,
        CreatedObligationId INT NULL,
        CreatedEventId      INT NULL,
        CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_AiSuggestion_Firma      FOREIGN KEY (FirmaId)      REFERENCES dbo.Firmas(FirmaId),
        CONSTRAINT FK_AiSuggestion_Extraction FOREIGN KEY (ExtractionId) REFERENCES dbo.ContractAiExtractions(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AiSuggestion_ExtractionId_Status ON dbo.AiSuggestion (ExtractionId, Status);
END
GO
