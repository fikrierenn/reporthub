-- Migration 04 — Plan 34.1: SopDocuments KVKK/ISO kurumsal SOP başlık alanları (2026-05-24)
-- PreparedBy, ApprovedBy, ReviewFrequency, Classification.
-- BKM kurumsal prosedür template'i baz alındı (PRD-CRM-001 referans).
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'PreparedBy')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD PreparedBy NVARCHAR(200) NULL;
    PRINT 'SopDocuments.PreparedBy eklendi.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'ApprovedBy')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD ApprovedBy NVARCHAR(200) NULL;
    PRINT 'SopDocuments.ApprovedBy eklendi.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'EffectiveDate')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD EffectiveDate DATETIME2 NULL;
    PRINT 'SopDocuments.EffectiveDate eklendi.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'ReviewFrequency')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD ReviewFrequency NVARCHAR(100) NULL;
    PRINT 'SopDocuments.ReviewFrequency eklendi.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'Classification')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD Classification NVARCHAR(100) NULL;
    PRINT 'SopDocuments.Classification eklendi.';
END
GO

PRINT 'Migration 04 tamamlandı.';
