-- =============================================================================
-- 83 — sop-editor rol seed (Plan 56 M-C)
-- =============================================================================
-- SOP modülü kodu 'sop-editor' rolünü KULLANIYOR ([Authorize(Roles="admin,sop-editor")]
-- SopController.cs:17 + HomeController.cs:23 IsInRole) ama rol tablosuna hiç seed
-- edilmemişti (Plan 34 Faz B "S-13 sonrası eklenir" notu açık kalmış) → rol atanamıyor,
-- yetki fiilen admin-only çalışıyordu. İdempotent seed.

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'sop-editor')
BEGIN
    INSERT INTO dbo.Roles (Name, Description)
    VALUES (N'sop-editor', N'SOP / prosedür düzenleme yetkisi (taslak oluşturma, revizyon, onaya gönderme)');
    PRINT 'sop-editor rolu seed edildi.';
END
ELSE
    PRINT 'sop-editor zaten mevcut - atlandi.';
