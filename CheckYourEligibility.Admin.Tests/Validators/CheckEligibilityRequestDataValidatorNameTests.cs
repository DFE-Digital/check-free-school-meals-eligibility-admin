using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Domain.Constants.ErrorMessages;
using CheckYourEligibility.Admin.Domain.Validation;
using FluentValidation;

namespace CheckYourEligibility.Admin.Tests.Validators;

[TestFixture]
public class CheckEligibilityRequestDataValidatorNameTests
{
    private IValidator<IEligibilityServiceType> _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new CheckEligibilityRequestDataValidator();
    }

    [TestCase("García")]
    [TestCase("O'Connor")]
    [TestCase("O’Connor")]
    [TestCase("Smith-Jones")]
    public async Task Validate_BasicSupportedLastName_Passes(string lastName)
    {
        var request = CreateValidBasicRequest();
        request.LastName = lastName;

        var result = await _validator.ValidateAsync(request);

        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public async Task Validate_BasicUnsupportedLastName_ReturnsInvalidCharacterMessage()
    {
        var request = CreateValidBasicRequest();
        request.LastName = "Smith@";

        var result = await _validator.ValidateAsync(request);

        Assert.That(
            result.Errors.Select(x => x.ErrorMessage),
            Is.EqualTo(new[] { ValidationMessages.InvalidLastName }));
    }

    [Test]
    public async Task Validate_BasicMissingLastName_ReturnsOnlyRequiredMessage()
    {
        var request = CreateValidBasicRequest();
        request.LastName = string.Empty;

        var result = await _validator.ValidateAsync(request);

        Assert.That(
            result.Errors.Select(x => x.ErrorMessage),
            Is.EqualTo(new[] { ValidationMessages.LastName }));
    }

    [Test]
    public async Task Validate_EnhancedSupportedNames_Passes()
    {
        var request = CreateValidEnhancedRequest();
        request.FirstName = "José";
        request.LastName = "García";
        request.ChildFirstName = "Zoë";
        request.ChildLastName = "O'Connor";

        var result = await _validator.ValidateAsync(request);

        Assert.That(result.Errors, Is.Empty);
    }

    [TestCase(nameof(CheckEligibilityRequestData_Enhanced.FirstName),
        ValidationMessages.InvalidFirstName)]
    [TestCase(nameof(CheckEligibilityRequestData_Enhanced.LastName),
        ValidationMessages.InvalidLastName)]
    [TestCase(nameof(CheckEligibilityRequestData_Enhanced.ChildFirstName),
        ValidationMessages.InvalidChildFirstName)]
    [TestCase(nameof(CheckEligibilityRequestData_Enhanced.ChildLastName),
        ValidationMessages.InvalidChildLastName)]
    public async Task Validate_EnhancedUnsupportedName_ReturnsInvalidCharacterMessage(
        string propertyName,
        string expectedMessage)
    {
        var request = CreateValidEnhancedRequest();
        SetName(request, propertyName, "Smith@");

        var result = await _validator.ValidateAsync(request);

        Assert.That(
            result.Errors.Select(x => x.ErrorMessage),
            Is.EqualTo(new[] { expectedMessage }));
    }

    private static CheckEligibilityRequestDataBase CreateValidBasicRequest()
    {
        return new CheckEligibilityRequestDataBase
        {
            LastName = "Smith",
            DateOfBirth = "1985-04-23",
            NationalInsuranceNumber = "AA123456C"
        };
    }

    private static CheckEligibilityRequestData_Enhanced CreateValidEnhancedRequest()
    {
        return new CheckEligibilityRequestData_Enhanced
        {
            FirstName = "John",
            LastName = "Smith",
            DateOfBirth = "1985-04-23",
            NationalInsuranceNumber = "AA123456C",
            ChildFirstName = "Emily",
            ChildLastName = "Smith",
            ChildDateOfBirth = "2015-09-10",
            ChildSchoolUrn = "123456"
        };
    }

    private static void SetName(
        CheckEligibilityRequestData_Enhanced request,
        string propertyName,
        string value)
    {
        typeof(CheckEligibilityRequestData_Enhanced)
            .GetProperty(propertyName)!
            .SetValue(request, value);
    }
}