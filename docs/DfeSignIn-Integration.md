# DfE Sign-in Integration

This document explains how the application integrates with DfE Sign-in for authentication and authorization, including how user roles are fetched from the DfE Sign-in Public API.

## Overview

The application uses two DfE Sign-in services:

1. **OpenID Connect (OIDC)** - For user authentication
2. **Public API** - For fetching user roles and permissions

```mermaid
flowchart TB
    subgraph User
        A[User Browser]
    end
    
    subgraph App["Application"]
        B[HomeController]
        C[DfeSignInExtensions]
        D[DfeSignInApiService]
    end
    
    subgraph DfE["DfE Sign-in"]
        E[OIDC Provider]
        F[Public API]
    end
    
    A -->|1. Access App| B
    B -->|2. Redirect to Login| E
    E -->|3. User Authenticates| E
    E -->|4. Return Claims| C
    C -->|5. Extract User & Org| B
    B -->|6. Request Roles| D
    D -->|7. JWT Auth| F
    F -->|8. Return Roles| D
    D -->|9. Roles| B
    B -->|10. Authorize & Render| A
```

## Supported Organisation Types

The FSM application supports three types of organisations:

| Organisation Type | Category Name | Required Role Code | DfE Sign-in Service |
|-------------------|---------------|-------------------|---------------------|
| Local Authority | `Local Authority` | `fsmLocalAuthority` | FSM - Local Authorities |
| School | `Establishment` | `fsmSchoolRole` | FSM - Schools |
| Multi-Academy Trust | `Multi-Academy Trust` | `fsmMATRole` | FSM - MATs |

Each organisation type has its own service configuration in DfE Sign-in with a corresponding role that must be assigned to users.

## Authentication Flow

### Step 1: OIDC Authentication

When a user accesses the application, they are redirected to DfE Sign-in for authentication. Upon successful login, DfE Sign-in returns claims containing:

- **User Information**: ID, email, first name, surname
- **Organisation Information**: ID, name, category (e.g., "Local Authority", "Establishment", "Multi-Academy Trust")

```mermaid
sequenceDiagram
    participant User
    participant App
    participant DfE as DfE Sign-in OIDC
    
    User->>App: Access protected page
    App->>DfE: Redirect to login
    User->>DfE: Enter credentials
    DfE->>DfE: Validate credentials
    DfE->>App: Return authorization code
    App->>DfE: Exchange code for tokens
    DfE->>App: Return ID token + claims
    App->>App: Extract user & org from claims
    App->>User: Continue to authorization
```

### Step 2: Role Authorization via Public API

**Important**: User roles are NOT included in the OIDC token claims when using the `code` response type. Roles must be fetched separately from the DfE Sign-in Public API.

```mermaid
sequenceDiagram
    participant App
    participant API as DfE Sign-in Public API
    
    App->>App: Generate JWT with API Secret
    App->>API: GET /services/{serviceId}/organisations/{orgId}/users/{userId}
    API->>API: Validate JWT signature
    API->>App: Return user roles
    App->>App: Check for required role based on org type
```

## Configuration

### Required Settings

Add the following to your `appsettings.json` or environment-specific settings:

```json
{
  "DfeSignIn": {
    "Authority": "https://oidc.signin.education.gov.uk",
    "MetaDataUrl": "https://oidc.signin.education.gov.uk/.well-known/openid-configuration",
    "ClientId": "<your-client-id>",
    "ClientSecret": "<your-client-secret>",
    "APIServiceProxyUrl": "https://api.signin.education.gov.uk",
    "APIServiceSecret": "<your-api-secret>",
    "CallbackUrl": "/auth/cb",
    "SignoutCallbackUrl": "/home/index",
    "SignoutRedirectUrl": "/",
    "Scopes": [
      "openid",
      "email",
      "profile",
      "organisation"
    ],
    "CookieName": "fsm-login",
    "CookieExpireTimeSpanInMinutes": 5,
    "GetClaimsFromUserInfoEndpoint": true,
    "SaveTokens": true,
    "SlidingExpiration": true
  }
}
```

### Environment URLs

| Environment | OIDC Authority | API URL |
|-------------|----------------|---------|
| Test | `https://test-oidc.signin.education.gov.uk` | `https://test-api.signin.education.gov.uk` |
| Production | `https://oidc.signin.education.gov.uk` | `https://api.signin.education.gov.uk` |

### Secrets

There are **two different secrets** required:

| Secret | Purpose | Where to find |
|--------|---------|---------------|
| `ClientSecret` | OIDC authentication | DfE Sign-in console → Service Configuration → Client secret |
| `APIServiceSecret` | Public API authentication | DfE Sign-in console → Service Configuration → API secret |

> ⚠️ **Important**: These are different values. Make sure you copy the correct secret for each purpose.

## Code Architecture

### Key Components

```mermaid
classDiagram
    class DfeSignInExtensions {
        +AddDfeSignInAuthentication(services, config)
        +GetDfeClaims(claims) DfeClaims
    }
    
    class IDfeSignInApiService {
        <<interface>>
        +GetUserRolesAsync(userId, orgId) Task~IList~Role~~
    }
    
    class DfeSignInApiService {
        -HttpClient _httpClient
        -IDfeSignInConfiguration _configuration
        -ILogger _logger
        +GetUserRolesAsync(userId, orgId) Task~IList~Role~~
        -GenerateApiToken() string
    }
    
    class DfeClaims {
        +Organisation Organisation
        +UserInformation User
        +IList~Role~ Roles
    }
    
    class HomeController {
        -IDfeSignInApiService _dfeSignInApiService
        +Index() Task~IActionResult~
    }
    
    IDfeSignInApiService <|.. DfeSignInApiService
    HomeController --> IDfeSignInApiService
    DfeSignInExtensions --> DfeClaims
    HomeController --> DfeClaims
```

### File Locations

| File | Purpose |
|------|---------|
| `Infrastructure/DfeSignInExtensions.cs` | OIDC setup and claims extraction |
| `Infrastructure/DfeSignInApiService.cs` | Public API integration for roles |
| `Infrastructure/IDfeSignInApiService.cs` | API service interface |
| `Infrastructure/IDfeSignInConfiguration.cs` | Configuration interface |
| `Infrastructure/DfeSignInConfiguration.cs` | Configuration implementation |
| `Domain/DfeSignIn/DfeClaims.cs` | Claims model (user, org, roles) |
| `Domain/DfeSignIn/Role.cs` | Role model |
| `Models/Constants.cs` | Organisation type and role code constants |

## Authorization Logic

The `HomeController.Index` method implements the following authorization checks:

```mermaid
flowchart TD
    A[User Accesses Home] --> B{Organisation Category?}
    B -->|Local Authority| C[Required Role: fsmLocalAuthority]
    B -->|Establishment| D[Required Role: fsmSchoolRole]
    B -->|Multi-Academy Trust| E[Required Role: fsmMATRole]
    B -->|Other/Unknown| F[Show UnauthorizedOrganization View]
    
    C --> G[Fetch Roles from API]
    D --> G
    E --> G
    
    G --> H{Has Required Role?}
    H -->|No| I[Show UnauthorizedRole View]
    H -->|Yes| J[Show Home View]
```

### Organisation Types and Required Roles

| Organisation Category | Constant | Required Role Code | Role Constant |
|----------------------|----------|-------------------|---------------|
| Local Authority | `Constants.CategoryTypeLA` | `fsmLocalAuthority` | `Constants.RoleCodeLA` |
| Establishment (School) | `Constants.CategoryTypeSchool` | `fsmSchoolRole` | `Constants.RoleCodeSchool` |
| Multi-Academy Trust | `Constants.CategoryTypeMAT` | `fsmMATRole` | `Constants.RoleCodeMAT` |

### Code Example

```csharp
// Determine the required role based on organization type
string? requiredRoleCode = categoryName switch
{
    Constants.CategoryTypeLA => Constants.RoleCodeLA,        // "fsmLocalAuthority"
    Constants.CategoryTypeSchool => Constants.RoleCodeSchool, // "fsmSchoolRole"
    Constants.CategoryTypeMAT => Constants.RoleCodeMAT,       // "fsmMATRole"
    _ => null
};
```

## JWT Token Generation for API

The Public API requires a JWT bearer token signed with the API secret:

```csharp
// Token structure
{
  "iss": "<client-id>",           // Your service's Client ID
  "aud": "signin.education.gov.uk", // Fixed audience
  "iat": 1733843721,               // Issued at (Unix timestamp)
  "exp": 1733844021                // Expires (Unix timestamp, +5 mins)
}
```

The token is signed using **HMAC-SHA256** with the API secret as the key.

## API Endpoint

### Get User Roles

```
GET {APIServiceProxyUrl}/services/{clientId}/organisations/{organisationId}/users/{userId}
```

**Headers:**
```
Authorization: Bearer <jwt-token>
```

**Response Example (Local Authority user):**
```json
{
  "userId": "C1F3A68B-F487-4895-9508-DC11DA29C567",
  "serviceId": "FSM-LocalAuthorities",
  "organisationId": "d30e3bf7-9116-4243-989c-d20cc063dab2",
  "roles": [
    {
      "id": "20965",
      "name": "FSM - Local Authority Role",
      "code": "fsmLocalAuthority",
      "numericId": "20965",
      "status": {
        "id": 1
      }
    }
  ]
}
```

**Response Example (School user):**
```json
{
  "userId": "A2B3C4D5-E6F7-8901-2345-678901234567",
  "serviceId": "FSM-Schools",
  "organisationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "roles": [
    {
      "id": "20964",
      "name": "FSM - School Role",
      "code": "fsmSchoolRole",
      "numericId": "20964",
      "status": {
        "id": 1
      }
    }
  ]
}
```

**Response Example (MAT user):**
```json
{
  "userId": "B3C4D5E6-F7A8-9012-3456-789012345678",
  "serviceId": "FSM-MATs",
  "organisationId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "roles": [
    {
      "id": "21058",
      "name": "FSM - MAT Role",
      "code": "fsmMATRole",
      "numericId": "21058",
      "status": {
        "id": 1
      }
    }
  ]
}
```

## DfE Sign-in Service Configuration

Three separate services are configured in DfE Sign-in for FSM:

### FSM - Local Authorities
- **Service Name**: FSM - Local Authorities
- **Role Name**: FSM - Local Authority Role
- **Role Code**: `fsmLocalAuthority`
- **Role ID**: 20965

### FSM - Schools
- **Service Name**: FSM - Schools
- **Role Name**: FSM - School Role
- **Role Code**: `fsmSchoolRole`
- **Role ID**: 20964

### FSM - MATs
- **Service Name**: FSM - MATs
- **Role Name**: FSM - MAT Role
- **Role Code**: `fsmMATRole`
- **Role ID**: 21058

## Troubleshooting

### Common Issues

| Error | Cause | Solution |
|-------|-------|----------|
| `403 Forbidden - invalid signature` | Wrong API secret or Client ID | Verify `APIServiceSecret` and `ClientId` match DfE Sign-in console |
| `403 Forbidden` | API secret vs Client secret confusion | Ensure you're using the **API secret**, not the Client secret |
| Empty roles | API not configured | Check service has API access enabled in DfE Sign-in |
| `UnauthorizedOrganization` view | User's org is not LA, School, or MAT | User must belong to one of the three supported organisation types |
| `UnauthorizedRole` view | User missing required role | Assign the appropriate role to user in DfE Sign-in based on their org type |

### Debugging

Enable debug logging to see API calls:

```json
{
  "Logging": {
    "LogLevel": {
      "CheckYourEligibility.Admin.Infrastructure.DfeSignInApiService": "Debug"
    }
  }
}
```

This will log:
- The API URL being called
- The API response (including roles)
- Any errors encountered

## References

- [DfE Sign-in Help](https://help.signin.education.gov.uk/)
- [DfE Sign-in Service Configuration](https://manage.signin.education.gov.uk/)
- [OpenID Connect Specification](https://openid.net/connect/)

---

## Appendix: The Hotel Analogy

To understand how DfE Sign-in works with our application, imagine a luxury hotel with multiple facilities and services.

### The Players

| Technical Term | Hotel Analogy |
|----------------|---------------|
| **User** | Guest arriving at the hotel |
| **DfE Sign-in** | The hotel's front desk / reception |
| **Our Application** | A restaurant inside the hotel |
| **ClientId** | The restaurant's business license number |
| **ClientSecret** | The secret handshake between the restaurant and front desk |
| **APIServiceSecret** | The restaurant manager's master key |
| **ID Token** | Guest's hotel key card |
| **JWT Bearer Token** | Signed request letter from the restaurant manager |
| **Organisation (LA/School/MAT)** | The guest's company/employer |
| **Role** | The guest's job title/access level |

### The Journey

```mermaid
sequenceDiagram
    autonumber
    participant Guest as 🧑 Guest
    participant FrontDesk as 🏨 Front Desk<br/>(DfE Sign-in)
    participant Restaurant as 🍽️ Restaurant<br/>(Our App)
    participant BackOffice as 📋 Back Office<br/>(DfE API)

    Note over Guest,Restaurant: Phase 1: Check-in (Authentication)
    Guest->>Restaurant: I'd like to dine here
    Restaurant->>FrontDesk: Please verify this guest<br/>(with my business license)
    FrontDesk->>Guest: Please show your ID
    Guest->>FrontDesk: Here are my credentials
    FrontDesk->>FrontDesk: Verify identity
    FrontDesk->>Guest: Here's your key card<br/>(ID Token)
    Guest->>Restaurant: Here's my hotel key card

    Note over Restaurant,BackOffice: Phase 2: Authorization Check
    Restaurant->>Restaurant: Read key card:<br/>Guest from "Local Authority" or<br/>"School" or "MAT"
    Restaurant->>BackOffice: What's this guest's access level?<br/>(signed letter from manager)
    BackOffice->>BackOffice: Verify manager's signature
    BackOffice->>Restaurant: Guest has "FSM LA/School/MAT Role"

    Note over Guest,Restaurant: Phase 3: Access Decision
    alt Has Required Access for Org Type
        Restaurant->>Guest: Welcome! Right this way...
    else Wrong Org Type
        Restaurant->>Guest: Sorry, we only serve LA, School, or MAT guests
    else Missing Role
        Restaurant->>Guest: Sorry, you need the FSM role for your org
    end
```

### How It Works

#### Step 1: Guest Arrives (User clicks "Sign in")
Just like a guest walking up to a hotel restaurant, the user tries to access our application. The restaurant (our app) can't verify guests directly—they must go through the front desk.

#### Step 2: Front Desk Verification (OIDC Authentication)
The restaurant sends the guest to the front desk with their business license number (`ClientId`). This proves to the front desk that the restaurant is a legitimate partner. The front desk asks the guest for credentials (username/password), verifies them, and issues a key card (ID Token).

```csharp
// The key card contains basic guest info
var claims = new DfeClaims
{
    FirstName = "John",                    // Guest's name
    LastName = "Smith",
    Email = "john@authority.gov",          // Contact details
    UserId = "abc-123",                    // Unique guest ID
    OrganisationId = "org-456",            // Which org they work for
    OrganisationName = "Example LA",
    OrganisationCategory = "Local Authority" // LA, School, or MAT
};
```

#### Step 3: Checking Guest's Access Level (API Role Fetch)
The key card tells us WHO the guest is, WHICH ORGANISATION they represent, and WHAT TYPE of organisation it is, but not WHAT ROLE they have. To find this out, the restaurant manager writes a signed letter (JWT token) to the back office:

```
"Dear Back Office,
 I'm the manager of Restaurant ABC (iss: ClientId)
 Please tell me about guest abc-123 from company org-456
 Signed at: 10:30 AM today
 Valid until: 10:40 AM
 — Manager (signed with APIServiceSecret)"
```

The back office verifies the manager's signature and responds with the guest's roles.

#### Step 4: Access Decision
Now the restaurant knows:
- ✅ The guest is who they say they are (authenticated)
- ✅ They work for a valid organisation type (LA, School, or MAT)
- ✅ They have the correct role for their organisation type

The guest is welcomed to their table! 🎉

### Why Two Secrets?

```mermaid
flowchart LR
    subgraph Secrets["🔐 Two Different Secrets"]
        CS["ClientSecret<br/>───────────<br/>Proves our app is<br/>legitimate to DfE Sign-in<br/>during login"]
        AS["APIServiceSecret<br/>───────────<br/>Signs our requests<br/>to the DfE API<br/>for fetching roles"]
    end

    subgraph Usage["📝 When Used"]
        CS --> OIDC["OIDC Login Flow"]
        AS --> API["API Requests"]
    end

    style CS fill:#e1f5fe
    style AS fill:#fff3e0
```

| Secret | Hotel Analogy | Technical Purpose |
|--------|---------------|-------------------|
| **ClientSecret** | Restaurant's partnership agreement with the hotel | Used during OIDC authentication to prove our app is registered with DfE Sign-in |
| **APIServiceSecret** | Manager's signature key | Used to sign JWT tokens when calling the DfE API to fetch user roles |

### The Key Insight

Just like a hotel separates "verifying who you are" (front desk) from "checking what you can access" (back office systems), DfE Sign-in separates:

1. **Authentication** (OIDC) — "Is this person really John Smith from Local Authority/School/MAT X?"
2. **Authorization** (API) — "Does John Smith have the FSM role for their organisation type?"

This separation of concerns provides:
- **Better security**: The ID token doesn't contain sensitive role information
- **Flexibility**: Roles can be updated without re-issuing ID tokens
- **Granularity**: Different services can have different role structures
- **Multi-org support**: Same user can have different roles in different organisations
