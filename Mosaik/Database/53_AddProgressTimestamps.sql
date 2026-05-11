-- Migration 53: ContractAiExtractions.ProgressTimestampsJson (Plan 27 Faz A)
-- Her step için başlangıç zamanı: {"queued":"ISO","ocr":"ISO","ai_stage1":"ISO","ai_stage2":"ISO","done":"ISO"}
-- UI bu JSON'dan delta hesaplar (her adımın süresi = sonraki adımın başlangıcı - kendisi).
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractAiExtractions')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContractAiExtractions') AND name = 'ProgressTimestampsJson')
BEGIN
    ALTER TABLE dbo.ContractAiExtractions ADD ProgressTimestampsJson NVARCHAR(MAX) NULL;
END
