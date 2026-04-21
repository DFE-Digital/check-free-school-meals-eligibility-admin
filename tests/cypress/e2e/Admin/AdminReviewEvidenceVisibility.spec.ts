describe('SchoolCanReviewEvidence dashboard tile visibility', () => {

    it('shows review tiles for non-MAT schools whose LA has the flag enabled', () => {
        cy.checkSession('schoolNonMatFlagOn');
        cy.visit((Cypress.config().baseUrl ?? "") + "/home");

        cy.get('.govuk-caption-l').should('include.text', 'The Astley Cooper School');
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

        cy.get('.govuk-caption-l').should('include.text', 'The Aldgate School');
        cy.get('h1').should('include.text', 'Manage eligibility for free school meals');
        cy.contains('a', 'Pending applications').should('not.exist');
        cy.contains('a', 'Guidance for reviewing evidence').should('not.exist');
    });

    it('shows review tiles for MAT-linked schools when the MAT flag is enabled even if the LA flag is disabled', () => {
        cy.checkSession('matSchoolWithLaFlagDisabled');
        cy.visit((Cypress.config().baseUrl ?? "") + "/home");

        cy.get('.govuk-caption-l').should('contain.text', 'Altrincham Grammar School');
        cy.get('h1').should('include.text', 'Manage eligibility for free school meals');
        cy.contains('a', 'Pending applications').should('be.visible');
        cy.contains('a', 'Guidance for reviewing evidence')
            .should('be.visible')
            .and('have.attr', 'href')
            .and('include', '/Home/Guidance');
    });
});