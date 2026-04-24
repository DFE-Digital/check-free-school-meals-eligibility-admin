using CheckYourEligibility.Admin.Domain.DfeSignIn;
using CheckYourEligibility.Admin.Models;

namespace CheckYourEligibility.Admin.ViewModels;

public sealed class HomeIndexViewModel
{
    public required DfeClaims Claims { get; init; }
    public bool SchoolCanReviewEvidence { get; init; }
    public bool SchoolIsPartOfMat { get; init; }
    public SchoolMenuContext? SchoolMenuContext { get; init; }
}