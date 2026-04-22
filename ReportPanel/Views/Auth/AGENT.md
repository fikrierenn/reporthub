> ⚠️ **DEPRECATED** — Bu dosya projenin erken fazındaki varsayımlara göre yazılmış ve artık geçerli değil.
> Yanıltıcı iddialar: "MVC yok", "API only" — proje **MVC + EF Core 10**. Güncel
> kılavuz: [`/CLAUDE.md`](/CLAUDE.md) + [`/.claude/rules/`](/.claude/rules/).
> Bu dosya referans/tarih amaçlı tutuluyor, **uygulama kuralı olarak okunmasın**.

---

ROLE: Senior Full-Stack Developer & Architect (15+ yıl) — .NET 10 default, API only if needed, MVC yok, direkt feature-based transaction script.
- SQL bilgin Senior seviyesinde (performans, optimizasyon, indeksleme, query tuning, execution plan analizi, kompleks sorgular, stored procedure, trigger, view, function, transaction yönetimi, vs.)
- Mümkün olduğunda .net core kullan js tarafında ise mümkün olduğunca minimal, lightweight, frameworksüz vanilla js tercih ederim. Gerektiğinde React veya Vue kullanırım.
- CSS tarafında TailwindCSS tercih ederim. Mümkün olduğunda özel css yazmam. Özel yazdığım css ler de mümkün olduğunca minimal olur ve harici css dosyasında olur. Sayfaların içinde olmaz.
- SQL taboları için mümkün olduğunca singular isimlendirme tercih ederim. (ör: Customer, Order, Product, vs.) Plural isimlendirme tercih etmem.
- SQL taboları için mümkün olduğunca Türkçe karakter kullanırım. (ör: Müşteri, Sipariş, Ürün, vs.) İngilizce karakter tercih etmem. Ama isimlendirme için ingilizce karaketler 
ile yazılır.
- Kodlama standartlarım genellikle Microsoft'un resmi C# kodlama standartlarına yakındır. (ör: PascalCase, camelCase, vs.)


.NET 8 (ASP.NET Core)

UI: Razor Pages (API’siz, direkt server-side)

DB: SQL Server (Dapper default; EF Core opsiyon)

Mimari: Minimal Hosting + Feature-based (MVC yok, Controller yok)

“API yok” kuralının .NET karşılığı

Aynı uygulama içindeysek: Razor Pages + PageModel OnPost/OnGet → direkt DB

API sadece dış client/entegrasyon varsa: Minimal API (Controller değil)

Proje iskeleti (feature-based, hızlı)
src/
  App/
    Program.cs
    appsettings.json
    Features/
      Orders/
        Orders.sql.cs        // SQL string’leri
        Orders.repo.cs       // Dapper sorguları
        Create.cshtml
        Create.cshtml.cs     // PageModel: OnPost direkt iş akışı
    Lib/
      Db.cs                  // SqlConnection factory
      Guard.cs               // validation helpers
      Auth.cs                // (varsa) auth helper

Kod: Program.cs (Razor Pages + Dapper, MVC yok)
using Microsoft.Data.SqlClient;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// UI (server-side) — API’siz ilerle
builder.Services.AddRazorPages();

// DB — Dapper için basit bağlantı factory
builder.Services.AddScoped<IDbConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Auth varsa açarsın (yoksa dokunma)
// app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Dış dünya gerekiyorsa (opsiyonel) Minimal API burada açılır, Controller yok.
// app.MapPost("/api/...", ...);

app.Run();

Kod: Lib/Db.cs (istersen factory’yi buraya al)
using Microsoft.Data.SqlClient;
using System.Data;

namespace App.Lib;

public static class Db
{
    public static IDbConnection Open(string connString)
    {
        var c = new SqlConnection(connString);
        c.Open();
        return c;
    }
}

Örnek Feature: Orders (Dapper ile “direkt iş”)
Features/Orders/Orders.sql.cs
namespace App.Features.Orders;

public static class OrdersSql
{
    public const string Insert = @"
INSERT INTO dbo.Orders(CustomerName, Total, CreatedAt)
OUTPUT INSERTED.Id
VALUES (@CustomerName, @Total, SYSUTCDATETIME());";
}

Features/Orders/Orders.repo.cs
using Dapper;
using System.Data;

namespace App.Features.Orders;

public static class OrdersRepo
{
    public static Task<int> CreateAsync(IDbConnection db, string customerName, decimal total)
        => db.ExecuteScalarAsync<int>(OrdersSql.Insert, new { CustomerName = customerName, Total = total });
}

Features/Orders/Create.cshtml
@page
@model App.Features.Orders.CreateModel

<form method="post" class="space-y-3">
  <div>
    <label>Customer</label>
    <input asp-for="CustomerName" />
  </div>
  <div>
    <label>Total</label>
    <input asp-for="Total" />
  </div>
  <button type="submit">Create</button>
</form>

Features/Orders/Create.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace App.Features.Orders;

public class CreateModel(IDbConnection db) : PageModel
{
    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public decimal Total { get; set; }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrWhiteSpace(CustomerName)) return BadRequest("CustomerName boş");
        if (Total <= 0) return BadRequest("Total > 0 olmalı");

        var id = await OrdersRepo.CreateAsync(db, CustomerName.Trim(), Total);

        // API yok: direkt redirect
        return Redirect($"/Orders/Details?id={id}");
    }
}