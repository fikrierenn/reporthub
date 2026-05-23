# Plan 46 — PWA Offline-First + Native Camera/Barcode

**Durum:** ✅ **ONAYLANDI 2026-05-25** — revize v2 (3 tarayıcı kısıtlama + 5 açık soru kapatma)
**Tier:** 3 (cross-cutting + JS arch + new tech)
**Tetik:** Kullanıcı strategic input 2026-05-25 — "depo/mağaza saha personeli portalı kullanamıyor"
**Effort:** 50-70h (5 faz, 4-5 hafta)
**Aciliyet:** 🟢 Plan 41 Form Builder Faz 0-3 sonrası

---

## 1. Problem

BKM Kitap perakende + lojistik (depo) operasyonu. Saha personeli profilleri:
- **Depo (forklift operatörü, sayım personeli)** — mobil/tablet, sinyal zayıf koridorlar
- **Mağaza (reyon sorumlusu, kasiyer)** — mobil, çoğunlukla wifi
- **Şoför / sevkiyat** — tablet, yolda 3G/4G değişken

**Mevcut sorun:**
- Mosaik pure server-rendered Razor MVC — her sayfa server roundtrip
- İnternet kopunca uygulama açılmaz, form kaydedilemez
- Mobil deneyim "responsive" düzeyinde, gerçek mobile app değil
- Barkod/QR tarama yok — depocu ekipman ID'sini el ile yazıyor (hata yüksek)
- Fotoğraf upload sıralı (browser file picker) — kamera açıp anında çekme yok

**Senaryo (gerçek BKM):**

> Sayım personeli depo bodrumunda 3 saatlik sayım yapıyor. Sinyal 2 koridorda yok. Şu an Excel kullanıyor; sayım sonu wifi'ye gelip Excel'i kaydediyor. Mosaik'te yapsa internet koptuğu an form içerik kaybolur.

**Beklenti:**
- Offline okuma: SOP/prosedür offline cache'lenmiş, sinyalsiz erişim
- Offline form: form doldurulur, IndexedDB queue'ya gider, internet gelince sync
- Kamera + barkod: form içinde anında tarama → field autofill
- Push notification: mağazaya gelen acil tamim sahaya bildirim olarak gelir

---

## 2. Scope

### Kapsam içi (Faz 1-5)
- PWA manifest.json + service worker (Workbox 7)
- Offline cache strategy:
  - SOP read-only (StaleWhileRevalidate)
  - Form definitions (CacheFirst, manifest-driven update)
  - Form submission queue (Background Sync + IndexedDB)
- Push notification (Web Push API + VAPID)
- HTML5 camera widget (getUserMedia + canvas snapshot)
- Barcode/QR scan: ZXing-js (Apache-2.0) veya Quagga.js (MIT)
- SurveyJS custom widget: "Barcode" field type
- Mobile UX: bottom nav, swipe gestures, touch-target ≥48px
- Conflict resolution: offline submit + server-side last-write-wins veya merge

### Kapsam dışı
- Native app (iOS/Android Swift/Kotlin) — PWA yeterli BKM için
- Bluetooth scanner integration (handheld barcode gun) — v2
- AR overlay (camera + ürün tanıma) — overkill
- Voice input (TR speech-to-text) — annyang araştırması (memory `kvkk-voice-research`) KVKK riski
- Çoklu cihaz live collaboration (Google Docs benzeri) — overkill

---

## 3. Mimari

### 3.0 Tarayıcı Kısıtlama Prereq (revize v2 — CRITICAL)

**A) HTTPS / Secure Context — ZORUNLU bariyer**

Modern mobile browser'lar `getUserMedia` (kamera) + Service Worker API'lerini **yalnızca Secure Context** (HTTPS veya localhost) altında çalıştırır. Saha deployment `http://mosaik.bkm.local` ile yapılırsa kamera açılmaz + PWA install çalışmaz.

**Prereq:** Saha deployment **geçerli SSL sertifikası + HTTPS (TLS)** zorunlu. INSTALL.md güncelle: BKM iç CA imzalı sertifika veya Let's Encrypt subdomain.

**B) iOS Safari PWA + Web Push — A2HS koşulu**

iOS 16.4+ Web Push destekli, ama kullanıcı bildirim almak için PWA'ı **önce "Ana Ekrana Ekle" (Add to Home Screen)** yapmak zorunda. Tarayıcı sekmesi içinden push subscribe **tetiklenmez** (Apple kuralı).

**Çözüm:** iOS Safari detect → zarif görsel rehber pop-up "Bildirimleri açmak için bu PWA'ı önce Ana Ekrana Ekle":

```
1. Paylaş butonuna bas (📤)
2. "Ana Ekrana Ekle" seç
3. Ana ekrandan Mosaik'i aç
4. Bildirimleri etkinleştir
```

iOS env: BKM saha cihazları envanteri kontrol — eski iOS varsa upgrade veya Android öncelik.

**C) Camera Cleanup — Pil tüketimi koruma**

`MediaStream` düzgün sonlandırılmazsa kamera arka planda çalışmaya devam eder, cihaz ısınır, pil 1 saatte tükenir. **stopStream zorunlu:**

```javascript
function stopStream(stream) {
  if (stream) {
    stream.getTracks().forEach(track => track.stop());
  }
}

// Modal kapatma + iptal + barkod okuma sonrası HER path'te çağrılır
// finally block + visibilitychange event (kullanıcı tab değiştirir) ekstra koruma
```

`navigator.permissions.query({name:'camera'})` ile aktif permission monitoring + admin debug paneli.

---

### 3.1 PWA Skeleton

```
Mosaik/wwwroot/
├─ manifest.json                — install metadata
├─ sw.js                        — service worker (Workbox bundled)
├─ assets/icons/                — 192/512 PNG (manifest + iOS splash)
└─ pwa/
   ├─ offline.html              — fallback page
   ├─ sw-bootstrap.js           — register + update flow
   ├─ db.js                     — IndexedDB wrapper (idb library)
   ├─ sync-queue.js             — Background Sync API
   ├─ push.js                   — Web Push subscribe
   └─ camera-barcode.js         — getUserMedia + ZXing widget
```

### 3.2 Service Worker Stratejisi

```js
// sw.js (Workbox 7)
import { precacheAndRoute, cleanupOutdatedCaches } from 'workbox-precaching';
import { registerRoute } from 'workbox-routing';
import { StaleWhileRevalidate, CacheFirst, NetworkFirst } from 'workbox-strategies';
import { BackgroundSyncPlugin } from 'workbox-background-sync';

// SOP read = StaleWhileRevalidate (gör → arka plan revalidate)
registerRoute(/^\/SOP\/(My|Sop)\/(Read|Details)/,
              new StaleWhileRevalidate({ cacheName: 'sop-read' }));

// Form definitions = CacheFirst (definition manifest version'lı)
registerRoute(/^\/Forms\/Render\/\d+/,
              new CacheFirst({ cacheName: 'form-def', plugins: [/* expiration */] }));

// Form submit = BackgroundSync (offline → IndexedDB queue → online retry)
registerRoute(/^\/Forms\/Submit/,
              new NetworkFirst({
                plugins: [new BackgroundSyncPlugin('form-submit-queue', { maxRetentionTime: 24 * 60 })]
              }),
              'POST');

// Static assets = precache (build sırasında manifest gen)
precacheAndRoute(self.__WB_MANIFEST);
```

### 3.3 IndexedDB Schema

```
DB: mosaik-offline (idb library)
Stores:
- sop-content:        { sopId, title, content, version, cachedAt }
- form-definitions:   { formId, surveyJsConfig, version, cachedAt }
- pending-submissions:{ id, formId, payload, attemptCount, lastError, createdAt }
- user-claims:        { userId, roles, departments, expiresAt }
```

### 3.4 Camera + Barcode Widget

```html
<!-- SurveyJS custom widget: barcode -->
<div data-field="barkod">
  <button type="button" onclick="startBarcodeScan('barkod_input')">
    <i class="fas fa-qrcode"></i> Tara
  </button>
  <input type="text" id="barkod_input" name="barkod" readonly />
</div>

<!-- Modal scanner -->
<div id="barcodeScannerModal" class="modal hidden">
  <video id="barcodePreview" autoplay></video>
  <button onclick="cancelScan()">İptal</button>
</div>

<script>
async function startBarcodeScan(targetInputId) {
  const stream = await navigator.mediaDevices.getUserMedia({
    video: { facingMode: 'environment' }   // arka kamera
  });
  // ZXing-js BrowserMultiFormatReader
  const reader = new ZXing.BrowserMultiFormatReader();
  reader.decodeFromStream(stream, 'barcodePreview', (result, err) => {
    if (result) {
      document.getElementById(targetInputId).value = result.text;
      stopStream(stream);
      closeModal();
    }
  });
}
</script>
```

### 3.5 Push Notification

```
Server tarafı:
- Plan 17 Faz H NotificationService genişlet:
  - PushSubscription tablosu (UserId + Endpoint + p256dh + auth keys)
  - SendPushAsync(userId, title, body, action) → WebPush.NET kütüphane
- VAPID anahtarları appsettings (Push:VapidPublicKey, Push:VapidPrivateKey)

Client tarafı:
- pwa/push.js: subscribe + endpoint kaydet
- sw.js: 'push' event handler → showNotification
```

### 3.6 Conflict Resolution

```
Offline submit senaryosu:
1. User offline form doldurur → IndexedDB queue
2. Online dönünce sync queue retry
3. Server tarafı: FormResponse insert (yeni ID, conflict yok normalde)
4. EXCEPTION: aynı form_unique_id (örn. günlük rapor 2026-05-25) zaten varsa →
   - Strateji A: Server kabul + duplicate flag (admin review)
   - Strateji B: Client çakışma UI'ı (offline submit reddedildi, ne yapayım?)
   - Önerim: Strateji A. Veri kaybı yok, admin sonradan birleştir/sil.
```

---

## 4. Alternatifler (5 lens)

### 🔴 Contrarian: Fatal flaw?

**Service worker version skew.** SW bir kez yüklendikten sonra browser cache'lediği SW'i kullanır. Yeni deploy → SW güncellenme 24h+ gecikebilir. User stale form definition'la submit yapar → server reddediyor → kullanıcı hayal kırıklığı.

**Mitigation:**
- SW `skipWaiting()` + `clientsClaim()` ile hızlı geçiş
- Form definition `version` field — server `Conflict 409` döner → client SW refresh + retry
- Manifest auto-update poll (her 30dk)

### 🔵 First Principles: Gerçek problem?

PWA değil. Gerçek problem: **sinyalsiz koridor + el ile barkod yazımı + sayım sonu data kaybı.**

PWA çözümlerden biri ama overkill olabilir:
- **Hafif yol:** Sadece form submit IndexedDB queue + barcode scanner (ZXing). Service worker yok, manifest yok.
- **Orta yol:** PWA + offline form submit + barcode (Plan 46 önerisi)
- **Ağır yol:** Full offline mode (SOP + dashboard + report tüm modüller offline)

**Karar:** Orta yol önerim. Hafif yol depodaki sayım için yeterli ama mağaza personeli "SOP'a bakayım sinyal yok" derse çare yok. Ağır yol overkill — dashboard offline değer az.

### 🟢 Expansionist: Daha büyük fırsat?

PWA + camera = **Computer Vision input pipeline.**

Mevcut Plan 27 vision provider (Gemini + OpenAI fallback) sözleşme PDF için. **Saha personeli kamera + AI vision**: depocu hasarlı ürün fotosu çeker → Qwen vision (Plan 34.1 model genişletmesi) "bu üründe yırtık paket var, hasar tipi: yırtık ambalaj" auto-fill.

Plan 46 v2 scope adayı (Faz 6+).

### ⚪ Outsider: Garip ne?

"Mosaik server-rendered Razor MVC — niye PWA? React/Vue SPA mantıklı değil mi?"

**Cevap:** Mosaik vNext kalbi server-side render (Plan 25.1 sonrası kabul). PWA service worker + IndexedDB sadece **belirli sayfa** path'lerini offline'a alır — full SPA gerekmez. Saha personelinin gördüğü sayfa sayısı sınırlı (form/sop read/dashboard widget). PWA pragmatik orta yol.

### 🟡 Executor: Pazartesi sabahı?

1. manifest.json + icon set (PWA test minimum)
2. sw.js boş skeleton + Workbox precache static asset
3. Lighthouse PWA score ≥ 50 target
4. ZXing-js NuGet/CDN, basic test sayfası
5. SurveyJS custom barcode widget (3-4 saat)
6. Plan 41 Form Builder bekleyiş — form render hazır olmadan offline sync test edemiyoruz

---

## 5. Riskler

| Risk | Olasılık | Etki | Mitigation |
|---|---|---|---|
| SW version skew → stale form → submit fail | yüksek | orta | version field + 409 retry + auto-update poll |
| iOS Safari PWA limit (push, background sync) | orta | yüksek | iOS BKM cihaz envanteri kontrol; Android öncelik |
| IndexedDB quota exceeded (10GB+ Excel attach) | düşük | orta | quota check + file attach uyarı + cleanup |
| Camera permission reddedilirse fallback yok | orta | orta | "Kamera izni kapalı, manuel girişe geç" UI |
| Barkod yanlış okur → yanlış ekipman | orta | yüksek | Barcode okuduktan sonra confirm UI + manual override |
| Background sync 24h pencerede yetişemezse | düşük | yüksek | Sync attempt counter + admin notification + manuel re-sync |
| KVKK: kamera kayıt izni + workplace surveillance | orta | yüksek | Kamera SADECE form scope (snapshot), kayıt YOK; KVKK aydınlatma metni |

---

## 6. Done Criteria

- [ ] PWA installable (Chrome/Safari "Ana ekrana ekle" çalışır)
- [ ] Lighthouse PWA score ≥ 90
- [ ] SOP read offline (cache hit ≥ 80% 2. ziyaret)
- [ ] Form submit offline → queue → online sync (5-cihaz manuel test)
- [ ] ZXing barkod scan EAN-13/QR/Code128 ≥ 95% doğruluk (10 deneme)
- [ ] SurveyJS "barcode" custom field type Plan 41 entegrasyonu
- [ ] Push notification: tamim publish → mobil cihaza bildirim (1dk içinde)
- [ ] Conflict (duplicate submit) admin review UI
- [ ] KVKK aydınlatma metni güncellendi (kamera + push subscription)
- [ ] ADR-028 yazımı (PWA pattern + offline-first stratejisi)
- [ ] iOS Safari + Android Chrome end-to-end test geçti

---

## 7. Rollback

- manifest.json silinir → PWA install çalışmaz, eski responsive web çalışır
- sw.js silinince user'da var olan SW kendini unregister edemez — `sw-bootstrap.js` içine `unregister()` yedek path
- IndexedDB cleanup endpoint admin için
- Push subscription unsubscribe endpoint

---

## 8. Adımlar / Fazlar

### Faz 1 — PWA skeleton (10h)
- manifest.json + icon set (PWA Asset Generator)
- sw.js Workbox 7 precache + StaleWhileRevalidate SOP read
- sw-bootstrap.js register + update flow
- Lighthouse target ≥ 90

### Faz 2 — Offline form submit + IndexedDB (14h)
- idb library + db.js schema
- Form definition CacheFirst + version field
- BackgroundSync queue + retry
- 409 conflict handling

### Faz 3 — Camera + Barcode (12h)
- ZXing-js NuGet/CDN
- camera-barcode.js + modal UI
- SurveyJS custom field type
- Mobile UX (bottom nav, swipe)

### Faz 4 — Push notification (10h)
- WebPush.NET server library
- PushSubscription tablo + migration
- VAPID anahtarları + appsettings
- Plan 17 Faz H NotificationService genişlet
- Tamim publish hook → push send

### Faz 5 — KVKK + audit + test (8h)
- KVKK aydınlatma metni güncelle
- Audit: offline_submit_synced event
- 5-cihaz end-to-end manuel test
- ADR-028

---

## 9. Bağımlılıklar

- **Prereq:** Plan 41 Faz 0-3 (FormDefinition + render)
- **Plan 17 Faz H reuse:** NotificationService genişlet
- **Plan 27 Faz E (sonra):** vision provider Qwen extension (v2 expansionist scope)
- **OSS:** Workbox 7 (MIT), idb (MIT), ZXing-js (Apache 2.0), WebPush.NET (MIT)

---

## 10. Açık Sorular — KAPATILDI 2026-05-25 (revize v2 kararları)

1. **iOS Safari push desteği eksikliği için workaround?**
   **Karar:** iOS 16.4+ standart PWA push API destekliyor (A2HS koşuluyla). **Native wrapper YOK.** Saha cihazları iOS 16.4+ olması sağlanacak. iOS Safari detect → A2HS rehber pop-up (§3.0 B).

2. **Barkod kütüphanesi tercihi?**
   **Karar:** **`@zxing/library` (ZXing-js).** EAN-13 + QR + Code128 + DataMatrix + PDF417 — en geniş format. QuaggaJS sadece 1D + bakım yavaş. html5-qrcode wrapper ama esneklik düşük. ZXing core engine bize Mosaik wrapper yazma esnekliği veriyor. Apache 2.0 lisans.

3. **Conflict (Çakışma) varsayılan politikası?**
   **Karar:** **Strateji A — Server Accept + Duplicate Flag.** Depo/saha personeli offline sayım sync ettiğinde reddetmek yerine kabul. Server duplicate kabul + audit `ImportedOffline: true` flag (FormResponse kolonu) + Admin Inbox inceleme uyarısı. Saha personeline karmaşık çakışma UI YOK. Admin sonradan merge/delete.

4. **PWA + KVKK Push subscribe izni ne zaman?**
   **Karar:** **Soft-Ask (Giriş Sonrası Banner).** Login sonrası ana ekranda dismissible banner: "Yeni prosedürler ve acil duyurulardan haberdar olmak için bildirimleri açmak ister misiniz?" Onay → tarayıcı push permission tetiklenir. Red → bir daha rahatsız etme (localStorage flag, opt-in re-prompt admin ayarı).

5. **Kamera görüntüleri kaydedilmemesi nasıl enforce?**
   **Karar:** **In-Memory Canvas Extraction.** Video stream sunucuya **asla** gönderilmez. QR okunduğu anda veya fotoğraf upload anında `canvas.toBlob()` → in-memory PNG/JPEG → form submit payload (Blob veya Base64). Video frame'ler RAM'de bırakılır + `stopStream` zorunlu. **KVKK aydınlatma metnine teknik taahhüt olarak eklenir.** Kod review checklist: `MediaRecorder` API YASAK (sadece snapshot, kayıt değil).

---

## 11. Onay + Revize Notu

**ONAYLANDI 2026-05-25** — Kullanıcı strategic review:
- 3 tarayıcı kısıtlama plan'a eklendi: HTTPS prereq + iOS A2HS A2HS + camera stopStream cleanup (§3.0)
- 5 açık soru cevaplandı + plan'a karar olarak gömüldü
- Implementation Plan 41 Faz 0-3 tamamlandığında başlar
- Saha SSL sertifika INSTALL.md prereq olarak eklenecek (deployment guide)
