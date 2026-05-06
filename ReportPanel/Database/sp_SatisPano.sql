/* =======================================================================
   dbo.sp_SatisPano — Satış Yönetim Dashboard'u
   -----------------------------------------------------------------------
   DerinSISBkm veritabanında çalışır.
   posOzet* tablolarından hızlı aggregate, 7 result set.

   Kullanım:
     EXEC dbo.sp_SatisPano;                       -- bugün, tüm mağazalar, tüm kategoriler
     EXEC dbo.sp_SatisPano '2026-04-15';          -- belirli gün
     EXEC dbo.sp_SatisPano NULL, '1,4477';        -- mekanID filtreli (sube)
     EXEC dbo.sp_SatisPano NULL, NULL, '2,3';     -- urunKategori filtreli (RS4 + RS5)

   Plan 07 Plan B (5 Mayıs 2026): @sube_Filtre artık doğrudan DerinSIS mekanID
   olarak yorumlanır (önceki vrd SubeNo → #subeMap pattern kaldırıldı). Her DataSource
   kendi native ID'sini kullanır; FilterDefinition (DER, sube) OptionsQuery'si
   posMagaza.mekanID döndürür, UserDataFilters'a yazılan mekanID'ler doğrudan SP'ye gider.

   Result Set Sırası:
     RS1. KPI tek satır (bugün, dün, ay kümüle, geçen yıl karşılaştırma)
     RS2. Mağaza bazlı bugünkü özet
     RS3. Son 15 gün ciro trendi (günlük toplam)
     RS4. Kategori bazlı satışlar (bu ay)
     RS5. En çok satan TOP 15 ürün (bu ay)
     RS6. Saatlik satış dağılımı (bugün)
     RS7. Ödeme tipi dağılımı (bugün)
   ======================================================================= */

CREATE OR ALTER PROCEDURE bkm.sp_SatisPano
  @Tarih              date          = NULL,
  @sube_Filtre        nvarchar(500) = NULL,  -- DerinSIS mekanID CSV (NULL=tüm aktif mağazalar)
  @urunKategori_Filtre nvarchar(500) = NULL  -- urnKtgr2.ktgrID CSV (NULL=tüm kategoriler hariç Tanımsız)
AS
BEGIN
  SET NOCOUNT ON;

  -- ===================================================================
  -- 0. TARİH + DEĞİŞKENLER
  -- ===================================================================
  DECLARE @Gun       date = ISNULL(@Tarih, CAST(GETDATE() AS date));
  DECLARE @Dun       date = DATEADD(DAY, -1, @Gun);
  DECLARE @AyBas     date = DATEADD(DAY, 1 - DAY(@Gun), @Gun);
  DECLARE @GecenYilGun date = DATEADD(YEAR, -1, @Gun);
  DECLARE @GecenYilAyBas date = DATEADD(YEAR, -1, @AyBas);
  DECLARE @Trend15Bas date = DATEADD(DAY, -14, @Gun);

  -- Mağaza filtresi: @sube_Filtre = mekanID CSV (Plan 07 Plan B native ID semantiği).
  -- NULL = tüm aktif mağazalar (mekanTip=0); önceki #subeMap hardcoded mapping kaldırıldı.
  CREATE TABLE #mgzFiltre (mekanID int PRIMARY KEY);
  IF @sube_Filtre IS NOT NULL
    INSERT INTO #mgzFiltre
    SELECT DISTINCT CAST(LTRIM(RTRIM(value)) AS int)
    FROM STRING_SPLIT(@sube_Filtre, ',')
    WHERE LTRIM(RTRIM(value)) <> '';
  ELSE
    INSERT INTO #mgzFiltre
    SELECT mekanID FROM dbo.posMagaza WHERE mekanTip = 0;

  -- ===================================================================
  -- RS1: KPI TEK SATIR
  -- ===================================================================
  DECLARE @BugunCiro decimal(15,2) = 0, @BugunAdet int = 0, @BugunFis int = 0;
  DECLARE @DunCiro decimal(15,2) = 0;
  DECLARE @AyKumule decimal(15,2) = 0, @AyAdet int = 0;
  DECLARE @GecenYilBugun decimal(15,2) = 0;
  DECLARE @GecenYilAyKumule decimal(15,2) = 0;

  -- Bugün
  SELECT
    @BugunCiro = ISNULL(SUM(satis - satisIndirim - iade + iadeIndirim), 0),
    @BugunAdet = ISNULL(SUM(satisUrunAdet - iadeUrunAdet), 0),
    @BugunFis  = ISNULL(SUM(satisAdet), 0)
  FROM posOzetMagaza
  WHERE satisTarih = @Gun AND magazaID IN (SELECT mekanID FROM #mgzFiltre);

  -- Dün
  SELECT @DunCiro = ISNULL(SUM(satis - satisIndirim - iade + iadeIndirim), 0)
  FROM posOzetMagaza
  WHERE satisTarih = @Dun AND magazaID IN (SELECT mekanID FROM #mgzFiltre);

  -- Bu ay kümüle
  SELECT
    @AyKumule = ISNULL(SUM(satis - satisIndirim - iade + iadeIndirim), 0),
    @AyAdet   = ISNULL(SUM(satisUrunAdet - iadeUrunAdet), 0)
  FROM posOzetMagaza
  WHERE satisTarih >= @AyBas AND satisTarih <= @Gun
    AND magazaID IN (SELECT mekanID FROM #mgzFiltre);

  -- Geçen yıl aynı gün
  SELECT @GecenYilBugun = ISNULL(SUM(satis - satisIndirim - iade + iadeIndirim), 0)
  FROM posOzetMagaza
  WHERE satisTarih = @GecenYilGun AND magazaID IN (SELECT mekanID FROM #mgzFiltre);

  -- Geçen yıl aynı dönem kümüle
  SELECT @GecenYilAyKumule = ISNULL(SUM(satis - satisIndirim - iade + iadeIndirim), 0)
  FROM posOzetMagaza
  WHERE satisTarih >= @GecenYilAyBas AND satisTarih <= @GecenYilGun
    AND magazaID IN (SELECT mekanID FROM #mgzFiltre);

  SELECT
    @Gun AS Tarih,
    DATENAME(WEEKDAY, @Gun) AS GunAdi,
    @BugunCiro AS BugunCiro,
    @BugunAdet AS BugunAdet,
    @BugunFis AS BugunFis,
    CASE WHEN @BugunFis > 0 THEN CAST(@BugunCiro / @BugunFis AS decimal(10,2)) ELSE 0 END AS OrtSepet,
    @DunCiro AS DunCiro,
    @AyKumule AS AyKumule,
    @AyAdet AS AyAdet,
    @GecenYilBugun AS GecenYilBugun,
    @GecenYilAyKumule AS GecenYilAyKumule,
    CASE WHEN @GecenYilBugun > 0
      THEN CAST(ROUND(100.0 * (@BugunCiro - @GecenYilBugun) / @GecenYilBugun, 1) AS decimal(5,1))
      ELSE NULL END AS GunYillikDegisim,
    CASE WHEN @GecenYilAyKumule > 0
      THEN CAST(ROUND(100.0 * (@AyKumule - @GecenYilAyKumule) / @GecenYilAyKumule, 1) AS decimal(5,1))
      ELSE NULL END AS AyYillikDegisim;

  -- ===================================================================
  -- RS2: MAĞAZA BAZLI BUGÜNKÜ ÖZET
  -- ===================================================================
  SELECT
    m.mekanAd AS Magaza,
    ISNULL(SUM(p.satis - p.satisIndirim - p.iade + p.iadeIndirim), 0) AS Ciro,
    ISNULL(SUM(p.satisUrunAdet - p.iadeUrunAdet), 0) AS Adet,
    ISNULL(SUM(p.satisAdet), 0) AS FisSayisi,
    CASE WHEN SUM(p.satisAdet) > 0
      THEN CAST(SUM(p.satis - p.satisIndirim - p.iade + p.iadeIndirim) / SUM(p.satisAdet) AS decimal(10,2))
      ELSE 0 END AS OrtSepet,
    ISNULL(SUM(p.satisIndirim + p.iadeIndirim), 0) AS ToplamIndirim
  FROM #mgzFiltre f
  INNER JOIN posMagaza m ON m.mekanID = f.mekanID
  LEFT JOIN posOzetMagaza p ON p.magazaID = f.mekanID AND p.satisTarih = @Gun
  GROUP BY m.mekanAd
  ORDER BY Ciro DESC;

  -- ===================================================================
  -- RS3: SON 15 GÜN CİRO TRENDİ
  -- ===================================================================
  ;WITH Gunler AS (
    SELECT DATEADD(DAY, n, @Trend15Bas) AS Gun
    FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14)) v(n)
  )
  SELECT
    CONVERT(varchar(5), g.Gun, 104) AS Tarih,   -- dd.MM
    ISNULL(SUM(p.satis - p.satisIndirim - p.iade + p.iadeIndirim), 0) AS Ciro,
    ISNULL(SUM(p.satisUrunAdet - p.iadeUrunAdet), 0) AS Adet
  FROM Gunler g
  LEFT JOIN posOzetMagaza p ON p.satisTarih = g.Gun AND p.magazaID IN (SELECT mekanID FROM #mgzFiltre)
  GROUP BY g.Gun
  ORDER BY g.Gun;

  -- Ürün kategori filtresi (Plan 07 sonrası — opsiyonel; NULL = tüm kategoriler hariç Tanımsız/Genel).
  CREATE TABLE #ktgrFiltre (ktgrID int PRIMARY KEY);
  IF @urunKategori_Filtre IS NOT NULL
    INSERT INTO #ktgrFiltre
    SELECT DISTINCT CAST(LTRIM(RTRIM(value)) AS int)
    FROM STRING_SPLIT(@urunKategori_Filtre, ',')
    WHERE LTRIM(RTRIM(value)) <> '';

  -- ===================================================================
  -- RS4: KATEGORİ BAZLI SATIŞLAR (bu ay)
  -- ===================================================================
  SELECT TOP 15
    ISNULL(k2.ktgrAd, 'Tanımsız') AS Kategori,
    SUM(H.ehTutarN) AS Ciro,
    SUM(H.ehAdetN) AS Adet,
    CAST(100.0 * SUM(H.ehTutarN) / NULLIF((SELECT SUM(x.ehTutarN) FROM irsHrk x
      INNER JOIN urn xu ON xu.stkID = x.ehstkID
      WHERE x.ehTrhS >= @AyBas AND x.ehTrhS <= @Gun
        AND x.ehMekan IN (SELECT mekanID FROM #mgzFiltre)
        AND x.ehTip IN (4, 100) AND x.ehAltDepo = 0
        AND xu.urnKtgr2ID NOT IN (11)
        AND (@urunKategori_Filtre IS NULL OR xu.urnKtgr2ID IN (SELECT ktgrID FROM #ktgrFiltre))
      ), 0) AS decimal(5,1)) AS CiroPay
  FROM irsHrk H
  INNER JOIN urn U ON U.stkID = H.ehstkID
  LEFT JOIN urnKtgr2 k2 ON k2.ktgrID = U.urnKtgr2ID
  WHERE H.ehTrhS >= @AyBas AND H.ehTrhS <= @Gun
    AND H.ehMekan IN (SELECT mekanID FROM #mgzFiltre)
    AND H.ehTip IN (4, 100)      -- mağaza + POS satış
    AND H.ehAltDepo = 0
    AND U.urnKtgr2ID NOT IN (11) -- Genel kategorisi hariç
    AND (@urunKategori_Filtre IS NULL OR U.urnKtgr2ID IN (SELECT ktgrID FROM #ktgrFiltre))
  GROUP BY k2.ktgrAd
  ORDER BY Ciro DESC;

  -- ===================================================================
  -- RS5: EN ÇOK SATAN TOP 15 ÜRÜN (bu ay)
  -- ===================================================================
  SELECT TOP 15
    U.stkAd AS Urun,
    ISNULL(k2.ktgrAd, '') AS Kategori,
    SUM(H.ehAdetN) AS Adet,
    SUM(H.ehTutarN) AS Ciro
  FROM irsHrk H
  INNER JOIN urn U ON U.stkID = H.ehstkID
  LEFT JOIN urnKtgr2 k2 ON k2.ktgrID = U.urnKtgr2ID
  WHERE H.ehTrhS >= @AyBas AND H.ehTrhS <= @Gun
    AND H.ehMekan IN (SELECT mekanID FROM #mgzFiltre)
    AND H.ehTip IN (4, 100)
    AND H.ehAltDepo = 0
    AND U.urnKtgr2ID NOT IN (11)
    AND (@urunKategori_Filtre IS NULL OR U.urnKtgr2ID IN (SELECT ktgrID FROM #ktgrFiltre))
  GROUP BY U.stkAd, k2.ktgrAd
  ORDER BY Adet DESC;

  -- ===================================================================
  -- RS6: SAATLİK SATIŞ DAĞILIMI (bugün)
  -- ===================================================================
  SELECT
    s.satisSaat AS Saat,
    SUM(s.satis - s.satisIndirim - s.iade + s.iadeIndirim) AS Ciro,
    SUM(s.satisMusteri) AS MusteriSayisi
  FROM posOzetSaat s
  WHERE s.satisTarih = @Gun
    AND s.magazaID IN (SELECT mekanID FROM #mgzFiltre)
  GROUP BY s.satisSaat
  HAVING SUM(s.satis - s.satisIndirim) > 0
  ORDER BY s.satisSaat;

  -- ===================================================================
  -- RS7: ÖDEME TİPİ DAĞILIMI (bugün)
  -- ===================================================================
  SELECT
    CASE o.odemeTip
      WHEN 0 THEN 'Nakit'
      WHEN 1 THEN 'Kredi Kartı'
      WHEN 2 THEN 'Döviz'
      WHEN 3 THEN 'Çek/Senet'
      ELSE 'Diğer'
    END AS OdemeTipi,
    SUM(oz.tutar - oz.tutarIade) AS Tutar
  FROM posOzetOdeme oz
  INNER JOIN posOdeme o ON o.odemeID = oz.sOdemeID
  WHERE oz.satisTarih = @Gun
    AND oz.magazaID IN (SELECT mekanID FROM #mgzFiltre)
  GROUP BY o.odemeTip
  HAVING SUM(oz.tutar - oz.tutarIade) > 0
  ORDER BY Tutar DESC;

  -- ===================================================================
  -- TEMİZLİK
  -- ===================================================================
  DROP TABLE IF EXISTS #mgzFiltre, #ktgrFiltre;
END;
GO
