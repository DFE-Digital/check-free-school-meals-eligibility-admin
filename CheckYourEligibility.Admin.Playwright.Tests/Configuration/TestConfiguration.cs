namespace CheckYourEligibility.Admin.Playwright.Tests.Configuration;

internal sealed class TestConfiguration
{
    private TestConfiguration(
        Uri adminBaseUrl,
        Uri apiBaseUrl,
        string dfeAdminEmailAddress,
        string dfeAdminPassword,
        string apiAuthorisationUsername,
        string apiAuthorisationPassword,
        string apiAuthorisationScope)
    {
        AdminBaseUrl = adminBaseUrl;
        ApiBaseUrl = apiBaseUrl;
        DfeAdminEmailAddress = dfeAdminEmailAddress;
        DfeAdminPassword = dfeAdminPassword;
        ApiAuthorisationUsername = apiAuthorisationUsername;
        ApiAuthorisationPassword = apiAuthorisationPassword;
        ApiAuthorisationScope = apiAuthorisationScope;
    }

    public Uri AdminBaseUrl { get; }

    public Uri ApiBaseUrl { get; }

    public string DfeAdminEmailAddress { get; }

    public string DfeAdminPassword { get; }

    public string ApiAuthorisationUsername { get; }

    public string ApiAuthorisationPassword { get; }

    public string ApiAuthorisationScope { get; }


    public static TestConfiguration Load()
    {
        return new TestConfiguration(
            GetRequiredUri("FSM_ADMIN_BASE_URL"),
            GetRequiredUri("FSM_API_BASE_URL"),
            GetRequiredValue("DFE_ADMIN_EMAIL_ADDRESS"),
            GetRequiredValue("DFE_ADMIN_PASSWORD"),
            GetRequiredValue("FSM_API_AUTHORISATION_USERNAME"),
            GetRequiredValue("FSM_API_AUTHORISATION_PASSWORD"),
            GetRequiredValue("FSM_API_AUTHORISATION_SCOPE"));
    }

    private static string GetRequiredValue(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required environment variable '{variableName}' is not configured.");
        }

        return value;
    }

    private static Uri GetRequiredUri(string variableName)
    {
        var value = GetRequiredValue(variableName);

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Environment variable '{variableName}' must be an absolute HTTP or HTTPS URL.");
        }

        return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
    }
}