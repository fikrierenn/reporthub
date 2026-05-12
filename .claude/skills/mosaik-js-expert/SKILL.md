---
name: mosaik-js-expert
description: Mosaik projesi vanilla JS + Alpine + HTMX uzman skill'i. Yeni JS dosyası yazılırken, fetch / POST yaparken, DOM manipülasyon eklenirken, Alpine `x-data` factory yazılırken zorunlu tetiklenir. IIFE wrap + ES5 hedef + XSS textContent + AntiForgery global helper + addEventListener + delegation pattern + Plan 25.2 kuralları (V1 Builder asset silindi, dead reference yok) garanti eder. alpine-js + htmx-expert skill'leri ile zincirleme.
---

# Mosaik JS Uzman Skill'i

## Ne zaman tetiklenir

- Yeni `.js` dosyası yazılırken (`wwwroot/assets/js/`)
- View'ın `@section Scripts` veya inline `<script>` bloğu yazılırken
- `fetch` POST / AntiForgery token gerektiren çağrı yazılırken
- DOM manipülasyon yazılırken (createElement, querySelector vs)
- Alpine `x-data` factory veya `x-on:` handler yazılırken
- HTMX trigger eklenirken
- Event listener atanırken (re-render leak riski)

## Mosaik JS mimari

```
wwwroot/assets/js/
├── app-shell.js              (118)  — sidebar/topbar + global helper'lar
│                                       window.getAntiForgeryToken() (cached)
│                                       window.sidebarGroup(key) Alpine factory
├── admin-datasource-form.js  (170)  — EditDataSource connection string builder
├── builder-v2/               (19 dosya, ~3K satır) — V2 Dashboard Builder
│   ├── builder-utils.js              — __BuilderV2.escHtml/.fmtCell/.fmtChart
│   ├── builder.js                    — main composition + drag-drop
│   ├── builder-canvas/-chart/-table  — widget render
│   └── ...
├── org-chart-render.js       (84)   — dabeng OrgChart wrapper
└── test-page.js              (23)   — dev-only test page
```

**Yükleme:** Her view kendi `<script src="~/assets/js/...">` ile yükler. `app-shell.js` `_AppLayout`'ta her sayfada.

## Kurallar (sıkı)

### 1. IIFE wrap zorunlu

```js
// app-shell.js veya benzeri
(function () {
    "use strict";

    // Private state, helpers
    var state = { open: false };

    function privateHelper(x) { return x.toLowerCase(); }

    // Global expose (gerekiyorsa, minimum)
    window.myFeature = function (opts) { /* ... */ };

    // Init
    init();
})();
```

Arrow IIFE `(() => { ... })();` de OK. `"use strict"` önerilir.

### 2. Vanilla JS — framework yok

- **React/Vue/Svelte yok**, sadece Alpine.js 3.x + HTMX 2.x CDN
- **jQuery YASAK** (yeni kod) — istisna: `org-chart-render.js` dabeng OrgChart.js dependency
- **ES5 uyumlu target:** `var` + `function` çoğunluk. Modern syntax (arrow, const, template literal) ölçülü kullanılabilir — IE11 desteği yok artık.

### 3. AntiForgery global helper (Plan 25.2)

```js
// app-shell.js'te tanımlı, her sayfada yüklü
var token = window.getAntiForgeryToken();  // cached, ilk çağrıda DOM'dan alır
if (!token) { /* hata göster */ return; }

fetch('/Admin/Action', {
    method: 'POST',
    credentials: 'same-origin',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': token,
        'X-Requested-With': 'fetch'
    },
    body: JSON.stringify(data)
});
```

**Eski pattern yasak** (`document.querySelector('input[name="__RequestVerificationToken"]')` duplicate). 5 callsite Plan 25.2'de helper'a çevrildi.

### 4. DOM manipülasyon — XSS-safe

```js
// İYİ — user data ile textContent
var el = document.createElement('div');
el.className = 'error-message';
el.textContent = errorMsg;  // user data güvenli
container.appendChild(el);

// İYİ — escape helper (builder-v2)
container.innerHTML = '<span>' + window.__BuilderV2.escHtml(userVal) + '</span>';

// YASAK — user data innerHTML string concat
container.innerHTML = '<div>' + userMsg + '</div>';  // XSS riski

// YASAK — eval
eval(jsonStr);
new Function(jsonStr);

// İYİ — JSON parse
var data = JSON.parse(jsonStr);
```

`innerHTML` kullanımı:
- **Server-template trusted** (Razor render edildi, sanitize edildi) → OK ama yorumda belirt
- **User input** → KESİNLİKLE textContent veya escape

### 5. Event listener pattern

```js
// İYİ — addEventListener
var btn = document.getElementById('saveBtn');
btn.addEventListener('click', function (e) { /* ... */ });

// YASAK — inline onclick (CSP uyumluluk + DRY)
// <button onclick="doSomething()">

// İYİ — delegation çok element için
container.addEventListener('click', function (e) {
    var btn = e.target.closest('.row-action');
    if (!btn) return;
    var id = btn.dataset.id;
    handleAction(id);
});
```

**Re-render leak uyarısı:** `addEventListener` tekrar bağlanırsa birikir. Çözüm:
- Event delegation (parent container'a tek listener — çok element pattern)
- `AbortController` (modern) — eski listener temizle
- Re-render'da `removeEventListener` ile dengele

### 6. Fetch pattern

```js
fetch('/Admin/Action?id=' + encodeURIComponent(id), {
    credentials: 'same-origin',
    headers: { 'X-Requested-With': 'fetch' }
})
    .then(function (r) {
        if (!r.ok) throw new Error('HTTP ' + r.status);
        return r.json();
    })
    .then(function (data) {
        if (!data.success) {
            showError(data.error || 'Hata oluştu.');
            return;
        }
        render(data.resultSets);
    })
    .catch(function (err) {
        showError('Beklenmedik hata: ' + err.message);
    });
```

- **URL encode** query params: `encodeURIComponent(value)`
- **`credentials: 'same-origin'`** auth cookie için
- **`X-Requested-With: 'fetch'`** AJAX işaretle
- **Error handling** `catch` zorunlu, Türkçe user mesaj
- **AntiForgery** POST'larda `RequestVerificationToken` header

### 7. Alpine.js pattern

```html
<div x-data="myWidget()" x-cloak>
    <button x-on:click="toggle()" x-text="open ? 'Kapat' : 'Aç'"></button>
    <div x-show="open" x-transition>İçerik</div>
</div>

<script>
function myWidget() {
    return {
        open: false,
        toggle: function () { this.open = !this.open; },
        async save() {
            var token = window.getAntiForgeryToken();
            // ...
        }
    };
}
</script>
```

- **`x-cloak`** CSS `[x-cloak] { display: none !important; }` (zaten tokens.css'te) — initial flash önle
- **`x-data="factory()"`** — factory function pattern, JS dosyasında tanımla
- **`x-on:click`** veya `@click` Razor'da `@@click` (Razor `@` escape)
- **`x-transition`** Alpine 3.x built-in (plugin değil)
- **`x-collapse`** plugin gerekli, lokal Alpine'da yok → `x-show + x-transition` kullan
- Cross-file event: `window.dispatchEvent(new CustomEvent('xyz', { detail: data }))`

### 8. HTMX pattern (server swap)

```html
<button hx-post="/Reports/Run/@reportId"
        hx-target="#result"
        hx-swap="innerHTML"
        hx-headers='{"RequestVerificationToken":"@token"}'>
    Çalıştır
</button>
<div id="result"></div>
```

- HTMX 2.x CDN
- `htmx-expert` skill'i detay için
- AntiForgery `hx-headers` ile gönder (HTML attribute'a tokenı kaç)

### 9. Dosya boyutu

- **Soft-limit: 250 satır** — "bölünebilir mi?" sorgusu
- **Hard-limit: 350 satır** — sonraki commit'in ilk işi bölme
- Çok-modüllü feature: alt-klasör `js/<feature>/<feature>-<konu>.js`

Şu an Mosaik'te aşan yok. En büyük `builder-v2/builder-render.js` 337 (sınırda).

### 10. State management

- **Dosya-yerel** IIFE içinde state
- **Global state** sadece açık isim: `window.__BuilderV2`, `window.__RS` gibi
- **Cross-file:** Custom event (`document.dispatchEvent`) + listener veya namespace üzerinden expose

### 11. Tailwind dinamik class

```js
// JIT'in görmediği dinamik class — utility name listede olmalı
element.className = 'px-4 py-2 ' + (isActive ? 'bg-blue-100' : 'bg-gray-100');

// YASAK — runtime'da class oluşturma (Tailwind bu yapıyı görmez)
element.className = 'bg-' + colorName + '-500';
```

## Pattern — yeni JS dosyası

```js
// my-feature.js — Açıklama 1 satır
// Bağımlılıkları: window.getAntiForgeryToken (app-shell.js)
// Yüklendiği view'lar: Mosaik/Views/My/Index.cshtml

(function () {
    "use strict";

    var els = {
        form: document.getElementById('myForm'),
        result: document.getElementById('myResult')
    };

    if (!els.form) return;  // sayfada yok — kibarca çık

    function showError(msg) {
        els.result.textContent = msg;
        els.result.classList.add('is-error');
    }

    function handleSubmit(e) {
        e.preventDefault();
        var fd = new FormData(els.form);
        var token = window.getAntiForgeryToken();
        if (!token) { showError('AntiForgery yok — sayfayı yenileyin.'); return; }

        fetch('/My/Save', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'RequestVerificationToken': token, 'X-Requested-With': 'fetch' },
            body: fd
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) location.reload();
                else showError(data.error || 'Kaydedilemedi.');
            })
            .catch(function (err) { showError('Ağ hatası: ' + err.message); });
    }

    els.form.addEventListener('submit', handleSubmit);
})();
```

## Anti-pattern (yasak)

- IIFE yok dosya (global namespace kirletir)
- `var token = document.querySelector('input[name="__RequestVerificationToken"]')` (helper kullan)
- `innerHTML = '<div>' + userInput + '</div>'` (XSS)
- `eval()` / `new Function(jsonStr)`
- `onclick="..."` inline HTML attribute (CSP)
- jQuery `$` yeni kod (dabeng wrapper istisna)
- `console.log` üretim kalır (debug.log/info OK ama meşru sebep)
- `setTimeout(fn, 0)` work-around (gerçek sebebi düzelt)
- Promise'i swallow (catch yok)
- Hardcoded URL (`fetch('/Admin/...')` OK — `fetch('https://prod.com/...')` yasak)

## Plan 25.2 sonuçları (2026-05-13)

- V1 dead silindi: `dashboard-builder/` (7 dosya), `admin-report-form/` (2 dosya) = 1535 satır
- `dashboard-builder.css` silindi (435 satır)
- AntiForgery global helper extract — 5 callsite tek source
- `org-chart-render.js` XSS sertleştirme (innerHTML → createElement+textContent)

**Toplam JS sağlık raporu:**
- Dead reference: 0
- IIFE eksik: 0 (admin-datasource + test-page wrap'li, agent raporu yanlıştı)
- Hard-limit 350 aşan: yok
- innerHTML user-data: 0 (org-chart fix sonrası)
- eval / new Function: 0
- var/let karışık: 0
- console.log: 1 (Gridstack uyarı, meşru)
- Event listener leak: 0 belirlendi

## Hızlı referans

- `Mosaik/wwwroot/assets/js/app-shell.js` — global helper'lar, sidebar
- `Mosaik/wwwroot/assets/js/builder-v2/builder-utils.js` — escHtml, fmtCell, fmtChart
- `.claude/rules/js-conventions.md` — JS kuralları detay
- `.claude/rules/security-principles.md` — XSS + güvenlik
- `.claude/skills/alpine-js/SKILL.md` — Alpine 3.x reactive directive
- `.claude/skills/htmx-expert/SKILL.md` — HTMX server-driven swap

## İlişkili skill'ler

- `alpine-js` — Alpine `x-data/x-show/x-model/x-on` pattern
- `htmx-expert` — HTMX trigger + swap
- `mosaik-csharp-razor` — POST endpoint + AntiForgery server-side
- `mosaik-css-expert` — JS-toggled state class'ları
