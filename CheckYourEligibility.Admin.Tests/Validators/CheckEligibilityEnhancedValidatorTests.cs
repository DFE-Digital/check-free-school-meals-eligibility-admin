using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Domain.Constants.ErrorMessages;
using CheckYourEligibility.Admin.Domain.DfeSignIn;
using FluentValidation;

namespace CheckYourEligibility.Admin.Tests.Validators
{
    [TestFixture]
    public class CheckEligibilityEnhancedValidatorTests : CheckEligibilityRequestDataValidatorTestBase
    {
        [Test]
        public async Task Validate_MissingNationalInsuranceAndAsylumNumbers_ReturnsRequiredMessage()
        {
            var model = CreateValidModel(null);
            model.NationalInsuranceNumber = null;
            model.NationalAsylumSeekerServiceNumber = null;

            var result = await Validator.ValidateAsync(CreateContext(model));

            Assert.That(
                result.Errors.Select(error => error.ErrorMessage),
                Does.Contain(ValidationMessages.NI_or_NASS));
        }

        [Test]
        public async Task Validate_NationalInsuranceAndAsylumNumbersBothSupplied_ReturnsMutuallyExclusiveMessage()
        {
            var model = CreateValidModel(null);
            model.NationalAsylumSeekerServiceNumber = "123456789";

            var result = await Validator.ValidateAsync(CreateContext(model));

            Assert.That(
                result.Errors.Select(error => error.ErrorMessage),
                Does.Contain(ValidationMessages.NI_and_NASS));
        }

        [TestCase("AA123456C", null, null)]
        [TestCase("invalid", null, ValidationMessages.InvalidNI)]
        public async Task Validate_NationalInsuranceNumberWhenSupplied_ValidatesItsFormat(
            string nationalInsuranceNumber,
            string? asylumNumber,
            string? expectedError)
        {
            var model = CreateValidModel(null);
            model.NationalInsuranceNumber = nationalInsuranceNumber;
            model.NationalAsylumSeekerServiceNumber = asylumNumber;

            var result = await Validator.ValidateAsync(CreateContext(model));

            AssertIdentifierError(result, expectedError);
        }

        [TestCase("123456789", null)]
        [TestCase("12/34/56789", null)]
        [TestCase("invalid", ValidationMessages.InvalidNASS)]
        public async Task Validate_AsylumNumberWhenSupplied_ValidatesItsFormat(
            string asylumNumber,
            string? expectedError)
        {
            var model = CreateValidModel(null);
            model.NationalInsuranceNumber = null;
            model.NationalAsylumSeekerServiceNumber = asylumNumber;

            var result = await Validator.ValidateAsync(CreateContext(model));

            AssertIdentifierError(result, expectedError);
        }

        [TestCase("abc")]
        [TestCase("0")]
        [TestCase("-1")]
        [TestCase("")]
        public async Task Validate_InvalidUrnFormat_ReturnsError(string urn)
        {
            var model = new CheckEligibilityRequestData_Enhanced
            {
                NationalInsuranceNumber = "AA123456C",
                LastName = "Test",
                FirstName = "Test",
                DateOfBirth = "1985-04-23",
                Order = 1,

                ChildFirstName = "Emily",
                ChildLastName = "Test",
                ChildDateOfBirth = "2015-09-10",
                ChildSchoolUrn = urn
            };

            var context = CreateContext(model, new HashSet<int> { 123456 });

            var result = await Validator.ValidateAsync(context);

            Assert.That(result.Errors.Any(e => e.ErrorMessage == ValidationMessages.ChildSchoolUrn), Is.True);
        }

        [Test]
        public async Task Validate_UrnNotInList_ForLocalAuthority_ReturnsLAMessage()
        {
            var model = new CheckEligibilityRequestData_Enhanced
            {
                NationalInsuranceNumber = "AA123456C",
                LastName = "Test",
                FirstName = "Test",
                DateOfBirth = "1985-04-23",
                Order = 1,

                ChildFirstName = "Emily",
                ChildLastName = "Test",
                ChildDateOfBirth = "2015-09-10",
                ChildSchoolUrn = "999999"
            };

            var context = CreateContext(
                model,
                validUrns: new HashSet<int> { 123456 },
                orgType: OrganisationCategory.LocalAuthority);

            var result = await Validator.ValidateAsync(context);

            Assert.That(result.Errors.Any(e =>
                e.ErrorMessage == ValidationMessages.InvalidSchoolUrnForLA), Is.True);
        }

        [Test]
        public async Task Validate_UrnNotInList_ForMAT_ReturnsMATMessage()
        {
            var model = new CheckEligibilityRequestData_Enhanced
            {
                NationalInsuranceNumber = "AA123456C",
                LastName = "Test",
                FirstName = "Test",
                DateOfBirth = "1985-04-23",
                Order = 1,

                ChildFirstName = "Emily",
                ChildLastName = "Test",
                ChildDateOfBirth = "2015-09-10",
                ChildSchoolUrn = "999999"
            };

            var context = CreateContext(
                model,
                validUrns: new HashSet<int> { 123456 },
                orgType: OrganisationCategory.MultiAcademyTrust);

            var result = await Validator.ValidateAsync(context);

            Assert.That(result.Errors.Any(e =>
                e.ErrorMessage == ValidationMessages.InvalidSchoolUrnForMAT), Is.True);
        }

        [TestCase(OrganisationCategory.LocalAuthority)]
        [TestCase(OrganisationCategory.MultiAcademyTrust)]
        public async Task Validate_ValidUrnInList_Passes(OrganisationCategory orgType)
        {
            var model = new CheckEligibilityRequestData_Enhanced
            {
                NationalInsuranceNumber = "AA123456C",
                FirstName = "Test",
                LastName = "Test",
                DateOfBirth = "1985-04-23",
                Order = 1,

                ChildFirstName = "Emily",
                ChildLastName = "Test",
                ChildDateOfBirth = "2015-09-10",
                ChildSchoolUrn = "123456"
            };

            var context = CreateContext(
                model,
                validUrns: new HashSet<int> { 123456 },
                orgType: orgType);

            var result = await Validator.ValidateAsync(context);

            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public async Task Validate_NoContextData_DoesNotThrowOrFail()
        {
            var model = new CheckEligibilityRequestData_Enhanced
            {
                NationalInsuranceNumber = "AA123456C",
                FirstName = "Test",
                LastName = "Test",
                DateOfBirth = "1985-04-23",
                Order = 1,

                ChildFirstName = "Emily",
                ChildLastName = "Test",
                ChildDateOfBirth = "2015-09-10",
                ChildSchoolUrn = "123456"
            };

            var context = new ValidationContext<CheckEligibilityRequestDataBase>(model);

            var result = await Validator.ValidateAsync(context);

            Assert.That(result.Errors, Is.Empty);
        }

        [TestCase("parent@example.com")]
        [TestCase("name+tag@sub.example.co.uk")]
        public async Task Validate_ValidEmailAddress_Passes(string emailAddress)
        {
            var model = CreateValidModel(emailAddress);
            var context = CreateContext(model);

            var result = await Validator.ValidateAsync(context);

            Assert.That(
                result.Errors.Any(e =>
                    e.PropertyName.EndsWith(nameof(CheckEligibilityRequestData_Enhanced.EmailAddress))),
                Is.False);
        }

        [Test]
        public async Task Validate_NullEmailAddress_Passes()
        {
            var model = CreateValidModel(null);
            var context = CreateContext(model);

            var result = await Validator.ValidateAsync(context);

            Assert.That(
                result.Errors.Any(e =>
                    e.PropertyName.EndsWith(nameof(CheckEligibilityRequestData_Enhanced.EmailAddress))),
                Is.False);
        }

        [TestCase("parent.example.com")]
        [TestCase("@example.com")]
        [TestCase("parent@")]
        [TestCase("parent@@example.com")]
        [TestCase("parent@example")]
        public async Task Validate_InvalidEmailAddress_ReturnsExpectedMessage(string emailAddress)
        {
            var model = CreateValidModel(emailAddress);
            var context = CreateContext(model);

            var result = await Validator.ValidateAsync(context);

            Assert.That(
                result.Errors.Any(e =>
                    e.PropertyName.EndsWith(nameof(CheckEligibilityRequestData_Enhanced.EmailAddress)) &&
                    e.ErrorMessage == ValidationMessages.InvalidEmailAddress),
                Is.True);
        }

        #region Helpers

        private static CheckEligibilityRequestData_Enhanced CreateValidModel(string? emailAddress)
        {
            return new CheckEligibilityRequestData_Enhanced
            {
                NationalInsuranceNumber = "AA123456C",
                FirstName = "Test",
                LastName = "Test",
                DateOfBirth = "1985-04-23",
                Order = 1,
                ChildFirstName = "Emily",
                ChildLastName = "Test",
                ChildDateOfBirth = "2015-09-10",
                ChildSchoolUrn = "123456",
                EmailAddress = emailAddress
            };
        }

        private static void AssertIdentifierError(
            FluentValidation.Results.ValidationResult result,
            string? expectedError)
        {
            var identifierErrors = result.Errors
                .Where(error => error.ErrorMessage == ValidationMessages.NI_or_NASS
                    || error.ErrorMessage == ValidationMessages.NI_and_NASS
                    || error.ErrorMessage == ValidationMessages.InvalidNI
                    || error.ErrorMessage == ValidationMessages.InvalidNASS)
                .Select(error => error.ErrorMessage);

            if (expectedError is null)
            {
                Assert.That(identifierErrors, Is.Empty);
            }
            else
            {
                Assert.That(identifierErrors, Is.EqualTo(new[] { expectedError }));
            }
        }

        private ValidationContext<CheckEligibilityRequestDataBase> CreateContext(
                            CheckEligibilityRequestData_Enhanced model,
            HashSet<int>? validUrns = null,
            OrganisationCategory? orgType = null)
        {
            var context = new ValidationContext<CheckEligibilityRequestDataBase>(model);

            if (validUrns != null)
                context.RootContextData["validSchoolUrns"] = validUrns;

            if (orgType != null)
                context.RootContextData["organisationType"] = orgType;

            return context;
        }

        #endregion Helpers
    }
}