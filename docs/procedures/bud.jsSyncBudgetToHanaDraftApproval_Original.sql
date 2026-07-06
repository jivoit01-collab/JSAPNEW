/* =====================================================================
   PERMANENT BACKUP - DO NOT MODIFY
   Procedure : bud.jsSyncBudgetToHanaDraftApproval
   Extracted : SQL Server (SSMS), pasted by user (LIVE version)
   Date      : 2026-07-06
   Status    : Original Backup (rollback source)
   Note      : Linked server = HANADB112. Already batched (no cursor).
   ===================================================================== */

CREATE PROCEDURE [bud].[jsSyncBudgetToHanaDraftApproval]
    @docId INT,
    @company VARCHAR(MAX),
    @status CHAR(1),  -- 'R' for Rejected, 'A' for Approved, 'V' for Verified
    @userId INT,
    @remarks NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Declare variables
    DECLARE @serverName NVARCHAR(100);
    DECLARE @schemaName NVARCHAR(100);
    DECLARE @HanaQuery NVARCHAR(MAX);
    DECLARE @execCmd NVARCHAR(MAX);
    DECLARE @userName NVARCHAR(100);
    DECLARE @currentDate NVARCHAR(30);
    DECLARE @rowCount INT;

    -- Get HANA config
    EXEC [bud].[jsGetHanaConfiguration] @company, @serverName OUTPUT, @schemaName OUTPUT;

    -- Get userName
    BEGIN TRY
        SELECT @userName = loginUser FROM jsUser WHERE UserId = @userId;
    END TRY
    BEGIN CATCH
        SET @userName = 'User' + CAST(@userId AS NVARCHAR);
    END CATCH;

    -- Format current date
    SET @currentDate = CONVERT(NVARCHAR(30), GETDATE(), 120);

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
    IF OBJECT_ID('STRING_AGG') IS NOT NULL
    BEGIN
        -- SQL Server 2017+ version using STRING_AGG
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
        -- SQL Server 2016 and earlier version using XML PATH
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
        -- Wrap the batch in a DO block for HANA
        SET @HanaQuery = 'DO BEGIN ' + @HanaQuery + '; END';
        SET @execCmd = 'EXEC (''' + REPLACE(@HanaQuery, '''', '''''') + ''') AT HANADB112';

        EXEC (@execCmd);

        -- Log success
        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
        VALUES (GETDATE(), 'Batch Executed Successfully for ' + CAST(@rowCount AS VARCHAR) + ' lines',
                'jsSyncBudgetToHanaDraftApproval', 'Batch update completed');
    END TRY
    BEGIN CATCH
        -- Log error
        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
        VALUES (GETDATE(), ERROR_MESSAGE(), 'jsSyncBudgetToHanaDraftApproval ERROR',
                LEFT(@execCmd, 4000));

        -- Re-throw the error
        THROW;
    END CATCH;
END
