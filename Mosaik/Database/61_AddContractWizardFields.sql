-- Migration 61: Contracts tablosuna AI wizard'ın çıkardığı 5 yeni alan
-- (ContractValue, Currency, GoverningLaw, AutoRenewal, KvkkInvolved).
-- WizardExtractionResult zaten dolduruyor; form alanları olmadığı için
-- veriler kullanıcıya görünmeden kayboluyordu (Plan 33 audit bulgusu).

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Contracts') AND name = 'ContractValue')
BEGIN
    ALTER TABLE dbo.Contracts ADD ContractValue DECIMAL(18, 2) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Contracts') AND name = 'Currency')
BEGIN
    ALTER TABLE dbo.Contracts ADD Currency NVARCHAR(8) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Contracts') AND name = 'GoverningLaw')
BEGIN
    ALTER TABLE dbo.Contracts ADD GoverningLaw NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Contracts') AND name = 'AutoRenewal')
BEGIN
    ALTER TABLE dbo.Contracts ADD AutoRenewal BIT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Contracts') AND name = 'KvkkInvolved')
BEGIN
    ALTER TABLE dbo.Contracts ADD KvkkInvolved BIT NULL;
END
GO
