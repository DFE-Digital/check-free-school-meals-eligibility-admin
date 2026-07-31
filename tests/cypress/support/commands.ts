import 'cypress-file-upload';

function getCookiesPath(userType: string): string {
  switch (userType) {
    case 'school':
      return 'cypress/fixtures/SchoolUserCookies.json';
    case 'schoolCanReviewEvidenceDisabled':
      return 'cypress/fixtures/SchoolUserFlagOffCookies.json';
    case 'matSchoolWithLaFlagDisabled':
      return 'cypress/fixtures/MatSchoolFlagOffCookies.json';
    case 'schoolNonMatFlagOn':
      return 'cypress/fixtures/SchoolNonMatFlagOnCookies.json';
    case 'matSchoolWithMatFlagDisabled':
      return 'cypress/fixtures/MatSchoolMatFlagOffCookies.json';
    case 'MAT':
      return 'cypress/fixtures/MATUserCookies.json';
    case 'LA':
      return 'cypress/fixtures/LAUserCookies.json';
    default:
      return '';
  }
}

const expectedOrganisation: Record<string, string> = {
  school: 'The Telford Park School',
  schoolCanReviewEvidenceDisabled: 'The Aldgate School',
  matSchoolWithLaFlagDisabled: 'Altrincham Grammar School For Girls',
  matSchoolWithMatFlagDisabled: 'The Telford Park School',
  schoolNonMatFlagOn: 'The Astley Cooper School',
  MAT: 'Thomas Telford Multi Academy Trust',
  basic: 'Manchester City Council',
  LA: 'Telford And Wrekin Council',
};

Cypress.Commands.add('checkSession', (userType: string) => {
  return cy.login(userType);
});

Cypress.Commands.add('login', (userType) => {
  return cy.session(
    ['role', userType],
    () => {
      if (userType === 'school') {
        cy.loginSchoolUser();
      } else if (userType === 'schoolCanReviewEvidenceDisabled') {
        cy.loginSchoolUserCanReviewEvidenceDisabled();
      } else if (userType === 'matSchoolWithLaFlagDisabled') {
        cy.loginMatSchoolWithLaFlagDisabled();
      } else if (userType === 'matSchoolWithMatFlagDisabled') {
        cy.loginMatSchoolWithMatFlagDisabled();
      } else if (userType === 'schoolNonMatFlagOn') {
        cy.loginSchoolNonMatFlagOn();
      } else if (userType === 'MAT') {
        cy.loginMultiAcademyTrustUser();
      } else if (userType === 'basic') {
        cy.loginBasicUser();
      } else {
        cy.loginLocalAuthorityUser();
      }
    },
    {
      validate() {
        cy.visit((Cypress.config().baseUrl ?? '') + '/home', {
          failOnStatusCode: false,
        });
        cy.get('.govuk-caption-l').should(
          'include.text',
          expectedOrganisation[userType]
        );
      },
      cacheAcrossSpecs: true,
    }
  );
});

Cypress.Commands.add('loginSchoolNonMatFlagOn', () => {
  cy.reload();
  cy.visit((Cypress.config().baseUrl ?? "") + "/home");
  cy.get('#username').type(Cypress.env('DFE_ADMIN_EMAIL_ADDRESS'));
  cy.get('button[type="submit"]').click();
  cy.get('#password').type(Cypress.env('DFE_ADMIN_PASSWORD'));
  cy.get('button[type="submit"]').click();
  cy.reload();

  cy.contains('The Astley Cooper School')
    .parent()
    .find('input[type="radio"]')
    .check();

  cy.contains('Continue').click();
});

Cypress.Commands.add('loginSchoolUser', () => {
  // Log in as a school user - For persisting session use checkSession('school')
  cy.reload();
  cy.visit((Cypress.config().baseUrl ?? "") + "/home");
  cy.get('#username').type(Cypress.env('DFE_ADMIN_EMAIL_ADDRESS'));
  cy.get('button[type="submit"]').click();
  cy.get('#password').type(Cypress.env('DFE_ADMIN_PASSWORD'));
  cy.get('button[type="submit"]').click();
  cy.reload();
  cy.contains('The Telford Park School')
    .parent()
    .find('input[type="radio"]')
    .check();
  cy.contains('Continue').click();
});

Cypress.Commands.add('loginSchoolUserCanReviewEvidenceDisabled', () => {
  // Log in as a school user whose LA has the review flag disabled
  // For persisting session use checkSession('schoolCanReviewEvidenceDisabled')
  cy.reload();
  cy.visit((Cypress.config().baseUrl ?? "") + "/home");
  cy.get('#username').type(Cypress.env('DFE_ADMIN_EMAIL_ADDRESS'));
  cy.get('button[type="submit"]').click();
  cy.get('#password').type(Cypress.env('DFE_ADMIN_PASSWORD'));
  cy.get('button[type="submit"]').click();
  cy.reload();

  cy.contains('The Aldgate School')
    .parent()
    .find('input[type="radio"]')
    .check();

  cy.contains('Continue').click();
});

Cypress.Commands.add('loginMatSchoolWithLaFlagDisabled', () => {
  cy.reload(true);
  cy.visit((Cypress.config().baseUrl ?? "") + "/home");
  cy.get('#username').type(Cypress.env('DFE_ADMIN_EMAIL_ADDRESS'));
  cy.get('button[type="submit"]').click();
  cy.get('#password').type(Cypress.env('DFE_ADMIN_PASSWORD'));
  cy.get('button[type="submit"]').click();
  cy.reload();

  cy.contains('Altrincham Grammar School for Girls (Open)')
    .parent()
    .find('input[type="radio"]')
    .check();

  cy.contains('Continue').click();
});

Cypress.Commands.add('loginMatSchoolWithMatFlagDisabled', () => {
  cy.reload(true);
  cy.visit((Cypress.config().baseUrl ?? "") + "/home");
  cy.get('#username').type(Cypress.env('DFE_ADMIN_EMAIL_ADDRESS'));
  cy.get('button[type="submit"]').click();
  cy.get('#password').type(Cypress.env('DFE_ADMIN_PASSWORD'));
  cy.get('button[type="submit"]').click();
  cy.reload();

  cy.contains('The Telford Park School')
    .closest('.govuk-radios__item')
    .find('input[type="radio"]')
    .check({ force: true });

  cy.contains('Continue').click();
});

Cypress.Commands.add('loginLocalAuthorityUser', () => {
  // Log in as a local authority user - For persisting session use checkSession('LA')
  cy.reload(true);
  cy.visit((Cypress.config().baseUrl ?? "") + "/home");
  cy.get('#username').type(Cypress.env('DFE_ADMIN_EMAIL_ADDRESS'));
  cy.get('button[type="submit"]').click();
  cy.get('#password', { timeout: 10000 }).type(Cypress.env('DFE_ADMIN_PASSWORD'));
  cy.get('button[type="submit"]').click();
  cy.contains('Telford and Wrekin Council')
    .parent()
    .find('input[type="radio"]')
    .check();
  cy.contains('Continue').click();
});

Cypress.Commands.add('loginBasicUser', () => {
  // Log in as a local authority user - For persisting session use checkSession('LA')
  cy.reload(true);
  cy.visit((Cypress.config().baseUrl ?? "") + "/home")
  cy.get('#username').type(Cypress.env('DFE_ADMIN_EMAIL_ADDRESS'));
  cy.get('button[type="submit"]').click();
  cy.get('#password').type(Cypress.env('DFE_ADMIN_PASSWORD'));
  cy.get('button[type="submit"]').click();
  cy.contains('MANCHESTER CITY COUNCIL')
    .parent()
    .find('input[type="radio"]')
    .check();
  cy.contains('Continue').click();
});

Cypress.Commands.add('loginMultiAcademyTrustUser', () => {
  // Log in as a Multi Academy Trust user - For persisting session use checkSession('MAT')
  cy.reload(true);
  cy.visit((Cypress.config().baseUrl ?? "") + "/home");
  cy.get('#username').type(Cypress.env('DFE_ADMIN_EMAIL_ADDRESS'));
  cy.get('button[type="submit"]').click();
  cy.get('#password').type(Cypress.env('DFE_ADMIN_PASSWORD'));
  cy.get('button[type="submit"]').click();
  cy.contains('THOMAS TELFORD MULTI ACADEMY TRUST')
    .parent()
    .find('input[type="radio"]')
    .check();
  cy.contains('Continue').click();
});

Cypress.Commands.add('storeCookies', (userType: string) => {
  const filePath = getCookiesPath(userType);
  cy.getCookies().then((cookies: Cypress.Cookie[]) => {
    const data: Cypress.CookieData = {
      timestamp: Date.now(),
      cookies: cookies
    };
    if (userType === 'basic') { }
    else { cy.writeFile(filePath, data); }
  });
});

Cypress.Commands.add('loadCookies', (userType: string) => {
  const filePath = getCookiesPath(userType);
  cy.readFile(filePath).then((data: Cypress.CookieData) => {
    if (data && data.cookies) {
      const currentTime = Date.now();
      const twoHoursInMillis = 60 * 60 * 1000; //Changed from 2 hours to 1 hour. Actual invalidation time unknown.
      if (currentTime - data.timestamp < twoHoursInMillis) {
        data.cookies.forEach((cookie: Cypress.Cookie) => {
          cy.setCookie(cookie.name, cookie.value, {
            domain: cookie.domain,
            path: cookie.path,
            secure: cookie.secure,
            httpOnly: cookie.httpOnly,
            expiry: cookie.expiry,
          });
        });
      } else {
        cy.log('Cookies are older than 1 hour, forcing new login');
        if (userType === 'school') {
          cy.login('school');
        } else if (userType === 'schoolCanReviewEvidenceDisabled') {
          cy.login('schoolCanReviewEvidenceDisabled');
        } else if (userType === 'matSchoolWithLaFlagDisabled') {
          cy.login('matSchoolWithLaFlagDisabled');
        } else if (userType === 'matSchoolWithMatFlagDisabled') {
          cy.login('matSchoolWithMatFlagDisabled');
        } else if (userType === 'schoolNonMatFlagOn') {
          cy.login('schoolNonMatFlagOn');
        } else if (userType === 'MAT') {
          cy.login('MAT');
        } else if (userType === 'basic') {
          cy.login('basic');
        } else {
          cy.login('LA');
        }
      }
    } else {
      cy.log('Invalid cookie data, forcing new login');
      if (userType === 'school') {
        cy.login('school');
      } else if (userType === 'schoolCanReviewEvidenceDisabled') {
        cy.login('schoolCanReviewEvidenceDisabled');
      } else if (userType === 'matSchoolWithLaFlagDisabled') {
        cy.login('matSchoolWithLaFlagDisabled');
      } else if (userType === 'matSchoolWithMatFlagDisabled') {
        cy.login('matSchoolWithMatFlagDisabled');
      } else if (userType === 'schoolNonMatFlagOn') {
        cy.login('schoolNonMatFlagOn');
      } else if (userType === 'MAT') {
        cy.login('MAT');
      } else if (userType === 'basic') {
        cy.login('basic');
      } else {
        cy.login('LA');
      }
    }
  });
});

Cypress.Commands.add('CheckValuesInSummaryCard', (sectionTitle: string, key: string, expectedValue: string) => {
  cy.contains('.govuk-summary-card__title', sectionTitle)
    .parents('.govuk-summary-card')
    .within(() => {
      cy.contains('.govuk-summary-list__key', key)
        .siblings('.govuk-summary-list__value')
        .should('include.text', expectedValue)
    });
});

Cypress.Commands.add('scanPagesForValue', (value, maxPages = 3) => {
  const checkForValue = (pageCount = 1) => {
    cy.get('body').then((body) => {
      if (body.find(`td a:contains("${value}")`).length > 0) {
        cy.get(`td a:contains("${value}")`).click();
      }
      else if (
        pageCount < maxPages &&
        body.find('.govuk-pagination__next a').length > 0
      ) {
        cy.get('.govuk-pagination__next a')
          .click()
          .then(() => {
            checkForValue(pageCount + 1);
          });
      }
      else {
        throw new Error(`Record not found within ${maxPages} pages`);
      }
    });
  };

  checkForValue();
});

Cypress.Commands.add('scanPagesForNewValue', (value, maxPages = 3) => {
  const checkForValue = (pageCount = 1) => {
    cy.get('body').then((body) => {
      if (body.find(`td a:contains("${value}")`).length > 0) {
        cy.get(`td a:contains("${value}")`).click();
      } 
      else if (pageCount < maxPages && body.find('.govuk-pagination__prev a').length > 0) {
        cy.get('.govuk-pagination__prev a')
          .click()
          .then(() => {
            checkForValue(pageCount + 1);
          });
      } 
      else {
        throw new Error(`Record not found within ${maxPages} pages`);
      }
    });
  };

  // Start by navigating to the last page
  cy.get('.govuk-pagination__list')
    .find('a[href*="PageNumber"]')
    .not('[rel="next"]')
    .last()
    .click()
    .then(() => {
      checkForValue();
    });
});

Cypress.Commands.add('scanPagesForStatusAndClick', (value: string) => {

  cy.get('body').then(($body) => {
    if ($body.text().includes(value)) {
      cy.get('tr').contains('strong', value).parents('tr').within(() => {
        cy.get('a.govuk-link').click();
      });
    } else {
      cy.get('nav.govuk-pagination').contains('a.govuk-pagination__link', 'Next').click().then(() => {
        cy.wait(2000);
        cy.scanPagesForStatusAndClick(value);
      }
      )
    };
  });
})

Cypress.Commands.add('findApplicationFinalise', (value: string) => {
  let referenceFound = false;
  function searchOnPage() {
    cy.get('.govuk-table tbody tr').each(($row) => {
      cy.wrap($row).find('td').eq(1).invoke('text').then((text) => {
        if (text.trim() === value) {
          referenceFound = true;
          cy.wrap($row).find('td').eq(0).find('input[type="checkbox"]').click();
          return false;
        }
      });
    }).then(() => {
      if (!referenceFound) {
        cy.get('.govuk-link').contains('Next').then(($nextButton) => {
          if ($nextButton.length > 0) {
            cy.wrap($nextButton).click({ force: true }).then(() => {
              cy.wait(500);
              searchOnPage();
            });
          } else {
            cy.log('Reference number could not be found');
          }
        })
      }
    });
  }
  searchOnPage();
});

Cypress.Commands.add('findNewApplicationFinalise', (value: string, maxPages = 3) => {
  let referenceFound = false;

  const searchOnPage = (pageCount = 1) => {
    cy.get('.govuk-table tbody tr')
      .each(($row) => {
        cy.wrap($row)
          .find('td')
          .eq(1)
          .invoke('text')
          .then((text) => {
            if (text.trim() === value) {
              referenceFound = true;
              cy.wrap($row)
                .find('td')
                .eq(0)
                .find('input[type="checkbox"]')
                .click();
              return false;
            }
          });
      })
      .then(() => {
        if (!referenceFound) {
          if (pageCount < maxPages) {
            cy.get('body').then((body) => {
              if (body.find('.govuk-link:contains("Previous")').length > 0) {
                cy.contains('.govuk-link', 'Previous')
                  .click({ force: true })
                  .then(() => {
                    cy.wait(500);
                    searchOnPage(pageCount + 1);
                  });
              } else {
                throw new Error(`Record not found and no more pages available`);
              }
            });
          } 
          else {
        throw new Error(`Record not found within ${maxPages} pages`);
          }
        }
      });
  };

  // Start by navigating to the last page
  cy.get('.govuk-pagination__list')
    .find('a[href*="PageNumber"]')
    .not('[rel="next"]')
    .last()
    .click()
    .then(() => {
      searchOnPage();
    });
});

Cypress.Commands.add('verifyFieldVisibility', (selector: string, isVisible: boolean) => {
  if (isVisible) {
    cy.get(selector).should('be.visible');
  } else {
    cy.get(selector).should('not.be.visible');
  }
});


Cypress.Commands.add('verifyH1Text', (expectedText: string) => {
  cy.contains('h1', expectedText).should('be.visible');
  cy.get('h1').invoke('text').then((actualText: string) => {
    expect(actualText.trim()).to.eq(expectedText);
  });
});

Cypress.Commands.add('selectYesNoOption', (baseSelector: string, isYes: boolean) => {
  const finalSelector = isYes ? `${baseSelector}[value="true"]` : `${baseSelector}[value="false"]`;
  cy.log(`selector being used: ${finalSelector}`)
  cy.get(finalSelector).click();
});

Cypress.Commands.add('retainAuthOnRedirect', (initialUrl, authHeader, alias) => {
  let redirectUrl: string;

  cy.intercept(initialUrl, (req) => {
    req.continue((res) => {
      const locationHeader = res.headers['location'];
      if (Array.isArray(locationHeader)) {
        redirectUrl = locationHeader[0];
      } else {
        redirectUrl = locationHeader;
      }
    });
  }).as('initialRequest');

  cy.request({
    url: initialUrl,
    headers: {
      'Authorization': authHeader,
    },
    followRedirect: false,
  }).then(() => {
    expect(redirectUrl).to.exist;

    cy.request({
      url: redirectUrl,
      headers: {
        'Authorization': authHeader,
      }
    }).as(alias);
  });
});

