using System.Linq;
using Microsoft.Extensions.Configuration;

namespace FeatureFilters.Utils;

/// <summary>
/// Provides utility methods for feature filters.
/// </summary>
public static class FeatureFilterUtils
{
    /// <summary>
    /// Extracts allowed values from feature filter parameters (supports both arrays and comma-separated strings).
    /// </summary>
    /// <param name="parameters">The configuration parameters containing the allowed values.</param>
    /// <param name="parameterKey">The key to look up the allowed values in the parameters.</param>
    /// <returns>An array of allowed values extracted from the parameters.</returns>
    public static string[] ExtractAllowedValues(IConfiguration parameters, string parameterKey)
    {
        var array = parameters.GetSection(parameterKey).Get<string[]>();
        if (array is { Length: > 0 })
            return array;

        var csv = parameters.GetValue<string>(parameterKey);
        return !string.IsNullOrWhiteSpace(csv)
            ? csv.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray()
            : [];
    }
}