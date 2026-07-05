-- =============================================================================
-- 84 — Org-amir çözümleme altyapısı (Plan 57 Part C)
-- =============================================================================
-- (1) Users.Personelno: Mosaik login kimliği ↔ Zirve personel kodu ("4634-BKM")
--     köprüsü. Amir çözüm zincirinin iki ucu da buna muhtaç (danışman C2, conf 88).
--     NVARCHAR(50) = OrgPositions.HolderPersonelno ile hizalı. Filtered-unique:
--     bir personel = bir user; NULL'lar (admin/servis hesapları) serbest.
-- (2) WorkflowInstances.ResolvedAssigneesJson: StartAsync anında çözülen
--     assigneeKind:"manager" atamaları {"s1":42} — instance-başına DONDURULUR
--     (audit tutarlılığı: onay başladığında amir kimse o karar verir; org sonradan
--     değişse bile). DefinitionJson şablonu paylaşımlı/immutable — oraya yazılamaz;
--     PayloadJson caller-payload'ı — çakışmasın diye AYRI kolon.
-- İdempotent.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'Personelno')
BEGIN
    ALTER TABLE dbo.Users ADD Personelno NVARCHAR(50) NULL;
    PRINT 'Users.Personelno eklendi.';
END
ELSE PRINT 'Users.Personelno zaten var.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'IX_Users_Personelno')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Users_Personelno
        ON dbo.Users (Personelno) WHERE Personelno IS NOT NULL;
    PRINT 'IX_Users_Personelno eklendi.';
END
ELSE PRINT 'IX_Users_Personelno zaten var.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.WorkflowInstances') AND name = N'ResolvedAssigneesJson')
BEGIN
    ALTER TABLE dbo.WorkflowInstances ADD ResolvedAssigneesJson NVARCHAR(400) NULL;
    PRINT 'WorkflowInstances.ResolvedAssigneesJson eklendi.';
END
ELSE PRINT 'ResolvedAssigneesJson zaten var.';
