using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34 §10 madde 6 — TinyMCE / Quill HTML save-time sanitize.
// HtmlSanitizer (Ganss.Xss) wrapper: XSS koruması + allowed tag/attribute/CSS subset.
public class SopContentSanitizerTests
{
    [Fact]
    public void Sanitize_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SopContentSanitizer.Sanitize(null));
        Assert.Equal(string.Empty, SopContentSanitizer.Sanitize(""));
        Assert.Equal(string.Empty, SopContentSanitizer.Sanitize("   "));
    }

    [Fact]
    public void Sanitize_ScriptTag_Stripped()
    {
        var input = "<p>Prosedür</p><script>alert('xss')</script>";
        var output = SopContentSanitizer.Sanitize(input);

        Assert.DoesNotContain("<script", output);
        Assert.DoesNotContain("alert", output);
        Assert.Contains("Prosedür", output);
    }

    [Fact]
    public void Sanitize_OnClickHandler_Stripped()
    {
        var input = "<a href=\"https://example.com\" onclick=\"alert(1)\">Link</a>";
        var output = SopContentSanitizer.Sanitize(input);

        Assert.DoesNotContain("onclick", output);
        Assert.Contains("href", output);
    }

    [Fact]
    public void Sanitize_JavascriptScheme_Stripped()
    {
        var input = "<a href=\"javascript:alert(1)\">x</a>";
        var output = SopContentSanitizer.Sanitize(input);

        Assert.DoesNotContain("javascript:", output);
    }

    [Fact]
    public void Sanitize_AllowedTags_Preserved()
    {
        var input = "<h2>Başlık</h2><p>Metin <strong>kalın</strong> <em>italik</em></p>" +
                    "<ul><li>madde 1</li></ul>" +
                    "<table><thead><tr><th>K1</th></tr></thead><tbody><tr><td>V1</td></tr></tbody></table>";
        var output = SopContentSanitizer.Sanitize(input);

        Assert.Contains("<h2>", output);
        Assert.Contains("<strong>", output);
        Assert.Contains("<table>", output);
        Assert.Contains("<td>", output);
    }

    [Fact]
    public void Sanitize_ImageWithDataScheme_Allowed()
    {
        // TinyMCE paste base64 inline image — data scheme allowed.
        var input = "<img src=\"data:image/png;base64,iVBORw0KGgo=\" alt=\"test\" />";
        var output = SopContentSanitizer.Sanitize(input);

        Assert.Contains("<img", output);
        Assert.Contains("data:", output);
    }

    [Fact]
    public void Sanitize_StyleAttribute_AllowedColors()
    {
        var input = "<p style=\"color:red; background-color:yellow;\">x</p>";
        var output = SopContentSanitizer.Sanitize(input);

        Assert.Contains("color", output);
    }

    [Fact]
    public void Sanitize_StyleAttribute_DangerousPropsStripped()
    {
        var input = "<p style=\"position:absolute; expression(alert(1));\">x</p>";
        var output = SopContentSanitizer.Sanitize(input);

        Assert.DoesNotContain("expression", output);
        Assert.DoesNotContain("position:absolute", output);
    }

    [Fact]
    public void ExtractPlainText_StripsTags()
    {
        var input = "<p>Birinci paragraf.</p><p>İkinci paragraf <strong>vurgu</strong>.</p>";
        var output = SopContentSanitizer.ExtractPlainText(input);

        Assert.DoesNotContain("<p>", output);
        Assert.DoesNotContain("<strong>", output);
        Assert.Contains("Birinci paragraf", output);
        Assert.Contains("vurgu", output);
    }

    [Fact]
    public void ExtractPlainText_DecodesHtmlEntities()
    {
        var input = "<p>Şirket &amp; Personel — &quot;test&quot;</p>";
        var output = SopContentSanitizer.ExtractPlainText(input);

        Assert.Contains("&", output);
        Assert.Contains("\"test\"", output);
        Assert.DoesNotContain("&amp;", output);
    }

    [Fact]
    public void ExtractPlainText_RespectsMaxChars()
    {
        var input = "<p>" + new string('x', 50_000) + "</p>";
        var output = SopContentSanitizer.ExtractPlainText(input, maxChars: 100);

        Assert.Equal(100, output.Length);
    }

    [Fact]
    public void ExtractPlainText_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SopContentSanitizer.ExtractPlainText(null));
        Assert.Equal(string.Empty, SopContentSanitizer.ExtractPlainText(""));
    }
}
