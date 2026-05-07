---
name: sql-migration-writer
description: Mosaik için idempotent SQL migration yazar (Database/NN_*.sql). CREATE TABLE / ALTER / data backfill / SP CREATE OR ALTER pattern'lerini takip eder. Yedek almadan silme YASAK kuralını uygular, migration zincirini ezme riskini önler.
---

# SQL Migration Writer

## Ne zaman tetiklenir

- "Yeni migration yaz X için"
- "Tablo X'e Y kolonu ekle (migration ile)"
- "FilterDefinition seed güncelle"
- "SP'ye CREATE OR ALTER yaz"
- Plan 14 Faz B/C, Plan 16.5 Faz A/B/C migration ihtiyaçları
- vNext modül scaffold sırasında EF migration

## Önkoşullar (her zaman)

1. **Migration zincirini oku** (memory: `feedback_migration_chain_oku.md`):
   ```
   Glob Database/*.sql
   Grep <hedef_tablo> Database/
   ```
   Mevcut migration'lar V1→V2 chain'i göster, en son state nedir.

2. **UPDATE öncesi SELECT + WHERE zorunlu** (memory: `feedback_db_update_where_zorunlu.md`):
   - Önce `SELECT` ile mevcut state'i oku
   - `UPDATE` daima `WHERE` ile, asla bare update
   - PDKS Pano contract ezme felaketi tekrarı yasak

3. **Yedek almadan silme YASAK** (memory: `feedback_yedek_almadan_silme_yok.md`):
   - DROP DATABASE/TABLE, TRUNCATE, kapsamlı DELETE öncesi BACKUP zorunlu
   - Kullanıcı "yap" dese bile sor + doğrula
   - Plan 11 reset felaket riskinden kıl payı kurtulduk

4. **Sıradaki migration numarası**:
   ```bash
   ls Database/*.sql | grep -oE '^Database/[0-9]+' | sort -t/ -k2 -n | tail -1
   ```

## Şablon kütüphanesi

### Şablon 1 — Yeni tablo (idempotent)

```sql
-- Migration NN: <Açıklama>
-- Tarih: YYYY-MM-DD
-- Plan: <plans/NN-...>
-- Idempotent: çoklu çalıştırma güvenli

SET NOCOUNT ON;
GO

-- Tablo oluştur (idempotent)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TabloAdi' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TabloAdi (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TabloAdi PRIMARY KEY,
        Ad NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TabloAdi_IsActive DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TabloAdi_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy NVARCHAR(100) NULL,
        UpdatedAt DATETIME2(0) NULL,
        UpdatedBy NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.TabloAdi oluşturuldu.';
END
ELSE
    PRINT 'Tablo dbo.TabloAdi zaten mevcut, atlandı.';
GO

-- Index'ler (idempotent)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TabloAdi_IsActive')
    CREATE NONCLUSTERED INDEX IX_TabloAdi_IsActive ON dbo.TabloAdi(IsActive) WHERE IsActive = 1;
GO
```

**Kurallar:**
- Constraint adı pattern: `PK_<Tablo>`, `FK_<Cocuk>_<Ata>`, `DF_<Tablo>_<Kolon>`, `IX_<Tablo>_<Kolon>`
- `GETUTCDATE()` (Migration 25 sonrası standart)
- `DATETIME2(0)` — saniye hassasiyeti yeterli, ms gereksiz
- `NVARCHAR` (Türkçe karakter desteği)
- Filtered index: `IsActive=1` gibi sık sorgulanan subset için

### Şablon 2 — Kolon ekleme

```sql
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TabloAdi') AND name = 'YeniKolon')
BEGIN
    ALTER TABLE dbo.TabloAdi ADD YeniKolon NVARCHAR(100) NULL;
    PRINT 'YeniKolon eklendi.';
END
GO

-- Backfill (gerekirse)
UPDATE dbo.TabloAdi SET YeniKolon = '*' WHERE YeniKolon IS NULL AND IsActive = 1;
GO

-- NOT NULL constraint sonradan (backfill sonrası)
-- ALTER TABLE dbo.TabloAdi ALTER COLUMN YeniKolon NVARCHAR(100) NOT NULL;
```

### Şablon 3 — Data UPDATE (WHERE zorunlu)

```sql
-- ÖNCE: mevcut state'i SELECT ile oku
SELECT TOP 10 Id, Kolon FROM dbo.TabloAdi WHERE <kosul>;

-- UPDATE: WHERE clause olmadan ASLA
UPDATE dbo.TabloAdi
   SET Kolon = 'yeni_deger'
 WHERE <kosul_aynisi>
   AND Kolon <> 'yeni_deger';  -- idempotency

-- DOĞRULA: rakam beklediğin gibi mi
SELECT @@ROWCOUNT AS Etkilenen;
```

### Şablon 4 — SP CREATE OR ALTER

```sql
-- CREATE OR ALTER (SQL Server 2016+) — alter mi create mi sormaz
CREATE OR ALTER PROCEDURE bkm.sp_AdimDuzelt
    @Tarih DATETIME = NULL,
    @FmTop INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SET @Tarih = ISNULL(@Tarih, GETDATE());
    -- ... body
END
GO
```

**Kritik:** Tüm parametreler için `DEFAULT` ver (NULL kabul edilebilir tipte). Memory `feedback_paramschema_sp_eksiklik.md`: `sys.parameters.has_default_value` yanıltıcı, source'a bak.

### Şablon 5 — FilterDefinition seed (Mosaik özel)

```sql
-- Idempotent FilterDefinition insert/update
DECLARE @FilterKey NVARCHAR(50) = 'sube';
DECLARE @DataSourceKey NVARCHAR(50) = 'IK';

IF NOT EXISTS (SELECT * FROM dbo.FilterDefinitions WHERE FilterKey = @FilterKey AND DataSourceKey = @DataSourceKey)
BEGIN
    INSERT INTO dbo.FilterDefinitions (FilterKey, DataSourceKey, Label, OptionsQuery, IsActive, DisplayOrder, CreatedAt)
    VALUES (@FilterKey, @DataSourceKey, 'IK Şube', N'SELECT Id = SubeId, Label = SubeAdi FROM BKM_GENEL.dbo.SubeListe WHERE IsActive = 1 ORDER BY SubeAdi', 1, 10, GETUTCDATE());
END
ELSE
BEGIN
    UPDATE dbo.FilterDefinitions
       SET OptionsQuery = N'SELECT Id = SubeId, Label = SubeAdi FROM BKM_GENEL.dbo.SubeListe WHERE IsActive = 1 ORDER BY SubeAdi',
           IsActive = 1
     WHERE FilterKey = @FilterKey AND DataSourceKey = @DataSourceKey;
END
GO
```

## Test edilecekler (her migration için)

1. **Çoklu çalıştırma**: aynı migration 2-3 kez çalıştır → hata yok, state aynı
2. **Rollback senaryosu**: production'da geri almak nasıl, dosyada belirt
3. **Diğer DB'lere etki**: cross-DB join varsa allowlist kontrol
4. **Plan referansı**: commit mesajında `(plan: NN)` zorunlu (Tier 3 için)

## Yaygın hatalar (KAÇIN)

- ❌ `WHERE` olmadan UPDATE (PDKS Pano contract ezme felaketi)
- ❌ `DROP TABLE` öncesi BACKUP yok
- ❌ Constraint adı varsayılan (sistem üretsin) — `DF__TabloAdi__YeniKol__1234ABCD` gibi guid'li, tekrarlanabilir değil
- ❌ `GETDATE()` kullanma — `GETUTCDATE()` standart (Migration 25)
- ❌ `VARCHAR` Türkçe kolon — `NVARCHAR` zorunlu
- ❌ Existing migration'ı güncelle — daima yeni dosya
- ❌ `sys.parameters` SP default sorgusu — yanıltıcı, source'a bak

## İlişkili

- `.claude/rules/sql-conventions.md`
- `memory/feedback_db_update_where_zorunlu.md`
- `memory/feedback_yedek_almadan_silme_yok.md`
- `memory/feedback_migration_chain_oku.md`
- `memory/feedback_paramschema_sp_eksiklik.md`
- `memory/project_dashboard_config_named_contract.md`
- ADR-001 (data-access)

## Tools

Write, Edit, Read, Glob, Grep, `mcp__sqlserver__*` (schema doğrulama).
