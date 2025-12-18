using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using CheckYourEligibility.Admin.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.Admin.Tests.Infrastructure;

/// <summary>
/// Test handler that captures HTTP requests for verification.
/// </summary>
internal class TestHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? CapturedRequest { get; private set; }
    public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CapturedRequest = request;
        
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(ResponseFactory?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}

[TestFixture]
internal class DfeSignInApiServiceTests
{
    private Mock<IDfeSignInConfiguration> _mockConfiguration;
    private Mock<ILogger<DfeSignInApiService>> _mockLogger;

    private const string TestClientId = "TestClientId";
    private const string TestApiSecret = "test-api-secret-key-that-is-long-enough-for-hmac-sha256";
    private const string TestApiProxyUrl = "https://test-api.signin.education.gov.uk";

    [SetUp]
    public void SetUp()
    {
        _mockConfiguration = new Mock<IDfeSignInConfiguration>();
        _mockConfiguration.Setup(c => c.ClientId).Returns(TestClientId);
        _mockConfiguration.Setup(c => c.APIServiceSecret).Returns(TestApiSecret);
        _mockConfiguration.Setup(c => c.APIServiceProxyUrl).Returns(TestApiProxyUrl);

        _mockLogger = new Mock<ILogger<DfeSignInApiService>>();
    }

    [Test]
    public async Task GetUserRolesAsync_ReturnsRoles_WhenApiReturnsValidResponse()
    {
        // Arrange
        var userId = "test-user-id";
        var organisationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var responseContent = @"{
            ""userId"": """ + userId + @""",
            ""serviceId"": """ + TestClientId + @""",
            ""organisationId"": """ + organisationId + @""",
            ""roles"": [
                {
                    ""id"": """ + roleId + @""",
                    ""name"": ""FSM - Local Authority Role"",
                    ""code"": ""FSM_LA_Role"",
                    ""numericId"": ""123"",
                    ""status"": {
                        ""id"": 1
                    }
                }
            ],
            ""identifiers"": []
        }";

        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            }
        };

        var httpClient = new HttpClient(handler);
        var service = new DfeSignInApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var roles = await service.GetUserRolesAsync(userId, organisationId);

        // Assert
        roles.Should().HaveCount(1);
        roles[0].Name.Should().Be("FSM - Local Authority Role");
        roles[0].Code.Should().Be("FSM_LA_Role");
    }

    [Test]
    public async Task GetUserRolesAsync_ReturnsEmptyList_WhenApiConfigurationIsMissing()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.APIServiceProxyUrl).Returns(string.Empty);

        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var service = new DfeSignInApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var roles = await service.GetUserRolesAsync("user-id", Guid.NewGuid());

        // Assert
        roles.Should().BeEmpty();
    }

    [Test]
    public async Task GetUserRolesAsync_ReturnsEmptyList_WhenApiReturnsBadRequest()
    {
        // Arrange
        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        };

        var httpClient = new HttpClient(handler);
        var service = new DfeSignInApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var roles = await service.GetUserRolesAsync("user-id", Guid.NewGuid());

        // Assert
        roles.Should().BeEmpty();
    }

    [Test]
    public async Task GetUserRolesAsync_ReturnsEmptyList_WhenExceptionIsThrown()
    {
        // Arrange
        var handler = new TestHttpMessageHandler
        {
            ExceptionToThrow = new HttpRequestException("Network error")
        };

        var httpClient = new HttpClient(handler);
        var service = new DfeSignInApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var roles = await service.GetUserRolesAsync("user-id", Guid.NewGuid());

        // Assert
        roles.Should().BeEmpty();
    }

    [Test]
    public async Task GetUserRolesAsync_SendsCorrectRequestUrl()
    {
        // Arrange
        var userId = "test-user-id";
        var organisationId = Guid.NewGuid();

        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""roles"": []}", Encoding.UTF8, "application/json")
            }
        };

        var httpClient = new HttpClient(handler);
        var service = new DfeSignInApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        await service.GetUserRolesAsync(userId, organisationId);

        // Assert
        var expectedUrl = $"{TestApiProxyUrl}/services/{TestClientId}/organisations/{organisationId}/users/{userId}";
        handler.CapturedRequest?.RequestUri?.ToString().Should().Be(expectedUrl);
    }

    [Test]
    public async Task GetUserRolesAsync_SendsAuthorizationHeader()
    {
        // Arrange
        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""roles"": []}", Encoding.UTF8, "application/json")
            }
        };

        var httpClient = new HttpClient(handler);
        var service = new DfeSignInApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        await service.GetUserRolesAsync("user-id", Guid.NewGuid());

        // Assert
        handler.CapturedRequest?.Headers.Authorization.Should().NotBeNull();
        handler.CapturedRequest?.Headers.Authorization?.Scheme.Should().Be("Bearer");
        
        // Verify the token is a valid JWT
        var token = handler.CapturedRequest?.Headers.Authorization?.Parameter;
        token.Should().NotBeNullOrEmpty();
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwt = jwtHandler.ReadJwtToken(token);
        jwt.Issuer.Should().Be(TestClientId);
        jwt.Audiences.Should().Contain("signin.education.gov.uk");
    }

    [Test]
    public async Task GetUserRolesAsync_ReturnsEmptyList_WhenNoRolesInResponse()
    {
        // Arrange
        var responseContent = @"{
            ""userId"": ""user-id"",
            ""serviceId"": ""service-id"",
            ""organisationId"": ""org-id"",
            ""roles"": [],
            ""identifiers"": []
        }";

        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            }
        };

        var httpClient = new HttpClient(handler);
        var service = new DfeSignInApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var roles = await service.GetUserRolesAsync("user-id", Guid.NewGuid());

        // Assert
        roles.Should().BeEmpty();
    }
}
