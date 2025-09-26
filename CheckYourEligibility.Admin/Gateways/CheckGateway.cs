﻿using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using Newtonsoft.Json;

namespace CheckYourEligibility.Admin.Gateways;

public class CheckGateway : BaseGateway, ICheckGateway
{
    private readonly string _FsmCheckBulkUploadUrl;
    private readonly string _FsmCheckUrl;
    private readonly HttpClient _httpClient;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    public CheckGateway(ILoggerFactory logger, HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : base("EcsService",
        logger, httpClient, configuration, httpContextAccessor)
    {
        _logger = logger.CreateLogger("EcsService");
        _httpClient = httpClient;
        _FsmCheckUrl = "check/free-school-meals";
        _FsmCheckBulkUploadUrl = "bulk-check/free-school-meals";
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<CheckEligibilityResponse> PostCheck(CheckEligibilityRequest_Fsm requestBody)
    {
        try
        {
            var result = await ApiDataPostAsynch(_FsmCheckUrl, requestBody, new CheckEligibilityResponse());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Post Check failed. uri:-{_httpClient.BaseAddress}{_FsmCheckUrl} content:-{JsonConvert.SerializeObject(requestBody)}");
            throw;
        }
    }

    public async Task<CheckEligibilityStatusResponse> GetStatus(CheckEligibilityResponse responseBody)
    {
        try
        {
            var response = await ApiDataGetAsynch($"{responseBody.Links.Get_EligibilityCheck}/status",
                new CheckEligibilityStatusResponse());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Get Status failed. uri:-{_httpClient.BaseAddress}{responseBody.Links.Get_EligibilityCheck}/status");
        }

        return null;
    }

    public async Task<CheckEligibilityBulkStatusResponse> GetBulkCheckProgress(string bulkCheckUrl)
    {
        try
        {
            var result = await ApiDataGetAsynch(bulkCheckUrl, new CheckEligibilityBulkStatusResponse());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"get failed. uri:-{_httpClient.BaseAddress}{_FsmCheckBulkUploadUrl}");
        }

        return null;
    }

    public async Task<CheckEligibilityBulkResponse> GetBulkCheckResults(string resultsUrl)
    {
        try
        {
            var result = await ApiDataGetAsynch(resultsUrl, new CheckEligibilityBulkResponse());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"get failed. uri:-{_httpClient.BaseAddress}{_FsmCheckBulkUploadUrl}");
            throw;
        }
    }


    public async Task<CheckEligibilityResponseBulk> PostBulkCheck(CheckEligibilityRequestBulk_Fsm requestBody)
    {
        try
        {
            var result =
                await ApiDataPostAsynch(_FsmCheckBulkUploadUrl, requestBody, new CheckEligibilityResponseBulk());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Post failed. uri:-{_httpClient.BaseAddress}{_FsmCheckBulkUploadUrl} content:-{JsonConvert.SerializeObject(requestBody)}");
            throw;
        }
    }
}