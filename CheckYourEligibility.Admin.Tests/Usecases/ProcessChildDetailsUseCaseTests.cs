using AutoFixture;
using CheckYourEligibility.Admin.Models;
using CheckYourEligibility.Admin.UseCases;
using FluentAssertions;

namespace CheckYourEligibility.Admin.Tests.UseCases;

[TestFixture]
public class ProcessChildDetailsUseCaseTests
{
    [SetUp]
    public void SetUp()
    {
        _sut = new ProcessChildDetailsUseCase();
        _fixture = new Fixture();
    }

    private ProcessChildDetailsUseCase _sut;
    private Fixture _fixture;

    [Test]
    public async Task Execute_Should_Create_FsmApplication_With_Posted_Parent_Data()
    {
        // Arrange
        var children = _fixture.Build<Children>()
            .With(x => x.ParentFirstName, "John")
            .With(x => x.ParentLastName, "Doe")
            .With(x => x.ParentDateOfBirth, "1990-01-01")
            .With(x => x.ParentEmail, "john@example.com")
            .With(x => x.ParentNino, "AB123456C")
            .With(x => x.ParentNass, (string)null)
            .Create();

        // Act
        var result = await _sut.Execute(children);

        // Assert
        result.Should().NotBeNull();
        result.Children.Should().BeEquivalentTo(children);
        result.ParentFirstName.Should().Be("John");
        result.ParentLastName.Should().Be("Doe");
        result.ParentDateOfBirth.Should().Be("1990-01-01");
        result.ParentEmail.Should().Be("john@example.com");
        result.ParentNino.Should().Be("AB123456C");
        result.ParentNass.Should().BeNull();
    }

    [Test]
    public async Task Execute_Should_Create_FsmApplication_With_NASS_Number()
    {
        // Arrange
        var children = _fixture.Build<Children>()
            .With(x => x.ParentNino, (string)null)
            .With(x => x.ParentNass, "2407001")
            .Create();

        // Act
        var result = await _sut.Execute(children);

        // Assert
        result.Should().NotBeNull();
        result.ParentNino.Should().BeNull();
        result.ParentNass.Should().Be("2407001");
    }

    [Test]
    public async Task Execute_Should_Handle_Missing_Parent_Data()
    {
        // Arrange
        var children = _fixture.Build<Children>()
            .With(x => x.ParentFirstName, (string)null)
            .With(x => x.ParentLastName, (string)null)
            .With(x => x.ParentDateOfBirth, (string)null)
            .With(x => x.ParentEmail, (string)null)
            .With(x => x.ParentNino, (string)null)
            .With(x => x.ParentNass, (string)null)
            .Create();

        // Act
        var result = await _sut.Execute(children);

        // Assert
        result.Should().NotBeNull();
        result.Children.Should().BeEquivalentTo(children);
        result.ParentFirstName.Should().BeNull();
        result.ParentLastName.Should().BeNull();
        result.ParentDateOfBirth.Should().BeNull();
        result.ParentEmail.Should().BeNull();
        result.ParentNino.Should().BeNull();
        result.ParentNass.Should().BeNull();
    }
}