---
paths:
  - "ReportPanel/wwwroot/assets/js/**/*.js"
---

# JavaScript Konvansiyonları

## Dosya Yapısı

- **Konum:** `wwwroot/assets/js/<ozellik>.js`.
- **IIFE wrap:** Her dosya bir IIFE ile sarılı, global namespace'i koru.
- **Dosya boyutu (M-11 F-7'de kabul, 27 Nis 2026):**
  - **Soft-limit: 250 satır** — bu eşiği geçen modülde "konuya göre alt-modüle bölünebilir mi?" sorusu sorulur.
  - **Hard-limit: 350 satır** — bu eşiği geçen modül **bölünmek zorunda**, sonraki commit'in ilk işi.
  - Çok-modüllü feature'larda alt-klasör pattern: `wwwroot/assets/js/<feature>/<feature>-<konu>.js` (örn. `dashboard-builder/builder-core.js`, `builder-drawer.js`).

```js
(function() {
    "use strict";

    // Private helpers, state
    var state = { ... };

    function privateHelper() { ... }

    // Global expose (gerekiyorsa, minimum)
    window.myFeatureAction = function() { ... };

    // Init
    init();
})();
```

## Vanilla JS

- **Framework yok** — React, Vue, jQuery **eklenmiyor**.
- **ES5 uyumlu** target — `var` + `function`, arrow function ve `const` ölçülü.
- **Fetch API** (XHR değil).

## DOM Manipülasyonu

- **`document.createElement` + `textContent`** birincil.
- **`innerHTML` yasak** user-data içerse (XSS).
- **Event listener** — `addEventListener` (onclick inline yasak).
- **Delegation** — çok sayıda element için container'a tek listener.

## Güvenlik

- **XSS:** `element.textContent = userValue` ✓. `element.innerHTML = userHtml` ✗ (user data ise).
- **`eval()` yasak.** Dinamik dispatch için `window['funcName']` veya object map.
- **JSON:** `JSON.parse(jsonString)` ✓. `new Function(jsonString)` ✗.

## Event Handling

```js
// İyi
var btn = document.getElementById('actionBtn');
btn.addEventListener('click', function(e) { ... });

// Kötü (inline onclick)
<button onclick="doSomething()">...</button>
```

## State Management

- **State dosya-yerel** (IIFE içinde).
- **Global state** sadece `window.__RS` gibi açık isimlendirilmiş.
- **Cross-file iletişim:** Custom event (`document.dispatchEvent(new CustomEvent('xyzReady', { detail: data }))`) + listener.

## Drag-Drop Pattern (dashboard-builder için)

HTML5 native API:
```js
element.setAttribute('draggable', 'true');
element.addEventListener('dragstart', function(e) {
    e.dataTransfer.setData('text/plain', index);
});
element.addEventListener('dragover', function(e) { e.preventDefault(); });
element.addEventListener('drop', function(e) {
    e.preventDefault();
    var from = parseInt(e.dataTransfer.getData('text/plain'));
    // reorder
});
```

**Memory leak uyarısı:** Her re-render'da listener'lar tekrar atanırsa birikir. Çözüm:
- Event delegation (parent container'a tek listener).
- Veya `AbortController` ile eski listener'ları temizle.

## Tailwind + Dinamik Class

```js
element.className = 'px-4 py-2 ' + (isActive ? 'bg-blue-100' : 'bg-gray-100');
```

Tailwind JIT'in görmediği dinamik class **inline ekleme**, explicit list kullan.

## Fetch Pattern

```js
fetch('/Admin/SpPreview?dataSourceKey=' + encodeURIComponent(dsKey))
    .then(function(r) { return r.json(); })
    .then(function(data) {
        if (!data.success) {
            showError(data.error);
            return;
        }
        render(data.resultSets);
    })
    .catch(function(err) {
        showError('Beklenmedik hata: ' + err.message);
    });
```

- **URL encode** query parametreler.
- **Error handling** — `catch` zorunlu, kullanıcıya net mesaj.
- **AntiForgery token** POST'larda: `fetch(url, { method: 'POST', headers: { 'RequestVerificationToken': token } })`.

## Dashboard Builder Özel Notlar

- `dashboard-builder.js` IIFE içinde `render()`, `attachDragDrop()`, `renderForm()`, event handler'lar.
- `window.__spPreview` global — SP Preview sonucunu builder'a taşır.
- `document.addEventListener('spPreviewReady', ...)` — henüz bağlanmadı (TODO FAZ 1, kolon datalist).

## Syntax Check

Her edit sonrası:
```bash
node -e "new Function(require('fs').readFileSync('wwwroot/assets/js/xyz.js','utf8'))"
```

`Stop` hook bunu otomatik yapıyor (planlı).
