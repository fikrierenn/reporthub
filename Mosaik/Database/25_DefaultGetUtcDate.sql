-- 25_DefaultGetUtcDate.sql
-- ADR-006 Faz D: 13 DEFAULT constraint GETDATE() → GETUTCDATE().
-- Faz C'de uygulama kodu DateTime.UtcNow'a tasinmisti; bu migration DB tarafini hizalar.
-- Mevcut veri DEGISMEZ (sadece sonraki INSERT'lerde default UTC olur). Veri shift Faz E.
--
-- Plan: ADR-006 (DateTime UTC); TODO.md "DateTime Faz D".
-- Tier: 2 (tek migration, schema-only, Tier 3 eşiği değil).
--
-- Idempotency: ALTER TABLE DROP CONSTRAINT yoksa hata vermez (IF EXISTS).
-- ROLLBACK: tersini çalıştırarak GETDATE'e geri dönülebilir.

USE [PortalHUB];
GO

-- Helper: DROP DEFAULT (named) + ADD DEFAULT (UTC) pattern.
-- Dinamik constraint adi alındığı için sys.default_constraints lookup yapıyoruz.
DECLARE @sql nvarchar(max) = N'';
DECLARE @tbl sysname, @col sysname, @cn sysname;

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.name, c.name, dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    INNER JOIN sys.tables t ON t.object_id = dc.parent_object_id
    WHERE dc.definition LIKE N'%getdate%' AND dc.definition NOT LIKE N'%getutcdate%';

OPEN cur;
FETCH NEXT FROM cur INTO @tbl, @col, @cn;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'ALTER TABLE [dbo].[' + @tbl + N'] DROP CONSTRAINT [' + @cn + N'];'
        + N' ALTER TABLE [dbo].[' + @tbl + N'] ADD CONSTRAINT [DF_' + @tbl + N'_' + @col + N']'
        + N' DEFAULT (GETUTCDATE()) FOR [' + @col + N'];';
    PRINT @sql;
    EXEC sp_executesql @sql;

    FETCH NEXT FROM cur INTO @tbl, @col, @cn;
END
CLOSE cur;
DEALLOCATE cur;
GO

-- Doğrulama
SELECT t.name AS Tablo, c.name AS Kolon, dc.name AS ConstraintAd, dc.definition AS Tanim
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables t ON t.object_id = dc.parent_object_id
WHERE dc.definition LIKE N'%getdate%' OR dc.definition LIKE N'%getutcdate%';
GO
