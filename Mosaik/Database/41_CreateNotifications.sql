-- Migration 41: Plan 17 Faz H — cross-modül bildirim tablosu
-- Tarih: 2026-05-09
-- Plan: plans/17-tamim.md (Faz H)
-- Mosaik.Core.Notification.Notification entity'sine eşlenir.
-- Cross-modül kullanım: Circular publish, Approval pending, HR sync, Görev atama vb.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notifications' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Notifications
    (
        Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
        UserId            INT           NOT NULL,
        EntityType        NVARCHAR(50)  NOT NULL,
        EntityId          INT           NULL,
        NotificationType  NVARCHAR(50)  NULL,
        Title             NVARCHAR(200) NOT NULL,
        Message           NVARCHAR(500) NULL,
        TargetUrl         NVARCHAR(500) NULL,
        IsRead            BIT           NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
        ReadAt            DATETIME2(0)  NULL,
        CreatedAt         DATETIME2(0)  NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy         NVARCHAR(100) NULL,
        UpdatedAt         DATETIME2(0)  NULL,
        UpdatedBy         NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.Notifications olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.Notifications zaten mevcut, atlandi.';
GO

-- Sidebar badge sorgusu için (UserId + IsRead) en sık erişim. CreatedAt sıralama için INCLUDE.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserUnread')
    CREATE NONCLUSTERED INDEX IX_Notifications_UserUnread
        ON dbo.Notifications (UserId, IsRead)
        INCLUDE (CreatedAt, Title);
GO

-- Liste sayfası için son N bildirim sıralaması.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserCreatedAt')
    CREATE NONCLUSTERED INDEX IX_Notifications_UserCreatedAt
        ON dbo.Notifications (UserId, CreatedAt DESC);
GO

PRINT 'Migration 41 tamamlandi: Notifications + IX_UserUnread + IX_UserCreatedAt.';
GO
