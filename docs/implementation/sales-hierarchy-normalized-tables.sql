USE [jsap_test]
GO

/*
    Sales hierarchy normalized table structure
    Generated: 2026-06-02

    Purpose:
    - Replace the old flat Hie.SalesHierarchy table.
    - Keep HO employee data separate from sales hierarchy person data.
    - Store H1/H2/H3/H4/EMP persons once, then store reporting by parent-child nodes.
    - Reuse existing Hie.Departments, Hie.SalesStates, Hie.SalesGroups,
      and Hie.SalesDesignations tables.

    Important:
    - This script drops the old Hie.SalesHierarchy table if it exists.
    - Take/export backup of old Hie.SalesHierarchy before running this in production.
    - Application import/list/report code must be updated before using this structure live.
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'Hie') IS NULL
    BEGIN
        EXEC(N'CREATE SCHEMA Hie');
    END;

    -------------------------------------------------------------------------
    -- Drop new dependent tables first if the script is re-run.
    -------------------------------------------------------------------------
    IF OBJECT_ID(N'Hie.SalesPersonProfile', N'U') IS NOT NULL
        DROP TABLE Hie.SalesPersonProfile;

    IF OBJECT_ID(N'Hie.SalesEmployeeHierarchyMap', N'U') IS NOT NULL
        DROP TABLE Hie.SalesEmployeeHierarchyMap;

    IF OBJECT_ID(N'Hie.SalesHierarchyNodes', N'U') IS NOT NULL
        DROP TABLE Hie.SalesHierarchyNodes;

    IF OBJECT_ID(N'Hie.SalesHierarchyLevels', N'U') IS NOT NULL
        DROP TABLE Hie.SalesHierarchyLevels;

    -- Old flat table/new master table name. Drop only after dependents are removed.
    IF OBJECT_ID(N'Hie.SalesHierarchy', N'U') IS NOT NULL
        DROP TABLE Hie.SalesHierarchy;

    -------------------------------------------------------------------------
    -- 1. Sales person master.
    --    H1, H2, H3, H4, EMP/promoters all live here.
    -------------------------------------------------------------------------
    CREATE TABLE Hie.SalesHierarchy
    (
        SalesHierarchyId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SalesHierarchy PRIMARY KEY,
        CompanyId INT NOT NULL,
        PersonCode NVARCHAR(50) NULL,
        PersonName NVARCHAR(150) NOT NULL,
        Mobile NVARCHAR(20) NULL,
        Email NVARCHAR(150) NULL,
        DateOfJoining DATE NULL,
        Qualification NVARCHAR(150) NULL,
        Gender NVARCHAR(20) NULL,
        SikhNonSikh NVARCHAR(30) NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_SalesHierarchy_IsActive DEFAULT (1),
        CreatedBy INT NULL,
        CreatedOn DATETIME2(0) NOT NULL
            CONSTRAINT DF_SalesHierarchy_CreatedOn DEFAULT (SYSDATETIME()),
        ModifiedBy INT NULL,
        ModifiedOn DATETIME2(0) NULL
    );

    CREATE UNIQUE INDEX UX_SalesHierarchy_Company_PersonCode
        ON Hie.SalesHierarchy (CompanyId, PersonCode)
        WHERE PersonCode IS NOT NULL AND PersonCode <> N'';

    CREATE INDEX IX_SalesHierarchy_Company_Name
        ON Hie.SalesHierarchy (CompanyId, PersonName);

    -------------------------------------------------------------------------
    -- 2. Hierarchy level master.
    --    Add H5/H6 in this table later without changing table structure.
    -------------------------------------------------------------------------
    CREATE TABLE Hie.SalesHierarchyLevels
    (
        SalesHierarchyLevelId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SalesHierarchyLevels PRIMARY KEY,
        CompanyId INT NOT NULL,
        LevelCode NVARCHAR(20) NOT NULL,
        LevelName NVARCHAR(100) NOT NULL,
        LevelOrder INT NOT NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_SalesHierarchyLevels_IsActive DEFAULT (1),
        CreatedBy INT NULL,
        CreatedOn DATETIME2(0) NOT NULL
            CONSTRAINT DF_SalesHierarchyLevels_CreatedOn DEFAULT (SYSDATETIME()),
        ModifiedBy INT NULL,
        ModifiedOn DATETIME2(0) NULL,
        CONSTRAINT CK_SalesHierarchyLevels_LevelOrder CHECK (LevelOrder > 0)
    );

    CREATE UNIQUE INDEX UX_SalesHierarchyLevels_Company_Code
        ON Hie.SalesHierarchyLevels (CompanyId, LevelCode);

    CREATE UNIQUE INDEX UX_SalesHierarchyLevels_Company_Order
        ON Hie.SalesHierarchyLevels (CompanyId, LevelOrder);

    INSERT INTO Hie.SalesHierarchyLevels
        (CompanyId, LevelCode, LevelName, LevelOrder, IsActive)
    VALUES
        (1, N'H1',  N'Hierarchy Level 1', 1, 1),
        (1, N'H2',  N'Hierarchy Level 2', 2, 1),
        (1, N'H3',  N'Hierarchy Level 3', 3, 1),
        (1, N'H4',  N'Hierarchy Level 4', 4, 1),
        (1, N'EMP', N'Employee/Promoter', 5, 1);

    -------------------------------------------------------------------------
    -- 3. Parent-child hierarchy nodes.
    --    One row represents one person's active position in the sales tree.
    -------------------------------------------------------------------------
    CREATE TABLE Hie.SalesHierarchyNodes
    (
        SalesHierarchyNodeId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SalesHierarchyNodes PRIMARY KEY,
        CompanyId INT NOT NULL,
        SalesHierarchyId INT NOT NULL,
        SalesHierarchyLevelId INT NOT NULL,
        ParentSalesHierarchyNodeId INT NULL,
        EffectiveFrom DATE NOT NULL
            CONSTRAINT DF_SalesHierarchyNodes_EffectiveFrom DEFAULT (CONVERT(date, GETDATE())),
        EffectiveTo DATE NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_SalesHierarchyNodes_IsActive DEFAULT (1),
        CreatedBy INT NULL,
        CreatedOn DATETIME2(0) NOT NULL
            CONSTRAINT DF_SalesHierarchyNodes_CreatedOn DEFAULT (SYSDATETIME()),
        ModifiedBy INT NULL,
        ModifiedOn DATETIME2(0) NULL,
        CONSTRAINT FK_SalesHierarchyNodes_SalesHierarchy
            FOREIGN KEY (SalesHierarchyId)
            REFERENCES Hie.SalesHierarchy (SalesHierarchyId),
        CONSTRAINT FK_SalesHierarchyNodes_Level
            FOREIGN KEY (SalesHierarchyLevelId)
            REFERENCES Hie.SalesHierarchyLevels (SalesHierarchyLevelId),
        CONSTRAINT FK_SalesHierarchyNodes_Parent
            FOREIGN KEY (ParentSalesHierarchyNodeId)
            REFERENCES Hie.SalesHierarchyNodes (SalesHierarchyNodeId),
        CONSTRAINT CK_SalesHierarchyNodes_DateRange
            CHECK (EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom),
        CONSTRAINT CK_SalesHierarchyNodes_NotSelfParent
            CHECK (ParentSalesHierarchyNodeId IS NULL OR ParentSalesHierarchyNodeId <> SalesHierarchyNodeId)
    );

    CREATE UNIQUE INDEX UX_SalesHierarchyNodes_ActivePerson
        ON Hie.SalesHierarchyNodes (CompanyId, SalesHierarchyId)
        WHERE IsActive = 1 AND EffectiveTo IS NULL;

    CREATE INDEX IX_SalesHierarchyNodes_Parent
        ON Hie.SalesHierarchyNodes (CompanyId, ParentSalesHierarchyNodeId, IsActive);

    CREATE INDEX IX_SalesHierarchyNodes_Level
        ON Hie.SalesHierarchyNodes (CompanyId, SalesHierarchyLevelId, IsActive);

    -------------------------------------------------------------------------
    -- 4. Sales profile/details.
    --    Sales-specific fields stay out of the person master.
    -------------------------------------------------------------------------
    CREATE TABLE Hie.SalesPersonProfile
    (
        SalesPersonProfileId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SalesPersonProfile PRIMARY KEY,
        CompanyId INT NOT NULL,
        SalesHierarchyId INT NOT NULL,
        DepartmentId INT NULL,
        StateId INT NULL,
        Area NVARCHAR(150) NULL,
        GroupId INT NULL,
        DesignationId INT NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_SalesPersonProfile_IsActive DEFAULT (1),
        CreatedBy INT NULL,
        CreatedOn DATETIME2(0) NOT NULL
            CONSTRAINT DF_SalesPersonProfile_CreatedOn DEFAULT (SYSDATETIME()),
        ModifiedBy INT NULL,
        ModifiedOn DATETIME2(0) NULL,
        CONSTRAINT FK_SalesPersonProfile_SalesHierarchy
            FOREIGN KEY (SalesHierarchyId)
            REFERENCES Hie.SalesHierarchy (SalesHierarchyId),
        CONSTRAINT FK_SalesPersonProfile_Department
            FOREIGN KEY (DepartmentId)
            REFERENCES Hie.Departments (DepartmentId),
        CONSTRAINT FK_SalesPersonProfile_State
            FOREIGN KEY (StateId)
            REFERENCES Hie.SalesStates (StateId),
        CONSTRAINT FK_SalesPersonProfile_Group
            FOREIGN KEY (GroupId)
            REFERENCES Hie.SalesGroups (GroupId),
        CONSTRAINT FK_SalesPersonProfile_Designation
            FOREIGN KEY (DesignationId)
            REFERENCES Hie.SalesDesignations (DesignationId)
    );

    CREATE UNIQUE INDEX UX_SalesPersonProfile_ActivePerson
        ON Hie.SalesPersonProfile (CompanyId, SalesHierarchyId)
        WHERE IsActive = 1;

    CREATE INDEX IX_SalesPersonProfile_Department
        ON Hie.SalesPersonProfile (CompanyId, DepartmentId, IsActive);

    CREATE INDEX IX_SalesPersonProfile_State
        ON Hie.SalesPersonProfile (CompanyId, StateId, IsActive);

    CREATE INDEX IX_SalesPersonProfile_Group
        ON Hie.SalesPersonProfile (CompanyId, GroupId, IsActive);

    CREATE INDEX IX_SalesPersonProfile_Designation
        ON Hie.SalesPersonProfile (CompanyId, DesignationId, IsActive);

    -------------------------------------------------------------------------
    -- 5. Excel-style employee hierarchy assignment map.
    --    One active mapping per sales employee code per company.
    --    This is the stable layer for repeated Excel uploads and page display.
    -------------------------------------------------------------------------
    CREATE TABLE Hie.SalesEmployeeHierarchyMap
    (
        SalesEmployeeHierarchyMapId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_SalesEmployeeHierarchyMap PRIMARY KEY,
        CompanyId INT NOT NULL,
        EmpPersonCode NVARCHAR(50) NOT NULL,
        H1PersonCode NVARCHAR(50) NULL,
        H2PersonCode NVARCHAR(50) NULL,
        H3PersonCode NVARCHAR(50) NULL,
        H4PersonCode NVARCHAR(50) NULL,
        DepartmentId INT NULL,
        StateId INT NULL,
        Area NVARCHAR(150) NULL,
        GroupId INT NULL,
        DesignationId INT NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_SalesEmployeeHierarchyMap_IsActive DEFAULT (1),
        CreatedBy INT NULL,
        CreatedOn DATETIME2(0) NOT NULL
            CONSTRAINT DF_SalesEmployeeHierarchyMap_CreatedOn DEFAULT (SYSDATETIME()),
        ModifiedBy INT NULL,
        ModifiedOn DATETIME2(0) NULL,
        CONSTRAINT FK_SalesEmployeeHierarchyMap_Department
            FOREIGN KEY (DepartmentId)
            REFERENCES Hie.Departments (DepartmentId),
        CONSTRAINT FK_SalesEmployeeHierarchyMap_State
            FOREIGN KEY (StateId)
            REFERENCES Hie.SalesStates (StateId),
        CONSTRAINT FK_SalesEmployeeHierarchyMap_Group
            FOREIGN KEY (GroupId)
            REFERENCES Hie.SalesGroups (GroupId),
        CONSTRAINT FK_SalesEmployeeHierarchyMap_Designation
            FOREIGN KEY (DesignationId)
            REFERENCES Hie.SalesDesignations (DesignationId),
        CONSTRAINT CK_SalesEmployeeHierarchyMap_EmpCode_NotBlank
            CHECK (LTRIM(RTRIM(EmpPersonCode)) <> N'')
    );

    CREATE UNIQUE INDEX UX_SalesEmployeeHierarchyMap_Company_EmpCode
        ON Hie.SalesEmployeeHierarchyMap (CompanyId, EmpPersonCode);

    CREATE INDEX IX_SalesEmployeeHierarchyMap_H1
        ON Hie.SalesEmployeeHierarchyMap (CompanyId, H1PersonCode, IsActive);

    CREATE INDEX IX_SalesEmployeeHierarchyMap_H2
        ON Hie.SalesEmployeeHierarchyMap (CompanyId, H2PersonCode, IsActive);

    CREATE INDEX IX_SalesEmployeeHierarchyMap_H3
        ON Hie.SalesEmployeeHierarchyMap (CompanyId, H3PersonCode, IsActive);

    CREATE INDEX IX_SalesEmployeeHierarchyMap_H4
        ON Hie.SalesEmployeeHierarchyMap (CompanyId, H4PersonCode, IsActive);

    COMMIT TRANSACTION;

    PRINT 'Sales hierarchy normalized tables created successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorNumber INT = ERROR_NUMBER();
    DECLARE @ErrorLine INT = ERROR_LINE();
    DECLARE @ThrowMessage NVARCHAR(2048) =
        CONCAT('Sales hierarchy table script failed. Error ', @ErrorNumber,
               ' at line ', @ErrorLine, ': ', @ErrorMessage);

    THROW 52000, @ThrowMessage, 1;
END CATCH;
GO

-- 6. Stored procedures for sales hierarchy upload/import.
-------------------------------------------------------------------------

CREATE OR ALTER PROCEDURE Hie.sp_UpsertSalesHierarchyPerson
    @CompanyId INT,
    @PersonCode NVARCHAR(50) = NULL,
    @PersonName NVARCHAR(150),
    @ChangedBy INT = NULL,
    @SalesHierarchyId INT OUTPUT,
    @WasCreated BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @PersonCode = NULLIF(UPPER(REPLACE(LTRIM(RTRIM(ISNULL(@PersonCode, N''))), N' ', N'')), N'');
    SET @PersonName = NULLIF(LTRIM(RTRIM(@PersonName)), N'');
    SET @WasCreated = 0;

    IF @PersonName IS NULL
        THROW 52010, 'PersonName is required.', 1;

    SELECT TOP (1)
        @SalesHierarchyId = SalesHierarchyId
    FROM Hie.SalesHierarchy
    WHERE CompanyId = @CompanyId
      AND (
            (@PersonCode IS NOT NULL AND PersonCode = @PersonCode)
         OR (@PersonCode IS NULL AND PersonCode IS NULL AND PersonName = @PersonName)
      )
    ORDER BY SalesHierarchyId DESC;

    IF @SalesHierarchyId IS NULL
    BEGIN
        INSERT INTO Hie.SalesHierarchy
            (CompanyId, PersonCode, PersonName, IsActive, CreatedBy, CreatedOn)
        VALUES
            (@CompanyId, @PersonCode, @PersonName, 1, @ChangedBy, SYSDATETIME());

        SET @SalesHierarchyId = CONVERT(INT, SCOPE_IDENTITY());
        SET @WasCreated = 1;
        RETURN;
    END;

    UPDATE Hie.SalesHierarchy
    SET PersonCode = COALESCE(@PersonCode, PersonCode),
        PersonName = @PersonName,
        IsActive = 1,
        ModifiedBy = @ChangedBy,
        ModifiedOn = SYSDATETIME()
    WHERE SalesHierarchyId = @SalesHierarchyId;
END;
GO

CREATE OR ALTER PROCEDURE Hie.sp_GetOrCreateSalesHierarchyLevel
    @CompanyId INT,
    @LevelCode NVARCHAR(20),
    @ChangedBy INT = NULL,
    @SalesHierarchyLevelId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @LevelCode = UPPER(LTRIM(RTRIM(@LevelCode)));

    SELECT @SalesHierarchyLevelId = SalesHierarchyLevelId
    FROM Hie.SalesHierarchyLevels
    WHERE CompanyId = @CompanyId
      AND LevelCode = @LevelCode;

    IF @SalesHierarchyLevelId IS NOT NULL
        RETURN;

    DECLARE @LevelOrder INT =
        CASE
            WHEN @LevelCode = N'EMP' THEN 999
            WHEN @LevelCode LIKE N'H[0-9]%' THEN TRY_CONVERT(INT, SUBSTRING(@LevelCode, 2, 10))
            ELSE 999
        END;

    INSERT INTO Hie.SalesHierarchyLevels
        (CompanyId, LevelCode, LevelName, LevelOrder, IsActive, CreatedBy, CreatedOn)
    VALUES
        (@CompanyId,
         @LevelCode,
         CASE WHEN @LevelCode = N'EMP' THEN N'Employee/Promoter' ELSE CONCAT(N'Hierarchy Level ', @LevelOrder) END,
         @LevelOrder,
         1,
         @ChangedBy,
         SYSDATETIME());

    SET @SalesHierarchyLevelId = CONVERT(INT, SCOPE_IDENTITY());
END;
GO

CREATE OR ALTER PROCEDURE Hie.sp_UpsertSalesHierarchyNode
    @CompanyId INT,
    @SalesHierarchyId INT,
    @LevelCode NVARCHAR(20),
    @ParentSalesHierarchyNodeId INT = NULL,
    @ChangedBy INT = NULL,
    @SalesHierarchyNodeId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SalesHierarchyLevelId INT;

    EXEC Hie.sp_GetOrCreateSalesHierarchyLevel
        @CompanyId = @CompanyId,
        @LevelCode = @LevelCode,
        @ChangedBy = @ChangedBy,
        @SalesHierarchyLevelId = @SalesHierarchyLevelId OUTPUT;

    SELECT TOP (1)
        @SalesHierarchyNodeId = SalesHierarchyNodeId
    FROM Hie.SalesHierarchyNodes
    WHERE CompanyId = @CompanyId
      AND SalesHierarchyId = @SalesHierarchyId
      AND IsActive = 1
      AND EffectiveTo IS NULL
    ORDER BY SalesHierarchyNodeId DESC;

    IF @SalesHierarchyNodeId IS NOT NULL
       AND EXISTS
       (
           SELECT 1
           FROM Hie.SalesHierarchyNodes
           WHERE SalesHierarchyNodeId = @SalesHierarchyNodeId
             AND SalesHierarchyLevelId = @SalesHierarchyLevelId
             AND ISNULL(ParentSalesHierarchyNodeId, 0) = ISNULL(@ParentSalesHierarchyNodeId, 0)
       )
    BEGIN
        RETURN;
    END;

    IF @SalesHierarchyNodeId IS NOT NULL
    BEGIN
        UPDATE Hie.SalesHierarchyNodes
        SET IsActive = 0,
            EffectiveTo = CONVERT(date, GETDATE()),
            ModifiedBy = @ChangedBy,
            ModifiedOn = SYSDATETIME()
        WHERE SalesHierarchyNodeId = @SalesHierarchyNodeId;
    END;

    INSERT INTO Hie.SalesHierarchyNodes
        (CompanyId, SalesHierarchyId, SalesHierarchyLevelId, ParentSalesHierarchyNodeId,
         EffectiveFrom, IsActive, CreatedBy, CreatedOn)
    VALUES
        (@CompanyId, @SalesHierarchyId, @SalesHierarchyLevelId, @ParentSalesHierarchyNodeId,
         CONVERT(date, GETDATE()), 1, @ChangedBy, SYSDATETIME());

    SET @SalesHierarchyNodeId = CONVERT(INT, SCOPE_IDENTITY());
END;
GO

CREATE OR ALTER PROCEDURE Hie.sp_UpsertSalesPersonProfile
    @CompanyId INT,
    @SalesHierarchyId INT,
    @StateName NVARCHAR(100) = NULL,
    @GroupName NVARCHAR(100) = NULL,
    @DesignationName NVARCHAR(100) = NULL,
    @Area NVARCHAR(150) = NULL,
    @ChangedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DepartmentId INT;
    DECLARE @StateId INT;
    DECLARE @GroupId INT;
    DECLARE @DesignationId INT;

    SELECT TOP (1) @DepartmentId = DepartmentId
    FROM Hie.Departments
    WHERE IsActive = 1
      AND DepartmentName = N'Frontend Sales'
    ORDER BY DepartmentId;

    SET @StateName = NULLIF(UPPER(LTRIM(RTRIM(@StateName))), N'');
    SET @GroupName = NULLIF(UPPER(LTRIM(RTRIM(@GroupName))), N'');
    SET @DesignationName = NULLIF(UPPER(LTRIM(RTRIM(@DesignationName))), N'');
    SET @Area = NULLIF(LTRIM(RTRIM(@Area)), N'');

    IF @StateName IS NOT NULL
    BEGIN
        SELECT TOP (1) @StateId = StateId
        FROM Hie.SalesStates
        WHERE UPPER(LTRIM(RTRIM(StateName))) = @StateName
        ORDER BY StateId;

        IF @StateId IS NULL
        BEGIN
            INSERT INTO Hie.SalesStates (StateName) VALUES (@StateName);
            SET @StateId = CONVERT(INT, SCOPE_IDENTITY());
        END;
    END;

    IF @GroupName IS NOT NULL
    BEGIN
        SELECT TOP (1) @GroupId = GroupId
        FROM Hie.SalesGroups
        WHERE UPPER(LTRIM(RTRIM(GroupName))) = @GroupName
        ORDER BY GroupId;

        IF @GroupId IS NULL
        BEGIN
            INSERT INTO Hie.SalesGroups (GroupName) VALUES (@GroupName);
            SET @GroupId = CONVERT(INT, SCOPE_IDENTITY());
        END;
    END;

    IF @DesignationName IS NOT NULL
    BEGIN
        SELECT TOP (1) @DesignationId = DesignationId
        FROM Hie.SalesDesignations
        WHERE UPPER(LTRIM(RTRIM(DesignationName))) = @DesignationName
        ORDER BY DesignationId;

        IF @DesignationId IS NULL
        BEGIN
            INSERT INTO Hie.SalesDesignations (DesignationName) VALUES (@DesignationName);
            SET @DesignationId = CONVERT(INT, SCOPE_IDENTITY());
        END;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM Hie.SalesPersonProfile
        WHERE CompanyId = @CompanyId
          AND SalesHierarchyId = @SalesHierarchyId
          AND IsActive = 1
    )
    BEGIN
        UPDATE Hie.SalesPersonProfile
        SET DepartmentId = @DepartmentId,
            StateId = @StateId,
            Area = @Area,
            GroupId = @GroupId,
            DesignationId = @DesignationId,
            ModifiedBy = @ChangedBy,
            ModifiedOn = SYSDATETIME()
        WHERE CompanyId = @CompanyId
          AND SalesHierarchyId = @SalesHierarchyId
          AND IsActive = 1;

        RETURN;
    END;

    INSERT INTO Hie.SalesPersonProfile
        (CompanyId, SalesHierarchyId, DepartmentId, StateId, Area, GroupId, DesignationId,
         IsActive, CreatedBy, CreatedOn)
    VALUES
        (@CompanyId, @SalesHierarchyId, @DepartmentId, @StateId, @Area, @GroupId, @DesignationId,
         1, @ChangedBy, SYSDATETIME());
END;
GO

CREATE OR ALTER PROCEDURE Hie.sp_ImportSalesHierarchyRow
    @CompanyId INT,
    @H1Code NVARCHAR(50) = NULL,
    @H1Name NVARCHAR(150) = NULL,
    @H2Code NVARCHAR(50) = NULL,
    @H2Name NVARCHAR(150) = NULL,
    @H3Code NVARCHAR(50) = NULL,
    @H3Name NVARCHAR(150) = NULL,
    @H4Code NVARCHAR(50) = NULL,
    @H4Name NVARCHAR(150) = NULL,
    @EmpCode NVARCHAR(50) = NULL,
    @EmpName NVARCHAR(150) = NULL,
    @StateName NVARCHAR(100) = NULL,
    @GroupName NVARCHAR(100) = NULL,
    @DesignationName NVARCHAR(100) = NULL,
    @Area NVARCHAR(150) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CreatedCount INT = 0;
    DECLARE @UpdatedCount INT = 0;
    DECLARE @WasCreated BIT;
    DECLARE @H1Id INT, @H2Id INT, @H3Id INT, @H4Id INT, @EmpId INT;
    DECLARE @H1NodeId INT, @H2NodeId INT, @H3NodeId INT, @H4NodeId INT, @EmpNodeId INT;

    IF NULLIF(LTRIM(RTRIM(ISNULL(@EmpCode, N''))), N'') IS NULL
       AND NULLIF(LTRIM(RTRIM(ISNULL(@EmpName, N''))), N'') IS NULL
        THROW 52020, 'EmpCode or EmpName is required.', 1;

    BEGIN TRANSACTION;

    IF NULLIF(LTRIM(RTRIM(ISNULL(@H1Code, N''))), N'') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ISNULL(@H1Name, N''))), N'') IS NOT NULL
    BEGIN
        EXEC Hie.sp_UpsertSalesHierarchyPerson @CompanyId, @H1Code, @H1Name, @CreatedBy, @H1Id OUTPUT, @WasCreated OUTPUT;
        SET @CreatedCount += CASE WHEN @WasCreated = 1 THEN 1 ELSE 0 END;
        SET @UpdatedCount += CASE WHEN @WasCreated = 0 THEN 1 ELSE 0 END;
        EXEC Hie.sp_UpsertSalesHierarchyNode @CompanyId, @H1Id, N'H1', NULL, @CreatedBy, @H1NodeId OUTPUT;
    END;

    IF NULLIF(LTRIM(RTRIM(ISNULL(@H2Code, N''))), N'') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ISNULL(@H2Name, N''))), N'') IS NOT NULL
    BEGIN
        EXEC Hie.sp_UpsertSalesHierarchyPerson @CompanyId, @H2Code, @H2Name, @CreatedBy, @H2Id OUTPUT, @WasCreated OUTPUT;
        SET @CreatedCount += CASE WHEN @WasCreated = 1 THEN 1 ELSE 0 END;
        SET @UpdatedCount += CASE WHEN @WasCreated = 0 THEN 1 ELSE 0 END;
        EXEC Hie.sp_UpsertSalesHierarchyNode @CompanyId, @H2Id, N'H2', @H1NodeId, @CreatedBy, @H2NodeId OUTPUT;
    END;

    IF NULLIF(LTRIM(RTRIM(ISNULL(@H3Code, N''))), N'') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ISNULL(@H3Name, N''))), N'') IS NOT NULL
    BEGIN
        EXEC Hie.sp_UpsertSalesHierarchyPerson @CompanyId, @H3Code, @H3Name, @CreatedBy, @H3Id OUTPUT, @WasCreated OUTPUT;
        SET @CreatedCount += CASE WHEN @WasCreated = 1 THEN 1 ELSE 0 END;
        SET @UpdatedCount += CASE WHEN @WasCreated = 0 THEN 1 ELSE 0 END;
        EXEC Hie.sp_UpsertSalesHierarchyNode @CompanyId, @H3Id, N'H3', @H2NodeId, @CreatedBy, @H3NodeId OUTPUT;
    END;

    IF NULLIF(LTRIM(RTRIM(ISNULL(@H4Code, N''))), N'') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ISNULL(@H4Name, N''))), N'') IS NOT NULL
    BEGIN
        EXEC Hie.sp_UpsertSalesHierarchyPerson @CompanyId, @H4Code, @H4Name, @CreatedBy, @H4Id OUTPUT, @WasCreated OUTPUT;
        SET @CreatedCount += CASE WHEN @WasCreated = 1 THEN 1 ELSE 0 END;
        SET @UpdatedCount += CASE WHEN @WasCreated = 0 THEN 1 ELSE 0 END;
        EXEC Hie.sp_UpsertSalesHierarchyNode @CompanyId, @H4Id, N'H4', @H3NodeId, @CreatedBy, @H4NodeId OUTPUT;
    END;

    EXEC Hie.sp_UpsertSalesHierarchyPerson @CompanyId, @EmpCode, @EmpName, @CreatedBy, @EmpId OUTPUT, @WasCreated OUTPUT;
    SET @CreatedCount += CASE WHEN @WasCreated = 1 THEN 1 ELSE 0 END;
    SET @UpdatedCount += CASE WHEN @WasCreated = 0 THEN 1 ELSE 0 END;

    EXEC Hie.sp_UpsertSalesHierarchyNode @CompanyId, @EmpId, N'EMP',
        COALESCE(@H4NodeId, @H3NodeId, @H2NodeId, @H1NodeId),
        @CreatedBy,
        @EmpNodeId OUTPUT;

    EXEC Hie.sp_UpsertSalesPersonProfile
        @CompanyId = @CompanyId,
        @SalesHierarchyId = @EmpId,
        @StateName = @StateName,
        @GroupName = @GroupName,
        @DesignationName = @DesignationName,
        @Area = @Area,
        @ChangedBy = @CreatedBy;

    DECLARE @MapEmpCode NVARCHAR(50);
    DECLARE @MapH1Code NVARCHAR(50);
    DECLARE @MapH2Code NVARCHAR(50);
    DECLARE @MapH3Code NVARCHAR(50);
    DECLARE @MapH4Code NVARCHAR(50);
    DECLARE @MapDepartmentId INT;
    DECLARE @MapStateId INT;
    DECLARE @MapGroupId INT;
    DECLARE @MapDesignationId INT;

    SELECT @MapEmpCode = PersonCode
    FROM Hie.SalesHierarchy
    WHERE SalesHierarchyId = @EmpId;

    SELECT @MapH1Code = PersonCode
    FROM Hie.SalesHierarchy
    WHERE SalesHierarchyId = @H1Id;

    SELECT @MapH2Code = PersonCode
    FROM Hie.SalesHierarchy
    WHERE SalesHierarchyId = @H2Id;

    SELECT @MapH3Code = PersonCode
    FROM Hie.SalesHierarchy
    WHERE SalesHierarchyId = @H3Id;

    SELECT @MapH4Code = PersonCode
    FROM Hie.SalesHierarchy
    WHERE SalesHierarchyId = @H4Id;

    SELECT TOP (1)
        @MapDepartmentId = DepartmentId,
        @MapStateId = StateId,
        @MapGroupId = GroupId,
        @MapDesignationId = DesignationId
    FROM Hie.SalesPersonProfile
    WHERE CompanyId = @CompanyId
      AND SalesHierarchyId = @EmpId
      AND IsActive = 1
    ORDER BY SalesPersonProfileId DESC;

    IF NULLIF(LTRIM(RTRIM(ISNULL(@MapEmpCode, N''))), N'') IS NOT NULL
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM Hie.SalesEmployeeHierarchyMap
            WHERE CompanyId = @CompanyId
              AND EmpPersonCode = @MapEmpCode
        )
        BEGIN
            UPDATE Hie.SalesEmployeeHierarchyMap
            SET H1PersonCode = @MapH1Code,
                H2PersonCode = @MapH2Code,
                H3PersonCode = @MapH3Code,
                H4PersonCode = @MapH4Code,
                DepartmentId = @MapDepartmentId,
                StateId = @MapStateId,
                Area = NULLIF(LTRIM(RTRIM(@Area)), N''),
                GroupId = @MapGroupId,
                DesignationId = @MapDesignationId,
                IsActive = 1,
                ModifiedBy = @CreatedBy,
                ModifiedOn = SYSDATETIME()
            WHERE CompanyId = @CompanyId
              AND EmpPersonCode = @MapEmpCode;
        END
        ELSE
        BEGIN
            INSERT INTO Hie.SalesEmployeeHierarchyMap
                (CompanyId, EmpPersonCode, H1PersonCode, H2PersonCode, H3PersonCode, H4PersonCode,
                 DepartmentId, StateId, Area, GroupId, DesignationId, IsActive, CreatedBy, CreatedOn)
            VALUES
                (@CompanyId, @MapEmpCode, @MapH1Code, @MapH2Code, @MapH3Code, @MapH4Code,
                 @MapDepartmentId, @MapStateId, NULLIF(LTRIM(RTRIM(@Area)), N''), @MapGroupId, @MapDesignationId,
                 1, @CreatedBy, SYSDATETIME());
        END;
    END;

    COMMIT TRANSACTION;

    SELECT
        @EmpId AS SalesHierarchyId,
        @EmpNodeId AS SalesHierarchyNodeId,
        @CreatedCount AS CreatedCount,
        @UpdatedCount AS UpdatedCount;
END;
GO

-- Verification
SELECT SalesHierarchyLevelId, CompanyId, LevelCode, LevelName, LevelOrder, IsActive
FROM Hie.SalesHierarchyLevels
ORDER BY CompanyId, LevelOrder;
GO
