-- Migration 73 — Plan 45 F-4: Forms DB-level invariants (2026-05-22)
-- Runtime validation Faz 1+ gelecek olsa bile DB-level guard değerli.
-- Idempotent: her constraint için IF NOT EXISTS check.

SET NOCOUNT ON;
GO

------------------------------------------------------------------------------
-- 1. FormDefinitions.Status enum range (0 Taslak | 1 Yayında | 2 Arşiv)
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_FormDefinitions_Status')
BEGIN
    ALTER TABLE dbo.FormDefinitions
        ADD CONSTRAINT CK_FormDefinitions_Status
        CHECK (Status BETWEEN 0 AND 2);
    PRINT 'CK_FormDefinitions_Status eklendi (0..2).';
END
ELSE
    PRINT 'CK_FormDefinitions_Status zaten mevcut, atlandı.';
GO

------------------------------------------------------------------------------
-- 2. FormFields.FieldType enum range (0 text .. 12 section)
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_FormFields_FieldType')
BEGIN
    ALTER TABLE dbo.FormFields
        ADD CONSTRAINT CK_FormFields_FieldType
        CHECK (FieldType BETWEEN 0 AND 12);
    PRINT 'CK_FormFields_FieldType eklendi (0..12).';
END
ELSE
    PRINT 'CK_FormFields_FieldType zaten mevcut, atlandı.';
GO

------------------------------------------------------------------------------
-- 3. FormFieldDataElementMaps.UsageType enum range (0 collects | 1 derives)
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_FFDEM_UsageType')
BEGIN
    ALTER TABLE dbo.FormFieldDataElementMaps
        ADD CONSTRAINT CK_FFDEM_UsageType
        CHECK (UsageType BETWEEN 0 AND 1);
    PRINT 'CK_FFDEM_UsageType eklendi (0..1).';
END
ELSE
    PRINT 'CK_FFDEM_UsageType zaten mevcut, atlandı.';
GO

------------------------------------------------------------------------------
-- 4. FormSubmissions.Status enum range (0 Submitted | 1 InReview | 2 Completed | 3 Rejected)
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_FormSubmissions_Status')
BEGIN
    ALTER TABLE dbo.FormSubmissions
        ADD CONSTRAINT CK_FormSubmissions_Status
        CHECK (Status BETWEEN 0 AND 3);
    PRINT 'CK_FormSubmissions_Status eklendi (0..3).';
END
ELSE
    PRINT 'CK_FormSubmissions_Status zaten mevcut, atlandı.';
GO

------------------------------------------------------------------------------
-- 5. PublicFormTokens: UsedCount <= MaxUses + UsedCount/MaxUses non-negative
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_PublicFormTokens_Usage')
BEGIN
    ALTER TABLE dbo.PublicFormTokens
        ADD CONSTRAINT CK_PublicFormTokens_Usage
        CHECK (UsedCount >= 0 AND MaxUses >= 1 AND UsedCount <= MaxUses);
    PRINT 'CK_PublicFormTokens_Usage eklendi (UsedCount<=MaxUses, MaxUses>=1, UsedCount>=0).';
END
ELSE
    PRINT 'CK_PublicFormTokens_Usage zaten mevcut, atlandı.';
GO

------------------------------------------------------------------------------
-- 6. FormSubmissionFieldValues: en fazla 1 value column NOT NULL
--    (Bool, Number, Date, Text, FileId — XOR. Hepsi NULL olabilir; runtime FieldType eşleştirir.)
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = N'CK_FSFV_SingleValue')
BEGIN
    ALTER TABLE dbo.FormSubmissionFieldValues
        ADD CONSTRAINT CK_FSFV_SingleValue
        CHECK (
            (CASE WHEN ValueText   IS NULL THEN 0 ELSE 1 END
           + CASE WHEN ValueNumber IS NULL THEN 0 ELSE 1 END
           + CASE WHEN ValueDate   IS NULL THEN 0 ELSE 1 END
           + CASE WHEN ValueBool   IS NULL THEN 0 ELSE 1 END
           + CASE WHEN ValueFileId IS NULL THEN 0 ELSE 1 END) <= 1
        );
    PRINT 'CK_FSFV_SingleValue eklendi (max 1 value column).';
END
ELSE
    PRINT 'CK_FSFV_SingleValue zaten mevcut, atlandı.';
GO

PRINT 'Migration 73 tamamlandi.';
