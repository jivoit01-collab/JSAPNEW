# Budget Module Documentation

## 1. Module Overview

The Budget module is used to view, approve, reject, allocate, and report budget-controlled business documents. It helps the business control spending before documents move forward in the financial process.

In this project, Budget is not implemented as one clean `BudgetController` and `BudgetService`. The real implementation is split across:

| Area | Main files |
| --- | --- |
| Budget approval and reporting APIs | `Controllers/AuthController.cs`, `Services/Interfaces/IUserService.cs`, `Services/Implementation/UserService.cs` |
| Budget master and monthly allocation APIs | `Controllers/Auth2Controller.cs`, `Services/Interfaces/IAuth2Service.cs`, `Services/Implementation/Auth2Service.cs` |
| Report/search API | `Controllers/ReportsController.cs`, `Services/Interfaces/IReportsService.cs`, `Services/Implementation/ReportsService.cs` |
| Dashboard API | `Controllers/DashboardController.cs`, `Services/Interfaces/IDashboardService.cs`, `Services/Implementation/DashboardService.cs` |
| Frontend report page | `Views/ReportsWeb/ApprovalStatusReport.cshtml` |
| Frontend JavaScript | `wwwroot/js/budget.js`, `wwwroot/js/AvtarDashboard.js` |
| Frontend CSS | `wwwroot/css/budget.css`, `wwwroot/css/AvtarDashboard.css` |
| DTO/model classes | `Models/AuthModels.cs`, `Models/Auth2Models.cs`, `Models/ReportsModels.cs`, `Models/DashboardModels.cs` |

### What Business Problem It Solves

The module solves these problems:

- Users need to see pending, approved, and rejected budget-related documents.
- Approvers need a workflow to approve or reject budget requests.
- Finance teams need to track monthly budget allocation, used amount, rejected amount, and remaining balance.
- Management needs dashboards and Excel exports.
- Budget masters need to be created with sub-budgets.
- Monthly allocation changes need separate approval before the allocation amount changes.

### Who Uses It

Typical users are:

| User/department | Usage |
| --- | --- |
| Budget owners | Track budget usage and document requests. |
| Department users | Submit or monitor budget-linked expenses/documents. |
| Approvers/managers | Approve or reject budget workflow items. |
| Finance team | Create monthly budgets, monitor balances, and control allocation changes. |
| Management | View summary dashboards and exports. |
| IT/support | Debug stuck approvals, wrong amounts, and report issues. |

### What Happens After Budget Approval

After approval, the stored procedure `[bud].[jsApproveBudget]` updates the budget workflow state. If the approval is not the final stage, the next approver is notified. If it is the final stage, the budget document becomes approved from the application workflow perspective.

Important: this codebase does not show a SAP Service Layer write for Budget approval. Budget approval and allocation updates are handled through SQL Server stored procedures in the `[bud]` schema.

### How Budget Allocation Works

Budget allocation exists in two forms:

1. Legacy category monthly budget:
   - API: `POST /api/auth/CreateCategoryMonthlyBudget`
   - Service: `UserService.CreateCategoryMonthlyBudgetAsync`
   - Stored procedure: `[bud].[jsCreateCategoryMonthlyBudget]`
   - Fields: `budgetCategory`, `subBudget`, `month`, `totalAmount`, `company`

2. New budget master/monthly allocation flow:
   - API: `POST /api/Auth2/CreateBudgetWithSubBudgets`
   - API: `POST /api/Auth2/CreateMonthlyAllocations`
   - API: `POST /api/Auth2/createBudgetAllocation`
   - Approval API: `POST /api/Auth2/approveBudgetAllocation`
   - Reject API: `POST /api/Auth2/rejectBudgetAllocation`
   - Service: `Auth2Service`
   - Stored procedures under `[bud]`

The hierarchy is:

```text
Budget
  |
  +-- SubBudget
        |
        +-- Monthly Allocation
              |
              +-- Allocation Change Request
                    |
                    +-- Approval Flow
```

## 2. Module Architecture

### Actual Architecture

```text
ReportsWeb/ApprovalStatusReport.cshtml
        |
        v
wwwroot/js/budget.js
        |
        +--> /api/auth/GetAllBudgetInsight
        +--> /api/auth/getpendingbudgetwithdetails
        +--> /api/auth/getapprovedbudgetwithdetails
        +--> /api/auth/getrejectedbudgetwithdetails
        +--> /api/auth/GetBudgetApprovalFlow
        +--> /api/auth/GetAllBudgetSummaryAmount
        |
        v
AuthController
        |
        v
IUserService / UserService
        |
        v
SQL Server stored procedures in [bud] schema


Budget allocation admin APIs
        |
        v
Auth2Controller
        |
        v
IAuth2Service / Auth2Service
        |
        v
[bud] master/allocation stored procedures


Budget search/report APIs
        |
        v
ReportsController
        |
        v
ReportsService
        |
        v
[bud].[jsSearchBudgetsByCompany]


Dashboard budget APIs
        |
        v
DashboardController
        |
        v
DashboardService
        |
        v
bud.jsBudgetTable_vg
```

### Dependency Registration

`Program.cs` registers these services:

| Interface | Implementation |
| --- | --- |
| `IUserService` | `UserService` |
| `IAuth2Service` | `Auth2Service` |
| `IReportsService` | `ReportsService` |
| `IDashboardService` | `DashboardService` |
| `INotificationService` | `NotificationService` |

### Main Stored Procedures and SQL Objects

| Purpose | Stored procedure / object |
| --- | --- |
| Budget dropdowns | `jsPostSapTables @mode='budget'`, `jsPostSapTables @mode='subBudget'` |
| User budget counts | `[bud].[jsGetBudgetInsight]` |
| All user budget counts | `[bud].[jsGetBudgetInsightAll]` |
| Pending budget list | `[bud].[jsGetPendingBudgets]` |
| Approved budget list | `[bud].[jsGetApprovedBudgets]` |
| Rejected budget list | `[bud].[jsGetRejectedBudgets]` |
| Next approver | `[bud].[jsGetNextApprover]` |
| Approve budget | `[bud].[jsApproveBudget]` |
| Reject budget | `[bud].[jsRejectBudget]` |
| Approval flow | `[bud].[jsGetBudgetApprovalFlow]` |
| Budget detail | `[bud].[jsGetBudgetDetailById]` |
| Attachments | `[bud].[jsGetBudgetAttachments]` |
| Category monthly budget create | `[bud].[jsCreateCategoryMonthlyBudget]` |
| Category monthly summary | `[bud].[jsGetBudgetCategorySummaryDashboard]` |
| Category monthly detail | `[bud].[jsGetCategoryMonthlyBudget]` |
| Budget category dropdown | `[bud].[jsBudgetCategoryDropdown]` |
| Sub-budget category dropdown | `[bud].[jsSubBudgetCategoryDropdown]` |
| User budget allocation | `[bud].[jsGetUserBudgetAllocation]` |
| Budget summary | `[bud].[jsGetBudgetSummary]` |
| Search budgets | `[bud].[jsSearchBudgetsByCompany]` |
| Budget master create | `[bud].[CreateBudgetWithSubBudgets]` |
| Monthly allocations create | `[bud].[CreateMonthlyAllocations]` |
| Monthly allocations update | `[bud].[UpdateMonthlyAllocations]` |
| Allocation request create | `[bud].[CreateBudgetAllocationRequest]` |
| Pending allocation requests | `[bud].[jsGetPendingBudgetAllocationRequests]` |
| Approved allocation requests | `[bud].[jsGetApprovedBudgetAllocationRequests]` |
| Rejected allocation requests | `[bud].[jsGetRejectedBudgetAllocationRequests]` |
| Approve allocation request | `[bud].[jsApproveBudgetAllocationRequest]` |
| Reject allocation request | `[bud].[jsRejectBudgetAllocationRequest]` |
| Allocation flow | `[bud].[jsGetBudApprovalFlow]` |
| Dashboard view | `bud.jsBudgetTable_vg` |

## 3. Complete Workflow

### Budget Approval Report Flow

1. User opens `Views/ReportsWeb/ApprovalStatusReport.cshtml`.
2. Razor injects `ViewBag.CompanyId` and `ViewBag.UserId` into JavaScript variables.
3. Page loads `wwwroot/js/budget.js`.
4. JavaScript sets default month, year, and type.
5. JavaScript calls:
   - `GET /api/auth/GetAllBudgetInsight?company={companyId}&month={MM-yyyy}`
6. `AuthController.GetBudgetInsight` calls:
   - `UserService.GetBudgetInsightAsync`
7. `UserService` executes:
   - `[bud].[jsGetBudgetInsightAll] @company, @month`
8. The frontend filters rows where `type === 'Budget'`.
9. User clicks a row to open the modal.
10. Modal calls one of:
    - `GET /api/auth/getpendingbudgetwithdetails`
    - `GET /api/auth/getapprovedbudgetwithdetails`
    - `GET /api/auth/getrejectedbudgetwithdetails`
11. Controller calls the related `UserService` method.
12. Service executes the matching `[bud]` stored procedure.
13. For each budget, controller also calls `GetNextApproverAsync`.
14. User can click "View Progress".
15. Frontend calls:
    - `GET /api/auth/GetBudgetApprovalFlow?budgetId={budgetId}`
16. Service executes `[bud].[jsGetBudgetApprovalFlow]`.

### Budget Approval Action Flow

1. Approver submits approval from a client.
2. Client calls:
   - `POST /api/auth/approvebudget`
3. Request maps to `BudgetRequest2`.
4. `AuthController.ApproveBudget` calls:
   - `UserService.ApproveBudgetAsync`
5. Service splits comma-separated `docIds`.
6. For each `docId`, service executes:
   - `[bud].[jsApproveBudget] @docId, @userId, @company, @remarks`
7. Service calls:
   - `GetUserIdsSendNotificatiosAsync`
   - stored procedure `[bud].[jsBudgetNotify]`
8. Service gets FCM tokens from `NotificationService.GetUserFcmTokenAsync`.
9. Service sends push notifications using `NotificationService.SendPushNotificationAsync`.
10. Service stores app notification using `NotificationService.InsertNotificationAsync`.
11. API returns success or SQL error.

### Budget Rejection Flow

1. Approver submits rejection.
2. Client calls:
   - `POST /api/auth/rejectebudget`
3. Request maps to `BudgetRequest`.
4. `AuthController.RejectBudget` calls:
   - `UserService.RejectBudgetAsync`
5. Service executes:
   - `[bud].[jsRejectBudget] @docId, @userId, @company, @remarks`
6. Stored procedure updates workflow status as rejected.
7. API returns the procedure message.

### Budget Master Creation Flow

1. Finance/admin creates a Budget with sub-budgets.
2. Client calls:
   - `POST /api/Auth2/CreateBudgetWithSubBudgets`
3. Request maps to `CreateBudget2Request`.
4. `Auth2Controller.CreateBudgetWithSubBudgets` calls:
   - `Auth2Service.CreateBudgetWithSubBudgetsAsync`
5. Service builds a table-valued parameter:
   - `bud.SubBudgetTableType`
6. Service executes:
   - `[bud].[CreateBudgetWithSubBudgets]`
7. Stored procedure creates Budget and SubBudget records.
8. API returns `BudgetId`, `BudgetName`, `CompanyId`, and `SubBudgetsCreated`.

### Monthly Allocation Flow

1. Finance/admin creates monthly allocation for a budget.
2. Client calls:
   - `POST /api/Auth2/CreateMonthlyAllocations`
3. Request maps to `CreateMonthlyAllocation2Request`.
4. Service builds table-valued parameter:
   - `bud.MonthlyAllocationTableType`
5. Service executes:
   - `[bud].[CreateMonthlyAllocations]`
6. Stored procedure creates header allocation and sub-budget allocation rows.
7. API returns allocation month, budget amount, sub-budget count, and total sub-budget amount.

### Allocation Change Approval Flow

1. User requests a change to an existing budget allocation.
2. Client calls:
   - `POST /api/Auth2/createBudgetAllocation`
3. Request maps to `CreateBudgetAllocationRequestModel`.
4. Controller validates:
   - `BudgetAllocationId > 0`
   - `NewAmount > 0`
   - `CreatedBy > 0`
5. Service executes:
   - `[bud].[CreateBudgetAllocationRequest]`
6. Stored procedure creates a change request and approval flow.
7. Approver calls:
   - `POST /api/Auth2/approveBudgetAllocation`
   - or `POST /api/Auth2/rejectBudgetAllocation`
8. Service executes:
   - `[bud].[jsApproveBudgetAllocationRequest]`
   - or `[bud].[jsRejectBudgetAllocationRequest]`
9. Final approval should apply the requested amount according to stored procedure logic.

### Draft, Edit, Freeze, Return

The requested documentation sections mention draft save, budget edit, freeze, and return for correction. In the analyzed code:

| Flow | Implementation status |
| --- | --- |
| Draft save | No dedicated Budget draft API found. |
| Edit budget master | `Auth2Service.UpdateMonthlyAllocationsAsync` exists, but no controller endpoint was found in the analyzed `Auth2Controller.cs` chunk. |
| Freeze budget | No explicit freeze API found. Active/inactive is handled by `IsActive` for Budget/SubBudget master records. |
| Return for correction | No explicit return API found. Available actions are approve and reject. |

## 4. Before Approval vs After Approval

### Before Budget Approval

| Item | Behavior |
| --- | --- |
| Main APIs | `getpendingbudgetwithdetails`, `GetBudgetDetailById`, `GetBudgetApprovalFlow`, `approvebudget`, `rejectebudget` |
| Main service | `UserService` |
| Main stored procedures | `[bud].[jsGetPendingBudgets]`, `[bud].[jsGetBudgetDetailById]`, `[bud].[jsGetBudgetApprovalFlow]` |
| Status | Usually returned as `Pending`; flow action status may be `P`, `A`, or `R` |
| Is budget active? | The request is visible in workflow but not final approved. |
| Can budget be used as approved spend? | No, not until final approval. |
| Are notifications sent? | Yes, after approval when the next approver exists, using `[bud].[jsBudgetNotify]` and `NotificationService`. |
| Is SAP called? | No SAP write call found in Budget approval code. |

### After Budget Approval

| Item | Behavior |
| --- | --- |
| Main API | `POST /api/auth/approvebudget` |
| Service method | `UserService.ApproveBudgetAsync` |
| Stored procedure | `[bud].[jsApproveBudget]` |
| Status | Stored procedure returns updated state; list APIs show `Approved` when done. |
| Notifications | Sent to next approver if more approval is required. |
| DB fields | Controlled by `[bud].[jsApproveBudget]`; visible through `CurrentStageId`, `CurrentStage`, `CurrentStatus`, approval history, and list SPs. |
| Is budget final? | Yes only when all approval stages are completed. |
| Is SAP/HANA updated? | No SAP/HANA update method was found for Budget approval. |

### Before Allocation Approval

| Item | Behavior |
| --- | --- |
| API | `POST /api/Auth2/createBudgetAllocation` |
| Service method | `Auth2Service.CreateBudgetAllocationRequestAsync` |
| Stored procedure | `[bud].[CreateBudgetAllocationRequest]` |
| Status | Request appears in pending allocation APIs. |
| Active amount changed? | No. Requested amount waits for approval. |
| Validation | Controller checks positive allocation id, positive amount, and valid creator. |

### After Allocation Approval

| Item | Behavior |
| --- | --- |
| API | `POST /api/Auth2/approveBudgetAllocation` |
| Service method | `Auth2Service.ApproveBudgetAllocationRequestAsync` |
| Stored procedure | `[bud].[jsApproveBudgetAllocationRequest]` |
| Status | Request moves to approved when SP completes final approval. |
| Allocation changed? | Controlled by stored procedure. Final stage approval should update allocation amount. |
| Rejection API | `POST /api/Auth2/rejectBudgetAllocation` |
| Rejection SP | `[bud].[jsRejectBudgetAllocationRequest]` |

## 5. API Documentation

### Approval and Reporting APIs - `AuthController`

Base route: `/api/auth`

| API | Purpose | Runs | Service method | Stored procedure |
| --- | --- | --- | --- | --- |
| `GET /getbudgets?company=` | Budget dropdown | Before request | `GetBudgetAsync` | `jsPostSapTables @mode='budget'` |
| `GET /getSubBudgets?company=` | Sub-budget dropdown | Before request | `GetSubBudgetAsync` | `jsPostSapTables @mode='subBudget'` |
| `GET /budgetstatusCount` | Counts for one user/month | Reporting | `GetAllBudgetApprovalCountsAsync` | `[bud].[jsGetBudgetInsight]` |
| `GET /budgetstatusCount2` | Counts for one user/month without default month | Reporting | `GetAllBudgetApprovalCountsAsync` | `[bud].[jsGetBudgetInsight]` |
| `POST /updatebudget` | Enable/disable budget mapping | Admin | `UpdateBudgetAsync` | `jsUpdateBudget` |
| `POST /updatesubbudget` | Enable/disable sub-budget mapping | Admin | `UpdateSubBudgetAsync` | `jsUpdateSubBudget` |
| `GET /getpendingbudgetwithdetails` | Pending budgets for user | Before approval | `GetPendingBudgetWithDetailsAsync` | `[bud].[jsGetPendingBudgets]` |
| `GET /getapprovedbudgetwithdetails` | Approved budgets for user | After approval | `GetApprovedBudgetWithDetailsAsync` | `[bud].[jsGetApprovedBudgets]` |
| `GET /getrejectedbudgetwithdetails` | Rejected budgets for user | After rejection | `GetRejectedBudgetWithDetailsAsync` | `[bud].[jsGetRejectedBudgets]` |
| `GET /getallbudgetwithdetails` | Pending + approved + rejected combined | Reporting | `GetAllBudgetWithDetailsAsync` | Three list SPs |
| `POST /approvebudget` | Approve one or many budget docs | Approval action | `ApproveBudgetAsync` | `[bud].[jsApproveBudget]` |
| `POST /rejectebudget` | Reject one budget doc | Rejection action | `RejectBudgetAsync` | `[bud].[jsRejectBudget]` |
| `GET /getuserbudgettypes` | User budget type list | Dropdown/report | `GetUserBudgetTypesAsync` | `jsGetUserBudgetTypes` |
| `GET /getbudgetallocation` | User monthly allocation | Reporting | `GetUserBudgetAllocationAsync` | `[bud].[jsGetUserBudgetAllocation]` |
| `POST /CreateCategoryMonthlyBudget` | Create category monthly budget | Allocation setup | `CreateCategoryMonthlyBudgetAsync` | `[bud].[jsCreateCategoryMonthlyBudget]` |
| `GET /GetBudgetCategorySummaryDashboard` | Category budget dashboard | Reporting | `GetBudgetCategorySummaryDashboardAsync` | `[bud].[jsGetBudgetCategorySummaryDashboard]` |
| `GET /GetBudgetDetailById` | Budget header, lines, attachments | Debug/details | `GetBudgetDetailByIdAsync` | `[bud].[jsGetBudgetDetailById]`, `[bud].[jsGetBudgetAttachments]` |
| `GET /GetBudgetDetailByIdv2` | Same as above with file URL company param | Debug/details | `GetBudgetDetailByIdAsyncv2` | Same |
| `GET /GetCategoryMonthlyBudget` | Category monthly detail | Reporting | `GetCategoryMonthlyBudgetAsync` | `[bud].[jsGetCategoryMonthlyBudget]` |
| `GET /BudgetCategoryDropdown` | User category dropdown | Frontend dropdown | `BudgetCategoryDropdownAsync` | `[bud].[jsBudgetCategoryDropdown]` |
| `GET /GetBudgetApprovalFlow` | Stage history | Debug/workflow | `GetBudgetApprovalFlowAsync` | `[bud].[jsGetBudgetApprovalFlow]` |
| `GET /GetAllBudgetInsight` | All user counts for month | Report page | `GetBudgetInsightAsync` | `[bud].[jsGetBudgetInsightAll]` |
| `GET /sendpendingbudgetnotify` | Send pending reminders | Notification | `SendPendingCountNotificationAsync` | `[bud].[jsGetBudgetInsight]` plus FCM |
| `GET /GetBudgetSummary` | Summary by category/sub-budget | Reporting | `GetBudgetSummaryAsync` | `[bud].[jsGetBudgetSummary]` |
| `GET /GetBudgetSummaryAmount` | Combined list/details for user | Export/detail | `GetCombinedBudgetsAsync` | List SPs + `[bud].[jsGetBudgetDetailById]` |
| `GET /GetAllBudgetSummaryAmount` | Combined list/details for all users | Excel export | `GetBudgetInsightAsync`, `GetCombinedBudgetsAsync` | Multiple `[bud]` SPs |

### Master and Allocation APIs - `Auth2Controller`

Base route: `/api/Auth2`

| API | Purpose | Runs | Service method | Stored procedure |
| --- | --- | --- | --- | --- |
| `POST /CreateBudgetWithSubBudgets` | Create budget master and child sub-budgets | Setup | `CreateBudgetWithSubBudgetsAsync` | `[bud].[CreateBudgetWithSubBudgets]` |
| `POST /CreateMonthlyAllocations` | Create monthly allocation and sub-budget allocation rows | Setup | `CreateMonthlyAllocationsAsync` | `[bud].[CreateMonthlyAllocations]` |
| `GET /GetAllBudgets` | List budget masters | Dropdown/admin | `GetAllBudgetsAsync` | `[bud].[GetAllBudgets]` |
| `GET /GetBudgetAndSubBudgetDetails` | Budget/sub-budget detail and monthly comparison | Details | `GetBudgetAndSubBudgetDetailsAsync` | `[bud].[GetBudgetAndSubBudgetDetails]` |
| `GET /GetBudgetWithSubBudgets` | Budget master with children | Details | `GetBudgetWithSubBudgetsAsync` | `[bud].[GetBudgetWithSubBudgets]` |
| `GET /GetDistinctBudgetAttributes` | Distinct filter values | Dropdown/report | `GetDistinctBudgetAttributesAsync` | `[bud].[GetDistinctBudgetAttributes]` |
| `GET /GetSubBudgetsByBudgetId` | Sub-budgets by parent budget | Dropdown/admin | `GetSubBudgetsByBudgetIdAsync` | `[bud].[GetSubBudgetsByBudgetId]` |
| `GET /GetBudgetMonthlyAllocationView` | Allocation view for budget/month | Details | `GetBudgetMonthlyAllocationViewAsync` | `[bud].[GetBudgetMonthlyAllocationView]` |
| `GET /getAllBudgetTypes` | Budget names/types | Dropdown | `GetAllTypeBudgetAsync` | `[bud].[jsGetAllTypeBudget]` |
| `GET /getSubBudgetByBudget` | Sub-budget names for a budget | Dropdown | `GetSubBudgetByBudgetAsync` | `[bud].[jsGetSubBudgetUsingBudget]` |
| `GET /GetPendingBudgetAllocationRequests` | Pending allocation change requests | Before approval | `GetPendingBudgetAllocationRequestsAsync` | `[bud].[jsGetPendingBudgetAllocationRequests]` |
| `GET /GetApprovedBudgetAllocationRequests` | Approved allocation requests | After approval | `GetApprovedBudgetAllocationRequestsAsync` | `[bud].[jsGetApprovedBudgetAllocationRequests]` |
| `GET /GetRejectedBudgetAllocationRequests` | Rejected allocation requests | After rejection | `GetRejectedBudgetAllocationRequestsAsync` | `[bud].[jsGetRejectedBudgetAllocationRequests]` |
| `POST /approveBudgetAllocation` | Approve allocation change | Approval action | `ApproveBudgetAllocationRequestAsync` | `[bud].[jsApproveBudgetAllocationRequest]` |
| `POST /rejectBudgetAllocation` | Reject allocation change | Rejection action | `RejectBudgetAllocationRequestAsync` | `[bud].[jsRejectBudgetAllocationRequest]` |
| `POST /createBudgetAllocation` | Create allocation change request | Before approval | `CreateBudgetAllocationRequestAsync` | `[bud].[CreateBudgetAllocationRequest]` |
| `GET /GetBudgetAllocationRequestDetail` | Request detail and approval history | Debug/details | `GetBudgetAllocationRequestDetail` | `[bud].[jsGetBudgetAllocationRequestDetail]` |
| `GET /getMonthlyAllocationInsights` | Count pending/approved/rejected allocation requests | Reporting | `GetBudgetMonthlyAllocationInsights` | `[bud].[jsGetBudgetInsightMonthlyAllocation]` |
| `GET /GetAllBudgetAllocationRequests` | Combined allocation requests | Reporting | `GetAllBudgetAllocationRequestsAsync` | Pending/approved/rejected allocation SPs |
| `GET /GetBudgetAllocationFlow` | Allocation approval flow | Debug/workflow | `GetBudgetAllocationFlowAsync` | `[bud].[jsGetBudApprovalFlow]` |

### Report API - `ReportsController`

Base route: `/api/Reports`

| API | Purpose | Service method | Stored procedure |
| --- | --- | --- | --- |
| `GET /GetBudgetByCompany?company=&docEntry=&cardName=&month=&status=` | Search budget docs by company, DocEntry, vendor/card name, month, status | `ReportsService.GetBudgetByCompanyAsync` | `[bud].[jsSearchBudgetsByCompany]` |

### Dashboard APIs - `DashboardController`

Base route: `/api/Dashboard`

| API | Purpose | Service method | SQL object |
| --- | --- | --- | --- |
| `GET /getUniqueBudgets?branch=` | Budget filter list for dashboard | `DashboardService.GetUniqueBudgets` | `bud.jsBudgetTable_vg` |
| `GET /getUniqueAccounts?branch=` | Account filter list for dashboard | `DashboardService.GetUniqueAccounts` | `bud.jsBudgetTable_vg` |
| `GET /getBudgetDataByBranch?branch=` | Dashboard raw budget data | `DashboardService.GetAllBudgetDataAsync` | `bud.jsBudgetTable_vg` |

## 6. Approval Flow

### Budget Document Approval

```text
Pending
  |
  v
Current approver opens request
  |
  +-- Reject --> Rejected
  |
  +-- Approve
        |
        +-- More stages exist --> Next approver notified
        |
        +-- Final stage reached --> Approved
```

### Actual Status Values

| Layer | Status values |
| --- | --- |
| Budget list models | `Pending`, `Approved`, `Rejected` |
| Flow UI in `budget.js` | `A` = Approved, `R` = Rejected, anything else = Pending |
| Stored procedures | Return workflow-specific values through fields like `CurrentStatus`, `ApprovalStatus`, `RejectionStatus`, `flowStatus`, `actionStatus` |

### How Approval Starts

For budget documents, approval flow is already present when documents appear in `[bud].[jsGetPendingBudgets]`. This module does not show the original document submission code that creates those budget workflow rows. The approval/reporting module consumes existing workflow rows.

For allocation change requests, approval starts when:

```text
POST /api/Auth2/createBudgetAllocation
  -> Auth2Service.CreateBudgetAllocationRequestAsync
  -> [bud].[CreateBudgetAllocationRequest]
```

### How Approver Is Selected

The C# code asks SQL for the next approver:

```text
UserService.GetNextApproverAsync
  -> EXEC [bud].[jsGetNextApprover] @budgetId
```

The service does not calculate approvers in C#. Stage, priority, assigned user, approval required, and rejection required are returned by stored procedures.

### Reject Flow

Budget document reject:

```text
POST /api/auth/rejectebudget
  -> UserService.RejectBudgetAsync
  -> [bud].[jsRejectBudget]
  -> Rejected list APIs show item
```

Allocation reject:

```text
POST /api/Auth2/rejectBudgetAllocation
  -> Auth2Service.RejectBudgetAllocationRequestAsync
  -> [bud].[jsRejectBudgetAllocationRequest]
  -> Rejected allocation APIs show item
```

### Return Flow

No return-for-correction API was found for Budget. Current supported workflow actions in code are approve and reject.

## 7. Budget Allocation Logic

### Budget Master

`CreateBudgetWithSubBudgetsAsync` creates a parent budget and child sub-budgets in one call.

Request model: `CreateBudget2Request` in `Models/Auth2Models.cs`

Important fields:

| Field | Meaning |
| --- | --- |
| `Company` | Company id |
| `BudgetName` | Parent budget name |
| `Description` | Optional description |
| `TotalAmount` | Yearly/overall parent amount |
| `IsActive` | Active flag |
| `SubBudgets` | Child budget list |

The service sends sub-budgets as TVP `bud.SubBudgetTableType`.

### Monthly Allocation

`CreateMonthlyAllocationsAsync` creates month-wise allocation for a parent budget and sub-budget rows.

Request model: `CreateMonthlyAllocation2Request`

Important fields:

| Field | Meaning |
| --- | --- |
| `BudgetId` | Parent budget id |
| `AllocationMonth` | Month date, example `2025-05-01` |
| `BudgetAllocatedAmount` | Total monthly amount for parent budget |
| `BudgetNotes` | Optional notes |
| `SubBudgetAllocations` | Child allocation list |

The service sends sub-budget amounts as TVP `bud.MonthlyAllocationTableType`.

### Allocation Change Request

`CreateBudgetAllocationRequestAsync` creates a request to change an existing allocation amount.

Request model: `CreateBudgetAllocationRequestModel`

| Field | Validation |
| --- | --- |
| `BudgetAllocationId` | Must be greater than 0 |
| `NewAmount` | Must be greater than 0 |
| `CreatedBy` | Must be greater than 0 |

The current amount, requested amount, and amount difference are returned by list models such as `PendingBudgetAllocation`, `ApprovedBudgetAllocation`, and `RejectedBudgetAllocation`.

### Remaining Balance and Consumed Budget

The C# code does not calculate remaining balance directly. It reads calculated values from SQL stored procedures and views.

| Value | Source |
| --- | --- |
| Total amount | `BudgetCategorySummaryDashboardModel.totalAmount`, `BudgetLineDetailDTO.Current_month_Budget` |
| Used amount | `BudgetCategorySummaryDashboardModel.usedAmount`, `BudgetLineDetailDTO.Current_month_Posted_Amount` |
| Rejected amount | `BudgetCategorySummaryDashboardModel.rejectedAmount` |
| Remaining | `BudgetCategorySummaryDashboardModel.remaining` |
| Usage percent | `BudgetCategorySummaryDashboardModel.usagePercent` |

Check these stored procedures first when totals are wrong:

- `[bud].[jsGetBudgetCategorySummaryDashboard]`
- `[bud].[jsGetCategoryMonthlyBudget]`
- `[bud].[jsGetBudgetSummary]`
- `[bud].[jsGetUserBudgetAllocation]`
- `bud.jsBudgetTable_vg`

### Over-Budget Prevention

No C# over-budget check was found beyond basic request validation. Over-budget prevention appears to be stored-procedure controlled. The most important procedures to inspect are:

- `[bud].[CreateMonthlyAllocations]`
- `[bud].[UpdateMonthlyAllocations]`
- `[bud].[CreateBudgetAllocationRequest]`
- `[bud].[jsApproveBudgetAllocationRequest]`

## 8. Database Flow

### Budget Document List Flow

```text
User/month/company
  |
  +-- [bud].[jsGetPendingBudgets]
  +-- [bud].[jsGetApprovedBudgets]
  +-- [bud].[jsGetRejectedBudgets]
        |
        v
PendingBudgetModel / ApproveBudgetModel / RejectedBudgetModel
        |
        v
AuthController adds NextApprover from [bud].[jsGetNextApprover]
        |
        v
Frontend modal
```

### Budget Detail Flow

```text
budgetId
  |
  v
[bud].[jsGetBudgetDetailById]
  |
  +-- BudgetDetailDTO header
  +-- BudgetLineDetailDTO lines
  |
  v
[bud].[jsGetBudgetAttachments]
  |
  v
BudgetResponseDTO
```

### Budget Approval Update Flow

```text
docIds + userId + company + remarks
  |
  v
[bud].[jsApproveBudget]
  |
  v
[bud].[jsBudgetNotify]
  |
  v
NotificationService
```

### Budget Allocation Request Flow

```text
BudgetAllocationId + NewAmount + CreatedBy
  |
  v
[bud].[CreateBudgetAllocationRequest]
  |
  v
Pending allocation request
  |
  +-- [bud].[jsApproveBudgetAllocationRequest]
  +-- [bud].[jsRejectBudgetAllocationRequest]
```

### Important Tables

Table names are mostly hidden behind stored procedures in this repository. The visible SQL object names suggest the Budget module is stored under the `[bud]` schema. When debugging at database level, start from these procedure definitions and the view:

- `bud.jsBudgetTable_vg`
- `[bud].[jsGetBudgetDetailById]`
- `[bud].[jsGetBudgetApprovalFlow]`
- `[bud].[jsGetBudApprovalFlow]`
- `[bud].[CreateBudgetWithSubBudgets]`
- `[bud].[CreateMonthlyAllocations]`
- `[bud].[CreateBudgetAllocationRequest]`

## 9. Frontend Flow

### Main Razor Page

File: `Views/ReportsWeb/ApprovalStatusReport.cshtml`

This page:

- Sets title as `Budget Approval`.
- Loads `~/css/budget.css`.
- Loads SheetJS from CDN for Excel export.
- Creates two tabs:
  - Month-wise report.
  - DocEntry/CardName search.
- Writes server-side values:
  - `companyId = @ViewBag.CompanyId`
  - `userId = @ViewBag.UserId`
- Loads `~/js/budget.js`.

### Main JavaScript

File: `wwwroot/js/budget.js`

Important functions:

| Function | Purpose | API called |
| --- | --- | --- |
| `initializeForm` | Sets dropdowns and default values | Calls `filterData` |
| `getBudgetData(date)` | Gets month-wise budget counts | `/api/auth/GetAllBudgetInsight` |
| `filterData()` | Filters counts by total/approved/pending/rejected | Uses cached data |
| `openModal(id, user)` | Opens user budget details | Calls `switchModalTab` |
| `getSingleData(status, id, data)` | Gets pending/approved/rejected detail | `/api/auth/get{status}budgetwithdetails` |
| `getBudgetFlow(budgetId)` | Gets approval flow | `/api/auth/GetBudgetApprovalFlow` |
| `getExcelBudgetData()` | Gets full export data | `/api/auth/GetAllBudgetSummaryAmount` |
| `getBudgetbyCompany()` | Searches by DocEntry or CardName | `/api/Reports/GetBudgetByCompany` |
| `displayBudget()` | Renders DocEntry/CardName results | Uses search API response |

### Dashboard JavaScript

File: `wwwroot/js/AvtarDashboard.js`

Important API calls:

- `/api/Dashboard/GetUniqueBudgets`
- `/api/Dashboard/GetUniqueBudgets?branch={branch}`
- `/api/Dashboard/getBudgetDataByBranch`
- `/api/Dashboard/getBudgetDataByBranch?branch={branch}`

Note: route attributes in ASP.NET are case-insensitive by default, but the controller method is `[HttpGet("getUniqueBudgets")]`. Keep casing consistent when possible.

## 10. Security

### Authentication

`Program.cs` configures JWT authentication:

- `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`
- `app.UseAuthentication()`
- `app.UseAuthorization()`

### Budget API Protection

Important: `AuthController` and `Auth2Controller` do not have `[Authorize]` at class level in the analyzed code. Budget endpoints also do not show active `[CheckUserPermission]` attributes.

This means access protection may depend on:

- Global middleware not shown in the controller.
- Frontend session routing.
- Reverse proxy/API gateway rules.
- Database stored procedure validation.

Do not assume Budget APIs are protected only because JWT is configured.

### Permission Filter

File: `Filters/CheckUserPermissionAttribute.cs`

This filter:

- Reads `userId` and `companyId` from session.
- Calls `IPermissionService.CheckUserPermissionAsync`.
- Blocks the request if the session is missing or permission is false.

However, no active Budget-specific usage of this filter was found on Budget endpoints.

### Company Validation

Some APIs validate company:

- `Auth2Controller.GetAllBudgets` checks `company > 0`.
- `ReportsController.GetBudgetByCompany` checks `company > 0`.

Many `AuthController` Budget endpoints accept `company` and pass it directly to stored procedures.

### Approval Ownership Validation

Approval ownership appears to be enforced in SQL stored procedures:

- `[bud].[jsApproveBudget]`
- `[bud].[jsRejectBudget]`
- `[bud].[jsApproveBudgetAllocationRequest]`
- `[bud].[jsRejectBudgetAllocationRequest]`

C# passes `userId`, `company`, and `docId`/`flowId`; it does not check approver ownership itself.

## 11. Debugging Guide

### If Budget Counts Are Not Loading

Check in this order:

1. Browser network call:
   - `GET /api/auth/GetAllBudgetInsight?company={companyId}&month={MM-yyyy}`
2. Frontend:
   - `wwwroot/js/budget.js`
   - `getBudgetData(date)`
   - `filterData()`
3. Controller:
   - `AuthController.GetBudgetInsight`
4. Service:
   - `UserService.GetBudgetInsightAsync`
5. Stored procedure:
   - `[bud].[jsGetBudgetInsightAll]`
6. Data issue:
   - Frontend filters `budget.type === 'Budget'`. If SQL returns a different `Type`, rows will disappear.

### If Pending/Approved/Rejected Modal Is Empty

Check:

| Status | API | Stored procedure |
| --- | --- | --- |
| Pending | `/api/auth/getpendingbudgetwithdetails` | `[bud].[jsGetPendingBudgets]` |
| Approved | `/api/auth/getapprovedbudgetwithdetails` | `[bud].[jsGetApprovedBudgets]` |
| Rejected | `/api/auth/getrejectedbudgetwithdetails` | `[bud].[jsGetRejectedBudgets]` |

Also check:

- `userId`
- `company`
- `month`
- `budgetId`
- `UserService.GetNextApproverAsync`
- `[bud].[jsGetNextApprover]`

### If Approval Is Stuck

Check:

1. API:
   - `POST /api/auth/approvebudget`
2. Payload:
   - `docIds`
   - `userId`
   - `company`
   - `remarks`
3. Service:
   - `UserService.ApproveBudgetAsync`
4. Stored procedure:
   - `[bud].[jsApproveBudget]`
5. Flow API:
   - `GET /api/auth/GetBudgetApprovalFlow?budgetId={budgetId}`
6. Flow SP:
   - `[bud].[jsGetBudgetApprovalFlow]`
7. Next approver SP:
   - `[bud].[jsGetNextApprover]`

Common causes:

- `docIds` is empty or not comma-separated correctly.
- `userId` is not the current approver.
- `company` does not match the request company.
- Stored procedure did not move `CurrentStageId`.
- Notification failed, so next approver did not know about the request.

### If Rejection Is Not Working

Check:

1. API spelling:
   - Actual endpoint is `POST /api/auth/rejectebudget`
   - It is misspelled as `rejecte`, not `reject`.
2. Service:
   - `UserService.RejectBudgetAsync`
3. Stored procedure:
   - `[bud].[jsRejectBudget]`
4. Request model:
   - `BudgetRequest`
5. `docId` vs `docIds`:
   - Service uses `docId` if available.
   - If `docId` is null, it converts `docIds` to integer.
   - Unlike approval, rejection is not built for multiple comma-separated ids.

### If Allocation Request Is Not Saving

Check:

1. API:
   - `POST /api/Auth2/createBudgetAllocation`
2. Controller validation:
   - `BudgetAllocationId > 0`
   - `NewAmount > 0`
   - `CreatedBy > 0`
3. Service:
   - `Auth2Service.CreateBudgetAllocationRequestAsync`
4. Stored procedure:
   - `[bud].[CreateBudgetAllocationRequest]`
5. Output params:
   - `@newRequestId`
   - `@message`
   - return value `0` means success.

### If Allocation Approval Is Stuck

Check:

| Action | API | Stored procedure |
| --- | --- | --- |
| Approve | `/api/Auth2/approveBudgetAllocation` | `[bud].[jsApproveBudgetAllocationRequest]` |
| Reject | `/api/Auth2/rejectBudgetAllocation` | `[bud].[jsRejectBudgetAllocationRequest]` |
| Flow | `/api/Auth2/GetBudgetAllocationFlow` | `[bud].[jsGetBudApprovalFlow]` |

Important fields:

- `FlowId`
- `Company`
- `UserId`
- `Remarks`
- `currentStage`
- `totalStage`
- `flowStatus`
- `actionStatus`

### If Budget Balance Is Incorrect

The balance is not calculated in C#. Check SQL first:

- `[bud].[jsGetBudgetCategorySummaryDashboard]`
- `[bud].[jsGetCategoryMonthlyBudget]`
- `[bud].[jsGetBudgetSummary]`
- `[bud].[jsGetUserBudgetAllocation]`
- `bud.jsBudgetTable_vg`

Check these model fields:

- `Current_month_Budget`
- `Current_month_Posted_Amount`
- `totalAmount`
- `usedAmount`
- `rejectedAmount`
- `remaining`
- `usagePercent`

### If Wrong Vendor/Card Is Showing

Check:

1. Search API:
   - `GET /api/Reports/GetBudgetByCompany`
2. Service:
   - `ReportsService.GetBudgetByCompanyAsync`
3. Stored procedure:
   - `[bud].[jsSearchBudgetsByCompany]`
4. Fields:
   - `DocEntry`
   - `CardCode`
   - `CardName`
   - `ObjType`
   - `Company`
   - `month`

### If Dropdowns Are Not Loading

Check:

| Dropdown | API | Service | Stored procedure |
| --- | --- | --- | --- |
| Budget | `/api/auth/getbudgets?company=` | `GetBudgetAsync` | `jsPostSapTables @mode='budget'` |
| Sub-budget | `/api/auth/getSubBudgets?company=` | `GetSubBudgetAsync` | `jsPostSapTables @mode='subBudget'` |
| Category | `/api/auth/BudgetCategoryDropdown` | `BudgetCategoryDropdownAsync` | `[bud].[jsBudgetCategoryDropdown]` |
| Sub-budget category | service method exists | `GetSubBudgetCategoryDropdownAsync` | `[bud].[jsSubBudgetCategoryDropdown]` |
| Auth2 budget types | `/api/Auth2/getAllBudgetTypes` | `GetAllTypeBudgetAsync` | `[bud].[jsGetAllTypeBudget]` |
| Auth2 sub-budget by budget | `/api/Auth2/getSubBudgetByBudget` | `GetSubBudgetByBudgetAsync` | `[bud].[jsGetSubBudgetUsingBudget]` |

### If Notifications Are Not Sent

Check:

1. Approval API:
   - `POST /api/auth/approvebudget`
2. Notification user id SP:
   - `[bud].[jsBudgetNotify]`
3. Service:
   - `UserService.GetUserIdsSendNotificatiosAsync`
4. Token fetch:
   - `NotificationService.GetUserFcmTokenAsync`
5. Push send:
   - `NotificationService.SendPushNotificationAsync`
6. DB notification insert:
   - `NotificationService.InsertNotificationAsync`
7. Page id:
   - Budget approval notifications use `pageId = 6`.

### If Excel Export Is Empty

Check:

1. Frontend function:
   - `getExcelBudgetData()`
2. API:
   - `/api/auth/GetAllBudgetSummaryAmount?company={companyId}&month={MM-yyyy}`
3. Controller:
   - `AuthController.GetAllBudgetSummaryAmount`
4. Service:
   - `GetBudgetInsightAsync`
   - `GetCombinedBudgetsAsync`
5. Stored procedures:
   - `[bud].[jsGetBudgetInsightAll]`
   - `[bud].[jsGetPendingBudgets]`
   - `[bud].[jsGetApprovedBudgets]`
   - `[bud].[jsGetRejectedBudgets]`
   - `[bud].[jsGetBudgetDetailById]`

## 12. Request/Response Examples

### Approve Budget

```http
POST /api/auth/approvebudget
Content-Type: application/json
```

```json
{
  "docIds": "101,102",
  "company": 1,
  "userId": 25,
  "remarks": "Approved"
}
```

Example response:

```json
{
  "success": true,
  "message": "Approved Budget ID 101 | Approved Budget ID 102"
}
```

### Reject Budget

```http
POST /api/auth/rejectebudget
Content-Type: application/json
```

```json
{
  "docId": 101,
  "company": 1,
  "userId": 25,
  "remarks": "Budget amount mismatch"
}
```

Example response:

```json
{
  "success": true,
  "message": "Rejected completed."
}
```

### Create Category Monthly Budget

```http
POST /api/auth/CreateCategoryMonthlyBudget
Content-Type: application/json
```

```json
{
  "budgetCategory": "Marketing",
  "subBudget": "Digital Ads",
  "month": "05-2026",
  "totalAmount": 250000,
  "company": 1
}
```

### Create Budget With Sub-Budgets

```http
POST /api/Auth2/CreateBudgetWithSubBudgets
Content-Type: application/json
```

```json
{
  "company": 1,
  "budgetName": "Sales Promotion",
  "description": "Sales department promotion budget",
  "totalAmount": 1200000,
  "isActive": true,
  "subBudgets": [
    {
      "subBudgetName": "Retail Campaign",
      "description": "Retail outlet campaign spend"
    },
    {
      "subBudgetName": "Distributor Scheme",
      "description": "Distributor incentive scheme"
    }
  ]
}
```

Example response:

```json
{
  "success": true,
  "message": "Budget created successfully",
  "budgetId": 12,
  "budgetName": "Sales Promotion",
  "companyId": 1,
  "subBudgetsCreated": 2
}
```

### Create Monthly Allocation

```http
POST /api/Auth2/CreateMonthlyAllocations
Content-Type: application/json
```

```json
{
  "budgetId": 12,
  "allocationMonth": "2026-05-01",
  "budgetAllocatedAmount": 100000,
  "budgetNotes": "May allocation",
  "subBudgetAllocations": [
    {
      "subBudgetId": 31,
      "allocatedAmount": 60000,
      "notes": "Retail"
    },
    {
      "subBudgetId": 32,
      "allocatedAmount": 40000,
      "notes": "Distributor"
    }
  ]
}
```

### Create Allocation Change Request

```http
POST /api/Auth2/createBudgetAllocation
Content-Type: application/json
```

```json
{
  "budgetAllocationId": 501,
  "newAmount": 125000,
  "createdBy": 25
}
```

Example response:

```json
{
  "success": true,
  "newRequestId": 77,
  "message": "Budget allocation request created"
}
```

### Approve Allocation Change

```http
POST /api/Auth2/approveBudgetAllocation
Content-Type: application/json
```

```json
{
  "flowId": 9001,
  "company": 1,
  "userId": 30,
  "remarks": "Approved allocation increase"
}
```

Example response:

```json
{
  "success": true,
  "data": {
    "success": true,
    "resultMessage": "Approved",
    "budgetAllocationRequestId": 77,
    "companyId": 1,
    "flowId": 9001
  }
}
```

### Pending Budget Response

```json
{
  "success": true,
  "data": [
    {
      "budgetId": 101,
      "budget": "Marketing",
      "objType": 18,
      "company": "1",
      "docEntry": "45012",
      "objectName": "Purchase Invoice",
      "cardCode": "V1001",
      "cardName": "ABC Vendor",
      "docDate": "2026-05-01",
      "totalAmount": "50000",
      "status": "Pending",
      "nextApprover": [
        {
          "userId": 30,
          "loginUser": "finance.manager"
        }
      ]
    }
  ]
}
```

### Budget Flow Response

```json
{
  "success": true,
  "data": [
    {
      "stageId": 1,
      "stageName": "Manager Approval",
      "priority": 1,
      "assignedTo": "manager.user",
      "actionStatus": "A",
      "actionDate": "2026-05-02T10:30:00",
      "description": "Department manager approval",
      "approvalRequired": 1,
      "rejectionRequired": 0
    },
    {
      "stageId": 2,
      "stageName": "Finance Approval",
      "priority": 2,
      "assignedTo": "finance.user",
      "actionStatus": "P",
      "actionDate": null,
      "description": "Finance approval",
      "approvalRequired": 1,
      "rejectionRequired": 0
    }
  ]
}
```

## 13. Important Notes

## 12A. Complete Budget API JSON Catalog

This section documents the Budget-related APIs with HTTP method, URL, query/body shape, and example response format. Most endpoints return a wrapper like:

```json
{
  "success": true,
  "data": []
}
```

Some endpoints use `Success` with capital `S`, and some older endpoints use `message` instead of `Message`. The examples below preserve the observed controller style.

### Legacy Budget Approval APIs - `/api/auth`

#### Get Budget Dropdown

```http
GET /api/auth/getbudgets?company=1
```

Query parameters:

| Name | Type | Required | Example |
| --- | --- | --- | --- |
| `company` | int | Yes | `1` |

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": "BackOff"
    }
  ]
}
```

#### Get Sub-Budget Dropdown

```http
GET /api/auth/getSubBudgets?company=1
```

Query parameters:

| Name | Type | Required | Example |
| --- | --- | --- | --- |
| `company` | int | Yes | `1` |

Example response:

```json
{
  "success": true,
  "data": [
    {
      "sBudgetId": "Admin"
    }
  ]
}
```

#### Get Budget Status Count

```http
GET /api/auth/budgetstatusCount?userId=68&company=1&month=07-2025
GET /api/auth/budgetstatusCount2?userId=68&company=1&month=07-2025
```

Query parameters:

| Name | Type | Required | Example |
| --- | --- | --- | --- |
| `userId` | int | Yes | `68` |
| `company` | int | Yes | `1` |
| `month` | string | Yes | `07-2025` |

Example response:

```json
{
  "success": true,
  "data": [
    {
      "type": "Budget",
      "pending": 10,
      "approved": 25,
      "rejected": 2,
      "total": 37
    }
  ]
}
```

#### Get Pending Budgets

```http
GET /api/auth/getpendingbudgetwithdetails?userId=68&company=1&month=07-2025
GET /api/auth/getpendingbudgetwithdetails2?userId=68&company=1&month=07-2025
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": 4525,
      "budget": "BackOff",
      "objType": 28,
      "company": "OIL",
      "docEntry": "2543",
      "objectName": "Document",
      "cardCode": "V1001",
      "cardName": "Vendor Name",
      "docDate": "2025-07-15",
      "totalAmount": "3500.00",
      "status": "Pending",
      "nextApprover": [
        {
          "userId": 68,
          "loginUser": "approver.user"
        }
      ]
    }
  ]
}
```

#### Get Approved Budgets

```http
GET /api/auth/getapprovedbudgetwithdetails?userId=68&company=1&month=07-2025
GET /api/auth/getapprovedbudgetwithdetails2?userId=68&company=1&month=07-2025
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": 4096,
      "objType": 28,
      "company": "OIL",
      "docEntry": "21333",
      "objectName": "Document",
      "cardCode": "V1001",
      "cardName": "Vendor Name",
      "docDate": "2025-07-15",
      "totalAmount": "4756.00",
      "status": "Approved",
      "nextApprover": []
    }
  ]
}
```

#### Get Rejected Budgets

```http
GET /api/auth/getrejectedbudgetwithdetails?userId=68&company=1&month=07-2025
GET /api/auth/getrejectedbudgetwithdetails2?userId=68&company=1&month=07-2025
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": 2261,
      "objType": 28,
      "company": "OIL",
      "docEntry": "1673",
      "objectName": "Document",
      "cardCode": "V1001",
      "cardName": "Vendor Name",
      "docDate": "2025-07-11",
      "totalAmount": "31595.16",
      "rejectionStatus": "R",
      "rejectedOn": "2025-05-31T17:24:12",
      "description": "Rejected",
      "status": "Rejected",
      "nextApprover": []
    }
  ]
}
```

#### Get All Budget Details

```http
GET /api/auth/getallbudgetwithdetails?userId=68&company=1&month=07-2025
GET /api/auth/getallbudgetwithdetails2?userId=68&company=1&month=07-2025
```

Returns pending, approved, and rejected budget rows together.

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": 4096,
      "objType": 28,
      "company": "OIL",
      "docEntry": "21333",
      "objectName": "Document",
      "cardCode": "V1001",
      "cardName": "Vendor Name",
      "docDate": "2025-07-15",
      "totalAmount": "4756.00",
      "status": "Approved",
      "nextApprover": []
    }
  ]
}
```

#### Approve Budget

```http
POST /api/auth/approvebudget
Content-Type: application/json
```

Request body:

```json
{
  "docIds": "4096,4159",
  "company": 1,
  "userId": 68,
  "remarks": "Approved"
}
```

Example response:

```json
{
  "success": true,
  "message": "Approved Budget ID 4096 | Approved Budget ID 4159"
}
```

#### Reject Budget

```http
POST /api/auth/rejectebudget
Content-Type: application/json
```

Request body:

```json
{
  "docId": 2261,
  "docIds": "2261",
  "company": 1,
  "userId": 68,
  "remarks": "Rejected due to budget mismatch"
}
```

Example response:

```json
{
  "success": true,
  "message": "Rejected completed."
}
```

#### Create Category Monthly Budget

```http
POST /api/auth/CreateCategoryMonthlyBudget
Content-Type: application/json
```

Request body:

```json
{
  "budgetCategory": "BackOff",
  "subBudget": "Admin",
  "month": "07-2025",
  "totalAmount": 6500000,
  "company": 1
}
```

Example response:

```json
{
  "success": true,
  "message": "Budget created successfully"
}
```

#### Get Budget Detail By Id

```http
GET /api/auth/GetBudgetDetailById?budgetId=4096
GET /api/auth/GetBudgetDetailByIdv2?budgetId=4096&company=1
```

Example response:

```json
{
  "success": true,
  "data": {
    "budgetHeader": {
      "budgetId": 4096,
      "docEntry": 21333,
      "cardCode": "V1001",
      "cardName": "Vendor Name",
      "totalAmount": 4756
    },
    "budgetLines": [
      {
        "docEntry": 21333,
        "lineNum": 0,
        "visOrder": 0,
        "budget": "BackOff",
        "subBudget": "Admin",
        "requestAmount": 1000,
        "currentMonthBudget": 6500000,
        "postedAmount": 653008.58
      }
    ],
    "attachments": [
      {
        "docEntry": 21333,
        "fileName": "invoice.pdf",
        "fileExt": "pdf",
        "downloadUrl": "http://files.jivo.in:8000/files/invoice.pdf"
      }
    ]
  }
}
```

#### Get Category Monthly Budget

```http
GET /api/auth/GetCategoryMonthlyBudget?budgetCategory=BackOff&subBudget=Admin&month=07-2025&company=1
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budget": "BackOff",
      "subBudget": "Admin",
      "month": "07-2025",
      "totalAmount": "6500000.00",
      "usedAmount": "653008.58",
      "rejectedAmount": "59723.16",
      "remaining": "5846991.42"
    }
  ]
}
```

#### Budget Category Dropdown

```http
GET /api/auth/BudgetCategoryDropdown?userId=68&company=1
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": "BackOff"
    }
  ]
}
```

#### Budget Approval Flow

```http
GET /api/auth/GetBudgetApprovalFlow?budgetId=4096
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "stageId": 217,
      "stageName": "Manager Approval",
      "priority": 1,
      "assignedTo": "Ravinder Chadda",
      "actionStatus": "A",
      "actionDate": "2025-07-15T11:39:20",
      "description": "Approved",
      "approvalRequired": 1,
      "rejectionRequired": 0
    }
  ]
}
```

#### Get All Budget Insight

```http
GET /api/auth/GetAllBudgetInsight?company=1&month=07-2025
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "userId": 68,
      "userName": "Approver Name",
      "type": "Budget",
      "pending": 2,
      "approved": 35,
      "rejected": 2,
      "total": 39
    }
  ]
}
```

#### Send Pending Budget Notification

```http
GET /api/auth/sendpendingbudgetnotify
```

Example response:

```json
{
  "success": true,
  "message": "Notifications sent successfully"
}
```

#### Get Budget Summary

```http
GET /api/auth/GetBudgetSummary?userId=68&budgetCategory=BackOff&subBudget=Admin&month=07-2025&company=1
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "totalBudget": "6500000.00",
      "approvedAmount": "653008.58",
      "rejectedAmount": "59723.16",
      "pendingAmount": "4696.00",
      "availableBalance": 5846991.42,
      "approvedPercentage": 10.05,
      "pendingPercentage": 0.07,
      "availablePercentage": 89.95
    }
  ]
}
```

#### Get Budget Summary Amount

```http
GET /api/auth/GetBudgetSummaryAmount?userId=68&company=1&month=07-2025
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": 4096,
      "objType": 28,
      "company": "OIL",
      "docEntry": "21333",
      "objectName": "Document",
      "cardCode": "V1001",
      "cardName": "Vendor Name",
      "docDate": "2025-07-15",
      "totalAmount": "4756.00",
      "status": "Approved",
      "header": {},
      "lines": []
    }
  ]
}
```

#### Get All Budget Summary Amount

```http
GET /api/auth/GetAllBudgetSummaryAmount?company=1&month=07-2025
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "userId": 68,
      "userName": "Approver Name",
      "budgetData": [],
      "budgetDetails": []
    }
  ]
}
```

#### Get DocEntry Helpers

```http
GET /api/auth/GetDocIdsUsingDocEntry?docEntry=21333
GET /api/auth/GetApprovedDocEntries?company=1&docEntry=21333
GET /api/auth/GetPendingDocEntries?company=1&docEntry=21333
GET /api/auth/GetRejectedDocEntries?company=1&docEntry=21333
GET /api/auth/GetAllDocEntries?company=1&docEntry=21333
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": 4096,
      "docEntry": 21333,
      "objectType": 28,
      "branch": "Branch",
      "objectName": "Document",
      "cardCode": "V1001",
      "cardName": "Vendor Name",
      "budget": "BackOff",
      "subBudget": "Admin",
      "requestAmount": 4756,
      "postedAmount": 653008.58,
      "currentMonthBudget": 6500000,
      "approvalStatus": "A"
    }
  ]
}
```

### Auth2 Budget Master and Allocation APIs - `/api/Auth2`

#### Create Budget With Sub-Budgets

```http
POST /api/Auth2/CreateBudgetWithSubBudgets
Content-Type: application/json
```

Request body:

```json
{
  "company": 1,
  "budgetName": "Sales Promotion",
  "description": "Sales department promotion budget",
  "totalAmount": 1200000,
  "isActive": true,
  "subBudgets": [
    {
      "subBudgetName": "Retail Campaign",
      "description": "Retail outlet campaign spend"
    },
    {
      "subBudgetName": "Distributor Scheme",
      "description": "Distributor incentive scheme"
    }
  ]
}
```

Example response:

```json
{
  "success": true,
  "message": "Budget created successfully",
  "budgetId": 12,
  "budgetName": "Sales Promotion",
  "companyId": 1,
  "subBudgetsCreated": 2
}
```

#### Create Monthly Allocations

```http
POST /api/Auth2/CreateMonthlyAllocations
Content-Type: application/json
```

Request body:

```json
{
  "budgetId": 12,
  "allocationMonth": "2026-05-01",
  "budgetAllocatedAmount": 100000,
  "budgetNotes": "May allocation",
  "subBudgetAllocations": [
    {
      "subBudgetId": 31,
      "allocatedAmount": 60000,
      "notes": "Retail"
    },
    {
      "subBudgetId": 32,
      "allocatedAmount": 40000,
      "notes": "Distributor"
    }
  ]
}
```

Example response:

```json
{
  "success": true,
  "message": "Monthly allocations created successfully",
  "budgetId": 12,
  "allocationMonth": "2026-05-01T00:00:00",
  "budgetAllocatedAmount": 100000,
  "subBudgetAllocationsCreated": 2,
  "totalSubBudgetAmount": 100000
}
```

#### Update Monthly Allocations

```http
POST /api/Auth2/UpdateMonthlyAllocations
Content-Type: application/json
```

Request body:

```json
{
  "budgetId": 12,
  "allocationMonth": "2026-05-01",
  "budgetAllocatedAmount": 125000,
  "budgetNotes": "Updated May allocation",
  "updateBudgetNotes": true,
  "subBudgetAllocations": [
    {
      "subBudgetId": 31,
      "allocatedAmount": 70000,
      "notes": "Updated Retail"
    },
    {
      "subBudgetId": 32,
      "allocatedAmount": 55000,
      "notes": "Updated Distributor"
    }
  ]
}
```

Example response:

```json
{
  "success": true,
  "message": "Monthly allocations updated successfully",
  "budgetId": 12,
  "allocationMonth": "2026-05-01T00:00:00",
  "budgetAllocationUpdated": true,
  "subBudgetAllocationsUpdated": 2
}
```

#### Get All Budgets

```http
GET /api/Auth2/GetAllBudgets?company=1&isActive=true
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budgetId": 12,
      "company": 1,
      "budgetName": "Sales Promotion",
      "description": "Sales department promotion budget",
      "totalAmount": 1200000,
      "isActive": true,
      "createdAt": "2026-05-01T10:00:00",
      "updatedAt": null,
      "totalSubBudgets": 2
    }
  ]
}
```

#### Get Budget And Sub-Budget Details

```http
GET /api/Auth2/GetBudgetAndSubBudgetDetails?budgetId=12&subBudgetId=31
```

Example response:

```json
{
  "success": true,
  "data": {
    "success": true,
    "message": "Data retrieved successfully",
    "budgetInfo": {
      "budgetId": 12,
      "company": 1,
      "budgetName": "Sales Promotion",
      "budgetDescription": "Sales department promotion budget",
      "totalAmount": 1200000,
      "budgetIsActive": true,
      "totalSubBudgets": 2,
      "totalMonthlyAllocations": 1,
      "totalAllocatedAmount": 100000
    },
    "subBudgetInfo": {
      "subBudgetId": 31,
      "budgetId": 12,
      "subBudgetName": "Retail Campaign",
      "subBudgetIsActive": true,
      "parentBudgetName": "Sales Promotion",
      "totalMonthlyAllocations": 1,
      "totalAllocatedAmount": 60000
    },
    "monthlyComparison": [
      {
        "allocationMonth": "2026-05",
        "budgetAllocatedAmount": 100000,
        "budgetNotes": "May allocation",
        "subBudgetAllocatedAmount": 60000,
        "subBudgetNotes": "Retail",
        "allocationStatus": "Allocated"
      }
    ]
  }
}
```

#### Get Budget With Sub-Budgets

```http
GET /api/Auth2/GetBudgetWithSubBudgets?budgetId=12
```

Example response:

```json
{
  "success": true,
  "message": "Data successfully retrieve",
  "data": {
    "budgets": {
      "budgetId": 12,
      "company": 1,
      "budgetName": "Sales Promotion",
      "description": "Sales department promotion budget",
      "totalAmount": 1200000,
      "isActive": true,
      "totalSubBudgets": 2
    },
    "subBudgets": [
      {
        "subBudgetId": 31,
        "budgetId": 12,
        "subBudgetName": "Retail Campaign",
        "description": "Retail outlet campaign spend",
        "isActive": true
      }
    ]
  }
}
```

#### Get Distinct Budget Attributes

```http
GET /api/Auth2/GetDistinctBudgetAttributes?mode=BUDGET
```

Accepted `mode` values:

```text
BUDGET, SUB_BUDGET, BRANCH, PROCESSTAT, OBJTYPE, ACCTCODE, BUDGETDATE
```

Example response:

```json
{
  "success": true,
  "message": "Data successfully retrieve",
  "data": [
    "BackOff",
    "Sales",
    "Factory"
  ]
}
```

#### Get Sub-Budgets By Budget Id

```http
GET /api/Auth2/GetSubBudgetsByBudgetId?budgetId=12&isActive=true
```

Example response:

```json
{
  "success": true,
  "message": "Data successfully retrieve",
  "totalSubBudgets": 2,
  "data": [
    {
      "subBudgetId": 31,
      "budgetId": 12,
      "subBudgetName": "Retail Campaign",
      "description": "Retail outlet campaign spend",
      "isActive": true
    }
  ]
}
```

#### Get Budget Monthly Allocation View

```http
GET /api/Auth2/GetBudgetMonthlyAllocationView?budgetName=Sales%20Promotion&allocationMonth=2026-05-01
```

Example response:

```json
{
  "success": true,
  "data": {
    "budget": {
      "budgetId": 12,
      "budgetName": "Sales Promotion",
      "allocationMonth": "2026-05-01",
      "allocatedAmount": 100000,
      "notes": "May allocation"
    },
    "subBudgets": [
      {
        "subBudgetId": 31,
        "subBudgetName": "Retail Campaign",
        "allocatedAmount": 60000,
        "notes": "Retail"
      }
    ]
  }
}
```

#### Get Budget Types And Sub-Budgets

```http
GET /api/Auth2/getAllBudgetTypes
GET /api/Auth2/getSubBudgetByBudget?budget=BackOff
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "budget": "BackOff",
      "subBudget": "Admin"
    }
  ]
}
```

#### Create Allocation Change Request

```http
POST /api/Auth2/createBudgetAllocation
Content-Type: application/json
```

Request body:

```json
{
  "budgetAllocationId": 501,
  "newAmount": 125000,
  "createdBy": 68
}
```

Example response:

```json
{
  "success": true,
  "data": {
    "success": true,
    "newRequestId": 77,
    "message": "Budget allocation request created"
  }
}
```

#### Get Allocation Requests

```http
GET /api/Auth2/GetPendingBudgetAllocationRequests?userId=68&companyId=1&month=05-2026
GET /api/Auth2/GetApprovedBudgetAllocationRequests?userId=68&companyId=1&month=05-2026
GET /api/Auth2/GetRejectedBudgetAllocationRequests?userId=68&companyId=1&month=05-2026
GET /api/Auth2/GetAllBudgetAllocationRequests?userId=68&companyId=1&month=05-2026
```

Example response:

```json
{
  "success": true,
  "totalCount": 1,
  "data": [
    {
      "id": 77,
      "companyId": 1,
      "budgetId": 12,
      "budgetName": "Sales Promotion",
      "allocationId": 501,
      "allocationMonth": "2026-05-01",
      "currentAmount": 100000,
      "requestedAmount": 125000,
      "amountDifference": 25000,
      "createdById": 68,
      "createdBy": "request.user",
      "flowId": 9001,
      "flowStatus": "P",
      "currentStage": 1,
      "totalStage": 2
    }
  ]
}
```

#### Approve Allocation Change

```http
POST /api/Auth2/approveBudgetAllocation
Content-Type: application/json
```

Request body:

```json
{
  "flowId": 9001,
  "company": 1,
  "userId": 68,
  "remarks": "Approved allocation change"
}
```

Example response:

```json
{
  "success": true,
  "data": {
    "success": true,
    "resultMessage": "Approved",
    "budgetAllocationRequestId": 77,
    "companyId": 1,
    "flowId": 9001
  }
}
```

#### Reject Allocation Change

```http
POST /api/Auth2/rejectBudgetAllocation
Content-Type: application/json
```

Request body:

```json
{
  "flowId": 9001,
  "company": 1,
  "userId": 68,
  "remarks": "Rejected allocation change"
}
```

Example response:

```json
{
  "success": true,
  "data": {
    "success": true,
    "resultMessage": "Rejected",
    "budgetAllocationRequestId": 77,
    "companyId": 1,
    "flowId": 9001
  }
}
```

#### Get Allocation Request Detail

```http
GET /api/Auth2/GetBudgetAllocationRequestDetail?requestId=77
```

Example response:

```json
{
  "success": true,
  "data": {
    "requestDetail": {
      "id": 77,
      "companyId": 1,
      "budgetId": 12,
      "budgetName": "Sales Promotion",
      "allocationId": 501,
      "allocationMonth": "2026-05-01",
      "currentAmount": 100000,
      "requestedAmount": 125000,
      "amountDifference": 25000,
      "createdById": 68
    },
    "flow": []
  }
}
```

#### Get Monthly Allocation Insights

```http
GET /api/Auth2/getMonthlyAllocationInsights?userId=68&company=1&month=05-2026
```

Example response:

```json
{
  "success": true,
  "data": {
    "total": 10,
    "pending": 3,
    "approved": 6,
    "rejected": 1
  }
}
```

#### Get Budget Allocation Flow

```http
GET /api/Auth2/GetBudgetAllocationFlow?flowId=9001
```

Example response:

```json
{
  "success": true,
  "totalCount": 2,
  "data": [
    {
      "stageId": 1,
      "stageName": "Manager Approval",
      "priority": 1,
      "assignedTo": "manager.user",
      "actionStatus": "A",
      "actionDate": "2026-05-02T10:30:00",
      "description": "Approved",
      "approvalRequired": 1,
      "rejectRequired": 0
    }
  ]
}
```

### Report and Dashboard Budget APIs

#### Search Budget By Company

```http
GET /api/Reports/GetBudgetByCompany?company=1&docEntry=21333&cardName=Vendor&month=07-2025&status=Approved
```

Query parameters:

| Name | Type | Required | Example |
| --- | --- | --- | --- |
| `company` | int | Yes | `1` |
| `docEntry` | int | No | `21333` |
| `cardName` | string | No | `Vendor` |
| `month` | string | No | `07-2025` |
| `status` | string | No | `Approved` |

Example response:

```json
{
  "success": true,
  "data": {
    "budgets": [
      {
        "budgetId": 4096,
        "objType": 28,
        "company": "OIL",
        "companyId": 1,
        "docEntry": 21333,
        "objectName": "Document",
        "cardCode": "V1001",
        "cardName": "Vendor Name",
        "docDate": "2025-07-15T00:00:00",
        "totalAmount": 4756,
        "currentMonth": "07-2025",
        "budgetOwner": "Ravinder Chadda",
        "ownerCode": "JWPL0011",
        "approverName": "Approver Name",
        "approvalCode": "A"
      }
    ]
  }
}
```

#### Dashboard Budget Dropdowns And Data

```http
GET /api/Dashboard/getUniqueBudgets
GET /api/Dashboard/getUniqueBudgets?branch=Mumbai
GET /api/Dashboard/getBudgetDataByBranch
GET /api/Dashboard/getBudgetDataByBranch?branch=Mumbai
GET /api/Dashboard/getUniqueAccounts?branch=Mumbai
```

Example response:

```json
{
  "success": true,
  "data": [
    {
      "branch": "Mumbai",
      "docEntry": 21333,
      "objectName": "Document",
      "objType": 28,
      "lineNum": 0,
      "visOrder": 0,
      "acctCode": "600001",
      "acctName": "Expense Account",
      "cardCode": "V1001",
      "cardName": "Vendor Name",
      "effectMonth": "07-2025",
      "budget": "BackOff",
      "subBudget": "Admin",
      "state": "MH",
      "amount": 4756,
      "currentMonth": "07-2025",
      "currentMonthPostedAmount": 653008.58,
      "budgetOwner": "Ravinder Chadda",
      "ownerCode": "JWPL0011",
      "currentMonthBudget": 6500000,
      "status": "Approved"
    }
  ]
}
```

## 12B. Budget Summary API Documentation

### Overview

Endpoint:

```http
GET /api/auth/GetBudgetSummary
```

Purpose:

Returns budget summary information for a specific user, budget, sub-budget, month, and company.

Query parameters:

| Name | Type | Required | Example |
| --- | --- | --- | --- |
| `userId` | int | Yes | `68` |
| `budgetCategory` | string | Yes | `BackOff` |
| `subBudget` | string | No | `Admin` |
| `month` | string | Yes | `07-2025` |
| `company` | int | Yes | `1` |

Response fields:

| Field | Meaning |
| --- | --- |
| `totalBudget` | Budget amount allocated for the selected month. |
| `approvedAmount` | Amount approved by the selected user. |
| `rejectedAmount` | Amount rejected by the selected user. |
| `pendingAmount` | Amount pending action for the selected user. |
| `availableBalance` | Calculated in `UserService` as `totalBudget - approvedAmount`, floored at zero. |
| `approvedPercentage` | Calculated in `UserService` when `totalBudget > 0`. |
| `pendingPercentage` | Calculated in `UserService` when `totalBudget > 0`. |
| `availablePercentage` | Calculated in `UserService` when `totalBudget > 0`. |

### Architecture Change

Previous behavior:

```text
TotalBudget came only from bud.jsBudgetCategoryMonthSummary.
```

Problem:

Many workflow records exist without matching rows in `bud.jsBudgetCategoryMonthSummary`. This caused cards like:

```json
{
  "totalBudget": "0.00",
  "approvedAmount": "653008.58",
  "rejectedAmount": "59723.16",
  "pendingAmount": "4696.00"
}
```

### New TotalBudget Resolution Logic

The procedure now uses a three-level fallback chain.

| Level | Source | When used | Purpose |
| --- | --- | --- | --- |
| 1 | `bud.jsBudgetCategoryMonthSummary` | Matching summary row exists | Supports legacy summary data. |
| 2 | `bud.BudgetMonthlyAllocations`, `bud.SubBudgetMonthlyAllocations` | No summary row exists | Supports Auth2 allocation architecture. |
| 3 | `bud.jsBudgetTable.Current_month_Budget` | No summary row and no Auth2 allocation | Supports historical workflow-only records. |

Level 3 selection rule:

```sql
ORDER BY
    COALESCE(UpdateDate, CreatedDate, DocDate, budgetDate) DESC,
    DocEntry DESC,
    LineNum DESC,
    VisOrder DESC
```

Only non-zero, non-null `Current_month_Budget` snapshots are considered.

### Sample API Calls

#### Level 3 Historical Workflow Fallback

```http
GET /api/auth/GetBudgetSummary?userId=68&budgetCategory=BackOff&subBudget=Admin&month=07-2025&company=1
```

Expected response:

```json
{
  "success": true,
  "data": [
    {
      "totalBudget": "6500000.00",
      "approvedAmount": "653008.58",
      "rejectedAmount": "59723.16",
      "pendingAmount": "4696.00",
      "availableBalance": 5846991.42,
      "approvedPercentage": 10.05,
      "pendingPercentage": 0.07,
      "availablePercentage": 89.95
    }
  ]
}
```

Source:

```text
Level 3: bud.jsBudgetTable.Current_month_Budget
```

#### Level 2 Auth2 Allocation

```http
GET /api/auth/GetBudgetSummary?userId=68&budgetCategory=BackOff&subBudget=Admin&month=12-2025&company=1
```

Expected response:

```json
{
  "success": true,
  "data": [
    {
      "totalBudget": "800000.00",
      "approvedAmount": "0.00",
      "rejectedAmount": "0.00",
      "pendingAmount": "0.00",
      "availableBalance": 800000,
      "approvedPercentage": 0,
      "pendingPercentage": 0,
      "availablePercentage": 100
    }
  ]
}
```

Source:

```text
Level 2: bud.SubBudgetMonthlyAllocations
```

#### Level 3 Interest Example

```http
GET /api/auth/GetBudgetSummary?userId=77&budgetCategory=Interest&subBudget=CC%20Limit&month=05-2025&company=1
```

Expected response:

```json
{
  "success": true,
  "data": [
    {
      "totalBudget": "2500000.00",
      "approvedAmount": "2747855.00",
      "rejectedAmount": "0.00",
      "pendingAmount": "0.00",
      "availableBalance": 0,
      "approvedPercentage": 109.91,
      "pendingPercentage": 0,
      "availablePercentage": 0
    }
  ]
}
```

Source:

```text
Level 3: bud.jsBudgetTable.Current_month_Budget
```

### SQL Validation Queries

```sql
EXEC bud.jsGetBudgetSummary
    @userId = 68,
    @budgetCategory = 'BackOff',
    @subBudget = 'Admin',
    @month = '07-2025',
    @company = 1;
```

Expected:

```text
TotalBudget = 6500000.00
ApprovedAmount = 653008.58
RejectedAmount = 59723.16
PendingAmount = 4696.00
```

```sql
EXEC bud.jsGetBudgetSummary
    @userId = 68,
    @budgetCategory = 'BackOff',
    @subBudget = 'Admin',
    @month = '12-2025',
    @company = 1;
```

Expected:

```text
TotalBudget = 800000.00
```

```sql
EXEC bud.jsGetBudgetSummary
    @userId = 77,
    @budgetCategory = 'Interest',
    @subBudget = 'CC Limit',
    @month = '05-2025',
    @company = 1;
```

Expected:

```text
TotalBudget = 2500000.00
ApprovedAmount = 2747855.00
```

### Troubleshooting

If `TotalBudget = 0`, check in this order:

1. `bud.jsBudgetCategoryMonthSummary`
2. `bud.BudgetMonthlyAllocations`
3. `bud.SubBudgetMonthlyAllocations`
4. `bud.jsBudgetTable.Current_month_Budget`

If `ApprovedAmount`, `RejectedAmount`, or `PendingAmount` is non-zero while `TotalBudget` is zero, the likely cause is missing summary/allocation records with no usable historical snapshot.

### Impact

No API contract changed. No controller, service, DTO, frontend, Flutter, Auth2 workflow, approval workflow, SAP, HANA, notification, or export API changes are required. Only `TotalBudget` source resolution changed inside `[bud].[jsGetBudgetSummary]`.

### Non-Obvious Implementation Details

- There is no dedicated `BudgetController.cs`, `IBudgetService`, or `BudgetService.cs`.
- Main Budget approval APIs are under `/api/auth`.
- Newer master/allocation APIs are under `/api/Auth2`.
- Budget rejection endpoint is misspelled: `/api/auth/rejectebudget`.
- Budget list status is normalized in C# as `Pending`, `Approved`, and `Rejected`.
- Approval flow status uses compact values like `A` and `R`, and frontend treats anything else as pending.
- The frontend filters budget insight rows by `type === 'Budget'`.
- Excel export uses `GetAllBudgetSummaryAmount`, which internally loops users and calls multiple stored procedures. This can be expensive.
- `GetBudgetDetailByIdv2` hardcodes attachment URLs using `http://files.jivo.in:8000/files/...`.

### Risky Areas

| Area | Risk |
| --- | --- |
| Approval ownership | C# does not validate ownership; it trusts stored procedures. |
| Multi-approval | `ApproveBudgetAsync` supports comma-separated `docIds`; reject does not safely support multiple comma-separated ids. |
| Notification duplication | Service deduplicates user ids and FCM tokens, but bad data from `[bud].[jsBudgetNotify]` can still cause missing notifications. |
| Month format | Most legacy Budget APIs expect `MM-yyyy`, while Auth2 monthly allocation uses `DateTime`, usually first day of month. |
| Performance | `GetAllBudgetSummaryAmount` loops every user and fetches combined details. |
| Security | Budget endpoints do not show active `[Authorize]` or `[CheckUserPermission]` attributes. |
| Allocation math | C# does not enforce over-budget rules; SQL must be correct. |
| Dashboard SQL | `DashboardService` reads direct SQL view `bud.jsBudgetTable_vg`, not stored procedures. |

### Business Rules to Protect

- Always pass correct `company`.
- Always use correct month format:
  - Legacy report APIs: `MM-yyyy`
  - Auth2 allocation APIs: date like `2026-05-01`
- Do not approve with a user who is not the current approver.
- Do not bypass allocation request approval when changing monthly amounts.
- Check sub-budget totals against parent monthly allocation in SQL.
- Treat budget approval and allocation approval as separate workflows.

### SAP/HANA Note

`ReportsService` has HANA reads for unrelated reports such as sales analysis, variety, and brand. The Budget code analyzed here uses SQL Server `[bud]` stored procedures and `bud.jsBudgetTable_vg`. No Budget approval or allocation method in the analyzed code calls SAP Service Layer or writes to HANA.
