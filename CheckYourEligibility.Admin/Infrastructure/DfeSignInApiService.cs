using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CheckYourEligibility.Admin.Domain.DfeSignIn;
using Microsoft.IdentityModel.Tokens;

namespace CheckYourEligibility.Admin.Infrastructure;

/// <summary>
///     Service for interacting with the DfE Sign-in public API.
/// </summary>
public class DfeSignInApiService : IDfeSignInApiService
{
    private readonly HttpClient _httpClient;
    private readonly IDfeSignInConfiguration _configuration;
    private readonly ILogger<DfeSignInApiService> _logger;

    public DfeSignInApiService(
        HttpClient httpClient,
        IDfeSignInConfiguration configuration,
        ILogger<DfeSignInApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IList<Role>> GetUserRolesAsync(string userId, Guid organisationId)
    {
        var roles = new List<Role>();

        try
        {
            if (string.IsNullOrEmpty(_configuration.APIServiceProxyUrl) ||
                string.IsNullOrEmpty(_configuration.APIServiceSecret))
            {
                _logger.LogWarning("DfE Sign-in API configuration is missing. Skipping role fetch.");
                return roles;
            }

            var token = GenerateApiToken();
            var requestUrl = $"{_configuration.APIServiceProxyUrl}/services/{_configuration.ClientId}/organisations/{organisationId}/users/{userId}";

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get user roles from DfE Sign-in API. Status: {StatusCode}",
                    response.StatusCode);
                return roles;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userAccessResponse = JsonSerializer.Deserialize<DfeSignInUserAccessResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userAccessResponse?.Roles != null)
            {
                roles.AddRange(userAccessResponse.Roles);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user roles from DfE Sign-in API");
        }

        return roles;
    }

    private string GenerateApiToken()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.APIServiceSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var header = new JwtHeader(credentials);
        var payload = new JwtPayload(
            issuer: _configuration.ClientId,
            audience: "signin.education.gov.uk",
            claims: new List<Claim>(),
            notBefore: null,
            expires: DateTime.UtcNow.AddMinutes(5),
            issuedAt: DateTime.UtcNow
        );

        var token = new JwtSecurityToken(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
///     Response from the DfE Sign-in API for user access information.
/// </summary>
public class DfeSignInUserAccessResponse
{
    public string UserId { get; set; } = null!;
    public string ServiceId { get; set; } = null!;
    public string OrganisationId { get; set; } = null!;
    public IList<Role> Roles { get; set; } = new List<Role>();
    public IList<object> Identifiers { get; set; } = new List<object>();
}
