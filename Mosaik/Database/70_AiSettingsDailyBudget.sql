-- Plan D-02-5 (2026-05-22): Günlük token bütçesi per-provider.
-- NULL = sınırsız. Aşımda AiSummaryProvider bu provider'ı atlar, fallback devam eder.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AiSettings' AND COLUMN_NAME = 'DailyTokenBudget'
)
BEGIN
    ALTER TABLE AiSettings ADD DailyTokenBudget INT NULL;
    PRINT 'AiSettings.DailyTokenBudget eklendi.';
END
ELSE
BEGIN
    PRINT 'AiSettings.DailyTokenBudget zaten mevcut, atlandı.';
END
