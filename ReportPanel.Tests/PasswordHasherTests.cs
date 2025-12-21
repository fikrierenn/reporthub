using ReportPanel.Services;

namespace ReportPanel.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void CreateHash_ShouldReturnExpectedFormat()
    {
        var hash = PasswordHasher.CreateHash("Secur3Pass!");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        var parts = hash.Split('$');
        Assert.Equal(4, parts.Length);
        Assert.Equal("PBKDF2", parts[0]);
        Assert.True(int.TryParse(parts[1], out _));
        Assert.False(string.IsNullOrWhiteSpace(parts[2]));
        Assert.False(string.IsNullOrWhiteSpace(parts[3]));
    }

    [Fact]
    public void Verify_ShouldReturnTrueForCorrectPassword()
    {
        var hash = PasswordHasher.CreateHash("Secur3Pass!");

        var result = PasswordHasher.Verify("Secur3Pass!", hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalseForWrongPassword()
    {
        var hash = PasswordHasher.CreateHash("Secur3Pass!");

        var result = PasswordHasher.Verify("WrongPass", hash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalseForInvalidHash()
    {
        var result = PasswordHasher.Verify("Secur3Pass!", "not-a-hash");

        Assert.False(result);
    }
}
