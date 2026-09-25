/*
 * [bud].[jsGetBudApprovalFlow] — LIVE definition, supplied 2026-09-24.
 * Created 2025-12-23 11:46:29.363 · Modified 2026-04-27 14:39:07.847
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the first `--` comment would comment out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 */
-- Reads the full approval flow for a BUD item  -- Input: @flowId = [bud].[jsFlow].id  -- Output: one row per stage in the template with assigned user, action (if any), and requirements.  CREATE PROCEDURE [bud].[jsGetBudApprovalFlow]      @flowId BIGINT = 13946  AS  BEGIN      SET NOCOUNT ON;      -- 1) Pull core flow info      DECLARE @templateId INT, @currentStageId INT;      SELECT           @templateId     = f.templateId,          @currentStageId = f.currentStageId      FROM bud.jsFlow AS f      WHERE f.id = @flowId;      -- 2) Enumerate stages for the template with assignments and any actions taken      SELECT           st.stageId,          s.stage AS stageName,          st.priority,          CONCAT(u.firstName, ' ', u.lastName) AS assignedTo,          wf.status AS actionStatus,          wf.createdOn AS actionDate,          wf.description,          ac.approval AS approvalRequired,          rc.rejection AS rejectRequired      FROM jsStageTemplate            AS st      LEFT JOIN jsUserStage           AS us ON st.stageId = us.stageId      LEFT JOIN jsStage               AS s  ON s.id       = us.stageId      LEFT JOIN jsUser                AS u  ON us.userId  = u.userId      LEFT JOIN bud.jsFlowStatus      AS wf              ON wf.stageId   = st.stageId            AND wf.flowId    = @flowId            AND wf.userId    = us.userId      LEFT JOIN dbo.jsApprovalCount   AS ac ON ac.id = s.approvalId      LEFT JOIN dbo.jsRejectionCount  AS rc ON rc.id = s.rejectid      WHERE st.templateId = @templateId        AND (us.status = 1 OR us.status IS NULL)      ORDER BY st.priority;  END
