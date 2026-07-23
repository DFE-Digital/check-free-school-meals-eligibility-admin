describe('Admin journey export CSV', () => {
  before(() => {
    cy.checkSession('LA');
  });

  it('Can export search results as CSV', () => {
    // Intercept the export request and verify response
    cy.intercept('GET', '**/ExportSearchResults').as('exportRequest');

    // Navigate to results
    cy.visit((Cypress.config().baseUrl ?? "") + "/home");
    cy.contains('Search all records').click();
    cy.url().should('include', 'Application/SearchResults');
    cy.get('.govuk-table').should('be.visible');

    // Click export and verify response
    cy.contains('Export as CSV').should('be.visible').click();
    cy.wait('@exportRequest').then((interception) => {
      const headers = interception.response?.headers;
    
      expect(headers?.['content-type']).to.include('text/csv');
    
      const contentDisposition = headers?.['content-disposition'];
      expect(contentDisposition).to.exist;
      expect(contentDisposition).to.include('eligibility-applications-');
      expect(contentDisposition).to.include('.csv');
    });
  });
});
