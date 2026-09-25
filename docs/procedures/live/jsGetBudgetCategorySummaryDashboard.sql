/*
 * [bud].[jsGetBudgetCategorySummaryDashboard] — LIVE definition, supplied 2026-09-24.
 * Created 2025-04-02 12:38:39.230 · Modified 2025-04-02 12:38:39.230
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the first `--` comment would comment out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 */
  CREATE PROCEDURE [bud].[jsGetBudgetCategorySummaryDashboard]      @month VARCHAR(20),      @company INT  AS  BEGIN      SET NOCOUNT ON;        SELECT           budget,          totalAmount,          ISNULL(usedAmount, 0) AS usedAmount,          ISNULL(rejectedAmount, 0) AS rejectedAmount,          (totalAmount - ISNULL(usedAmount, 0) - ISNULL(rejectedAmount, 0)) AS remaining,          CAST(              (ISNULL(usedAmount, 0) * 100.0) /               NULLIF(totalAmount, 0)              AS DECIMAL(5,2)          ) AS usagePercent      FROM bud.jsBudgetCategoryMonthSummary      WHERE [month] = @month AND company = @company      ORDER BY budget;  END;
