// Ignore Spelling: Finalise

using System.Globalization;
using System.Text;
using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Domain.DfeSignIn;
using CheckYourEligibility.Admin.Domain.Enums;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.Infrastructure;
using CheckYourEligibility.Admin.Models;
using CheckYourEligibility.Admin.UseCases;
using CheckYourEligibility.Admin.ViewModels;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

//using CheckYourEligibility.Admin.Gateways.Domain;

namespace CheckYourEligibility.Admin.Controllers;

public class ApplicationController : BaseController
{
    private readonly IAdminGateway _adminGateway;
    private readonly IConfiguration _config;
    private readonly ILogger<ApplicationController> _logger;
    private readonly IDownloadEvidenceFileUseCase _downloadEvidenceFileUseCase;
    private readonly ISendNotificationUseCase _sendNotificationUseCase;
    protected DfeClaims? _Claims;

    public ApplicationController(ILogger<ApplicationController> logger, IAdminGateway adminGateway, IConfiguration configuration, IDownloadEvidenceFileUseCase downloadEvidenceFileUseCase, ISendNotificationUseCase sendNotificationUseCase)
    {
        _logger = logger;
        _adminGateway = adminGateway ?? throw new ArgumentNullException(nameof(adminGateway));
        _config = configuration;
        _downloadEvidenceFileUseCase = downloadEvidenceFileUseCase ?? throw new ArgumentNullException(nameof(downloadEvidenceFileUseCase));
        _sendNotificationUseCase = sendNotificationUseCase ?? throw new ArgumentNullException(nameof(sendNotificationUseCase));
    }

    private async Task<IActionResult> GetResults(ApplicationRequestSearch? applicationSearch, string detailView,
        bool showSelector, bool showSchool, bool showParentDob)
    {
        var response = await _adminGateway.PostApplicationSearch(applicationSearch);
        response ??= new ApplicationSearchResponse { Data = new List<ApplicationResponse>() };
        if (response.Data == null || (!response.Data.Any() && detailView == "ApplicationDetail"))
        {
            TempData["Message"] = "There are no records matching your search.";
            return RedirectToAction("Search");
        }

        var criteria = JsonConvert.SerializeObject(applicationSearch);
        TempData["SearchCriteria"] = criteria;
        ViewBag.CurrentPage = applicationSearch.PageNumber;
        ViewBag.TotalPages = response.TotalPages;
        ViewBag.TotalRecords = response.TotalRecords;
        ViewBag.RecordsPerPage = applicationSearch.PageSize;

        var viewModel = response.Data.Select(x => new SelectPersonEditorViewModel
        {
            DetailView = detailView,
            ShowSelectorCheck = showSelector,
            Person = x,
            ShowSchool = showSchool,
            ShowParentDob = showParentDob
        });

        var viewData = new PeopleSelectionViewModel { People = viewModel.ToList() };
        return View(viewData);
    }

    private async Task<IActionResult> GetResultsForSearch(ApplicationRequestSearch? applicationSearch,
        string detailView, bool showSelector, bool showSchool, bool showParentDob, SearchAllRecordsViewModel viewModel)
    {
        var response = await _adminGateway.PostApplicationSearch(applicationSearch);
        response ??= new ApplicationSearchResponse { Data = new List<ApplicationResponse>() };
        if (response.Data == null || (!response.Data.Any() && detailView == "ApplicationDetail"))
        {
            TempData["Message"] = "There are no records matching your search.";
            return View(viewModel);
        }

        var criteria = JsonConvert.SerializeObject(applicationSearch);
        TempData["SearchCriteria"] = criteria;
        ViewBag.CurrentPage = applicationSearch.PageNumber;
        ViewBag.TotalPages = response.TotalPages;
        ViewBag.TotalRecords = response.TotalRecords;
        ViewBag.RecordsPerPage = applicationSearch.PageSize;
        if (applicationSearch.Data.Keyword != null) ViewBag.Keyword = applicationSearch.Data.Keyword;
        if (applicationSearch.Data.Statuses != null) ViewBag.Status = applicationSearch.Data.Statuses;

        viewModel.People = response.Data.Select(x => new SearchAllRecordsViewModel
        {
            DetailView = detailView,
            ShowSelectorCheck = showSelector,
            Person = x,
            ShowSchool = showSchool,
            ShowParentDob = showParentDob
        }).ToList();

        return View(viewModel);
    }

    private static ApplicationDetailViewModel GetViewData(ApplicationItemResponse response)
    {
        var viewData = new ApplicationDetailViewModel
        {
            Id = response.Data.Id,
            Reference = response.Data.Reference,
            ParentName = $"{response.Data.ParentFirstName} {response.Data.ParentLastName}",
            ParentEmail = response.Data.ParentEmail,
            ParentNas = response.Data.ParentNationalAsylumSeekerServiceNumber,
            ParentNI = response.Data.ParentNationalInsuranceNumber,
            Status = response.Data.Status,
            ChildName = $"{response.Data.ChildFirstName} {response.Data.ChildLastName}",
            School = response.Data.Establishment.Name
        };
        viewData.ParentDob = DateTime
            .ParseExact(response.Data.ParentDateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            .ToString("d MMMM yyyy");
        viewData.ChildDob = DateTime
            .ParseExact(response.Data.ChildDateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            .ToString("d MMMM yyyy");
        viewData.Evidence = response.Data.Evidence;

        return viewData;
    }


    private byte[] WriteCsvToMemory(IEnumerable<ApplicationExport> records)
    {
        using (var memoryStream = new MemoryStream())
        using (var streamWriter = new StreamWriter(memoryStream))
        using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
        {
            csvWriter.WriteRecords(records);
            streamWriter.Flush();
            return memoryStream.ToArray();
        }
    }

    private bool CheckAccess(ApplicationItemResponse response)
    {
        _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);
        if ((_Claims.Organisation.Category.Name == Constants.CategoryTypeSchool
                ? Convert.ToInt32(_Claims.Organisation.Urn)
                : null) != null)
            if (response.Data.Establishment.Id.ToString() != _Claims.Organisation.Urn)
            {
                _logger.LogError(
                    $"Invalid School access attempt {response.Data.Establishment.Id} organisation Urn:-{_Claims.Organisation.Urn}");
                return false;
            }

        if ((_Claims.Organisation.Category.Name == Constants.CategoryTypeLA
                ? Convert.ToInt32(_Claims.Organisation.Urn)
                : null) != null)
            if (response.Data.Establishment.LocalAuthority.Id.ToString() != _Claims.Organisation.EstablishmentNumber)
            {
                _logger.LogError(
                    $"Invalid Local Authority access attempt {response.Data.Establishment.LocalAuthority.Id} organisation Urn:-{_Claims.Organisation.Urn}");
                return false;
            }

        return true;
    }

    #region Search

    [HttpGet]
    public IActionResult Search()
    {
        if (TempData["Message"] != null) ViewBag.Message = TempData["Message"];
        if (TempData["Errors"] != null)
        {
            var errors = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(TempData["Errors"].ToString());
            foreach (var kvp in errors)
                foreach (var error in kvp.Value)
                    ModelState.AddModelError(kvp.Key, error);
        }

        return View();
    }

    public async Task<IActionResult> SearchResults(ApplicationSearch request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ApplicationSearch"] = JsonConvert.SerializeObject(request);
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToList());
            TempData["Errors"] = JsonConvert.SerializeObject(errors);
            return View(new SearchAllRecordsViewModel { ApplicationSearch = request });
        }

        _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);
        var applicationSearch = new ApplicationRequestSearch
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Data = new ApplicationRequestSearchData
            {
                LocalAuthority = _Claims.Organisation.Category.Name == Constants.CategoryTypeLA
                    ? Convert.ToInt32(_Claims.Organisation.EstablishmentNumber)
                    : null,
                MultiAcademyTrust = _Claims.Organisation.Category.Name == Constants.CategoryTypeMAT
                    ? Convert.ToInt32(_Claims.Organisation.Uid)
                    : null,
                Establishment = _Claims.Organisation.Category.Name == Constants.CategoryTypeSchool
                    ? Convert.ToInt32(_Claims.Organisation.Urn)
                    : null,
                Keyword = request.Keyword,
                DateRange = request.DateRange != null
                    ? new DateRange
                    {
                        DateFrom = request.DateRange.DateFrom,
                        DateTo = DateTime.Now
                    }
                    : null,
                Statuses = request.Status.Any() ? request.Status : null // Apply filter only if statuses are selected
            }
        };

        TempData["ApplicationSearch"] = JsonConvert.SerializeObject(request);

        var viewModel = new SearchAllRecordsViewModel
        {
            ApplicationSearch = request
        };

        return await GetResultsForSearch(applicationSearch, "ApplicationDetail", false, false, false, viewModel);
    }


    [HttpGet]
    public async Task<IActionResult> ApplicationDetail(string id)
    {
        var response = await _adminGateway.GetApplication(id);
        _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);
        var org = _Claims.Organisation.Category.Name;
        if (response == null) return NotFound();
        if (!CheckAccess(response)) return new ContentResult { StatusCode = StatusCodes.Status403Forbidden };
        ViewData["OrganisationCategory"] = org;
        return View(GetViewData(response));
    }


    [HttpGet]
    public async Task<IActionResult> ExportSearchResults()
    {
        try
        {
            _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);

            // Get the current search criteria the same way the search does
            var currentSearch =
                JsonConvert.DeserializeObject<ApplicationRequestSearch>(TempData["SearchCriteria"].ToString());

            // Ensure we get all results for the current search
            currentSearch.PageSize = int.MaxValue;
            currentSearch.PageNumber = 1;

            // Keep TempData available for the redirect if needed
            TempData.Keep("SearchCriteria");

            var response = await _adminGateway.PostApplicationSearch(currentSearch);

            if (response?.Data == null || !response.Data.Any())
                return RedirectToAction("SearchResults", new { PageNumber = 1 });

            var csvContent = new StringBuilder();
            csvContent.AppendLine("Reference," +
                                  "Status," +
                                  "Parent First Name," +
                                  "Parent Last Name," +
                                  "Parent Email," +
                                  "Parent DOB," +
                                  "Parent NI Number," +
                                  "Child First Name," +
                                  "Child Last Name," +
                                  "Child DOB," +
                                  "Establishment," +
                                  "Local Authority," +
                                  "Submission Date");

            foreach (var app in response.Data)
                csvContent.AppendLine(string.Format(
                    "{0},{1},\"{2}\",\"{3}\",\"{4}\",{5},{6},\"{7}\",\"{8}\",{9},\"{10}\",\"{11}\",{12}",
                    app.Reference,
                    app.Status,
                    app.ParentFirstName?.Replace("\"", "\"\""),
                    app.ParentLastName?.Replace("\"", "\"\""),
                    app.ParentEmail?.Replace("\"", "\"\""),
                    app.ParentDateOfBirth,
                    app.ParentNationalInsuranceNumber?.Replace("\"", "\"\"") ?? "",
                    app.ChildFirstName?.Replace("\"", "\"\""),
                    app.ChildLastName?.Replace("\"", "\"\""),
                    app.ChildDateOfBirth,
                    app.Establishment?.Name?.Replace("\"", "\"\"") ?? "",
                    app.Establishment?.LocalAuthority?.Name?.Replace("\"", "\"\"") ?? "",
                    app.Created.ToString("dd/MM/yyyy")));

            return File(
                Encoding.UTF8.GetBytes(csvContent.ToString()),
                "text/csv",
                $"eligibility-applications-{DateTime.Now:yyyyMMddHHmmss}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting search results to CSV");
            return RedirectToAction("SearchResults", new { PageNumber = 1 });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEvidence(string id, string blobReference)
    {
        try
        {
            var response = await _adminGateway.GetApplication(id);
            if (response == null) return NotFound();
            
            // Should we check something here to ensure the user has access to the evidence?
            if (!CheckAccess(response)) 
                return new ContentResult { StatusCode = StatusCodes.Status403Forbidden, Content = "You don't have permission to download this file." };
            
            
            var evidenceFile = response.Data.Evidence?.FirstOrDefault(e => e.StorageAccountReference == blobReference);
            if (evidenceFile == null)
                return NotFound("The requested file was not found.");
            
            // Extract blob name from URL if it's a full URL (for backward compatibility)
            string blobName = blobReference;
            if (blobReference.Contains("/"))
            {
                blobName = blobReference.Substring(blobReference.LastIndexOf('/') + 1);
                _logger.LogInformation($"Converting legacy URL format to blob name: {blobReference.Replace(Environment.NewLine, "")} -> {blobName.Replace(Environment.NewLine, "")}");
            }
            
            var (fileStream, contentType) = await _downloadEvidenceFileUseCase.Execute(blobName, _config["AzureStorageEvidence:EvidenceFilesContainerName"]);
            
            return File(fileStream, contentType, evidenceFile.FileName);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning($"Evidence file not found: {blobReference.Replace(Environment.NewLine, "")}");
            return NotFound("The requested file was not found in storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading evidence file: {blobReference.Replace(Environment.NewLine, "")}");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while downloading the file.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ViewEvidence(string id, string blobReference)
    {
        try
        {
            var response = await _adminGateway.GetApplication(id);
            if (response == null) return NotFound();
            
            // Check access permissions
            if (!CheckAccess(response)) 
                return new ContentResult { StatusCode = StatusCodes.Status403Forbidden, Content = "You don't have permission to view this file." };
            
            var evidenceFile = response.Data.Evidence?.FirstOrDefault(e => e.StorageAccountReference == blobReference);
            if (evidenceFile == null)
                return NotFound("The requested file was not found.");
            
            // Extract blob name from URL if it's a full URL (for backward compatibility)
            string blobName = blobReference;
            if (blobReference.Contains("/"))
            {
                blobName = blobReference.Substring(blobReference.LastIndexOf('/') + 1);
                _logger.LogInformation($"Converting legacy URL format to blob name: {blobReference.Replace(Environment.NewLine, "")} -> {blobName.Replace(Environment.NewLine, "")}");
            }
            
            var (fileStream, contentType) = await _downloadEvidenceFileUseCase.Execute(blobName, _config["AzureStorageEvidence:EvidenceFilesContainerName"]);
                        
            return File(fileStream, contentType);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning($"Evidence file not found: {blobReference.Replace(Environment.NewLine, "")}");
            return NotFound("The requested file was not found in storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error viewing evidence file: {blobReference.Replace(Environment.NewLine, "")}");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while attempting to view the file.");
        }
    }

    #endregion

    #region School Appeals

    [HttpGet]
    public async Task<IActionResult> AppealsApplications(int PageNumber)
    {
        var applicationSearch = GetApplicationsForStatuses(
            new List<ApplicationStatus>
            {
                ApplicationStatus.EvidenceNeeded,
                ApplicationStatus.SentForReview
            },
            PageNumber, 10);
        return await GetResults(applicationSearch, "ApplicationDetailAppeal", false, false, false);
    }

    [HttpGet]
    public IActionResult EvidenceGuidance()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationDetailAppeal(string id)
    {
        var response = await _adminGateway.GetApplication(id);
        if (response == null) return NotFound();
        if (!CheckAccess(response)) return new ContentResult { StatusCode = StatusCodes.Status403Forbidden };
        HttpContext.Session.SetString("ApplicationReference", response.Data.Reference);
        return View(GetViewData(response));
    }


    [HttpGet]
    public async Task<IActionResult> ApplicationDetailAppealConfirmation(string id)
    {
        TempData["AppAppealID"] = id;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationDetailAppealSend(string id)
    {
        var checkAccess = await ConfirmCheckAccess(id);
        if (checkAccess != null) return checkAccess;

        await _adminGateway.PatchApplicationStatus(id, ApplicationStatus.SentForReview);

        var application = await _adminGateway.GetApplication(id);
        if (application == null || application.Data == null)
        {
            _logger.LogError($"Application not found for ID: {id.Replace(Environment.NewLine, "")}");
            return NotFound("Application not found");
        }

        try 
        {
            var notificationRequest = new NotificationRequest
            {
                Data = new NotificationRequestData
                {
                    Email = application.Data.ParentEmail,
                    Type = NotificationType.ParentApplicationEvidenceSent,
                    Personalisation = new Dictionary<string, object>
                    {
                        { "reference", application.Data.Reference },
                        { "parentFirstName", $"{application.Data.ParentFirstName}" }
                    }
                }
            };
            
            await _sendNotificationUseCase.Execute(notificationRequest);
            _logger.LogInformation($"Notification sent for application {application.Data.Reference}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send notification for application {application?.Data?.Reference ?? id.Replace(Environment.NewLine, "")}");
        }

        return RedirectToAction("ApplicationDetailAppealConfirmationSent", new { id });
    }


    [HttpGet]
    public async Task<IActionResult> ApplicationDetailAppealConfirmationSent(string id)
    {
        ViewBag.AppReference = HttpContext.Session.GetString("ApplicationReference");
        TempData["AppAppealID"] = id;
        return View();
    }

    private async Task<IActionResult> ConfirmCheckAccess(string id)
    {
        var response = await _adminGateway.GetApplication(id);
        if (response == null) return NotFound();

        var access = CheckAccess(response);

        if ((access == false) | (response.Data.Id != id))
            return new ContentResult { StatusCode = StatusCodes.Status403Forbidden };
        return null;
    }

    #endregion

    #region School finalise Applications

    [HttpGet]
    public async Task<IActionResult> FinaliseApplications(int PageNumber)
    {
        var applicationSearch = GetApplicationsForStatuses(
            new List<ApplicationStatus>
            {
                ApplicationStatus.Entitled,
                ApplicationStatus.ReviewedEntitled
            },
            PageNumber, 10);
        return await GetResults(applicationSearch, "ApplicationDetailFinalise", true, false, false);
    }


    private ApplicationRequestSearch GetApplicationsForStatuses(IEnumerable<ApplicationStatus> statuses,
        int pageNumber, int pageSize)
    {
        ApplicationRequestSearch applicationSearch;
        if (pageNumber == 0)
        {
            _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);
            applicationSearch = new ApplicationRequestSearch
            {
                PageNumber = 1,
                PageSize = pageSize,
                Data = new ApplicationRequestSearchData
                {
                    LocalAuthority = _Claims.Organisation.Category.Name == Constants.CategoryTypeLA
                        ? Convert.ToInt32(_Claims.Organisation.EstablishmentNumber)
                        : null,
                    MultiAcademyTrust = _Claims.Organisation.Category.Name == Constants.CategoryTypeMAT
                        ? Convert.ToInt32(_Claims.Organisation.Uid)
                        : null,
                    Establishment = _Claims.Organisation.Category.Name == Constants.CategoryTypeSchool
                        ? Convert.ToInt32(_Claims.Organisation.Urn)
                        : null,
                    Statuses = statuses
                }
            };
        }
        else
        {
            applicationSearch =
                JsonConvert.DeserializeObject<ApplicationRequestSearch>(TempData["SearchCriteria"].ToString());
            applicationSearch.PageNumber = pageNumber;
        }

        return applicationSearch;
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationDetailFinalise(string id)
    {
        var response = await _adminGateway.GetApplication(id);
        if (response == null) return NotFound();
        if (!CheckAccess(response)) return new ContentResult { StatusCode = StatusCodes.Status403Forbidden };

        return View(GetViewData(response));
    }

    [HttpPost]
    public ActionResult FinaliseSelectedApplications(PeopleSelectionViewModel model)
    {
        var selectedIds = model.getSelectedIds();

        if (selectedIds.Any())
        {
            TempData["FinaliseApplicationIds"] = selectedIds;
        }
        else
        {
            TempData["ErrorMessage"] = "Select records to finalise";
            return RedirectToAction("FinaliseApplications", new { PageNumber = 0 });
        }

        return View("ApplicationFinaliseConfirmation");
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationFinaliseSend()
    {
        if (TempData["FinaliseApplicationIds"] != null)
            foreach (var id in TempData["FinaliseApplicationIds"] as IEnumerable<string>)
                await _adminGateway.PatchApplicationStatus(id, ApplicationStatus.Receiving);

        return RedirectToAction("FinaliseApplications");
    }

    public async Task<IActionResult> FinalisedApplicationsdownload()
    {
        var applicationSearch = GetApplicationsForStatuses(
            new List<ApplicationStatus>
            {
                ApplicationStatus.Entitled,
                ApplicationStatus.ReviewedEntitled
            },
            0, int.MaxValue);
        var resultData = await _adminGateway.PostApplicationSearch(applicationSearch);

        var fileName = $"finalise-applications-{DateTime.Now.ToString("yyyyMMdd")}.csv";

        var result = WriteCsvToMemory(resultData.Data.Select(x => new ApplicationExport
        {
            Reference = x.Reference,
            Parent = $"{x.ParentFirstName} {x.ParentLastName}",
            Child = $"{x.ChildFirstName} {x.ChildLastName}",
            ChildDOB = Convert.ToDateTime(x.ChildDateOfBirth).ToString("d MMM yyyy"),
            Status = x.Status.GetFsmStatusDescription(),
            SubmisionDate = x.Created.ToString("d MMM yyyy")
        }));
        var memoryStream = new MemoryStream(result);
        return new FileStreamResult(memoryStream, "text/csv") { FileDownloadName = fileName };
    }

    #endregion

    #region LA

    public async Task<IActionResult> PendingApplications(int PageNumber)
    {
        var applicationSearch = GetApplicationsForStatuses(
            new List<ApplicationStatus>
            {
                ApplicationStatus.SentForReview
            },
            PageNumber, 10);
        return await GetResults(applicationSearch, "ApplicationDetailLa", false, true, true);
    }


    [HttpGet]
    public async Task<IActionResult> ApplicationDetailLa(string id)
    {
        var response = await _adminGateway.GetApplication(id);
        _Claims = DfeSignInExtensions.GetDfeClaims(HttpContext.User.Claims);
        var org = _Claims.Organisation.Category.Name;
        if (response == null) return NotFound();
        if (!CheckAccess(response)) return new UnauthorizedResult();
        ViewData["OrganisationCategory"] = org;
        return View(GetViewData(response));
    }

    [HttpGet]
    public async Task<IActionResult> ApproveConfirmation(string id)
    {
        TempData["AppApproveId"] = id;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> DeclineConfirmation(string id)
    {
        TempData["AppApproveId"] = id;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationApproved(string id)
    {
        var response = await _adminGateway.GetApplication(id);
        if (response == null) return NotFound();
        if (!CheckAccess(response)) return new UnauthorizedResult();
        return View(GetViewData(response));
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationDeclined(string id)
    {
        var response = await _adminGateway.GetApplication(id);
        if (response == null) return NotFound();
        if (!CheckAccess(response)) return new UnauthorizedResult();
        return View(GetViewData(response));
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationApproveSend(string id)
    {
        var checkAccess = await ConfirmCheckAccess(id);
        if (checkAccess != null) return checkAccess;

        await _adminGateway.PatchApplicationStatus(id, ApplicationStatus.ReviewedEntitled);

        return RedirectToAction("ApplicationApproved", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationDeclineSend(string id)
    {
        var checkAccess = await ConfirmCheckAccess(id);
        if (checkAccess != null) return checkAccess;

        await _adminGateway.PatchApplicationStatus(id, ApplicationStatus.ReviewedNotEntitled);

        return RedirectToAction("ApplicationDeclined", new { id });
    }

    #endregion
}