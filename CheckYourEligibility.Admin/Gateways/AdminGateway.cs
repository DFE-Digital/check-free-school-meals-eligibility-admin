﻿using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Domain.Enums;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using Newtonsoft.Json;

namespace CheckYourEligibility.Admin.Gateways;

public class AdminGateway : BaseGateway, IAdminGateway
{
    private readonly string _ApplicationSearchUrl = "application/search";
    private readonly string _ApplicationUrl = "/application";
    private readonly HttpClient _httpClient;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    public AdminGateway(ILoggerFactory logger, HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : base("EcsService",
        logger, httpClient, configuration, httpContextAccessor)
    {
        _logger = logger.CreateLogger("EcsService");
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<ApplicationItemResponse> GetApplication(string id)
    {
        try
        {
            // TODO: Check the first slash part of the _ApplicationUrl
            var response = await ApiDataGetAsynch($"{_httpClient.BaseAddress?.OriginalString}{_ApplicationUrl}/{id}",
                new ApplicationItemResponse());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Get failed. uri-{_httpClient.BaseAddress}{_ApplicationUrl}/{id.Replace(Environment.NewLine, "")}");
            throw;
        }
    }

    public async Task<ApplicationSearchResponse> PostApplicationSearch(ApplicationRequestSearch requestBody)
    {
        try
        {
            var result = await ApiDataPostAsynch(_ApplicationSearchUrl, requestBody, new ApplicationSearchResponse());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Post failed. uri:-{_httpClient.BaseAddress}{_ApplicationSearchUrl} content:-{JsonConvert.SerializeObject(requestBody)}");
            throw;
        }
    }

    public async Task<ApplicationStatusUpdateResponse> PatchApplicationStatus(string id, ApplicationStatus status)
    {
        var url = $"{_ApplicationUrl}/{id}";
        var request = new ApplicationStatusUpdateRequest
        {
            Data = new ApplicationStatusData { Status = status }
        };
        try
        {
            var result = await ApiDataPatchAsynch(url, request, new ApplicationStatusUpdateResponse());
            if (result.Data.Status != status.ToString()) throw new Exception("Failed to update status");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Post failed. uri:-{_httpClient.BaseAddress}{_ApplicationSearchUrl} content:-{JsonConvert.SerializeObject(request)}");
            throw;
        }
    }
}