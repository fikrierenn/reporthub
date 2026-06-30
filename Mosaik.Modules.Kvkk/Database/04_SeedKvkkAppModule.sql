-- Plan 54 M6 / Plan 40 Faz 2 — KVKK modülü sidebar kaydı (AppModules).
-- GroupKey='process' (Süreçler grubu), enabled. Idempotent.
IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = N'kvkk')
BEGIN
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType, GroupKey)
    VALUES (N'kvkk', N'KVKK Envanteri', 1, 50, N'extension', N'process');
END
GO

PRINT 'KVKK AppModule (04) seed edildi.';
