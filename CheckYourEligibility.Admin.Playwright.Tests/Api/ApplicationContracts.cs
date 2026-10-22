namespace CheckYourEligibility.Admin.Playwright.Tests.Api;

internal sealed class ApplicationCreateRequest
{
    public required ApplicationCreateData Data { get; init; }
}

internal sealed class ApplicationCreateData
{
    public string Type { get; init; } = "FreeSchoolMeals";

    public required int Establishment { get; init; }

    public required string ParentFirstName { get; init; }

    public required string ParentLastName { get; init; }

    public required string ParentEmail { get; init; }

    public string? ParentNationalInsuranceNumber { get; init; }

    public string? ParentNationalAsylumSeekerServiceNumber { get; init; }

    public required string ParentDateOfBirth { get; init; }

    public required string ChildFirstName { get; init; }

    public required string ChildLastName { get; init; }

    public required string ChildDateOfBirth { get; init; }

    public string? UserId { get; init; }
}

internal sealed class ApplicationCreateResponse
{
    public ApplicationCreated? Data { get; init; }
}

internal sealed class ApplicationCreated
{
    public Guid Id { get; init; }

    public string? Reference { get; init; }
}