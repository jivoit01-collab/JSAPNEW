/*
    BP Master portal field cleanup
    Generated: 2026-05-25

    Purpose:
    - Keep workflow, approval history, SAP status/audit, stage, and attachment tables intact.
    - Keep BP.jsMaster.isStaff.
    - Remove only retired BP business fields that are not present in the current Node-compatible SAP Portal forms.
    - Preserve active MSME fields: msmeNo, msmeType, and msmeBType.
    - Back up removed-column data before any drop.

    Rollback:
    - This script is wrapped in a transaction. Any runtime error rolls back the schema/procedure changes.
    - After a successful commit, removed-column values remain in BP.*_RemovedColumnsBackup tables by MigrationRunId.
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @MigrationRunId UNIQUEIDENTIFIER = NEWID();
DECLARE @StartedOn DATETIME2(0) = SYSUTCDATETIME();
DECLARE @sql NVARCHAR(MAX);

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT CONCAT('BP field cleanup started. MigrationRunId=', CONVERT(VARCHAR(36), @MigrationRunId));

    -------------------------------------------------------------------------
    -- 1. Permanent backup tables for rollback/audit of retired fields.
    -------------------------------------------------------------------------
    IF OBJECT_ID(N'BP.jsMaster_RemovedColumnsBackup', N'U') IS NULL
    BEGIN
        CREATE TABLE BP.jsMaster_RemovedColumnsBackup
        (
            BackupId INT IDENTITY(1,1) PRIMARY KEY,
            MigrationRunId UNIQUEIDENTIFIER NOT NULL,
            BackedUpOn DATETIME2(0) NOT NULL,
            code INT NOT NULL,
            staffCode VARCHAR(10) NULL,
            groupID VARCHAR(256) NULL,
            mainGroupID VARCHAR(256) NULL,
            chain NVARCHAR(100) NULL,
            contactPerson NVARCHAR(100) NULL,
            paymentTermID VARCHAR(50) NULL,
            creditLimit DECIMAL(18,2) NULL,
            priceList VARCHAR(100) NULL
        );
    END;

    IF OBJECT_ID(N'BP.jsMasterAddress_RemovedColumnsBackup', N'U') IS NULL
    BEGIN
        CREATE TABLE BP.jsMasterAddress_RemovedColumnsBackup
        (
            BackupId INT IDENTITY(1,1) PRIMARY KEY,
            MigrationRunId UNIQUEIDENTIFIER NOT NULL,
            BackedUpOn DATETIME2(0) NOT NULL,
            addressID INT NOT NULL,
            code INT NOT NULL,
            email NVARCHAR(100) NULL,
            isDefault BIT NULL,
            gstType VARCHAR(10) NULL,
            addressUid VARCHAR(100) NULL
        );
    END;

    IF OBJECT_ID(N'BP.jsContactPersons_RemovedColumnsBackup', N'U') IS NULL
    BEGIN
        CREATE TABLE BP.jsContactPersons_RemovedColumnsBackup
        (
            BackupId INT IDENTITY(1,1) PRIMARY KEY,
            MigrationRunId UNIQUEIDENTIFIER NOT NULL,
            BackedUpOn DATETIME2(0) NOT NULL,
            contactID INT NOT NULL,
            code INT NOT NULL,
            email NVARCHAR(100) NULL,
            phone NVARCHAR(15) NULL,
            telephone NVARCHAR(15) NULL,
            isPrimary BIT NULL,
            contactUid VARCHAR(100) NULL
        );
    END;

    IF OBJECT_ID(N'BP.jsTaxDetails_RemovedColumnsBackup', N'U') IS NULL
    BEGIN
        CREATE TABLE BP.jsTaxDetails_RemovedColumnsBackup
        (
            BackupId INT IDENTITY(1,1) PRIMARY KEY,
            MigrationRunId UNIQUEIDENTIFIER NOT NULL,
            BackedUpOn DATETIME2(0) NOT NULL,
            taxDetailID INT NOT NULL,
            code INT NOT NULL,
            msmeBusinessType NVARCHAR(100) NULL
        );
    END;

    IF OBJECT_ID(N'BP.jsBankDetails_RemovedColumnsBackup', N'U') IS NULL
    BEGIN
        CREATE TABLE BP.jsBankDetails_RemovedColumnsBackup
        (
            BackupId INT IDENTITY(1,1) PRIMARY KEY,
            MigrationRunId UNIQUEIDENTIFIER NOT NULL,
            BackedUpOn DATETIME2(0) NOT NULL,
            bankDetailID INT NOT NULL,
            code INT NOT NULL,
            countryID INT NULL,
            acctName NVARCHAR(100) NULL
        );
    END;

    SET @sql = N'
        INSERT INTO BP.jsMaster_RemovedColumnsBackup
            (MigrationRunId, BackedUpOn, code, staffCode, groupID, mainGroupID, chain, contactPerson, paymentTermID, creditLimit, priceList)
        SELECT @MigrationRunId, @StartedOn, code, ' +
        CASE WHEN COL_LENGTH(N'BP.jsMaster', N'staffCode') IS NOT NULL THEN N'staffCode' ELSE N'CAST(NULL AS VARCHAR(10))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMaster', N'groupID') IS NOT NULL THEN N'groupID' ELSE N'CAST(NULL AS VARCHAR(256))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMaster', N'mainGroupID') IS NOT NULL THEN N'mainGroupID' ELSE N'CAST(NULL AS VARCHAR(256))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMaster', N'chain') IS NOT NULL THEN N'chain' ELSE N'CAST(NULL AS NVARCHAR(100))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMaster', N'contactPerson') IS NOT NULL THEN N'contactPerson' ELSE N'CAST(NULL AS NVARCHAR(100))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMaster', N'paymentTermID') IS NOT NULL THEN N'paymentTermID' ELSE N'CAST(NULL AS VARCHAR(50))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMaster', N'creditLimit') IS NOT NULL THEN N'creditLimit' ELSE N'CAST(NULL AS DECIMAL(18,2))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMaster', N'priceList') IS NOT NULL THEN N'priceList' ELSE N'CAST(NULL AS VARCHAR(100))' END + N'
        FROM BP.jsMaster;';
    EXEC sp_executesql @sql,
        N'@MigrationRunId UNIQUEIDENTIFIER, @StartedOn DATETIME2(0)',
        @MigrationRunId = @MigrationRunId,
        @StartedOn = @StartedOn;

    SET @sql = N'
        INSERT INTO BP.jsMasterAddress_RemovedColumnsBackup
            (MigrationRunId, BackedUpOn, addressID, code, email, isDefault, gstType, addressUid)
        SELECT @MigrationRunId, @StartedOn, addressID, code, ' +
        CASE WHEN COL_LENGTH(N'BP.jsMasterAddress', N'email') IS NOT NULL THEN N'email' ELSE N'CAST(NULL AS NVARCHAR(100))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMasterAddress', N'isDefault') IS NOT NULL THEN N'isDefault' ELSE N'CAST(NULL AS BIT)' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMasterAddress', N'gstType') IS NOT NULL THEN N'gstType' ELSE N'CAST(NULL AS VARCHAR(10))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsMasterAddress', N'addressUid') IS NOT NULL THEN N'addressUid' ELSE N'CAST(NULL AS VARCHAR(100))' END + N'
        FROM BP.jsMasterAddress;';
    EXEC sp_executesql @sql,
        N'@MigrationRunId UNIQUEIDENTIFIER, @StartedOn DATETIME2(0)',
        @MigrationRunId = @MigrationRunId,
        @StartedOn = @StartedOn;

    SET @sql = N'
        INSERT INTO BP.jsContactPersons_RemovedColumnsBackup
            (MigrationRunId, BackedUpOn, contactID, code, email, phone, telephone, isPrimary, contactUid)
        SELECT @MigrationRunId, @StartedOn, contactID, code, ' +
        CASE WHEN COL_LENGTH(N'BP.jsContactPersons', N'email') IS NOT NULL THEN N'email' ELSE N'CAST(NULL AS NVARCHAR(100))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsContactPersons', N'phone') IS NOT NULL THEN N'phone' ELSE N'CAST(NULL AS NVARCHAR(15))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsContactPersons', N'telephone') IS NOT NULL THEN N'telephone' ELSE N'CAST(NULL AS NVARCHAR(15))' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsContactPersons', N'isPrimary') IS NOT NULL THEN N'isPrimary' ELSE N'CAST(NULL AS BIT)' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsContactPersons', N'contactUid') IS NOT NULL THEN N'contactUid' ELSE N'CAST(NULL AS VARCHAR(100))' END + N'
        FROM BP.jsContactPersons;';
    EXEC sp_executesql @sql,
        N'@MigrationRunId UNIQUEIDENTIFIER, @StartedOn DATETIME2(0)',
        @MigrationRunId = @MigrationRunId,
        @StartedOn = @StartedOn;

    IF COL_LENGTH(N'BP.jsTaxDetails', N'msmeBusinessType') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            INSERT INTO BP.jsTaxDetails_RemovedColumnsBackup
                (MigrationRunId, BackedUpOn, taxDetailID, code, msmeBusinessType)
            SELECT @MigrationRunId, @StartedOn, taxDetailID, code, msmeBusinessType
            FROM BP.jsTaxDetails;',
            N'@MigrationRunId UNIQUEIDENTIFIER, @StartedOn DATETIME2(0)',
            @MigrationRunId = @MigrationRunId,
            @StartedOn = @StartedOn;
    END;

    SET @sql = N'
        INSERT INTO BP.jsBankDetails_RemovedColumnsBackup
            (MigrationRunId, BackedUpOn, bankDetailID, code, countryID, acctName)
        SELECT @MigrationRunId, @StartedOn, bankDetailID, code, ' +
        CASE WHEN COL_LENGTH(N'BP.jsBankDetails', N'countryID') IS NOT NULL THEN N'countryID' ELSE N'CAST(NULL AS INT)' END + N', ' +
        CASE WHEN COL_LENGTH(N'BP.jsBankDetails', N'acctName') IS NOT NULL THEN N'acctName' ELSE N'CAST(NULL AS NVARCHAR(100))' END + N'
        FROM BP.jsBankDetails;';
    EXEC sp_executesql @sql,
        N'@MigrationRunId UNIQUEIDENTIFIER, @StartedOn DATETIME2(0)',
        @MigrationRunId = @MigrationRunId,
        @StartedOn = @StartedOn;

    -------------------------------------------------------------------------
    -- 2. Add active portal columns. Snapshot/audit tables are expanded only;
    --    no audit or workflow data is deleted.
    -------------------------------------------------------------------------
    IF COL_LENGTH(N'BP.jsMaster', N'foreignName') IS NULL
        ALTER TABLE BP.jsMaster ADD foreignName NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsMaster', N'typeOfBusiness') IS NULL
        ALTER TABLE BP.jsMaster ADD typeOfBusiness NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsMaster', N'industry') IS NULL
        ALTER TABLE BP.jsMaster ADD industry NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsMaster', N'currency') IS NULL
        ALTER TABLE BP.jsMaster ADD currency VARCHAR(10) NULL CONSTRAINT DF_jsMaster_currency DEFAULT ('INR');
    IF COL_LENGTH(N'BP.jsMaster', N'remarks') IS NULL
        ALTER TABLE BP.jsMaster ADD remarks NVARCHAR(500) NULL;

    EXEC(N'
        UPDATE BP.jsMaster
        SET currency = ISNULL(NULLIF(currency, ''''), ''INR'')
        WHERE currency IS NULL OR currency = '''';');

    IF COL_LENGTH(N'BP.jsMasterAddress', N'addressName') IS NULL
        ALTER TABLE BP.jsMasterAddress ADD addressName VARCHAR(100) NULL;

    IF COL_LENGTH(N'BP.jsMasterAddress', N'addressUid') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE BP.jsMasterAddress
            SET addressName = ISNULL(NULLIF(addressName, ''''), addressUid);');
    END;

    IF COL_LENGTH(N'BP.jsContactPersons', N'emailAddress') IS NULL
        ALTER TABLE BP.jsContactPersons ADD emailAddress NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsContactPersons', N'alternateEmail') IS NULL
        ALTER TABLE BP.jsContactPersons ADD alternateEmail NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsContactPersons', N'mobileNumber') IS NULL
        ALTER TABLE BP.jsContactPersons ADD mobileNumber NVARCHAR(15) NULL;
    IF COL_LENGTH(N'BP.jsContactPersons', N'alternateContact') IS NULL
        ALTER TABLE BP.jsContactPersons ADD alternateContact NVARCHAR(15) NULL;

    IF COL_LENGTH(N'BP.jsContactPersons', N'email') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE BP.jsContactPersons
            SET emailAddress = COALESCE(NULLIF(emailAddress, ''''), email);');
    END;

    IF COL_LENGTH(N'BP.jsContactPersons', N'phone') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE BP.jsContactPersons
            SET mobileNumber = COALESCE(NULLIF(mobileNumber, ''''), phone);');
    END;

    IF COL_LENGTH(N'BP.jsContactPersons', N'telephone') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE BP.jsContactPersons
            SET alternateContact = COALESCE(NULLIF(alternateContact, ''''), telephone);');
    END;

    IF COL_LENGTH(N'BP.jsTaxDetails', N'gstin') IS NULL
        ALTER TABLE BP.jsTaxDetails ADD gstin VARCHAR(15) NULL;
    IF COL_LENGTH(N'BP.jsTaxDetails', N'msmeType') IS NULL
        ALTER TABLE BP.jsTaxDetails ADD msmeType NVARCHAR(50) NULL;
    IF COL_LENGTH(N'BP.jsTaxDetails', N'msmeBType') IS NULL
        ALTER TABLE BP.jsTaxDetails ADD msmeBType NVARCHAR(100) NULL;

    IF COL_LENGTH(N'BP.jsTaxDetails', N'msmeBusinessType') IS NOT NULL
    BEGIN
        EXEC(N'
            UPDATE BP.jsTaxDetails
            SET msmeBType = COALESCE(NULLIF(msmeBType, ''''), msmeBusinessType)
            WHERE ISNULL(msmeBusinessType, '''') <> '''';');
    END;

    EXEC(N'
        UPDATE td
        SET gstin = COALESCE(NULLIF(td.gstin, ''''), a.gstNo)
        FROM BP.jsTaxDetails td
        OUTER APPLY
        (
            SELECT TOP (1) ma.gstNo
            FROM BP.jsMasterAddress ma
            WHERE ma.code = td.code AND ISNULL(ma.gstNo, '''') <> ''''
            ORDER BY CASE WHEN ma.addressType = ''B'' THEN 0 ELSE 1 END, ma.addressID
        ) a
        WHERE ISNULL(td.gstin, '''') = '''';');

    IF COL_LENGTH(N'BP.jsBankDetails', N'accountType') IS NULL
        ALTER TABLE BP.jsBankDetails ADD accountType NVARCHAR(50) NULL;

    IF COL_LENGTH(N'BP.jsMasterSnapshot', N'foreignName') IS NULL
        ALTER TABLE BP.jsMasterSnapshot ADD foreignName NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsMasterSnapshot', N'typeOfBusiness') IS NULL
        ALTER TABLE BP.jsMasterSnapshot ADD typeOfBusiness NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsMasterSnapshot', N'industry') IS NULL
        ALTER TABLE BP.jsMasterSnapshot ADD industry NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsMasterSnapshot', N'currency') IS NULL
        ALTER TABLE BP.jsMasterSnapshot ADD currency VARCHAR(10) NULL;
    IF COL_LENGTH(N'BP.jsMasterSnapshot', N'remarks') IS NULL
        ALTER TABLE BP.jsMasterSnapshot ADD remarks NVARCHAR(500) NULL;
    IF COL_LENGTH(N'BP.jsMasterAddressSnapshot', N'addressName') IS NULL
        ALTER TABLE BP.jsMasterAddressSnapshot ADD addressName VARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsContactPersonsSnapshot', N'emailAddress') IS NULL
        ALTER TABLE BP.jsContactPersonsSnapshot ADD emailAddress NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsContactPersonsSnapshot', N'alternateEmail') IS NULL
        ALTER TABLE BP.jsContactPersonsSnapshot ADD alternateEmail NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsContactPersonsSnapshot', N'mobileNumber') IS NULL
        ALTER TABLE BP.jsContactPersonsSnapshot ADD mobileNumber NVARCHAR(15) NULL;
    IF COL_LENGTH(N'BP.jsContactPersonsSnapshot', N'alternateContact') IS NULL
        ALTER TABLE BP.jsContactPersonsSnapshot ADD alternateContact NVARCHAR(15) NULL;
    IF COL_LENGTH(N'BP.jsTaxDetailsSnapshot', N'gstin') IS NULL
        ALTER TABLE BP.jsTaxDetailsSnapshot ADD gstin VARCHAR(15) NULL;
    IF COL_LENGTH(N'BP.jsTaxDetailsSnapshot', N'msmeType') IS NULL
        ALTER TABLE BP.jsTaxDetailsSnapshot ADD msmeType NVARCHAR(50) NULL;
    IF COL_LENGTH(N'BP.jsTaxDetailsSnapshot', N'msmeBType') IS NULL
        ALTER TABLE BP.jsTaxDetailsSnapshot ADD msmeBType NVARCHAR(100) NULL;
    IF COL_LENGTH(N'BP.jsBankDetailsSnapshot', N'accountType') IS NULL
        ALTER TABLE BP.jsBankDetailsSnapshot ADD accountType NVARCHAR(50) NULL;

    -------------------------------------------------------------------------
    -- 3. Drop procedure wrappers that reference old TVP definitions.
    -------------------------------------------------------------------------
    DROP PROCEDURE IF EXISTS BP.jsInsertBPMasterData;
    DROP PROCEDURE IF EXISTS BP.jsUpdateBPMasterData;
    DROP PROCEDURE IF EXISTS BP.jsGetAddressUid;
    DROP PROCEDURE IF EXISTS BP.jsGetContactUid;
    DROP PROCEDURE IF EXISTS BP.jsUpdateBPMasterData_DEBUG;

    IF TYPE_ID(N'BP.AddressTableType') IS NOT NULL DROP TYPE BP.AddressTableType;
    IF TYPE_ID(N'BP.ContactPersonTableType') IS NOT NULL DROP TYPE BP.ContactPersonTableType;

    EXEC(N'
        CREATE TYPE BP.AddressTableType AS TABLE
        (
            addressType CHAR(1) NOT NULL,
            street NVARCHAR(200) NOT NULL,
            blockArea NVARCHAR(200) NULL,
            state VARCHAR(50) NULL,
            city VARCHAR(50) NULL,
            pinCode VARCHAR(10) NULL,
            country VARCHAR(50) NULL,
            gstin VARCHAR(15) NULL,
            addressName VARCHAR(100) NULL
        );');

    EXEC(N'
        CREATE TYPE BP.ContactPersonTableType AS TABLE
        (
            firstName NVARCHAR(100) NULL,
            lastName NVARCHAR(100) NULL,
            designation NVARCHAR(100) NULL,
            emailAddress NVARCHAR(100) NULL,
            alternateEmail NVARCHAR(100) NULL,
            mobileNumber NVARCHAR(15) NULL,
            alternateContact NVARCHAR(15) NULL
        );');

    -------------------------------------------------------------------------
    -- 4. Recreate BP procedures with active portal fields only.
    -------------------------------------------------------------------------
    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsInsertBPMasterData
    @type CHAR(1),
    @isStaff BIT,
    @name NVARCHAR(100),
    @company INT,
    @foreignName NVARCHAR(100) = NULL,
    @typeOfBusiness NVARCHAR(100) = NULL,
    @industry NVARCHAR(100) = NULL,
    @firstName NVARCHAR(100) = NULL,
    @lastName NVARCHAR(100) = NULL,
    @designation NVARCHAR(100) = NULL,
    @mobileNo VARCHAR(15) = NULL,
    @emailAddress NVARCHAR(100) = NULL,
    @alternateEmail NVARCHAR(100) = NULL,
    @currency VARCHAR(10) = ''INR'',
    @remarks NVARCHAR(500) = NULL,
    @userId INT,
    @companyByUser VARCHAR(100),
    @tan VARCHAR(20) = NULL,
    @panNo VARCHAR(10),
    @fssaiNo VARCHAR(14) = NULL,
    @msmeNo VARCHAR(20) = NULL,
    @msmeType NVARCHAR(50) = NULL,
    @msmeBType NVARCHAR(100) = NULL,
    @gstin VARCHAR(15) = NULL,
    @addresses BP.AddressTableType READONLY,
    @bankName NVARCHAR(100) = NULL,
    @branchName NVARCHAR(100) = NULL,
    @accountNo VARCHAR(30) = NULL,
    @ifscCode VARCHAR(11) = NULL,
    @swiftCode VARCHAR(11) = NULL,
    @accountType NVARCHAR(50) = NULL,
    @contacts BP.ContactPersonTableType READONLY,
    @attachments BP.AttachmentsTableType READONLY,
    @generatedCode INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @templateId INT;
    DECLARE @currentStageId INT;
    DECLARE @totalStage INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @type NOT IN (''V'', ''C'') THROW 50001, ''Invalid BP type. Use C or V.'', 1;
        IF ISNULL(@name, '''') = '''' THROW 50002, ''Company name is required.'', 1;
        IF @company IS NULL THROW 50003, ''Company is required.'', 1;
        IF ISNULL(@panNo, '''') = '''' THROW 50004, ''PAN is required.'', 1;

        INSERT INTO BP.jsMaster
            (type, isStaff, name, company, foreignName, typeOfBusiness, industry, mobileNo, currency, remarks, userId, companyByUser)
        VALUES
            (@type, @isStaff, @name, @company, @foreignName, @typeOfBusiness, @industry, @mobileNo, ISNULL(NULLIF(@currency, ''''), ''INR''), @remarks, @userId, @companyByUser);

        SET @generatedCode = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO BP.jsTaxDetails (code, buyerTANNo, panNo, fssaiNo, msmeNo, msmeType, msmeBType, gstin)
        VALUES (@generatedCode, @tan, @panNo, @fssaiNo, @msmeNo, @msmeType, @msmeBType, @gstin);

        IF EXISTS (SELECT 1 FROM @addresses)
        BEGIN
            INSERT INTO BP.jsMasterAddress
                (code, addressType, addressLine1, addressLine2, stateID, cityID, pincode, countryID, gstNo, addressName)
            SELECT @generatedCode, addressType, street, blockArea, state, city, pinCode, country, COALESCE(NULLIF(gstin, ''''), @gstin), addressName
            FROM @addresses;
        END;

        IF EXISTS (SELECT 1 FROM @contacts)
        BEGIN
            INSERT INTO BP.jsContactPersons
                (code, firstName, lastName, designation, emailAddress, alternateEmail, mobileNumber, alternateContact)
            SELECT @generatedCode, firstName, lastName, designation, COALESCE(NULLIF(emailAddress, ''''), @emailAddress),
                   COALESCE(NULLIF(alternateEmail, ''''), @alternateEmail), COALESCE(NULLIF(mobileNumber, ''''), @mobileNo), alternateContact
            FROM @contacts;
        END
        ELSE IF ISNULL(@firstName, '''') <> '''' OR ISNULL(@lastName, '''') <> '''' OR ISNULL(@emailAddress, '''') <> '''' OR ISNULL(@mobileNo, '''') <> ''''
        BEGIN
            INSERT INTO BP.jsContactPersons
                (code, firstName, lastName, designation, emailAddress, alternateEmail, mobileNumber)
            VALUES
                (@generatedCode, @firstName, @lastName, @designation, @emailAddress, @alternateEmail, @mobileNo);
        END;

        IF @type = ''V'' AND ISNULL(@bankName, '''') <> '''' AND ISNULL(@accountNo, '''') <> ''''
        BEGIN
            INSERT INTO BP.jsBankDetails (code, name, accountNo, ifscCode, branch, swiftCode, accountType)
            VALUES (@generatedCode, @bankName, @accountNo, @ifscCode, @branchName, @swiftCode, @accountType);
        END;

        IF EXISTS (SELECT 1 FROM @attachments)
        BEGIN
            INSERT INTO BP.jsAttachments (code, fileName, filePath, fileSize, contentType, fileType)
            SELECT @generatedCode, fileName, filePath, fileSize, contentType, fileType
            FROM @attachments;
        END;

        -- Template lookup: select only an active BP-specific workflow template for this company.
        SELECT TOP 1
            @templateId = id
        FROM dbo.jsTemplate
        WHERE company = @company
          AND isActive = 1
          AND name LIKE ''%BP%''
        ORDER BY id DESC;

        IF @templateId IS NULL
            THROW 50005, ''Active BP workflow template not found for company.'', 1;

        -- Stage lookup: start approval at the first priority stage in the BP template.
        SELECT TOP 1
            @currentStageId = stageId
        FROM dbo.jsStageTemplate
        WHERE templateId = @templateId
        ORDER BY priority ASC;

        SELECT
            @totalStage = COUNT(*)
        FROM dbo.jsStageTemplate
        WHERE templateId = @templateId;

        IF @currentStageId IS NULL
            THROW 50006, ''No stages found for active BP workflow template.'', 1;

        IF ISNULL(@totalStage, 0) = 0
            THROW 50007, ''BP workflow template has no stages.'', 1;

        -- Workflow insert: creates the pending approval route used by GetPendingBP/ApproveBP/RejectBP.
        -- Guarded with UPDLOCK/HOLDLOCK because some environments already have legacy SQL workflow creation.
        IF NOT EXISTS
        (
            SELECT 1
            FROM BP.jsFlow WITH (UPDLOCK, HOLDLOCK)
            WHERE bpCode = @generatedCode
        )
        BEGIN
            INSERT INTO BP.jsFlow
                (bpCode, status, currentStageId, templateId, totalStage, currentStage, createdOn, updatedOn)
            VALUES
                (@generatedCode, ''P'', @currentStageId, @templateId, @totalStage, 1, GETDATE(), GETDATE());
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;');

    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsUpdateBPMasterData
    @code INT,
    @type CHAR(1) = NULL,
    @isStaff BIT = NULL,
    @name NVARCHAR(100) = NULL,
    @company INT = NULL,
    @foreignName NVARCHAR(100) = NULL,
    @typeOfBusiness NVARCHAR(100) = NULL,
    @industry NVARCHAR(100) = NULL,
    @firstName NVARCHAR(100) = NULL,
    @lastName NVARCHAR(100) = NULL,
    @designation NVARCHAR(100) = NULL,
    @mobileNo VARCHAR(15) = NULL,
    @emailAddress NVARCHAR(100) = NULL,
    @alternateEmail NVARCHAR(100) = NULL,
    @currency VARCHAR(10) = NULL,
    @remarks NVARCHAR(500) = NULL,
    @userId INT,
    @companyByUser VARCHAR(100),
    @tan VARCHAR(20) = NULL,
    @panNo VARCHAR(10) = NULL,
    @fssaiNo VARCHAR(14) = NULL,
    @msmeNo VARCHAR(20) = NULL,
    @msmeType NVARCHAR(50) = NULL,
    @msmeBType NVARCHAR(100) = NULL,
    @gstin VARCHAR(15) = NULL,
    @addresses BP.AddressTableType READONLY,
    @bankName NVARCHAR(100) = NULL,
    @branchName NVARCHAR(100) = NULL,
    @accountNo VARCHAR(30) = NULL,
    @ifscCode VARCHAR(11) = NULL,
    @swiftCode VARCHAR(11) = NULL,
    @accountType NVARCHAR(50) = NULL,
    @contacts BP.ContactPersonTableType READONLY,
    @attachments BP.AttachmentsTableType READONLY,
    @updateAddresses BIT = 0,
    @updateBankDetails BIT = 0,
    @updateContacts BIT = 0,
    @updateAttachments BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SessionID UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM BP.jsMaster WHERE code = @code)
            THROW 50010, ''BP master record not found.'', 1;
        IF @type IS NOT NULL AND @type NOT IN (''V'', ''C'')
            THROW 50011, ''Invalid BP type. Use C or V.'', 1;

        INSERT INTO BP.jsMasterSnapshot
            (OriginalCode, SessionID, ChangedBy, CompanyByUser, Operation, type, isStaff, name, company, mobileNo,
             userId, companyByUser_Original, foreignName, typeOfBusiness, industry, currency, remarks)
        SELECT @code, @SessionID, @userId, @companyByUser, ''BEFORE_UPDATE'', type, isStaff, name, company, mobileNo,
               userId, companyByUser, foreignName, typeOfBusiness, industry, currency, remarks
        FROM BP.jsMaster
        WHERE code = @code;

        INSERT INTO BP.jsTaxDetailsSnapshot
            (OriginalCode, SessionID, buyerTANNo, panNo, fssaiNo, msmeNo, msmeType, msmeBType, gstin)
        SELECT @code, @SessionID, buyerTANNo, panNo, fssaiNo, msmeNo, msmeType, msmeBType, gstin
        FROM BP.jsTaxDetails
        WHERE code = @code;

        INSERT INTO BP.jsMasterAddressSnapshot
            (OriginalCode, SessionID, addressType, addressLine1, addressLine2, stateID, cityID, pincode, countryID, gstNo, addressName)
        SELECT @code, @SessionID, addressType, addressLine1, addressLine2, stateID, cityID, pincode, countryID, gstNo, addressName
        FROM BP.jsMasterAddress
        WHERE code = @code;

        INSERT INTO BP.jsBankDetailsSnapshot
            (OriginalCode, SessionID, name, accountNo, ifscCode, branch, swiftCode, accountType)
        SELECT @code, @SessionID, name, accountNo, ifscCode, branch, swiftCode, accountType
        FROM BP.jsBankDetails
        WHERE code = @code;

        INSERT INTO BP.jsContactPersonsSnapshot
            (OriginalCode, SessionID, designation, firstName, lastName, emailAddress, alternateEmail, mobileNumber, alternateContact)
        SELECT @code, @SessionID, designation, firstName, lastName, emailAddress, alternateEmail, mobileNumber, alternateContact
        FROM BP.jsContactPersons
        WHERE code = @code;

        ;WITH Changes AS
        (
            SELECT ''type'' FieldName, CONVERT(NVARCHAR(4000), type) OldValue, CONVERT(NVARCHAR(4000), @type) NewValue FROM BP.jsMaster WHERE code = @code AND @type IS NOT NULL
            UNION ALL SELECT ''isStaff'', CONVERT(NVARCHAR(4000), isStaff), CONVERT(NVARCHAR(4000), @isStaff) FROM BP.jsMaster WHERE code = @code AND @isStaff IS NOT NULL
            UNION ALL SELECT ''name'', name, @name FROM BP.jsMaster WHERE code = @code AND @name IS NOT NULL
            UNION ALL SELECT ''company'', CONVERT(NVARCHAR(4000), company), CONVERT(NVARCHAR(4000), @company) FROM BP.jsMaster WHERE code = @code AND @company IS NOT NULL
            UNION ALL SELECT ''foreignName'', foreignName, @foreignName FROM BP.jsMaster WHERE code = @code AND @foreignName IS NOT NULL
            UNION ALL SELECT ''typeOfBusiness'', typeOfBusiness, @typeOfBusiness FROM BP.jsMaster WHERE code = @code AND @typeOfBusiness IS NOT NULL
            UNION ALL SELECT ''industry'', industry, @industry FROM BP.jsMaster WHERE code = @code AND @industry IS NOT NULL
            UNION ALL SELECT ''mobileNo'', mobileNo, @mobileNo FROM BP.jsMaster WHERE code = @code AND @mobileNo IS NOT NULL
            UNION ALL SELECT ''currency'', currency, @currency FROM BP.jsMaster WHERE code = @code AND @currency IS NOT NULL
            UNION ALL SELECT ''remarks'', remarks, @remarks FROM BP.jsMaster WHERE code = @code AND @remarks IS NOT NULL
        )
        INSERT INTO BP.jsAuditLog (Code, TableName, Operation, FieldName, OldValue, NewValue, ChangedBy, CompanyByUser, SessionID)
        SELECT @code, ''jsMaster'', ''UPDATE'', FieldName, OldValue, NewValue, @userId, @companyByUser, @SessionID
        FROM Changes
        WHERE ISNULL(OldValue, '''') <> ISNULL(NewValue, '''');

        UPDATE BP.jsMaster
        SET type = ISNULL(@type, type),
            isStaff = ISNULL(@isStaff, isStaff),
            name = ISNULL(@name, name),
            company = ISNULL(@company, company),
            foreignName = ISNULL(@foreignName, foreignName),
            typeOfBusiness = ISNULL(@typeOfBusiness, typeOfBusiness),
            industry = ISNULL(@industry, industry),
            mobileNo = ISNULL(@mobileNo, mobileNo),
            currency = ISNULL(NULLIF(@currency, ''''), currency),
            remarks = ISNULL(@remarks, remarks)
        WHERE code = @code;

        IF @tan IS NOT NULL OR @panNo IS NOT NULL OR @fssaiNo IS NOT NULL OR @msmeNo IS NOT NULL OR @msmeType IS NOT NULL OR @msmeBType IS NOT NULL OR @gstin IS NOT NULL
        BEGIN
            UPDATE BP.jsTaxDetails
            SET buyerTANNo = ISNULL(@tan, buyerTANNo),
                panNo = ISNULL(@panNo, panNo),
                fssaiNo = ISNULL(@fssaiNo, fssaiNo),
                msmeNo = ISNULL(@msmeNo, msmeNo),
                msmeType = ISNULL(@msmeType, msmeType),
                msmeBType = ISNULL(@msmeBType, msmeBType),
                gstin = ISNULL(@gstin, gstin)
            WHERE code = @code;
        END;

        IF @updateAddresses = 1
        BEGIN
            INSERT INTO BP.jsAuditLog (Code, TableName, Operation, FieldName, OldValue, NewValue, ChangedBy, CompanyByUser, SessionID)
            VALUES (@code, ''jsMasterAddress'', ''REPLACE_ALL'', ''TABLE_OPERATION'',
                    CONVERT(NVARCHAR(50), (SELECT COUNT(*) FROM BP.jsMasterAddress WHERE code = @code)),
                    CONVERT(NVARCHAR(50), (SELECT COUNT(*) FROM @addresses)), @userId, @companyByUser, @SessionID);

            DELETE FROM BP.jsMasterAddress WHERE code = @code;
            INSERT INTO BP.jsMasterAddress
                (code, addressType, addressLine1, addressLine2, stateID, cityID, pincode, countryID, gstNo, addressName)
            SELECT @code, addressType, street, blockArea, state, city, pinCode, country, COALESCE(NULLIF(gstin, ''''), @gstin), addressName
            FROM @addresses;
        END;

        IF @updateBankDetails = 1
        BEGIN
            DELETE FROM BP.jsBankDetails WHERE code = @code;
            IF @bankName IS NOT NULL AND @accountNo IS NOT NULL
                INSERT INTO BP.jsBankDetails (code, name, accountNo, ifscCode, branch, swiftCode, accountType)
                VALUES (@code, @bankName, @accountNo, @ifscCode, @branchName, @swiftCode, @accountType);
        END;

        IF @updateContacts = 1
        BEGIN
            DELETE FROM BP.jsContactPersons WHERE code = @code;
            INSERT INTO BP.jsContactPersons
                (code, firstName, lastName, designation, emailAddress, alternateEmail, mobileNumber, alternateContact)
            SELECT @code, firstName, lastName, designation, emailAddress, alternateEmail, mobileNumber, alternateContact
            FROM @contacts;
        END;

        IF @updateAttachments = 1
        BEGIN
            DELETE FROM BP.jsAttachments WHERE code = @code;
            INSERT INTO BP.jsAttachments (code, fileName, filePath, fileSize, contentType, fileType)
            SELECT @code, fileName, filePath, fileSize, contentType, fileType
            FROM @attachments;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;');

    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsGetSingleBPData
    @bpCode INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.code,
        m.type,
        m.isStaff,
        m.name,
        m.foreignName,
        m.typeOfBusiness,
        m.industry,
        c.firstName,
        c.lastName,
        c.designation,
        m.mobileNo AS mobileNumber,
        c.emailAddress,
        c.alternateEmail,
        m.currency,
        m.remarks,
        m.companyByUser,
        m.company,
        f.id AS flowId
    FROM BP.jsMaster AS m
    INNER JOIN BP.jsFlow AS f ON f.bpCode = m.code
    OUTER APPLY
    (
        SELECT TOP (1) firstName, lastName, designation, emailAddress, alternateEmail
        FROM BP.jsContactPersons
        WHERE code = m.code
        ORDER BY contactID
    ) AS c
    WHERE m.code = @bpCode;

    SELECT
        buyerTANNo AS tan,
        panNo AS panNumber,
        fssaiNo AS fssaiLicense,
        msmeNo AS msme,
        msmeType,
        msmeBType,
        gstin
    FROM BP.jsTaxDetails
    WHERE code = @bpCode;

    SELECT
        addressType,
        addressLine1 AS street,
        addressLine2 AS blockArea,
        stateID AS state,
        cityID AS city,
        pincode AS pinCode,
        countryID AS country,
        gstNo AS gstin,
        addressName
    FROM BP.jsMasterAddress
    WHERE code = @bpCode;

    SELECT
        name AS bankName,
        branch AS branchName,
        accountNo AS accountNumber,
        ifscCode,
        swiftCode,
        accountType
    FROM BP.jsBankDetails
    WHERE code = @bpCode;

    SELECT
        firstName,
        lastName,
        designation,
        emailAddress,
        alternateEmail,
        mobileNumber,
        alternateContact
    FROM BP.jsContactPersons
    WHERE code = @bpCode;

    SELECT fileName, filePath, fileSize, contentType, fileType
    FROM BP.jsAttachments
    WHERE code = @bpCode;
END;');

    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsGetPendingBP
    @userId INT,
    @companyId INT,
    @month VARCHAR(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH UserStages AS
    (
        SELECT DISTINCT us.stageId
        FROM dbo.jsUserStage AS us
        INNER JOIN dbo.jsStageTemplate AS st ON us.stageId = st.stageId
        INNER JOIN dbo.jsTemplate AS t ON st.templateId = t.id
        WHERE us.userId = @userId
          AND t.company = @companyId
          AND t.isActive = 1
          AND ISNULL(us.status, 1) = 1
    ),
    PendingFlows AS
    (
        SELECT f.id AS flowId, f.bpCode, f.templateId, f.currentStageId, f.currentStage, f.totalStage, f.createdOn
        FROM BP.jsFlow AS f
        INNER JOIN BP.jsMaster AS m ON m.code = f.bpCode
        INNER JOIN UserStages AS us ON us.stageId = f.currentStageId
        WHERE f.status = ''P''
          AND m.company = @companyId
          AND (@month IS NULL OR FORMAT(f.createdOn, ''MM-yyyy'') = @month)
          AND NOT EXISTS
          (
              SELECT 1
              FROM BP.jsFlowStatus fs
              WHERE fs.flowId = f.id
                AND fs.templateId = f.templateId
                AND fs.stageId = f.currentStageId
                AND fs.userId = @userId
                AND fs.status IN (''A'', ''R'')
          )
    )
    SELECT
        m.code AS code,
        m.code AS id,
        m.company AS companyId,
        m.type,
        m.name AS companyName,
        m.name AS partyName,
        m.foreignName,
        m.typeOfBusiness,
        m.industry,
        c.firstName,
        c.lastName,
        c.designation,
        m.mobileNo AS mobileNumber,
        c.emailAddress,
        c.alternateEmail,
        m.currency,
        m.remarks,
        m.isStaff,
        pf.createdOn,
        pf.flowId,
        pf.currentStage,
        pf.totalStage,
        pf.currentStageId,
        s.stage AS currentStageName,
        CAST(CASE WHEN pf.currentStage = pf.totalStage THEN 1 ELSE 0 END AS BIT) AS isFinalStage,
        sd.apiStatusTag,
        CASE WHEN sd.apiStatusTag = ''Y'' THEN ''SAP Success''
             WHEN sd.apiStatusTag = ''P'' THEN ''SAP Processing''
             WHEN sd.apiStatusTag = ''N'' THEN ''SAP Failed''
             ELSE ''SAP Not Started'' END AS sapStatus,
        sd.apiMessage,
        sd.sapCardCode,
        sd.sapAttachmentEntry,
        sd.payloadHash,
        sd.lastAttemptOn,
        sd.lastAttemptBy,
        ISNULL(sd.retryCount, 0) AS retryCount,
        CAST(CASE WHEN pf.currentStage = pf.totalStage AND sd.apiStatusTag = ''N'' THEN 1 ELSE 0 END AS BIT) AS canRetrySap
    FROM PendingFlows pf
    INNER JOIN BP.jsMaster m ON m.code = pf.bpCode
    LEFT JOIN dbo.jsStage s ON s.id = pf.currentStageId
    LEFT JOIN BP.jsSAPData sd ON sd.masterId = m.code
    OUTER APPLY
    (
        SELECT TOP (1) firstName, lastName, designation, emailAddress, alternateEmail
        FROM BP.jsContactPersons
        WHERE code = m.code
        ORDER BY contactID
    ) c
    ORDER BY CASE WHEN sd.apiStatusTag = ''N'' THEN 0 ELSE 1 END, pf.currentStage, m.code DESC;
END;');

    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsGetApprovedBP
    @userId INT,
    @companyId INT,
    @month VARCHAR(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.code AS code,
        m.code AS id,
        m.company AS companyId,
        m.type,
        m.name AS companyName,
        m.name AS partyName,
        m.foreignName,
        m.typeOfBusiness,
        m.industry,
        c.firstName,
        c.lastName,
        c.designation,
        m.mobileNo AS mobileNumber,
        c.emailAddress,
        c.alternateEmail,
        m.currency,
        m.remarks,
        m.isStaff,
        fs.createdOn,
        f.id AS flowId
    FROM BP.jsFlowStatus fs
    INNER JOIN BP.jsFlow f ON fs.flowId = f.id
    INNER JOIN BP.jsMaster m ON f.bpCode = m.code
    OUTER APPLY
    (
        SELECT TOP (1) firstName, lastName, designation, emailAddress, alternateEmail
        FROM BP.jsContactPersons
        WHERE code = m.code
        ORDER BY contactID
    ) c
    WHERE fs.userId = @userId
      AND fs.status = ''A''
      AND m.company = @companyId
      AND (@month IS NULL OR FORMAT(fs.createdOn, ''MM-yyyy'') = @month)
    ORDER BY fs.createdOn DESC;
END;');

    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsGetRejectedBP
    @userId INT,
    @companyId INT,
    @month VARCHAR(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.code AS code,
        m.code AS id,
        m.company AS companyId,
        m.type,
        m.name AS companyName,
        m.name AS partyName,
        m.foreignName,
        m.typeOfBusiness,
        m.industry,
        c.firstName,
        c.lastName,
        c.designation,
        m.mobileNo AS mobileNumber,
        c.emailAddress,
        c.alternateEmail,
        m.currency,
        m.remarks,
        m.isStaff,
        f.createdOn,
        f.id AS flowId,
        fs.description AS remark
    FROM BP.jsFlowStatus fs
    INNER JOIN BP.jsFlow f ON fs.flowId = f.id
    INNER JOIN BP.jsMaster m ON f.bpCode = m.code
    OUTER APPLY
    (
        SELECT TOP (1) firstName, lastName, designation, emailAddress, alternateEmail
        FROM BP.jsContactPersons
        WHERE code = m.code
        ORDER BY contactID
    ) c
    WHERE fs.userId = @userId
      AND fs.status = ''R''
      AND m.company = @companyId
      AND (@month IS NULL OR FORMAT(f.createdOn, ''MM-yyyy'') = @month)
    ORDER BY f.createdOn DESC;
END;');

    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsGetBPInsights
    @userId INT,
    @companyId INT,
    @month VARCHAR(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH UserStages AS
    (
        SELECT DISTINCT us.stageId
        FROM dbo.jsUserStage AS us
        INNER JOIN dbo.jsStageTemplate AS st ON us.stageId = st.stageId
        INNER JOIN dbo.jsTemplate AS t ON st.templateId = t.id
        WHERE us.userId = @userId
          AND t.company = @companyId
          AND t.isActive = 1
          AND ISNULL(us.status, 1) = 1
    )
    SELECT
        (
            SELECT COUNT(*)
            FROM BP.jsFlow f
            INNER JOIN BP.jsMaster m ON m.code = f.bpCode
            INNER JOIN UserStages us ON us.stageId = f.currentStageId
            WHERE f.status = ''P''
              AND m.company = @companyId
              AND (@month IS NULL OR FORMAT(f.createdOn, ''MM-yyyy'') = @month)
              AND NOT EXISTS
              (
                  SELECT 1 FROM BP.jsFlowStatus fs
                  WHERE fs.flowId = f.id
                    AND fs.templateId = f.templateId
                    AND fs.stageId = f.currentStageId
                    AND fs.userId = @userId
                    AND fs.status IN (''A'', ''R'')
              )
        ) AS TotalPending,
        (
            SELECT COUNT(*)
            FROM BP.jsFlowStatus fs
            INNER JOIN BP.jsFlow f ON f.id = fs.flowId
            INNER JOIN BP.jsMaster m ON m.code = f.bpCode
            WHERE fs.userId = @userId
              AND fs.status = ''A''
              AND m.company = @companyId
              AND (@month IS NULL OR FORMAT(fs.createdOn, ''MM-yyyy'') = @month)
        ) AS TotalApproved,
        (
            SELECT COUNT(*)
            FROM BP.jsFlowStatus fs
            INNER JOIN BP.jsFlow f ON f.id = fs.flowId
            INNER JOIN BP.jsMaster m ON m.code = f.bpCode
            WHERE fs.userId = @userId
              AND fs.status = ''R''
              AND m.company = @companyId
              AND (@month IS NULL OR FORMAT(f.createdOn, ''MM-yyyy'') = @month)
        ) AS TotalRejected;
END;');

    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsGetBPSnapshots
    @code INT,
    @fromDate DATETIME2 = NULL,
    @toDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.SessionID,
        s.SnapshotDate,
        s.ChangedBy,
        s.CompanyByUser,
        s.Operation,
        s.type,
        s.isStaff,
        s.name AS companyName,
        s.foreignName,
        s.typeOfBusiness,
        s.industry,
        s.mobileNo AS mobileNumber,
        s.currency,
        s.remarks,
        s.company,
        (SELECT COUNT(*) FROM BP.jsMasterAddressSnapshot a WHERE a.OriginalCode = s.OriginalCode AND a.SessionID = s.SessionID) AS AddressCount,
        (SELECT COUNT(*) FROM BP.jsBankDetailsSnapshot b WHERE b.OriginalCode = s.OriginalCode AND b.SessionID = s.SessionID) AS BankDetailsCount,
        (SELECT COUNT(*) FROM BP.jsContactPersonsSnapshot c WHERE c.OriginalCode = s.OriginalCode AND c.SessionID = s.SessionID) AS ContactsCount,
        (SELECT COUNT(*) FROM BP.jsAttachmentsSnapshot at WHERE at.OriginalCode = s.OriginalCode AND at.SessionID = s.SessionID) AS AttachmentsCount
    FROM BP.jsMasterSnapshot s
    WHERE s.OriginalCode = @code
      AND (@fromDate IS NULL OR s.SnapshotDate >= @fromDate)
      AND (@toDate IS NULL OR s.SnapshotDate <= @toDate)
    ORDER BY s.SnapshotDate DESC;
END;');

    EXEC(N'
CREATE OR ALTER PROCEDURE BP.jsRestoreBPFromSnapshot
    @code INT,
    @sessionID UNIQUEIDENTIFIER,
    @userId INT,
    @companyByUser VARCHAR(100),
    @confirmRestore BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @confirmRestore <> 1
        THROW 50101, ''Restore operation must be confirmed by setting @confirmRestore = 1.'', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM BP.jsMasterSnapshot WHERE OriginalCode = @code AND SessionID = @sessionID)
            THROW 50102, ''No BP snapshot found for the supplied code/session.'', 1;

        DECLARE @RestoreSessionID UNIQUEIDENTIFIER = NEWID();

        INSERT INTO BP.jsMasterSnapshot
            (OriginalCode, SessionID, ChangedBy, CompanyByUser, Operation, type, isStaff, name, company, mobileNo,
             userId, companyByUser_Original, foreignName, typeOfBusiness, industry, currency, remarks)
        SELECT @code, @RestoreSessionID, @userId, @companyByUser, ''BEFORE_RESTORE'', type, isStaff, name, company, mobileNo,
               userId, companyByUser, foreignName, typeOfBusiness, industry, currency, remarks
        FROM BP.jsMaster
        WHERE code = @code;

        UPDATE m
        SET type = s.type,
            isStaff = ISNULL(s.isStaff, m.isStaff),
            name = ISNULL(s.name, m.name),
            company = ISNULL(s.company, m.company),
            foreignName = s.foreignName,
            typeOfBusiness = s.typeOfBusiness,
            industry = s.industry,
            mobileNo = s.mobileNo,
            currency = ISNULL(NULLIF(s.currency, ''''), m.currency),
            remarks = s.remarks
        FROM BP.jsMaster m
        INNER JOIN BP.jsMasterSnapshot s ON s.OriginalCode = m.code
        WHERE s.OriginalCode = @code AND s.SessionID = @sessionID;

        UPDATE t
        SET buyerTANNo = s.buyerTANNo,
            panNo = ISNULL(s.panNo, t.panNo),
            fssaiNo = s.fssaiNo,
            msmeNo = s.msmeNo,
            msmeType = s.msmeType,
            msmeBType = s.msmeBType,
            gstin = s.gstin
        FROM BP.jsTaxDetails t
        INNER JOIN BP.jsTaxDetailsSnapshot s ON s.OriginalCode = t.code
        WHERE s.OriginalCode = @code AND s.SessionID = @sessionID;

        DELETE FROM BP.jsMasterAddress WHERE code = @code;
        INSERT INTO BP.jsMasterAddress (code, addressType, addressLine1, addressLine2, stateID, cityID, pincode, countryID, gstNo, addressName)
        SELECT @code, addressType, addressLine1, addressLine2, stateID, cityID, pincode, countryID, gstNo, addressName
        FROM BP.jsMasterAddressSnapshot
        WHERE OriginalCode = @code AND SessionID = @sessionID;

        DELETE FROM BP.jsBankDetails WHERE code = @code;
        INSERT INTO BP.jsBankDetails (code, name, accountNo, ifscCode, branch, swiftCode, accountType)
        SELECT @code, name, accountNo, ifscCode, branch, swiftCode, accountType
        FROM BP.jsBankDetailsSnapshot
        WHERE OriginalCode = @code AND SessionID = @sessionID;

        DELETE FROM BP.jsContactPersons WHERE code = @code;
        INSERT INTO BP.jsContactPersons (code, designation, emailAddress, alternateEmail, mobileNumber, alternateContact, firstName, lastName)
        SELECT @code, designation, emailAddress, alternateEmail, mobileNumber, alternateContact, firstName, lastName
        FROM BP.jsContactPersonsSnapshot
        WHERE OriginalCode = @code AND SessionID = @sessionID;

        DELETE FROM BP.jsAttachments WHERE code = @code;
        INSERT INTO BP.jsAttachments (code, fileName, filePath, fileSize, contentType, fileType)
        SELECT @code, fileName, filePath, fileSize, contentType, fileType
        FROM BP.jsAttachmentsSnapshot
        WHERE OriginalCode = @code AND SessionID = @sessionID;

        INSERT INTO BP.jsAuditLog (Code, TableName, Operation, FieldName, OldValue, NewValue, ChangedBy, CompanyByUser, SessionID)
        VALUES (@code, ''ALL_TABLES'', ''RESTORE'', ''COMPLETE_RESTORE'', ''Current data'', CONCAT(''Restored from SessionID: '', CONVERT(VARCHAR(36), @sessionID)), @userId, @companyByUser, @RestoreSessionID);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;');

    -------------------------------------------------------------------------
    -- 5. Remove obsolete columns after backups, value migration, and procedure
    --    recreation. Workflow/SAP status/audit/attachment tables are preserved.
    -------------------------------------------------------------------------
    DECLARE @DropColumns TABLE(TableName SYSNAME, ColumnName SYSNAME);
    INSERT INTO @DropColumns(TableName, ColumnName)
    VALUES
        (N'jsMaster', N'staffCode'),
        (N'jsMaster', N'groupID'),
        (N'jsMaster', N'contactPerson'),
        (N'jsMaster', N'paymentTermID'),
        (N'jsMaster', N'priceList'),
        (N'jsMasterAddress', N'email'),
        (N'jsMasterAddress', N'isDefault'),
        (N'jsMasterAddress', N'gstType'),
        (N'jsMasterAddress', N'addressUid'),
        (N'jsContactPersons', N'email'),
        (N'jsContactPersons', N'phone'),
        (N'jsContactPersons', N'telephone'),
        (N'jsContactPersons', N'isPrimary'),
        (N'jsContactPersons', N'contactUid'),
        (N'jsTaxDetails', N'msmeBusinessType'),
        (N'jsBankDetails', N'countryID'),
        (N'jsBankDetails', N'acctName');

    DECLARE @TableName SYSNAME, @ColumnName SYSNAME;
    DECLARE column_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName, ColumnName FROM @DropColumns;

    OPEN column_cursor;
    FETCH NEXT FROM column_cursor INTO @TableName, @ColumnName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF COL_LENGTH(N'BP.' + @TableName, @ColumnName) IS NOT NULL
        BEGIN
            SELECT @sql = STRING_AGG(N'ALTER TABLE BP.' + QUOTENAME(@TableName) + N' DROP CONSTRAINT ' + QUOTENAME(kc.name) + N';', CHAR(10))
            FROM sys.key_constraints kc
            INNER JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
            INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            INNER JOIN sys.tables t ON t.object_id = kc.parent_object_id
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = N'BP' AND t.name = @TableName AND c.name = @ColumnName;

            IF @sql IS NOT NULL AND LEN(@sql) > 0 EXEC(@sql);

            SELECT @sql = STRING_AGG(N'DROP INDEX ' + QUOTENAME(i.name) + N' ON BP.' + QUOTENAME(@TableName) + N';', CHAR(10))
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            INNER JOIN sys.tables t ON t.object_id = i.object_id
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = N'BP'
              AND t.name = @TableName
              AND c.name = @ColumnName
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0;

            IF @sql IS NOT NULL AND LEN(@sql) > 0 EXEC(@sql);

            SELECT @sql = STRING_AGG(N'ALTER TABLE BP.' + QUOTENAME(@TableName) + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';', CHAR(10))
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
            INNER JOIN sys.tables t ON t.object_id = dc.parent_object_id
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = N'BP' AND t.name = @TableName AND c.name = @ColumnName;

            IF @sql IS NOT NULL AND LEN(@sql) > 0 EXEC(@sql);

            SET @sql = N'ALTER TABLE BP.' + QUOTENAME(@TableName) + N' DROP COLUMN ' + QUOTENAME(@ColumnName) + N';';
            EXEC(@sql);
        END;

        FETCH NEXT FROM column_cursor INTO @TableName, @ColumnName;
    END;
    CLOSE column_cursor;
    DEALLOCATE column_cursor;

    -------------------------------------------------------------------------
    -- 6. Existing approval/stage procedures do not reference removed business
    --    columns. Refresh them to verify metadata without changing workflow.
    -------------------------------------------------------------------------
    IF OBJECT_ID(N'BP.jsApproveBP', N'P') IS NOT NULL EXEC sys.sp_refreshsqlmodule N'BP.jsApproveBP';
    IF OBJECT_ID(N'BP.jsRejectBP', N'P') IS NOT NULL EXEC sys.sp_refreshsqlmodule N'BP.jsRejectBP';
    IF OBJECT_ID(N'BP.jsGetBPApprovalFlow', N'P') IS NOT NULL EXEC sys.sp_refreshsqlmodule N'BP.jsGetBPApprovalFlow';
    IF OBJECT_ID(N'BP.jsGetBPSnapshots', N'P') IS NOT NULL EXEC sys.sp_refreshsqlmodule N'BP.jsGetBPSnapshots';
    IF OBJECT_ID(N'BP.jsRestoreBPFromSnapshot', N'P') IS NOT NULL EXEC sys.sp_refreshsqlmodule N'BP.jsRestoreBPFromSnapshot';

    COMMIT TRANSACTION;
    PRINT CONCAT('BP field cleanup committed. MigrationRunId=', CONVERT(VARCHAR(36), @MigrationRunId));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorNumber INT = ERROR_NUMBER();
    DECLARE @ErrorLine INT = ERROR_LINE();
    DECLARE @ThrowMessage NVARCHAR(2048) = CONCAT('BP field cleanup failed and was rolled back. Error ', @ErrorNumber, ' at line ', @ErrorLine, ': ', @ErrorMessage);
    THROW 51000, @ThrowMessage, 1;
END CATCH;

/*
Post-deploy checks:

EXEC BP.jsGetSingleBPData @bpCode = <existing BP code>;
EXEC BP.jsGetPendingBP @userId = <approver user id>, @companyId = <company id>, @month = NULL;
EXEC BP.jsGetBPInsights @userId = <approver user id>, @companyId = <company id>, @month = NULL;

Rollback after commit:
- Use the MigrationRunId printed by the script.
- Restore retired values from:
  BP.jsMaster_RemovedColumnsBackup
  BP.jsMasterAddress_RemovedColumnsBackup
  BP.jsContactPersons_RemovedColumnsBackup
  BP.jsTaxDetails_RemovedColumnsBackup
  BP.jsBankDetails_RemovedColumnsBackup
- Re-add retired columns only if a legacy portal rollback is required.
*/
