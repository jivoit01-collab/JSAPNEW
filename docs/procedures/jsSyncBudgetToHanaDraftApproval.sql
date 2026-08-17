USE [jsaplive3]
GO
/****** Object:  StoredProcedure [bud].[jsSyncBudgetToHanaDraftApproval]    Script Date: 7/9/2026 10:46:54 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [bud].[jsSyncBudgetToHanaDraftApproval]
    @docId INT,
    @company VARCHAR(MAX),
    @status CHAR(1),  -- 'R' for Rejected, 'A' for Approved, 'V' for Verified
    @userId INT,
    @remarks NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @serverName NVARCHAR(100);
    DECLARE @schemaName NVARCHAR(100);
    DECLARE @HanaQuery NVARCHAR(MAX);
    DECLARE @execCmd NVARCHAR(MAX);
    DECLARE @rowCount INT;
    -- OPT: removed @userName and @currentDate (declared/assigned but never used anywhere)

    -- Get HANA config
    EXEC [bud].[jsGetHanaConfiguration] @company, @serverName OUTPUT, @schemaName OUTPUT;

    -- OPT: removed the unused @userName lookup (a jsUser read per call) and @currentDate format.

    -- Check if there are any records to process
    SELECT @rowCount = COUNT(*)
    FROM bud.jsDocEnrtyDetail d
    JOIN bud.jsDocEntry e ON d.docId = e.id
    WHERE d.docId = @docId;

    IF @rowCount = 0
    BEGIN
        RETURN; -- No records to process
    END

    -- Build a single batch query for all updates
    SET @HanaQuery = '';

    -- Build the batch update query using STRING_AGG (if SQL Server 2017+) or XML PATH
    -- (Builder logic left EXACTLY as original to guarantee an identical HANA query string.)
    IF OBJECT_ID('STRING_AGG') IS NOT NULL
    BEGIN
        SELECT @HanaQuery = STRING_AGG(
            'UPDATE "' + @schemaName + '"."tbl_Draft_Approvals" SET ' +
            '"ApprovedStatus" = ''' + @status + ''', ' +
            '"ACOMMENT" = ''' + REPLACE(ISNULL(@remarks, ''), '''', '''''') + ''' ' +
            'WHERE "DocEntry" = ' + CAST(e.docEntry AS VARCHAR) + ' ' +
            'AND "ObjType" = ''' + d.objType + ''' ' +
            'AND "LineNum" = ' + CAST(d.lineNum AS VARCHAR) + ' ' +
            'AND "VisOrder" = ' + CAST(d.visOrder AS VARCHAR),
            '; ') WITHIN GROUP (ORDER BY d.lineNum, d.visOrder)
        FROM bud.jsDocEnrtyDetail d
        JOIN bud.jsDocEntry e ON d.docId = e.id
        WHERE d.docId = @docId;
    END
    ELSE
    BEGIN
        SELECT @HanaQuery = STUFF((
            SELECT '; ' +
            'UPDATE "' + @schemaName + '"."tbl_Draft_Approvals" SET ' +
            '"ApprovedStatus" = ''' + @status + ''', ' +
            '"ACOMMENT" = ''' + REPLACE(ISNULL(@remarks, ''), '''', '''''') + ''' ' +
            'WHERE "DocEntry" = ' + CAST(e.docEntry AS VARCHAR) + ' ' +
            'AND "ObjType" = ''' + d.objType + ''' ' +
            'AND "LineNum" = ' + CAST(d.lineNum AS VARCHAR) + ' ' +
            'AND "VisOrder" = ' + CAST(d.visOrder AS VARCHAR)
            FROM bud.jsDocEnrtyDetail d
            JOIN bud.jsDocEntry e ON d.docId = e.id
            WHERE d.docId = @docId
            ORDER BY d.lineNum, d.visOrder
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '');
    END

    -- Log query
    INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
    VALUES (GETDATE(), 'Built HANA Batch Query for ' + CAST(@rowCount AS VARCHAR) + ' lines',
            'jsSyncBudgetToHanaDraftApproval', LEFT(@HanaQuery, 4000));

    -- Execute batch query in HANA
    BEGIN TRY
        SET @HanaQuery = 'DO BEGIN ' + @HanaQuery + '; END';
        SET @execCmd = 'EXEC (''' + REPLACE(@HanaQuery, '''', '''''') + ''') AT HANADB112';

        EXEC (@execCmd);

        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
        VALUES (GETDATE(), 'Batch Executed Successfully for ' + CAST(@rowCount AS VARCHAR) + ' lines',
                'jsSyncBudgetToHanaDraftApproval', 'Batch update completed');
    END TRY
    BEGIN CATCH
        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
        VALUES (GETDATE(), ERROR_MESSAGE(), 'jsSyncBudgetToHanaDraftApproval ERROR',
                LEFT(@execCmd, 4000));
        THROW;
    END CATCH;
END
