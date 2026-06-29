-- Migration 76: Plan 21 + Operax port — DictionaryType.IsSystemDefined (AllowValueCrud lock)
-- Tarih: 2026-06-29
-- Enum-anchored tipler (contract*/obligation*/recurrence*/event*) sistem-tanımlı işaretlenir:
-- admin yeni değer EKLEYEMEZ (bozuk Code → enum binding kırılması engellenir).
-- Mevcut değerlerin Label/DisplayOrder/IsActive düzenlemesi serbest kalır.
-- İdempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.DictionaryTypes') AND name = N'IsSystemDefined')
BEGIN
    ALTER TABLE dbo.DictionaryTypes ADD IsSystemDefined BIT NOT NULL CONSTRAINT DF_DictionaryTypes_IsSystemDefined DEFAULT 0;
    PRINT 'IsSystemDefined kolonu eklendi.';
END
GO

UPDATE dbo.DictionaryTypes
SET IsSystemDefined = 1
WHERE Code IN (N'contractCategory', N'obligationCategory', N'obligationType', N'recurrenceType', N'eventType')
  AND IsSystemDefined = 0;
GO

PRINT 'Migration 76: 5 enum-anchored tip IsSystemDefined=1 (idempotent).';
GO
