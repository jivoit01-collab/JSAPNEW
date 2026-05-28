# BKDT / BackDate Module Developer Documentation

## 1. Module Overview

BKDT means **BackDate Document**. In this project, the BKDT module lets users request temporary backdate/posting-date rights for selected SAP document types, users, branches, and date ranges.

The module is sensitive because posting dates affect financial periods, tax periods, audit trails, document sequencing, and month-end closing. A wrong backdate permission can allow users to add or update documents in a closed or controlled period.

Business problem solved:

| Problem | How BKDT helps |
|---|---|
| Users need to create or update SAP documents for an earlier date. | User creates a BackDate request instead of directly changing SAP/HANA permissions. |
| Backdate changes need approval. | Request is stored in SQL Server and routed through `[backdate]` approval workflow. |
| Finance/management need traceability. | Request detail, approval flow, creator history, and HANA status are available by API. |
| SAP/HANA rights must be applied only after approval. | Final approved status triggers HANA `OPEN_BKDT`. |

Users/departments:

| User/department | Role |
|---|---|
| Creator / business user | Requests backdate access for document add/update. |
| Finance/accounts | Reviews financial period risk. |
| Manager/approver | Approves or rejects the workflow. |
| ERP/IT team | Debugs SQL workflow, HANA procedure calls, notifications, and production issues. |

After final approval, `BackDateSaveInHana` reads the approved SQL request, builds a `BKDTModel`, and calls `ItemMasterService.SaveBKDTAsync()`. That method executes HANA procedure `OPEN_BKDT` in the selected branch schemas.

Important naming note: there is no separate `BackDateController.cs`, `IBackDateService`, or `BackDateService.cs` in this repository. BKDT is implemented inside:

| Actual file | Role |
|---|---|
| `Controllers/ItemMasterController.cs` | BKDT API endpoints under `/api/ItemMaster`. |
| `Services/Interfaces/IItemMasterService.cs` | BKDT service method declarations. |
| `Services/Implementation/ItemMasterService.cs` | BKDT SQL Server, HANA, approval, and notification logic. |
| `Models/ItemMasterModel.cs` | BKDT DTOs/models. |

## 2. Module Architecture

Main files:

| Layer | File | Responsibility |
|---|---|---|
| Controller | `Controllers/ItemMasterController.cs` | Exposes BKDT APIs under `/api/ItemMaster`. |
| Service interface | `Services/Interfaces/IItemMasterService.cs` | Defines BKDT methods. |
| Service implementation | `Services/Implementation/ItemMasterService.cs` | Handles SQL `[backdate]` procedures, HANA calls, approval, and notification. |
| Models/DTOs | `Models/ItemMasterModel.cs` | `BKDTModel`, `CreateDocumentRequest`, `ApproveRequestModel`, `RejectRequestModel`, flow/status/detail DTOs. |
| Notifications | `Services/Interfaces/INotificationService.cs`, `Services/Implementation/NotificationService.cs` | Sends FCM push notifications and inserts notification records. |
| User lookup | `Services/Interfaces/IUserService.cs`, `Services/Implementation/UserService.cs` | Used for active-user pending count notifications. |
| Security filter | `Filters/CheckUserPermissionAttribute.cs` | Exists, but BKDT controller attributes are commented out or absent. |
| App setup | `Program.cs` | Registers `IItemMasterService`, session, JWT auth, CORS, controllers. |

Frontend files:

| File/search result | Finding |
|---|---|
| `Views/*` | No dedicated BKDT Razor page was found. |
| `wwwroot/*` | No dedicated BKDT JavaScript file or `/api/ItemMaster/*BKDT*` frontend call was found. |

Architecture diagram:

```text
Frontend / Mobile / API Client
        |
        | /api/ItemMaster/* BKDT endpoints
        v
Controllers/ItemMasterController.cs
        |
        | IItemMasterService
        v
Services/Implementation/ItemMasterService.cs
        |
        +--> SQL Server [backdate] stored procedures
        |       - create request
        |       - approval workflow
        |       - document detail
        |       - HANA status
        |       - insights/history
        |
        +--> SAP HANA live schemas
        |       - GETUSERDETAILS
        |       - GETMOBJDETAILS
        |       - OPEN_BKDT
        |
        +--> NotificationService
                - FCM push
                - notification row insert
```

HANA branch mapping in `SaveBKDTAsync()`:

| Branch code | Branch name | HANA schema from `GetLiveHanaSettings()` |
|---|---|---|
| `1` | `OIL` | `JIVO_OIL_HANADB` |
| `2` | `BEVERAGES` | `JIVO_BEVERAGES_HANADB` |
| `3` | `MART` | `JIVO_MART_HANADB` |

## 3. Complete Workflow

1. User opens BackDate screen in the external frontend/mobile app.
2. Frontend loads users using `GET /api/ItemMaster/GetUserDetails?company=...`.
3. Frontend loads SAP document/object types using `GET /api/ItemMaster/GetMobjDetails?company=...`.
4. User selects branch, user, document type, date range, time limit, and action (`A` add or `U` update).
5. Frontend creates request with `POST /api/ItemMaster/CreateDocument`.
6. `ItemMasterController.CreateDocument()` checks request is not null.
7. `ItemMasterService.CreateDocumentAsync()` calls `[backdate].[jsCreateDocument]`.
8. SQL Server stores the request and creates the approval workflow.
9. SQL outputs `@newDocumentId`.
10. Service calls `[backdate].[GetUsersInCurrentStage]`.
11. Service sends FCM notifications and inserts notification rows.
12. Approver gets pending list using `GET /api/ItemMaster/GetBKDTPendingDoc`.
13. Approver approves with `POST /api/ItemMaster/ApproveBKDT` or rejects with `POST /api/ItemMaster/RejectBKDT`.
14. `ApproveBKDT` calls `[backdate].[jsApproveDocument]`.
15. Controller reads flow status from `[backdate].[jsGetFlowStatus]`.
16. If flow status is `A`, controller calls `BackDateSaveInHana(flowId)`.
17. `BackDateSaveInHana()` reads detail by flow using `[backdate].[jsGetDocumentDetailUsingFlowId]`.
18. Controller parses dates and document type, builds `BKDTModel`, then calls `SaveBKDTAsync()`.
19. `SaveBKDTAsync()` executes HANA `OPEN_BKDT` for each selected branch.
20. Controller updates SQL HANA status using `[backdate].[updateHanaStatus]`.
21. Response returns approval result, flow status, BackDate result, and HANA status text.

## 4. Before Approval vs After Approval

### Before Approval

| Area | Behavior |
|---|---|
| Create API | `POST /api/ItemMaster/CreateDocument` |
| SQL stored procedure | `[backdate].[jsCreateDocument]` |
| Request data | Stored in SQL through `[backdate]` schema procedures. Exact table names are not visible in C#. |
| Approval flow | Created by `[backdate].[jsCreateDocument]`. |
| HANA called? | Only for dropdown/master data (`GETUSERDETAILS`, `GETMOBJDETAILS`). `OPEN_BKDT` is not called in normal create flow. |
| SAP/HANA document rights updated? | No. |
| Notifications sent? | Yes, users from `[backdate].[GetUsersInCurrentStage]` are notified. |
| Statuses | Pending documents come from `[backdate].[jsGetPendingDocuments]`; full list labels include `Pending`, `Approved`, `Rejected`. |

### After Approval

| Area | Behavior |
|---|---|
| Approval API | `POST /api/ItemMaster/ApproveBKDT` |
| SQL approval method | `ItemMasterService.ApproveDocumentAsync()` |
| SQL approval SP | `[backdate].[jsApproveDocument]` |
| Final status check | `[backdate].[jsGetFlowStatus]`; HANA update runs only when `Status == "A"`. |
| HANA update method | `ItemMasterController.BackDateSaveInHana()` plus `ItemMasterService.SaveBKDTAsync()` |
| HANA procedure | `OPEN_BKDT(?,?,?,?,?,?,?,?,?,?,?)` |
| SQL status update | `[backdate].[updateHanaStatus]` |
| SAP/HANA document rights updated? | Yes only after final approved status `A` and successful `OPEN_BKDT`. |
| Notifications triggered | Approval sends notifications using `[backdate].[jsBackdateNotify]`. |

Important behavior: SQL approval happens first. Then `ApproveBKDT` checks status and runs HANA only when final status is `A`. If `OPEN_BKDT` fails, the controller can return an error from `BackDateSaveInHana`, and `[backdate].[updateHanaStatus]` may show failure or not be reached depending on failure point.

## 5. API Documentation

Base route: `/api/ItemMaster`

### Master Data / Dropdown APIs

| API | Purpose | Service method | HANA procedure |
|---|---|---|---|
| `GET /GetUserDetails?company=...` | Gets SAP users for selected company/branch. | `GetUserDetailsAsync()` | `GETUSERDETAILS()` |
| `GET /GetMobjDetails?company=...` | Gets SAP object/document types. | `GetMobjDetailsAsync()` | `GETMOBJDETAILS()` |

### Create / Direct HANA APIs

| API | Purpose | Runs | Service method | Backend call |
|---|---|---|---|---|
| `POST /CreateDocument` | Creates a BKDT approval request in SQL. | Before approval | `CreateDocumentAsync()` | `[backdate].[jsCreateDocument]` |
| `POST /SaveBKDT` | Directly executes HANA `OPEN_BKDT` from request body. | Manual/direct HANA operation | `SaveBKDTAsync()` | HANA `OPEN_BKDT` |
| `POST /BackDateSaveInHana?flowId=...` | Applies approved SQL request to HANA. | After final approval/manual retry | `BackDateSaveInHana()` then `SaveBKDTAsync()` | `[backdate].[jsGetDocumentDetailUsingFlowId]`, HANA `OPEN_BKDT`, `[backdate].[updateHanaStatus]` |

`CreateDocument` payload:

```json
{
  "branch": "1",
  "username": "manager01",
  "documentType": "13",
  "fromDate": "2026-05-01T00:00:00",
  "toDate": "2026-05-05T00:00:00",
  "timeLimit": "2026-05-09T18:00:00",
  "action": "A",
  "companyId": 1,
  "createdBy": 101
}
```

`SaveBKDT` direct payload:

```json
{
  "branch": "1,2",
  "userId": "manager01",
  "transType": 13,
  "fromDate": "01-05-2026",
  "toDate": "05-05-2026",
  "timeLimit": "2026-05-09T18:00:00",
  "rights": "NO",
  "createdBy": "101",
  "createdOn": "2026-05-09T10:30:00",
  "deletedBy": null,
  "deletedOn": "0001-01-01T00:00:00"
}
```

### List / Detail APIs

| API | Purpose | Service method | Stored procedure |
|---|---|---|---|
| `GET /GetBKDTinsights?userId=...&company=...&month=...` | Counts pending/approved/rejected. | `GetBKDTinsightsAsync()` | `[backdate].[jsGetDocumentInsight]` |
| `GET /GetBKDTPendingDoc?userId=...&company=...&month=...` | Lists pending BKDT docs. | `GetBKDTPendingDocAsync()` | `[backdate].[jsGetPendingDocuments]` |
| `GET /GetBKDTApprovedDoc?userId=...&company=...&month=...` | Lists approved BKDT docs. | `GetBKDTApprovedDocAsync()` | `[backdate].[jsGetApprovedDocuments]` |
| `GET /GetBKDTRejectedDoc?userId=...&company=...&month=...` | Lists rejected BKDT docs. | `GetBKDTRejectedDocAsync()` | `[backdate].[jsGetRejectedDocuments]` |
| `GET /GetBKDTFullDetails?userId=...&company=...&month=...` | Merges pending, approved, and rejected docs. | `GetBKDTFullDetailsAsync()` | Calls all three list SPs |
| `GET /GetBKDTDocumentDetail?documentId=...` | Reads request detail by document ID. | `GetBKDTDocumentDetailAsync()` | `[backdate].[jsGetDocumentDetail]` |
| `GET /GetBKDTDocumentDetailUsingFlowId?flowId=...` | Reads request detail by flow ID. | `GetBKDTDocumentDetailBasedOnFlowIdAsync()` | `[backdate].[jsGetDocumentDetailUsingFlowId]` |
| `GET /GetBackDateApprovalFlow?flowId=...` | Reads approval flow. | `GetBackDateApprovalFlowAsync()` | `[backdate].[jsGetBackDateApprovalFlow]` |
| `GET /GetUserDocumentInsights?createdBy=...&month=...` | Creator-level insight counts. | `GetUserDocumentInsightsAsync()` | `[backdate].[jsGetUserDocumentInsights]` |
| `GET /GetUserDocumentsByCreatedByAndMonth?createdBy=...&monthYear=...&status=...&company=...` | Creator document history by month/status. | `GetUserDocumentsByCreatedByAndMonthAsync()` | `[backdate].[jsGetUserDocumentsByCreatedByAndMonth]` |
| `GET /GetFlowStatus?flowId=...` | Reads workflow status. | `GetFlowStatusAsync()` | `[backdate].[jsGetFlowStatus]` |

### Approval APIs

| API | Purpose | Runs | Service method | Backend calls |
|---|---|---|---|---|
| `POST /ApproveBKDT` | Approves workflow stage; on final status `A`, applies HANA `OPEN_BKDT`. | During/after approval | `ApproveDocumentAsync()`, then `BackDateSaveInHana()` | `[backdate].[jsApproveDocument]`, `[backdate].[jsBackdateNotify]`, `[backdate].[jsGetFlowStatus]`, HANA `OPEN_BKDT` |
| `POST /RejectBKDT` | Rejects workflow stage. | During approval | `RejectDocumentAsync()` | `[backdate].[jsRejectDocument]` |

Approval payload:

```json
{
  "flowId": 5001,
  "company": 1,
  "userId": 45,
  "remarks": "Approved for month-end correction"
}
```

Reject payload:

```json
{
  "flowId": 5001,
  "company": 1,
  "userId": 45,
  "remarks": "Period already closed"
}
```

### HANA Status / Notification APIs

| API | Purpose | Service method | Stored procedure |
|---|---|---|---|
| `POST /UpdateHanaStatus` | Manually updates HANA status flag/text. | `UpdateHanaStatusAsync()` | `[backdate].[updateHanaStatus]` |
| `GET /GetBkdtUserIdsSendNotificatios?flowId=...` | Reads users to notify after approval. | `GetBkdtUserIdsSendNotificatiosAsync()` | `[backdate].[jsBackdateNotify]` |
| `GET /SendPendingBkdtCountNotification` | Sends pending-count notification to active users. | `SendPendingBkdtCountNotificationAsync()` | Uses `[backdate].[jsGetUserDocumentInsights]` |
| `GET /GetBKDTCurrentUsersSendNotification?userDocumentId=...` | Reads current-stage users after create. | `GetBKDTCurrentUsersSendNotificationAsync()` | `[backdate].[GetUsersInCurrentStage]` |

## 6. Approval Flow

Approval begins when `[backdate].[jsCreateDocument]` creates a request and workflow rows. The C# code does not access approval tables directly; the `[backdate]` stored procedures own the workflow.

Workflow diagram:

```text
Create BackDate Request
        |
        v
[backdate].[jsCreateDocument]
        |
        v
Pending First Approver
        |
        +--> Reject --> [backdate].[jsRejectDocument] --> Rejected
        |
        v
Approve Stage --> [backdate].[jsApproveDocument]
        |
        v
Notify Next Approver --> [backdate].[jsBackdateNotify]
        |
        v
Check Flow Status --> [backdate].[jsGetFlowStatus]
        |
        +--> Status != A --> HANA not triggered
        |
        v
Status A
        |
        v
BackDateSaveInHana
        |
        v
HANA OPEN_BKDT
        |
        v
[backdate].[updateHanaStatus]
```

Approval flow fields from `BKDTApprovalFlow`:

| Field | Meaning |
|---|---|
| `stageId` | Stage ID. |
| `stageName` | Stage name. |
| `priority` | Stage order. |
| `assignedTo` | Approver/role. |
| `actionStatus` | Action status. |
| `actionDate` | Action date/time. |
| `description` | Remarks/description. |
| `approvalRequired` | Required approval count. |
| `rejectRequired` | Required rejection count. |

Known statuses:

| Status | Where seen | Meaning |
|---|---|---|
| `Pending` | `GetBKDTFullDetailsAsync()` label | Waiting for approval. |
| `Approved` | `GetBKDTFullDetailsAsync()` label | Approved list label. |
| `Rejected` | `GetBKDTFullDetailsAsync()` label | Rejected list label. |
| `A` | `FlowStatus.Status` | Final approved status that triggers HANA `OPEN_BKDT`. |

Return flow: no dedicated "return to creator" API exists for BKDT in `ItemMasterController.cs`. Negative flow is rejection through `RejectBKDT`.

## 7. SAP/HANA Integration

BKDT uses SAP HANA procedures, not SAP Service Layer PATCH/POST.

### HANA Master Data

| Method | API | HANA procedure |
|---|---|---|
| `GetUserDetailsAsync(company)` | `GET /GetUserDetails` | `GETUSERDETAILS()` |
| `GetMobjDetailsAsync(company)` | `GET /GetMobjDetails` | `GETMOBJDETAILS()` |

These use `GetLiveHanaSettings(company)`:

| Company | Connection string | Schema |
|---|---|---|
| 1 | `LiveHanaConnection` | `JIVO_OIL_HANADB` |
| 2 | `LiveBevHanaConnection` | `JIVO_BEVERAGES_HANADB` |
| 3 | `LiveMartHanaConnection` | `JIVO_MART_HANADB` |

### HANA BackDate Update

Main update path:

```text
ApproveBKDT
    |
    v
FlowStatus == "A"
    |
    v
BackDateSaveInHana(flowId)
    |
    v
[backdate].[jsGetDocumentDetailUsingFlowId]
    |
    v
Build BKDTModel
    |
    v
SaveBKDTAsync(BKDTModel)
    |
    v
CALL "{schema}"."OPEN_BKDT"(?,?,?,?,?,?,?,?,?,?,?)
```

`OPEN_BKDT` parameters:

| HANA parameter | Source |
|---|---|
| `branch` | Branch code mapped to `OIL`, `BEVERAGES`, or `MART`. |
| `userId` | `BKDTModel.UserId` / SQL detail `username`. |
| `transType` | Parsed document type/object type. |
| `fromDate` | Parsed from `fromDate`, sent as date. |
| `toDate` | Parsed from `toDate`, sent as date. |
| `timeLimit` | Parsed optional time limit, sent as timestamp or null. |
| `rights` | Controller sets `NO` in approved flow. Direct `SaveBKDT` can pass request value. |
| `createdBy` | SQL detail `createdBy`. |
| `createdOn` | Parsed created date or null. |
| `deletedBy` | Direct model value or null. |
| `deletedOn` | Direct model value or null. |

Date handling:

| Place | Accepted/expected format |
|---|---|
| `BackDateSaveInHana.TryParseDate()` | Tries common formats: `MM/dd/yyyy`, `yyyy-MM-dd`, `dd/MM/yyyy`, `dd-MM-yyyy`, with optional time. |
| `SaveBKDTAsync()` | Expects `BKDTModel.FromDate` and `ToDate` in `dd-MM-yyyy`. |
| HANA parameter | Sends parsed `DateTime.Date`. |

Failure behavior:

| Failure | Behavior |
|---|---|
| No detail for flow | `BackDateSaveInHana` returns 404. |
| Invalid from/to date | Returns 400 with invalid date message. |
| Invalid document type | Returns 400 with invalid document type message. |
| `OPEN_BKDT` fails | `SaveBKDTAsync()` returns `Success=false`; controller returns 400. |
| HANA succeeds but SQL status update fails | Returns 400: `BKDT saved but failed to update Hana status.` |

Retry flow:

| Retry API | When to use |
|---|---|
| `POST /api/ItemMaster/BackDateSaveInHana?flowId=...` | Retry HANA `OPEN_BKDT` for an already approved flow. |
| `POST /api/ItemMaster/SaveBKDT` | Direct/manual HANA procedure execution, use carefully. |
| `POST /api/ItemMaster/UpdateHanaStatus` | Manual HANA status correction. |

## 8. Database Flow

SQL Server stored procedures used by BKDT:

| Stored procedure | Usage |
|---|---|
| `[backdate].[jsCreateDocument]` | Creates request and workflow; outputs `@newDocumentId`. |
| `[backdate].[jsGetDocumentInsight]` | Gets pending/approved/rejected counts by user/company/month. |
| `[backdate].[jsGetPendingDocuments]` | Lists pending documents. |
| `[backdate].[jsGetApprovedDocuments]` | Lists approved documents. |
| `[backdate].[jsGetRejectedDocuments]` | Lists rejected documents. |
| `[backdate].[jsGetDocumentDetail]` | Gets request detail by document ID. |
| `[backdate].[jsGetDocumentDetailUsingFlowId]` | Gets request detail by flow ID. |
| `[backdate].[jsApproveDocument]` | Approves current workflow stage. |
| `[backdate].[jsRejectDocument]` | Rejects workflow stage. |
| `[backdate].[jsGetBackDateApprovalFlow]` | Reads approval flow stages/history. |
| `[backdate].[jsGetUserDocumentInsights]` | Reads creator/user insights by month. |
| `[backdate].[jsGetUserDocumentsByCreatedByAndMonth]` | Reads creator document history by month/status. |
| `[backdate].[jsGetFlowStatus]` | Reads current workflow status; `A` triggers HANA. |
| `[backdate].[updateHanaStatus]` | Stores HANA status flag and status text. |
| `[backdate].[jsBackdateNotify]` | Gets users to notify after approval. |
| `[backdate].[GetUsersInCurrentStage]` | Gets current-stage users after create. |
| `[backdate].[jsGetId]` | Maps `flowId` to document ID for notifications. |

Exact table names are not referenced in C#. Request, approval, HANA status, audit, and history table details are hidden inside the `[backdate]` stored procedures in SQL Server.

Insert sequence:

```text
CreateDocumentRequest
        |
        v
[backdate].[jsCreateDocument]
        |
        +--> request data
        +--> approval workflow
        +--> @newDocumentId
        |
        v
[backdate].[GetUsersInCurrentStage]
        |
        v
FCM notification + notification insert
```

Approval/update sequence:

```text
ApproveRequestModel
        |
        v
[backdate].[jsApproveDocument]
        |
        v
[backdate].[jsBackdateNotify]
        |
        v
[backdate].[jsGetFlowStatus]
        |
        v
if Status == A:
    [backdate].[jsGetDocumentDetailUsingFlowId]
        |
        v
    HANA OPEN_BKDT
        |
        v
    [backdate].[updateHanaStatus]
```

## 9. Audit + History Tracking

Audit and history are mainly stored in SQL Server through `[backdate]` procedures and in HANA through `OPEN_BKDT`.

Tracked fields visible in C# models:

| Field | Model | Meaning |
|---|---|---|
| `fromDate` | `BKDTGetDocumentsModels`, `BKDTDocumentDetailModels`, `CreateDocumentRequest` | Start of allowed backdate range. |
| `toDate` | Same | End of allowed backdate range. |
| `timeLimit` | Same | Time until which access is valid. |
| `createdById` / `CreatedBy` | Multiple models | User who created request. |
| `createdOn` | BKDT models | Request creation time. |
| `createdByUser`, `createdByUserId` | `BKDTDocumentDetailModels` | Creator identity returned in detail APIs. |
| `flowId` | List/detail models | Workflow ID for approval trail. |
| `stageId`, `stageName`, `assignedTo`, `actionStatus`, `actionDate`, `description` | `BKDTApprovalFlow` | Approval history. |
| `HanaStatus` | `BKDTGetDocumentsModels` | HANA update status text/status as returned by SQL. |
| `hanastatusText` | `UpdateHanaStatusRequest` | Text stored by `[backdate].[updateHanaStatus]`. |

Old date vs new date:

This implementation does not patch an individual SAP document's posting date. It grants or records backdate rights/ranges via `OPEN_BKDT`. Therefore the module tracks requested date range (`fromDate`, `toDate`, `timeLimit`) rather than a specific old posting date and new posting date for one `DocEntry`.

Financial audit history APIs:

| API | Purpose |
|---|---|
| `GET /GetBackDateApprovalFlow` | Shows who approved/rejected and when. |
| `GET /GetBKDTDocumentDetail` | Shows request details by document ID. |
| `GET /GetBKDTDocumentDetailUsingFlowId` | Shows request details by workflow flow ID. |
| `GET /GetUserDocumentsByCreatedByAndMonth` | Shows creator history by month/status. |
| `GET /GetUserDocumentInsights` | Shows creator counts. |
| `GET /GetBKDTFullDetails` | Shows merged pending/approved/rejected list. |

Audit table names are not visible in C#. Inspect `[backdate]` stored procedures for exact request, approval, history, and status tables.

## 10. Frontend Flow

No dedicated BKDT Razor page or JavaScript file was found in this repository. The module appears to be consumed by an external frontend/mobile app.

Expected frontend flow:

```text
Open BackDate screen
        |
        v
GET /api/ItemMaster/GetUserDetails?company=1
GET /api/ItemMaster/GetMobjDetails?company=1
        |
        v
User selects branch/user/document type/action/date range
        |
        v
POST /api/ItemMaster/CreateDocument
        |
        v
Approver list refresh:
GET /api/ItemMaster/GetBKDTPendingDoc?userId=...&company=...&month=...
        |
        v
Approver opens details:
GET /api/ItemMaster/GetBKDTDocumentDetailUsingFlowId?flowId=...
        |
        v
POST /api/ItemMaster/ApproveBKDT or RejectBKDT
        |
        v
UI shows FlowStatus, BackDateResult, HanaStatusText
```

Transform logic in controller:

| Field | Mapping |
|---|---|
| Branch `1` | `OIL` |
| Branch `2` | `BEVERAGES` |
| Branch `3` | `MART` |
| Action `A` | `ADD` |
| Action `U` | `UPDATE` |
| Document type number | Mapped to name using `GETMOBJDETAILS()` result. |

## 11. Security

Security setup:

| Area | File | Behavior |
|---|---|---|
| JWT auth | `Program.cs` | JWT bearer authentication configured. |
| Session | `Program.cs` | Session enabled. |
| Middleware | `Program.cs` | `UseAuthentication()` and `UseAuthorization()` enabled. |
| Permission filter | `Filters/CheckUserPermissionAttribute.cs` | Reads session `userId`, `companyId`, and checks permissions via `IPermissionService`. |
| BKDT endpoints | `Controllers/ItemMasterController.cs` | Some `[CheckUserPermission]` comments exist but are commented out. Many BKDT endpoints have no active authorization attribute. |

Company validation:

| Area | Validation |
|---|---|
| `GetLiveHanaSettings(company)` | Only supports companies `1`, `2`, `3`. |
| `SaveBKDTAsync()` | Parses branch list and rejects invalid branch codes. |

Approval ownership validation:

C# passes `flowId`, `Company`, `UserId`, and `remarks` to `[backdate].[jsApproveDocument]` / `[backdate].[jsRejectDocument]`. Whether the user is the correct approver must be enforced inside SQL stored procedures.

Financial-role validation:

No explicit finance-role validation is visible in controller/service code. If finance-only approval is required, verify the `[backdate]` approval stored procedures and permission setup.

Duplicate request prevention:

No explicit duplicate-request lock or duplicate-date-range check is visible in C#. Any duplicate prevention must exist in `[backdate].[jsCreateDocument]` or HANA `OPEN_BKDT`.

## 12. Debugging Guide

### Request Not Saving

Check:

| Step | Where |
|---|---|
| API | `POST /api/ItemMaster/CreateDocument` |
| Controller | `ItemMasterController.CreateDocument()` |
| Service | `ItemMasterService.CreateDocumentAsync()` |
| Stored procedure | `[backdate].[jsCreateDocument]` |
| Output | `@newDocumentId` must be greater than 0. |
| Notifications | `[backdate].[GetUsersInCurrentStage]`, FCM token lookup, notification insert. |

Common causes:

| Symptom | Likely cause |
|---|---|
| `Invalid request` | Empty/malformed JSON. |
| `Document creation failed or missing newDocumentId` | SP did not output document ID. |
| Created but no notification | `[backdate].[GetUsersInCurrentStage]` returned no users or users have no FCM token. |

### Approval Stuck

Check:

| Step | Where |
|---|---|
| Pending list | `GET /GetBKDTPendingDoc` |
| Approval flow | `GET /GetBackDateApprovalFlow?flowId=...` |
| Approve SP | `[backdate].[jsApproveDocument]` |
| Flow status | `GET /GetFlowStatus?flowId=...` |
| Notify SP | `[backdate].[jsBackdateNotify]` |

If `FlowStatus.Status` is not `A`, HANA update will not run.

### SAP/HANA Update Failed

Check:

| Step | Where |
|---|---|
| Retry API | `POST /api/ItemMaster/BackDateSaveInHana?flowId=...` |
| Detail SP | `[backdate].[jsGetDocumentDetailUsingFlowId]` |
| Date parsing | `ItemMasterController.TryParseDate()` |
| HANA method | `ItemMasterService.SaveBKDTAsync()` |
| HANA procedure | `OPEN_BKDT` |
| Status SP | `[backdate].[updateHanaStatus]` |

Common causes:

| Symptom | Likely cause |
|---|---|
| `No BKDT document details found` | Wrong `flowId` or detail SP issue. |
| `Invalid fromDate format` / `Invalid toDate format` | SQL returned date in unsupported format. |
| `Invalid documentType format` | Document type was transformed to text before HANA save or bad SQL value. |
| `Invalid branch code` | Branch is not `1`, `2`, `3`, or valid comma-separated list. |
| `OPEN_BKDT` error | HANA procedure validation, period/user/object issue, or connectivity issue. |

### Wrong Document/Object Updated

This module applies backdate rights by object type/user/branch/date range, not by a specific `DocEntry`.

Check:

| Area | What to verify |
|---|---|
| `documentType` | Must be numeric object type before `BackDateSaveInHana` parses it. |
| `GETMOBJDETAILS()` | Used for display mapping only; do not pass display name into `OPEN_BKDT`. |
| `username` | Becomes HANA `userId`. |
| `branch` | Controls which HANA schema receives `OPEN_BKDT`. |
| `action` | Display mapping is `A=ADD`, `U=UPDATE`; HANA `OPEN_BKDT` does not receive action in current code. |

### Date Not Updating

Check:

| Step | Where |
|---|---|
| Final status | `[backdate].[jsGetFlowStatus]` returns `A`. |
| Detail dates | `[backdate].[jsGetDocumentDetailUsingFlowId]` returns `fromDate`, `toDate`, `timeLimit`. |
| Date parsing | `TryParseDate()` in controller. |
| HANA call | `SaveBKDTAsync()` expects `dd-MM-yyyy` after controller conversion. |
| Status | `[backdate].[updateHanaStatus]` stores status/text. |

### Notifications Not Sent

Check:

| Step | Where |
|---|---|
| After create users | `[backdate].[GetUsersInCurrentStage]` |
| After approval users | `[backdate].[jsBackdateNotify]` |
| Token lookup | `INotificationService.GetUserFcmTokenAsync(userId)` |
| Push send | `INotificationService.SendPushNotificationAsync(...)` |
| Notification insert | `INotificationService.InsertNotificationAsync(...)` |
| Pending count | `GET /SendPendingBkdtCountNotification` |

### Financial Period Validation Fails

No explicit period validation method is visible in C# code. Validation is likely inside HANA `OPEN_BKDT` and/or `[backdate]` SQL procedures.

Check:

| Area | What to inspect |
|---|---|
| HANA | `OPEN_BKDT` procedure logic and any period tables it checks. |
| SQL | `[backdate].[jsCreateDocument]` validation for requested date range. |
| Dates | `fromDate`, `toDate`, `timeLimit` values. |
| User/object | `username`, `documentType`, `branch`. |

## 13. Request / Response Examples

### Create Request

```http
POST /api/ItemMaster/CreateDocument
Content-Type: application/json
```

```json
{
  "branch": "1",
  "username": "manager01",
  "documentType": "13",
  "fromDate": "2026-05-01T00:00:00",
  "toDate": "2026-05-05T00:00:00",
  "timeLimit": "2026-05-09T18:00:00",
  "action": "A",
  "companyId": 1,
  "createdBy": 101
}
```

Response:

```json
{
  "newDocumentId": 250,
  "success": true,
  "message": "Document created successfully and notifications sent once per token."
}
```

### Approve Request

```http
POST /api/ItemMaster/ApproveBKDT
Content-Type: application/json
```

```json
{
  "flowId": 5001,
  "company": 1,
  "userId": 45,
  "remarks": "Approved"
}
```

Intermediate approval response:

```json
{
  "success": true,
  "flowId": 5001,
  "message": "BKDT document processed.",
  "approvalResult": {
    "message": "Approved Document of FlowId 5001",
    "success": true
  },
  "flowStatus": {
    "status": "P"
  },
  "backDateResult": null,
  "hanaStatusText": "HANA not triggered"
}
```

Final approval/HANA success response:

```json
{
  "success": true,
  "flowId": 5001,
  "message": "BKDT document processed.",
  "approvalResult": {
    "message": "Approved Document of FlowId 5001",
    "success": true
  },
  "flowStatus": {
    "status": "A"
  },
  "backDateResult": {
    "success": true,
    "flowId": 5001,
    "message": "BKDT saved and Hana status updated successfully.",
    "hanaStatusText": "BKDT executed successfully for: OIL (schema: JIVO_OIL_HANADB)."
  },
  "hanaStatusText": "HANA not triggered"
}
```

Note: in current controller code, top-level `hanaStatusText` remains `"HANA not triggered"` even when `BackDateResult` contains a successful HANA status. Read `BackDateResult.HanaStatusText` for the actual HANA result.

### Reject Request

```http
POST /api/ItemMaster/RejectBKDT
Content-Type: application/json
```

```json
{
  "flowId": 5001,
  "company": 1,
  "userId": 45,
  "remarks": "Rejected because financial period is closed"
}
```

Response:

```json
{
  "message": "No message returned from procedure.",
  "success": true
}
```

### Pending Request Response

```http
GET /api/ItemMaster/GetBKDTPendingDoc?userId=45&company=1&month=05-2026
```

```json
{
  "success": true,
  "data": [
    {
      "id": 250,
      "companyId": 1,
      "documentType": "A/R Invoice",
      "branch": "OIL",
      "action": "ADD",
      "username": "manager01",
      "fromDate": "2026-05-01",
      "toDate": "2026-05-05",
      "timeLimit": "2026-05-09T18:00:00",
      "createdById": 101,
      "createdBy": "Creator Name",
      "createdOn": "2026-05-09T10:30:00",
      "flowId": 5001,
      "status": "Pending",
      "hanaStatus": null
    }
  ]
}
```

### Direct HANA Save Response

```http
POST /api/ItemMaster/SaveBKDT
Content-Type: application/json
```

```json
{
  "branch": "1",
  "userId": "manager01",
  "transType": 13,
  "fromDate": "01-05-2026",
  "toDate": "05-05-2026",
  "timeLimit": "2026-05-09T18:00:00",
  "rights": "NO",
  "createdBy": "101",
  "createdOn": "2026-05-09T10:30:00",
  "deletedBy": null,
  "deletedOn": "0001-01-01T00:00:00"
}
```

```json
{
  "message": "BKDT executed successfully for: OIL (schema: JIVO_OIL_HANADB).",
  "success": true
}
```

## 14. Important Notes

| Area | Note |
|---|---|
| Actual location | BKDT is inside `ItemMasterController` / `ItemMasterService`; there is no separate BackDate controller/service. |
| Financial risk | Backdate rights can affect closed periods, tax periods, and audit trails. |
| Authorization gap | BKDT endpoints do not have active `[Authorize]` or `[CheckUserPermission]` attributes in the controller. |
| Final trigger | HANA update runs only when `[backdate].[jsGetFlowStatus]` returns `A`. |
| Top-level HANA text bug | `ApproveBKDT` leaves top-level `HanaStatusText` as `"HANA not triggered"` even after successful `BackDateSaveInHana`; check nested `BackDateResult`. |
| Direct HANA endpoint | `POST /SaveBKDT` can execute `OPEN_BKDT` without approval; protect or remove if not intended. |
| Date format risk | `SaveBKDTAsync()` requires `dd-MM-yyyy`; controller converts dates before calling it. |
| Document type risk | HANA needs numeric object type. Display-transformed document names should not be passed into `BackDateSaveInHana`. |
| Branch risk | Branch can be comma-separated; invalid branch fails. Wrong branch applies rights in the wrong company database. |
| Action field | Request has `Action`, UI maps `A/U`, but current `OPEN_BKDT` call does not pass action. Verify business expectation. |
| Audit tables | Table names are hidden behind `[backdate]` procedures; inspect SQL for exact audit/history storage. |
| Duplicate prevention | No C# duplicate guard exists for overlapping branch/user/document/date requests. |
| Notifications | Notification insert uses `pageId = 5` on create and `pageId = 6` on approval; verify frontend routing. |

