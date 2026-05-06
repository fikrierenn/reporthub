using System.Collections.Generic;
using ReportPanel.Services.Eval;

namespace ReportPanel.Tests;

public class FormulaEvaluatorTests
{
    private static IDictionary<string, object?> Row(params (string k, object? v)[] kvs)
    {
        var d = new Dictionary<string, object?>(System.StringComparer.Ordinal);
        foreach (var (k, v) in kvs) d[k] = v;
        return d;
    }

    private static object? Eval(string formula, IDictionary<string, object?>? row = null)
    {
        var ev = FormulaEvaluator.FromSource(formula);
        return ev.Evaluate(row ?? new Dictionary<string, object?>(System.StringComparer.Ordinal));
    }

    // ============================================================
    // Tokenizer / Parser — happy path
    // ============================================================

    [Fact] public void Number_Integer() => Assert.Equal(42m, Eval("42"));

    [Fact] public void Number_Decimal() => Assert.Equal(12.5m, Eval("12.5"));

    [Fact] public void String_Simple() => Assert.Equal("merhaba", Eval("'merhaba'"));

    [Fact] public void String_EscapedQuote() => Assert.Equal("it's", Eval("'it''s'"));

    [Fact] public void Bool_True() => Assert.Equal(true, Eval("TRUE"));

    [Fact] public void Bool_False_Lowercase() => Assert.Equal(false, Eval("false"));

    [Fact] public void Null_Literal() => Assert.Null(Eval("NULL"));

    [Fact]
    public void Column_Bareword()
    {
        var row = Row(("satis", 100m));
        Assert.Equal(100m, Eval("satis", row));
    }

    [Fact]
    public void Column_Bracketed_Turkish()
    {
        var row = Row(("Şube", "Ankara"));
        Assert.Equal("Ankara", Eval("[Şube]", row));
    }

    [Fact]
    public void Arithmetic_Mixed_Precedence()
    {
        // 2 + 3 * 4 = 14, parser doğru precedence
        Assert.Equal(14m, Eval("2 + 3 * 4"));
    }

    [Fact]
    public void Arithmetic_Parens_Override()
    {
        Assert.Equal(20m, Eval("(2 + 3) * 4"));
    }

    [Fact]
    public void Arithmetic_UnaryMinus()
    {
        Assert.Equal(-7m, Eval("-7"));
        Assert.Equal(-1m, Eval("-(2 - 1)"));
    }

    [Fact]
    public void Arithmetic_Column_Subtract()
    {
        var row = Row(("Bugun", 1000m), ("Gecen", 800m));
        Assert.Equal(200m, Eval("Bugun - Gecen", row));
    }

    [Fact]
    public void Comparison_Numeric()
    {
        Assert.Equal(true, Eval("5 > 3"));
        Assert.Equal(false, Eval("5 = 3"));
        Assert.Equal(true, Eval("5 != 3"));
        Assert.Equal(true, Eval("5 <> 3"));
        Assert.Equal(true, Eval("3 <= 3"));
    }

    [Fact]
    public void Logic_And_Or_Not()
    {
        Assert.Equal(true, Eval("TRUE AND TRUE"));
        Assert.Equal(false, Eval("TRUE AND FALSE"));
        Assert.Equal(true, Eval("FALSE OR TRUE"));
        Assert.Equal(false, Eval("NOT TRUE"));
    }

    [Fact]
    public void Logic_3VL_Null_Or_True()
    {
        // SQL semantik: NULL OR TRUE = TRUE
        Assert.Equal(true, Eval("NULL OR TRUE"));
        // NULL AND FALSE = FALSE
        Assert.Equal(false, Eval("NULL AND FALSE"));
        // NULL AND TRUE = NULL
        Assert.Null(Eval("NULL AND TRUE"));
    }

    [Fact]
    public void If_Basic()
    {
        Assert.Equal("yuksek", Eval("IF(5 > 3, 'yuksek', 'dusuk')"));
        Assert.Equal("dusuk", Eval("IF(2 > 3, 'yuksek', 'dusuk')"));
    }

    [Fact]
    public void Iif_Alias()
    {
        var row = Row(("satis", 150m));
        Assert.Equal("Yüksek", Eval("IIF(satis > 100, 'Yüksek', 'Düşük')", row));
    }

    [Fact]
    public void If_Nested()
    {
        var row = Row(("stok", 30m));
        var formula = "IF(stok < 10, 'Az', IF(stok < 50, 'Orta', 'Bol'))";
        Assert.Equal("Orta", Eval(formula, row));
    }

    [Fact]
    public void Case_Branches()
    {
        var row = Row(("durum", "B"));
        var formula = "CASE WHEN durum = 'A' THEN 1 WHEN durum = 'B' THEN 2 ELSE 0 END";
        Assert.Equal(2m, Eval(formula, row));
    }

    [Fact]
    public void Case_Else_Default()
    {
        var row = Row(("durum", "Z"));
        var formula = "CASE WHEN durum = 'A' THEN 1 ELSE 99 END";
        Assert.Equal(99m, Eval(formula, row));
    }

    [Fact]
    public void Case_NoMatch_NoElse_Null()
    {
        var row = Row(("durum", "Z"));
        var formula = "CASE WHEN durum = 'A' THEN 1 END";
        Assert.Null(Eval(formula, row));
    }

    [Fact]
    public void Plan_Example_PercentChange()
    {
        // (satis - maliyet) / maliyet * 100
        var row = Row(("satis", 150m), ("maliyet", 100m));
        Assert.Equal(50m, Eval("(satis - maliyet) / maliyet * 100", row));
    }

    // ============================================================
    // Edge cases — null + zero division
    // ============================================================

    [Fact]
    public void Arithmetic_Null_Propagates()
    {
        var row = Row(("a", null));
        Assert.Null(Eval("a + 5", row));
    }

    [Fact]
    public void Divide_By_Zero_Returns_Null()
    {
        Assert.Null(Eval("10 / 0"));
    }

    [Fact]
    public void Comparison_Null_Returns_Null()
    {
        // NULL = anything → NULL (SQL semantik)
        Assert.Null(Eval("NULL = 1"));
        Assert.Null(Eval("1 < NULL"));
    }

    [Fact]
    public void DBNull_Treated_As_Null()
    {
        var row = Row(("x", System.DBNull.Value));
        Assert.Null(Eval("x + 1", row));
    }

    // ============================================================
    // Hata path'ler — security + UX
    // ============================================================

    [Fact]
    public void Unknown_Column_Throws()
    {
        var ex = Assert.Throws<FormulaEvaluationException>(() => Eval("yokKolon * 2"));
        Assert.Contains("yokKolon", ex.Message);
    }

    [Fact]
    public void Unclosed_String_Throws_Parse()
    {
        var ex = Assert.Throws<FormulaParseException>(() => Eval("'metin"));
        Assert.Contains("kapatılmadı", ex.Message);
    }

    [Fact]
    public void Unclosed_Paren_Throws_Parse()
    {
        var ex = Assert.Throws<FormulaParseException>(() => Eval("(1 + 2"));
        Assert.Contains("Beklenen", ex.Message);
    }

    [Fact]
    public void Unknown_Symbol_Semicolon_Throws()
    {
        var ex = Assert.Throws<FormulaParseException>(() => Eval("1; 2"));
        Assert.Contains("Bilinmeyen sembol", ex.Message);
    }

    [Fact]
    public void Eval_Function_Not_Whitelisted_Throws()
    {
        // 'eval' bareword sayılır, sonra '(' geldiğinde parser primary-after-ident desteklemediği için hata.
        // Tokenizer eval'i Ident olarak kabul eder, parser ColumnNode olarak kabul eder, '(' ondan sonra
        // beklenmedik sembol → parse hata.
        var ex = Assert.Throws<FormulaParseException>(() => Eval("eval('x')"));
        Assert.Contains("formül sonu", ex.Message);
    }

    [Fact]
    public void Bang_Without_Equals_Throws()
    {
        var ex = Assert.Throws<FormulaParseException>(() => Eval("1 ! 2"));
        Assert.Contains("!", ex.Message);
    }

    [Fact]
    public void Empty_Bracket_Throws()
    {
        Assert.Throws<FormulaParseException>(() => Eval("[]"));
    }

    [Fact]
    public void Type_Mismatch_String_Plus_Number_Throws()
    {
        var ex = Assert.Throws<FormulaEvaluationException>(() => Eval("'a' + 1"));
        Assert.Contains("String", ex.Message);
    }

    [Fact]
    public void Cmp_Cross_Type_Lt_Throws()
    {
        var ex = Assert.Throws<FormulaEvaluationException>(() => Eval("'a' < 1"));
        Assert.Contains("aynı tip", ex.Message);
    }

    [Fact]
    public void Cmp_Cross_Type_Eq_Returns_False()
    {
        // Eşitlik cross-type için reddetmiyor (SQL'de hata, biz false dönüyoruz — explicit karar)
        Assert.Equal(false, Eval("1 = '1'"));
    }

    [Fact]
    public void Too_Long_Formula_Throws()
    {
        var longFormula = new string('1', 5000);
        Assert.Throws<FormulaParseException>(() => Eval(longFormula));
    }

    [Fact]
    public void Deeply_Nested_Throws()
    {
        // 70 iç içe parantez — limit 64
        var f = new string('(', 70) + "1" + new string(')', 70);
        Assert.Throws<FormulaParseException>(() => Eval(f));
    }

    [Fact]
    public void Case_Without_End_Throws()
    {
        Assert.Throws<FormulaParseException>(() => Eval("CASE WHEN 1=1 THEN 'a'"));
    }

    [Fact]
    public void If_Wrong_Arity_Throws()
    {
        Assert.Throws<FormulaParseException>(() => Eval("IF(1=1, 'a')"));
    }

    [Fact]
    public void TryParse_Returns_False_For_Invalid()
    {
        Assert.False(FormulaParser.TryParse("(1 +", out _, out var err, out var pos));
        Assert.NotNull(err);
        Assert.True(pos > 0);
    }

    [Fact]
    public void TryParse_Returns_True_For_Valid()
    {
        Assert.True(FormulaParser.TryParse("a + b", out var node, out _, out _));
        Assert.NotNull(node);
    }

    [Fact]
    public void Reuse_AST_For_Multiple_Rows()
    {
        var ev = FormulaEvaluator.FromSource("satis * 2");
        Assert.Equal(20m, ev.Evaluate(Row(("satis", 10m))));
        Assert.Equal(50m, ev.Evaluate(Row(("satis", 25m))));
        Assert.Null(ev.Evaluate(Row(("satis", null))));
    }
}
