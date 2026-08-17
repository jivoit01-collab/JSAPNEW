USE [jsaplive3]
GO
/****** Object:  StoredProcedure [bud].[InsertBudgetFromHANA]    Script Date: 7/9/2026 11:44:49 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [bud].[InsertBudgetFromHANA]
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Temporary table to hold HANA data
        CREATE TABLE #TempBudgetData (
            Branch NVARCHAR(10),
            DocEntry INT,
            ObjectName NVARCHAR(50),
            ObjType INT,
            LineNum INT,
            VisOrder INT,
            AcctCode NVARCHAR(50),
            AcctName NVARCHAR(255),
            CardCode NVARCHAR(50),
            CardName NVARCHAR(255),
            EFFECTMONTH NVARCHAR(10),
            BUDGET NVARCHAR(50),
            SUB_BUDGET NVARCHAR(50),
            STATE NVARCHAR(10),
            DocDate DATETIME2,
            CreateDate DATETIME2,
            AMOUNT DECIMAL(18, 2),
            CURRENTMONTH NVARCHAR(10),
            Current_month_Posted_Amount DECIMAL(18, 2),
            Budget_Owner NVARCHAR(100),
            OwnerCode NVARCHAR(50),
            [Approver Name] NVARCHAR(100),
            ApprovalCode NVARCHAR(50),
            Current_month_Budget NVARCHAR(50),
            [Status] NVARCHAR(10),
            U_NAME NVARCHAR(100),
            CreatedDate DATETIME2,
            CreateTime NVARCHAR(50),
            LineRemarks NVARCHAR(MAX),
            Comments NVARCHAR(MAX),
            ProcesStat NVARCHAR(10),
            UpdateDate DATETIME2,
            OcrCode NVARCHAR(50),
            ACOMMENT NVARCHAR(MAX),
            VCOMMENT NVARCHAR(MAX),
            VerifiedStatus NVARCHAR(10),
            ApprovedStatus NVARCHAR(10)
        );
        
        -- Step 1: Insert data from HANA to temp table
        INSERT INTO #TempBudgetData
        EXEC ('CALL "JIVO_OIL_HANADB"."DRAFT_APPROVAL"') AT HANADB112;
        
        -- Step 2: Insert into bud.jsBudgetTable
        INSERT INTO bud.jsBudgetTable (
            Branch, DocEntry, ObjectName, ObjType, LineNum, VisOrder,
            AcctCode, AcctName, CardCode, CardName, EFFECTMONTH, BUDGET,
            SUB_BUDGET, STATE, DocDate, CreateDate, AMOUNT, CURRENTMONTH,
            Current_month_Posted_Amount, Budget_Owner, OwnerCode, [Approver Name],
            ApprovalCode, Current_month_Budget, [Status], U_NAME, CreatedDate,
            CreateTime, LineRemarks, Comments, ProcesStat, UpdateDate, OcrCode,
            ACOMMENT, VCOMMENT, VerifiedStatus, ApprovedStatus, [flag], budgetDate
        )
        SELECT
            Branch, DocEntry, ObjectName, ObjType, LineNum, VisOrder,
            AcctCode, AcctName, CardCode, CardName, EFFECTMONTH, BUDGET,
            SUB_BUDGET, STATE, DocDate, CreateDate, AMOUNT, CURRENTMONTH,
            Current_month_Posted_Amount, Budget_Owner, OwnerCode, [Approver Name],
            ApprovalCode, Current_month_Budget, [Status], U_NAME, CreatedDate,
            CreateTime, LineRemarks, Comments, ProcesStat, UpdateDate, OcrCode,
            ACOMMENT, VCOMMENT, VerifiedStatus, ApprovedStatus,
            'A' AS [flag],
            GETDATE() AS budgetDate
        FROM #TempBudgetData;
        
        -- Step 3: First, let's create the table in HANA if it doesn't exist
        BEGIN TRY
            EXEC ('DROP TABLE "JIVO_OIL_HANADB"."tbl_Draft_Approvals"') AT HANADB112;
        END TRY
        BEGIN CATCH
            -- Table doesn't exist, that's fine
        END CATCH
        
        -- Create the table with exact column names
        EXEC ('
        CREATE COLUMN TABLE "JIVO_OIL_HANADB"."tbl_Draft_Approvals" (
            "Branch" NVARCHAR(10),
            "DocEntry" INTEGER,
            "ObjectName" NVARCHAR(50),
            "ObjType" INTEGER,
            "LineNum" INTEGER,
            "VisOrder" INTEGER,
            "AcctCode" NVARCHAR(50),
            "AcctName" NVARCHAR(255),
            "CardCode" NVARCHAR(50),
            "CardName" NVARCHAR(255),
            "EFFECTMONTH" NVARCHAR(10),
            "BUDGET" NVARCHAR(50),
            "SUB_BUDGET" NVARCHAR(50),
            "STATE" NVARCHAR(10),
            "DocDate" TIMESTAMP,
            "CreateDate" TIMESTAMP,
            "AMOUNT" DECIMAL(18, 2),
            "CURRENTMONTH" NVARCHAR(10),
            "Current_month_Posted_Amount" DECIMAL(18, 2),
            "Budget_Owner" NVARCHAR(100),
            "OwnerCode" NVARCHAR(50),
            "Approver_Name" NVARCHAR(100),
            "ApprovalCode" NVARCHAR(50),
            "Current_month_Budget" NVARCHAR(50),
            "Status" NVARCHAR(10),
            "U_NAME" NVARCHAR(100),
            "CreatedDate" TIMESTAMP,
            "CreateTime" NVARCHAR(50),
            "LineRemarks" NVARCHAR(5000),
            "Comments" NVARCHAR(5000),
            "ProcesStat" NVARCHAR(10),
            "UpdateDate" TIMESTAMP,
            "OcrCode" NVARCHAR(50),
            "ACOMMENT" NVARCHAR(5000),
            "VCOMMENT" NVARCHAR(5000),
            "VerifiedStatus" NVARCHAR(10),
            "ApprovedStatus" NVARCHAR(10)
        )') AT HANADB112;
        
        -- Now insert data using exact column names
        DECLARE @sql NVARCHAR(MAX);
        DECLARE @Branch NVARCHAR(10), @DocEntry INT, @ObjectName NVARCHAR(50),
                @ObjType INT, @LineNum INT, @VisOrder INT,
                @AcctCode NVARCHAR(50), @AcctName NVARCHAR(255),
                @CardCode NVARCHAR(50), @CardName NVARCHAR(255),
                @EFFECTMONTH NVARCHAR(10), @BUDGET NVARCHAR(50),
                @SUB_BUDGET NVARCHAR(50), @STATE NVARCHAR(10),
                @DocDate DATETIME2, @CreateDate DATETIME2,
                @AMOUNT DECIMAL(18, 2), @CURRENTMONTH NVARCHAR(10),
                @Current_month_Posted_Amount DECIMAL(18, 2),
                @Budget_Owner NVARCHAR(100), @OwnerCode NVARCHAR(50),
                @Approver_Name NVARCHAR(100), @ApprovalCode NVARCHAR(50),
                @Current_month_Budget NVARCHAR(50), @Status NVARCHAR(10),
                @U_NAME NVARCHAR(100), @CreatedDate DATETIME2,
                @CreateTime NVARCHAR(50), @LineRemarks NVARCHAR(4000),
                @Comments NVARCHAR(4000), @ProcesStat NVARCHAR(10),
                @UpdateDate DATETIME2, @OcrCode NVARCHAR(50),
                @ACOMMENT NVARCHAR(4000), @VCOMMENT NVARCHAR(4000),
                @VerifiedStatus NVARCHAR(10), @ApprovedStatus NVARCHAR(10);
        
        DECLARE data_cursor CURSOR FOR
            SELECT Branch, DocEntry, ObjectName, ObjType, LineNum, VisOrder,
                   AcctCode, AcctName, CardCode, CardName, EFFECTMONTH, BUDGET,
                   SUB_BUDGET, STATE, DocDate, CreateDate, AMOUNT, CURRENTMONTH,
                   Current_month_Posted_Amount, Budget_Owner, OwnerCode, [Approver Name],
                   ApprovalCode, Current_month_Budget, [Status], U_NAME, CreatedDate,
                   CreateTime, 
                   LEFT(ISNULL(LineRemarks, ''), 4000),
                   LEFT(ISNULL(Comments, ''), 4000),
                   ProcesStat, UpdateDate, OcrCode,
                   LEFT(ISNULL(ACOMMENT, ''), 4000),
                   LEFT(ISNULL(VCOMMENT, ''), 4000),
                   VerifiedStatus, ApprovedStatus
            FROM #TempBudgetData;
        
        OPEN data_cursor;
        FETCH NEXT FROM data_cursor INTO 
            @Branch, @DocEntry, @ObjectName, @ObjType, @LineNum, @VisOrder,
            @AcctCode, @AcctName, @CardCode, @CardName, @EFFECTMONTH, @BUDGET,
            @SUB_BUDGET, @STATE, @DocDate, @CreateDate, @AMOUNT, @CURRENTMONTH,
            @Current_month_Posted_Amount, @Budget_Owner, @OwnerCode, @Approver_Name,
            @ApprovalCode, @Current_month_Budget, @Status, @U_NAME, @CreatedDate,
            @CreateTime, @LineRemarks, @Comments, @ProcesStat, @UpdateDate, @OcrCode,
            @ACOMMENT, @VCOMMENT, @VerifiedStatus, @ApprovedStatus;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Build dynamic SQL with proper NULL handling
            SET @sql = N'INSERT INTO "JIVO_OIL_HANADB"."tbl_Draft_Approvals" VALUES (' +
                CASE WHEN @Branch IS NULL THEN 'NULL' ELSE '''' + @Branch + '''' END + ', ' +
                ISNULL(CAST(@DocEntry AS NVARCHAR(20)), 'NULL') + ', ' +
                CASE WHEN @ObjectName IS NULL THEN 'NULL' ELSE '''' + @ObjectName + '''' END + ', ' +
                ISNULL(CAST(@ObjType AS NVARCHAR(20)), 'NULL') + ', ' +
                ISNULL(CAST(@LineNum AS NVARCHAR(20)), 'NULL') + ', ' +
                ISNULL(CAST(@VisOrder AS NVARCHAR(20)), 'NULL') + ', ' +
                CASE WHEN @AcctCode IS NULL THEN 'NULL' ELSE '''' + @AcctCode + '''' END + ', ' +
                CASE WHEN @AcctName IS NULL THEN 'NULL' ELSE '''' + REPLACE(@AcctName, '''', '''''') + '''' END + ', ' +
                CASE WHEN @CardCode IS NULL THEN 'NULL' ELSE '''' + @CardCode + '''' END + ', ' +
                CASE WHEN @CardName IS NULL THEN 'NULL' ELSE '''' + REPLACE(@CardName, '''', '''''') + '''' END + ', ' +
                CASE WHEN @EFFECTMONTH IS NULL THEN 'NULL' ELSE '''' + @EFFECTMONTH + '''' END + ', ' +
                CASE WHEN @BUDGET IS NULL THEN 'NULL' ELSE '''' + @BUDGET + '''' END + ', ' +
                CASE WHEN @SUB_BUDGET IS NULL THEN 'NULL' ELSE '''' + @SUB_BUDGET + '''' END + ', ' +
                CASE WHEN @STATE IS NULL THEN 'NULL' ELSE '''' + @STATE + '''' END + ', ' +
                CASE WHEN @DocDate IS NULL THEN 'NULL' ELSE '''' + CONVERT(NVARCHAR(30), @DocDate, 120) + '''' END + ', ' +
                CASE WHEN @CreateDate IS NULL THEN 'NULL' ELSE '''' + CONVERT(NVARCHAR(30), @CreateDate, 120) + '''' END + ', ' +
                ISNULL(CAST(@AMOUNT AS NVARCHAR(50)), 'NULL') + ', ' +
                CASE WHEN @CURRENTMONTH IS NULL THEN 'NULL' ELSE '''' + @CURRENTMONTH + '''' END + ', ' +
                ISNULL(CAST(@Current_month_Posted_Amount AS NVARCHAR(50)), 'NULL') + ', ' +
                CASE WHEN @Budget_Owner IS NULL THEN 'NULL' ELSE '''' + REPLACE(@Budget_Owner, '''', '''''') + '''' END + ', ' +
                CASE WHEN @OwnerCode IS NULL THEN 'NULL' ELSE '''' + @OwnerCode + '''' END + ', ' +
                CASE WHEN @Approver_Name IS NULL THEN 'NULL' ELSE '''' + REPLACE(@Approver_Name, '''', '''''') + '''' END + ', ' +
                CASE WHEN @ApprovalCode IS NULL THEN 'NULL' ELSE '''' + @ApprovalCode + '''' END + ', ' +
                CASE WHEN @Current_month_Budget IS NULL THEN 'NULL' ELSE '''' + @Current_month_Budget + '''' END + ', ' +
                CASE WHEN @Status IS NULL THEN 'NULL' ELSE '''' + @Status + '''' END + ', ' +
                CASE WHEN @U_NAME IS NULL THEN 'NULL' ELSE '''' + REPLACE(@U_NAME, '''', '''''') + '''' END + ', ' +
                CASE WHEN @CreatedDate IS NULL THEN 'NULL' ELSE '''' + CONVERT(NVARCHAR(30), @CreatedDate, 120) + '''' END + ', ' +
                CASE WHEN @CreateTime IS NULL THEN 'NULL' ELSE '''' + @CreateTime + '''' END + ', ' +
                CASE WHEN @LineRemarks IS NULL THEN 'NULL' ELSE '''' + REPLACE(@LineRemarks, '''', '''''') + '''' END + ', ' +
                CASE WHEN @Comments IS NULL THEN 'NULL' ELSE '''' + REPLACE(@Comments, '''', '''''') + '''' END + ', ' +
                CASE WHEN @ProcesStat IS NULL THEN 'NULL' ELSE '''' + @ProcesStat + '''' END + ', ' +
                CASE WHEN @UpdateDate IS NULL THEN 'NULL' ELSE '''' + CONVERT(NVARCHAR(30), @UpdateDate, 120) + '''' END + ', ' +
                CASE WHEN @OcrCode IS NULL THEN 'NULL' ELSE '''' + @OcrCode + '''' END + ', ' +
                CASE WHEN @ACOMMENT IS NULL THEN 'NULL' ELSE '''' + REPLACE(@ACOMMENT, '''', '''''') + '''' END + ', ' +
                CASE WHEN @VCOMMENT IS NULL THEN 'NULL' ELSE '''' + REPLACE(@VCOMMENT, '''', '''''') + '''' END + ', ' +
                CASE WHEN @VerifiedStatus IS NULL THEN 'NULL' ELSE '''' + @VerifiedStatus + '''' END + ', ' +
                CASE WHEN @ApprovedStatus IS NULL THEN 'NULL' ELSE '''' + @ApprovedStatus + '''' END + ')';
            
            -- Execute the INSERT on HANA
            EXEC (@sql) AT HANADB112;
            
            FETCH NEXT FROM data_cursor INTO 
                @Branch, @DocEntry, @ObjectName, @ObjType, @LineNum, @VisOrder,
                @AcctCode, @AcctName, @CardCode, @CardName, @EFFECTMONTH, @BUDGET,
                @SUB_BUDGET, @STATE, @DocDate, @CreateDate, @AMOUNT, @CURRENTMONTH,
                @Current_month_Posted_Amount, @Budget_Owner, @OwnerCode, @Approver_Name,
                @ApprovalCode, @Current_month_Budget, @Status, @U_NAME, @CreatedDate,
                @CreateTime, @LineRemarks, @Comments, @ProcesStat, @UpdateDate, @OcrCode,
                @ACOMMENT, @VCOMMENT, @VerifiedStatus, @ApprovedStatus;
        END
        
        CLOSE data_cursor;
        DEALLOCATE data_cursor;
        
        DROP TABLE #TempBudgetData;
        
    END TRY
    BEGIN CATCH
        -- Clean up
        IF CURSOR_STATUS('global', 'data_cursor') >= -1
        BEGIN
            IF CURSOR_STATUS('global', 'data_cursor') >= 0
                CLOSE data_cursor;
            DEALLOCATE data_cursor;
        END
        
        IF OBJECT_ID('tempdb..#TempBudgetData') IS NOT NULL
            DROP TABLE #TempBudgetData;
            
        DECLARE @ErrMsg NVARCHAR(MAX) = ERROR_MESSAGE();
        DECLARE @ErrLine INT = ERROR_LINE();
        RAISERROR('Error in InsertBudgetFromHANA at line %d: %s', 16, 1, @ErrLine, @ErrMsg);
    END CATCH
END;
