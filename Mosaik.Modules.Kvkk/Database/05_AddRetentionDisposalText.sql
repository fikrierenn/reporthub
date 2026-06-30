-- Plan 54 M6 / Plan 40 Faz 6 — KvkkProcesses serbest-metin saklama/imha kolonları.
-- Import xlsx "Saklama Süresi"(col16) + "İmha Yöntemi"(col17) değerlerini korur (VERBİS kaynağı).
-- Idempotent.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KvkkProcesses') AND name = 'RetentionText')
    ALTER TABLE dbo.KvkkProcesses ADD RetentionText NVARCHAR(500) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KvkkProcesses') AND name = 'DisposalText')
    ALTER TABLE dbo.KvkkProcesses ADD DisposalText NVARCHAR(500) NULL;
GO

PRINT 'KVKK Faz 6: RetentionText + DisposalText kolonlari eklendi (05).';
