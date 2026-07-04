using Mosaik.Modules.Forms.Entities;
using Mosaik.Modules.Forms.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 56 M-A G3 — koşullu görünürlük (conditional-logic) saf değerlendirici.
    public class FormConditionEvaluatorTests
    {
        private static FormField FieldWith(string? conditionalLogic) => new()
        {
            FieldKey = "ad_soyad", Label = "Ad Soyad", FieldType = FormFieldType.Text,
            ConditionalLogic = conditionalLogic
        };

        [Fact]
        public void TryParse_ValidEq_ReturnsCondition()
        {
            var c = FormConditionEvaluator.TryParse("{\"field\":\"anonim\",\"op\":\"eq\",\"value\":\"Hayır\"}");
            Assert.NotNull(c);
            Assert.Equal("anonim", c!.Field);
            Assert.Equal("eq", c.Op);
            Assert.Equal("Hayır", c.Value);
        }

        [Fact]
        public void TryParse_EmptyOrWhitespace_ReturnsNull()
        {
            Assert.Null(FormConditionEvaluator.TryParse(null));
            Assert.Null(FormConditionEvaluator.TryParse("   "));
        }

        [Fact]
        public void TryParse_MalformedJson_ReturnsNull()
        {
            Assert.Null(FormConditionEvaluator.TryParse("{not valid"));
        }

        [Fact]
        public void TryParse_InvalidOp_ReturnsNull()
        {
            Assert.Null(FormConditionEvaluator.TryParse("{\"field\":\"a\",\"op\":\"gt\",\"value\":\"1\"}"));
        }

        [Fact]
        public void TryParse_MissingFieldOrValue_ReturnsNull()
        {
            Assert.Null(FormConditionEvaluator.TryParse("{\"op\":\"eq\",\"value\":\"x\"}"));
            Assert.Null(FormConditionEvaluator.TryParse("{\"field\":\"a\",\"op\":\"eq\"}"));
        }

        [Fact]
        public void IsVisible_NoCondition_ReturnsTrue()
        {
            var values = new Dictionary<string, string?>();
            Assert.True(FormConditionEvaluator.IsVisible(FieldWith(null), values));
        }

        [Fact]
        public void IsVisible_EqMatch_ReturnsTrue()
        {
            var values = new Dictionary<string, string?> { ["anonim"] = "Hayır" };
            var field = FieldWith("{\"field\":\"anonim\",\"op\":\"eq\",\"value\":\"Hayır\"}");
            Assert.True(FormConditionEvaluator.IsVisible(field, values));
        }

        [Fact]
        public void IsVisible_EqNoMatch_ReturnsFalse()
        {
            var values = new Dictionary<string, string?> { ["anonim"] = "Evet" };
            var field = FieldWith("{\"field\":\"anonim\",\"op\":\"eq\",\"value\":\"Hayır\"}");
            Assert.False(FormConditionEvaluator.IsVisible(field, values));
        }

        [Fact]
        public void IsVisible_MissingReferenceValue_EqTreatedAsEmpty_ReturnsFalse()
        {
            var values = new Dictionary<string, string?>(); // referans alan submit edilmemiş
            var field = FieldWith("{\"field\":\"anonim\",\"op\":\"eq\",\"value\":\"Hayır\"}");
            Assert.False(FormConditionEvaluator.IsVisible(field, values));
        }

        [Fact]
        public void IsVisible_Ne_InvertsMatch()
        {
            var field = FieldWith("{\"field\":\"anonim\",\"op\":\"ne\",\"value\":\"Evet\"}");
            Assert.True(FormConditionEvaluator.IsVisible(field, new Dictionary<string, string?> { ["anonim"] = "Hayır" }));
            Assert.False(FormConditionEvaluator.IsVisible(field, new Dictionary<string, string?> { ["anonim"] = "Evet" }));
        }

        [Fact]
        public void IsVisible_BrokenCondition_FailClosed_ReturnsTrue()
        {
            // Bozuk koşul → görünür varsayılır (required zorlanır, sessiz bypass yok).
            var field = FieldWith("{bozuk json");
            Assert.True(FormConditionEvaluator.IsVisible(field, new Dictionary<string, string?>()));
        }

        [Fact]
        public void BuildVisibleIf_Eq_BuildsSurveyExpression()
        {
            var expr = FormConditionEvaluator.BuildVisibleIf("{\"field\":\"anonim\",\"op\":\"eq\",\"value\":\"Hayır\"}");
            Assert.Equal("{anonim} = \"Hayır\"", expr);
        }

        [Fact]
        public void BuildVisibleIf_Ne_UsesNotEqualsOperator()
        {
            var expr = FormConditionEvaluator.BuildVisibleIf("{\"field\":\"anonim\",\"op\":\"ne\",\"value\":\"Evet\"}");
            Assert.Equal("{anonim} <> \"Evet\"", expr);
        }

        [Fact]
        public void BuildVisibleIf_ValueWithQuote_IsEscaped_NoExpressionBreakout()
        {
            // Injection denemesi: value içinde çift-tırnak → JsonSerializer literal kaçışı, breakout yok.
            var expr = FormConditionEvaluator.BuildVisibleIf("{\"field\":\"x\",\"op\":\"eq\",\"value\":\"a\\\" or {y}=\\\"b\"}");
            Assert.StartsWith("{x} = ", expr);
            Assert.Contains("\\\"", expr); // kaçırılmış tırnak
        }

        [Fact]
        public void BuildVisibleIf_NoCondition_ReturnsNull()
        {
            Assert.Null(FormConditionEvaluator.BuildVisibleIf(null));
        }
    }
}
