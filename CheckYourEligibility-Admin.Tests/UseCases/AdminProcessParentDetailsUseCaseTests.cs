using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility_DfeSignIn.Models;
using CheckYourEligibility_FrontEnd.Models;
using CheckYourEligibility_FrontEnd.Services;
using CheckYourEligibility_FrontEnd.UseCases.Admin;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CheckYourEligibility_FrontEnd.Tests.UseCases.Admin
{
    [TestFixture]
    public class AdminProcessParentDetailsUseCaseTests
    {
        private AdminProcessParentDetailsUseCase _sut;
        private Mock<ILogger<AdminProcessParentDetailsUseCase>> _loggerMock;
        private Mock<IEcsCheckService> _checkServiceMock;
        private Mock<ISession> _sessionMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<AdminProcessParentDetailsUseCase>>();
            _checkServiceMock = new Mock<IEcsCheckService>();
            _sessionMock = new Mock<ISession>();

            _sut = new AdminProcessParentDetailsUseCase(_loggerMock.Object, _checkServiceMock.Object);
        }

        [Test]
        public void Execute_With_NinAsrSelection_None_ShouldThrowValidationException()
        {
            // Arrange
            var request = new ParentGuardian
            {
                NinAsrSelection = ParentGuardian.NinAsrSelect.None
            };

            // Act
            Func<Task> act = async () => await _sut.Execute(request, _sessionMock.Object);

            // Assert
            act.Should().ThrowAsync<AdminParentDetailsValidationException>()
                .WithMessage(JsonConvert.SerializeObject(new Dictionary<string, List<string>>
                {
                    { "NINAS", new List<string> { "Please select one option" } }
                }));
        }

        [Test]
        public async Task Execute_With_NinAsrSelection_NinSelected_ShouldStoreNINOInSession_AndCallPostCheck()
        {
            // Arrange
            var request = new ParentGuardian
            {
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john.doe@example.com",
                Day = "01",
                Month = "01",
                Year = "2000",
                NinAsrSelection = ParentGuardian.NinAsrSelect.NinSelected,
                NationalInsuranceNumber = "ab123456c"
            };

            var eligibilityResponse = new CheckEligibilityResponse
            {
                Data = new StatusValue { Status = "eligible" }
            };

            _checkServiceMock
                .Setup(x => x.PostCheck(It.IsAny<CheckEligibilityRequest_Fsm>()))
                .ReturnsAsync(eligibilityResponse);

            // Act
            var result = await _sut.Execute(request, _sessionMock.Object);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Response.Should().Be(eligibilityResponse);
            result.RedirectAction.Should().Be("Loader");

            // Verify that session values were stored correctly using the underlying Set method.
            _sessionMock.Verify(s =>
                s.Set("ParentFirstName",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == request.FirstName)),
                Times.Once);

            _sessionMock.Verify(s =>
                s.Set("ParentLastName",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == request.LastName)),
                Times.Once);

            var expectedDOB = new DateOnly(int.Parse(request.Year), int.Parse(request.Month), int.Parse(request.Day))
                                .ToString("yyyy-MM-dd");
            _sessionMock.Verify(s =>
                s.Set("ParentDOB",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == expectedDOB)),
                Times.Once);

            _sessionMock.Verify(s =>
                s.Set("ParentEmail",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == request.EmailAddress)),
                Times.Once);

            _sessionMock.Verify(s =>
                s.Set("ParentNINO",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == request.NationalInsuranceNumber.ToUpper())),
                Times.Once);

            _sessionMock.Verify(s => s.Remove("ParentNASS"), Times.Once);
        }

        [Test]
        public async Task Execute_With_NinAsrSelection_AsrnSelected_ShouldStoreNASSInSession_AndCallPostCheck()
        {
            // Arrange
            var request = new ParentGuardian
            {
                FirstName = "Jane",
                LastName = "Doe",
                EmailAddress = "jane.doe@example.com",
                Day = "02",
                Month = "02",
                Year = "1990",
                NinAsrSelection = ParentGuardian.NinAsrSelect.AsrnSelected,
                NationalAsylumSeekerServiceNumber = "AS123456"
            };

            var eligibilityResponse = new CheckEligibilityResponse
            {
                Data = new StatusValue { Status = "eligible" }
            };

            _checkServiceMock
                .Setup(x => x.PostCheck(It.IsAny<CheckEligibilityRequest_Fsm>()))
                .ReturnsAsync(eligibilityResponse);

            // Act
            var result = await _sut.Execute(request, _sessionMock.Object);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Response.Should().Be(eligibilityResponse);
            result.RedirectAction.Should().Be("Loader");

            var expectedDOB = new DateOnly(int.Parse(request.Year), int.Parse(request.Month), int.Parse(request.Day))
                                .ToString("yyyy-MM-dd");

            _sessionMock.Verify(s =>
                s.Set("ParentFirstName",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == request.FirstName)),
                Times.Once);

            _sessionMock.Verify(s =>
                s.Set("ParentLastName",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == request.LastName)),
                Times.Once);

            _sessionMock.Verify(s =>
                s.Set("ParentDOB",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == expectedDOB)),
                Times.Once);

            _sessionMock.Verify(s =>
                s.Set("ParentEmail",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == request.EmailAddress)),
                Times.Once);

            _sessionMock.Verify(s =>
                s.Set("ParentNASS",
                    It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == request.NationalAsylumSeekerServiceNumber)),
                Times.Once);

            _sessionMock.Verify(s => s.Remove("ParentNINO"), Times.Once);
        }

        [Test]
        public void Execute_When_PostCheckThrowsException_ShouldThrowValidationException()
        {
            // Arrange
            var request = new ParentGuardian
            {
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john.doe@example.com",
                Day = "01",
                Month = "01",
                Year = "2000",
                NinAsrSelection = ParentGuardian.NinAsrSelect.NinSelected,
                NationalInsuranceNumber = "ab123456c"
            };

            _checkServiceMock
                .Setup(x => x.PostCheck(It.IsAny<CheckEligibilityRequest_Fsm>()))
                .ThrowsAsync(new Exception("Service failure"));

            // Act
            Func<Task> act = async () => await _sut.Execute(request, _sessionMock.Object);

            // Assert
            act.Should().ThrowAsync<AdminParentDetailsValidationException>()
                .WithMessage("Failed to process eligibility check");
        }
    }
}
