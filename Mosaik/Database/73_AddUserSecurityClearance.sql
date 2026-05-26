-- Migration 73 — Plan 44 Faz 1: Users.SecurityClearance nullable byte (2026-05-26)
-- NULL = role'den türet; set edilmişse override (yönetim kurulu üyesi gibi).
-- Clearance map (default): admin=3, hr/finans/manager=2, user=1, guest=0
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'SecurityClearance'
)
BEGIN
    ALTER TABLE dbo.Users
        ADD SecurityClearance TINYINT NULL;
    PRINT 'Migration 73 — Users.SecurityClearance eklendi.';
END
ELSE
    PRINT 'Users.SecurityClearance zaten mevcut, atlandı.';
GO
