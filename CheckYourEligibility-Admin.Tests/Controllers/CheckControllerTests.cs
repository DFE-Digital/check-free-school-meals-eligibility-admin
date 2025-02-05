using AutoFixture;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.TestBase;
using CheckYourEligibility_FrontEnd.Controllers;
using CheckYourEligibility_FrontEnd.Models;
using CheckYourEligibility_FrontEnd.Services;
using CheckYourEligibility_FrontEnd.UseCases.Admin;
using CheckYourEligibility_FrontEnd.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Child = CheckYourEligibility_FrontEnd.Models.Child;
using School = CheckYourEligibility_FrontEnd.Models.School;
using static CheckYourEligibility_FrontEnd.Models.Constants;

namespace CheckYourEligibility_Admin.Tests.Controllers
{
    [TestFixture]
    public class CheckControllerTests : TestBase
    {
        private ILogger<CheckController> _loggerMock;
        private Mock<IEcsServiceParent> _parentServiceMock;
        private Mock<IEcsCheckService> _checkServiceMock;
        private Mock<IAdminLoadParentDetailsUseCase> _adminLoadParentDetailsUseCaseMock;
        private Mock<IAdminProcessParentDetailsUseCase> _adminProcessParentDetailsUseCaseMock;
        private Mock<IAdminProcessFSMApplicationUseCase> _adminProcessFSMApplicationUseCaseMock;
        private CheckController _sut;
        private IFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
            _parentServiceMock = new Mock<IEcsServiceParent>();
            _checkServiceMock = new Mock<IEcsCheckService>();
            _loggerMock = Mock.Of<ILogger<CheckController>>();
            _adminLoadParentDetailsUseCaseMock = new Mock<IAdminLoadParentDetailsUseCase>();
            _adminProcessParentDetailsUseCaseMock = new Mock<IAdminProcessParentDetailsUseCase>();
            _adminProcessFSMApplicationUseCaseMock = new Mock<IAdminProcessFSMApplicationUseCase>();

            _sut = new CheckController(
                _loggerMock,
                _parentServiceMock.Object,
                _checkServiceMock.Object,
                _configMock.Object,
                _adminLoadParentDetailsUseCaseMock.Object,
                _adminProcessParentDetailsUseCaseMock.Object,
                _adminProcessFSMApplicationUseCaseMock.Object
            );

            base.SetUp();

            _sut.TempData = _tempData;
            _sut.ControllerContext.HttpContext = _httpContext.Object;
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public async Task Given_Enter_Details_When_NIN_ASR_ValidationErrors_Exist_Should_Handle_Special_Case()
        {
            // Arrange
            var errors = new Dictionary<string, List<string>>
            {
                { "NationalInsuranceNumber", new List<string> { "Please select one option" } },
                { "NationalAsylumSeekerServiceNumber", new List<string> { "Please select one option" } }
            };
            var errorsJson = JsonConvert.SerializeObject(errors);
            _sut.TempData["Errors"] = errorsJson;

            var expectedViewModel = new AdminLoadParentDetailsViewModel
            {
                ValidationErrors = new Dictionary<string, List<string>>
                {
                    { "NINAS", new List<string> { "Please select one option" } }
                }
            };

            _adminLoadParentDetailsUseCaseMock
                .Setup(x => x.ExecuteAsync(null, errorsJson))
                .ReturnsAsync(expectedViewModel);

            // Act
            var result = await _sut.Enter_Details();

            // Assert
            _sut.ModelState["NINAS"].Errors.Should().ContainSingle()
                .Which.ErrorMessage.Should().Be("Please select one option");
            _sut.ModelState.Should().NotContainKey("NationalInsuranceNumber");
            _sut.ModelState.Should().NotContainKey("NationalAsylumSeekerServiceNumber");
        }

        [Test]
        public async Task Given_Enter_Details_Should_Load_EnterDetailsPage()
        {
            // Arrange
            var expectedViewModel = new AdminLoadParentDetailsViewModel();
            _adminLoadParentDetailsUseCaseMock
                .Setup(x => x.ExecuteAsync(null, null))
                .ReturnsAsync(expectedViewModel);

            // Act
            var result = await _sut.Enter_Details();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Model.Should().BeNull();
            _adminLoadParentDetailsUseCaseMock.Verify(x => x.ExecuteAsync(null, null), Times.Once);
        }

        [Test]
        public async Task Given_Enter_Details_When_TempData_Contains_ParentDetails_Should_Load_Parent_Data()
        {
            // Arrange
            var parent = _fixture.Create<ParentGuardian>();
            var parentJson = JsonConvert.SerializeObject(parent);
            _sut.TempData["ParentDetails"] = parentJson;

            var expectedViewModel = new AdminLoadParentDetailsViewModel
            {
                Parent = parent
            };

            _adminLoadParentDetailsUseCaseMock
                .Setup(x => x.ExecuteAsync(parentJson, null))
                .ReturnsAsync(expectedViewModel);

            // Act
            var result = await _sut.Enter_Details();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.Model.Should().BeEquivalentTo(parent);
            _adminLoadParentDetailsUseCaseMock.Verify(
                x => x.ExecuteAsync(parentJson, null),
                Times.Once);
        }

        [Test]
        public async Task Given_Enter_Details_When_ValidationErrors_Exist_Should_Add_To_ModelState()
        {
            // Arrange
            var errors = new Dictionary<string, List<string>>
            {
                { "Field1", new List<string> { "Error1" } }
            };
            var errorsJson = JsonConvert.SerializeObject(errors);
            _sut.TempData["Errors"] = errorsJson;

            var expectedViewModel = new AdminLoadParentDetailsViewModel
            {
                ValidationErrors = errors
            };

            _adminLoadParentDetailsUseCaseMock
                .Setup(x => x.ExecuteAsync(null, errorsJson))
                .ReturnsAsync(expectedViewModel);

            // Act
            var result = await _sut.Enter_Details();

            // Assert
            _sut.ModelState["Field1"].Errors.Should().ContainSingle()
                .Which.ErrorMessage.Should().Be("Error1");
            _adminLoadParentDetailsUseCaseMock.Verify(
                x => x.ExecuteAsync(null, errorsJson),
                Times.Once);
        }

        [Test]
        public async Task Given_Enter_Details_Post_When_ModelStateInvalid_ShouldStoreTempDataAndRedirect()
        {
            // Arrange
            var request = _fixture.Create<ParentGuardian>();
            _sut.ModelState.AddModelError("test", "error");

            // Act
            var result = await _sut.Enter_Details(request);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Enter_Details");

            _sut.TempData["ParentDetails"].Should().Be(JsonConvert.SerializeObject(request));
            _sut.TempData["Errors"].Should().NotBeNull();
        }

        [Test]
        public async Task Given_Enter_Details_Post_When_Valid_ShouldProcessAndRedirect()
        {
            // Arrange
            var request = _fixture.Create<ParentGuardian>();
            var response = _fixture.Create<CheckEligibilityResponse>();

            _adminProcessParentDetailsUseCaseMock
                .Setup(x => x.Execute(request, _httpContext.Object.Session))
                .ReturnsAsync((true, response, "Loader"));

            // Act
            var result = await _sut.Enter_Details(request);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Loader");

            _sut.TempData["Response"].Should().Be(JsonConvert.SerializeObject(response));
        }

        [Test]
        public async Task Given_Enter_Details_Post_When_ValidationException_ShouldRedirectWithErrors()
        {
            // Arrange
            var request = _fixture.Create<ParentGuardian>();
            var errorMessage = "{\"NINAS\":[\"Please select one option\"]}";

            _adminProcessParentDetailsUseCaseMock
                .Setup(x => x.Execute(request, _httpContext.Object.Session))
                .ThrowsAsync(new AdminParentDetailsValidationException(errorMessage));

            // Act
            var result = await _sut.Enter_Details(request);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Enter_Details");

            _sut.TempData["ParentDetails"].Should().Be(JsonConvert.SerializeObject(request));
            _sut.TempData["Errors"].Should().Be(errorMessage);
        }

        [Test]
        public void Enter_Child_Details_TempData_IsChildAddOrRemove_False_Returns_View_With_Empty_ChildList()
        {
            _tempData["IsChildAddOrRemove"] = false;

            var result = _sut.Enter_Child_Details();

            var viewResult = result as ViewResult;
            var model = viewResult.Model as Children;
            model.ChildList.Should().NotBeNullOrEmpty();
            model.ChildList[0].FirstName.Should().BeNull();
            model.ChildList[0].LastName.Should().BeNull();
        }

        [Test]
        public void Enter_Child_Details_TempData_IsChildAddOrRemove_True_Returns_View_With_ChildList_From_TempData()
        {
            _tempData["IsChildAddOrRemove"] = true;

            var children = _fixture.Create<List<Child>>();
            var childListJson = JsonConvert.SerializeObject(children);

            _tempData["ChildList"] = childListJson;

            var result = _sut.Enter_Child_Details();

            var viewResult = result as ViewResult;
            var model = viewResult.Model as Children;
            model.ChildList.Should().NotBeNullOrEmpty();
            model.ChildList[0].FirstName.Should().Be(children[0].FirstName);
            model.ChildList[0].LastName.Should().Be(children[0].LastName);
        }

        [Test]
        public void Given_Enter_Child_Details_When_TempData_IsRedirect_IsTrue_Returns_View_With_Request()
        {
            // Arrange
            _tempData["FsmApplication"] = _fixture.Create<FsmApplication>();
            _tempData["IsRedirect"] = true;
            var request = _fixture.Create<Children>();

            // Act
            var result = _sut.Enter_Child_Details(request);

            // Assert
            var viewResult = result as ViewResult;
            viewResult.ViewName.Should().Be("Enter_Child_Details");
            viewResult.Model.Should().BeSameAs(request);
            _sut.ModelState.IsValid.Should().BeTrue();
        }

        [Test]
        public void Given_Enter_Child_Details_When_ModelState_IsNotValid_Returns_SameView_With_Request()
        {
            // Arrange
            _sut.ModelState.AddModelError("Error Key", "Error Message");
            var request = _fixture.Create<Children>();

            // Act
            var result = _sut.Enter_Child_Details(request);

            // Assert
            var viewResult = result as ViewResult;
            viewResult.ViewName.Should().Be("Enter_Child_Details");
            viewResult.Model.Should().BeSameAs(request);
            _sut.ModelState.IsValid.Should().BeFalse();
        }

        [Test]
        public void Given_Enter_Child_Details_When_ModelState_IsValid_Returns_Check_Answers_View_With_FsmApplication()
        {
            // Arrange
            var request = _fixture.Create<Children>();

            // Act
            var result = _sut.Enter_Child_Details(request);

            // Assert
            var viewResult = result as ViewResult;
            viewResult.ViewName.Should().Be("Check_Answers");
            var model = viewResult.Model as FsmApplication;
            model.Children.Should().BeSameAs(request);
            _sut.ModelState.IsValid.Should().BeTrue();
        }

        [Test]
        public void Given_Add_Child_When_ChildList_ContainsLessThan99Children_ItAddsNewChild_And_Redirects_To_Enter_Child_Details_Page()
        {
            // Arrange
            var request = _fixture.Create<Children>();
            var children = _fixture.CreateMany<Child>(5).ToList();
            request.ChildList.Clear();
            request.ChildList = children;

            // Act
            var result = _sut.Add_Child(request);

            // Assert
            var redirectToActionResult = result as RedirectToActionResult;
            redirectToActionResult.ActionName.Should().Be("Enter_Child_Details");
            _sut.TempData["ChildList"].Should().NotBeNull();
            var convertedTempData = JsonConvert.DeserializeObject<List<Child>>(_sut.TempData["ChildList"] as string);
            convertedTempData.Count.Should().Be(6);
        }

        [Test]
        public void Given_Add_Child_When_ChildList_ContainsExactly99Children_ItDoesNotAddNewChild_And_Redirects_To_Enter_Child_Details_Page()
        {
            var request = _fixture.Create<Children>();
            var children = _fixture.CreateMany<Child>(99).ToList();
            request.ChildList.Clear();
            request.ChildList = children;

            var result = _sut.Add_Child(request);

            var redirectToActionResult = result as RedirectToActionResult;
            redirectToActionResult.ActionName.Should().Be("Enter_Child_Details");
            _sut.TempData["ChildList"].Should().BeNull();
        }

        [Test]
        public void Given_Remove_Child_When_ValidIndexIsProvided_The_ChildIsRemovedFrom_ChildList_AtGivenIndex()
        {
            // Arrange
            var request = _fixture.Create<Children>();
            var childListCount = request.ChildList.Count();

            // Act
            var result = _sut.Remove_Child(request, 1);

            // Assert
            _sut.TempData["ChildList"].Should().NotBeNull();
            var convertedTempData = JsonConvert.DeserializeObject<List<Child>>(_sut.TempData["ChildList"] as string);
            convertedTempData.Count.Should().Be(childListCount - 1); // has 1 less
        }

        [Test]
        public void Given_Remove_Child_When_InvalidIndexIsProvided_NoChild_IsRemovedFrom_ChildList_AtGivenIndex_And_ArgumentOutOfRangeException_IsThrown()
        {
            // Arrange
            var request = _fixture.Create<Children>();
            var childListCount = request.ChildList.Count();
            var nonExistantIndex = childListCount + 1;

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Remove_Child(request, nonExistantIndex));
            _sut.TempData["ChildList"].Should().BeNull();
        }

        [Test]
        public async Task Given_Check_Answers_When_MissingClaims_Returns_TechnicalError()
        {
            // Arrange
            var request = _fixture.Create<FsmApplication>();

            // Create empty claims - simulating no auth
            _sut.ControllerContext.HttpContext = new DefaultHttpContext();
            _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = await _sut.Check_Answers(request);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().Be("Outcome/Technical_Error");
        }



        [Test]
        public async Task Given_Check_Answers_When_Valid_Returns_ApplicationsRegistered()
        {
            // Arrange
            var request = _fixture.Create<FsmApplication>();
            var applications = new List<ApplicationConfirmationEntitledChildViewModel>
    {
        new()
        {
            ChildName = "Test Child",
            ParentName = "Test Parent",
            Reference = "REF123"
        }
    };

            SetupUserClaims();

            _adminProcessFSMApplicationUseCaseMock
                .Setup(x => x.Execute(
                    request,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((applications, "ApplicationsRegistered"));

            // Act
            var result = await _sut.Check_Answers(request);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be("ApplicationsRegistered");

            var savedViewModel = JsonConvert.DeserializeObject<ApplicationConfirmationEntitledViewModel>(
                _sut.TempData["confirmationApplication"] as string);

            savedViewModel.Children.Should().BeEquivalentTo(applications);
            savedViewModel.ParentName.Should().Be($"{request.ParentFirstName} {request.ParentLastName}");
        }

        [Test]
        public async Task Given_Check_Answers_When_NotEntitled_Returns_AppealsRegistered()
        {
            // Arrange
            var request = _fixture.Create<FsmApplication>();
            var applications = new List<ApplicationConfirmationEntitledChildViewModel>
    {
        new() { ChildName = "Test Child", ParentName = "Test Parent" }
    };

            SetupUserClaims();

            _adminProcessFSMApplicationUseCaseMock
                .Setup(x => x.Execute(
                    request,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((applications, "AppealsRegistered"));

            // Act
            var result = await _sut.Check_Answers(request);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>()
                .Which.ActionName.Should().Be("AppealsRegistered");
        }

        [Test]
        public async Task Given_Check_Answers_When_ProcessingFails_Returns_TechnicalError()
        {
            // Arrange
            var request = _fixture.Create<FsmApplication>();

            SetupUserClaims();

            _adminProcessFSMApplicationUseCaseMock
                .Setup(x => x.Execute(
                    request,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new AdminProcessFSMApplicationException("Test error"));

            // Act
            var result = await _sut.Check_Answers(request);

            // Assert
            result.Should().BeOfType<ViewResult>()
                .Which.ViewName.Should().Be("Outcome/Technical_Error");
        }

        private void SetupUserClaims()
        {
            var claims = new List<Claim>
    {
        new("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "12345"),
        new("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "test@example.com"),
        new("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname", "John"),
        new("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname", "Doe"),
        new("OrganisationCategoryName", Constants.CategoryTypeLA),
        new("OrganisationUrn", "54321")
    };

            _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(claims));
        }

        [Test]
        public void Given_ApplicationsRegistered_It_ReturnsView_With_ApplicationConfirmationEntitledViewModel()
        {
            // Arrange
            var fixture = _fixture.Create<ApplicationConfirmationEntitledViewModel>();
            _sut.TempData["confirmationApplication"] = JsonConvert.SerializeObject(fixture);

            // Act
            var result = _sut.ApplicationsRegistered();

            // Assert
            var viewResult = result as ViewResult;
            viewResult.ViewName.Should().Be("ApplicationsRegistered");
            viewResult.Model.Should().NotBeNull();
            var model = viewResult.Model as ApplicationConfirmationEntitledViewModel;
            model.Children[0].ChildName.Should().Be(fixture.Children[0].ChildName);
            model.Children[0].ParentName.Should().Be(fixture.Children[0].ParentName);
            model.Children[1].ChildName.Should().Be(fixture.Children[1].ChildName);
        }

        [Test]
        public void Given_ChangeChildDetails_With_Valid_TempData_Returns_View()
        {
            // Arrange
            var fsmApplication = _fixture.Create<FsmApplication>();

            _sut.TempData["FsmApplication"] = JsonConvert.SerializeObject(fsmApplication);

            // Act
            int child = 0;
            var result = _sut.ChangeChildDetails(child);

            // Assert
            var viewResult = result as ViewResult;
            viewResult.ViewName.Should().Be("Enter_Child_Details");
            var model = viewResult.Model as Children;
            model.Should().BeEquivalentTo(fsmApplication.Children);
        }

        [TestCase(CheckEligibilityStatus.eligible, "Outcome/Eligible")]
        [TestCase(CheckEligibilityStatus.notEligible, "Outcome/Not_Eligible")]
        [TestCase(CheckEligibilityStatus.parentNotFound, "Outcome/Not_Found")]
        [TestCase(CheckEligibilityStatus.DwpError, "Outcome/Technical_Error")]
        public async Task Given_Poll_Status_With_Valid_Status_Returns_Correct_View(CheckEligibilityStatus status, string expectedView)
        {
            // Arrange
            var statusValue = _fixture.Build<StatusValue>()
                .With(x => x.Status, status.ToString())
                .Create();

            var checkEligibilityResponse = _fixture.Build<CheckEligibilityResponse>()
                .With(x => x.Data, statusValue)
                .Create();

            var responseJson = JsonConvert.SerializeObject(checkEligibilityResponse);
            _tempData["Response"] = responseJson;

            var checkEligibilityStatusResponse = _fixture.Build<CheckEligibilityStatusResponse>()
                .With(x => x.Data, checkEligibilityResponse.Data)
                .Create();

            _checkServiceMock.Setup(x => x.GetStatus(It.IsAny<CheckEligibilityResponse>()))
                .ReturnsAsync(checkEligibilityStatusResponse);

            // Set up a basic HttpContext with claims
            _sut.ControllerContext.HttpContext = new DefaultHttpContext();
            _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "12345"),
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "test@example.com"),
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname", "John"),
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname", "Doe"),
                new Claim("OrganisationCategoryName", Constants.CategoryTypeLA)
            }));

            // Act
            var result = await _sut.Loader();

            // Assert
            if (result is ViewResult viewResult)
            {
                viewResult.ViewName.Should().Be(expectedView);
            }
            else if (result is RedirectToActionResult redirectResult)
            {
                redirectResult.ActionName.Should().Be("Application_Sent");
            }
            else
            {
                Assert.Fail("Unexpected result type");
            }
        }

        [Test]
        public async Task Given_Poll_Status_When_Response_Is_Null_Returns_Error_Status()
        {
            // Arrange
            _tempData["Response"] = null;

            // Act
            var result = await _sut.Loader();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.ViewName.Should().Be("Outcome/Technical_Error");
        }

        [Test]
        public async Task Given_Poll_Status_When_Status_Is_Processing_Returns_Processing()
        {
            // Arrange
            var response = new CheckEligibilityResponse
            {
                Data = new StatusValue { Status = "processing" }
            };
            _tempData["Response"] = JsonConvert.SerializeObject(response);

            _checkServiceMock.Setup(x => x.GetStatus(It.IsAny<CheckEligibilityResponse>()))
                .ReturnsAsync(new CheckEligibilityStatusResponse { Data = response.Data });

            // Act
            var result = await _sut.Loader();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.ViewName.Should().Be("Loader");
        }
    }
}
