# Mosaik — Kapsam Sınırı + Rakip Benchmark (2026-06-29)

_`mosaik-portal-danismani` kapsam review (CORE/ADJACENT/OUT) + 2 paralel web-research (global digital-workplace/ops + Türkiye pazarı). Soru: portalda ne OLMALI, ne OLMAMALI, rakipler ne yapıyor. Çerçeve: `docs/RESEARCH_VISION_2026-06-29.md` EK (Gartner DEX/ClearBox/Wardley/Kano)._

## 1. Mevcut envanter — sınıflandırma (danışman)

**CORE (canlı, koru):** Reports, Dashboard, Notifications, OrgChart, Auth/RBAC/Admin, AuditLog.
**ADJACENT (canlı/yarım, şirket süreç sahibi → IN):** Tamim/Circular ✅, SOP ✅, Workflow engine (designer yarım, Plan 36), Contracts/Obligations/Compliance (İZLE), Documents (%60), Calendar (%75), AI extraction.
**ADJACENT (taslak/yok):** Form Builder (Plan 41), KVKK/Process backbone (Plan 40), Process Execution (Plan 42), Comment/Mention (Plan 35).

## 2. EKSİK must-have (kapatılmalı — öncelik sırası)

### A. Platform CORE bağ-dokusu (en yüksek — "modül yığını → platform" eşiği)
1. **Unified Inbox (platform servisi)** — şu an `WorkflowInboxService` SADECE workflow item'ı veriyor; `/Inbox` + `IInboxProvider` (ADR-018 opt-in) + çok-modül aggregation YOK. OI'nin "exception→aktör" yüzeyi. (global: Appian case mgmt + tüm intranet'ler table-stakes.)
2. **Cross-module search** — kodda 0 (sadece ADR-018 taslak). Rakiplerin #1 pazarladığı özellik (Simpplr/LumApps/Unily). SQL Server full-text yeter — **kendi vector altyapısı kurma** (do-NOT-build).
3. **Dashboard→aksiyon alert** — eşik aşınca `INotificationService` fire = OI≠BI literal tanımı. Hangfire+Notification ile düşük effort.
4. **Notification digest** — batched/akıllı zamanlama (global table-stakes, ucuz, yüksek adoption).

### B. Türkiye table-stakes (kredibilite eşiği — yerel rakipler standart sunuyor)
5. **e-İmza entegrasyonu (5070)** — Logo Flow/eBA standart; sözleşme+tamim için. **En büyük TR parite boşluğu.** (Kısıt: teminat/kefalet/resmî şekle tabi işlemler e-imza ile yapılamaz — modelde dikkat.) CLM tarafında global table-stakes de.
6. **İK self-servis** (izinlerim/masraf/bordro-görüntüleme/eğitim) — TR intranet standart modülü, Mosaik'te YOK. (Bordro/SGK hesaplama DEĞİL — o ERP işi, entegre et.)
7. **No-code workflow designer** (vekalet/eskalasyon/grade-bazlı) — Logo Flow/Netsis E-Flow standart; Plan 36 designer'ı bunu karşılamalı.

### C. KVKK modülünü tamamla (yarım privacy modülü kredibilite vermez)
8. **DSAR/DSR akışı** (veri-sahibi talep intake→fulfillment, KVKK m.13 30-gün, clock-pause-on-verification) — OneTrust/DataGrail table-stakes.
9. **VERBİS-format export + açık rıza yönetimi + 72h ihlal bildirimi + otomatik imha tetikleyici** — TR KVKK araçları (Egebilgi/KVKSİS) standart.

### D. Diğer
10. **Audit/ops analytics dashboard** (SLA breach, bottleneck, engagement) — SP-reporting motoru var, yüzeye çıkar.
11. **DYS olgunlaştırma** (versioning + tam metin arama) — Documents Plan 27 Faz C (DocumentVersions ✅, FTS config + metadata eksik).
12. **e-İmza tarafı $0 yol:** signature_pad + audit-hash (DocuSign şart değil tek-şirket içi).

## 3. OLMAMALI — OUT / CUT / SKIP

**OUT (system-of-record → entegre et, ASLA yeniden yazma):** ERP/muhasebe ledger, CRM, HRIS/bordro/SGK hesaplama, data-warehouse/heavy-BI, real-time chat/email (Teams/Exchange), e-Devlet, enterprise GRC SaaS-ölçek, Jira-ölçek PM. (VISION §7.8 "Invisible ERP / Logo connector" = INTEGRATE, modül yapma.)

**do-NOT-build (OSS reuse — Mosaik zaten doğru):** auth/SSO, chat, sıfırdan BI, form engine (SurveyJS ADR-020), PDF engine (Gotenberg ADR-021), full-text/vector search altyapısı, notification/email altyapısı, workflow engine (Stateless ADR-019). Bu disiplini bozma.

**CUT (onaylı ama scope-creep red-flag — yeniden değerlendir):**
- **Plan 49 Zero-UI Ops (biyometrik)** — biyometrik = KVKK en yüksek-risk; KVKK envanteri yaparken kendin biyometrik toplama ironisi. CUT veya çok-dar pilot.
- **Plan 51 Differential Privacy** — 200-300 kişi tek-tenant'ta epsilon-budget akademik overkill (Kano: Indifferent). UserDataFilter zaten izolasyon. CUT.
- **Plan 50 Shadow Org Graph** — @mention/bypass davranış analizi = workplace-surveillance red-flag, çalışan güveni + KVKK aydınlatma yükü. İZLE/CUT, strategic review.
> Bu 3'ü VISION'da ✅ onaylı (2026-05-25) ama onay kapsam-disiplininden önce. Karar kullanıcıya.

**SKIP/DEFER (NICE, tek-şirket düşük ROI):** SCIM provisioning, otomatik data-discovery/scanning, process mining + generative NL-workflow, public API/webhooks, native mobile app (PWA önce), KEP (orta öncelik — özel sektör kamu kadar kritik değil), şirket-içi sosyal feed.

## 4. DIFFERENTIATOR — koru + pazarla (rakip yapısal olarak eşleşemez)

1. **Local-LLM KVKK-safe on-prem AI** (Qwen/LLamaSharp in-process) — OneTrust/DataGrail/Securiti + tüm AI intranet'ler cloud LLM'e veri yolluyor = KVKK yurt dışı aktarım blocker'ı (Eylül 2024 rejimi sıkı). Mosaik **sıfır veri-egress** ile chat-over-data + clause extraction + summarization. **Gerçek wedge.**
2. **KVKK-native** (6698 madde-mapping, VERBİS kategori, TR saklama hukuku) — generic GDPR-first GRC'den keskin.
3. **SP-driven canlı veri reporting** — ETL/connector vergisi yok; Appian 200+ connector ile ulaşmaya çalışıyor, Mosaik zaten içinde.
4. **Tek-şirket birebir fit + $0 OSS** — multi-tenant SaaS config bloat + seat-cost yok.
5. **Entegre suite** — rakip Tallyfy+OneTrust+Ironclad+Simpplr+workflow stitch ediyor; Mosaik tek auth/audit/UI altında birleştiriyor — entegrasyon = değer.

> **Pazarlama merkezi:** on-prem + KVKK-safe AI + birebir uyum.

## 5. Net aksiyon (kapsam-disiplinli sıra)
vNext kalbi 6 (Plan 40/41/42/35/36/37) + **3 eksik CORE** (inbox/search/alert) önce. Sonra TR table-stakes (e-imza, İK self-servis, KVKK tamamlama). Plan 47/48 spekülatif → Plan 42 runtime verisi birikmeden başlama. Plan 49/50/51 → CUT review. Plan 53 (~300-380h) kapsam genişliği = risk; walking-skeleton sınırı çiz.

## Kaynaklar (özet)
Global: Gartner DEX, ClearBox 2024, Simpplr/LumApps/Unily/Staffbase, Appian/Pega/Camunda, Tallyfy/Process Street/Scribe, OneTrust/DataGrail/Securiti, Ironclad/DocuSign CLM, WorkOS enterprise-readiness. · TR: Logo Flow, Bimser eBA, Masraff, DNA Portal (İK), SECUBE/ISODECK (EBYS/DYS), Egebilgi/KVKSİS (KVKK), 5070 e-İmza Kanunu, KVKK ihlal bildirimi + yurt dışı aktarım rehberi. Tam URL'ler task transcript'lerinde.
