# Plan 54 — vNext Yol Haritası v2 (Kapsam-Disiplinli, Rakip-Hizalı)

**Durum:** ⏳ TASLAK 2026-06-29 — onay bekliyor
**Tier:** 3 (master roadmap — Plan 53 + Plan 16'yı SUPERSEDE eder)
**Tetik:** Kullanıcı 2026-06-29 — "tüm planları elden geçir, sıralamayı tekrar yap, ciddi bir yol haritası"
**Girdi:** Bu oturumun 4 araştırması — vizyon best-practice ([RESEARCH_VISION](../docs/RESEARCH_VISION_2026-06-29.md)), kapsam + rakip benchmark ([COMPETITIVE_SCOPE](../docs/COMPETITIVE_SCOPE_2026-06-29.md)), `mosaik-portal-danismani` kapsam review.

---

## 1. Neden yeni roadmap

Plan 53 (2026-05-25 master) iki kusurla maluldü:
1. **Scope-creep içeriyor** — Plan 49 (biyometrik Zero-UI), 50 (shadow-org surveillance), 51 (differential privacy) onaylı ama kapsam-sınır lens'inde red-flag (KVKK riski / tek-tenant overkill / surveillance). Plan 48 spekülatif.
2. **Rakip table-stakes eksik** — global + TR benchmark'ın "olmazsa kredibilite yok" dediği 6 yetenek Plan 53'te yok: unified inbox, cross-module search, dashboard→alert, e-imza, İK self-servis, KVKK tamamlama (DSAR/VERBİS/ihlal/imha).

Bu plan: **önce platform bağ-dokusu + table-stakes, sonra differentiator, scope-creep park.** Plan 53'ün "8 modülü 12 haftada" hırsı walking-skeleton sınırı ile değiştirilir.

## 2. Önceliklendirme metodu

Her kalem 4 lensten geçti (`mosaik-portal-danismani` çerçevesi): (a) CORE/ADJACENT/OUT, (b) table-stakes mi differentiator mı nice mı, (c) dependency DAG konumu, (d) Kano (Must/Performance/Delighter/Indifferent) → RICE kabası. Kuzey yıldızı: **Operational Intelligence = detect→notify-actor→act→audit.** Wedge: **local-LLM KVKK-safe AI** (koru + pazarla).

## 3. Plan envanteri — yeniden sınıflama

| Plan | Eski durum | Yeni karar | Tier | Gerekçe |
|---|---|---|---|---|
| 32 SMTP/scheduled | Taslak | **YAP** (prereq) | S0 | Notification/comment/digest hepsinin altyapısı |
| **(yeni) Unified Inbox** | Plan 37 taslak (ADR-018) | **YAP** | S1 | Eksik CORE — OI exception→aktör yüzeyi |
| **(yeni) Cross-module Search** | ADR-018 taslak | **YAP** | S1 | Eksik CORE — modül→platform eşiği, #1 rakip özelliği |
| **(yeni) Dashboard→Alert** | yok | **YAP** | S1 | OI≠BI literal tanımı, düşük effort (Hangfire+Notification) |
| 36 Workflow Designer | Onaylandı | **YAP** | 1 | vNext kalbi; no-code designer + vekalet/eskalasyon (TR table-stakes) |
| 35 Comment/Mention | yok | **YAP** | 1 | Cross-cutting CORE-capability; polymorphic + fan-out-on-write |
| 40 KVKK backbone | Taslak | **YAP** (düzelt) | 1 | + ProcessingPurpose/Recipient/VerbisRegistration lookup + Pattern 9 (research) |
| 41 Form Builder | Taslak | **YAP** (düzelt) | 1 | KRİTİK PATH; + FormVersion snapshot, SurveyJS reuse (research) |
| 42 Process Exec Runtime | Taslak | **YAP** (düzelt) | 1 | Birleştirici; **6 aspect → tek ProcessActivity stream** (research) |
| **(yeni) KVKK tamamlama** | yok | **YAP** | 2 | DSAR akışı + VERBİS export + açık rıza + 72h ihlal + otomatik imha (TR table-stakes) |
| **(yeni) e-İmza (5070)** | yok | **YAP** | 2 | En büyük TR parite boşluğu (sözleşme+tamim); $0 signature_pad+audit-hash |
| **(yeni) İK self-servis** | yok | **YAP** | 2 | TR table-stakes (izin/masraf/bordro-görüntüleme); bordro hesabı DEĞİL (ERP) |
| **(yeni) Audit/ops analytics** | yok | **YAP** | 2 | SP-motoru var, SLA/bottleneck/engagement yüzeye çıkar |
| 27 Documents Faz C | Kısmi | **YAP** | 2 | DYS olgunlaştırma: C-02 check-in/out, C-04 FTS, C-05 metadata, C-07 audit (74/DocVersions ✅) |
| 44 RAG Permission Guard | ✅ Onaylı | **YAP** | 3 | Differentiator AI temeli |
| 45 Excel-to-Process AI | ✅ Onaylı | **YAP** | 3 | Differentiator; AI ingestion |
| **(yeni) AI chat-over-data** | DocumentChat iskelet | **YAP** (productize) | 3 | Wedge; local-LLM + citation, RAG-permission sınırında |
| 46 PWA offline+camera | ✅ Onaylı | **İZLE** | Park | Saha ihtiyacı DOĞRULA; yoksa 5-7h gold-plating |
| 47 Auto-tuning advisor | ✅ Onaylı | **ERTELE** | Park | Plan 42 runtime verisi birikmeden anlamsız |
| 48 Executable SOP | Araştırma | **ERTELE** | Park | Spekülatif; Plan 42 sonrası, kanıt yok |
| 49 Zero-UI biyometrik | ✅ Onaylı | **CUT** | ❌ | Biyometrik = KVKK en yüksek-risk; KVKK envanteri yaparken biyometrik toplama ironisi |
| 50 Shadow Org Graph | ✅ Onaylı | **CUT** | ❌ | Workplace-surveillance red-flag; güven + KVKK yükü |
| 51 Differential Privacy | ✅ Onaylı | **CUT** | ❌ | 200-300 kişi tek-tenant'ta epsilon-budget akademik overkill; UserDataFilter yeter |
| 52 Trend Endeksi | Plan 53 içi | **İZLE** | Park | Nice; analytics değerli ama vNext kalbi değil |
| 14 Filter prod-ready | Kısmi | **KAPAT→Plan 18** | — | Faz B Plan 18'e bağlı; arşiv-notu |
| 16 vNext roadmap | Aktif | **SUPERSEDE** | — | Bu plan devralır → arşiv |
| 53 Unified master | ✅ Onaylı | **SUPERSEDE** | — | Bu plan v2; 53 arşiv (scope-creep düzeltildi) |
| 21 Lookup | ✅ | tamam | — | Arşivlendi (`1bcebfe`) |

## 4. Faz roadmap (dependency-sıralı)

```
S0  Foundation:     Plan 32 SMTP + IEmailService doğrula.                         (~1 hafta)
S1  Platform bağ-dokusu (EKSİK CORE — en yüksek kaldıraç):
      Unified Inbox (IInboxProvider/ADR-018) + Cross-module Search (SQL FTS) +
      Dashboard→Alert (Hangfire eşik→Notification).                              (~2-3 hafta)
1   vNext kalbi (process backbone — differentiator çekirdek):
      40 KVKK backbone(düzelt) → 41 Form Builder(+FormVersion) → 42 ProcessExec(tek-stream)
      ∥ 36 Workflow Designer  ∥ 35 Comment/Mention.                              (~5-6 hafta)
2   TR table-stakes (kredibilite):
      KVKK tamamlama(DSAR/VERBİS/ihlal/imha) + e-İmza(5070) + İK self-servis +
      Audit/ops analytics + 27 Documents Faz C(DYS).                            (~4-5 hafta)
3   Differentiator AI (mevcut local-LLM kaldıracı):
      44 RAG Guard + 45 Excel-to-Process + AI chat-over-data(citation).         (~3-4 hafta)
Park (ertele/doğrula):  46 PWA, 47 Auto-tuning, 48 Executable-SOP, 52 Trend.
CUT (review):           49 Zero-UI-biyometrik, 50 Shadow-Org, 51 Differential-Privacy.
```

**Kritik path:** S0 → Plan 40 → Plan 41 (FormVersion) → Plan 42 (tek-stream). Form Builder hâlâ darboğaz (Plan 42 prereq). S1 paralel gidebilir (40/41/42'den bağımsız altyapı). Tier 2 e-imza/İK/KVKK-tamamlama Tier 1 bitmeden başlayabilir (ayrı hat).

## 5. CUT kararları — gerekçe (onay ister)

49/50/51 VISION'da ✅ onaylı (2026-05-25) ama onay **kapsam-disiplini araştırmasından önce**. Kapsam lens'i:
- **49 Zero-UI biyometrik:** biyometrik veri KVKK m.6 özel nitelikli; KVKK uyum modülü yapan portal kendi biyometrik toplama riski üretemez. → CUT veya ses-only (biyometrik olmayan) dar pilot.
- **50 Shadow Org Graph:** çalışan davranış grafiği = surveillance; KVKK aydınlatma + güven maliyeti mimariyle çözülmez. → CUT veya anonim-agregat-only.
- **51 Differential Privacy:** tek-tenant 200-300 kişi; UserDataFilter zaten izolasyon. Laplace epsilon-budget akademik. → CUT.

Bütçe geri kazanımı: ~180-280h (49:80-120h + 50 + 51:50-80h) → table-stakes'e yönlendirilir.

## 6. Yeni plan adayları (henüz dosya yok — onay sonrası yazılır)

S1 + Tier 2'deki "(yeni)" kalemler plan dosyası ister: Plan 37 (Unified Inbox — ADR-018 mevcut, plan yaz), Plan 55 (Cross-module Search), Plan 56 (Dashboard→Alert/EscalationRule), Plan 57 (KVKK DSAR/VERBİS — Plan 40 alt-faz olabilir), Plan 58 (e-İmza 5070), Plan 59 (İK self-servis), Plan 60 (Audit/ops analytics). Her biri Tier 3 → onay + 5-paralel-research (OSS reuse) pattern.

## 7. Done criteria (bu roadmap planı)
- [ ] Kullanıcı tier sıralamasını + CUT kararlarını (49/50/51) onaylar
- [ ] Plan 53 + 16 → `plans/archive/` (superseded notu)
- [ ] Düzeltmeler (40 lookup, 41 FormVersion, 42 tek-stream) ilgili plan dosyalarına fold
- [ ] S0/S1 ilk kalemler için plan dosyası + TODO.md faz girişi

## 8. Rollback
Bu bir roadmap belgesi — kod yok. Reddedilirse `plans/archive/`, Plan 53 aktif kalır.

## 9. Onay
- [ ] Tier sıralaması onaylandı: ___
- [ ] CUT 49/50/51 onaylandı / reddedildi: ___
- [ ] S0/S1 başlangıç onayı: ___

## İlişkili
- [COMPETITIVE_SCOPE_2026-06-29](../docs/COMPETITIVE_SCOPE_2026-06-29.md) — kapsam + rakip parity gap
- [RESEARCH_VISION_2026-06-29](../docs/RESEARCH_VISION_2026-06-29.md) — best-practice + 3 düzeltme
- `docs/VISION.md` §7 — kuzey yıldızı · `.claude/agents/mosaik-portal-danismani.md` — kapsam çerçevesi
- Plan 53 (superseded), Plan 16 (superseded), Plan 38 ✅, ADR-018 (inbox/search opt-in)
