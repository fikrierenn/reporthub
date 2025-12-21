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

ALTER TABLE [dbo].[Users] ADD DEFAULT (GETDATE()) FOR [CreatedAt];
GO

ALTER TABLE [dbo].[Users] ADD DEFAULT (GETDATE()) FOR [UpdatedAt];
GO
