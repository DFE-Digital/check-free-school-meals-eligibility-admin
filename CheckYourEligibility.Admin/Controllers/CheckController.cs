using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Domain.Constants;
using CheckYourEligibility.Admin.Domain.DfeSignIn;
using CheckYourEligibility.Admin.Domain.Enums;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.Infrastructure;
using CheckYourEligibility.Admin.Models;
using CheckYourEligibility.Admin.Usecases;
using CheckYourEligibility.Admin.UseCases;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using Child = CheckYourEligibility.Admin.Models.Child;

namespace CheckYourEligibility.Admin.Controllers;

public class CheckController : BaseController
{
    private readonly IAddChildUseCase _addChildUseCase;
    private readonly IChangeChildDetailsUseCase _changeChildDetailsUseCase;
    private readonly ICheckGateway _checkGateway;
    private readonly IConfiguration _config;
    private readonly ICreateUserUseCase _createUserUseCase;
    private readonly IEnterChildDetailsUseCase _enterChildDetailsUseCase;
    private readonly IGetCheckStatusUseCase _getCheckStatusUseCase;
    private readonly IGetCheckUseCase _getCheckUseCase;
    private readonly ILoadParentDetailsUseCase _loadParentDetailsUseCase;
    private readonly ILogger<CheckController> _logger;
    private readonly IPerformEligibilityCheckUseCase _performEligibilityCheckUseCase;
    private readonly IProcessChildDetailsUseCase _processChildDetailsUseCase;
    private readonly IRemoveChildUseCase _removeChildUseCase;
    private readonly ISearchSchoolsUseCase _searchSchoolsUseCase;
    private readonly ISubmitApplicationUseCase _submitApplicationUseCase;
    private readonly IValidateParentDetailsUseCase _validateParentDetailsUseCase;
    private readonly IUploadEvidenceFileUseCase _uploadEvidenceFileUseCase;
    private readonly IValidateEvidenceFileUseCase _validateEvidenceFileUse;
    private readonly ISendNotificationUseCase _sendNotificationUseCase;
    private readonly IDeleteEvidenceFileUseCase _deleteEvidenceFileUseCase;


    public CheckController(
        ILogger<CheckController> logger,
        ICheckGateway checkGateway,
        IConfiguration configuration,
        ILoadParentDetailsUseCase loadParentDetailsUseCase,
        IPerformEligibilityCheckUseCase performEligibilityCheckUseCase,
        IEnterChildDetailsUseCase enterChildDetailsUseCase,
        IProcessChildDetailsUseCase processChildDetailsUseCase,
        IGetCheckStatusUseCase getCheckStatusUseCase,
        IGetCheckUseCase getCheckUseCase,
        IAddChildUseCase addChildUseCase,
        IRemoveChildUseCase removeChildUseCase,
        ISearchSchoolsUseCase searchSchoolsUseCase,
        IChangeChildDetailsUseCase changeChildDetailsUseCase,
        ICreateUserUseCase createUserUseCase,
        ISubmitApplicationUseCase submitApplicationUseCase,
        IValidateParentDetailsUseCase validateParentDetailsUseCase,
        IUploadEvidenceFileUseCase uploadEvidenceFileUseCase,
        IValidateEvidenceFileUseCase validateEvidenceFileUseCase,
        ISendNotificationUseCase sendNotificationUseCase,
        IDeleteEvidenceFileUseCase deleteEvidenceFileUseCase,
        IDfeSignInApiService dfeSignInApiService,
        ISchoolMenuContextResolver schoolMenuContextResolver,
        ILocalAuthoritySettingsGateway localAuthoritySettingsGateway) : base(dfeSignInApiService, schoolMenuContextResolver, localAuthoritySettingsGateway)
    {
        _config = configuration;
        _logger = logger;
        _checkGateway = checkGateway;
        _loadParentDetailsUseCase = loadParentDetailsUseCase;
        _performEligibilityCheckUseCase = performEligibilityCheckUseCase;
        _enterChildDetailsUseCase = enterChildDetailsUseCase;
        _processChildDetailsUseCase = processChildDetailsUseCase;
        _getCheckStatusUseCase = getCheckStatusUseCase;
        _getCheckUseCase = getCheckUseCase;
        _addChildUseCase = addChildUseCase;
        _removeChildUseCase = removeChildUseCase;
        _searchSchoolsUseCase = searchSchoolsUseCase;
        _changeChildDetailsUseCase = changeChildDetailsUseCase;
        _createUserUseCase = createUserUseCase;
        _submitApplicationUseCase = submitApplicationUseCase;
        _validateParentDetailsUseCase = validateParentDetailsUseCase;
        _uploadEvidenceFileUseCase = uploadEvidenceFileUseCase;
        _validateEvidenceFileUse = validateEvidenceFileUseCase;
        _sendNotificationUseCase = sendNotificationUseCase ?? throw new ArgumentNullException(nameof(sendNotificationUseCase));
        _deleteEvidenceFileUseCase = deleteEvidenceFileUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Consent_Declaration()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Consent_Declaration_Approval(string consent)
    {
        if (consent == "checked") return RedirectToAction("Enter_Details");

        return View("Consent_Declaration", true);
    }

    [HttpGet]
    public async Task<IActionResult> Enter_Details()
    {
        var (parent, validationErrors) = await _loadParentDetailsUseCase.Execute(
            TempData["ParentDetails"]?.ToString()!,
            TempData["Errors"]?.ToString()!
        );

        if (validationErrors != null)
            foreach (var (key, errorList) in validationErrors)
                foreach (var error in errorList)
                    ModelState.AddModelError(key, error);

        // Cache the organisation type for use in the view
        OrganisationCategory organisationType = _Claims.Organisation.Category.Id;
        TempData["organisationType"] = organisationType;

        return View(parent);
    }

    [HttpPost]
    public async Task<IActionResult> Enter_Details(ParentGuardian request)
    {
        var validationResult = _validateParentDetailsUseCase.Execute(request, ModelState);

        if (!validationResult.IsValid)
        {
            TempData["ParentDetails"] = JsonConvert.SerializeObject(request);
            TempData["Errors"] = JsonConvert.SerializeObject(validationResult.Errors);
            return RedirectToAction("Enter_Details");
        }

        // Clear data when starting a new application
        TempData.Remove("FsmApplication");
        TempData.Remove("FsmEvidence");

        var response = await _performEligibilityCheckUseCase.Execute(request);
        var responseJson = JsonConvert.SerializeObject(response);

        // Render the loader directly from the response already in hand (rather than redirecting and
        // reading TempData back) - a concurrent tab overwriting TempData in that gap could otherwise
        // hijack this tab's very first render. TempData is still set for the <noscript> fallback. See ELIG-3594.
        TempData["Response"] = responseJson;
        TempData["ParentGuardianRequest"] = JsonConvert.SerializeObject(request);
        return await ProcessLoaderStatus(responseJson, request, saveToTempDataForNoScript: true);
    }

    [HttpGet]
    public async Task<IActionResult> Loader(ParentGuardian request)
    {
        if (TempData["ParentGuardianRequest"] != null) // Means it was queued previously and stored in temp
        {
            var json = TempData["ParentGuardianRequest"] as string;
            request = JsonConvert.DeserializeObject<ParentGuardian>(json);
        }

        var responseJson = TempData["Response"] as string;
        return await ProcessLoaderStatus(responseJson, request, saveToTempDataForNoScript: true);
    }

    [HttpPost]
    public async Task<IActionResult> Loader(string responseJson, string parentGuardianJson)
    {
        // Polls from the JS loader carry their own check reference/parent details (echoed back from
        // the page that rendered them) instead of relying on TempData, so a concurrent tab writing
        // TempData for a different check cannot hijack this poll. See ELIG-3594.
        var request = string.IsNullOrEmpty(parentGuardianJson)
            ? new ParentGuardian()
            : JsonConvert.DeserializeObject<ParentGuardian>(parentGuardianJson);

        return await ProcessLoaderStatus(responseJson, request, saveToTempDataForNoScript: false);
    }

    private async Task<IActionResult> ProcessLoaderStatus(string responseJson, ParentGuardian request, bool saveToTempDataForNoScript)
    {
        try
        {

            // Cache the organisation type for use in the view
            OrganisationCategory organisationType = _Claims.Organisation.Category.Id;
            TempData["organisationType"] = organisationType;

            // Cache the role for use in the view
            TempData["organisationRole"] = OrgRole.enhanced; //default to enhanced
            if (_Claims?.Roles?.Any(x => x.Code == DfeSignInRoles.RoleCodeBasic) == true)
            {
                TempData["organisationRole"] = OrgRole.basic; // set only if basic
            }

            var outcome = await _getCheckStatusUseCase.Execute(responseJson);

            // If the check is still queued, show the loader again. TempData is only kept here for the
            // <noscript> meta-refresh fallback; JS-enabled polling instead re-posts the values embedded
            // in the LoaderState model below, so it never depends on TempData/Session. See ELIG-3594.
            if (outcome.Status == "queuedForProcessing")
            {
                if (saveToTempDataForNoScript)
                {
                    TempData["Response"] = responseJson;
                    TempData["ParentGuardianRequest"] = JsonConvert.SerializeObject(request);
                }

                return View("Loader", new LoaderState
                {
                    ResponseJson = responseJson,
                    ParentGuardianJson = JsonConvert.SerializeObject(request)
                });
            }
            
            // Get current FSM policy 
            await IsExpandedFSMEnabled();

            var tieredOutcome = new TieredOutcome
            {
                Status = outcome.Status,
                Tier = outcome.Tier,
                ParentGuardian = request
            };

            // If check is now complete and eligible, retrieve the full check data
            if (outcome.Status == "eligible")
            {
                var checkData = await _getCheckUseCase.Execute(responseJson);
                tieredOutcome.Tier = checkData.Data.Tier;
                tieredOutcome.EligibilityEndDate = checkData.Data.EligibilityEndDate;
            }

            // ELIG-3594: pass the outcome as the view model (rather than caching it in Session) so it can be
            // carried forward via hidden fields, instead of being read back from shared per-browser state later.
            switch (outcome.Status)
            {
                case "eligible":
                    return View("Outcome/Eligible", tieredOutcome);
                case "notEligible":
                    return View("Outcome/Not_Eligible", tieredOutcome);
                case "parentNotFound":
                    return View("Outcome/Not_Found");
                default:
                    return View("Outcome/Technical_Error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing check status in Loader action");
            return View("Outcome/Technical_Error");
        }
    }

    [HttpGet]
    public IActionResult Start_Child_Details()
    {
        // Only reached if the auth cookie expired and the re-authentication redirect replayed this
        // as a GET (the original POST body/outcome data is lost in that replay, so it can't be
        // recovered here) - send the user back to a safe, working page instead of a raw 404.
        TempData["Errors"] = JsonConvert.SerializeObject(new Dictionary<string, List<string>>
        {
            { string.Empty, new List<string> { "Your session timed out. Please start the check again." } }
        });
        return RedirectToAction("Enter_Details");
    }

    [HttpPost]
    public IActionResult Start_Child_Details(TieredOutcome model)
    {
        string? dobString = null;
        if (int.TryParse(model.ParentGuardian?.Year, out var year) &&
            int.TryParse(model.ParentGuardian?.Month, out var month) &&
            int.TryParse(model.ParentGuardian?.Day, out var day))
        {
            dobString = new DateOnly(year, month, day).ToString("yyyy-MM-dd");
        }

        // Seed the parent/tier bundle for the very next Enter_Child_Details render; from then on it
        // travels as hidden fields on that form, not via Session/TempData. See ELIG-3594.
        TempData["ParentBundle"] = JsonConvert.SerializeObject(new Children
        {
            ParentFirstName = model.ParentGuardian?.FirstName,
            ParentLastName = model.ParentGuardian?.LastName,
            ParentDateOfBirth = dobString,
            ParentEmail = model.ParentGuardian?.EmailAddress,
            ParentNino = model.ParentGuardian?.NationalInsuranceNumber,
            Status = model.Status,
            Tier = model.Tier,
            EligibilityEndDate = model.EligibilityEndDate
        });

        return RedirectToAction("Enter_Child_Details");
    }

    [HttpGet]
    public IActionResult Enter_Child_Details()
    {
        var childrenModel = _enterChildDetailsUseCase.Execute(
             TempData["ChildList"] as string,
             TempData["IsChildAddOrRemove"] as bool?);

        if (TempData["ParentBundle"] is string parentBundleJson && !string.IsNullOrEmpty(parentBundleJson))
        {
            var parentBundle = JsonConvert.DeserializeObject<Children>(parentBundleJson);
            childrenModel.ParentFirstName = parentBundle.ParentFirstName;
            childrenModel.ParentLastName = parentBundle.ParentLastName;
            childrenModel.ParentDateOfBirth = parentBundle.ParentDateOfBirth;
            childrenModel.ParentEmail = parentBundle.ParentEmail;
            childrenModel.ParentNino = parentBundle.ParentNino;
            childrenModel.ParentNass = parentBundle.ParentNass;
            childrenModel.Status = parentBundle.Status;
            childrenModel.Tier = parentBundle.Tier;
            childrenModel.EligibilityEndDate = parentBundle.EligibilityEndDate;
            TempData.Keep("ParentBundle");
        }

        OrganisationCategory organisationType = _Claims.Organisation.Category.Id;
        TempData["organisationType"] = organisationType;

        return View(childrenModel);
    }

    [HttpPost]
    public IActionResult Enter_Child_Details(Children request)
    {
        OrganisationCategory organisationType = _Claims.Organisation.Category.Id;
        TempData["organisationType"] = organisationType;

        if (!ModelState.IsValid) return View("Enter_Child_Details", request);

        var fsmApplication = _processChildDetailsUseCase.Execute(request).Result;
        // Render the next page directly from the application just built (rather than redirecting and
        // reading TempData back), so a concurrent tab overwriting TempData can't hijack this render. See ELIG-3594.
        TempData["FsmApplication"] = JsonConvert.SerializeObject(fsmApplication);
        if (request.Status == "eligible")
        {
            return View("Check_Answers", fsmApplication);
        }
        // Restore evidence from TempData if it exists (from ChangeChildDetails)
        if (TempData["FsmEvidence"] != null)
        {
            var savedEvidence = JsonConvert.DeserializeObject<Evidence>(TempData["FsmEvidence"].ToString());
            fsmApplication.Evidence = savedEvidence;

            TempData.Remove("FsmEvidence");
        }
        else
        {
            fsmApplication.Evidence = new Evidence { EvidenceList = new List<EvidenceFile>() };
        }

        TempData["FsmApplication"] = JsonConvert.SerializeObject(fsmApplication);

        return View("UploadEvidence", fsmApplication);
    }

    [HttpPost]
    public IActionResult Add_Child(Children request)
    {
        try
        {
            TempData["IsChildAddOrRemove"] = true;

            var updatedChildren = _addChildUseCase.Execute(request);

            TempData["ChildList"] = JsonConvert.SerializeObject(updatedChildren.ChildList);
            TempData["ParentBundle"] = JsonConvert.SerializeObject(updatedChildren);
        }
        catch (MaxChildrenException e)
        {
            TempData["ChildList"] = JsonConvert.SerializeObject(request.ChildList);
            TempData["ParentBundle"] = JsonConvert.SerializeObject(request);
        }

        return RedirectToAction("Enter_Child_Details");
    }

    [HttpPost]
    public async Task<IActionResult> Remove_Child(Children request, int index)
    {
        try
        {
            TempData["IsChildAddOrRemove"] = true;

            var updatedChildren = await _removeChildUseCase.Execute(request, index);

            TempData["ChildList"] = JsonConvert.SerializeObject(updatedChildren.ChildList);
            TempData["ParentBundle"] = JsonConvert.SerializeObject(updatedChildren);

            return RedirectToAction("Enter_Child_Details");
        }

        catch (RemoveChildValidationException e)
        {
            ModelState.AddModelError(string.Empty, e.Message);
            return RedirectToAction("Enter_Child_Details");
        }
    }

    [HttpGet]
    public async Task<IActionResult> SearchSchools(string query)
    {
        try
        {
            // Sanitize input before processing
            var sanitizedQuery = query?.Trim()
                .Replace(Environment.NewLine, "")
                .Replace("\n", "")
                .Replace("\r", "")
                // Add more sanitization as needed
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            if (string.IsNullOrEmpty(sanitizedQuery) || sanitizedQuery.Length < 3)
            {
                _logger.LogWarning("Invalid school search query: {Query}", sanitizedQuery);
                return BadRequest("Query must be at least 3 characters long.");
            }
            string organisationType;
            string organisationNumber;
            if (_Claims.Organisation.Category.Id == OrganisationCategory.MultiAcademyTrust)
            {
                organisationType = "mat";
                organisationNumber = _Claims.Organisation.Uid;
            }
            else
            {
                organisationType = "la";
                organisationNumber = _Claims.Organisation.EstablishmentNumber;
            }
            var schools = await _searchSchoolsUseCase.Execute(sanitizedQuery, organisationNumber, organisationType);
            return Json(schools.ToList());
        }
        catch (Exception ex)
        {
            // Log sanitized query only
            _logger.LogError(ex, "Error searching schools for query: {Query}",
                query?.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", ""));
            return BadRequest("An error occurred while searching for schools.");
        }
    }
    [HttpGet]

    public async Task<IActionResult> Check_Answers()
    {
        if (TempData["FsmApplication"] != null)
        {
            var fsmApplication = JsonConvert.DeserializeObject<FsmApplication>(TempData["FsmApplication"].ToString());
            // Re-save the application data to TempData for the next request
            TempData["FsmApplication"] = JsonConvert.SerializeObject(fsmApplication);

            OrganisationCategory organisationType = _Claims.Organisation.Category.Id;
            TempData["organisationType"] = organisationType;

            return View("Check_Answers", fsmApplication);
        }

        // Fallback - empty model
        return View("Check_Answers");
    }
    //
    [HttpPost]
    [ActionName("Check_Answers")]
    public async Task<IActionResult> Check_Answers_Post(FsmApplication request)
    {
        if (TempData["FsmApplication"] != null)
        {
            var savedApplication = JsonConvert.DeserializeObject<FsmApplication>(TempData["FsmApplication"].ToString());
            if (savedApplication.Evidence?.EvidenceList?.Count > 0)
            {
                request.Evidence = savedApplication.Evidence;
            }
        }

        OrganisationCategory organisationType = _Claims.Organisation.Category.Id;
        TempData["organisationType"] = organisationType;

        // var userId = await _createUserUseCase.Execute(HttpContext.User.Claims);

        var responses = await _submitApplicationUseCase.Execute(
            request,
            null,
            _Claims.Organisation.Urn);

        TempData["FsmApplicationResponse"] = JsonConvert.SerializeObject(responses);

        foreach (var response in responses)
        {
            try
            {
                var notificationRequest = new NotificationRequest
                {
                    Data = new NotificationRequestData
                    {
                        Email = response.Data.ParentEmail,
                        Type = NotificationType.ParentApplicationSuccessful,
                        Personalisation = new Dictionary<string, object>
                        {
                        { "reference", $"{response.Data.Reference}" },
                        { "parentFirstName", $"{request.ParentFirstName}" }
                    }
                    }
                };

                await _sendNotificationUseCase.Execute(notificationRequest);
                _logger.LogInformation("Notification sent successfully for application reference: {Reference}",
                    response.Data.Reference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification for application reference: {Reference}",
                    response.Data.Reference);
            }
        }

        // Carry Tier/EndDate forward for the single immediate redirect to ApplicationsRegistered
        // (not cached in Session, which would be visible to/overwritten by other tabs).
        TempData["FSM_Tier"] = request.Tier;
        TempData["FSM_EndDate"] = request.EligibilityEndDate;

        return RedirectToAction(
            responses.FirstOrDefault()?.Data.Status == ApplicationStatus.Entitled
                ? "ApplicationsRegistered"
                : "AppealsRegistered");
                
    }

    [HttpPost]
    public IActionResult RemoveEvidenceItem(string fileName, string redirectAction)
    {
        if (TempData["FsmApplication"] != null)
        {
            var fsmApplication = JsonConvert.DeserializeObject<FsmApplication>(TempData["FsmApplication"].ToString());
            var evidenceItem = fsmApplication.Evidence.EvidenceList.FirstOrDefault(e => e.FileName == fileName);
            if (evidenceItem != null)
            {
                fsmApplication.Evidence.EvidenceList.Remove(evidenceItem);
                TempData["FsmApplication"] = JsonConvert.SerializeObject(fsmApplication);
            }

            // Delete the file from blob storage
            if (evidenceItem != null && !string.IsNullOrEmpty(evidenceItem.StorageAccountReference))
            {
                _deleteEvidenceFileUseCase.Execute(evidenceItem.StorageAccountReference, _config["AzureStorageEvidence:EvidenceFilesContainerName"]);
            }
        }

        return RedirectToAction(redirectAction);
    }

    [HttpPost]
    public IActionResult ChangeChildDetails(FsmApplication request)
    {
        // Re-posting the full FsmApplication triggers auto-validation of fields this page doesn't
        // care about; matches the same ModelState.Clear() already used by UploadEvidence for this reason.
        ModelState.Clear();
        var model = new Children { ChildList = new List<Child>() };

        try
        {
            // Build from the answers just posted by this page (rather than TempData, which is shared
            // across all tabs of the browser and can hold a different tab's application). See ELIG-3594.
            TempData["FsmEvidence"] = JsonConvert.SerializeObject(request.Evidence);

            model = _changeChildDetailsUseCase.Execute(JsonConvert.SerializeObject(request));
        }
        catch (JSONException e)
        {
            ;
        }
        catch (NoChildException)
        {
            ;
        }

        OrganisationCategory organisationType = _Claims.Organisation.Category.Id;
        TempData["organisationType"] = organisationType;

        return View("Enter_Child_Details", model);
    }


    [HttpGet]
    public IActionResult ApplicationsRegistered()
    {
        if (TempData["FsmApplicationResponse"] == null) return RedirectToAction("Index", "Home");

        var vm = JsonConvert.DeserializeObject<List<ApplicationSaveItemResponse>>(TempData["FsmApplicationResponse"]
            .ToString());
        // Re-save so a page refresh doesn't lose it (TempData only survives one request by default).
        TempData["FsmApplicationResponse"] = JsonConvert.SerializeObject(vm);

        OrganisationCategory organisationType = _Claims.Organisation.Category.Id;
        TempData["organisationType"] = organisationType;

        var tier = TempData["FSM_Tier"] as string;
        var endDate = TempData["FSM_EndDate"] as string;

        string? formattedEndDate = null;
        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var parsed))
        {
            formattedEndDate = parsed.ToString("dd MMMM yyyy");
        }
        ViewBag.Tier = tier;
        ViewBag.FormattedEndDate = formattedEndDate;

        return View("ApplicationsRegistered", vm);
    }


    [HttpGet]
    public IActionResult AppealsRegistered()
    {
        if (TempData["FsmApplicationResponse"] == null) return RedirectToAction("Index", "Home");

        var vm = JsonConvert.DeserializeObject<List<ApplicationSaveItemResponse>>(TempData["FsmApplicationResponse"]
            .ToString());
        // Re-save so a page refresh doesn't lose it (TempData only survives one request by default).
        TempData["FsmApplicationResponse"] = JsonConvert.SerializeObject(vm);
        return View("AppealsRegistered", vm);
    }

    [HttpPost]
    public IActionResult ShowUploadEvidence(FsmApplication request)
    {
        // Re-posting the full FsmApplication triggers auto-validation of fields this page doesn't
        // care about; matches the same ModelState.Clear() already used by UploadEvidence for this reason.
        ModelState.Clear();

        // Refresh TempData with what was just posted (rather than trusting whatever's already there,
        // which could belong to a different tab) so the evidence submission reads the right data. See ELIG-3594.
        TempData["FsmApplication"] = JsonConvert.SerializeObject(request);
        return View("UploadEvidence", request);
    }

    [HttpGet]
    public IActionResult UploadEvidence()
    {
        if (TempData["FsmApplication"] != null)
        {
            var fsmApplication = JsonConvert.DeserializeObject<FsmApplication>(TempData["FsmApplication"].ToString());
            TempData["FsmApplication"] = JsonConvert.SerializeObject(fsmApplication);
            return View(fsmApplication);
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadEvidence(FsmApplication request, string actionType)
    {
        ModelState.Clear();
        var isValid = true;

        var evidenceExists = false;

        if (string.Equals(actionType, "email"))
        {
            evidenceExists = true;
        }

        var updatedRequest = new FsmApplication
        {
            ParentFirstName = request.ParentFirstName,
            ParentLastName = request.ParentLastName,
            ParentNino = request.ParentNino,
            ParentNass = request.ParentNass ?? string.Empty, // Ensure not null
            ParentDateOfBirth = request.ParentDateOfBirth,
            ParentEmail = request.ParentEmail,
            Tier = request.Tier,
            EligibilityEndDate = request.EligibilityEndDate,
            Children = request.Children,
            Evidence = new Evidence { EvidenceList = new List<EvidenceFile>() }
        };

        // Retrieve existing application with evidence from TempData
        if (TempData["FsmApplication"] != null)
        {
            var existingApplication = JsonConvert.DeserializeObject<FsmApplication>(TempData["FsmApplication"].ToString());

            // Add existing evidence files if they exist
            if (existingApplication?.Evidence?.EvidenceList != null && existingApplication.Evidence.EvidenceList.Any())
            {
                updatedRequest.Evidence.EvidenceList.AddRange(existingApplication.Evidence.EvidenceList);
                evidenceExists = true;
            }
        }

        //Handle no evidence files selected
        if ((request.EvidenceFiles == null || !request.EvidenceFiles.Any()) && !evidenceExists)
        {
            isValid = false;
            ModelState.AddModelError("EvidenceFiles", $"You have not selected a file");
            TempData["ErrorMessage"] = "You have not selected a file";
        }

        // Process new files from the form if any were uploaded
        if (request.EvidenceFiles != null && request.EvidenceFiles.Count > 0)
        {
            foreach (var file in request.EvidenceFiles)
            {
                var validationResult = _validateEvidenceFileUse.Execute(file);
                if (!validationResult.IsValid)
                {
                    isValid = false;
                    TempData["ErrorMessage"] = validationResult.ErrorMessage;

                    continue;
                }

                try
                {
                    if (file.Length > 0)
                    {
                        string blobUrl = await _uploadEvidenceFileUseCase.Execute(file, _config["AzureStorageEvidence:EvidenceFilesContainerName"]);

                        updatedRequest.Evidence.EvidenceList.Add(new EvidenceFile
                        {
                            FileName = file.FileName,
                            FileType = file.ContentType,
                            StorageAccountReference = blobUrl
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload evidence file {FileName}", file.FileName);
                    ModelState.AddModelError("EvidenceFiles", $"Failed to upload file {file.FileName}");
                }
            }
        }

        // preserve any evidence files that came from the form submission
        if (request.Evidence?.EvidenceList != null && request.Evidence.EvidenceList.Any())
        {
            var existingFiles = updatedRequest.Evidence.EvidenceList
                .Select(f => f.StorageAccountReference)
                .ToHashSet();

            foreach (var file in request.Evidence.EvidenceList)
            {
                // Only add files that aren't already in our list
                if (!string.IsNullOrEmpty(file.StorageAccountReference) &&
                    !existingFiles.Contains(file.StorageAccountReference))
                {
                    updatedRequest.Evidence.EvidenceList.Add(file);
                    existingFiles.Add(file.StorageAccountReference);
                }
            }
        }

        TempData["FsmApplication"] = JsonConvert.SerializeObject(updatedRequest);

        if (!ModelState.IsValid || !isValid)
        {
            return View("UploadEvidence", updatedRequest);
        }

        // Render directly from the application just built (rather than redirecting and reading
        // TempData back), so a concurrent tab overwriting TempData can't hijack this render. See ELIG-3594.
        OrganisationCategory organisationTypeForAnswers = _Claims.Organisation.Category.Id;
        TempData["organisationType"] = organisationTypeForAnswers;
        return View("Check_Answers", updatedRequest);
    }

    [HttpPost]
    public IActionResult ContinueWithoutMoreFiles(FsmApplication request)
    {
        var application = new FsmApplication
        {
            ParentFirstName = request.ParentFirstName,
            ParentLastName = request.ParentLastName,
            ParentNino = request.ParentNino,
            ParentNass = request.ParentNass,
            ParentDateOfBirth = request.ParentDateOfBirth,
            ParentEmail = request.ParentEmail,
            Children = request.Children,
            Evidence = request.Evidence,
            Tier = request.Tier,
            EligibilityEndDate = request.EligibilityEndDate
        };

        TempData["FsmApplication"] = JsonConvert.SerializeObject(application);

        // Render directly (see ELIG-3594 comment above) rather than redirecting through TempData.
        OrganisationCategory organisationTypeForContinue = _Claims.Organisation.Category.Id;
        TempData["organisationType"] = organisationTypeForContinue;
        return View("Check_Answers", application);
    }
}