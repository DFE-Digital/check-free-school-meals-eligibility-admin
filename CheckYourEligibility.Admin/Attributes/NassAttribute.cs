
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NassAttribute : ValidationAttribute
{
    private static readonly string NassPattern = @"^[0-9]{2}(0[1-9]|1[0-2])[0-9]{5,6}$";
    private static readonly Regex _regex = new(NassPattern, RegexOptions.Compiled);

    private readonly string _selectionPropertyName;
    private readonly string _noneName;
    private readonly string _ninName;
    private readonly string _asrName;

    public NassAttribute(
        string selectionPropertyName,
        string noneName = "None",
        string ninName = "NinSelected",
        string asrName = "AsrnSelected")
    {
        _selectionPropertyName = selectionPropertyName ?? throw new ArgumentNullException(nameof(selectionPropertyName));
        _noneName = noneName;
        _ninName = ninName;
        _asrName = asrName;
    }

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        var prop = context.ObjectType.GetProperty(_selectionPropertyName);
        if (prop == null)
            return new ValidationResult($"Unknown property '{_selectionPropertyName}' referenced by {nameof(NassAttribute)}.");

        var selectionName = prop.GetValue(context.ObjectInstance)?.ToString();

        if (string.Equals(selectionName, _ninName, StringComparison.Ordinal))
            return ValidationResult.Success;

        if (string.Equals(selectionName, _noneName, StringComparison.Ordinal))
            return new ValidationResult("Please select one option");

        if (string.Equals(selectionName, _asrName, StringComparison.Ordinal))
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return new ValidationResult("Asylum support reference number is required");

            var asr = value.ToString().Trim();
            if (!_regex.IsMatch(asr))
                return new ValidationResult("Nass field contains an invalid character");
        }

        return ValidationResult.Success;
    }
}
