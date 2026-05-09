/* =======================================================================
   dbo.sp_IkKidemDagilimi — Kıdem Dağılımı Raporu
   -----------------------------------------------------------------------
   Kaynak: vw_PersonelDepartman (BKM_GENEL).

   Result Set Sırası:
     RS0. KPI (OrtKidemYil, MaxKidemYil, Kidem5Plus, Kidem10Plus)
     RS1. Kıdem grubu dağılımı — bar chart
     RS2. Şube bazlı ortalama kıdem — tablo
   ======================================================================= */

IF OBJECT_ID('dbo.sp_IkKidemDagilimi', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_IkKidemDagilimi;
GO

CREATE PROCEDURE dbo.sp_IkKidemDagilimi
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Bugun DATE = CAST(GETDATE() AS DATE);

    -- RS0: KPI
    SELECT
        CAST(AVG(DATEDIFF(DAY, Igt, @Bugun) / 365.25) AS DECIMAL(5,1))             AS OrtKidemYil,
        CAST(MAX(DATEDIFF(DAY, Igt, @Bugun) / 365.25) AS DECIMAL(5,1))             AS MaxKidemYil,
        SUM(CASE WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 >= 5  THEN 1 ELSE 0 END) AS Kidem5Plus,
        SUM(CASE WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 >= 10 THEN 1 ELSE 0 END) AS Kidem10Plus
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL;

    -- RS1: Kıdem grubu dağılımı
    SELECT
        KidemGrubu,
        COUNT(*) AS PersonelSayisi,
        SortKey
    FROM (
        SELECT
            CASE
                WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 < 1   THEN N'0-1 Yıl'
                WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 < 3   THEN N'1-3 Yıl'
                WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 < 5   THEN N'3-5 Yıl'
                WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 < 10  THEN N'5-10 Yıl'
                ELSE N'10+ Yıl'
            END AS KidemGrubu,
            CASE
                WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 < 1   THEN 1
                WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 < 3   THEN 2
                WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 < 5   THEN 3
                WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 < 10  THEN 4
                ELSE 5
            END AS SortKey
        FROM dbo.vw_PersonelDepartman
        WHERE Ict IS NULL
    ) g
    GROUP BY KidemGrubu, SortKey
    ORDER BY SortKey;

    -- RS2: Şube bazlı ortalama kıdem
    SELECT
        ISNULL(AltLokasyon, N'Belirtilmemiş')                                      AS AltLokasyon,
        COUNT(*)                                                                    AS PersonelSayisi,
        CAST(AVG(DATEDIFF(DAY, Igt, @Bugun) / 365.25) AS DECIMAL(5,1))            AS OrtKidemYil,
        CAST(MAX(DATEDIFF(DAY, Igt, @Bugun) / 365.25) AS DECIMAL(5,1))            AS MaxKidemYil,
        SUM(CASE WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 >= 5 THEN 1 ELSE 0 END) AS Kidem5Plus
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL
    GROUP BY AltLokasyon;
END
GO
