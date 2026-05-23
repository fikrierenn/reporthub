# Plan 50 — Shadow Organization Graph (Gerçek İş Ağı)

**Durum:** ✅ **ONAYLANDI 2026-05-25** — revize v2 (workplace surveillance mimari zırh + 5 açık soru kapatma)
**Tier:** 3 (yeni paradigma + graph analytics + cross-modül log analiz + KVKK hassas)
**Effort:** 40-60h (5 faz, 3-4 hafta) — tahmini
**Aciliyet:** 🟣 Plan 35 (Comment/Mention) ✅ + Plan 36 (Workflow log) ✅ + Plan 38 (EntityRelations) ✅ sonrası

---

## 1. Vizyon

Resmi organizasyon şeması = **yalan**. İşler resmi hiyerarşiye göre değil, gayriresmi "kimin iş bitirdiği, kimin kime danıştığı" ağına göre yürür.

**Radikal fikir:** Mosaik resmi şemanın **arkasındaki gerçek operasyonel güç ve bilgi ağını** (Shadow Hierarchy) görselleştirir.

Onay logları + @mention + comment thread + approval bypass pattern'leri analiz edilir. **Observed Graph** çıkar — resmi vs gerçek farkı dashboard.

---

## 2. Senaryo (BKM gerçek)

> Resmi: Ahmet Bey IT Departman Yöneticisi.
>
> **6 ay log analizi:**
> - Onay zincirindeki zor kararlar %78 oranında Elif Hanım'a @mention atılıyor
> - Elif yorum yapmadan workflow %95 ilerlemiyor (anomali — Elif resmi onay node'unda DEĞİL)
> - Documents thread'lerinde Elif "soru sorulan" en sık 1. kişi (IT konusu)
> - Comment'larda Elif → diğerleri reply oranı %92 (knowledge hub sinyali)
>
> **Observed Graph görselleştirme:**
> - Resmi: Genel Müdür → Ahmet → IT ekip
> - Gerçek: Genel Müdür → Ahmet (resmi) | Genel Müdür → Elif (de facto IT lider)
> - Anomali skoru: 0.72 (yüksek shadow influence)
>
> **AI önerisi:**
> *"IT konusunda kritik kararlarda Elif Hanım'a doğrudan danışılması organizasyonel verimliliği %15-20 artırabilir. Resmi rol değiştirmeden 'IT Knowledge Lead' subject expert ünvanı tanımlanabilir."*

Genel Müdür **gerçek bilgi merkezi**ni görür. Resmi şema yenilenir veya gerçek lider tanınır.

---

## 3. Mimari (ön araştırma)

### 3.1 Component'ler

```
Mosaik.Modules.Intelligence/
├─ Services/
│  ├─ Graph/
│  │  ├─ IShadowGraphBuilder.cs           — log queries → edge weight build
│  │  ├─ ShadowGraphBuilder.cs            — Hangfire daily 03:00
│  │  ├─ EdgeWeightCalculator.cs          — formula: mention_freq + reply_rate + workflow_dep
│  │  └─ AnomalyDetector.cs               — official vs observed diff scorer
│  ├─ Visualization/
│  │  ├─ GraphRendererService.cs          — Cytoscape.js node+edge JSON
│  │  └─ ComparisonViewService.cs         — official Org Chart vs observed overlay
│  └─ Privacy/
│     ├─ KvkkGraphAnonymizer.cs           — düşük yetkili kullanıcı için ID anonimize
│     └─ ConsentChecker.cs                — KVKK aydınlatma onay tracking
├─ Entities/
│  ├─ ShadowEdge.cs                       — { FromUserId, ToUserId, Type, Weight, FirstSeen, LastSeen }
│  ├─ InfluenceScore.cs                   — { UserId, Topic, ScoreType, Score, ComputedAt }
│  └─ AnomalyDetection.cs
├─ Areas/Intelligence/
│  ├─ Controllers/ShadowGraphController.cs  — /Intelligence/ShadowGraph (admin only)
│  └─ Views/ShadowGraph/
│     ├─ Index.cshtml                       — Cytoscape.js graph + filter (topic/dept/period)
│     └─ AnomalyReport.cshtml               — official vs observed diff table
└─ Database/
   └─ NN_ShadowGraph.sql
```

### 3.2 Edge Tipleri + Weight Formülü

```
Edge tipi             | Kaynak                              | Weight katkı
----------------------|-------------------------------------|---------------
mention_target        | Comment.@MentionedUserId            | 0.3 × frequency
mention_source        | Comment.AuthorId → MentionedUserId  | 0.3 × frequency
workflow_dependency   | WorkflowInstance step bypass        | 0.4 × (1 - approve_rate)
reply_thread          | Comment threading                    | 0.2 × reply_count
approval_consult      | Workflow note "X'e danışıldı"        | 0.5 × note_count
doc_consult           | DocumentChunk retrieval by user      | 0.1 × access_count
```

**Edge weight time-decay:**
```
weight(t) = sum(events) × exp(-λ × (now - event_time))
λ = 1/180day (6 ay yarı-ömür)
```

### 3.3 Influence Score

Per-user per-topic skor (topic = department / project / SOP category):

```
InfluenceScore[user, topic] =
  Σ outgoing_edges_weight(user, topic)      // ne kadar konuşuluyor
  + Σ incoming_edges_weight(user, topic) × 0.5  // ne kadar dinleniyor
  + bypass_premium(user, topic)              // resmi onay node'unda olmadan etkileyenler
```

Bypass premium = official Org Chart'ta yok ama @mention/danışıldı oranı yüksek → kritik sinyal.

### 3.4 Görselleştirme (Cytoscape.js)

```
Sol pane: Resmi Org Chart (dabeng/OrgChart Plan 20 reuse)
Sağ pane: Observed Graph (Cytoscape.js force-directed)
  - Node: User (boyut = total influence score)
  - Edge: Shadow edge (kalınlık = weight, renk = type)
  - Anomaly user'lar kırmızı highlight
  - Filter: topic, period, department, score threshold
```

---

## 4. KVKK + Etik Riskler

| Risk | Etki | Mitigation |
|---|---|---|
| Workplace surveillance — çalışan davranış izleme | **CRITICAL** | KVKK aydınlatma + İK politikası onayı + DPO consult; sadece **aggregate edge weight** (per-mesaj log gösterimi YOK) |
| Yöneticinin "Elif benim yerime karar alıyor" reaksiyon | yüksek | Sadece **admin/üst yönetim** erişim; orta-yönetici görmesin |
| Anomaly skoru yanlış → kişisel hak ihlali | yüksek | Confidence + sample size threshold (N=50+ event); manuel admin review |
| Network effect — popüler kişi "etkili" zannedilir | orta | Random baseline (Erdős-Rényi) ile karşılaştır; sadece anomalous shadow influence flag |
| Ses/metin biometrik veri analizi (Plan 49 + Plan 50 birleşim) | yüksek | Plan 50 sadece text/event log; ses-bazlı influence skip |
| Topic detection yanlış (IT konusu zannedilir, gerçekte HR) | orta | Topic = entity_type/category (deterministic), AI topic detect opsiyonel + onay |
| Negative shadow ("kötü influence" yaratabilir mi?) | düşük | Sadece **positive influence** ölç (consult/danışı/onay); negative pattern Plan 50.1 ayrı plan |

---

## 5. Bağımlılıklar (ön)

- **Hard prereq:** Plan 35 Comment/Mention (@MentionedUserId logging)
- **Hard prereq:** Plan 36 Workflow Designer (WorkflowInstanceLog event sourcing)
- **Hard prereq:** Plan 38 EntityRelations + DecisionLog (knowledge consult logging)
- **Reuse:** Plan 20 OrgChart (dabeng) — comparison view
- **Reuse:** Plan 34.1 Qwen (topic detection opsiyonel)

**Yeni JS:**
- Cytoscape.js 3.x (MIT) — graph layout + interaction
- cytoscape-fcose layout (MIT) — force-directed

---

## 6. Deep Dive Sonraki Adımlar

1. **KVKK + İK ön onay:** DPO consult — bu analiz çalışan rıza gerektiriyor mu? Aydınlatma metni hazırla.
2. **5 lens deep tradeoff:**
   - **Contrarian:** "DB-side row level security (RLS) varken niye app-side?" — RLS SP tarafında: SP yazımı karmaşıklaşır, mevcut 200+ SP yeniden yazmak imkansız. App-side: bir kez yaz, tüm SP'leri sanitize.
   - **First Principles:** Asıl problem 50+ yetki tablosu yönetememe → centralized policy DSL.
   - **Expansionist:** Aynı policy framework Plan 27 Documents + Plan 41 Form export'a uygulanır (universal sanitize).
   - **Outsider:** Microsoft Viva Glint pattern, kurumsal analytics ürünleri),
   - **Executor:** POC
3. **POC (1 hafta):** Var olan 6 ay mention+workflow log üzerinde 10 user için influence score hesapla, manuel doğrulama (yönetici "evet/hayır bu gerçek").
4. **ADR-032 adayı:** "Shadow graph analytics + KVKK aggregate-only constraint"
5. **Erişim policy:** Sadece Genel Müdür + İK Direktör + DPO. Default disabled, admin toggle.

---

## 7. Bu plan ne zaman aktif olur?

Plan 35 Comment + Plan 36 Workflow + Plan 38 EntityRelations production'da 6+ ay log birikince. **Erken implement = data fakir = false anomaly.**

**Tahmini başlangıç:** 2027 Q1 (Şubat-Mart).

---

## 8. Açık Sorular — KAPATILDI 2026-05-25 (revize v2 kararları)

### Mimari Zırh — Workplace Surveillance Engeli (CRITICAL)

Çalışanların **birbirleriyle olan anlık yazışmalarının detayları asla ekranda listelenmez**. Mosaik shadow graph **sadece anonymize edilmiş aggregated edge weight** gösterir.

**Zorunlu kural:**
```csharp
// ShadowEdge entity sadece sayısal weight saklar
public class ShadowEdge {
    public int Id;
    public int FromUserId;
    public int ToUserId;
    public string Type;          // mention_target / workflow_dep / vs.
    public decimal Weight;       // 0-1 aggregate sayısal
    public int EventCount;       // sample size sayısı
    public DateTime FirstSeen;
    public DateTime LastSeen;
    // !!! Comment.Text, mention sentence, workflow note text → YOK !!!
}

// Render-time veri akışı
ShadowGraphController.Index → GraphRendererService.GetGraph(period) →
    SELECT FromUserId, ToUserId, Type, SUM(Weight), SUM(EventCount)
    FROM ShadowEdges
    GROUP BY FromUserId, ToUserId, Type
    // Per-mesaj log SORGULANMIYOR, sadece aggregate
```

**KVKK aydınlatma metni** zorunlu: "Şirket içi iletişim sıklığı + paterni analiz edilir; içerik analiz edilmez." DPO consult Plan başlangıcında.

### 1. Topic detection deterministic mi AI mi?

**Karar:** **Deterministic Öncelikli, AI v2.**

Konu eşleme veritabanı düzeyinde:
- Etkileşim entity tipi (SopCategory, FormType, DocumentCategory) → topic
- Departman anahtarı (User.DepartmentId) → topic context
- Workflow template kategori → topic

LLM tabanlı sınıflandırma v2 (Plan 50.1) — deterministic yeterli olmadığında.

### 2. Anomaly threshold default ne?

**Karar:** **`shadow_influence > 0.5 + sample_size ≥ 50 + official_role_match = false`**

```csharp
bool IsShadowAnomaly(InfluenceScore score, User user) =>
    score.ShadowInfluence > 0.5
    && score.SampleSize >= 50            // son 6 ay min 50 event
    && !OfficialRoleMatchesTopic(user, score.Topic);
```

Yorum: "Resmi rolü yokken son 6 ayda 50+ kez @mention/danışma alan = gölge lider".

### 3. Erişim yetkisi kimlerde?

**Karar:** **Sadece Genel Müdür + İK Direktörü + DPO (Veri Koruma Görevlisi).**

- Orta yöneticilerin görmesi → ast-üst gerilimi ("Elif benim yerime karar alıyor") + mobbing riski
- Plan 32 ModuleRoleAccess pattern: `intelligence_shadow_graph` access scope
- Default DISABLED, admin toggle ile aç
- Erişim log: kim ne zaman shadow graph baktı (audit)

### 4. Bypass premium nasıl hesaplanır?

**Karar:** **`(bypass_approvals + mention_consults) / total_decisions`**

```csharp
decimal CalculateBypassPremium(User u, string topic, DateRange period)
{
    var bypassApprovals = workflowLog.Where(w =>
        w.MentionedUserId == u.Id
        && !workflowTemplate.HasUserInApprovalChain(u.Id, topic)
        && w.Period.Within(period)
    ).Count();

    var totalDecisions = workflowLog.Where(w => w.Topic == topic).Count();

    return totalDecisions > 0
        ? (decimal)(bypassApprovals + mentionConsults) / totalDecisions
        : 0m;
}
```

Yorum: "Onay şemasında adı yokken kararları etkileyen sıklığı".

### 5. Negative influence (engelleyici) v1'de?

**Karar:** **HAYIR — v1 Sadece Pozitif İş Birliği.**

- Engelleyici/red-eden analiz → iş yeri etiği + mobbing tetikleyici
- v1 sadece **pozitif edge tipleri** (consult, danışı, onay, reply, mention)
- v2 (Plan 50.1) negatif pattern ayrı plan — DPO + İK + Hukuk üçlü onay
- Reject reason analiz Plan 47 Auto-Tuning kapsamında (workflow optimize bağlamı), Plan 50 kapsamı dışında

---

## 9. Onay + Revize Notu

**ONAYLANDI 2026-05-25** — Kullanıcı strategic review (kurumsal etik + KVKK lens):

- **Mimari Zırh:** Workplace Surveillance Engeli — sadece aggregate edge weight, mesaj içerik analiz YASAK
- 5 açık soru cevaplandı + plan'a karar olarak gömüldü (§8)
- Deterministik topic detection v1, LLM v2
- Anomaly formula: `>0.5 + N≥50 + !official_role`
- Erişim: Sadece GM + İK Dir + DPO
- Bypass premium formula netleştirildi
- v1 sadece pozitif, negatif Plan 50.1
- **KVKK aydınlatma metni güncelleme:** Plan başlangıcında DPO consult zorunlu
- **Implementasyon:** Plan 35 Comment + Plan 36 Workflow + Plan 38 EntityRelations production'da 6+ ay log birikmesi sonrası
