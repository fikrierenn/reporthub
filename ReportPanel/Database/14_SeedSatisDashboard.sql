-- 14_SeedSatisDashboard.sql
-- DerinSIS Satış Dashboard — bkm.sp_SatisPano SP'sine göre yapılandırılmış
-- Önce DerinSISBkm'de sp_SatisPano.sql çalıştırılmış olmalı.

-- Data source: mevcut MAIN veya DerinSIS key'ini kullanın
-- Aşağıdaki INSERT'teki DataSourceKey değerini kontrol edin

DECLARE @ConfigJson NVARCHAR(MAX) = N'{
  "layout": "standard",
  "tabs": [
    {
      "title": "Genel Bakış",
      "components": [
        {
          "type": "kpi",
          "title": "Bugün Ciro",
          "resultSet": 0,
          "span": 1,
          "agg": "first",
          "column": "BugunCiro",
          "color": "blue",
          "icon": "fas fa-coins",
          "subtitle": "Günlük net satış"
        },
        {
          "type": "kpi",
          "title": "Ort. Sepet",
          "resultSet": 0,
          "span": 1,
          "agg": "first",
          "column": "OrtSepet",
          "color": "indigo",
          "icon": "fas fa-shopping-cart",
          "subtitle": "Fiş başına ortalama"
        },
        {
          "type": "kpi",
          "title": "Ay Kümüle",
          "resultSet": 0,
          "span": 1,
          "agg": "first",
          "column": "AyKumule",
          "color": "green",
          "icon": "fas fa-chart-line",
          "subtitle": "Ay başından bugüne"
        },
        {
          "type": "kpi",
          "title": "Yıllık Değişim",
          "resultSet": 0,
          "span": 1,
          "agg": "first",
          "column": "AyYillikDegisim",
          "color": "purple",
          "icon": "fas fa-percent",
          "subtitle": "Geçen yıl aynı döneme göre"
        },
        {
          "type": "chart",
          "title": "Son 15 Gün Ciro Trendi",
          "resultSet": 2,
          "span": 4,
          "chartType": "line",
          "labelColumn": "Tarih",
          "datasets": [
            {"column": "Ciro", "label": "Ciro (TL)", "color": "blue"}
          ]
        },
        {
          "type": "table",
          "title": "Mağaza Performansı (Bugün)",
          "resultSet": 1,
          "span": 2,
          "columns": [
            {"key": "Magaza", "label": "Mağaza", "align": "left"},
            {"key": "Ciro", "label": "Ciro", "align": "right", "color": "blue"},
            {"key": "Adet", "label": "Adet", "align": "right"},
            {"key": "FisSayisi", "label": "Fiş", "align": "right"},
            {"key": "OrtSepet", "label": "Ort. Sepet", "align": "right", "color": "indigo"},
            {"key": "ToplamIndirim", "label": "İndirim", "align": "right", "color": "red"}
          ],
          "clickDetail": true
        },
        {
          "type": "chart",
          "title": "Ödeme Dağılımı (Bugün)",
          "resultSet": 6,
          "span": 2,
          "chartType": "doughnut",
          "labelColumn": "OdemeTipi",
          "datasets": [
            {"column": "Tutar", "label": "Tutar", "color": "blue"}
          ]
        }
      ]
    },
    {
      "title": "Kategori & Ürün",
      "components": [
        {
          "type": "kpi",
          "title": "Bugün Satılan Ürün",
          "resultSet": 0,
          "span": 1,
          "agg": "first",
          "column": "BugunAdet",
          "color": "green",
          "icon": "fas fa-box",
          "subtitle": "Toplam adet"
        },
        {
          "type": "kpi",
          "title": "Fiş Sayısı",
          "resultSet": 0,
          "span": 1,
          "agg": "first",
          "column": "BugunFis",
          "color": "blue",
          "icon": "fas fa-receipt",
          "subtitle": "Bugünkü işlem sayısı"
        },
        {
          "type": "kpi",
          "title": "Dünkü Ciro",
          "resultSet": 0,
          "span": 1,
          "agg": "first",
          "column": "DunCiro",
          "color": "gray",
          "icon": "fas fa-arrow-left",
          "subtitle": "Karşılaştırma"
        },
        {
          "type": "kpi",
          "title": "Geçen Yıl Bugün",
          "resultSet": 0,
          "span": 1,
          "agg": "first",
          "column": "GecenYilBugun",
          "color": "purple",
          "icon": "fas fa-calendar",
          "subtitle": "Aynı gün geçen yıl"
        },
        {
          "type": "table",
          "title": "Kategori Bazlı Satışlar (Bu Ay)",
          "resultSet": 3,
          "span": 2,
          "columns": [
            {"key": "Kategori", "label": "Kategori", "align": "left"},
            {"key": "Ciro", "label": "Ciro", "align": "right", "color": "blue"},
            {"key": "Adet", "label": "Adet", "align": "right"},
            {"key": "CiroPay", "label": "Pay %", "align": "right", "color": "purple"}
          ],
          "clickDetail": false
        },
        {
          "type": "table",
          "title": "En Çok Satan 15 Ürün (Bu Ay)",
          "resultSet": 4,
          "span": 2,
          "columns": [
            {"key": "Urun", "label": "Ürün", "align": "left"},
            {"key": "Kategori", "label": "Kategori", "align": "left"},
            {"key": "Adet", "label": "Adet", "align": "right", "color": "green"},
            {"key": "Ciro", "label": "Ciro", "align": "right", "color": "blue"}
          ],
          "clickDetail": true
        }
      ]
    },
    {
      "title": "Saat & Ödeme",
      "components": [
        {
          "type": "chart",
          "title": "Saatlik Satış Dağılımı (Bugün)",
          "resultSet": 5,
          "span": 4,
          "chartType": "bar",
          "labelColumn": "Saat",
          "datasets": [
            {"column": "Ciro", "label": "Ciro (TL)", "color": "blue"},
            {"column": "MusteriSayisi", "label": "Müşteri", "color": "green"}
          ]
        },
        {
          "type": "table",
          "title": "Ödeme Tipi Detayı (Bugün)",
          "resultSet": 6,
          "span": 2,
          "columns": [
            {"key": "OdemeTipi", "label": "Ödeme Tipi", "align": "left"},
            {"key": "Tutar", "label": "Tutar", "align": "right", "color": "blue"}
          ],
          "clickDetail": false
        },
        {
          "type": "table",
          "title": "Saatlik Detay (Bugün)",
          "resultSet": 5,
          "span": 2,
          "columns": [
            {"key": "Saat", "label": "Saat", "align": "left"},
            {"key": "Ciro", "label": "Ciro", "align": "right", "color": "blue"},
            {"key": "MusteriSayisi", "label": "Müşteri", "align": "right", "color": "green"}
          ],
          "clickDetail": false
        }
      ]
    }
  ]
}';

-- Seed: Satış Dashboard rapor kaydı
INSERT INTO ReportCatalog (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
VALUES (
    N'Satış Pano',
    N'BKM Kitap · Mağaza Satış Dashboard — günlük ciro, mağaza performansı, kategori analizi, saatlik dağılım',
    'DER',
    'bkm.sp_SatisPano',
    '{"fields":[{"name":"Tarih","label":"Tarih","type":"date","required":false,"default":"today","help":"Boş bırakılırsa bugün"}]}',
    'admin',
    1,
    'dashboard',
    @ConfigJson
);

-- Rol ataması
INSERT INTO ReportAllowedRoles (ReportId, RoleId, CreatedAt)
SELECT r.ReportId, ro.RoleId, GETDATE()
FROM ReportCatalog r
CROSS JOIN Roles ro
WHERE r.Title = N'Satış Pano'
  AND ro.Name IN ('admin', 'muhasebe')
  AND NOT EXISTS (
    SELECT 1 FROM ReportAllowedRoles rar
    WHERE rar.ReportId = r.ReportId AND rar.RoleId = ro.RoleId
  );

/*
bkm.sp_SatisPano RESULT SET YAPISI:
====================================

RS_0  KPI tek satır
      Tarih, GunAdi, BugunCiro, BugunAdet, BugunFis, OrtSepet,
      DunCiro, AyKumule, AyAdet,
      GecenYilBugun, GecenYilAyKumule, GunYillikDegisim, AyYillikDegisim

RS_1  Mağaza bazlı bugünkü özet
      Magaza, Ciro, Adet, FisSayisi, OrtSepet, ToplamIndirim

RS_2  Son 15 gün ciro trendi
      Tarih, Ciro, Adet

RS_3  Kategori bazlı satışlar (bu ay)
      Kategori, Ciro, Adet, CiroPay

RS_4  En çok satan TOP 15 ürün (bu ay)
      Urun, Kategori, Adet, Ciro

RS_5  Saatlik satış dağılımı (bugün)
      Saat, Ciro, MusteriSayisi

RS_6  Ödeme tipi dağılımı (bugün)
      OdemeTipi, Tutar
*/
