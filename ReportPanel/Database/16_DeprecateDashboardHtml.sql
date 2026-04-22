-- M-05 Faz B (22 Nisan 2026)
-- DashboardHtml legacy retirement — orphan check + idempotent.
-- DashboardConfigJson birincil source-of-truth. Mevcut DashboardHtml kayitlari
-- legacy fallback ile render edilir (ReportsController.Run), ama yeni yazim
-- yollari bu alana dokunmaz. Kolon bu migration'da DROP EDILMEZ — sadece
-- orphan (HTML-only) raporlar log'lanir. Faz C'de (ileride) drop edilecek.
--
-- ADR: docs/ADR/005-dashboard-architecture.md (TODO yazim)

USE [PortalHUB];
GO

-- Orphan check: HtmlOnly dashboard raporlari (HTML var ama ConfigJson yok).
-- Bu raporlar legacy fallback yolundan render ediliyor; ideali DashboardConfigJson'a
-- migrate etmek, Faz C once.
DECLARE @HtmlOnlyCount INT = (
    SELECT COUNT(1)
    FROM [dbo].[ReportCatalog]
    WHERE ReportType = 'dashboard'
      AND IsActive = 1
      AND DashboardHtml IS NOT NULL
      AND LEN(LTRIM(RTRIM(DashboardHtml))) > 0
      AND (DashboardConfigJson IS NULL OR LEN(LTRIM(RTRIM(DashboardConfigJson))) = 0)
);

DECLARE @BothCount INT = (
    SELECT COUNT(1)
    FROM [dbo].[ReportCatalog]
    WHERE ReportType = 'dashboard'
      AND IsActive = 1
      AND DashboardHtml IS NOT NULL
      AND LEN(LTRIM(RTRIM(DashboardHtml))) > 0
      AND DashboardConfigJson IS NOT NULL
      AND LEN(LTRIM(RTRIM(DashboardConfigJson))) > 0
);

IF @HtmlOnlyCount > 0
BEGIN
    PRINT CONCAT(
        'UYARI: ', @HtmlOnlyCount,
        ' aktif dashboard raporu yalnizca DashboardHtml kullaniyor (DashboardConfigJson YOK). ',
        'Bu raporlar legacy fallback ile render edilir. Faz C''den once DashboardConfigJson''a ',
        'migrate edilmeli — aksi halde kolon drop edildiginde render kirilir.'
    );
    PRINT 'Liste icin:';
    PRINT '  SELECT ReportId, Title FROM dbo.ReportCatalog';
    PRINT '   WHERE ReportType = ''dashboard'' AND IsActive = 1';
    PRINT '     AND DashboardHtml IS NOT NULL AND LEN(LTRIM(RTRIM(DashboardHtml))) > 0';
    PRINT '     AND (DashboardConfigJson IS NULL OR LEN(LTRIM(RTRIM(DashboardConfigJson))) = 0);';
END
ELSE
BEGIN
    PRINT 'OK — DashboardHtml-only aktif dashboard yok.';
END

IF @BothCount > 0
BEGIN
    PRINT CONCAT(
        'Bilgi: ', @BothCount,
        ' aktif dashboard raporunda hem DashboardHtml hem DashboardConfigJson dolu. ',
        'ReportsController ConfigJson''u tercih eder; HTML alani legacy olarak DB''de kalir.'
    );
END
GO

-- NOT: Kolon DROP (Faz C) ileride ayri bir migration (17_) ile yapilacak.
-- Once tum dashboard'lar ConfigJson'a migrate edilmeli ve
-- audit log'ta 'dashboard_html_legacy_render' event'i belirli bir sure
-- hic olusmamali. Guvenli yol.
