/*
 * [bud].[jsGetBudgetDetailById] — LIVE definition, supplied 2026-09-24.
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the first `--` comment would comment out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 */
CREATE PROCEDURE [bud].[jsGetBudgetDetailById]      @budgetId INT = 13946  AS  BEGIN      SET NOCOUNT ON;        -- Step 1: Get basic header info from jsDocEntry      SELECT           jd.id AS BudgetId,          jd.docEntry,          jd.templateId,          jd.totalStage,          jd.currentStageId,          jd.currentSatge,          jd.status AS CurrentStatus      FROM bud.jsDocEntry jd      WHERE jd.id = @budgetId;        -- Step 2: Get all line-level detail (objType, company, lineNum, visOrder)      SELECT           d.objType,          d.company,          --l.allocationId as budgetallocationId,          d.lineNum,          d.visOrder,    b.objectName,          b.AcctCode,          b.AcctName,          b.CardCode,          b.CardName,          b.AMOUNT,          b.DocDate,    b.EFFECTMONTH,          b.Budget_Owner AS BudgetOwner,          b.Current_month_Budget,          b.Current_month_Posted_Amount,          b.LineRemarks,          b.STATE,          b.BUDGET,          b.SUB_BUDGET AS subBudget,    b.OcrCode AS variety,    b.Comments      FROM bud.jsDocEnrtyDetail d      INNER JOIN bud.jsDocEntry jd ON jd.id = d.docId      INNER JOIN bud.jsBudgetTable b           ON b.DocEntry = jd.docEntry           AND b.ObjType = d.objType           AND b.Branch = d.company           AND b.LineNum = d.lineNum           AND b.VisOrder = d.visOrder      --LEFT JOIN bud.budgets bu ON b.BUDGET = bu.budgetName      --LEFT JOIN BUD.BudgetMonthlyAllocations l ON l.budgetId = bu.budgetId      WHERE jd.id = @budgetId;    END;
