/* =======================================================================
   dbo.sp_PdksPano — PDKS Yönetim Panosu
   -----------------------------------------------------------------------
   7 result set döndürür. Tüm PDKS verisi tek #pdksPersonel temp table'dan
   türetilir; şube filtresi bu table üzerinden uygulanır.

   Sunucu  : 192.168.40.201 (BKM veritabanı)
   Bağımlı : Linked Server [PDKS] → 192.168.40.66\SQLEXPRESS (wtimserv)
             vrd şeması, [PDKS].[wtimserv].[bkm].[SubeListe]

   Result Set Sırası:
     RS1. Plan-Fiili kişi detay
     RS2. Şube özet
     RS3. KPI tek satır
     RS4. FM1 fazla mesai TOP N
     RS5. Geç kalma TOP N
     RS6. Bölüm doluluk
     RS7. Eksik okutma (dün)
   ======================================================================= */

CREATE OR ALTER PROCEDURE dbo.sp_PdksPano
  @Tarih        date = NULL,
  @FmTop        int = 10,
  @GecTop       int = 10,
  @sube_Filtre  nvarchar(500) = NULL,    -- SubeNo virgülle ayrılmış (NULL=tümü)
  @bolum_Filtre nvarchar(500) = NULL     -- Bölüm adı virgülle ayrılmış (NULL=tümü)
AS
BEGIN
  SET NOCOUNT ON;

  -- ===================================================================
  -- 0. TARİH + DEĞİŞKENLER
  -- ===================================================================
  DECLARE @Gun       date = ISNULL(@Tarih, CAST(GETDATE() AS date));
  DECLARE @AyBas     date = DATEADD(DAY, 1 - DAY(@Gun), @Gun);
  DECLARE @Dun       date = DATEADD(DAY, -1, @Gun);
  DECLARE @HaftaBas  date = DATEADD(DAY, -((DATEPART(WEEKDAY, @Gun) + @@DATEFIRST + 5) % 7), @Gun);
  DECLARE @GunISO    char(8) = CONVERT(char(8), @Gun,   112);
  DECLARE @AyBasISO  char(8) = CONVERT(char(8), @AyBas, 112);
  DECLARE @DunISO    char(8) = CONVERT(char(8), @Dun,   112);
  DECLARE @SimdiTime time    = CONVERT(time, GETDATE());
  DECLARE @GecmisTarih bit   = CASE WHEN @Gun < CAST(GETDATE() AS date) THEN 1 ELSE 0 END;

  -- Şube filtre tablosu (performans için bir kez oluştur)
  CREATE TABLE #subeFiltre (SubeNo int PRIMARY KEY);
  IF @sube_Filtre IS NOT NULL
    INSERT INTO #subeFiltre SELECT CAST(LTRIM(RTRIM(value)) AS int) FROM STRING_SPLIT(@sube_Filtre, ',');

  -- Yetkili Per_Grp2 değerleri (PDKS.bkm.SubeListe köprüsü)
  CREATE TABLE #grp2Filtre (Per_Grp2 nvarchar(50) COLLATE Turkish_CI_AS);
  IF @sube_Filtre IS NOT NULL
    INSERT INTO #grp2Filtre
    SELECT DISTINCT RTRIM(s.Per_Grp2)
    FROM [PDKS].[wtimserv].[bkm].[SubeListe] s
    INNER JOIN #subeFiltre sf ON sf.SubeNo = s.SubeNo
    WHERE s.Per_Grp2 IS NOT NULL;

  -- ===================================================================
  -- 1. ANA PDKS VERİSİ — tek OPENQUERY, her yerde kullanılacak
  -- ===================================================================
  CREATE TABLE #pdks (
    PersNr    int,
    TC        nvarchar(20) COLLATE Turkish_CI_AS,
    AdSoyad   nvarchar(100),
    Per_Grp2  nvarchar(50) COLLATE Turkish_CI_AS
  );

  INSERT INTO #pdks
  SELECT PIn_PersNr, PIn_SteuerNr, Per_Vorname + ' ' + Per_Name, Per_Grp2
  FROM OPENQUERY([PDKS], '
    SELECT i.PIn_PersNr, i.PIn_SteuerNr,
           p.Per_Vorname, p.Per_Name, p.Per_Grp2
    FROM TPerInd i
    INNER JOIN TPerTab p ON p.Per_PersNr = i.PIn_PersNr
    WHERE p.Per_ZeitAktiv = 1
  ');

  -- Şube filtresi uygula (filtre yoksa tümü kalır)
  IF @sube_Filtre IS NOT NULL
    DELETE FROM #pdks
    WHERE NOT EXISTS (
      SELECT 1 FROM #grp2Filtre g
      WHERE g.Per_Grp2 = RTRIM(#pdks.Per_Grp2) COLLATE Turkish_CI_AS
    );

  -- ===================================================================
  -- 2. BUGÜNÜN FİİLİ GİRİŞ/ÇIKIŞ VERİSİ
  -- ===================================================================
  CREATE TABLE #fiili (
    TC         nvarchar(20) COLLATE Turkish_CI_AS,
    FiiliGiris varchar(5),
    FiiliCikis varchar(5),
    BrutSure   decimal(18,2),
    MazeretKod nvarchar(10)
  );

  DECLARE @oqFiili nvarchar(max) = N'
    SELECT * FROM OPENQUERY([PDKS], ''
      SELECT i.PIn_SteuerNr  AS TC,
             CONVERT(varchar(5), l.TLe_VonZeit, 108) AS FiiliGiris,
             CONVERT(varchar(5), l.TLe_BisZeit, 108) AS FiiliCikis,
             l.TLe_IstZeit   AS BrutSure,
             l.TLe_AbwArt    AS MazeretKod
      FROM TPerInd i
      INNER JOIN TTagLes l ON l.TLe_PersNr = i.PIn_PersNr
      WHERE l.TLe_Datum = ''''' + @GunISO + '''''
        AND l.TLe_BeginnKz = 0
    '')';

  INSERT INTO #fiili EXEC sp_executesql @oqFiili;

  -- Şube filtresi: #fiili'den yetkisiz personeli sil
  IF @sube_Filtre IS NOT NULL
    DELETE FROM #fiili
    WHERE TC COLLATE Turkish_CI_AS NOT IN (SELECT TC FROM #pdks);

  -- ===================================================================
  -- 3. PLAN VERİSİ (vrd)
  -- ===================================================================
  SELECT
    g.SubeNo, s.SubeAd AS Sube, g.Bolum, g.Personel, g.SicilNo AS TC,
    g.VardiyaId,
    vz.Aciklama AS PlanVardiya,
    CONVERT(varchar(5), vz.Baslama, 108) AS PlanBas,
    CONVERT(varchar(5), vz.Bitis,   108) AS PlanBit,
    vz.ToplamCalismaDk AS PlanDk,
    vz.ToplamCalismaDk - ISNULL(vz.MolaSureDk,0) AS PlanNetDk,
    CAST(vz.Izin AS int) AS IzinMi,
    CASE WHEN g.VardiyaId = 100 THEN 1 ELSE 0 END AS OzelDurumMu
  INTO #plan
  FROM vrd.Vardiya v
  INNER JOIN vrd.VardiyaDetay vd ON vd.VardiyaNo = v.VardiyaNo
  CROSS APPLY (VALUES
    (DATEADD(DAY,0,v.Tarih),vd.Pazartesi), (DATEADD(DAY,1,v.Tarih),vd.Sali),
    (DATEADD(DAY,2,v.Tarih),vd.Carsamba),  (DATEADD(DAY,3,v.Tarih),vd.Persembe),
    (DATEADD(DAY,4,v.Tarih),vd.Cuma),      (DATEADD(DAY,5,v.Tarih),vd.Cumartesi),
    (DATEADD(DAY,6,v.Tarih),vd.Pazar)
  ) gun(GunTarih, VardiyaId)
  INNER JOIN vrd.SubeListe    s  ON s.SubeNo    = v.SubeNo
  LEFT  JOIN vrd.VardiyaZaman vz ON vz.VardiyaId = gun.VardiyaId
  CROSS APPLY (
    SELECT v.SubeNo, vd.Bolum, vd.Personel, vd.SicilNo, gun.VardiyaId
  ) g(SubeNo, Bolum, Personel, SicilNo, VardiyaId)
  WHERE gun.GunTarih = @Gun
    AND (@sube_Filtre IS NULL OR v.SubeNo IN (SELECT SubeNo FROM #subeFiltre))
    AND (@bolum_Filtre IS NULL OR vd.Bolum IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@bolum_Filtre, ',')));

  -- ===================================================================
  -- RS1: PLAN-FİİLİ KİŞİ DETAY
  -- ===================================================================
  SELECT
    p.Sube, p.Bolum, p.Personel, p.TC,
    p.PlanVardiya, p.PlanBas, p.PlanBit, p.PlanDk, p.PlanNetDk,
    f.FiiliGiris, f.FiiliCikis, f.BrutSure, f.MazeretKod,
    p.IzinMi, p.OzelDurumMu,
    CASE
      WHEN p.IzinMi = 1 THEN 'IZINLI'
      WHEN p.OzelDurumMu = 1 AND f.FiiliGiris IS NULL THEN 'OZEL'
      WHEN f.MazeretKod IS NOT NULL AND f.MazeretKod <> '' THEN 'MAZERETLI'
      WHEN f.FiiliGiris IS NULL AND f.TC IS NULL THEN 'YOK'
      WHEN f.FiiliGiris IS NULL AND (
             @GecmisTarih = 1 OR p.PlanBas IS NULL
             OR @SimdiTime > DATEADD(MINUTE, 15, CONVERT(time, p.PlanBas))
           ) THEN 'GELMEDI'
      WHEN f.FiiliGiris IS NULL THEN 'BEKLIYOR'
      WHEN f.FiiliCikis IS NULL THEN 'ICERIDE'
      ELSE 'TAMAM'
    END AS Durum,
    CASE WHEN f.FiiliGiris IS NOT NULL AND p.PlanBas IS NOT NULL
      THEN DATEDIFF(MINUTE, CONVERT(time, p.PlanBas), CONVERT(time, f.FiiliGiris))
    END AS GirisSapmaDk,
    CASE WHEN f.FiiliCikis IS NOT NULL AND p.PlanBit IS NOT NULL
      THEN DATEDIFF(MINUTE, CONVERT(time, p.PlanBit), CONVERT(time, f.FiiliCikis))
    END AS CikisSapmaDk
  FROM #plan p
  LEFT JOIN #fiili f ON f.TC COLLATE Turkish_CI_AS = p.TC COLLATE Turkish_CI_AS
  WHERE p.IzinMi = 0 AND p.OzelDurumMu = 0
  ORDER BY p.Sube, p.Bolum, p.Personel;

  -- ===================================================================
  -- RS2: ŞUBE ÖZET
  -- ===================================================================
  SELECT
    p.Sube,
    COUNT(*) AS PlanSayisi,
    SUM(CASE WHEN p.IzinMi=0 AND p.OzelDurumMu=0 THEN 1 ELSE 0 END) AS CalismaPlan,
    SUM(CASE WHEN p.IzinMi=0 AND p.OzelDurumMu=0 AND f.FiiliGiris IS NOT NULL THEN 1 ELSE 0 END) AS Geldi,
    SUM(CASE WHEN p.IzinMi=0 AND p.OzelDurumMu=0 AND f.FiiliGiris IS NULL AND f.TC IS NOT NULL
              AND (@GecmisTarih=1 OR p.PlanBas IS NULL OR @SimdiTime > DATEADD(MINUTE,15,CONVERT(time,p.PlanBas)))
              THEN 1 ELSE 0 END) AS Gelmedi,
    SUM(CASE WHEN p.IzinMi=0 AND p.OzelDurumMu=0 AND f.FiiliGiris IS NULL AND f.TC IS NOT NULL
              AND @GecmisTarih=0 AND p.PlanBas IS NOT NULL
              AND @SimdiTime <= DATEADD(MINUTE,15,CONVERT(time,p.PlanBas))
              THEN 1 ELSE 0 END) AS Bekliyor,
    SUM(CASE WHEN p.IzinMi=0 AND p.OzelDurumMu=0 AND f.TC IS NULL THEN 1 ELSE 0 END) AS PdksYok,
    SUM(CASE WHEN p.IzinMi=1 THEN 1 ELSE 0 END) AS Izinli,
    SUM(CASE WHEN p.OzelDurumMu=1 THEN 1 ELSE 0 END) AS OzelDurum,
    SUM(CASE WHEN p.IzinMi=0 AND p.OzelDurumMu=0 AND f.FiiliGiris IS NOT NULL AND f.FiiliCikis IS NULL THEN 1 ELSE 0 END) AS SuAnIceride
  FROM #plan p
  LEFT JOIN #fiili f ON f.TC COLLATE Turkish_CI_AS = p.TC COLLATE Turkish_CI_AS
  GROUP BY p.Sube
  ORDER BY p.Sube;

  -- ===================================================================
  -- RS3: KPI TEK SATIR
  -- ===================================================================
  DECLARE @TotalKadro int, @TotalGelen int, @TotalIzinli int, @TotalGelmedi int;

  SELECT
    @TotalKadro  = COUNT(*),
    @TotalGelen  = SUM(CASE WHEN f.FiiliGiris IS NOT NULL THEN 1 ELSE 0 END),
    @TotalIzinli = SUM(CASE WHEN f.MazeretKod IS NOT NULL AND f.MazeretKod <> '' THEN 1 ELSE 0 END)
  FROM #pdks pd
  LEFT JOIN #fiili f ON f.TC COLLATE Turkish_CI_AS = pd.TC COLLATE Turkish_CI_AS;

  SET @TotalGelmedi = @TotalKadro - ISNULL(@TotalGelen,0) - ISNULL(@TotalIzinli,0);

  DECLARE @PlanToplam int, @PlanGeldi int, @PlanGelmedi int, @PlanBekliyor int, @PlanIzinli int;

  SELECT
    @PlanToplam = SUM(CASE WHEN IzinMi=0 AND OzelDurumMu=0 THEN 1 ELSE 0 END),
    @PlanIzinli = SUM(CASE WHEN IzinMi=1 THEN 1 ELSE 0 END)
  FROM #plan;

  SELECT @PlanGeldi = COUNT(DISTINCT f.TC)
  FROM #plan p
  INNER JOIN #fiili f ON f.TC COLLATE Turkish_CI_AS = p.TC COLLATE Turkish_CI_AS
  WHERE p.IzinMi=0 AND p.OzelDurumMu=0 AND f.FiiliGiris IS NOT NULL;

  SELECT
    @PlanGelmedi = SUM(CASE WHEN f.FiiliGiris IS NULL AND f.TC IS NOT NULL
      AND (@GecmisTarih=1 OR p.PlanBas IS NULL OR @SimdiTime > DATEADD(MINUTE,15,CONVERT(time,p.PlanBas)))
      THEN 1 ELSE 0 END),
    @PlanBekliyor = SUM(CASE WHEN f.FiiliGiris IS NULL AND f.TC IS NOT NULL
      AND @GecmisTarih=0 AND p.PlanBas IS NOT NULL
      AND @SimdiTime <= DATEADD(MINUTE,15,CONVERT(time,p.PlanBas))
      THEN 1 ELSE 0 END)
  FROM #plan p
  LEFT JOIN #fiili f ON f.TC COLLATE Turkish_CI_AS = p.TC COLLATE Turkish_CI_AS
  WHERE p.IzinMi=0 AND p.OzelDurumMu=0;

  SELECT
    @Gun AS Tarih, DATENAME(WEEKDAY, @Gun) AS GunAdi, @HaftaBas AS HaftaBas,
    @TotalKadro AS KadroToplam, @TotalGelen AS KadroGelen,
    @TotalIzinli AS KadroIzinli, @TotalGelmedi AS KadroGelmedi,
    CAST(ROUND(100.0*@TotalGelen/NULLIF(@TotalKadro,0),1) AS decimal(5,1)) AS KadroKatilimYuzde,
    @PlanToplam AS PlanToplam, @PlanGeldi AS PlanGeldi,
    @PlanGelmedi AS PlanGelmedi, ISNULL(@PlanBekliyor,0) AS PlanBekliyor,
    @PlanIzinli AS PlanIzinli,
    CAST(ROUND(100.0*@PlanGeldi/NULLIF(@PlanToplam,0),1) AS decimal(5,1)) AS PlanKatilimYuzde;

  -- ===================================================================
  -- RS4: FM1 FAZLA MESAİ TOP N (ay başı → bugün)
  -- ===================================================================
  CREATE TABLE #fmRaw (
    PersNr int, Bolum nvarchar(50), Gun int, FmDk int
  );

  DECLARE @oqMesai nvarchar(max) = N'
    SELECT * FROM OPENQUERY([PDKS], ''
      SELECT z.TZe_PersNr AS PersNr, p.Per_Grp2 AS Bolum,
             COUNT(DISTINCT z.TZe_Datum) AS Gun,
             SUM(DATEDIFF(MINUTE, z.TZe_VonZeit, z.TZe_BisZeit)) AS FmDk
      FROM TTagZei z
      INNER JOIN TPerTab p ON p.Per_PersNr = z.TZe_PersNr
      WHERE z.TZe_Datum >= ''''' + @AyBasISO + '''''
        AND z.TZe_Datum <= ''''' + @GunISO + '''''
        AND z.TZe_ZeitArt = ''''FM1''''
        AND z.TZe_VonZeit IS NOT NULL AND z.TZe_BisZeit IS NOT NULL
      GROUP BY z.TZe_PersNr, p.Per_Grp2
    '')';

  INSERT INTO #fmRaw EXEC sp_executesql @oqMesai;

  SELECT TOP (@FmTop)
    pd.PersNr AS Sicil, pd.AdSoyad, RTRIM(pd.Per_Grp2) AS Bolum,
    fm.Gun, CAST(fm.FmDk / 60.0 AS decimal(10,2)) AS FmSaat, fm.FmDk
  FROM #fmRaw fm
  INNER JOIN #pdks pd ON pd.PersNr = fm.PersNr
  ORDER BY fm.FmDk DESC;

  -- ===================================================================
  -- RS5: GEÇ KALMA TOP N (ay başı → bugün)
  -- ===================================================================
  CREATE TABLE #ayPlan (
    TC nvarchar(20), Personel nvarchar(100), Sube nvarchar(50), Gun date, PlanBas time
  );

  INSERT INTO #ayPlan
  SELECT vd.SicilNo, vd.Personel, s.SubeAd, gun.GunTarih, vz.Baslama
  FROM vrd.Vardiya v
  INNER JOIN vrd.VardiyaDetay vd ON vd.VardiyaNo = v.VardiyaNo
  CROSS APPLY (VALUES
    (DATEADD(DAY,0,v.Tarih),vd.Pazartesi), (DATEADD(DAY,1,v.Tarih),vd.Sali),
    (DATEADD(DAY,2,v.Tarih),vd.Carsamba),  (DATEADD(DAY,3,v.Tarih),vd.Persembe),
    (DATEADD(DAY,4,v.Tarih),vd.Cuma),      (DATEADD(DAY,5,v.Tarih),vd.Cumartesi),
    (DATEADD(DAY,6,v.Tarih),vd.Pazar)
  ) gun(GunTarih, VardiyaId)
  INNER JOIN vrd.VardiyaZaman vz ON vz.VardiyaId = gun.VardiyaId
  INNER JOIN vrd.SubeListe    s  ON s.SubeNo    = v.SubeNo
  WHERE gun.GunTarih >= @AyBas AND gun.GunTarih <= @Gun
    AND vz.Izin = 0 AND gun.VardiyaId <> 100
    AND vz.Baslama IS NOT NULL AND CAST(vz.Baslama AS time) <> CAST('00:00' AS time)
    AND (@sube_Filtre IS NULL OR v.SubeNo IN (SELECT SubeNo FROM #subeFiltre));

  CREATE TABLE #ayFiili (TC nvarchar(20), Gun date, Giris time);

  DECLARE @oqAyFiili nvarchar(max) = N'
    SELECT * FROM OPENQUERY([PDKS], ''
      SELECT i.PIn_SteuerNr AS TC,
             CONVERT(date, l.TLe_Datum, 112) AS Gun,
             CONVERT(time, l.TLe_VonZeit) AS Giris
      FROM TPerInd i
      INNER JOIN TTagLes l ON l.TLe_PersNr = i.PIn_PersNr
      WHERE l.TLe_Datum >= ''''' + @AyBasISO + '''''
        AND l.TLe_Datum <= ''''' + @GunISO + '''''
        AND l.TLe_BeginnKz = 0 AND l.TLe_VonZeit IS NOT NULL
    '')';

  INSERT INTO #ayFiili EXEC sp_executesql @oqAyFiili;

  SELECT TOP (@GecTop)
    ap.TC, ap.Personel, ap.Sube,
    COUNT(*) AS Gun,
    SUM(DATEDIFF(MINUTE, ap.PlanBas, af.Giris)) AS ToplamGecikmeDk
  FROM #ayPlan ap
  INNER JOIN #ayFiili af ON af.TC COLLATE Turkish_CI_AS = ap.TC COLLATE Turkish_CI_AS AND af.Gun = ap.Gun
  WHERE DATEDIFF(MINUTE, ap.PlanBas, af.Giris) > 5
  GROUP BY ap.TC, ap.Personel, ap.Sube
  ORDER BY ToplamGecikmeDk DESC;

  -- ===================================================================
  -- RS6: BÖLÜM DOLULUK
  -- ===================================================================
  SELECT
    RTRIM(pd.Per_Grp2) AS Bolum,
    COUNT(*) AS Kadro,
    SUM(CASE WHEN f.FiiliGiris IS NOT NULL THEN 1 ELSE 0 END) AS Gelen,
    SUM(CASE WHEN f.MazeretKod IS NOT NULL AND f.MazeretKod <> '' THEN 1 ELSE 0 END) AS Izinli
  FROM #pdks pd
  LEFT JOIN #fiili f ON f.TC COLLATE Turkish_CI_AS = pd.TC COLLATE Turkish_CI_AS
  WHERE pd.Per_Grp2 IS NOT NULL AND RTRIM(pd.Per_Grp2) <> ''
  GROUP BY RTRIM(pd.Per_Grp2)
  ORDER BY Kadro DESC;

  -- ===================================================================
  -- RS7: EKSİK OKUTMA (dün)
  -- ===================================================================
  CREATE TABLE #eksik (
    PersNr int, Bolum nvarchar(50), Giris varchar(5), Cikis varchar(5)
  );

  DECLARE @oqEksik nvarchar(max) = N'
    SELECT * FROM OPENQUERY([PDKS], ''
      SELECT l.TLe_PersNr AS PersNr, p.Per_Grp2 AS Bolum,
             CONVERT(varchar(5), l.TLe_VonZeit, 108) AS Giris,
             CONVERT(varchar(5), l.TLe_BisZeit, 108) AS Cikis
      FROM TTagLes l
      INNER JOIN TPerTab p ON p.Per_PersNr = l.TLe_PersNr
      WHERE l.TLe_Datum = ''''' + @DunISO + '''''
        AND l.TLe_BeginnKz = 0
        AND l.TLe_VonZeit IS NOT NULL AND l.TLe_BisZeit IS NULL
    '')';

  INSERT INTO #eksik EXEC sp_executesql @oqEksik;

  SELECT TOP 20
    pd.PersNr AS Sicil, pd.AdSoyad, RTRIM(pd.Per_Grp2) AS Bolum,
    e.Giris, e.Cikis
  FROM #eksik e
  INNER JOIN #pdks pd ON pd.PersNr = e.PersNr
  ORDER BY pd.Per_Grp2, pd.AdSoyad;

  -- ===================================================================
  -- TEMİZLİK
  -- ===================================================================
  DROP TABLE IF EXISTS #subeFiltre, #grp2Filtre, #pdks, #fiili, #plan,
                       #fmRaw, #ayPlan, #ayFiili, #eksik;
END;
GO
