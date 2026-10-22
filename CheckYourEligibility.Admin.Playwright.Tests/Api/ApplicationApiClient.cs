using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CheckYourEligibility.Admin.Playwright.Tests.Api;

internal sealed class ApplicationApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ApiTokenProvider _tokenProvider;
    private string? _accessToken;

    public ApplicationApiClient(
        HttpClient httpClient,
        ApiTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    public async Task<ApplicationCreated> CreateApplicationAsync(
        ApplicationCreateRequest application,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "application")
        {
            Content = JsonContent.Create(
                application,
                options: JsonOptions)
        };

        await AuthoriseAsync(request, cancellationToken);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        EnsureSuccess(response, "POST application");

        var result =
            await response.Content.ReadFromJsonAsync<ApplicationCreateResponse>(
                JsonOptions,
                cancellationToken);

        if (result?.Data is null ||
            result.Data.Id == Guid.Empty ||
            string.IsNullOrWhiteSpace(result.Data.Reference))
        {
            throw new InvalidOperationException(
                "Application creation response did not contain a valid ID and reference.");
        }

        return result.Data;
    }

    public async Task DeleteApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"application/{applicationId:D}");

        await AuthoriseAsync(request, cancellationToken);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        EnsureSuccess(response, "DELETE application");
    }

    private async Task AuthoriseAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _accessToken ??=
            await _tokenProvider.GetAccessTokenAsync(cancellationToken);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    private static void EnsureSuccess(
        HttpResponseMessage response,
        string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"{operation} failed with HTTP " +
                $"{(int)response.StatusCode} ({response.ReasonPhrase}).");
        }
    }
}