-- Migration 76: Plan 56 M-A — şifreli alan invariant (defense-in-depth, security-reviewer M-1)
-- IsEncrypted=1 ise SADECE ValueText dolu olabilir; typed kolonlar (Number/Date/Bool/File) NULL.
-- C# BuildFieldValue bunu zaten garanti ediyor; bu CHECK ileride başka write-path (import/migration/Gap2)
-- yanlışlıkla IsEncrypted=1 + typed plaintext yazarsa DB düzeyinde engeller (KVKK ihbar PII sızıntısı).
-- İdempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_FSFV_EncryptedTextOnly')
    ALTER TABLE dbo.FormSubmissionFieldValues ADD CONSTRAINT CK_FSFV_EncryptedTextOnly
        CHECK (IsEncrypted = 0 OR
            (ValueNumber IS NULL AND ValueDate IS NULL AND ValueBool IS NULL AND ValueFileId IS NULL));
GO

PRINT 'Migration 76 tamamlandi: CK_FSFV_EncryptedTextOnly (sifreli alan sadece ValueText).';
GO
