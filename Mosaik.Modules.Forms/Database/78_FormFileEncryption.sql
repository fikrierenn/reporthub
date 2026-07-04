-- =============================================================================
-- 78 — FormSubmissionFiles.IsEncrypted (Plan 57 A2-#1)
-- =============================================================================
-- Şifreli formda (IsEncrypted — ihbar) dosya ekleri App_Data'da DÜZ METİN kalıyordu;
-- alan-şifrelemenin (76/G1) amacını deliyordu. Artık form IsEncrypted ise dosya içeriği
-- DataProtection ile şifreli yazılır; bu kolon indirmede decrypt-gate'i tetikler.
-- İdempotent.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.FormSubmissionFiles') AND name = N'IsEncrypted')
BEGIN
    ALTER TABLE dbo.FormSubmissionFiles
        ADD IsEncrypted BIT NOT NULL CONSTRAINT DF_FormSubmissionFiles_IsEncrypted DEFAULT 0;
    PRINT 'FormSubmissionFiles.IsEncrypted eklendi.';
END
ELSE
    PRINT 'IsEncrypted zaten mevcut - atlandi.';
