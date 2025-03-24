# FeatureFilters for Azure App Configuration

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)

A collection of feature filters for use with Azure App Configuration and Microsoft FeatureManagement. These filters make it easy to control feature flags based on HTTP headers, allowing for tenant-based, country-based, role-based, and progressive rollout scenarios.

## 📋 Features

- **TenantFilter** - Enable features for specific tenants
- **CountryFilter** - Enable features for specific countries
- **RoleFilter** - Enable features based on user roles
- **PercentageRolloutFilter** - Gradually roll out features to a percentage of users/requests

## 🚀 Installation

```bash
dotnet add package FeatureFilters
```

## 🔧 Configuration

### Register in your ASP.NET Core application

```csharp
// Program.cs or Startup.cs
using FeatureFilters.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add feature management with Azure App Configuration
builder.Services.AddHttpContextAccessor();
builder.Services.AddFeatureManagement()
    .AddFeatureFilter<TenantFilter>()
    .AddFeatureFilter<CountryFilter>()
    .AddFeatureFilter<RoleFilter>()
    .AddFeatureFilter<PercentageRolloutFilter>();

// If using Azure App Configuration:
builder.Configuration.AddAzureAppConfiguration(options => {
    options.Connect(builder.Configuration["ConnectionString:AppConfig"])
           .UseFeatureFlags();
});
```

## 📖 Usage

### TenantFilter

Enables a feature for specific tenant IDs. The filter checks the `X-Tenant-ID` header against a list of allowed tenants.

```json
{
  "FeatureManagement": {
    "MultiTenantFeature": {
      "EnabledFor": [
        {
          "Name": "TenantFilter",
          "Parameters": {
            "AllowedTenants": ["tenant1", "tenant2", "premium-client"]
          }
        }
      ]
    }
  }
}
```

### CountryFilter

Enables a feature for specific countries. The filter checks the `X-Country-Code` header against a list of allowed countries.

```json
{
  "FeatureManagement": {
    "RegionalFeature": {
      "EnabledFor": [
        {
          "Name": "CountryFilter",
          "Parameters": {
            "AllowedCountries": ["US", "CA", "UK", "AU"]
          }
        }
      ]
    }
  }
}
```

### RoleFilter

Enables a feature for users with specific roles. The filter checks the `X-User-Roles` header (comma-separated roles) against a list of allowed roles.

```json
{
  "FeatureManagement": {
    "AdminFeature": {
      "EnabledFor": [
        {
          "Name": "RoleFilter",
          "Parameters": {
            "AllowedRoles": ["Admin", "SuperUser"]
          }
        }
      ]
    }
  }
}
```

### PercentageRolloutFilter

Gradually enables a feature for a percentage of requests or users. When using `StickyMode: true`, the filter uses the `X-Correlation-ID` header to ensure consistent behavior for the same user/request.

```json
{
  "FeatureManagement": {
    "BetaFeature": {
      "EnabledFor": [
        {
          "Name": "PercentageRolloutFilter",
          "Parameters": {
            "Percentage": 25.5,
            "StickyMode": true
          }
        }
      ]
    }
  }
}
```

## 🔄 Header Value Format

| Filter | Header | Format | Example |
|--------|--------|--------|---------|
| TenantFilter | X-Tenant-ID | String | `premium-client` |
| CountryFilter | X-Country-Code | String | `US` |
| RoleFilter | X-User-Roles | Comma-separated | `Admin,Editor,Viewer` |
| PercentageRolloutFilter | X-Correlation-ID | String (when using StickyMode) | `user123` or GUID |

## 🧪 Azure App Configuration Example

When using Azure App Configuration to manage feature flags, you can configure these filters through the Azure Portal:

1. Navigate to your App Configuration resource
2. Go to "Feature Manager" → Select/Create a feature flag
3. Add a filter of type "TenantFilter", "CountryFilter", "RoleFilter", or "PercentageRolloutFilter"
4. Configure parameters as JSON, e.g., `{"AllowedTenants": ["tenant1", "tenant2"]}`

## 🔍 Advanced Usage

### Parameter Formats

All `Allowed*` parameters support both array and comma-separated string formats:

```json
// Array format
"AllowedTenants": ["tenant1", "tenant2"]

// String format
"AllowedTenants": "tenant1,tenant2"
```

### Combining Filters

You can combine multiple filters for more complex scenarios:

```json
{
  "FeatureManagement": {
    "PremiumFeature": {
      "EnabledFor": [
        {
          "Name": "TenantFilter",
          "Parameters": {
            "AllowedTenants": ["premium-tenant"]
          }
        },
        {
          "Name": "RoleFilter",
          "Parameters": {
            "AllowedRoles": ["Admin", "PremiumUser"]
          }
        }
      ]
    }
  }
}
```

In this example, the feature is enabled if EITHER the tenant is "premium-tenant" OR the user has the "Admin" or "PremiumUser" role.

## 💡 Tips and Tricks for Feature Management

### 🚀 Best Practices for Feature Flags
1. **Centralized Management**: Use **Azure App Configuration** to manage feature flags centrally for dynamic updates and consistency.
2. **Environment-Specific Labels**: Assign labels (`development`, `staging`, `production`) to feature flags for environment-specific configurations.
3. **Dynamic Refresh**: Enable dynamic refresh to apply changes without restarting the application.

---

### 🔑 Naming Conventions
- **Format**: `{product}__{scope}__{featureName}` (all lowercase)
  - **product**: Application name (e.g., `myapp`).
  - **scope**: `global`, `institution`, or `municipality`. Optional. 
  - **featureName**: Descriptive feature name.
- **Example**: `myapp__global__newfeaturebutton`

---

### 📌 Custom Filters
- **TenantFilter**: Targets specific tenants.
- **CountryFilter**: Targets specific countries.
- **RoleFilter**: Targets specific user roles.
- **PercentageRolloutFilter**: Gradually rolls out features to a percentage of users.

**Example JSON for a Country-Specific Flag**:
```json
{
  "id": "myapp__country__newdashboard",
  "enabled": true,
  "label": "production",
  "conditions": {
    "client_filters": [
      {
        "name": "CountryFilter",
        "parameters": {
          "AllowedCountries": ["US", "CA"]
        }
      }
    ]
  }
}
```

---

### ⚙️ Dynamic Refresh Setup
- **Feature Flag Refresh**: Every 30 minutes.
- **Sentinel Key Polling**: Every 30 seconds for immediate updates.

**ASP.NET Core Example**:
```csharp
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(connectionString)
        .Select("myapp:*", environmentLabel)
        .UseFeatureFlags(ffOptions =>
        {
            ffOptions.Label = environmentLabel;
            ffOptions.SetRefreshInterval(TimeSpan.FromMinutes(30));
        })
        .ConfigureRefresh(refreshOptions =>
        {
            refreshOptions.Register("myapp:sentinel", environmentLabel, refreshAll: true)
                .SetRefreshInterval(TimeSpan.FromSeconds(30));
        });
});
```

---

### ✅ Feature Flag Service
- **Centralized Logic**: Abstract feature flag handling into a service.

**Example**:
```csharp
public class FeatureFlagService(IFeatureManager featureManager)
{
    public Task<bool> IsFeatureEnabled(string featureName)
    {
        return featureManager.IsEnabledAsync(featureName);
    }
}
```

---

### 📚 Additional Resources
- [Azure App Configuration Documentation](https://learn.microsoft.com/azure/azure-app-configuration/)
- [Feature Management in Azure](https://learn.microsoft.com/azure/azure-app-configuration/howto-feature-flags-aspnet-core)

## 💻 Requirements

- .NET 9.0 or later
- Microsoft.FeatureManagement package
- ASP.NET Core

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
