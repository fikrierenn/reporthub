-- 21_AddSubeCanonical.sql
-- Plan 07 Faz 5b — canonical Sube master + SubeMapping (per-DataSource external code).
--
-- Mimari:
-- - Sube (master): sirketin canonical sube listesi (5 satir, PDKS vrd.SubeListe seed).
-- - SubeMapping: her DataSource'a sube'nin o sistemde sahip oldugu external code'u tutar.
--   (SubeId, DataSourceKey, ExternalCode); UNIQUE (SubeId, DataSourceKey) + (DataSourceKey, ExternalCode).
-- - UserDataFilterInjector translate: SP parametresine yazarken DataSourceKey'e gore
--   SubeMapping lookup'i ile ExternalCode'a cevirir. Mapping yoksa o deger sessizce drop edilir
--   (kullanici karari, 4 Mayis 2026).
--
-- Seed strateji:
-- - Canonical Sube: PDKS vrd.SubeListe'den 5 sube import (GENEL MUDURLUK, FSM, FSM KAFE, HEYKEL, IST. YOLU).
-- - SubeMapping PDKS: her sube icin direct mapping (ExternalCode = PDKS'teki SubeNo).
-- - SubeMapping DER: BOS — Faz 6 Admin UI'da kullanici manuel eslestirir (DerinSIS posMagaza
--   eslesmesi otomatik isim-bazli yapilamadi, "FSM" vs "FSM Mğz" vs "ÖZLÜCE Mğz" gibi farklar var).
-- - SubeMapping IK: BOS (kullanici karari — IK Zirve mapping'i sonradan).
--
-- Plan: plans/07-yetki-filter-revizyon.md (Faz 5b ek)
-- ROLLBACK: 21_AddSubeCanonical_rollback.sql (DROP TABLE SubeMapping, Sube).

USE [PortalHUB];
GO

-- 1. Sube (canonical master)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Sube')
BEGIN
    CREATE TABLE [dbo].[Sube] (
        SubeId          INT IDENTITY(1,1) NOT NULL,
        SubeAd          NVARCHAR(100) NOT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Sube_IsActive DEFAULT (1),
        DisplayOrder    INT NOT NULL CONSTRAINT DF_Sube_DisplayOrder DEFAULT (0),
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_Sube_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt       DATETIME2 NULL,

        CONSTRAINT PK_Sube PRIMARY KEY CLUSTERED (SubeId),
        CONSTRAINT UQ_Sube_Ad UNIQUE (SubeAd)
    );

    CREATE INDEX IX_Sube_IsActive ON [dbo].[Sube] (IsActive, DisplayOrder);
    PRINT 'Sube tablosu olusturuldu.';
END
ELSE
BEGIN
    PRINT 'Sube tablosu zaten var (migration idempotent).';
END
GO

-- 2. SubeMapping (per-DataSource external code)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SubeMapping')
BEGIN
    CREATE TABLE [dbo].[SubeMapping] (
        MappingId       INT IDENTITY(1,1) NOT NULL,
        SubeId          INT NOT NULL,
        DataSourceKey   NVARCHAR(50) NOT NULL,
        ExternalCode    NVARCHAR(50) NOT NULL,
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_SubeMapping_CreatedAt DEFAULT (GETDATE()),

        CONSTRAINT PK_SubeMapping PRIMARY KEY CLUSTERED (MappingId),
        CONSTRAINT UQ_SubeMapping_Sube_DS UNIQUE (SubeId, DataSourceKey),
        CONSTRAINT UQ_SubeMapping_DS_ExtCode UNIQUE (DataSourceKey, ExternalCode),
        CONSTRAINT FK_SubeMapping_Sube FOREIGN KEY (SubeId)
            REFERENCES [dbo].[Sube] (SubeId) ON DELETE CASCADE,
        CONSTRAINT FK_SubeMapping_DataSource FOREIGN KEY (DataSourceKey)
            REFERENCES [dbo].[DataSources] (DataSourceKey)
    );

    CREATE INDEX IX_SubeMapping_DS ON [dbo].[SubeMapping] (DataSourceKey, ExternalCode);
    PRINT 'SubeMapping tablosu olusturuldu.';
END
ELSE
BEGIN
    PRINT 'SubeMapping tablosu zaten var (migration idempotent).';
END
GO

-- 3. Canonical Sube seed (PDKS vrd.SubeListe'den)
-- PDKS suzgec: hepsi alinir, GENEL MUDURLUK fiziksel sube degil ama listede tutulur (yetki amacli).
INSERT INTO dbo.Sube (SubeAd, IsActive, DisplayOrder)
SELECT N'GENEL MÜDÜRLÜK', 1, 100
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sube WHERE SubeAd = N'GENEL MÜDÜRLÜK');

INSERT INTO dbo.Sube (SubeAd, IsActive, DisplayOrder)
SELECT N'FSM', 1, 10
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sube WHERE SubeAd = N'FSM');

INSERT INTO dbo.Sube (SubeAd, IsActive, DisplayOrder)
SELECT N'FSM KAFE', 1, 11
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sube WHERE SubeAd = N'FSM KAFE');

INSERT INTO dbo.Sube (SubeAd, IsActive, DisplayOrder)
SELECT N'HEYKEL', 1, 20
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sube WHERE SubeAd = N'HEYKEL');

INSERT INTO dbo.Sube (SubeAd, IsActive, DisplayOrder)
SELECT N'İST. YOLU', 1, 30
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sube WHERE SubeAd = N'İST. YOLU');

PRINT 'Canonical Sube seed: 5 sube (PDKS).';
GO

-- 4. SubeMapping seed — PDKS direct mapping (SubeNo aynen ExternalCode olarak).
INSERT INTO dbo.SubeMapping (SubeId, DataSourceKey, ExternalCode)
SELECT s.SubeId, N'PDKS', N'1'
FROM dbo.Sube s WHERE s.SubeAd = N'GENEL MÜDÜRLÜK'
  AND NOT EXISTS (SELECT 1 FROM dbo.SubeMapping m WHERE m.SubeId = s.SubeId AND m.DataSourceKey = N'PDKS');

INSERT INTO dbo.SubeMapping (SubeId, DataSourceKey, ExternalCode)
SELECT s.SubeId, N'PDKS', N'2'
FROM dbo.Sube s WHERE s.SubeAd = N'FSM'
  AND NOT EXISTS (SELECT 1 FROM dbo.SubeMapping m WHERE m.SubeId = s.SubeId AND m.DataSourceKey = N'PDKS');

INSERT INTO dbo.SubeMapping (SubeId, DataSourceKey, ExternalCode)
SELECT s.SubeId, N'PDKS', N'3'
FROM dbo.Sube s WHERE s.SubeAd = N'FSM KAFE'
  AND NOT EXISTS (SELECT 1 FROM dbo.SubeMapping m WHERE m.SubeId = s.SubeId AND m.DataSourceKey = N'PDKS');

INSERT INTO dbo.SubeMapping (SubeId, DataSourceKey, ExternalCode)
SELECT s.SubeId, N'PDKS', N'4'
FROM dbo.Sube s WHERE s.SubeAd = N'HEYKEL'
  AND NOT EXISTS (SELECT 1 FROM dbo.SubeMapping m WHERE m.SubeId = s.SubeId AND m.DataSourceKey = N'PDKS');

INSERT INTO dbo.SubeMapping (SubeId, DataSourceKey, ExternalCode)
SELECT s.SubeId, N'PDKS', N'5'
FROM dbo.Sube s WHERE s.SubeAd = N'İST. YOLU'
  AND NOT EXISTS (SELECT 1 FROM dbo.SubeMapping m WHERE m.SubeId = s.SubeId AND m.DataSourceKey = N'PDKS');

PRINT 'SubeMapping PDKS seed tamamlandi.';
GO

-- 5. FilterDefinition.sube OptionsQuery'sini canonical Sube'ye yonlendir
-- Faz 1'de DataSourceKey=PDKS + OptionsQuery=vrd.SubeListe idi; artik canonical (DataSourceKey=NULL,
-- Scope='spInjection' kalir ama OptionsQuery NULL — UserDataFilterInjector translate sirasinda
-- bilir + FilterOptionsService canonical native source kullanir Faz 5b sonrasi).
-- Ancak FilterOptionsService'in CHECK CONSTRAINT'leri bozulmasin diye OptionsQuery NULL yerine
-- canonical Sube SELECT'i konur — FilterOptionsService SELECT exec et, ama bu kez kendi PortalHUB'ta.
UPDATE dbo.FilterDefinition
SET DataSourceKey = NULL,
    OptionsQuery = NULL,
    UpdatedAt = GETDATE()
WHERE FilterKey = N'sube';

PRINT 'FilterDefinition.sube canonical native source moduna gecti (DataSourceKey=NULL, OptionsQuery=NULL).';
GO

-- 6. Dogrulama
SELECT 'Sube' AS Tablo, COUNT(*) AS Adet FROM dbo.Sube
UNION ALL
SELECT 'SubeMapping_PDKS', COUNT(*) FROM dbo.SubeMapping WHERE DataSourceKey = N'PDKS'
UNION ALL
SELECT 'SubeMapping_DER', COUNT(*) FROM dbo.SubeMapping WHERE DataSourceKey = N'DER'
UNION ALL
SELECT 'SubeMapping_IK', COUNT(*) FROM dbo.SubeMapping WHERE DataSourceKey = N'IK';
GO
