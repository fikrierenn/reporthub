-- Migration 44: Users.FirmaIds CSV kolon (çoklu firma erişimi)
-- Plan 25 — User N firmaya erişebilir. NULL/boş = modül kapalı.
-- Format: "1,2,3" veya "1,2" veya "1" veya NULL.
-- Eski Users.FirmaId int kolonu (Migration 42) yerine FirmaIds NVARCHAR(50).

-- Adım 1: yeni kolonu ekle (idempotent)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'FirmaIds')
BEGIN
    ALTER TABLE dbo.Users ADD FirmaIds NVARCHAR(50) NULL;
    PRINT 'Users.FirmaIds kolonu eklendi.';
END
GO

-- Adım 2: eski Users.FirmaId verisini FirmaIds CSV'ye taşı (kolon henüz drop edilmedi)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'FirmaId')
BEGIN
    EXEC sp_executesql N'
        UPDATE dbo.Users
        SET FirmaIds = CAST(FirmaId AS NVARCHAR(20))
        WHERE FirmaId IS NOT NULL AND FirmaIds IS NULL;
    ';
    PRINT 'Mevcut Users.FirmaId verileri FirmaIds CSV''ye taşındı.';
END
GO

-- Adım 3: FK constraint drop (varsa)
IF EXISTS (SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('dbo.Users') AND name = 'FK_Users_Firmas')
BEGIN
    ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_Firmas;
    PRINT 'FK_Users_Firmas drop edildi.';
END
GO

-- Adım 4: eski Users.FirmaId kolonunu drop et
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'FirmaId')
BEGIN
    ALTER TABLE dbo.Users DROP COLUMN FirmaId;
    PRINT 'Users.FirmaId kolonu drop edildi.';
END
GO

PRINT 'Migration 44 tamamlandı.';
