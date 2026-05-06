-- 30_SeedDashboardConfigs.sql
-- PDKS Pano (ReportId=10) DashboardConfigJson V2 ve Satış Pano yeni rapor kaydı.
-- sp_PdksPano: 7 RS (rs0=personel detay, rs1=şube özet, rs2=KPI, rs3=FM1, rs4=geç kalma, rs5=bölüm doluluk, rs6=eksik okutma)
-- sp_SatisPano: 7 RS (rs0=KPI, rs1=mağaza özet, rs2=ciro trendi, rs3=kategori, rs4=top ürün, rs5=saatlik, rs6=ödeme tipi)

-- PDKS Pano config
UPDATE dbo.ReportCatalog
SET DashboardConfigJson = N'{"schemaVersion":2,"tabs":[{"title":"Özet","components":[{"id":"w_kpi_a1b2c3d4","type":"kpi","title":"Toplam Kadro","span":1,"result":"rs2","agg":"first","column":"KadroToplam","color":"blue","icon":"fas fa-users","subtitle":"Aktif personel"},{"id":"w_kpi_e5f6a7b8","type":"kpi","title":"Gelen","span":1,"result":"rs2","agg":"first","column":"KadroGelen","color":"green","icon":"fas fa-user-check","subtitle":"Bugün giriş yapan"},{"id":"w_kpi_c9d0e1f2","type":"kpi","title":"İzinli","span":1,"result":"rs2","agg":"first","column":"KadroIzinli","color":"yellow","icon":"fas fa-umbrella-beach","subtitle":"Mazeret / İzin"},{"id":"w_kpi_a3b4c5d6","type":"kpi","title":"Gelmedi","span":1,"result":"rs2","agg":"first","column":"KadroGelmedi","color":"red","icon":"fas fa-user-times","subtitle":"Devamsız"},{"id":"w_table_e7f8a9b0","type":"table","title":"Şube Özet","span":4,"result":"rs1","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":20}}]},{"title":"Personel","components":[{"id":"w_table_c1d2e3f4","type":"table","title":"Plan / Fiili Detay","span":4,"result":"rs0","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":50}}]},{"title":"Analizler","components":[{"id":"w_table_a5b6c7d8","type":"table","title":"Fazla Mesai","span":2,"result":"rs3","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"id":"w_table_e9f0a1b2","type":"table","title":"Geç Kalma","span":2,"result":"rs4","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"id":"w_table_c3d4e5f6","type":"table","title":"Bölüm Doluluk","span":2,"result":"rs5","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":20}},{"id":"w_table_a7b8c9d0","type":"table","title":"Eksik Okutma (Dün)","span":2,"result":"rs6","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":20}}]}]}'
WHERE ReportId = 10;
GO

-- Satış Pano — yoksa ekle
IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_SatisPano')
BEGIN
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'Satış Pano',
        N'Günlük satış özet panosu — ciro, mağaza, kategori, ürün analizi',
        N'DER',
        N'dbo.sp_SatisPano',
        N'{"fields":[{"name":"Tarih","label":"Tarih","type":"date","required":false,"default":"today","placeholder":"gg.aa.yyyy","help":"Boş bırakılırsa bugün"},{"name":"sube_Filtre","label":"Mağaza Filtresi","type":"text","required":false,"placeholder":"mekanID virgülle, örn: 1,4477"},{"name":"urunKategori_Filtre","label":"Ürün Kategorisi","type":"text","required":false,"placeholder":"Kategori ID virgülle"}]}',
        N'Mali,Yonetim',
        1,
        N'dashboard',
        N'{"schemaVersion":2,"tabs":[{"title":"Genel Bakış","components":[{"id":"w_kpi_b1c2d3e4","type":"kpi","title":"Bugün Ciro","span":1,"result":"rs0","agg":"first","column":"BugunCiro","color":"blue","icon":"fas fa-coins","subtitle":"Günlük net satış","numberFormat":"currency"},{"id":"w_kpi_f5a6b7c8","type":"kpi","title":"Ort. Sepet","span":1,"result":"rs0","agg":"first","column":"OrtSepet","color":"green","icon":"fas fa-shopping-cart","subtitle":"Ort. fiş tutarı","numberFormat":"currency"},{"id":"w_kpi_d9e0f1a2","type":"kpi","title":"Fiş Sayısı","span":1,"result":"rs0","agg":"first","column":"BugunFis","color":"purple","icon":"fas fa-receipt","subtitle":"Bugün kesilen"},{"id":"w_kpi_b3c4d5e6","type":"kpi","title":"Ay Kümüle","span":1,"result":"rs0","agg":"first","column":"AyKumule","color":"orange","icon":"fas fa-chart-line","subtitle":"Ay başı – bugün","numberFormat":"currency"},{"id":"w_chart_f7a8b9c0","type":"chart","variant":"line","title":"Son 15 Gün Ciro Trendi","span":4,"result":"rs2","labelColumn":"Tarih","datasets":[{"label":"Ciro","column":"Ciro"}]}]},{"title":"Mağaza","components":[{"id":"w_table_d1e2f3a4","type":"table","title":"Mağaza Bazlı Özet","span":4,"result":"rs1","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":20}},{"id":"w_chart_b5c6d7e8","type":"chart","variant":"bar","title":"Saatlik Satış Dağılımı","span":4,"result":"rs5","labelColumn":"Saat","datasets":[{"label":"Ciro","column":"Ciro"}]}]},{"title":"Analiz","components":[{"id":"w_table_f9a0b1c2","type":"table","title":"Kategori Bazlı","span":2,"result":"rs3","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"id":"w_table_d3e4f5a6","type":"table","title":"En Çok Satan Ürünler","span":2,"result":"rs4","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"id":"w_chart_b7c8d9e0","type":"chart","variant":"doughnut","title":"Ödeme Tipi Dağılımı","span":2,"result":"rs6","labelColumn":"OdemeTipi","datasets":[{"label":"Tutar","column":"Tutar"}]}]}]}'
    );
END
GO

-- Satış Pano ReportAllowedRoles — mali(9) + yonetim(10)
-- ReportId dinamik: ProcName üzerinden bulunur (idempotent)
DECLARE @SatisPanoId INT = (SELECT TOP 1 ReportId FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_SatisPano');
IF @SatisPanoId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.ReportAllowedRoles WHERE ReportId = @SatisPanoId AND RoleId = 9)
        INSERT INTO dbo.ReportAllowedRoles (ReportId, RoleId, CreatedAt) VALUES (@SatisPanoId, 9, GETUTCDATE());
    IF NOT EXISTS (SELECT 1 FROM dbo.ReportAllowedRoles WHERE ReportId = @SatisPanoId AND RoleId = 10)
        INSERT INTO dbo.ReportAllowedRoles (ReportId, RoleId, CreatedAt) VALUES (@SatisPanoId, 10, GETUTCDATE());
END
GO
