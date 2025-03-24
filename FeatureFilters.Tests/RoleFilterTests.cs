using FeatureFilters.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace FeatureFilters.Tests;

public class RoleFilterTests
{
    private static IConfiguration CreateMockConfiguration(string[] allowedRoles)
    {
        var configData = new Dictionary<string, string?>();

        for (int i = 0; i < allowedRoles.Length; i++)
        {
            configData[$"AllowedRoles:{i}"] = allowedRoles[i];
        }

        return new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
    }

    private static Mock<HttpContext> CreateMockHttpContext(string roles)
    {
        var httpContextMock = new Mock<HttpContext>();
        var headerMock = new Mock<IHeaderDictionary>();

        headerMock.Setup(h => h["X-User-Roles"]).Returns(new StringValues(roles));
        httpContextMock.Setup(h => h.Request.Headers).Returns(headerMock.Object);

        return httpContextMock;
    }

    [Fact]
    public async Task EvaluateAsync_NoAllowedRoles_ReturnsFalse()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var filter = new RoleFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(Array.Empty<string>())
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_NoRolesHeader_ReturnsFalse()
    {
        var httpContextMock = CreateMockHttpContext(string.Empty);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new RoleFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "Admin" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_RoleInAllowedList_ReturnsTrue()
    {
        var httpContextMock = CreateMockHttpContext("Admin");
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new RoleFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "Admin" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateAsync_RoleNotInAllowedList_ReturnsFalse()
    {
        var httpContextMock = CreateMockHttpContext("User");
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var filter = new RoleFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "Admin" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateAsync_NullHttpContext_ReturnsFalse()
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var filter = new RoleFilter(httpContextAccessorMock.Object);
        var context = new FeatureFilterEvaluationContext
        {
            Parameters = CreateMockConfiguration(new[] { "Admin" })
        };

        var result = await filter.EvaluateAsync(context);

        Assert.False(result);
    }
}