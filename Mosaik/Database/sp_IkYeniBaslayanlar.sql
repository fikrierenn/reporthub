/* =======================================================================
   dbo.sp_IkYeniBaslayanlar — Yeni Başlayanlar Raporu
   -----------------------------------------------------------------------
   Kaynak: vw_PersonelDepartman (BKM_GENEL).
   @Days: kaç günlük pencere (30 / 60 / 90). Default 30.

   Result Set:
     RS0. KPI (BuDönem, ÖncekiDönem, Değişim%)
     RS1. Detay liste
   ======================================================================= */

IF OBJECT_ID('dbo.sp_IkYeniBaslayanlar', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_IkYeniBaslayanlar;
GO

CREATE PROCEDURE dbo.sp_IkYeniBaslayanlar
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    IF @Days IS NULL SET @Days = 30;

    DECLARE @Bugun    DATE = CAST(GETDATE() AS DATE);
    DECLARE @BasTarih DATE = DATEADD(DAY, -@Days, @Bugun);
    DECLARE @OncekiBasTarih DATE = DATEADD(DAY, -@Days, @BasTarih);

    -- RS0: KPI
    SELECT
        SUM(CASE WHEN Igt >= @BasTarih                              THEN 1 ELSE 0 END) AS BuDonem,
        SUM(CASE WHEN Igt >= @OncekiBasTarih AND Igt < @BasTarih   THEN 1 ELSE 0 END) AS OncekiDonem,
        CASE
            WHEN SUM(CASE WHEN Igt >= @OncekiBasTarih AND Igt < @BasTarih THEN 1 ELSE 0 END) = 0 THEN NULL
            ELSE CAST(
                100.0
                * SUM(CASE WHEN Igt >= @BasTarih THEN 1 ELSE 0 END)
                / SUM(CASE WHEN Igt >= @OncekiBasTarih AND Igt < @BasTarih THEN 1 ELSE 0 END)
                - 100
                AS DECIMAL(6,1))
        END AS DegisimYuzde
    FROM dbo.vw_PersonelDepartman
    WHERE Igt >= @OncekiBasTarih;

    -- RS1: Detay — bu dönemde başlayanlar
    SELECT
        Personelno,
        AdSoyad,
        Firma,
        ISNULL(AltLokasyon, N'—') AS AltLokasyon,
        ISNULL(Departman,   N'—') AS Departman,
        ISNULL(Unvan,       N'—') AS Unvan,
        CONVERT(VARCHAR(10), Igt, 120) AS IseGirisTarihi
    FROM dbo.vw_PersonelDepartman
    WHERE Igt >= @BasTarih
    ORDER BY Igt DESC;
END
GO
