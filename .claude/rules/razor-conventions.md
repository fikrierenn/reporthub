---
paths:
  - "ReportPanel/Views/**/*.cshtml"
---

# Razor View Konvansiyonları

## View Yapısı

1. **Model:** `@model ReportPanel.ViewModels.XyzViewModel` — Entity direkt kullanma.
2. **Layout:** `Layout = "_AppLayout";` (unutma).
3. **ViewData / ViewBag:** Minimum. Mümkünse ViewModel property'si kullan.

## Form Kuralları

- **Tek pattern:** `@using (Html.BeginForm("Action", "Controller", method, htmlAttrs)) { ... }`. Raw `<form>` kullanma (tutarsızlık — TODO).
- **Antiforgery:** `@Html.AntiForgeryToken()` her form'da. Tag helper kullanıyorsan otomatik gelir.
- **Validation:** `<div asp-validation-for="Field"></div>` veya manuel `@Html.ValidationMessageFor(m => m.Field)`.
- **Input:** `form-input-brand` custom class veya Tailwind utility. Karışık yazma, bir form = bir stil.

## Güvenlik

- **`@Html.Raw` minimum.** Admin-only view'de bile dikkat. Kullanman gerekiyorsa yorum bırak: `@* Raw HTML: user input değil, admin-controlled *@`.
- **Model binding:** User-input ViewModel'e bind oluyor. Kritik alanlar (`UserId`, `PasswordHash`) `[BindNever]`.
- **ReturnUrl:** `Url.IsLocalUrl(returnUrl)` **yeterli değil**. Ek: `returnUrl.StartsWith("/") && !returnUrl.StartsWith("//")`.

## Türkçe UI

- UTF-8, düzgün karakterler. Detay: `.claude/rules/turkish-ui.md`.
- `<html lang="tr">` `_AppLayout.cshtml`'de.

## CSS

- **Tailwind utility** birincil.
- **Custom class** (`btn-brand`, `btn-brand-outline`, `form-input-brand`) — brand renkler için.
- İkisini bir element'te karıştırma: ya utility ya custom.

## Icon

- **Font Awesome 6** (CDN). `fas fa-*` pattern.
- `fas fa-pen` (edit), `fas fa-trash` (sil), `fas fa-save` (kaydet), `fas fa-plus` (ekle), `fas fa-chart-bar` (grafik).

## Inline JS

- **Minimum.** Kısa handler OK (`onclick="toggleX()"`) ama büyük logic `wwwroot/assets/js/`'e taşı.
- IIFE wrap, global namespace'i koru.
- **CSP uyumluluk:** gelecekte CSP gelirse inline script kırılır — yavaş yavaş dışarı çek.

## Partial View

- Tekrar eden UI parçası → `Views/Shared/_PartialName.cshtml`.
- `@Html.Partial("_PartialName", model)` veya `@await Html.PartialAsync(...)`.

## Hata / Mesaj Gösterimi

```cshtml
@if (!string.IsNullOrWhiteSpace(Model.Message))
{
    <div class="@(Model.MessageType == "success" ? "bg-green-100 border-green-400 text-green-700" : "bg-red-100 border-red-400 text-red-700") px-4 py-3 rounded border">
        @Model.Message
    </div>
}
```

## Dashboard Iframe

- `Views/Reports/Run.cshtml:264` → `<iframe sandbox="allow-scripts">`.
- **`allow-same-origin` ekleme** — XSS izolasyonu kalkar.
- `srcdoc` ile render, `src` ile dış URL yükleme.
