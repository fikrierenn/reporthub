-- Migration 42: Firma tablosu + seed (ADR-012 — firma = güvenlik sınırı)
-- Plan 25 Faz A

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Firmas' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Firmas (
        FirmaId   INT          NOT NULL IDENTITY(1,1),
        Kod       NVARCHAR(50) NOT NULL,
        Ad        NVARCHAR(100) NOT NULL,
        IsActive  BIT          NOT NULL DEFAULT 1,
        CONSTRAINT PK_Firmas PRIMARY KEY (FirmaId),
        CONSTRAINT UQ_Firmas_Kod UNIQUE (Kod)
    );

    -- Seed: BKMKİTAP grubu — 3 tüzel kişilik
    -- IDENTITY_INSERT gerekmiyor; IDENTITY(1,1) ile 1,2,3 otomatik atanır.
    -- Sıra önemli: BKM_GENEL=1, BURSA_KÜLTÜR_MERKEZİ=2, ASİYE_BİNGÖLBALİ=3
    INSERT INTO dbo.Firmas (Kod, Ad, IsActive) VALUES
        (N'BKM_GENEL',            N'BKMKİTAP',         1),
        (N'BURSA_KÜLTÜR_MERKEZİ', N'Bursa Kültür',      1),
        (N'ASİYE_BİNGÖLBALİ',    N'Asiye Bingölbali', 1);

    PRINT 'Firmas tablosu oluşturuldu ve seed verileri eklendi.';
END
ELSE
    PRINT 'Firmas tablosu zaten var — atlandı.';

-- User tablosuna FirmaId kolonu ekle (nullable — mevcut kullanıcılar için)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'FirmaId')
BEGIN
    ALTER TABLE dbo.Users
        ADD FirmaId INT NULL
        CONSTRAINT FK_Users_Firmas FOREIGN KEY (FirmaId) REFERENCES dbo.Firmas(FirmaId);

    -- Mevcut admin/sistem kullanıcılarını BKM_GENEL'e ata (isteğe bağlı)
    -- UPDATE dbo.Users SET FirmaId = 1 WHERE Username = 'admin';

    PRINT 'Users.FirmaId kolonu eklendi.';
END
ELSE
    PRINT 'Users.FirmaId zaten var — atlandı.';
