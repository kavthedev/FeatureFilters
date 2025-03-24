using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace FeatureFilters.Tests;

public class IntegrationTests
{
    private readonly HttpClient _client;

    public IntegrationTests()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddHttpContextAccessor();
                services.AddRouting();

                services.AddFeatureManagement()
                    .AddFeatureFilter<Filters.TenantFilter>()
                    .AddFeatureFilter<Filters.CountryFilter>()
                    .AddFeatureFilter<Filters.RoleFilter>()
                    .AddFeatureFilter<Filters.PercentageRolloutFilter>();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/feature/tenant", async context =>
                    {
                        var featureManager = context.RequestServices.GetRequiredService<IFeatureManager>();
                        if (await featureManager.IsEnabledAsync("TenantFeature"))
                        {
                            await context.Response.WriteAsync("TenantFeature is enabled.");
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        }
                    });
                });
            });

        var server = new TestServer(builder);
        _client = server.CreateClient();
    }

    [Fact]
    public async Task TenantFeature_ReturnsForbidden_WhenNotEnabled()
    {
        var response = await _client.GetAsync("/feature/tenant");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}