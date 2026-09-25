/*
 * [bud].[jsBudgetTable_Dedup] — LIVE definition, supplied 2026-09-24.
 * VIEW. Created 2026-09-14 17:55:53.430 · Modified 2026-09-14 17:55:53.430
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim.
 *
 * Created 27 seconds before the 2026-09-14 patch of jsExecuteBudgetQueries
 * (modified 17:56:20.583), which was a fix for duplicated rows. Nothing in
 * JSAPNEW source mentions this view; which objects read it is not known.
 */
  CREATE VIEW bud.jsBudgetTable_Dedup  AS  SELECT *  FROM (      SELECT *,             ROW_NUMBER() OVER (                 PARTITION BY DocEntry, ObjType, Branch, LineNum, VisOrder, CURRENTMONTH                 ORDER BY budgetDate DESC             ) AS _dedup_rn      FROM bud.jsBudgetTable  ) x  WHERE x._dedup_rn = 1;
