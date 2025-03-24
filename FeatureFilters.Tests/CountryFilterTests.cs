using FeatureFilters.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Primitives;
using Moq;

namespace FeatureFilters.Tests;

public class CountryFilterTests
{
    private static IConfiguration CreateMockConfiguration(string[] allowedCountries)
    {
        var configData = new Dictionary<string, string?>();

        for (var i = 0; i < allowedCountries.Length; i++)
        {
            configData[$"AllowedCountries:{i}"] = allowedCountries[i];
        }

        return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
    }

    private static Mock<HttpContext> CreateMockHttpContext(string country)
    {
        var httpContextMock = new Mock<HttpContext>();
        var headerMock = new Mock<IHeaderDictionary>();

        headerMock.Setup(h => h["X-Country-Code"]).Returns(new StringValues(country));
        httpContextMock.Setup(h => h.Request.Headers).Returns(headerMock.Object);

        return httpContextMock;
    }

    [Fact]
    public async Task EvaluateAsync_NoAllowedCountries_ReturnsFalse()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var filter = new CountryFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(Array.Empty<string>())
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_NoCountryHeader_ReturnsFalse()
    {
        var httpContextMock = CreateMockHttpContext(string.Empty);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new CountryFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "US" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_CountryInAllowedList_ReturnsTrue()
    {
        var httpContextMock = CreateMockHttpContext("US");
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new CountryFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "US" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_CountryNotInAllowedList_ReturnsFalse()
    {
        var httpContextMock = CreateMockHttpContext("CA");
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new CountryFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "US" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_NullHttpContext_ReturnsFalse()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var filter = new CountryFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "US" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }
}