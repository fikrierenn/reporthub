-- Migration 03 — Plan 34.1: SopDocuments kurumsal metadata kolonları (2026-05-24)
-- DocumentNumber, RevisionNumber, PublishDate, RevisionDate.
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'DocumentNumber')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD DocumentNumber NVARCHAR(50) NULL;
    PRINT 'SopDocuments.DocumentNumber eklendi.';
END
ELSE
    PRINT 'SopDocuments.DocumentNumber zaten mevcut.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'RevisionNumber')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD RevisionNumber NVARCHAR(50) NULL;
    PRINT 'SopDocuments.RevisionNumber eklendi.';
END
ELSE
    PRINT 'SopDocuments.RevisionNumber zaten mevcut.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'PublishDate')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD PublishDate DATETIME2 NULL;
    PRINT 'SopDocuments.PublishDate eklendi.';
END
ELSE
    PRINT 'SopDocuments.PublishDate zaten mevcut.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SopDocuments') AND name = 'RevisionDate')
BEGIN
    ALTER TABLE dbo.SopDocuments ADD RevisionDate DATETIME2 NULL;
    PRINT 'SopDocuments.RevisionDate eklendi.';
END
ELSE
    PRINT 'SopDocuments.RevisionDate zaten mevcut.';
GO

PRINT 'Migration 03 tamamlandı.';
