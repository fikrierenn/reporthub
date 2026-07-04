-- =============================================================================
-- 77 — Form template seed: İhbar / Etik Bildirim Hattı (Plan 56 M-A G5, plan 57 A1)
-- =============================================================================
-- Gerçek İhbar formu (dev'de id=6, E2E test edilmiş) → yeniden kurulabilir template.
-- KRİTİK (forms-expert G5): Status=0 TASLAK seed edilir — Status=1 yayın + version
-- snapshot'sız form SubmitAsync fail + Render 404 üretir. Admin Publish'leyince
-- FormSchemaBuilder güncel şemayı üretir (survey-core JSON'u SQL'de TEKRARLANMAZ).
-- İdempotent: Slug üzerinden WHERE NOT EXISTS — mevcut DB'de no-op.
-- CreatedBy=0 (system template): G4 owner-bildirimi CreatedBy>0 guard'ıyla atlanır;
-- admin formu sahiplenip düzenleyince gerçek owner atanır.
-- iletisim alanı G3 koşullu-görünürlük örneği taşır: yalnız "iletişim bırakacağım"
-- seçilince görünür (structured {field,op,value} — FormConditionEvaluator).
-- =============================================================================

SET NOCOUNT ON;

DECLARE @firmaId INT = 1;   -- default firma (kurulumda tek firma varsayımı)
DECLARE @formId INT;

IF NOT EXISTS (SELECT 1 FROM dbo.FormDefinitions WHERE Slug = N'ihbar-hatti' AND FirmaId = @firmaId)
BEGIN
    INSERT INTO dbo.FormDefinitions
        (FirmaId, Slug, Name, Description, Category,
         IsPublic, IsAnonymous, IsEncrypted, Status, Version, CreatedBy, CreatedAt, UpdatedAt)
    VALUES
        (@firmaId, N'ihbar-hatti', N'İhbar / Etik Bildirim Hattı',
         N'Etik ihlali, usulsüzlük ve mevzuata aykırılık bildirimleri. Gizli ve şifreli. Anonim gönderebilirsiniz.',
         N'Uyum',
         1, 1, 1, 0 /* TASLAK — admin Publish'ler */, 1, 0 /* system */, SYSUTCDATETIME(), SYSUTCDATETIME());

    SET @formId = SCOPE_IDENTITY();

    INSERT INTO dbo.FormFields
        (FormDefinitionId, [Order], FieldKey, Label, HelpText, FieldType, IsRequired,
         ValidationRules, Options, ConditionalLogic, DefaultValue, Placeholder)
    VALUES
        (@formId, 1, N'konu', N'İhbar Konusu', NULL, 5 /* select */, 1,
         NULL, N'["Etik İhlali","Mali Usulsüzlük / Yolsuzluk","Mobbing / Taciz","İş Güvenliği İhlali","Çıkar Çatışması","Diğer"]',
         NULL, NULL, NULL),
        (@formId, 2, N'birim', N'İlgili Birim / Departman', NULL, 0 /* text */, 0,
         NULL, NULL, NULL, NULL, NULL),
        (@formId, 3, N'olayTarihi', N'Olay Tarihi', NULL, 3 /* date */, 0,
         NULL, NULL, NULL, NULL, NULL),
        (@formId, 4, N'aciklama', N'Açıklama', NULL, 1 /* textarea */, 1,
         NULL, NULL, NULL, NULL, NULL),
        (@formId, 5, N'belge', N'Kanıt / Belge (opsiyonel)', NULL, 9 /* file */, 0,
         NULL, NULL, NULL, NULL, NULL),
        (@formId, 6, N'iletisimTercihi', N'İletişim Tercihi', NULL, 7 /* radio */, 1,
         NULL, N'["Anonim kalmak istiyorum","Geri dönüş için iletişim bırakacağım"]',
         NULL, NULL, NULL),
        (@formId, 7, N'iletisim', N'İletişim (e-posta / telefon)', NULL, 0 /* text */, 0,
         NULL, NULL,
         N'{"field":"iletisimTercihi","op":"eq","value":"Geri dönüş için iletişim bırakacağım"}',
         NULL, NULL);

    PRINT 'Ihbar template seed edildi (formId=' + CAST(@formId AS NVARCHAR(10)) + ', Status=0 taslak).';
END
ELSE
    PRINT 'Ihbar template zaten mevcut - atlandi (idempotent no-op).';
