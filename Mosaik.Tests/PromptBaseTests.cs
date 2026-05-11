using Mosaik.Core.Ai.Prompts;

namespace Mosaik.Tests;

public class PromptBaseTests
{
    [Fact]
    public void BuildSystemPrompt_IncludesRoleAndDomain()
    {
        var result = PromptBase.BuildSystemPrompt(
            roleDescription: "Sen bir uzman analistsin.",
            domainInstructions: "Sözleşme verilerini analiz et.",
            includeOcrTolerance: false,
            includeDateFormat: false);

        Assert.Contains("Sen bir uzman analistsin.", result);
        Assert.Contains("Sözleşme verilerini analiz et.", result);
        Assert.Contains(PromptBase.JsonOutputDirective, result);
    }

    [Fact]
    public void BuildSystemPrompt_IncludesOcrDirectiveByDefault()
    {
        var result = PromptBase.BuildSystemPrompt("role", "instructions");

        Assert.Contains(PromptBase.OcrToleranceDirective, result);
        Assert.Contains(PromptBase.DateFormatDirective, result);
    }

    [Fact]
    public void BuildSystemPrompt_OmitsOcrWhenDisabled()
    {
        var result = PromptBase.BuildSystemPrompt("role", "instructions", includeOcrTolerance: false);

        Assert.DoesNotContain("OCR METNİ KURALLARI", result);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("test", 1)]
    [InlineData("1234", 1)]
    [InlineData("12345678", 2)]
    public void EstimateTokens_ReturnsApproximation(string text, int expected)
    {
        Assert.Equal(expected, PromptBase.EstimateTokens(text));
    }

    [Fact]
    public void TruncateToTokenLimit_ShortTextUnchanged()
    {
        var text = "kısa metin";
        var result = PromptBase.TruncateToTokenLimit(text, 100);
        Assert.Equal(text, result);
    }

    [Fact]
    public void TruncateToTokenLimit_LongTextTruncated()
    {
        var text = new string('a', 1000);
        var result = PromptBase.TruncateToTokenLimit(text, 10); // 10 token = 40 chars max
        Assert.True(result.Length <= 40);
    }

    [Fact]
    public void TruncateToTokenLimit_PrefersSentenceBoundary()
    {
        // Build a text where a sentence boundary exists near the truncation point
        var sentence = "Bu bir cümledir. ";
        var text = string.Concat(Enumerable.Repeat(sentence, 100)); // ~1700 chars
        var result = PromptBase.TruncateToTokenLimit(text, 100); // 100 tokens = 400 chars max

        Assert.True(result.EndsWith('.'), $"Expected sentence end, got: '{result[^Math.Min(10, result.Length)..]}'");
    }

    [Fact]
    public void TruncateToTokenLimit_EmptyStringReturnsEmpty()
    {
        Assert.Equal("", PromptBase.TruncateToTokenLimit("", 100));
    }
}
