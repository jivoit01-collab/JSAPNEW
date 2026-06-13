# SAP B1 Item Creation via Service Layer — Complete Documentation

> **Source:** Queried directly from live SAP databases (JIVO_OIL_HANADB, JIVO_BEVERAGES_HANADB, JIVO_MART_HANADB)
> **API Endpoint:** `POST https://103.89.45.192:50000/b1s/v1/Items`
> **Date:** 2026-04-08

---

## 1. COMPANY & DATABASE OVERVIEW

| Company ID | Name | SAP Database (Live) | SAP Database (Test) | Login |
|:----------:|------|---------------------|---------------------|-------|
| 1 | Oil | JIVO_OIL_HANADB | TEST_OIL_15122025 | B1i / 1234 |
| 2 | Beverages | JIVO_BEVERAGES_HANADB | TEST_BEVERAGES_15122025 | B1i / 1234 |
| 3 | Mart | JIVO_MART_HANADB | TEST_MART_15122025 | B1i / 1234 |

**SAP Base URL:** `https://103.89.45.192:50000/b1s/v1/`

---

## 2. ITEM GROUP CODES PER COMPANY

| GroupCode | Group Name | OIL (1) | BEV (2) | MART (3) | Series (OIL) | Series (BEV) | Series (MART) |
|:---------:|-----------|:-------:|:-------:|:--------:|:------------:|:------------:|:-------------:|
| 101 | CONSUMABLES | - | YES | YES | - | 395 | 395 |
| 102 | FINISHED | YES | YES | YES | 389 | 389 | 389 |
| 103 | FLAV/PRESTV/INGRDNT | - | - | YES (empty) | - | - | - |
| 104 | LAB INVENTORY | - | - | YES (empty) | - | - | - |
| 105 | PACKAGING MATERIAL | YES | YES | YES | 391 | 391 | 391 |
| 106 | RAW MATERIAL | YES | YES | YES | 392 | 392 | 392 |
| 107 | TRADING ITEMS | YES | YES | YES | 393 | 393 | 393 |
| 109 | SALES BOM | YES | YES | YES | 394 | 394 | 394 |
| 110 | FIXED ASSETS | - | YES | YES | - | 390 | 390 |
| 111 | CONSUMABLES (OIL) / LAB APPARATUS (BEV) | YES | YES | - | 395 | 822 | - |
| 112 | FIXED ASSETS (OIL) / FA CONSUMABLES (BEV) | YES | YES | - | 390 | 824 | - |
| 113 | CONSUMABLES WITH INVENTORY | YES | - | - | 820 | - | - |
| 114 | FA CONSUMABLES (OIL) / CONS WITH INV (BEV) | YES | YES | - | 821 | 2367 | - |
| 115 | SEMI FINISHED GOODS | YES | - | - | 2364 | - | - |

---

## 3. STANDARD FIELDS BY GROUP CODE — COMPANY 1 (OIL)

| GroupCode | Group Name | IssueMethod | CostAccountingMethod | ManageBatchNumbers | ManageSerialNumbers | WTLiable | SalesItem | PurchaseItem | InventoryItem | TreeType |
|:---------:|-----------|:-----------:|:--------------------:|:------------------:|:-------------------:|:--------:|:---------:|:------------:|:-------------:|:--------:|
| 102 | FINISHED | im_Manual | bis_SNB | tYES | tNO | **tYES** | tYES | tYES | tYES | iProductionTree |
| 105 | PACKAGING MATERIAL | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tYES | tYES | iNotATree |
| 106 | RAW MATERIAL | im_Manual | bis_SNB | tYES | tNO | tNO | tYES | tYES | tYES | iNotATree |
| 107 | TRADING ITEMS | im_Manual | bis_FIFO | tYES | tNO | tNO | tYES | tYES | tYES | iProductionTree |
| 109 | SALES BOM | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tNO | tNO | iSalesTree |
| 111 | CONSUMABLES | im_Backflush | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tNO | iNotATree |
| 112 | FIXED ASSETS | im_Manual | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tYES | iNotATree |
| 113 | CONS WITH INVENTORY | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tYES | tYES | iNotATree |
| 114 | FA CONSUMABLES | im_Backflush | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tNO | iNotATree |
| 115 | SEMI FINISHED GOODS | im_Manual | bis_SNB | tYES | tNO | tNO | tYES | tYES | tYES | iNotATree |

---

## 4. STANDARD FIELDS BY GROUP CODE — COMPANY 2 (BEVERAGES)

| GroupCode | Group Name | IssueMethod | CostAccountingMethod | ManageBatchNumbers | ManageSerialNumbers | WTLiable | SalesItem | PurchaseItem | InventoryItem | TreeType |
|:---------:|-----------|:-----------:|:--------------------:|:------------------:|:-------------------:|:--------:|:---------:|:------------:|:-------------:|:--------:|
| 101 | CONSUMABLES | im_Backflush | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tNO | iNotATree |
| 102 | FINISHED | im_Manual | bis_FIFO | tYES | tNO | **tYES** | tYES | tYES | tYES | iProductionTree |
| 105 | PACKAGING MATERIAL | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tYES | tYES | iNotATree |
| 106 | RAW MATERIAL | im_Manual | bis_FIFO | tYES | tNO | tNO | tYES | tYES | tYES | iProductionTree |
| 107 | TRADING ITEMS | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tYES | tYES | iNotATree |
| 109 | SALES BOM | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tNO | tNO | iSalesTree |
| 110 | FIXED ASSETS | im_Manual | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tYES | iNotATree |
| 111 | LAB APPARATUS | im_Backflush | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tNO | iNotATree |
| 112 | FA CONSUMABLES | im_Backflush | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tNO | iNotATree |
| 114 | CONS WITH INVENTORY | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tYES | tYES | iNotATree |

---

## 5. STANDARD FIELDS BY GROUP CODE — COMPANY 3 (MART)

| GroupCode | Group Name | IssueMethod | CostAccountingMethod | ManageBatchNumbers | ManageSerialNumbers | WTLiable | SalesItem | PurchaseItem | InventoryItem | TreeType |
|:---------:|-----------|:-----------:|:--------------------:|:------------------:|:-------------------:|:--------:|:---------:|:------------:|:-------------:|:--------:|
| 101 | CONSUMABLES | im_Backflush | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tNO | iNotATree |
| 102 | FINISHED | im_Manual | bis_SNB | tYES | tNO | **tNO** | tYES | tYES | tYES | iProductionTree |
| 105 | PACKAGING MATERIAL | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tYES | tYES | iNotATree |
| 106 | RAW MATERIAL | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tYES | tYES | iNotATree |
| 107 | TRADING ITEMS | im_Manual | bis_FIFO | tNO | tNO | tNO | tYES | tYES | tYES | iNotATree |
| 109 | SALES BOM | im_Backflush | bis_FIFO | tNO | tNO | tNO | tYES | tNO | tNO | iSalesTree |
| 110 | FIXED ASSETS | im_Manual | bis_FIFO | tNO | tNO | tNO | tNO | tYES | tYES | iNotATree |

---


## 6. FIELDS CONSTANT ACROSS ALL COMPANIES & ALL GROUPS

These fields have the same value regardless of company or group code:

| Field | Value | Notes |
|-------|-------|-------|
| GLMethod | `glm_WH` | Always warehouse-level |
| TaxType | `tt_Yes` | Always taxable |
| GSTRelevnt | `tYES` | Always GST relevant |
| GSTTaxCategory | `gtc_Regular` | Always regular |
| ForceSelectionOfSerialNumber | `tYES` | Always forced |
| SRIAndBatchManageMethod | `bomm_OnEveryTransaction` | Always on every transaction |
| ManageSerialNumbers | `tNO` | Never serial managed |
| VatLiable | `tYES` | Always VAT liable |
| UoMGroupEntry | `-1` | Manual (no UoM group) in samples |

---

## 7. USER DEFINED FIELDS (UDFs) — COMPANY-WISE AVAILABILITY

### 7.1 Common UDFs (ALL 3 Companies)

| UDF Field | Type | Description | Sample Values |
|-----------|------|-------------|---------------|
| `U_Brand` | string | Brand name | "JIVO" |
| `U_Unit` | string | Business unit | "OIL", "BEVERAGES", "TRADING", "CONSUMABLES", "FOODS" |
| `U_Variety` | string | Variety | "REFINED", "EXTRA VIRGIN", "COLD PRESS", "DRINKS" |
| `U_Sub_Group` | string | Sub group | "SOYABEAN", "OLIVE", "CANOLA", "DRINKS", "CARTON" |
| `U_SKU` | string | SKU size | "15 LTR", "200 MLS", "10 MLS", "1 KGS" |
| `U_IsLitre` | string | Is litre based | "Y" / "N" |
| `U_Gross_Weight` | decimal | Gross weight | 0.01, 5.1, 18.0 |
| `U_MRP` | int | Maximum retail price | 0, 50, 500, 3050 |
| `U_PACK_TYPE` | string | Pack type | "CONSUMER PACK", "BULK PACK" |
| `U_TYPE` | string | Item type classification | "PREMIUM" / "COMMODITY" |
| `U_Tax_Rate` | string | Tax rate | "0", "5", "12", "18" |
| `U_Rev_tax_Rate` | string | Revenue tax rate | "5", "18" |
| `U_JRID` | string | JR ID | |
| `U_Index_No` | string | Index number | "91815T" |
| `U_UNE_TOTB` | decimal | UNE total B | 0.0 |
| `U_UNE_TOTL` | decimal | UNE total L | 0.0 |
| `U_UTL_ST_ISSERVICE` | string | Is service flag | "N" |

### 7.2 Company-Specific UDFs

| UDF Field | OIL (1) | BEV (2) | MART (3) | Used on Groups | Sample Values |
|-----------|:-------:|:-------:|:--------:|----------------|---------------|
| **`U_FA_Type`** (mixed case) | **YES** | **YES** | **NO** | 112 (OIL), 110 (BEV) — Fixed Assets | "MOVABLE" |
| **`U_FA_TYPE`** (ALL CAPS) | **NO** | **NO** | **YES** | 110 (MART) — Fixed Assets | "MOVABLE" |
| **`U_Packing_Type`** | **YES** | **YES** | **NO** | 102, 107, 115 (OIL) | "TIN", "GLASS BOTTLE" |
| `U_GL_ACCT` | YES | YES | NO | 112, 114 (OIL), 110 (BEV) — FA groups | "1205006", "1204003" |
| `U_ITEM_LOCK` | YES | NO | NO | All groups (OIL) | "N" |
| `U_Mart_ItemCode` | YES | NO | NO | 107 (OIL) | "SC0000079" |
| `U_WG_ItemCode` | YES | NO | NO | - | |
| `U_CONSUMPTION_PER_DAY` | YES | NO | NO | - | |
| `U_Is_Plastic` | YES | NO | NO | - | |
| `U_P_WEIGHT` | YES | NO | NO | - | |
| `U_Is_CSD` | YES | YES | NO | 102 (BEV) | "Y" |
| `U_Shelflife` | YES (lowercase 'l') | NO | YES (`U_ShelfLife` capital 'L') | - | |
| `U_OIL_ItemCode` | NO | YES | NO | 105 (BEV) | "PM0000074" |
| `U_Oil_ItemCode` | NO | NO | YES | 102, 107 (MART) | "FG0000001", "SC0000088" |
| `U_RECIPE_NO` | NO | YES | NO | - | |

---

## 8. BUSINESS RULES & CONDITIONS

### 8.1 IssueMethod Rule

| Condition | Value | Applied In |
|-----------|-------|------------|
| GroupCode = 109 (SALES BOM) | `im_Backflush` | All 3 companies |
| GroupCode = 111 (CONSUMABLES/LAB) | `im_Backflush` | OIL, BEV |
| GroupCode = 114 (FA CONS/CONS W/INV) | `im_Backflush` | OIL, BEV |
| GroupCode = 105 (PACKAGING) | `im_Backflush` | All 3 companies |
| GroupCode = 101 (CONSUMABLES) | `im_Backflush` | BEV, MART |
| GroupCode = 112 (FA CONS in BEV) | `im_Backflush` | BEV |
| GroupCode = 113 (CONS W/ INV in OIL) | `im_Backflush` | OIL |
| GroupCode = 106 (RAW MATERIAL in MART) | `im_Backflush` | MART only |
| GroupCode = 107 (TRADING in BEV) | `im_Backflush` | BEV only |
| All other cases | `im_Manual` | - |

**Summary pattern:** Backflush is for items consumed automatically (consumables, packaging, BOM components). Manual is for items tracked individually (finished goods, raw materials, trading, fixed assets).

### 8.2 CostAccountingMethod Rule

| Condition | Value | Applied In |
|-----------|-------|------------|
| ManageBatchNumbers = tYES | `bis_SNB` | OIL (102, 106, 115), MART (102) |
| ManageBatchNumbers = tNO | `bis_FIFO` | All other groups in all companies |
| BEV — ALL groups | `bis_FIFO` | Even BEV Group 102 with Batch=tYES uses FIFO |

**Note:** BEV is the exception — it always uses `bis_FIFO` even for batch-managed items.

### 8.3 WTLiable (Withholding Tax) Rule

| Company | Group 102 (FINISHED) | All Other Groups |
|---------|:-------------------:|:----------------:|
| OIL (1) | **tYES** | tNO |
| BEV (2) | **tYES** | tNO |
| MART (3) | **tNO** | tNO |

**Rule:** WTLiable = tYES only for FINISHED (102) in OIL and BEV. MART never has WTLiable.

### 8.4 ManageBatchNumbers Rule

| GroupCode | OIL | BEV | MART |
|:---------:|:---:|:---:|:----:|
| 102 (FINISHED) | tYES | tYES | tYES |
| 106 (RAW MATERIAL) | tYES | tYES | tNO |
| 107 (TRADING) | tYES | tNO | tNO |
| 115 (SEMI FINISHED) | tYES | - | - |
| All others | tNO | tNO | tNO |

### 8.5 SalesItem / PurchaseItem / InventoryItem Rules

| GroupCode | Group Name | SalesItem | PurchaseItem | InventoryItem |
|:---------:|-----------|:---------:|:------------:|:-------------:|
| 102 | FINISHED | tYES | tYES | tYES |
| 105 | PACKAGING MATERIAL | tYES | tYES | tYES |
| 106 | RAW MATERIAL | tYES | tYES | tYES |
| 107 | TRADING ITEMS | tYES | tYES | tYES |
| 109 | SALES BOM | tYES | **tNO** | **tNO** |
| 101/111 | CONSUMABLES/LAB | **tNO** | tYES | **tNO** |
| 110/112 | FIXED ASSETS/FA | **tNO** | tYES | tYES |
| 113/114 | CONS W/ INV | Varies | tYES | Varies |
| 115 | SEMI FINISHED | tYES | tYES | tYES |

### 8.6 TreeType Rule

| TreeType | Group Codes | Description |
|----------|-------------|-------------|
| `iProductionTree` | 102 (all), 106 (BEV), 107 (OIL) | Items with BOM/production |
| `iSalesTree` | 109 (all) | Sales BOM items |
| `iNotATree` | All others | No BOM structure |

### 8.7 U_TYPE Rule

| Condition | Value |
|-----------|-------|
| IsLitre = "N" AND Variety in (CANOLA, OLIVE, GROUNDNUT) | `PREMIUM` |
| IsLitre = "Y" AND Variety in (EXTRA VIRGIN, POMACE, EXTRA LIGHT) | `PREMIUM` |
| Everything else | `COMMODITY` |

### 8.8 U_FA_Type / U_FA_TYPE Rule

| Company | Field Name | Used On | Values |
|---------|-----------|---------|--------|
| OIL (1) | `U_FA_Type` | Group 112 (FIXED ASSETS) | "MOVABLE", etc. |
| BEV (2) | `U_FA_Type` | Group 110 (FIXED ASSETS) | "MOVABLE", etc. |
| MART (3) | `U_FA_TYPE` | Group 110 (FIXED ASSETS) | "MOVABLE", etc. |

**IMPORTANT:** The casing is different between companies. OIL & BEV use `U_FA_Type`, MART uses `U_FA_TYPE`.

### 8.9 U_Packing_Type Rule

| Company | Exists | Used On Groups |
|---------|:------:|----------------|
| OIL (1) | YES | 102 (FINISHED), 107 (TRADING), 115 (SEMI FINISHED) |
| BEV (2) | YES | Field exists but typically null |
| MART (3) | **NO** | Field does NOT exist in MART database |

---

## 9. MART AUTO-SYNC RULE

When an item is created in Company 1 (OIL) or Company 2 (BEV), it is automatically synced to Company 3 (MART) if:

1. Primary item creation was **successful**
2. Item is a **Finished Good**: GroupCode = 102 OR GroupName contains "FINISHED" / "FG"
3. Company is NOT already 3

**MART sync payload differences:**
- `U_FA_Type` → NOT sent (null, field doesn't exist in MART)
- `U_FA_TYPE` → NOT sent (not applicable for synced items)
- `U_Packing_Type` → NOT sent (field doesn't exist in MART)
- `WTLiable` → Should be `tNO` (MART rule)
- All other common fields → Copied from primary item

---

## 10. COMPLETE JSON PAYLOAD STRUCTURE

### 10.1 Fields sent for ALL companies (common)

```json
{
  "ItemName": "string",
  "ItemsGroupCode": "int (102, 105, 106, etc.)",
  "Series": "int (389, 390, 391, etc. — per group)",
  "ChapterID": "int",

  "SalesItem": "tYES / tNO (based on group)",
  "PurchaseItem": "tYES / tNO (based on group)",
  "InventoryItem": "tYES / tNO (based on group)",

  "SalesUnit": "string (if SalesItem=tYES)",
  "SalesPackagingUnit": "string (if SalesItem=tYES)",
  "SalesQtyPerPackUnit": "decimal (if SalesItem=tYES)",
  "SalesFactor2": "decimal",
  "InventoryUOM": "string (if InventoryItem=tYES)",
  "PurchaseUnit": "string (if PurchaseItem=tYES)",
  "PurchasePackagingUnit": "string (if PurchaseItem=tYES)",

  "UoMGroupEntry": "int (-1 = Manual)",
  "IssueMethod": "im_Manual / im_Backflush (based on group)",
  "CostAccountingMethod": "bis_FIFO / bis_SNB (based on group & company)",
  "ManageBatchNumbers": "tYES / tNO (based on group)",
  "ManageSerialNumbers": "tNO",
  "ForceSelectionOfSerialNumber": "tYES",
  "SRIAndBatchManageMethod": "bomm_OnEveryTransaction",
  "WTLiable": "tYES / tNO (see WTLiable rule)",

  "GLMethod": "glm_WH",
  "TaxType": "tt_Yes",
  "GSTRelevnt": "tYES",
  "GSTTaxCategory": "gtc_Regular",

  "U_Tax_Rate": "string",
  "U_Rev_tax_Rate": "string",
  "U_Brand": "string",
  "U_Unit": "string",
  "U_Variety": "string",
  "U_Sub_Group": "string",
  "U_SKU": "string",
  "U_IsLitre": "Y / N",
  "U_Gross_Weight": "decimal",
  "U_MRP": "int",
  "U_PACK_TYPE": "string",
  "U_TYPE": "PREMIUM / COMMODITY"
}
```

### 10.2 Company 1 (OIL) — Additional fields

```json
{
  "U_Packing_Type": "string (TIN, GLASS BOTTLE, etc. — Groups 102, 107, 115)",
  "U_FA_Type": "string (MOVABLE, etc. — Group 112 only)"
}
```

### 10.3 Company 2 (BEV) — Additional fields

```json
{
  "U_Packing_Type": "string (field exists, used when applicable)",
  "U_FA_Type": "string (MOVABLE, etc. — Group 110 only)"
}
```

### 10.4 Company 3 (MART) — Additional fields

```json
{
  "U_FA_TYPE": "string (MOVABLE, etc. — Group 110 only, NOTE: ALL CAPS)"
}
```

**MART does NOT have:** `U_Packing_Type`, `U_FA_Type` (mixed case)

---

## 11. DIFFERENCES SUMMARY BETWEEN COMPANIES

| Aspect | OIL (1) | BEV (2) | MART (3) |
|--------|---------|---------|----------|
| WTLiable for Group 102 | tYES | tYES | **tNO** |
| CostAccountingMethod for Group 102 | bis_SNB | **bis_FIFO** | bis_SNB |
| CostAccountingMethod for Group 106 | bis_SNB | **bis_FIFO** | bis_FIFO |
| FA Type UDF name | U_FA_Type | U_FA_Type | **U_FA_TYPE** |
| U_Packing_Type exists | YES | YES | **NO** |
| U_GL_ACCT exists | YES | YES | **NO** |
| U_ITEM_LOCK exists | YES | NO | NO |
| ManageBatch for Group 106 | tYES | tYES | **tNO** |
| ManageBatch for Group 107 | tYES | **tNO** | tNO |
| IssueMethod for Group 106 | im_Manual | im_Manual | **im_Backflush** |
| IssueMethod for Group 107 | im_Manual | **im_Backflush** | im_Manual |
| Fixed Assets GroupCode | **112** | **110** | **110** |
| FA Consumables GroupCode | **114** | **112** | - |
| Cons with Inventory GroupCode | **113** | **114** | - |
| Semi Finished GroupCode | **115** | - | - |
