-- M-03 Faz B (22 Nisan 2026)
-- User.Roles CSV kolonu deprecate yolu — birinci adim: NOT NULL'den NULL'a.
-- UserRole junction tablosu artik tek resmi rol kaynagi (Faz A, commit 2d0c3fd).
-- Bu migration sonrasi yeni user create/update yollari User.Roles'a yazmaz
-- (AdminController'da bos string atamasi da kaldirilabilir, model [Obsolete]
-- ile isaretli).
--
-- Faz C (ileride) kolonu tamamen DROP eder; o zamana kadar legacy okuma yollari
-- (audit history vb.) icin kolon DB'de durur ama NULL olabilir.

USE [PortalHUB];
GO

-- Guvenlik kontrolu: herkesin UserRole junction'da satiri var mi?
-- (Aksi durumda login rol iddiasiz giris yapar — bunu kontrol et ve log'la.)
DECLARE @OrphanCount INT = (
    SELECT COUNT(1)
    FROM [dbo].[Users] u
    LEFT JOIN [dbo].[UserRoles] ur ON ur.UserId = u.UserId
    WHERE ur.UserId IS NULL
      AND u.IsActive = 1
      AND LTRIM(RTRIM(ISNULL(u.Roles, ''))) <> ''
);

IF @OrphanCount > 0
BEGIN
    PRINT CONCAT(
        'UYARI: ', @OrphanCount,
        ' aktif kullanici UserRole tablosunda rol kaydina sahip degil ama User.Roles CSV''de role tanimli. ',
        'Faz A migration (08_MigrateRolesAndCategories.sql) tekrar calistirilmali veya elle senkron yapilmali.'
    );
END
ELSE
BEGIN
    PRINT 'UserRole senkron OK — no orphan active users with CSV-only roles.';
END
GO

-- Kolonu NULL kabul edecek sekilde degistir.
-- (NOT NULL ise: ALTER COLUMN ile NULL yap + DEFAULT constraint yok zaten.)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
      AND name = N'Roles'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE [dbo].[Users]
        ALTER COLUMN [Roles] NVARCHAR(200) NULL;
    PRINT 'Users.Roles kolonu NULL kabul eder hale getirildi.';
END
ELSE
BEGIN
    PRINT 'Users.Roles kolonu zaten NULLABLE (migration idempotent).';
END
GO
