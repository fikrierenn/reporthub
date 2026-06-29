# Plan 54 — vNext Yol Haritası v2 (Modül-Modül Seri, Denetim-Kapılı)

**Durum:** ✅ ONAYLANDI 2026-06-29 — modül-modül seri + denetim kapısı kabul; CUT 49/50/51 onaylandı; Plan 53+16 superseded. M1 (SMTP) başlangıç onaylı.
**Tier:** 3 (master roadmap — Plan 53 + Plan 16'yı SUPERSEDE eder)
**Tetik:** Kullanıcı 2026-06-29 — "tüm planları elden geçir, sıralamayı tekrar yap, ciddi yol haritası" + "modül modül yap, biri bitip tamamlanmadan diğerine geçmek yok, her kapanışta o oturumda dokunulan tüm modüller denetlenir"
**Girdi:** Bu oturumun 4 araştırması — [RESEARCH_VISION](../docs/RESEARCH_VISION_2026-06-29.md), [COMPETITIVE_SCOPE](../docs/COMPETITIVE_SCOPE_2026-06-29.md), `mosaik-portal-danismani` kapsam review.

---

## 1. İki temel ilke (DEĞİŞMEZ)

1. **SERİ — modül modül.** Aynı anda tek modül açık. Modül N **tam kapanmadan** (kod + test + denetim + commit) modül N+1 **başlamaz**. Paralel hat YOK. "Yarım bırakıp diğerine geç" YASAK.
2. **DENETİM KAPISI — her modül kapanışında.** Modül "bitti" sayılmaz; **denetimden geçer.** Denetim kapsamı = o oturumda dokunulan **TÜM modüller** (sadece yeni olan değil — komşu regresyon yakalanır).

## 2. Neden yeni roadmap

Plan 53 (master): (a) scope-creep içeriyor (49 biyometrik / 50 surveillance / 51 differential-privacy red-flag), (b) rakip table-stakes eksik (inbox/search/alert/e-imza/İK/KVKK-tamamlama). Plan 54 bunu düzeltir + **paralel 12-hafta yerine seri modül-kapı** modeline çevirir.

## 3. MODÜL SIRASI (seri — dependency + değer)

Her satır = bir oturum-üstü "modül". Sıra dependency'e saygılıdır; üstten alta tek tek kapatılır.

| # | Modül | Bağımlılık | Tip | Kısa kapsam |
|---|---|---|---|---|
| **M1** | **SMTP foundation** (Plan 32) | — | prereq | `IEmailService` + scheduled caller; notification/digest/comment altyapısı |
| **M2** | **Unified Inbox** (Plan 37, ADR-018) | M1 | CORE | `IInboxProvider` opt-in + `/Inbox` çok-modül aggregation (workflow+form+obligation+circular-read) |
| **M3** | **Cross-module Search** (ADR-018) | — | CORE | SQL Server full-text; rapor+SOP+sözleşme+tamim tek arama. **vector kurma** |
| **M4** | **Dashboard→Alert** (EscalationRule) | M1 | CORE | eşik aşımı → `INotificationService`; Hangfire sweeper. OI≠BI |
| **M5** | **Comment/Mention** (Plan 35) | M1 | CORE-cap | polymorphic + entity_type lookup-FK + denormalize firma_id + fan-out-on-write |
| **M6** | **KVKK backbone** (Plan 40, düzelt) | M2 | vNext | Process+DataElement + ProcessingPurpose/Recipient/VerbisRegistration lookup + Pattern 9 |
| **M7** | **Form Builder** (Plan 41, düzelt) | M6 | vNext KRİTİK | hybrid + **FormVersion snapshot** + SurveyJS reuse + DataElement map |
| **M8** | **Process Exec Runtime** (Plan 42, düzelt) | M6,M7 | vNext birleştirici | **tek `ProcessActivity` stream** (6 aspect değil) + Stateless guard + SLA sweeper |
| **M9** | **Workflow Designer** (Plan 36) | M8 | TR table-stakes | no-code designer + vekalet/eskalasyon/grade |
| **M10** | **KVKK tamamlama** | M6 | TR table-stakes | DSAR akışı (30g, clock-pause) + VERBİS export + açık rıza + 72h ihlal + otomatik imha |
| **M11** | **e-İmza (5070)** | — | TR table-stakes | sözleşme+tamim; $0 signature_pad + audit-hash (DocuSign değil) |
| **M12** | **İK self-servis** | — | TR table-stakes | izin/masraf/talep + bordro-görüntüleme (hesap DEĞİL — ERP entegre) |
| **M13** | **Audit/ops analytics** | — | table-stakes | SLA breach/bottleneck/engagement dashboard (SP-motoru reuse) |
| **M14** | **Documents Faz C** (Plan 27) | — | DYS | C-02 check-in/out + C-04 FTS + C-05 metadata + C-07 audit (DocVersions+74 ✅) |
| **M15** | **AI differentiator** (Plan 44✅+45✅+chat) | M6-M8 | wedge | RAG guard + Excel-to-process + chat-over-data citation |

**Park (ertele/doğrula):** PWA (46 — saha ihtiyacı doğrula), Auto-tuning (47 — M8 verisi sonrası), Executable-SOP (48 — spekülatif), Trend-Endeksi (52 — nice).
**CUT (onay ister, §6):** Zero-UI biyometrik (49), Shadow-Org (50), Differential-Privacy (51).

> Sıra önerisidir; her modül kapanışında bir sonraki yeniden değerlendirilebilir (değer/aciliyet değişirse). Ama **aynı anda bir modül** kuralı sabit.

## 4. DENETİM KAPISI — her modül kapanış protokolü (ZORUNLU)

Modül "bitti" demek için **hepsi** geçmeli. Biri kırmızıysa modül kapanmaz, sonraki başlamaz.

1. **Build yeşil** — `dotnet build` 0 hata.
2. **Test yeşil** — `dotnet test` tam geçer (test-discipline: build≠test). Yeni davranış → yeni test.
3. **4 paralel compliance scan** (session-protocol Adım 5) — **kapsam: o oturumda dokunulan TÜM modüller** (`git diff` + bu modül + komşu):
   - `code-reviewer` (CLAUDE.md + mimari uyum)
   - `silent-failure-hunter` (sessiz catch / fallback)
   - `security-reviewer` veya `/security-check` (güvenlik 10 kural)
   - inline-style + ui-patterns envanter (UI dokunulduysa)
4. **`mosaik-portal-danismani` modelleme + kapsam denetimi** — bu modül + dokunulan komşular: status enum→lookup, modül izolasyonu (ADR-002/015), EntityRelations/typed-junction, kapsam-sınır (scope-creep girmiş mi). Karar tablosu + confidence + file:line.
5. **Smoke test** — kritik path (preview/manuel) çalışıyor.
6. **ARCHITECTURE_MAP refresh** — `scripts/refresh-arch-map.sh`.
7. **Commit** — modül kapanış commit'i (plan referansı: `feat(x): M# … (plan: 54)`).
8. **Plan/TODO senkron** — bu modül ✅ + commit hash; sonraki modül `in_progress`.

**Bulgu çıkarsa:** düzelt → denetimi tekrar → temizse kapat. CRITICAL/HIGH varken sonraki modüle geçiş YASAK.

## 5. CUT kararları — gerekçe (onay ister)

49/50/51 VISION'da ✅ ama onay kapsam-disiplininden önce:
- **49 Zero-UI biyometrik** — biyometrik KVKK m.6 özel nitelikli; KVKK modülü yapan portal biyometrik toplama riski üretemez. CUT veya ses-only dar pilot.
- **50 Shadow Org Graph** — workplace-surveillance; güven + KVKK aydınlatma yükü. CUT veya anonim-agregat.
- **51 Differential Privacy** — tek-tenant 200-300 kişi, UserDataFilter zaten izolasyon; epsilon-budget akademik. CUT.
- Kazanım ~180-280h → table-stakes'e (M10-M13).

## 6. Bu roadmap planının kendi done criteria
- [ ] Modül sırası + denetim kapısı protokolü onaylandı
- [ ] CUT 49/50/51 onay/ret
- [ ] Plan 53 + 16 → arşiv (superseded notu)
- [ ] Düzeltmeler (40 lookup / 41 FormVersion / 42 tek-stream) ilgili plan dosyalarına fold
- [ ] M1 için plan dosyası + TODO faz + `in_progress`

## 7. Onay
- [ ] Modül-modül seri + denetim kapısı onaylandı: ___
- [ ] CUT 49/50/51: ___
- [ ] M1 (SMTP) başlangıç: ___

## İlişkili
- [COMPETITIVE_SCOPE](../docs/COMPETITIVE_SCOPE_2026-06-29.md) · [RESEARCH_VISION](../docs/RESEARCH_VISION_2026-06-29.md) · `docs/VISION.md` §7
- `.claude/rules/session-protocol.md` Adım 5 (compliance scan = denetim kapısı kaynağı) · `.claude/rules/test-discipline.md`
- `.claude/agents/mosaik-portal-danismani.md` (modelleme + kapsam denetimi) · ADR-018 (inbox/search opt-in)
- Plan 53 (superseded), Plan 16 (superseded)
