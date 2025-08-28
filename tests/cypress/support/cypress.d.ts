/// <reference types="cypress" />

declare namespace Cypress {
  interface Cookie {
    name: string;
    value: string;
    domain: string;
    path: string;
    secure: boolean;
    httpOnly: boolean;
    expiry?: number;
  }

  interface CookieDefaults {
    preserve: string[] | ((cookie: Cookie) => boolean);
  }

  interface Cookies {
    defaults(options: CookieDefaults): void;
  }

  interface CookieData {
    timestamp: number;
    cookies: Cypress.Cookie[];
  }

  interface Chainable<Subject = any> {
    checkSession(userType: string): Chainable<void>;
    login(userType: string): Chainable<void>;
    loginSchoolUser(): Chainable<void>;
    loginLocalAuthorityUser(): Chainable<void>;
    loginMultiAcademyTrustUser(): Chainable<void>;
    storeCookies(userType: string): Chainable<void>;
    loadCookies(userType: string): Chainable<void>;
    SignInSchool(): Chainable<void>;
    SignInLA(): Chainable<void>;
    SignInMAT(): Chainable<void>;
    CheckValuesInSummaryCard(sectionTitle: string, key: string, expectedValue: string): Chainable<void>;
    scanPagesForValue(value: string): Chainable<void>;
    scanPagesForNewValue(value: string): Chainable<void>;
    scanPagesForStatusAndClick(value: string): Chainable<void>;
    findApplicationFinalise(value: string): Chainable<void>;
    findNewApplicationFinalise(value: string): Chainable<void>;
    clickButtonByText(buttonText: string): Chainable<Element>;
    typeIntoInput(selector: string, text: string): Chainable<void>;
    verifyFieldVisibility(selector: string, isVisible: boolean): Chainable<void>;
    enterDate(daySelector: string, monthSelector: string, yearSelector: string, day: string, month: string, year: string): Chainable<void>;
    clickButtonByRole(role: string): Chainable<void>;
    clickButton(text: string): Chainable<void>;
    verifyH1Text(expectedText: string): Chainable<void>;
    selectYesNoOption(selector: string, isYes: boolean): Chainable<Element>;
    retainAuthOnRedirect(initialUrl: string, authHeader: string, alias: string): Chainable<void>;
  }
}