using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CheckYourEligibility.Admin.Playwright.Tests.Configuration;

namespace CheckYourEligibility.Admin.Playwright.Tests.Api;

internal sealed class ApiTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly TestConfiguration _configuration;

    public ApiTokenProvider(
        HttpClient httpClient,
        TestConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["client_id"] =
                    $"{_configuration.ApiAuthorisationUsername}:" +
                    _configuration.DfeAdminEmailAddress,
                ["client_secret"] =
                    _configuration.ApiAuthorisationPassword,
                ["scope"] =
                    _configuration.ApiAuthorisationScope
            });

        using var response = await _httpClient.PostAsync(
            "oauth2/token",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"API token request failed with HTTP " +
                $"{(int)response.StatusCode} ({response.ReasonPhrase}).");
        }

        var tokenResponse =
            await response.Content.ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
        {
            throw new InvalidOperationException(
                "API token response did not contain an access token.");
        }

        return tokenResponse.AccessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }
}