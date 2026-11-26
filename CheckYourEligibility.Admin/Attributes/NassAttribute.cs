using CheckYourEligibility.Admin.Domain.Constants;
using CheckYourEligibility.Admin.Domain.Constants.ErrorMessages;
using CheckYourEligibility.Admin.Models;
using CheckYourEligibility.API.Domain.Validation;
using System.ComponentModel.DataAnnotations;

namespace CheckYourEligibility.Admin.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NassAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is ParentGuardian check &&
            !IdentifierTypeConstants.ShouldValidateNass(check.IdentifierType))
        {
            return ValidationResult.Success;
        }

        if (value is null || value is string { Length: 0 })
        {
            return validationContext.ObjectInstance is ParentGuardian
                ? new ValidationResult(ValidationMessages.NI_or_NASS)
                : ValidationResult.Success;
        }

        return value is string nass && DataValidation.BeAValidNass(nass)
            ? ValidationResult.Success
            : new ValidationResult(ValidationMessages.InvalidNASS);
    }
}