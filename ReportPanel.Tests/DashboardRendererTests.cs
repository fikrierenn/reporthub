using System.Collections.Generic;
using ReportPanel.Models;
using ReportPanel.Services;

namespace ReportPanel.Tests;

/// <summary>
/// XSS regression coverage for DashboardRenderer. Dashboards render inside a
/// sandboxed iframe (allow-scripts, no allow-same-origin) — so top-level XSS is
/// contained — but any payload that escapes the JSON data island or HTML attribute
/// escaping would still defile the iframe's own execution context. These tests
/// lock the escaping behaviour so future edits don't regress.
/// </summary>
public class DashboardRendererTests
{
    private static DashboardConfig ConfigWithTab(string tabTitle, DashboardComponent? comp = null)
    {
        var tab = new DashboardTab { Title = tabTitle };
        if (comp != null) tab.Components.Add(comp);
        return new DashboardConfig { Tabs = new List<DashboardTab> { tab, new DashboardTab { Title = "Other" } } };
    }

    private static List<List<Dictionary<string, object>>> EmptyRs() => new() { new() };

    // ---- HTML encoding on user-controlled titles ----

    [Fact]
    public void Render_tab_title_is_html_encoded()
    {
        var cfg = ConfigWithTab("<script>alert('xss')</script>");
        var html = DashboardRenderer.Render(cfg, EmptyRs());

        Assert.DoesNotContain("<script>alert('xss')</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Render_component_title_is_html_encoded()
    {
        var comp = new DashboardComponent { Type = "kpi", Title = "<img src=x onerror=alert(1)>", ResultSet = 0 };
        var cfg = ConfigWithTab("Genel", comp);
        var html = DashboardRenderer.Render(cfg, EmptyRs());

        // Raw HTML ogesi cikmasin (HtmlEncode `<` -> `&lt;` donusturur; substring `onerror=alert(1)`
        // encoded context icinde hala kalir ama anlamli DOM olusturamaz).
        Assert.DoesNotContain("<img src=x onerror=alert(1)>", html);
        Assert.Contains("&lt;img", html);
    }

    [Fact]
    public void Render_component_subtitle_is_html_encoded()
    {
        var comp = new DashboardComponent { Type = "kpi", Title = "t", Subtitle = "<script>x</script>", ResultSet = 0 };
        var cfg = ConfigWithTab("Genel", comp);
        var html = DashboardRenderer.Render(cfg, EmptyRs());

        Assert.DoesNotContain("<script>x</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Render_component_icon_is_html_encoded()
    {
        // Icon is used inside a class attribute — breaking out via `'` must not succeed.
        var comp = new DashboardComponent { Type = "kpi", Title = "t", Icon = "' onclick='alert(1)", ResultSet = 0 };
        var cfg = ConfigWithTab("Genel", comp);
        var html = DashboardRenderer.Render(cfg, EmptyRs());

        Assert.DoesNotContain("onclick='alert(1)", html);
    }

    // ---- Data island (window.__RS) break-out guards ----

    // JSON serializer `<` karakterini zaten `\u003c`'a kacirir; regex post-processing
    // ek bir guvenlik katmani. Test'ler ham `</script` ve `<!--` substring'lerinin
    // cikmamasini dogrular (escape mekanizmasinin kendisini degil).

    [Fact]
    public void Render_data_island_does_not_leak_script_close_tag()
    {
        var rs = new List<List<Dictionary<string, object>>>
        {
            new() { new Dictionary<string, object> { ["name"] = "</script><script>alert(1)</script>" } }
        };
        var html = DashboardRenderer.Render(ConfigWithTab("Genel"), rs);

        Assert.DoesNotContain("</script><script>alert(1)", html);
        Assert.DoesNotContain("</script>alert(1)", html);
    }

    [Fact]
    public void Render_data_island_does_not_leak_uppercase_script_close_tag()
    {
        var rs = new List<List<Dictionary<string, object>>>
        {
            new() { new Dictionary<string, object> { ["name"] = "abc</SCRIPT>def" } }
        };
        var html = DashboardRenderer.Render(ConfigWithTab("Genel"), rs);

        Assert.DoesNotContain("abc</SCRIPT>def", html);
    }

    [Fact]
    public void Render_data_island_does_not_leak_mixed_case_script_close_tag()
    {
        var rs = new List<List<Dictionary<string, object>>>
        {
            new() { new Dictionary<string, object> { ["name"] = "abc</ScRiPt>def" } }
        };
        var html = DashboardRenderer.Render(ConfigWithTab("Genel"), rs);

        Assert.DoesNotContain("abc</ScRiPt>def", html);
    }

    [Fact]
    public void Render_data_island_does_not_leak_html_comment_open()
    {
        var rs = new List<List<Dictionary<string, object>>>
        {
            new() { new Dictionary<string, object> { ["name"] = "abc<!-- injected xyz" } }
        };
        var html = DashboardRenderer.Render(ConfigWithTab("Genel"), rs);

        Assert.DoesNotContain("abc<!-- injected xyz", html);
    }

    // ---- Structural guard: no eval, no raw innerHTML concat ----

    [Fact]
    public void Render_does_not_emit_eval()
    {
        var html = DashboardRenderer.Render(ConfigWithTab("x"), EmptyRs());
        Assert.DoesNotContain("eval(", html);
    }

    [Fact]
    public void Render_sets_window_dunder_rs_data_island()
    {
        var html = DashboardRenderer.Render(ConfigWithTab("x"), EmptyRs());
        Assert.Contains("window.__RS = [", html);
    }

    // ---- ADR-007 resolver ----

    [Fact]
    public void ResolveResultSet_prefers_name_over_legacy_index()
    {
        var cfg = new DashboardConfig
        {
            ResultContract = new()
            {
                ["chart"] = new() { ResultSet = 2 }
            }
        };
        var comp = new DashboardComponent { Result = "chart", ResultSet = 999 };
        Assert.Equal(2, cfg.ResolveResultSet(comp, resultSetCount: 3));
    }

    [Fact]
    public void ResolveResultSet_returns_null_for_unknown_name()
    {
        var cfg = new DashboardConfig { ResultContract = new() };
        var comp = new DashboardComponent { Result = "ghost" };
        Assert.Null(cfg.ResolveResultSet(comp, resultSetCount: 3));
    }

    [Fact]
    public void ResolveResultSet_returns_null_for_out_of_bounds_contract_index()
    {
        var cfg = new DashboardConfig
        {
            ResultContract = new()
            {
                ["chart"] = new() { ResultSet = 5 }
            }
        };
        var comp = new DashboardComponent { Result = "chart" };
        Assert.Null(cfg.ResolveResultSet(comp, resultSetCount: 3));
    }

    [Fact]
    public void ResolveResultSet_falls_back_to_legacy_index_when_result_not_set()
    {
        var cfg = new DashboardConfig();
        var comp = new DashboardComponent { ResultSet = 1 };
        Assert.Equal(1, cfg.ResolveResultSet(comp, resultSetCount: 3));
    }

    [Fact]
    public void ResolveResultSet_returns_null_for_out_of_bounds_legacy_index()
    {
        var cfg = new DashboardConfig();
        var comp = new DashboardComponent { ResultSet = 10 };
        Assert.Null(cfg.ResolveResultSet(comp, resultSetCount: 3));
    }

    [Fact]
    public void ResolveResultSet_returns_null_when_no_binding_at_all()
    {
        var cfg = new DashboardConfig();
        var comp = new DashboardComponent { Type = "kpi" };
        Assert.Null(cfg.ResolveResultSet(comp, resultSetCount: 3));
    }

    [Fact]
    public void Render_unknown_binding_emits_missing_placeholder_not_widget()
    {
        var comp = new DashboardComponent { Type = "kpi", Title = "Toplam", Result = "nonexistent" };
        var cfg = new DashboardConfig
        {
            ResultContract = new(),
            Tabs = new() { new DashboardTab { Title = "T", Components = { comp } } }
        };
        var html = DashboardRenderer.Render(cfg, EmptyRs());

        Assert.Contains("Veri bağlantısı çözümlenemedi", html);
        Assert.Contains("nonexistent", html); // debug: binding info visible
    }

    [Fact]
    public void Render_unknown_widget_type_emits_removed_placeholder()
    {
        var comp = new DashboardComponent { Type = "futureWidget", Id = "w_future_abc123", ResultSet = 0 };
        var cfg = new DashboardConfig
        {
            Tabs = new() { new DashboardTab { Title = "T", Components = { comp } } }
        };
        var html = DashboardRenderer.Render(cfg, EmptyRs());

        Assert.Contains("Bilinmeyen bileşen tipi", html);
        Assert.Contains("futureWidget", html);
        Assert.Contains("w_future_abc123", html);
    }

    [Fact]
    public void Render_emits_required_missing_banner_when_required_result_empty()
    {
        var cfg = new DashboardConfig
        {
            ResultContract = new()
            {
                ["summary"] = new() { ResultSet = 0, Required = true }
            },
            Tabs = new() { new DashboardTab { Title = "T" } }
        };
        var html = DashboardRenderer.Render(cfg, EmptyRs()); // EmptyRs: rs[0] = 0 rows

        Assert.Contains("Eksik zorunlu veri", html);
        Assert.Contains("summary", html);
    }

    // ============================================================
    // Plan 05 Faz 2 — CalculatedFields render-time enrichment
    // ============================================================

    [Fact]
    public void CalculatedFields_enrich_rows_with_arithmetic_result()
    {
        var cfg = new DashboardConfig
        {
            Tabs = new() { new DashboardTab { Title = "T" } },
            CalculatedFields = new()
            {
                new CalculatedField { Name = "kar", Formula = "satis - maliyet" }
            }
        };
        var rs = new List<List<Dictionary<string, object>>>
        {
            new()
            {
                new Dictionary<string, object> { ["satis"] = 150m, ["maliyet"] = 100m },
                new Dictionary<string, object> { ["satis"] = 80m,  ["maliyet"] = 30m }
            }
        };

        DashboardRenderer.Render(cfg, rs);

        Assert.Equal(50m, rs[0][0]["kar"]);
        Assert.Equal(50m, rs[0][1]["kar"]);
    }

    [Fact]
    public void CalculatedFields_iif_label_added_to_each_row()
    {
        var cfg = new DashboardConfig
        {
            Tabs = new() { new DashboardTab { Title = "T" } },
            CalculatedFields = new()
            {
                new CalculatedField { Name = "kategori", Formula = "IIF(adet > 100, 'Buyuk', 'Kucuk')" }
            }
        };
        var rs = new List<List<Dictionary<string, object>>>
        {
            new()
            {
                new Dictionary<string, object> { ["adet"] = 250m },
                new Dictionary<string, object> { ["adet"] = 50m }
            }
        };

        DashboardRenderer.Render(cfg, rs);

        Assert.Equal("Buyuk", rs[0][0]["kategori"]);
        Assert.Equal("Kucuk", rs[0][1]["kategori"]);
    }

    [Fact]
    public void CalculatedFields_unknown_column_yields_dbnull_not_throw()
    {
        // Plan 05: satır-bazlı eval hatası dashboard'u çöktürmez, cell DBNull.
        var cfg = new DashboardConfig
        {
            Tabs = new() { new DashboardTab { Title = "T" } },
            CalculatedFields = new()
            {
                new CalculatedField { Name = "x", Formula = "yokKolon * 2" }
            }
        };
        var rs = new List<List<Dictionary<string, object>>>
        {
            new() { new Dictionary<string, object> { ["adet"] = 5m } }
        };

        var ex = Record.Exception(() => DashboardRenderer.Render(cfg, rs));

        Assert.Null(ex);
        Assert.Equal(System.DBNull.Value, rs[0][0]["x"]);
    }

    [Fact]
    public void CalculatedFields_scope_limits_target_resultset()
    {
        // ResultScope = "ozet" → sadece resultContract'a göre RS 0'a uygulanır, RS 1 dokunmaz.
        var cfg = new DashboardConfig
        {
            ResultContract = new()
            {
                ["ozet"] = new() { ResultSet = 0 },
                ["detay"] = new() { ResultSet = 1 }
            },
            Tabs = new() { new DashboardTab { Title = "T" } },
            CalculatedFields = new()
            {
                new CalculatedField { Name = "etiket", Formula = "'X'", ResultScope = "ozet" }
            }
        };
        var rs = new List<List<Dictionary<string, object>>>
        {
            new() { new Dictionary<string, object> { ["a"] = 1m } },
            new() { new Dictionary<string, object> { ["a"] = 2m } }
        };

        DashboardRenderer.Render(cfg, rs);

        Assert.Equal("X", rs[0][0]["etiket"]);
        Assert.False(rs[1][0].ContainsKey("etiket"));
    }
}
