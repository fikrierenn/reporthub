-- M-03 Faz C (4 Mayis 2026)
-- User.Roles CSV kolonu DROP. Faz A (kod-duzeyi temizlik, commit 2d0c3fd) +
-- Faz B (NULL + [Obsolete], commit bf922ae) sonrasi hicbir kod yolu
-- User.Roles'a yazmiyor; UserRole junction tablosu tek resmi rol kaynagi.
--
-- Bu migration kolonu DB'den kaldirir.
--
-- ADR-003: docs/ADR/003-role-model.md
-- ROLLBACK: Kolonu yeniden olusturmak mumkun ama icindeki veri kaybolur;
-- restore icin backup gerekli. Calistirma oncesi DB backup zorunlu.

USE [PortalHUB];
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'Roles'
)
BEGIN
    -- Onceden default constraint varsa once drop et (kolon drop'u patlamasin).
    DECLARE @ConstraintName NVARCHAR(200);
    SELECT @ConstraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[Users]')
      AND c.name = N'Roles';

    IF @ConstraintName IS NOT NULL
    BEGIN
        EXEC ('ALTER TABLE [dbo].[Users] DROP CONSTRAINT [' + @ConstraintName + ']');
        PRINT 'Default constraint kaldirildi: ' + @ConstraintName;
    END

    ALTER TABLE [dbo].[Users] DROP COLUMN [Roles];
    PRINT 'Users.Roles kolonu drop edildi.';
END
ELSE
BEGIN
    PRINT 'Users.Roles kolonu zaten yok (migration idempotent).';
END
GO
