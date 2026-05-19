-- Migration 62: Notifications.ExternalKey — N-1 Hibrit idempotency
-- Amaç: DailyReminderJob aynı yükümlülük için birden fazla bildirim oluşturmasın.
-- ExternalKey = "obligation_reminder:{obligationId}:{date:yyyyMMdd}" gibi caller-defined string.
-- Filtered unique index: NULL kabul edilir (ExternalKey gerektirmeyen bildirimler), NULL olanlar eşsizlik dışı.
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Notifications' AND COLUMN_NAME = 'ExternalKey'
)
BEGIN
    ALTER TABLE Notifications ADD ExternalKey NVARCHAR(256) NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UQ_Notifications_ExternalKey' AND object_id = OBJECT_ID('Notifications')
)
BEGIN
    -- EXEC zorunlu: kolon aynı batch'te eklenince SQL parser derleme aşamasında bulamıyor.
    EXEC sp_executesql N'CREATE UNIQUE INDEX UQ_Notifications_ExternalKey ON Notifications (ExternalKey) WHERE ExternalKey IS NOT NULL';
END
