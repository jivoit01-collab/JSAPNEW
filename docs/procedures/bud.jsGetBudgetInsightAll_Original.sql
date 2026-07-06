/* =====================================================================
   PERMANENT BACKUP - DO NOT MODIFY
   Procedure : bud.jsGetBudgetInsightAll
   Extracted : SQL Server (SSMS), pasted by user
   Date      : 2026-07-06
   Status    : Original Backup (rollback source)
   ===================================================================== */

CREATE PROCEDURE [bud].[jsGetBudgetInsightAll]
    @company INT = 1,
    @month   VARCHAR(100) = '11-2025'
AS
BEGIN
    SET NOCOUNT ON;

    -- ============================================
    -- TEMP TABLE 1: ALL users in the company (base list)
    -- ============================================
    CREATE TABLE #AllUsers (
        userId INT PRIMARY KEY CLUSTERED,
        userName NVARCHAR(201) NOT NULL
    );

    INSERT INTO #AllUsers (userId, userName)
    SELECT
        u.userId,
        CONCAT(u.firstName, ' ', u.lastName)
    FROM jsUser u
    INNER JOIN jsUserCompany uc ON uc.userId = u.userId
    WHERE uc.companyId = @company;

    -- ============================================
    -- TEMP TABLE 2: Users with their valid stages
    -- ============================================
    CREATE TABLE #UserStages (
        userId INT NOT NULL,
        stageId INT NOT NULL,
        PRIMARY KEY CLUSTERED (userId, stageId)
    );

    INSERT INTO #UserStages (userId, stageId)
    SELECT DISTINCT
        u.userId,
        jus.stageId
    FROM #AllUsers u
    INNER JOIN jsUserStage jus ON jus.userId = u.userId
    INNER JOIN jsStageTemplate st ON jus.stageId = st.stageId
    INNER JOIN jsTemplate t ON st.templateId = t.id
    INNER JOIN jsTemplateApproval ta ON t.id = ta.templateId
    WHERE t.company = @company
      AND ta.approvalId = 5
      AND (
            (jus.startTime IS NULL AND jus.endDate IS NULL)
            OR (
                jus.status = 1 AND
                GETDATE() BETWEEN jus.startTime AND jus.endDate
            )
          );

    -- ============================================
    -- TEMP TABLE 3: Pending documents with month filter
    -- ============================================
    CREATE TABLE #PendingDocs (
        userId INT NOT NULL,
        docId INT NOT NULL,
        stageId INT NOT NULL,
        templateId INT NOT NULL,
        INDEX IX_Pending NONCLUSTERED (userId, docId, stageId, templateId)
    );

    INSERT INTO #PendingDocs (userId, docId, stageId, templateId)
    SELECT DISTINCT
        us.userId,
        jd.id,
        jd.currentStageId,
        jd.templateId
    FROM #UserStages us
    INNER JOIN bud.jsDocEntry jd ON jd.currentStageId = us.stageId
    INNER JOIN bud.jsDocEnrtyDetail d ON jd.id = d.docId
    INNER JOIN bud.jsBudgetTable bt
        ON bt.docEntry = jd.docEntry
        AND bt.ObjType = d.objType
        AND bt.Branch = CAST(d.company AS NVARCHAR(200))
        AND bt.LineNum = d.lineNum
        AND bt.VisOrder = d.visOrder
    WHERE jd.status = 'P'
      AND bt.CURRENTMONTH = @month;

    -- Remove already acted upon by user
    DELETE pd
    FROM #PendingDocs pd
    WHERE EXISTS (
        SELECT 1
        FROM bud.jsBudgetStatusWorkflow wf
        WHERE wf.docId = pd.docId
          AND wf.stageId = pd.stageId
          AND wf.templateId = pd.templateId
          AND wf.userId = pd.userId
    );

    -- ============================================
    -- TEMP TABLE 4: Pending counts per user
    -- ============================================
    CREATE TABLE #PendingCounts (
        userId INT PRIMARY KEY CLUSTERED,
        TotalPending INT NOT NULL DEFAULT 0
    );

    INSERT INTO #PendingCounts (userId, TotalPending)
    SELECT userId, COUNT(DISTINCT docId)
    FROM #PendingDocs
    GROUP BY userId;

    -- ============================================
    -- TEMP TABLE 5: Approved & Rejected counts (ONE PASS)
    -- ============================================
    CREATE TABLE #ActionCounts (
        userId INT PRIMARY KEY CLUSTERED,
        TotalApproved INT NOT NULL DEFAULT 0,
        TotalRejected INT NOT NULL DEFAULT 0
    );

    INSERT INTO #ActionCounts (userId, TotalApproved, TotalRejected)
    SELECT
        us.userId,
        COUNT(DISTINCT CASE WHEN wf.status = 'A' THEN jd.id END),
        COUNT(DISTINCT CASE WHEN wf.status = 'R' THEN jd.id END)
    FROM #UserStages us
    INNER JOIN bud.jsBudgetStatusWorkflow wf
        ON wf.stageId = us.stageId
        AND wf.userId = us.userId
        AND wf.status IN ('A', 'R')
    INNER JOIN bud.jsDocEntry jd ON wf.docId = jd.id
    INNER JOIN bud.jsDocEnrtyDetail d ON jd.id = d.docId
    INNER JOIN bud.jsBudgetTable bt
        ON bt.docEntry = jd.docEntry
        AND bt.ObjType = d.objType
        AND bt.Branch = CAST(d.company AS NVARCHAR(200))
        AND bt.LineNum = d.lineNum
        AND bt.VisOrder = d.visOrder
    WHERE bt.CURRENTMONTH = @month
    GROUP BY us.userId;

    -- ============================================
    -- FINAL OUTPUT: ALL users with their counts
    -- ============================================
    SELECT
        au.userId AS UserID,
        au.userName AS UserName,
        ISNULL(pc.TotalPending, 0) AS TotalPending,
        ISNULL(ac.TotalApproved, 0) AS TotalApproved,
        ISNULL(ac.TotalRejected, 0) AS TotalRejected,
        'Budget' AS type
    FROM #AllUsers au
    LEFT JOIN #PendingCounts pc ON au.userId = pc.userId
    LEFT JOIN #ActionCounts ac ON au.userId = ac.userId
    ORDER BY au.userName;

    -- Cleanup
    DROP TABLE #AllUsers;
    DROP TABLE #UserStages;
    DROP TABLE #PendingDocs;
    DROP TABLE #PendingCounts;
    DROP TABLE #ActionCounts;
END;
