using System.ComponentModel.DataAnnotations;
using CheckYourEligibility.API.Domain.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NinValidatorAttribute : ValidationAttribute
{
    public NinValidatorAttribute()
    {
        ErrorMessage = "Enter a National Insurance number in the correct format";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string nino)
        {
            return new ValidationResult("National Insurance number is required");
        }

        return DataValidation.BeAValidNi(nino)
            ? ValidationResult.Success
            : new ValidationResult("Enter a National Insurance number in the correct format");
    }
}
