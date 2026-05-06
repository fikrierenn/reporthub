-- Create relational tables and migrate roles/categories from legacy CSV columns

-- Roles
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Roles_IsActive] DEFAULT ((1)),
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Roles_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
        CONSTRAINT [UQ_Roles_Name] UNIQUE NONCLUSTERED ([Name] ASC)
    );
END

-- UserRoles
IF OBJECT_ID('dbo.UserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [UserId] INT NOT NULL,
        [RoleId] INT NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_UserRoles_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC)
    );

    ALTER TABLE [dbo].[UserRoles] WITH CHECK ADD CONSTRAINT [FK_UserRoles_Users_UserId]
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[UserRoles] WITH CHECK ADD CONSTRAINT [FK_UserRoles_Roles_RoleId]
    FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE;
END

-- ReportCategories
IF OBJECT_ID('dbo.ReportCategories', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportCategories] (
        [CategoryId] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(300) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_ReportCategories_IsActive] DEFAULT ((1)),
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_ReportCategories_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_ReportCategories] PRIMARY KEY CLUSTERED ([CategoryId] ASC),
        CONSTRAINT [UQ_ReportCategories_Name] UNIQUE NONCLUSTERED ([Name] ASC)
    );
END

-- ReportCategoryLinks
IF OBJECT_ID('dbo.ReportCategoryLinks', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportCategoryLinks] (
        [ReportId] INT NOT NULL,
        [CategoryId] INT NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_ReportCategoryLinks_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_ReportCategoryLinks] PRIMARY KEY CLUSTERED ([ReportId] ASC, [CategoryId] ASC)
    );

    ALTER TABLE [dbo].[ReportCategoryLinks] WITH CHECK ADD CONSTRAINT [FK_ReportCategoryLinks_ReportCatalog_ReportId]
    FOREIGN KEY ([ReportId]) REFERENCES [dbo].[ReportCatalog] ([ReportId]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[ReportCategoryLinks] WITH CHECK ADD CONSTRAINT [FK_ReportCategoryLinks_ReportCategories_CategoryId]
    FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[ReportCategories] ([CategoryId]) ON DELETE CASCADE;
END

-- ReportAllowedRoles
IF OBJECT_ID('dbo.ReportAllowedRoles', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportAllowedRoles] (
        [ReportId] INT NOT NULL,
        [RoleId] INT NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_ReportAllowedRoles_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_ReportAllowedRoles] PRIMARY KEY CLUSTERED ([ReportId] ASC, [RoleId] ASC)
    );

    ALTER TABLE [dbo].[ReportAllowedRoles] WITH CHECK ADD CONSTRAINT [FK_ReportAllowedRoles_ReportCatalog_ReportId]
    FOREIGN KEY ([ReportId]) REFERENCES [dbo].[ReportCatalog] ([ReportId]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[ReportAllowedRoles] WITH CHECK ADD CONSTRAINT [FK_ReportAllowedRoles_Roles_RoleId]
    FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE;
END

-- Seed Roles from existing CSVs
IF COL_LENGTH('dbo.Users', 'Roles') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[Roles] ([Name], [IsActive], [CreatedAt])
    SELECT DISTINCT LTRIM(RTRIM(value)) AS Name, 1, GETDATE()
    FROM [dbo].[Users]
    CROSS APPLY string_split([Roles], ',')
    WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM [dbo].[Roles] r WHERE r.[Name] = LTRIM(RTRIM(value)));
END

IF COL_LENGTH('dbo.ReportCatalog', 'AllowedRoles') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[Roles] ([Name], [IsActive], [CreatedAt])
    SELECT DISTINCT LTRIM(RTRIM(value)) AS Name, 1, GETDATE()
    FROM [dbo].[ReportCatalog]
    CROSS APPLY string_split([AllowedRoles], ',')
    WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM [dbo].[Roles] r WHERE r.[Name] = LTRIM(RTRIM(value)));
END

-- UserRoles from Users.Roles
IF COL_LENGTH('dbo.Users', 'Roles') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt])
    SELECT u.[UserId], r.[RoleId], GETDATE()
    FROM [dbo].[Users] u
    CROSS APPLY string_split(u.[Roles], ',') s
    JOIN [dbo].[Roles] r ON r.[Name] = LTRIM(RTRIM(s.value))
    WHERE NULLIF(LTRIM(RTRIM(s.value)), '') IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[UserRoles] ur
          WHERE ur.[UserId] = u.[UserId] AND ur.[RoleId] = r.[RoleId]
      );
END

-- ReportAllowedRoles from ReportCatalog.AllowedRoles
IF COL_LENGTH('dbo.ReportCatalog', 'AllowedRoles') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[ReportAllowedRoles] ([ReportId], [RoleId], [CreatedAt])
    SELECT r.[ReportId], rl.[RoleId], GETDATE()
    FROM [dbo].[ReportCatalog] r
    CROSS APPLY string_split(r.[AllowedRoles], ',') s
    JOIN [dbo].[Roles] rl ON rl.[Name] = LTRIM(RTRIM(s.value))
    WHERE NULLIF(LTRIM(RTRIM(s.value)), '') IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[ReportAllowedRoles] ar
          WHERE ar.[ReportId] = r.[ReportId] AND ar.[RoleId] = rl.[RoleId]
      );
END

-- Report categories from legacy column if exists
IF COL_LENGTH('dbo.ReportCatalog', 'Category') IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
    INSERT INTO [dbo].[ReportCategories] ([Name], [IsActive], [CreatedAt])
    SELECT DISTINCT LTRIM(RTRIM([Category])) AS Name, 1, GETDATE()
    FROM [dbo].[ReportCatalog]
    WHERE NULLIF(LTRIM(RTRIM([Category])), '''') IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM [dbo].[ReportCategories] c WHERE c.[Name] = LTRIM(RTRIM([Category])));

    INSERT INTO [dbo].[ReportCategoryLinks] ([ReportId], [CategoryId], [CreatedAt])
    SELECT r.[ReportId], c.[CategoryId], GETDATE()
    FROM [dbo].[ReportCatalog] r
    JOIN [dbo].[ReportCategories] c ON c.[Name] = LTRIM(RTRIM(r.[Category]))
    WHERE NULLIF(LTRIM(RTRIM(r.[Category])), '''') IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[ReportCategoryLinks] rc
          WHERE rc.[ReportId] = r.[ReportId] AND rc.[CategoryId] = c.[CategoryId]
      );';

    EXEC sys.sp_executesql @sql;
END
