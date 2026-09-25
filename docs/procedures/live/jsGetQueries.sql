/*
 * [dbo].[jsGetQueries] — LIVE definition, supplied 2026-09-24.  (schema dbo)
 * Created 2025-03-19 11:49:46.803 · Modified 2025-05-06 11:16:14.890
 * Source: sys.sql_modules.definition, copied from an SSMS results grid.
 *
 * The grid collapsed every line break into spaces, so the definition below is
 * the original text on ONE line, otherwise verbatim. Read it; do not run it —
 * the leading `--` comment comments out everything after it. For a runnable
 * copy use SSMS Tasks > Generate Scripts.
 *
 * Called by bud.jsProcessAllUsersBudgetApprovals as
 *     EXEC [dbo].[jsGetQueries] @userId, @company, 5
 * and its rows are handed to bud.jsExecuteBudgetQueries, which runs each
 * jsQuery.query and creates budget documents from the result.
 */
-- EXEC [dbo].[jsGetQueries] 77,1,5    CREATE PROCEDURE [dbo].[jsGetQueries]      @userId INT = 76,      @company INT = 1,      @type INT = 5  AS  BEGIN      SET NOCOUNT ON;        -- Temp Table: StageWithCompany      IF OBJECT_ID('tempdb..#StageWithCompany') IS NOT NULL DROP TABLE #StageWithCompany;      CREATE TABLE #StageWithCompany (stageId INT, userId INT);        INSERT INTO #StageWithCompany      SELECT js.id AS stageId, jus.userId      FROM jsUserStage jus      INNER JOIN jsStage js ON jus.stageId = js.id AND jus.userId = @userId      WHERE js.company = @company        AND (              (jus.startTime IS NULL AND jus.endDate IS NULL) -- Include if both dates are null (ignore status)              OR (                  jus.startTime IS NOT NULL AND                   jus.endDate IS NOT NULL AND                  jus.status = 1 AND                  GETDATE() BETWEEN jus.startTime AND jus.endDate              )            );      -- Temp Table: TemplateWithQuery      IF OBJECT_ID('tempdb..#TemplateWithQuery') IS NOT NULL DROP TABLE #TemplateWithQuery;      CREATE TABLE #TemplateWithQuery (tempId INT, query NVARCHAR(MAX), queryId INT);        INSERT INTO #TemplateWithQuery      SELECT jt.id AS tempId, jq.query, jtq.queryId      FROM jsTemplate jt      INNER JOIN jsTemplateQuery jtq ON jt.id = jtq.templateId      INNER JOIN jsQuery jq ON jtq.queryId = jq.id      WHERE jt.company = @company AND jt.isActive = 1;        -- Temp Table: JoinedResults      IF OBJECT_ID('tempdb..#JoinedResults') IS NOT NULL DROP TABLE #JoinedResults;      CREATE TABLE #JoinedResults (userId INT, stageId INT, tempId INT, queryId INT, query NVARCHAR(MAX));        INSERT INTO #JoinedResults      SELECT swc.userId, swc.stageId, twq.tempId, twq.queryId, twq.query      FROM #StageWithCompany swc      INNER JOIN jsStageTemplate jst ON swc.stageId = jst.stageId      INNER JOIN #TemplateWithQuery twq ON jst.templateId = twq.tempId;        -- Final Join and Output      SELECT DISTINCT jr.userId, jr.stageId, jr.tempId, jr.queryId, jr.query,             jta.approvalId, ja.approvalName      FROM #JoinedResults jr      INNER JOIN jsTemplateApproval jta ON jr.tempId = jta.templateId      INNER JOIN jsApproval ja ON jta.approvalId = ja.id      WHERE ja.id = @type;        -- Cleanup      DROP TABLE #StageWithCompany;      DROP TABLE #TemplateWithQuery;      DROP TABLE #JoinedResults;  END;
