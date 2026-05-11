-- Migration 54: ContractFile.AiSummary + AiTagsJson + AiClassifiedAt (Plan 27 Faz B)
-- Auto-classification + executive summary için yeni alanlar.
-- AiTagsJson: {"documentType":"...","year":2026,"department":"...","tags":["..."]}
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractFiles')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContractFiles') AND name = 'AiSummary')
BEGIN
    ALTER TABLE dbo.ContractFiles ADD AiSummary NVARCHAR(2000) NULL;
END
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractFiles')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContractFiles') AND name = 'AiTagsJson')
BEGIN
    ALTER TABLE dbo.ContractFiles ADD AiTagsJson NVARCHAR(MAX) NULL;
END
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractFiles')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContractFiles') AND name = 'AiClassifiedAt')
BEGIN
    ALTER TABLE dbo.ContractFiles ADD AiClassifiedAt DATETIME2 NULL;
END
