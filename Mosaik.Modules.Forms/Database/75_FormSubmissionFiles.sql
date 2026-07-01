-- Plan 41 Faz 4 -- form eki + imza gorseli disk storage kaydi.
-- Forms modulu Documents'a (ana proje) baglanamaz (ADR-002) -> kendi App_Data/forms/ storage'i.
-- FieldValue.ValueFileId bu tabloya soft-ref FK (NoAction -- coklu cascade yolu engellenir).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FormSubmissionFiles')
BEGIN
    CREATE TABLE dbo.FormSubmissionFiles (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        FormSubmissionId INT NOT NULL,
        FirmaId          INT NOT NULL,
        FieldKey         NVARCHAR(80)  NOT NULL,
        FileName         NVARCHAR(300) NOT NULL,
        DiskPath         NVARCHAR(400) NOT NULL,   -- ContentRoot-goreli
        FileSize         BIGINT NOT NULL,
        MimeType         NVARCHAR(120) NULL,
        CreatedAt        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_FormSubmissionFiles_FormSubmissions
            FOREIGN KEY (FormSubmissionId) REFERENCES dbo.FormSubmissions(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_FormSubmissionFiles_Submission
        ON dbo.FormSubmissionFiles (FormSubmissionId, FieldKey);
END
GO

-- FieldValue.ValueFileId FK -> FormSubmissionFiles (NoAction: dosya Submission cascade'iyle silinir).
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FormSubmissionFieldValues_FormSubmissionFiles')
BEGIN
    ALTER TABLE dbo.FormSubmissionFieldValues
        ADD CONSTRAINT FK_FormSubmissionFieldValues_FormSubmissionFiles
        FOREIGN KEY (ValueFileId) REFERENCES dbo.FormSubmissionFiles(Id);
END
GO
