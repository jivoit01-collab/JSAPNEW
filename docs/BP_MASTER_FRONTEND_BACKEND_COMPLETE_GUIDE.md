# BP Master Frontend and Backend Complete Guide

This is the single source of truth for the BP Master module in `JSAPNEW`.

Audience:

- Flutter frontend developers
- .NET backend developers
- SAP support engineers
- QA testers
- Future maintainers
- AI coding assistants

The BP module has been refactored to match the current SAP Portal Customer Registration and Vendor Registration forms. Only fields visible in the current portal forms should be used by the frontend and backend. The old legacy business fields were removed from active DTOs, stored procedure contracts, SAP payload mapping, and planned database cleanup scripts. The only preserved old field is `isStaff`.

---

## 1. Module Overview

### What BP Master Is

BP Master means Business Partner Master. In SAP Business One, a Business Partner is a party that the company buys from or sells to. This portal module lets users request a new customer or vendor BP, route the request through approval stages, and post the final approved record to SAP Service Layer.

| BP Type | Portal form | SAP `CardType` | Meaning |
|---|---|---|---|
| Customer | Customer Registration | `cCustomer` | Party that buys from the company |
| Vendor | Vendor Registration | `cSupplier` | Party that supplies goods or services |

### Why This Module Exists

Creating SAP BPs directly can introduce tax, bank, duplicate, or approval issues. This module adds:

- controlled data entry through SAP Portal forms
- structured approval workflow
- SAP-first final approval
- attachment/document capture
- SAP retry handling
- audit and snapshot history

### Main Components

| Component | Responsibility |
|---|---|
| Flutter portal | Collects customer/vendor data, attachments, and approval actions |
| `BPmasterController` | Exposes BP REST APIs |
| `BPmasterService` | Calls SQL procedures and orchestrates workflow/SAP retry logic |
| `BPMasterSapService` | Builds SAP payloads and calls SAP Service Layer |
| SQL BP tables | Store BP master, child data, workflow, SAP status, attachments, audit |
| SAP Service Layer | Creates `OCRD`, `CRD1`, `OCPR`, `OCRB`, and `Attachments2` records |

### End-to-End Flow

1. User creates a Customer or Vendor BP in the Flutter portal.
2. Frontend submits multipart form data to `POST /api/BPmaster/InsertBPmasterData`.
3. Backend saves uploaded files under `wwwroot/Uploads/BPmaster`.
4. Backend calls `BP.jsInsertBPMasterData`.
5. SQL inserts rows into `BP.jsMaster`, `BP.jsTaxDetails`, `BP.jsMasterAddress`, `BP.jsContactPersons`, `BP.jsBankDetails`, and `BP.jsAttachments`.
6. BP workflow is created in `BP.jsFlow`.
7. Stage 1 approver sees the request in `GetPendingBP`.
8. Stage 1 approval moves the flow to stage 2.
9. Stage 2 approval moves the flow to stage 3.
10. Stage 3 final approver clicks approve.
11. Backend sets SAP API status to `P` for processing.
12. Backend uploads attachments to SAP `Attachments2`.
13. Backend posts the BP to SAP `BusinessPartners`.
14. SAP response is stored in `BP.jsSAPData`.
15. If SAP succeeds, SQL marks final workflow status approved.
16. If SAP fails, workflow remains pending at stage 3 and retry is allowed.

### Workflow Summary

| Stage | Approver userId | Role | Result |
|---:|---:|---|---|
| 1 | `70` | Manager Approval | Verifies business need and basic data |
| 2 | `69` | Accounts Approval | Verifies tax, address, bank, and documents |
| 3 | `108` | SAP Team Final Approval | Posts to SAP, then completes final approval |

### SAP-First Final Approval

Final approval must not show success unless SAP has created the BP.

```text
Final approver approves
-> BPmasterService sets apiStatusTag = P
-> BPMasterSapService posts attachments and BusinessPartners payload
-> if SAP success:
      apiStatusTag = Y
      SAP-confirmed sapCardCode and payloadHash saved
      BP.jsApproveBP marks workflow approved
   if SAP failure:
      apiStatusTag = N
      apiMessage saved
      sapCardCode remains NULL
      workflow stays pending at stage 3
      RetrySapPost is enabled
```

### Snapshot and Audit System

Updates create snapshots before data changes. Audit rows capture field-level changes where relevant.

| Table | Purpose |
|---|---|
| `BP.jsAuditLog` | Field and table operation audit |
| `BP.jsMasterSnapshot` | Header snapshot |
| `BP.jsMasterAddressSnapshot` | Address snapshot |
| `BP.jsContactPersonsSnapshot` | Contact snapshot |
| `BP.jsBankDetailsSnapshot` | Bank snapshot |
| `BP.jsTaxDetailsSnapshot` | Tax snapshot |

### Attachment System

Attachments are preserved as multipart uploads.

1. Frontend sends files plus `fileTypes`.
2. Backend stores files in `wwwroot/Uploads/BPmaster`.
3. Metadata is stored in `BP.jsAttachments`.
4. On final SAP approval, attachments are posted to SAP `Attachments2`.
5. SAP returns `AbsoluteEntry`.
6. Backend sends that value as `OCRD.AttachmentEntry`.

---

## 2. Current Active Frontend Fields

Only these fields are active for BP Master. The frontend must not send retired legacy fields. The only preserved legacy field is `isStaff`.

### Common Fields

| Field Name | Required | Table | Column | API Field | Used In | Description |
|---|---:|---|---|---|---|---|
| Company Id | Yes | `BP.jsMaster` | `company` | `companyId` | SQL, SAP session, queues | Portal company/database identifier |
| BP Type | Yes | `BP.jsMaster` | `type` | `customerType` or `vendorType` | Workflow, SAP | `C` for customer, `V` for vendor |
| Company Name | Yes | `BP.jsMaster` | `name` | `companyName` | Lists, details, SAP | Legal/display BP name |
| Foreign Name | No | `BP.jsMaster` | `foreignName` | `foreignName`, `foreignTradeName` | Details, SAP | Alternate/foreign/trade name |
| Industry | No | `BP.jsMaster` | `industry` | `industry`, `industrySector` | Details, SAP UDF | Industry or sector |
| Currency | Yes | `BP.jsMaster` | `currency` | `currency` | SAP | BP currency, normally `INR` |
| Remarks | No | `BP.jsMaster` | `remarks` | `remarks` | Details, SAP notes | User-entered comments |
| Is Staff | Yes | `BP.jsMaster` | `isStaff` | `isStaff` | Portal/workflow | Preserved old flag |
| Created By | Yes | `BP.jsMaster` | `userId` | `userId` | Audit, ownership | Creator user id |
| Company By User | Yes | `BP.jsMaster` | `companyByUser` | `companyByUser` | Audit | User company label |

### Customer-Only Fields

| Field Name | Required | Table | Column | API Field | Used In | Description |
|---|---:|---|---|---|---|---|
| Customer Type | Yes | `BP.jsMaster` | `type` | `customerType` | SQL, SAP | Customer marker, normalized to `C` |
| Type Of Business | No | `BP.jsMaster` | `typeOfBusiness` | `typeOfBusiness` | Details, SAP UDF | Distributor, dealer, retailer, etc. |
| PAN Number | Yes | `BP.jsTaxDetails` | `panNo` | `panNumber` | Tax, SAP fiscal tax | Customer PAN |
| MSME | No | `BP.jsTaxDetails` | `msmeNo` | `msme` | Tax, SAP UDF | MSME/Udyam number if available |

### Vendor-Only Fields

| Field Name | Required | Table | Column | API Field | Used In | Description |
|---|---:|---|---|---|---|---|
| Vendor Type | Yes | `BP.jsMaster` | `type` | `vendorType` | SQL, SAP | Vendor marker, normalized to `V` |
| PAN | Yes | `BP.jsTaxDetails` | `panNo` | `pan` | Tax, SAP fiscal tax | Vendor PAN |
| TAN | No | `BP.jsTaxDetails` | `buyerTANNo` | `tan` | Tax, SAP UDF | Vendor TAN |
| FSSAI License | No | `BP.jsTaxDetails` | `fssaiNo` | `fssaiLicense` | Tax, SAP UDF | Vendor FSSAI license |
| MSME | No | `BP.jsTaxDetails` | `msmeNo` | `msme` | Tax, SAP UDF | Vendor MSME/Udyam number |

### Contact Fields

| Field Name | Required | Table | Column | API Field | Used In | Description |
|---|---:|---|---|---|---|---|
| First Name | Recommended | `BP.jsContactPersons` | `firstName` | `firstName` | Details, SAP OCPR | Contact first name |
| Last Name | No | `BP.jsContactPersons` | `lastName` | `lastName` | Details, SAP OCPR | Contact last name |
| Designation | No | `BP.jsContactPersons` | `designation` | `designation`, `title` | Details, SAP OCPR | Role/title |
| Mobile Number | Recommended | `BP.jsMaster`, `BP.jsContactPersons` | `mobileNo`, `mobileNumber` | `mobileNumber`, `mobile` | Header phone, contact, SAP | Primary mobile |
| Alternate Contact | No | `BP.jsContactPersons` | `alternateContact` | `alternateContact` | Details, SAP OCPR phone | Alternate phone/contact |
| Email Address | Recommended | `BP.jsContactPersons` | `emailAddress` | `emailAddress` | Details, SAP OCRD/OCPR | Primary email |
| Alternate Email | No | `BP.jsContactPersons` | `alternateEmail` | `alternateEmail` | Details | Secondary email |

### Address Fields

| Field Name | Required | Table | Column | API Field | Used In | Description |
|---|---:|---|---|---|---|---|
| Address Type | Yes | `BP.jsMasterAddress` | `addressType` | derived from `billingAddresses` / `shippingAddresses` | SQL, SAP CRD1 | `B` for bill-to, `S` for ship-to |
| Address Name | Recommended | `BP.jsMasterAddress` | `addressName` | `addressName` | SAP CRD1 | SAP address identifier |
| Street | Yes | `BP.jsMasterAddress` | `addressLine1` | `street` | Details, SAP CRD1 | Street/address line |
| Block Area | No | `BP.jsMasterAddress` | `addressLine2` | `blockArea` | Details, SAP CRD1 | Area/block/locality |
| City | Yes | `BP.jsMasterAddress` | `cityID` | `city` | Details, SAP CRD1 | City |
| State | Yes | `BP.jsMasterAddress` | `stateID` | `state` | Details, SAP CRD1 | State name or SAP code |
| Pin Code | Yes | `BP.jsMasterAddress` | `pincode` | `pinCode` | Details, SAP CRD1 | Postal code |
| Country | Yes | `BP.jsMasterAddress` | `countryID` | `country` | Details, SAP CRD1 | Country name/code |
| GSTIN | Conditional | `BP.jsMasterAddress`, `BP.jsTaxDetails` | `gstNo`, `gstin` | `gstin` | Tax, SAP CRD1 | GST registration number |

### Tax Fields

| Field Name | Required | Table | Column | API Field | Used In | Description |
|---|---:|---|---|---|---|---|
| GSTIN | Conditional | `BP.jsTaxDetails` | `gstin` | `gstin` | Tax, SAP CRD1 | Header GSTIN/default GSTIN |
| PAN Number | Yes | `BP.jsTaxDetails` | `panNo` | `panNumber`, `pan` | SAP fiscal tax | PAN |
| TAN | Vendor optional | `BP.jsTaxDetails` | `buyerTANNo` | `tan` | SAP UDF | TAN |
| MSME | No | `BP.jsTaxDetails` | `msmeNo` | `msme` | SAP UDF | MSME/Udyam |
| FSSAI License | Vendor optional | `BP.jsTaxDetails` | `fssaiNo` | `fssaiLicense` | SAP UDF | FSSAI license |

### Bank Fields

| Field Name | Required | Table | Column | API Field | Used In | Description |
|---|---:|---|---|---|---|---|
| Bank Name | Vendor recommended | `BP.jsBankDetails` | `name` | `bankName` | Details, SAP OCRB | Bank name/code |
| Branch Name | No | `BP.jsBankDetails` | `branch` | `branchName` | Details, SAP OCRB | Bank branch |
| Account Number | Vendor recommended | `BP.jsBankDetails` | `accountNo` | `accountNumber` | Details, SAP OCRB | Bank account |
| IFSC Code | Vendor recommended | `BP.jsBankDetails` | `ifscCode` | `ifscCode` | Details, SAP OCRB | IFSC |
| SWIFT Code | No | `BP.jsBankDetails` | `swiftCode` | `swiftCode` | Details, SAP OCRB | SWIFT/IBAN support |
| Account Type | No | `BP.jsBankDetails` | `accountType` | `accountType` | Details | Current/savings/etc. |

### Attachment Fields

| Field Name | Required | Table | Column | API Field | Used In | Description |
|---|---:|---|---|---|---|---|
| File Name | Backend generated | `BP.jsAttachments` | `fileName` | multipart file | Details, SAP attachments | Stored file name |
| File Path | Backend generated | `BP.jsAttachments` | `filePath` | backend generated | Download, SAP attachments | Relative upload folder |
| File Size | Backend generated | `BP.jsAttachments` | `fileSize` | backend generated | Details | File size in bytes |
| Content Type | Backend generated | `BP.jsAttachments` | `contentType` | backend generated | Details | MIME type |
| File Type | Yes per file | `BP.jsAttachments` | `fileType` | `fileTypes` | Details | Business document category |

---

## 3. Removed Legacy Fields

These fields are removed from active frontend and backend usage because they are not present in the current Customer Registration or Vendor Registration portal forms.

| Removed Field | Old Table | Reason Removed | Replacement |
|---|---|---|---|
| `staffCode` | `BP.jsMaster` | Not present in new portal UI | None; `isStaff` remains |
| `groupID` | `BP.jsMaster` | Old SAP grouping field hidden from UI | SAP setup no longer driven by portal form |
| `mainGroupID` | `BP.jsMaster` | Old UDF hidden from UI | `industry` or `typeOfBusiness` where applicable |
| `chain` | `BP.jsMaster` | Old chain field hidden from UI | None |
| `contactPerson` | `BP.jsMaster` | Replaced by structured contact fields | `firstName`, `lastName`, `designation` |
| `paymentTermID` | `BP.jsMaster` | Old payment term field hidden from UI | None |
| `creditLimit` | `BP.jsMaster` | Credit limit moved out of BP registration | Credit Limit module |
| `priceList` | `BP.jsMaster` | Old price list field hidden from UI | None |
| `email` | `BP.jsMasterAddress` | Address-level email removed | `emailAddress` in contacts |
| `isDefault` | `BP.jsMasterAddress` | UI uses billing/shipping arrays instead | Address array type |
| `gstType` | `BP.jsMasterAddress` | Derived by SAP mapping when GSTIN valid | Valid GSTIN sets SAP GST type |
| `addressUid` | `BP.jsMasterAddress` | Renamed for portal clarity | `addressName` |
| `email` | `BP.jsContactPersons` | Renamed for portal clarity | `emailAddress` |
| `phone` | `BP.jsContactPersons` | Renamed for portal clarity | `mobileNumber` |
| `telephone` | `BP.jsContactPersons` | Renamed for portal clarity | `alternateContact` |
| `isPrimary` | `BP.jsContactPersons` | UI currently submits one primary contact block | First contact row |
| `contactUid` | `BP.jsContactPersons` | UID helper removed from UI | None |
| `msmeType` | `BP.jsTaxDetails` | Not present in new UI | `msme` |
| `msmeBusinessType` | `BP.jsTaxDetails` | Not present in new UI | `msme` |
| `countryID` | `BP.jsBankDetails` | Bank country not in new UI | None |
| `acctName` | `BP.jsBankDetails` | Account holder not in new UI | `companyName` is used as SAP account name |

Removed helper routes:

- `GET /api/BPmaster/CheckAddressUid`
- `GET /api/BPmaster/CheckContactUid`

Removed helper procedures in cleanup script:

- `BP.jsGetAddressUid`
- `BP.jsGetContactUid`
- `BP.jsUpdateBPMasterData_DEBUG`

### Removed Stored Procedure Parameters and DTO/API Fields

The active SQL procedure contracts and .NET DTOs were aligned to the new Customer Registration and Vendor Registration forms. These retired inputs must not be reintroduced in frontend payloads, Swagger examples, validation rules, SQL TVPs, or SAP mapping without a new UI requirement.

| Removed Input/Property | Previous Area | Reason Removed | Current Rule |
|---|---|---|---|
| `groupID` / `GroupCode` | DTO, procedures, SAP payload | Not shown in new frontend | Do not send to SAP from BP registration |
| `mainGroupID` / `U_Main_Group` | DTO, procedures, SAP payload | Not shown in new frontend | Use `industry` or `typeOfBusiness` only when relevant |
| `chain` / `U_Chain` | DTO, procedures, SAP payload | Not shown in new frontend | No replacement |
| `contactPerson` | DTO, procedures | Replaced by structured contact fields | Use `firstName`, `lastName`, `designation` |
| `paymentTermID` / `PayTermsGrpCode` | DTO, procedures, SAP payload | Not shown in new frontend | Payment term is not portal-controlled |
| `creditLimit` / `CreditLimit` | DTO, procedures, SAP payload | Not part of BP registration | Managed by the credit-limit module |
| `priceList` | DTO, procedures, SAP payload | Not shown in new frontend | No replacement |
| `staffCode` | DTO, procedures | Old staff code field removed | Keep only `isStaff` |
| `addressUid` | Address DTO/procedure | Renamed for portal clarity | Use `addressName` |
| `contactUid` | Contact DTO/procedure | UID helper removed from UI | No replacement |
| `msmeType`, `msmeBusinessType` | Tax DTO/procedure/SAP UDFs | Not shown in new frontend | Use `msme` as MSME/Udyam value |
| `acctName`, `countryID` | Bank DTO/procedure | Not shown in new frontend | Use company name as SAP account name where needed |

### Migration and Rollback Notes

The cleanup script is `docs/implementation/bp-remove-unused-columns.sql`.

| Requirement | How the migration handles it |
|---|---|
| Transaction safety | Uses `SET XACT_ABORT ON` and wraps schema changes in one transaction |
| Rollback on error | Any runtime error rolls back the active transaction |
| Backup before drop | Stores retired column values in `BP.*_RemovedColumnsBackup` tables with `MigrationRunId` |
| IF EXISTS checks | Uses object/column existence checks before adding, backing up, or dropping |
| Workflow preservation | Does not drop `BP.jsFlow`, `BP.jsFlowStatus`, stages, templates, audit, SAP, or attachment tables |
| `isStaff` preservation | Keeps `BP.jsMaster.isStaff` as an active field |

After a committed migration, rollback of removed business columns requires recreating the dropped columns and restoring values from the matching backup table and `MigrationRunId`. Workflow, approval history, SAP status, snapshots, and attachments should never be rolled back by deleting rows.

---

## 4. Database Table Structure

### `BP.jsMaster`

Purpose: BP header/master record.

Important columns:

| Column | Meaning |
|---|---|
| `code` | BP portal primary key |
| `type` | `C` customer or `V` vendor |
| `isStaff` | Preserved staff flag |
| `name` | Company name |
| `foreignName` | Foreign/trade name |
| `typeOfBusiness` | Customer business type |
| `industry` | Industry/sector |
| `mobileNo` | Header mobile number |
| `currency` | SAP currency |
| `remarks` | Notes |
| `company` | Company id |
| `userId` | Creator |
| `companyByUser` | Creator company label |

Inserted by: `BP.jsInsertBPMasterData`.

Used by: lists, details, approval flow, SAP `OCRD`.

### `BP.jsMasterAddress`

Purpose: Billing and shipping address rows.

Important columns:

| Column | Meaning |
|---|---|
| `addressID` | Address primary key |
| `code` | FK to `BP.jsMaster.code` |
| `addressType` | `B` bill-to or `S` ship-to |
| `addressLine1` | Street |
| `addressLine2` | Block/area |
| `stateID` | State |
| `cityID` | City |
| `pincode` | Pin code |
| `countryID` | Country |
| `gstNo` | Address GSTIN |
| `addressName` | SAP address identifier |

Inserted by: create/update procedures.

Used by: detail screen, SAP `CRD1`, PAN fiscal tax address link.

### `BP.jsContactPersons`

Purpose: BP contact person details.

Important columns:

| Column | Meaning |
|---|---|
| `contactID` | Contact primary key |
| `code` | FK to `BP.jsMaster.code` |
| `firstName` | Contact first name |
| `lastName` | Contact last name |
| `designation` | Title/designation |
| `emailAddress` | Primary email |
| `alternateEmail` | Secondary email |
| `mobileNumber` | Primary mobile |
| `alternateContact` | Alternate phone/contact |

Inserted by: create/update procedures.

Used by: detail screen, SAP `OCPR`, list summaries.

### `BP.jsTaxDetails`

Purpose: BP tax and compliance fields.

Important columns:

| Column | Meaning |
|---|---|
| `taxDetailID` | Tax primary key |
| `code` | FK to `BP.jsMaster.code` |
| `buyerTANNo` | TAN |
| `panNo` | PAN |
| `fssaiNo` | FSSAI license |
| `msmeNo` | MSME/Udyam |
| `gstin` | Header/default GSTIN |

Inserted by: create/update procedures.

Used by: details, SAP `BPFiscalTaxIDCollection`, SAP UDFs.

### `BP.jsBankDetails`

Purpose: Vendor bank details.

Important columns:

| Column | Meaning |
|---|---|
| `bankDetailID` | Bank primary key |
| `code` | FK to `BP.jsMaster.code` |
| `name` | Bank name/code |
| `branch` | Branch name |
| `accountNo` | Account number |
| `ifscCode` | IFSC |
| `swiftCode` | SWIFT/IBAN value |
| `accountType` | Account type |

Inserted by: create/update procedures when vendor bank data is supplied.

Used by: details, SAP `OCRB`.

### `BP.jsAttachments`

Purpose: Uploaded BP document metadata.

Important columns:

| Column | Meaning |
|---|---|
| `attachmentID` | Attachment primary key |
| `code` | FK to `BP.jsMaster.code` |
| `fileName` | Stored file name |
| `filePath` | Upload path |
| `fileSize` | Size in bytes |
| `contentType` | MIME type |
| `uploadedOn` | Upload timestamp |
| `fileType` | Business document type |

Inserted by: controller upload handling and create/update procedures.

Used by: download/details and SAP `Attachments2`.

### `BP.jsFlow`

Purpose: Runtime workflow state for each BP.

Important columns:

| Column | Meaning |
|---|---|
| `id` | Flow id |
| `bpCode` | BP code |
| `status` | `P`, `A`, `R` |
| `currentStageId` | Current stage id |
| `templateId` | Approval template |
| `totalStage` | Total stage count |
| `currentStage` | Current stage priority |
| `createdOn`, `updatedOn` | Flow timestamps |

Inserted by: BP workflow trigger/procedure after master insert.

Used by: pending/approved/rejected logic, approval flow, final-stage SAP trigger.

### `BP.jsFlowStatus`

Purpose: Stage action history.

Important columns:

| Column | Meaning |
|---|---|
| `flowId` | FK to `BP.jsFlow.id` |
| `status` | `A`, `R`, `Revoked`, etc. |
| `stageId` | Stage acted on |
| `templateId` | Template id |
| `userId` | Approver |
| `createdOn` | Action timestamp |
| `description` | Remarks |

Inserted by: `BP.jsApproveBP` and `BP.jsRejectBP`.

Used by: approval history, duplicate action prevention, approved/rejected lists.

### `BP.jsSAPData`

Purpose: SAP posting setup and result status.

Important columns:

| Column | Meaning |
|---|---|
| `masterId` | BP code |
| `apiStatusTag` | `P`, `Y`, `N` |
| `apiMessage` | SAP result/error |
| `sapCardCode` | Generated SAP card code |
| `sapAttachmentEntry` | SAP attachment entry |
| `payloadHash` | SHA-256 hash of SAP payload |
| `retryCount` | SAP posting attempts |
| `lastAttemptOn` | Last SAP attempt timestamp |
| `lastAttemptBy` | User who triggered last attempt |

Used by: final approval gate, retry button, SAP diagnostics.

### Snapshot and Audit Tables

| Table | Purpose | Data Inserted When |
|---|---|---|
| `BP.jsAuditLog` | Field/table operation history | During update/restore operations |
| `BP.jsMasterSnapshot` | Master before-update snapshot | Before update/restore |
| `BP.jsMasterAddressSnapshot` | Address snapshot | Before address replacement |
| `BP.jsContactPersonsSnapshot` | Contact snapshot | Before contact replacement |
| `BP.jsBankDetailsSnapshot` | Bank snapshot | Before bank replacement |
| `BP.jsTaxDetailsSnapshot` | Tax snapshot | Before tax update |

### Workflow Configuration Tables

| Table | Purpose |
|---|---|
| `dbo.jsTemplate` | Approval template per module/company |
| `dbo.jsStageTemplate` | Stages linked to template with priority |
| `dbo.jsStage` | Stage metadata and approval/rejection requirements |
| `dbo.jsUserStage` | Users assigned to stages |

Relation summary:

```text
BP.jsMaster.code
  -> BP.jsMasterAddress.code
  -> BP.jsContactPersons.code
  -> BP.jsTaxDetails.code
  -> BP.jsBankDetails.code
  -> BP.jsAttachments.code
  -> BP.jsFlow.bpCode
       -> BP.jsFlowStatus.flowId
  -> BP.jsSAPData.masterId
```

---

## 5. Data Flow Mapping

### Header Fields

| Frontend Field | API Payload | Stored In | Used In | Displayed In |
|---|---|---|---|---|
| Company Name | `companyName` | `BP.jsMaster.name` | SAP `OCRD.CardName` | Pending, approved, rejected, detail |
| Customer Type | `customerType` | `BP.jsMaster.type = C` | SAP `CardType = cCustomer` | Lists, detail |
| Vendor Type | `vendorType` | `BP.jsMaster.type = V` | SAP `CardType = cSupplier` | Lists, detail |
| Foreign Name | `foreignName` / `foreignTradeName` | `BP.jsMaster.foreignName` | SAP `CardForeignName` | Detail |
| Type Of Business | `typeOfBusiness` | `BP.jsMaster.typeOfBusiness` | SAP UDF | Detail |
| Industry | `industry` / `industrySector` | `BP.jsMaster.industry` | SAP UDF | Detail |
| Currency | `currency` | `BP.jsMaster.currency` | SAP `Currency` | Detail |
| Remarks | `remarks` | `BP.jsMaster.remarks` | SAP `Notes` | Detail |
| Is Staff | `isStaff` | `BP.jsMaster.isStaff` | Workflow/list flag | Lists, detail |

### Contact Fields

| Frontend Field | API Payload | Stored In | Used In | Displayed In |
|---|---|---|---|---|
| First Name | `firstName` | `BP.jsContactPersons.firstName` | SAP `OCPR.FirstName` | Detail |
| Last Name | `lastName` | `BP.jsContactPersons.lastName` | SAP `OCPR.LastName` | Detail |
| Designation | `designation` / `title` | `BP.jsContactPersons.designation` | SAP `OCPR.Position` | Detail |
| Mobile | `mobileNumber` / `mobile` | `BP.jsMaster.mobileNo`, `BP.jsContactPersons.mobileNumber` | SAP `Phone1`, `MobilePhone` | Lists, detail |
| Alternate Contact | `alternateContact` | `BP.jsContactPersons.alternateContact` | SAP `OCPR.Phone1` | Detail |
| Email | `emailAddress` | `BP.jsContactPersons.emailAddress` | SAP `OCRD.EmailAddress`, `OCPR.E_Mail` | Detail |
| Alternate Email | `alternateEmail` | `BP.jsContactPersons.alternateEmail` | Portal only | Detail |

### Address Fields

| Frontend Field | API Payload | Stored In | Used In | Displayed In |
|---|---|---|---|---|
| Billing Address | `billingAddresses[]` | `BP.jsMasterAddress.addressType = B` | SAP `bo_BillTo` | Detail |
| Shipping Address | `shippingAddresses[]` | `BP.jsMasterAddress.addressType = S` | SAP `bo_ShipTo` | Detail |
| Address Name | `addressName` | `BP.jsMasterAddress.addressName` | SAP address key | Detail |
| Street | `street` | `BP.jsMasterAddress.addressLine1` | SAP `Street` | Detail |
| Block Area | `blockArea` | `BP.jsMasterAddress.addressLine2` | SAP `Block` | Detail |
| City | `city` | `BP.jsMasterAddress.cityID` | SAP `City` | Detail |
| State | `state` | `BP.jsMasterAddress.stateID` | SAP `State` | Detail |
| Pin Code | `pinCode` | `BP.jsMasterAddress.pincode` | SAP `ZipCode` | Detail |
| Country | `country` | `BP.jsMasterAddress.countryID` | SAP `Country` | Detail |
| GSTIN | `gstin` | `BP.jsMasterAddress.gstNo` | SAP `GSTIN` | Detail |

### Tax and Bank Fields

| Frontend Field | API Payload | Stored In | Used In | Displayed In |
|---|---|---|---|---|
| PAN | `panNumber` / `pan` | `BP.jsTaxDetails.panNo` | SAP fiscal tax | Detail |
| TAN | `tan` | `BP.jsTaxDetails.buyerTANNo` | SAP UDF | Detail |
| GSTIN | `gstin` | `BP.jsTaxDetails.gstin` | SAP address GSTIN fallback | Detail |
| MSME | `msme` | `BP.jsTaxDetails.msmeNo` | SAP UDF | Detail |
| FSSAI | `fssaiLicense` | `BP.jsTaxDetails.fssaiNo` | SAP UDF | Detail |
| Bank Name | `bankName` | `BP.jsBankDetails.name` | SAP `OCRB.BankCode` | Detail |
| Branch Name | `branchName` | `BP.jsBankDetails.branch` | SAP `OCRB.Branch` | Detail |
| Account Number | `accountNumber` | `BP.jsBankDetails.accountNo` | SAP `OCRB.AccountNo` | Detail |
| IFSC Code | `ifscCode` | `BP.jsBankDetails.ifscCode` | SAP `BICSwiftCode`, `UserNo1` | Detail |
| SWIFT Code | `swiftCode` | `BP.jsBankDetails.swiftCode` | SAP `IBAN` | Detail |
| Account Type | `accountType` | `BP.jsBankDetails.accountType` | Portal only | Detail |

---

## 6. Approval Workflow

### Current Approval Stages

| Stage | `currentStage` | UserId | Role |
|---:|---:|---:|---|
| 1 | `1` | `70` | Manager Approval |
| 2 | `2` | `69` | Accounts Approval |
| 3 | `3` | `108` | SAP Team Final Approval |

### How `currentStage` Works

`BP.jsFlow.currentStage` stores the priority number of the current stage. `BP.jsFlow.currentStageId` stores the actual stage id from `dbo.jsStage`.

### How `totalStage` Works

`BP.jsFlow.totalStage` stores the total stage count. The backend treats a flow as final when:

```csharp
CurrentStage >= TotalStage
```

This is why the current template must keep `totalStage = 3`.

### How Next Approver Is Determined

Pending records are selected by matching the logged-in user to `BP.jsFlow.currentStageId` through `dbo.jsUserStage`.

```sql
SELECT DISTINCT us.stageId
FROM dbo.jsUserStage us
JOIN dbo.jsStageTemplate st ON us.stageId = st.stageId
JOIN dbo.jsTemplate t ON st.templateId = t.id
WHERE us.userId = @userId
  AND t.company = @companyId
  AND t.isActive = 1
  AND ISNULL(us.status, 1) = 1;
```

### Workflow Diagram

```text
Create BP
  |
  v
BP.jsMaster + child tables inserted
  |
  v
BP.jsFlow created with status P, currentStage 1
  |
  v
Stage 1 Manager userId 70 approves
  |
  v
Stage 2 Accounts userId 69 approves
  |
  v
Stage 3 SAP Team userId 108 approves
  |
  v
Backend posts to SAP Service Layer
  |
  +-- SAP success -> apiStatusTag Y -> BP.jsFlow status A
  |
  +-- SAP failure -> apiStatusTag N -> BP.jsFlow remains P -> Retry shown
```

### Pending Logic

`GetPendingBP` returns only records where:

- `BP.jsFlow.status = P`
- BP belongs to the requested company
- current stage is assigned to the requested user
- the user has not already approved/rejected the current stage

### Approved Logic

`GetApprovedBP` reads `BP.jsFlowStatus` rows with `status = A` for the user and joins the BP master record.

### Rejected Logic

`GetRejectedBP` reads `BP.jsFlowStatus` rows with `status = R` for the user and joins the BP master record.

### Final SAP Trigger Rule

SAP posting only runs on final-stage approval. Stage 1 and stage 2 approvals only move the workflow forward.

---

## 7. SAP Integration

### When SAP API Triggers

SAP posting triggers only from:

- `POST /api/BPmaster/ApproveBP` when the BP is at final stage
- `POST /api/BPmaster/RetrySapPost` when final-stage SAP posting previously failed

### SAP Tables/Objects Created

| SAP object | Source |
|---|---|
| `OCRD` Business Partner header | `BP.jsMaster`, `BP.jsTaxDetails` |
| `CRD1` Addresses | `BP.jsMasterAddress` |
| `OCPR` Contact employees | `BP.jsContactPersons` |
| `OCRB` Vendor bank accounts | `BP.jsBankDetails` |
| `Attachments2` | `BP.jsAttachments` |

### SAP Header Mapping

| Portal/DB field | SAP field |
|---|---|
| Generated card code | `CardCode` |
| `BP.jsMaster.name` | `CardName` |
| `type = C/V` | `CardType = cCustomer/cSupplier` |
| `foreignName` | `CardForeignName` |
| `mobileNo` | `Phone1` |
| `emailAddress` | `EmailAddress` |
| `currency` | `Currency` |
| `remarks` | `Notes` |
| `typeOfBusiness` | `U_TypeOfBusiness` |
| `industry` | `U_Industry` |
| `fssaiNo` | `U_Fssai` |
| `msmeNo` | `U_MSME` |
| `buyerTANNo` | `U_TAN` |
| SAP attachment entry | `AttachmentEntry` |

### `apiStatusTag`

| Value | Meaning | UI behavior |
|---|---|---|
| `P` | SAP processing | Show processing badge, disable duplicate approve |
| `Y` | SAP success | Show success badge, final approval completes |
| `N` | SAP failed | Show failed badge, allow retry at final stage |
| `NULL` | SAP not started | Normal pending state before final stage |

### SAP Status Columns

| Column | Meaning |
|---|---|
| `sapCardCode` | SAP-confirmed CardCode; must remain `NULL` for failed or processing SAP posts |
| `sapAttachmentEntry` | SAP `Attachments2.AbsoluteEntry` |
| `retryCount` | Number of SAP processing attempts |
| `payloadHash` | SHA-256 hash of SAP payload |
| `apiMessage` | Success or error message |
| `lastAttemptOn` | Last SAP attempt timestamp |
| `lastAttemptBy` | User who triggered the last attempt |

### Retry Logic

`RetrySapPostAsync`:

1. Loads the flow runtime.
2. Verifies company access.
3. Verifies flow is pending.
4. Verifies flow is at final stage.
5. Checks `apiStatusTag`.
6. Allows retry only when tag is `N` or already `Y`.
7. Calls normal final approval path with `Action = Approve`.

Retry is never allowed for stage 1 or stage 2.

---

## 8. API Documentation

Base route:

```text
/api/BPmaster
```

### Create BP

| Item | Value |
|---|---|
| URL | `/api/BPmaster/InsertBPmasterData` |
| Method | `POST` |
| Content type | `multipart/form-data` |
| Purpose | Create customer/vendor BP request and start workflow |

Form fields:

| Key | Type | Description |
|---|---|---|
| `requests` | Text | JSON request body |
| `files` | File list | Attachments |
| `fileTypes` | Text | Comma-separated file type labels |

Customer request example:

```json
{
  "companyId": 1,
  "customerType": "C",
  "companyName": "North India Distributor",
  "foreignName": "North India Distributor",
  "typeOfBusiness": "Distributor",
  "industry": "FMCG",
  "firstName": "Ramesh",
  "lastName": "Kumar",
  "designation": "Owner",
  "mobileNumber": "9876543210",
  "emailAddress": "ramesh@example.com",
  "alternateEmail": "accounts@example.com",
  "gstin": "03ABCDE1234F1Z5",
  "panNumber": "ABCDE1234F",
  "currency": "INR",
  "msme": "",
  "remarks": "Customer registration",
  "isStaff": false,
  "userId": 107,
  "companyByUser": "Jivo Oil",
  "billingAddresses": [
    {
      "addressName": "BILL-PB-001",
      "street": "Plot 14 Industrial Area",
      "blockArea": "Phase 2",
      "city": "Ludhiana",
      "state": "PB",
      "pinCode": "141001",
      "country": "IN",
      "gstin": "03ABCDE1234F1Z5"
    }
  ],
  "shippingAddresses": []
}
```

Vendor request example:

```json
{
  "companyId": 1,
  "vendorType": "V",
  "companyName": "ABC Bottle Supplier Pvt Ltd",
  "foreignTradeName": "ABC Bottles",
  "industrySector": "Packaging",
  "firstName": "Amit",
  "lastName": "Sharma",
  "designation": "Sales Head",
  "mobile": "9876543210",
  "alternateContact": "0161123456",
  "emailAddress": "amit@example.com",
  "gstin": "03AAKCA1234F1Z1",
  "pan": "AAKCA1234F",
  "tan": "PTLA12345B",
  "currency": "INR",
  "msme": "UDYAM-PB-00-0001234",
  "fssaiLicense": "10012022000011",
  "bankName": "HDFC",
  "branchName": "Ludhiana",
  "accountNumber": "50100123456789",
  "ifscCode": "HDFC0001234",
  "swiftCode": "HDFCINBB",
  "accountType": "Current",
  "remarks": "Vendor registration",
  "isStaff": false,
  "userId": 107,
  "companyByUser": "Jivo Oil",
  "billingAddresses": [],
  "shippingAddresses": []
}
```

Response example:

```json
{
  "success": true,
  "message": "BP Master inserted successfully.",
  "generatedCode": 1234
}
```

Validation notes:

- `companyId`, `companyName`, BP type, and PAN are required.
- File count must match `fileTypes` count.
- Attachments are optional.

### Update BP

| Item | Value |
|---|---|
| URL | `/api/BPmaster/UpdateBPMaster` |
| Method | `POST` |
| Content type | `multipart/form-data` |
| Purpose | Update BP data and optionally replace child rows/attachments |

Request body is the same as create, plus:

```json
{
  "code": 1234,
  "updateAddresses": true,
  "updateBankDetails": true,
  "updateContacts": true,
  "updateAttachments": false
}
```

Response:

```json
{
  "success": true,
  "message": "BP Master updated successfully."
}
```

Important notes:

- Update creates snapshots and audit rows.
- Child tables are replaced only when the corresponding `update...` flag is true.

### Get Single BP Data

| Item | Value |
|---|---|
| URL | `/api/BPmaster/GetSingleBPData?bpCode=1234` |
| Method | `GET` |
| Purpose | Load full BP detail |

Response example:

```json
{
  "master": {
    "code": 1234,
    "type": "C",
    "isStaff": false,
    "name": "North India Distributor",
    "foreignName": "North India Distributor",
    "typeOfBusiness": "Distributor",
    "industry": "FMCG",
    "mobileNumber": "9876543210",
    "currency": "INR",
    "remarks": "Customer registration",
    "company": 1,
    "flowId": 1115
  },
  "taxDetails": {
    "panNumber": "ABCDE1234F",
    "gstin": "03ABCDE1234F1Z5",
    "msme": ""
  },
  "billingAddresses": [],
  "shippingAddresses": [],
  "bankDetails": [],
  "contactPersons": [],
  "attachments": []
}
```

### Get Pending BP

| Item | Value |
|---|---|
| URL | `/api/BPmaster/GetPendingBP?userId=70&companyId=1&month=05-2026` |
| Method | `GET` |
| Purpose | Show current user's pending BP approvals |

Response example:

```json
{
  "success": true,
  "data": [
    {
      "flowId": 1115,
      "code": 1234,
      "companyId": 1,
      "type": "C",
      "companyName": "North India Distributor",
      "isStaff": false,
      "currentStage": 1,
      "totalStage": 3,
      "currentStageName": "Manager Approval",
      "isFinalStage": false,
      "apiStatusTag": null,
      "sapStatus": "SAP Not Started",
      "canRetrySap": false
    }
  ]
}
```

Notes:

- User sees only records assigned to their current stage.
- Final-stage failed SAP records return `canRetrySap = true`.

### Get Approved BP

| Item | Value |
|---|---|
| URL | `/api/BPmaster/GetApprovedBP?userId=70&companyId=1&month=05-2026` |
| Method | `GET` |
| Purpose | Show records approved by the user |

Response:

```json
{
  "success": true,
  "data": [
    {
      "flowId": 1115,
      "id": 1234,
      "companyName": "North India Distributor",
      "type": "C",
      "createdOn": "2026-05-22T10:30:00"
    }
  ]
}
```

### Get Rejected BP

| Item | Value |
|---|---|
| URL | `/api/BPmaster/GetRejectedBP?userId=69&companyId=1&month=05-2026` |
| Method | `GET` |
| Purpose | Show records rejected by the user |

Response:

```json
{
  "success": true,
  "data": [
    {
      "flowId": 1115,
      "id": 1234,
      "companyName": "ABC Bottle Supplier Pvt Ltd",
      "type": "V",
      "remark": "Bank document missing"
    }
  ]
}
```

### Approve BP

| Item | Value |
|---|---|
| URL | `/api/BPmaster/ApproveBP` |
| Method | `POST` |
| Purpose | Approve current stage; final stage posts SAP first |

Request:

```json
{
  "flowId": 1115,
  "company": 1,
  "userId": 70,
  "remarks": "Verified.",
  "action": "Approve"
}
```

Response:

```json
{
  "success": true,
  "data": {
    "success": true,
    "resultMessage": "BP moved to next stage",
    "bpCode": 1234,
    "bpCompany": 1,
    "approvalStatus": "Advanced",
    "sapStatus": "Not final stage"
  }
}
```

Final-stage SAP success response:

```json
{
  "success": true,
  "data": {
    "approvalStatus": "Approved",
    "sapStatus": "Success",
    "sapCardCode": "CUSTA000123",
    "attachmentEntry": 456,
    "payloadHash": "A1B2C3..."
  }
}
```

### Reject BP

| Item | Value |
|---|---|
| URL | `/api/BPmaster/RejectBP` |
| Method | `POST` |
| Purpose | Reject current approval stage |

Request:

```json
{
  "flowId": 1115,
  "company": 1,
  "userId": 69,
  "remarks": "PAN attachment mismatch.",
  "action": "Reject"
}
```

Response:

```json
{
  "success": true,
  "data": {
    "approvalStatus": "Rejected",
    "sapStatus": "Not applicable",
    "resultMessage": "BP rejected successfully"
  }
}
```

### Retry SAP Post

| Item | Value |
|---|---|
| URL | `/api/BPmaster/RetrySapPost` |
| Method | `POST` |
| Purpose | Retry failed final-stage SAP posting |

Request:

```json
{
  "flowId": 1115,
  "company": 1,
  "userId": 108,
  "remarks": "Retry after SAP correction."
}
```

Rules:

- Flow must be pending.
- Flow must be at final stage.
- SAP status must be `N` or already `Y`.
- User must be assigned to final stage.

### Get BP Approval Flow

| Item | Value |
|---|---|
| URL | `/api/BPmaster/GetBPApprovalFlow?flowId=1115` |
| Method | `GET` |
| Purpose | Show stage assignment and action history |

Response:

```json
{
  "success": true,
  "data": [
    {
      "stageId": 1,
      "stageName": "Manager Approval",
      "priority": 1,
      "assignedTo": "Manager User",
      "actionStatus": "A",
      "actionDate": "2026-05-22T10:30:00",
      "description": "Verified",
      "approvalRequired": 1,
      "rejectRequired": 1
    }
  ]
}
```

### Insights APIs

| API | Method | Purpose |
|---|---|---|
| `/GetBPInsights?userId=70&companyId=1&month=05-2026` | `GET` | Pending/approved/rejected counts for approver |
| `/GetBPInsightsByCreator?userId=107&companyId=1&month=05-2026` | `GET` | Counts by creator |
| `/GetBPCounts?month=05-2026&userId=70` | `GET` | BP count summary |

Example:

```json
{
  "success": true,
  "data": [
    {
      "totalPending": 4,
      "totalApproved": 8,
      "totalRejected": 1,
      "totalBP": 13
    }
  ]
}
```

### Combined Approval APIs

| API | Method | Purpose |
|---|---|---|
| `/GetTotalBPData` | `GET` | Combined pending/approved/rejected BP list |
| `/GetAllBpPendingApproval` | `GET` | BP + Item pending approval data |
| `/GetAllBpApprovedApproval` | `GET` | BP + Item approved data |
| `/GetAllBpRejectedApproval` | `GET` | BP + Item rejected data |
| `/GetAllBpTotalApproval` | `GET` | BP + Item total approval data |

### Lookup APIs

These APIs support dropdowns and validation. Some old lookup APIs are retained for compatibility, but new BP forms should use only active fields.

| API | Method | Purpose |
|---|---|---|
| `/GetOptions?company=1&bpType=C&isStaff=false&countryCode=IN` | `GET` | Consolidated active options |
| `/GetDistinctBankName?company=1` | `GET` | Bank options |
| `/GetCountry?company=1` | `GET` | Country options |
| `/GetDistinctStates?company=1&CountryCode=IN` | `GET` | State options |
| `/GetUniquePANs?company=1` | `GET` | Existing PANs |
| `/GetGSTMismatchByState?company=1&stateCode=PB` | `GET` | GST state validation helper |
| `/BPGetCardInfo?company=1&BPType=C&IsStaff=false` | `GET` | Existing SAP card info |
| `/GetBpPANByBranch?Branch=Ludhiana&company=1` | `GET` | PAN lookup by branch |
| `/GetSPAData?masterId=1234` | `GET` | SAP setup/status data |
| `/UpdateSapData` | `POST` | Update SAP setup metadata |

Legacy dropdown APIs such as group, chain, payment term, price list, and MSME type are not part of the active Customer/Vendor registration fields.

---

## 9. Frontend Implementation Guide

### Form Rules

Flutter should maintain separate form models:

- Customer Registration
- Vendor Registration
- Billing addresses
- Shipping addresses
- Attachments

Required fields:

| Field | Customer | Vendor |
|---|---:|---:|
| `companyId` | Yes | Yes |
| `customerType` / `vendorType` | Yes | Yes |
| `companyName` | Yes | Yes |
| `panNumber` / `pan` | Yes | Yes |
| `currency` | Yes | Yes |
| `billingAddresses` | Recommended | Recommended |
| `bankName`, `accountNumber`, `ifscCode` | No | Recommended |
| `isStaff` | Yes | Yes |

### Submit Flow

1. Validate required fields.
2. Build `requests` JSON.
3. Append attachments as multipart files.
4. Send matching comma-separated `fileTypes`.
5. Call `POST /api/BPmaster/InsertBPmasterData`.
6. On success, show generated BP code.
7. Refresh creator dashboard or approval lists as needed.

### Edit Flow

1. Call `GetSingleBPData`.
2. Populate form from master/tax/address/contact/bank/attachments.
3. Submit `UpdateBPMaster`.
4. Set update flags only for sections being replaced.

### Approval Pages

Pending page:

- Call `GetPendingBP`.
- Show company name, type, current stage, SAP badge, created date.
- Show Approve/Reject buttons only when row is pending.

Approved page:

- Call `GetApprovedBP`.
- Show records approved by the current user.

Rejected page:

- Call `GetRejectedBP`.
- Show rejection remarks.

### SAP Status Badges

| `apiStatusTag` | Badge | Button behavior |
|---|---|---|
| `P` | Processing | Disable approve/retry |
| `Y` | SAP Success | No retry |
| `N` | SAP Failed | Show retry only for final stage |
| `NULL` | Not Started | Normal pending |

Retry button condition:

```dart
row.isFinalStage == true &&
row.canRetrySap == true &&
row.apiStatusTag == 'N'
```

### Stage Badges

| Stage | Label |
|---:|---|
| 1 | Manager Approval |
| 2 | Accounts Approval |
| 3 | SAP Final Approval |

After approval/rejection/retry:

1. Show action result.
2. Refresh pending list.
3. Refresh insights count.
4. Refresh approval flow if on detail page.

---

## 10. SQL Verification Queries

Replace `1234`, `1115`, `70`, and `1` with real BP code, flow id, user id, and company id.

### 1. Check BP Master

```sql
SELECT *
FROM BP.jsMaster
WHERE code = 1234;
```

Purpose: Check BP header fields such as name, type, company, `isStaff`, currency, remarks.

### 2. Check Addresses

```sql
SELECT *
FROM BP.jsMasterAddress
WHERE code = 1234
ORDER BY addressType, addressID;
```

Purpose: Verify billing/shipping address rows and GSTIN.

### 3. Check Contacts

```sql
SELECT *
FROM BP.jsContactPersons
WHERE code = 1234
ORDER BY contactID;
```

Purpose: Verify contact name, designation, email, mobile, alternate contact.

### 4. Check Tax Details

```sql
SELECT *
FROM BP.jsTaxDetails
WHERE code = 1234;
```

Purpose: Verify PAN, TAN, GSTIN, MSME, FSSAI.

### 5. Check Bank Details

```sql
SELECT *
FROM BP.jsBankDetails
WHERE code = 1234;
```

Purpose: Verify vendor bank details.

### 6. Check Attachments

```sql
SELECT *
FROM BP.jsAttachments
WHERE code = 1234
ORDER BY uploadedOn DESC;
```

Purpose: Verify uploaded file metadata and file types.

### 7. Check Approval Flow

```sql
SELECT *
FROM BP.jsFlow
WHERE bpCode = 1234;
```

Purpose: Check workflow status, current stage, total stage, and current stage id.

### 8. Check Current Approver

```sql
SELECT
    f.id AS flowId,
    f.bpCode,
    f.currentStage,
    f.currentStageId,
    s.stage AS stageName,
    us.userId,
    u.loginUser
FROM BP.jsFlow f
LEFT JOIN dbo.jsStage s ON s.id = f.currentStageId
LEFT JOIN dbo.jsUserStage us ON us.stageId = f.currentStageId AND ISNULL(us.status, 1) = 1
LEFT JOIN dbo.jsUser u ON u.userId = us.userId
WHERE f.bpCode = 1234;
```

Purpose: Confirm which user should see the BP in pending approvals.

### 9. Check SAP Status

```sql
SELECT *
FROM BP.jsSAPData
WHERE masterId = 1234
ORDER BY id DESC;
```

Purpose: Check `apiStatusTag`, SAP error/success message, card code, retry count, payload hash.

### 10. Check Audit Logs

```sql
SELECT *
FROM BP.jsAuditLog
WHERE Code = 1234
ORDER BY ChangedDate DESC;
```

Purpose: Review field-level update/restore history.

### 11. Check Snapshots

```sql
EXEC BP.jsGetBPSnapshots
    @code = 1234,
    @fromDate = NULL,
    @toDate = NULL;
```

Purpose: Review available snapshots before update/restore.

### 12. Check Pending Approvals

```sql
EXEC BP.jsGetPendingBP
    @userId = 70,
    @companyId = 1,
    @month = NULL;
```

Purpose: Verify pending queue for a specific approver.

### 13. Check Approved History

```sql
EXEC BP.jsGetApprovedBP
    @userId = 70,
    @companyId = 1,
    @month = NULL;
```

Purpose: Verify records approved by a user.

### 14. Check Rejected History

```sql
EXEC BP.jsGetRejectedBP
    @userId = 69,
    @companyId = 1,
    @month = NULL;
```

Purpose: Verify records rejected by a user.

### 15. Full Debug Query

```sql
SELECT TOP 20
    f.id AS flowId,
    f.bpCode,
    m.name AS bpName,
    m.type,
    f.status AS workflowStatus,
    f.currentStage,
    f.totalStage,
    s.stage AS currentStageName,
    u.loginUser AS currentApprover,
    sd.apiStatusTag,
    CASE
        WHEN sd.apiStatusTag = 'Y' THEN 'SAP SUCCESS'
        WHEN sd.apiStatusTag = 'N' THEN 'SAP FAILED'
        WHEN sd.apiStatusTag = 'P' THEN 'SAP PROCESSING'
        ELSE 'SAP NOT STARTED'
    END AS sapStatus,
    sd.apiMessage,
    sd.sapCardCode,
    sd.sapAttachmentEntry,
    sd.retryCount,
    sd.lastAttemptOn
FROM BP.jsFlow f
INNER JOIN BP.jsMaster m ON m.code = f.bpCode
LEFT JOIN dbo.jsStage s ON s.id = f.currentStageId
LEFT JOIN dbo.jsUserStage us ON us.stageId = f.currentStageId AND us.status = 1
LEFT JOIN dbo.jsUser u ON u.userId = us.userId
LEFT JOIN BP.jsSAPData sd ON sd.masterId = f.bpCode
ORDER BY f.id DESC;
```

Purpose: One query to diagnose workflow and SAP status.

---

## 11. Important Notes

- The BP module now follows the new SAP Portal Customer Registration and Vendor Registration forms.
- Only `isStaff` is preserved from the old field set.
- All other retired legacy fields must not be sent by the frontend.
- Approval workflow is 3-stage: `70 -> 69 -> 108`.
- SAP posting happens only at final approval.
- Retry is only available for failed final-stage SAP posting.
- Attachments remain multipart uploads and are posted to SAP through `Attachments2`.
- Audit logs preserve update/restore history.
- Snapshots preserve before-update data for support and rollback workflows.
- `BP.jsFlow`, `BP.jsFlowStatus`, `BP.jsSAPData`, and attachment tables must never be manually deleted during BP cleanup.

---

## 12. Cleanup

The content from these old files has been merged into this guide:

- `docs/BP_FIELD_MAPPING_DOCUMENTATION.md`
- `docs/BP_UNUSED_FIELDS_REPORT.md`

Those files have been removed from `docs`. This file is now the master BP module documentation and should be treated as the single source of truth for BP frontend/backend behavior.

---

## 13. Final Requirements

This guide is intended to be the complete operational reference for the BP Master module. Future changes should keep the frontend form, DTOs, stored procedures, database tables, SAP payload mapping, validation rules, API examples, migration scripts, and this document in sync.

### Troubleshooting Notes

#### BP Not Showing in Pending

Check:

- `BP.jsFlow.status` is `P`
- `currentStageId` is assigned to the logged-in user in `dbo.jsUserStage`
- user has not already approved/rejected the current stage
- BP company matches the selected company

#### Final Approval Fails

Check:

- `BP.jsSAPData.apiStatusTag`
- `BP.jsSAPData.apiMessage`
- attachment source path configuration
- SAP session configuration for company id
- SAP-required values such as PAN, address name, and bank code/name

#### Retry Button Not Visible

Retry should show only when:

- row is final stage
- `apiStatusTag = N`
- `canRetrySap = true`
- current user is the final-stage approver

#### Wrong SAP Data Posted

Check data in this order:

1. `GetSingleBPData` API response
2. `BP.jsMaster` and child tables
3. `BPMasterSapService.BuildBusinessPartnerPayload`
4. `BP.jsSAPData.payloadHash`
5. SAP Service Layer response body

### Safe Change Checklist

Before changing BP fields again:

1. Confirm the frontend form field exists.
2. Update DTOs in `Models/BPmasterModels.cs`.
3. Update `BPmasterService` stored procedure parameters and TVPs.
4. Update SQL migration/procedure script.
5. Update `BPMasterSapService` only if SAP needs the field.
6. Update this document.
7. Build the project.
8. Dry-run SQL changes in a rollback transaction.

Useful files:

| File | Purpose |
|---|---|
| `Controllers/BPmasterController.cs` | API routes |
| `Services/Implementation/BPmasterService.cs` | SQL/workflow orchestration |
| `Services/Implementation/BPMasterSapService.cs` | SAP payload and posting |
| `Models/BPmasterModels.cs` | DTO/request/response models |
| `docs/implementation/bp-remove-unused-columns.sql` | SQL cleanup/migration script |

### Maintainer Rules

- Do not add a BP field unless it exists in the current Customer or Vendor Registration frontend.
- Do not delete workflow, approval, SAP audit, snapshot, or attachment data.
- Do not remove `isStaff`.
- Do not send retired legacy fields to SAP from BP registration.
- Keep retry available only for final-stage SAP failures.
- Validate the .NET build after backend changes.
- Validate stored procedures in a rollback transaction before deploying SQL changes.
- Update this guide in the same pull request as any BP contract change.
