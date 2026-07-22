import { waitForStatusCompleted } from "../../support/BulkCheckHelper";
const bulkUploadAttemptLimit = Number(
  Cypress.env("BULK_UPLOAD_ATTEMPT_LIMIT") ?? 10,
);
const bulkOverLimitRowCount = Number(
  Cypress.env("BULK_OVER_LIMIT_ROW_COUNT") ?? 6001,
);

//session configuration
const sessionConfigs = {
  school: {
    fixtureInvalid:
      "BulkCheckFileValidation/bulkchecktemplate_invalid_headers.csv",
    includeSchoolURN: false,
  },
  LA: {
    fixtureInvalid:
      "BulkCheckFileValidation/bulkchecktemplate_invalid_headers.csv",
    includeSchoolURN: true,
  },
};

//dynamic CSV generator
const createBulkCsv = (rowCount: number, includeSchoolURN: boolean): string => {
  const baseHeader =
    "Parent First Name,Parent Last Name,Parent Date of Birth,Parent National Insurance number,Child First Name,Child Last Name,Child Date of Birth";

  const header = includeSchoolURN
    ? `${baseHeader},Child School Urn`
    : baseHeader;

  const rows = Array.from({ length: rowCount }, (_, index) => {
    const day = ((index % 28) + 1).toString().padStart(2, "0");

    const baseRow = [
      `John`,
      `Smith`,
      `${day}/01/2000`,
      `AB123456C`,
      `Jay`,
      `Smith`,
      `${day}/01/2010`,
    ];

    if (includeSchoolURN) {
      baseRow.push("150716"); //Telford Park School URN
    }

    return baseRow.join(",");
  });

  return [header, ...rows].join("\n");
};

//helper upload
const uploadFile = (fileContent: any, fileName: string) => {
  cy.get('input[type="file"]').attachFile([
    {
      fileContent,
      fileName,
      mimeType: "text/csv",
    },
  ]);
};

// loop sessions
Object.entries(sessionConfigs).forEach(([sessionType, config]) => {
  describe(`Admin Bulk Check Journey (${sessionType})`, () => {
    beforeEach(() => {
      cy.checkSession(sessionType);
      cy.visit((Cypress.config().baseUrl ?? "") + "/home");
      cy.contains("Run a batch check").click();
      cy.url().should("include", "Bulk_Check");
    });

    it("returns error for invalid headers", () => {
      cy.fixture(config.fixtureInvalid).then((fileContent) => {
        uploadFile(fileContent, "invalid_headers.csv");
      });

      cy.contains("button", "Run a batch check").click();

      cy.get("#file-upload-1-error").should(
        "contain",
        "The column headers in the selected file must exactly match the template",
      );
    });

    it("returns error when row limit exceeded", () => {
      const csv = createBulkCsv(
        bulkOverLimitRowCount + 1,
        config.includeSchoolURN,
      );

      uploadFile(csv, "over_limit.csv");

      cy.contains("button", "Run a batch check").click();

    cy.get("#file-upload-1-error")
      .should(($el) => {
        expect($el.text()).to.match(/CSV file cannot contain more than\s+\d+\s+records/);
      });
    });

    it("runs a successful batch check", () => {
      const csv = createBulkCsv(10, config.includeSchoolURN);
      uploadFile(csv, "valid.csv");

      cy.contains("button", "Run a batch check").click();

      cy.get("h1").should(
        "include.text",
        "Batch checks history",
      );

      const today = new Date()
        .toLocaleDateString("en-GB", {
          day: "2-digit",
          month: "short",
          year: "numeric",
        })
        .replace(",", "");

      cy.contains("table tbody tr", "valid.csv")
        .first()
        .should("exist")
        .within(() => {
          cy.get("td")
            .eq(0)
            .invoke("text")
            .should("match", /\.csv$/i);
          cy.get("td").eq(1).should("have.text", "10");
          cy.get("td").eq(2).should("have.text", "Smith");
          cy.get("td").eq(3).should("contain.text", today);
          cy.get("td").eq(4).invoke("text").should("not.be.empty");
          cy.get("td").eq(5).find("strong").should("have.class", "govuk-tag");
        });

      
      waitForStatusCompleted("valid.csv");

      cy.contains("table tbody tr", "valid.csv")
      .first()
      .within(() => {
        cy.get("td").eq(5).should("contain.text", "Checks completed");
      });
      cy.get("td")
            .eq(6)
            .within(() => {
              cy.contains("a", "View results").should("exist");
              cy.contains("a", "Download results").should("exist");

      });

    });

    it("does not count failed uploads towards the attempt limit", () => {
      // Upload more failing files than the attempt limit - none of these should
      // ever trigger the "exceeded" error, since only successful uploads count.
      for (let i = 0; i <= bulkUploadAttemptLimit + 3; i++) {
        cy.fixture(config.fixtureInvalid).then((fileContent) => {
          uploadFile(fileContent, "invalid_headers.csv");
        });

        cy.contains("button", "Run a batch check").click();

        cy.get("#file-upload-1-error")
          .should(
            "contain",
            "The column headers in the selected file must exactly match the template",
          )
          .and(
            "not.contain",
            "You have exceeded the maximum number of bulk upload attempts",
          );
      }
    });

    it("returns error after exceeding attempt limit with successful uploads", () => {
      // Note: login cookies are cached/reused across tests (and cypress runs), so the
      // rate-limit session counter may not start at 0 here - don't assume an exact
      // number of successful uploads before the limit kicks in, just that it does
      // kick in within a generous number of attempts, and that uploads succeed
      // normally up until that point.
      let limitReached = false;
      const maxAttempts = bulkUploadAttemptLimit + 2;

      for (let i = 0; i < maxAttempts; i++) {
        cy.then(() => limitReached).then((reached) => {
          if (reached) return;

          const csv = createBulkCsv(1, config.includeSchoolURN);
          uploadFile(csv, `valid-${i}.csv`);
          cy.contains("button", "Run a batch check").click();

          cy.get("body").then(($body) => {
            if (
              $body.text().includes(
                "You have exceeded the maximum number of bulk upload attempts",
              )
            ) {
              limitReached = true;
            } else {
              cy.contains("h1", "Batch checks history").should("exist");
              cy.get(".govuk-back-link").click();
              cy.url().should("include", "Bulk_Check");
            }
          });
        });
      }

      cy.then(() => {
        expect(
          limitReached,
          `expected to hit the bulk upload attempt limit within ${maxAttempts} successful uploads`,
        ).to.be.true;
      });
    });
  });
});
