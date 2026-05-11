-- Migration 50: Migration 47-48-49'da tekil oluşturulan tabloları çoğula rename et
-- EF Core convention: DbSet<AiSuggestion> → tablo adı "AiSuggestions"

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractEvent')
    EXEC sp_rename 'dbo.ContractEvent', 'ContractEvents';
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AiSuggestion')
    EXEC sp_rename 'dbo.AiSuggestion', 'AiSuggestions';
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComplianceTemplate')
    EXEC sp_rename 'dbo.ComplianceTemplate', 'ComplianceTemplates';
GO
