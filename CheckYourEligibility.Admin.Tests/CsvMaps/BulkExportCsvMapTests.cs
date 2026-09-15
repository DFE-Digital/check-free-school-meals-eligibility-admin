using AutoMapper;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.CsvMaps;
using CheckYourEligibility.Admin.Domain.Constants.BulkCheck;
using CheckYourEligibility.Admin.Mappings;
using CheckYourEligibility.Admin.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace CheckYourEligibility.Admin.Tests.CsvMaps;

[TestFixture]
public class BulkExportCsvMapTests
{
    [Test]
    public void BulkExportProfile_MapsEmailAddress()
    {
        var configuration = new MapperConfiguration(config =>
            config.AddProfile<BulkExportProfile>());

        var mapper = configuration.CreateMapper();

        var source = new CheckEligibilityItem
        {
            EmailAddress = "parent@example.com",
            Status = "eligible",
            Tier = "1"
        };

        var result = mapper.Map<BulkExport>(source);

        Assert.That(result.EmailAddress, Is.EqualTo("parent@example.com"));
    }

    [Test]
    public void BulkExportCsvMap_WritesEmailHeaderAndValue()
    {
        AssertCsv<BulkExportCsvMap>(
            "parent@example.com",
            [
                BulkCheckConstants.ParentFirstNameHeader,
                BulkCheckConstants.ParentLastNameHeader,
                BulkCheckConstants.ParentDateOfBirthHeader,
                BulkCheckConstants.ParentNINOHeader,
                BulkCheckConstants.ChildFirstNameHeader,
                BulkCheckConstants.ChildLastNameHeader,
                BulkCheckConstants.ChildDateOfBirthHeader,
                BulkCheckConstants.ChildSchoolUrnHeader,
                BulkCheckConstants.ParentEmailAddressHeader,
                BulkCheckConstants.Outcome
            ]);
    }

    [Test]
    public void BulkExportExpandedCsvMap_WritesEmailHeaderAndValue()
    {
        AssertCsv<BulkExportExpandedCsvMap>(
            "parent@example.com",
            [
                BulkCheckConstants.ParentFirstNameHeader,
                BulkCheckConstants.ParentLastNameHeader,
                BulkCheckConstants.ParentDateOfBirthHeader,
                BulkCheckConstants.ParentNINOHeader,
                BulkCheckConstants.ChildFirstNameHeader,
                BulkCheckConstants.ChildLastNameHeader,
                BulkCheckConstants.ChildDateOfBirthHeader,
                BulkCheckConstants.ChildSchoolUrnHeader,
                BulkCheckConstants.ParentEmailAddressHeader,
                BulkCheckConstants.Outcome,
                BulkCheckConstants.EligibilityEndDate
            ]);
    }

    [Test]
    public void EnhancedExportMaps_WithNullEmail_WriteEmptyEmailValue()
    {
        AssertEmailValue<BulkExportCsvMap>(null, string.Empty);
        AssertEmailValue<BulkExportExpandedCsvMap>(null, string.Empty);
    }

    private static void AssertCsv<TMap>(
        string? emailAddress,
        string[] expectedHeaders)
        where TMap : ClassMap
    {
        var csvContent = WriteCsv<TMap>(emailAddress);

        using var textReader = new StringReader(csvContent);
        using var csv = new CsvReader(textReader, CultureInfo.InvariantCulture);

        Assert.That(csv.Read(), Is.True);
        csv.ReadHeader();

        Assert.That(csv.HeaderRecord, Is.EqualTo(expectedHeaders));

        Assert.That(csv.Read(), Is.True);
        Assert.That(
            csv.GetField(BulkCheckConstants.ParentEmailAddressHeader),
            Is.EqualTo(emailAddress ?? string.Empty));
    }

    private static void AssertEmailValue<TMap>(
        string? emailAddress,
        string expectedValue)
        where TMap : ClassMap
    {
        var csvContent = WriteCsv<TMap>(emailAddress);

        using var textReader = new StringReader(csvContent);
        using var csv = new CsvReader(textReader, CultureInfo.InvariantCulture);

        Assert.That(csv.Read(), Is.True);
        csv.ReadHeader();
        Assert.That(csv.Read(), Is.True);

        Assert.That(
            csv.GetField(BulkCheckConstants.ParentEmailAddressHeader),
            Is.EqualTo(expectedValue));
    }

    private static string WriteCsv<TMap>(string? emailAddress)
        where TMap : ClassMap
    {
        using var textWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var csv = new CsvWriter(textWriter, CultureInfo.InvariantCulture);

        csv.Context.RegisterClassMap<TMap>();
        csv.WriteRecords(
        [
            new BulkExport
            {
                FirstName = "John",
                LastName = "Smith",
                DateOfBirth = "1985-03-15",
                NationalInsuranceNumber = "AB123456C",
                ChildFirstName = "Emily",
                ChildLastName = "Smith",
                ChildDateOfBirth = "2015-09-10",
                ChildSchoolUrn = "123456",
                EmailAddress = emailAddress,
                Outcome = "Eligible",
                EligibilityEndDate = "2026-12-31"
            }
        ]);

        return textWriter.ToString();
    }
}
