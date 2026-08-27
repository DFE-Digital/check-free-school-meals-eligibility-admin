using CheckYourEligibility.Admin.Playwright.Tests.Api;

namespace CheckYourEligibility.Admin.Playwright.Tests.TestData;

internal static class ApplicationTestDataFactory
{
    private const int TelfordParkSchoolUrn = 150716;

    public static ApplicationCreateRequest Create()
    {
        var suffix = CreateAlphabeticSuffix(8);

        return new ApplicationCreateRequest
        {
            Data = new ApplicationCreateData
            {
                Establishment = TelfordParkSchoolUrn,
                ParentFirstName = $"Playwright{suffix}",
                ParentLastName = "Tester",
                ParentEmail =
                    $"playwright.{Guid.NewGuid():N}@example.com",
                ParentNationalInsuranceNumber = "PN668767B",
                ParentDateOfBirth = "1990-01-01",
                ChildFirstName = $"Browser{suffix}",
                ChildLastName = "Tester",
                ChildDateOfBirth = "2016-01-01"
            }
        };
    }

    private static string CreateAlphabeticSuffix(int length)
    {
        return string.Concat(
            Enumerable.Range(0, length)
                .Select(_ => (char)('A' + Random.Shared.Next(26))));
    }
}