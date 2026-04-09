describe('Review evidence tile visibility for school users', () => {

    it('shows review tiles for non-MAT schools whose LA flag is enabled', () => {
        cy.checkSession('school');
        cy.visit((Cypress.config().baseUrl ?? "") + "/home");

        cy.get('.govuk-caption-l').should('include.text', 'The Telford Park School');
        cy.get('h1').should('include.text', 'Manage eligibility for free school meals');
        cy.contains('a', 'Pending applications').should('be.visible');
        cy.contains('a', 'Guidance for reviewing evidence')
            .should('be.visible')
            .and('have.attr', 'href')
            .and('include', '/Home/Guidance');
    });

    it('does not show review tiles for non-MAT schools whose LA flag is disabled', () => {
        cy.checkSession('schoolCanReviewEvidenceDisabled');
        cy.visit((Cypress.config().baseUrl ?? "") + "/home");

        cy.get('.govuk-caption-l').should('include.text', 'The Astley Cooper School');
        cy.get('h1').should('include.text', 'Manage eligibility for free school meals');
        cy.contains('a', 'Pending applications').should('not.exist');
        cy.contains('a', 'Guidance for reviewing evidence').should('not.exist');
    });

    it('shows review tiles for MAT schools even when their LA flag is disabled', () => {
        cy.checkSession('matSchoolWithLaFlagDisabled');
        cy.visit("/home");

        cy.get('.govuk-caption-l').should('include.text', 'The Meadows Primary School');
        cy.get('h1').should('include.text', 'Manage eligibility for free school meals');

        cy.contains('a', 'Pending applications').should('be.visible');
        cy.contains('a', 'Guidance for reviewing evidence')
            .should('be.visible')
            .and('have.attr', 'href')
            .and('include', '/Home/Guidance');
    });
});