---
name: alpine-js
description: Alpine.js 3.x patterns + Razor MVC entegrasyonu. CDN-only (build step yok), Tailwind ile birebir uyum, ~15kb. Vanilla JS DOM API kalabaligini (createElement/appendChild/event listener) reactive directive'lara (x-data/x-show/x-on/x-model) cevirir. ASP.NET Core Razor view'larda `<div x-data="{...}">` ile mount, htmx ile birlikte kullanilabilir (Alpine = client state, htmx = server swap). HTMX skill'i ile zincirleme calisir.
---

# Alpine.js + Razor MVC Skill

Vanilla JS DOM API kodunun **%70'i** Alpine reactive directive'larla yok edilebilir. Build step yok, CDN.

## When to Apply

✅ **Use Alpine when:**
- Form state (toggle, mode segmented, dirty flag, modal open/close)
- Conditional render (show/hide, tab switch)
- Dynamic list (filter row add/remove, item array CRUD)
- Two-way binding (input ↔ state)
- Dropdown / accordion / tooltip / popover

❌ **Skip Alpine when:**
- Server-side data swap (kullan htmx)
- Tek seferlik DOM injection (vanilla yeterli)
- 1000+ item liste (Alpine virtualization yok)
- Complex state graph (Vue/React düşün)

## CDN Setup (Razor MVC)

`Views/Shared/_AppLayout.cshtml` `<head>` veya `<body>` sonu:

```html
<!-- Alpine 3.x defer (DOM hazır olduktan sonra başlat) -->
<script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
```

`defer` ZORUNLU — DOMContentLoaded öncesi `x-data` parse edemez.

## Core Directives

| Directive | Vanilla JS karşılığı | Use case |
|---|---|---|
| `x-data="{open:false}"` | scope state | component state |
| `x-show="open"` | `display:none` toggle | conditional visibility |
| `x-on:click="open=!open"` (`@click`) | `addEventListener('click', ...)` | event binding |
| `x-model="username"` | input.value sync | two-way binding |
| `x-text="title"` | `el.textContent = title` | reactive text |
| `x-html="content"` | `el.innerHTML = content` (XSS dikkat) | reactive HTML |
| `x-bind:class="{active:isActive}"` (`:class`) | classList.add/remove | conditional class |
| `x-for="item in items"` | createElement loop | reactive list |
| `x-if="cond"` | template branch | structural conditional |
| `x-init="loadData()"` | DOMContentLoaded handler | mount-time hook |
| `x-ref="myInput"` + `$refs.myInput` | getElementById | template ref |
| `x-cloak` | flash-of-unstyled-content guard | initial hide |

## Razor + Alpine Patterns

### Pattern 1 — Toggle (parola show/hide)

**Vanilla (15+ satır):**
```html
<input type="password" id="pw">
<button id="toggleBtn">Göster</button>
<script>
document.getElementById('toggleBtn').addEventListener('click', function() {
    var input = document.getElementById('pw');
    if (input.type === 'password') { input.type = 'text'; this.textContent = 'Gizle'; }
    else { input.type = 'password'; this.textContent = 'Göster'; }
});
</script>
```

**Alpine (3 satır):**
```html
<div x-data="{shown:false}">
    <input :type="shown ? 'text' : 'password'" />
    <button @click="shown=!shown" x-text="shown ? 'Gizle' : 'Göster'"></button>
</div>
```

### Pattern 2 — Mode Segmented (Düzenle/Önizle)

```html
<div x-data="{mode:'edit'}" role="tablist">
    <button @click="mode='edit'" :class="{active:mode==='edit'}" role="tab" :aria-selected="mode==='edit'">Düzenle</button>
    <button @click="mode='preview'" :class="{active:mode==='preview'}" role="tab" :aria-selected="mode==='preview'">Önizle</button>

    <div x-show="mode==='edit'">@* edit form *@</div>
    <div x-show="mode==='preview'">@* preview *@</div>
</div>
```

### Pattern 3 — Dirty Chip (form değişikliği takibi)

```html
<form x-data="{dirty:false}" @input="dirty=true">
    <span class="dirty-chip" x-show="dirty">kaydedilmemiş</span>
    <input name="Title" />
    <button type="submit" @click="dirty=false">Kaydet</button>
</form>
```

### Pattern 4 — Dynamic List (filter row add/remove)

**Vanilla (~90 satır createElement loop) → Alpine (~15 satır):**

```html
<div x-data="{
    filters: [],
    addFilter() { this.filters.push({key:'sube', value:'', ds:''}); },
    removeFilter(i) { this.filters.splice(i, 1); }
}">
    <template x-for="(f, i) in filters" :key="i">
        <div class="filter-row">
            <select x-model="f.key" name="FilterKeys" class="inp"><!-- options --></select>
            <select x-model="f.value" name="FilterValues" class="inp"></select>
            <button type="button" @click="removeFilter(i)" class="btn ghost">Sil</button>
        </div>
    </template>
    <button type="button" @click="addFilter()" class="btn ghost">+ Filtre Ekle</button>
</div>
```

### Pattern 5 — Server data injection (Razor)

Razor'dan Alpine'a JSON-encoded data geçir:

```cshtml
<div x-data="@(Html.Raw(System.Text.Json.JsonSerializer.Serialize(new {
    items = Model.Items,
    selectedId = Model.SelectedId
})))">
    <template x-for="item in items">
        <div :class="{active: item.id === selectedId}" x-text="item.title"></div>
    </template>
</div>
```

`@Html.Raw` + `JsonSerializer.Serialize` kombinasyonu — XSS güvenli (JSON encoding HTML attribute içinde escape eder).

### Pattern 6 — htmx ile birlikte

```html
<!-- Alpine = client state, htmx = server swap -->
<div x-data="{loading:false}">
    <button @click="loading=true"
            hx-get="/Admin/SpPreview?dataSourceKey=PDKS"
            hx-target="#result"
            hx-on::after-request="loading=false">
        Önizle
    </button>
    <span x-show="loading">Yükleniyor...</span>
    <div id="result"></div>
</div>
```

## Anti-Patterns

❌ **`x-data` global scope kirletme** — her component kendi `x-data` scope'unda olmalı.

❌ **`x-html` user input ile** — XSS riski. Sadece güvenilir HTML.

❌ **`@click` inline complex logic** — 3+ satır olursa metod tanımla:
```html
<!-- Kötü -->
@click="if(x){y=1; z=2; w=3} else {y=0}"
<!-- İyi -->
x-data="{handle(x) { if(x) {...} else {...} }}"
@click="handle(x)"
```

❌ **`x-show` vs `x-if` karıştırma** — `x-show` toggle (CSS), `x-if` mount/unmount (DOM). Heavy component için `x-if`, sık toggle için `x-show`.

❌ **Tailwind CDN içinde dinamik class** — `:class="{'bg-red-500':err}"` Tailwind JIT görmez. Explicit class list kullan.

## Performance

- **15kb gzipped** — küçük
- **No virtual DOM** — direct DOM mutation, küçük ölçek için ideal
- **Lazy mount** — `x-data` görüldüğünde başlar, sayfa-wide observer yok
- **1000+ item liste** — virtualization yok, performans düşer (Vue/React düşün)

## ASP.NET Core'a özel notlar

- `@` Razor escape — `@@click` → `@click`. `defer` script ile Alpine'ı yükle, view'larda direkt directive yaz.
- AntiForgeryToken: form submit'lerde `@Html.AntiForgeryToken()` korunur (Alpine attribute eklerken hidden input'a dokunma).
- Razor section'lar: `@section Scripts { ... }` Alpine init logic için — ama mümkünse view inline tutar (Alpine'ın felsefesi).

## Migration Stratejisi (Vanilla → Alpine)

1. **Inventory** — Vanilla JS DOM API kullanan dosyaları listele
2. **Triage** — UI state (Alpine) vs server-roundtrip (htmx) ayır
3. **Sayfa-by-sayfa** — bir sayfa al, vanilla JS bloğunu Alpine directive'larıyla değiştir
4. **Smoke test** — preview ile davranış doğrula
5. **Old JS sil** — eski IIFE bloğu kaldır

## Kaynaklar

- [Alpine.js docs](https://alpinejs.dev/)
- [Alpine.js GitHub](https://github.com/alpinejs/alpine)
- [AspNetCoreWithAlpineJs sample](https://github.com/marcominerva/AspNetCoreWithAlpineJs)
- [Hydro framework](https://usehydro.dev/) — Alpine + Razor stateful components
- htmx-expert skill (zincirleme)
