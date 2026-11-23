using CheckYourEligibility.Admin.Attributes;
using CheckYourEligibility.Admin.Domain.Constants;
using CheckYourEligibility.Admin.Domain.Constants.ErrorMessages;
using CheckYourEligibility.Admin.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace CheckYourEligibility.Admin.Tests.Attributes;

[TestFixture]
public class IdentifierAttributeTests
{
    [Test]
    public void NinoAttribute_WhenNassSelected_ShouldIgnoreNinoValue()
    {
        var parent = new ParentGuardian
        {
            IdentifierType = IdentifierTypeConstants.Nass,
            NationalInsuranceNumber = "invalid"
        };

        var result = new NinoAttribute().GetValidationResult(
            parent.NationalInsuranceNumber,
            new ValidationContext(parent) { MemberName = nameof(ParentGuardian.NationalInsuranceNumber) });

        result.Should().Be(ValidationResult.Success);
    }

    [Test]
    public void NassAttribute_WhenNinoSelected_ShouldIgnoreNassValue()
    {
        var parent = new ParentGuardian
        {
            IdentifierType = IdentifierTypeConstants.Nino,
            NationalAsylumSeekerServiceNumber = "invalid"
        };

        var result = new NassAttribute().GetValidationResult(
            parent.NationalAsylumSeekerServiceNumber,
            new ValidationContext(parent) { MemberName = nameof(ParentGuardian.NationalAsylumSeekerServiceNumber) });

        result.Should().Be(ValidationResult.Success);
    }

    [Test]
    public void NassAttribute_WhenSelectedValueIsMissing_ShouldRequireAnIdentifier()
    {
        var parent = new ParentGuardian() { IdentifierType = IdentifierTypeConstants.Nass };

        var result = new NassAttribute().GetValidationResult(
            parent.NationalAsylumSeekerServiceNumber,
            new ValidationContext(parent) { MemberName = nameof(ParentGuardian.NationalAsylumSeekerServiceNumber) });

        result!.ErrorMessage.Should().Be(ValidationMessages.NI_or_NASS);
    }

    [Test]
    public void NinoAttribute_WhenSelectedValueIsMissing_ShouldRequireAnIdentifier()
    {
        var parent = new ParentGuardian { IdentifierType = IdentifierTypeConstants.Nino };

        var result = new NinoAttribute().GetValidationResult(
            parent.NationalInsuranceNumber,
            new ValidationContext(parent) { MemberName = nameof(ParentGuardian.NationalInsuranceNumber) });

        result!.ErrorMessage.Should().Be(ValidationMessages.NI_or_NASS);
    }
}