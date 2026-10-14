/* =====================================================================
   CHECK - is the NEW budget workflow actually catching entries?
   Read-only. Run any time after BUDGET_Workflow_Cutover.sql.

   Budget lines reach JSAP through the SQL Agent job "Budget Sync Data",
   which runs every 5 minutes. So allow at least one cycle - and some
   real SAP activity - before expecting anything here.
   ===================================================================== */

USE jsaplive3;
GO

DECLARE @cut DATETIME = (SELECT MIN(backedUpOn) FROM bud.bak_BudgetCutover_20261014);

/* ---------------------------------------------------------------------
   1. WHEN - how long has the new flow been live?
   --------------------------------------------------------------------- */
SELECT @cut AS cutoverTime, GETDATE() AS now_,
       DATEDIFF(minute, @cut, GETDATE()) AS minutesLive;

/* ---------------------------------------------------------------------
   2. IS THERE ANYTHING TO ROUTE? - budget lines that arrived from SAP
      since the cutover. If this is 0, nothing has come in yet and an
      empty result below means nothing is wrong.
   --------------------------------------------------------------------- */
SELECT COUNT(*) AS budgetLinesSinceCutover,
       MAX(budgetDate) AS lastLineArrived
  FROM bud.jsBudgetTable
 WHERE budgetDate >= @cut;

/* ---------------------------------------------------------------------
   3. NEW vs OLD - entries raised since the cutover.
      "OLD flow" must stay 0. Anything there means an old template is
      still catching work and the cutover did not take.
   --------------------------------------------------------------------- */
WITH x AS (
    SELECT CASE WHEN e.templateId IN (SELECT templateId FROM bud.bak_BudgetCutover_20261014)
                THEN 'OLD flow (must be 0)' ELSE 'NEW flow' END AS flow,
           e.createdOn
      FROM bud.jsDocEntry e
     WHERE e.createdOn >= CAST(@cut AS DATE))
SELECT flow, COUNT(*) AS entries, MAX(createdOn) AS latest
  FROM x GROUP BY flow;

/* ---------------------------------------------------------------------
   4. WHICH new template, and who it went to.
   --------------------------------------------------------------------- */
SELECT t.name AS template,
       COUNT(*) AS entries,
       MAX(e.createdOn) AS latest,
       STUFF((SELECT ' > ' + u.firstName + ' ' + ISNULL(u.lastName,'')
                FROM jsStageTemplate st
                JOIN jsUserStage us ON us.stageId = st.stageId
                JOIN jsUser u ON u.userId = us.userId
               WHERE st.templateId = t.id
               ORDER BY st.priority FOR XML PATH('')), 1, 3, '') AS approvers
  FROM bud.jsDocEntry e
  JOIN jsTemplate t ON t.id = e.templateId
 WHERE e.createdOn >= CAST(@cut AS DATE)
   AND t.name LIKE 'bw %'
 GROUP BY t.id, t.name
 ORDER BY entries DESC;

/* ---------------------------------------------------------------------
   5. THE REAL FAILURE MODE - a budget line arrived but no template
      picked it up, so nobody will ever approve it.
      MUST return 0 rows. If it does not, send me the output.
   --------------------------------------------------------------------- */
SELECT b.Branch, b.BUDGET, b.SUB_BUDGET, b.AcctCode, b.ObjType,
       COUNT(*) AS orphanLines, MAX(b.budgetDate) AS latest
  FROM bud.jsBudgetTable b
 WHERE b.budgetDate >= @cut
   AND b.ProcesStat = 'Y'
   AND NOT EXISTS (SELECT 1 FROM bud.jsDocEntry e WHERE e.docEntry = b.DocEntry)
 GROUP BY b.Branch, b.BUDGET, b.SUB_BUDGET, b.AcctCode, b.ObjType
 ORDER BY orphanLines DESC;

/* ---------------------------------------------------------------------
   6. The old backlog - these stay with their original approvers and
      finish on the old templates. The number should only ever fall.
   --------------------------------------------------------------------- */
SELECT COUNT(*) AS oldPendingStillOpen
  FROM bud.jsDocEntry
 WHERE status = 'P'
   AND templateId IN (SELECT templateId FROM bud.bak_BudgetCutover_20261014);
GO
