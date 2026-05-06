-- 30_SeedDashboardConfigs.sql
-- PDKS Pano + Satış Pano DashboardConfigJson V2 (named contract binding) seed.
--
-- Mimari prensip:
--   1. Named contract binding kullan (`result: "kpi"`, `"subeOzet"` vs) — `rs0/rs1` rsN
--      pattern V2 builder default'udur, seed yolu DEĞİLDİR.
--   2. Widget id alanı YAZMA — validator regex `^w_[a-z]+_[a-z0-9]{6,}$` strict.
--      Builder GUI ilk save'de otomatik üretir, seed'de optional bırak.
--   3. tableOptions.pageSize ∈ {0,10,20,50,100} (KnownPageSizes whitelist).
--   4. Mevcut config'i EZME — `WHERE DashboardConfigJson IS NULL` koşulu zorunlu.
--      Aksi halde admin'in elle yaptığı + migration 26+27 chain'inden geçmiş
--      contract'lı config kaybolur (geri dönüşü zor).
--
-- SP yapıları:
--   sp_PdksPano: 7 RS (rs0=planFiili, rs1=subeOzet, rs2=kpi, rs3=fmTopN,
--                rs4=gecTopN, rs5=bolumDoluluk, rs6=eksikOkutma)
--   sp_SatisPano: 7 RS (rs0=kpi, rs1=magazaOzet, rs2=ciroTrend, rs3=kategoriSatis,
--                 rs4=topUrun, rs5=saatlikSatis, rs6=odemeTip)

-- =========================================================================
-- 1. PDKS Pano config (ReportId varsa + DashboardConfigJson NULL ise seed)
-- =========================================================================
UPDATE dbo.ReportCatalog
SET DashboardConfigJson = N'{"schemaVersion":2,"resultContract":{"planFiili":{"resultSet":0,"required":true,"shape":"table"},"subeOzet":{"resultSet":1,"required":true,"shape":"table"},"kpi":{"resultSet":2,"required":true,"shape":"row"},"fmTopN":{"resultSet":3,"required":false,"shape":"table"},"gecTopN":{"resultSet":4,"required":false,"shape":"table"},"bolumDoluluk":{"resultSet":5,"required":false,"shape":"table"},"eksikOkutma":{"resultSet":6,"required":false,"shape":"table"}},"tabs":[{"title":"Özet","components":[{"type":"kpi","title":"Toplam Kadro","span":1,"result":"kpi","agg":"first","column":"KadroToplam","color":"blue","icon":"fas fa-users","subtitle":"Aktif personel"},{"type":"kpi","title":"Gelen","span":1,"result":"kpi","agg":"first","column":"KadroGelen","color":"green","icon":"fas fa-user-check","subtitle":"Bugün giriş yapan"},{"type":"kpi","title":"İzinli","span":1,"result":"kpi","agg":"first","column":"KadroIzinli","color":"yellow","icon":"fas fa-umbrella-beach","subtitle":"Mazeret / İzin"},{"type":"kpi","title":"Gelmedi","span":1,"result":"kpi","agg":"first","column":"KadroGelmedi","color":"red","icon":"fas fa-user-times","subtitle":"Devamsız"},{"type":"table","title":"Şube Özet","span":4,"result":"subeOzet","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":20}}]},{"title":"Personel","components":[{"type":"table","title":"Plan / Fiili Detay","span":4,"result":"planFiili","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":50}}]},{"title":"Analizler","components":[{"type":"table","title":"Fazla Mesai","span":2,"result":"fmTopN","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"type":"table","title":"Geç Kalma","span":2,"result":"gecTopN","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"type":"table","title":"Bölüm Doluluk","span":2,"result":"bolumDoluluk","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":20}},{"type":"table","title":"Eksik Okutma (Dün)","span":2,"result":"eksikOkutma","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":20}}]}]}'
WHERE ProcName LIKE N'%sp_PdksPano' AND DashboardConfigJson IS NULL;
GO

-- =========================================================================
-- 2. Satış Pano — yoksa ekle (config + ReportAllowedRoles)
--    NOT: ProcName schema (`bkm.` vs `dbo.`) DataSource'a (DER) bağlı; mevcut
--    DB'de hangisi varsa o korunur. Yeni install'da `bkm.sp_SatisPano` standardı.
-- =========================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName LIKE N'%sp_SatisPano')
BEGIN
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'Satış Pano',
        N'Günlük satış özet panosu — ciro, mağaza, kategori, ürün analizi',
        N'DER',
        N'bkm.sp_SatisPano',
        N'{"fields":[{"name":"Tarih","label":"Tarih","type":"date","required":false,"default":"today","placeholder":"gg.aa.yyyy","help":"Boş bırakılırsa bugün"},{"name":"sube_Filtre","label":"Mağaza Filtresi","type":"text","required":false,"placeholder":"mekanID virgülle, örn: 1,4477"},{"name":"urunKategori_Filtre","label":"Ürün Kategorisi","type":"text","required":false,"placeholder":"Kategori ID virgülle"}]}',
        N'admin,muhasebe',
        1,
        N'dashboard',
        N'{"schemaVersion":2,"resultContract":{"kpi":{"resultSet":0,"required":true,"shape":"row"},"magazaOzet":{"resultSet":1,"required":true,"shape":"table"},"ciroTrend":{"resultSet":2,"required":false,"shape":"table"},"kategoriSatis":{"resultSet":3,"required":false,"shape":"table"},"topUrun":{"resultSet":4,"required":false,"shape":"table"},"saatlikSatis":{"resultSet":5,"required":false,"shape":"table"},"odemeTip":{"resultSet":6,"required":false,"shape":"table"}},"tabs":[{"title":"Genel Bakış","components":[{"type":"kpi","title":"Bugün Ciro","span":1,"result":"kpi","agg":"first","column":"BugunCiro","color":"blue","icon":"fas fa-coins","subtitle":"Günlük net satış","numberFormat":"currency"},{"type":"kpi","title":"Ort. Sepet","span":1,"result":"kpi","agg":"first","column":"OrtSepet","color":"green","icon":"fas fa-shopping-cart","subtitle":"Ort. fiş tutarı","numberFormat":"currency"},{"type":"kpi","title":"Fiş Sayısı","span":1,"result":"kpi","agg":"first","column":"BugunFis","color":"purple","icon":"fas fa-receipt","subtitle":"Bugün kesilen"},{"type":"kpi","title":"Ay Kümüle","span":1,"result":"kpi","agg":"first","column":"AyKumule","color":"orange","icon":"fas fa-chart-line","subtitle":"Ay başı – bugün","numberFormat":"currency"},{"type":"chart","variant":"line","title":"Son 15 Gün Ciro Trendi","span":4,"result":"ciroTrend","labelColumn":"Tarih","datasets":[{"label":"Ciro","column":"Ciro"}]}]},{"title":"Mağaza","components":[{"type":"table","title":"Mağaza Bazlı Özet","span":4,"result":"magazaOzet","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":20}},{"type":"chart","variant":"bar","title":"Saatlik Satış Dağılımı","span":4,"result":"saatlikSatis","labelColumn":"Saat","datasets":[{"label":"Ciro","column":"Ciro"}]}]},{"title":"Analiz","components":[{"type":"table","title":"Kategori Bazlı","span":2,"result":"kategoriSatis","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"type":"table","title":"En Çok Satan Ürünler","span":2,"result":"topUrun","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"type":"chart","variant":"doughnut","title":"Ödeme Tipi Dağılımı","span":2,"result":"odemeTip","labelColumn":"OdemeTipi","datasets":[{"label":"Tutar","column":"Tutar"}]}]}]}'
    );
END
GO

-- =========================================================================
-- 3. Satış Pano ReportAllowedRoles — admin + muhasebe (varsa atla)
--    ReportId dinamik: ProcName üzerinden bulunur (idempotent).
--    Role.Name eşleşmesi yapılır (RoleId hardcode etme — install ortamında değişebilir).
-- =========================================================================
DECLARE @SatisPanoId INT = (SELECT TOP 1 ReportId FROM dbo.ReportCatalog WHERE ProcName LIKE N'%sp_SatisPano');
IF @SatisPanoId IS NOT NULL
BEGIN
    INSERT INTO dbo.ReportAllowedRoles (ReportId, RoleId, CreatedAt)
    SELECT @SatisPanoId, ro.RoleId, GETUTCDATE()
    FROM dbo.Roles ro
    WHERE ro.Name IN (N'admin', N'muhasebe', N'mali', N'yonetim')
      AND NOT EXISTS (
          SELECT 1 FROM dbo.ReportAllowedRoles rar
          WHERE rar.ReportId = @SatisPanoId AND rar.RoleId = ro.RoleId
      );
END
GO
