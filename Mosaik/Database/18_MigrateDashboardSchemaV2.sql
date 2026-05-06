-- 18_MigrateDashboardSchemaV2.sql
-- M-11 F-1 · Idempotent migration: schemaVersion 1 -> 2 + ReportType='table' -> dashboard.
-- ADR-008 (schema v2) + ADR-009 (report type consolidation).
--
-- ADIM A: v1 dashboard configleri v2'ye cevir (variant + numberFormat + axisOptions + tableOptions + calculatedFields default).
-- ADIM B: ReportType='table' AND DashboardConfigJson IS NULL olan raporlara tek-table-widget dashboard config yaz.
-- ADIM C: Her UPDATE icin AuditLog entry (eski JSON OldValuesJson'da).
--
-- Idempotent: schemaVersion>=2 olanlar SKIP, zaten config'i olan table raporlari SKIP.
-- Pre-migration: Database/backup/YYYYMMDD_pre_m11.sql backup'i ZORUNLU (bkz. backup/README.md).
-- Rollback: backup/*.sql UPDATE betiklerini transaction icinde calistir.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @MigratedV2 INT = 0;
    DECLARE @ConvertedTable INT = 0;

    -- ============================================================
    -- ADIM A: v1 -> v2 schema migration
    -- ============================================================
    DECLARE @ReportId INT;
    DECLARE @OldJson NVARCHAR(MAX);
    DECLARE @NewJson NVARCHAR(MAX);
    DECLARE @TabIdx INT;
    DECLARE @CompIdx INT;
    DECLARE @TabCount INT;
    DECLARE @CompCount INT;
    DECLARE @CompType NVARCHAR(20);
    DECLARE @CompChartType NVARCHAR(20);
    DECLARE @CompPath NVARCHAR(200);

    DECLARE cfg_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT ReportId, DashboardConfigJson
        FROM dbo.ReportCatalog
        WHERE DashboardConfigJson IS NOT NULL
          AND ISJSON(DashboardConfigJson) = 1
          AND ISNULL(TRY_CAST(JSON_VALUE(DashboardConfigJson, '$.schemaVersion') AS INT), 1) < 2;

    OPEN cfg_cursor;
    FETCH NEXT FROM cfg_cursor INTO @ReportId, @OldJson;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @NewJson = @OldJson;
        SET @NewJson = JSON_MODIFY(@NewJson, '$.schemaVersion', 2);

        SET @TabCount = (SELECT COUNT(*) FROM OPENJSON(@NewJson, '$.tabs'));
        SET @TabIdx = 0;

        WHILE @TabIdx < @TabCount
        BEGIN
            SET @CompCount = (SELECT COUNT(*) FROM OPENJSON(@NewJson, '$.tabs[' + CAST(@TabIdx AS VARCHAR) + '].components'));
            SET @CompIdx = 0;

            WHILE @CompIdx < @CompCount
            BEGIN
                SET @CompPath = '$.tabs[' + CAST(@TabIdx AS VARCHAR) + '].components[' + CAST(@CompIdx AS VARCHAR) + ']';
                SET @CompType = JSON_VALUE(@NewJson, @CompPath + '.type');
                SET @CompChartType = JSON_VALUE(@NewJson, @CompPath + '.chartType');

                -- variant: kpi -> 'basic', chart -> chartType degeri (fallback 'bar')
                IF @CompType = 'kpi' AND JSON_VALUE(@NewJson, @CompPath + '.variant') IS NULL
                    SET @NewJson = JSON_MODIFY(@NewJson, @CompPath + '.variant', 'basic');
                ELSE IF @CompType = 'chart' AND JSON_VALUE(@NewJson, @CompPath + '.variant') IS NULL
                    SET @NewJson = JSON_MODIFY(@NewJson, @CompPath + '.variant', ISNULL(@CompChartType, 'bar'));

                -- numberFormat default (kpi + chart)
                IF @CompType IN ('kpi', 'chart') AND JSON_VALUE(@NewJson, @CompPath + '.numberFormat') IS NULL
                    SET @NewJson = JSON_MODIFY(@NewJson, @CompPath + '.numberFormat', 'auto');

                -- axisOptions default (chart)
                IF @CompType = 'chart' AND JSON_QUERY(@NewJson, @CompPath + '.axisOptions') IS NULL
                    SET @NewJson = JSON_MODIFY(@NewJson, @CompPath + '.axisOptions',
                        JSON_QUERY('{"showLegend":true,"showGrid":true,"beginAtZero":false,"tooltip":true,"dataLabels":false,"smooth":true}'));

                -- tableOptions default (table)
                IF @CompType = 'table' AND JSON_QUERY(@NewJson, @CompPath + '.tableOptions') IS NULL
                    SET @NewJson = JSON_MODIFY(@NewJson, @CompPath + '.tableOptions',
                        JSON_QUERY('{"totalRow":false,"stripe":true,"stickyHeader":true,"clientSearch":false,"pageSize":0}'));

                SET @CompIdx = @CompIdx + 1;
            END

            SET @TabIdx = @TabIdx + 1;
        END

        -- calculatedFields top-level (bos array)
        IF JSON_QUERY(@NewJson, '$.calculatedFields') IS NULL
            SET @NewJson = JSON_MODIFY(@NewJson, '$.calculatedFields', JSON_QUERY('[]'));

        UPDATE dbo.ReportCatalog
        SET DashboardConfigJson = @NewJson
        WHERE ReportId = @ReportId;

        INSERT INTO dbo.AuditLog
            (AuditId, Username, EventType, TargetType, TargetKey, Description,
             OldValuesJson, NewValuesJson, IsSuccess, CreatedAt, ReportId)
        VALUES
            (NEWID(), 'system', 'dashboard_schema_migrated_v1_to_v2', 'report', CAST(@ReportId AS VARCHAR),
             'DashboardConfigJson v1 -> v2 (Migration 18 Adim A)',
             @OldJson, @NewJson, 1, GETUTCDATE(), @ReportId);

        SET @MigratedV2 = @MigratedV2 + 1;

        FETCH NEXT FROM cfg_cursor INTO @ReportId, @OldJson;
    END

    CLOSE cfg_cursor;
    DEALLOCATE cfg_cursor;

    -- ============================================================
    -- ADIM B: ReportType='table' -> dashboard auto-convert
    -- ============================================================
    DECLARE @TableReportId INT;
    DECLARE @TableTitle NVARCHAR(500);
    DECLARE @TableConfig NVARCHAR(MAX);
    DECLARE @WidgetHash VARCHAR(8);
    DECLARE @EscapedTitle NVARCHAR(1000);

    DECLARE tbl_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT ReportId, Title
        FROM dbo.ReportCatalog
        WHERE ReportType = 'table'
          AND (DashboardConfigJson IS NULL
               OR DashboardConfigJson = ''
               OR ISJSON(DashboardConfigJson) = 0);

    OPEN tbl_cursor;
    FETCH NEXT FROM tbl_cursor INTO @TableReportId, @TableTitle;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Stabil widget id: ReportId hex (deterministik, re-run ayni hash uretir)
        SET @WidgetHash = LOWER(SUBSTRING(CONVERT(VARCHAR(32), HASHBYTES('MD5', CAST(@TableReportId AS VARCHAR)), 2), 1, 8));
        SET @EscapedTitle = REPLACE(REPLACE(@TableTitle, '\', '\\'), '"', '\"');

        SET @TableConfig = N'{
  "schemaVersion": 2,
  "tabs": [
    {
      "title": "Genel",
      "components": [
        {
          "id": "w_table_' + @WidgetHash + N'",
          "type": "table",
          "title": "' + @EscapedTitle + N'",
          "span": 4,
          "resultSet": 0,
          "columns": [],
          "tableOptions": {
            "totalRow": false,
            "stripe": true,
            "stickyHeader": true,
            "clientSearch": false,
            "pageSize": 0
          }
        }
      ]
    }
  ],
  "calculatedFields": []
}';

        UPDATE dbo.ReportCatalog
        SET DashboardConfigJson = @TableConfig
        WHERE ReportId = @TableReportId;

        INSERT INTO dbo.AuditLog
            (AuditId, Username, EventType, TargetType, TargetKey, Description,
             OldValuesJson, NewValuesJson, IsSuccess, CreatedAt, ReportId)
        VALUES
            (NEWID(), 'system', 'report_type_consolidated_to_dashboard', 'report', CAST(@TableReportId AS VARCHAR),
             'ReportType=table -> auto-generated table widget dashboard (Migration 18 Adim B)',
             '{"ReportType":"table","DashboardConfigJson":null}',
             @TableConfig, 1, GETUTCDATE(), @TableReportId);

        SET @ConvertedTable = @ConvertedTable + 1;

        FETCH NEXT FROM tbl_cursor INTO @TableReportId, @TableTitle;
    END

    CLOSE tbl_cursor;
    DEALLOCATE tbl_cursor;

    -- ============================================================
    -- OZET
    -- ============================================================
    PRINT '=== Migration 18 ozeti ===';
    PRINT 'ADIM A (v1 -> v2): ' + CAST(@MigratedV2 AS VARCHAR) + ' dashboard';
    PRINT 'ADIM B (table -> dashboard): ' + CAST(@ConvertedTable AS VARCHAR) + ' rapor';

    COMMIT TRANSACTION;
    PRINT 'COMMIT: Migration 18 basarili.';

END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'cfg_cursor') >= 0
    BEGIN
        CLOSE cfg_cursor;
        DEALLOCATE cfg_cursor;
    END
    IF CURSOR_STATUS('local', 'tbl_cursor') >= 0
    BEGIN
        CLOSE tbl_cursor;
        DEALLOCATE tbl_cursor;
    END

    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR('Migration 18 HATA (satir %d): %s', 16, 1, @ErrLine, @ErrMsg);
END CATCH
