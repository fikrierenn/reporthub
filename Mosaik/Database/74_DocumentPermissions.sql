-- Plan 27 Faz C-06 — DocumentPermissions tablosu
-- Subject: user | role. Scope: ContractFileId (dosya) veya FolderId (klasör).
-- Level: 1=Oku, 2=Yaz, 3=Yönet.

CREATE TABLE dbo.DocumentPermissions (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    FirmaId         INT NOT NULL,
    SubjectType     NVARCHAR(10) NOT NULL,   -- 'user' | 'role'
    SubjectId       INT NOT NULL,
    ContractFileId  INT NULL REFERENCES dbo.ContractFiles(Id) ON DELETE CASCADE,
    FolderId        INT NULL,
    Level           TINYINT NOT NULL DEFAULT 1,
    GrantedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GrantedById     INT NOT NULL,
    ValidUntil      DATETIME2 NULL
);

CREATE INDEX IX_DocPerm_Subject ON dbo.DocumentPermissions (FirmaId, SubjectType, SubjectId);
CREATE INDEX IX_DocPerm_File    ON dbo.DocumentPermissions (ContractFileId) WHERE ContractFileId IS NOT NULL;

-- Rollback: DROP TABLE dbo.DocumentPermissions;
