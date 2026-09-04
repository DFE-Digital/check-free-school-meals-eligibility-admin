using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.Models;
using CheckYourEligibility.Admin.UseCases;
using FluentAssertions;
using Moq;

namespace CheckYourEligibility.Admin.Tests.UseCases;

[TestFixture]
public class PerformEligibilityCheckUseCaseTests
{
    [SetUp]
    public void SetUp()
    {
        _checkGatewayMock = new Mock<ICheckGateway>();
        _sut = new PerformEligibilityCheckUseCase(_checkGatewayMock.Object);

        _parent = new ParentGuardian
        {
            FirstName = "John",
            LastName = "Doe",
            EmailAddress = "a@b.c",
            Day = "01",
            Month = "01",
            Year = "1980",
            NationalInsuranceNumber = "AB123456C"
        };

        _eligibilityResponse = new CheckEligibilityResponse
        {
            Data = new StatusValue { Status = "queuedForProcessing" },
            Links = new CheckEligibilityResponseLinks { Get_EligibilityCheck = "test-link" }
        };
    }

    private PerformEligibilityCheckUseCase _sut;
    private Mock<ICheckGateway> _checkGatewayMock;

    private ParentGuardian _parent;
    private CheckEligibilityResponse _eligibilityResponse;

    [Test]
    public async Task Execute_WithValidParent_ShouldReturnValidResponse()
    {
        // Arrange
        _checkGatewayMock.Setup(s => s.PostCheck(It.IsAny<CheckEligibilityRequest_Enhanced>()))
            .ReturnsAsync(_eligibilityResponse);

        // Act
        var response = await _sut.Execute(_parent);

        // Assert
        response.Should().BeEquivalentTo(_eligibilityResponse);
    }

    [Test]
    public async Task Execute_WhenApiThrowsException_ShouldThrow()
    {
        // Arrange
        _checkGatewayMock.Setup(s => s.PostCheck(It.IsAny<CheckEligibilityRequest_Enhanced>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        Func<Task> act = async () => await _sut.Execute(_parent);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("API Error");
    }
}