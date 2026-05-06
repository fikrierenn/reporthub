-- 31_SeedFilterDefinitions.sql
-- FilterDefinition kayıtları — PDKS (sube) ve DER (sube, urunKategori)
-- IsActive=0: admin UI'da görünür, henüz zorunlu değil.
-- Aktive etmeden önce admin GUI'den her kullanıcıya UserDataFilter ataması yapılmalı.

-- PDKS — şube filtresi
IF NOT EXISTS (SELECT 1 FROM dbo.FilterDefinition WHERE FilterKey = N'sube' AND DataSourceKey = N'PDKS')
    INSERT INTO dbo.FilterDefinition
        (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder)
    VALUES (
        N'sube',
        N'Şube',
        N'spInjection',
        N'PDKS',
        N'SELECT CAST(SubeNo AS NVARCHAR(20)) AS Value, SubeAd AS Label FROM vrd.SubeListe ORDER BY SubeAd',
        0,
        1
    );
GO

-- DER — şube (mağaza) filtresi
IF NOT EXISTS (SELECT 1 FROM dbo.FilterDefinition WHERE FilterKey = N'sube' AND DataSourceKey = N'DER')
    INSERT INTO dbo.FilterDefinition
        (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder)
    VALUES (
        N'sube',
        N'Mağaza',
        N'spInjection',
        N'DER',
        N'SELECT CAST(mekanID AS NVARCHAR(20)) AS Value, mekanAd AS Label FROM posMagaza WHERE mekanTip = 0 ORDER BY mekanAd',
        0,
        1
    );
GO

-- DER — ürün kategorisi filtresi
IF NOT EXISTS (SELECT 1 FROM dbo.FilterDefinition WHERE FilterKey = N'urunKategori' AND DataSourceKey = N'DER')
    INSERT INTO dbo.FilterDefinition
        (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder)
    VALUES (
        N'urunKategori',
        N'Ürün Kategorisi',
        N'spInjection',
        N'DER',
        N'SELECT CAST(ktgrID AS NVARCHAR(20)) AS Value, ktgrAd AS Label FROM urnKtgr2 WHERE ktgrID NOT IN (11) ORDER BY ktgrAd',
        0,
        2
    );
GO
