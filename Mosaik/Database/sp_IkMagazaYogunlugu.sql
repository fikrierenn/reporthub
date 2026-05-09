/* =======================================================================
   dbo.sp_IkMagazaYogunlugu — Mağaza Personel Yoğunluğu
   -----------------------------------------------------------------------
   Kaynak: vw_PersonelDepartman (BKM_GENEL).

   Result Set:
     RS0. KPI (ToplamAktif, SubeKayisi, OrtKidemYil)
     RS1. AltLokasyon bazlı özet (PersonelSayisi, OrtKidemYil)
     RS2. Departman dağılımı (tüm şubelerde)
   ======================================================================= */

IF OBJECT_ID('dbo.sp_IkMagazaYogunlugu', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_IkMagazaYogunlugu;
GO

CREATE PROCEDURE dbo.sp_IkMagazaYogunlugu
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Bugun DATE = CAST(GETDATE() AS DATE);

    -- RS0: KPI
    SELECT
        COUNT(*)                                                                    AS ToplamAktif,
        COUNT(DISTINCT AltLokasyon)                                                 AS SubeKayisi,
        CAST(AVG(DATEDIFF(DAY, Igt, @Bugun) / 365.25) AS DECIMAL(5,1))            AS OrtKidemYil
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL;

    -- RS1: AltLokasyon özeti
    SELECT
        ISNULL(AltLokasyon, N'Belirtilmemiş')                                      AS AltLokasyon,
        ISNULL(Lokasyon,    N'—')                                                   AS Lokasyon,
        COUNT(*)                                                                    AS PersonelSayisi,
        CAST(AVG(DATEDIFF(DAY, Igt, @Bugun) / 365.25) AS DECIMAL(5,1))            AS OrtKidemYil
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL
    GROUP BY AltLokasyon, Lokasyon;

    -- RS2: Departman dağılımı
    SELECT
        ISNULL(Departman, N'Belirtilmemiş') AS Departman,
        COUNT(*)                             AS PersonelSayisi
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL
    GROUP BY Departman;
END
GO
