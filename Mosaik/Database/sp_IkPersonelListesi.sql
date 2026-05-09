/* =======================================================================
   dbo.sp_IkPersonelListesi — Aktif Personel Listesi
   -----------------------------------------------------------------------
   Kaynak: vw_PersonelDepartman (BKM_GENEL).
   @sube_Filtre: UserDataFilterInjector tarafından otomatik enjekte edilir
                 (FilterKey='sube', DataSourceKey='IK').

   Result Set:
     RS0. Aktif personel listesi
   ======================================================================= */

IF OBJECT_ID('dbo.sp_IkPersonelListesi', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_IkPersonelListesi;
GO

CREATE PROCEDURE dbo.sp_IkPersonelListesi
    @sube_Filtre NVARCHAR(500) = NULL   -- NULL = tümü, virgülle ayrılmış AltLokasyon
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Personelno,
        AdSoyad,
        Firma,
        ISNULL(Lokasyon,    N'—') AS Lokasyon,
        ISNULL(AltLokasyon, N'—') AS AltLokasyon,
        ISNULL(Departman,   N'—') AS Departman,
        ISNULL(Unvan,       N'—') AS Unvan,
        CONVERT(VARCHAR(10), Igt, 120) AS IseGirisTarihi,
        CAST(DATEDIFF(DAY, Igt, GETDATE()) / 365.25 AS DECIMAL(5,1)) AS KidemYil
    FROM dbo.vw_PersonelDepartman
    WHERE Ict IS NULL
      AND (
          @sube_Filtre IS NULL
          OR ',' + @sube_Filtre + ',' LIKE '%,' + AltLokasyon + ',%'
      )
    ORDER BY Firma, AltLokasyon, AdSoyad;
END
GO
