import { getValidChildDob, getInvalidChildDob } from '../../support/dateHelpers';
describe('Test that approved accented characters are accepted in name input fields', () => {
    const parentFirstName = 'Tim';
    const parentLastName = Cypress.env('lastName');
    const parentEmailAddress = 'TimJones@Example.com';
    const NIN = 'PN668767B'
    const childFirstName = 'Timmy';
    const childLastName = 'Smith';

    it('Parent first and last names on Enter_Details should accept approved accented characters', () => {
        //Setup - Get to Enter_Details page to perform test
        cy.checkSession('school');
        cy.visit((Cypress.config().baseUrl ?? "") + "/home");
        cy.wait(1);
        cy.get('.govuk-caption-l').should('include.text', 'The Telford Park School');
        cy.contains('Run a check for one parent or guardian').click();

        cy.get('#consent').check();
        cy.get('#submitButton').click();

        cy.url().should('include', '/Check/Enter_Details');
        //First and Last name fields should accept
        // [("OBrien", "plain letters")]
        // [("O'Brien", "straight apostrophe (U+0027)")]
        // [("O\u2019Brien", "right curly apostrophe (U+2019)")]
        // [("O\u2018Brien", "left curly apostrophe (U+2018)")]
        // [("Smith-Jones", "hyphen")]
        // [("St. Claire", "period and space")]
        // [("van den Berg", "spaces")]
        // [("ÁáÉéÍíÓóÚúÝýĆćĹĺŃńŔŕŚśŹź", "acute")]
        // [("ÀàÈèÌìÒòÙùẀẁỲỳ", "grave")]
        // [("ÂâÊêÎîÔôÛûĈĉĜĝĤĥĴĵŜŝŴŵŶŷ", "circumflex")]
        // [("ÃãÑñÕõĨĩŨũẼẽỸỹ", "tilde")]
        // [("ÄäËëÏïÖöÜüŸÿ", "umlaut or diaeresis")]
        // [("ÇçĢģĶķĻļŅņŖŗŞşŢţ", "cedilla")]
        // [("ÅåŮů", "ring")]
        // [("ĀāĒēĪīŌōŪūȲȳ", "macron")]
        // [("ĂăĔĕĞğĬĭŎŏŬŭ", "breve")]
        // [("ĊċĖėĠġİẊẋŻż", "dot above")]
        // [("ĄąĘęĮįŲų", "ogonek")]
        // [("ŐőŰű", "double acute")]
        cy.get('#FirstName').type('OBrienO\'BrienO\u2019BrienO\u2018BrienSmith-JonesSt. Clairevan den BergÁáÉéÍíÓóÚúÝýĆćĹĺŃńŔŕŚśŹźÀàÈèÌìÒòÙùẀẁỲỳÀàÈèÌìÒòÙùẀẁỲỳÃãÑñÕõĨĩŨũẼẽỸỹÄäËëÏïÖöÜüŸÿÇçĢģĶķĻļŅņŖŗŞşŢţÅåŮůĀāĒēĪīŌōŪūȲȳĂăĔĕĞğĬĭŎŏŬŭĊċĖėĠġİẊẋŻżĄąĘęĮįŲųŐőŰű');
        cy.get('#LastName').type('OBrienO\'BrienO\u2019BrienO\u2018BrienSmith-JonesSt. Clairevan den BergÁáÉéÍíÓóÚúÝýĆćĹĺŃńŔŕŚśŹźÀàÈèÌìÒòÙùẀẁỲỳÀàÈèÌìÒòÙùẀẁỲỳÃãÑñÕõĨĩŨũẼẽỸỹÄäËëÏïÖöÜüŸÿÇçĢģĶķĻļŅņŖŗŞşŢţÅåŮůĀāĒēĪīŌōŪūȲȳĂăĔĕĞğĬĭŎŏŬŭĊċĖėĠġİẊẋŻżĄąĘęĮįŲųŐőŰű');
        cy.contains('button', 'Perform check').click();
        cy.get('#error-summary')
            .should('not.contain.text', 'First Name field contains an invalid character')
            .and('not.contain.text', 'Last Name field contains an invalid character');

        //Continue to Add_Child_Details to check the Child Name validation with valid Parent Details
        cy.get('#FirstName').clear().type(parentFirstName);
        cy.get('#LastName').clear().type(parentLastName);
        cy.get('#EmailAddress').type(parentEmailAddress);
        cy.get('[id="DateOfBirth.Day"]').type('01');
        cy.get('[id="DateOfBirth.Month"]').type('01');
        cy.get('[id="DateOfBirth.Year"]').type('1990');
        cy.get('#NationalInsuranceNumber').type(NIN);
        cy.contains('button', 'Perform check').click();

        //Loader page
        cy.url().should('include', 'Check/Loader');

        //Not eligible outcome
        cy.get('p.govuk-notification-banner__heading', { timeout: 80000 }).should('include.text', 'may not be eligible for free school meals.');
        cy.contains('a.govuk-button', 'Appeal now').click();

        //Enter child details
        cy.url().should('include', '/Enter_Child_Details');
        cy.get('[id="ChildList[0].FirstName"]').type('OBrienO\'BrienO\u2019BrienO\u2018BrienSmith-JonesSt. Clairevan den BergÁáÉéÍíÓóÚúÝýĆćĹĺŃńŔŕŚśŹźÀàÈèÌìÒòÙùẀẁỲỳÀàÈèÌìÒòÙùẀẁỲỳÃãÑñÕõĨĩŨũẼẽỸỹÄäËëÏïÖöÜüŸÿÇçĢģĶķĻļŅņŖŗŞşŢţÅåŮůĀāĒēĪīŌōŪūȲȳĂăĔĕĞğĬĭŎŏŬŭĊċĖėĠġİẊẋŻżĄąĘęĮįŲųŐőŰű');
        cy.get('[id="ChildList[0].LastName"]').type('OBrienO\'BrienO\u2019BrienO\u2018BrienSmith-JonesSt. Clairevan den BergÁáÉéÍíÓóÚúÝýĆćĹĺŃńŔŕŚśŹźÀàÈèÌìÒòÙùẀẁỲỳÀàÈèÌìÒòÙùẀẁỲỳÃãÑñÕõĨĩŨũẼẽỸỹÄäËëÏïÖöÜüŸÿÇçĢģĶķĻļŅņŖŗŞşŢţÅåŮůĀāĒēĪīŌōŪūȲȳĂăĔĕĞğĬĭŎŏŬŭĊċĖėĠġİẊẋŻżĄąĘęĮįŲųŐőŰű');

        const childDob = getValidChildDob();

        cy.get('[id="ChildList[0].DateOfBirth.Day"]').type(childDob.day);
        cy.get('[id="ChildList[0].DateOfBirth.Month"]').type(childDob.month);
        cy.get('[id="ChildList[0].DateOfBirth.Year"]').type(childDob.year);
        cy.contains('button', 'Save and continue').click();

        //Add supporting evidence or skip
        cy.url().should('include', '/UploadEvidence');
        cy.fixture('TestImage.png').then(fileContent => {
            cy.get('input[type="file"]').attachFile({
                fileContent,
                fileName: 'TestImage.png',
                mimeType: 'image/png'
            });
        });
        cy.contains('button', 'Attach evidence').click();

        //Check answers page
        cy.get('.govuk-heading-l').should('include.text', 'Check your answers before submitting');
        cy.CheckValuesInSummaryCard('Child 1 details', "Name", 'OBrienO\'BrienO\u2019BrienO\u2018BrienSmith-JonesSt. Clairevan den BergÁáÉéÍíÓóÚúÝýĆćĹĺŃńŔŕŚśŹźÀàÈèÌìÒòÙùẀẁỲỳÀàÈèÌìÒòÙùẀẁỲỳÃãÑñÕõĨĩŨũẼẽỸỹÄäËëÏïÖöÜüŸÿÇçĢģĶķĻļŅņŖŗŞşŢţÅåŮůĀāĒēĪīŌōŪūȲȳĂăĔĕĞğĬĭŎŏŬŭĊċĖėĠġİẊẋŻżĄąĘęĮįŲųŐőŰű' + " " + 'OBrienO\'BrienO\u2019BrienO\u2018BrienSmith-JonesSt. Clairevan den BergÁáÉéÍíÓóÚúÝýĆćĹĺŃńŔŕŚśŹźÀàÈèÌìÒòÙùẀẁỲỳÀàÈèÌìÒòÙùẀẁỲỳÃãÑñÕõĨĩŨũẼẽỸỹÄäËëÏïÖöÜüŸÿÇçĢģĶķĻļŅņŖŗŞşŢţÅåŮůĀāĒēĪīŌōŪūȲȳĂăĔĕĞğĬĭŎŏŬŭĊċĖėĠġİẊẋŻżĄąĘęĮįŲųŐőŰű');
    });
});