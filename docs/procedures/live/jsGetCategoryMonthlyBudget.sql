/*
 * [bud].[jsGetCategoryMonthlyBudget] — LIVE definition, supplied 2026-09-24.
 * Created 2025-04-02 12:38:41.803 · Modified 2025-06-05 12:06:52.713
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the first `--` comment would comment out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 *
 * The trailing `--ALTER PROCEDURE ...` block is part of the stored definition:
 * an older version kept as comments inside the live object.
 */
  CREATE PROCEDURE [bud].[jsGetCategoryMonthlyBudget]      @budgetCategory VARCHAR(100),   @subBudget VARCHAR(124),      @month VARCHAR(20),      @company INT  AS  BEGIN      SET NOCOUNT ON;        SELECT           id,          budget,          [month],          company,          totalAmount,          usedAmount,          rejectedAmount,          timeStamp      FROM bud.jsBudgetCategoryMonthSummary      WHERE budget = @budgetCategory         AND [month] = @month         AND company = @company     AND subBudget = @subBudget  END;    --ALTER PROCEDURE [bud].[jsGetCategoryMonthlyBudget]  --    @budgetCategory VARCHAR(100),  --    @month VARCHAR(20),  --    @company INT  --AS  --BEGIN  --    SET NOCOUNT ON;  --  --    SELECT   --        id,  --        budget,  --        [month],  --        company,  --        totalAmount,  --        usedAmount,  --        rejectedAmount,  --        timeStamp  --    FROM bud.jsBudgetCategoryMonthSummary  --    WHERE budget = @budgetCategory   --      AND [month] = @month   --      AND company = @company;  --END;
