/*
 * [bud].[UpdateMonthlyAllocations] — DOES NOT EXIST in jsaplive3. Checked 2026-09-24.
 *
 * Two lookups returned no such object:
 *   1. By exact name in schema bud, alongside the other 17 audited objects
 *      (asked for twice; never returned).
 *   2. By pattern, any schema: sys.objects WHERE name LIKE '%MonthlyAlloc%'.
 *      Everything that pattern DID find, as returned:
 *
 *   schema  object_name                                              type_desc                 created                  modified
 *   sys     TT_MonthlyAllocationTableType_57899CD7                   TYPE_TABLE                2025-12-27 14:57:35.987  2025-12-27 14:57:35.987
 *   bud     BudgetMonthlyAllocations                                 USER_TABLE                2025-12-23 11:46:23.703  2025-12-23 11:46:26.183
 *   bud     CreateMonthlyAllocations                                 SQL_STORED_PROCEDURE      2025-12-27 14:58:24.260  2025-12-27 14:58:24.260
 *   bud     FK_bud_BudgetAllocationRequest_BudgetMonthlyAllocations  FOREIGN_KEY_CONSTRAINT    2025-12-23 11:46:26.127  2025-12-23 11:46:26.127
 *   bud     FK_BudgetMonthlyAllocations_Budgets                      FOREIGN_KEY_CONSTRAINT    2025-12-23 11:46:26.183  2025-12-23 11:46:26.183
 *   bud     FK_SubBudgetMonthlyAllocations_SubBudgets                FOREIGN_KEY_CONSTRAINT    2025-12-23 11:46:26.300  2025-12-23 11:46:26.300
 *   bud     GetBudgetMonthlyAllocationView                           SQL_STORED_PROCEDURE      2025-12-23 11:46:26.627  2026-03-18 17:17:29.110
 *   bud     GetSubBudgetsWithMonthlyAllocation                       SQL_STORED_PROCEDURE      2025-12-23 11:46:26.787  2025-12-23 11:46:26.787
 *   bud     jsGetBudgetInsightMonthlyAllocation                      SQL_STORED_PROCEDURE      2025-12-23 11:46:29.733  2026-06-09 14:36:54.627
 *   bud     SubBudgetMonthlyAllocations                              USER_TABLE                2025-12-23 11:46:25.307  2025-12-23 11:46:26.300
 *   bud     UQ_BudgetMonthlyAllocations_BudgetMonth                  UNIQUE_CONSTRAINT         2025-12-23 11:46:23.707  2025-12-23 11:46:23.707
 *   bud     UQ_SubBudgetMonthlyAllocations_SubBudgetMonth            UNIQUE_CONSTRAINT         2025-12-23 11:46:25.310  2025-12-23 11:46:25.310
 *
 * CONSEQUENCE. JSAP's Auth2Service.UpdateMonthlyAllocationsAsync calls
 * "bud.UpdateMonthlyAllocations" (route POST /api/auth2/UpdateMonthlyAllocations).
 * Against jsaplive3 that call can only fail with "Could not find stored
 * procedure", which the C# catches and returns as Success = false.
 *
 * Not checked: whether it exists in the other databases on the server
 * (JSAPNew, jsap). The pattern lookup above ran in jsaplive3 only.
 */
