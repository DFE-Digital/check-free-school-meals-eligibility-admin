import "cypress-file-upload";
import { waitForStatusCompleted } from "../../support/BulkCheckHelper";

const services = [
  { session: "basic", enhanced: false, school: false, template: "BulkCheckTemplate.csv" },
  { session: "LA", enhanced: true, school: false, template: "BulkCheckTemplate_Enhanced.csv" },
  { session: "school", enhanced: true, school: true, template: "BulkCheckTemplate_Enhanced_School.csv" },
];

services.forEach((service) => {
  const headers = service.enhanced
    ? ["Parent First Name", "Parent Last Name", "Parent Date of Birth", "Parent National Insurance number",
        "Child First Name", "Child Last Name", "Child Date of Birth"]
    : ["Parent Last Name", "Parent Date of Birth", "Parent National Insurance number"];
  if (service.enhanced && !service.school) headers.push("Child School Urn");
  if (service.enhanced) headers.push("Parent Email Address (optional)");
  headers.push("Reference (optional)");
  const row = (reference: string, email = "") => {
    const fields = service.enhanced
      ? ["John", "Smith", "1985-03-15", "AB123456C", "Emily", "Smith", "2015-09-10"]
      : ["Smith", "1985-03-15", "AB123456C"];
    if (service.enhanced && !service.school) fields.push("150716"); // Telford Park School, as in existing journey tests.
    if (service.enhanced) fields.push(email);
    fields.push(reference);
    return fields.join(",");
  };
  const upload = (content: string, fileName: string) => {
    cy.get('input[type="file"]').attachFile({ fileContent: content, fileName, mimeType: "text/csv" });
    cy.contains("button", "Run a batch check").click();
  };

  describe(`FSM ${service.session} batch references`, () => {
    beforeEach(() => {
      cy.checkSession(service.session);
      // The Basic journey deliberately exhausts the upload allowance in its cached session.
      // Start each reference case with fresh application state after restoring sign-in.
      cy.clearCookie(".AspNetCore.Session");
      cy.visit("/home");
      cy.contains("Run a batch check").click();
    });

    it("shows the optional-column guidance and serves the matching template under its existing filename", () => {
      cy.contains("h2", "Download our CSV template").should("be.visible");
      if (service.enhanced) {
        cy.contains("p", "There are 2 optional columns for adding:").next("ul")
          .should("contain.text", "parent email address")
          .and("contain.text", "reference - for further information or reference numbers");
        cy.contains("li", "Make sure you include:").children("ul").first()
          .should("not.contain.text", "email");
      } else {
        cy.contains("p", "There is an optional ‘Reference’ column where you can add reference numbers or additional information.")
          .should("be.visible");
      }
      cy.contains("a", "batch check template").should("have.attr", "href").then((href) => {
        cy.request(String(href)).then((response) => {
          expect(response.status).to.eq(200);
          expect(String(response.body).replace(/^\uFEFF/, "").trim()).to.eq(headers.join(","));
          expect(response.headers["content-disposition"]).to.include(service.template);
        });
      });
    });

    it("identifies a missing Reference header", () => {
      upload(headers.slice(0, -1).join(",") + "\r\n" + row(""), "missing-reference.csv");
      cy.get("#file-upload-1-error").should("contain.text",
        "The selected file must include the column heading Reference (optional).");
    });

    it("rejects a reference over 100 characters after trimming", () => {
      upload(headers.join(",") + "\r\n" + row("  " + "x".repeat(101) + "  "), "overlong-reference.csv");
      cy.contains("tbody tr", "Reference must be 100 characters or fewer.")
        .find("td").first().should("have.text", "2");
    });

    it("downloads the new batch with trimmed references, blank values and independent optional email", () => {
      const fileName = `references-${service.session}-${Date.now()}-${Cypress._.random(100000, 999999)}.csv`;
      const expectedReferences = ["a".repeat(100), "", "MiXeD-003"];
      const emails = ["Parent@Example.COM", "", ""];
      const rows = [row("  " + expectedReferences[0] + "  ", "  " + emails[0] + "  "), row(""), row("  MiXeD-003  ")];
      upload(headers.join(",") + "\r\n" + rows.join("\r\n"), fileName);
      cy.contains("h1", "Batch checks history", { timeout: 80000 }).should("be.visible");
      waitForStatusCompleted(fileName, 18);
      cy.contains("tr", fileName).contains("a", "Download results")
        .should("have.attr", "href").then((href) => {
          cy.request(String(href)).then((response) => {
            expect(response.status).to.eq(200);
            const lines = String(response.body).replace(/^\uFEFF/, "").split(/\r?\n/).filter((line) => line.length > 0);
            const actualHeaders = lines[0].split(",");
            const expectedHeaders = service.enhanced
              ? ["Parent First Name", "Parent Last Name", "Parent Date of Birth", "Parent National Insurance number",
                  "Child First Name", "Child Last Name", "Child Date of Birth", "Child School Urn", "Parent Email Address (optional)", "Outcome"]
              : ["Parent Last Name", "Parent Date of Birth", "Parent National Insurance number", "Outcome"];
            // Live policy can be standard or expanded; NUnit verifies both controller/map selections explicitly.
            if (actualHeaders.indexOf("Eligibility End Date") >= 0) expectedHeaders.push("Eligibility End Date");
            expectedHeaders.push("Reference (optional)");
            expect(actualHeaders).to.deep.eq(expectedHeaders);
            expect(lines).to.have.length(4);
            lines.slice(1).forEach((line, index) => {
              // These fields contain no commas/newlines; NUnit covers CSV quoting and Unicode.
              const fields = line.split(",");
              expect(fields).to.have.length(expectedHeaders.length);
              expect(fields[fields.length - 1]).to.eq(expectedReferences[index]);
              if (service.enhanced) expect(fields[8]).to.eq(emails[index]);
            });
          });
        });
    });
  });
});
