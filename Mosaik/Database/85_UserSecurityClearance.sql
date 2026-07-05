-- =============================================================================
-- 85 — Users.SecurityClearance (Plan 44 karar §10.1 — kalan delta)
-- =============================================================================
-- RAG erişim seviyesi: default rollerden türetilir (RagUserContext.ClearanceFromRoles);
-- bu kolon KULLANICI-ÖZEL OVERRIDE (örn. yönetim kurulu üyesi 'user' rolünde ama
-- restricted içerik görmeli). NULL = override yok, role-map çalışır.
-- 0=public 1=internal 2=confidential 3=restricted. İdempotent.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'SecurityClearance')
BEGIN
    ALTER TABLE dbo.Users ADD SecurityClearance TINYINT NULL;
    PRINT 'Users.SecurityClearance eklendi.';
END
ELSE PRINT 'SecurityClearance zaten var.';
