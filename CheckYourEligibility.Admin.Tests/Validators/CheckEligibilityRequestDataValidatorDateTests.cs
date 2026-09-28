using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Domain.Validation;
using FluentAssertions;

namespace CheckYourEligibility.Admin.Tests.Validators;

[TestFixture]
public class CheckEligibilityRequestDataValidatorDateTests
{
    private CheckEligibilityRequestDataValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new CheckEligibilityRequestDataValidator();
    }

    [Test]
    public async Task Validate_ImpossibleDate_ReturnsError()
    {
        var request = new CheckEligibilityRequestDataBase
        {
            LastName = "Smith",
            DateOfBirth = "2026-02-31",
            NationalInsuranceNumber = "AB123456C"
        };

        var result = await _validator.ValidateAsync(request);

        result.Errors
            .Should()
            .Contain(x => x.PropertyName.EndsWith(nameof(CheckEligibilityRequestDataBase.DateOfBirth)));
    }
}
