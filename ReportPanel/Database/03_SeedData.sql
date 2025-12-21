-- BKM Report Panel - Seed Data (PortalHUB)

USE [PortalHUB];
GO

-- DataSources
MERGE INTO [dbo].[DataSources] AS target
USING (VALUES
    ('MAIN_STG', 'Ana Veritabani (Staging)', 'Server=staging-server\\SQLEXPRESS;Database=MainDB_Staging;User Id=staging_user;Password=CHANGE_ME;TrustServerCertificate=true;', 1),
    ('IK_STG', 'Insan Kaynaklari (Staging)', 'Server=staging-server\\SQLEXPRESS;Database=IKDB_Staging;User Id=staging_user;Password=CHANGE_ME;TrustServerCertificate=true;', 1),
    ('MALI_STG', 'Mali Isler (Staging)', 'Server=staging-server\\SQLEXPRESS;Database=MaliDB_Staging;User Id=staging_user;Password=CHANGE_ME;TrustServerCertificate=true;', 1),
    ('TEST_STG', 'Test Veritabani (Staging)', 'Server=staging-server\\SQLEXPRESS;Database=TestDB_Staging;User Id=staging_user;Password=CHANGE_ME;TrustServerCertificate=true;', 1)
) AS source (DataSourceKey, Title, ConnString, IsActive)
ON target.DataSourceKey = source.DataSourceKey
WHEN NOT MATCHED BY TARGET THEN
    INSERT (DataSourceKey, Title, ConnString, IsActive)
    VALUES (source.DataSourceKey, source.Title, source.ConnString, source.IsActive);
GO

-- ReportCatalog
MERGE INTO [dbo].[ReportCatalog] AS target
USING (VALUES
    ('Personel Listesi (Staging)', 'Staging ortaminda tum personellerin listesi', 'IK_STG', 'sp_PersonelListesi_Staging', '{}', 'admin,ik', 1),
    ('Maas Raporu (Staging)', 'Staging ortaminda aylik maas raporu', 'MALI_STG', 'sp_MaasRaporu_Staging', '{"ay": "int", "yil": "int"}', 'admin,mali', 1),
    ('Genel Istatistik (Staging)', 'Staging ortaminda genel sistem istatistikleri', 'MAIN_STG', 'sp_GenelIstatistik_Staging', '{}', 'admin', 1),
    ('Departman Raporu (Staging)', 'Staging ortaminda departman bazli raporlar', 'IK_STG', 'sp_DepartmanRaporu_Staging', '{"departman_id": "int"}', 'admin,ik,yonetim', 1),
    ('Gelir-Gider Analizi (Staging)', 'Staging ortaminda mali analiz raporu', 'MALI_STG', 'sp_GelirGiderAnalizi_Staging', '{"baslangic_tarih": "date", "bitis_tarih": "date"}', 'admin,mali,yonetim', 1),
    ('Sistem Performansi (Staging)', 'Staging ortaminda sistem performans metrikleri', 'MAIN_STG', 'sp_SistemPerformansi_Staging', '{}', 'admin', 1)
) AS source (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive)
ON target.Title = source.Title AND target.DataSourceKey = source.DataSourceKey
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive)
    VALUES (source.Title, source.Description, source.DataSourceKey, source.ProcName, source.ParamSchemaJson, source.AllowedRoles, source.IsActive);
GO

-- Users
MERGE INTO [dbo].[Users] AS target
USING (VALUES
    ('admin_staging', 'PBKDF2$100000$ZwUdULSNAcmfixdnxVg6Zg==$fqcUJwpCBMezKDoZ60050EGvKyHh0DnFpQIigNBSWg8=', 'Staging Admin', 'admin@staging.bkm.com', 'admin', 1),
    ('ik_staging', 'PBKDF2$100000$4sdIBFsFMo9EgCMCUGBkCg==$TPE3RuG2zWOWYhdJdkKMFi+Nj7qkmQX/szfruffawmk=', 'Staging IK', 'ik@staging.bkm.com', 'ik', 1),
    ('mali_staging', 'PBKDF2$100000$X/5deb14Cl0nENXyYHFz5w==$ZXCfNzAVwf3kENtK1s/xeGuAQsr5x2HnLE0riRLVQy4=', 'Staging Mali', 'mali@staging.bkm.com', 'mali', 1),
    ('user_staging', 'PBKDF2$100000$vzeoqV+YtXNFGxsVqilqyQ==$EN6fnkfbRLYeL4PE4zsSs3ycubCViQTi8kJY+im48Sk=', 'Staging User', 'user@staging.bkm.com', 'user', 1),
    ('test_staging', 'PBKDF2$100000$R8Xyj0VCRcSVfhxLM5nxEQ==$iBZVhefKhFqxiImqazN7UrKMz9sS/3nF4LCyvZoEgX8=', 'Staging Test', 'test@staging.bkm.com', 'admin,ik,mali', 1)
) AS source (Username, PasswordHash, FullName, Email, Roles, IsActive)
ON target.Username = source.Username
WHEN MATCHED THEN
    UPDATE SET
        PasswordHash = source.PasswordHash,
        FullName = source.FullName,
        Email = source.Email,
        Roles = source.Roles,
        IsActive = source.IsActive
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Username, PasswordHash, FullName, Email, Roles, IsActive)
    VALUES (source.Username, source.PasswordHash, source.FullName, source.Email, source.Roles, source.IsActive);
GO

-- ReportRunLog (sample)
IF NOT EXISTS (SELECT 1 FROM [dbo].[ReportRunLog] WHERE Username = 'admin_staging')
BEGIN
    DECLARE @ReportPersonelId INT = (SELECT TOP 1 ReportId FROM [dbo].[ReportCatalog] WHERE Title = 'Personel Listesi (Staging)');
    DECLARE @ReportMaasId INT = (SELECT TOP 1 ReportId FROM [dbo].[ReportCatalog] WHERE Title = 'Maas Raporu (Staging)');
    DECLARE @ReportGenelId INT = (SELECT TOP 1 ReportId FROM [dbo].[ReportCatalog] WHERE Title = 'Genel Istatistik (Staging)');
    DECLARE @ReportDepartmanId INT = (SELECT TOP 1 ReportId FROM [dbo].[ReportCatalog] WHERE Title = 'Departman Raporu (Staging)');

    IF @ReportPersonelId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[ReportRunLog]
            (RunId, Username, ReportId, DataSourceKey, ParamsJson, RunAt, DurationMs, ResultRowCount, IsSuccess, ErrorMessage)
        VALUES
            (NEWID(), 'admin_staging', @ReportPersonelId, 'IK_STG', '{}', DATEADD(HOUR, -2, GETDATE()), 1250, 45, 1, NULL),
            (NEWID(), 'ik_staging', @ReportPersonelId, 'IK_STG', '{}', DATEADD(HOUR, -1, GETDATE()), 980, 45, 1, NULL);
    END

    IF @ReportMaasId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[ReportRunLog]
            (RunId, Username, ReportId, DataSourceKey, ParamsJson, RunAt, DurationMs, ResultRowCount, IsSuccess, ErrorMessage)
        VALUES
            (NEWID(), 'mali_staging', @ReportMaasId, 'MALI_STG', '{"ay": 12, "yil": 2024}', DATEADD(MINUTE, -30, GETDATE()), 2100, 156, 1, NULL);
    END

    IF @ReportGenelId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[ReportRunLog]
            (RunId, Username, ReportId, DataSourceKey, ParamsJson, RunAt, DurationMs, ResultRowCount, IsSuccess, ErrorMessage)
        VALUES
            (NEWID(), 'admin_staging', @ReportGenelId, 'MAIN_STG', '{}', DATEADD(MINUTE, -15, GETDATE()), 750, 8, 1, NULL);
    END

    IF @ReportDepartmanId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[ReportRunLog]
            (RunId, Username, ReportId, DataSourceKey, ParamsJson, RunAt, DurationMs, ResultRowCount, IsSuccess, ErrorMessage)
        VALUES
            (NEWID(), 'test_staging', @ReportDepartmanId, 'IK_STG', '{"departman_id": 1}', DATEADD(MINUTE, -5, GETDATE()), 0, 0, 0, 'Stored procedure not found: sp_DepartmanRaporu_Staging');
    END
END
GO

PRINT 'Seed verileri basariyla eklendi!';
DECLARE @DataSourceCount INT = (SELECT COUNT(*) FROM [dbo].[DataSources]);
DECLARE @ReportCount INT = (SELECT COUNT(*) FROM [dbo].[ReportCatalog]);
DECLARE @UserCount INT = (SELECT COUNT(*) FROM [dbo].[Users]);
DECLARE @LogCount INT = (SELECT COUNT(*) FROM [dbo].[ReportRunLog]);
PRINT 'Toplam Veri Kaynagi: ' + CAST(@DataSourceCount AS VARCHAR(10));
PRINT 'Toplam Rapor: ' + CAST(@ReportCount AS VARCHAR(10));
PRINT 'Toplam Kullanici: ' + CAST(@UserCount AS VARCHAR(10));
PRINT 'Toplam Log Kaydi: ' + CAST(@LogCount AS VARCHAR(10));
