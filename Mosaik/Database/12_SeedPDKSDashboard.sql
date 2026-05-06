-- 12_SeedPDKSDashboard.sql
-- BKM PDKS Dashboard — dbo.sp_PdksPano SP'sine göre yapılandırılmış
-- Önce 10 ve 11 numaralı migration'lar çalıştırılmış olmalı.

DECLARE @ConfigJson NVARCHAR(MAX) = N'{
  "layout": "standard",
  "tabs": [
    {
      "title": "Özet",
      "components": [
        {
          "type": "kpi",
          "title": "Toplam Kadro",
          "resultSet": 2,
          "span": 1,
          "agg": "first",
          "column": "KadroToplam",
          "color": "blue",
          "icon": "fas fa-users",
          "subtitle": "Aktif personel (tüm birimler)"
        },
        {
          "type": "kpi",
          "title": "Kadro Katılımı",
          "resultSet": 2,
          "span": 1,
          "agg": "first",
          "column": "KadroGelen",
          "color": "green",
          "icon": "fas fa-check-circle",
          "subtitle": "Giriş okutmuş personel"
        },
        {
          "type": "kpi",
          "title": "Vardiya Planı",
          "resultSet": 2,
          "span": 1,
          "agg": "first",
          "column": "PlanToplam",
          "color": "indigo",
          "icon": "fas fa-calendar",
          "subtitle": "Bugün planlı kişi sayısı"
        },
        {
          "type": "kpi",
          "title": "Plana Göre Geldi",
          "resultSet": 2,
          "span": 1,
          "agg": "first",
          "column": "PlanGeldi",
          "color": "green",
          "icon": "fas fa-user-check",
          "subtitle": "Vardiya planında olup gelen"
        },
        {
          "type": "chart",
          "title": "Şube Bazlı Plan vs Fiili",
          "resultSet": 1,
          "span": 4,
          "chartType": "bar",
          "labelColumn": "Sube",
          "datasets": [
            {"column": "CalismaPlan", "label": "Plan", "color": "blue"},
            {"column": "Geldi", "label": "Geldi", "color": "green"},
            {"column": "Gelmedi", "label": "Gelmedi", "color": "red"}
          ]
        },
        {
          "type": "table",
          "title": "Şube Özet",
          "resultSet": 1,
          "span": 2,
          "columns": [
            {"key": "Sube", "label": "Mağaza", "align": "left"},
            {"key": "CalismaPlan", "label": "Plan", "align": "right"},
            {"key": "Geldi", "label": "Geldi", "align": "right", "color": "green"},
            {"key": "Gelmedi", "label": "Gelmedi", "align": "right", "color": "red"},
            {"key": "Izinli", "label": "İzinli", "align": "right", "color": "yellow"},
            {"key": "SuAnIceride", "label": "İçeride", "align": "right", "color": "blue"}
          ],
          "clickDetail": true
        },
        {
          "type": "table",
          "title": "Bölüm Doluluk (Tüm Kadro)",
          "resultSet": 5,
          "span": 2,
          "columns": [
            {"key": "Bolum", "label": "Bölüm", "align": "left"},
            {"key": "Kadro", "label": "Kadro", "align": "right"},
            {"key": "Gelen", "label": "Gelen", "align": "right", "color": "green"},
            {"key": "Izinli", "label": "İzinli", "align": "right", "color": "yellow"}
          ],
          "clickDetail": false
        }
      ]
    },
    {
      "title": "Vardiya Detay",
      "components": [
        {
          "type": "kpi",
          "title": "Plan Katılım %",
          "resultSet": 2,
          "span": 1,
          "agg": "first",
          "column": "PlanKatilimYuzde",
          "color": "green",
          "icon": "fas fa-percent",
          "subtitle": "Vardiyalı kadro katılım oranı"
        },
        {
          "type": "kpi",
          "title": "Plan Gelmedi",
          "resultSet": 2,
          "span": 1,
          "agg": "first",
          "column": "PlanGelmedi",
          "color": "red",
          "icon": "fas fa-times-circle",
          "subtitle": "Planda var, giriş yok"
        },
        {
          "type": "kpi",
          "title": "Kadro İzinli",
          "resultSet": 2,
          "span": 1,
          "agg": "first",
          "column": "KadroIzinli",
          "color": "yellow",
          "icon": "fas fa-calendar",
          "subtitle": "Rapor/yıllık/mazeret"
        },
        {
          "type": "kpi",
          "title": "Kadro Gelmedi",
          "resultSet": 2,
          "span": 1,
          "agg": "first",
          "column": "KadroGelmedi",
          "color": "red",
          "icon": "fas fa-exclamation-triangle",
          "subtitle": "Okutma yok, mazeret yok"
        },
        {
          "type": "table",
          "title": "Personel Vardiya Listesi",
          "resultSet": 0,
          "span": 4,
          "columns": [
            {"key": "Sube", "label": "Mağaza", "align": "left"},
            {"key": "Bolum", "label": "Bölüm", "align": "left"},
            {"key": "Personel", "label": "Personel", "align": "left"},
            {"key": "PlanBas", "label": "Plan Baş", "align": "right"},
            {"key": "PlanBit", "label": "Plan Bit", "align": "right"},
            {"key": "FiiliGiris", "label": "Giriş", "align": "right", "color": "green"},
            {"key": "FiiliCikis", "label": "Çıkış", "align": "right"},
            {"key": "BrutSure", "label": "Brüt", "align": "right"},
            {"key": "Durum", "label": "Durum", "align": "left"},
            {"key": "GirisSapmaDk", "label": "Sapma (dk)", "align": "right", "color": "red"}
          ],
          "clickDetail": true
        }
      ]
    },
    {
      "title": "Mesai & Sapma",
      "components": [
        {
          "type": "table",
          "title": "Fazla Mesai TOP 10 (Ay Başı → Bugün)",
          "resultSet": 3,
          "span": 2,
          "columns": [
            {"key": "Sicil", "label": "Sicil", "align": "left"},
            {"key": "AdSoyad", "label": "Ad Soyad", "align": "left"},
            {"key": "Bolum", "label": "Bölüm", "align": "left"},
            {"key": "Gun", "label": "Gün", "align": "right"},
            {"key": "FmSaat", "label": "FM Saat", "align": "right", "color": "green"}
          ],
          "clickDetail": true
        },
        {
          "type": "table",
          "title": "Geç Kalma TOP 10 (Ay Başı → Bugün)",
          "resultSet": 4,
          "span": 2,
          "columns": [
            {"key": "TC", "label": "TC", "align": "left"},
            {"key": "Personel", "label": "Ad Soyad", "align": "left"},
            {"key": "Sube", "label": "Mağaza", "align": "left"},
            {"key": "Gun", "label": "Gün", "align": "right"},
            {"key": "ToplamGecikmeDk", "label": "Gecikme (dk)", "align": "right", "color": "red"}
          ],
          "clickDetail": true
        },
        {
          "type": "table",
          "title": "Eksik Okutma (Dün — Giriş Var, Çıkış Yok)",
          "resultSet": 6,
          "span": 4,
          "columns": [
            {"key": "Sicil", "label": "Sicil", "align": "left"},
            {"key": "AdSoyad", "label": "Ad Soyad", "align": "left"},
            {"key": "Bolum", "label": "Bölüm", "align": "left"},
            {"key": "Giris", "label": "Giriş", "align": "right"},
            {"key": "Cikis", "label": "Çıkış", "align": "right", "color": "red"}
          ],
          "clickDetail": true
        }
      ]
    }
  ]
}';

-- Önce PDKS data source'u ekle (yoksa)
IF NOT EXISTS (SELECT 1 FROM DataSources WHERE DataSourceKey = 'PDKS')
BEGIN
    INSERT INTO DataSources (DataSourceKey, Title, ConnString, IsActive)
    VALUES ('PDKS', N'PDKS (GecoTime)', 'Server=192.168.40.201;Database=BKM;User Id=sa;Password=SIFRE_BURAYA;TrustServerCertificate=True;', 1);
END

-- Seed: PDKS Pano dashboard rapor kaydı
INSERT INTO ReportCatalog (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
VALUES (
    N'PDKS Pano',
    N'BKM Kitap · Personel Devam Kontrol Sistemi — günlük kadro katılımı, vardiya takibi, mesai & sapma analizi',
    'PDKS',
    'dbo.sp_PdksPano',
    '{"fields":[{"name":"Tarih","label":"Tarih","type":"date","required":false,"default":"today","placeholder":"gg.aa.yyyy","help":"Boş bırakılırsa bugün kullanılır"}]}',
    'admin',                             -- << izinli rolleri yazın
    1,
    'dashboard',
    @ConfigJson
);

/*
dbo.sp_PdksPano RESULT SET YAPISI:
====================================

RS_0  Plan-Fiili kişi detay (D dizisi)
      Sube, Bolum, Personel, TC, PlanVardiya, PlanBas, PlanBit,
      PlanDk, PlanNetDk, FiiliGiris, FiiliCikis, BrutSure,
      MazeretKod, IzinMi, OzelDurumMu, Durum, GirisSapmaDk, CikisSapmaDk

RS_1  Şube özet
      Sube, PlanSayisi, CalismaPlan, Geldi, Gelmedi, PdksYok,
      Izinli, OzelDurum, SuAnIceride

RS_2  KPI tek satır
      Tarih, GunAdi, HaftaBas,
      KadroToplam, KadroGelen, KadroIzinli, KadroGelmedi, KadroKatilimYuzde,
      PlanToplam, PlanGeldi, PlanGelmedi, PlanIzinli, PlanKatilimYuzde

RS_3  FM1 fazla mesai TOP N
      Sicil, AdSoyad, Bolum, Gun, FmSaat, FmDk

RS_4  Geç kalma TOP N
      TC, Personel, Sube, Gun, ToplamGecikmeDk

RS_5  Bölüm doluluk (tüm kadro)
      Bolum, Kadro, Gelen, Izinli

RS_6  Eksik okutma (dün)
      Sicil, AdSoyad, Bolum, Giris, Cikis
*/
