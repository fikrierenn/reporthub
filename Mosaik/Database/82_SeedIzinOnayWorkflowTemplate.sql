-- =============================================================================
-- 82 — İzin Talebi Onayı workflow şablonu seed (Plan 57 B2)
-- =============================================================================
-- Form-tetikli onay walking-skeleton: form submit → amir (yonetim) onayı →
-- İK'ya bildirim → kapanış. EntityType='FormSubmission' — Forms köprüsü (B1)
-- FormDefinition.TriggersWorkflowId üzerinden bu şablonu tetikler.
-- DefinitionJson = mevcut demo şablonla aynı sequence formatı (WorkflowDefinition.Parse).
-- Atama rol-bazlı (assigneeRole) — kullanıcı-id bağımlılığı yok, kurulumlar arası taşınır.
-- İdempotent: Name+EntityType üzerinden WHERE NOT EXISTS.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.WorkflowTemplates
               WHERE Name = N'İzin Talebi Onayı' AND EntityType = N'FormSubmission')
BEGIN
    INSERT INTO dbo.WorkflowTemplates (FirmaId, Name, EntityType, DefinitionJson, IsActive, CreatedAt)
    VALUES (1, N'İzin Talebi Onayı', N'FormSubmission',
        N'{"properties":{},"sequence":[' +
        N'{"id":"s1","componentType":"task","type":"approval","name":"Amir Onayı","properties":{"name":"Amir Onayı","assigneeRole":"yonetim","requireComment":false}},' +
        N'{"id":"s2","componentType":"task","type":"notify","name":"İK Bilgilendirme","properties":{"name":"İK Bilgilendirme","assigneeRole":"ik"}}' +
        N']}',
        1, SYSUTCDATETIME());
    PRINT 'İzin Talebi Onayı şablonu seed edildi.';
END
ELSE
    PRINT 'Şablon zaten mevcut - atlandi.';
