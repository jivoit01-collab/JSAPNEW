# Bill Verification Software Pages Documentation

## 1. Module Overview

The Bill Verification module manages the movement of purchase bills from initial document upload through checker approval, invoice payment monitoring, paid invoice review, and admin audit. The user-facing pages are built with Razor views, Bootstrap, jQuery, MVC controllers, service classes, SQL Server stored procedures, and the `AttachmentUpload` workflow table.

The main pages are:

| Page | View | Controller | Main purpose |
| --- | --- | --- | --- |
| Admin | `Views/Admin/AdminPage.cshtml` | `AdminController` | Full bill verification dashboard, summary, activity review, attachment deletion |
| Maker | `Views/Maker/MakerPage.cshtml` | `MakerController` | Search purchase bills, upload invoice attachment, enter maker remark |
| Checker | `Views/Checker/CheckerPage.cshtml` | `CheckerController` | Review maker-submitted bills and approve or reject them |
| Invoice Payment | `Views/Invoicepayment/InvoicePaymentPage.cshtml` | `InvoicePaymentController` | Monitor approved and unpaid invoices before payment |
| Payment Checker | `Views/PaymentChecker/PaymentCheckerPage.cshtml` | `PaymentCheckerController` | Review paid invoices and payment summary |

## 2. Business Workflow

The expected flow is:

```text
Purchase bill created
  |
  v
Maker searches bill and uploads attachment
  |
  v
Checker reviews attachment and maker remark
  |
  +-- Rejects bill -> bill remains visible with rejected checker status
  |
  +-- Approves bill
        |
        v
Invoice Payment page shows approved and unpaid invoices
        |
        v
Payment data is updated by the payment process/source data
        |
        v
Payment Checker page shows paid invoices
        |
        v
Admin can monitor all stages and delete attachments when required
```

## 3. Shared Data Model

The pages mainly use `BillDetailDto` and `InvoiceItemDto` from `Models/BillVerificationModels.cs`.

### BillDetailDto Fields

| Field | Usage |
| --- | --- |
| `AccountName` | Vendor or account name shown in all grids |
| `VchNumber` | Voucher number and primary bill identifier |
| `VoucherDate` | Purchase voucher date |
| `BillAmount` | Bill amount |
| `SupplierRef` | Supplier reference number |
| `SupplierRefDate` | Supplier reference date |
| `DueDate` | Bill due date |
| `PaymentDate` | Payment completion date where available |
| `AttachmentPath` | Uploaded invoice document path |
| `Attachment` | Admin helper value, usually `File` or `No File` |
| `MakerRemark` | Remark entered by Maker during upload |
| `CheckerRemark` | Remark entered by Checker during approval/rejection |
| `CheckerStatus` | `Approved`, `Rejected`, or pending/null |
| `MakerStatus` | Maker upload/submission status |
| `PaymentStatus` | `Paid` or `UnPaid` |
| `SerialNumber` | Used for invoice item lookup in some checker flows |
| `TotalQuantity` | Maker summary total quantity |
| `TotalItemValue` | Maker summary total item value |
| `TotalItems` | Maker summary item count |

### InvoiceItemDto Fields

| Field | Usage |
| --- | --- |
| `ProductName` | Product/item name in expanded invoice details |
| `Quantity` | Item quantity |
| `Rate` | Purchase cost/rate |
| `Tax` | Tax percentage |
| `Amount` | Item amount |
| `WarehouseName` | Warehouse shown in checker/payment details |
| `TaxName` | Tax name shown in checker/payment details |
| `ItemValue` | Item value shown in expanded details |

## 4. Database and Stored Procedures

The module uses the `FHConnection` connection string.

| Object | Purpose |
| --- | --- |
| `GetBillDetails` / `dbo.GetBillDetails` | Returns purchase bill header data, attachment status, maker/checker status, and payment status |
| `GetInvoice` | Returns invoice item details after the header result set |
| `GetSummaryData` | Returns admin summary counts |
| `AttachmentUpload` | Stores uploaded attachment path, maker remark, maker status, checker remark, checker status, and checker date |
| `PurchaseHeader` | Purchase bill source table used by stored procedures and account search |
| `AccountMaster` | Account/vendor name lookup for autocomplete |

## 5. Admin Page

### Page

- URL action: `Admin/AdminPage`
- View: `Views/Admin/AdminPage.cshtml`
- Controller: `Controllers/AdminController.cs`
- Services: `IPaymentCheckerService`

### Purpose

The Admin page gives an overview of the whole Bill Verification process. It provides summary cards, filters, workflow tabs, attachment viewing, and attachment deletion.

### Summary Cards

The summary cards are loaded from `GET /Admin/GetSummary`, which calls stored procedure `GetSummaryData` using start date `2026-04-01`.

| Card | Meaning |
| --- | --- |
| Total Bills | Total distinct bills in the selected/source range |
| Pending (Maker) | Bills with no maker status or pending maker status |
| Approved (Checker) | Bills approved by checker |
| Paid (Invoice) | Bills marked as paid |

### Filters

| Filter | Description |
| --- | --- |
| From Date | Lower voucher date range |
| To Date | Upper voucher date range |
| Account Name | Optional account/vendor filter |
| Search button | Reloads all admin tabs and summary |

### Tabs

| Tab | Endpoint | Columns |
| --- | --- | --- |
| Maker Activity | `GET /Admin/GetMakerActivity` | Account, Voucher, Date, Amount, Maker Remark, Status, Attachment, Delete |
| Checker Activity | `GET /Admin/GetCheckerActivity` | Account, Voucher, Date, Amount, Checker Remark, Checker Status, Attachment, Delete |
| Invoice Payment | `GET /Admin/GetInvoiceActivity` | Account, Voucher, Date, Amount, Due Date, Payment Status, Payment Date, Attachment, Delete |
| Payment Checker | `GET /Admin/GetPaymentCheckerActivity` | Account, Voucher, Date, Amount, Status, Remark, Attachment, Delete |

### Admin Attachment Actions

- `View` opens the uploaded file in an iframe PDF viewer.
- `Delete` opens a confirmation modal.
- Confirm delete calls `POST /Admin/DeleteAttachment`.
- The controller deletes the physical file under `wwwroot` and removes the matching `AttachmentUpload` record by `VchNumber`.

## 6. Maker Page

### Page

- URL action: `Maker/MakerPage`
- View: `Views/Maker/MakerPage.cshtml`
- Controller: `Controllers/MakerController.cs`
- Service: `IMakerService` / `MakerService`

### Purpose

The Maker page is the entry point for bill verification. The Maker searches purchase bills, checks whether an attachment already exists, uploads a bill document, and adds a maker remark.

### Filters and Search

| Control | Behavior |
| --- | --- |
| From Date | Required before search |
| To Date | Required before search |
| Account Name | Optional search box with autocomplete |
| Search | Calls `GET /Maker/GetBillDetails` |

The page validates that both dates are selected and that From Date is not greater than To Date.

### Account Autocomplete

The Account Name field starts searching after two characters. It calls:

```text
GET /Maker/GetAccountSuggestions?term={text}
```

The service returns the top matching account names from `PurchaseHeader` joined with `AccountMaster`.

### Bill Grid Columns

| Column | Description |
| --- | --- |
| Account | Account/vendor name |
| Voucher | Voucher number |
| Date | Voucher date |
| Amount | Bill amount |
| Supplier Ref | Supplier reference |
| Ref Date | Supplier reference date |
| Due Date | Bill due date |
| Attachment | File upload input or View button |
| Maker Remark | Text input before upload, saved remark after upload |
| Checker Status | Pending, Approved, or Rejected |
| Action | Submit button before upload, Uploaded text after upload |
| Total Qty | Total quantity from bill data |
| Total Amount | Total item value |
| Total Items | Number of items |
| Payment Status | Paid or UnPaid |
| Payment Date | Payment date if available |

### Upload Flow

When the Maker clicks Submit:

1. The page reads the selected file and maker remark.
2. It sends a `FormData` request to `POST /Maker/SubmitBill`.
3. The controller saves the file under `wwwroot/uploads/maker`.
4. A unique file name is generated with `Guid.NewGuid()`.
5. A row is inserted into `AttachmentUpload` with:
   - `VchNumber`
   - `AttachmentPath`
   - `MakerRemark`
   - `Status = 'Submitted'`
   - `CreatedDate = GETDATE()`
6. The page reloads the bill grid after successful upload.

## 7. Checker Page

### Page

- URL action: `Checker/CheckerPage`
- View: `Views/Checker/CheckerPage.cshtml`
- Controller: `Controllers/CheckerController.cs`
- Service: `ICheckerService` / `CheckerService`

### Purpose

The Checker page is used to review bills submitted by Makers. A Checker can view the uploaded document, inspect invoice item details, enter a checker remark, and approve or reject the bill.

### Filters

| Control | Behavior |
| --- | --- |
| From Date | Optional date filter |
| To Date | Optional date filter |
| Account Name | Optional account filter |
| Search | Calls `GET /Checker/GetBillDetails` |

### Bill Grid Columns

| Column | Description |
| --- | --- |
| Account | Clickable account name, expands invoice item details |
| Voucher | Voucher number |
| Date | Voucher date |
| Amount | Bill amount |
| Supplier Ref | Supplier reference |
| Due Date | Bill due date |
| Attachment | View uploaded file in iframe |
| Maker Remark | Maker-entered remark |
| Status | Checker status badge |
| Checker Remark | Text input for checker remark |
| Action | Approve and Reject buttons, or final status badge |

### Approve/Reject Flow

| Action | Endpoint | Effect |
| --- | --- | --- |
| Approve | `POST /Checker/UpdateCheckerStatus` with `status=Approved` | Updates `AttachmentUpload.CheckerStatus`, `CheckerRemark`, and `CheckerDate` |
| Reject | `POST /Checker/UpdateCheckerStatus` with `status=Rejected` | Updates `AttachmentUpload.CheckerStatus`, `CheckerRemark`, and `CheckerDate` |

After either action, the grid reloads.

### Invoice Item Expansion

Clicking the Account value calls:

```text
GET /Checker/GetInvoiceItems?serialNumber={serialNumber}
```

The expanded row shows Product, Qty, Warehouse, Tax %, Tax Name, and Item Value.

## 8. Invoice Payment Page

### Page

- URL action: `InvoicePayment/InvoicePaymentPage`
- View: `Views/Invoicepayment/InvoicePaymentPage.cshtml`
- Controller: `Controllers/InvoicePaymentController.cs`
- Service: `IInvoicePaymentService` / `InvoicePaymentService`

### Purpose

The Invoice Payment page monitors invoices that have already been approved by Checker but are still unpaid. It is a read-oriented page for payment follow-up.

### Filters

| Control | Behavior |
| --- | --- |
| From Date | Voucher date filter |
| To Date | Voucher date filter |
| Account Name | Optional account filter |
| Search | Calls `GET /InvoicePayment/GetBillDetails` |

### Display Rule

The page fetches bill data and then displays only records where:

```text
CheckerStatus = Approved
PaymentStatus = UnPaid
```

If there are no matching records, the grid displays `No Approved & Unpaid Records Found`.

### Grid Columns

| Column | Description |
| --- | --- |
| Account | Account/vendor name |
| Voucher | Voucher number |
| Date | Voucher date |
| Amount | Bill amount |
| Supplier Ref | Supplier reference |
| Ref Date | Supplier reference date |
| Due Date | Due date |
| Attachment | View uploaded file |
| Maker Remark | Maker remark |
| Checker Remark | Checker remark |
| Checker Status | Always displayed as Approved for listed rows |
| Payment Status | UnPaid for listed rows |

## 9. Payment Checker Page

### Page

- URL action: `PaymentChecker/PaymentCheckerPage`
- View: `Views/PaymentChecker/PaymentCheckerPage.cshtml`
- Controller: `Controllers/PaymentCheckerController.cs`
- Service: `IPaymentCheckerService` / `PaymentCheckerService`

### Purpose

The Payment Checker page reviews invoices that are already paid. It gives a concise paid-invoice dashboard, paid invoice list, PDF viewing, account search, and row expansion for invoice item details.

### Required Filters

| Control | Behavior |
| --- | --- |
| From Date | Required before search |
| To Date | Required before search |
| Account Name | Optional account search |
| Search | Calls `GET /PaymentChecker/GetPaidBillDetails` |

The page validates that both dates are selected and that From Date is not greater than To Date.

### Summary Cards

The summary cards are calculated client-side from returned paid invoice records.

| Card | Meaning |
| --- | --- |
| Total Paid Invoices | Count of unique paid voucher numbers |
| Total Paid Amount | Sum of paid bill amounts |
| Latest Payment Date | Latest payment date in the result set |
| Unique Accounts | Count of distinct account names |

### Paid Invoice Grid Columns

| Column | Description |
| --- | --- |
| Account | Clickable account name, expands invoice item details |
| Voucher | Voucher number |
| Date | Voucher date |
| Amount | Bill amount |
| Supplier Ref | Supplier reference |
| Ref Date | Supplier reference date |
| Due Date | Bill due date |
| Payment Date | Payment completion date |
| Attachment | View uploaded file |
| Maker Remark | Maker remark |
| Checker Remark | Checker remark |
| Checker Status | Approved badge |
| Payment Status | Paid badge |

### Paid Data Rule

`PaymentCheckerService.GetPaidBillDetails` calls `GetBillDetails` and filters records in C#:

```text
PaymentStatus = Paid
```

### Invoice Item Expansion

Clicking the Account value calls:

```text
GET /PaymentChecker/GetInvoiceItems?vchNumber={vchNumber}
```

The expanded row shows Product, Qty, Warehouse, Tax %, Tax Name, and Item Value.

## 10. API Endpoint Summary

| Method | Endpoint | Used by | Description |
| --- | --- | --- | --- |
| GET | `/Admin/GetSummary` | Admin | Loads summary cards |
| GET | `/Admin/GetMakerActivity` | Admin | Loads maker activity tab |
| GET | `/Admin/GetCheckerActivity` | Admin | Loads checker activity tab |
| GET | `/Admin/GetInvoiceActivity` | Admin | Loads invoice payment activity tab |
| GET | `/Admin/GetPaymentCheckerActivity` | Admin | Loads payment checker activity tab |
| POST | `/Admin/DeleteAttachment` | Admin | Deletes attachment file and DB record |
| GET | `/Maker/GetBillDetails` | Maker | Loads bills for maker processing |
| GET | `/Maker/GetAccountSuggestions` | Maker, Payment Checker | Loads account autocomplete values |
| GET | `/Maker/GetInvoiceItems` | Maker | Loads invoice item data by voucher number |
| POST | `/Maker/SubmitBill` | Maker | Uploads attachment and maker remark |
| GET | `/Checker/GetBillDetails` | Checker | Loads submitted bills for checking |
| POST | `/Checker/UpdateCheckerStatus` | Checker | Approves or rejects a bill |
| GET | `/Checker/GetInvoiceItems` | Checker | Loads invoice item data by serial number |
| GET | `/InvoicePayment/GetBillDetails` | Invoice Payment | Loads bills for approved/unpaid filtering |
| GET | `/InvoicePayment/GetInvoiceItems` | Invoice Payment | Loads invoice item data by voucher number |
| GET | `/PaymentChecker/GetPaidBillDetails` | Payment Checker | Loads paid bill records |
| GET | `/PaymentChecker/GetInvoiceItems` | Payment Checker | Loads invoice item data by voucher number |

## 11. Service Registration

The services are registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IMakerService, MakerService>();
builder.Services.AddScoped<ICheckerService, CheckerService>();
builder.Services.AddScoped<IInvoicePaymentService, InvoicePaymentService>();
builder.Services.AddScoped<IPaymentCheckerService, PaymentCheckerService>();
```

## 12. File Map

| Layer | Files |
| --- | --- |
| Admin controller/view | `Controllers/AdminController.cs`, `Views/Admin/AdminPage.cshtml` |
| Maker controller/view/service | `Controllers/MakerController.cs`, `Views/Maker/MakerPage.cshtml`, `Services/Implementation/MakerService.cs`, `Services/Interfaces/IMakerService.cs` |
| Checker controller/view/service | `Controllers/CheckerController.cs`, `Views/Checker/CheckerPage.cshtml`, `Services/Implementation/CheckerService.cs`, `Services/Interfaces/ICheckerService.cs` |
| Invoice Payment controller/view/service | `Controllers/InvoicePaymentController.cs`, `Views/Invoicepayment/InvoicePaymentPage.cshtml`, `Services/Implementation/InvoicePaymentService.cs`, `Services/Interfaces/IInvoicePaymentService.cs` |
| Payment Checker controller/view/service | `Controllers/PaymentCheckerController.cs`, `Views/PaymentChecker/PaymentCheckerPage.cshtml`, `Services/Implementation/PaymentCheckerService.cs`, `Services/Interfaces/IPaymentCheckerService.cs` |
| Shared models | `Models/BillVerificationModels.cs` |

## 13. Operational Notes

- Uploaded maker files are stored under `wwwroot/uploads/maker`.
- Attachment paths are stored in `AttachmentUpload.AttachmentPath`.
- The PDF viewer is implemented with an iframe on all major pages.
- Several pages de-duplicate displayed rows by voucher number on the client side.
- Payment Checker uses the Maker account suggestion endpoint for autocomplete.
- Admin deletion is destructive because it removes both the physical file and the database row.
- Checker status updates are stored against `AttachmentUpload` using `VchNumber`.
- Invoice item endpoints use `GetInvoice` and skip the first result set before reading item rows.

