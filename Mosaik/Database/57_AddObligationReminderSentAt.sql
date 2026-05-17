-- Migration 57: ContractObligations.ReminderSentAt
-- DailyReminderJob bu alani null gordugunde hatirlatma gonderir, sonra now() yazar.
ALTER TABLE ContractObligations
    ADD ReminderSentAt DATETIME2 NULL;
