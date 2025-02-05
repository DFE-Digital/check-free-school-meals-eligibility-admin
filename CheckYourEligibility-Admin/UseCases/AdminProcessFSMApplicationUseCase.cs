using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility_FrontEnd.Models;
using CheckYourEligibility_FrontEnd.Services;
using CheckYourEligibility_FrontEnd.ViewModels;
using Microsoft.Extensions.Logging;
using ModelChild = CheckYourEligibility_FrontEnd.Models.Child;

namespace CheckYourEligibility_FrontEnd.UseCases.Admin
{
    public interface IAdminProcessFSMApplicationUseCase
    {
        Task<(List<ApplicationConfirmationEntitledChildViewModel> Applications, string RedirectAction)> Execute(
            FsmApplication request,
            string userEmail,
            string userId,
            string establishmentUrn);
    }

    [Serializable]
    public class AdminProcessFSMApplicationException : Exception
    {
        public AdminProcessFSMApplicationException(string message) : base(message)
        {
        }
    }

    public class AdminProcessFSMApplicationUseCase : IAdminProcessFSMApplicationUseCase
    {
        private readonly ILogger<AdminProcessFSMApplicationUseCase> _logger;
        private readonly IEcsServiceParent _parentService;

        public AdminProcessFSMApplicationUseCase(
            ILogger<AdminProcessFSMApplicationUseCase> logger,
            IEcsServiceParent parentService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parentService = parentService ?? throw new ArgumentNullException(nameof(parentService));
        }

        public async Task<(List<ApplicationConfirmationEntitledChildViewModel> Applications, string RedirectAction)> Execute(
            FsmApplication request,
            string userEmail,
            string userId,
            string establishmentUrn)
        {
            try
            {
                _logger.LogInformation("Processing FSM application for user {UserId}", userId);

                var user = await CreateUser(userEmail, userId);
                if (string.IsNullOrEmpty(user?.Data))
                {
                    throw new AdminProcessFSMApplicationException("Failed to create user record");
                }

                var applications = new List<ApplicationConfirmationEntitledChildViewModel>();
                ApplicationSaveItemResponse lastResponse = null;

                foreach (var child in request.Children.ChildList)
                {
                    var application = await ProcessChildApplication(request, child, user.Data, establishmentUrn);
                    lastResponse = application;

                    applications.Add(new ApplicationConfirmationEntitledChildViewModel
                    {
                        ParentName = $"{request.ParentFirstName} {request.ParentLastName}",
                        ChildName = $"{application.Data.ChildFirstName} {application.Data.ChildLastName}",
                        Reference = application.Data.Reference
                    });
                }

                _logger.LogInformation("Successfully processed {Count} applications for user {UserId}",
                    applications.Count, userId);

                return (applications,
                    lastResponse?.Data?.Status == "Entitled" ? "ApplicationsRegistered" : "AppealsRegistered");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing FSM application for user {UserId}", userId);
                throw new AdminProcessFSMApplicationException("Failed to process FSM application: " + ex.Message);
            }
        }

        private async Task<UserSaveItemResponse> CreateUser(string email, string uniqueId)
        {
            var userRequest = new UserCreateRequest
            {
                Data = new UserData
                {
                    Email = email,
                    Reference = uniqueId
                }
            };

            return await _parentService.CreateUser(userRequest);
        }

        private async Task<ApplicationSaveItemResponse> ProcessChildApplication(
            FsmApplication request,
            ModelChild child,
            string userId,
            string establishmentUrn)
        {
            var application = new ApplicationRequest
            {
                Data = new ApplicationRequestData
                {
                    Type = CheckEligibilityType.FreeSchoolMeals,
                    ParentFirstName = request.ParentFirstName,
                    ParentLastName = request.ParentLastName,
                    ParentEmail = request.ParentEmail,
                    ParentDateOfBirth = request.ParentDateOfBirth,
                    ParentNationalInsuranceNumber = request.ParentNino,
                    ParentNationalAsylumSeekerServiceNumber = request.ParentNass,
                    ChildFirstName = child.FirstName,
                    ChildLastName = child.LastName,
                    ChildDateOfBirth = new DateOnly(
                        int.Parse(child.Year),
                        int.Parse(child.Month),
                        int.Parse(child.Day)).ToString("yyyy-MM-dd"),
                    Establishment = int.Parse(establishmentUrn),
                    UserId = userId
                }
            };

            return await _parentService.PostApplication_Fsm(application);
        }
    }
}