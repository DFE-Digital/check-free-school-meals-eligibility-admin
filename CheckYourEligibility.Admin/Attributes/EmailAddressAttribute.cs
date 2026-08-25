using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CheckYourEligibility.Admin.Models;

public class EmailAddressAttribute : ValidationAttribute
{
    private const string ValidationErrorMessage =
        "Enter an email address in the correct format, like name@example.com";

    private const string LocalPartPattern = @"^[a-zA-Z0-9._'+-]+$";

    private const string DomainPartPattern =
        @"^[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$";

    public static bool IsValidEmailAddress(string? value)
    {
        if (value == null)
            return true;

        if (value.Contains(' ') || value.Length > 320)
            return false;

        var index = value.IndexOf('@');
        if (index <= 0 || index == value.Length - 1 || index != value.LastIndexOf('@'))
            return false;

        var localPart = value[..index];
        var domainPart = value[(index + 1)..];

        if (localPart.Length > 64 || domainPart.Length > 255)
            return false;

        if (!IsValidLocalPart(localPart) || !IsValidDomainPart(domainPart))
        {
            if (ContainsUnicodeCharacters(domainPart))
                return IsValidInternationalizedDomainPart(domainPart);

            return false;
        }

        return true;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        // Skip this validation when searching application records.
        var parentObject = validationContext.ObjectInstance.GetType()
            .GetProperty(validationContext.MemberName)
            ?.DeclaringType;

        if (parentObject == typeof(ApplicationSearch))
            return ValidationResult.Success;

        if (value != null && value is not string)
            return new ValidationResult(ValidationErrorMessage);

        return IsValidEmailAddress(value as string)
            ? ValidationResult.Success
            : new ValidationResult(ValidationErrorMessage);
    }

    private static bool IsValidLocalPart(string localPart)
    {
        if (!Regex.IsMatch(localPart, LocalPartPattern))
            return false;

        return !localPart.StartsWith(".") && !localPart.EndsWith(".");
    }

    private static bool IsValidDomainPart(string domainPart)
    {
        if (!Regex.IsMatch(domainPart, DomainPartPattern))
            return false;

        return !domainPart.StartsWith(".") &&
               !domainPart.StartsWith("-") &&
               !domainPart.EndsWith(".") &&
               !domainPart.EndsWith("-");
    }

    private static bool ContainsUnicodeCharacters(string text)
    {
        return text.Any(c => c > 127);
    }

    private static bool IsValidInternationalizedDomainPart(string domainPart)
    {
        if (!domainPart.Contains("."))
            return false;

        if (domainPart.StartsWith(".") || domainPart.StartsWith("-") ||
            domainPart.EndsWith(".") || domainPart.EndsWith("-"))
            return false;

        return !domainPart.Contains("..");
    }
}
