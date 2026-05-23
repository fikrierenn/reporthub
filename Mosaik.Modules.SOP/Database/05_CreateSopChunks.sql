-- Migration 05 — Plan 34.1 Faz 1 A-08: SOP RAG chunk + embedding tablosu (2026-05-24)
-- intfloat/multilingual-e5-base embeddings (768-dim, JSON serialize).
-- SopVersion silinince cascade temizler — version-scoped lifetime.
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SopChunks')
BEGIN
    CREATE TABLE dbo.SopChunks (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        SopVersionId    INT          NOT NULL,
        ChunkOrder      INT          NOT NULL,
        Content         NVARCHAR(MAX) NOT NULL,
        EmbeddingJson   NVARCHAR(MAX) NOT NULL,
        CreatedAt       DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SopChunks_SopVersion
            FOREIGN KEY (SopVersionId) REFERENCES dbo.SopVersions(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_SopChunks_SopVersionId_ChunkOrder
        ON dbo.SopChunks(SopVersionId, ChunkOrder);
    PRINT 'Migration 05 — SopChunks created.';
END
ELSE
    PRINT 'SopChunks zaten mevcut, atlandı.';
GO
