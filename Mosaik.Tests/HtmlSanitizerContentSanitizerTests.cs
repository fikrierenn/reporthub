using Mosaik.Core.Html;
using Mosaik.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 39 Faz B — render-time XSS koruma sözleşme testleri.
    public class HtmlSanitizerContentSanitizerTests
    {
        private readonly IContentSanitizer _sut = new HtmlSanitizerContentSanitizer();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Empty_Or_Null_Returns_Empty(string? input)
        {
            Assert.Equal(string.Empty, _sut.Sanitize(input));
        }

        [Fact]
        public void Safe_Quill_Output_Passes_Through()
        {
            const string input = "<p><strong>Önemli</strong> <em>not</em></p><ul><li>madde</li></ul>";
            var output = _sut.Sanitize(input);
            Assert.Contains("<strong>Önemli</strong>", output);
            Assert.Contains("<ul><li>madde</li></ul>", output);
        }

        [Fact]
        public void Script_Tag_Stripped()
        {
            const string input = "<p>merhaba</p><script>alert(1)</script>";
            var output = _sut.Sanitize(input);
            Assert.DoesNotContain("<script>", output);
            Assert.DoesNotContain("alert(1)", output);
            Assert.Contains("merhaba", output);
        }

        [Fact]
        public void OnError_Attribute_Stripped()
        {
            const string input = "<img src=\"x\" onerror=\"alert(1)\" />";
            var output = _sut.Sanitize(input);
            Assert.DoesNotContain("onerror", output);
        }

        [Fact]
        public void Javascript_Scheme_Blocked()
        {
            const string input = "<a href=\"javascript:alert(1)\">tıkla</a>";
            var output = _sut.Sanitize(input);
            Assert.DoesNotContain("javascript:", output);
        }

        [Fact]
        public void Data_Uri_Image_Blocked()
        {
            const string input = "<img src=\"data:image/svg+xml,<svg/onload=alert(1)>\" />";
            var output = _sut.Sanitize(input);
            Assert.DoesNotContain("data:", output);
        }

        [Fact]
        public void Mailto_Scheme_Allowed()
        {
            const string input = "<a href=\"mailto:test@example.com\">mail</a>";
            var output = _sut.Sanitize(input);
            Assert.Contains("mailto:test@example.com", output);
        }

        [Fact]
        public void Quill_Color_Style_Allowed()
        {
            const string input = "<span style=\"color: #ff0000;\">kırmızı</span>";
            var output = _sut.Sanitize(input);
            Assert.Contains("color", output);
            Assert.Contains("kırmızı", output);
        }

        [Fact]
        public void Idempotent_Double_Sanitize_No_Change()
        {
            const string input = "<p><strong>x</strong></p>";
            var once = _sut.Sanitize(input);
            var twice = _sut.Sanitize(once);
            Assert.Equal(once, twice);
        }
    }
}
