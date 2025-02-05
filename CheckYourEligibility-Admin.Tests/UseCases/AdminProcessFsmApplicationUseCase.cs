using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility_FrontEnd.Models;
using CheckYourEligibility_FrontEnd.Services;
using CheckYourEligibility_FrontEnd.UseCases.Admin;
using CheckYourEligibility_FrontEnd.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ModelChild = CheckYourEligibility_FrontEnd.Models.Child;

namespace CheckYourEligibility_Admin.Tests.UseCases
{
    [TestFixture]
    public class AdminProcessFSMApplicationUseCaseTests
    {
        private Mock<ILogger<AdminProcessFSMApplicationUseCase>> _loggerMock;
        private Mock<IEcsServiceParent> _parentServiceMock;
        private AdminProcessFSMApplicationUseCase _sut;
        private FsmApplication _testApplication;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<AdminProcessFSMApplicationUseCase>>();
            _parentServiceMock = new Mock<IEcsServiceParent>();
            _sut = new AdminProcessFSMApplicationUseCase(_loggerMock.Object, _parentServiceMock.Object);

            _testApplication = new FsmApplication
            {
                ParentFirstName = "Test",
                ParentLastName = "Parent",
                ParentDateOfBirth = "1990-01-01",
                ParentEmail = "test@example.com",
                Children = new Children
                {
                    ChildList = new List<ModelChild>
                    {
                        new ModelChild
                        {
                            FirstName = "Test",
                            LastName = "Child",
                            Day = "1",
                            Month = "1",
                            Year = "2015"
                        }
                    }
                }
            };
        }

        [Test]
        public async Task Execute_WithValidRequest_ShouldProcessApplicationSuccessfully()
        {
            // Arrange
            var userId = "user123";
            var email = "test@example.com";
            var urn = "12345";
            var userResponse = new UserSaveItemResponse { Data = userId };
            var applicationResponse = new ApplicationSaveItemResponse
            {
                Data = new ApplicationResponse
                {
                    ChildFirstName = "Test",
                    ChildLastName = "Child",
                    Reference = "REF123",
                    Status = "Entitled"
                }
            };

            _parentServiceMock.Setup(x => x.CreateUser(It.IsAny<UserCreateRequest>()))
                .ReturnsAsync(userResponse);
            _parentServiceMock.Setup(x => x.PostApplication_Fsm(It.IsAny<ApplicationRequest>()))
                .ReturnsAsync(applicationResponse);

            // Act
            var result = await _sut.Execute(_testApplication, email, userId, urn);

            // Assert
            result.Applications.Should().HaveCount(1);
            result.RedirectAction.Should().Be("ApplicationsRegistered");
            result.Applications.First().ChildName.Should().Be("Test Child");
            result.Applications.First().ParentName.Should().Be("Test Parent");

            _parentServiceMock.Verify(x => x.CreateUser(It.Is<UserCreateRequest>(r =>
                r.Data.Email == email && r.Data.Reference == userId)), Times.Once);
        }

        [Test]
        public async Task Execute_WithNotEntitledStatus_ShouldRedirectToAppeals()
        {
            // Arrange
            var userId = "user123";
            var email = "test@example.com";
            var urn = "12345";
            var userResponse = new UserSaveItemResponse { Data = userId };
            var applicationResponse = new ApplicationSaveItemResponse
            {
                Data = new ApplicationResponse
                {
                    Status = "NotEntitled"
                }
            };

            _parentServiceMock.Setup(x => x.CreateUser(It.IsAny<UserCreateRequest>()))
                .ReturnsAsync(userResponse);
            _parentServiceMock.Setup(x => x.PostApplication_Fsm(It.IsAny<ApplicationRequest>()))
                .ReturnsAsync(applicationResponse);

            // Act
            var result = await _sut.Execute(_testApplication, email, userId, urn);

            // Assert
            result.RedirectAction.Should().Be("AppealsRegistered");
        }

        [Test]
        public async Task Execute_WithMultipleChildren_ShouldProcessAllApplications()
        {
            // Arrange
            var userId = "user123";
            var email = "test@example.com";
            var urn = "12345";

            _testApplication.Children.ChildList.Add(new ModelChild
            {
                FirstName = "Test2",
                LastName = "Child2",
                Day = "1",
                Month = "1",
                Year = "2017"
            });

            _parentServiceMock.Setup(x => x.CreateUser(It.IsAny<UserCreateRequest>()))
                .ReturnsAsync(new UserSaveItemResponse { Data = userId });
            _parentServiceMock.Setup(x => x.PostApplication_Fsm(It.IsAny<ApplicationRequest>()))
                .ReturnsAsync(new ApplicationSaveItemResponse
                {
                    Data = new ApplicationResponse
                    {
                        Status = "Entitled"
                    }
                });

            // Act
            var result = await _sut.Execute(_testApplication, email, userId, urn);

            // Assert
            result.Applications.Should().HaveCount(2);
            _parentServiceMock.Verify(x => x.PostApplication_Fsm(It.IsAny<ApplicationRequest>()), Times.Exactly(2));
        }

        [Test]
        public async Task Execute_WhenUserCreationFails_ShouldThrowException()
        {
            // Arrange
            _parentServiceMock.Setup(x => x.CreateUser(It.IsAny<UserCreateRequest>()))
                .ReturnsAsync((UserSaveItemResponse)null);

            // Act & Assert
            await FluentActions.Invoking(() =>
                _sut.Execute(_testApplication, "test@example.com", "user123", "12345"))
                .Should().ThrowAsync<AdminProcessFSMApplicationException>()
                .WithMessage("Failed to process FSM application: Failed to create user record");
        }

        [Test]
        public async Task Execute_WhenApplicationProcessingFails_ShouldThrowException()
        {
            // Arrange
            var userId = "user123";
            _parentServiceMock.Setup(x => x.CreateUser(It.IsAny<UserCreateRequest>()))
                .ReturnsAsync(new UserSaveItemResponse { Data = userId });
            _parentServiceMock.Setup(x => x.PostApplication_Fsm(It.IsAny<ApplicationRequest>()))
                .ThrowsAsync(new Exception("API Error"));

            // Act
            Func<Task> act = () => _sut.Execute(_testApplication, "test@example.com", userId, "12345");

            // Assert
            await act.Should().ThrowAsync<AdminProcessFSMApplicationException>()
                .WithMessage("Failed to process FSM application: API Error");
        }
    }
}