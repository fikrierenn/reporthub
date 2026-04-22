using ReportPanel.Services;

namespace ReportPanel.Tests;

/// <summary>
/// G-03 regression coverage: UserDataFilter key/value whitelist.
/// Values coming from a tampered DB row or a misconfigured admin UI must
/// never reach the SP parameter binding.
/// </summary>
public class UserDataFilterValidatorTests
{
    // ---- FilterKey ----

    [Theory]
    [InlineData("sube")]
    [InlineData("Bolum")]
    [InlineData("dept_code")]
    [InlineData("_underscore_first")]
    [InlineData("a")]
    [InlineData("a1b2c3")]
    public void IsValidKey_accepts_safe_identifiers(string key)
    {
        Assert.True(UserDataFilterValidator.IsValidKey(key));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("1starts_with_digit")]
    [InlineData("has space")]
    [InlineData("has-dash")]
    [InlineData("has;semicolon")]
    [InlineData("has'quote")]
    [InlineData("has\"doublequote")]
    [InlineData("has.dot")]
    [InlineData("drop_table;--")]
    [InlineData("a/**/or/**/1=1")]
    public void IsValidKey_rejects_unsafe_values(string? key)
    {
        Assert.False(UserDataFilterValidator.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_rejects_over_63_chars()
    {
        var tooLong = new string('a', 64);
        Assert.False(UserDataFilterValidator.IsValidKey(tooLong));
    }

    [Fact]
    public void IsValidKey_accepts_exact_63_chars()
    {
        var atLimit = new string('a', 63);
        Assert.True(UserDataFilterValidator.IsValidKey(atLimit));
    }

    // ---- FilterValue ----

    [Theory]
    [InlineData("FSM")]
    [InlineData("FSM,HEYKEL")]
    [InlineData("101,102,103")]
    [InlineData("code_01")]
    [InlineData("dept-a, dept-b")]
    [InlineData("item.one,item.two")]
    [InlineData("A B C")]
    public void IsValidValue_accepts_safe_csv(string value)
    {
        Assert.True(UserDataFilterValidator.IsValidValue(value));
    }

    // Regex'in yakaladigi karakterler (SqlParameter + STRING_SPLIT zaten parametrizasyon yapar;
    // whitelist en az savunma olarak meta karakterleri keser: ' " ; / * \ < > : | & ( ) { } [ ] ? = + $ !)
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("a'b")]
    [InlineData("a\"b")]
    [InlineData("a;b")]
    [InlineData("a/*b*/c")]
    [InlineData("\\xDROP")]
    [InlineData("<script>")]
    [InlineData("a=b")]
    [InlineData("a|b")]
    [InlineData("a&b")]
    public void IsValidValue_rejects_meta_chars(string? value)
    {
        Assert.False(UserDataFilterValidator.IsValidValue(value));
    }

    // SQL keyword'leri veya "-- comment" patern'i izin verilen karakterlerden olustugu
    // icin regex gecer — savunma katmani server-side (SqlParameter + STRING_SPLIT).
    // Sadece kayıt olarak bunun beklenen davranis oldugunu belgele.
    [Theory]
    [InlineData("a--b")]
    [InlineData("DROP TABLE users")]
    [InlineData("1 or 1")]
    public void IsValidValue_accepts_alphanum_but_protection_is_serverside(string value)
    {
        Assert.True(UserDataFilterValidator.IsValidValue(value));
    }

    // ---- Combined ----

    [Fact]
    public void IsValid_true_when_both_valid()
    {
        Assert.True(UserDataFilterValidator.IsValid("sube", "FSM,HEYKEL"));
    }

    [Fact]
    public void IsValid_false_when_key_invalid()
    {
        Assert.False(UserDataFilterValidator.IsValid("has space", "FSM"));
    }

    [Fact]
    public void IsValid_false_when_value_invalid()
    {
        Assert.False(UserDataFilterValidator.IsValid("sube", "FSM;DROP TABLE"));
    }
}
