using FeatureFilters.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace FeatureFilters.Tests;

public class PercentageRolloutFilterTests
{
    private static IConfiguration CreateMockConfiguration(double percentage, bool stickyMode)
    {
        var configData = new Dictionary<string, string?>
        {
            { "Percentage", percentage.ToString() },
            { "StickyMode", stickyMode.ToString() }
        };

        return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
    }

    private static Mock<HttpContext> CreateMockHttpContext(string correlationId)
    {
        var httpContextMock = new Mock<HttpContext>();
        var headerMock = new Mock<IHeaderDictionary>();

        headerMock.Setup(h => h["X-Correlation-ID"]).Returns(new StringValues(correlationId));
        httpContextMock.Setup(h => h.Request.Headers).Returns(headerMock.Object);

        return httpContextMock;
    }

    [Fact]
    public async Task EvaluateAsync_PercentageZero_ReturnsFalse()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var filter = new PercentageRolloutFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(0, false)
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_PercentageHundred_ReturnsTrue()
    {
        var httpContextMock = new Mock<HttpContext>();
        var headerMock = new Mock<IHeaderDictionary>();
        httpContextMock.Setup(h => h.Request.Headers).Returns(headerMock.Object);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new PercentageRolloutFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(100, false)
        };

        var result = await filter.EvaluateAsync(context);

        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_StickyModeWithCorrelationId_ReturnsConsistentResult()
    {
        var httpContextMock = CreateMockHttpContext("user123");
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new PercentageRolloutFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(50, true)
        };

        var result1 = await filter.EvaluateAsync(context);
        var result2 = await filter.EvaluateAsync(context);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public async Task EvaluateAsync_NonStickyMode_ReturnsRandomResult()
    {
        var httpContextMock = new Mock<HttpContext>();
        var headerMock = new Mock<IHeaderDictionary>();
        httpContextMock.Setup(h => h.Request.Headers).Returns(headerMock.Object);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new PercentageRolloutFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(50, false)
        };

        var result1 = await filter.EvaluateAsync(context);
        await Task.Delay(10);
        var result2 = await filter.EvaluateAsync(context);

        Assert.NotEqual(result1, result2); // May still fail randomly if both happen to be same!
    }

    [Fact]
    public async Task EvaluateAsync_NullHttpContext_ReturnsFalse()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var filter = new PercentageRolloutFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(50, true)
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }
}