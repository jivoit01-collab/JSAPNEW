/*
 * [bud].[jsGetBudgetApprovalFlow] — LIVE definition, supplied 2026-09-24.
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the first `--` comment would comment out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 */
CREATE PROCEDURE [bud].[jsGetBudgetApprovalFlow]      @budgetId INT = 13953  AS  BEGIN      SET NOCOUNT ON;        -- Step 1: Get core budget info      DECLARE @templateId INT, @currentStageId INT;        SELECT           @templateId = templateId,          @currentStageId = currentStageId      FROM bud.jsDocEntry      WHERE id = @budgetId;        -- Step 2: Get all stages for the template (in order)      SELECT           st.stageId,    s.stage AS stageName,          st.priority,          CONCAT(u.firstName,' ',u.lastName) AS assignedTo,          wf.status AS actionStatus,          wf.createdOn AS actionDate,          wf.description,    ac.approval AS approvalRequired,    rc.rejection AS rejectRequired      FROM jsStageTemplate st      LEFT JOIN jsUserStage us ON st.stageId = us.stageId   LEFT JOIN jsStage s ON s.id = us.stageId      LEFT JOIN jsUser u ON us.userId = u.userId      LEFT JOIN bud.jsBudgetStatusWorkflow wf          ON wf.stageId = st.stageId           AND wf.docId = @budgetId           AND wf.userId = us.userId   LEFT JOIN dbo.jsApprovalCount as ac ON ac.id = s.approvalId   LEFT JOIN dbo.jsRejectionCount as rc ON rc.id = s.rejectid      WHERE st.templateId = @templateId AND (us.status = 1 OR us.status IS NULL)      ORDER BY st.priority;  END;
