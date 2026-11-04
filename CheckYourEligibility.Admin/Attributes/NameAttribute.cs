using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CheckYourEligibility.Admin.Attributes;

public class NameAttribute : ValidationAttribute
{
    public static readonly string NameValidationRegex = @"^[a-zA-Z" +
            @"ÁáÉéÍíÓóÚúÝýĆćĹĺŃńŔŕŚśŹź" +
            @"ÀàÈèÌìÒòÙùẀẁỲỳ" +
            @"ÂâÊêÎîÔôÛûĈĉĜĝĤĥĴĵŜŝŴŵŶŷ" +
            @"ÃãÑñÕõĨĩŨũẼẽỸỹ" +
            @"ÄäËëÏïÖöÜüŸÿ" +
            @"ÇçĢģĶķĻļŅņŖŗŞşŢţ" +
            @"ÅåŮů" +
            @"ĀāĒēĪīŌōŪūȲȳ" +
            @"ĂăĔĕĞğĬĭŎŏŬŭ" +
            @"ĊċĖėĠġİẊẋŻż" +
            @"ĄąĘęĮįŲų" +
            @"ŐőŰű" +
            @" ,.''\u2018\u2019-]+$";

    private static readonly Regex regex = new(NameValidationRegex);

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null || value == "")
            return ValidationResult.Success;

        if (regex.IsMatch(value.ToString()))
            return ValidationResult.Success;

        return validationContext.MemberName switch
        {
            "FirstName" => new ValidationResult("First Name field contains an invalid character"),
            "LastName" => new ValidationResult("Last Name field contains an invalid character"),
            _ => new ValidationResult("Name field contains an invalid character")
        };
    }
}
