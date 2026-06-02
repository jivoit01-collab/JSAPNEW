# IMC Module Developer Documentation

## 1. Module Overview

IMC means **Item Master Creation**. In this project, IMC is the backend module used to create SAP Business One item master records only after an approval workflow is completed.

The module solves a common ERP problem: users need to request new items, but those items should not be inserted into SAP until the right approvers check the business data, tax data, item group, UOM, SAP flags, and company-specific rules.

Typical users:

| User type | What they do |
|---|---|
| Creator / maker | Creates a new item request. |
| Approver | Approves or rejects the request stage by stage. |
| Master data / ERP team | Checks SAP fields and fixes failed SAP syncs. |
| IT / backend developer | Debugs API, approval, SQL, SAP, and notification issues. |

After item creation, the record is stored in SQL Server as an IMC request. When the request reaches final approval, the backend builds an SAP Service Layer payload and posts it to SAP `Items`. If the primary SAP insertion succeeds, selected finished goods from Oil or Beverages can also be synced to MART.

## 2. Module Architecture

Main backend files:

| Layer | File | Responsibility |
|---|---|---|
| API controller | `Controllers/ItemMasterController.cs` | Exposes IMC APIs under `/api/ItemMaster`. |
| Service interface | `Services/Interfaces/IItemMasterService.cs` | Defines IMC service methods. |
| Service implementation | `Services/Implementation/ItemMasterService.cs` | Handles SQL Server, HANA, SAP Service Layer, approval, notifications, and error logging. |
| Models | `Models/ItemMasterModel.cs` | Request/response DTOs for item data, SAP data, approval data, and SAP payloads. |
| SAP sessions | `Services/Interfaces/IBom2Service.cs`, `Services/Implementation/Bom2Service.cs` | Provides SAP sessions for Oil, Beverages, and MART. |
| Notifications | `Services/Interfaces/INotificationService.cs`, `Services/Implementation/NotificationService.cs` | Sends FCM push notifications and stores notification records. |
| Security filter | `Filters/CheckUserPermissionAttribute.cs` | Session-based permission filter, currently commented out on IMC endpoints. |
| DI and auth setup | `Program.cs` | Registers `IItemMasterService`, JWT auth, session, CORS, and controllers. |
| SAP rules doc | `SAP_ITEM_CREATION_DOCUMENTATION.md` | Existing detailed SAP item-field rule reference. |

Important Razor / JS files:

| File | IMC usage |
|---|---|
| `Views/InventoryAuditWeb/AddSession.cshtml` | Calls `/api/ItemMaster/GetGroup?company=...` for item groups. |
| `Views/InventoryAuditWeb/AllInventory.cshtml` | Calls `/api/ItemMaster/GetGroup?company=...` for item groups. |
| `wwwroot/js/*` | No dedicated IMC JavaScript file was found in this repository. IMC appears to be consumed mostly by API clients or external frontend/mobile apps. |
| `Views/*` | No dedicated Item Master Creation Razor page was found in this repository. |

Architecture diagram:

```text
Frontend / Mobile / API Client
        |
        | HTTP /api/ItemMaster/*
        v
Controllers/ItemMasterController.cs
        |
        | IItemMasterService
        v
Services/Implementation/ItemMasterService.cs
        |
        +--> SQL Server stored procedures in [imc]
        |       - request data
        |       - approval flow
        |       - SAP status/tag
        |       - API error logs
        |
        +--> SAP HANA stored procedures
        |       - dropdown/master data
        |       - existing item names
        |
        +--> IBom2Service SAP session methods
        |       - Oil
        |       - Beverages
        |       - MART
        |
        +--> SAP Service Layer POST /Items
        |
        +--> INotificationService
                - FCM push
                - notification table insert
```

## 3. Complete Workflow

1. User opens the IMC screen in the frontend or mobile app.
2. Frontend loads dropdown data from APIs such as `GetGroup`, `GetBrand`, `GetHSN`, `GetTaxRate`, `GetSKU`, `GetUOMGroup`, and UOM APIs.
3. User fills item business fields and SAP-related fields.
4. Frontend submits the item using `POST /api/ItemMaster/InsertFullItem`.
5. `ItemMasterController.InsertFullItem()` validates that request is not null.
6. `ItemMasterService.InsertFullItemDataAsync()` calls SQL Server stored procedure `[imc].[jsInsertFullItemData]`.
7. SQL Server creates the IMC request, initial item data, SAP data, and approval workflow. The stored procedure returns `NewInitId`.
8. Service calls `[imc].[GetUsersInCurrentStage]` through `GetItemCurrentUsersSendNotificationAsync(newRecordId)`.
9. Service sends push notifications to current approvers using `INotificationService`.
10. Approver opens pending list using `GET /api/ItemMaster/GetPendingItems`.
11. Approver approves using `POST /api/ItemMaster/ApproveItem`.
12. `ApproveItemAsync()` checks whether this is the last approval stage by calling `[imc].[jsGetPendingItemApiInsertions]`.
13. If it is not the last stage, backend only calls `[imc].[jsApproveItem]` and notifies the next approver.
14. If it is the last stage, backend posts the item to SAP before approving in SQL.
15. SAP result is written by `[imc].[jsUpdateItemApiStatus]`.
16. If SAP fails, approval is blocked and `[imc].[jsApproveItem]` is not called.
17. If SAP succeeds, `[imc].[jsApproveItem]` marks approval complete.
18. Final response returns `ApprovalStatus`, `SapStatus`, `MartStatus`, and `Message`.

## 4. Before Approval vs After Approval

### Before Approval

| Area | Behavior |
|---|---|
| Main create API | `POST /api/ItemMaster/InsertFullItem` |
| Legacy split create APIs | `POST /api/ItemMaster/InsertInitData`, `POST /api/ItemMaster/InsertSAPData` |
| SQL stored procedures | `[imc].[jsInsertFullItemData]`, `imc.jsInsertInitData`, `imc.jsInsertSAPData` |
| SQL tables | Not directly visible in C# code. Tables are hidden behind `[imc]` stored procedures. |
| Approval workflow | Created by `[imc].[jsInsertFullItemData]` or related insert procedures. |
| SAP called? | No. Item is only stored as a request. |
| Notifications sent? | Yes. `GetItemCurrentUsersSendNotificationAsync()` calls `[imc].[GetUsersInCurrentStage]`, then `INotificationService` sends FCM notifications. |
| Item active in SAP? | No. |
| Common statuses | Pending items are returned by `[imc].[jsGetPendingItems]`; `GetAllItemsAsync()` labels them as `Pending`. |
| API status tag | Not final. SAP status is only updated during SAP posting through `[imc].[jsUpdateItemApiStatus]`. |

### After Approval

| Area | Behavior |
|---|---|
| Approval API | `POST /api/ItemMaster/ApproveItem` |
| Service method | `ItemMasterService.ApproveItemAsync()` |
| Intermediate stage | Calls `[imc].[jsApproveItem]`; SAP is skipped. |
| Final stage | Calls `[imc].[jsGetPendingItemApiInsertions]`, then `PostItemsToSAPAsync()`, then `[imc].[jsApproveItem]`. |
| SAP method | `PostItemsToSAPAsync()` sends `POST {SapServiceLayer:BaseUrl}/Items`. |
| SAP session methods | `GetSAPSessionOilAsync()`, `GetSAPSessionBevAsync()`, `GetSAPSessionMartAsync()` from `IBom2Service`. |
| SQL status update | `[imc].[jsUpdateItemApiStatus] @itemId, @apiMessage, @tag` |
| SAP status tags | `P` = processing, `Y` = SAP success/already created, `N` = SAP failed. One exception path passes `false.ToString()` which writes `False`. |
| Approval status | Controller returns `ApprovalStatus = Done` after `[imc].[jsApproveItem]`. If SAP fails on final stage, returns `ApprovalStatus = Blocked`. |
| Item active in SAP? | Yes only if SAP Service Layer returns success. |
| MART sync | Runs only after primary SAP success, only for company 1 or 2, only if item is finished goods (`GroupCode = 102` or group name contains `FINISHED` / `FG`). |
| Notifications sent? | Yes. Current/next-stage users are notified after approval. |

Important behavior: final approval is **SAP-first, SQL-approval-second**. If SAP creation fails, the approval is blocked so the database workflow does not show a final approval for an item that is missing from SAP.

## 5. API Documentation

Base route: `/api/ItemMaster`

### Create and Edit APIs

| API | Purpose | Runs | Service method | Stored procedure |
|---|---|---|---|---|
| `POST /InsertFullItem` | Creates full item request with init data and SAP data in one call. | Before approval | `InsertFullItemDataAsync()` | `[imc].[jsInsertFullItemData]` |
| `POST /InsertInitData` | Creates only initial/business item data. Older split flow. | Before approval | `InsertInitDataAsync()` | `imc.jsInsertInitData` |
| `POST /InsertSAPData` | Creates SAP field data for an existing init record. Older split flow. | Before approval | `InsertSAPDataAsync()` | `imc.jsInsertSAPData` |
| `POST /UpdateInitData` | Updates business item fields before final processing. | Before approval / correction | `UpdateInitDataAsync()` | `imc.jsUpdateInitData` |
| `POST /UpdateSAPData` | Updates SAP-specific fields before final processing. | Before approval / correction | `UpdateSAPDataAsync()` | `imc.jsUpdateSAPData` |

### Approval APIs

| API | Purpose | Runs | Service method | Stored procedure |
|---|---|---|---|---|
| `POST /ApproveItem` | Approves current stage. On final stage, posts to SAP first. | During approval | `ApproveItemAsync()` | `[imc].[jsApproveItem]` |
| `POST /RejectItem` | Rejects item request. | During approval | `RejectItemAsync()` | `[imc].[jsRejectItem]` |
| `GET /GetIMCApprovalFlow?flowId=...` | Shows approval stages and actions. | Read/debug | `GetIMCApprovalFlowAsync()` | `[imc].[jsGetIMCApprovalFlow]` |
| `GET /GetItemUserIdsSendNotificatios?flowId=...` | Gets users to notify for an IMC flow. | Notification/debug | `GetItemUserIdsSendNotificatiosAsync()` | `[imc].[jsImcNotify]` |
| `GET /GetItemCurrentUsersSendNotification?initID=...` | Gets current stage users after creation. | Notification/debug | `GetItemCurrentUsersSendNotificationAsync()` | `[imc].[GetUsersInCurrentStage]` |
| `GET /SendPendingItemCountNotification` | Sends pending IMC count notifications to active users. | Scheduled/manual notification | `SendPendingItemCountNotificationAsync()` | Uses `[imc].[jsGetWorkflowInsights]` |

### List and Detail APIs

| API | Purpose | Service method | Stored procedure |
|---|---|---|---|
| `GET /GetPendingItems?userId=...&company=...` | Current user's pending item approvals. | `GetPendingItemsAsync()` | `[imc].[jsGetPendingItems]` |
| `GET /GetApprovedItems?userId=...&company=...` | Approved items. | `GetApprovedItemsAsync()` | `[imc].[jsGetApprovedItems]` |
| `GET /GetRejectedItems?userId=...&company=...` | Rejected items. | `GetRejectedItemsAsync()` | `[imc].[jsGetRejectedItems]` |
| `GET /GetAllItems?userId=...&companyId=...` | Merges pending, approved, and rejected lists. | `GetAllItemsAsync()` | Calls pending/approved/rejected SPs |
| `GET /GetFullItemDetails?itemId=...` | Full item details including init and SAP data. | `GetFullItemDetailsAsync()` | `[imc].[jsGetFullItemDetails]` |
| `GET /GetWorkflowInsights?userId=...&companyId=...&month=MM-yyyy` | Counts pending/approved/rejected. | `GetWorkflowInsightsAsync()` | `[imc].[jsGetWorkflowInsights]` |
| `GET /GetItemByUserId?userId=...&company=...&month=...` | User's item list by month. | `GetItemByIdAsync()` | `[imc].[jsGetItemByUserId]` |
| `GET /GetCreatedByDetail?userId=...&companyId=...` | Items created by a user. | `GetCreatedByDetailAsync()` | `[imc].[jsGetCreatedByDetail]` |
| `GET /GetRejectedItemsForCreator?userId=...&company=...` | Rejected items visible to creator. | `GetRejectedItemsForCreatorAsync()` | `[imc].[jsGetRejectedItemsForCreator]` |

### SAP APIs

| API | Purpose | Runs | Service method | Stored procedure / external API |
|---|---|---|---|---|
| `GET /GetPendingItemApiInsertions?ItemId=...` | Reads final-stage item rows ready for SAP payload creation. | Before SAP sync | `GetPendingItemApiInsertionsAsync()` | `[imc].[jsGetPendingItemApiInsertions]` |
| `POST /Items?ItemId=...` | Manually posts pending IMC item to SAP. | After/final approval or retry/manual | `PostItemsToSAPAsync()` | `POST SAP Service Layer /Items`, `[imc].[jsUpdateItemApiStatus]` |
| Internal `LogApiErrorAsync()` | Logs SAP API errors. | On SAP failure | `LogApiErrorAsync()` | `[imc].[LogApiError]` |

### Dropdown / Master Data APIs

These read mostly from SAP HANA using configured `HanaSettings:{ActiveEnvironment}`.

| API | HANA procedure |
|---|---|
| `GET /GetHSN?company=...` | `JsGetHSN()` |
| `GET /GetTaxRate?company=...` | `JsGetTaxRate()` |
| `GET /GetInventoryUOM?company=...` | `JsGetInvntryUom()` |
| `GET /GetPackingType?GroupCode=...&company=...` | `JsGetPackingType(?)` |
| `GET /GetPackType?GroupCode=...&company=...` | `JsGetPackType(?)` |
| `GET /GetPurPackType?company=...` | `JsGetPurPackMsr()` |
| `GET /GetSalPackType?company=...` | `JsGetSalPackMsr()` |
| `GET /GetSalUnitType?GroupCode=...&company=...` | `JsGetSalUnitMsr(?)` |
| `GET /GetSKU?GroupCode=...&company=...` | `JsGetSKU(?)` |
| `GET /GetVariety?BRAND=...&GroupCode=...&company=...` | `JsGetVariety(?,?)` |
| `GET /GetSubGroup?BRAND=...&VARIETY=...&GroupCode=...&company=...` | `JsGetSubGroup(?,?,?)` |
| `GET /GetUnit?GroupCode=...&company=...` | `JsGetUnit(?)` |
| `GET /GetFA?GroupCode=...&company=...` | `JsGetFaType(?)` |
| `GET /GetBuyUnit?company=...` | `JsGetBuyUnitUom()` |
| `GET /GetGroup?company=...` | `JsGetGroupNameWithCode()` |
| `GET /GetBrand?GroupCode=...&company=...` | `JsGetBrand(?)` |
| `GET /GetBuyUnitMsr?GroupCode=...&company=...` | `JsGetBuyUnitMsr(?)` |
| `GET /GetInvUnitMsr?GroupCode=...&company=...` | `JsGetInvUnitMsr(?)` |
| `GET /JsGetUOMGroup?GroupCode=...&company=...` | `JsGetUOMGroup(?)` |
| `GET /GetDistinctItemName?company=...` | `JSGETDISTINCTITEMNAMES()` |
| `GET /GetDistinctItemNamesSQL` | `[imc].[jsGetDistinctItemNames]` |
| `GET /GetMergedDistinctItemNames?company=...` | Merges HANA item names and SQL item names. |

## 6. Approval Flow

Approval starts when an item is inserted through `[imc].[jsInsertFullItemData]`. The stored procedure creates the request and approval stages. The exact approval table names are not visible in C# because the implementation is inside SQL Server stored procedures.

Flow diagram:

```text
Create Request
    |
    v
Pending First Approver
    |
    +--> Reject --> Rejected --> Creator can view in GetRejectedItemsForCreator
    |
    v
Approve Intermediate Stage
    |
    v
Pending Next Approver
    |
    v
Final Stage Detected
    |
    v
Fetch SAP payload rows: [imc].[jsGetPendingItemApiInsertions]
    |
    v
POST SAP /Items
    |
    +--> SAP Failed --> ApprovalStatus = Blocked, tag = N, API error logged
    |
    v
SAP Success, tag = Y
    |
    v
Approve in SQL: [imc].[jsApproveItem]
    |
    v
Completed / Approved
```

Approval stage fields returned by `GetIMCApprovalFlow`:

| Field | Meaning |
|---|---|
| `stageId` | Approval stage ID. |
| `stageName` | Name of the approval stage. |
| `priority` | Stage order. |
| `assignedTo` | User/role assigned to the stage. |
| `actionStatus` | Current action status. |
| `actionDate` | Date/time of action. |
| `description` | Stage description or remarks. |
| `approvalRequired` | Number/count of approvals required. |
| `rejectRequired` | Number/count of rejections required. |

Reject flow:

```text
Approver submits POST /api/ItemMaster/RejectItem
        |
        v
ItemMasterService.RejectItemAsync()
        |
        v
[imc].[jsRejectItem]
        |
        v
Rejected list / creator rejected list
```

Return flow: there is no separate "return to creator" API in `ItemMasterController.cs`. Corrections appear to be handled by `UpdateInitData`, `UpdateSAPData`, or a new create request depending on frontend behavior and stored procedure rules.

## 7. SAP Integration

SAP insertion is handled by `ItemMasterService.PostItemsToSAPAsync()`.

SAP endpoint:

```text
POST {SapServiceLayer:BaseUrl}/Items
```

The base URL comes from `appsettings.json` key `SapServiceLayer:BaseUrl`. Company and HANA settings come from:

| Config key | Purpose |
|---|---|
| `ActiveEnvironment` | Selects `Test` or `Live`. |
| `HanaSettings:{ActiveEnvironment}` | HANA connection and schema for dropdowns/master data. |
| `ConnectionStrings:DefaultConnection` | SQL Server database for IMC workflow data. |
| `SapServiceLayer:CompanyDB` | Company database names for SAP login/session flow. |
| `ConnectionStrings:LiveHanaConnection` | Live Oil HANA connection. |
| `ConnectionStrings:LiveBevHanaConnection` | Live Beverages HANA connection. |
| `ConnectionStrings:LiveMartHanaConnection` | Live MART HANA connection. |

SAP session selection:

| Company | Method |
|---|---|
| 1 Oil | `_bom2Service.GetSAPSessionOilAsync()` |
| 2 Beverages | `_bom2Service.GetSAPSessionBevAsync()` |
| 3 MART | `_bom2Service.GetSAPSessionMartAsync()` |

SAP payload model: `ItemsTree` in `Models/ItemMasterModel.cs`.

Important mapped fields:

| SAP field | Source / rule |
|---|---|
| `ItemName` | `PendingItemApiInsertionsModel.ItemName` |
| `ItemsGroupCode` | `ItemGroupCode` |
| `U_Tax_Rate`, `U_Rev_tax_Rate` | `TaxRate` |
| `ChapterID` | Parsed from `ChapterId`; defaults to `0` if parse fails. |
| `U_Unit`, `U_Brand`, `U_Variety`, `U_Sub_Group`, `U_SKU` | Business item fields. |
| `U_IsLitre`, `U_Gross_Weight`, `U_MRP`, `U_PACK_TYPE` | Business item fields. |
| `SalesItem`, `PurchaseItem`, `InventoryItem` | Computed from group code. |
| `IssueMethod` | Computed from group code and company. |
| `CostAccountingMethod` | BEV is always `bis_FIFO`; others use `bis_SNB` when batch-managed. |
| `ManageBatchNumbers` | Computed from group code and company. |
| `WTLiable` | `tYES` only for group `102` in companies 1 and 2. |
| `Series` | Computed from `(company, groupCode)` with fallback to DB `Series`. |
| `UoMGroupEntry` | `Manual=-1`, `MTS2LITRE=1`, `KG2LITRE=2`, `MTS2LITRE(OLIVE)=3`, else `0`. |
| `U_TYPE` | `PREMIUM` or `COMMODITY` based on `IsLitre` and `Variety`. |
| `U_FA_Type` | Used for Oil/Beverages. |
| `U_FA_TYPE` | Used for MART. |
| `U_Packing_Type` | Sent only for Oil/Beverages, not MART. |

SAP failure behavior:

1. SAP response body is parsed by `ExtractSapErrorCodeAndMessage()`.
2. Error is logged through `[imc].[LogApiError]`.
3. Item API status is updated through `[imc].[jsUpdateItemApiStatus]` with tag `N`.
4. `ApproveItemAsync()` returns `Success = false`, `ApprovalStatus = Blocked`.
5. `[imc].[jsApproveItem]` is not called on final-stage SAP failure.

Retry behavior:

| Retry path | Behavior |
|---|---|
| `POST /api/ItemMaster/Items?ItemId=...` | Manual SAP post retry using the same `PostItemsToSAPAsync()` method. |
| `POST /api/ItemMaster/ApproveItem` | Can retry final approval; method will again check pending insertions and SAP status. |
| Duplicate prevention | `_sapPostLocks` locks by `InitId`, and `[imc].[jsUpdateItemApiStatus]` returns previous tag. If previous tag is `Y`, SAP call is skipped as already created. |

MART auto-sync:

```text
Primary SAP success
    |
    +-- Company 3? skip
    +-- Company not 1/2? skip
    +-- Not finished goods? skip
    |
    v
Get MART SAP session
    |
    v
Clear fields not valid in MART:
    U_FA_TYPE = null
    U_FA_Type = null
    U_Packing_Type = null
    WTLiable = tNO
    CostAccountingMethod recalculated for MART
    |
    v
POST SAP /Items to MART
```

## 8. Database Flow

SQL Server stored procedures used by IMC:

| Stored procedure | Usage |
|---|---|
| `[imc].[jsInsertFullItemData]` | Inserts full IMC request and likely creates approval flow. |
| `imc.jsInsertInitData` | Inserts initial item data. |
| `imc.jsInsertSAPData` | Inserts SAP item data. |
| `imc.jsUpdateInitData` | Updates initial item data. |
| `imc.jsUpdateSAPData` | Updates SAP item data. |
| `[imc].[jsApproveItem]` | Approves the current workflow stage. |
| `[imc].[jsRejectItem]` | Rejects the item workflow. |
| `[imc].[jsGetPendingItems]` | Reads pending items for a user/company. |
| `[imc].[jsGetApprovedItems]` | Reads approved items. |
| `[imc].[jsGetRejectedItems]` | Reads rejected items. |
| `[imc].[jsGetFullItemDetails]` | Reads full item detail. |
| `[imc].[jsGetWorkflowInsights]` | Reads pending/approved/rejected counts. |
| `[imc].[jsGetPendingItemApiInsertions]` | Reads rows ready for SAP insertion. |
| `[imc].[jsUpdateItemApiStatus]` | Updates SAP API message and tag. |
| `[imc].[LogApiError]` | Stores SAP/API error details. |
| `[imc].[jsGetIMCApprovalFlow]` | Reads approval flow history. |
| `[imc].[jsGetCreatedByDetail]` | Reads creator item details. |
| `[imc].[jsGetDistinctItemNames]` | Reads SQL-side item names. |
| `[imc].[jsGetItemByUserId]` | Reads items for user/month. |
| `[imc].[jsImcNotify]` | Gets users to notify after approval action. |
| `[imc].[GetUsersInCurrentStage]` | Gets current-stage users after creation. |
| `[imc].[jsGetRejectedItemsForCreator]` | Reads rejected items for creator. |
| `[imc].[jsGetId]` | Gets init/document ID from flow ID. Present in service helper. |

Important note about tables: actual table names are not referenced directly in C# for IMC. All SQL Server writes and workflow transitions are hidden inside the `[imc]` stored procedures. To debug table-level issues, inspect those procedures in the SQL Server database selected by `ConnectionStrings:DefaultConnection`.

HANA stored procedures used for dropdowns/master data:

| HANA procedure | Usage |
|---|---|
| `JsGetVariety` | Variety dropdown. |
| `JsGetTaxRate` | Tax rate dropdown. |
| `JsGetSubGroup` | Sub group dropdown. |
| `JsGetSKU` | SKU dropdown. |
| `JsGetHSN` | HSN dropdown. |
| `JsGetInvntryUom` | Inventory UOM dropdown. |
| `JsGetPackingType` | Packing type dropdown. |
| `JsGetPackType` | Pack type dropdown. |
| `JsGetPurPackMsr` | Purchase pack UOM. |
| `JsGetSalPackMsr` | Sales pack UOM. |
| `JsGetSalUnitMsr` | Sales unit. |
| `JsGetUnit` | Unit dropdown. |
| `JsGetFaType` | FA type dropdown. |
| `JsGetBuyUnitUom` | Buy unit UOM. |
| `JsGetGroupNameWithCode` | Item groups. |
| `JsGetBrand` | Brand dropdown. |
| `JsGetBuyUnitMsr` | Purchase unit by group. |
| `JsGetInvUnitMsr` | Inventory unit by group. |
| `JsGetUOMGroup` | UOM group by group. |
| `JSGETDISTINCTITEMNAMES` | HANA item names. |

Insert/update flow:

```text
InsertFullItemDataModel
    |
    v
[imc].[jsInsertFullItemData]
    |
    +--> init item fields
    +--> SAP fields
    +--> workflow rows
    +--> returns Message and NewInitId
    |
    v
[imc].[GetUsersInCurrentStage]
    |
    v
Push notification and notification insert
```

## 9. Frontend Flow

No dedicated IMC Razor page or IMC JavaScript file was found in the repository. The module exposes APIs that can be consumed by an external frontend/mobile app.

Known frontend references in this repository:

| File | API call |
|---|---|
| `Views/InventoryAuditWeb/AddSession.cshtml` | `/api/ItemMaster/GetGroup?company=${companyId}` |
| `Views/InventoryAuditWeb/AllInventory.cshtml` | `/api/ItemMaster/GetGroup?company=${companyId}` |

Expected external frontend flow:

```text
Load user/session/company
    |
    v
Call dropdown APIs from /api/ItemMaster
    |
    v
User fills form
    |
    v
POST /api/ItemMaster/InsertFullItem
    |
    v
Show success and NewInitId message
    |
    v
Approver sees item from GET /GetPendingItems
    |
    v
Approver calls POST /ApproveItem or POST /RejectItem
```

## 10. Security

Authentication and authorization are configured in `Program.cs`:

| Security item | File | Notes |
|---|---|---|
| JWT bearer auth | `Program.cs` | `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` validates issuer, audience, lifetime, signing key. |
| Session | `Program.cs` | Session is enabled with `IdleTimeout` from `Session:TimeoutDays`. |
| Authorization middleware | `Program.cs` | `app.UseAuthentication()` and `app.UseAuthorization()` are enabled. |
| Permission filter | `Filters/CheckUserPermissionAttribute.cs` | Reads `userId` and `companyId` from session, then calls `IPermissionService.CheckUserPermissionAsync()`. |

Important current state:

Most IMC controller permission attributes are commented out, for example:

```csharp
// [CheckUserPermission("item_master_creation", "view")]
// [CheckUserPermission("item_master_creation", "create")]
// [CheckUserPermission("item_master_creation", "approve")]
```

That means endpoint-level IMC permission enforcement is not active in `ItemMasterController.cs` unless another global policy exists outside this file. Developers should treat this as a security risk and verify frontend/API gateway restrictions.

Company validation:

| Area | Validation |
|---|---|
| HANA dropdown APIs | `_hanaSettings.TryGetValue(company, out settings)` rejects invalid company IDs. |
| Live HANA helper | `GetLiveHanaSettings(company)` allows only `1`, `2`, `3`. |
| SAP posting | `PostItemsToSAPAsync()` rejects unsupported company and logs `UNSUPPORTED_COMPANY`. |

Approval ownership validation:

Ownership and current approver validation are expected inside `[imc].[jsApproveItem]`, `[imc].[jsRejectItem]`, `[imc].[jsGetPendingItems]`, and related approval stored procedures. C# passes `itemId`, `company`, and `userId`; it does not manually verify approver ownership before calling the stored procedure.

## 11. Debugging Guide

### Item Not Saving

Check in this order:

| Step | Check |
|---|---|
| API | `POST /api/ItemMaster/InsertFullItem` response. |
| Controller | `ItemMasterController.InsertFullItem()` around null request and `result.Success`. |
| Service | `ItemMasterService.InsertFullItemDataAsync()`. |
| Model | `InsertFullItemDataModel` in `Models/ItemMasterModel.cs`. |
| Stored procedure | `[imc].[jsInsertFullItemData]`. |
| DB output | Confirm SP returns `Message` and `NewInitId`. |
| Notifications | If save succeeds but notification fails, response message appends `Notification failed: ...`. |

Common causes:

| Symptom | Likely cause |
|---|---|
| `Request is null` | Frontend sent empty body or wrong content type. |
| SQL error | Missing/invalid parameter or stored procedure issue. |
| `No data returned from stored procedure` | `[imc].[jsInsertFullItemData]` did not return expected result set. |
| Wrong variety/subgroup saved | Service intentionally swaps `Variety` and `SubGroup` through `MapToDb()` / `MapFromDb()`. Check mapping and SP parameter order. |

### Approval Stuck

Check:

| Step | Check |
|---|---|
| Pending API | `GET /api/ItemMaster/GetPendingItems?userId=...&company=...` |
| Approval API | `POST /api/ItemMaster/ApproveItem` response fields: `ApprovalStatus`, `SapStatus`, `MartStatus`, `Message`. |
| Approval flow | `GET /api/ItemMaster/GetIMCApprovalFlow?flowId=...` |
| SP | `[imc].[jsApproveItem]` |
| Users to notify | `[imc].[jsImcNotify]`, `[imc].[GetUsersInCurrentStage]` |

If response says `ApprovalStatus = Blocked`, it usually means final-stage SAP creation failed and SQL approval was intentionally not completed.

### SAP Sync Failed

Check:

| Step | Check |
|---|---|
| API | `POST /api/ItemMaster/ApproveItem` or manual `POST /api/ItemMaster/Items?ItemId=...` |
| Service | `ItemMasterService.PostItemsToSAPAsync()` |
| Payload model | `ItemsTree` |
| Source data | `GET /api/ItemMaster/GetPendingItemApiInsertions?ItemId=...` |
| Status SP | `[imc].[jsUpdateItemApiStatus]` |
| Error SP | `[imc].[LogApiError]` |
| SAP sessions | `_bom2Service.GetSAPSessionOilAsync()`, `GetSAPSessionBevAsync()`, `GetSAPSessionMartAsync()` |
| SAP endpoint | `POST {SapServiceLayer:BaseUrl}/Items` |

Common SAP failure areas:

| Area | What to inspect |
|---|---|
| Invalid company | Company must be 1, 2, or 3. |
| Invalid item group | Group code drives item flags, series, batch, issue method, and sales/purchase/inventory flags. |
| HSN/chapter | `ChapterId` parse failure sends `0`. |
| UDF casing | MART uses `U_FA_TYPE`; Oil/Beverages use `U_FA_Type`. |
| MART sync | MART cannot receive `U_Packing_Type`, `U_FA_Type`, or incompatible WTLiable rules. |
| Duplicate creation | Check tag from `[imc].[jsUpdateItemApiStatus]`; tag `Y` skips duplicate SAP call. |

### Wrong Data Saved

Check:

| Area | File/procedure |
|---|---|
| Request DTO | `InsertFullItemDataModel`, `UpdateInitDataModel`, `UpdateSAPDataModel` in `Models/ItemMasterModel.cs`. |
| SQL parameters | `InsertFullItemDataAsync()`, `UpdateInitDataAsync()`, `UpdateSAPDataAsync()` in `ItemMasterService.cs`. |
| Variety/subgroup mapping | `MapToDb()` and `MapFromDb()` in `ItemMasterService.cs`. |
| Stored procedure | `[imc].[jsInsertFullItemData]` or relevant update SP. |
| SAP payload mapping | `PostItemsToSAPAsync()` and `ItemsTree`. |

### Dropdown Not Loading

Check:

| Dropdown | API | Backend source |
|---|---|---|
| Item group | `GET /GetGroup?company=...` | HANA `JsGetGroupNameWithCode()` |
| Brand | `GET /GetBrand?GroupCode=...&company=...` | HANA `JsGetBrand(?)` |
| HSN | `GET /GetHSN?company=...` | HANA `JsGetHSN()` |
| Tax rate | `GET /GetTaxRate?company=...` | HANA `JsGetTaxRate()` |
| SKU | `GET /GetSKU?GroupCode=...&company=...` | HANA `JsGetSKU(?)` |
| UOM group | `GET /JsGetUOMGroup?GroupCode=...&company=...` | HANA `JsGetUOMGroup(?)` |

If all dropdowns fail, check:

| Check | Why |
|---|---|
| `ActiveEnvironment` | Selects the HANA schema set. |
| `HanaSettings` | Missing company key causes invalid company error. |
| HANA connectivity | Service uses `Sap.Data.Hana.HanaConnection`. |
| Procedure name/case | HANA procedure names are case-sensitive when quoted. |

## 12. Request / Response Examples

### Create Item Request

```http
POST /api/ItemMaster/InsertFullItem
Content-Type: application/json
```

```json
{
  "userId": 101,
  "company": 1,
  "itemName": "JIVO SAMPLE OIL 1 LTR",
  "itemGroupCode": 102,
  "itemGroupName": "FINISHED",
  "taxRate": "5",
  "chapterId": "1512",
  "chapterName": "Edible oil",
  "unit": "OIL",
  "brand": "JIVO",
  "variety": "CANOLA",
  "subGroup": "CANOLA",
  "sku": "1 LTR",
  "isLitre": "Y",
  "litre": 1.0,
  "grossWeight": 0.95,
  "mrp": 220,
  "packType": "CONSUMER PACK",
  "packingType": "BOTTLE",
  "faType": null,
  "uom": "NOS",
  "salesUom": "NOS",
  "invUom": "NOS",
  "purchaseUom": "NOS",
  "boxSize": 12,
  "unitSize": 1,
  "uomGroup": "Manual",
  "franName": 1,
  "prchseItem": "tYES",
  "invItem": "tYES",
  "numInBuy": 1,
  "salUnitMsr": "NOS",
  "numInSale": 1,
  "evalSystem": "bis_FIFO",
  "threeType": "iProductionTree",
  "manSerNum": "tNO",
  "salFactor1": 1,
  "salFactor2": 12,
  "salFactor3": 1,
  "salFactor4": 1,
  "purFactor1": 1,
  "purFactor2": 1,
  "purFactor3": 1,
  "purFactor4": 1,
  "purPackMsr": "NOS",
  "purPackUn": 1,
  "salPackUn": 12,
  "manBtchNum": "tYES",
  "genEntry": "Y",
  "wtLiable": "tYES",
  "issueMethod": "M",
  "mngMethod": "bomm_OnEveryTransaction",
  "invntoryUom": "NOS",
  "series": 389,
  "gstRelevant": "A",
  "gstTaxCtg": "R",
  "sellItem": "tYES",
  "prcrmntMtd": "B"
}
```

Example response:

```json
{
  "message": "Item inserted successfully.",
  "success": true,
  "approvalStatus": null,
  "sapStatus": null,
  "martStatus": null
}
```

### Approve Item Request

```http
POST /api/ItemMaster/ApproveItem
Content-Type: application/json
```

```json
{
  "itemId": 512,
  "company": 1,
  "userId": 45,
  "remarks": "Approved"
}
```

Intermediate approval response:

```json
{
  "success": true,
  "approvalStatus": "Done",
  "sapStatus": "Skipped (intermediate approval stage)",
  "martStatus": "Skipped (not FG item or intermediate stage)",
  "message": "Approved Document of FlowId 512"
}
```

Final approval success response:

```json
{
  "success": true,
  "approvalStatus": "Done",
  "sapStatus": "Success",
  "martStatus": "Success - Item created in MART SAP (Company 3)",
  "message": "SAP item created successfully | Approved Document of FlowId 512 | API Triggered after final approval"
}
```

Final approval SAP failure response:

```json
{
  "success": false,
  "approvalStatus": "Blocked",
  "sapStatus": "Failed: Item already exists (SAP Error Code: -5002)",
  "martStatus": "Skipped",
  "message": "Item already exists (SAP Error Code: -5002)"
}
```

### Reject Item Request

```http
POST /api/ItemMaster/RejectItem
Content-Type: application/json
```

```json
{
  "itemId": 512,
  "company": 1,
  "userId": 45,
  "remarks": "Incorrect tax rate"
}
```

Example response:

```json
{
  "message": "Rejected successfully",
  "success": true
}
```

Note: `RejectItemAsync()` currently does not pass `remarks` to `[imc].[jsRejectItem]`; it only passes `itemId`, `company`, and `userId`.

### Pending Item API Response

```http
GET /api/ItemMaster/GetPendingItems?userId=45&company=1
```

```json
{
  "success": true,
  "data": [
    {
      "flowId": 512,
      "initDataId": "512",
      "itemName": "JIVO SAMPLE OIL 1 LTR",
      "itemGroupCode": 102,
      "itemGroupName": "FINISHED",
      "taxRate": "5",
      "chapterId": "1512",
      "unit": "OIL",
      "brand": "JIVO",
      "variety": "CANOLA",
      "subGroup": "CANOLA",
      "sku": "1 LTR",
      "isLitre": "Y",
      "createdByName": "Creator Name",
      "flag": "Pending"
    }
  ]
}
```

### SAP Sync Result Response

```http
POST /api/ItemMaster/Items?ItemId=512
```

```json
{
  "success": true,
  "data": [
    {
      "itemId": 512,
      "isSuccess": true,
      "message": "Successfully created",
      "martStatus": "Skipped - Not an FG item (GroupCode: '105', GroupName: 'PACKAGING MATERIAL'). MART sync requires GroupCode '102' or GroupName containing 'FINISHED'/'FG'"
    }
  ]
}
```

## 13. Important Notes

| Area | Note |
|---|---|
| Final approval | SAP is called before `[imc].[jsApproveItem]`. This is intentional. |
| SAP failure | Blocks final approval and returns `ApprovalStatus = Blocked`. |
| Duplicate SAP creation | `_approvalLocks`, `_sapPostLocks`, and `[imc].[jsUpdateItemApiStatus]` tag checks protect against double posting. |
| API tag values | Code uses `P`, `Y`, `N`, and in one exception path `False`. Standardize if possible. |
| Permission attributes | IMC `CheckUserPermission` attributes are commented out. Confirm security before production use. |
| Remarks on reject | `RejectItemModel.remarks` exists but is not passed to `[imc].[jsRejectItem]`. |
| Variety/subgroup | Service swaps values through `MapToDb()` and `MapFromDb()`. This is a high-risk mapping area. |
| HANA procedure case | Quoted HANA procedures are case-sensitive. |
| MART UDFs | MART does not receive `U_Packing_Type` or mixed-case `U_FA_Type`. |
| Chapter ID | Invalid `ChapterId` becomes `0` in SAP payload. This can cause SAP rejection or wrong data. |
| Frontend files | No dedicated IMC page/script found in this repo; API clients may live outside this codebase. |
| SQL tables | Table names are not visible in C#; inspect `[imc]` stored procedures in SQL Server for exact table flow. |

