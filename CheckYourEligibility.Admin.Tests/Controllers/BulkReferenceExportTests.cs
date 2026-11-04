using System.Globalization;
using System.Net;
using AutoMapper;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Controllers;
using CheckYourEligibility.Admin.Domain.Constants;
using CheckYourEligibility.Admin.Domain.DfeSignIn;
using CheckYourEligibility.Admin.Domain.Enums;
using CheckYourEligibility.Admin.Gateways;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using CheckYourEligibility.Admin.Infrastructure;
using CheckYourEligibility.Admin.Mappings;
using CheckYourEligibility.Admin.Usecases;
using CsvHelper;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CheckYourEligibility.Admin.Tests.Controllers;

[TestFixture]
public class BulkReferenceExportTests : TestBase
{
    [Test]
    public async Task Download_Preserves_Reference_Row_Order_Email_And_Policy_Columns(
        [Values(DfeSignInRoles.RoleCodeBasic, DfeSignInRoles.RoleCodeLA,
            DfeSignInRoles.RoleCodeMAT, DfeSignInRoles.RoleCodeSchool)] string roleCode,
        [Values(EligibilityCriteria.standard, EligibilityCriteria.expanded)] EligibilityCriteria criteria,
        [Values("populated", "blank", "legacy", "empty")] string scenario)
    {
        var enhanced = roleCode != DfeSignInRoles.RoleCodeBasic;
        string?[] references = scenario == "populated"
            ? ["  MiXeD, \"Référence\"  ", "", " " + new string('a', 100) + " ", "  FIRST\nSECOND  "]
            : [null, "", "   ", ""];
        string?[] emails = ["Parent@Example.COM", null, "", "other@example.com"];
        var statuses = new[] { "eligible", "notEligible", "parentNotFound", "error" };
        var outcomes = new[] { "Eligible targeted", "Not eligible", "Information does not match records", "Error" };
        var data = new JArray();
        if (scenario != "empty")
        {
            // Completion order differs from upload order; references must remain attached to their rows.
            for (var i = 3; i >= 0; i--)
            {
                var item = new JObject
                {
                    ["Order"] = i + 1,
                    ["FirstName"] = "John",
                    ["LastName"] = "Parent" + (i + 1),
                    ["DateOfBirth"] = "1985-03-15",
                    ["NationalInsuranceNumber"] = "AB123456C",
                    ["ChildFirstName"] = "Emily",
                    ["ChildLastName"] = "Parent" + (i + 1),
                    ["ChildDateOfBirth"] = "2015-09-10",
                    ["ChildSchoolUrn"] = "123456",
                    ["EmailAddress"] = emails[i] == null ? JValue.CreateNull() : new JValue(emails[i]),
                    ["Status"] = statuses[i],
                    ["Tier"] = "targeted",
                    ["EligibilityEndDate"] = "2027-07-31"
                };
                if (scenario != "legacy")
                    item["Reference"] = references[i] == null ? JValue.CreateNull() : new JValue(references[i]);
                data.Add(item);
            }
        }

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(new JObject { ["Data"] = data }.ToString())
        };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get
                    && r.RequestUri!.AbsolutePath == "/bulk-check/reference-batch/"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        using var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://localhost:7000/") };
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());
        var mapper = new MapperConfiguration(config => config.AddProfile<BulkExportProfile>()).CreateMapper();
        _sessionMock.Object.SetString("JwtToken", "test-token");
        _sessionMock.Object.SetString("JwtTokenExpiry", DateTime.UtcNow.AddHours(1).ToString("o"));
        _sessionMock.Object.SetString("FreeSchoolMealsPolicy", JsonConvert.SerializeObject(new EligibilityPolicyAssignment
        {
            CheckType = "FreeSchoolMeals", EligibilityCriteria = criteria
        }));
        _sessionMock.Object.SetString("LocalAuthorityRegion", "England");
        var gateway = new CheckGateway(loggerFactory.Object, client, _configMock.Object,
            new HttpContextAccessor { HttpContext = _httpContext.Object }, mapper);
        var signIn = new Mock<IDfeSignInApiService>();
        signIn.Setup(x => x.GetUserRolesAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<Role> { new() { Code = roleCode } });

        using var controller = new BulkCheckController(Mock.Of<ILogger<BulkCheckController>>(), gateway,
            _configMock.Object, Mock.Of<IWebHostEnvironment>(), Mock.Of<IParseBulkCheckFileUseCase>(),
            Mock.Of<IGetBulkChecks>(), Mock.Of<IDeleteBulkCheckFileUseCase>(), signIn.Object,
            Mock.Of<ISchoolMenuContextResolver>(), Mock.Of<ILocalAuthoritySettingsGateway>())
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext.Object },
            TempData = _tempData
        };
        await controller.GetDfeClaimsAsync();

        var action = await controller.Bulk_Check_Download("reference-batch");

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        if (scenario == "empty")
        {
            action.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Bulk_Check_History");
            controller.TempData["ErrorMessage"].Should().Be("No results found for this bulk check.");
            return;
        }

        var file = action.Should().BeOfType<FileContentResult>().Which;
        file.ContentType.Should().Be("text/csv");
        file.FileDownloadName.Should().StartWith("fsm-outcomes-").And.EndWith(".csv");
        file.FileContents.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF });
        using var stream = new MemoryStream(file.FileContents);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read().Should().BeTrue();
        csv.ReadHeader();
        var headers = enhanced
            ? new List<string> { "Parent First Name", "Parent Last Name", "Parent Date of Birth", "Parent National Insurance number",
                "Child First Name", "Child Last Name", "Child Date of Birth", "Child School Urn", "Parent Email Address (optional)", "Outcome" }
            : new List<string> { "Parent Last Name", "Parent Date of Birth", "Parent National Insurance number", "Outcome" };
        if (criteria == EligibilityCriteria.expanded) headers.Add("Eligibility End Date");
        headers.Add("Reference (optional)");
        csv.HeaderRecord.Should().Equal(headers);
        var rows = new List<string[]>();
        while (csv.Read()) rows.Add(csv.Parser.Record!.ToArray());
        rows.Should().HaveCount(4);
        for (var i = 0; i < rows.Count; i++)
        {
            var expected = enhanced
                ? new List<string> { "John", "Parent" + (i + 1), "1985-03-15", "AB123456C", "Emily",
                    "Parent" + (i + 1), "2015-09-10", "123456", emails[i] ?? "", outcomes[i] }
                : new List<string> { "Parent" + (i + 1), "1985-03-15", "AB123456C", outcomes[i] };
            if (criteria == EligibilityCriteria.expanded) expected.Add("2027-07-31");
            expected.Add(scenario == "legacy" ? "" : references[i]?.Trim() ?? "");
            rows[i].Should().Equal(expected);
        }
    }
}
