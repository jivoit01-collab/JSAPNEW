/*
Permanent budget workflow fix

Problem:
Same SAP DocEntry can exist in more than one SAP company database.
Budget workflow must identify rows by:
Branch/company + DocEntry + ObjType + LineNum + VisOrder

This patch fixes:
1. bud.jsGetHanaConfiguration accepts branch names as well as company ids.
2. bud.jsExecuteBudgetQueries creates workflow rows only for missing branch/doc/line identities.
3. bud.jsExecuteBudgetQueries syncs HANA workflow detail to HANADB112, not missing HANA112.
4. bud.jsSyncBudgetToHanaDraftApproval updates each HANA schema based on detail row branch.
*/

CREATE OR ALTER PROCEDURE [bud].[jsGetHanaConfiguration]
    @company VARCHAR(MAX) = '1',
    @serverName NVARCHAR(100) OUTPUT,
    @schemaName NVARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @normalizedCompany VARCHAR(50) = UPPER(LTRIM(RTRIM(ISNULL(@company, '1'))));

    SELECT @serverName = ConfigValue
    FROM jsConfiguration
    WHERE ConfigKey = 'ServerName';

    IF @normalizedCompany IN ('1', 'OIL')
    BEGIN
        SELECT @schemaName = ConfigValue
        FROM jsConfiguration
        WHERE ConfigKey = 'OilDatabase';
    END
    ELSE IF @normalizedCompany IN ('2', 'BEVERAGE', 'BEVERAGES')
    BEGIN
        SELECT @schemaName = ConfigValue
        FROM jsConfiguration
        WHERE ConfigKey = 'BeverageDatabase';
    END
    ELSE IF @normalizedCompany IN ('3', 'MART')
    BEGIN
        SELECT @schemaName = ConfigValue
        FROM jsConfiguration
        WHERE ConfigKey = 'MartDatabase';
    END
    ELSE
    BEGIN
        SELECT @schemaName = ConfigValue
        FROM jsConfiguration
        WHERE ConfigKey = 'OilDatabase';
    END
END;
GO

CREATE OR ALTER PROCEDURE [bud].[jsExecuteBudgetQueries]
    @inputTable QueryTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;

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
    DECLARE @serverName NVARCHAR(100);
    DECLARE @schemaName NVARCHAR(100);
    DECLARE @branchCompanyCode VARCHAR(10);
    DECLARE @HanaDetailInsertQuery NVARCHAR(MAX);

    DECLARE query_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT tempId, queryId, query
    FROM @inputTable;

    OPEN query_cursor;
    FETCH NEXT FROM query_cursor INTO @templateId, @queryId, @query;

    WHILE @@FETCH_STATUS = 0
    BEGIN
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

        EXEC sp_executesql @query;

        DECLARE rec_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT DocEntry, Branch, ObjType, CURRENTMONTH
        FROM bud.TempResults
        WHERE flag = 'A'
          AND ProcesStat = 'Y'
          AND executionId = @executionId;

        OPEN rec_cursor;
        FETCH NEXT FROM rec_cursor INTO @docEntry, @branch, @objType, @CURRENTMONTH;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @totalStage = COUNT(*)
            FROM jsStageTemplate
            WHERE templateId = @templateId;

            SELECT @currentStage = stageId
            FROM jsStageTemplate
            WHERE templateId = @templateId
              AND priority = 1;

            SET @branchCompanyCode =
                CASE
                    WHEN UPPER(LTRIM(RTRIM(ISNULL(@branch, '')))) = 'OIL' THEN '1'
                    WHEN UPPER(LTRIM(RTRIM(ISNULL(@branch, '')))) IN ('BEVERAGE', 'BEVERAGES') THEN '2'
                    WHEN UPPER(LTRIM(RTRIM(ISNULL(@branch, '')))) = 'MART' THEN '3'
                    ELSE '1'
                END;

            EXEC [bud].[jsGetHanaConfiguration]
                @branchCompanyCode,
                @serverName OUTPUT,
                @schemaName OUTPUT;

            IF EXISTS (
                SELECT 1
                FROM bud.TempResults tr
                WHERE tr.DocEntry = @docEntry
                  AND tr.Branch = @branch
                  AND tr.ObjType = @objType
                  AND tr.flag = 'A'
                  AND tr.ProcesStat = 'Y'
                  AND tr.executionId = @executionId
                  AND NOT EXISTS (
                      SELECT 1
                      FROM bud.jsDocEntry jd
                      JOIN bud.jsDocEnrtyDetail d
                          ON d.docId = jd.id
                      WHERE jd.docEntry = tr.DocEntry
                        AND jd.templateId = @templateId
                        AND d.company = tr.Branch
                        AND d.objType = tr.ObjType
                        AND d.lineNum = tr.LineNum
                        AND d.visOrder = tr.VisOrder
                  )
            )
            BEGIN
                DECLARE @newDocIdTable TABLE (id INT);

                INSERT INTO bud.jsDocEntry (
                    docEntry, status, currentStageId, templateId, totalStage, currentSatge, date
                )
                OUTPUT INSERTED.id INTO @newDocIdTable(id)
                VALUES (
                    @docEntry, 'P', @currentStage, @templateId, @totalStage, 1, @CURRENTMONTH
                );

                SELECT @newDocId = id
                FROM @newDocIdTable;

                INSERT INTO bud.jsDocEnrtyDetail (
                    objType, company, lineNum, visOrder, docId
                )
                SELECT
                    tr.ObjType,
                    tr.Branch,
                    tr.LineNum,
                    tr.VisOrder,
                    @newDocId
                FROM bud.TempResults tr
                WHERE tr.DocEntry = @docEntry
                  AND tr.Branch = @branch
                  AND tr.ObjType = @objType
                  AND tr.flag = 'A'
                  AND tr.ProcesStat = 'Y'
                  AND tr.executionId = @executionId
                  AND NOT EXISTS (
                      SELECT 1
                      FROM bud.jsDocEntry jd
                      JOIN bud.jsDocEnrtyDetail d
                          ON d.docId = jd.id
                      WHERE jd.docEntry = tr.DocEntry
                        AND jd.templateId = @templateId
                        AND d.company = tr.Branch
                        AND d.objType = tr.ObjType
                        AND d.lineNum = tr.LineNum
                        AND d.visOrder = tr.VisOrder
                  );

                EXEC [bud].[jsAddDocEntryToSap]
                    @docEntry = @docEntry,
                    @status = 'P',
                    @currentStage = @currentStage,
                    @templateId = @templateId,
                    @totalStage = @totalStage,
                    @company = @branchCompanyCode,
                    @newDocId = @newDocId;

                CREATE TABLE #TempDetails (
                    ObjType NVARCHAR(50),
                    Company NVARCHAR(100),
                    LineNum INT,
                    VisOrder INT
                );

                INSERT INTO #TempDetails (ObjType, Company, LineNum, VisOrder)
                SELECT d.objType, d.company, d.lineNum, d.visOrder
                FROM bud.jsDocEnrtyDetail d
                WHERE d.docId = @newDocId;

                DECLARE @ObjTypeValue NVARCHAR(50), @CompanyValue NVARCHAR(100), @LineNumValue INT, @VisOrderValue INT;

                DECLARE detail_cursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT ObjType, Company, LineNum, VisOrder
                FROM #TempDetails;

                OPEN detail_cursor;
                FETCH NEXT FROM detail_cursor INTO @ObjTypeValue, @CompanyValue, @LineNumValue, @VisOrderValue;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @HanaDetailInsertQuery = 'INSERT INTO "' + @schemaName + '"."jsDocEntryDetail" ' +
                        '("objType", "company", "lineNum", "visOrder", "docId") VALUES (' +
                        '''' + REPLACE(@ObjTypeValue, '''', '''''') + ''', ' +
                        '''' + REPLACE(@CompanyValue, '''', '''''') + ''', ' +
                        CAST(@LineNumValue AS VARCHAR(20)) + ', ' +
                        CAST(@VisOrderValue AS VARCHAR(20)) + ', ' +
                        CAST(@newDocId AS VARCHAR(20)) + ')';

                    BEGIN TRY
                        EXEC (@HanaDetailInsertQuery) AT HANADB112;
                    END TRY
                    BEGIN CATCH
                        INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
                        VALUES (GETDATE(), ERROR_MESSAGE(), 'jsExecuteBudgetQueries-DetailInsert', @HanaDetailInsertQuery);
                    END CATCH;

                    FETCH NEXT FROM detail_cursor INTO @ObjTypeValue, @CompanyValue, @LineNumValue, @VisOrderValue;
                END

                CLOSE detail_cursor;
                DEALLOCATE detail_cursor;

                DROP TABLE #TempDetails;
                DELETE FROM @newDocIdTable;
            END

            FETCH NEXT FROM rec_cursor INTO @docEntry, @branch, @objType, @CURRENTMONTH;
        END

        CLOSE rec_cursor;
        DEALLOCATE rec_cursor;

        DELETE FROM bud.TempResults
        WHERE executionId = @executionId;

        FETCH NEXT FROM query_cursor INTO @templateId, @queryId, @query;
    END

    CLOSE query_cursor;
    DEALLOCATE query_cursor;
END;
GO

CREATE OR ALTER PROCEDURE [bud].[jsSyncBudgetToHanaDraftApproval]
    @docId INT,
    @company VARCHAR(MAX),
    @status CHAR(1),
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
    DECLARE @detailCompany NVARCHAR(100);
    DECLARE @branchCompanyCode VARCHAR(10);
    DECLARE @safeRemarks NVARCHAR(MAX) = REPLACE(ISNULL(@remarks, ''), '''', '''''');

    DECLARE branch_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT d.company
    FROM bud.jsDocEnrtyDetail d
    WHERE d.docId = @docId;

    OPEN branch_cursor;
    FETCH NEXT FROM branch_cursor INTO @detailCompany;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @branchCompanyCode =
            CASE
                WHEN UPPER(LTRIM(RTRIM(ISNULL(@detailCompany, '')))) = 'OIL' THEN '1'
                WHEN UPPER(LTRIM(RTRIM(ISNULL(@detailCompany, '')))) IN ('BEVERAGE', 'BEVERAGES') THEN '2'
                WHEN UPPER(LTRIM(RTRIM(ISNULL(@detailCompany, '')))) = 'MART' THEN '3'
                ELSE UPPER(LTRIM(RTRIM(ISNULL(@company, '1'))))
            END;

        EXEC [bud].[jsGetHanaConfiguration]
            @branchCompanyCode,
            @serverName OUTPUT,
            @schemaName OUTPUT;

        SELECT @rowCount = COUNT(*)
        FROM bud.jsDocEnrtyDetail d
        WHERE d.docId = @docId
          AND d.company = @detailCompany;

        IF @rowCount > 0
        BEGIN
            SELECT @HanaQuery = STUFF((
                SELECT '; ' +
                    'UPDATE "' + @schemaName + '"."tbl_Draft_Approvals" SET ' +
                    '"ApprovedStatus" = ''' + @status + ''', ' +
                    '"ACOMMENT" = ''' + @safeRemarks + ''' ' +
                    'WHERE "DocEntry" = ' + CAST(e.docEntry AS VARCHAR(20)) + ' ' +
                    'AND "ObjType" = ''' + REPLACE(d.objType, '''', '''''') + ''' ' +
                    'AND "LineNum" = ' + CAST(d.lineNum AS VARCHAR(20)) + ' ' +
                    'AND "VisOrder" = ' + CAST(d.visOrder AS VARCHAR(20))
                FROM bud.jsDocEnrtyDetail d
                JOIN bud.jsDocEntry e
                    ON d.docId = e.id
                WHERE d.docId = @docId
                  AND d.company = @detailCompany
                ORDER BY d.lineNum, d.visOrder
                FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)'), 1, 2, '');

            INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
            VALUES (
                GETDATE(),
                'Built HANA Batch Query for ' + CAST(@rowCount AS VARCHAR(20)) + ' lines in ' + @detailCompany,
                'jsSyncBudgetToHanaDraftApproval',
                LEFT(@HanaQuery, 4000)
            );

            BEGIN TRY
                SET @HanaQuery = 'DO BEGIN ' + @HanaQuery + '; END';
                SET @execCmd = 'EXEC (''' + REPLACE(@HanaQuery, '''', '''''') + ''') AT HANADB112';

                EXEC (@execCmd);

                INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
                VALUES (
                    GETDATE(),
                    'Batch Executed Successfully for ' + CAST(@rowCount AS VARCHAR(20)) + ' lines in ' + @detailCompany,
                    'jsSyncBudgetToHanaDraftApproval',
                    'Batch update completed'
                );
            END TRY
            BEGIN CATCH
                INSERT INTO bud.jsSyncErrors (ErrorTime, ErrorMessage, ProcedureName, Query)
                VALUES (
                    GETDATE(),
                    ERROR_MESSAGE(),
                    'jsSyncBudgetToHanaDraftApproval ERROR',
                    LEFT(ISNULL(@execCmd, @HanaQuery), 4000)
                );
            END CATCH;
        END

        FETCH NEXT FROM branch_cursor INTO @detailCompany;
    END

    CLOSE branch_cursor;
    DEALLOCATE branch_cursor;
END;
GO
