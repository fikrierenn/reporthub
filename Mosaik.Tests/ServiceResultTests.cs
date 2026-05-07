using Mosaik.Core.Domain;

namespace Mosaik.Tests;

// Plan 16.5 Faz A — ServiceResult/ServiceResult<T> pattern.
public class ServiceResultTests
{
    [Fact]
    public void Ok_NonGeneric_SetsIsSuccessTrue()
    {
        var result = ServiceResult.Ok("Tamam.");
        Assert.True(result.IsSuccess);
        Assert.Equal("Tamam.", result.Message);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_NonGeneric_SetsErrorCode()
    {
        var result = ServiceResult.Failure("Hata.", "ERR_X");
        Assert.False(result.IsSuccess);
        Assert.Equal("Hata.", result.Message);
        Assert.Equal("ERR_X", result.ErrorCode);
    }

    [Fact]
    public void Ok_Generic_CarriesData()
    {
        var result = ServiceResult<int>.Ok(42, "Sonuc");
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data);
        Assert.Equal("Sonuc", result.Message);
    }

    [Fact]
    public void Failure_Generic_DataIsDefault()
    {
        var result = ServiceResult<string>.Failure("Bulunamadi.");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal("Bulunamadi.", result.Message);
    }
}
