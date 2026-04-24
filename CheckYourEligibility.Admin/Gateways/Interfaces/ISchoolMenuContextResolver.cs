using CheckYourEligibility.Admin.Domain.DfeSignIn;
using CheckYourEligibility.Admin.Models;

namespace CheckYourEligibility.Admin.Gateways.Interfaces;

public interface ISchoolMenuContextResolver
{
    Task<SchoolMenuContext> ResolveAsync(DfeClaims claims);
}