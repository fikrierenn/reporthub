# Plan 57 — Forms → Workflow Köprüsü + Forms M-A Kapanış

**Durum:** TASLAK · **Tarih:** 2026-07-04 · **Tier:** 3 (modüller-arası, kullanıcı-görünür, yeni pattern)
**Danışman:** `mosaik-portal-danismani` (2026-07-04, conf 80-92) · **Bağlam:** Plan 56 M-A Forms (G1-G4 ✅), Plan 36 (WorkflowEngine), Plan 42 (ProcessInstance — ERTELENDİ).

---

## 1. Problem

Kullanıcı hedefi: **"izin formu doldur → imzala → amir onayla → İK'ya düş"** gibi FORM-TETİKLEMELİ ONAY SÜRECİ. Genişletilmiş vizyon: workflow motoru **portal-geneli generic onay omurgası** olsun (izin, masraf, doküman onayı, sözleşme imza, satınalma — tek motor + N tüketici).

**Gerçek durum (danışman kanıtladı):** Portalda **3 paralel onay altyapısı** var, biri bile Forms'a bağlı değil:
- **WorkflowEngine** (`Mosaik/Services/Workflow/WorkflowEngine.cs`) — CANLI ama **tetikleyicisiz** (event-sourced, 10 test yeşil, inbox+Hangfire processor bağlı; ama sadece admin elle `POST /Workflow/Trigger` başlatıyor, hiçbir modül otomatik `StartAsync` çağırmıyor). **En olgun motor.**
- `Mosaik/Services/ApprovalService.cs` — **ÖLÜ** (sadece testler çağırıyor, YonetIQ port artığı).
- `SopApprovalService` — tek gerçek canlı akış, `ApprovalRequest` entity'leriyle kendi 3-adımlı onayını yazıyor (WorkflowEngine kullanmıyor).

**Forms↔Workflow kablosu eksik:** `FormDefinition.TriggersWorkflowId` + `FormSubmission.WorkflowInstanceId` soft-ref alanları hazır; `FormSubmissionService.cs:177` trigger **çıplak yorum, sıfır kod**.

**Ayrıca Forms M-A yarım:** G5 template seed + izlenen borçlar (submission pagination, export tarih, şifreli-form dosya ekleri, locale) kapanmadı.

---

## 2. Scope

### Kapsam İÇİ
- **Part A — Forms M-A kapanış:** G5 template seed + izlenen borçların değerlendirmesi/kapanışı.
- **Part B — Forms→Workflow köprüsü (walking skeleton):** Forms submit → `IWorkflowService.StartAsync` → onaycı inbox → karar → submission kapanış. Tek-adımlı izin-onay şablonu.
- **Generic pattern kur:** köprü `EntityType="FormSubmission"` generic — sonraki tüketiciler (masraf, doküman) aynı yolu izler.

### Kapsam DIŞI (ertelenen — gerekçeli)
- **Plan 42 ProcessInstance runtime** — ERTELE. İkinci yürütme motoru = bakım felaketi; kullanıcının izin-onayı Plan 42'nin ~10x'i (yeni csproj, 6 aspect, PDF, ICS). WorkflowInstance canonical kalır; Plan 42 gelirse onu **wrap** eder (plan 42:73-78), dublicate etmez.
- **Zamanlama motoru (Hangfire/scheduled reports, Plan 32)** — AYRI omurga, zaten kısmen canlı (`TamimReminderJob`, `KvkkIntegrityScanJob`). Onay motoruna GÖMÜLMEZ. Kompozisyon mümkün (scheduled job → workflow başlat; workflow SLA-timer → Hangfire) ama bu planın kapsamı değil.
- **3 onay-altyapısı birleştirme** (`IApprovalService` refactor, VISION.md:83 ADR-024 adayı) — drive-by YOK. İzin-onay WorkflowEngine pattern'ini kurar; SOP + ApprovalService gelecekte buraya migrate (ayrı plan).
- Çok-adımlı onay / koşullu dallanma / SLA / PDF çıktı / e-imza (KEP) — sonraki fazlar.

---

## 3. Alternatifler (5 lens + reddedilenler)

**Seçilen yol: (a) Forms submit → mevcut WorkflowEngine'i `IWorkflowService` üzerinden tetikle.**

Reddedilenler:
- **(b) Plan 42 ProcessInstance inşa et** — REDDEDİLDİ: gold-plating (10x), iki-motor çakışması.
- **(c) Forms-native yeni approval** — REDDEDİLDİ: dördüncü onay altyapısı; motor zaten olgun+boşta.
- **(d) WorkflowEngine ölü, önce dirilt** — yanlış teşhis: ölü değil, trigger'ı yok; diriltme = tek köprü.

**5 lens:**
- 🔴 **Contrarian:** Fatal flaw? WorkflowEngine `DefinitionJson` şeması (sequential-workflow-designer) izin-onay için yeterince basit mi, yoksa tek-adım seed bile karmaşık mı? → Faz 2'de mevcut seeded template/step parser'dan doğrula, riski erken kapat.
- 🔵 **First Principles:** Gerçek problem "yeni motor" değil, **"olay → motor" kablosu**. Motor + inbox + advance hazır; tek eksik submit→StartAsync + 1 şablon.
- 🟢 **Expansionist:** Daha büyük fırsat — köprü `EntityType` generic olduğu için izin bittiğinde masraf/doküman/sözleşme **kod yazmadan** template ekleyerek gelir. Bu planın asıl değeri tek senaryo değil, **generic tüketici pattern'i**.
- ⚪ **Outsider:** Yabancı ne garip bulur? 3 paralel onay altyapısı + hiçbiri forma bağlı değil + en iyi motor kullanılmıyor. Köprü bu israfı düzeltir.
- 🟡 **Executor:** Pazartesi ilk adım — `FormSubmissionService`'e `IWorkflowService` inject + `:177` stub'ı gerçekle (G4 bildirim deseniyle aynı best-effort).

---

## 4. Riskler + Kritik Uyarılar (danışman)

1. **İki-paralel-yürütme-motoru riski (Plan 42):** WorkflowInstance canonical kalmalı. Plan 42 onaya girmeden `ProcessInstance` vs `IProcessExecutionService` ayrı-motor çelişkisi (plan 42:57 vs :73) netleşmeli. İzin-onay bu kararı BEKLEMEZ.
2. **Üç onay-entity dağınıklığı:** izin-onay `WorkflowInstance`, SOP-onay `ApprovalRequest` dünyasında kalır — kısa vade kabul, birleştirme borcu (VISION:83) büyür. **Şimdi birleştirme YOK.** TODO/journal'a "izin-onay WorkflowEngine pattern kurdu; SOP+ApprovalService sonra migrate" notu.
3. **`ServiceResult<int>.Failure` Data guard:** köprüde `result.IsSuccess` kontrolü ZORUNLU — aksi `submission.WorkflowInstanceId = 0` kirliliği (`WorkflowController.Admin.cs:282` audit `result.Data.ToString()` fail'de 0 döner). Best-effort try/catch + IsSuccess gate.
4. **Modül izolasyonu (ADR-002/015):** Forms `IWorkflowService` (Core abstraction) inject eder — `WorkflowEngine`'i (ana proje) DOĞRUDAN inject ETMEZ. `IApprovalService` icat etme, `IWorkflowService` yeterli.
5. **Şifreli form + workflow:** izin formu şifreli değil ama şablon `EntityType="FormSubmission"` generic olduğu için şifreli forma (ihbar) da bağlanabilir → onaycıya submission içeriği decrypt-gate ile açılmalı (G2 zaten yetki-gated); workflow payload'a düz içerik KOYMA.

---

## 4.5 İki-Motor Çakışması — Council Verdict (2026-07-04, llm-council 5+3)

**Karar: TEK yürütme motoru = WorkflowEngine (WorkflowInstance). Portalda ikinci state machine ASLA kurulmaz.** Council yakınsaması 4/5 (First Principles + Outsider + Executor + Contrarian koşullu); Expansionist (upfront 7-primitif) peer-review'da YAGNI olarak elendi.

1. **ProcessInstance ≠ state machine.** İnşa edilirse (ve ancak gerektiğinde) **ince VAKA KONTEYNERİ**: korelasyon kimliği + **türetilmiş** status (Açık/Kapalı/İptal, kendi WorkflowInstance'ından) + timeline = **EntityRelations** (Plan 38 ✅) sorgusu. Kendi state'i / kendi transition log'u YOK. Kategori ayrımı (First Principles): **Yürütme = durum makinesi** (çözülü, WorkflowEngine) ≠ **Vaka = korelasyon kimliği** (klasör/dosya-no, motor değil). Vakaya davranış eklemek = iki-motor; eklenmezse çakışma kökten yok.

2. **Timer/delay/recursion sahibi ZATEN çözülü** (Peer 3 kararı, Peer 2'nin TimerStep önerisine üstün): WorkflowEngine'in **mevcut delay-step'i** (`waitDays` + `IWorkflowService.TickDelayedStepsAsync`) + Hangfire `WorkflowStepProcessor`. DSAR 30-gün = delay-step; Hangfire tick'ler, WorkflowInstance ilerler. **Yeni TimerStep/RetentionStep soyutlaması GEREKSİZ** (Hangfire üstüne ikinci scheduler = footprint ihlali). KVKK imha recursion = **ayrı korelasyonlu** WorkflowInstance (parent'a EntityRelations `instanceOf`/`produces` ile bağlı), iç içe DEĞİL. Contrarian'ın "sahibi kim" korkusu böylece açıkça yanıtlanır: **Hangfire (zaman) + WorkflowEngine delay-step (model) + EntityRelations (korelasyon)** — ProcessInstance hiç motor olmadığı için sahiplik dağılmaz.

3. **Step tipleri motorda, ARTIMLI** (D'nin tek geçerli parçası, upfront-build reddedilerek): `ApprovalStep` var. `FormStep`/`NotifyStep`/`DocGenStep`/`StartWorkflow`(recursion) → **her biri gerçek senaryo gelince** WorkflowEngine'e eklenir. Kural: mantık **step tipi olarak motorda** yaşar (Hangfire/controller'a dağılmaz) — gizli ikinci motoru bu önler. 7 primitifi baştan kurma (Plan 42 YAGNI, tek müşteri).

4. **Ad-hoc/case modu** (Outsider): belirsiz-akış vakalar (ihbar soruşturması — adımlar runtime çıkar) için konteyner/motor "boş workflow + manuel eklenen görev" moduna izin vermeli. Sahte-sequential template'e sıkıştırma. NOT edildi; ihbar-soruşturma fiilen yapılınca inşa.

5. **Çift-yön + idempotency** (Peer 1 blind-spot): köprü giriş yönü = form submit → StartAsync (bu plan). Çıkış yönü = motor `FormStep` ile sonraki aktöre form spawn'lar (gelecek). Köprüde **idempotency guard**: `FormSubmission.WorkflowInstanceId` zaten set ise tekrar StartAsync YAPMA (çift-submit → çift-instance önlenir; korelasyon = submission id).

**Plan 42 çözümü (42:56 vs 42:73 çelişkisi):** `IProcessExecutionService`'in **kendi state machine'i (başlat/ilerlet/kapat + ProcessInstanceTransition) İPTAL**. ProcessInstance "ilerlet" = `IWorkflowService.AdvanceAsync`'e **delege**. Transition log = WorkflowInstance event stream'i (tekrar yazma yok). ProcessInstance'ın aspect/timeline/PDF/ICS'i = tek motor üstünde **okuma/çıktı** katmanı. Yani **wrap, duplicate etme**. Plan 42 onaya girmeden bu revizyonla güncellenmeli.

---

## 4.6 Hazır-Örnek Araştırması (2026-07-05, 7 lokal repo + 3 web ajanı)

**Soru:** Tasarladığımız sistemin (form→imza→onay→inbox, tek motor + ince vaka konteyneri) hazır örneği var mı?

**Lokal (D:/Dev):** Turnkey örnek YOK. Tek kısmi: **yonet/YonetIQ** `ApprovalService` — MIN(StepOrder) bekleyen-adım inbox sorgusu + çok-adım advance/reject (`Data/Services/ApprovalService.cs:25-45,89-133`) referans algoritma; ama iki-paralel-ada + string status + hardcoded approver = **karşı örnek** (bizim tasarım daha ileri). CrewOps (AI-pipeline), FLYX (designer stub, motor yok), Operax (doküman status-flip), MIMBAL (sadece doküman), esign/talep/toplanti (boş) — elendi.

**OSS .NET motorlar — verdict: CUSTOM WorkflowEngine'i KORU.** Hiçbir MIT OSS form→onay→**inbox** dikeyini hazır vermiyor (inbox her durumda bizde kalıyor). Tek tam paket **OptimaJet** = ticari. **MassTransit v9 ticari oldu** (v8 EOL 2026-sonu) — elendi. **Elsa 3** bookmark/suspend-resume = bizim delay-step'in endüstri-standart eşleniği → tasarım **doğrulandı**; ileride paralel dallanma/BPMN gerekirse en düşük maliyetli geçiş adayı Elsa (MIT). Stateless = entity-status guard olarak kalır (motor değil).

**Case-management OSS — karar OMG-standart çıktı:** **Flowable CMMN** `CaseInstance` = konteyner/execution-context, motor DEĞİL (plan-item'lar kendi lifecycle'ında) = bizim "ProcessInstance=ince konteyner" kararının birebir endüstri karşılığı. Camunda `businessKey` ayrımı (iş varlığı ≠ yürütme instance'ı) aynı yönde. **Frappe HR** = en ince DB-driven örnek: `Workflow State`+`Workflow Transition` tabloları `(state, action, next_state, allowed_role, condition)` + `Workflow Action` inbox — kod okunabilir (`frappe/model/workflow.py`, `workflow_action.py`).

**Rakip ürünler:** ServiceNow (record producer→HR Case) + Kissflow/Pipefy (case=konteyner) bizim çizgide; Power Automate konteynersiz = denetim-izi anti-örneği; sektör onay ile imzayı AYIRIYOR (onay=hafif kayıt; imza=form-içi signature field ← bizde survey-core signaturepad VAR; hukuki imza=ayrı e-sign adımı, motor içine gömülmez).

**Çalınabilir 5 fikir (backlog — bu planın fazlarına değil, sonrasına):**
1. Frappe transitions-tablosu + onaycı çözümleme hiyerarşisi (kişi-override > birim-default) — **OrgPositions omurga + GorevPersonelMap ile birebir oturur**.
2. Action-Center: e-postadan tek-tık onay/red linki (Inbox M2'ye onay-aksiyon kartı).
3. Pipefy phase-specific fields: vaka ilerledikçe adım kendi ek alanlarını açar (İK'ya düşünce İK-alanları).
4. Jotform quorum/outcome: onay düğümünde N-of-M + adlandırılmış sonuç ("Revizyon iste") — lookup-driven.
5. ServiceNow çift görünüm: requester/approver/İK için aynı vakanın ayrı form view'ları (ihbar/DSAR alan-düzeyi görünürlük).

---

## 5. Fazlar

### Part A — Forms M-A Kapanış (önce, küçük)
- **A1 · G5 template seed** — İhbar formu (id=6, canlı draft/publish) → idempotent seed migration (`Database/NN_FormTemplateSeed.sql`, VALUES + WHERE NOT EXISTS). **KRİTİK:** Status=0 taslak seed (admin Publish'ler; FormSchemaBuilder JSON'unu SQL'de tekrarlama). Danışman: `mosaik-forms-expert` skill G5 notu.
- **A2 · İzlenen borç triage** (todo-verification disiplini — her biri file:line doğrula, gerçekten açık mı):
  - Şifreli-form **dosya ekleri şifrelenmiyor** (App_Data düz metin) — ihbar için değerlendir (HIGH aday; ayrı karar).
  - Submission liste pagination/filter-bar yok (>5000 form).
  - Export UTC vs ekran local tarih tutarsızlığı.
  - survey-core file "Select" caption locale key (minör).
  - Karar: hangisi bu planda, hangisi backlog. Şifreli-dosya HIGH ise Part A'da kapat.

### Part B — Forms→Workflow Köprüsü (walking skeleton)
- **B1 · Köprü kod** — `FormSubmissionService` → `IWorkflowService` inject. `SubmitAsync` SaveChanges SONRASI (G4 bildirimin yanında), `def.TriggersWorkflowId != null` ise:
  `StartAsync(WorkflowStartInput(FirmaId, TemplateId=def.TriggersWorkflowId.Value, EntityType:"FormSubmission", EntityId:submission.Id, StartedBy))` → `result.IsSuccess` ise `submission.WorkflowInstanceId = result.Data` + ikinci SaveChanges. **Best-effort try/catch** (submit'i kırma) + IsSuccess gate (Risk 3) + **idempotency**: `WorkflowInstanceId` zaten set ise StartAsync YAPMA (çift-instance guard, §4.5-5).
- **B2 · İzin-onay şablonu seed** — tek-adımlı `WorkflowTemplate` (`EntityType="FormSubmission"`, amir onayı + son adım İK bildirim). `DefinitionJson` şemasını mevcut seeded template / `WorkflowStepProcessor` step parser'dan türet (Contrarian lens — erken doğrula).
- **B3 · Admin: forma şablon bağlama** — FormDefinition edit ekranına `TriggersWorkflowId` seçimi (aktif `EntityType="FormSubmission"` şablonlar dropdown). Yoksa köprü tetiklenmez.
- **B4 · Onaycı inbox linki** — `WorkflowInboxProvider` zaten "Süreçlerim"e düşürüyor (EntityUrl boşsa `/Workflow/Instance/{id}` fallback). İzin verisini görmek için `EntityUrl` → FormSubmission detay map'i: `IEntityWorkflowProvider` FormSubmission implementasyonu (Faz B4, küçük).
- **B5 · Kapanış senkron** — advance/karar sonrası submission durum yansıması (opsiyonel Status güncelleme); TODO/journal migrate notu (Risk 2).

### Sıra
Part A (G5 + borç triage) → Part B1→B2→B3→B4→B5. Her faz: **danış→kod→4-scan→preview→commit** (faz-geliştirme disiplini).

---

## 6. Done Criteria
- [ ] G5: İhbar template idempotent seed (Status=0), migration çalıştı, admin Publish'leyebiliyor.
- [ ] A2: her borç file:line doğrulandı, karar verildi (kapandı veya backlog'a yazıldı); şifreli-dosya kararı net.
- [ ] B1: `TriggersWorkflowId` set formda submit → WorkflowInstance başlıyor, `WorkflowInstanceId` yazılıyor; set değilse eski davranış (best-effort, submit kırılmıyor).
- [ ] B2: izin-onay şablonu seed, admin görüyor.
- [ ] B3: admin formu şablona bağlayabiliyor.
- [ ] B4: onaycı "Süreçlerim"de izin talebini görüyor, karar verebiliyor; link form verisine gidiyor.
- [ ] Preview E2E: izin formu doldur → submit → amir inbox → onayla → kapanış (tam akış).
- [ ] Test: yeşil (köprü + evaluator). Scan: 0 CRIT/HIGH. Skill senkron (`mosaik-forms-expert` + workflow notu).
- [ ] Risk 2 borç notu journal/TODO'da.

## 7. Rollback
- Köprü best-effort + `TriggersWorkflowId` null ise no-op → eski submit davranışı korunur (feature-flag benzeri: şablon bağlanmadıkça pasif).
- Migration seed idempotent (WHERE NOT EXISTS) → tekrar çalıştırma güvenli; geri alma = seed satırı sil.
- Git revert faz-bazlı (her faz ayrı commit, plan:57 referanslı).

## 8. Generic Vizyon (bu plan sonrası, ayrı planlar)
Köprü `EntityType` generic olduğu için sonraki tüketiciler **kod değil template**:
masraf onayı · doküman onayı (Documents) · sözleşme imza (Contracts) · satınalma · SOP migrate (ApprovalRequest→WorkflowInstance).
**Ayrı omurga (bu plan değil):** zamanlama/otomasyon (Hangfire) — zamanlanmış rapor (Plan 32), hatırlatma, tarama. Kompozisyon: scheduled job → workflow başlat.

## 9. İlişkili
- Danışman analizi: bu oturum (mosaik-portal-danismani, 3-motor tespiti + karar tablosu).
- `plans/42-process-execution-runtime.md` (ERTELE — wrap eder, dublicate etmez).
- `plans/56-modul-tamamlama-programi.md` (Forms M-A parent) · `.claude/skills/mosaik-forms-expert/SKILL.md`.
- `Mosaik.Core/Workflow/IWorkflowService.cs` (köprü abstraction) · `Mosaik/Services/Workflow/WorkflowEngine.cs` (canonical motor) · `Mosaik/Services/Inbox/WorkflowInboxProvider.cs`.
- VISION.md:83 (IApprovalService birleştirme borcu — ADR-024 adayı, ertelendi).
