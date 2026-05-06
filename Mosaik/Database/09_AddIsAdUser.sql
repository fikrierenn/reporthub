-- Add IsAdUser column for Windows AD logins

IF COL_LENGTH('dbo.Users', 'IsAdUser') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [IsAdUser] BIT NOT NULL
            CONSTRAINT [DF_Users_IsAdUser] DEFAULT ((0));
END
GO
