-- 13_CreateUserDataFilter.sql
-- Satır seviyesi veri filtreleme: kullanıcı bazlı şube/bölüm/kategori kısıtlaması
--
-- Aynı FilterKey içinde birden fazla satır → OR (FSM veya HEYKEL)
-- Farklı FilterKey'ler arası → AND (şube FSM VE bölüm KIRTASİYE)
-- Hiç kaydı olmayan kullanıcı → filtre yok, tümünü görür (GM, admin)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserDataFilters')
BEGIN
    CREATE TABLE UserDataFilters (
        FilterId        INT IDENTITY(1,1) PRIMARY KEY,
        UserId          INT NOT NULL,
        FilterKey       NVARCHAR(50) NOT NULL,      -- sube, bolum, kategori, bolge, maliyet_merkezi...
        FilterValue     NVARCHAR(100) NOT NULL,      -- FSM, KIRTASİYE, Marmara...
        DataSourceKey   NVARCHAR(50) NULL,           -- NULL = tüm veri kaynaklarında geçerli
        ReportId        INT NULL,                    -- NULL = tüm raporlarda geçerli
        CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_UserDataFilter_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserDataFilter_DataSource FOREIGN KEY (DataSourceKey) REFERENCES DataSources(DataSourceKey),
        CONSTRAINT FK_UserDataFilter_Report FOREIGN KEY (ReportId) REFERENCES ReportCatalog(ReportId) ON DELETE SET NULL
    );

    CREATE INDEX IX_UserDataFilter_UserId ON UserDataFilters(UserId);
    CREATE INDEX IX_UserDataFilter_Key ON UserDataFilters(FilterKey);
END
GO
