-- Migration 78: Comments — Plan 54 M5 polymorphic yorum + mention
-- Polymorphic (EntityType + EntityId), FirmaId denormalize (ADR-012), Body sanitize-on-write.
-- entityType lookup ("commentEntityType") seed: contract / circular / sopDocument.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Comments')
BEGIN
    CREATE TABLE dbo.Comments (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Comments PRIMARY KEY,
        FirmaId     INT            NOT NULL,
        EntityType  NVARCHAR(50)   NOT NULL,
        EntityId    INT            NOT NULL,
        Body        NVARCHAR(4000) NOT NULL,
        AuthorId    INT            NOT NULL,
        AuthorName  NVARCHAR(150)  NOT NULL,
        CreatedAt   DATETIME2      NOT NULL CONSTRAINT DF_Comments_CreatedAt DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Comments_Entity' AND object_id = OBJECT_ID('dbo.Comments'))
BEGIN
    CREATE INDEX IX_Comments_Entity ON dbo.Comments(FirmaId, EntityType, EntityId);
END
GO

-- commentEntityType lookup (idempotent, migration 38 pattern)
IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryTypes WHERE Code = N'commentEntityType')
BEGIN
    INSERT INTO dbo.DictionaryTypes (Code, Name, Description, IsActive)
    VALUES (N'commentEntityType', N'Yorum Varlık Türü', N'Plan 54 M5: yorum eklenebilen varlık türleri', 1);
END
GO

DECLARE @CommentEntityTypeId INT = (SELECT Id FROM dbo.DictionaryTypes WHERE Code = N'commentEntityType');

INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive)
SELECT v.TypeId, v.Code, v.Label, v.DisplayOrder, 1
FROM (VALUES
    (@CommentEntityTypeId, N'contract',    N'Sözleşme', 10),
    (@CommentEntityTypeId, N'circular',    N'Tamim',    20),
    (@CommentEntityTypeId, N'sopDocument', N'SOP',      30)
) v (TypeId, Code, Label, DisplayOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DictionaryValues dv
    WHERE dv.TypeId = v.TypeId AND dv.Code = v.Code
);
GO

PRINT 'Migration 78 tamamlandi: Comments tablosu + commentEntityType lookup seed edildi.';
