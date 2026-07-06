/* =====================================================================
   PERMANENT BACKUP - DO NOT MODIFY
   Procedure : bud.jsExecuteBudgetQueries
   Extracted : SQL Server (SSMS), pasted by user (LIVE version)
   Date      : 2026-07-06
   Status    : Original Backup (rollback source)
   Note      : The paste also contained a fully commented-out legacy ALTER
               version (inert). Only the ACTIVE procedure is stored here,
               since that is the rollback source. Linked server = HANA112.
   ===================================================================== */

CREATE PROCEDURE [bud].[jsExecuteBudgetQueries]
    @inputTable QueryTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    -- Generate a unique ID for this execution
    DECLARE @executionId UNIQUEIDENTIFIER = NEWID();

    DECLARE @query NVARCHAR(MAX);
    DECLARE @templateId INT, @queryId INT;
    DECLARE @docEntry INT;
    DECLARE @branch VARCHAR(50);
    DECLARE @objType NVARCHAR(50);
    DECLARE @CURRENTMONTH VARCHAR(100);
    DECLARE @totalStage INT;
    DECLARE @currentStage INT;
    DECLARE @newDocId INT;
    DECLARE @comp INT;
    DECLARE @company VARCHAR(MAX);
    DECLARE @serverName NVARCHAR(100);
    DECLARE @schemaName NVARCHAR(100);
    DECLARE @HanaDetailInsertQuery NVARCHAR(MAX);

    DECLARE query_cursor CURSOR FOR
    SELECT tempId, queryId, query
    FROM @inputTable;
    OPEN query_cursor;
    FETCH NEXT FROM query_cursor INTO @templateId, @queryId, @query;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Add executionId to the query results
        SET @query = 'INSERT INTO bud.TempResults (
            Branch, DocEntry, ObjectName, ObjType, LineNum, VisOrder,
            AcctCode, AcctName, CardCode, CardName, EFFECTMONTH, BUDGET,
            SUB_BUDGET, STATE, DocDate, CreateDate, AMOUNT, CURRENTMONTH,
            Current_month_Posted_Amount, Budget_Owner, OwnerCode, [Approver Name],
            ApprovalCode, Current_month_Budget, Status, U_NAME, CreatedDate,
            CreateTime, LineRemarks, Comments, ProcesStat, UpdateDate,
            OcrCode, ACOMMENT, VCOMMENT, VerifiedStatus, ApprovedStatus, flag, executionId
        )
        SELECT
            Branch, DocEntry, ObjectName, ObjType, LineNum, VisOrder,
            AcctCode, AcctName, CardCode, CardName, EFFECTMONTH, BUDGET,
            SUB_BUDGET, STATE, DocDate, CreateDate, AMOUNT, CURRENTMONTH,
            Current_month_Posted_Amount, Budget_Owner, OwnerCode, [Approver Name],
            ApprovalCode, Current_month_Budget, Status, U_NAME, CreatedDate,
            CreateTime, LineRemarks, Comments, ProcesStat, UpdateDate,
            OcrCode, ACOMMENT, VCOMMENT, VerifiedStatus, ApprovedStatus, flag,
            ''' + CAST(@executionId AS NVARCHAR(36)) + '''
        FROM (' + @query + ') AS QueryResults';

        -- Execute query and store results with the executionId
        EXEC sp_executesql @query;

        -- Take company from template table
        SELECT @comp = company FROM jsTemplate WHERE id = @templateId
        IF(@comp = 1)
        BEGIN
            SET @company = 'OIL'
        END
        ELSE
        BEGIN
            SET @company = 'BEVERAGE'
        END

        -- Get HANA configuration for this company
        EXEC [bud].[jsGetHanaConfiguration] @company, @serverName OUTPUT, @schemaName OUTPUT;

        DECLARE rec_cursor CURSOR FOR
        SELECT DISTINCT DocEntry, Branch, ObjType, CURRENTMONTH
        FROM bud.TempResults
        WHERE flag = 'A' AND ProcesStat = 'Y' AND executionId = @executionId;
        OPEN rec_cursor;
        FETCH NEXT FROM rec_cursor INTO @docEntry, @branch, @objType, @CURRENTMONTH;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Get stage information
            SELECT @totalStage = COUNT(*)
            FROM jsStageTemplate
            WHERE templateId = @templateId;
            SELECT @currentStage = stageId
            FROM jsStageTemplate
            WHERE templateId = @templateId AND priority = 1;

            -- Check if already exists in new jsDocEntry table
            IF NOT EXISTS (
                SELECT 1 FROM bud.jsDocEntry
                WHERE docEntry = @docEntry AND templateId = @templateId
            )
            BEGIN
                DECLARE @newDocIdTable TABLE (id INT);
                -- Insert into jsDocEntry table
                INSERT INTO bud.jsDocEntry (
                    docEntry, status, currentStageId, templateId, totalStage, currentSatge, date
                )
                OUTPUT INSERTED.id INTO @newDocIdTable(id)
                VALUES (
                    @docEntry, 'P', @currentStage, @templateId, @totalStage, 1, @CURRENTMONTH
                );

                -- Get inserted ID
                SELECT @newDocId = id FROM @newDocIdTable;

                -- Insert into local jsDocEnrtyDetail
                INSERT INTO bud.jsDocEnrtyDetail (
                    objType, company, lineNum, visOrder, docId
                )
                SELECT
                    ObjType,
                    Branch,
                    LineNum,
                    VisOrder,
                    @newDocId
                FROM bud.TempResults
                WHERE
                    DocEntry = @docEntry AND
                    Branch = @branch AND
                    ObjType = @objType AND
                    flag = 'A' AND
                    ProcesStat = 'Y' AND
                    executionId = @executionId;

                DELETE FROM @newDocIdTable;

                -- Now insert the same details into HANA
                CREATE TABLE #TempDetails (
                    ObjType NVARCHAR(50),
                    Company NVARCHAR(100),
                    LineNum INT,
                    VisOrder INT
                );

                INSERT INTO #TempDetails (ObjType, Company, LineNum, VisOrder)
                SELECT
                    ObjType,
                    Branch,
                    LineNum,
                    VisOrder
                FROM bud.TempResults
                WHERE
                    DocEntry = @docEntry AND
                    Branch = @branch AND
                    ObjType = @objType AND
                    flag = 'A' AND
                    ProcesStat = 'Y' AND
                    executionId = @executionId;

                DECLARE @ObjTypeValue NVARCHAR(50), @CompanyValue NVARCHAR(100), @LineNumValue INT, @VisOrderValue INT;

                DECLARE detail_cursor CURSOR FOR
                SELECT ObjType, Company, LineNum, VisOrder FROM #TempDetails;

                OPEN detail_cursor;
                FETCH NEXT FROM detail_cursor INTO @ObjTypeValue, @CompanyValue, @LineNumValue, @VisOrderValue;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @HanaDetailInsertQuery = 'INSERT INTO "' + @schemaName + '"."jsDocEntryDetail" ' +
                        '("objType", "company", "lineNum", "visOrder", "docId") VALUES (' +
                        '''' + @ObjTypeValue + ''', ' +
                        '''' + @CompanyValue + ''', ' +
                        CAST(@LineNumValue AS VARCHAR) + ', ' +
                        CAST(@VisOrderValue AS VARCHAR) + ', ' +
                        CAST(@newDocId AS VARCHAR) + ')';

                    BEGIN TRY
                        EXEC (@HanaDetailInsertQuery) AT HANA112;
                    END TRY
                    BEGIN CATCH
                        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
                        VALUES (GETDATE(), ERROR_MESSAGE(), 'jsExecuteBudgetQueries-DetailInsert', @HanaDetailInsertQuery);
                    END CATCH

                    FETCH NEXT FROM detail_cursor INTO @ObjTypeValue, @CompanyValue, @LineNumValue, @VisOrderValue;
                END

                CLOSE detail_cursor;
                DEALLOCATE detail_cursor;

                DROP TABLE #TempDetails;
            END

            FETCH NEXT FROM rec_cursor INTO @docEntry, @branch, @objType, @CURRENTMONTH;
        END
        CLOSE rec_cursor;
        DEALLOCATE rec_cursor;

        -- Clean up TempResults data for this query execution
        DELETE FROM bud.TempResults WHERE executionId = @executionId;

        FETCH NEXT FROM query_cursor INTO @templateId, @queryId, @query;
    END
    CLOSE query_cursor;
    DEALLOCATE query_cursor;
END;
