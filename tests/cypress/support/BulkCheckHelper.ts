export function waitForStatusCompleted(fileName, retries = 5) {
  if (retries === 0) {
    throw new Error("Status did not become 'Checks completed' in time");
  }
  cy.contains("table tbody tr", fileName)
    .should("exist")
    .then(($row) => {
      const status = $row.find("td").eq(5).text().trim();
      if (!status.includes("Checks completed")) {
        cy.wait(5000);
        cy.reload();
        waitForStatusCompleted(fileName, retries - 1);
      }
    });
}
