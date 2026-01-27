skipSetup = false

describe('Full journey of creating an application through school portal through to approving in LA portal', () => {
    const parentFirstName = 'Tim';
    let referenceNumber: string;
    const parentLastName = Cypress.env('lastName');
    const parentEmailAddress = 'TimJones@Example.com';
    const NIN = 'NN668767B'
    const childFirstName = 'Timmy';
    const childLastName = 'Smith';
    beforeEach(() => {
        if (!skipSetup) {
            cy.checkSession('school');
            cy.visit(Cypress.config().baseUrl ?? "");
            cy.wait(1);
            cy.get('h1').should('include.text', 'The Telford Park School');
        }
    });
     it('Will allow a basic user to check for eligibility that is eligible', () => {
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
        cy.get('#NationalInsuranceNumber').type(NIN);
        cy.contains('button', 'Perform check').click();

        //Loader page
        cy.url().should('include', 'Check/Loader');

        //Not eligible outcome
        cy.get('p.govuk-notification-banner__heading', { timeout: 80000 }).should('include.text', 'The children of this parent or guardian may not be eligible for free school meals');
        cy.contains('a.govuk-button', 'Do another check').click();
    });
});
