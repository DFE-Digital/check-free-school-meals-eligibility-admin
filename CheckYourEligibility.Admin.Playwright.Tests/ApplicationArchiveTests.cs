using CheckYourEligibility.Admin.Playwright.Tests.Api;
using CheckYourEligibility.Admin.Playwright.Tests.Configuration;
using CheckYourEligibility.Admin.Playwright.Tests.TestData;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace CheckYourEligibility.Admin.Playwright.Tests;

[TestFixture]
[Explicit("Requires local Admin, API and DfE test credentials.")]
[NonParallelizable]
public class ApplicationArchiveTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        };
    }

    [Test]
    public async Task SchoolUserCanArchiveApplicationCreatedThroughApi()
    {
        var configuration = TestConfiguration.Load();

        if (!configuration.AdminBaseUrl.IsLoopback ||
            !configuration.ApiBaseUrl.IsLoopback)
        {
            throw new InvalidOperationException(
                "This proof-of-concept test may only run against local services.");
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = configuration.ApiBaseUrl
        };

        var tokenProvider =
            new ApiTokenProvider(httpClient, configuration);

        var applicationClient =
            new ApplicationApiClient(httpClient, tokenProvider);

        var applicationRequest = ApplicationTestDataFactory.Create();
        ApplicationCreated? createdApplication = null;

        try
        {
            createdApplication =
                await applicationClient.CreateApplicationAsync(
                    applicationRequest);

            var reference = createdApplication.Reference!;
            var parentName =
                $"{applicationRequest.Data.ParentFirstName} " +
                applicationRequest.Data.ParentLastName;

            await SignInAsTelfordParkSchoolAsync(configuration);

            var applicationDetailUri = new Uri(
                 configuration.AdminBaseUrl,
                 $"Application/ApplicationDetail?id={createdApplication.Id:D}");

            await Page.GotoAsync(applicationDetailUri.AbsoluteUri);

            await Expect(
                    Page.GetByRole(
                        AriaRole.Heading,
                        new() { Name = parentName, Exact = true }))
                .ToBeVisibleAsync();

            await Page
                .GetByRole(
                    AriaRole.Link,
                    new() { Name = "Archive record", Exact = true })
                .ClickAsync();

            await Expect(
                    Page.GetByRole(
                        AriaRole.Heading,
                        new()
                        {
                            Name = $"Archive record for {parentName}?",
                            Exact = true
                        }))
                .ToBeVisibleAsync();

            await Page
                .GetByRole(
                    AriaRole.Link,
                    new() { Name = "Archive now", Exact = true })
                .ClickAsync();

            await Expect(
                    Page.GetByRole(
                        AriaRole.Heading,
                        new()
                        {
                            Name =
                                $"Record for {parentName} has been archived",
                            Exact = true
                        }))
                .ToBeVisibleAsync();

            await Expect(
                    Page.Locator(".govuk-panel__body")
                        .Filter(new() { HasText = reference }))
                .ToContainTextAsync(reference);
        }
        finally
        {
            if (createdApplication is not null)
            {
                await applicationClient.DeleteApplicationAsync(
                    createdApplication.Id);
            }
        }
    }

    private async Task SignInAsTelfordParkSchoolAsync(
    TestConfiguration configuration)
    {
        var homeUri = new Uri(configuration.AdminBaseUrl, "home");

        await Page.GotoAsync(homeUri.AbsoluteUri);

        await Page.Locator("#username")
            .FillAsync(configuration.DfeAdminEmailAddress);

        await Page.Locator("button[type='submit']").ClickAsync();

        await Page.Locator("#password")
            .FillAsync(configuration.DfeAdminPassword);

        await Page.Locator("button[type='submit']").ClickAsync();

        var schoolOption = Page
            .Locator(".govuk-radios__item")
            .Filter(new() { HasText = "The Telford Park School" })
            .Locator("input[type='radio']");

        await schoolOption.CheckAsync();

        await Page
            .GetByRole(
                AriaRole.Button,
                new() { Name = "Continue", Exact = true })
            .ClickAsync();

        await Page.WaitForURLAsync("**/home");
    }
}