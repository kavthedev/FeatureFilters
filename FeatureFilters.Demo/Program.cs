using FeatureFilters.Filters;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddFeatureManagement()
    .AddFeatureFilter<TenantFilter>()
    .AddFeatureFilter<CountryFilter>()
    .AddFeatureFilter<RoleFilter>()
    .AddFeatureFilter<PercentageRolloutFilter>();

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FeatureFilters Demo API",
        Version = "v1",
        Description = "API demonstrating the use of FeatureFilters for Azure App Configuration."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FeatureFilters Demo API v1");
    });
}

app.UseHttpsRedirection();

// Feature flag endpoints
app.MapGet("/feature/tenant", async (IFeatureManager featureManager, HttpContext httpContext) =>
{
    if (await featureManager.IsEnabledAsync("TenantFeature"))
    {
        return Results.Ok("TenantFeature is enabled for this tenant.");
    }
    return Results.StatusCode(StatusCodes.Status403Forbidden);
});

app.MapGet("/feature/country", async (IFeatureManager featureManager, HttpContext httpContext) =>
{
    if (await featureManager.IsEnabledAsync("CountryFeature"))
    {
        return Results.Ok("CountryFeature is enabled for this country.");
    }
    return Results.StatusCode(StatusCodes.Status403Forbidden);
});

app.MapGet("/feature/role", async (IFeatureManager featureManager, HttpContext httpContext) =>
{
    if (await featureManager.IsEnabledAsync("RoleFeature"))
    {
        return Results.Ok("RoleFeature is enabled for this role.");
    }
    return Results.StatusCode(StatusCodes.Status403Forbidden);
});

app.MapGet("/feature/rollout", async (IFeatureManager featureManager, HttpContext httpContext) =>
{
    if (await featureManager.IsEnabledAsync("RolloutFeature"))
    {
        return Results.Ok("RolloutFeature is enabled for this request.");
    }
    return Results.StatusCode(StatusCodes.Status403Forbidden);
});

await app.RunAsync();
