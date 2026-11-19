using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.Models;
using CheckYourEligibility.API.Domain.Validation;

namespace CheckYourEligibility.Admin.UseCases;

public interface IPerformEligibilityCheckUseCase
{
    Task<CheckEligibilityResponse> Execute(
        ParentGuardian parentRequest
    );
}

public class PerformEligibilityCheckUseCase : IPerformEligibilityCheckUseCase
{
    private readonly ICheckGateway _checkGateway;

    public PerformEligibilityCheckUseCase(ICheckGateway checkGateway)
    {
        _checkGateway = checkGateway ?? throw new ArgumentNullException(nameof(checkGateway));
    }

    public async Task<CheckEligibilityResponse> Execute(
        ParentGuardian parentRequest)
    {
        // Build DOB string
        var dobString = new DateOnly(
            int.Parse(parentRequest.Year),
            int.Parse(parentRequest.Month),
            int.Parse(parentRequest.Day)
        ).ToString("yyyy-MM-dd");
        
        // Note: this need be refactored as part of the' check to application'  journey TempData removal work
        parentRequest.NationalInsuranceNumber =  NinoValidation.Normalize(parentRequest.NationalInsuranceNumber);;
        var checkEligibilityRequest = new CheckEligibilityRequest_Enhanced
        {
            Data = new CheckEligibilityRequestData_Enhanced
            {
                LastName = parentRequest.LastName,
                NationalInsuranceNumber =  parentRequest.NationalInsuranceNumber,
                NationalAsylumSeekerServiceNumber = parentRequest.NationalAsylumSeekerServiceNumber ?? string.Empty,
                DateOfBirth = dobString
            }
        };

        var response = await _checkGateway.PostCheck(checkEligibilityRequest);

        return response;
    }
}