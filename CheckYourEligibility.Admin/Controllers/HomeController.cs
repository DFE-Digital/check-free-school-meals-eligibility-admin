using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.Infrastructure;
using CheckYourEligibility.Admin.Models;
using CheckYourEligibility.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CheckYourEligibility.Admin.Controllers;

public class HomeController : BaseController
{
    private readonly ILocalAuthoritySettingsGateway _localAuthoritySettingsGateway;
    private readonly IAdminGateway _adminGateway;
    private readonly IMemoryCache _cache;
    private readonly ISchoolMenuContextResolver _schoolMenuContextResolver;

    public HomeController(
    IDfeSignInApiService dfeSignInApiService,
    ILocalAuthoritySettingsGateway localAuthoritySettingsGateway,
    ISchoolMenuContextResolver schoolMenuContextResolver,
    IAdminGateway adminGateway,
    IMemoryCache cache) : base(dfeSignInApiService, schoolMenuContextResolver)
    {
        _localAuthoritySettingsGateway = localAuthoritySettingsGateway;
        _adminGateway = adminGateway;
        _schoolMenuContextResolver = schoolMenuContextResolver;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var categoryName = _Claims?.Organisation?.Category?.Name;
        if (categoryName == null)
        {
            return View("UnauthorizedOrganization");
        }

        List<string>? requiredRoleCodes = categoryName switch
        {
            Constants.CategoryTypeLA => [Constants.RoleCodeLA, Constants.RoleCodeBasic],
            Constants.CategoryTypeSchool => [Constants.RoleCodeSchool],
            Constants.CategoryTypeMAT => [Constants.RoleCodeMAT],
            _ => null
        };

        if (requiredRoleCodes == null)
        {
            return View("UnauthorizedOrganization");
        }

        bool hasRequiredRole = false;
        if (_Claims.Roles is IEnumerable<dynamic> rolesEnumerable)
        {
            hasRequiredRole = rolesEnumerable.Any(r =>
                requiredRoleCodes.Any(code => code.Equals(r.Code, StringComparison.OrdinalIgnoreCase)));
        }

        if (!hasRequiredRole)
        {
            return View("UnauthorizedRole");
        }

        var schoolMenuContext = await _schoolMenuContextResolver.ResolveAsync(_Claims);
        var schoolCanReviewEvidence = schoolMenuContext.ShowReviewEvidenceTiles;
        var schoolIsPartOfMat = schoolMenuContext.IsPartOfMat;

        var model = new HomeIndexViewModel
        {
            Claims = _Claims,
            SchoolMenuContext = schoolMenuContext,
            SchoolCanReviewEvidence = schoolCanReviewEvidence,
            SchoolIsPartOfMat = schoolIsPartOfMat
        };

        return View(model);
    }
    public IActionResult Accessibility() => View("Accessibility");

    public IActionResult Cookies() => View("Cookies");

    public IActionResult Guidance() => View("Guidance");

    public IActionResult Guidance_Redirect()
    {
        ViewData["Expand"] = "asylum-support";
        return View("Guidance");
    }

    public IActionResult Guidance_Basic()
    {
        ViewData["Directory"] = "yes";
        return View("Guidance");
    }

    public IActionResult FSMFormDownload() => View("FSMFormDownload");

    public IActionResult AsylumCheck() => View("Guidance_steps/Asylum_Check");

    public IActionResult BatchCheck() => View("Guidance_steps/Batch_Check");

    public IActionResult EvidenceGuidance() => View("Guidance_steps/Evidence_Guidance");

    private async Task<bool> CacheAndGetSchoolCanReviewEvidence()
    {
        var isSchoolUser = _Claims?.Roles?.Any(r =>
            string.Equals(r.Code, Constants.RoleCodeSchool, StringComparison.OrdinalIgnoreCase)) == true;

        if (!isSchoolUser)
        {
            return false;
        }

        var establishmentIdString = _Claims?.Organisation?.Urn;

        if (!int.TryParse(establishmentIdString, out var establishmentId))
        {
            return false;
        }

        var matId = await _adminGateway.GetMultiAcademyTrustIdForEstablishment(establishmentId);

        _cache.Set($"SchoolMatId_{establishmentId}", matId, TimeSpan.FromMinutes(5));
        _cache.Set($"SchoolMatMembership_{establishmentId}", matId > 0, TimeSpan.FromMinutes(5));

        if (matId > 0)
        {
            var matCacheKey = $"MatSettings_{matId}";

            if (_cache.TryGetValue(matCacheKey, out MultiAcademyTrustSettingsResponse? cachedMatSettings))
            {
                return cachedMatSettings?.AcademyCanReviewEvidence ?? false;
            }

            var matSettings = await _adminGateway.GetMultiAcademyTrustSettingsAsync(matId);

            if (matSettings != null)
            {
                _cache.Set(matCacheKey, matSettings, TimeSpan.FromMinutes(5));
            }

            return matSettings?.AcademyCanReviewEvidence ?? false;
        }

        var laCodeString = _Claims?.Organisation?.LocalAuthority?.Code;

        if (!int.TryParse(laCodeString, out var laCode))
        {
            return false;
        }

        var laCacheKey = $"LocalAuthoritySettings_{laCode}";

        if (_cache.TryGetValue(laCacheKey, out LocalAuthoritySettingsResponse? cachedSettings))
        {
            return cachedSettings?.SchoolCanReviewEvidence ?? false;
        }

        var localAuthoritySettings =
            await _localAuthoritySettingsGateway.GetLocalAuthoritySettingsAsync(laCode);

        if (localAuthoritySettings != null)
        {
            _cache.Set(laCacheKey, localAuthoritySettings, TimeSpan.FromMinutes(5));
        }

        return localAuthoritySettings?.SchoolCanReviewEvidence ?? false;
    }

    private async Task<bool> CacheAndGetSchoolIsPartOfMat()
    {
        var isSchoolUser = _Claims?.Roles?.Any(r =>
            string.Equals(r.Code, Constants.RoleCodeSchool, StringComparison.OrdinalIgnoreCase)) == true;

        if (!isSchoolUser)
        {
            return false;
        }

        var establishmentIdString = _Claims?.Organisation?.Urn;

        if (!int.TryParse(establishmentIdString, out var establishmentId))
        {
            return false;
        }

        var matIdCacheKey = $"SchoolMatId_{establishmentId}";
        var membershipCacheKey = $"SchoolMatMembership_{establishmentId}";

        if (_cache.TryGetValue(matIdCacheKey, out int cachedMatId))
        {
            var cachedIsPartOfMat = cachedMatId > 0;
            _cache.Set(membershipCacheKey, cachedIsPartOfMat, TimeSpan.FromMinutes(5));
            return cachedIsPartOfMat;
        }

        if (_cache.TryGetValue(membershipCacheKey, out bool cachedMembership))
        {
            return cachedMembership;
        }

        var matId = await _adminGateway.GetMultiAcademyTrustIdForEstablishment(establishmentId);
        var schoolIsPartOfMat = matId > 0;

        _cache.Set(matIdCacheKey, matId, TimeSpan.FromMinutes(5));
        _cache.Set(membershipCacheKey, schoolIsPartOfMat, TimeSpan.FromMinutes(5));

        return schoolIsPartOfMat;
    }
}