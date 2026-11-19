using CheckYourEligibility.Admin.Domain.Constants;
using CheckYourEligibility.Admin.Domain.Constants.ErrorMessages;
using CheckYourEligibility.Admin.Models;
using CheckYourEligibility.API.Domain.Validation;
using System.ComponentModel.DataAnnotations;

namespace CheckYourEligibility.Admin.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NinoAttribute : ValidationAttribute
{
    public NinoAttribute()
    {
        ErrorMessage = ValidationMessages.InvalidNI;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is ParentGuardian check &&
            IdentifierTypeConstants.ShouldValidateNass(check.IdentifierType))
        {
            return ValidationResult.Success;
        }

        if (value is null || value is string { Length: 0 })
        {
            return new ValidationResult(ValidationMessages.NI_or_NASS);
        }

        var nino = value.ToString()!;
        var despacedNino = NinoValidation.RemoveSpaces(nino)!;
        var normalizedNino = NinoValidation.Normalize(nino)!;

        if (normalizedNino.Length > 9)
        {
            return new ValidationResult(
                "National Insurance number should contain no more than 9 alphanumeric characters");
        }

        if (!NinoValidation.IsValidInput(despacedNino))
        {
            return new ValidationResult(ValidationMessages.InvalidNI);
        }

        return ValidationResult.Success;
    }
}