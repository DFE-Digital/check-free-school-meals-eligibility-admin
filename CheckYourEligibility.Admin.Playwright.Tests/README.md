# FSM Admin Playwright proof of concept

This project demonstrates a hybrid end-to-end testing approach:

1. Create isolated test data through the Eligibility Checking Engine API.
2. Sign in to FSM Admin through DfE Sign-in using Playwright.
3. Exercise and verify the archive-record browser journey.
4. Delete the synthetic application through the API in a `finally` block.

The test is restricted to loopback URLs and will refuse to run against deployed environments.

## Prerequisites

- .NET 8 SDK
- Chromium installed for Playwright
- FSM Admin running locally using its HTTPS launch profile
- Eligibility Checking Engine API running locally
- A DfE test account with access to The Telford Park School
- API credentials whose scope includes `application`, `local_authority`, and `admin`

## Install Chromium

Build the project first:

```powershell
dotnet build `
    .\CheckYourEligibility.Admin.Playwright.Tests\CheckYourEligibility.Admin.Playwright.Tests.csproj
```

Then install Chromium:

```powershell
& .\CheckYourEligibility.Admin.Playwright.Tests\bin\Debug\net8.0\playwright.ps1 `
    install chromium
```
## Configuration

Configure these process-scoped environment variables. Do not commit their values.

| Variable | Purpose |
| --- | --- |
| `FSM_ADMIN_BASE_URL` | Local FSM Admin HTTPS URL, normally `https://localhost:7228/` |
| `FSM_API_BASE_URL` | Local Eligibility Checking Engine API URL, normally `https://localhost:7117/` |
| `DFE_ADMIN_EMAIL_ADDRESS` | DfE Sign-in test-account email |
| `DFE_ADMIN_PASSWORD` | DfE Sign-in test-account password |
| `FSM_API_AUTHORISATION_USERNAME` | API OAuth username |
| `FSM_API_AUTHORISATION_PASSWORD` | API OAuth password |
| `FSM_API_AUTHORISATION_SCOPE` | API OAuth scope |

## Start the services

Start FSM Admin from this repository:

```powershell
dotnet run `
    --launch-profile https `
    --project .\CheckYourEligibility.Admin\CheckYourEligibility.Admin.csproj
```

Start the API from the separate `eligibility-checking-engine` repository:

```powershell
dotnet run `
    --project .\CheckYourEligibility.API\CheckYourEligibility.API.csproj
```

Keep both processes running.

## Run the test

The test is marked explicit because it requires local services and credentials. Run it directly from the FSM Admin repository:

```powershell
dotnet test `
    .\CheckYourEligibility.Admin.Playwright.Tests\CheckYourEligibility.Admin.Playwright.Tests.csproj `
    --filter "FullyQualifiedName~ApplicationArchiveTests"
```

Ordinary solution test runs will discover but skip this local-only POC.

The test creates a uniquely identifiable synthetic application for The Telford Park School, archives it through the browser, verifies the confirmation page, and deletes it through the API.