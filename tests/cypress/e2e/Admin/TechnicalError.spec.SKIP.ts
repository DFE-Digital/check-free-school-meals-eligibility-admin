//Test set as SKIP due to the length of time a tech error response takes to return, so we do not want to
//  include this in regular test runs. The timeout is set to how long it will keep checking rather than a 
// fixed wait, but our intention is to see if we can generate the response faster before including
// this test more permanently. Then just remove SKIP from filename to re-enable it.

describe('TechnicalError outcome should display Error Code and CorrelationID', () => {
    const parentFirstName = 'Tim';
    const parentLastName = Cypress.env('lastName');
    const parentEmailAddress = 'TimJones@Example.com';

    it('TechnicalError outcome should display Error Code and CorrelationID', () => {

        cy.checkSession('school');
        cy.visit((Cypress.config().baseUrl ?? "") + "/home");
        cy.wait(1);
        cy.get('.govuk-caption-l').should('include.text', 'The Telford Park School');

        //Add parent details
        cy.contains('Run a check for one parent or guardian').click();
        cy.get('#consent').check();
        cy.get('#submitButton').click();
        cy.url().should('include', '/Check/Enter_Details');
        cy.get('#FirstName').type(parentFirstName);
        cy.get('#LastName').type(parentLastName);
        cy.get('#EmailAddress').type(parentEmailAddress);
        cy.get('[id="DateOfBirth.Day"]').type('01');
        cy.get('[id="DateOfBirth.Month"]').type('01');
        cy.get('[id="DateOfBirth.Year"]').type('1990');
        cy.get('#NationalInsuranceNumber').type("XX123456C");
        cy.contains('button', 'Perform check').click();

        //Loader page
        cy.url().should('include', 'Check/Loader');

        //Technical_Error outcome
        cy.get('h1',{ timeout: 120000 }).should('include.text', 'Check failed');
        cy.get('body').should('include.text', 'Error code: STE50');
        cy.get('body').should('include.text', 'Correlation ID:'); //Only shown if Guid was available from the check
    });
});