# Plan 51 — Differential Privacy Reporting (Aktif Veri Bağışıklığı)

**Durum:** ✅ **ONAYLANDI 2026-05-25** — revize v2 (epsilon budget mimari zırh + 5 açık soru kapatma)
**Tier:** 3 (yeni paradigma + KVKK kritik + reporting pipeline değişiklik + matematiksel hassas)
**Effort:** 50-80h (6 faz, 4-6 hafta) — tahmini
**Aciliyet:** 🟣 Plan 44 (RAG Guard) ✅ + Plan 14 (UserDataScope) ✅ olgun production sonrası

---

## 1. Vizyon

DB yöneticisi + rapor geliştirici ne kadar dikkatli olursa olsun, SP'ler bazen **yetkisiz personelin görmemesi gereken hassas veriyi** (PII, maaş, müşteri data) döndürür. Tek tek SP düzeltmek imkansız (200+ SP, sürekli yeni).

**Radikal fikir:** DB'den gelen veriyi user'a olduğu gibi GÖSTERME. Raporlama motorunun içine **Aktif Gizlilik Filtresi** yerleştir.

`StoredProcedureExecutor` DB sorgusu çekti — Mosaik **memory'de** veri setini durdurur. AI motor + role tabanlı filter:
- Genel Müdür: net isim + net maaş görür
- Analist: isim maskeli + maaş departman ortalaması (gürültü eklenmiş anonim)
- Misafir: hücre tamamen `***`

DB sorgusu **bozulmaz**, dönen result set **dinamik ehlileştirilir**.

---

## 2. Senaryo (BKM gerçek)

> SP: `sp_PersonelMaasRaporu` → result columns: `ad, soyad, departman, maas, yan_haklar, baslama_tarihi`
>
> **Aynı SP üç farklı user:**
>
> ```
> Genel Müdür:
>   Ahmet | Yılmaz | IT | 45000 | 5000 | 2022-01-15
>   Elif  | Kara   | IT | 38000 | 4500 | 2023-03-20
>
> Analist (yetki: aggregate-only):
>   ***   | ***    | IT | 41500 ± 3500 | *** | 2022/2023
>   ***   | ***    | IT | 41500 ± 3500 | *** | 2022/2023
>
> Misafir (read-only):
>   ***   | ***    | IT | ***          | *** | ***
> ```
>
> Aynı SQL, aynı SP, **3 ayrı sanitize edilmiş output**. 50+ DB yetki tablosu yönetmek yerine arayüz veri setini dinamik ehlileştirir.

---

## 3. Mimari (ön araştırma)

### 3.1 Component'ler

```
Mosaik.Core/Reporting/Privacy/
├─ IDataPrivacyFilter.cs                — interface
├─ DefaultDataPrivacyFilter.cs          — default impl
├─ PrivacyPolicy.cs                     — { ColumnName, Strategy, Threshold }
├─ Strategies/
│  ├─ MaskStrategy.cs                   — "***", first_letter, hash
│  ├─ HashStrategy.cs                   — SHA256 deterministic
│  ├─ NoiseStrategy.cs                  — Laplace/Gaussian noise (differential privacy)
│  ├─ AggregateStrategy.cs              — group-by + mean/median
│  └─ NullStrategy.cs                   — null replace
└─ AiSensitiveColumnDetector.cs         — Qwen + Presidio scan column content

Mosaik/Services/Reporting/
├─ StoredProcedureExecutor.cs           — POST hook: result set sanitize
└─ PrivacyAwareResultProcessor.cs       — yeni — column policy lookup + apply
```

### 3.2 Pipeline

```
ReportsController.Run
  ↓
StoredProcedureExecutor.ExecuteMultipleAsync()
  ↓ raw DataTable[]
PrivacyAwareResultProcessor.SanitizeAsync(tables, userClaims)
  ├─ Per-column policy lookup
  │  - Static: PrivacyPolicy tablosu (admin tanımlı: SP_X.column_Y → strategy)
  │  - Dynamic: AiSensitiveColumnDetector (Plan 40 Presidio + Qwen column content scan)
  ├─ Per-user role/clearance check
  │  - claims.SecurityClearance ≥ policy.RequiredLevel → raw
  │  - claims.SecurityClearance < policy.RequiredLevel → apply strategy
  ├─ Strategy execute per cell
  │  - Mask "Ahmet" → "A***"
  │  - Hash 12345 (TCKN) → "a3f5d..."
  │  - Noise maas 45000 → 41500 + Laplace(scale=2000)
  │  - Aggregate group by dept → mean
  ├─ Audit: { user, sp_name, columns_sanitized[], strategies_applied[] }
  └─ Return sanitized DataTable[]
  ↓
DashboardRenderer.Render() — sanitized data
```

### 3.3 Policy Definition

**Static policy (admin tanımlı, SP-spesifik):**

```sql
CREATE TABLE dbo.ReportingPrivacyPolicies (
    Id INT IDENTITY PRIMARY KEY,
    SpName NVARCHAR(200) NOT NULL,
    ColumnName NVARCHAR(100) NOT NULL,
    Strategy NVARCHAR(50) NOT NULL,         -- mask, hash, noise, aggregate, null
    StrategyParams NVARCHAR(MAX) NULL,      -- JSON: { "noise_scale": 2000, "mask_pattern": "first_letter" }
    RequiredClearanceLevel TINYINT NOT NULL,-- bu seviyeden düşük → strategy uygula
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_RPP UNIQUE (SpName, ColumnName)
);

-- Örnek seed
INSERT INTO ReportingPrivacyPolicies (SpName, ColumnName, Strategy, StrategyParams, RequiredClearanceLevel)
VALUES
('sp_PersonelMaasRaporu', 'ad',     'mask',      '{"pattern":"first_letter"}', 3),
('sp_PersonelMaasRaporu', 'soyad',  'mask',      '{"pattern":"all"}',          3),
('sp_PersonelMaasRaporu', 'maas',   'aggregate', '{"group":"departman","fn":"mean","noise":"laplace","scale":2000}', 3),
('sp_PersonelMaasRaporu', 'tckn',   'hash',      NULL,                         2);
```

**Dynamic AI policy (column content sniff):**

Presidio + Qwen ilk koşumda otomatik sniff. Admin "kabul" / "düzelt" / "skip". Bir sonraki koşumda static policy reuse.

### 3.4 Differential Privacy — Noise Strategy Math

```
Laplace mechanism: noisy_value = true_value + Laplace(0, sensitivity/epsilon)
  - sensitivity = max value change one row can cause (örn. maaş için 100000)
  - epsilon = privacy budget (küçük = daha gizli, daha gürültülü)
  - epsilon = 1.0 → orta gizli (BKM önerim)

Group aggregate:
  - Group by department, mean(maas) → 41500
  - Add Laplace(0, 100000/1.0) = ~ ± 3500
  - Output: 41500 ± 3500 (kullanıcıya range göster)
```

### 3.5 KVKK + Reporting Constraint

- **Sanitize MUST happen before render** (defense-in-depth)
- **Export** (Excel/PDF) **aynı sanitize uygulanır** (genelde export'ta unutulur, kritik bug riski)
- **Cache invalidation:** Policy değişirse cached report invalidate
- **Audit log:** Her sanitize event (kim, hangi SP, hangi kolon, hangi strategy)

---

## 4. KVKK + Teknik Riskler

| Risk | Etki | Mitigation |
|---|---|---|
| Sanitize bypass — export endpoint atlandı | **CRITICAL** | Single pipeline (export her zaman PrivacyAwareResultProcessor'dan geçer) + integration test |
| Noise mathematical attack — repeated query → noise average reveals truth | yüksek | Per-user query rate limit + same-query cache (aynı user aynı SP aynı sanitize) |
| Performance — 100K row × column sanitize | yüksek | Streaming sanitize + lazy column-on-demand + benchmark |
| Yeni SP eklendi, policy unutuldu → default raw expose | **CRITICAL** | **Default-deny:** policy yoksa Qwen+Presidio scan zorunlu + admin onayı; "policy_missing" warning admin'e |
| Aggregate strategy çok az row → individual reveal | orta | Group min N=5; N<5 → null |
| AI false negative (hassas kolon detect edilmedi) | yüksek | Periyodik audit job + admin review pending detect; opt-in continuous learning |
| Pivot table / drill-down reverse engineering | orta | Hierarchical policy enforcement (parent + child kolonlar aynı strategy) |

---

## 5. Bağımlılıklar (ön)

- **Hard prereq:** Plan 14 UserDataScope + Plan 44 IRagAccessPolicy (UserClaims olgun)
- **Hard prereq:** Plan 40 KVKK + Presidio (column content scan)
- **Reuse:** Plan 34.1 Qwen + skill catalog (kvkk-veri-envanteri skill)
- **Reuse:** Plan 14 Audit log infrastructure

**Yeni NuGet:**
- Microsoft.Presidio (Plan 40 reuse)
- MathNet.Numerics (MIT) — Laplace distribution noise
- Microsoft.ML.Tokenizers (zaten Plan 34.1)

---

## 6. Deep Dive Sonraki Adımlar

1. **KVKK + Hukuk ön onay:** DPO + İç Hukuk consult — DP epsilon parametresi yasal yeterli mi (UE/USA standartı ε=1.0)?
2. **5 lens deep tradeoff:**
   - **Contrarian:** "DB-side row level security (RLS) varken niye app-side?" — RLS SP tarafında: SP yazımı karmaşıklaşır, mevcut 200+ SP yeniden yazmak imkansız. App-side: bir kez yaz, tüm SP'leri sanitize.
   - **First Principles:** Asıl problem 50+ yetki tablosu yönetememe → centralized policy DSL.
   - **Expansionist:** Aynı policy framework Plan 27 Documents + Plan 41 Form export'a uygulanır (universal sanitize).
   - **Outsider:** Snowflake/BigQuery DP query var — niye uygulamada? Cevap: BKM SQL Server Express, BI tool yok, kendimiz yazıyoruz.
   - **Executor:** POC sp_PersonelMaasRaporu + 3 user role test.
3. **Performance benchmark:** 100K row × 10 kolon sanitize → < 200ms hedef.
4. **ADR-033 adayı:** "Privacy-aware reporting middleware + Laplace DP noise"
5. **Default-deny policy + AI sniff POC:** yeni SP added → automatic Presidio+Qwen scan → policy candidate.

---

## 7. Bu plan ne zaman aktif olur?

Plan 14 UserDataScope + Plan 44 RAG Guard production stabil + Plan 40 KVKK Presidio canlı sonrası. **Erken implement = policy infrastructure eksik = yanlış sanitize.**

**Tahmini başlangıç:** 2027 Q2 (Mayıs-Haziran).

---

## 8. Açık Sorular — KAPATILDI 2026-05-25 (revize v2 kararları)

### Mimari Zırh — Epsilon Budget Saldırı Koruması (CRITICAL)

**Risk:** Differential Privacy noise stratejisi tekrarlanan sorgularla kırılabilir. Saldırgan aynı SP'yi 1000 kez çağırırsa Laplace noise ortalaması sıfıra yaklaşır → gerçek değer ifşa olur. (Noise averaging attack)

**Zorunlu mitigation:**

```csharp
// PrivacyAwareResultProcessor
public async Task<DataTable[]> SanitizeAsync(DataTable[] tables, UserClaims claims, string spName, string paramsHash)
{
    // 1. Per-user query cache (aynı SP + aynı param → aynı sanitized output)
    var cacheKey = $"dp:{claims.UserId}:{spName}:{paramsHash}";
    if (_cache.TryGet(cacheKey, out DataTable[] cached))
        return cached;  // Tekrar noise hesaplamaz — saldırı engelleme

    // 2. Per-user rate limit (saatlik max 50 unique SP call)
    var limit = await _rateLimit.CheckAsync(claims.UserId, "dp_sanitize_hourly");
    if (limit.Exceeded)
    {
        await _audit.LogAsync("dp_rate_limit_exceeded", claims.UserId);
        throw new RateLimitException("Saatlik gizlilik sorgu limiti aşıldı.");
    }

    // 3. Privacy budget tracking (ε accumulated)
    // Aynı kullanıcı + aynı kolon kombinasyonu için epsilon harcaması toplanır
    // Toplam ε > 5.0 → admin'e alert, yeni sorgu mask'la geçer (aggregate yerine)

    var result = ApplyStrategies(tables, claims, spName);
    _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
    return result;
}
```

**Sonuç:** Aynı user aynı sorguda hep aynı noise — averaging attack engelleme. Audit log her sanitize event.

### 1. DB-side RLS vs App-side filter?

**Karar:** **App-side (Uygulama Seviyesi).**

**Gerekçe:**
- Mevcut 200+ SP'yi RLS uygulamak imkansız (her SP yeniden yazılır + test)
- `StoredProcedureExecutor` tek noktada merkezi filtreleme = pragmatik
- Yeni SP eklendiğinde policy unutulamaz (default-deny rule)
- Reporting pipeline single source of truth

DB-side RLS v2'de (Plan 51.1) tamamlayıcı katman olarak değerlendirilir.

### 2. Hassas kolon tespit edildiğinde default strateji?

**Karar:** **Mask Varsayılan.**

```csharp
PrivacyStrategy DefaultStrategy(SensitiveColumnType type) => type switch
{
    SensitiveColumnType.TCKN          => MaskStrategy.Pattern("***********"),
    SensitiveColumnType.Name          => MaskStrategy.FirstLetter,
    SensitiveColumnType.Email         => MaskStrategy.PreserveDomain,
    SensitiveColumnType.Phone         => MaskStrategy.LastFour,
    SensitiveColumnType.IBAN          => MaskStrategy.Pattern("TR** **** **** **** **** XXXX"),
    SensitiveColumnType.Salary        => MaskStrategy.AllAsterisk,  // user explicit aggregate ister
    SensitiveColumnType.HealthData    => MaskStrategy.AllAsterisk,
    _                                  => MaskStrategy.AllAsterisk
};
```

- Default refleks: maske (görünür ama anonim)
- Aggregate (Laplace noise) admin explicit opt-in per column policy
- Hash strategy TCKN gibi join-key alanlar için (deterministic SHA256)

### 3. DP epsilon (ε) parametresi nasıl seçilir?

**Karar:** **Default ε = 1.0 (Orta seviye gürültü).**

- ε=1.0 differential privacy literature standard (orta gizli)
- MathNet.Numerics `Laplace(0, sensitivity/epsilon)` distribution
- Admin per-SP per-column override:
  - ε=0.5 (daha gizli, daha gürültülü) — yüksek hassas maaş
  - ε=1.0 default
  - ε=2.0 (daha az gürültü) — aggregate report yöneticisi için
- ε budget tracking (mimari zırh) global per-user

### 4. Export (Excel/PDF) sanitize aynı mı sıkı mı?

**Karar:** **EVET, DAHA SIKI.**

- Ekran ortalama+varyans gösterirken → Excel/PDF dışa aktarımda **tamamen NULL** olabilir
- Export dış paylaşım riski (mail, USB, paylaşımlı klasör) — defense-in-depth
- `PrivacyPolicy.ExportStrategy` ayrı kolon:
  ```sql
  ALTER TABLE ReportingPrivacyPolicies ADD
      ExportStrategy NVARCHAR(50) NULL,            -- null=Strategy ile aynı, else override
      ExportStrategyParams NVARCHAR(MAX) NULL;
  ```
- Export endpoint **aynı `PrivacyAwareResultProcessor`** pipeline ama `ExportStrategy` öncelik

### 5. Cache invalidate stratejisi?

**Karar:** **ReportingPrivacyPolicies UPDATE → `ReportCache.InvalidateAll()` + audit.**

```csharp
// PrivacyPolicyController.Update
[HttpPost]
public async Task<IActionResult> Update(int id, PrivacyPolicyDto dto)
{
    await _repo.UpdateAsync(id, dto);
    _reportCache.InvalidateAll();              // global flush
    _privacyCache.InvalidateUser(allUsers);    // per-user DP cache flush

    await _audit.LogAsync("privacy_policy_changed", new {
        policy_id = id,
        old_strategy = old.Strategy,
        new_strategy = dto.Strategy,
        cache_invalidated = true
    });

    return Ok();
}
```

- Policy değişikliği rare olay → full cache flush kabul (BKM ölçek)
- Audit log compliance kanıt: "policy değişti, eski cache silindi"

---

## 9. Onay + Revize Notu

**ONAYLANDI 2026-05-25** — Kullanıcı strategic review (matematiksel gizlilik + güvenlik mimarı lens):

- **Mimari Zırh:** Epsilon Budget Saldırı Koruması — per-user query cache (deterministic noise) + rate limit + ε accumulation tracking — averaging attack engelleme
- 5 açık soru cevaplandı + plan'a karar olarak gömüldü (§8)
- App-side filter (RLS v2 tamamlayıcı)
- Mask default, aggregate opt-in
- ε=1.0 default + admin override per-SP-column
- Export DAHA SIKI (ayrı ExportStrategy kolon)
- Cache invalidate `ReportCache.InvalidateAll()` + audit
- **Implementasyon:** Plan 14 UserDataScope + Plan 44 RAG Guard ✅ + Plan 40 KVKK Presidio ✅ production sonrası
