using System.Globalization;
using System.Text;
using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Domain.Constants.BulkCheck;
using CheckYourEligibility.Admin.Domain.Constants.ErrorMessages;
using CheckYourEligibility.Admin.Domain.DfeSignIn;
using CheckYourEligibility.Admin.Domain.Validation;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.Usecases;
using CsvHelper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json.Linq;
using static CheckYourEligibility.Admin.Helpers.CsvBulkCheckValidatorHelper;

namespace CheckYourEligibility.Admin.Tests.Usecases;

[TestFixture]
public class BulkReferenceUploadTests
{
    public enum UploadMode { Basic, EnhancedLA, EnhancedMAT, EnhancedSchool }
    private static readonly UploadMode[] Modes = Enum.GetValues<UploadMode>();

    private static IEnumerable<TestCaseData> AcceptedReferences()
    {
        var references = new[] { "", new string(' ', 101), new string('a', 99), new string('a', 100),
            " \t" + new string('a', 100) + "\t ", "  MiXeD  Case-001  ", "  Référence, \"quoted\"  " };
        foreach (var mode in Modes)
        foreach (var reference in references)
            yield return new TestCaseData(mode, reference);
    }

    [TestCaseSource(nameof(AcceptedReferences))]
    public async Task Execute_Trims_Before_Validation_And_Preserves_Reference_And_Email_In_Json(
        UploadMode mode, string reference)
    {
        var result = await Parse(mode, Header(mode) + "\r\n" + Row(mode, reference));

        result.ErrorMessage.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        var request = result.ValidRequests.Should().ContainSingle().Which;
        request.Reference.Should().Be(reference.Trim());
        request.Order.Should().Be(1);
        request.NationalInsuranceNumber.Should().Be("AB123456C");
        var json = JObject.FromObject(new { Data = result.ValidRequests });
        json["Data"]![0]!["Reference"]!.Value<string>().Should().Be(reference.Trim());
        if (mode == UploadMode.Basic)
        {
            json["Data"]![0]!["EmailAddress"].Should().BeNull();
        }
        else
        {
            var enhanced = request.Should().BeOfType<CheckEligibilityRequestData_Enhanced>().Which;
            enhanced.EmailAddress.Should().Be("Parent@Example.COM");
            enhanced.ChildSchoolUrn.Should().Be("123456");
            json["Data"]![0]!["EmailAddress"]!.Value<string>().Should().Be("Parent@Example.COM");
        }
    }

    [TestCaseSource(nameof(Modes))]
    public async Task Execute_Rejects_101_Characters_After_Trim_With_Exact_Error_And_Row_Association(UploadMode mode)
    {
        var result = await Parse(mode, Header(mode) + "\r\n" + Row(mode, " FIRST ") + "\r\n"
            + Row(mode, " " + new string('x', 101) + " ") + "\r\n" + Row(mode, " THIRD "));

        result.ErrorMessage.Should().BeEmpty();
        var error = result.Errors.Should().ContainSingle().Which;
        error.LineNumber.Should().Be(3);
        error.Message.Should().Be("Reference must be 100 characters or fewer.");
        result.ValidRequests.Select(x => x.Reference).Should().Equal("FIRST", "THIRD");
        result.ValidRequests.Select(x => x.Order).Should().Equal(1, 3);
    }

    [Test]
    public async Task Execute_Identifies_Missing_Exact_Reference_Header(
        [ValueSource(nameof(Modes))] UploadMode mode,
        [Values("", ",Reference", ",Reference(optional)")] string referenceHeader)
    {
        var legacyHeader = Header(mode).Replace(",Reference (optional)", "");
        var result = await Parse(mode, legacyHeader + referenceHeader + "\r\n" + Row(mode, ""));

        result.ErrorMessage.Should().Be("The selected file must include the column heading Reference (optional).");
        result.ValidRequests.Should().BeEmpty();
    }

    [TestCaseSource(nameof(Modes))]
    public async Task Execute_Requires_Reference_To_Be_Final_Column(UploadMode mode)
    {
        var header = "Reference (optional)," + Header(mode).Replace(",Reference (optional)", "");
        var result = await Parse(mode, header + "\r\n" + Row(mode, ""));

        result.ErrorMessage.Should().Be(IncorrectHeadersErrorMessage);
        result.ValidRequests.Should().BeEmpty();
    }

    [Test]
    public async Task Execute_Basic_Rejects_Parent_Email_Column()
    {
        var header = Header(UploadMode.Basic).Replace(",Reference (optional)", ",Parent Email Address (optional),Reference (optional)");
        var result = await Parse(UploadMode.Basic, header + "\r\nSmith,1985-03-15,AB123456C,parent@example.com,REF");

        result.ErrorMessage.Should().Be(IncorrectHeadersErrorMessage);
        result.ValidRequests.Should().BeEmpty();
    }

    [Test]
    public async Task Execute_Enhanced_Preserves_Optional_Email_Alongside_Reference(
        [Values(UploadMode.EnhancedLA, UploadMode.EnhancedMAT, UploadMode.EnhancedSchool)] UploadMode mode,
        [Values("", "   ", "  Parent@Example.COM  ")] string email)
    {
        var result = await Parse(mode, Header(mode) + "\r\n" + Row(mode, " REF ", email));

        result.ErrorMessage.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        var request = result.ValidRequests.Should().ContainSingle().Which
            .Should().BeOfType<CheckEligibilityRequestData_Enhanced>().Which;
        request.EmailAddress.Should().Be(string.IsNullOrWhiteSpace(email) ? null : email.Trim());
        request.Reference.Should().Be("REF");
    }

    [TestCase(UploadMode.EnhancedLA)]
    [TestCase(UploadMode.EnhancedMAT)]
    [TestCase(UploadMode.EnhancedSchool)]
    public async Task Execute_Enhanced_Still_Rejects_Invalid_Email(UploadMode mode)
    {
        var result = await Parse(mode, Header(mode) + "\r\n" + Row(mode, "REF", "invalid-email"));

        result.ErrorMessage.Should().BeEmpty();
        result.ValidRequests.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be(ValidationMessages.InvalidEmailAddress);
    }

    [TestCase(UploadMode.EnhancedLA)]
    [TestCase(UploadMode.EnhancedMAT)]
    [TestCase(UploadMode.EnhancedSchool)]
    public async Task Execute_Enhanced_Still_Requires_Email_Header_When_Reference_Header_Is_Present(UploadMode mode)
    {
        var result = await Parse(mode, Header(mode).Replace(",Parent Email Address (optional)", "")
            + "\r\n" + Row(mode, "REF"));

        result.ErrorMessage.Should().Be(IncorrectHeadersErrorMessage);
        result.ValidRequests.Should().BeEmpty();
    }

    [TestCase(UploadMode.EnhancedLA)]
    [TestCase(UploadMode.EnhancedMAT)]
    public async Task Execute_Enhanced_Still_Rejects_School_Outside_Organisation(UploadMode mode)
    {
        var result = await Parse(mode, Header(mode) + "\r\n" + Row(mode, "REF", urn: "999999"));

        result.ErrorMessage.Should().BeEmpty();
        result.ValidRequests.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be(mode == UploadMode.EnhancedLA
            ? ValidationMessages.InvalidSchoolUrnForLA : ValidationMessages.InvalidSchoolUrnForMAT);
    }

    [TestCaseSource(nameof(Modes))]
    public async Task Execute_Allows_Blank_Trailing_Value_With_Mandatory_Header(UploadMode mode)
    {
        var result = await Parse(mode, Header(mode) + "\r\n" + Row(mode, "").TrimEnd(','));

        result.ErrorMessage.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        result.ValidRequests.Should().ContainSingle().Which.Reference.Should().BeNullOrEmpty();
    }

    [TestCaseSource(nameof(Modes))]
    public async Task Execute_Header_Only_Returns_No_Requests(UploadMode mode)
    {
        var result = await Parse(mode, Header(mode));

        result.ErrorMessage.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        result.ValidRequests.Should().BeEmpty();
    }

    [TestCaseSource(nameof(Modes))]
    public async Task Service_Template_Has_Expected_Schema_And_Is_Accepted_By_Parser(UploadMode mode)
    {
        var fileName = mode switch
        {
            UploadMode.Basic => "BulkCheckTemplate.csv",
            UploadMode.EnhancedSchool => "BulkCheckTemplate_Enhanced_School.csv",
            _ => "BulkCheckTemplate_Enhanced.csv"
        };
        var content = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates", fileName));
        content.TrimEnd('\r', '\n').Should().Be(Header(mode));
        var result = await Parse(mode, content.TrimEnd('\r', '\n') + "\r\n" + Row(mode, " TEMPLATE-001 "));

        result.ErrorMessage.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        result.ValidRequests.Should().ContainSingle().Which.Reference.Should().Be("TEMPLATE-001");
    }

    private static string Header(UploadMode mode)
    {
        if (mode == UploadMode.Basic)
            return "Parent Last Name,Parent Date of Birth,Parent National Insurance number,Reference (optional)";
        var header = "Parent First Name,Parent Last Name,Parent Date of Birth,Parent National Insurance number,Child First Name,Child Last Name,Child Date of Birth";
        if (mode != UploadMode.EnhancedSchool) header += ",Child School Urn";
        return header + ",Parent Email Address (optional),Reference (optional)";
    }

    private static string Row(UploadMode mode, string reference, string email = "  Parent@Example.COM  ", string urn = "123456")
    {
        var fields = mode == UploadMode.Basic
            ? new List<string> { "Smith", "1985-03-15", "ab123456c" }
            : new List<string> { "John", "Smith", "1985-03-15", "ab123456c", "Emily", "Smith", "2015-09-10" };
        if (mode != UploadMode.Basic)
        {
            if (mode != UploadMode.EnhancedSchool) fields.Add(urn);
            fields.Add(email);
        }
        fields.Add(reference);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        foreach (var field in fields) csv.WriteField(field);
        csv.NextRecord();
        return writer.ToString().TrimEnd('\r', '\n');
    }

    private static async Task<BulkCheckCsvResult<CheckEligibilityRequestDataBase>> Parse(UploadMode mode, string content)
    {
        var provider = new Mock<IServiceProvider>();
        var validator = new CheckEligibilityRequestDataValidator();
        provider.Setup(x => x.GetService(typeof(IValidator<CheckEligibilityRequestDataBase>))).Returns(validator);
        provider.Setup(x => x.GetService(typeof(IValidator<CheckEligibilityRequestData_Enhanced>))).Returns(validator);
        var gateway = new Mock<ICheckGateway>();
        var schools = new EstablishmentResponse { Data = new List<EstablishmentResponseItem> { new() { URN = 123456, Name = "Test School" } } };
        gateway.Setup(x => x.GetSchoolsAsync(It.IsAny<int>())).ReturnsAsync(schools);
        gateway.Setup(x => x.GetAcademiesAsync(It.IsAny<int>())).ReturnsAsync(schools);
        var parser = new ParseBulkCheckFileUseCase(provider.Object, new ConfigurationBuilder().Build(), gateway.Object);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        if (mode == UploadMode.Basic)
            return await parser.Execute<CheckEligibilityRequestDataBase>(stream, CreateRequestItem, BulkCheckConstants.Headers, 352, OrganisationCategory.LocalAuthority);
        var organisationType = mode switch
        {
            UploadMode.EnhancedSchool => OrganisationCategory.Establishment,
            UploadMode.EnhancedMAT => OrganisationCategory.MultiAcademyTrust,
            _ => OrganisationCategory.LocalAuthority
        };
        var result = await parser.Execute<CheckEligibilityRequestData_Enhanced>(stream,
            mode == UploadMode.EnhancedSchool ? CreateEnhancedSchoolRequestItem : CreateEnhancedRequestItem,
            mode == UploadMode.EnhancedSchool ? BulkCheckConstants.enhancedSchoolHeaders : BulkCheckConstants.enhancedHeaders,
            352, organisationType, schoolUrn: mode == UploadMode.EnhancedSchool ? "123456" : null);
        return new BulkCheckCsvResult<CheckEligibilityRequestDataBase>
        {
            ValidRequests = result.ValidRequests.Cast<CheckEligibilityRequestDataBase>().ToList(),
            Errors = result.Errors,
            ErrorMessage = result.ErrorMessage
        };
    }
}
