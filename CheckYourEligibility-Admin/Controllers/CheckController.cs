using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility_DfeSignIn;
using CheckYourEligibility_DfeSignIn.Models;
using CheckYourEligibility_FrontEnd.Models;
using CheckYourEligibility_FrontEnd.Services;
using CheckYourEligibility_FrontEnd.UseCases.Admin;
using CheckYourEligibility_FrontEnd.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Child = CheckYourEligibility_FrontEnd.Models.Child;

namespace CheckYourEligibility_FrontEnd.Controllers
{
    public class CheckController : BaseController
    {

        private readonly ILogger<CheckController> _logger;
        private readonly IEcsCheckService _checkService;
        private readonly IEcsServiceParent _parentService;
        private readonly IConfiguration _config;
        private readonly IAdminLoadParentDetailsUseCase _adminLoadParentDetailsUseCase;
        private readonly IAdminProcessParentDetailsUseCase _adminProcessParentDetailsUseCase;
        private readonly IAdminProcessFSMApplicationUseCase _adminProcessFSMApplicationUseCase;
        DfeClaims? _Claims;

        public CheckController(
            ILogger<CheckController> logger,
            IEcsServiceParent ecsServiceParent,
            IEcsCheckService ecsCheckService,
            IConfiguration configuration,
            IAdminLoadParentDetailsUseCase adminLoadParentDetailsUseCase,
            IAdminProcessParentDetailsUseCase adminProcessParentDetailsUseCase,
            IAdminProcessFSMApplicationUseCase adminProcessFSMApplicationUseCase)
            
        {
            _config = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parentService = ecsServiceParent ?? throw new ArgumentNullException(nameof(ecsServiceParent));
            _checkService = ecsCheckService ?? throw new ArgumentNullException(nameof(ecsCheckService));
            _adminLoadParentDetailsUseCase = adminLoadParentDetailsUseCase ?? throw new ArgumentNullException(nameof(adminLoadParentDetailsUseCase));
            _adminProcessParentDetailsUseCase = adminProcessParentDetailsUseCase ?? throw new ArgumentNullException(nameof(adminProcessParentDetailsUseCase));
            _adminProcessFSMApplicationUseCase = adminProcessFSMApplicationUseCase ?? throw new ArgumentNullException(nameof(adminProcessFSMApplicationUseCase));
        }

        [HttpGet]
        public async Task<IActionResult> Enter_Details()
        {
            var viewModel = await _adminLoadParentDetailsUseCase.ExecuteAsync(
                TempData["ParentDetails"]?.ToString(),
                TempData["Errors"]?.ToString()
            );

            if (viewModel.ValidationErrors != null)
            {
                foreach (var (key, errorList) in viewModel.ValidationErrors)
                {
                    foreach (var error in errorList)
                    {
                        ModelState.AddModelError(key, error);
                    }
                }
            }

            return View(viewModel.Parent);
        }

        [HttpPost]
        public async Task<IActionResult> Enter_Details(ParentGuardian request)
        {
            if (!ModelState.IsValid)
            {
                TempData["ParentDetails"] = JsonConvert.SerializeObject(request);
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToList());
                TempData["Errors"] = JsonConvert.SerializeObject(errors);
                return RedirectToAction("Enter_Details");
            }

            try
            {
                var (isValid, response, redirectAction) = await _adminProcessParentDetailsUseCase.Execute(request, HttpContext.Session);

                if (response != null)
                {
                    TempData["Response"] = JsonConvert.SerializeObject(response);
                }

                return RedirectToAction(redirectAction);
            }
            catch (AdminParentDetailsValidationException ex)
            {
                TempData["ParentDetails"] = JsonConvert.SerializeObject(request);
                TempData["Errors"] = ex.Message;
                return RedirectToAction("Enter_Details");
            }
        }

        public async Task<IActionResult> Loader()
        {
            _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);

            // Retrieve the API response from TempData
            var responseJson = TempData["Response"] as string;
            if (responseJson == null)
            {
                _logger.LogWarning("No response data found in TempData.");
                return View("Outcome/Technical_Error");
            }

            var response = JsonConvert.DeserializeObject<CheckEligibilityResponse>(responseJson);
            _logger.LogInformation($"Check status processed: {response?.Data?.Status}");

            // Call the service to check the current status
            var check = await _checkService.GetStatus(response);
            if (check == null || check.Data == null)
            {
                _logger.LogWarning("Null response received from GetStatus.");
                return View("Outcome/Technical_Error");
            }

            _logger.LogInformation($"Received status: {check.Data.Status}");
            Enum.TryParse(check.Data.Status, out CheckEligibilityStatus status);
            TempData["OutcomeStatus"] = status;
            bool isLA = _Claims?.Organisation?.Category?.Name == Constants.CategoryTypeLA; //false=school
            switch (status)
            {
                case CheckEligibilityStatus.eligible:
                    return (isLA ? View("Outcome/Eligible_LA") : View("Outcome/Eligible"));
                case CheckEligibilityStatus.notEligible:
                    return (isLA ? View("Outcome/Not_Eligible_LA") : View("Outcome/Not_Eligible"));
                case CheckEligibilityStatus.parentNotFound:
                    return View("Outcome/Not_Found");
                case CheckEligibilityStatus.DwpError:
                    return View("Outcome/Technical_Error");
                case CheckEligibilityStatus.queuedForProcessing:
                    _logger.LogInformation("Still queued for processing.");
                    // Save the response back to TempData for the next poll
                    TempData["Response"] = JsonConvert.SerializeObject(response);
                    // Render the loader view which will auto-refresh
                    return View("Loader");
                default:
                    _logger.LogError($"Unknown Status {status}");
                    return View("Outcome/Technical_Error");
            }
        }

        public IActionResult Enter_Child_Details()
        {
            var children = new Children() { ChildList = [new()] };

            // Check if this is a redirect after add or remove child
            if (TempData["IsChildAddOrRemove"] != null && (bool)TempData["IsChildAddOrRemove"] == true)
            {
                ModelState.Clear();

                // Retrieve Children from TempData
                var childDetails = TempData["ChildList"] as string;
                children.ChildList = JsonConvert.DeserializeObject<List<Child>>(childDetails);
            }

            return View(children);
        }

        [HttpPost]
        public IActionResult Enter_Child_Details(Children request)
        {
            if (TempData["FsmApplication"] != null && TempData["IsRedirect"] != null && (bool)TempData["IsRedirect"] == true)
            {
                return View("Enter_Child_Details", request);
            }

            if (!ModelState.IsValid)
            {
                return View("Enter_Child_Details", request);
            }

            var fsmApplication = new FsmApplication
            {
                ParentFirstName = HttpContext.Session.GetString("ParentFirstName"),
                ParentLastName = HttpContext.Session.GetString("ParentLastName"),
                ParentDateOfBirth = HttpContext.Session.GetString("ParentDOB"),
                ParentNass = HttpContext.Session.GetString("ParentNASS") ?? null,
                ParentNino = HttpContext.Session.GetString("ParentNINO") ?? null,
                ParentEmail = HttpContext.Session.GetString("ParentEmail"),
                Children = request
            };

            TempData["FsmApplication"] = JsonConvert.SerializeObject(fsmApplication);

            return View("Check_Answers", fsmApplication);
        }

        [HttpPost]
        public IActionResult Add_Child(Children request)
        {
            // set initial tempdata
            TempData["IsChildAddOrRemove"] = true;

            // don't allow the model to contain more than 99 items
            if (request.ChildList.Count >= 99)
            {
                return RedirectToAction("Enter_Child_Details");
            }

            request.ChildList.Add(new Child());

            TempData["ChildList"] = JsonConvert.SerializeObject(request.ChildList);

            return RedirectToAction("Enter_Child_Details");
        }

        [HttpPost]
        public IActionResult Remove_Child(Children request, int index)
        {
            try
            {
                // remove child at given index
                var child = request.ChildList[index];
                request.ChildList.Remove(child);

                // set up tempdata so page can be correctly rendered
                TempData["IsChildAddOrRemove"] = true;
                TempData["ChildList"] = JsonConvert.SerializeObject(request.ChildList);

                return RedirectToAction("Enter_Child_Details");
            }
            catch (IndexOutOfRangeException ex)
            {
                throw ex;
            }

        }

        public IActionResult Check_Answers()
        {
            return View("Check_Answers");
        }

        [HttpPost]
        public async Task<IActionResult> Check_Answers(FsmApplication request)
        {
            try
            {
                _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);

                if (_Claims == null || string.IsNullOrEmpty(_Claims.User.Email) ||
                    string.IsNullOrEmpty(_Claims.User.Id) || string.IsNullOrEmpty(_Claims.Organisation?.Urn))
                {
                    _logger.LogWarning("Missing required claims for FSM application");
                    return View("Outcome/Technical_Error");
                }

                var (applications, redirectAction) = await _adminProcessFSMApplicationUseCase.Execute(
                    request,
                    _Claims.User.Email,
                    _Claims.User.Id,
                    _Claims.Organisation.Urn);

                var viewModel = new ApplicationConfirmationEntitledViewModel
                {
                    ParentName = $"{request.ParentFirstName} {request.ParentLastName}",
                    Children = applications
                };

                TempData["confirmationApplication"] = JsonConvert.SerializeObject(viewModel);

                return RedirectToAction(redirectAction);
            }
            catch (AdminProcessFSMApplicationException ex)
            {
                _logger.LogError(ex, "Error processing FSM application");
                return View("Outcome/Technical_Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing FSM application");
                return View("Outcome/Technical_Error");
            }
        }

        [HttpGet]
        public IActionResult ApplicationsRegistered()
        {
            var vm = JsonConvert.DeserializeObject<ApplicationConfirmationEntitledViewModel>(TempData["confirmationApplication"].ToString());
            return View("ApplicationsRegistered", vm);
        }

        [HttpGet]
        public IActionResult AppealsRegistered()
        {
            var vm = JsonConvert.DeserializeObject<ApplicationConfirmationEntitledViewModel>(TempData["confirmationApplication"].ToString());
            return View("AppealsRegistered", vm);
        }

        public IActionResult ChangeChildDetails(int child)
        {
            // set up tempdata and access existing temp data object
            TempData["IsRedirect"] = true;
            TempData["childIndex"] = child;
            var responseJson = TempData["FsmApplication"] as string;
            // deserialize
            var responses = JsonConvert.DeserializeObject<FsmApplication>(responseJson);
            // get children details
            var children = responses.Children;
            // populate enter_child_details page with children model
            return View("Enter_Child_Details", children);
        }
    }
}