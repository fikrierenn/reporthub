-- BKM Report Panel - Staging Database Setup
-- Staging ortamı için veritabanı oluşturma scripti

USE master;
GO

-- Eğer veritabanı varsa sil (sadece staging için!)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ReportPanel_Staging')
BEGIN
    ALTER DATABASE ReportPanel_Staging SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ReportPanel_Staging;
END
GO

-- Staging veritabanını oluştur
CREATE DATABASE ReportPanel_Staging
ON 
( NAME = 'ReportPanel_Staging_Data',
  FILENAME = 'C:\Database\ReportPanel_Staging.mdf',
  SIZE = 100MB,
  MAXSIZE = 1GB,
  FILEGROWTH = 10MB )
LOG ON 
( NAME = 'ReportPanel_Staging_Log',
  FILENAME = 'C:\Database\ReportPanel_Staging.ldf',
  SIZE = 10MB,
  MAXSIZE = 100MB,
  FILEGROWTH = 5MB );
GO

-- Staging veritabanını kullan
USE ReportPanel_Staging;
GO

PRINT 'Staging veritabanı başarıyla oluşturuldu!';