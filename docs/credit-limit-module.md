# Credit Limit Module Developer Documentation

## 1. Module Overview

The Credit Limit module is used to create, approve, reject, and finally apply credit limit changes for SAP Business Partners.

In this codebase, a credit limit request is first stored in SQL Server and routed through an approval workflow. After approval reaches the final approved status, the backend updates SAP Business One Business Partner credit fields through the SAP Service Layer.

Business problem solved:

| Problem | How the module solves it |
|---|---|
| Sales/finance users need to increase or change customer credit limits. | User creates a credit limit request instead of directly editing SAP. |
| Credit limit changes are financially risky. | Request goes through approval stages before SAP is updated. |
| Approvers need pending notifications. | Module sends FCM push notifications and inserts notification records. |
| Developers need traceability. | SQL status, approval flow, HANA/SAP status text, and document details are available by API. |

Users/departments:

| User/department | Role |
|---|---|
| Sales / branch users | Create credit limit requests. |
| Finance / accounts | Review financial risk and approve/reject. |
| Management | Higher-level approval, depending on workflow setup in SQL. |
| ERP / IT team | Debug SQL, SAP Service Layer, HANA, and notification issues. |

After approval, `CreditLimitService.UpdateCreditLimitAsync()` patches SAP Business Partner fields:

```json
{
  "MaxCommitment": "<newCreditLimit>",
  "CreditLimit": "<newCreditLimit>"
}
```

The SAP BP selected is `BusinessPartners('{CustomerCode}')`, using the branch/company session selected from `BranchId`.

## 2. Module Architecture

Main files:

| Layer | File | Responsibility |
|---|---|---|
| API controller | `Controllers/CreditLimitController.cs` | Exposes `/api/CreditLimit/*` APIs. |
| Service interface | `Services/Interfaces/ICreditLimitService.cs` | Defines Credit Limit service contract. |
| Service implementation | `Services/Implementation/CreditLimitService.cs` | Handles SQL stored procedures, HANA reads, SAP patch, attachments, and notifications. |
| DTOs/models | `Models/CreditLimitModels.cs` | Request/response models for create, approve, reject, detail, attachment, status, and insight APIs. |
| SAP session provider | `Services/Interfaces/IBom2Service.cs`, `Services/Implementation/Bom2Service.cs` | Provides SAP Service Layer sessions for Oil, Beverages, MART. |
| Notifications | `Services/Interfaces/INotificationService.cs`, `Services/Implementation/NotificationService.cs` | Sends FCM push notifications and stores notification rows. |
| User service | `Services/Interfaces/IUserService.cs`, `Services/Implementation/UserService.cs` | Used for active users in pending-count notifications. |
| Security filter | `Filters/CheckUserPermissionAttribute.cs` | Session permission filter exists, but Credit Limit endpoints do not use it. |
| App setup | `Program.cs` | Registers `ICreditLimitService`, JWT auth, session, CORS, controllers. |

Frontend files found:

| File | Credit Limit usage |
|---|---|
| `Views/BPmasterweb/Index.cshtml` | Has a BP master credit limit field, not the Credit Limit approval module. |
| `Views/BPmasterweb/Index1.cshtml` | Has a BP master credit limit field, not the Credit Limit approval module. |
| `wwwroot/js/bp-creation.js` | Logs BP master credit limit changes, not Credit Limit approval API flow. |

No dedicated Credit Limit Razor page or dedicated Credit Limit JavaScript file was found in this repository. The module appears to be API-first and likely consumed by an external frontend/mobile app.

Architecture diagram:

```text
Frontend / Mobile / API Client
        |
        | /api/CreditLimit/*
        v
Controllers/CreditLimitController.cs
        |
        | ICreditLimitService
        v
Services/Implementation/CreditLimitService.cs
        |
        +--> SQL Server [cl] stored procedures
        |       - create request
        |       - approval workflow
        |       - document lists/details
        |       - HANA/SAP status text
        |       - attachments
        |
        +--> SAP HANA procedures
        |       - GetCustomerCards
        |       - OPENCSLM
        |
        +--> SAP Service Layer
        |       - PATCH BusinessPartners('{CustomerCode}')
        |
        +--> NotificationService
                - FCM push
                - notification insert
```

## 3. Complete Workflow

1. User opens Credit Limit screen in the frontend/mobile app.
2. Frontend loads customers using `GET /api/CreditLimit/GetCustomerCards?company=...`.
3. User selects customer/business partner and enters current balance, current limit, new limit, valid-till date, branch, company, and creator.
4. Frontend creates the request:
   - old JSON API: `POST /api/CreditLimit/CreateCLDocument`
   - newer attachment API: `POST /api/CreditLimit/CreateCLDocumentV2`
5. Controller validates that request data exists. For V2, if `TotalEntries == 1`, attachment is mandatory.
6. Service calls `[cl].[jsCreateDocument]`.
7. SQL Server creates the credit document and workflow, then returns `@newDocumentId`.
8. V2 saves attachment metadata using `[cl].[jsInsertCreditDocumentAttachment]` and stores the physical file under `wwwroot/Uploads/CreditLimit`.
9. Service calls `[cl].[GetUsersInCurrentStage]` to identify current approvers.
10. Service sends FCM push notifications and inserts notification records.
11. Approver loads pending documents using `POST /api/CreditLimit/GetPendingDocuments`.
12. Approver approves with `POST /api/CreditLimit/ApproveDocument` or rejects with `POST /api/CreditLimit/RejectDocument`.
13. Approval service calls `[cl].[jsApproveDocument]`.
14. Controller then calls `UpdateCreditLimitAsync(flowId)`.
15. `UpdateCreditLimitAsync()` checks `[cl].[jsGetFlowStatus]`.
16. Only if status is `A`, it reads document details using `[cl].[jsGetDocumentDetailUsingFlowId]`.
17. Service selects SAP session by `BranchId`.
18. Service sends `PATCH BusinessPartners('{CustomerCode}')` to SAP Service Layer.
19. Result is saved to SQL using `[cl].[updateHanaStatus]`.
20. API returns approval success plus `HanaStatusText`.

## 4. Before Approval vs After Approval

### Before Approval

| Area | Behavior |
|---|---|
| Create APIs | `POST /CreateCLDocument`, `POST /CreateCLDocumentV2` |
| Optional direct HANA API | `POST /OpenCslm` calls HANA `OPENCSLM`; this bypasses the SQL approval document flow and should be treated separately. |
| SQL procedures | `[cl].[jsCreateDocument]`, `[cl].[jsInsertCreditDocumentAttachment]` |
| Tables | Not directly referenced in C#; table writes are hidden inside `[cl]` stored procedures. |
| Approval flow | Created by `[cl].[jsCreateDocument]`. |
| SAP Service Layer called? | No for normal create flow. |
| HANA called? | Yes only for customer dropdown/name lookup, and for `OpenCslm`. |
| Notifications sent? | Yes, to users returned by `[cl].[GetUsersInCurrentStage]`. |
| Credit limit active in SAP? | No. The BP credit limit is not changed yet. |
| Statuses visible in APIs | Pending documents are returned by `[cl].[jsGetPendingDocuments]`; `GetAllDocumentsAsync()` labels them as `Pending`. |

### After Approval

| Area | Behavior |
|---|---|
| Approval API | `POST /api/CreditLimit/ApproveDocument` |
| SQL approval method | `CreditLimitService.ApproveDocumentAsync()` |
| SQL approval procedure | `[cl].[jsApproveDocument]` |
| Notification procedure | `[cl].[jsCreditLimitNotify]` |
| Final-status check | `[cl].[jsGetFlowStatus]` must return `status = "A"` for SAP update. |
| SAP update method | `CreditLimitService.UpdateCreditLimitAsync()` |
| SAP endpoint | `PATCH {SapServiceLayer:BaseUrl}/BusinessPartners('{CustomerCode}')` |
| SAP payload fields | `MaxCommitment`, `CreditLimit` |
| SAP/HANA status procedure | `[cl].[updateHanaStatus]` |
| Is SAP BP updated? | Yes only when final flow status is `A` and SAP PATCH succeeds. |
| Notifications sent? | Yes, approval sends notifications to next-stage users returned by `[cl].[jsCreditLimitNotify]`. |

Important behavior: unlike the IMC final approval path, Credit Limit approval first updates the SQL workflow and then attempts SAP update. If SAP update fails, the SQL approval has already happened; the failure is stored in HANA status text via `[cl].[updateHanaStatus]`.

## 5. API Documentation

Base route: `/api/CreditLimit`

### Create APIs

| API | Purpose | Runs | Service method | Backend calls |
|---|---|---|---|---|
| `POST /CreateCLDocument` | Creates a credit limit approval document without attachment upload. | Before approval | `CreateDocumentAsync()` | `[cl].[jsCreateDocument]`, `[cl].[GetUsersInCurrentStage]`, notifications |
| `POST /CreateCLDocumentV2` | Creates document using multipart form data and optional attachment. Attachment is required when `TotalEntries == 1`. | Before approval | `CreateDocumentWithAttachmentAsyncV2()` | `[cl].[jsCreateDocument]`, `[cl].[jsInsertCreditDocumentAttachment]`, `[cl].[GetUsersInCurrentStage]`, notifications |
| `POST /OpenCslm` | Calls HANA `OPENCSLM` directly. This is not the normal SQL approval document flow. | Direct HANA create/update | `OpenCslmAsync()` | HANA `OPENCSLM(?,?,?,?,?,?,?)` |

`CreateCLDocument` JSON payload:

```json
{
  "branchId": "1",
  "customerCode": "C000123",
  "customerValue": "ABC Traders",
  "currentBalance": 250000.0,
  "currentCreditLimit": 500000.0,
  "newCreditLimit": 750000.0,
  "validTill": "2026-06-30",
  "companyId": 1,
  "createdBy": 101
}
```

`CreateCLDocumentV2` form-data payload:

| Form field | Type | Notes |
|---|---|---|
| `documentData` | string JSON | Serialized `CreateDocumentDtoV2`. |
| `attachment` | file | Required when `TotalEntries == 1`. |

### Read/List APIs

| API | Purpose | Service method | Stored procedure |
|---|---|---|---|
| `GET /GetCustomerCards?company=...` | Reads customers/BPs from HANA. | `GetCustomerCardsAsync()` | HANA `GetCustomerCards()` |
| `POST /GetPendingDocuments` | Reads pending credit documents for user/company/month. | `GetPendingDocumentsAsync()` | `[cl].[jsGetPendingDocuments]` |
| `POST /GetApprovedDocuments` | Reads approved documents. | `GetApprovedDocumentsAsync()` | `[cl].[jsGetApprovedDocuments]` |
| `POST /GetRejectedDocuments` | Reads rejected documents. | `GetRejectedDocumentsAsync()` | `[cl].[jsGetRejectedDocuments]` |
| `POST /GetAllDocuments` | Combines pending, approved, rejected documents and labels status. | `GetAllDocumentsAsync()` | Calls all three list procedures |
| `POST /GetCreditDocumentInsight` | Reads pending/approved/rejected counts. | `GetCreditDocumentInsightAsync()` | `[cl].[jsGetCreditDocumentInsight]` |
| `POST /GetUserDocumentInsights` | Reads creator insights by month. | `GetUserDocumentInsightsAsync()` | `[cl].[jsGetUserDocumentInsights]` |
| `GET /GetDocumentDetail?documentId=...` | Reads document detail. | `GetDocumentDetailAsync()` | `[cl].[jsGetDocumentDetail]` |
| `GET /GetDocumentDetailV2?documentId=...` | Reads document detail plus attachments and download URLs. | `GetCreditDocumentDetailAsyncV2()` | `[cl].[jsGetDocumentDetail]` |
| `GET /GetDocumentDetailUsingFlowId?flowId=...` | Reads document detail by workflow flow ID. | `GetDocumentDetailUsingFlowIdAsync()` | `[cl].[jsGetDocumentDetailUsingFlowId]` |
| `GET /GetApprovalFlow?flowId=...` | Reads approval flow stages. | `GetApprovalFlowAsync()` | `[cl].[jsGetCreditLimitApprovalFlow]` |
| `POST /GetUserDocumentsByCreatedByAndMonth` | Reads documents created by a user and filtered by status/month. | `GetUserDocumentsAsync()` | `[cl].[jsGetUserDocumentsByCreatedByAndMonth]` |
| `GET /GetFlowStatus?flowId=...` | Reads workflow status. | `GetFlowStatusAsync()` | `[cl].[jsGetFlowStatus]` |

List request payload:

```json
{
  "userId": 101,
  "companyId": 1,
  "month": "05-2026"
}
```

### Approval APIs

| API | Purpose | Runs | Service method | Backend calls |
|---|---|---|---|---|
| `POST /ApproveDocument` | Approves workflow stage. Controller then tries SAP BP update. | During/after approval | `ApproveDocumentAsync()`, then `UpdateCreditLimitAsync()` | `[cl].[jsApproveDocument]`, `[cl].[jsCreditLimitNotify]`, `[cl].[jsGetFlowStatus]`, SAP PATCH, `[cl].[updateHanaStatus]` |
| `POST /RejectDocument` | Rejects workflow stage. | During approval | `RejectDocumentAsync()` | `[cl].[jsRejectDocument]` |

Approval payload:

```json
{
  "flowId": 10025,
  "company": 1,
  "userId": 45,
  "remarks": "Approved after finance review",
  "action": "Approve"
}
```

Reject payload:

```json
{
  "flowId": 10025,
  "company": 1,
  "userId": 45,
  "remarks": "Outstanding balance is too high",
  "action": "Reject"
}
```

### SAP/HANA Status APIs

| API | Purpose | Runs | Service method |
|---|---|---|---|
| `POST /UpdateCreditLimitInHana?flowId=...` | Manual/retry trigger for SAP BP credit update. | After final approval | `UpdateCreditLimitAsync()` |
| `POST /UpdateHanaStatus` | Manually updates SQL HANA/SAP status. | Debug/manual correction | `UpdateHanaStatusAsync()` |

`UpdateHanaStatus` payload:

```json
{
  "flowId": 10025,
  "status": true,
  "hanaStatusText": "Credit Limit updated successfully in HANA for Customer: C000123"
}
```

### Notification APIs

| API | Purpose | Service method | Stored procedure |
|---|---|---|---|
| `GET /GetCLUserIdsSendNotifications?flowId=...` | Reads users to notify for a flow. | `GetCLUserIdsSendNotificatiosAsync()` | `[cl].[jsCreditLimitNotify]` |
| `GET /GetCurrentUsersSendNotification?userDocumentId=...` | Reads users in current approval stage after creation. | `GetCurrentUsersSendNotificationAsync()` | `[cl].[GetUsersInCurrentStage]` |
| `GET /SendPendingCLCountNotification` | Sends pending-count notification to active users. | `SendPendingCLCountNotificationAsync()` | Uses `[cl].[jsGetCreditDocumentInsight]` |

## 6. Approval Flow

Approval starts inside `[cl].[jsCreateDocument]`. The exact workflow tables are not accessed directly from C#; they are controlled by `[cl]` stored procedures.

Approval stages are returned by `GET /api/CreditLimit/GetApprovalFlow?flowId=...`.

Fields returned in `CreditLimitApprovalFlowDto`:

| Field | Meaning |
|---|---|
| `StageId` | Workflow stage ID. |
| `StageName` | Name of approval stage. |
| `Priority` | Stage order. |
| `AssignedTo` | Approver/role assigned to stage. |
| `ActionStatus` | Current stage action status. |
| `ActionDate` | Action time. |
| `Description` | Remarks/description. |
| `ApprovalRequired` | Required approvals count. |
| `RejectRequired` | Required rejections count. |

Workflow diagram:

```text
Create Credit Limit Request
        |
        v
Pending First Approver
        |
        +--> Reject --> [cl].[jsRejectDocument] --> Rejected
        |
        v
Approve Stage --> [cl].[jsApproveDocument]
        |
        v
Notify Next Stage --> [cl].[jsCreditLimitNotify]
        |
        v
Final Approved Status?
        |
        +--> No --> HANA/SAP update skipped with "Flow is not approved from final stage"
        |
        v
Status A
        |
        v
PATCH SAP BusinessPartners('{CustomerCode}')
        |
        +--> Failure --> [cl].[updateHanaStatus] status=false
        |
        v
Success --> [cl].[updateHanaStatus] status=true
```

Known statuses:

| Status | Where seen | Meaning |
|---|---|---|
| `Pending` | `GetAllDocumentsAsync()` label | Pending request. |
| `Approved` | `GetAllDocumentsAsync()` label | Approved request list label. |
| `Rejected` | `GetAllDocumentsAsync()` label | Rejected request list label. |
| `A` | `FlowStatusRequest.status` from `[cl].[jsGetFlowStatus]` | Final approved status required for SAP update. |

Return flow: no dedicated "return to creator" API exists in `CreditLimitController.cs`. The implemented negative path is rejection through `RejectDocument`.

Final approval logic:

1. `ApproveDocument` calls `[cl].[jsApproveDocument]`.
2. Controller calls `UpdateCreditLimitAsync(flowId)`.
3. `UpdateCreditLimitAsync()` calls `[cl].[jsGetFlowStatus]`.
4. SAP update only runs if status is `A`.

## 7. SAP/HANA Integration

### HANA Customer Data

Customer dropdown data is read from live HANA schemas:

| Company/branch | Connection | Schema |
|---|---|---|
| 1 | `ConnectionStrings:LiveHanaConnection` | `JIVO_OIL_HANADB` |
| 2 | `ConnectionStrings:LiveBevHanaConnection` | `JIVO_BEVERAGES_HANADB` |
| 3 | `ConnectionStrings:LiveMartHanaConnection` | `JIVO_MART_HANADB` |

Methods:

| Method | HANA procedure |
|---|---|
| `GetCustomerCardsAsync(company)` | `CALL "{schema}"."GetCustomerCards"()` |
| `GetCustomerNameByCodeAsync(company, customerCode)` | Calls `GetCustomerCards()` and finds matching `CardCode`. |
| `OpenCslmAsync(request)` | `CALL "{schema}"."OPENCSLM"(?,?,?,?,?,?,?)` |

`OpenCslm` parameters:

| Parameter | Source |
|---|---|
| `CardCode` | `OpenCslmRequest.CardCode` |
| `CurrentLimit` | `OpenCslmRequest.CurrentLimit` |
| `NewLimit` | `OpenCslmRequest.NewLimit` |
| `ValidTill` | `OpenCslmRequest.ValidTill` |
| `createdBy` | `OpenCslmRequest.CreatedBy` |
| `Balance` | `OpenCslmRequest.Balance` |
| `result_id` | HANA output parameter |

### SAP Business Partner Update

Main SAP method: `CreditLimitService.UpdateCreditLimitAsync(int flowId)`.

Flow:

```text
flowId
  |
  v
[cl].[jsGetFlowStatus]
  |
  +-- status != A --> update SQL HANA status false and stop
  |
  v
[cl].[jsGetDocumentDetailUsingFlowId]
  |
  v
Pick SAP session by BranchId
  |
  +-- 1 --> GetSAPSessionOilAsync()
  +-- 2 --> GetSAPSessionBevAsync()
  +-- 3 --> GetSAPSessionMartAsync()
  |
  v
PATCH BusinessPartners('{CustomerCode}')
  |
  v
[cl].[updateHanaStatus]
```

SAP payload:

```json
{
  "MaxCommitment": 750000.0,
  "CreditLimit": 750000.0
}
```

Fields mapped:

| SAP field | Source |
|---|---|
| Business Partner key | `DocumentDetailDto.CustomerCode` |
| `MaxCommitment` | `DocumentDetailDto.NewCreditLimit` |
| `CreditLimit` | `DocumentDetailDto.NewCreditLimit` |
| SAP company/session | `DocumentDetailDto.BranchId` |

SAP failure behavior:

| Failure | Behavior |
|---|---|
| Flow not final approved | Writes `Flow is not approved from final stage` through `[cl].[updateHanaStatus]`, status false. |
| Document not found | Writes `Document detail not found.` through `[cl].[updateHanaStatus]`, status false. |
| Unknown branch | Catches exception and writes failure text through `[cl].[updateHanaStatus]`. |
| SAP PATCH not success | Reads SAP response body and writes `HANA PATCH failed: ...` through `[cl].[updateHanaStatus]`, status false. |
| SQL status update failed after SAP success | Returns `HANA updated but SQL status update failed: ...`. |

Retry flow:

| Retry API | When to use |
|---|---|
| `POST /api/CreditLimit/UpdateCreditLimitInHana?flowId=...` | Retry SAP BP update after final approval. |
| `POST /api/CreditLimit/UpdateHanaStatus` | Manual correction/debug status update. |

Important naming note: the code calls this "HANA update", but the final credit update is actually a SAP Service Layer PATCH to `BusinessPartners`. HANA is used for customer-card reads and stored procedures such as `OPENCSLM`.

## 8. Database Flow

SQL Server stored procedures used by Credit Limit:

| Stored procedure | Usage |
|---|---|
| `[cl].[jsCreateDocument]` | Creates credit limit document and approval workflow; outputs `@newDocumentId`. |
| `[cl].[jsInsertCreditDocumentAttachment]` | Stores attachment metadata for V2 create flow. |
| `[cl].[jsGetApprovedDocuments]` | Lists approved documents. |
| `[cl].[jsGetPendingDocuments]` | Lists pending documents. |
| `[cl].[jsGetRejectedDocuments]` | Lists rejected documents. |
| `[cl].[jsGetCreditDocumentInsight]` | Counts pending/approved/rejected documents. |
| `[cl].[jsGetUserDocumentInsights]` | Counts creator documents by month. |
| `[cl].[jsGetDocumentDetail]` | Reads document detail; V2 expects a second result set for attachments. |
| `[cl].[jsGetCreditLimitApprovalFlow]` | Reads approval flow stages/history. |
| `[cl].[jsGetUserDocumentsByCreatedByAndMonth]` | Reads creator documents by month/status. |
| `[cl].[jsGetId]` | Maps `flowId` to document ID for notification text. |
| `[cl].[jsApproveDocument]` | Approves current workflow stage. |
| `[cl].[jsRejectDocument]` | Rejects workflow stage. |
| `[cl].[jsGetDocumentDetailUsingFlowId]` | Reads document detail needed for SAP update. |
| `[cl].[jsGetFlowStatus]` | Reads workflow status; `A` is required before SAP update. |
| `[cl].[updateHanaStatus]` | Stores SAP/HANA update status and status text. |
| `[cl].[jsCreditLimitNotify]` | Gets users to notify after approval. |
| `[cl].[GetUsersInCurrentStage]` | Gets current-stage approvers after creation. |

Table names are not visible in the C# layer. To identify exact request, approval, status, attachment, and audit tables, inspect the `[cl]` stored procedures in the SQL Server database configured by `ConnectionStrings:DefaultConnection`.

Insert/update sequence:

```text
CreateDocumentDto / CreateDocumentDtoV2
        |
        v
[cl].[jsCreateDocument]
        |
        +--> request data
        +--> approval workflow
        +--> output @newDocumentId
        |
        v
V2 only: file saved to wwwroot/Uploads/CreditLimit
        |
        v
V2 only: [cl].[jsInsertCreditDocumentAttachment]
        |
        v
[cl].[GetUsersInCurrentStage]
        |
        v
FCM notification + notification table insert
```

Approval/SAP sequence:

```text
ApproveDocumentRequest
        |
        v
[cl].[jsApproveDocument]
        |
        v
[cl].[jsCreditLimitNotify]
        |
        v
Controller calls UpdateCreditLimitAsync
        |
        v
[cl].[jsGetFlowStatus]
        |
        v
[cl].[jsGetDocumentDetailUsingFlowId]
        |
        v
SAP PATCH BusinessPartners
        |
        v
[cl].[updateHanaStatus]
```

## 9. Frontend Flow

No dedicated Credit Limit approval page/script was found in this repository.

Known related frontend files:

| File | What it contains |
|---|---|
| `Views/BPmasterweb/Index.cshtml` | BP master form has a credit limit input. |
| `Views/BPmasterweb/Index1.cshtml` | BP master form has a credit limit input. |
| `wwwroot/js/bp-creation.js` | Logs credit limit field changes for BP creation. |

Expected frontend/API-client flow:

```text
Open Credit Limit screen
        |
        v
GET /api/CreditLimit/GetCustomerCards?company=1
        |
        v
User selects customer and enters new credit limit
        |
        v
POST /api/CreditLimit/CreateCLDocumentV2
        |
        v
Pending approver sees request via POST /GetPendingDocuments
        |
        v
Approver opens detail via GET /GetDocumentDetailV2
        |
        v
Approver submits POST /ApproveDocument or /RejectDocument
        |
        v
UI reads response HanaStatusText and refreshes pending/approved lists
```

Dropdown APIs:

| UI need | API |
|---|---|
| Customer/BP dropdown | `GET /api/CreditLimit/GetCustomerCards?company=...` |
| Document detail | `GET /api/CreditLimit/GetDocumentDetailV2?documentId=...` |
| Approval flow/history | `GET /api/CreditLimit/GetApprovalFlow?flowId=...` |
| Counts | `POST /api/CreditLimit/GetCreditDocumentInsight` |

## 10. Security

Security setup:

| Area | File | Current behavior |
|---|---|---|
| JWT auth | `Program.cs` | JWT bearer authentication is configured. |
| Session | `Program.cs` | Session is enabled. |
| Middleware | `Program.cs` | `UseAuthentication()` and `UseAuthorization()` are enabled. |
| Permission filter | `Filters/CheckUserPermissionAttribute.cs` | Filter exists and checks session `userId`, `companyId`, module, permission type. |
| Credit Limit endpoints | `Controllers/CreditLimitController.cs` | No `[Authorize]` or `[CheckUserPermission]` attributes are applied in this controller. |

Company/branch validation:

| Area | Validation |
|---|---|
| HANA live settings | `GetLiveHanaSettings(company)` only supports `1`, `2`, `3`. |
| SAP update branch | `UpdateCreditLimitAsync()` only supports `BranchId == "1"`, `"2"`, `"3"`. |
| Unknown branch | Throws exception and writes failure through `[cl].[updateHanaStatus]`. |

Approval ownership validation:

C# passes `FlowId`, `Company`, `UserId`, and `Remarks` into `[cl].[jsApproveDocument]` / `[cl].[jsRejectDocument]`. Any validation that the user is the correct approver must be inside these stored procedures. It is not enforced in `CreditLimitController.cs`.

Financial-role validation:

No explicit finance-role validation is visible in the controller/service. If finance-only approval is required, verify `[cl]` approval stored procedures and permission configuration.

Production risk: Credit Limit changes affect financial exposure. Before production, add or verify endpoint authorization for create, approve, reject, manual retry, and `UpdateHanaStatus`.

## 11. Debugging Guide

### Request Not Saving

Check:

| Step | Where |
|---|---|
| API | `POST /api/CreditLimit/CreateCLDocument` or `CreateCLDocumentV2` |
| Controller | `CreditLimitController.CreateDocument()` / `CreateDocumentV2()` |
| Service | `CreditLimitService.CreateDocumentAsync()` / `CreateDocumentWithAttachmentAsyncV2()` |
| SP | `[cl].[jsCreateDocument]` |
| Output | `@newDocumentId` must be greater than 0. |
| Attachment V2 | `[cl].[jsInsertCreditDocumentAttachment]` and `wwwroot/Uploads/CreditLimit`. |

Common causes:

| Symptom | Likely cause |
|---|---|
| `Invalid request` | Empty/malformed JSON or missing `documentData` form field. |
| `Attachment is required...` | `TotalEntries == 1` and no `attachment` uploaded. |
| `Document creation failed or missing newDocumentId` | `[cl].[jsCreateDocument]` did not output a valid document ID. |
| Attachment insert failed | `[cl].[jsInsertCreditDocumentAttachment]` did not output valid `@attachmentId`. |

### Approval Stuck

Check:

| Step | Where |
|---|---|
| Pending list | `POST /api/CreditLimit/GetPendingDocuments` |
| Flow detail | `GET /api/CreditLimit/GetApprovalFlow?flowId=...` |
| Flow status | `GET /api/CreditLimit/GetFlowStatus?flowId=...` |
| Approve SP | `[cl].[jsApproveDocument]` |
| Notify SP | `[cl].[jsCreditLimitNotify]` |

If approval succeeded but SAP did not update, check whether `[cl].[jsGetFlowStatus]` returns `A`. If it does not, `UpdateCreditLimitAsync()` returns `Flow is not approved from final stage`.

### SAP/HANA Update Failed

Check:

| Step | Where |
|---|---|
| Manual retry API | `POST /api/CreditLimit/UpdateCreditLimitInHana?flowId=...` |
| Service method | `CreditLimitService.UpdateCreditLimitAsync()` |
| Detail source | `[cl].[jsGetDocumentDetailUsingFlowId]` |
| Status source | `[cl].[jsGetFlowStatus]` |
| SAP endpoint | `PATCH BusinessPartners('{CustomerCode}')` |
| Status save | `[cl].[updateHanaStatus]` |
| Response text | `HanaStatusText` from `ApproveDocument` response or detail/list APIs. |

Common causes:

| Symptom | Likely cause |
|---|---|
| `Flow is not approved from final stage` | Workflow has not reached status `A`. |
| `Document detail not found.` | Bad `flowId` or SP not returning detail. |
| `Unknown branch type` | `BranchId` is not `1`, `2`, or `3`. |
| `HANA PATCH failed: ...` | SAP Service Layer rejected BP patch; inspect response body in `hanaStatusText`. |
| SAP session issue | Check `IBom2Service` session methods and `SapServiceLayer:BaseUrl`. |

### Wrong Customer Updated

Check:

| Area | What to verify |
|---|---|
| Request payload | `customerCode`, `branchId`, `companyId` in create request. |
| SQL detail | `[cl].[jsGetDocumentDetailUsingFlowId]` returns correct `CustomerCode` and `BranchId`. |
| Customer lookup | `GetCustomerNameByCodeAsync(Convert.ToInt32(doc.BranchId), doc.CustomerCode)` finds customer name from branch HANA database. |
| SAP patch URI | `BusinessPartners('{customerCode}')` uses `CustomerCode` exactly. |
| Branch/session | `BranchId` selects Oil/Bev/MART session. Wrong branch can update the wrong company DB. |

### Notifications Not Sent

Check:

| Step | Where |
|---|---|
| Current stage users after create | `[cl].[GetUsersInCurrentStage]` |
| Users after approval | `[cl].[jsCreditLimitNotify]` |
| FCM token lookup | `INotificationService.GetUserFcmTokenAsync(userId)` |
| Push send | `INotificationService.SendPushNotificationAsync(...)` |
| Notification insert | `INotificationService.InsertNotificationAsync(...)` |
| Pending count notification | `GET /api/CreditLimit/SendPendingCLCountNotification` |

Common causes:

| Symptom | Likely cause |
|---|---|
| No notification after create | `[cl].[GetUsersInCurrentStage]` returned no users. |
| No notification after approval | `[cl].[jsCreditLimitNotify]` returned empty `userIdsToApprove`. |
| Push not received | User has no FCM token or duplicate/blank token skipped. |
| Notification count wrong | `[cl].[jsGetCreditDocumentInsight]` count data is wrong for user/company/month. |

### Dropdown Not Loading

Check:

| Step | Where |
|---|---|
| API | `GET /api/CreditLimit/GetCustomerCards?company=...` |
| Service | `GetCustomerCardsAsync(company)` |
| HANA helper | `GetLiveHanaSettings(company)` |
| HANA procedure | `CALL "{schema}"."GetCustomerCards"()` |
| Config | `LiveHanaConnection`, `LiveBevHanaConnection`, `LiveMartHanaConnection` |

## 12. Request / Response Examples

### Create Request

```http
POST /api/CreditLimit/CreateCLDocument
Content-Type: application/json
```

```json
{
  "branchId": "1",
  "customerCode": "C000123",
  "customerValue": "ABC Traders",
  "currentBalance": 250000,
  "currentCreditLimit": 500000,
  "newCreditLimit": 750000,
  "validTill": "2026-06-30",
  "companyId": 1,
  "createdBy": 101
}
```

Response:

```json
{
  "success": true,
  "message": "Document created successfully and notifications sent.",
  "newDocumentId": 345
}
```

### Create Request V2 With Attachment

```http
POST /api/CreditLimit/CreateCLDocumentV2
Content-Type: multipart/form-data
```

`documentData`:

```json
{
  "branchId": "1",
  "customerCode": "C000123",
  "customerValue": "ABC Traders",
  "currentBalance": 250000,
  "currentCreditLimit": 500000,
  "newCreditLimit": 750000,
  "validTill": "2026-06-30",
  "companyId": 1,
  "createdBy": 101,
  "totalEntries": 1
}
```

`attachment`: uploaded file.

Response:

```json
{
  "success": true,
  "message": "Document created successfully",
  "creditDocumentId": 345
}
```

### Approval Payload

```http
POST /api/CreditLimit/ApproveDocument
Content-Type: application/json
```

```json
{
  "flowId": 10025,
  "company": 1,
  "userId": 45,
  "remarks": "Approved",
  "action": "Approve"
}
```

Final approval success response:

```json
{
  "success": true,
  "flowId": 10025,
  "message": "Flow approved successfully.",
  "hanaStatusText": "Credit Limit updated successfully in HANA for Customer: C000123, Branch: 1, New Credit Limit: 750000"
}
```

Intermediate approval response:

```json
{
  "success": true,
  "flowId": 10025,
  "message": "Flow approved successfully.",
  "hanaStatusText": "Flow is not approved from final stage"
}
```

SAP failure response pattern:

```json
{
  "success": true,
  "flowId": 10025,
  "message": "Flow approved successfully.",
  "hanaStatusText": "HANA PATCH failed: { SAP error response body }"
}
```

Note: the HTTP response can still be `200 OK` with `success: true` because SQL approval succeeded. Use `hanaStatusText` to know whether SAP update succeeded.

### Reject Payload

```http
POST /api/CreditLimit/RejectDocument
Content-Type: application/json
```

```json
{
  "flowId": 10025,
  "company": 1,
  "userId": 45,
  "remarks": "Rejecting due to high outstanding balance",
  "action": "Reject"
}
```

Response:

```json
{
  "success": true,
  "message": "Document rejected successfully."
}
```

### Pending Request Response

```http
POST /api/CreditLimit/GetPendingDocuments
Content-Type: application/json
```

```json
{
  "userId": 45,
  "companyId": 1,
  "month": "05-2026"
}
```

Response:

```json
{
  "success": true,
  "data": [
    {
      "id": 345,
      "branchId": "1",
      "customerCode": "C000123",
      "customerName": "ABC Traders",
      "customerValue": "ABC Traders",
      "customerBalance": 250000,
      "currentCreditLimit": 500000,
      "newCreditLimit": 750000,
      "validTill": "2026-06-30",
      "createdById": 101,
      "createdBy": "Creator Name",
      "createdOn": "2026-05-09T10:00:00",
      "flowId": 10025
    }
  ]
}
```

### Manual SAP Update Retry

```http
POST /api/CreditLimit/UpdateCreditLimitInHana?flowId=10025
```

Success:

```json
{
  "success": true,
  "message": "Credit Limit updated successfully in HANA for Customer: C000123, Branch: 1, New Credit Limit: 750000"
}
```

Failure:

```json
{
  "success": false,
  "message": "Flow is not approved from final stage"
}
```

## 13. Important Notes

| Area | Note |
|---|---|
| Financial risk | This module changes SAP BP credit exposure. Treat approval and retry APIs as high-risk. |
| Authorization gap | `CreditLimitController.cs` has no `[Authorize]` or `[CheckUserPermission]` attributes. |
| SAP update timing | SQL approval happens before SAP PATCH. SAP failure does not undo approval in C# code. |
| Final status | SAP update only runs when `[cl].[jsGetFlowStatus]` returns `A`. |
| Branch controls SAP DB | `BranchId` selects Oil/Bev/MART SAP session. Wrong branch can update the wrong company database. |
| Customer code is critical | SAP patch URI uses `BusinessPartners('{CustomerCode}')`. Wrong customer code updates wrong BP. |
| HANA wording | Methods and fields say "HANA", but final credit update is SAP Service Layer PATCH. |
| Attachments | V2 stores files in `wwwroot/Uploads/CreditLimit`; file system permissions can break upload. |
| Attachment rule | Controller requires attachment when `TotalEntries == 1`. |
| Customer name lookup | Lists call HANA `GetCustomerCards()` per document to populate `CustomerName`; this can be slow or fail if HANA is down. |
| No direct table names | Request, approval, attachment, and status table names are hidden behind `[cl]` stored procedures. |
| Manual status API | `UpdateHanaStatus` can change status text manually; protect it carefully. |
| OpenCslm | `OpenCslm` directly calls HANA `OPENCSLM`; confirm whether it is still intended for production because it does not follow the normal SQL approval flow. |

