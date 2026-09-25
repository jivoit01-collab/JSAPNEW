/*
 * bud.jsBudgetTable_vg — NOT A VIEW. Supplied 2026-09-24.
 *
 * sys.objects reports:
 *     type_desc    USER_TABLE
 *     create_date  2025-12-23 17:57:10.947
 *     modify_date  2025-12-23 17:57:30.073
 *     definition   NULL   (tables have no module definition to script)
 *
 * The name ends in `_vg` and the audit brief lists it as a view, but it is a
 * physical table.
 *
 * ---------------------------------------------------------------------------
 * WHAT WRITES TO IT — nothing that could be found (checked 2026-09-24)
 * ---------------------------------------------------------------------------
 *   sys.sql_modules  : no procedure, view or function mentions it
 *   SQL Agent jobs   : no job step mentions it. The UNFILTERED job list
 *                      (all jobs, all databases, checked 2026-09-24) has no
 *                      job on a 4-hour or hh:04 schedule either, so the
 *                      writer is not a SQL Agent job on this instance.
 *   JSAP C#          : one reference, DashboardService.cs, READ-ONLY
 *
 * IT IS NEVERTHELESS REFRESHED — a writer exists outside all three.
 * Compared on 2026-09-24:
 *
 *   source            row_count  newest_createdate        newest_budgetdate
 *   jsBudgetTable_vg     208441  2026-09-23 00:00:00.000  2026-09-23 20:04:22.273
 *   jsBudgetTable         33056  2026-09-23 00:00:00.000  2026-09-23 17:30:03.940
 *
 * So it is loaded independently of bud.jsBudgetTable (later, on its own
 * schedule) and holds six times the rows.
 *
 * LOAD PATTERN — rows per load day, last 15 load days (2026-09-24):
 *
 *   load_day    rows  first_load               last_load
 *   2026-09-23   323  2026-09-23 12:04:34.860  2026-09-23 20:04:22.273
 *   2026-09-22   160  2026-09-22 00:04:20.617  2026-09-22 20:04:21.170
 *   2026-09-21   256  2026-09-21 12:04:26.640  2026-09-21 20:04:35.830
 *   2026-09-19   316  2026-09-19 12:04:35.633  2026-09-19 20:04:20.120
 *   2026-09-18   162  2026-09-18 12:04:31.960  2026-09-18 20:04:32.223
 *   2026-09-17   283  2026-09-17 12:04:27.080  2026-09-17 20:04:25.747
 *   2026-09-16   253  2026-09-16 00:04:17.277  2026-09-16 20:04:19.697
 *   2026-09-15   166  2026-09-15 12:04:41.513  2026-09-15 20:04:19.307
 *   2026-09-14   336  2026-09-14 12:04:40.423  2026-09-14 20:04:23.257
 *   2026-09-13    40  2026-09-13 16:04:18.587  2026-09-13 16:04:18.823
 *   2026-09-12   236  2026-09-12 12:04:26.937  2026-09-12 20:04:17.087
 *   2026-09-11   430  2026-09-11 12:04:44.650  2026-09-11 20:04:18.297
 *   2026-09-10   261  2026-09-10 00:04:29.427  2026-09-10 20:04:20.463
 *   2026-09-09   334  2026-09-09 00:04:21.190  2026-09-09 20:04:18.923
 *   2026-09-08   314  2026-09-08 12:04:30.860  2026-09-08 20:04:20.657
 *
 * Every load lands at hh:04, and only at 00, 12, 16 or 20 — a scheduled
 * process, consistent with one running every 4 hours. 40-430 rows a day is an
 * INCREMENTAL load of new lines, not a repeated full snapshot: the 208,441
 * rows are accumulated history, not duplicates. (No row was loaded on
 * 2026-09-20; whether the process ran and found nothing, or did not run, is
 * not visible from this table.) The writer is still unidentified:
 * candidates the checks above cannot see are a procedure in ANOTHER database
 * on the instance (sys.sql_modules is per-database), dynamic SQL that builds
 * the table name, an SSIS package, or a process outside SQL Server.
 *
 * ---------------------------------------------------------------------------
 * CONTENTS
 * ---------------------------------------------------------------------------
 *   row_count   208441   (bud.jsBudgetTable: 33056)
 *
 * Columns, exactly as sys.columns returned them. The same 39 names, in the
 * same order, as the insert list into bud.jsBudgetTable in
 * InsertBudgetFromHANA. (JSAP's DashboardService comment says the table has
 * 30 columns; sys.columns says 39.)
 *
 *   id  column_name                  sql_type   max_length  prec  scale  nullable
 *    1  Branch                       nvarchar       200       0     0     1
 *    2  DocEntry                     int              4      10     0     1
 *    3  ObjectName                   nvarchar       200       0     0     1
 *    4  ObjType                      nvarchar       100       0     0     1
 *    5  LineNum                      int              4      10     0     1
 *    6  VisOrder                     int              4      10     0     1
 *    7  AcctCode                     nvarchar       100       0     0     1
 *    8  AcctName                     nvarchar       200       0     0     1
 *    9  CardCode                     nvarchar       100       0     0     1
 *   10  CardName                     nvarchar       200       0     0     1
 *   11  EFFECTMONTH                  varchar        100       0     0     1
 *   12  BUDGET                       varchar        300       0     0     1
 *   13  SUB_BUDGET                   varchar        300       0     0     1
 *   14  STATE                        nvarchar       100       0     0     1
 *   15  DocDate                      datetime         8      23     3     1
 *   16  CreateDate                   datetime         8      23     3     1
 *   17  AMOUNT                       decimal          9      18     2     1
 *   18  CURRENTMONTH                 varchar        100       0     0     1
 *   19  Current_month_Posted_Amount  decimal          9      18     2     1
 *   20  Budget_Owner                 nvarchar       200       0     0     1
 *   21  OwnerCode                    nvarchar       100       0     0     1
 *   22  Approver Name                nvarchar       200       0     0     1
 *   23  ApprovalCode                 nvarchar       100       0     0     1
 *   24  Current_month_Budget         decimal          9      18     2     1
 *   25  Status                       nvarchar       100       0     0     1
 *   26  U_NAME                       nvarchar       200       0     0     1
 *   27  CreatedDate                  datetime         8      23     3     1
 *   28  CreateTime                   varchar        100       0     0     1
 *   29  LineRemarks                  nvarchar        -1       0     0     1
 *   30  Comments                     nvarchar        -1       0     0     1
 *   31  ProcesStat                   nvarchar       100       0     0     1
 *   32  UpdateDate                   datetime         8      23     3     1
 *   33  OcrCode                      nvarchar      1000       0     0     1
 *   34  ACOMMENT                     nvarchar        -1       0     0     1
 *   35  VCOMMENT                     nvarchar        -1       0     0     1
 *   36  VerifiedStatus               nvarchar       100       0     0     1
 *   37  ApprovedStatus               nvarchar       100       0     0     1
 *   38  flag                         varchar          1       0     0     0
 *   39  budgetDate                   datetime         8      23     3     1
 *
 *   max_length -1 = (max). nvarchar max_length is in BYTES: 200 = nvarchar(100).
 *
 * No DDL is reproduced here: the audit reproduces only what the database
 * returned, and a CREATE TABLE statement was not part of that.
 */
