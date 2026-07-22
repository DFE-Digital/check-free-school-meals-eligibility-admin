describe('Admin FSM Reporting', () => {   

    const getReportIds = ($tbody: JQuery<HTMLElement>): string[] =>
        Array.from($tbody[0].querySelectorAll<HTMLTableRowElement>('tr[data-report-id]'))
            .map((row) => row.dataset.reportId)
            .filter((reportId): reportId is string => Boolean(reportId));

    const waitForReportToComplete = (
        reportId: string,
        attemptsRemaining = 40
    ): Cypress.Chainable<JQuery<HTMLElement>> => {
        return cy.get(`tr[data-report-id="${reportId}"]`).then(($row) => {
            const status = $row.find('.govuk-tag').text().trim();

            if (status === 'Complete') {
                return cy.wrap($row);
            }

            if (status.includes('System error')) {
                throw new Error(`Report ${reportId} failed to generate`);
            }

            if (attemptsRemaining === 0) {
                throw new Error(`Report ${reportId} did not complete in time`);
            }

            return cy.wait(2000)
                .then(() => cy.reload())
                .then(() => waitForReportToComplete(reportId, attemptsRemaining - 1));
        });
    };

    beforeEach(() => {
        cy.checkSession('basic');
        cy.visit((Cypress.config().baseUrl ?? '') + '/home');
        cy.get('.govuk-caption-l').should('include.text', 'Manchester City Council');
    });

    it('Can generate, download and delete an FSM report', () => {
        let existingReportIds: string[] = [];
        let generatedReportId = '';

        cy.contains('a.dfe-card-link--header', 'Reports').click();
        cy.get('.govuk-heading-l').should('include.text', 'Report history');

        cy.get('.govuk-table tbody').then(($tbody) => {
            existingReportIds = getReportIds($tbody);
        });

        cy.contains('Generate report').click();
        cy.url().should('include', '/EligibilityCheckReporting/Create_Report');
        cy.get('.govuk-heading-l').should('include.text', 'Generate a report');
        cy.get('#StartDate\\.Day').should('not.exist');
        cy.get('#StartDate\\.Month').should('not.exist');
        cy.get('#StartDate\\.Year').should('not.exist');
        cy.get('#EndDate\\.Day').should('not.exist');
        cy.get('#EndDate\\.Month').should('not.exist');
        cy.get('#EndDate\\.Year').should('not.exist');

        cy.get('select[name="DateRange"]')
            .should('be.visible')
            .find('option')
            .should('have.length', 1)
            .and('contain.text', 'Last 30 days');

        cy.get('select[name="CheckType"]')
            .should('be.visible')
            .find('option')
            .should('have.length', 3);

        cy.get('select[name="CheckType"]').find('option')
            .should('contain.text', 'All checks')
            .and('contain.text', 'Individual checks')
            .and('contain.text', 'Batch checks');

        cy.contains('Generate report').click();

        cy.url({ timeout: 80000 })
            .should('include', '/EligibilityCheckReporting/Reports');

        cy.get('.govuk-table tbody')
            .should(($tbody) => {
                const newReportIds = getReportIds($tbody)
                    .filter((reportId) => !existingReportIds.includes(reportId));

                expect(newReportIds, 'new report IDs').to.have.length(1);
            })
            .then(($tbody) => {
                generatedReportId = getReportIds($tbody)
                    .find((reportId) => !existingReportIds.includes(reportId))!;

                expect(generatedReportId, 'generated report ID').not.to.be.empty;
            });

        cy.then(() => waitForReportToComplete(generatedReportId));

        cy.intercept(
            'GET',
            '**/EligibilityCheckReporting/Download_Report?reportId=*'
        ).as('downloadReport');

        cy.then(() => {
            cy.get(`tr[data-report-id="${generatedReportId}"]`).within(() => {
                cy.contains('a', 'Download report').click();
            });
        });

        cy.wait('@downloadReport').then(({ response }) => {
            expect(response?.statusCode).to.eq(200);
            expect(response?.headers['content-type']).to.include('text/csv');

            const contentDisposition =
                response?.headers['content-disposition'] as string;

            const filenameMatch =
                contentDisposition.match(/filename="?([^";]+\.csv)"?/i);

            expect(filenameMatch, 'download filename').not.to.be.null;

            const filename = filenameMatch![1];

            cy.readFile(`cypress/downloads/${filename}`, {
                timeout: 20000
            })
                .should('not.be.empty')
                .and('contain', 'Parent Surname');
        });

        cy.then(() => {
            cy.get(`tr[data-report-id="${generatedReportId}"]`).within(() => {
                cy.contains('a', 'Delete').click();
            });
        });

        cy.url()
            .should('include', '/EligibilityCheckReporting/Delete_Report_Confirmation');

        cy.contains('button', 'Delete report').click();

        cy.url({ timeout: 80000 })
            .should('include', '/EligibilityCheckReporting/Reports');

        cy.get('.govuk-table tbody')
            .find(`tr[data-report-id="${generatedReportId}"]`)
            .should('not.exist');
    });

    it('Can view historical reports on reports page', () => {
        cy.contains('a.dfe-card-link--header', 'Reports').click();
        cy.get('.govuk-heading-l').should('include.text', 'Report history');

        cy.get('.govuk-table__head').within(() => {
            cy.contains('th', 'Report generated').should('be.visible');
            cy.contains('th', 'Start date').should('be.visible');
            cy.contains('th', 'End date').should('be.visible');
            cy.contains('th', 'Generated by').should('be.visible');
            cy.contains('th', 'Number of results').should('be.visible');
            cy.contains('th', 'Status').should('be.visible');
        });

        cy.get('.govuk-table tbody').then(($tbody) => {
            const rows = $tbody.find('tr[data-report-id]').toArray();
        
            rows.forEach((row) => {
                cy.wrap(row).within(() => {
                    cy.get('.govuk-tag').invoke('text').then((status) => {
                        if (status.trim() === 'Complete') {
                            cy.contains('a', 'Download report').should('be.visible');
                            cy.contains('a', 'Delete').should('be.visible');
                        }
                    });
                });
            });
        });

        cy.get('nav.govuk-pagination').should('be.visible');
    });
});
