-- 30_SeedDashboardConfigs.sql
-- PDKS Pano (ReportId=10) DashboardConfigJson V2 ve Satış Pano yeni rapor kaydı.
-- sp_PdksPano: 7 RS (rs0=personel detay, rs1=şube özet, rs2=KPI, rs3=FM1, rs4=geç kalma, rs5=bölüm doluluk, rs6=eksik okutma)
-- sp_SatisPano: 7 RS (rs0=KPI, rs1=mağaza özet, rs2=ciro trendi, rs3=kategori, rs4=top ürün, rs5=saatlik, rs6=ödeme tipi)

-- PDKS Pano config
UPDATE dbo.ReportCatalog
SET DashboardConfigJson = N'{"schemaVersion":2,"tabs":[{"title":"Özet","components":[{"id":"kpi-kadro","type":"kpi","title":"Toplam Kadro","span":1,"result":"rs2","agg":"first","column":"KadroToplam","color":"blue","icon":"fas fa-users","subtitle":"Aktif personel"},{"id":"kpi-gelen","type":"kpi","title":"Gelen","span":1,"result":"rs2","agg":"first","column":"KadroGelen","color":"green","icon":"fas fa-user-check","subtitle":"Bugün giriş yapan"},{"id":"kpi-izinli","type":"kpi","title":"İzinli","span":1,"result":"rs2","agg":"first","column":"KadroIzinli","color":"yellow","icon":"fas fa-umbrella-beach","subtitle":"Mazeret / İzin"},{"id":"kpi-gelmedi","type":"kpi","title":"Gelmedi","span":1,"result":"rs2","agg":"first","column":"KadroGelmedi","color":"red","icon":"fas fa-user-times","subtitle":"Devamsız"},{"id":"tbl-sube","type":"table","title":"Şube Özet","span":4,"result":"rs1","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":20}}]},{"title":"Personel","components":[{"id":"tbl-personel","type":"table","title":"Plan / Fiili Detay","span":4,"result":"rs0","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":50}}]},{"title":"Analizler","components":[{"id":"tbl-fm","type":"table","title":"Fazla Mesai","span":2,"result":"rs3","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"id":"tbl-gec","type":"table","title":"Geç Kalma","span":2,"result":"rs4","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":10}},{"id":"tbl-bolum","type":"table","title":"Bölüm Doluluk","span":2,"result":"rs5","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":20}},{"id":"tbl-eksik","type":"table","title":"Eksik Okutma (Dün)","span":2,"result":"rs6","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":20}}]}]}'
WHERE ReportId = 10 AND DashboardConfigJson IS NULL;
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
        N'{"schemaVersion":2,"tabs":[{"title":"Genel Bakış","components":[{"id":"kpi-ciro","type":"kpi","title":"Bugün Ciro","span":1,"result":"rs0","agg":"first","column":"BugunCiro","color":"blue","icon":"fas fa-coins","subtitle":"Günlük net satış","numberFormat":"currency"},{"id":"kpi-sepet","type":"kpi","title":"Ort. Sepet","span":1,"result":"rs0","agg":"first","column":"OrtSepet","color":"green","icon":"fas fa-shopping-cart","subtitle":"Ort. fiş tutarı","numberFormat":"currency"},{"id":"kpi-fis","type":"kpi","title":"Fiş Sayısı","span":1,"result":"rs0","agg":"first","column":"BugunFis","color":"purple","icon":"fas fa-receipt","subtitle":"Bugün kesilen"},{"id":"kpi-ay","type":"kpi","title":"Ay Kümüle","span":1,"result":"rs0","agg":"first","column":"AyKumule","color":"orange","icon":"fas fa-chart-line","subtitle":"Ay başı – bugün","numberFormat":"currency"},{"id":"chart-trend","type":"chart","variant":"line","title":"Son 15 Gün Ciro Trendi","span":4,"result":"rs2","labelColumn":"Tarih","datasets":[{"label":"Ciro","column":"Ciro"}]}]},{"title":"Mağaza","components":[{"id":"tbl-magaza","type":"table","title":"Mağaza Bazlı Özet","span":4,"result":"rs1","tableOptions":{"stripe":true,"stickyHeader":true,"clientSearch":true,"pageSize":30}},{"id":"chart-saatlik","type":"chart","variant":"bar","title":"Saatlik Satış Dağılımı","span":4,"result":"rs5","labelColumn":"Saat","datasets":[{"label":"Ciro","column":"Ciro"}]}]},{"title":"Analiz","components":[{"id":"tbl-kategori","type":"table","title":"Kategori Bazlı","span":2,"result":"rs3","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":15}},{"id":"tbl-urun","type":"table","title":"En Çok Satan Ürünler","span":2,"result":"rs4","tableOptions":{"stripe":true,"stickyHeader":true,"pageSize":15}},{"id":"chart-odeme","type":"chart","variant":"doughnut","title":"Ödeme Tipi Dağılımı","span":2,"result":"rs6","labelColumn":"OdemeTipi","datasets":[{"label":"Tutar","column":"Tutar"}]}]}]}'
    );
END
GO
