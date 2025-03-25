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
/// Filters feature access based on the country code specified in HTTP headers.
/// </summary>
[FilterAlias("CountryFilter")]
public class CountryFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CountryFilter> _logger;
    private const string HeaderName = "X-Country-Code";
    private const string ParameterKey = "AllowedCountries";

    /// <summary>
    /// Initializes a new instance of the <see cref="CountryFilter"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor used to access the current HTTP context.</param>
    /// <param name="logger">Optional logger for diagnostic information. If not provided, a null logger will be used.</param>
    public CountryFilter(IHttpContextAccessor httpContextAccessor, ILogger<CountryFilter>? logger = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger ?? NullLogger<CountryFilter>.Instance;
    }

    /// <summary>
    /// Evaluates whether the feature should be enabled based on the country code in the HTTP headers.
    /// </summary>
    /// <param name="context">The feature filter evaluation context containing parameters and configuration.
    /// The context must include a parameter named "AllowedCountries" containing the list of country codes for which the feature should be enabled.</param>
    /// <returns>A task that resolves to true if the current country code matches one in the allowed list, otherwise false.</returns>
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        if (context.Parameters is null)
        {
            _logger.LogError("CountryFilter: Missing parameters.");
            return Task.FromResult(false);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _logger.LogWarning("CountryFilter: No HttpContext found.");
            return Task.FromResult(false);
        }

        var country = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(country))
        {
            _logger.LogInformation("CountryFilter: Header '{Header}' not present.", HeaderName);
            return Task.FromResult(false);
        }

        var allowed = FeatureFilterUtils.ExtractAllowedValues(context.Parameters, ParameterKey);
        if (allowed.Length == 0)
        {
            _logger.LogWarning("CountryFilter: No allowed countries configured.");
            return Task.FromResult(false);
        }

        var match = allowed.Any(x => x.Equals(country!.Trim(), StringComparison.OrdinalIgnoreCase));
        _logger.LogDebug("CountryFilter: Country '{Country}' match = {Match}.", country, match);

        return Task.FromResult(match);
    }
}