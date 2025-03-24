using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FeatureFilters.Filters;

/// <summary>
/// Enables progressive rollout of features to a percentage of users or requests.
/// </summary>
[FilterAlias("PercentageRolloutFilter")]
public class PercentageRolloutFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PercentageRolloutFilter> _logger;
    private const string HeaderName = "X-Correlation-ID";
    private const string ParameterKeyPercentage = "Percentage";
    private const string ParameterKeyStickyMode = "StickyMode";
    private static readonly ThreadLocal<Random> ThreadLocalRandom = new(() => new Random());

    /// <summary>
    /// Initializes a new instance of the <see cref="PercentageRolloutFilter"/> class with the specified HTTP context accessor and logger.
    /// </summary>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    /// <param name="logger">The logger instance used for logging filter behavior. If null, a no-op logger will be used.</param>
    public PercentageRolloutFilter(IHttpContextAccessor httpContextAccessor, ILogger<PercentageRolloutFilter>? logger = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger ?? NullLogger<PercentageRolloutFilter>.Instance;
    }

    /// <summary>
    /// Evaluates whether the feature should be enabled based on the rollout percentage and sticky mode.
    /// </summary>
    /// <param name="context">The feature filter evaluation context containing parameters and configuration.
    /// The context must include parameters named "Percentage" and "StickyMode" to determine the rollout behavior.</param>
    /// <returns>A task that resolves to true if the feature is enabled based on the rollout logic, otherwise false.</returns>
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        if (context.Parameters is null)
        {
            _logger.LogError("PercentageRolloutFilter: Missing parameters.");
            return Task.FromResult(false);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _logger.LogWarning("PercentageRolloutFilter: No HttpContext found.");
            return Task.FromResult(false);
        }

        // Extract and validate percentage
        if (!context.Parameters.GetValue<double?>(ParameterKeyPercentage).HasValue)
        {
            _logger.LogError("PercentageRolloutFilter: Missing or invalid percentage value.");
            return Task.FromResult(false);
        }

        var percentage = context.Parameters.GetValue<double>(ParameterKeyPercentage);
        if (percentage < 0 || percentage > 100)
        {
            _logger.LogError("PercentageRolloutFilter: Percentage value must be between 0 and 100, got {Percentage}.", percentage);
            return Task.FromResult(false);
        }

        // If percentage is 0, feature is disabled
        if (percentage <= 0)
        {
            _logger.LogDebug("PercentageRolloutFilter: Feature disabled (percentage = 0).");
            return Task.FromResult(false);
        }

        // If percentage is 100, feature is fully enabled
        if (percentage >= 100)
        {
            _logger.LogDebug("PercentageRolloutFilter: Feature fully enabled (percentage = 100).");
            return Task.FromResult(true);
        }

        // Check if we're using "sticky" mode (based on user/request identifier)
        var stickyMode = context.Parameters.GetValue<bool>(ParameterKeyStickyMode);
        
        // In sticky mode, we use correlation ID; otherwise we use random roll
        if (stickyMode)
        {
            var correlationId = httpContext.Request.Headers[HeaderName].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                _logger.LogInformation("PercentageRolloutFilter: Sticky mode enabled but no correlation ID found, using random roll instead.");
                return Task.FromResult(GetRandomRoll() <= percentage);
            }

            // Generate deterministic value between 0-100 based on correlation ID
            var roll = GetHashBasedRoll(correlationId!);
            var enabled = roll <= percentage;
            
            _logger.LogDebug("PercentageRolloutFilter: Sticky mode for ID '{CorrelationId}', roll = {Roll}, enabled = {Enabled}.", 
                correlationId, roll, enabled);

                        return Task.FromResult(enabled);
        }
        else
        {
            // Non-sticky mode: simple random percentage
            var roll = GetRandomRoll();
            var enabled = roll <= percentage;
            
            _logger.LogDebug("PercentageRolloutFilter: Random roll = {Roll}, enabled = {Enabled}.", roll, enabled);

            return Task.FromResult(enabled);
        }
    }

    /// <summary>
    /// Generates a random roll value between 0 and 100.
    /// </summary>
    /// <returns>A random double value between 0 and 100.</returns>
    private static double GetRandomRoll()
    {
        return ThreadLocalRandom.Value!.NextDouble() * 100;
    }

    /// <summary>
    /// Generates a deterministic roll value based on a given identifier.
    /// </summary>
    /// <param name="identifier">The identifier used to generate the roll value.</param>
    /// <returns>A double value between 0 and 100 based on the hash of the identifier.</returns>
    private static double GetHashBasedRoll(string identifier)
    {
        // Create a deterministic hash of the identifier
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(identifier));
        
        // Use first 4 bytes (32 bits) of hash to create a value between 0-100
        var value = BitConverter.ToUInt32(hashBytes, 0);
        return (value % 10001) / 100.0; // Range: 0.0 to 100.0
    }
}