---
paths:
  - "ReportPanel/Database/**/*.sql"
---

# SQL + Stored Procedure Konvansiyonları

## Dosya Konvansiyonu

- **Migration:** `Database/NN_KisaAciklama.sql` — numaralı, sıralı. Mevcut: 01-14.
- **Stored Procedure:** `Database/sp_PascalCase.sql`.
- **Function (TVF):** `Database/fn_PascalCase.sql`.
- **Seed:** `Database/NN_SeedX.sql` veya `Database/Seed/` alt klasörü (ilerde).

## Idempotency — Zorunlu

Her migration **yeniden çalıştırıldığında patlamamalı**:

```sql
-- Yeni tablo
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Foo')
BEGIN
    CREATE TABLE dbo.Foo (...);
END
GO

-- Yeni kolon
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.User')
               AND name = 'Department')
BEGIN
    ALTER TABLE dbo.User ADD Department NVARCHAR(100) NULL;
END
GO

-- Yeni index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Foo_Bar')
BEGIN
    CREATE INDEX IX_Foo_Bar ON dbo.Foo(Bar);
END
GO
```

## Naming

| Obje | Kural | Örnek |
|---|---|---|
| Table | PascalCase, İngilizce, tekil | `User`, `Report`, `ReportFavorite` |
| Column | PascalCase, İngilizce | `UserId`, `CreatedAt`, `IsActive` |
| PK | `[table]Id` | `UserId`, `ReportId` |
| FK | `[referenced_table]Id` | `UserRole.UserId`, `UserRole.RoleId` |
| Index | `IX_[Table]_[Cols]` | `IX_User_Username` |
| SP | `sp_PascalCase` | `sp_PdksPano`, `sp_SatisPano` |
| TVF | `fn_PascalCase` | `fn_PdksKpiOzet`, `fn_PdksDetay` |
| View | `vw_PascalCase` | `vw_ActiveUsers` |
| Parameter | `@PascalCase` İngilizce tercih | `@StartDate` (önerilen), `@Tarih` (legacy sp_PdksPano) |

**Not:** SP parametrelerinde tutarsızlık var — `sp_PdksPano` Türkçe (`@Tarih`), `sp_SatisPano` İngilizce. Yeni SP'lerde **İngilizce**.

## Stored Procedure Şablonu

```sql
CREATE OR ALTER PROCEDURE dbo.sp_XyzReport
    @StartDate DATE,
    @EndDate DATE = NULL  -- default
AS
BEGIN
    SET NOCOUNT ON;

    IF @EndDate IS NULL SET @EndDate = GETDATE();

    SELECT ...
    FROM ...
    WHERE DateField BETWEEN @StartDate AND @EndDate;
END
GO
```

- `SET NOCOUNT ON` zorunlu — ExecuteReader gereksiz "N rows affected" mesajlarıyla kirlenmesin.
- `CREATE OR ALTER` (SQL Server 2016+) — idempotency için.
- Default parametre değeri verilebilir.
- NULL kabul etmeyen parametre → NULL gelirse fail değil, default ata (SpPreview için kritik).

## Inline TVF (reuse için)

```sql
CREATE OR ALTER FUNCTION dbo.fn_PdksKpiOzet(@Tarih DATE)
RETURNS TABLE
AS RETURN
(
    SELECT
        COUNT(*) AS ToplamPersonel,
        SUM(CASE WHEN FiiliGiris IS NULL THEN 1 ELSE 0 END) AS EksikGiris
    FROM ...
    WHERE Tarih = @Tarih
);
GO
```

- **Inline TVF** (`RETURNS TABLE AS RETURN (...)`) — performans için birincil.
- **Multi-statement TVF** (`RETURNS @t TABLE ... BEGIN ... END`) — **yasak**, optimizer'i kör eder.
- Dashboard SP'si TVF'leri çağırır (orkestratör), iş TVF'de yapılır.

## Multi-result-set SP

Dashboard SP'leri birden fazla `SELECT` döndürür:

```sql
CREATE OR ALTER PROCEDURE dbo.sp_PdksPano
    @Tarih DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM fn_PdksDetay(@Tarih);         -- RS 0
    SELECT * FROM fn_PdksKpiOzet(@Tarih);       -- RS 1
    SELECT * FROM fn_PdksDepartmanKirilim(@Tarih);  -- RS 2
END
GO
```

`DashboardConfig.json` içinde `resultSet: 0, 1, 2, ...` index'leri buna denk gelir.

## Güvenlik

- **Dinamik SQL yasak** (`EXEC(@sql)` + user input). Tek istisna: admin'in tanımlı SP listesinden seçtiği `procName`.
- **STRING_SPLIT** user input parse için — parametreli güvenli.
- **SP grant:** DB user'a sadece spesifik SP'lere `EXEC` izni ver. `SELECT` direkt tabloya verilmez.

## İdempotent Örnek (referans)

`Database/13_CreateUserDataFilter.sql` — iyi örnek pattern. Yeni migration'larda bunu kopyala.
