-- 46_SeedIkReports.sql
-- Plan 18A: İK Quick Reports + Dashboard
-- 6 rapor: İK Pano + Personel Listesi + Yeni Başlayanlar + İşten Ayrılanlar + Mağaza Yoğunluğu + Kıdem Dağılımı
-- DataSource: IK (mevcut, BKM_GENEL bağlantılı)
-- Roller: admin + ik
-- FilterDefinition sube/IK: IsActive=0 (ilerisi için hazır)

-- =========================================================================
-- 1. İK Pano (sp_IkPano)
-- =========================================================================
DECLARE @IkPanoConfig NVARCHAR(MAX) = N'{
  "schemaVersion": 2,
  "resultContract": {
    "kpi":             { "resultSet": 0, "required": true,  "shape": "row"   },
    "sube":            { "resultSet": 1, "required": true,  "shape": "table" },
    "departman":       { "resultSet": 2, "required": true,  "shape": "table" },
    "yeniBaslayanlar": { "resultSet": 3, "required": true,  "shape": "table" },
    "kidemGrubu":      { "resultSet": 4, "required": true,  "shape": "table" }
  },
  "tabs": [{
    "title": "Genel",
    "components": [
      { "type": "kpi", "result": "kpi", "span": 1, "column": "ToplamAktif",  "agg": "first", "color": "blue",   "icon": "fas fa-users",        "title": "Toplam Aktif",      "subtitle": "Tüm firmalar" },
      { "type": "kpi", "result": "kpi", "span": 1, "column": "Yeni30",       "agg": "first", "color": "green",  "icon": "fas fa-user-plus",    "title": "Yeni (30 Gün)",     "subtitle": "Son 30 gün" },
      { "type": "kpi", "result": "kpi", "span": 1, "column": "Ayrilan30",    "agg": "first", "color": "red",    "icon": "fas fa-user-minus",   "title": "Ayrılan (30 Gün)", "subtitle": "Son 30 gün" },
      { "type": "kpi", "result": "kpi", "span": 1, "column": "OrtKidemYil", "agg": "first", "color": "purple", "icon": "fas fa-calendar-alt", "title": "Ort. Kıdem (Yıl)", "subtitle": "Aktif personel" },
      { "type": "kpi", "result": "kpi", "span": 1, "column": "Kidem5Plus",  "agg": "first", "color": "indigo", "icon": "fas fa-award",        "title": "5+ Yıl Kıdemli",   "subtitle": "Aktif personel" },
      { "type": "kpi", "result": "kpi", "span": 1, "column": "Kidem10Plus", "agg": "first", "color": "orange", "icon": "fas fa-star",         "title": "10+ Yıl Kıdemli",  "subtitle": "Aktif personel" },
      { "type": "chart", "result": "sube",      "span": 2, "variant": "doughnut", "labelColumn": "AltLokasyon", "datasets": [{"label": "Personel", "column": "PersonelSayisi"}], "title": "Şube Dağılımı" },
      { "type": "chart", "result": "kidemGrubu","span": 2, "variant": "bar",      "labelColumn": "KidemGrubu",  "datasets": [{"label": "Personel", "column": "PersonelSayisi"}], "title": "Kıdem Dağılımı" },
      { "type": "table", "result": "yeniBaslayanlar", "span": 4, "title": "Son Başlayanlar", "tableOptions": { "stripe": true, "stickyHeader": true, "pageSize": 10 } }
    ]
  }]
}';

IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_IkPano')
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'İK Pano',
        N'İnsan Kaynakları yönetim panosu — aktif personel KPI, kıdem, şube ve departman dağılımı, son başlayanlar',
        N'IK', N'dbo.sp_IkPano', N'{"fields":[]}', N'admin', 1, N'dashboard', @IkPanoConfig
    );
ELSE
    UPDATE dbo.ReportCatalog
    SET DashboardConfigJson = @IkPanoConfig
    WHERE ProcName = N'dbo.sp_IkPano' AND DashboardConfigJson IS NULL;
GO

-- =========================================================================
-- 2. Personel Listesi (sp_IkPersonelListesi)
-- =========================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_IkPersonelListesi')
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'İK Personel Listesi',
        N'Aktif personel listesi — tüm firmalar, şube ve departman bazlı',
        N'IK', N'dbo.sp_IkPersonelListesi', N'{"fields":[]}', N'admin', 1, N'dashboard',
        N'{
  "schemaVersion": 2,
  "resultContract": { "liste": { "resultSet": 0, "required": true, "shape": "table" } },
  "tabs": [{ "title": "Personel", "components": [
    { "type": "table", "result": "liste", "span": 4, "tableOptions": { "stripe": true, "stickyHeader": true, "clientSearch": true, "pageSize": 50 } }
  ]}]
}'
    );
GO

-- =========================================================================
-- 3. Yeni Başlayanlar (sp_IkYeniBaslayanlar)
-- =========================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_IkYeniBaslayanlar')
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'Yeni Başlayanlar',
        N'Belirtilen gün sayısı içinde işe giren personel ve önceki dönem karşılaştırması',
        N'IK', N'dbo.sp_IkYeniBaslayanlar',
        N'{"fields":[{"name":"Days","label":"Gün Sayısı","type":"number","required":false,"default":"30","help":"30, 60 veya 90 gün"}]}',
        N'admin', 1, N'dashboard',
        N'{
  "schemaVersion": 2,
  "resultContract": {
    "kpi":   { "resultSet": 0, "required": true, "shape": "row"   },
    "detay": { "resultSet": 1, "required": true, "shape": "table" }
  },
  "tabs": [{ "title": "Genel", "components": [
    { "type": "kpi", "result": "kpi", "span": 1, "column": "BuDonem",      "agg": "first", "color": "green",  "icon": "fas fa-user-plus",  "title": "Bu Dönem",     "subtitle": "Yeni başlayan" },
    { "type": "kpi", "result": "kpi", "span": 1, "column": "OncekiDonem", "agg": "first", "color": "gray",   "icon": "fas fa-user",       "title": "Önceki Dönem", "subtitle": "Karşılaştırma" },
    { "type": "kpi", "result": "kpi", "span": 1, "column": "DegisimYuzde","agg": "first", "color": "blue",   "icon": "fas fa-chart-line", "title": "Değişim %",    "subtitle": "Önceki döneme göre" },
    { "type": "table", "result": "detay", "span": 4, "tableOptions": { "stripe": true, "stickyHeader": true, "clientSearch": true, "pageSize": 20 } }
  ]}]
}'
    );
GO

-- =========================================================================
-- 4. İşten Ayrılanlar (sp_IkIstenAyrilanlar)
-- =========================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_IkIstenAyrilanlar')
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'İşten Ayrılanlar',
        N'Belirtilen tarih aralığında işten ayrılan personel listesi ve ayrılma kodu dağılımı',
        N'IK', N'dbo.sp_IkIstenAyrilanlar',
        N'{"fields":[
            {"name":"StartDate","label":"Başlangıç Tarihi","type":"date","required":false,"help":"Boş = ay başı"},
            {"name":"EndDate","label":"Bitiş Tarihi","type":"date","required":false,"help":"Boş = bugün"}
        ]}',
        N'admin', 1, N'dashboard',
        N'{
  "schemaVersion": 2,
  "resultContract": {
    "kpi":   { "resultSet": 0, "required": true, "shape": "row"   },
    "detay": { "resultSet": 1, "required": true, "shape": "table" },
    "kodlar":{ "resultSet": 2, "required": true, "shape": "table" }
  },
  "tabs": [{ "title": "Genel", "components": [
    { "type": "kpi", "result": "kpi", "span": 1, "column": "ToplamAyrilan","agg": "first", "color": "red",    "icon": "fas fa-user-minus",   "title": "Toplam Ayrılan",    "subtitle": "Seçili dönem" },
    { "type": "kpi", "result": "kpi", "span": 1, "column": "OrtKidemYil", "agg": "first", "color": "purple", "icon": "fas fa-calendar-alt", "title": "Ort. Kıdem (Yıl)", "subtitle": "Ayrılanların kıdemi" },
    { "type": "chart", "result": "kodlar", "span": 2, "variant": "pie", "labelColumn": "AyrilmaKodu", "datasets": [{"label": "Kişi", "column": "Sayi"}], "title": "Ayrılma Kodu Dağılımı" },
    { "type": "table", "result": "detay", "span": 4, "tableOptions": { "stripe": true, "stickyHeader": true, "clientSearch": true, "pageSize": 20 } }
  ]}]
}'
    );
GO

-- =========================================================================
-- 5. Mağaza Yoğunluğu (sp_IkMagazaYogunlugu)
-- =========================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_IkMagazaYogunlugu')
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'Mağaza Personel Yoğunluğu',
        N'Şube ve departman bazlı aktif personel dağılımı ve kıdem analizi',
        N'IK', N'dbo.sp_IkMagazaYogunlugu', N'{"fields":[]}', N'admin', 1, N'dashboard',
        N'{
  "schemaVersion": 2,
  "resultContract": {
    "kpi":       { "resultSet": 0, "required": true, "shape": "row"   },
    "sube":      { "resultSet": 1, "required": true, "shape": "table" },
    "departman": { "resultSet": 2, "required": true, "shape": "table" }
  },
  "tabs": [{ "title": "Şube Analizi", "components": [
    { "type": "kpi", "result": "kpi", "span": 1, "column": "ToplamAktif",  "agg": "first", "color": "blue",   "icon": "fas fa-users",        "title": "Toplam Aktif",     "subtitle": "Tüm şubeler" },
    { "type": "kpi", "result": "kpi", "span": 1, "column": "SubeKayisi",   "agg": "first", "color": "indigo", "icon": "fas fa-store",        "title": "Şube Sayısı",      "subtitle": "Aktif şubeler" },
    { "type": "kpi", "result": "kpi", "span": 1, "column": "OrtKidemYil", "agg": "first", "color": "purple", "icon": "fas fa-calendar-alt", "title": "Ort. Kıdem (Yıl)", "subtitle": "Tüm personel" },
    { "type": "chart", "result": "departman", "span": 1, "variant": "doughnut", "labelColumn": "Departman", "datasets": [{"label": "Personel", "column": "PersonelSayisi"}], "title": "Departman Dağılımı" },
    { "type": "table", "result": "sube", "span": 4, "tableOptions": { "stripe": true, "stickyHeader": true, "pageSize": 20 } }
  ]}]
}'
    );
GO

-- =========================================================================
-- 6. Kıdem Dağılımı (sp_IkKidemDagilimi)
-- =========================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_IkKidemDagilimi')
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'Kıdem Dağılımı',
        N'Aktif personelin kıdem grubu, şube bazlı ortalama kıdem ve kıdemli personel analizi',
        N'IK', N'dbo.sp_IkKidemDagilimi', N'{"fields":[]}', N'admin', 1, N'dashboard',
        N'{
  "schemaVersion": 2,
  "resultContract": {
    "kpi":        { "resultSet": 0, "required": true, "shape": "row"   },
    "kidemGrubu": { "resultSet": 1, "required": true, "shape": "table" },
    "sube":       { "resultSet": 2, "required": true, "shape": "table" }
  },
  "tabs": [{ "title": "Kıdem Analizi", "components": [
    { "type": "kpi", "result": "kpi", "span": 1, "column": "OrtKidemYil",  "agg": "first", "color": "purple", "icon": "fas fa-calendar-alt", "title": "Ort. Kıdem (Yıl)",  "subtitle": "Aktif personel" },
    { "type": "kpi", "result": "kpi", "span": 1, "column": "MaxKidemYil",  "agg": "first", "color": "indigo", "icon": "fas fa-trophy",       "title": "En Uzun Kıdem",     "subtitle": "Aktif personel" },
    { "type": "kpi", "result": "kpi", "span": 1, "column": "Kidem5Plus",   "agg": "first", "color": "blue",   "icon": "fas fa-award",        "title": "5+ Yıl Kıdemli",    "subtitle": "Deneyimli personel" },
    { "type": "kpi", "result": "kpi", "span": 1, "column": "Kidem10Plus",  "agg": "first", "color": "orange", "icon": "fas fa-star",         "title": "10+ Yıl Kıdemli",   "subtitle": "Çok deneyimli" },
    { "type": "chart", "result": "kidemGrubu", "span": 4, "variant": "bar", "labelColumn": "KidemGrubu", "datasets": [{"label": "Personel Sayısı", "column": "PersonelSayisi"}], "title": "Kıdem Grubu Dağılımı" },
    { "type": "table", "result": "sube", "span": 4, "tableOptions": { "stripe": true, "stickyHeader": true, "pageSize": 20 } }
  ]}]
}'
    );
GO

-- =========================================================================
-- 7. Rol ataması — admin + ik (tüm 6 rapor)
-- =========================================================================
INSERT INTO dbo.ReportAllowedRoles (ReportId, RoleId, CreatedAt)
SELECT rc.ReportId, ro.RoleId, GETUTCDATE()
FROM dbo.ReportCatalog rc
CROSS JOIN dbo.Roles ro
WHERE rc.ProcName IN (
    N'dbo.sp_IkPano',
    N'dbo.sp_IkPersonelListesi',
    N'dbo.sp_IkYeniBaslayanlar',
    N'dbo.sp_IkIstenAyrilanlar',
    N'dbo.sp_IkMagazaYogunlugu',
    N'dbo.sp_IkKidemDagilimi'
)
  AND ro.Name IN (N'admin', N'ik')
  AND NOT EXISTS (
    SELECT 1 FROM dbo.ReportAllowedRoles rar
    WHERE rar.ReportId = rc.ReportId AND rar.RoleId = ro.RoleId
  );
GO

-- =========================================================================
-- 8. FilterDefinition sube/IK (IsActive=0 — ilerisi için hazır)
-- =========================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.FilterDefinition WHERE FilterKey = N'sube' AND DataSourceKey = N'IK')
    INSERT INTO dbo.FilterDefinition
        (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder)
    VALUES (
        N'sube',
        N'Şube',
        N'spInjection',
        N'IK',
        N'SELECT AltLokasyon AS Value, AltLokasyon AS Label FROM dbo.vw_PersonelDepartman WHERE Ict IS NULL GROUP BY AltLokasyon',
        0,
        1
    );
GO
