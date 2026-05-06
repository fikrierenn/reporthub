-- Mosaik - Database Setup
-- Dev/Staging ortamı için veritabanı oluşturma scripti

USE master;
GO

-- Eski PortalHUB (Mosaik öncesi adı) varsa sil (rebrand için)
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = 'PortalHUB')
BEGIN
    ALTER DATABASE PortalHUB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PortalHUB;
END
GO

-- Mevcut Mosaik varsa sil (sadece dev/staging için!)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'Mosaik')
BEGIN
    ALTER DATABASE Mosaik SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Mosaik;
END
GO

-- Mosaik veritabanını oluştur (SQL Server default data/log path kullanır)
CREATE DATABASE Mosaik;
GO

-- Mosaik veritabanını kullan
USE Mosaik;
GO

PRINT 'Mosaik veritabanı başarıyla oluşturuldu!';
