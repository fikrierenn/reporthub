-- Migration 06 — Plan 34.1 Faz 2 A-12: SOP RAG advisor soru-cevap geçmişi (2026-05-24)
-- KVKK retention: 1 yıl (Faz 6 Hangfire cleanup job). Audit log ana kaydı 5-yıl ayrı tutulur.
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SopAiConversations')
BEGIN
    CREATE TABLE dbo.SopAiConversations (
        Id                      INT IDENTITY(1,1) PRIMARY KEY,
        UserId                  INT          NOT NULL,
        FirmaId                 INT          NULL,
        Question                NVARCHAR(MAX) NOT NULL,
        Answer                  NVARCHAR(MAX) NOT NULL,
        SourceSopVersionIds     NVARCHAR(500) NULL,
        TokensIn                INT          NULL,
        TokensOut               INT          NULL,
        UserFeedback            TINYINT      NOT NULL DEFAULT 0,
        FeedbackNote            NVARCHAR(500) NULL,
        CreatedAt               DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
        FeedbackAt              DATETIME2    NULL,
        CONSTRAINT CK_SopAiConversations_Feedback CHECK (UserFeedback IN (0, 1, 2))
    );
    -- Faz 3 rate limit query (UserId + son 1 saat) için index.
    CREATE INDEX IX_SopAiConversations_UserId_CreatedAt
        ON dbo.SopAiConversations(UserId, CreatedAt DESC);
    -- Admin dashboard "thumbs-down listesi" (Faz 5 A-27) için.
    CREATE INDEX IX_SopAiConversations_Feedback
        ON dbo.SopAiConversations(UserFeedback)
        WHERE UserFeedback = 2;
    PRINT 'Migration 06 — SopAiConversations created.';
END
ELSE
    PRINT 'SopAiConversations zaten mevcut, atlandı.';
GO
