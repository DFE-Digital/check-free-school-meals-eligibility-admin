using CheckYourEligibility.Admin.Domain.DfeSignIn;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CheckYourEligibility.Admin.Gateways;

public class SchoolMenuContextResolver : ISchoolMenuContextResolver
{
    private readonly IMemoryCache _cache;
    private readonly IAdminGateway _adminGateway;
    private readonly ILocalAuthoritySettingsGateway _localAuthoritySettingsGateway;
    private readonly ILogger<SchoolMenuContextResolver> _logger;

    public SchoolMenuContextResolver(
        IMemoryCache cache,
        IAdminGateway adminGateway,
        ILocalAuthoritySettingsGateway localAuthoritySettingsGateway,
        ILogger<SchoolMenuContextResolver> logger)
    {
        _cache = cache;
        _adminGateway = adminGateway;
        _localAuthoritySettingsGateway = localAuthoritySettingsGateway;
        _logger = logger;
    }

    public async Task<SchoolMenuContext> ResolveAsync(DfeClaims claims)
    {
        return new SchoolMenuContext();
    }
}