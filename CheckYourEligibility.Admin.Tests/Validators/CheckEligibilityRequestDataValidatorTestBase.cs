using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Domain.Validation;
using FluentValidation;

namespace CheckYourEligibility.Admin.Tests.Validators;

public class CheckEligibilityRequestDataValidatorTestBase
{
    protected IValidator<CheckEligibilityRequestDataBase> Validator { get; private set; } = null!;

    [OneTimeSetUp]
    public void SetUpValidator()
    {
        Validator = new CheckEligibilityRequestDataValidator();
    }
}