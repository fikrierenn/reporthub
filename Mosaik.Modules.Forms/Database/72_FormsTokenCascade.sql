-- Migration 72 — Plan 45 F-3: PublicFormTokens FK cascade (2026-05-22)
-- EF FormsModule.cs:111 OnDelete(DeleteBehavior.Cascade) ile SQL FK uyumsuzdu.
-- FormDefinition silinince ilgili PublicFormToken'lar da silinmeli — aksi halde FK 547.
-- Idempotent.

SET NOCOUNT ON;
GO

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_PublicFormTokens_FormDefinitions'
      AND fk.delete_referential_action <> 1  -- 1 = CASCADE
)
BEGIN
    ALTER TABLE dbo.PublicFormTokens
        DROP CONSTRAINT FK_PublicFormTokens_FormDefinitions;

    ALTER TABLE dbo.PublicFormTokens
        ADD CONSTRAINT FK_PublicFormTokens_FormDefinitions
        FOREIGN KEY (FormDefinitionId)
        REFERENCES dbo.FormDefinitions(Id)
        ON DELETE CASCADE;

    PRINT 'FK_PublicFormTokens_FormDefinitions ON DELETE CASCADE olarak güncellendi.';
END
ELSE
BEGIN
    PRINT 'FK_PublicFormTokens_FormDefinitions zaten CASCADE veya mevcut değil — atlandı.';
END
GO

PRINT 'Migration 72 tamamlandi.';
