using CheckYourEligibility.Admin.Infrastructure;
using CheckYourEligibility.Admin.Models;
using Microsoft.AspNetCore.Mvc;

namespace CheckYourEligibility.Admin.Controllers;

public class HomeController : BaseController
{
    private readonly IDfeSignInApiService _dfeSignInApiService;

    public HomeController(IDfeSignInApiService dfeSignInApiService)
    {
        _dfeSignInApiService = dfeSignInApiService;
    }

    public async Task<IActionResult> Index()
    {
        _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);
        
        // Check if user belongs to an allowed organization type
        var categoryName = _Claims?.Organisation?.Category?.Name;
        if (categoryName == null)
        {
            return View("UnauthorizedOrganization");
        }

        // Determine the required role based on organization type
        string? requiredRoleCode = categoryName switch
        {
            Constants.CategoryTypeLA => Constants.RoleCodeLA,
            Constants.CategoryTypeSchool => Constants.RoleCodeSchool,
            Constants.CategoryTypeMAT => Constants.RoleCodeMAT,
            _ => null
        };

        if (requiredRoleCode == null)
        {
            return View("UnauthorizedOrganization");
        }

        // Fetch roles from DfE Sign-in API
        if (_Claims.Organisation.Id != Guid.Empty && !string.IsNullOrEmpty(_Claims.User?.Id))
        {
            _Claims.Roles = await _dfeSignInApiService.GetUserRolesAsync(_Claims.User.Id, _Claims.Organisation.Id);
        }

        // Check if user has the required role for their organization type
        var hasRequiredRole = _Claims.Roles.Any(r => 
            r.Code.Equals(requiredRoleCode, StringComparison.OrdinalIgnoreCase));

        if (!hasRequiredRole)
        {
            return View("UnauthorizedRole");
        }

        return View(_Claims);
    }


    public IActionResult Privacy()
    {
        return View("Privacy");
    }

    public IActionResult Accessibility()
    {
        return View("Accessibility");
    }

    public IActionResult Cookies()
    {
        return View("Cookies");
    }

    public IActionResult Guidance()
    {
        return View("Guidance");
    }

    public IActionResult FSMFormDownload()
    {
        return View("FSMFormDownload");
    }
}