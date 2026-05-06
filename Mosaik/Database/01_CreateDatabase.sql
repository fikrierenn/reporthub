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

-- Mosaik veritabanını oluştur
CREATE DATABASE Mosaik
ON
( NAME = 'Mosaik_Data',
  FILENAME = 'C:\Database\Mosaik.mdf',
  SIZE = 100MB,
  MAXSIZE = 1GB,
  FILEGROWTH = 10MB )
LOG ON
( NAME = 'Mosaik_Log',
  FILENAME = 'C:\Database\Mosaik.ldf',
  SIZE = 10MB,
  MAXSIZE = 100MB,
  FILEGROWTH = 5MB );
GO

-- Mosaik veritabanını kullan
USE Mosaik;
GO

PRINT 'Mosaik veritabanı başarıyla oluşturuldu!';
