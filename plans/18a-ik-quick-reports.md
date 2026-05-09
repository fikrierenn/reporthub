# Plan 18A — İK Quick Reports + Dashboard

**Tarih:** 2026-05-12  
**Durum:** Implement  
**Tier:** 3 (yeni SP + seed + FilterDef + dashboard config)

## Problem

BKM İK verisi Zirve (BKM_GENEL) DB'de `vw_PersonelDepartman` view'ında mevcut ama Mosaik'te hiç rapor yok. Yönetim, İK departmanı, şube müdürleri personel durumunu göremez.

## Scope

- 4 rapor + 1 pano, toplam 5 SP (BKM_GENEL / dbo)
- Mosaik ReportCatalog seed (Migration 46)
- FilterDefinition sube/IK (IsActive=0, ilerisi için hazır)
- Roller: admin + ik
- Firma: tüm firmalar (BKM_GENEL + Bursa Kültür + Şura)
- DataSource: IK (mevcut, BKM_GENEL bağlantılı)

## SP Listesi

| SP | Açıklama | RS Sayısı | Parametreler |
|---|---|---|---|
| `dbo.sp_IkPano` | Ana pano — KPI + şube pie + departman bar + son başlayanlar | 4 | — |
| `dbo.sp_IkPersonelListesi` | Aktif personel listesi | 1 | @sube_Filtre (injected) |
| `dbo.sp_IkYeniBaslayanlar` | Yeni başlayanlar | 2 | @Days INT=30 |
| `dbo.sp_IkIstenAyrilanlar` | İşten ayrılanlar | 3 | @StartDate, @EndDate |
| `dbo.sp_IkMagazaYogunlugu` | Mağaza × departman pivot | 3 | — |

## Done Criteria

- [x] Plan yazıldı
- [ ] 5 SP BKM_GENEL'de mevcut (CREATE OR ALTER)
- [ ] Migration 46 Mosaik DB'ye uygulandı
- [ ] 5 rapor Mosaik UI'da görünüyor (admin + ik rolü)
- [ ] İK Pano dashboard render ediyor (KPI + pie + tablo)
- [ ] Build yeşil

## Rollback

Migration 46 idempotent — `IF NOT EXISTS` + `WHERE DashboardConfigJson IS NULL`. Geri almak için `ReportCatalog` kayıtlarını `IsActive=0` yapılır, SP'ler kaldırılır (BKM_GENEL'e dokunmak gerekir).
