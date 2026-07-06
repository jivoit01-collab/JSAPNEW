# problems that sometimes occur and solution

## 1. PRDO planned production order exists but does not show in pending/workflow

### Problem

The entry exists in:

```sql
SELECT *
FROM PRDO.PlannedProductionOrders
WHERE DocEntry = 11954;
```

But it does not show in pending approval or workflow.

Important: `PRDO.jsProductionOrderStatusWorkflow` is not the pending table. It stores approve/reject action history only after a user takes action.

Pending starts from:

```text
PRDO.jsDocEntry
PRDO.jsDocEntryDetail
```

### Check

```sql
SELECT *
FROM PRDO.jsDocEntry
WHERE docEntry IN (11953,11954,11957,11960);

SELECT *
FROM PRDO.jsDocEntryDetail
WHERE docId IN (
    SELECT id
    FROM PRDO.jsDocEntry
    WHERE docEntry IN (11953,11954,11957,11960)
);

SELECT *
FROM PRDO.jsProductionOrderStatusWorkflow
WHERE docId IN (
    SELECT id
    FROM PRDO.jsDocEntry
    WHERE docEntry IN (11953,11954,11957,11960)
);
```

If `PRDO.PlannedProductionOrders` has rows but `PRDO.jsDocEntry` has no rows, the workflow generation procedure did not run.

### Solution

Run the workflow generation procedure:

```sql
EXEC PRDO.jsProcessAllUsersProductionOrderApprovals @company = 1;
```

Then check pending by correct approver:

```sql
EXEC PRDO.jsGetPendingProductionOrders
    @userId = 101,
    @company = 1,
    @month = '06-2026';

EXEC PRDO.jsGetPendingProductionOrders
    @userId = 136,
    @company = 1,
    @month = '06-2026';
```

If the user approves/rejects, then action history will appear:

```sql
SELECT *
FROM PRDO.jsProductionOrderStatusWorkflow
WHERE docId = 4195;
```

Note: workflow `docId` is `PRDO.jsDocEntry.id`, not SAP `DocEntry`.

## 2. Budget approved in JSAP but SAP says Budget Expense not Approved

### Problem

In SQL Server the budget is approved:

```sql
SELECT *
FROM bud.jsDocEntry
WHERE docEntry = 13773;
```

Example:

```text
id = 63
docEntry = 13773
status = A
```

But in SAP Beverage Unit, the A/P Invoice draft shows:

```text
Budget Expense not Approved
```

Cause found: same `DocEntry` can exist in multiple SAP company databases. In this case, JSAP approval synced to `JIVO_OIL_HANADB`, but the SAP user was working in `JIVO_BEVERAGES_HANADB`.

### Check SQL mapping

```sql
SELECT
    e.id,
    e.docEntry,
    e.status,
    e.templateId,
    e.currentStageId,
    e.currentSatge,
    d.objType,
    d.company AS DetailCompany,
    d.lineNum,
    d.visOrder,
    b.Branch,
    b.ObjectName,
    b.CardName,
    b.BUDGET,
    b.AMOUNT,
    b.CURRENTMONTH
FROM bud.jsDocEntry e
JOIN bud.jsDocEnrtyDetail d
    ON d.docId = e.id
LEFT JOIN bud.jsBudgetTable b
    ON b.DocEntry = e.docEntry
   AND b.LineNum = d.lineNum
   AND b.VisOrder = d.visOrder
WHERE e.docEntry = 13773
ORDER BY d.lineNum, d.visOrder;
```

### Check HANA target table

Budget approval updates this HANA table:

```text
tbl_Draft_Approvals
```

Not only:

```text
jsDocEntries
```

For Beverage:

```sql
SELECT "DocEntry", "ObjType", "LineNum", "VisOrder", "ApprovedStatus", "VerifiedStatus"
FROM "JIVO_BEVERAGES_HANADB"."tbl_Draft_Approvals"
WHERE "DocEntry" = 13773;
```

For Oil:

```sql
SELECT "DocEntry", "ObjType", "LineNum", "VisOrder", "ApprovedStatus", "VerifiedStatus"
FROM "JIVO_OIL_HANADB"."tbl_Draft_Approvals"
WHERE "DocEntry" = 13773;
```

### Manual solution for confirmed Beverage document

Use only after confirming the SAP screen is Beverage Unit and the document details match.

```sql
EXEC bud.jsSyncBudgetToHanaDraftApproval
    @docId = 63,
    @company = 2,
    @status = 'A',
    @userId = 79,
    @remarks = 'Manual BEVERAGE approval sync for SAP DocEntry 13773';
```

Then check:

```sql
SELECT "DocEntry", "ObjType", "LineNum", "VisOrder", "ApprovedStatus", "VerifiedStatus"
FROM "JIVO_BEVERAGES_HANADB"."tbl_Draft_Approvals"
WHERE "DocEntry" = 13773;
```

Expected:

```text
ApprovedStatus = A
```

### Permanent solution

Budget workflow should not identify documents only by `DocEntry`. It should use:

```text
Branch/company + DocEntry + ObjType + LineNum + VisOrder
```

This prevents Oil approval from updating Beverage or Beverage approval from updating Oil when both SAP companies have the same `DocEntry`.
