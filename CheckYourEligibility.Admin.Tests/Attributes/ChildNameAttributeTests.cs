using System.ComponentModel.DataAnnotations;
using CheckYourEligibility.Admin.Models;
using FluentAssertions;

namespace CheckYourEligibility.Admin.Tests.Attributes;

[TestFixture]
public class ChildNameAttributeTests
{
    [Test]
    public void Validate_InvalidChildFirstName_ReturnsInvalidCharacterMessage()
    {
        var model = new Child
        {
            ChildIndex = 1,
            FirstName = "Emily@"
        };

        var context = new ValidationContext(model)
        {
            MemberName = nameof(Child.FirstName)
        };

        var results = new List<ValidationResult>();

        Validator.TryValidateProperty(
            model.FirstName,
            context,
            results);

        results.Should().ContainSingle();
        results[0].ErrorMessage.Should().Be(
            "First Name field contains an invalid character");
    }
}
