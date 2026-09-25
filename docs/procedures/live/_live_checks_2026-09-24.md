# Live read-only checks — jsaplive3, 2026-09-24

Results of read-only queries run by the user in SSMS against `jsaplive3` during
the Budget legacy audit. Numbers are as returned; interpretation is marked
[SQL] (follows from a definition in this folder) or [INFERRED].

## Linked servers (`sys.servers`)
Only `HANADB112` exists (product HANA, provider MSDASQL, data_source HANA64,
modified 2024-12-14). **`HANA112` does not exist** — every object that uses it
(`usp_Cleanup_Obsolete_Budget_DocEntries`, both `...1` copies,
`jsCompareDocEntryWithDraftApproval`, `jsGetBudgetSap`) cannot reach HANA.

## SQL Agent jobs touching budget (full list checked)
| Job | Enabled | Runs | Every |
|---|---|---|---|
| 48 hrs | yes | `bud.jsAutoApproveBudgetAfter48Hours 75` | 10 min |
| Budget Sync Data | yes | `InsertBudgetFromHANA`; `jsProcessAllUsersBudgetApprovals 1`; `... 2` | 5 min |
| Sync attchments | yes | `bud.jsSyncAttachmentsToHana` | 2 min |
| Sync Workflow To HANA | yes | `bud.jsSyncBudgetApprovalWorkflow` | 60 min |
| 24 hrs | no | auto-approve `75,1` / `75,2` in database `jsap` | 10 min |
| Budget Stage Transitions Email Job | no | `bud.jsBudgetStageTransitionsmail` | 11:00, 16:00 |

No job loads `bud.jsBudgetTable_vg` and none runs every 4 hours at hh:04.
`jsaplive3_backup` overwrites a single `C:\jsap_backup\jsaplive3.bak` nightly.

## Users referenced by auto-approval
75 Gagandeep Singh (the "system" user) · never auto-approved for: 68 Nirmal Kaur,
79 Gurpreet Singh, 95 Arshdeep Singh.

## `bud.jsAutoApprovalLog`
| actionType | rows | distinct docs | approvals added |
|---|---|---|---|
| SKIPPED_LIMIT | 6,389,292 | 2,652 | 0 |
| SKIPPED_BAD_MONTH | 297,702 | 411 | 0 |
| AUTO_APPROVE | 271,185 | 2,335 | 1,991 |
| SKIPPED | 100,285 | 51 | 0 |
| SKIPPED_NO_BUDGET | 64,258 | 2 | 0 |
| FINAL_APPROVE | 1,920 | 1,920 | 0 |
| MOVE_STAGE | 71 | 66 | 0 |
| ERROR | 13 | 5 | 0 |

## Loading and document creation by company
| Branch | rows in `bud.jsBudgetTable` | last loaded |
|---|---|---|
| BEVERAGE | 8,541 | 2026-09-23 17:30:03.940 |
| OIL | 24,515 | 2026-09-23 16:20:04.083 |

| template company | budget documents | newest document |
|---|---|---|
| 1 | 9,390 | 2026-09-23 |
| 2 | 2,353 | 2026-09-23 |

Both companies are still loaded and routed. Oil's `DRAFT_APPROVAL` evidently
returns Beverage lines too (the `Branch` value comes from its result set)
[INFERRED].

## Same SAP line in more than one approval document
Query: group `jsDocEntry` x `jsDocEnrtyDetail` by company, docEntry, objType,
lineNum, visOrder; `HAVING COUNT(DISTINCT jsDocEntry.id) > 1`.

Rows returned: several hundred. Every row is a line approved in two (a few in
three) separate approval documents. Main template pairs:

- **421 + 422** — the large majority: OIL ObjType 18 docEntries 36826–49737.
- 90 + 276 — OIL ObjType 18, docEntries 31054–31202.
- 78 + 337, 134 + 412, 158 + 422, 85 + 348 — OIL ObjType 28, docEntries 2247–2409, 3220–3222.
- 320 + 343, 320 + 353 — OIL ObjType 18, docEntries 50321–50744.
- 218 + 341 (ObjType 14), 12 + 55, 12 + 73 + 408, 112 + 345, 79 + 156, and
  BEVERAGE 93 + 100, 268 + 373, 416 + 431, 100 + 302.
- **OIL 3410 / 28 / line 1: two documents under the SAME template (192, 192).**
  The creation guard (`NOT EXISTS docEntry + templateId`) should make that
  impossible; cause unknown [UNKNOWN].

Cause for the pairs: two templates active at the same time whose filters both
match the line [INFERRED — needs the template list, see
`jsQuery_budget_templates.csv`].

## SAP lines with no approval document although the same docEntry has one
Since 2025-11-29, `ProcesStat = 'Y'`:

| Branch | DocEntry | ObjType | lines | first loaded |
|---|---|---|---|---|
| OIL | 6806 | 28 | 4 | 2026-09-16 19:00:04.300 |
| OIL | 55214 | 18 | 1 | 2026-08-27 13:08:02.523 |
| OIL | 5484 | 28 | 18 | 2026-04-30 13:00:05.437 |
| OIL | 5483 | 28 | 268 | 2026-04-30 12:50:04.720 |
| OIL | 42632 | 18 | 6 | 2026-04-16 15:50:05.300 |
| BEVERAGE | 247 | 46 | 1 | 2026-04-04 17:50:06.070 |
| OIL | 39382 | 18 | 12 | 2026-02-13 15:05:04.330 |
| OIL | 37896 | 14 | 2 | 2026-01-29 18:20:04.087 |
| OIL | 33048 | 14 | 1 | 2025-12-06 18:10:03.527 |
| OIL | 33017 | 14 | 1 | 2025-12-06 18:00:03.687 |
| OIL | 32937 | 14 | 1 | 2025-12-05 17:42:03.290 |

Either these lines match no active template, or they were skipped by the
creation guard in `jsExecuteBudgetQueries`, which checks only
`docEntry + templateId` (not objType, company, or line) [SQL].

### Line-by-line result (the unrouted lines, and the documents their SAP document did get)

Approval documents that exist for these SAP documents:

| docEntry | templateId | status | created | lines in doc |
|---|---|---|---|---|
| 247 | 365 | A | 2026-04-04 | 3 |
| 5483 | 329, 330, 331, 337, 342, 347, 349, 412 | A | 2026-04-30 | **0** |
| 5483 | 353, 422 | **P** | 2026-04-30 | **0** |
| 5484 | 331, 337, 412 | A | 2026-04-30 | **0** |
| 5484 | 348, 353 | **P** | 2026-04-30 | **0** |
| 6806 | 330 | R | 2026-09-16 | 1 |
| 32937 | 344 | A | 2025-12-05 | 6 |
| 33017 | 344 | A | 2025-12-06 | 5 |
| 33048 | 344 | R | 2025-12-06 | 5 |
| 37896 | 345 | R | 2026-01-29 | 6 |
| 39382 | 324 | A | 2026-02-13 | 10 |
| 42632 | 324 | A | 2026-03-25 | 9 |
| 55214 | 324 | P | 2026-08-27 | 3 |

Three distinct causes:

1. **Headers with zero lines (5483, 5484).** 15 approval documents were created
   on 2026-04-30 with no detail rows — the failure the 2026-09-14 patch of
   `jsExecuteBudgetQueries` describes (duplicated source rows violate
   `UQ_jsDocEnrtyDetail_Unique`, header survives) [SQL]. 11 of them were
   approved although empty; 4 are still `P` after five months. Across all
   documents: 18 have no lines — 14 `A` (2025-08-23 … 2026-04-30) and 4 `P`
   (2026-04-30). A `P` document
   with no lines cannot appear in `jsGetPendingBudgets` (it joins the lines)
   and auto-approval logs it `SKIPPED` ("Company not found in
   jsDocEnrtyDetail") every 10 minutes [SQL]. Because the headers exist, the
   creation guard stops the 286 real lines from ever being routed.
2. **Lines that arrived after the document was created.** 42632: document
   created 2026-03-25, six more lines loaded 2026-04-16, all matching the same
   template 324 — never added [SQL guard + data]. 39382 and 6806 look the same
   (lines match the template of the existing document) [INFERRED].
3. **No active template matches.** 32937, 33017, 33048, 37896: budget `OTE`
   with sub-budget GT/CSD/MT on ObjType 14 — the only active non-JV OTE
   template covers sub-budget `Admin` only. 55214 line 1: `Del Bkhp` with
   AcctCode 5680011 — 5680011 is excluded from template 324 and the 5680011
   templates cover Sales, FACT_COM/Factory and BackOff only [INFERRED from the
   visible part of the template list]. BEVERAGE 247: Beverage templates not
   yet seen.

### Journal vouchers do not have a unique line key
For JV 5483 (ObjType 28), the same `LineNum`/`VisOrder` appears with up to
four different budget/account combinations (e.g. line 0: Factory 5680020,
FACT_COM 5660005, BackOff/Legal 5680008, BackOff/IT 5680004), and each
combination was stored four times. Every key in the pipeline —
`jsDocEnrtyDetail`, `UQ_jsDocEnrtyDetail_Unique`, the joins to
`bud.jsBudgetTable`, and the partition of `bud.jsBudgetTable_Dedup` — uses
`DocEntry + ObjType + Branch + LineNum + VisOrder` (+ month). For such a JV the
de-duplicating view keeps one of the four real lines and drops the other three
[SQL + data]. Why HANA returns repeated line numbers for a JV (for example,
several journal entries in one batch each numbered from 0) is not visible from
SQL Server [UNKNOWN]. Whole table: 35 colliding keys, all ObjType 28, Branch OIL.

Scope note (2026-09-24): OMS Budget is built new with no data migration, so
these checks were stopped here; they describe legacy behaviour only.
