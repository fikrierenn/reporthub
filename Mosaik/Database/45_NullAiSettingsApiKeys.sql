-- Migration 45: AiSettings.ApiKey at-rest encryption geçişi
-- Plan 25.1 Faz 2 — artık ApiKey IDataProtector ile şifrelenerek kaydediliyor.
-- Mevcut plaintext key'ler şifrelenmiş formatı beklediği için geçersiz → NULL yapıp admin'in yeniden girmesi sağlanıyor.

UPDATE AiSettings SET ApiKey = NULL WHERE ApiKey IS NOT NULL AND ApiKey != '';
