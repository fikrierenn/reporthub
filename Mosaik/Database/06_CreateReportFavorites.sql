-- Create ReportFavorites table (favori raporlar)
IF OBJECT_ID('dbo.ReportFavorites', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportFavorites] (
        [UserId] INT NOT NULL,
        [ReportId] INT NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL,
        CONSTRAINT [PK_ReportFavorites] PRIMARY KEY CLUSTERED ([UserId] ASC, [ReportId] ASC)
    );

    ALTER TABLE [dbo].[ReportFavorites] ADD DEFAULT (GETDATE()) FOR [CreatedAt];

    ALTER TABLE [dbo].[ReportFavorites] WITH CHECK ADD CONSTRAINT [FK_ReportFavorites_Users_UserId]
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[ReportFavorites] CHECK CONSTRAINT [FK_ReportFavorites_Users_UserId];

    ALTER TABLE [dbo].[ReportFavorites] WITH CHECK ADD CONSTRAINT [FK_ReportFavorites_ReportCatalog_ReportId]
    FOREIGN KEY ([ReportId]) REFERENCES [dbo].[ReportCatalog] ([ReportId]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[ReportFavorites] CHECK CONSTRAINT [FK_ReportFavorites_ReportCatalog_ReportId];
END
