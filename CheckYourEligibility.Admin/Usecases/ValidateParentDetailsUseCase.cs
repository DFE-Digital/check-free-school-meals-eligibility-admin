using CheckYourEligibility.Admin.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CheckYourEligibility.Admin.UseCases;

public interface IValidateParentDetailsUseCase
{
    ValidationResult Execute(ParentGuardian request, ModelStateDictionary modelState);
}

public class ValidationResult
{
    public bool IsValid { get; set; }

    public Dictionary<string, List<string>> Errors { get; set; }
}

public class ValidateParentDetailsUseCase : IValidateParentDetailsUseCase
{
    private readonly ILogger<ValidateParentDetailsUseCase> _logger;

    public ValidateParentDetailsUseCase(ILogger<ValidateParentDetailsUseCase> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValidationResult Execute(ParentGuardian request, ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(x => x.Value.Errors.Count > 0)
            .ToDictionary(
                k => k.Key,
                v => v.Value.Errors.Select(e => e.ErrorMessage).ToList()
            );

        if (!modelState.IsValid)
        {
            return new ValidationResult { IsValid = false, Errors = errors };
        }
        return new ValidationResult { IsValid = true, Errors = errors };
    }
}