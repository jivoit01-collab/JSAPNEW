# BP Field Mapping Documentation

Generated on 2026-05-22 for the SAP Portal BP Master cleanup.

## Active Fields

### Customer

| Frontend field | Required | DB column/source | SAP mapping |
|---|---:|---|---|
| `companyId` | Yes | `BP.jsMaster.company` | SAP company session selection |
| `customerType` | Yes | `BP.jsMaster.type = C` | `OCRD.CardType = cCustomer` |
| `companyName` | Yes | `BP.jsMaster.name` | `OCRD.CardName` |
| `foreignName` | No | `BP.jsMaster.foreignName` | `OCRD.CardForeignName` |
| `typeOfBusiness` | No | `BP.jsMaster.typeOfBusiness` | `OCRD.U_TypeOfBusiness` |
| `industry` | No | `BP.jsMaster.industry` | `OCRD.U_Industry` |
| `firstName` | Recommended | `BP.jsContactPersons.firstName` | `OCPR.FirstName` |
| `lastName` | No | `BP.jsContactPersons.lastName` | `OCPR.LastName` |
| `designation` / `title` | No | `BP.jsContactPersons.designation` | `OCPR.Position` |
| `mobileNumber` | Recommended | `BP.jsMaster.mobileNo`, contact `mobileNumber` | `OCRD.Phone1`, `OCPR.MobilePhone` |
| `emailAddress` | Recommended | `BP.jsContactPersons.emailAddress` | `OCRD.EmailAddress`, `OCPR.E_Mail` |
| `alternateEmail` | No | `BP.jsContactPersons.alternateEmail` | Stored in portal; not sent to standard SAP field |
| `billingAddresses` | Yes | `BP.jsMasterAddress.addressType = B` | `CRD1.AddressType = bo_BillTo` |
| `shippingAddresses` | No | `BP.jsMasterAddress.addressType = S` | `CRD1.AddressType = bo_ShipTo` |
| `gstin` | Conditional | `BP.jsTaxDetails.gstin`, address `gstNo` | `CRD1.GSTIN` |
| `panNumber` | Yes | `BP.jsTaxDetails.panNo` | `BPFiscalTaxIDCollection.TaxId0` |
| `currency` | Yes | `BP.jsMaster.currency` | `OCRD.Currency` |
| `msme` | No | `BP.jsTaxDetails.msmeNo` | `OCRD.U_MSME` |
| `remarks` | No | `BP.jsMaster.remarks` | `OCRD.Notes` |
| `attachments` | No | `BP.jsAttachments` | `Attachments2`, then `OCRD.AttachmentEntry` |
| `isStaff` | Yes | `BP.jsMaster.isStaff` | Portal/workflow flag only |

### Vendor

| Frontend field | Required | DB column/source | SAP mapping |
|---|---:|---|---|
| `vendorType` | Yes | `BP.jsMaster.type = V` | `OCRD.CardType = cSupplier` |
| `companyName` | Yes | `BP.jsMaster.name` | `OCRD.CardName` |
| `foreignTradeName` | No | `BP.jsMaster.foreignName` | `OCRD.CardForeignName` |
| `industrySector` | No | `BP.jsMaster.industry` | `OCRD.U_Industry` |
| `firstName` | Recommended | `BP.jsContactPersons.firstName` | `OCPR.FirstName` |
| `lastName` | No | `BP.jsContactPersons.lastName` | `OCPR.LastName` |
| `designation` | No | `BP.jsContactPersons.designation` | `OCPR.Position` |
| `mobile` | Recommended | `BP.jsMaster.mobileNo`, contact `mobileNumber` | `OCRD.Phone1`, `OCPR.MobilePhone` |
| `alternateContact` | No | `BP.jsContactPersons.alternateContact` | `OCPR.Phone1` |
| `gstin` | Conditional | `BP.jsTaxDetails.gstin`, address `gstNo` | `CRD1.GSTIN` |
| `pan` | Yes | `BP.jsTaxDetails.panNo` | `BPFiscalTaxIDCollection.TaxId0` |
| `tan` | No | `BP.jsTaxDetails.buyerTANNo` | `OCRD.U_TAN` |
| `currency` | Yes | `BP.jsMaster.currency` | `OCRD.Currency` |
| `msme` | No | `BP.jsTaxDetails.msmeNo` | `OCRD.U_MSME` |
| `fssaiLicense` | No | `BP.jsTaxDetails.fssaiNo` | `OCRD.U_Fssai` |
| `bankName` | Vendor recommended | `BP.jsBankDetails.name` | `OCRB.BankCode` |
| `branchName` | No | `BP.jsBankDetails.branch` | `OCRB.Branch` |
| `accountNumber` | Vendor recommended | `BP.jsBankDetails.accountNo` | `OCRB.AccountNo` |
| `ifscCode` | Vendor recommended | `BP.jsBankDetails.ifscCode` | `OCRB.BICSwiftCode`, `OCRB.UserNo1` |
| `swiftCode` | No | `BP.jsBankDetails.swiftCode` | `OCRB.IBAN` |
| `accountType` | No | `BP.jsBankDetails.accountType` | Stored in portal |
| `remarks` | No | `BP.jsMaster.remarks` | `OCRD.Notes` |
| `address fields` | Yes | `BP.jsMasterAddress` | `CRD1` |
| `attachments` | No | `BP.jsAttachments` | `Attachments2`, then `OCRD.AttachmentEntry` |
| `isStaff` | Yes | `BP.jsMaster.isStaff` | Portal/workflow flag only |

## Address Rules

`billingAddresses` and `shippingAddresses` are separate API arrays. Each row maps to `BP.jsMasterAddress`:

| Frontend field | DB column | SAP field |
|---|---|---|
| `addressName` | `addressName` | `AddressName` |
| `street` | `addressLine1` | `Street` |
| `blockArea` | `addressLine2` | `Block` |
| `city` | `cityID` | `City` |
| `state` | `stateID` | `State` |
| `pinCode` | `pincode` | `ZipCode` |
| `country` | `countryID` | `Country` |
| `gstin` | `gstNo` | `GSTIN` |

If no shipping address is supplied, SAP posting reuses bill-to rows as ship-to rows.

## Attachment Rules

Create and update APIs remain multipart:

| Form key | Type | Rule |
|---|---|---|
| `requests` | text JSON | New BP payload |
| `files` | file list | Saved under `wwwroot/Uploads/BPmaster` |
| `fileTypes` | comma-separated text | Count must match uploaded file count |

Attachments are kept in `BP.jsAttachments` and posted to SAP `Attachments2` before `BusinessPartners`.

## API Payload Examples

Customer:

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
  "remarks": "Portal customer",
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

Vendor:

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
  "remarks": "Portal vendor",
  "isStaff": false,
  "userId": 107,
  "companyByUser": "Jivo Oil",
  "billingAddresses": [],
  "shippingAddresses": []
}
```

## Workflow

The cleanup does not change workflow tables or stage movement.

| Stage | Role | Behavior |
|---:|---|---|
| 1 | Manager | Verifies business need and basic BP data |
| 2 | Accounts | Verifies tax, address, bank, and attachments |
| 3 | SAP Team | Posts to SAP first, then completes final approval |

Final approval still follows SAP-first behavior:

1. `apiStatusTag = P`
2. Backend posts attachments and BP payload to SAP
3. On success, `apiStatusTag = Y`, `sapCardCode`, `sapAttachmentEntry`, and `payloadHash` are stored
4. Only then does `BP.jsApproveBP` mark the workflow approved
5. On SAP failure, `apiStatusTag = N`; the workflow remains pending at final stage

Retry remains `POST /api/BPmaster/RetrySapPost` and is allowed only when the final-stage SAP status is `N`.

## Validation Rules

| Rule | Location |
|---|---|
| BP type must be customer or vendor | .NET normalization and SQL procedure |
| `companyId` is required | SQL procedure |
| `companyName` is required | SQL procedure |
| `panNumber` / `pan` is required | SQL procedure |
| GSTIN is sent to SAP only when it matches Indian GST format | `BPMasterSapService` |
| PAN is sent to SAP fiscal tax only when it matches PAN format | `BPMasterSapService` |
| Attachment file count must match `fileTypes` count | Controller |
| Final approval cannot complete until SAP status is success | `BP.jsApproveBP` |

No FluentValidation validators or hand-written Swagger examples existed in this project for BP Master. The OpenAPI contract is generated from the updated DTOs.
