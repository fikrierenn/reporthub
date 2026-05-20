-- Plan 36 demo seed — Sözleşme Onay Zinciri (3 step: Onay → Bilgilendirme → Final Onay).
-- Delay step kaldırıldı (kafa karıştırıcı; Onay step zaten deadline+escalation taşır).
-- Tek seferlik, idempotent. Mevcut Mosaik kurulumunda admin (UserId=6) + ik (UserId=7) + Contract Id=4 varsayar.
-- Çalıştırma: sqlcli script Mosaik/Database/seed-workflow-demo.sql

-- 1. Template
IF NOT EXISTS (SELECT 1 FROM WorkflowTemplates WHERE Name = N'Sözleşme Onay Zinciri (Demo)' AND FirmaId = 1)
BEGIN
    INSERT INTO WorkflowTemplates (FirmaId, Name, EntityType, DefinitionJson, IsActive, CreatedAt, CreatedBy)
    VALUES (
        1,
        N'Sözleşme Onay Zinciri (Demo)',
        N'Contract',
        N'{"properties":{},"sequence":[' +
          N'{"id":"s1","componentType":"task","type":"approval","name":"Yönetici Onayı","properties":{"name":"Yönetici Onayı","assigneeUserId":6,"assigneeRole":"","deadlineDays":2,"escalateTo":"admin","requireComment":false}},' +
          N'{"id":"s2","componentType":"task","type":"notify","name":"İK Bilgilendirme","properties":{"name":"İK Bilgilendirme","assigneeUserId":7,"assigneeRole":""}},' +
          N'{"id":"s3","componentType":"task","type":"approval","name":"Final Onay","properties":{"name":"Final Onay","assigneeUserId":7,"assigneeRole":"","deadlineDays":3,"escalateTo":"admin","requireComment":true}}' +
          N']}',
        1,
        SYSUTCDATETIME(),
        6
    );
END
GO

DECLARE @TemplateId INT = (SELECT TOP 1 Id FROM WorkflowTemplates WHERE Name = N'Sözleşme Onay Zinciri (Demo)' AND FirmaId = 1);
DECLARE @ContractId INT = (SELECT TOP 1 Id FROM Contracts WHERE FirmaId = 1 ORDER BY Id DESC);

-- 2. Instance — sadece bu template + contract için açık aktif instance yoksa
IF NOT EXISTS (
    SELECT 1 FROM WorkflowInstances
    WHERE TemplateId = @TemplateId AND EntityType = N'Contract' AND EntityId = @ContractId AND Status = 0
)
BEGIN
    INSERT INTO WorkflowInstances (FirmaId, TemplateId, EntityType, EntityId, CurrentStepId, Status, StartedAt, StartedBy, PayloadJson)
    VALUES (1, @TemplateId, N'Contract', @ContractId, N's1', 0, SYSUTCDATETIME(), 6, NULL);

    DECLARE @InstanceId INT = SCOPE_IDENTITY();

    INSERT INTO WorkflowInstanceLogs (InstanceId, StepId, EventType, ActorId, OccurredAt, PayloadJson)
    VALUES
        (@InstanceId, NULL, N'InstanceStarted', 6, SYSUTCDATETIME(), NULL),
        (@InstanceId, N's1', N'StepEntered', 6, SYSUTCDATETIME(), NULL);

    PRINT N'Seed tamam — TemplateId=' + CAST(@TemplateId AS NVARCHAR(10)) + N', InstanceId=' + CAST(@InstanceId AS NVARCHAR(10)) + N', Contract Id=' + CAST(@ContractId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT N'Seed atlandı — aynı template + contract için aktif instance zaten var.';
END
GO
