-- Plan 41 rev 2 (2026-06-29 duzeltme, 2026-07-01 plan govdesine islendi) --
-- FormSubmission.FormVersionId ZORUNLU FK -- submission hangi sema versiyonuna
-- gore render/validate edildiyse o versiyona baglanir (geriye uyumluluk garantisi).
-- FormSubmissions bos (Faz 1+ render/submit pipeline henuz yazilmadi) -- NOT NULL guvenli.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FormSubmissions') AND name = 'FormVersionId')
BEGIN
    ALTER TABLE dbo.FormSubmissions ADD FormVersionId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FormSubmissions_FormDefinitionVersions')
BEGIN
    ALTER TABLE dbo.FormSubmissions
        ADD CONSTRAINT FK_FormSubmissions_FormDefinitionVersions
        FOREIGN KEY (FormVersionId) REFERENCES dbo.FormDefinitionVersions(Id);
END
GO

-- Tablo bos oldugu icin NOT NULL'a gecebiliriz (ileride veri varsa bu adim atlanir).
IF (SELECT COUNT(*) FROM dbo.FormSubmissions) = 0
BEGIN
    ALTER TABLE dbo.FormSubmissions ALTER COLUMN FormVersionId INT NOT NULL;
END
GO

PRINT 'Forms rev2: FormSubmissions.FormVersionId ZORUNLU FK (74) hazir.';
