# UI Snippets — Copy-Paste Kalıpları

_Eşlik: `ui-patterns.md` (kurallar). Bu dosya: tam HTML snippet'leri kopyala-yapıştır içindir._

## §3 Hero
```cshtml
<div class="hero">
    <h1>Sayfa Başlığı</h1>
    <div class="sub">
        <span class="mono">@count kayıt</span>
        <span class="pill"><span class="dot"></span>Durum</span>
    </div>
</div>
```

## §4 Filter Bar (Liste sayfaları)
```cshtml
<div x-data="{ q: '@Model.SearchTerm' }">
<form method="get"
      style="background:var(--paper); border:1px solid var(--line); border-radius:10px;
             padding:14px 16px; margin-bottom:18px; display:flex; gap:12px;
             flex-wrap:wrap; align-items:end;">

    <div style="flex:1; min-width:240px;">
        <label class="lab" for="filterSearch">Arama</label>
        <div style="position:relative;">
            <span style="position:absolute; left:10px; top:50%; transform:translateY(-50%);
                         color:var(--ink-4); pointer-events:none;">
                <i class="fas fa-magnifying-glass" style="font-size:12px;" aria-hidden="true"></i>
            </span>
            <input type="text" id="filterSearch" name="q" x-model="q"
                   class="inp" style="padding-left:32px;"
                   placeholder="Kullanıcı, başlık..." />
        </div>
    </div>

    @* Opsiyonel select filtreler *@
    @* <div style="min-width:180px;">
        <label class="lab" for="filterX">Kategori</label>
        <select id="filterX" name="cat" class="inp">
            <option value="">Tümü</option>
            ...
        </select>
    </div> *@

    <div style="display:flex; gap:6px;">
        <button type="submit" class="btn primary">
            <i class="fas fa-filter text-[10px]" aria-hidden="true"></i>
            Filtrele
        </button>
        <button type="button" class="btn ghost" @@click="q = ''" x-show="q" x-cloak>
            <i class="fas fa-xmark text-[10px]" aria-hidden="true"></i>
            Temizle
        </button>
    </div>
</form>
</div>
```

Tablo satırlarında: `<tr data-search="@hayString" x-show="!q || $el.dataset.search.includes(q.toLowerCase())" x-cloak>`

## §5 Empty State
```cshtml
<div style="background:var(--paper); border:1px dashed var(--line); border-radius:10px;
            padding:48px 24px; text-align:center; color:var(--ink-3);">
    <i class="fas fa-folder-open" style="font-size:48px; opacity:.4; margin-bottom:12px;" aria-hidden="true"></i>
    <h3 style="font-size:14px; font-weight:600; color:var(--ink-1); margin:0 0 4px;">
        Henüz kayıt yok
    </h3>
    <p style="margin:0; font-size:12.5px;">
        İlk kaydı oluşturmak için <a href="/Create" style="color:var(--accent); font-weight:500;">yeni ekle</a>.
    </p>
</div>
```

## §6 Tablo
```cshtml
<div style="background:var(--paper); border:1px solid var(--line); border-radius:10px; overflow:hidden;">
    <table class="dt">
        <thead>
            <tr>
                <th scope="col">Kullanıcı</th>
                <th scope="col">Ad Soyad</th>
                <th scope="col">Durum</th>
                <th scope="col">Son Giriş</th>
                <th scope="col" style="text-align:right;">İşlemler</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model.Items)
            {
                <tr>
                    <td><span class="mono">@item.Username</span></td>
                    <td>@item.FullName</td>
                    <td>
                        <span class="status @(item.IsActive ? "ok" : "err")">
                            @(item.IsActive ? "Aktif" : "Pasif")
                        </span>
                    </td>
                    <td><span class="ago">@(item.LastLoginAt?.ToString("dd.MM.yyyy HH:mm") ?? "-")</span></td>
                    <td style="text-align:right;">
                        <div style="display:inline-flex; gap:6px;">
                            <a href="/Edit/@item.Id" class="btn ghost sm">
                                <i class="fas fa-pen text-[9px]" aria-hidden="true"></i>
                                Düzenle
                            </a>
                            <form method="post" style="display:inline;"
                                  onsubmit="return confirm('Silinsin mi?')">
                                @Html.AntiForgeryToken()
                                <input type="hidden" name="id" value="@item.Id" />
                                <button type="submit" class="btn sm danger">
                                    <i class="fas fa-trash text-[9px]" aria-hidden="true"></i>
                                    Sil
                                </button>
                            </form>
                        </div>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

## §7 Form Section Card
```cshtml
<div style="background:var(--paper); border:1px solid var(--line); border-radius:10px;
            overflow:hidden; max-width:880px; margin-bottom:18px;">

    <div style="padding:14px 18px; border-bottom:1px solid var(--line);">
        <h2 style="font-size:13px; font-weight:600; color:var(--ink-1); margin:0;">
            <i class="fas fa-user-plus" style="margin-right:6px; color:var(--ink-3);" aria-hidden="true"></i>
            Bölüm Başlığı
        </h2>
        <div style="font-size:12px; color:var(--ink-3); margin-top:2px;">Alt açıklama</div>
    </div>

    <div style="padding:20px;">
        <div style="display:grid; grid-template-columns:1fr 1fr; gap:14px; margin-bottom:14px;">
            <div class="field" style="margin-bottom:0;">
                <label class="lab">Alan <span style="color:var(--accent);">*</span></label>
                <input type="text" name="Field" class="inp" />
            </div>
        </div>
    </div>
</div>
```

## §7 Form Action Row
```cshtml
<div style="display:flex; align-items:center; justify-content:space-between;
            max-width:880px; padding:14px 20px;
            background:var(--paper); border:1px solid var(--line); border-radius:10px;">
    <a href="/Back" class="btn ghost">
        <i class="fas fa-arrow-left text-[10px]" aria-hidden="true"></i>
        Geri Dön
    </a>
    <button type="submit" class="btn primary">
        <i class="fas fa-check text-[10px]" aria-hidden="true"></i>
        Kaydet
    </button>
</div>
```

Veya partial: `@await Html.PartialAsync("_FormActionRow", new FormActionRowViewModel { BackUrl = "...", SubmitText = "Kaydet" })`

## §8 Alert / Mesaj
```cshtml
@if (!string.IsNullOrEmpty(Model.Message))
{
    var t = Model.MessageType; // "success" | "error" | "warn"
    <div role="alert" style="margin-bottom:18px; padding:12px 16px; border-radius:10px; font-size:13px;
                background: @(t == "success" ? "rgba(16,185,129,0.10)"
                            : t == "warn"    ? "rgba(245,158,11,0.10)"
                                             : "rgba(220,38,38,0.10)");
                border: 1px solid @(t == "success" ? "rgba(16,185,129,0.25)"
                                  : t == "warn"    ? "rgba(245,158,11,0.30)"
                                                   : "rgba(220,38,38,0.25)");
                color: @(t == "success" ? "#065f46"
                       : t == "warn"    ? "#92400e"
                                        : "var(--accent-ink)");">
        <i class="fas @(t == "success" ? "fa-check-circle"
                      : t == "warn"    ? "fa-triangle-exclamation"
                                       : "fa-exclamation-circle")" style="margin-right:8px;" aria-hidden="true"></i>
        @Model.Message
    </div>
}
```

Veya partial: `@await Html.PartialAsync("_AlertMessage", ("success", "Kaydedildi"))`

## §9 Breadcrumb
```cshtml
@section Breadcrumb {
    <nav class="crumbs" aria-label="Sayfa konumu">
        <a href="/Dashboard">Ana Sayfa</a>
        <span class="sep" aria-hidden="true">/</span>
        <a href="/Section">Bölüm</a>
        <span class="sep" aria-hidden="true">/</span>
        <span class="current" aria-current="page">Mevcut</span>
    </nav>
}
```

## §9 Subnav
```cshtml
<nav class="subnav" aria-label="X alt-navigasyonu">
    <a href="/X/A" class="@NavActive("A")">
        <i class="fas fa-..."></i>
        <span>Etiket</span>
        <span class="count">@count</span>  @* opsiyonel *@
    </a>
</nav>
```

## §9 FooterHint
```cshtml
@await Html.PartialAsync("_FooterHint", (string?)$"{count} kayıt")
```

Cast `(string?)` zorunlu.

## §2 Top Actions

**A) Sade İptal:**
```cshtml
@section TopActions {
    <a href="/Back" class="btn">
        <i class="fas fa-xmark text-[10px]" aria-hidden="true"></i>
        İptal
    </a>
}
```

**B) Primary CTA + Yardım:**
```cshtml
@section TopActions {
    @if (isAdmin)
    {
        <a href="/Create" class="btn primary">
            <i class="fas fa-plus text-[10px]" aria-hidden="true"></i>
            Yeni X
        </a>
    }
    <a href="#" class="icon-btn" title="Yardım">
        <i class="fas fa-circle-question"></i>
    </a>
}
```

**C) Sadece Yardım:**
```cshtml
@section TopActions {
    <a href="#" class="icon-btn" title="Yardım">
        <i class="fas fa-circle-question"></i>
    </a>
}
```
