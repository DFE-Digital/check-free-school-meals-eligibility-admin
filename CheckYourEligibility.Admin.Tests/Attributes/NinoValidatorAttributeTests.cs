using System.ComponentModel.DataAnnotations;
using CheckYourEligibility.Admin.Attributes;
using FluentAssertions;

namespace CheckYourEligibility.Admin.Tests.Attributes;

[TestFixture]
public class NinoValidatorAttributeTests
{
    private class TestModel
    {
        [NinValidator]
        public string? NationalInsuranceNumber { get; set; }
    }

    [TestCase("AB123456A")]
    [TestCase("AB123456B")]
    [TestCase("AB123456C")]
    [TestCase("AB123456D")]
    [TestCase("ab123456c")]
    public void Validate_ValidNino_ReturnsSuccess(string nino)
    {
        var results = Validate(nino);

        results.Should().BeEmpty();
    }

    [TestCase("12123456C")]
    [TestCase("AB123456E")]
    [TestCase("AB123456")]
    [TestCase("AB-123456C")]
    [TestCase("AB 12 34 56 C")]
    [TestCase("BG123456A")]
    [TestCase("GB123456A")]
    [TestCase("NK123456A")]
    [TestCase("KN123456A")]
    [TestCase("TN123456A")]
    [TestCase("NT123456A")]
    [TestCase("ZZ123456A")]
    [TestCase("AB12345 6C")]
    [TestCase("AB123456 ")]
    public void Validate_InvalidNino_ReturnsError(string nino)
    {
        var results = Validate(nino);

        results.Should().ContainSingle();
        results[0].ErrorMessage.Should().Be(
            "Enter a National Insurance number in the correct format");
    }

    private static List<ValidationResult> Validate(string? nino)
    {
        var model = new TestModel
        {
            NationalInsuranceNumber = nino
        };

        var context = new ValidationContext(model)
        {
            MemberName = nameof(TestModel.NationalInsuranceNumber)
        };

        var results = new List<ValidationResult>();

        Validator.TryValidateProperty(
            model.NationalInsuranceNumber,
            context,
            results);

        return results;
    }
}
