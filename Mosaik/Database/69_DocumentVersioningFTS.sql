-- Plan 27 Faz C (2026-05-22) — Document versioning + Full-Text Search
-- 1. DocumentVersions tablosu (eski dosya versiyonları)
-- 2. ContractFiles.ContentText kolonu (PDF'den çıkarılan metin, FTS için)
-- 3. Full-Text Search catalog + index (FTS feature gerekli; yoksa CATCH ile atlanır)

-- 1) DocumentVersions
IF OBJECT_ID('dbo.DocumentVersions') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentVersions (
        Id              INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_DocumentVersions PRIMARY KEY CLUSTERED,
        ContractFileId  INT NOT NULL,
        VersionNumber   INT NOT NULL,
        FilePath        NVARCHAR(500) NOT NULL,
        FileName        NVARCHAR(260) NOT NULL,
        FileSize        BIGINT        NOT NULL DEFAULT 0,
        MimeType        NVARCHAR(100) NOT NULL DEFAULT '',
        ArchivedAt      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        ArchivedById    INT           NOT NULL DEFAULT 0,
        CONSTRAINT FK_DocVersions_ContractFiles
            FOREIGN KEY (ContractFileId)
            REFERENCES dbo.ContractFiles (Id)
            ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_DocVersions_ContractFileId
        ON dbo.DocumentVersions (ContractFileId);

    PRINT 'DocumentVersions tablosu oluşturuldu.';
END
ELSE
    PRINT 'DocumentVersions zaten mevcut, atlandı.';
GO

-- 2) ContentText kolonu (PDF metni — NVARCHAR(MAX))
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ContractFiles') AND name = 'ContentText'
)
BEGIN
    ALTER TABLE dbo.ContractFiles ADD ContentText NVARCHAR(MAX) NULL;
    PRINT 'ContractFiles.ContentText kolonu eklendi.';
END
ELSE
    PRINT 'ContractFiles.ContentText zaten mevcut, atlandı.';
GO

-- 3) Full-Text Search — bu ortamda FTS feature kurulu değil.
-- ContentText kolonu LIKE '%q%' ile çalışıyor (LIKE fallback).
-- FTS gerekirse ayrı migration: MosaikDocsFT catalog + FULLTEXT INDEX ON ContractFiles.
-- CREATE FULLTEXT INDEX ON dbo.ContractFiles (FileName, ContentText)
--     KEY INDEX PK_ContractFiles ON MosaikDocsFT WITH CHANGE_TRACKING AUTOMATIC;
PRINT 'FTS atlandı — LIKE fallback aktif.';
GO
