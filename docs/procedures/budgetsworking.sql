USE [jsaplive3]
GO
/****** Object:  StoredProcedure [bud].[jsSyncBudgetToHanaDraftApproval]    Script Date: 23-07-2026 12:20:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER   PROCEDURE [bud].[jsSyncBudgetToHanaDraftApproval]
    @docId INT,
    @company VARCHAR(MAX),
    @status CHAR(1),
    @userId INT,
    @remarks NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @serverName NVARCHAR(100), @schemaName NVARCHAR(100);
    DECLARE @HanaQuery NVARCHAR(MAX), @execCmd NVARCHAR(MAX);
    DECLARE @rowCount INT, @branchName NVARCHAR(20), @safeRemarks NVARCHAR(MAX);

    EXEC [bud].[jsGetHanaConfiguration] @company, @serverName OUTPUT, @schemaName OUTPUT;

    SET @branchName =
        CASE WHEN @company IN ('1','OIL') THEN 'OIL'
             WHEN @company IN ('2','BEVERAGE','BEV') THEN 'BEVERAGE'
             WHEN @company IN ('3','MART') THEN 'MART'
             ELSE 'UNKNOWN' END;

    SET @safeRemarks = REPLACE(ISNULL(@remarks, ''), '''', '''''');

    SELECT @rowCount = COUNT(*)
    FROM bud.jsDocEnrtyDetail d JOIN bud.jsDocEntry e ON d.docId = e.id
    WHERE d.docId = @docId;

    IF @rowCount = 0
    BEGIN
        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
        VALUES (GETDATE(), 'No detail rows found for docId ' + CAST(@docId AS VARCHAR(20)),
                'jsSyncBudgetToHanaDraftApproval', NULL);
        RETURN;
    END;

    -- ===== your existing UPSERT batch build (unchanged) =====
    SELECT @HanaQuery = STUFF((
        SELECT '; ' +
            'INSERT INTO "' + @schemaName + '"."tbl_Draft_Approvals" ' +
            '("Branch","DocEntry","ObjectName","ObjType","LineNum","VisOrder",' +
             '"AcctCode","CardCode","CardName","DocDate","AMOUNT","CURRENTMONTH",' +
             '"BUDGET","SUB_BUDGET","ApprovedStatus","ACOMMENT") ' +
            'SELECT ''' + @branchName + ''', O."DocEntry", ' +
                'CASE WHEN O."ObjType"=''18'' THEN ''A/P INVOICE'' ' +
                     'WHEN O."ObjType"=''19'' THEN ''A/P CREDIT MEMO'' ' +
                     'WHEN O."ObjType"=''13'' THEN ''A/R INVOICE'' ' +
                     'WHEN O."ObjType"=''14'' THEN ''A/R CREDIT MEMO'' ELSE ''DRAFT'' END, ' +
                'O."ObjType", D."LineNum", D."VisOrder", D."AcctCode", O."CardCode", ' +
                'O."CardName", O."DocDate", D."LineTotal", D."OcrCode2", D."OcrCode3", ' +
                'D."OcrCode4", ''' + @status + ''', ''' + @safeRemarks + ''' ' +
            'FROM "' + @schemaName + '"."ODRF" O ' +
            'JOIN "' + @schemaName + '"."DRF1" D ON D."DocEntry" = O."DocEntry" ' +
            'WHERE O."DocEntry" = ' + CAST(e.docEntry AS VARCHAR(20)) + ' ' +
              'AND O."ObjType" = ''' + CAST(d.objType AS VARCHAR(20)) + ''' ' +
              'AND D."LineNum" = ' + CAST(d.lineNum AS VARCHAR(20)) + ' ' +
              'AND D."VisOrder" = ' + CAST(d.visOrder AS VARCHAR(20)) + ' ' +
              'AND NOT EXISTS (SELECT 1 FROM "' + @schemaName + '"."tbl_Draft_Approvals" A ' +
                    'WHERE A."DocEntry"=O."DocEntry" AND A."ObjType"=O."ObjType" ' +
                      'AND A."LineNum"=D."LineNum" AND A."VisOrder"=D."VisOrder")' +
            '; UPDATE "' + @schemaName + '"."tbl_Draft_Approvals" SET ' +
                '"ApprovedStatus"=''' + @status + ''', "ACOMMENT"=''' + @safeRemarks + ''' ' +
            'WHERE "DocEntry"=' + CAST(e.docEntry AS VARCHAR(20)) + ' ' +
              'AND "ObjType"=''' + CAST(d.objType AS VARCHAR(20)) + ''' ' +
              'AND "LineNum"=' + CAST(d.lineNum AS VARCHAR(20)) + ' ' +
              'AND "VisOrder"=' + CAST(d.visOrder AS VARCHAR(20))
        FROM bud.jsDocEnrtyDetail d JOIN bud.jsDocEntry e ON d.docId = e.id
        WHERE d.docId = @docId
        ORDER BY d.lineNum, d.visOrder
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '');

    BEGIN TRY
        SET @HanaQuery = 'DO BEGIN ' + @HanaQuery + '; END';
        SET @execCmd = 'EXEC (''' + REPLACE(@HanaQuery, '''', '''''') + ''') AT HANADB112';
        EXEC (@execCmd);

        -- ============== NEW: verify it actually landed ==============
        DECLARE @verifyPredicate NVARCHAR(MAX);
        SELECT @verifyPredicate = STUFF((
            SELECT ' OR ("DocEntry"=' + CAST(e.docEntry AS VARCHAR(20)) +
                   ' AND "ObjType"=''' + CAST(d.objType AS VARCHAR(20)) + '''' +
                   ' AND "LineNum"=' + CAST(d.lineNum AS VARCHAR(20)) +
                   ' AND "VisOrder"=' + CAST(d.visOrder AS VARCHAR(20)) + ')'
            FROM bud.jsDocEnrtyDetail d JOIN bud.jsDocEntry e ON d.docId = e.id
            WHERE d.docId = @docId
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 4, '');

        DECLARE @verifyCmd NVARCHAR(MAX) =
            'SELECT COUNT(*) FROM "' + @schemaName + '"."tbl_Draft_Approvals" ' +
            'WHERE "ApprovedStatus" = ''' + @status + ''' AND (' + @verifyPredicate + ')';

        CREATE TABLE #verify (c INT);
        INSERT INTO #verify EXEC (@verifyCmd) AT HANADB112;
        DECLARE @hanaMatched INT = (SELECT TOP 1 c FROM #verify);
        DROP TABLE #verify;

        IF ISNULL(@hanaMatched, 0) < @rowCount
        BEGIN
            INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
            VALUES (GETDATE(),
                    'VERIFY FAILED docId ' + CAST(@docId AS VARCHAR(20)) + ': only ' +
                    CAST(ISNULL(@hanaMatched,0) AS VARCHAR(20)) + ' of ' +
                    CAST(@rowCount AS VARCHAR(20)) + ' lines have ApprovedStatus=' + @status +
                    ' in ' + @schemaName,
                    'jsSyncBudgetToHanaDraftApproval VERIFY', LEFT(@execCmd, 4000));

            THROW 51001, 'HANA sync incomplete: not all lines updated (missing draft or wrong company).', 1;
        END
        -- ============== end NEW ==============

        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
        VALUES (GETDATE(),
                'HANA UPSERT verified for docId ' + CAST(@docId AS VARCHAR(20)) +
                ' (' + CAST(@rowCount AS VARCHAR(20)) + ' lines)',
                'jsSyncBudgetToHanaDraftApproval', 'UPSERT + verify OK');
    END TRY
    BEGIN CATCH
        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
        VALUES (GETDATE(), ERROR_MESSAGE(), 'jsSyncBudgetToHanaDraftApproval ERROR',
                LEFT(ISNULL(@execCmd, @HanaQuery), 4000));
        THROW;
    END CATCH;
END
