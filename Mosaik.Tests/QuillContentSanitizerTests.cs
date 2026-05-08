using Mosaik.Modules.Circular.Services;

namespace Mosaik.Tests;

public class QuillContentSanitizerTests
{
    [Fact]
    public void RemovesScriptTag()
    {
        var input = "<p>ok</p><script>alert('x')</script>";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.DoesNotContain("<script", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>ok</p>", output);
    }

    [Fact]
    public void StripsOnEventAttributes()
    {
        var input = "<img src=\"x\" onerror=\"alert(1)\" alt=\"ok\">";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.DoesNotContain("onerror", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alt=\"ok\"", output);
    }

    [Fact]
    public void StripsOnclickHandler()
    {
        var input = "<p onclick=\"alert(1)\">text</p>";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.DoesNotContain("onclick", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text", output);
    }

    [Fact]
    public void RejectsJavascriptHrefScheme()
    {
        var input = "<a href=\"javascript:alert(1)\">click</a>";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.DoesNotContain("javascript:", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllowsHttpsHref()
    {
        var input = "<a href=\"https://example.com\">safe</a>";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.Contains("https://example.com", output);
        Assert.Contains("safe", output);
    }

    [Fact]
    public void RemovesIframe()
    {
        var input = "<iframe src=\"https://evil.com\"></iframe><p>ok</p>";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.DoesNotContain("<iframe", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>ok</p>", output);
    }

    [Fact]
    public void RemovesStyleTag()
    {
        var input = "<style>body{display:none}</style><p>ok</p>";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.DoesNotContain("<style", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("display:none", output);
    }

    [Fact]
    public void RemovesSvgWithOnload()
    {
        var input = "<svg onload=\"alert(1)\"></svg><p>ok</p>";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.DoesNotContain("<svg", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PreservesQuillFormatting()
    {
        var input = "<p><strong>bold</strong> <em>italic</em> <u>underline</u></p>" +
                    "<ol><li>first</li><li>second</li></ol>" +
                    "<blockquote>quote</blockquote>";
        var output = QuillContentSanitizer.Sanitize(input);
        Assert.Contains("<strong>", output);
        Assert.Contains("<em>", output);
        Assert.Contains("<u>", output);
        Assert.Contains("<ol>", output);
        Assert.Contains("<blockquote>", output);
    }

    [Fact]
    public void EmptyInputReturnsEmpty()
    {
        Assert.Equal(string.Empty, QuillContentSanitizer.Sanitize(null));
        Assert.Equal(string.Empty, QuillContentSanitizer.Sanitize(""));
        Assert.Equal(string.Empty, QuillContentSanitizer.Sanitize("   "));
    }
}
