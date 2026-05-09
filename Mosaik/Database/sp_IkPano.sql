/* =======================================================================
   dbo.sp_IkPano — İK Yönetim Panosu
   -----------------------------------------------------------------------
   5 result set döndürür. Kaynak: vw_PersonelDepartman (BKM_GENEL).

   Result Set Sırası:
     RS0. KPI tek satır
     RS1. AltLokasyon dağılımı — pie chart
     RS2. Departman dağılımı — bar chart
     RS3. Son 10 yeni başlayan — tablo
     RS4. Kıdem grubu dağılımı — bar chart
   ======================================================================= */

IF OBJECT_ID('dbo.sp_IkPano', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_IkPano;
GO

CREATE PROCEDURE dbo.sp_IkPano
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Bugun DATE = CAST(GETDATE() AS DATE);
    DECLARE @Son30  DATE = DATEADD(DAY, -30, @Bugun);

    -- RS0: KPI
    SELECT
        COUNT(*)                                                                    AS ToplamAktif,
        SUM(CASE WHEN Igt >= @Son30 THEN 1 ELSE 0 END)                             AS Yeni30,
        (SELECT COUNT(*) FROM dbo.vw_PersonelDepartman
         WHERE Ict BETWEEN @Son30 AND @Bugun)                                       AS Ayrilan30,
        CAST(AVG(DATEDIFF(DAY, Igt, @Bugun) / 365.25) AS DECIMAL(5,1))             AS OrtKidemYil,
        SUM(CASE WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 >= 5  THEN 1 ELSE 0 END) AS Kidem5Plus,
        SUM(CASE WHEN DATEDIFF(DAY, Igt, @Bugun) / 365.25 >= 10 THEN 1 ELSE 0 END) AS Kidem10Plus
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL;

    -- RS1: AltLokasyon dağılımı (pie)
    SELECT
        ISNULL(AltLokasyon, N'Belirtilmemiş') AS AltLokasyon,
        COUNT(*)                               AS PersonelSayisi
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL
    GROUP BY AltLokasyon;

    -- RS2: Departman dağılımı (bar) — top 15
    SELECT TOP 15
        ISNULL(Departman, N'Belirtilmemiş') AS Departman,
        COUNT(*)                             AS PersonelSayisi
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL
    GROUP BY Departman
    ORDER BY PersonelSayisi DESC;

    -- RS3: Son 10 yeni başlayan (tablo)
    SELECT TOP 10
        AdSoyad,
        Firma,
        ISNULL(AltLokasyon, N'—') AS AltLokasyon,
        ISNULL(Departman,   N'—') AS Departman,
        ISNULL(Unvan,       N'—') AS Unvan,
        CONVERT(VARCHAR(10), Igt, 120) AS IseGirisTarihi
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL
    ORDER BY Igt DESC;

    -- RS4: Kıdem grubu dağılımı (bar)
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
END
GO
