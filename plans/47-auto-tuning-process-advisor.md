# Plan 47 — Auto-Tuning Process Optimization Advisor

**Durum:** ⏳ TASLAK 2026-05-25 — onay bekliyor
**Tier:** 3 (AI + workflow event sourcing + cross-modül + yönetim UX)
**Tetik:** Kullanıcı strategic input 2026-05-25 — "kendi verisini okuyarak operasyonu iyileştiren canlı beyin"
**Effort:** 25-35h (5 faz, 2-3 hafta)
**Aciliyet:** 🟡 Plan 36 (Workflow) Faz B+C bittikten + Plan 42 (Process Runtime) onaylandıktan sonra

---

## 1. Problem

Mevcut vizyon (VISION §7) "Friction Heatmap" planlamış — workflow log → dashboard chart. Ama bu **pasif veri**: yönetici girip grafiğe bakacak.

Gerçek değer: **sistem proaktif öneri üretirse.** Sürekli onay logarını tarayıp pattern bulup yönetici Inbox'una "şunu otomatiklestir, %X efor kazanırsın" diye düşürmeli.

**Senaryo (BKM gerçek):**

> Satın alma departmanında 5.000 TL altındaki kırtasiye taleplerinin son 6 ayda 250 tanesi yönetici onayına geldi. %99.2'si onaylandı. Ortalama onay süresi 2 saat (asistan re-routing dahil). Yönetici hiç reddetmemiş. **Bu adım gereksiz.**
>
> Sistem proaktif olarak: "Bu pattern'i tespit ettim. 'Kırtasiye < 5000 TL → otomatik onay' kuralı önereyim mi? Beklenen tasarruf: haftalık 8 saat."

**Şirket beyni** seviyesi. Standart portal yok. Mosaik differentiator.

---

## 2. Scope

### Kapsam içi (Faz 1-5)
- `WorkflowInstanceLog` event sourcing query (Plan 36 Migration 62 sonrası)
- `ProcessOptimizationSuggestion` entity + tablo
- Hangfire daily job: `ProcessAnalyzerJob` (cron daily 02:00 TR)
- LLM prompt orchestration (Qwen 3B + skill catalog)
- Pattern detector tipleri:
  - **Approval bypass:** %95+ approve, <5dk süre → autoapprove öneri
  - **Bottleneck:** ortalama dakika threshold (örn. p95 > 24h) → assignee değiştir/SLA azalt öneri
  - **Dead-end:** %50+ reject reason aynı (örn. "evrak eksik") → form pre-validation öneri
  - **Duplicate step:** aynı user 2 adımı sıralı yapıyor → birleştir öneri
  - **Volume spike:** belirli iş tipi 3x arttı → kapasite öneri
- Admin Inbox: "Süreç Öneri" tab + Accept/Reject/Defer
- Accept → workflow template auto-edit (rule injection) + audit
- Confidence + impact ölçüsü (her öneri için)
- Aylık trend mail: yönetici "geçen ay kabul edilen öneriler → kazanılan zaman"

### Kapsam dışı
- Self-modifying workflow (kullanıcı onayı olmadan değiştirme) — kategorik red
- Predictive analytics (yarın ne olacak) — overkill, Phase 2 adayı
- ML training pipeline (Plan 34.1 Faz 7 LoRA pattern reuse — opsiyonel)
- Cross-firma benchmark (BKM tek-tenant)
- Real-time streaming analytics — daily batch yeterli

---

## 3. Mimari

### 3.1 Workflow

```
Daily 02:00 TR:
  ProcessAnalyzerJob
    ├─ Phase 1: Aggregator
    │   ├─ WorkflowInstanceLog (son 30 gün)
    │   ├─ Group by WorkflowTemplateId + StepName
    │   └─ Metrik hesapla: count, p50/p95/p99 latency, approve_rate, reject_reasons[]
    ├─ Phase 2: Heuristic Pattern Detector
    │   ├─ 5 pattern tipi (yukarıdaki listede)
    │   ├─ Threshold tabanlı candidate flag
    │   └─ Output: List<RawSuggestion>
    ├─ Phase 3: LLM Augmentation
    │   ├─ Skill catalog: "süreç optimizasyon önerme" prompt
    │   ├─ Her RawSuggestion → narrative + impact estimate
    │   └─ Output: List<EnrichedSuggestion>
    ├─ Phase 4: Persist
    │   ├─ ProcessOptimizationSuggestion entity write
    │   ├─ Status: Pending
    │   └─ Audit log
    └─ Phase 5: Notification
        └─ Admin'lere "{count} yeni öneri" Inbox badge
```

### 3.2 Entity

```csharp
public class ProcessOptimizationSuggestion
{
    public int Id { get; set; }
    public DateTime DetectedAt { get; set; }
    public int FirmaId { get; set; }
    public int? WorkflowTemplateId { get; set; }       // önerinin hedefi
    public string PatternType { get; set; }            // approval_bypass / bottleneck / dead_end / duplicate / volume_spike
    public byte Severity { get; set; }                  // 0 info, 1 medium, 2 high, 3 critical (lookup)
    public byte Confidence { get; set; }                // 0-100
    public string Title { get; set; }                  // "Kırtasiye <5K otomatik onay önerisi"
    public string Summary { get; set; }                // 2-3 cümle LLM narrative
    public string Evidence { get; set; }               // JSON: stats + N=250 + approve_rate=99.2
    public string ProposedAction { get; set; }         // JSON: rule_type + thresholds + step_to_remove
    public decimal? EstimatedHoursSavedPerWeek { get; set; }
    public byte Status { get; set; }                    // 0 Pending, 1 Accepted, 2 Rejected, 3 Deferred (lookup)
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerNotes { get; set; }
}
```

### 3.3 LLM Prompt Design

```
Sistem: BKM süreç optimizasyon analist. Aşağıdaki workflow metriklerine bakıp insancıl
bir öneri yaz (max 3 cümle). İstatistikleri Türkçe somutlaştır. Severity + impact tahmin.

Workflow: {template.Name}
Adım: {step.Name}
Son 30 gün:
- Toplam vaka: {count}
- Onaylanma oranı: {approve_rate}%
- Ortalama onay süresi: {avg_duration}
- En sık ret nedeni: {top_reject_reason}

Pattern: {pattern_type}
Eşik aşımı: {evidence_summary}

Çıktı JSON:
{
  "title": "string",
  "summary": "string (2-3 cümle, somut sayı, eylem önerisi)",
  "severity": 0-3,
  "confidence": 0-100,
  "proposed_action": { ... },
  "estimated_hours_saved_per_week": number
}
```

**Skill catalog inject:** "BKM kurumsal süreç optimizasyon" skill — onay zinciri tasarım prensiplerini içerir (SOX, BKM internal policy).

### 3.4 Pattern Detector — Approval Bypass Örneği

```csharp
public sealed class ApprovalBypassDetector : IPatternDetector
{
    public IEnumerable<RawSuggestion> Detect(WorkflowMetrics metrics)
    {
        foreach (var step in metrics.Steps)
        {
            if (step.Count < 50) continue;                      // minimum data
            if (step.ApproveRate < 0.95) continue;              // %95+ approve
            if (step.RejectCount > 0) continue;                 // hiç red yok
            if (step.AvgDuration > TimeSpan.FromMinutes(15)) continue; // hızlı onaylanmış

            yield return new RawSuggestion
            {
                PatternType = "approval_bypass",
                Evidence = new {
                    step.Count, step.ApproveRate, step.AvgDuration,
                    step.MaxAmount
                }
            };
        }
    }
}
```

### 3.5 Admin Inbox UI

```
/Admin/ProcessSuggestions
├─ Filter: Status (Pending / Accepted / Rejected) + Severity + Date range
├─ List card:
│   ┌─────────────────────────────────────────────────┐
│   │ 🟡 Severity: Medium  |  Confidence: 87%         │
│   │ Title: Kırtasiye <5K otomatik onay önerisi      │
│   │ Summary: Son 30 günde 250 vaka, %99.2 onay,     │
│   │ ortalama 2 saat onay süresi. Bu adımı kaldırın. │
│   │ Beklenen tasarruf: 8 saat/hafta                 │
│   │ [Detay]  [Kabul Et]  [Reddet]  [Ertele]        │
│   └─────────────────────────────────────────────────┘
└─ Accept onayı: 2-step confirm + audit + workflow template auto-edit preview
```

### 3.6 Accept akışı

```
User clicks "Kabul Et" →
  Confirmation modal: "Bu öneri uygulandığında workflow template şu şekilde değişir: [diff]"
  →
  WorkflowTemplate.Update(rule injection)
  → AuditLog event "process_suggestion_accepted"
  → ProcessOptimizationSuggestion.Status = Accepted, ReviewedBy + Notes
  → İlerideki vakalarda yeni rule uygulanır (yarın 02:00 sonra effect)
```

---

## 4. Alternatifler (5 lens)

### 🔴 Contrarian: Fatal flaw?

**LLM hallucinate öneri verir.** Qwen 3B "haftalık 8 saat tasarruf" tahmin ediyor ama gerçekte 2 saat. Yönetici 3 yanlış öneri sonrası sisteme güvenmez.

**Mitigation:**
- LLM sadece **narrative** üretir (Türkçe metin yazma). Metrik + impact **heuristic hesap** ile (formül: avg_duration × count × hourly_rate).
- Confidence + evidence görünür (yönetici hesabı doğrulayabilir)
- "Estimated" prefix vurgu; gerçek post-accept impact 30 gün sonra ölç ("önceki ay %X kazanım gerçekleşti")

### 🔵 First Principles: Gerçek problem?

Optimizasyon değil. Gerçek problem **yönetici görünürlük eksikliği.** Yönetici onay verirken pattern göremez (her vakaya tek tek bakar). Plan 47 yöneticiye dashboard verir, ama o zaten VISION §7 Friction Heatmap kapsamı.

**Mitigation:** Plan 47 v1 = Friction Heatmap dashboard (pasif). Plan 47 v2 = proaktif öneri (Inbox). v1'i önce yapıp v2'yi 6 ay sonra (gerçek log birikince) ekle. Şu an v2 yapmak data fakir → kötü öneri.

**Karar:** Plan 47 iki faz — Friction Heatmap (v1, 10h) + Proaktif Öneri (v2, 15-25h). v1 önce, v2 v1 production'da 3 ay sonra.

### 🟢 Expansionist: Daha büyük fırsat?

Auto-Tuning **workflow** ile sınırlı kalmasın. Aynı pattern detector:
- **Reports usage:** %80+ kullanılmayan rapor → archive öneri
- **SOP read:** %10- okuma oranı → "bu prosedür gözden geçir" öneri
- **Form completion:** %30 abandonment → "form çok uzun" öneri
- **Notification noise:** kullanıcı %95 sound off → "bildirimi kapat" öneri

Plan 47 expansion adayı (Plan 47.1, daha sonra).

### ⚪ Outsider: Garip ne?

"Niye LLM? Bunlar basit SQL aggregation + threshold rule. LLM overkill."

**Cevap:** Doğru — pattern **detection** SQL+rule, narrative **generation** LLM. Plan 47 hibrit: heuristic detect + LLM "Türkçe insanlaştır" + impact hesap formula. LLM sadece son cümle. Eğer LLM kapalıysa fallback "{count} vaka, %{rate} onay — adım kaldır" Türkçe template.

### 🟡 Executor: Pazartesi sabahı?

1. ProcessOptimizationSuggestion entity + migration
2. ProcessAnalyzerJob skeleton (Hangfire register)
3. WorkflowMetrics aggregator query
4. ApprovalBypassDetector tek pattern (MVP)
5. Hardcoded template Turkish summary (LLM henüz yok)
6. Admin Inbox sayfa skeleton (filter + list)
7. Test: 1 sentetik dataset → bir öneri çıkar

---

## 5. Riskler

| Risk | Olasılık | Etki | Mitigation |
|---|---|---|---|
| LLM hallucinate öneri → güven kaybı | yüksek | yüksek | Heuristic + LLM hybrid; LLM sadece narrative; post-accept impact tracking |
| Yetersiz log data (yeni workflow) → false pattern | yüksek | orta | Minimum N=50 vaka eşiği; "data yeterli değil" durumu |
| Accept aksiyonu workflow template'i kırar | düşük | yüksek | Diff preview + 2-step confirm + rollback (audit'ten geri yükle) |
| Suggestion noise (çok öneri, yönetici görmezden) | orta | orta | Severity threshold + daily digest (tek mail/gün) + 1 öneri/template max |
| KVKK: workflow log analytical processing | düşük | orta | Aydınlatma metni güncelle; aggregated metric → anonymized OK |
| Auto-edit yetkisi suistimal (admin değiştirebilir) | düşük | yüksek | Accept role-based + accept log + workflow template versioning |

---

## 6. Done Criteria

- [ ] Migration: `ProcessOptimizationSuggestions` tablo + lookup `processOptSeverity` + `processOptStatus`
- [ ] `ProcessAnalyzerJob` Hangfire registered (daily 02:00 TR)
- [ ] `IPatternDetector` interface + en az 3 implementation (bypass, bottleneck, dead_end)
- [ ] `IOptimizationNarrator` LLM wrapper + heuristic fallback
- [ ] Admin Inbox `/Admin/ProcessSuggestions` filter+list+detail+accept/reject
- [ ] Accept akışı: workflow template auto-edit + diff preview + audit
- [ ] Post-accept impact tracking (30 gün sonra "gerçek tasarruf") trend chart
- [ ] ADR-029 yazımı (auto-tuning + LLM narrator pattern)
- [ ] Test 15+ (pattern detector unit + integration)
- [ ] Sentetik dataset end-to-end senaryosu

---

## 7. Rollback

- ProcessAnalyzerJob remove + suggestion tablo DROP (rollback safe — audit'te kayıt)
- Accept edilen workflow template değişiklikleri: WorkflowTemplate.Version pattern (Plan 36) ile önceki versiyona dön

---

## 8. Adımlar / Fazlar

### Faz 1 — Schema + entity + job skeleton (5h)
- Migration NN_ProcessOptimizationSuggestions.sql
- Entity + EF config + lookup seed (severity/status)
- Hangfire job register

### Faz 2 — Pattern detectors (8h)
- `IPatternDetector` + 3 implementation
- `WorkflowMetrics` aggregator (Plan 36 log query)
- Unit test 12+

### Faz 3 — LLM narrator + heuristic fallback (6h)
- Qwen prompt + JSON response
- Skill catalog "süreç optimizasyon" markdown
- Heuristic Turkish template

### Faz 4 — Admin Inbox UI + Accept akışı (10h)
- /Admin/ProcessSuggestions sayfa
- List + detail + accept/reject/defer
- Workflow template auto-edit + diff preview
- Audit log

### Faz 5 — Impact tracking + ADR (6h)
- Post-accept 30-gün metric capture
- Trend chart
- ADR-029
- KVKK aydınlatma güncelleme

---

## 9. Bağımlılıklar

- **Prereq:** Plan 36 Workflow Faz B+C (event sourcing log)
- **Prereq:** Plan 42 Process Runtime Faz 0-2 (instance log)
- **Reuse:** Plan 34.1 LLamaSharp + Qwen + skill catalog
- **Reuse:** Plan 17 NotificationService (Inbox badge)
- **Bağımsız:** Plan 44, 45, 46

---

## 10. Açık Sorular

1. **LLM zorunlu mu, yoksa heuristic-only mode?** — Önerim: hibrit + LLM opt-in. Production LLM kapalı default; admin "AI önerileri aç" toggle.
2. **Severity threshold default ne?** — Önerim: Medium+ visible (Info noise yüksek). Admin filter ile Info görünür.
3. **Accept auto-apply mi staging mi?** — Önerim: staging — accept sonrası template "Pending Activation" status, 24h preview window, sonra apply.
4. **Cross-firma benchmark ileride mi?** — BKM tek-tenant ama gelecek SaaS dönüşümde "benzer şirketler bu pattern'i kabul etti" sosyal kanıt değerli. Plan 47.2 adayı.
5. **Hallucinate olursa user feedback nasıl toplanır?** — Reject reason çoktan seçmeli (yanlış evidence / yanlış impact / iyi öneri ama zaman değil); LoRA fine-tune data (Plan 34.1 Faz 7 pattern reuse).
