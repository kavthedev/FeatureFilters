using FeatureFilters.Filters;
using FeatureFilters.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace FeatureFilters.Tests;

public class TenantFilterTests
{
    private static IConfiguration CreateMockConfiguration(string[] allowedTenants)
    {
        var configData = new Dictionary<string, string?>();

        for (int i = 0; i < allowedTenants.Length; i++)
        {
            configData[$"AllowedTenants:{i}"] = allowedTenants[i];
        }

        return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
    }

    private static Mock<HttpContext> CreateMockHttpContext(string tenantId)
    {
        var httpContextMock = new Mock<HttpContext>();
        var headerMock = new Mock<IHeaderDictionary>();

        headerMock.Setup(h => h["X-Tenant-ID"]).Returns(new StringValues(tenantId));
        httpContextMock.Setup(h => h.Request.Headers).Returns(headerMock.Object);

        return httpContextMock;
    }

    [Fact]
    public async Task EvaluateAsync_NoAllowedTenants_ReturnsFalse()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var filter = new TenantFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(Array.Empty<string>())
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_NoTenantId_ReturnsFalse()
    {
        var httpContextMock = CreateMockHttpContext(string.Empty);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new TenantFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "tenant1" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_TenantIdInAllowedList_ReturnsTrue()
    {
        var httpContextMock = CreateMockHttpContext("tenant1");
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new TenantFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "tenant1" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_TenantIdNotInAllowedList_ReturnsFalse()
    {
        var httpContextMock = CreateMockHttpContext("tenant2");
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new TenantFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "tenant1" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_NullHttpContext_ReturnsFalse()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var filter = new TenantFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "tenant1" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }
}