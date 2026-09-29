using System.ComponentModel.DataAnnotations;
using CheckYourEligibility.Admin.Attributes;
using CheckYourEligibility.Admin.Models;
using FluentAssertions;

namespace CheckYourEligibility.Admin.Tests.Attributes;

[TestFixture]
public class NameAttributeTests
{
    [Test]
    public void Validate_IdenticalInvalidNames_UsesLastNameMessage()
    {
        var model = new ParentGuardian
        {
            FirstName = "Smith@",
            LastName = "Smith@"
        };

        var attribute = new NameAttribute();
        var context = new ValidationContext(model)
        {
            MemberName = nameof(ParentGuardian.LastName)
        };

        var result = attribute.GetValidationResult(model.LastName, context);

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Last Name field contains an invalid character");
    }
}
