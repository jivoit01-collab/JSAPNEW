/*
 * [bud].[jsGetNextApprover] — LIVE definition, supplied 2026-09-24.
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the first `--` comment would comment out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 *
 * Not to be confused with [adv].[jsGetNextApprover] (Advance Payment module).
 */
  CREATE PROCEDURE [bud].[jsGetNextApprover]      @budgetId INT = 14  AS  BEGIN      SET NOCOUNT ON;        BEGIN TRY          -- Declare variable to hold current stage ID          DECLARE @stageId INT;            -- Get current stage ID for the given budget          SELECT @stageId = currentStageId           FROM bud.jsDocEntry          WHERE id = @budgetId;            -- If no matching budget entry found, return          IF @stageId IS NULL          BEGIN              RAISERROR('No currentStageId found for the given budgetId.', 16, 1);              RETURN;          END            -- Select users assigned to the current stage who haven't taken action yet          SELECT               us.userId,              CONCAT(u.firstName,' ',u.lastName) AS loginUser          FROM               dbo.jsUserStage us          INNER JOIN               jsUser u ON u.userId = us.userId          LEFT JOIN               bud.jsBudgetStatusWorkflow bsw               ON bsw.userId = us.userId                  AND bsw.stageId = @stageId                  AND bsw.docId = @budgetId          WHERE               us.stageId = @stageId              AND bsw.docId IS NULL;        END TRY      BEGIN CATCH          -- Return error information          DECLARE @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;          SELECT               @ErrorMessage = ERROR_MESSAGE(),              @ErrorSeverity = ERROR_SEVERITY(),              @ErrorState = ERROR_STATE();          RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);      END CATCH  END;
