# ADR-003 · Rol modeli: UserRole junction tablosu birincil, User.Roles CSV deprecate

- **Durum:** Kabul edildi (22 Nisan 2026)
- **Etkilenen:** `User` entity, auth akışı, rapor erişim kontrolü, audit log
- **İlgili TODO:** M-03 (BIRLESIK ONCELIK SIRASI Faz 1)

## Bağlam

Erken sürümde kullanıcı rolleri tek bir CSV alanında tutuldu (`User.Roles = "admin,ik,mali"`). Daha sonra çok tabanlı rol gereksinimi (rapor erişim kuralları, denetim, rol yönetimi) normalize bir yapı gerektirdi ve paralel olarak üç yeni tablo eklendi:

- `Roles` — rol tanımları (isim, açıklama, aktiflik)
- `UserRole` — kullanıcı ↔ rol junction (N:N)
- `ReportAllowedRole` — rapor ↔ rol allowlist junction

Bu iki yapı yan yana yaşamaya başladı:

- `AuthController.Login`: önce UserRole'dan çekiyor, boşsa CSV'ye fallback ediyordu.
- `AdminController.CreateUser/EditUser`: **hem** `user.Roles` CSV alanına yazıyor, **hem** `SyncUserRoles` ile UserRole junction'a yazıyordu. İki kaynak eş zamanlı senkron olmayabiliyor.
- `ReportsController.GetCurrentUserRoleIds`: UserRole'dan çekiyor, boşsa CSV'ye düşüyordu.
- `ProfileController`: CSV string'i doğrudan view'a gönderiyordu.
- `AdminController.RenameRoleNameInCsv/RemoveRoleNameFromCsv`: rol adı değişince tüm `User.Roles` CSV'lerini elle güncellemek için vardı.

**Sorun:**
- İki kaynak hiçbir zaman deterministik şekilde senkron değil (`CreateUser` hatada path'e göre UserRole yazmayabiliyor).
- Hangi kaynağın doğru olduğu belirsiz (Dashboard CSV parse ederken Reports junction'a bakıyor).
- Rol rename/remove akışı her iki yerde de elle compensate edilmek zorunda.

## Karar

**`UserRole` junction tablosu rolün tek resmî kaynağıdır.** `User.Roles` CSV kolonu deprecate edilir. Geçiş üç fazlıdır:

### Faz A — Kod-düzeyi temizlik (bu iş, 22 Nisan 2026)

- `AuthController.Login`: CSV fallback kaldırılır. Rol iddiaları yalnızca `UserRole` → `Role.Name` üzerinden kurulur.
- `ReportsController.GetCurrentUserRoleIds`: CSV fallback kaldırılır.
- `ProfileController.Index` ve validation yeniden render path'leri: `UserRole` join'ünden role isimleri hesaplanıp virgülle birleştirilir (view zaten string bekliyor).
- `AdminController.CreateUser`: `user.Roles = rolesCsv` kaldırıldı; `user.Roles = string.Empty` yazılır (kolon hâlâ NOT NULL olduğu sürece zorunlu). Validation `selectedRoleIds.Count == 0` ile yapılır. Audit snapshot `UserRole` sorgusuyla hesaplanır.
- `AdminController.EditUser`: `existing.Roles = roles` kaldırıldı; audit snapshot CSV yerine rol adı listesi.
- `AdminController.HandlePostAction` / `user_delete` audit: `delUser.Roles` CSV yerine `UserRole` sorgusuyla hesaplanır.
- `AdminController.RenameRoleNameInCsv` / `RemoveRoleNameFromCsv`: **user** CSV loop'ları kaldırılır (UserRole FK cascade zaten ele alır). `ReportCatalog.AllowedRoles` CSV loop'ları **korunur** — rapor allowed roles CSV'si ayrı bir deprecate konusudur (ADR-004 adayı).

**Veri etkisi:** Yok. CSV kolon hâlâ DB'de, hâlâ okunabilir; sadece yeni yazımlar boş string olur.

### Faz B — Kolonu nullable + model'e `[Obsolete]` (sonraki PR)

- `Database/15_NullableUserRolesCsv.sql`: `User.Roles` kolonu NULL kabul eder.
- `Models/User.Roles` alanına `[Obsolete]` işaret konur, model consumer kalmadığından rahat.
- Opsiyonel: mevcut CSV verilerinden UserRole junction doluluğunu doğrulayan bir migration check scripti çalıştırılır, eksik olan kullanıcılar log'lanır.

### Faz C — Kolonu drop + model field sil (çok sonra, rollback uzun)

- `Database/16_DropUserRolesCsv.sql`: `ALTER TABLE Users DROP COLUMN Roles;`
- `Models/User` sınıfından `Roles` alanı silinir.
- Bu noktada kodda tek bir `user.Roles` kalmamalıdır; güvenlik için repo grep + regresyon test şart.

## Alternatifler

- **(A) Her iki kaynağı eş zamanlı senkron tutmaya devam** — mevcut durum. Sürekli ince bug kaynağı, audit/security açısından riskli. **Red.**
- **(B) CSV'yi birincil kaynak yap, UserRole kaldır** — raporAllowedRole + ReportCategoryLink gibi ek junction'larla uyumsuz; raporun mapping'i zaten junction, rol de junction olmalı. **Red.**
- **(C) UserRole birincil, CSV deprecate (seçilen)** — bu ADR. Senkron sorununu ortadan kaldırır, junction DB ilişkilerinde tutarlılık sağlar. Geçiş üç fazlı, risk kademeli.

## Sonuçlar

**Olumlu:**
- Tek veri kaynağı, eş-zamanlı senkron sorunu yok.
- Rol rename/remove FK cascade ile otomatik.
- Audit snapshotları her durumda doğru.
- Yeni rol modeli (ileride permission-level granularity) UserRole üzerinde genişletilebilir.

**Olumsuz / dikkat:**
- Faz A'da kolon hâlâ var, yanlışlıkla okuyan eski kod varsa boş string görecek (acute sessizlik). → Grep ile test zorunlu (M-04 kısmi unit test sonraki aşama).
- Faz B'ye geçerken `User.Roles` değeri olan aktif kullanıcıların UserRole senkron kontrolü gerek (validation scripti).
- `ReportCatalog.AllowedRoles` CSV ayrı bir deprecate kampanyası bekliyor (Faz 2, ADR-004 adayı).

## Referanslar

- `TODO.md` → "BIRLESIK ONCELIK SIRASI" → Faz 1 M-03
- `.claude/rules/architecture.md` → "Bilinen Tutarsızlıklar" → #1
- İlgili commit: Faz A bu PR.
