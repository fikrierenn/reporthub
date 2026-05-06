-- BKM Report Panel - Tables (PortalHUB)

USE [PortalHUB];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- DataSources
CREATE TABLE [dbo].[DataSources] (
    [DataSourceKey] NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(100) NOT NULL,
    [ConnString] NVARCHAR(1000) NOT NULL,
    [IsActive] BIT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_DataSources] PRIMARY KEY CLUSTERED ([DataSourceKey] ASC)
);
GO

ALTER TABLE [dbo].[DataSources] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

-- ReportCatalog
CREATE TABLE [dbo].[ReportCatalog] (
    [ReportId] INT IDENTITY(1,1) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [Category] NVARCHAR(100) NULL,
    [DataSourceKey] NVARCHAR(50) NOT NULL,
    [ProcName] NVARCHAR(200) NOT NULL,
    [ParamSchemaJson] NVARCHAR(MAX) NOT NULL,
    [AllowedRoles] NVARCHAR(200) NOT NULL,
    [IsActive] BIT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_ReportCatalog] PRIMARY KEY CLUSTERED ([ReportId] ASC)
);
GO

ALTER TABLE [dbo].[ReportCatalog] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

ALTER TABLE [dbo].[ReportCatalog] WITH CHECK ADD CONSTRAINT [FK_ReportCatalog_DataSources_DataSourceKey]
FOREIGN KEY ([DataSourceKey]) REFERENCES [dbo].[DataSources] ([DataSourceKey]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReportCatalog] CHECK CONSTRAINT [FK_ReportCatalog_DataSources_DataSourceKey];
GO

-- ReportRunLog
CREATE TABLE [dbo].[ReportRunLog] (
    [RunId] UNIQUEIDENTIFIER NOT NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [ReportId] INT NOT NULL,
    [DataSourceKey] NVARCHAR(50) NOT NULL,
    [ParamsJson] NVARCHAR(MAX) NOT NULL,
    [RunAt] DATETIME2(7) NOT NULL,
    [DurationMs] INT NULL,
    [ResultRowCount] INT NULL,
    [IsSuccess] BIT NOT NULL,
    [ErrorMessage] NVARCHAR(1000) NULL,
    CONSTRAINT [PK_ReportRunLog] PRIMARY KEY CLUSTERED ([RunId] ASC)
);
GO

ALTER TABLE [dbo].[ReportRunLog] ADD DEFAULT (GETDATE()) FOR [RunAt];
GO

ALTER TABLE [dbo].[ReportRunLog] WITH CHECK ADD CONSTRAINT [FK_ReportRunLog_ReportCatalog_ReportId]
FOREIGN KEY ([ReportId]) REFERENCES [dbo].[ReportCatalog] ([ReportId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReportRunLog] CHECK CONSTRAINT [FK_ReportRunLog_ReportCatalog_ReportId];
GO

-- AuditLog
CREATE TABLE [dbo].[AuditLog] (
    [AuditId] UNIQUEIDENTIFIER NOT NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [EventType] NVARCHAR(50) NOT NULL,
    [TargetType] NVARCHAR(50) NULL,
    [TargetKey] NVARCHAR(200) NULL,
    [Description] NVARCHAR(500) NULL,
    [OldValuesJson] NVARCHAR(MAX) NULL,
    [NewValuesJson] NVARCHAR(MAX) NULL,
    [IsSuccess] BIT NOT NULL,
    [ErrorMessage] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [ReportId] INT NULL,
    [DataSourceKey] NVARCHAR(50) NULL,
    [ParamsJson] NVARCHAR(MAX) NULL,
    [DurationMs] INT NULL,
    [ResultRowCount] INT NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(300) NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([AuditId] ASC)
);
GO

ALTER TABLE [dbo].[AuditLog] ADD DEFAULT ((1)) FOR [IsSuccess];
GO

ALTER TABLE [dbo].[AuditLog] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

-- Users
CREATE TABLE [dbo].[Users] (
    [UserId] INT IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(50) NOT NULL,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [FullName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(100) NULL,
    [Roles] NVARCHAR(200) NOT NULL,
    [IsAdUser] BIT NOT NULL,
    [IsActive] BIT NOT NULL,
    [LastLoginAt] DATETIME NULL,
    [CreatedAt] DATETIME NOT NULL,
    [UpdatedAt] DATETIME NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [UQ_Users_Username] UNIQUE NONCLUSTERED ([Username] ASC)
);
GO

ALTER TABLE [dbo].[Users] ADD DEFAULT ((1)) FOR [IsActive];
GO

ALTER TABLE [dbo].[Users] ADD DEFAULT ((0)) FOR [IsAdUser];
GO

ALTER TABLE [dbo].[Users] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

ALTER TABLE [dbo].[Users] ADD DEFAULT (GETDATE()) FOR [UpdatedAt];
GO

-- Roles
CREATE TABLE [dbo].[Roles] (
    [RoleId] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(200) NULL,
    [IsActive] BIT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
    CONSTRAINT [UQ_Roles_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);
GO

ALTER TABLE [dbo].[Roles] ADD DEFAULT ((1)) FOR [IsActive];
GO

ALTER TABLE [dbo].[Roles] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

-- UserRoles
CREATE TABLE [dbo].[UserRoles] (
    [UserId] INT NOT NULL,
    [RoleId] INT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC)
);
GO

ALTER TABLE [dbo].[UserRoles] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

ALTER TABLE [dbo].[UserRoles] WITH CHECK ADD CONSTRAINT [FK_UserRoles_Users_UserId]
FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users_UserId];
GO

ALTER TABLE [dbo].[UserRoles] WITH CHECK ADD CONSTRAINT [FK_UserRoles_Roles_RoleId]
FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles_RoleId];
GO

-- ReportCategories
CREATE TABLE [dbo].[ReportCategories] (
    [CategoryId] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(300) NULL,
    [IsActive] BIT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_ReportCategories] PRIMARY KEY CLUSTERED ([CategoryId] ASC),
    CONSTRAINT [UQ_ReportCategories_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);
GO

ALTER TABLE [dbo].[ReportCategories] ADD DEFAULT ((1)) FOR [IsActive];
GO

ALTER TABLE [dbo].[ReportCategories] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

-- ReportCategoryLinks
CREATE TABLE [dbo].[ReportCategoryLinks] (
    [ReportId] INT NOT NULL,
    [CategoryId] INT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_ReportCategoryLinks] PRIMARY KEY CLUSTERED ([ReportId] ASC, [CategoryId] ASC)
);
GO

ALTER TABLE [dbo].[ReportCategoryLinks] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

ALTER TABLE [dbo].[ReportCategoryLinks] WITH CHECK ADD CONSTRAINT [FK_ReportCategoryLinks_ReportCatalog_ReportId]
FOREIGN KEY ([ReportId]) REFERENCES [dbo].[ReportCatalog] ([ReportId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReportCategoryLinks] CHECK CONSTRAINT [FK_ReportCategoryLinks_ReportCatalog_ReportId];
GO

ALTER TABLE [dbo].[ReportCategoryLinks] WITH CHECK ADD CONSTRAINT [FK_ReportCategoryLinks_ReportCategories_CategoryId]
FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[ReportCategories] ([CategoryId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReportCategoryLinks] CHECK CONSTRAINT [FK_ReportCategoryLinks_ReportCategories_CategoryId];
GO

-- ReportAllowedRoles
CREATE TABLE [dbo].[ReportAllowedRoles] (
    [ReportId] INT NOT NULL,
    [RoleId] INT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_ReportAllowedRoles] PRIMARY KEY CLUSTERED ([ReportId] ASC, [RoleId] ASC)
);
GO

ALTER TABLE [dbo].[ReportAllowedRoles] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

ALTER TABLE [dbo].[ReportAllowedRoles] WITH CHECK ADD CONSTRAINT [FK_ReportAllowedRoles_ReportCatalog_ReportId]
FOREIGN KEY ([ReportId]) REFERENCES [dbo].[ReportCatalog] ([ReportId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReportAllowedRoles] CHECK CONSTRAINT [FK_ReportAllowedRoles_ReportCatalog_ReportId];
GO

ALTER TABLE [dbo].[ReportAllowedRoles] WITH CHECK ADD CONSTRAINT [FK_ReportAllowedRoles_Roles_RoleId]
FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReportAllowedRoles] CHECK CONSTRAINT [FK_ReportAllowedRoles_Roles_RoleId];
GO

-- ReportFavorites
CREATE TABLE [dbo].[ReportFavorites] (
    [UserId] INT NOT NULL,
    [ReportId] INT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_ReportFavorites] PRIMARY KEY CLUSTERED ([UserId] ASC, [ReportId] ASC)
);
GO

ALTER TABLE [dbo].[ReportFavorites] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

ALTER TABLE [dbo].[ReportFavorites] WITH CHECK ADD CONSTRAINT [FK_ReportFavorites_Users_UserId]
FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReportFavorites] CHECK CONSTRAINT [FK_ReportFavorites_Users_UserId];
GO

ALTER TABLE [dbo].[ReportFavorites] WITH CHECK ADD CONSTRAINT [FK_ReportFavorites_ReportCatalog_ReportId]
FOREIGN KEY ([ReportId]) REFERENCES [dbo].[ReportCatalog] ([ReportId]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[ReportFavorites] CHECK CONSTRAINT [FK_ReportFavorites_ReportCatalog_ReportId];
GO
