-- Migration 52: ContractAiExtractions.Stage2ResultText kolonu (Plan 27 Faz A)
-- Stage 2 derinleştirme çıktısını ayrı sakla — detay/debug görünümünde göster.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractAiExtractions')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContractAiExtractions') AND name = 'Stage2ResultText')
BEGIN
    ALTER TABLE dbo.ContractAiExtractions ADD Stage2ResultText NVARCHAR(MAX) NULL;
END
