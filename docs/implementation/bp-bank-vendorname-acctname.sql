USE [jsap_test]
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'BP.jsBankDetails', N'U') IS NULL
        THROW 51001, 'BP.jsBankDetails table does not exist.', 1;

    IF OBJECT_ID(N'BP.jsBankDetails_Backup_BankVendorName', N'U') IS NULL
    BEGIN
        SELECT *
        INTO BP.jsBankDetails_Backup_BankVendorName
        FROM BP.jsBankDetails;
    END

    IF COL_LENGTH(N'BP.jsBankDetails', N'VendorName') IS NULL
       AND COL_LENGTH(N'BP.jsBankDetails', N'name') IS NOT NULL
    BEGIN
        EXEC sys.sp_rename
            @objname = N'BP.jsBankDetails.name',
            @newname = N'VendorName',
            @objtype = N'COLUMN';
    END

    IF COL_LENGTH(N'BP.jsBankDetails', N'VendorName') IS NULL
    BEGIN
        ALTER TABLE BP.jsBankDetails
            ADD VendorName NVARCHAR(150) NULL;
    END

    IF COL_LENGTH(N'BP.jsBankDetails', N'BankCode') IS NULL
    BEGIN
        ALTER TABLE BP.jsBankDetails
            ADD BankCode NVARCHAR(50) NULL;
    END

    COMMIT TRANSACTION;
    PRINT 'BP.jsBankDetails vendorName/BankCode migration completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH
GO

/*
Required stored procedure update after this table change:

1. In BP.jsInsertBPMasterData, change the vendor bank insert column from name to vendorName:

   INSERT INTO BP.jsBankDetails (code, VendorName, accountNo, ifscCode, branch, swiftCode, accountType)
   VALUES (@generatedCode, @bankName, @accountNo, @ifscCode, @branchName, @swiftCode, @accountType);

2. In BP.jsUpdateBPMasterData, change the vendor bank insert column from name to vendorName:

   INSERT INTO BP.jsBankDetails (code, VendorName, accountNo, ifscCode, branch, swiftCode, accountType)
   VALUES (@code, @bankName, @accountNo, @ifscCode, @branchName, @swiftCode, @accountType);

The .NET service now passes vendor/account-holder name into the existing @bankName parameter.
*/
