/* =======================================================================
   dbo.sp_IkIstenAyrilanlar — İşten Ayrılanlar Raporu
   -----------------------------------------------------------------------
   Kaynak: vw_PersonelDepartman (BKM_GENEL).
   Default: cari ay başı → bugün.

   Result Set:
     RS0. KPI (ToplamAyrilan, OrtKidemYil)
     RS1. Detay liste
     RS2. Ayrılma kodu dağılımı
   ======================================================================= */

IF OBJECT_ID('dbo.sp_IkIstenAyrilanlar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_IkIstenAyrilanlar;
GO

CREATE PROCEDURE dbo.sp_IkIstenAyrilanlar
    @StartDate DATE = NULL,
    @EndDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Bugun DATE = CAST(GETDATE() AS DATE);
    IF @StartDate IS NULL SET @StartDate = DATEADD(DAY, 1 - DAY(@Bugun), @Bugun);
    IF @EndDate   IS NULL SET @EndDate   = @Bugun;

    -- RS0: KPI
    SELECT
        COUNT(*) AS ToplamAyrilan,
        CAST(AVG(DATEDIFF(DAY, Igt, Ict) / 365.25) AS DECIMAL(5,1)) AS OrtKidemYil
    FROM dbo.vw_PersonelDepartman
    WHERE Ict BETWEEN @StartDate AND @EndDate;

    -- RS1: Detay
    SELECT
        Personelno,
        AdSoyad,
        Firma,
        ISNULL(AltLokasyon,    N'—') AS AltLokasyon,
        ISNULL(Departman,      N'—') AS Departman,
        ISNULL(Unvan,          N'—') AS Unvan,
        CONVERT(VARCHAR(10), Igt, 120) AS IseGirisTarihi,
        CONVERT(VARCHAR(10), Ict, 120) AS IstenCikisTarihi,
        CAST(DATEDIFF(DAY, Igt, Ict) / 365.25 AS DECIMAL(5,1)) AS KidemYil,
        ISNULL(IstenCikisKodu, N'—') AS AyrilmaKodu
    FROM dbo.vw_PersonelDepartman
    WHERE Ict BETWEEN @StartDate AND @EndDate
    ORDER BY Ict DESC;

    -- RS2: Ayrılma kodu dağılımı
    SELECT
        ISNULL(IstenCikisKodu, N'Belirtilmemiş') AS AyrilmaKodu,
        COUNT(*) AS Sayi
    FROM dbo.vw_PersonelDepartman
    WHERE Ict BETWEEN @StartDate AND @EndDate
    GROUP BY IstenCikisKodu;
END
GO
