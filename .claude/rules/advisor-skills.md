# Danışman (Advisor) Disiplini — Kod-Öncesi İstişare

_Rule katmanı: on-demand (ilgili iş tetiklenince birincil). `paths:` YOK. Operax advisor-skills pattern uyarlaması, 2026-06-29._

## Temel Kural

Belirli iş türlerine dokunmadan **ÖNCE** ilgili **danışmana** danışılır. Danışmanlar **SALT-REHBER**: kendileri kod yazmaz; doğrulanacak noktaları + kaynakları + karar tablosunu verir. Sonra kodu sen (ana ajan) yazarsın.

"Doğru görünüyor" yetmez. Güvenlik / KVKK / süreç-modelleme / mimari işi danışmadan üretilirse **eksik kabul edilir**.

## İş Türü → Danışman Router

| İş türü (kod öncesi) | Danışman | Tip |
|---|---|---|
| Yeni controller / POST / SQL exec / SP wrapper / file upload / email / JS fetch | `mosaik-security` skill | skill (mevcut) |
| KVKK / DataElement mapping / VERBİS / saklama süresi / aydınlatma | `kvkk-veri-envanteri` skill | skill (mevcut) |
| Yeni view / layout / hero-card-table / token-theming / WCAG | `ui-ux-pro-max` + `accessibility-compliance` skill | skill (mevcut) |
| Yeni C# / Razor / ViewModel / service / migration | `mosaik-csharp-razor` + `mosaik-css-expert`/`mosaik-js-expert` skill | skill (mevcut) |
| **Portal/süreç modelleme kararı** (status enum→lookup, EntityRelations, modül izolasyon, Process/aspect, approval/inbox, lifecycle) **VEYA kapsam/sınır kararı** (portala mı / ayrı tool mu / entegrasyon mu / hiç yapılmamalı mı — CORE/ADJACENT/OUT, build/buy/cut, scope-creep) | **`mosaik-portal-danismani` agent** | agent (read-only) |
| Mimari "hangi yol" belirsizliği (gerçek tradeoff, scope, önceliklendirme) | `llm-council` skill | skill (mevcut) |
| BKM kurumsal DB şema keşfi | `bkm-db-explorer` skill | skill (mevcut) |

## Skill vs Agent ayrımı (Operax kuralı)

- **Domain bilgi / checkpoint lookup → skill** (salt-rehber, tetik-bazlı).
- **Cross-cutting modelleme/mimari karar → agent** (read-only, Edit/Write yok, çıktı: karar tablosu + confidence + kanıt → ana ajana döner).
- **Yüksek-risk "hangi yol" → `llm-council`** (5 danışman + chairman; günlük iş için değil, gerçek tradeoff için).

## Çıktı sözleşmesi (modelleme danışmanı)

Karar tablosu: `| Domain/Kod | Canonical küme | VT/kod kanıtı (file:line) | Karar | Gerekçe |` + her kararda **confidence (0-100)** + dayanak. Final mesaj ana ajana döner, kullanıcıya değil.

## Guardrails

- ❌ Danışman kod yazmaz (modelleme agent'ında Edit/Write yok).
- ❌ Her ihtiyaca yeni danışman skill'i yaratma — router'daki mevcutları kullan (`footprint-ladder.md`).
- ❌ Council'i günlük işe çağırma — sadece gerçek belirsizlik.
- ✅ Kod-öncesi danış, confidence+kanıt iste, sonra yaz.

## İlişkili

- `.claude/agents/mosaik-portal-danismani.md` — portal/süreç modelleme read-only danışman.
- `.claude/skills/llm-council/SKILL.md` — yüksek-risk panel.
- `.claude/rules/footprint-ladder.md` — yeni danışman yaratma freni.
- `.claude/rules/security-principles.md` + `.claude/skills/mosaik-security/SKILL.md` — güvenlik danışmanı.
