using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Domain.Validation;
using FluentAssertions;

namespace CheckYourEligibility.Admin.Tests.Validators;

[TestFixture]
public class CheckEligibilityRequestDataValidatorDateTests : CheckEligibilityRequestDataValidatorTestBase
{
    [Test]
    public async Task Validate_ImpossibleDate_ReturnsError()
    {
        var request = new CheckEligibilityRequestDataBase
        {
            LastName = "Smith",
            DateOfBirth = "2026-02-31",
            NationalInsuranceNumber = "AB123456C"
        };

        var result = await Validator.ValidateAsync(request);

        result.Errors
            .Should()
            .Contain(x => x.PropertyName.EndsWith(nameof(CheckEligibilityRequestDataBase.DateOfBirth)));
    }
}
