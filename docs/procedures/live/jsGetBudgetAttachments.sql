/*
 * [bud].[jsGetBudgetAttachments] — LIVE definition, supplied 2026-09-24.
 * Created 2025-04-02 12:38:38.620 · Modified 2025-07-12 16:03:56.510
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the first `--` comment would comment out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 *
 * Not one of the 18 audited objects: called by C# for the detail screen's
 * attachments (UserService.GetBudgetAttachmentsAsync).
 */
CREATE PROCEDURE [bud].[jsGetBudgetAttachments]      @budgetId INT = 3831  AS  BEGIN      SET NOCOUNT ON;        DECLARE @docEntry INT;        -- Step 1: Get docEntry from jsDocEntry      SELECT @docEntry = docEntry      FROM bud.jsDocEntry      WHERE id = @budgetId;        -- Step 2: Return matching files from SAPAttachments      SELECT           Branch,          DocEntry,          ObjectName,          ObjType,          trgtPath,          FileName,          FileExt,          AtcEntry      FROM bud.SAPAttachments      WHERE DocEntry = @docEntry;  END;
