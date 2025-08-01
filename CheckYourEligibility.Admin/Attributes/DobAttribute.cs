﻿using System.ComponentModel.DataAnnotations;
using Child = CheckYourEligibility.Admin.Models.Child;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class DobAttribute : ValidationAttribute
{
    private readonly bool _applyAgeRange;
    private readonly string _childIndexPropertyName;
    private readonly string _dayPropertyName;
    private readonly string _fieldName;
    private readonly bool _isRequired;
    private readonly string _monthPropertyName;
    private readonly string _objectName;
    private readonly string _yearPropertyName;

    public DobAttribute(string fieldName, string objectName, string? childIndexPropertyName, string dayPropertyName,
        string monthPropertyName, string yearPropertyName, bool isRequired = true, bool applyAgeRange = false,
        string? errorMessage = null) : base(errorMessage)
    {
        _fieldName = fieldName;
        _objectName = objectName;
        _isRequired = isRequired;
        _applyAgeRange = applyAgeRange;
        _childIndexPropertyName = childIndexPropertyName;
        _dayPropertyName = dayPropertyName;
        _monthPropertyName = monthPropertyName;
        _yearPropertyName = yearPropertyName;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var model = validationContext.ObjectInstance;
        int? childIndex = null;

        if (model.GetType() == typeof(Child))
        {
            model = validationContext.ObjectInstance as Child;
            childIndex = GetPropertyIntValue(model, _childIndexPropertyName);
        }

        var dayString = GetPropertyStringValue(model, _dayPropertyName);
        var monthString = GetPropertyStringValue(model, _monthPropertyName);
        var yearString = GetPropertyStringValue(model, _yearPropertyName);

        var allFieldsEmpty = string.IsNullOrEmpty(dayString) &&
                             string.IsNullOrEmpty(monthString) &&
                             string.IsNullOrEmpty(yearString);

        if (!_isRequired && allFieldsEmpty) return ValidationResult.Success;

        // Collect all invalid fields for highlighting
        var errorFields = new List<string>();

        // Check for missing fields first
        var hasEmptyFields = false;
        if (string.IsNullOrWhiteSpace(dayString))
        {
            errorFields.Add("Day");
            hasEmptyFields = true;
        }

        if (string.IsNullOrWhiteSpace(monthString))
        {
            errorFields.Add("Month");
            hasEmptyFields = true;
        }

        if (string.IsNullOrWhiteSpace(yearString))
        {
            errorFields.Add("Year");
            hasEmptyFields = true;
        }

        // Check for invalid values even if some fields are empty
        if (!string.IsNullOrWhiteSpace(yearString))
        {
            if (int.TryParse(yearString, out var yearInt))
            {
                if (yearInt < 1900)
                    if (!errorFields.Contains("Year"))
                        errorFields.Add("Year");
            }
            else if (!errorFields.Contains("Year"))
            {
                errorFields.Add("Year");
            }
        }

        if (!string.IsNullOrWhiteSpace(monthString))
        {
            if (int.TryParse(monthString, out var monthInt))
            {
                if (monthInt < 1 || monthInt > 12)
                    if (!errorFields.Contains("Month"))
                        errorFields.Add("Month");
            }
            else if (!errorFields.Contains("Month"))
            {
                errorFields.Add("Month");
            }
        }

        if (!string.IsNullOrWhiteSpace(dayString))
        {
            if (int.TryParse(dayString, out var dayInt))
            {
                if (dayInt < 1 || dayInt > 31)
                    if (!errorFields.Contains("Day"))
                        errorFields.Add("Day");
            }
            else if (!errorFields.Contains("Day"))
            {
                errorFields.Add("Day");
            }
        }

        // Always add DateOfBirth to error fields if we have any errors
        if (errorFields.Any() && !errorFields.Contains("DateOfBirth")) errorFields.Insert(0, "DateOfBirth");

        // Determine the appropriate error message while maintaining all error fields
        string message;
        if (hasEmptyFields)
        {
            if (errorFields.Count == 2) // One field missing (plus DateOfBirth)
            {
                var missingField = errorFields[1]; // [0] is DateOfBirth
                message = $"Date of birth must include a {missingField.ToLower()}";
            }
            else if (errorFields.Count == 4) // All fields missing
            {
                message = childIndex != null
                    ? $"Enter a {_fieldName} for {_objectName} {childIndex}"
                    : $"Enter a {_fieldName}";
            }
            else // Multiple but not all fields missing
            {
                message = "Enter a complete date of birth";
            }
        }
        else if (errorFields.Any())
        {
            message = "Date of birth must be a real date";
        }
        else
        {
            try
            {
                var yearInt = int.Parse(yearString);
                var monthInt = int.Parse(monthString);
                var dayInt = int.Parse(dayString);

                var dob = new DateTime(yearInt, monthInt, dayInt);

                if (dob > DateTime.Now)
                    return new ValidationResult(
                        childIndex != null
                            ? $"Enter a date in the past for {_objectName} {childIndex}"
                            : "Enter a date in the past",
                        new[] { "DateOfBirth", "Day", "Month", "Year" });

                if (_applyAgeRange)
                {
                    //var now = new DateTime(2025, 4, 30); //For TESTING specific application dates only
                    var now = DateTime.Now;

                    var currentAcademicYear = now.Month >= 5 ? now.Year : now.Year - 1;
                    var academicYearStart = new DateTime(currentAcademicYear, 9, 1);
                    var ageOnAcademicYearStart = CalculateAge(dob, academicYearStart);

                    if (ageOnAcademicYearStart < 4 || ageOnAcademicYearStart > 19)
                    {
                        return new ValidationResult(
                            $"Enter an age between 4 and 19 for {_objectName} {childIndex}",
                            new[] { "DateOfBirth", "Day", "Month", "Year" });
                    }
                }

                return ValidationResult.Success;
            }
            catch
            {
                message = "Date of birth must be a real date";
                if (!errorFields.Contains("Day")) errorFields.Add("Day");
                if (!errorFields.Contains("Month")) errorFields.Add("Month");
                if (!errorFields.Contains("Year")) errorFields.Add("Year");
            }
        }

        return new ValidationResult(message, errorFields);
    }

    private string GetPropertyStringValue(object model, string propertyName)
    {
        return model.GetType().GetProperty(propertyName)?.GetValue(model) as string;
    }

    private int? GetPropertyIntValue(object model, string propertyName)
    {
        return model.GetType().GetProperty(propertyName)?.GetValue(model) as int?;
    }

    private int CalculateAge(DateTime birthDate, DateTime now)
    {
        var age = now.Year - birthDate.Year;
        if (now < birthDate.AddYears(age)) age--;
        return age;
    }
}