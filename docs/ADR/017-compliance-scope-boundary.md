# ADR-017 — Compliance Scope Sınırı

**Tarih:** 2026-05-21
**Durum:** Kabul edildi
**Yazan:** Claude (Plan 33 §5 kararından port)

---

## Bağlam

Mosaik'te `ComplianceTemplate` tablosu + `Compliance` modülü var (Plan 25 / Migration 60). Kapsam belirsizliği:

- KVKK uyum checklist, ISO 27001 audit, müşteri uyum gereklilikleri vs. **ayrı modül** mü, yoksa mevcut `ContractObligation` ile **ortak şablon deposu** mu?
- "Compliance" kelimesi geniş: hukuki uyum, vergi uyumu, regülasyon uyumu, müşteri SLA, iç politika çakışması.
- Boundary çizilmezse Compliance modülü Documents + Tamim + Workflow + Approval ile çakışır, ürün scope patlar.

Plan 34 SOP, Plan 35 Comment, Plan 36 Workflow Designer'ın **hepsi** "uyum" kullanımına alet edilebilir. Birinin diğerine bağımlılığı engellemek için Compliance neye **DEĞİL** sınırı netleşir.

---

## Karar

**`ComplianceTemplate` = "Obligation şablon deposu + import wizard". Ayrı KVKK/ISO modülü yok.**

### Kapsam dahili (Compliance modülünün YAPTIĞI)

1. **Şirket geneli periyodik yükümlülük şablonları** — KDV beyanı, SGK bildirimi, BES kesinti, SBS ödemesi vb. tekrarlanan yükümlülüklerin **şablonu**.
2. **Import wizard** — kullanıcı bir şablon paketini (CSV / JSON) seçer → `ContractObligations` tablosuna toplu import; her satır periyodik üretici (Plan 25 Faz C generator) ile çoğalır.
3. **Türk vergi takvimi seed'i** — Migration 60'da paketlenmiş Şubat/Mayıs/Ağustos/Kasım Q-due şablonları.

### Kapsam dışı (Compliance modülünün YAPMADIĞI)

| Talep | Doğru yer |
|---|---|
| Sözleşmeye bağlı KVKK aydınlatma metni | `ContractObligation.Category = Legal`, Plan 25 Sözleşme modülü |
| ISO 27001 audit log + politika dokümanları | `Documents` modülü (Plan 27) + Tamim politika yayını |
| KVKK veri envanteri / VERBİS form üretici | **YOK** — ayrı plan açılmadı, YAGNI |
| Müşteri SLA takibi | `ContractObligation.Category = SLA`, Sözleşme modülü |
| Uyum onay akışı (KVKK formu onaylanmalı) | Plan 36 Workflow Designer template'i |
| Uyum eğitimi tamamlandı bildirimi | Plan 34 SOP modülü "okudum" pattern'i |
| Compliance KPI dashboard'u | Mevcut Reports + Dashboard modülü |

### Sözleşme bazlı vs şirket geneli ayrımı (karar matrisi)

- **Sözleşmeye bağlı** uyum gereği → `ContractObligation` (FK = Contract). Yaşam döngüsü sözleşmeyle bağlı.
- **Şirket geneli** periyodik uyum (vergi/SGK/regülasyon) → `ComplianceTemplate` import → `ContractObligation` (FK = NULL veya `IsCompanyWide = true`). Yaşam döngüsü bağımsız.

İki path tek tablo (`ContractObligations`) üzerinde yaşar — Compliance bağımsız tablo açmaz.

---

## Alternatifler

### A: Ayrı Compliance Modülü (KVKK checklist + ISO 27001 doküman kütüphanesi)
**Reddetme:** Overkill. Mosaik 200 kişilik şirket için yazılıyor — KVKK aydınlatma metni dokümanlardadır, ISO 27001 audit ayrı uzman yazılım işidir. Mosaik içinde **şablon + import** yeterli.

### B: `ComplianceTemplate` + import wizard (SEÇİLEN)
Şablon import → ContractObligation periyodik üretim. KVKK/ISO talebi gelirse:
- Sözleşmeye bağlıysa Sözleşme modülüne
- Doküman ihtiyacıysa Documents modülüne
- Audit gereği olursa Workflow Designer ile akış kurulur

Her ihtiyaç **mevcut modülün** üzerine yığılır, yeni modül açılmaz.

### C: Tüm uyum sözleşme altında ("KVKK aslında sözleşmedir")
**Reddetme:** Vergi beyanı / SGK ödemesi / regülasyon takibi sözleşmeden bağımsız periyodik iş. Sözleşme yokken bunlar üretilmez → "şirket geneli yükümlülük" kaybedilir.

---

## Sonuçlar

- `ComplianceController` + `Views/Compliance/*` yalnızca **şablon listesi + import wizard** sunar.
- `ContractObligations` tablosu **iki tip** taşır: (a) sözleşmeli (`ContractId NOT NULL`), (b) şirket geneli (`ContractId NULL` veya marker kolon).
- Migration 60 seed (`ComplianceTemplates` Türk vergi takvimi) bu kararın canlı örneği — değişmez.
- Plan 34 SOP "uyum eğitimi" istemiyle gelirse → SOP'un `IsMandatory` + okuma takibi pattern'i kullanılır, Compliance dokunulmaz.
- Plan 35 Comment "uyum yorumlama" → modül-agnostik polymorphic Comments, Compliance özel kolonu yok.
- Plan 36 Workflow Designer "uyum onayı" → template `EntityType=ContractObligation`, Compliance template'i ayrı engine değil.
- **Yeni "Compliance" özelliği talep gelirse:** önce bu ADR okunur, talep yukarıdaki tablodaki "doğru yer"e yönlendirilir. Compliance modülüne **yeni controller / yeni entity** eklenmez (şablon/import dışı).

---

## İlişkili

- Plan 25 — Sözleşme + Yükümlülük (`ContractObligation` host tablosu)
- Plan 33 — Modül Tamamlama Roadmap §5 (bu kararın kaynağı)
- Plan 34 — SOP / Prosedür (uyum eğitimi talebi buraya yönlenir)
- Plan 36 — Workflow Designer (uyum onay akışı buraya yönlenir)
- ADR-015 — Yeni modüller ayrı assembly (Compliance scope büyürse bu kural uyarınca ayrılır, **ama mevcut karar ayırma değil**)
- Migration 60 — Türk vergi takvimi seed
