using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using FeatureFilters.Utils;

namespace FeatureFilters.Filters;

/// <summary>
/// Filters feature access based on the tenant ID specified in HTTP headers.
/// </summary>
[FilterAlias("TenantFilter")]
public class TenantFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantFilter> _logger;
    private const string HeaderName = "X-Tenant-ID";
    private const string ParameterKey = "AllowedTenants";

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantFilter"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor used to access the current HTTP context.</param>
    /// <param name="logger">Optional logger for diagnostic information. If not provided, a null logger will be used.</param>
    public TenantFilter(IHttpContextAccessor httpContextAccessor, ILogger<TenantFilter>? logger = null)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? NullLogger<TenantFilter>.Instance;
    }

    /// <summary>
    /// Evaluates whether the feature should be enabled based on the tenant ID in the HTTP headers.
    /// </summary>
    /// <param name="context">The feature filter evaluation context containing parameters and configuration.
    /// The context must include a parameter named "AllowedTenants" containing the list of tenant IDs for which the feature should be enabled.</param>
    /// <returns>A task that resolves to true if the current tenant ID matches one in the allowed list, otherwise false.</returns>
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        if (context.Parameters is null)
        {
            _logger.LogError("TenantFilter: Missing parameters.");
            return Task.FromResult(false);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _logger.LogWarning("TenantFilter: No HttpContext found.");
            return Task.FromResult(false);
        }

        var tenantId = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogInformation("TenantFilter: Header '{Header}' not present.", HeaderName);
            return Task.FromResult(false);
        }

        var allowed = FeatureFilterUtils.ExtractAllowedValues(context.Parameters, ParameterKey);
        if (allowed.Length == 0)
        {
            _logger.LogWarning("TenantFilter: No allowed tenants configured.");
            return Task.FromResult(false);
        }

        var match = allowed.Any(x => x.Equals(tenantId!.Trim(), StringComparison.OrdinalIgnoreCase));
        _logger.LogDebug("TenantFilter: TenantId '{Tenant}' match = {Match}.", tenantId, match);

        return Task.FromResult(match);
    }
}