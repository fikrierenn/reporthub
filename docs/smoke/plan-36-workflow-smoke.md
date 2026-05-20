# Plan 36 — Workflow Designer Smoke Test (W-17)

**Tarih:** 2026-05-21
**Kapsam:** End-to-end manuel smoke. Integration test infrastructure yok (2026-05-14 kararı), unit testler 376/376 yeşil. Bu döküman canlı dev ortamında manuel doğrulama içindir.

> **Ön koşul:** Migration 64 + 65 uygulandı. Admin kullanıcı + en az 2 normal kullanıcı (ör. UserId=10 ve UserId=20) seed'li.

---

## 1. Şablon Oluştur (W-05 + W-06 + W-07)

1. Admin olarak giriş yap.
2. Sidebar → Sistem → **Workflow Şablonları**.
3. **Yeni Şablon** → form aç.
4. Doldur:
   - **Ad:** "Sözleşme Onay Zinciri"
   - **Modül:** Sözleşme (Contract)
   - **Aktif:** ✓
5. Designer canvas → toolbox'tan **Onay Adımı** sürükle, 2 kez bırak (s1, s2 oluşur).
6. İlk adımı seç → sağ panel:
   - **Adım Adı:** "Yönetici Onayı"
   - **Atanan Kullanıcı:** 10 (test user)
   - **Süre (gün):** 2
   - **Eskalasyon Hedefi:** admin
7. İkinci adımı seç → benzer şekilde "Mali İşler" + UserId 20 + 3 gün.
8. **Kaydet**.
9. **Beklenti:**
   - Listede yeni şablon görünür, **Aktif** badge yeşil.
   - DB: `SELECT TOP 1 * FROM WorkflowTemplates ORDER BY Id DESC` → DefinitionJson içinde `sequence` 2 step ile dolu.

---

## 2. Akış Tetikle (W-14)

1. Sözleşme modülüne git → herhangi bir aktif sözleşme **Detay** sayfası.
2. **Top Actions** kısmında "Onay Akışı Başlat" butonu görünür mü? ✓
3. Tıkla → Trigger sayfası açılır, dropdown'da Adım 1'deki şablon listede.
4. Şablonu seç → **Akışı Başlat**.
5. **Beklenti:**
   - Instance detay sayfasına redirect (`/Workflow/Instance/{id}`).
   - Timeline'da: InstanceStarted + StepEntered (s1) iki kayıt.
   - Status: **Aktif**.
   - DB log: `SELECT * FROM WorkflowInstanceLogs WHERE InstanceId=...` → 2 satır.
   - DB: `SELECT * FROM AuditLog WHERE EventType='workflow_instance_start'` → 1 satır.

---

## 3. Bildirim + Inbox (W-10)

1. UserId=10 olarak (s1 atanmış) giriş yap.
2. Sidebar → Ana → **Bekleyen Onaylar**.
3. **Beklenti:**
   - Inbox'ta yeni satır görünür: "Sözleşme Onay Zinciri — Yönetici Onayı".
   - Dashboard'ta (Index) **Bekleyen Onaylarım** kartı görünür (W-16).
   - Notifications tablosunda satır var: `entityType='workflow_instance'`, `notificationType='workflow_step'`.

---

## 4. ICS Export (W-12)

1. UserId=10 olarak Instance sayfasında.
2. **Takvime Ekle (.ics)** linkine tıkla.
3. **Beklenti:**
   - `workflow-{id}.ics` dosyası iner, Content-Type `text/calendar`.
   - İçerik: VCALENDAR + VEVENT, DTSTART = (şimdi + 2 gün), SUMMARY = "Sözleşme Onay Zinciri — Yönetici Onayı".

---

## 5. Approve (W-13 + E-09 dual-write)

1. UserId=10 olarak Instance sayfasında, Decision panel'inde **Onayla** + yorum gir.
2. Submit.
3. **Beklenti:**
   - Redirect Instance sayfasına, "Adım ilerletildi: Mali İşler" mesajı.
   - Timeline: StepCompleted (s1) + StepEntered (s2) iki yeni satır.
   - CurrentStepId artık s2.
   - **DecisionLog** tablosu: 1 yeni satır, Title="Workflow [Yönetici Onayı] onaylandı", MadeBy=10, RelatedEntityType=WorkflowInstance, RelatedEntityId={id}.
   - **EntityRelations** tablosu: 1 yeni satır, SourceType=User, SourceId=10, RelationType=approved, TargetType=WorkflowInstance.
   - UserId=20'ye yeni bildirim (s2 atanmış).

---

## 6. Reject Path (alternatif senaryo)

1. UserId=20 olarak Instance sayfasında **Reddet** + yorum.
2. **Beklenti:**
   - Status = **İptal**.
   - Timeline: StepRejected + InstanceCancelled.
   - DecisionLog yeni satır, Title="… reddedildi".
   - EntityRelations yeni satır, RelationType=rejected.

---

## 7. Escalation (W-09 + W-11)

> Gerçek zamanlı test zor — saat ileri al veya DB'de WorkflowInstanceLogs.OccurredAt'i geriye it.

1. Manuel test (SQL):
   ```sql
   UPDATE WorkflowInstanceLogs
   SET OccurredAt = DATEADD(DAY, -10, OccurredAt)
   WHERE InstanceId = {id} AND EventType = 'StepEntered';
   ```
2. Hangfire dashboard → `/hangfire` → workflow-step-processor → **Trigger now**.
3. **Beklenti:**
   - Yeni log: EscalationFired (s2).
   - Admin rolündeki kullanıcılara bildirim (escalateTo step.properties).
   - Tekrar çalıştır → log sayısı artmaz (idempotent).

---

## 8. Negatif Senaryolar

- **Şablon yok:** Trigger sayfası → "Bu modül için aktif şablon yok" empty state.
- **Atanmamış user Respond endpoint'i çağırırsa:** 403 Forbid.
- **Pasif şablon Trigger:** "Seçilen şablon bu modüle uygun değil" mesajı.
- **DefinitionJson bozuk:** Engine `BROKEN_DEFINITION` döner.

---

## 9. Done Criteria

- [ ] §1-§5 birincil happy path
- [ ] §6 reject path
- [ ] §7 escalation (manuel SQL ile)
- [ ] §8 negatif senaryolar
- [ ] DB temizlik: oluşan instance/template/log satırları silinir veya isolated test firma kullanılır

---

## 10. Bilinen Sınırlamalar

- **Integration test infra yok** — bu smoke manuel yapılır. Eklenirse: WebApplicationFactory + ApplicationDbContext override (2026-05-14 deneyimi sonrası).
- **Multi-firma sorunlu olabilir** — Trigger sırasında FirmaId template'den çekiliyor, kullanıcı farklı firmadaysa instance yanlış firmaya yazılır. Plan 14 Faz B sonrası kullanıcı context picker düzeltir.
- **W-18/W-19 Faz D** — SOP + Tamim onay zinciri henüz bağlı değil. Plan 34/17 sonrası eklenecek.
