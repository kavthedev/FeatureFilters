using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using FeatureFilters.Utils;
using StringSplitOptions = System.StringSplitOptions;

namespace FeatureFilters.Filters;

/// <summary>
/// Filters feature access based on user roles specified in HTTP headers.
/// </summary>
[FilterAlias("RoleFilter")]
public class RoleFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RoleFilter> _logger;
    private const string HeaderName = "X-User-Roles";
    private const string ParameterKey = "AllowedRoles";

    /// <summary>
    /// A feature filter that enables a feature based on user roles provided in the HTTP request header.
    /// </summary>
    public RoleFilter(IHttpContextAccessor httpContextAccessor, ILogger<RoleFilter>? logger = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger ?? NullLogger<RoleFilter>.Instance;
    }

    /// <summary>
    /// Evaluates whether the feature should be enabled based on the user roles in the HTTP headers.
    /// </summary>
    /// <param name="context">The feature filter evaluation context containing parameters and configuration.</param>
    /// <returns>A task that resolves to true if the feature is enabled, otherwise false.</returns>
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        if (context.Parameters is null)
        {
            _logger.LogError("RoleFilter: Missing parameters.");
            return Task.FromResult(false);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _logger.LogWarning("RoleFilter: No HttpContext found.");
            return Task.FromResult(false);
        }

        var rolesHeader = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rolesHeader))
        {
            _logger.LogInformation("RoleFilter: Header '{Header}' not present.", HeaderName);
            return Task.FromResult(false);
        }

        var userRoles = rolesHeader!
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .ToArray();

        var allowed = FeatureFilterUtils.ExtractAllowedValues(context.Parameters, ParameterKey);
        if (allowed.Length == 0)
        {
            _logger.LogWarning("RoleFilter: No allowed roles configured.");
            return Task.FromResult(false);
        }

        var match = userRoles.Any(userRole =>
            allowed.Any(allowedRole =>
                allowedRole.Equals(userRole, StringComparison.OrdinalIgnoreCase)));

        _logger.LogDebug("RoleFilter: Roles '{Roles}' matched = {Match}.", string.Join(",", userRoles), match);
        return Task.FromResult(match);
    }
}