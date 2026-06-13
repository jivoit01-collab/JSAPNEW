# BP Master Frontend and Backend Complete Guide

This document is the single source of truth for the BP Master module in `JSAPNEW`.

It is written for:

- Business users who need to understand what a BP request is
- QA testers who need repeatable test cases
- Support engineers who need production diagnostics
- Flutter frontend developers who need the API contract
- .NET backend developers who need implementation details
- SAP support engineers who need SAP mapping and error context
- Future maintainers and AI coding assistants

The BP module is aligned with the current SAP Portal Customer Registration and Vendor Registration forms. New clients must use the flat Node-compatible request contract. Do not reintroduce old nested `contact`, `tax`, `bank`, `billingAddress`, `shippingAddress`, or legacy business fields unless the frontend form is officially changed.

---

## 1. What Is BP?

### Business Meaning

`BP` means `Business Partner`.

In SAP Business One, a Business Partner is any party that the company buys from or sells to.

| BP Type | Simple Meaning | Real Example | SAP CardType |
|---|---|---|---|
| Customer | Someone Jivo sells goods to | Jivo sells oil to Reliance Retail | `cCustomer` |
| Vendor | Someone Jivo buys goods or services from | Jivo buys bottles from Bharat Packaging Industries | `cSupplier` |

### Customer Example

Jivo sells finished goods to a distributor.

The distributor is created in SAP as a customer BP so invoices, payments, GST, credit limits, and sales data can be tracked.

Example:

```text
Jivo -> sells goods -> Reliance Retail
Reliance Retail = Customer BP
```

### Vendor Example

Jivo buys raw material or packaging from a supplier.

The supplier is created in SAP as a vendor BP so purchase orders, bills, bank details, GST, MSME, and payments can be tracked.

Example:

```text
Jivo -> buys bottles -> Bharat Packaging Industries Pvt Ltd
Bharat Packaging Industries Pvt Ltd = Vendor BP
```

### SAP Terms In Plain English

| SAP Term | Beginner Meaning | What This Module Sends |
|---|---|---|
| `OCRD` | Main Business Partner table | BP name, type, currency, group, control account, payment term |
| `CRD1` | BP address table | Billing and shipping addresses |
| `OCPR` | BP contact person table | Contact first name, last name, email, mobile |
| `OCRB` | BP bank account table | Vendor bank code, account number, IFSC, account holder name |
| `Attachments2` | SAP attachment object | Uploaded files/documents |
| `OACT` | SAP chart of accounts | AR/AP control accounts |
| `OCRG` / `BusinessPartnerGroups` | SAP BP group master | Customer/vendor group |
| `OCTG` | SAP payment terms | Payment term code and name |
| `OSLP` | SAP sales employee master | Sales employee code and name |
| `OTER` | SAP territory master | Territory id and name |
| `ODSC` / `Banks` | SAP bank master | Vendor bank code, name, SWIFT, country |

---

## 2. Business Process Overview

### Full Flow

```text
User creates Customer/Vendor BP request
  |
  v
Backend stores BP data in SQL BP tables
  |
  v
Workflow starts in BP.jsFlow
  |
  v
Manager Approval
  |
  v
Accounts Approval
  |
  v
SAP Approval
  |
  v
Backend posts BP to SAP Service Layer
  |
  +--> SAP success -> workflow approved
  |
  +--> SAP failure -> workflow remains pending, user fixes data, SAP retry allowed
```

### Why The Process Exists

Creating BPs directly in SAP can cause duplicate vendors, wrong tax data, wrong control accounts, invalid bank details, or missing approvals. The portal adds a controlled workflow before data reaches SAP.

### Approval Stages

| Stage | Typical User | Purpose | What They Check |
|---:|---:|---|---|
| 1 | `70` | Manager Approval | Business need, basic details, duplicate possibility |
| 2 | `69` | Accounts Approval | PAN, GSTIN, bank, tax, addresses, documents |
| 3 | `108` | SAP Final Approval | SAP master-data values and final SAP posting |

For test environments, the approver mapping may be changed, for example to user `76`. Production can use different users per stage.

### What Happens At Each Stage

Manager approval:

- Confirms the request is valid.
- Moves the workflow to the next stage.
- Does not post anything to SAP.

Accounts approval:

- Checks financial and compliance fields.
- Moves the workflow to SAP approval.
- Does not post anything to SAP.

SAP approval:

- Reads the latest saved SQL data.
- Builds the SAP `BusinessPartners` payload.
- Uploads attachments when available.
- Posts the BP to SAP.
- Completes the workflow only if SAP confirms the BP was created.

### Why SAP Posting Happens Only At Final Stage

SAP should contain only approved BPs. If the module posted to SAP before approval, bad or incomplete data could become live in SAP. Therefore the final approval is SAP-first:

```text
Final approver clicks approve
  |
  v
SAP posting starts
  |
  +--> SAP success: mark workflow A
  |
  +--> SAP failure: keep workflow P and show retry
```

---

## 3. Active Request Contract

The working Node.js SAP portal is the source of truth for request JSON. The .NET backend supports this flat contract.

| Area | Active Request Fields |
|---|---|
| Header | `company`, `type`, `customerType`, `vendorType`, `cardName`, `foreignName`, `typeOfBusiness`, `industry`, `isStaff`, `currency`, `remarks`, `userId` |
| Contact | `contactFirst`, `contactLast`, `contactTitle`, `mobile`, `email`, `contactEmail`, `altContact` |
| Address | `sameAsBill`, `allBillAddresses[]`, `allShipAddresses[]` |
| Address rows | `addrName`, `street`, `block`, `city`, `zip`, `state`, `country`, `gstin` |
| Tax/MSME | `gstin`, `pan`, `tan`, `hasMsme`, `msmeNo`, `msmeType`, `msmeBType`, `fssaiNo` |
| SAP approval data | Not sent in Create BP or Update BP. Use `/api/BPmaster/UpdateSapData` after the BP request is created. |
| Vendor bank | `bankAccounts[]` |
| Vendor bank rows | `bankCode`, `bankName`, `vendorName`, `branch`, `accNo`, `ifsc`, `swiftCode`, `accountType`, `isPrimary` |
| Attachments | Multipart `files` and comma-separated `fileTypes` |

Retired fields must not be used by new clients:

- `bpName`
- `businessType`
- nested `contact`
- nested `tax`
- nested `msme`
- singular nested `bank`
- `tax.msmeStatus`
- `bankAccounts[].accountNo`
- `bankAccounts[].ifscCode`
- old `groupID`
- old `paymentTermID`
- old `priceList`
- old `staffCode`

`isStaff` is the only preserved legacy flag.

---

## 4. Field Reference Guide

### How To Read This Section

`Sent to SAP` means the field is included directly in the SAP payload or used to build a SAP payload value.

`SQL only` means the field is stored for portal, workflow, display, or audit use but is not posted to SAP unless a future SAP mapping is added.

### Header Fields

| Field | Purpose | Required | Customer/Vendor | Stored In | Sent To SAP | Example | Business Meaning |
|---|---|---:|---|---|---|---|---|
| `company` | Select SAP company/database | Yes | Both | `BP.jsMaster.company` | Used to choose SAP session/schema | `1`, `JIVO_OIL_HANADB` | Determines which SAP company receives the BP |
| `type` | BP type | Yes | Both | `BP.jsMaster.type` | `OCRD.CardType` | `C`, `V` | Tells SAP customer or vendor |
| `customerType` | Customer form marker | Yes for customer | Customer | normalized into `BP.jsMaster.type` | `OCRD.CardType` | `B2B` | Frontend customer classification |
| `vendorType` | Vendor form marker | Yes for vendor | Vendor | normalized into `BP.jsMaster.type` | `OCRD.CardType` | `SUPPLIER` | Frontend vendor classification |
| `cardName` | Legal/display name | Yes | Both | `BP.jsMaster.name` | `OCRD.CardName` | `Bharat Packaging Industries Pvt Ltd` | Main name visible in SAP |
| `foreignName` | Alternate/trade name | No | Both | `BP.jsMaster.foreignName` | `OCRD.CardForeignName` | `Bharat Packaging` | Additional display/search name |
| `typeOfBusiness` | Business type | No | Both | `BP.jsMaster.typeOfBusiness` | SQL only unless SAP UDF is added | `Company` | Portal classification |
| `industry` | Industry/sector | Recommended | Both | `BP.jsMaster.industry` | SQL only unless SAP UDF is added | `Packaging` | Helps reviewers understand business context |
| `isStaff` | Staff flag | Yes | Both | `BP.jsMaster.isStaff` | SQL/workflow only | `false` | Marks employee/staff-related BP |
| `currency` | BP currency | Yes | Both | `BP.jsMaster.currency` | `OCRD.Currency` | `INR` | Transaction currency |
| `remarks` | Notes | No | Both | `BP.jsMaster.remarks` | `OCRD.Notes` | `Vendor registration` | Reviewer/SAP notes |
| `userId` | Creator user id | Yes | Both | `BP.jsMaster.userId` | SQL/audit only | `76` | Shows who created the request |

### Contact Fields

| Field | Purpose | Required | Customer/Vendor | Stored In | Sent To SAP | Example | Business Meaning |
|---|---|---:|---|---|---|---|---|
| `contactFirst` | Contact first name | Yes | Both | `BP.jsContactPersons.firstName` | `OCPR.FirstName` | `Amit` | Person to contact |
| `contactLast` | Contact last name | Yes | Both | `BP.jsContactPersons.lastName` | `OCPR.LastName` | `Sharma` | Person to contact |
| `contactTitle` | Role/title | No | Both | `BP.jsContactPersons.designation` | `OCPR.Position` | `Sales Head` | Contact designation |
| `mobile` | Primary mobile | Recommended | Both | `BP.jsMaster.mobileNo`, `BP.jsContactPersons.mobileNumber` | `OCRD.Phone1`, contact mobile | `9876543210` | Main phone number |
| `email` | Primary email | Recommended | Both | `BP.jsContactPersons.emailAddress` | `OCRD.EmailAddress`, contact email | `amit@example.com` | Main email |
| `contactEmail` | Alternate/contact email | No | Both | `BP.jsContactPersons.alternateEmail` | Portal display/audit | `accounts@example.com` | Alternate email |
| `altContact` | Alternate phone | No | Both | `BP.jsContactPersons.alternateContact` | Contact phone when mapped | `0161123456` | Secondary phone |

### Address Fields

| Field | Purpose | Required | Customer/Vendor | Stored In | Sent To SAP | Example | Business Meaning |
|---|---|---:|---|---|---|---|---|
| `sameAsBill` | Copy billing to shipping | No | Both | Backend logic | Controls address rows | `true` | Avoids duplicate entry |
| `allBillAddresses[]` | Billing addresses | Yes | Both | `BP.jsMasterAddress` with `addressType='B'` | `CRD1` bill-to | array | Invoice address |
| `allShipAddresses[]` | Shipping addresses | Recommended | Both | `BP.jsMasterAddress` with `addressType='S'` | `CRD1` ship-to | array | Delivery address |
| `addrName` | Address identifier | Recommended | Both | `BP.jsMasterAddress.addressName` | `CRD1.AddressName` | `BILL-PB-001` | SAP address key |
| `street` | Street/address line | Yes | Both | `BP.jsMasterAddress.addressLine1` | `CRD1.Street` | `Plot 14 Industrial Area` | Physical address |
| `block` | Area/block | No | Both | `BP.jsMasterAddress.addressLine2` | `CRD1.Block` | `Phase 2` | Area/locality |
| `city` | City | Yes | Both | `BP.jsMasterAddress.cityID` | `CRD1.City` | `Ludhiana` | City |
| `zip` | Pin code | Yes | Both | `BP.jsMasterAddress.pincode` | `CRD1.ZipCode` | `141001` | Postal code |
| `state` | State | Yes | Both | `BP.jsMasterAddress.stateID` | `CRD1.State` | `PB` | GST/state mapping |
| `country` | Country | Yes | Both | `BP.jsMasterAddress.countryID` | `CRD1.Country` | `IN` | Country code |
| `gstin` | Address GSTIN | Conditional | Both | `BP.jsMasterAddress.gstNo` | SAP GST address field | `03AAKCR1234F1Z5` | GST registration for address |

Address storage rule: backend normalizes all BP address fields to uppercase before insert/update, and `docs/implementation/bp-uppercase-address-normalization.sql` backfills existing rows and keeps `BP.jsMasterAddress` uppercase at database level. This prevents SAP validation `(200023) Address should be in upper case`.

### Tax And Compliance Fields

| Field | Purpose | Required | Customer/Vendor | Stored In | Sent To SAP | Example | Business Meaning |
|---|---|---:|---|---|---|---|---|
| `pan` | PAN number | Yes | Both | `BP.jsTaxDetails.panNo` | SAP fiscal tax collection | `AAKCR1234F` | Indian tax identity |
| `gstin` | Header/default GSTIN | Conditional | Both | `BP.jsTaxDetails.gstin` | Used for address GSTIN fallback | `03AAKCR1234F1Z5` | GST identity |
| `tan` | TAN number | No | Vendor mainly | `BP.jsTaxDetails.buyerTANNo` | SQL only unless SAP UDF is added | `PTLA12345B` | Tax deduction account number |
| `hasMsme` | MSME flag | No | Both | Backend logic | Controls MSME fields | `true` | Whether MSME details apply |
| `msmeNo` | MSME/Udyam number | Conditional | Both | `BP.jsTaxDetails.msmeNo` | SAP UDF when configured | `UDYAM-PB-00-0001234` | MSME registration |
| `msmeType` | MSME size type | Conditional | Both | `BP.jsTaxDetails.msmeType` | SQL/display | `MICRO` | MSME classification |
| `msmeBType` | MSME business type | Conditional | Both | `BP.jsTaxDetails.msmeBType` | SQL/display | `Manufacturing` | MSME business category |
| `fssaiNo` | FSSAI license | No | Vendor mainly | `BP.jsTaxDetails.fssaiNo` | SAP UDF when configured | `10012022000011` | Food license |

### SAP Master-Data Fields

These fields are not normal BP request fields. The SAP/Finance approver fills them from the SAP section after the BP request exists, and the frontend saves them through `/api/BPmaster/UpdateSapData`. Final approval reads them from `BP.jsSAPData`.

| Field | Purpose | Required | Customer/Vendor | Stored In | Sent To SAP | Example | Business Meaning |
|---|---|---:|---|---|---|---|---|
| `cardCodePrefix` | Prefix for SAP card code generation | Approval-time | Both | `BP.jsSAPData.cardCodePrefix` | Used to generate `OCRD.CardCode` | `CUSTA`, `VENDA` | Controls generated BP code series |
| `bpGroupCode` | SAP BP group code | Approval-time | Both | `BP.jsSAPData.bpGroupCode` | `OCRD.GroupCode` | `132` | Assigns customer/vendor to SAP BP group |
| `bpGroupName` | SAP BP group display name | Approval-time with code | Both | `BP.jsSAPData.bpGroupName` | Display/audit only | `ANDHRA PRADESH` | Human-readable group label |
| `arAccountCode` | Customer control account | Customer approval-time | Customer | `BP.jsSAPData.arAccountCode` | `OCRD.DebPayAcct` / payload `DebitorAccount` | `1101001` | Receivable account for customers |
| `apAccountCode` | Vendor control account | Vendor approval-time | Vendor | `BP.jsSAPData.apAccountCode` | `OCRD.DebPayAcct` / payload `DebitorAccount` | `2110000` | Liability account for vendors |
| `paymentTermCode` | Payment term code | Approval-time | Both | `BP.jsSAPData.paymentTermCode` | `OCRD.GroupNum` / payload `PayTermsGrpCode` | `29` | Payment/credit term |
| `salesEmployeeCode` | Sales employee | Customer approval-time | Customer | `BP.jsSAPData.salesEmployeeCode` | `OCRD.SlpCode` / payload `SalesPersonCode` | `78` | Customer sales owner |
| `territoryId` | Sales territory | Customer approval-time | Customer | `BP.jsSAPData.territoryId` | `OCRD.Territory` when greater than zero | `-2` | Customer territory; `-2` means no territory |
| `sapBankCode` | SAP bank master code | Vendor approval-time | Vendor | `BP.jsSAPData.sapBankCode` | `OCRB.BankCode` override for vendor bank rows | `HDFC` | Must exist in SAP `ODSC`; used when SAP/Finance chooses the bank code after creation |

### Vendor Bank Fields

| Field | Purpose | Required | Customer/Vendor | Stored In | Sent To SAP | Example | Business Meaning |
|---|---|---:|---|---|---|---|---|
| `bankAccounts[].bankCode` | User-entered bank code | Vendor yes at create unless SAP section later supplies `sapBankCode` | Vendor | `BP.jsBankDetails.BankCode` | `OCRB.BankCode` unless overridden by `BP.jsSAPData.sapBankCode` | `ABC` | Must exist in SAP `ODSC` |
| `bankAccounts[].bankName` | Bank display name | Vendor recommended | Vendor | `BP.jsBankDetails.name` or display source | Display/validation | `ABC BANK` | Human-readable bank name |
| `bankAccounts[].vendorName` | Bank account holder name | Vendor recommended | Vendor | `BP.jsBankDetails.VendorName` | `OCRB.AcctName` via `AccountName` | `Bharat Packaging Industries Pvt Ltd` | Name on bank account |
| `bankAccounts[].branch` | Branch | No | Vendor | `BP.jsBankDetails.branch` | `OCRB.Branch` | `Ludhiana` | Bank branch |
| `bankAccounts[].accNo` | Account number | Vendor yes | Vendor | `BP.jsBankDetails.accountNo` | `OCRB.AccountNo` | `50100123456789` | Payment account number |
| `bankAccounts[].ifsc` | IFSC | Vendor yes | Vendor | `BP.jsBankDetails.ifscCode` | `OCRB.BICSwiftCode` / `UserNo1` | `HDFC0001234` | Indian bank routing code |
| `bankAccounts[].swiftCode` | SWIFT/IBAN support | No | Vendor | `BP.jsBankDetails.swiftCode` | SAP bank field when mapped | `HDFCINBB` | International bank code |
| `bankAccounts[].accountType` | Account type | No | Vendor | `BP.jsBankDetails.accountType` | SQL/display only | `Current` | Current/savings/etc. |
| `bankAccounts[].isPrimary` | Primary bank row | No | Vendor | Backend selection logic | Determines row used when only one supported | `true` | Main payment bank |

Bank-code flow:

1. Frontend loads `GET /api/BPmaster/GetBankCodes?company=1&countryCode=IN`.
2. User selects `bankCode` from SAP bank master.
3. Backend stores it in `BP.jsBankDetails.BankCode` during create/update.
4. Detail and approval APIs return the saved bank code in `bankDetails`.
5. BP remains editable while workflow status is `P` or `R`.
6. Final approval validates the saved bank code against SAP `ODSC`.
7. SAP payload sends it as `OCRB.BankCode` through `BPBankAccounts`.

### Attachment Fields

| Field | Purpose | Required | Customer/Vendor | Stored In | Sent To SAP | Example | Business Meaning |
|---|---|---:|---|---|---|---|---|
| `files` | Uploaded documents | Optional | Both | File system and `BP.jsAttachments` | `Attachments2` | PDF/image | Supporting documents |
| `fileTypes` | Business document labels | Required per file | Both | `BP.jsAttachments.fileType` | Attachment metadata/context | `PAN,GST` | Tells reviewer document type |

---

## 5. Database Design

### Relationship Map

```text
BP.jsMaster.code
  -> BP.jsTaxDetails.code
  -> BP.jsMasterAddress.code
  -> BP.jsContactPersons.code
  -> BP.jsBankDetails.code
  -> BP.jsAttachments.code
  -> BP.jsFlow.bpCode
       -> BP.jsFlowStatus.flowId
  -> BP.jsSAPData.masterId
  -> BP snapshot/audit tables
```

### `BP.jsMaster`

| Item | Detail |
|---|---|
| Purpose | Stores the BP header record |
| Inserted when | `InsertBPmasterData` calls `BP.jsInsertBPMasterData` |
| Updated when | `UpdateBPMaster` updates editable BP data |
| Read by | List APIs, detail API, approval logic, SAP posting |
| Relationship | Parent table for all BP child tables |

Important columns:

- `code`
- `type`
- `isStaff`
- `name`
- `foreignName`
- `typeOfBusiness`
- `industry`
- `mobileNo`
- `currency`
- `remarks`
- `company`
- `userId`
- `mainGroupID`
- `chain`
- `creditLimit`
- `createDate`
- `updationDate`
- `action`

### `BP.jsTaxDetails`

| Item | Detail |
|---|---|
| Purpose | Stores PAN, GSTIN, TAN, MSME, FSSAI |
| Inserted when | BP is created |
| Updated when | BP tax data is changed |
| Read by | Detail API, SAP posting, support checks |
| Relationship | One row per `BP.jsMaster.code` |

### `BP.jsMasterAddress`

| Item | Detail |
|---|---|
| Purpose | Stores billing and shipping addresses |
| Inserted when | BP is created or address section is updated |
| Updated when | Usually replaced as a section during update |
| Read by | Detail API and SAP `CRD1` payload |
| Relationship | Many rows per BP; `addressType='B'` or `addressType='S'` |

### `BP.jsContactPersons`

| Item | Detail |
|---|---|
| Purpose | Stores contact people |
| Inserted when | BP is created or contacts are updated |
| Updated when | Contact section is replaced |
| Read by | Detail API and SAP `OCPR` payload |
| Relationship | Many rows per BP |

### `BP.jsBankDetails`

| Item | Detail |
|---|---|
| Purpose | Stores vendor bank account data |
| Inserted when | Vendor BP includes bank details |
| Updated when | Bank section is replaced |
| Read by | Detail API, bank validation, SAP `OCRB` payload |
| Relationship | Vendor only; many rows possible |

### `BP.jsAttachments`

| Item | Detail |
|---|---|
| Purpose | Stores uploaded file metadata |
| Inserted when | Files are uploaded with create/update |
| Updated when | Attachment section is replaced or appended depending API flags |
| Read by | Detail API, download, SAP `Attachments2` upload |
| Relationship | Many rows per BP |

### `BP.jsFlow`

| Item | Detail |
|---|---|
| Purpose | Stores current workflow state |
| Inserted when | BP request is created |
| Updated when | Approver approves/rejects or SAP status changes |
| Read by | Pending/approved/rejected APIs, retry, final approval |
| Relationship | One flow per BP request |

Important columns:

- `id`: flow id used by approve/reject/retry
- `bpCode`: BP master code
- `status`: `P`, `A`, or `R`
- `currentStageId`: current stage id
- `templateId`: approval template
- `totalStage`: total number of approval stages
- `currentStage`: current priority number

### `BP.jsFlowStatus`

| Item | Detail |
|---|---|
| Purpose | Stores approval/rejection/pending history |
| Inserted when | Initial pending row is created and each user acts |
| Updated when | Usually append-only |
| Read by | Approval history, approved list, rejected list, duplicate action prevention |
| Relationship | Many rows per `BP.jsFlow.id` |

### `BP.jsSAPData`

| Item | Detail |
|---|---|
| Purpose | Stores SAP approval metadata, posting status, and result |
| Inserted when | Immediately after BP create succeeds, before any approval or SAP retry |
| Updated when | SAP/Finance saves approval fields, SAP post succeeds/fails/retries |
| Read by | SAP section, final approval, retry logic, pending list, support diagnostics |
| Relationship | One active SAPData row per BP master record |

Important columns:

- `masterId`
- `apiStatusTag`
- `apiMessage`
- `sapCardCode`
- `sapAttachmentEntry`
- `payloadHash`
- `retryCount`
- `cardCodePrefix`
- `bpGroupCode`
- `bpGroupName`
- `arAccountCode`
- `apAccountCode`
- `paymentTermCode`
- `salesEmployeeCode`
- `territoryId`
- `sapBankCode`
- `updatedBy`
- `createdOn`
- `updatedOn`

`BP.jsSAPData` is the single source of truth for SAP approval fields. Create BP and Update BP must not write these fields into `BP.jsMaster`. Create BP creates a default SAPData row with `apiStatusTag = 'N'`, configured `cardCodePrefix`, configured AR/AP account from `appsettings.json`, `createdOn`, `updatedOn`, and `updatedBy`.

Legacy SAPData fields `debPayAcct`, `wtLabel`, and `series` are intentionally removed from the BP SAPData contract. SAP `DebitorAccount` / `OCRD.DebPayAcct` is calculated only during SAP payload creation: customers use `arAccountCode`, and vendors use `apAccountCode`.

### Snapshot Tables

| Table | Purpose |
|---|---|
| `BP.jsMasterSnapshot` | Before-update/restore copy of BP header business fields |
| `BP.jsMasterAddressSnapshot` | Before-update address copy |
| `BP.jsContactPersonsSnapshot` | Before-update contact copy |
| `BP.jsBankDetailsSnapshot` | Before-update bank copy |
| `BP.jsTaxDetailsSnapshot` | Before-update tax copy |

Snapshots are used when support needs to see what changed or restore a previous version.

### Audit Tables

| Table | Purpose |
|---|---|
| `BP.jsAuditLog` | Field/table operation history |

Audit logs should show who changed data, when it changed, and what changed. This is important because BP data can be edited while workflow is still pending or rejected.

---

## 6. Workflow Engine

### Workflow Fields

| Field | Meaning |
|---|---|
| `currentStage` | Numeric priority of current approval stage |
| `currentStageId` | Actual stage id from `dbo.jsStage` |
| `totalStage` | Total number of stages in the template |
| `status` | Current workflow status |

### Workflow Status Values

| Status | Meaning | Update Allowed | Retry Allowed |
|---|---|---:|---:|
| `P` | Pending | Yes | Yes, only at final stage and SAP failed |
| `A` | Approved | No | No |
| `R` | Rejected | Yes | No, unless reworked into pending by business process |

### How Next Approver Is Selected

The pending list checks the current stage and finds users assigned to that stage.

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

If the user is not mapped to the stage, they will not see the BP in pending even if the flow is pending.

### Pending List Logic

A BP appears in `GetPendingBP` when:

- `BP.jsFlow.status = 'P'`
- the BP belongs to the requested company
- the current stage is assigned to the requested user
- the user has not already approved/rejected the current stage
- the month filter includes the flow date

### Approved List Logic

`GetApprovedBP` reads `BP.jsFlowStatus` rows where the user acted with status `A`.

This is user-specific history, not necessarily all approved BPs in the company.

### Rejected List Logic

`GetRejectedBP` reads `BP.jsFlowStatus` rows where the user acted with status `R`.

### Example

```text
Flow 1153
BP Code 1190
Status P
Current Stage 3
SAP Status N

Meaning:
The BP is still pending at SAP approval.
SAP failed previously.
Creator can update data.
Final approver can retry SAP after correction.
```

---

## 7. SAP Integration

### When SAP Call Happens

SAP posting happens only from:

- final-stage `POST /api/BPmaster/ApproveBP`
- `POST /api/BPmaster/RetrySapPost` when final SAP posting failed

Stage 1 and Stage 2 approvals never post to SAP.

### SAP Objects Created

| SAP Object | Source |
|---|---|
| `BusinessPartners` / `OCRD` | BP header, tax, and `BP.jsSAPData` approval fields |
| `BPAddresses` / `CRD1` | Billing and shipping addresses |
| `ContactEmployees` / `OCPR` | Contact persons |
| `BPBankAccounts` / `OCRB` | Vendor bank details |
| `Attachments2` | Uploaded files |

### Card Code Generation

`cardCodePrefix` controls the SAP BP code prefix and is saved through `/api/BPmaster/UpdateSapData`.

| BP Type | Default Prefix | Example SAP CardCode |
|---|---|---|
| Customer | `CUSTA` | `CUSTA001106` |
| Vendor | `VENDA` | `VENDA001583` |

Create BP does not accept this as a required field. The SAP/Finance user saves it in the SAP section before final approval. If it is blank, the backend falls back to the existing Node-compatible prefix behavior.

### Control Accounts

Control accounts decide where SAP posts receivables or liabilities.

| Account Type | Used For | SAP Meaning | Example |
|---|---|---|---|
| AR Account | Customer | Receivable account | `1101001` |
| AP Account | Vendor | Liability account | `2110000` |

In SAP payload both are sent as `DebitorAccount` because SAP Service Layer uses the same BP property for the control account. The meaning changes by `CardType`.

### SAP Mapping

| SQL/API Field | SAP Payload Field | SAP Table/Field |
|---|---|---|
| `cardName` / `BP.jsMaster.name` | `CardName` | `OCRD.CardName` |
| `type` | `CardType` | `OCRD.CardType` |
| `BP.jsSAPData.cardCodePrefix` | builds `CardCode` | `OCRD.CardCode` |
| `BP.jsSAPData.bpGroupCode` | `GroupCode` | `OCRD.GroupCode` |
| `BP.jsSAPData.arAccountCode` | `DebitorAccount` | `OCRD.DebPayAcct` |
| `BP.jsSAPData.apAccountCode` | `DebitorAccount` | `OCRD.DebPayAcct` |
| `BP.jsSAPData.paymentTermCode` | `PayTermsGrpCode` | `OCRD.GroupNum` |
| `BP.jsSAPData.salesEmployeeCode` | `SalesPersonCode` | `OCRD.SlpCode` |
| `BP.jsSAPData.territoryId` | `Territory` | `OCRD.Territory` |
| `currency` | `Currency` | `OCRD.Currency` |
| `remarks` | `Notes` | `OCRD.Notes` |
| billing/shipping addresses | `BPAddresses` | `CRD1` |
| contacts | `ContactEmployees` | `OCPR` |
| vendor bank rows | `BPBankAccounts` | `OCRB` |
| attachment entry | `AttachmentEntry` | `OCRD.AttachmentEntry` |

### Attachments

Attachment flow:

```text
Portal file upload
  -> saved under wwwroot/Uploads/BPmaster
  -> metadata saved in BP.jsAttachments
  -> final SAP approval uploads files to Attachments2
  -> SAP returns AbsoluteEntry
  -> BP payload sends AttachmentEntry
```

### SAP Error Handling Policy

The API must show SAP validation/business errors clearly.

Failure response style:

```json
{
  "success": false,
  "approvalStatus": "Blocked",
  "sapStatus": "Failed: Bank Code 'HDFC BANK' was rejected by SAP Bank Master. (SAP Error Code: -5002)",
  "message": "Bank Code 'HDFC BANK' was rejected by SAP Bank Master. (SAP Error Code: -5002)"
}
```

Rules:

- Do not replace SAP errors with generic text.
- Do not expose stack traces or connection strings.
- Do not dump the full SAP payload to frontend.
- Log the full payload and raw SAP response on backend.
- Frontend should display top-level `message`.

---

## 8. Dropdown Master Data

Dropdown values must come from SAP dynamically. Do not hardcode values in frontend or backend.

### API Summary

| API | Source | Used For |
|---|---|---|
| `GET /api/BPmaster/GetBPGroups?company=1&bpType=C` | SAP Service Layer `BusinessPartnerGroups` | Customer BP group |
| `GET /api/BPmaster/GetBPGroups?company=1&bpType=V` | SAP Service Layer `BusinessPartnerGroups` | Vendor BP group |
| `GET /api/BPmaster/GetARAccounts?company=1` | SAP HANA `OACT` | Customer control account |
| `GET /api/BPmaster/GetAPAccounts?company=1` | SAP HANA `OACT` | Vendor control account |
| `GET /api/BPmaster/GetPaymentTerms?company=1` | SAP HANA `OCTG` | Payment terms |
| `GET /api/BPmaster/GetSalesEmployees?company=1` | SAP HANA `OSLP` | Customer sales employee |
| `GET /api/BPmaster/GetTerritories?company=1` | SAP HANA `OTER` | Customer territory |
| `GET /api/BPmaster/GetBankCodes?company=1&countryCode=IN` | SAP Service Layer `Banks`; HANA `ODSC` fallback | Vendor bank code |

### Node.js Parity Audit

The working Node.js BP portal was audited before the .NET dropdown implementation was aligned. The active BP routes are in `backend_sap/routes/customers.js` and `backend_sap/routes/vendors.js`. Older inline routes also exist in `server.js` and generic SAP lookup routes exist in `routes/sap.js`, but the active vendor AP account fix is in `routes/vendors.js`.

| Dropdown | Active Node Route | Node File | SAP Source | Node Query / Service Layer Call | Node Returned Fields | .NET BP API |
|---|---|---|---|---|---|---|
| Customer BP Groups | `GET /api/customers/lookup/bp-groups` | `routes/customers.js` / inline customer router | Service Layer `BusinessPartnerGroups` | `BusinessPartnerGroups?$filter=Type eq 'bbpgt_CustomerGroup'&$select=Code,Name&$orderby=Name` | `GroupCode`, `GroupName` | `GET /api/BPmaster/GetBPGroups?company=1&bpType=C` |
| Vendor BP Groups | `GET /api/vendors/lookup/bp-groups` | `routes/vendors.js` | Service Layer `BusinessPartnerGroups` | `BusinessPartnerGroups?$filter=Type eq 'bbpgt_VendorGroup'&$select=Code,Name&$orderby=Name` | `GroupCode`, `GroupName` | `GET /api/BPmaster/GetBPGroups?company=1&bpType=V` |
| AR Accounts | `GET /api/customers/lookup/ar-accounts` | `server.js` customer router | HANA `OACT` | `SELECT "AcctCode","AcctName" FROM DB."OACT" WHERE "FatherNum"='1101000' ORDER BY "AcctCode"` | `AcctCode`, `AcctName` | `GET /api/BPmaster/GetARAccounts?company=1` |
| AP Accounts | `GET /api/vendors/lookup/ap-accounts` | `routes/vendors.js` | HANA `OACT` | `SELECT "AcctCode","AcctName" FROM DB."OACT" WHERE ("FatherNum"='2101000' OR "AcctCode" LIKE '211%') AND "Finanse"='N' ORDER BY "AcctCode"` | `AcctCode`, `AcctName` | `GET /api/BPmaster/GetAPAccounts?company=1` |
| Payment Terms | `GET /api/customers/lookup/payment-terms`, `GET /api/vendors/lookup/payment-terms` | `server.js`, `routes/vendors.js` | HANA `OCTG` | `SELECT "GroupNum","PymntGroup" FROM DB."OCTG" ORDER BY "PymntGroup"` | Node maps to `Code`, `Name`; .NET returns cleaned `groupNum`, `pymntGroup` | `GET /api/BPmaster/GetPaymentTerms?company=1` |
| Sales Employees | `GET /api/customers/lookup/sales-employees`, `GET /api/vendors/lookup/sales-employees` | `server.js`, `routes/vendors.js` | HANA `OSLP` | `SELECT "SlpCode","SlpName" FROM DB."OSLP" WHERE "SlpCode" > 0 AND "Locked"='N' ORDER BY "SlpName"` | `SlpCode`, `SlpName`; .NET exposes `salesEmployeeName` for frontend clarity | `GET /api/BPmaster/GetSalesEmployees?company=1` |
| Territories | Node stores `mgrTerritory` in manager fields; no active BP dropdown route found | `routes/customers.js`, `routes/vendors.js` manager fields | HANA `OTER` in .NET | `SELECT "territryID","descript" FROM DB."OTER" ORDER BY "descript"` | .NET-only cleaned dropdown: `territoryId`, `territoryName` | `GET /api/BPmaster/GetTerritories?company=1` |
| Bank Codes | `GET /api/vendors/lookup/banks?country=IN` | `routes/vendors.js`, `services/sapServiceLayer.js` | Service Layer `Banks`; HANA `ODSC` fallback | `Banks?$filter=CountryCode eq 'IN'&$select=BankCode,BankName,SwiftNo,CountryCode&$orderby=BankName`; fallback `SELECT "BankCode","BankName","SwiftNo","CountryCode" FROM DB."ODSC" WHERE "CountryCode"=? ORDER BY "BankName"` | `BankCode`, `BankName`, `SwiftNo`, `CountryCode`; .NET returns cleaned camelCase JSON | `GET /api/BPmaster/GetBankCodes?company=1&countryCode=IN` |

AP account root cause from the parity audit: older Node/server examples and the earlier .NET implementation only used `FatherNum='2101000'`. The active vendor route adds `AcctCode LIKE '211%'` and `Finanse='N'`, which is required because vendor control/liability accounts can sit under the `211%` hierarchy instead of being direct children of `2101000`.

### Response Shapes

BP Groups:

```json
{
  "success": true,
  "data": [
    {
      "groupCode": 132,
      "groupName": "ANDHRA PRADESH"
    }
  ]
}
```

AR/AP Accounts:

```json
{
  "success": true,
  "data": [
    {
      "acctCode": "1101001",
      "acctName": "SUNDRY DEBTORS GT"
    }
  ]
}
```

Payment Terms:

```json
{
  "success": true,
  "data": [
    {
      "groupNum": 29,
      "pymntGroup": "20% ADVANCE"
    }
  ]
}
```

Sales Employees:

```json
{
  "success": true,
  "data": [
    {
      "slpCode": 78,
      "salesEmployeeName": "AASHNA THAKUR"
    }
  ]
}
```

Territories:

```json
{
  "success": true,
  "data": [
    {
      "territoryId": -2,
      "territoryName": "-No Territory-"
    }
  ]
}
```

Bank Codes:

```json
{
  "success": true,
  "data": [
    {
      "bankCode": "ABC",
      "bankName": "ABC BANK",
      "swiftNo": "",
      "countryCode": "IN"
    }
  ]
}
```

Do not return duplicate `code` and `name` aliases when the SAP fields already exist.

Dropdown failure shape:

```json
{
  "success": false,
  "message": "SAP query failed",
  "errorCode": "SAP_QUERY_FAILED",
  "sapError": {
    "code": -2028,
    "message": "No matching records found"
  }
}
```

The top-level message stays stable for frontend handling; `sapError.message` carries the exact SAP/HANA detail for debugging.

### Direct SAP Verification Queries

Replace `JIVO_OIL_HANADB` with the selected company schema.

BP groups are loaded from Service Layer:

```http
GET BusinessPartnerGroups?$filter=Type eq 'bbpgt_CustomerGroup'&$select=Code,Name&$orderby=Name
GET BusinessPartnerGroups?$filter=Type eq 'bbpgt_VendorGroup'&$select=Code,Name&$orderby=Name
```

AR accounts:

```sql
SELECT "AcctCode", "AcctName"
FROM "JIVO_OIL_HANADB"."OACT"
WHERE "FatherNum" = '1101000'
ORDER BY "AcctCode";
```

AP accounts:

```sql
SELECT "AcctCode", "AcctName"
FROM "JIVO_OIL_HANADB"."OACT"
WHERE ("FatherNum" = '2101000' OR "AcctCode" LIKE '211%')
  AND "Finanse" = 'N'
ORDER BY "AcctCode";
```

Payment terms:

```sql
SELECT "GroupNum", "PymntGroup"
FROM "JIVO_OIL_HANADB"."OCTG"
ORDER BY "PymntGroup";
```

Sales employees:

```sql
SELECT "SlpCode", "SlpName"
FROM "JIVO_OIL_HANADB"."OSLP"
WHERE "SlpCode" > 0
  AND IFNULL("Locked", 'N') = 'N'
ORDER BY "SlpName";
```

Territories:

```sql
SELECT "territryID" AS "TerritoryId", "descript" AS "TerritoryName"
FROM "JIVO_OIL_HANADB"."OTER"
ORDER BY "descript";
```

Bank codes:

```sql
SELECT "BankCode", "BankName", "SwiftNo", "CountryCode"
FROM "JIVO_OIL_HANADB"."ODSC"
WHERE "CountryCode" = 'IN'
ORDER BY "BankName";
```

---

## 9. API Documentation

Base route:

```text
/api/BPmaster
```

### Create BP

| Item | Value |
|---|---|
| Method | `POST` |
| URL | `/api/BPmaster/InsertBPmasterData` |
| Content Type | `application/json`, `multipart/form-data`, or `application/x-www-form-urlencoded` |
| Purpose | Create BP request and start workflow |

Supported request formats:

1. Raw JSON body.
2. `multipart/form-data` with one `requests` text field containing the full JSON body.
3. `multipart/form-data` or `application/x-www-form-urlencoded` direct fields such as `type=V`, `companyId=1`, `cardName=TEST VENDOR`.
4. `multipart/form-data` with direct fields and files. For arrays such as `bankAccounts`, `allBillAddresses`, and `allShipAddresses`, send the field value as a JSON array string when using direct form fields.

Complex FormData fields:

These collection fields support either a JSON array string or indexed form-data fields:

- `bankAccounts`
- `allBillAddresses`
- `allShipAddresses`
- `contacts`
- `contactPersons`
- `attachments`

Correct JSON-array string format:

```text
bankAccounts=[{"bankCode":"HDFC","bankName":"HDFC BANK","accNo":"50100123456789","ifsc":"HDFC0001234"}]
allBillAddresses=[{"addrName":"BILL-001","street":"PLOT 14 INDUSTRIAL AREA","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN"}]
allShipAddresses=[{"addrName":"SHIP-001","street":"PLOT 14 INDUSTRIAL AREA","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN"}]
```

Correct indexed field format:

```text
bankAccounts[0].bankCode=HDFC
bankAccounts[0].bankName=HDFC BANK
bankAccounts[0].vendorName=TEST VENDOR BP 0612 01
bankAccounts[0].branch=LUDHIANA
bankAccounts[0].accNo=50100123456789
bankAccounts[0].ifsc=HDFC0001234
allBillAddresses[0].addrName=BILL-001
allBillAddresses[0].street=PLOT 14 INDUSTRIAL AREA
allBillAddresses[0].city=LUDHIANA
allBillAddresses[0].zip=141001
allBillAddresses[0].state=PB
allBillAddresses[0].country=IN
```

Incorrect browser-generated format:

```text
bankAccounts=[object Object]
```

This happens when frontend code appends a JavaScript object directly, for example `formData.append("bankAccounts", bankAccounts[0])`. Send `JSON.stringify(bankAccounts)` or use indexed keys instead. The API returns:

```json
{
  "success": false,
  "message": "Invalid bankAccounts format. Send JSON array or indexed form-data fields."
}
```

### Critical create payload requirement for backend

Before sending create requests, ensure these fields are always present:

- `type`
- `company` (SAP company DB id like `JIVO_OIL_HANADB`)
- `companyId` (numeric SQL company id, e.g. `1`)
- `cardName`
- `industry`
- `pan`
- `contactFirst`
- `contactLast`
- `mobile`
- `email`
- `currency`
- `isStaff`
- `allBillAddresses`

`allBillAddresses` is required in both JSON and form-data. If it is missing or empty, insert may fail during server-side parsing/address normalization.

Use one of these exact payloads.

Create BP (Customer) — JSON:

```json
{
  "company": "JIVO_OIL_HANADB",
  "companyId": 1,
  "type": "C",
  "customerType": "B2B",
  "cardName": "TEST CUSTOMER BP 0611 01",
  "foreignName": "TEST CUSTOMER FOREIGN",
  "typeOfBusiness": "Company",
  "industry": "FMCG",
  "contactFirst": "RAMESH",
  "contactLast": "KUMAR",
  "contactTitle": "OWNER",
  "mobile": "9876543210",
  "email": "customer061101@example.com",
  "contactEmail": "accounts061101@example.com",
  "gstin": "03AAKCU6101F1Z5",
  "pan": "AAKCU6101F",
  "currency": "INR",
  "remarks": "Customer registration test",
  "isStaff": false,
  "userId": 76,
  "companyByUser": "JIVO_OIL_HANADB",
  "sameAsBill": true,
  "allBillAddresses": [
    {
      "addrName": "BILL-CUST-061101",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCU6101F1Z5"
    }
  ],
  "allShipAddresses": [
    {
      "addrName": "SHIP-CUST-061101",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCU6101F1Z5"
    }
  ],
  "hasMsme": false,
  "msmeNo": "",
  "msmeType": "",
  "msmeBType": ""
}
```

Create BP (Customer) — multipart/form-data (direct fields):

```text
company=JIVO_OIL_HANADB
companyId=1
type=C
customerType=B2B
cardName=TEST CUSTOMER BP 0611 01
foreignName=TEST CUSTOMER FOREIGN
typeOfBusiness=Company
industry=FMCG
contactFirst=RAMESH
contactLast=KUMAR
contactTitle=OWNER
mobile=9876543210
email=customer061101@example.com
contactEmail=accounts061101@example.com
gstin=03AAKCU6101F1Z5
pan=AAKCU6101F
currency=INR
remarks=Customer registration test
isStaff=false
userId=76
companyByUser=JIVO_OIL_HANADB
sameAsBill=true
allBillAddresses=[{"addrName":"BILL-CUST-061101","street":"PLOT 14 INDUSTRIAL AREA","block":"PHASE 2","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN","gstin":"03AAKCU6101F1Z5"}]
allShipAddresses=[{"addrName":"SHIP-CUST-061101","street":"PLOT 14 INDUSTRIAL AREA","block":"PHASE 2","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN","gstin":"03AAKCU6101F1Z5"}]
hasMsme=false
msmeNo=
msmeType=
msmeBType=
```

Create BP (Vendor) — JSON:

```json
{
  "type": "V",
  "company": "JIVO_OIL_HANADB",
  "companyId": 1,
  "vendorType": "SUPPLIER",
  "cardName": "TEST VENDOR BP 0611 02",
  "foreignName": "TEST VENDOR FOREIGN",
  "typeOfBusiness": "Partnership",
  "industry": "IT Services",
  "contactFirst": "ROHIT",
  "contactLast": "RATHOD",
  "contactTitle": "OWNER",
  "mobile": "8571954685",
  "altContact": "0161123456",
  "email": "vendor061102@example.com",
  "gstin": "03AAKCT6102F1Z5",
  "pan": "AAKCT6102F",
  "tan": "PTLA12345B",
  "currency": "INR",
  "remarks": "Vendor registration test",
  "isStaff": false,
  "userId": 76,
  "companyByUser": "JIVO_OIL_HANADB",
  "sameAsBill": true,
  "allBillAddresses": [
    {
      "addrName": "BILL-VEND-061102",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCT6102F1Z5"
    }
  ],
  "allShipAddresses": [
    {
      "addrName": "SHIP-VEND-061102",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCT6102F1Z5"
    }
  ],
  "hasMsme": true,
  "msmeNo": "UDYAM-PB-00-0611022",
  "msmeType": "MICRO",
  "msmeBType": "Manufacturing",
  "fssaiNo": "10012022006112",
  "bankAccounts": [
    {
      "bankCode": "HDFC",
      "bankName": "HDFC BANK",
      "vendorName": "TEST VENDOR BP 0611 02",
      "branch": "LUDHIANA",
      "accNo": "50100123456789",
      "ifsc": "HDFC0001234",
      "swiftCode": "HDFCINBB",
      "accountType": "Current",
      "isPrimary": true
    }
  ]
}
```

Create BP (Vendor) — multipart/form-data (direct fields):

```text
type=V
company=JIVO_OIL_HANADB
companyId=1
vendorType=SUPPLIER
cardName=TEST VENDOR BP 0611 02
foreignName=TEST VENDOR FOREIGN
typeOfBusiness=Partnership
industry=IT SERVICES
contactFirst=ROHIT
contactLast=RATHOD
contactTitle=OWNER
mobile=8571954685
altContact=0161123456
email=vendor061102@example.com
gstin=03AAKCT6102F1Z5
pan=AAKCT6102F
tan=PTLA12345B
currency=INR
remarks=Vendor registration test
isStaff=false
userId=76
companyByUser=JIVO_OIL_HANADB
sameAsBill=true
allBillAddresses=[{\"addrName\":\"BILL-VEND-061102\",\"street\":\"PLOT 14 INDUSTRIAL AREA\",\"block\":\"PHASE 2\",\"city\":\"LUDHIANA\",\"zip\":\"141001\",\"state\":\"PB\",\"country\":\"IN\",\"gstin\":\"03AAKCT6102F1Z5\"}]
allShipAddresses=[{\"addrName\":\"SHIP-VEND-061102\",\"street\":\"PLOT 14 INDUSTRIAL AREA\",\"block\":\"PHASE 2\",\"city\":\"LUDHIANA\",\"zip\":\"141001\",\"state\":\"PB\",\"country\":\"IN\",\"gstin\":\"03AAKCT6102F1Z5\"}]
bankAccounts=[{\"bankCode\":\"HDFC\",\"bankName\":\"HDFC BANK\",\"vendorName\":\"TEST VENDOR BP 0611 02\",\"branch\":\"LUDHIANA\",\"accNo\":\"50100123456789\",\"ifsc\":\"HDFC0001234\",\"swiftCode\":\"HDFCINBB\",\"accountType\":\"Current\",\"isPrimary\":true}]
hasMsme=true
msmeNo=UDYAM-PB-00-0611022
msmeType=MICRO
msmeBType=Manufacturing
fssaiNo=10012022006112
```

Multipart compatibility fields:

| Key | Type | Required | Meaning |
|---|---|---:|---|
| `requests` | Text JSON | No if direct fields are sent | BP request body |
| `files` | File list | No | Uploaded documents |
| `fileTypes` | Text | Required per file unless file input names such as `panFile`/`gstFile` are used | Comma-separated document labels |

Customer request example:

Postman setup:

- Method: `POST`
- URL: `/api/BPmaster/InsertBPmasterData`
- Body: `form-data`
- Option A: key `requests`, type `Text`, value is the full JSON below.
- Option B: send the same properties as direct form fields.
- Do not send SAP approval fields in this create payload.

```json
{
  "company": "JIVO_OIL_HANADB",
  "companyId": 1,
  "type": "C",
  "customerType": "B2B",
  "cardName": "TEST CUSTOMER BP 0611 01",
  "foreignName": "TEST CUSTOMER FOREIGN",
  "typeOfBusiness": "Company",
  "industry": "FMCG",
  "contactFirst": "RAMESH",
  "contactLast": "KUMAR",
  "contactTitle": "OWNER",
  "mobile": "9876543210",
  "email": "customer061101@example.com",
  "contactEmail": "accounts061101@example.com",
  "gstin": "03AAKCU6101F1Z5",
  "pan": "AAKCU6101F",
  "currency": "INR",
  "remarks": "Customer registration test",
  "isStaff": false,
  "userId": 76,
  "companyByUser": "JIVO_OIL_HANADB",
  "sameAsBill": true,
  "allBillAddresses": [
    {
      "addrName": "BILL-CUST-061101",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCU6101F1Z5"
    }
  ],
  "allShipAddresses": [
    {
      "addrName": "SHIP-CUST-061101",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCU6101F1Z5"
    }
  ],
  "hasMsme": false,
  "msmeNo": "",
  "msmeType": "",
  "msmeBType": ""
}
```

Vendor request example:

Postman setup:

- Method: `POST`
- URL: `/api/BPmaster/InsertBPmasterData`
- Body: `form-data`
- Option A: key `requests`, type `Text`, value is the full JSON below.
- Option B: send the same properties as direct form fields.
- Do not send SAP approval fields in this create payload.
- IFSC must match `^[A-Z]{4}0[A-Z0-9]{6}$`, for example `HDFC0001234`.

```json
{
  "type": "V",
  "company": "JIVO_OIL_HANADB",
  "companyId": 1,
  "vendorType": "SUPPLIER",
  "cardName": "TEST VENDOR BP 0611 02",
  "foreignName": "TEST VENDOR FOREIGN",
  "typeOfBusiness": "Partnership",
  "industry": "IT Services",
  "contactFirst": "ROHIT",
  "contactLast": "RATHOD",
  "contactTitle": "OWNER",
  "mobile": "8571954685",
  "altContact": "0161123456",
  "email": "vendor061102@example.com",
  "contactEmail": "accounts061102@example.com",
  "gstin": "03AAKCT6102F1Z5",
  "pan": "AAKCT6102F",
  "tan": "PTLA12345B",
  "currency": "INR",
  "hasMsme": true,
  "msmeNo": "UDYAM-PB-00-0611022",
  "msmeType": "MICRO",
  "msmeBType": "Manufacturing",
  "fssaiNo": "10012022006112",
  "bankAccounts": [
    {
      "bankCode": "HDFC",
      "bankName": "HDFC BANK",
      "vendorName": "TEST VENDOR BP 0611 02",
      "branch": "LUDHIANA",
      "accNo": "50100123456789",
      "ifsc": "HDFC0001234",
      "swiftCode": "HDFCINBB",
      "accountType": "Current",
      "isPrimary": true
    }
  ],
  "remarks": "Vendor registration test",
  "isStaff": false,
  "userId": 76,
  "companyByUser": "JIVO_OIL_HANADB",
  "sameAsBill": true,
  "allBillAddresses": [
    {
      "addrName": "BILL-VEND-061102",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCT6102F1Z5"
    }
  ],
  "allShipAddresses": [
    {
      "addrName": "SHIP-VEND-061102",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCT6102F1Z5"
    }
  ]
}
```

Vendor direct form-data example:

```text
type=V
company=JIVO_OIL_HANADB
companyId=1
vendorType=SUPPLIER
cardName=TEST VENDOR BP 0611 02
foreignName=TEST VENDOR FOREIGN
typeOfBusiness=Partnership
industry=IT Services
contactFirst=ROHIT
contactLast=RATHOD
mobile=8571954685
email=vendor061102@example.com
gstin=03AAKCT6102F1Z5
pan=AAKCT6102F
currency=INR
userId=76
companyByUser=JIVO_OIL_HANADB
isStaff=false
sameAsBill=true
bankAccounts=[{"bankCode":"HDFC","bankName":"HDFC BANK","vendorName":"TEST VENDOR BP 0611 02","branch":"LUDHIANA","accNo":"50100123456789","ifsc":"HDFC0001234","swiftCode":"HDFCINBB","accountType":"Current","isPrimary":true}]
allBillAddresses=[{"addrName":"BILL-VEND-061102","street":"PLOT 14 INDUSTRIAL AREA","block":"PHASE 2","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN","gstin":"03AAKCT6102F1Z5"}]
allShipAddresses=[{"addrName":"SHIP-VEND-061102","street":"PLOT 14 INDUSTRIAL AREA","block":"PHASE 2","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN","gstin":"03AAKCT6102F1Z5"}]
```

Success response:

```json
{
  "success": true,
  "message": "BP Master inserted successfully.",
  "generatedCode": 1234,
  "masterId": 1234,
  "sapDataId": 59,
  "flowId": 1148,
  "status": "Pending"
}
```

After this response, these rows must already exist:

- one row in `BP.jsMaster`
- one row in `BP.jsFlow`
- one row in `BP.jsSAPData`

The frontend should use `masterId` for `/api/BPmaster/GetSAPData` and `/api/BPmaster/UpdateSAPData`. It should not wait for an approval failure before saving SAP approval values.

Common errors:

| Error | Cause | Solution |
|---|---|---|
| `Invalid multipart request` | Postman body is not form-data or `requests` missing | Use form-data with `requests` text field |
| `PAN is required` | PAN blank | Send `pan` |
| `UserId is required` | Creator missing | Send valid `userId` |
| SQL procedure parameter error | Procedure not deployed | Run current BP procedure script |

### Update BP

| Item | Value |
|---|---|
| Method | `POST` |
| URL | `/api/BPmaster/UpdateBPMaster` |
| Content Type | `application/json`, `multipart/form-data`, or `application/x-www-form-urlencoded` |
| Purpose | Update BP while workflow is `P` or `R` |

Update is allowed when:

- `BP.jsFlow.status = 'P'`
- `BP.jsFlow.status = 'R'`

Update is blocked when:

- `BP.jsFlow.status = 'A'`

Example update body:

```json
{
  "code": 1191,
  "company": "1",
  "type": "C",
  "userId": 76,
  "cardName": "North India Distributor",
  "industry": "FMCG",
  "contactFirst": "Ramesh",
  "contactLast": "Kumar",
  "mobile": "9876543210",
  "email": "ramesh@example.com",
  "pan": "AAKCR1234F",
  "currency": "INR",
  "updateAddresses": false,
  "updateBankDetails": false,
  "updateContacts": false,
  "updateAttachments": false
}
```

Form-data direct field example:

```text
code=1234
cardName=UPDATED TEST VENDOR
mobile=8571954685
updateAddresses=false
updateBankDetails=false
updateContacts=false
updateAttachments=false
```

Approved-lock response:

```json
{
  "success": false,
  "message": "BP update is blocked because workflow is already approved."
}
```

### Get SAP Data

| Item | Value |
|---|---|
| Method | `GET` |
| URL | `/api/BPmaster/GetSAPData?masterId=1234` |
| Compatibility URL | `/api/BPmaster/GetSPAData?masterId=1234` |
| Purpose | Load the create-time SAPData row and any saved SAP approval/status values |

Response:

```json
{
  "success": true,
  "data": {
    "id": 59,
    "masterId": 1234,
    "apiStatusTag": "N",
    "apiMessage": "",
    "sapCardCode": "",
    "sapAttachmentEntry": null,
    "payloadHash": "",
    "retryCount": 0,
    "cardCodePrefix": "CUSTA",
    "bpGroupCode": null,
    "bpGroupName": "",
    "arAccountCode": "1101001",
    "apAccountCode": "",
    "paymentTermCode": null,
    "salesEmployeeCode": null,
    "territoryId": null,
    "sapBankCode": "",
    "updatedBy": 76,
    "createdOn": "2026-06-11T16:30:00",
    "updatedOn": "2026-06-11T16:30:00"
  }
}
```

Default `cardCodePrefix`, `arAccountCode`, and `apAccountCode` come from `appsettings.json` under `BPDefaults`. The backend creates the SAPData row during Create BP, so this API should work before any approval attempt.

### Update SAP Data

| Item | Value |
|---|---|
| Method | `PUT` or `POST` |
| URL | `/api/BPmaster/UpdateSAPData` or `/api/BPmaster/UpdateSapData` |
| Content Type | `application/json`, `multipart/form-data`, or `application/x-www-form-urlencoded` |
| Purpose | Save SAP/Finance approval fields after BP creation and before final approval |

Customer SAP data body:

Use values returned by:

- `GET /api/BPmaster/GetBPGroups?company=1&bpType=C`
- `GET /api/BPmaster/GetARAccounts?company=1`
- `GET /api/BPmaster/GetPaymentTerms?company=1`
- `GET /api/BPmaster/GetSalesEmployees?company=1`
- `GET /api/BPmaster/GetTerritories?company=1`

```json
{
  "masterId": 1254,
  "userId": 76,
  "cardCodePrefix": "CUSTA",
  "bpGroupCode": 132,
  "bpGroupName": "ANDHRA PRADESH",
  "arAccountCode": "1101001",
  "paymentTermCode": 29,
  "salesEmployeeCode": 78,
  "territoryId": -2,
  "sapBankCode": ""
}
```

Vendor SAP data body:

Use values returned by:

- `GET /api/BPmaster/GetBPGroups?company=1&bpType=V`
- `GET /api/BPmaster/GetAPAccounts?company=1`
- `GET /api/BPmaster/GetPaymentTerms?company=1`
- `GET /api/BPmaster/GetBankCodes?company=1&countryCode=IN`

```json
{
  "masterId": 1255,
  "userId": 76,
  "cardCodePrefix": "VENDA",
  "bpGroupCode": 101,
  "bpGroupName": "BRANCH VENDOR",
  "apAccountCode": "2110005",
  "paymentTermCode": 29,
  "sapBankCode": "HDFC"
}
```

Vendor SAP data form-data example:

```text
masterId=1255
userId=76
cardCodePrefix=VENDA
bpGroupCode=101
bpGroupName=BRANCH VENDOR
apAccountCode=2110005
paymentTermCode=29
sapBankCode=HDFC
```

Rules:

- `masterId` is required; frontend does not need to send `sapDataId`.
- `userId` should be sent so `BP.jsSAPData.updatedBy` records who changed SAP approval values.
- Partial update is supported. Omitted fields keep their existing values.
- Allowed while `BP.jsFlow.status` is `P` or `R`.
- Blocked when `BP.jsFlow.status` is `A`.
- Final approval and retry read these values from `BP.jsSAPData`.
- Create BP and Update BP must not write these values to `BP.jsMaster`.
- For vendors, `sapBankCode` overrides the create-time bank code during SAP posting and must exist in SAP `ODSC`.
- For vendors, `bpGroupCode` and `apAccountCode` must be a valid SAP combination. If SAP returns `(200002) Incorrect Group or Payable/Receivable Account`, the payload reached SAP but SAP rejected that group/account pair. Pick both values from the live dropdown APIs and retry.
- Do not send retired fields `debPayAcct`, `wtLabel`, or `series`; SAP `DebitorAccount` is calculated during SAP payload creation from `arAccountCode` for customers and `apAccountCode` for vendors.

### Frontend Copy-Paste Payloads (JSON + Form-Data)

Use these payloads to test quickly from both body types.

Important:

- `allBillAddresses` is required in both Create APIs.
- `allBillAddresses` can be empty only when business rules allow zero addresses in the environment.
- Create APIs must not send SAP approval fields (`cardCodePrefix`, `bpGroupCode`, `arAccountCode`, `apAccountCode`, `paymentTermCode`, `salesEmployeeCode`, `territoryId`, `sapBankCode`).
- Use `masterId` from Create response for SAP and approval endpoints.

#### Create BP (Customer) JSON

```json
{
  "company": "JIVO_OIL_HANADB",
  "companyId": 1,
  "type": "C",
  "customerType": "B2B",
  "cardName": "TEST CUSTOMER BP 0612 01",
  "foreignName": "TEST CUSTOMER FOREIGN",
  "typeOfBusiness": "Company",
  "industry": "FMCG",
  "contactFirst": "RAMESH",
  "contactLast": "KUMAR",
  "contactTitle": "OWNER",
  "mobile": "9876543210",
  "email": "customer061201@example.com",
  "contactEmail": "accounts061201@example.com",
  "gstin": "03AAKCU6101F1Z5",
  "pan": "AAKCU6101F",
  "currency": "INR",
  "remarks": "Customer registration test",
  "isStaff": false,
  "userId": 76,
  "companyByUser": "JIVO_OIL_HANADB",
  "sameAsBill": true,
  "allBillAddresses": [
    {
      "addrName": "BILL-CUST-061201",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCU6101F1Z5"
    }
  ],
  "allShipAddresses": [
    {
      "addrName": "SHIP-CUST-061201",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCU6101F1Z5"
    }
  ],
  "hasMsme": false,
  "msmeNo": "",
  "msmeType": "",
  "msmeBType": ""
}
```

Create BP (Customer) Form-Data direct fields

```text
company=JIVO_OIL_HANADB
companyId=1
type=C
customerType=B2B
cardName=TEST CUSTOMER BP 0612 01
foreignName=TEST CUSTOMER FOREIGN
typeOfBusiness=Company
industry=FMCG
contactFirst=RAMESH
contactLast=KUMAR
contactTitle=OWNER
mobile=9876543210
email=customer061201@example.com
contactEmail=accounts061201@example.com
gstin=03AAKCU6101F1Z5
pan=AAKCU6101F
currency=INR
remarks=Customer registration test
isStaff=false
userId=76
companyByUser=JIVO_OIL_HANADB
sameAsBill=true
allBillAddresses=[{"addrName":"BILL-CUST-061201","street":"PLOT 14 INDUSTRIAL AREA","block":"PHASE 2","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN","gstin":"03AAKCU6101F1Z5"}]
allShipAddresses=[{"addrName":"SHIP-CUST-061201","street":"PLOT 14 INDUSTRIAL AREA","block":"PHASE 2","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN","gstin":"03AAKCU6101F1Z5"}]
hasMsme=false
msmeNo=
msmeType=
msmeBType=
```

#### Create BP (Vendor) JSON

```json
{
  "type": "V",
  "company": "JIVO_OIL_HANADB",
  "companyId": 1,
  "vendorType": "SUPPLIER",
  "cardName": "TEST VENDOR BP 0612 02",
  "foreignName": "TEST VENDOR FOREIGN",
  "typeOfBusiness": "Partnership",
  "industry": "IT Services",
  "contactFirst": "ROHIT",
  "contactLast": "RATHOD",
  "contactTitle": "OWNER",
  "mobile": "8571954685",
  "altContact": "0161123456",
  "email": "vendor061202@example.com",
  "gstin": "03AAKCT6102F1Z5",
  "pan": "AAKCT6102F",
  "tan": "PTLA12345B",
  "currency": "INR",
  "remarks": "Vendor registration test",
  "isStaff": false,
  "userId": 76,
  "companyByUser": "JIVO_OIL_HANADB",
  "sameAsBill": true,
  "allBillAddresses": [
    {
      "addrName": "BILL-VEND-061202",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCT6102F1Z5"
    }
  ],
  "allShipAddresses": [
    {
      "addrName": "SHIP-VEND-061202",
      "street": "PLOT 14 INDUSTRIAL AREA",
      "block": "PHASE 2",
      "city": "LUDHIANA",
      "zip": "141001",
      "state": "PB",
      "country": "IN",
      "gstin": "03AAKCT6102F1Z5"
    }
  ],
  "hasMsme": true,
  "msmeNo": "UDYAM-PB-00-0612022",
  "msmeType": "MICRO",
  "msmeBType": "Manufacturing",
  "fssaiNo": "10012022006112",
  "bankAccounts": [
    {
      "bankCode": "HDFC",
      "bankName": "HDFC BANK",
      "vendorName": "TEST VENDOR BP 0612 02",
      "branch": "LUDHIANA",
      "accNo": "50100123456789",
      "ifsc": "HDFC0001234",
      "swiftCode": "HDFCINBB",
      "accountType": "Current",
      "isPrimary": true
    }
  ]
}
```

Create BP (Vendor) Form-Data direct fields

```text
type=V
company=JIVO_OIL_HANADB
companyId=1
vendorType=SUPPLIER
cardName=TEST VENDOR BP 0612 02
foreignName=TEST VENDOR FOREIGN
typeOfBusiness=Partnership
industry=IT SERVICES
contactFirst=ROHIT
contactLast=RATHOD
contactTitle=OWNER
mobile=8571954685
altContact=0161123456
email=vendor061202@example.com
gstin=03AAKCT6102F1Z5
pan=AAKCT6102F
tan=PTLA12345B
currency=INR
remarks=Vendor registration test
isStaff=false
userId=76
companyByUser=JIVO_OIL_HANADB
sameAsBill=true
allBillAddresses=[{"addrName":"BILL-VEND-061202","street":"PLOT 14 INDUSTRIAL AREA","block":"PHASE 2","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN","gstin":"03AAKCT6102F1Z5"}]
allShipAddresses=[{"addrName":"SHIP-VEND-061202","street":"PLOT 14 INDUSTRIAL AREA","block":"PHASE 2","city":"LUDHIANA","zip":"141001","state":"PB","country":"IN","gstin":"03AAKCT6102F1Z5"}]
hasMsme=true
msmeNo=UDYAM-PB-00-0612022
msmeType=MICRO
msmeBType=Manufacturing
fssaiNo=10012022006112
bankAccounts=[{"bankCode":"HDFC","bankName":"HDFC BANK","vendorName":"TEST VENDOR BP 0612 02","branch":"LUDHIANA","accNo":"50100123456789","ifsc":"HDFC0001234","swiftCode":"HDFCINBB","accountType":"Current","isPrimary":true}]
```

#### UpdateSAPData (Customer) JSON

```json
{
  "masterId": 1254,
  "userId": 76,
  "cardCodePrefix": "CUSTA",
  "bpGroupCode": 132,
  "bpGroupName": "ANDHRA PRADESH",
  "arAccountCode": "1101001",
  "paymentTermCode": 29,
  "salesEmployeeCode": 78,
  "territoryId": -2,
  "sapBankCode": ""
}
```

UpdateSAPData (Customer) Form-Data

```text
masterId=1254
userId=76
cardCodePrefix=CUSTA
bpGroupCode=132
bpGroupName=ANDHRA PRADESH
arAccountCode=1101001
paymentTermCode=29
salesEmployeeCode=78
territoryId=-2
```

#### UpdateSAPData (Vendor) JSON

```json
{
  "masterId": 1255,
  "userId": 76,
  "cardCodePrefix": "VENDA",
  "bpGroupCode": 101,
  "bpGroupName": "BRANCH VENDOR",
  "apAccountCode": "2110005",
  "paymentTermCode": 29,
  "sapBankCode": "HDFC"
}
```

UpdateSAPData (Vendor) Form-Data

```text
masterId=1255
userId=76
cardCodePrefix=VENDA
bpGroupCode=101
bpGroupName=BRANCH VENDOR
apAccountCode=2110005
paymentTermCode=29
sapBankCode=HDFC
```

#### Approve / Reject / Retry examples

Approve:

```json
{
  "flowId": 1155,
  "company": 1,
  "userId": 70,
  "remarks": "Approved by manager.",
  "action": "Approve"
}
```

```text
flowId=1155
company=1
userId=70
remarks=Approved by manager.
action=Approve
```

Reject:

```json
{
  "flowId": 1155,
  "company": 1,
  "userId": 70,
  "remarks": "Missing tax document.",
  "action": "Reject"
}
```

```text
flowId=1155
company=1
userId=70
remarks=Missing tax document.
action=Reject
```

Retry SAP:

```json
{
  "flowId": 1155,
  "company": 1,
  "userId": 108,
  "remarks": "Retry after correction in SAP approval fields."
}
```

```text
flowId=1155
company=1
userId=108
remarks=Retry after correction in SAP approval fields.
```

GetSAPData:

```text
GET /api/BPmaster/GetSAPData?masterId=1255
```

- JSON returns same data in `data` node, with `apiStatusTag`, `cardCodePrefix`, `bpGroupCode`, `bpGroupName`, `arAccountCode`, `apAccountCode`, `paymentTermCode`, `salesEmployeeCode`, `territoryId`, `sapBankCode`, `retryCount`.

### Get Single BP Data

| Item | Value |
|---|---|
| Method | `GET` |
| URL | `/api/BPmaster/GetSingleBPData?bpCode=1234` |
| Purpose | Load complete BP detail |

Use case:

- Populate edit form
- Support checks
- Confirm data before retry

### Get Pending BP

| Item | Value |
|---|---|
| Method | `GET` |
| URL | `/api/BPmaster/GetPendingBP?userId=76&companyId=1&month=05-2026` |
| Purpose | Show BPs waiting for this user |

Response shape:

```json
{
  "success": true,
  "data": [
    {
      "workflow": {
        "flowId": 1153,
        "sapStatus": "SAP Failed",
        "apiMessage": "Internal error (-5002) occurred",
        "sapCardCode": ""
      },
      "master": {},
      "taxDetails": {},
      "billingAddresses": [],
      "shippingAddresses": [],
      "bankDetails": [],
      "contactPersons": [],
      "attachments": []
    }
  ]
}
```

The `workflow` block intentionally contains only:

- `flowId`
- `sapStatus`
- `apiMessage`
- `sapCardCode`

Business details are returned in the separate `master`, `taxDetails`, `billingAddresses`, `shippingAddresses`, `bankDetails`, `contactPersons`, and `attachments` blocks.

### Get Approved BP

| Item | Value |
|---|---|
| Method | `GET` |
| URL | `/api/BPmaster/GetApprovedBP?userId=76&companyId=1&month=05-2026` |
| Purpose | Show BPs approved by this user |

### Get Rejected BP

| Item | Value |
|---|---|
| Method | `GET` |
| URL | `/api/BPmaster/GetRejectedBP?userId=76&companyId=1&month=05-2026` |
| Purpose | Show BPs rejected by this user |

### Approve BP

| Item | Value |
|---|---|
| Method | `POST` |
| URL | `/api/BPmaster/ApproveBP` |
| Content Type | `application/json`, `multipart/form-data`, or `application/x-www-form-urlencoded` |
| Purpose | Approve current stage; final stage posts to SAP |

Request:

```json
{
  "flowId": 1153,
  "company": 1,
  "userId": 76,
  "remarks": "Approved",
  "action": "Approve"
}
```

Form-data direct field example:

```text
flowId=1153
company=1
userId=70
remarks=Verified.
action=Approve
```

Final-stage SAP success response:

```json
{
  "success": true,
  "message": "BP approved and activated successfully.",
  "data": {
    "approvalStatus": "Approved",
    "sapStatus": "Success",
    "sapCardCode": "VENDA001583"
  }
}
```

SAP failure response:

```json
{
  "success": false,
  "approvalStatus": "Blocked",
  "sapStatus": "Failed: Internal error (-5002) occurred",
  "message": "Internal error (-5002) occurred"
}
```

### Reject BP

| Item | Value |
|---|---|
| Method | `POST` |
| URL | `/api/BPmaster/RejectBP` |
| Content Type | `application/json`, `multipart/form-data`, or `application/x-www-form-urlencoded` |
| Purpose | Reject current approval stage |

Request:

```json
{
  "flowId": 1153,
  "company": 1,
  "userId": 76,
  "remarks": "PAN attachment mismatch.",
  "action": "Reject"
}
```

Form-data direct field example:

```text
flowId=1153
company=1
userId=69
remarks=PAN attachment mismatch.
action=Reject
```

### Retry SAP Post

| Item | Value |
|---|---|
| Method | `POST` |
| URL | `/api/BPmaster/RetrySapPost` |
| Content Type | `application/json`, `multipart/form-data`, or `application/x-www-form-urlencoded` |
| Purpose | Retry failed final-stage SAP posting |

JSON request:

```json
{
  "flowId": 1153,
  "company": 1,
  "userId": 108,
  "remarks": "Retry after SAP correction."
}
```

Form-data direct field example:

```text
flowId=1153
company=1
userId=108
remarks=Retry after SAP correction.
```

Rules:

- Flow must be pending.
- Flow must be at final stage.
- SAP status must be failed (`apiStatusTag = 'N'`).
- Current user must be assigned to final stage.
- Retry reads latest SQL data, not stale payload snapshots.

Request:

```json
{
  "flowId": 1153,
  "company": 1,
  "userId": 76,
  "remarks": "Retry after correction."
}
```

### Get BP Approval Flow

| Item | Value |
|---|---|
| Method | `GET` |
| URL | `/api/BPmaster/GetBPApprovalFlow?flowId=1153` |
| Purpose | Show stages, approvers, and action history |

### Insights And Combined APIs

| API | Purpose |
|---|---|
| `/api/BPmaster/GetBPInsights?userId=76&companyId=1&month=05-2026` | Pending/approved/rejected counts |
| `/api/BPmaster/GetBPInsightsByCreator?userId=76&companyId=1&month=05-2026` | Creator/approver dashboard alias |
| `/api/BPmaster/GetBPCounts?month=05-2026&userId=76&companyId=1` | BP count summary |
| `/api/BPmaster/GetTotalBPData` | Combined BP total data |
| `/api/BPmaster/GetAllBpPendingApproval` | BP-only pending data |
| `/api/BPmaster/GetAllBpApprovedApproval` | BP-only approved data |
| `/api/BPmaster/GetAllBpRejectedApproval` | BP-only rejected data |
| `/api/BPmaster/GetAllBpTotalApproval` | BP-only total data |

These APIs must return BP data only. They must not return IMC/item data.

### Lookup APIs

| API | Purpose |
|---|---|
| `/api/BPmaster/GetOptions?company=1&bpType=C&isStaff=false&countryCode=IN` | Consolidated options |
| `/api/BPmaster/GetBankCodes?company=1&countryCode=IN` | Official vendor bank-code dropdown from SAP bank master |
| `/api/BPmaster/GetDistinctBankName?company=1` | Compatibility bank lookup; delegates to the same bank-code source |
| `/api/BPmaster/GetCountry?company=1` | Countries |
| `/api/BPmaster/GetDistinctStates?company=1&CountryCode=IN` | States |
| `/api/BPmaster/GetUniquePANs?company=1` | Existing PANs |
| `/api/BPmaster/GetGSTMismatchByState?company=1&stateCode=PB` | GST-state validation |
| `/api/BPmaster/BPGetCardInfo?company=1&BPType=C&IsStaff=false` | Existing SAP card info |
| `/api/BPmaster/GetBpPANByBranch?Branch=Ludhiana&company=1` | PAN lookup by branch |
| `/api/BPmaster/GetSAPData?masterId=1234` | SAP approval fields and SAP posting status |
| `/api/BPmaster/GetSPAData?masterId=1234` | Compatibility alias for `GetSAPData` |
| `/api/BPmaster/UpdateSAPData` | Save SAP approval fields in `BP.jsSAPData` |
| `/api/BPmaster/UpdateSapData` | Compatibility alias for `UpdateSAPData` |

---

## 10. Troubleshooting Guide

### BP Not Visible In Pending

Root causes:

- Flow is not pending.
- User is not mapped to `currentStageId`.
- Wrong company selected.
- Wrong month filter.
- User already acted on current stage.

Verification:

```sql
SELECT
    f.id AS flowId,
    f.bpCode,
    f.status,
    f.currentStage,
    f.currentStageId,
    f.templateId,
    m.company,
    m.name
FROM BP.jsFlow f
JOIN BP.jsMaster m ON m.code = f.bpCode
WHERE f.bpCode = 1234;
```

Check current approver:

```sql
SELECT us.*
FROM dbo.jsUserStage us
WHERE us.stageId = 1259
  AND ISNULL(us.status, 1) = 1;
```

Fix:

- Correct user-stage mapping.
- Use correct `userId`, `companyId`, and `month`.
- Confirm `BP.jsFlow.status = 'P'`.

### SAP Approval Failed

Root causes:

- Invalid BP group.
- Invalid AR/AP account.
- Invalid payment term.
- Invalid sales employee.
- Invalid territory.
- Invalid GSTIN/state/country.
- Invalid bank code.
- SAP session expired.
- Attachment path unavailable.

Verification:

```sql
SELECT *
FROM BP.jsSAPData
WHERE masterId = 1234
ORDER BY id DESC;
```

Fix:

- Read `apiMessage`.
- Correct BP data through `UpdateBPMaster`.
- Retry final SAP post.

### Workflow Stuck

Root causes:

- `currentStage` and `currentStageId` mismatch.
- Template/stage mapping changed after flow creation.
- User-stage mapping missing.
- SAP failed and final approver has not retried.

Verification:

```sql
SELECT
    f.*,
    s.stage AS currentStageName
FROM BP.jsFlow f
LEFT JOIN dbo.jsStage s ON s.id = f.currentStageId
WHERE f.id = 1153;
```

Fix:

- Correct template/user-stage setup.
- If SAP failed, correct data and retry.

### Retry Button Not Visible

Root causes:

- Flow is not final stage.
- `apiStatusTag` is not `N`.
- User is not final-stage approver.
- Flow status is not `P`.

Verification:

```sql
SELECT
    f.id,
    f.status,
    f.currentStage,
    f.totalStage,
    sd.apiStatusTag,
    sd.apiMessage
FROM BP.jsFlow f
LEFT JOIN BP.jsSAPData sd ON sd.masterId = f.bpCode
WHERE f.id = 1153;
```

Fix:

- Make sure final-stage user is logged in.
- Confirm SAP status is failed.

### No AP Accounts Returned

Root causes:

- Wrong company schema.
- Old query only checked `FatherNum='2101000'`.
- SAP chart of accounts has AP accounts under `211%`.
- HANA privilege issue.

Verification:

```sql
SELECT "AcctCode", "AcctName"
FROM "JIVO_OIL_HANADB"."OACT"
WHERE ("FatherNum" = '2101000' OR "AcctCode" LIKE '211%')
  AND "Finanse" = 'N'
ORDER BY "AcctCode";
```

Fix:

- Confirm backend uses current Node-compatible AP query.
- Check HANA connection/schema in `appsettings.json`.
- Check HANA privileges.

### No BP Groups Returned

Root causes:

- SAP Service Layer session failed.
- Wrong company.
- No groups for selected `bpType`.
- Service Layer permission issue.

Verification:

```http
GET BusinessPartnerGroups?$filter=Type eq 'bbpgt_CustomerGroup'&$select=Code,Name&$orderby=Name
GET BusinessPartnerGroups?$filter=Type eq 'bbpgt_VendorGroup'&$select=Code,Name&$orderby=Name
```

Fix:

- Verify SAP session for company.
- Verify Service Layer user permissions.

### Card Code Generation Failed

Root causes:

- Prefix blank and default failed.
- SAP already has unexpected card code format.
- Service Layer query failed.

Fix:

- Check `cardCodePrefix`.
- Check existing SAP `CardCode` values.
- Review backend logs for generated candidate code.

### Control Account Error

Example:

```text
Define account in "Liabilities" drawer [OCRD.DebPayAcct]
```

Root cause:

- Vendor AP account is not valid for liabilities.
- Customer AR account is not valid for assets/receivables.
- Account is not postable.
- Account does not exist.
- SAP rejected the selected BP group and control account combination.

For this SAP error:

```text
(200002) Incorrect Group or Payable/Receivable Account
```

The BP payload reached SAP, but SAP rejected the combination of `GroupCode` and `DebitorAccount`. For vendors, `GroupCode` comes from `BP.jsSAPData.bpGroupCode` and `DebitorAccount` comes from `BP.jsSAPData.apAccountCode`. For customers, `GroupCode` comes from `BP.jsSAPData.bpGroupCode` and `DebitorAccount` comes from `BP.jsSAPData.arAccountCode`.

Verification:

```sql
SELECT "AcctCode", "AcctName", "Postable", "GroupMask"
FROM "JIVO_OIL_HANADB"."OACT"
WHERE "AcctCode" = '2110000';
```

Fix:

- Select a valid dropdown value from `GetAPAccounts` or `GetARAccounts`.
- Select BP group from `GetBPGroups` using the correct `bpType`.
- Confirm the BP group and control account are valid together in SAP.
- Confirm account is postable and valid in SAP.

### Attachment Upload Failed

Root causes:

- File missing from upload folder.
- File path inaccessible after deployment.
- SAP Attachments2 path issue.
- File type mismatch.

Verification:

```sql
SELECT *
FROM BP.jsAttachments
WHERE code = 1234
ORDER BY uploadedOn DESC;
```

Fix:

- Confirm file exists on server.
- Confirm upload folder permissions.
- Retry SAP after file issue is fixed.

### SAP Session Expired

Root causes:

- Service Layer session timeout.
- Bad company login.
- SAP unavailable.

Fix:

- Confirm SAP Service Layer health.
- Confirm company-specific SAP credentials.
- Retry after session refresh.

---

## 11. Developer Change Guide

### Before Adding A New Field

Ask:

1. Does the current frontend form need this field?
2. Is it customer-only, vendor-only, or both?
3. Is it SQL-only or SAP-posted?
4. Is it required?
5. Does it need dropdown master data?
6. Does it need audit/snapshot support?

### Files To Update

| Area | File/Place |
|---|---|
| Request DTO | `Models/BPmasterModels.cs` |
| Response DTO | `Models/BPmasterModels.cs` |
| Controller route | `Controllers/BPmasterController.cs` |
| SQL orchestration | `Services/Implementation/BPmasterService.cs` |
| SAP payload | `Services/Implementation/BPMasterSapService.cs` |
| Service interfaces | `Services/Interfaces/IBPmasterService.cs`, `IBPMasterSapService.cs` |
| Database columns | `BP.jsMaster`, `BP.jsSAPData`, or child table depending ownership |
| Stored procedures | `BP.jsInsertBPMasterData`, `BP.jsUpdateBPMasterData`, detail/list procedures |
| Snapshots | `BP.jsMasterSnapshot` or child snapshot table |
| Audit | `BP.jsAuditLog` logic |
| Documentation | This guide |

### Safe Change Checklist

1. Confirm the frontend field exists.
2. Add request/response DTO properties.
3. Add database column through idempotent SQL.
4. Update create procedure.
5. Update update procedure.
6. Update detail/list procedures.
7. Update snapshot/restore procedures.
8. Update audit logging.
9. Update SAP payload only if SAP needs the field.
10. Add dropdown API if the value must come from SAP.
11. Build `JSAPNEW.csproj`.
12. Test create, update, pending list, detail, final approval, and retry.
13. Update this guide in the same change.

### SQL Deployment Order

1. Backup database or confirm rollback plan.
2. Run `bp-add-sap-fields-to-sapdata.sql`.
3. Run `bp-clean-master-sap-field-procedure-dependencies.sql`.
4. Run the dependency verification query at the end of the cleanup script.
5. Continue only when no `BP.jsMaster` procedures reference SAP approval columns.
6. Deploy backend code.
7. Restart API process.
8. Test Create BP without SAP approval fields and verify `masterId`, `sapDataId`, and `flowId` are returned.
9. Test `/api/BPmaster/GetSAPData?masterId=<created masterId>`.
10. Test `/api/BPmaster/UpdateSAPData`.
11. Test final approval/retry.
12. Run `bp-remove-sap-fields-from-master.sql`.
13. Test dropdown APIs.
14. Verify SAP result.

Current SAP master-data scripts:

| Script | Purpose |
|---|---|
| `docs/implementation/bp-create-sapdata-on-create-flow.sql` | Adds SAPData status/default columns, `sapBankCode`, and updates SAPData/status procedures so one SAPData row exists before approval |
| `docs/implementation/bp-add-sap-fields-to-sapdata.sql` | Adds SAP approval columns to `BP.jsSAPData` and updates SAPData read/update procedures |
| `docs/implementation/bp-reorder-sapdata-remove-lastattempt.sql` | Rebuilds active `BP.jsSAPData` in the requested column order and removes `lastAttemptOn` / `lastAttemptBy` after dependency checks |
| `docs/implementation/bp-remove-sapdata-unused-legacy-fields.sql` | Removes unused BP SAPData legacy fields `debPayAcct`, `wtLabel`, and `series`; SAP `DebitorAccount` is derived from `arAccountCode` / `apAccountCode` |
| `docs/implementation/bp-rollback-sapdata-unused-legacy-fields.sql` | Rollback helper to re-add removed legacy SAPData columns if a DB compatibility rollback is needed |
| `docs/implementation/bp-clean-master-sap-field-procedure-dependencies.sql` | Alters existing BP procedures so master/detail/list/audit/snapshot logic no longer reads SAP approval fields from `BP.jsMaster` |
| `docs/implementation/bp-remove-sap-fields-from-master.sql` | Removes SAP approval columns from `BP.jsMaster` after dependent procedure references are gone |

---

## 12. Support Team Quick Guide

### When A User Reports A BP Issue

Ask for:

- BP code
- flow id
- company id
- user id
- screenshot/API response
- whether issue is create, pending, approve, reject, SAP failure, or retry

### First Queries To Run

Check master:

```sql
SELECT *
FROM BP.jsMaster
WHERE code = 1234;
```

Check flow:

```sql
SELECT *
FROM BP.jsFlow
WHERE bpCode = 1234;
```

Check SAP status:

```sql
SELECT *
FROM BP.jsSAPData
WHERE masterId = 1234
ORDER BY id DESC;
```

Check current approver:

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

Check child data:

```sql
SELECT * FROM BP.jsTaxDetails WHERE code = 1234;
SELECT * FROM BP.jsMasterAddress WHERE code = 1234 ORDER BY addressType, addressID;
SELECT * FROM BP.jsContactPersons WHERE code = 1234 ORDER BY contactID;
SELECT * FROM BP.jsBankDetails WHERE code = 1234 ORDER BY bankDetailID;
SELECT * FROM BP.jsAttachments WHERE code = 1234 ORDER BY uploadedOn DESC;
```

### Quick Decision Table

| User Problem | Check First | Likely Fix |
|---|---|---|
| BP not visible in pending | `BP.jsFlow`, `dbo.jsUserStage` | Correct approver mapping or query params |
| SAP approval failed | `BP.jsSAPData.apiMessage` | Correct field and retry |
| Retry not visible | `apiStatusTag`, final stage, current user | Use final approver and failed SAP status |
| AP accounts empty | HANA `OACT` AP query | Check company/schema/privileges/query |
| BP group empty | Service Layer `BusinessPartnerGroups` | Check SAP session/permission |
| Wrong SAP data posted | `GetSPAData`, `BP.jsSAPData`, payload logs | Update SAP section and retry before final approval |
| Approved BP cannot update | `BP.jsFlow.status='A'` | This is expected behavior |

---

## 13. QA Test Checklist

### Create Tests

- Create customer with no attachments.
- Create customer with attachments.
- Create vendor with bank details.
- Create vendor with MSME details.
- Create with `company` as number.
- Create with `company` as SAP schema name.
- Verify `userId` is stored.
- Verify `createDate`, `updationDate`, and `action`.

### Update Tests

- Update pending BP.
- Update rejected BP.
- Confirm approved BP update is blocked.
- Update SAP approval fields and confirm `GetSPAData` returns new values.
- Update vendor bank and confirm `vendorName` maps to SAP account name.

### Approval Tests

- Approve stage 1.
- Approve stage 2.
- Final approval with valid SAP data.
- Final approval with invalid AP account.
- Correct failed data and retry.

### Dropdown Tests

- Call all dropdown APIs for company 1, 2, and 3.
- Confirm no duplicate `code`/`name` aliases in SAP master-data dropdown responses.
- Confirm AP accounts return rows where SAP has matching accounts.
- Confirm frontend binds exact returned fields.

---

## 14. Important Maintainer Rules

- Do not create a new BP module.
- Do not create duplicate BP documentation.
- Do not add a BP field unless it exists in the current Customer/Vendor form.
- Do not delete workflow, approval, SAP, audit, snapshot, or attachment data.
- Do not remove `isStaff`.
- Do not hardcode SAP dropdown values.
- Do not send retired legacy fields to SAP.
- Do not mark final approval successful unless SAP created the BP.
- Keep retry available only for final-stage SAP failures.
- Keep BP editable while workflow is `P` or `R`.
- Block updates after workflow status is `A`.
- Validate `.NET` build after backend changes.
- Validate stored procedures in a rollback transaction before live deployment.
- Update this guide whenever BP contract, SQL, SAP mapping, or workflow behavior changes.
