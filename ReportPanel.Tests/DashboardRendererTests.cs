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
        var comp = new DashboardComponent { Type = "kpi", Title = "<img src=x onerror=alert(1)>" };
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
        var comp = new DashboardComponent { Type = "kpi", Title = "t", Subtitle = "<script>x</script>" };
        var cfg = ConfigWithTab("Genel", comp);
        var html = DashboardRenderer.Render(cfg, EmptyRs());

        Assert.DoesNotContain("<script>x</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Render_component_icon_is_html_encoded()
    {
        // Icon is used inside a class attribute — breaking out via `'` must not succeed.
        var comp = new DashboardComponent { Type = "kpi", Title = "t", Icon = "' onclick='alert(1)" };
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
}
