
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public enum NinAsrSelect
{
    None = 0,
    NinSelected = 1,
    AsrnSelected = 2
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NinValidatorAttribute : ValidationAttribute
{
    private readonly string _selectionPropertyName;
    private readonly Regex _regex;

    public NinValidatorAttribute(string selectionPropertyName)
    {
        _selectionPropertyName = selectionPropertyName ?? throw new ArgumentNullException(nameof(selectionPropertyName));
        ErrorMessage = "Invalid National Insurance number format";
        _regex = new Regex("^[A-Z0-9]{2}\\d{6}[A-D]?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        PropertyInfo selectionProp = validationContext.ObjectType.GetProperty(_selectionPropertyName);
        if (selectionProp == null)
        {
            return new ValidationResult(
                $"Unknown property '{_selectionPropertyName}' referenced by {nameof(NinValidatorAttribute)}.");
        }

        object selectionValue = selectionProp.GetValue(validationContext.ObjectInstance);
        
        string selectionAsString = selectionValue?.ToString();

        // If ASR is selected stop validating NIN option
        if (string.Equals(selectionAsString, nameof(NinAsrSelect.AsrnSelected), StringComparison.Ordinal))
            return ValidationResult.Success;

        // Neither option selected
        if (string.Equals(selectionAsString, nameof(NinAsrSelect.None), StringComparison.Ordinal))
            return new ValidationResult("Please select one option");

        // NIN selected but not provided
        if (string.Equals(selectionAsString, nameof(NinAsrSelect.NinSelected), StringComparison.Ordinal) && value == null)
            return new ValidationResult("National Insurance number is required");

        // NIN selected and provided - validate
        if (string.Equals(selectionAsString, nameof(NinAsrSelect.NinSelected), StringComparison.Ordinal) && value != null)
        {
            var nino = new string(value.ToString()
                                   .ToUpperInvariant()
                                   .Where(char.IsLetterOrDigit)
                                   .ToArray());

            if (nino.Length > 9)
            {
                return new ValidationResult(
                    "National Insurance number should contain no more than 9 alphanumeric characters");
            }

            if (!_regex.IsMatch(nino))
            {
                return new ValidationResult("Invalid National Insurance number format");
            }
        }

        return ValidationResult.Success;
    }
}

