using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Domain.Enums;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CheckYourEligibility.Admin.Tests.UseCases;

[TestFixture]
public class SendNotificationUseCaseTests
{
    private Mock<ILogger<SendNotificationUseCase>> _loggerMock = null!;
    private Mock<INotificationGateway> _notificationGatewayMock = null!;
    private SendNotificationUseCase _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<SendNotificationUseCase>>();
        _notificationGatewayMock = new Mock<INotificationGateway>(MockBehavior.Strict);
        _sut = new SendNotificationUseCase(_loggerMock.Object, _notificationGatewayMock.Object);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n")]
    public async Task Execute_WithMissingEmail_SkipsNotificationWithoutLoggingSuccessOrError(string? email)
    {
        var request = new NotificationRequest
        {
            Data = new NotificationRequestData
            {
                Email = email,
                Type = NotificationType.ParentApplicationSuccessful
            }
        };

        var result = await _sut.Execute(request);

        result.Should().BeNull();
        _notificationGatewayMock.Verify(x => x.SendNotification(It.IsAny<NotificationRequest>()), Times.Never);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, type) =>
                value.ToString() == "Notification skipped because no recipient email was supplied"),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Execute_WithEmail_SendsUnchangedRequestAndReturnsResponse()
    {
        var request = new NotificationRequest
        {
            Data = new NotificationRequestData
            {
                Email = "parent@example.com",
                Type = NotificationType.ParentApplicationSuccessful,
                Personalisation = new Dictionary<string, object> { { "reference", "REF12345" } }
            }
        };
        var response = new NotificationItemResponse();
        _notificationGatewayMock.Setup(x => x.SendNotification(request)).ReturnsAsync(response);

        var result = await _sut.Execute(request);

        result.Should().BeSameAs(response);
        _notificationGatewayMock.Verify(x => x.SendNotification(request), Times.Once);
    }

    [Test]
    public async Task Execute_WhenGatewayFails_LogsAndRethrowsFailure()
    {
        var request = new NotificationRequest
        {
            Data = new NotificationRequestData { Email = "parent@example.com" }
        };
        var exception = new Exception("Notification failed");
        _notificationGatewayMock.Setup(x => x.SendNotification(request)).ThrowsAsync(exception);

        Func<Task> act = async () => await _sut.Execute(request);

        (await act.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(exception);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, type) => value.ToString() == "Failed to send notification"),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
