# Parent notification scenarios

This document lists every point in the admin application where an email notification is sent to a
parent/guardian, what triggers it, and which `NotificationType` is used.

Source: `CheckYourEligibility.Admin/Controllers/CheckController.cs` and
`CheckYourEligibility.Admin/Controllers/ApplicationController.cs`.

## Summary table

| # | Trigger (user action) | Controller / action | Decision | Notification sent |
|---|---|---|---|---|
| 1 | Parent/guardian details submitted, check comes back **eligible**, application created with no evidence needed | `CheckController.Check_Answers_Post` | Submitted application status == `Entitled` | `ParentApplicationSuccessful` |
| 2 | Parent/guardian details submitted, check comes back **not eligible** (or any other outcome), evidence attached, application created for review | `CheckController.Check_Answers_Post` | Submitted application status != `Entitled` | `ParentApplicationEvidenceSent` |
| 3 | School/LA sends an appeal application for review with supporting evidence | `ApplicationController.ApplicationDetailAppealSend` | Status patched to `SentForReview` | `ParentApplicationEvidenceSent` |
| 4 | School/LA approves a pending ("Sent for review") application | `ApplicationController.ApplicationApproveSend` | Status patched to `ReviewedEntitled` | `ParentApplicationSuccessful` |
| 5 | School/LA declines a pending ("Sent for review") application | `ApplicationController.ApplicationDeclineSend` | Status patched to `ReviewedNotEntitled` | `ParentApplicationUnsuccessful` |

All notification sends are wrapped in `try/catch`: if sending fails, the error is logged but the
user-facing flow (redirect) still completes.

## Flow diagram

```mermaid
flowchart TD
    A[Parent details entered] --> B[Eligibility check run]
    B --> C{Check result}

    C -->|eligible| D[Skip evidence upload]
    C -->|notEligible / parentNotFound / error / other| E[Evidence upload forced]

    D --> F[Check_Answers submitted]
    E --> F

    F --> G[Application created via API]
    G --> H{Application status returned}

    H -->|Entitled| N1[["Notification 1:<br/>ParentApplicationSuccessful"]]
    H -->|Not Entitled<br/>e.g. SentForReview| N2[["Notification 2:<br/>ParentApplicationEvidenceSent"]]

    subgraph Later - School / LA review of a pending application
        P[School/LA opens Pending Applications] --> Q[Appeal sent for review<br/>ApplicationDetailAppealSend]
        Q --> N3[["Notification 3:<br/>ParentApplicationEvidenceSent"]]

        R[School/LA records a decision<br/>on Sent for review application]
        R -->|Approve| N4[["Notification 4:<br/>ParentApplicationSuccessful"]]
        R -->|Decline| N5[["Notification 5:<br/>ParentApplicationUnsuccessful"]]
    end
```

## Sequence view (per scenario)

```mermaid
sequenceDiagram
    participant Parent
    participant SchoolLA as School / Local authority
    participant Admin as Admin app
    participant API as Eligibility API
    participant Notify as Notification service

    Note over Parent,Notify: Scenario 1 & 2 - initial check + application submission
    Parent->>SchoolLA: Provides details
    SchoolLA->>Admin: Enters details, runs check
    Admin->>API: Perform eligibility check
    API-->>Admin: Check result (eligible / notEligible / ...)
    alt eligible
        SchoolLA->>Admin: Continue to add child details (no evidence)
    else not eligible
        SchoolLA->>Admin: Continue + upload evidence
    end
    SchoolLA->>Admin: Check answers (submit)
    Admin->>API: Create application
    API-->>Admin: Application status
    alt status == Entitled
        Admin->>Notify: ParentApplicationSuccessful
    else status != Entitled
        Admin->>Notify: ParentApplicationEvidenceSent
    end
    Notify-->>Parent: Email

    Note over Parent,Notify: Scenario 3 - appeal sent for review
    SchoolLA->>Admin: Send appeal for review
    Admin->>API: Patch status = SentForReview
    Admin->>Notify: ParentApplicationEvidenceSent
    Notify-->>Parent: Email

    Note over Parent,Notify: Scenario 4 & 5 - decision recorded on a reviewed application
    SchoolLA->>Admin: Record a decision (Approve / Decline)
    alt Approve
        Admin->>API: Patch status = ReviewedEntitled
        Admin->>Notify: ParentApplicationSuccessful
    else Decline
        Admin->>API: Patch status = ReviewedNotEntitled
        Admin->>Notify: ParentApplicationUnsuccessful
    end
    Notify-->>Parent: Email
```

## Notes / caveats

- The choice between `ParentApplicationSuccessful` and `ParentApplicationEvidenceSent` in
  `Check_Answers_Post` is based on the **application status returned by the API after submission**,
  not the earlier check result. In practice these are correlated (an `eligible` check normally
  leads to `Entitled`; a `notEligible` check normally leads to evidence being required and a
  non-`Entitled` status such as `SentForReview`), but the code itself only inspects the submitted
  application's status.
- `Entitled` is the first (default, value `0`) member of the `ApplicationStatus` enum. If the API
  ever returned an application with an unset/unrecognised status, it would default to `Entitled`
  and would incorrectly trigger `ParentApplicationSuccessful`. This same risk already existed in
  the pre-existing `ApplicationsRegistered` vs `AppealsRegistered` redirect logic.
- There is a fourth `NotificationType` value, `ParentApplicationEvidenceToTakeToSchool`, which is
  defined in `Domain/Enums/NotificationType.cs` but is not currently sent from any controller
  action.
- **Bulk check has no notifications.** `BulkCheckController` (and its use cases) never references
  `ISendNotificationUseCase`/`NotificationRequest`/`NotificationType` anywhere. Only single checks
  (`CheckController`) and application review decisions (`ApplicationController`) trigger parent
  emails; bulk-submitted checks/applications do not notify parents at all today.
