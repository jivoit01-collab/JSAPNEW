USE [jsap_test]
GO

/*
    BP SAP control account configuration

    Purpose:
    - Stores the SAP control account used for BP Business Partner posting.
    - Customer BP rows must point to a receivable asset account.
    - Vendor BP rows must point to a liability account.

    Important:
    - Replace the sample account codes below with real SAP OACT account codes
      before production execution.
    - The backend validates each configured account against SAP OACT before
      posting to SAP Service Layer.
*/

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'BP') IS NULL
    BEGIN
        EXEC(N'CREATE SCHEMA BP');
    END;

    IF OBJECT_ID(N'BP.jsSAPAccountConfig', N'U') IS NULL
    BEGIN
        CREATE TABLE BP.jsSAPAccountConfig
        (
            id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_jsSAPAccountConfig PRIMARY KEY,
            companyId INT NOT NULL,
            bpType CHAR(1) NOT NULL,
            accountCode NVARCHAR(50) NOT NULL,
            accountName NVARCHAR(200) NULL,
            isActive BIT NOT NULL CONSTRAINT DF_jsSAPAccountConfig_isActive DEFAULT (1),
            createdOn DATETIME2(0) NOT NULL CONSTRAINT DF_jsSAPAccountConfig_createdOn DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT CK_jsSAPAccountConfig_bpType CHECK (bpType IN ('C', 'V'))
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'BP.jsSAPAccountConfig')
          AND name = N'IX_jsSAPAccountConfig_company_bpType_active'
    )
    BEGIN
        CREATE INDEX IX_jsSAPAccountConfig_company_bpType_active
            ON BP.jsSAPAccountConfig (companyId, bpType, isActive, id DESC);
    END;

    DECLARE @CompanyId INT = 1;
    DECLARE @CustomerAccountCode NVARCHAR(50) = N'110001'; -- sample receivable asset account; replace
    DECLARE @CustomerAccountName NVARCHAR(200) = N'Customer Receivable Control Account';
    DECLARE @VendorAccountCode NVARCHAR(50) = N'210001'; -- sample liability account; replace
    DECLARE @VendorAccountName NVARCHAR(200) = N'Vendor Liability Control Account';

    IF NOT EXISTS
    (
        SELECT 1
        FROM BP.jsSAPAccountConfig
        WHERE companyId = @CompanyId
          AND bpType = 'C'
          AND accountCode = @CustomerAccountCode
          AND isActive = 1
    )
    BEGIN
        INSERT INTO BP.jsSAPAccountConfig
            (companyId, bpType, accountCode, accountName, isActive)
        VALUES
            (@CompanyId, 'C', @CustomerAccountCode, @CustomerAccountName, 1);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM BP.jsSAPAccountConfig
        WHERE companyId = @CompanyId
          AND bpType = 'V'
          AND accountCode = @VendorAccountCode
          AND isActive = 1
    )
    BEGIN
        INSERT INTO BP.jsSAPAccountConfig
            (companyId, bpType, accountCode, accountName, isActive)
        VALUES
            (@CompanyId, 'V', @VendorAccountCode, @VendorAccountName, 1);
    END;

    COMMIT TRANSACTION;

    PRINT 'BP.jsSAPAccountConfig created/verified successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO

-- Verification
SELECT
    id,
    companyId,
    bpType,
    accountCode,
    accountName,
    isActive,
    createdOn
FROM BP.jsSAPAccountConfig
ORDER BY companyId, bpType, id DESC;
GO
