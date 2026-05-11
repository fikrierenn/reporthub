-- Migration 51: BaseEntity<int> CreatedBy/UpdatedBy kolonları
-- 47-49 migration'larında eksik kalmıştı, EF SELECT'te SqlException veriyordu.

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractEvents')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContractEvents') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE dbo.ContractEvents ADD CreatedBy NVARCHAR(100) NULL;
END
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContractEvents')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContractEvents') AND name = 'UpdatedBy')
BEGIN
    ALTER TABLE dbo.ContractEvents ADD UpdatedBy NVARCHAR(100) NULL;
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AiSuggestions')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AiSuggestions') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE dbo.AiSuggestions ADD CreatedBy NVARCHAR(100) NULL;
END
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AiSuggestions')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AiSuggestions') AND name = 'UpdatedBy')
BEGIN
    ALTER TABLE dbo.AiSuggestions ADD UpdatedBy NVARCHAR(100) NULL;
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComplianceTemplates')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ComplianceTemplates') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE dbo.ComplianceTemplates ADD CreatedBy NVARCHAR(100) NULL;
END
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComplianceTemplates')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ComplianceTemplates') AND name = 'UpdatedBy')
BEGIN
    ALTER TABLE dbo.ComplianceTemplates ADD UpdatedBy NVARCHAR(100) NULL;
END
