import { waitForStatusCompleted } from "../../support/BulkCheckHelper";

const bulkBasicUploadAttemptLimit = Number(
  Cypress.env("BULK_UPLOAD_ATTEMPT_LIMIT") ?? 10,
);
const bulkBasicOverLimitRowCount = Number(
  Cypress.env("BULK_OVER_LIMIT_ROW_COUNT") ?? 6001,
);

const createBasicBulkCsv = (rowCount: number): string => {
  const header =
    "Parent Last Name,Parent Date of Birth,Parent National Insurance number";
  const rows = Array.from({ length: rowCount }, (_, index) => {
    const day = ((index % 28) + 1).toString().padStart(2, "0");
    return `Tester,${day}/01/2000,AB123456C`;
  });
  return [header, ...rows].join("\n");
};

describe("BasicLAHappyPath", () => {
  let skipSetupBasic = false;
  beforeEach(() => {
    if (!skipSetupBasic) {
      cy.checkSession("basic");
      cy.visit((Cypress.config().baseUrl ?? "") + "/home");
      cy.wait(1);
      cy.get(".govuk-caption-l").should(
        "include.text",
        "Manchester City Council",
      );
      cy.contains("Run a batch check").click();
      cy.url().should("include", "/BulkCheck/Bulk_Check");
    }
  });

  it("will return an error message if the bulk file contains header content that doesn't match the template", () => {
    cy.fixture(
      "BulkCheckFileValidation/BASIC-bulkchecktemplate_invalid_HeadersContent.csv",
    ).then((fileContent1) => {
      cy.get('input[type="file"]').attachFile([
        {
          fileContent: fileContent1,
          fileName: "BASIC-bulkchecktemplate_invalid_HeadersContent.csv",
          mimeType: "text/csv",
        },
      ]);
    });
    cy.contains("button", "Run a batch check").click();
    cy.get("#file-upload-1-error").as("errorMessage");
    cy.get("@errorMessage").should(($p) => {
      expect($p.first()).to.contain(
        "Invalid CSV format. Missing required header: 'Parent Date of Birth'",
      );
    });
  });

  it("will return an error message if the bulk file contains wrong number of headers or out of sequence headers", () => {
    cy.fixture(
      "BulkCheckFileValidation/BASIC-bulkchecktemplate_invalid_HeadersSequenceOrCount.csv",
    ).then((fileContent1) => {
      cy.get('input[type="file"]').attachFile([
        {
          fileContent: fileContent1,
          fileName:
            "BASIC-bulkchecktemplate_invalid_HeadersSequenceOrCount.csv",
          mimeType: "text/csv",
        },
      ]);
    });
    cy.contains("button", "Run a batch check").click();
    cy.get("#file-upload-1-error").as("errorMessage");
    cy.get("@errorMessage").should(($p) => {
      expect($p.first()).to.contain(
        "The column headers in the selected file must exactly match the template",
      );
    });
  });

  it("will return an error message if the bulk file contains more than the configured row limit", () => {
    const overLimitCsv = createBasicBulkCsv(bulkBasicOverLimitRowCount + 1);
    cy.get('input[type="file"]').attachFile([
      {
        fileContent: overLimitCsv,
        fileName: "bulkcheck_over_limit.csv",
        mimeType: "text/csv",
      },
    ]);
    cy.contains("button", "Run a batch check").click();

    cy.get("#file-upload-1-error")
      .should(($el) => {
        expect($el.text()).to.match(/CSV file cannot contain more than\s+\d+\s+records/);
      });
  });

it("will run a successful batch check", () => {
  cy.fixture("BulkCheckFileValidation/BASIC-bulkchecktemplate_complete.csv")
    .then((fileContent1) => {
      cy.get('input[type="file"]').attachFile([
        {
          fileContent: fileContent1,
          fileName: "BASIC-bulkchecktemplate_complete.csv",
          mimeType: "text/csv",
        },
      ]);
    });

  cy.contains("button", "Run a batch check").click();

  cy.get("h1").should("include.text", "Batch checks history");

  const today = new Date()
    .toLocaleDateString("en-GB", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    })
    .replace(",", "");

  cy.contains("table tbody tr", "BASIC-bulkchecktemplate_complete.csv")
    .should("exist")
    .then(($row) => {
      cy.wrap($row).find("td").eq(0).invoke("text").should("match", /\.csv$/i);
      cy.wrap($row).find("td").eq(1).should("have.text", "15");
      cy.wrap($row).find("td").eq(2).should("have.text", "TESTER"); //API sets last name value.Trim().ToUpperInvariant()
      cy.wrap($row).find("td").eq(3).should("contain.text", today);
      cy.wrap($row).find("td").eq(4).invoke("text").should("not.be.empty");
      cy.wrap($row).find("td").eq(5).find("strong").should("have.class", "govuk-tag");
    });

  waitForStatusCompleted("BASIC-bulkchecktemplate_complete.csv");

  cy.contains("table tbody tr", "BASIC-bulkchecktemplate_complete.csv")
    .should("exist")
    .then(($row) => {
      cy.wrap($row).find("td").eq(5).should("contain.text", "Checks completed");

      cy.wrap($row)
        .find("td")
        .eq(6)
        .should("contain.text", "Download results")
        .and("contain.text", "Delete");
    });
});

  it("will run a successful batch check when last name contains a curly apostrophe", () => {
    cy.fixture(
      "BulkCheckFileValidation/BASIC-bulkchecktemplate_curly_apostrophe.csv",
    ).then((fileContent1) => {
      cy.get('input[type="file"]').attachFile([
        {
          fileContent: fileContent1,
          fileName: "BASIC-bulkchecktemplate_curly_apostrophe.csv",
          mimeType: "text/csv",
        },
      ]);
    });
    cy.get('input[type="file"]').attachFile(
      "BulkCheckFileValidation/BASIC-bulkchecktemplate_curly_apostrophe.csv",
    );

    cy.get('input[type="file"]').should(($input) => {
      expect(($input[0] as HTMLInputElement).files?.length).to.eq(1);
    });

    cy.contains("button", "Run a batch check").click();

    cy.get("h1", { timeout: 80000 }).should(
      "include.text",
      "Batch checks history",
    );

    cy.contains(
      "table tbody tr",
      "BASIC-bulkchecktemplate_curly_apostrophe.csv",
      { timeout: 80000 },
    ).should("exist");
  });

  it("Navigate to Batch checks history and delete a batch check if one exists", () => {
    cy.contains("a", "Batch checks history").click();
    cy.get("h1", { timeout: 80000 }).should(
      "include.text",
      "Batch checks history",
    );

    
    cy.get("body").then(($body) => {
        const deleteLinks = $body.find("a").filter((_, el) =>
          el.innerText.trim().includes("Delete"),
        );

        if (deleteLinks.length === 0) {
          cy.log("No delete links found");
          return;
        }

        cy.wrap(deleteLinks[0]).click();

        cy.get("h3.govuk-notification-banner__heading").should(
          "contain.text",
          "Batch check deleted successfully.",
        );

      });
  });

  it("does not count failed uploads towards the attempt limit", () => {
    // Upload more failing files than the attempt limit - none of these should
    // ever trigger the "exceeded" error, since only successful uploads count.
    for (let i = 0; i <= bulkBasicUploadAttemptLimit + 3; i++) {
      cy.fixture(
        "BulkCheckFileValidation/BASIC-bulkchecktemplate_invalid_HeadersContent.csv",
      ).then((fileContent1) => {
        cy.get('input[type="file"]').attachFile([
          {
            fileContent: fileContent1,
            fileName: "BASIC-bulkchecktemplate_invalid_HeadersContent.csv",
            mimeType: "text/csv",
          },
        ]);
      });
      cy.contains("button", "Run a batch check").click();

      cy.get("#file-upload-1-error")
        .should(
          "contain",
          "Invalid CSV format. Missing required header: 'Parent Date of Birth'",
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
    const maxAttempts = bulkBasicUploadAttemptLimit + 2;

    for (let i = 0; i < maxAttempts; i++) {
      cy.then(() => limitReached).then((reached) => {
        if (reached) return;

        const csv = createBasicBulkCsv(1);
        cy.get('input[type="file"]').attachFile([
          {
            fileContent: csv,
            fileName: `basic-valid-${i}.csv`,
            mimeType: "text/csv",
          },
        ]);

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
