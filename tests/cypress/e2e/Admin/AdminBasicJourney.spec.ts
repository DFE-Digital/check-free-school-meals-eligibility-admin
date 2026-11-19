describe('BasicLAHappyPath', () => {
    let skipSetupBasic = false
    const parentFirstName = 'Tim';
    let referenceNumber: string;
    const parentLastName = Cypress.env('lastName');
    const NIN = 'NN668767B'
    beforeEach(() => {
        if (!skipSetupBasic) {
            cy.checkSession('basic');
            cy.visit((Cypress.config().baseUrl ?? "") + "/home");
            cy.wait(1);
            cy.get('.govuk-caption-l').should('include.text', 'Manchester City Council');
        }
    });

    it('Will allow a basic user to check for eligibility that is eligible', () => {
        //Add parent details
        cy.contains('Run a check for one parent or guardian').click();
        cy.url().should('include', '/Check/Enter_Details');
        cy.get('#FirstName').type(parentFirstName);
        cy.get('#LastName').type(parentLastName);
        cy.get('[id="DateOfBirth.Day"]').type('01');
        cy.get('[id="DateOfBirth.Month"]').type('01');
        cy.get('[id="DateOfBirth.Year"]').type('1990');
        cy.get('#identifierType-nino').check();
        cy.get('#NationalInsuranceNumber').type(NIN);
        cy.contains('button', 'Run check').click();

        //eligible outcome
        cy.get('h2.govuk-notification-banner__title', { timeout: 80000 }).should('include.text', 'Children eligible');
        cy.contains('a.govuk-link', 'Do another check');
    });

    it('Will allow a basic user to run a check with an asylum support reference', () => {
        cy.contains('Run a check for one parent or guardian').click();
        cy.url().should('include', '/Check/Enter_Details');
        cy.get('#FirstName').type(parentFirstName);
        cy.get('#LastName').type(parentLastName);
        cy.get('[id="DateOfBirth.Day"]').type('01');
        cy.get('[id="DateOfBirth.Month"]').type('01');
        cy.get('[id="DateOfBirth.Year"]').type('1990');
        cy.get('#identifierType-nass').check();

        cy.get('#NationalAsylumSeekerServiceNumber')
            .should('be.visible')
            .and('be.enabled')
            .type('120156789');
        cy.get('#NationalInsuranceNumber').should('be.disabled');
        cy.contains('button', 'Run check').click();

        cy.get('h2.govuk-notification-banner__title', { timeout: 80000 }).should('be.visible');
    });
    
    it('Will show updated guidance when a basic user check is not eligible', () => {
        cy.checkSession('basic');
    
        cy.visit((Cypress.config().baseUrl ?? "") + "/home");
        cy.get('.govuk-caption-l').should('include.text', 'Manchester City Council');
    
        cy.contains('Run a check for one parent or guardian').click();
    
        cy.url().should('include', '/Check/Enter_Details');
    
        cy.get('#FirstName').clear().type(parentFirstName);
        cy.get('#LastName').clear().type(parentLastName);
        cy.get('[id="DateOfBirth.Day"]').clear().type('01');
        cy.get('[id="DateOfBirth.Month"]').clear().type('01');
        cy.get('[id="DateOfBirth.Year"]').clear().type('1990');
        cy.get('#identifierType-nino').check();
        cy.get('#NationalInsuranceNumber').clear().type('PN123456A');
    
        cy.contains('button', 'Run check').click();
    
        cy.get('h2.govuk-notification-banner__title', { timeout: 80000 })
            .should('contain.text', 'Not eligible');
    
        cy.contains(
            'You can contact the relevant school to ask them to collect it from the parent or guardian.',
            { timeout: 80000 }
        ).should('be.visible');
    
        cy.contains('request a separate check').should('not.exist');
        cy.contains('Department for Education support desk').should('not.exist');
    });
});
