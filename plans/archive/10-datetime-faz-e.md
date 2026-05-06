# Plan 10 — DateTime Faz E (Veri shift + seed hizalama)

**Tarih:** 2026-05-07
**Yazan:** Fikri / Claude
**Durum:** `İptal — superseded by Plan 11`

**Not (7 May 2026):** Plan 10 onay almadı. Dev ortamı + sıfırlanabilir data → karmaşık veri shift gereksiz, reset baseline çözer. Plan 11 (Mosaik Foundation + DB reset + UTC seed) bu plan'ın hedefini ek brand/module foundation ile birlikte kapsadı. Plan 10 referans olarak archive.

---

## 1. Problem

ADR-006 DateTime UTC convention 5 fazlık iş. Faz A/B/C (app kodu `DateTime.Now → UtcNow` sweep, 22 Nisan 2026, commit `dba9dc4`) ve Faz D (DB DEFAULT `GETDATE() → GETUTCDATE()`, Migration 25, 5 Mayıs 2026) tamamlandı. Eksik: **22 Nisan öncesi kayıtlar hâlâ TR-local olarak DB'de duruyor** — 14 tablodaki tarih kolonları (`CreatedAt`/`UpdatedAt`/`LastLoginAt`/`RunAt` vs.) bu yüzden kayıt-tipine göre +3 saatlik tutarsızlık taşıyor. `DashboardController` KPI'larında "bu ay" / "son 1 saat" / "saatlik trend" filtreleri bu satırlarda yanlış sonuç döndürüyor. Ek olarak `Database/03_SeedData.sql` hâlâ `GETDATE()` kullanıyor — seed yeniden koşturulursa yine local yazar.

## 2. Scope

### Kapsam dahili
- **Migration 28** (`28_DataShiftToUtc.sql`): 22 Nisan 2026 öncesi tarihli satırlarda tüm tarih kolonlarına `DATEADD(MINUTE, -180, <kolon>)` UPDATE. 14 tablo, transaction sarmalı.
- **Pre-flight inspection** (Migration öncesi salt-okunur SELECT'ler): her tablo için (a) toplam satır, (b) `< 22 Nis` (kesin shift), (c) `22 Nis – 5 May` (gri bölge — kullanıcıya raporlanacak), (d) `>= 5 May` (UTC, dokunulmaz).
- **Seed güncelleme**: `03_SeedData.sql` 3 yerdeki `GETDATE()` → `GETUTCDATE()` (idempotent, geleceğe yönelik).
- **Backup pattern**: Tam DB backup (SSMS, kullanıcı) + `Database/backup/20260507_pre_faz_e_rollback.sql` (transaction wrap'lı geri-al SQL).
- **Doğrulama smoke**: shift sonrası `SELECT MIN/MAX` her tablo için + DashboardController KPI sapma kontrolü (admin görünümünde "Bu Ay" sayısı shift öncesi/sonrası karşılaştır).

### Kapsam dışı
- **SP body `GETDATE()` → `GETUTCDATE()`** (`sp_PdksPano:34,41,42` + `sp_SatisPano:39`) — external DB'lere (PDKS, DER) bağlı, o sunucularda saat zaten TR-local. Çevirmek anlam taşımaz; SP refactor fazı (TODO madde 21, ADR-004 adayı) ile eş zamanlı ele alınır.
- **Tarihsel migration body `GETDATE()`** (`08`, `14`, `20`, `21`, `22`, `23`) — bir kez çalışmış migration'lar; dokunulmaz (idempotency bozulur).
- **Gri bölge default shift** (22 Nis – 5 May) — pre-flight raporundan sonra ayrı karar; default olarak shift **YOK**.
- **Application kodu** — Faz C'de zaten temiz.

### Etkilenen dosyalar (tahmin)
- `ReportPanel/Database/28_DataShiftToUtc.sql` (yeni, ~120 satır) — ana migration
- `ReportPanel/Database/03_SeedData.sql` — 3 yerde `GETDATE()` → `GETUTCDATE()`
- `Database/backup/20260507_pre_faz_e_rollback.sql` (yeni, .gitignore'da) — rollback SQL
- `TODO.md` — madde 28 [x]
- `docs/ADR/006-datetime-utc-convention.md` — Faz E closure notu (varsa)

**Tahmini boyut:** 3 dosya / ~200 satır SQL + ~50 satır pre-flight inspection.

## 3. Alternatifler

### A: Tablo başına ayrı migration (28a, 28b, ..., 28n)
**Açıklama:** Her tablo için bağımsız `.sql` dosyası, sıralı koşturulur.
**Reddetme sebebi:** 14 dosya bakım yükü, sıralama hatası riski, idempotency garantisi yok, rollback karmaşık.

### B: Single-shot toplu UPDATE (transaction yok)
**Açıklama:** Tek dosyada tüm UPDATE'ler arka arkaya, transaction sarmalı yok.
**Reddetme sebebi:** Bir tablo başarısız olursa öncekiler committed kalır → tutarsızlık. Atomicity yok.

### C (SEÇİLEN): Tek migration + dış transaction + tablo-bazlı TRY/CATCH blok
**Açıklama:** `28_DataShiftToUtc.sql` tek dosya. `BEGIN TRANSACTION` dış sarmalı. Her tablo `BEGIN TRY ... END TRY BEGIN CATCH ROLLBACK; THROW; END CATCH` bloğunda. Hatada tüm UPDATE rollback. Sona doğrulama SELECT, sonunda `COMMIT`.
**Sebep:** Atomicity + selective error message + 14 tabloyu tek geçişte. Rollback = backup script (Migration 25 cursor pattern'inden farklı çünkü constraint değil veri).

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Gri bölge (22 Nis – 5 May) satırlarının yanlış sınıflanması | Yüksek (data corruption) | Orta | Pre-flight inspection: kullanıcı gri bölge sayılarını gör, default shift YOK, sadece konservatif `< 22 Nis` shift edilir |
| Tam DB backup eksik / hasarlı | Çok yüksek (data loss) | Düşük | Defans 2 katman: SSMS BACKUP DATABASE + rollback SQL transaction wrap |
| Migration tekrar çalışırsa double-shift | Orta (data corruption) | Düşük | `WHERE <kolon> < '2026-04-22'` filter idempotent — shift sonrası 22 Nis öncesi yok artık. Migration script header'da "ONE-TIME, do not re-run" warning |
| `DashboardController` KPI'ları shift sonrası yanlış davranır | Orta (UX) | Düşük | Smoke öncesi/sonrası karşılaştırma: "Bu Ay" sayısı admin/sıradan kullanıcı görünümünde tutarlı mı |
| FK constraint / audit log cascade | Düşük | Düşük | UPDATE değer-only, FK ilişki yok |
| Test DB'de çalışıp prod DB'de patlama | Orta | Düşük | Önce DEV PortalHUB'da koş, MIN/MAX doğrula, sonra PROD |

## 5. Done Criteria

- [ ] Pre-flight inspection raporu kullanıcıya gösterildi (gri bölge satır sayıları)
- [ ] Tam DB backup alındı (SSMS, kullanıcı yaptı, dosya yolu kayıtlı)
- [ ] `28_DataShiftToUtc.sql` yazıldı, dev/test DB'de doğrulandı
- [ ] Rollback SQL `Database/backup/20260507_pre_faz_e_rollback.sql` yazıldı
- [ ] Production DB'de çalıştırıldı (kullanıcı SSMS), MIN/MAX doğrulama OK
- [ ] `03_SeedData.sql` `GETUTCDATE()` güncellemesi commit
- [ ] DashboardController KPI smoke: "Bu Ay" sayıları kullanıcı browser'da görsel doğrulama
- [ ] Build + 246/246 test yeşil
- [ ] Journal entry (`docs/journal/2026-05-07.md`)
- [ ] TODO.md madde 28 [x] + ADR-006 Faz E closure notu

## 6. Rollback Planı

### Acil (tam DB hasarı)
- SSMS: `RESTORE DATABASE PortalHUB FROM DISK = '<backup_path>' WITH REPLACE` (kullanıcı yapacak)

### Selective (sadece bu migration'ı geri al)
- `Database/backup/20260507_pre_faz_e_rollback.sql` çalıştır — tersine `DATEADD(MINUTE, +180, ...)` UPDATE'leri, transaction wrap'lı.
- Filter aynı (`< '2026-04-22'`) ama dikkat: rollback sonrası migration tekrar koşturulamaz (yine WHERE filter idempotent ama gri bölge confusion riski).

### Plan revert (kod değişikliği)
- `git revert <commit-hash>` — `03_SeedData.sql` ve plan dosyası geri alınır. Migration script'lerini revert etmek anlamsız (zaten one-time DB işlemi, kod state'inden bağımsız).

## 7. Adımlar / İçerdiği TODO maddeleri

### Faz E.1 — Pre-flight inspection (~30 dk)
1. [ ] Pre-flight SELECT script: 14 tablo × 4 metric (toplam, `<22 Nis`, `22 Nis–5 May`, `≥5 May`). Tek atış raporu.
2. [ ] PortalHUB'da çalıştır, sonuçları kullanıcıya göster.
3. [ ] Gri bölge sayısına göre karar (kullanıcıyla):
   - Sıfır/küçük → konservatif kesim `< 22 Nis`
   - Büyük → satır örnekleme + manuel inceleme

### Faz E.2 — Backup (~10 dk, kullanıcı)
4. [ ] **Kullanıcı SSMS:** `BACKUP DATABASE PortalHUB TO DISK = 'D:\backup\PortalHUB_pre_faz_e_20260507.bak' WITH FORMAT, INIT`
5. [ ] Backup dosyasının varlığı + boyutu doğrulandı.

### Faz E.3 — Migration yazımı (~1.5h)
6. [ ] `28_DataShiftToUtc.sql` yaz: header warning + `BEGIN TRANSACTION` + 14 tablo TRY/CATCH bloğu + doğrulama SELECT + `COMMIT`.
7. [ ] `Database/backup/20260507_pre_faz_e_rollback.sql` yaz: tersine `+180 dk` UPDATE'ler.
8. [ ] Idempotency check: WHERE filter `< '2026-04-22'` her tabloda var mı.

### Faz E.4 — Dev test (~30 dk)
9. [ ] **Kullanıcı SSMS DEV:** Migration 28 dry-run (BEGIN TRANS + ROLLBACK ile test).
10. [ ] MIN/MAX SELECT doğrulama: 22 Nis öncesi tarihler 21 Nis 21:00 öncesine düştü mü.

### Faz E.5 — Seed güncelleme (~10 dk)
11. [ ] `03_SeedData.sql` 3 yerdeki `GETDATE()` → `GETUTCDATE()`. Hardcoded literal yok (keşfe göre).
12. [ ] Build doğrulama.

### Faz E.6 — Production execution (~10 dk, kullanıcı)
13. [ ] **Kullanıcı SSMS PROD:** Migration 28 çalıştır.
14. [ ] MIN/MAX doğrulama tekrarı.
15. [ ] Sample tablo (User.LastLoginAt) bireysel inceleme.

### Faz E.7 — Smoke + commit + closure (~30 dk)
16. [ ] DashboardController KPI smoke: admin + sıradan kullanıcı, "Bu Ay" / "Son 7 Gün" sayıları beklenen aralıkta.
17. [ ] `dotnet test` → 246/246.
18. [ ] Commit chain:
    - `chore(db): seed GETDATE → GETUTCDATE (plan: 10)`
    - `feat(db): Migration 28 — data shift to UTC for pre-22-Apr rows (plan: 10)`
19. [ ] Journal entry `docs/journal/2026-05-07.md`.
20. [ ] TODO.md madde 28 [x] + ADR-006 Faz E "tamamlandı" notu.
21. [ ] Plan dosyası `plans/archive/`.

## 8. İlişkili

- ADR: [`docs/ADR/006-datetime-utc-convention.md`](../docs/ADR/006-datetime-utc-convention.md) — UTC convention + faz tablosu
- Önceki migration: [`Database/25_DefaultGetUtcDate.sql`](../ReportPanel/Database/25_DefaultGetUtcDate.sql) — Faz D pattern (cursor + DEFAULT constraint)
- Backup pattern: [`Database/backup/`](../ReportPanel/Database/backup/) — `.gitignore`, transaction wrap
- TODO madde: 28 (FAZ 2)
- Faz C commit: `dba9dc4` (22 Nisan 2026)
- Faz D commit: Migration 25 (5 Mayıs 2026)
- Bağlantılı (ileride): TODO madde 21 — `sp_PdksPano` / `sp_SatisPano` SP refactor; o iş esnasında SP body `GETDATE()` → `GETUTCDATE()` ele alınır

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Geri bildirim alındı (varsa düzeltildi)
- [ ] Onay alındı: <tarih, kullanıcı imzası>
