# BP Unused Fields Cleanup Report

Generated on 2026-05-22 for `JSAPNEW`.

## Scope

The cleanup targets only retired BP business fields that are not present in the current SAP Portal customer/vendor forms. The following are preserved:

- `BP.jsMaster.isStaff`
- Workflow tables: `BP.jsFlow`, `BP.jsFlowStatus`
- SAP status/audit table: `BP.jsSAPData`
- Attachment table and upload flow: `BP.jsAttachments`
- Approval stage/configuration tables under `dbo`
- Snapshot/audit tables and restore flow, updated to active fields

## Removed Columns

| Table | Removed columns |
|---|---|
| `BP.jsMaster` | `staffCode`, `groupID`, `mainGroupID`, `chain`, `contactPerson`, `paymentTermID`, `creditLimit`, `priceList` |
| `BP.jsMasterAddress` | `email`, `isDefault`, `gstType`, `addressUid` |
| `BP.jsContactPersons` | `email`, `phone`, `telephone`, `isPrimary`, `contactUid` |
| `BP.jsTaxDetails` | `msmeType`, `msmeBusinessType` |
| `BP.jsBankDetails` | `countryID`, `acctName` |

## Added or Renamed Active Storage

| Active field | Storage |
|---|---|
| `foreignName` / `foreignTradeName` | `BP.jsMaster.foreignName` |
| `typeOfBusiness` | `BP.jsMaster.typeOfBusiness` |
| `industry` / `industrySector` | `BP.jsMaster.industry` |
| `currency` | `BP.jsMaster.currency` |
| `remarks` | `BP.jsMaster.remarks` |
| `addressName` | `BP.jsMasterAddress.addressName`, migrated from `addressUid` |
| `emailAddress` | `BP.jsContactPersons.emailAddress`, migrated from `email` |
| `mobileNumber` / `mobile` | `BP.jsContactPersons.mobileNumber`, migrated from `phone`; `BP.jsMaster.mobileNo` remains header phone |
| `alternateContact` | `BP.jsContactPersons.alternateContact`, migrated from `telephone` |
| `alternateEmail` | `BP.jsContactPersons.alternateEmail` |
| `gstin` | `BP.jsTaxDetails.gstin` and address `gstNo` |
| `accountType` | `BP.jsBankDetails.accountType` |

## Removed Stored Procedure Parameters

Removed from `BP.jsInsertBPMasterData` and `BP.jsUpdateBPMasterData`:

- `@staffCode`
- `@groupID`
- `@mainGroupID`
- `@chain`
- `@contactPerson`
- `@paymentTermID`
- `@creditLimit`
- `@priceList`
- `@buyerTANNo` (replaced by `@tan`)
- `@msmeType`
- `@msmeBusinessType`
- `@bankCountryID`
- `@acctName`
- `@branch` (replaced by `@branchName`)

Removed TVP columns:

- `BP.AddressTableType`: `email`, `isDefault`, `addressUid`
- `BP.ContactPersonTableType`: `email`, `phone`, `telephone`, `isPrimary`, `contactUid`

Removed obsolete helper procedures:

- `BP.jsGetAddressUid`
- `BP.jsGetContactUid`
- `BP.jsUpdateBPMasterData_DEBUG`

## Updated Procedures

| Procedure | Update |
|---|---|
| `BP.jsInsertBPMasterData` | New portal request fields only; inserts active master/tax/address/contact/bank/attachment data |
| `BP.jsUpdateBPMasterData` | New portal update fields only; keeps snapshots and audit rows for active fields |
| `BP.jsGetSingleBPData` | Returns active field names for master, tax, billing/shipping addresses, contacts, bank, attachments |
| `BP.jsGetPendingBP` | Returns active queue fields plus workflow/SAP retry status |
| `BP.jsGetApprovedBP` | Returns active list fields |
| `BP.jsGetRejectedBP` | Returns active list fields plus rejection remark |
| `BP.jsGetBPInsights` | Rewritten to count directly without old list-table shapes |
| `BP.jsApproveBP` | Preserved; metadata refreshed because it does not reference retired fields |
| `BP.jsRejectBP` | Preserved; metadata refreshed because it does not reference retired fields |
| `BP.jsGetBPApprovalFlow` | Preserved; metadata refreshed |
| `BP.jsGetBPSnapshots` | Updated to active snapshot summary fields |
| `BP.jsRestoreBPFromSnapshot` | Updated to restore active fields only |

## Removed DTO and API Fields

Removed from BP request/response DTOs:

- `StaffCode`
- `GroupID`
- `MainGroupID`
- `Chain`
- `ContactPerson`
- `PaymentTermID`
- `CreditLimit`
- `PriceList`
- `BuyerTANNo`
- `PanNo` as public API name, replaced by `panNumber` or `pan`
- `FssaiNo` as public API name, replaced by `fssaiLicense`
- `MsmeNo` as public API name, replaced by `msme`
- `MsmeType`
- `MsmeBusinessType`
- `BankCountryID`
- `AcctName`
- `Address.Email`
- `Address.IsDefault`
- `Address.AddressUid`, replaced by `addressName`
- `Contact.Email`, replaced by `emailAddress`
- `Contact.Phone`, replaced by `mobileNumber`
- `Contact.Telephone`, replaced by `alternateContact`
- `Contact.IsPrimary`
- `Contact.ContactUid`

Removed controller routes tied to retired UID fields:

- `GET /api/BPmaster/CheckAddressUid`
- `GET /api/BPmaster/CheckContactUid`

## SAP Payload Cleanup

Stopped sending retired/hidden fields:

- `CreditLimit`
- `GroupCode`
- `PayTermsGrpCode`
- `U_Main_Group`
- `U_Chain`
- `U_MSME_Type`
- `U_MSME_BType`
- `DebitorAccount`

Current payload uses only portal-active fields plus generated SAP identifiers:

- `OCRD`: card code, name, type, foreign name, phone, email, currency, notes, portal UDFs, attachment entry
- `CRD1`: billing/shipping address fields and GSTIN
- `OCPR`: contact name, designation, mobile, alternate contact, email
- `OCRB`: vendor bank name/code, account number, branch, IFSC, SWIFT
- `Attachments2`: uploaded files

## Migration Notes

Migration script:

`docs/implementation/bp-remove-unused-columns.sql`

Safety behavior:

- Runs inside one transaction with `XACT_ABORT ON`
- Backs up retired-column values before drops
- Uses `COL_LENGTH`, `OBJECT_ID`, `TYPE_ID`, and `DROP ... IF EXISTS`
- Preserves workflow, approval, SAP status, attachments, and stage tables
- Expands snapshot tables but does not remove historical snapshot columns

Backup tables created:

- `BP.jsMaster_RemovedColumnsBackup`
- `BP.jsMasterAddress_RemovedColumnsBackup`
- `BP.jsContactPersons_RemovedColumnsBackup`
- `BP.jsTaxDetails_RemovedColumnsBackup`
- `BP.jsBankDetails_RemovedColumnsBackup`

## Rollback Notes

If the script fails before commit, SQL Server rolls back the transaction automatically.

If rollback is needed after commit:

1. Use the printed `MigrationRunId`.
2. Re-add required legacy columns.
3. Restore values from the backup tables above.
4. Redeploy the pre-cleanup stored procedures/TVP types from source control or database backup.
5. Rebuild/redeploy the pre-cleanup .NET API if the legacy portal must be restored.

The migration intentionally does not delete workflow rows, approval history, SAP API status, attachment rows, or historical snapshot rows.
