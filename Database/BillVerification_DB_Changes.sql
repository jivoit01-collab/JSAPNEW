/* =====================================================================
   BILL VERIFICATION — DATABASE CHANGES
   ---------------------------------------------------------------------
   Run this whole script against the  FR8HODBNEW  database (FHConnection),
   which is where AttachmentUpload lives and where we keep the audit log.
   The script is idempotent — safe to run more than once.
   ===================================================================== */


/* =====================================================================
   1) AUDIT LOG TABLE
      Captures EVERY bill-verification action (Invoice Maker / Invoice
      Checker / Payment Maker / Payment Checker / Admin / Document Hub).
      Written to by the app (BillVerificationLogService).
   ===================================================================== */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BillVerificationLog')
BEGIN
    CREATE TABLE BillVerificationLog
    (
        LogId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BillVerificationLog PRIMARY KEY,
        Module     NVARCHAR(50)   NULL,   -- Invoice Maker / Invoice Checker / Payment Maker / Payment Checker / Admin / Document Hub
        Action     NVARCHAR(100)  NULL,   -- Submit / Approved / Rejected / Mark Paid / Verify / Delete File ...
        VchNumber  NVARCHAR(50)   NULL,   -- bill voucher no. (or file/folder id for Document Hub)
        Remark     NVARCHAR(MAX)  NULL,
        Details    NVARCHAR(MAX)  NULL,   -- extra info (file name, count, etc.)
        UserId     INT            NULL,   -- who did it (session userId)
        UserName   NVARCHAR(150)  NULL,   -- who did it (session username)
        CompanyId  INT            NULL,
        IpAddress  NVARCHAR(50)   NULL,
        CreatedAt  DATETIME       NOT NULL
                   CONSTRAINT DF_BillVerificationLog_CreatedAt DEFAULT (GETDATE())
    );
END
GO

/* index for fast lookup by voucher / date */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_BillVerificationLog_Vch'
                 AND object_id = OBJECT_ID('BillVerificationLog'))
    CREATE INDEX IX_BillVerificationLog_Vch
        ON BillVerificationLog (VchNumber, CreatedAt);
GO


/* =====================================================================
   2) AttachmentUpload — "WHO DID IT" COLUMNS
      So each row can show who Submitted / Approved / Rejected / Paid /
      Verified it (e.g. the "Rejected by X" popup on the Maker page).

      We store BOTH the user id AND the user name (captured from the
      session at action time) — because jsUser is on a different server,
      so a live join is not possible. Name is a snapshot, which is exactly
      what an audit trail wants.
   ===================================================================== */

-- Invoice Maker (Submit / Upload)
IF COL_LENGTH('AttachmentUpload', 'MakerBy')       IS NULL  ALTER TABLE AttachmentUpload ADD MakerBy       INT           NULL;
GO
IF COL_LENGTH('AttachmentUpload', 'MakerByName')   IS NULL  ALTER TABLE AttachmentUpload ADD MakerByName   NVARCHAR(150) NULL;
GO

-- Invoice Checker (Approve / Reject)  [CheckerDate/CheckerRemark already exist]
IF COL_LENGTH('AttachmentUpload', 'CheckerBy')     IS NULL  ALTER TABLE AttachmentUpload ADD CheckerBy     INT           NULL;
GO
IF COL_LENGTH('AttachmentUpload', 'CheckerByName') IS NULL  ALTER TABLE AttachmentUpload ADD CheckerByName NVARCHAR(150) NULL;
GO

-- Payment Maker (Mark Paid)  — PaymentStatus/PaymentDate are NOT in the current table, so add them too
IF COL_LENGTH('AttachmentUpload', 'PaymentStatus') IS NULL  ALTER TABLE AttachmentUpload ADD PaymentStatus NVARCHAR(20)  NULL;
GO
IF COL_LENGTH('AttachmentUpload', 'PaymentDate')   IS NULL  ALTER TABLE AttachmentUpload ADD PaymentDate   DATETIME      NULL;
GO
IF COL_LENGTH('AttachmentUpload', 'PaidBy')        IS NULL  ALTER TABLE AttachmentUpload ADD PaidBy        INT           NULL;
GO
IF COL_LENGTH('AttachmentUpload', 'PaidByName')    IS NULL  ALTER TABLE AttachmentUpload ADD PaidByName    NVARCHAR(150) NULL;
GO

-- Payment Checker (Verify / Reject payment)
IF COL_LENGTH('AttachmentUpload', 'VerifiedBy')    IS NULL  ALTER TABLE AttachmentUpload ADD VerifiedBy    INT           NULL;
GO
IF COL_LENGTH('AttachmentUpload', 'VerifiedByName') IS NULL ALTER TABLE AttachmentUpload ADD VerifiedByName NVARCHAR(150) NULL;
GO
IF COL_LENGTH('AttachmentUpload', 'VerifiedDate')  IS NULL  ALTER TABLE AttachmentUpload ADD VerifiedDate  DATETIME      NULL;
GO

/* Column notes for AttachmentUpload:
     ALREADY EXIST  -> Id, VchNumber, AttachmentPath, MakerRemark, Status,
                       CreatedDate, CheckerRemark, CheckerStatus, CheckerDate,
                       IsPaymentVerified
     Timestamps ready:
       CreatedDate -> Invoice Maker submit ;  CheckerDate -> Invoice Checker action
     ADDED by this script (were missing):
       PaymentStatus / PaymentDate  -> Payment Maker mark-paid
       VerifiedDate                 -> Payment Checker verify/reject
       MakerBy/Name, CheckerBy/Name, PaidBy/Name, VerifiedBy/Name -> "who did it"
*/


/* =====================================================================
   3) STORED PROCEDURE  dbo.GetBillDetails
      This SP feeds the tables/popups on the pages, so it must RETURN the
      new columns. This is a GUIDE — merge the marked lines into your
      existing GetBillDetails SELECT.  (au = AttachmentUpload alias.)

      Column names below are chosen to match what the app already reads
      (CheckerDate, CheckerBy, CheckerName, etc.) — keep these aliases.
   ---------------------------------------------------------------------
   -- Add to the SELECT column list:

        au.CheckerDate,
        au.MakerBy,
        au.MakerByName      AS MakerName,
        au.CheckerBy,
        au.CheckerByName    AS CheckerName,
        au.PaidBy,
        au.PaidByName       AS PaidName,
        au.VerifiedBy,
        au.VerifiedByName   AS VerifiedName,
        au.VerifiedDate,

      (No JOIN needed — the names are stored right in AttachmentUpload.)
   ===================================================================== */


/* =====================================================================
   3b) I need the CURRENT dbo.GetBillDetails body to give you the exact
       modified version. Run this and share the output:

            EXEC sp_helptext 'dbo.GetBillDetails';

       (or in SSMS: right-click the SP -> Script Stored Procedure as ->
        ALTER To -> New Query Window, then paste it back to me.)
       I'll return the full ALTER PROCEDURE with the new columns merged in.
   ===================================================================== */


/* =====================================================================
   4) READ AUDIT LOG  (for a logs report screen)
      Fully ready — uses the BillVerificationLog table above.
   ===================================================================== */
IF OBJECT_ID('dbo.usp_GetBillVerificationLog', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetBillVerificationLog;
GO
CREATE PROCEDURE dbo.usp_GetBillVerificationLog
    @FromDate  DATETIME     = NULL,
    @ToDate    DATETIME     = NULL,
    @Module    NVARCHAR(50) = NULL,   -- Invoice Maker / Invoice Checker / ...
    @Action    NVARCHAR(100)= NULL,
    @VchNumber NVARCHAR(50) = NULL,
    @UserId    INT          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        LogId,
        Module,
        Action,
        VchNumber,
        Remark,
        Details,
        UserId,
        UserName,
        CompanyId,
        IpAddress,
        CreatedAt
    FROM BillVerificationLog
    WHERE (@FromDate  IS NULL OR CreatedAt >= @FromDate)
      AND (@ToDate    IS NULL OR CreatedAt <  DATEADD(DAY, 1, @ToDate))
      AND (@Module    IS NULL OR Module    = @Module)
      AND (@Action    IS NULL OR Action    = @Action)
      AND (@VchNumber IS NULL OR VchNumber = @VchNumber)
      AND (@UserId    IS NULL OR UserId    = @UserId)
    ORDER BY CreatedAt DESC, LogId DESC;
END
GO


/* =====================================================================
   5) (OPTIONAL) INSERT INTO AUDIT LOG via a proc instead of inline SQL.
      The app currently INSERTs directly; this proc is here only if you
      prefer procs. If you use it, tell me and I'll switch the app to it.
   ===================================================================== */
IF OBJECT_ID('dbo.usp_InsertBillVerificationLog', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_InsertBillVerificationLog;
GO
CREATE PROCEDURE dbo.usp_InsertBillVerificationLog
    @Module    NVARCHAR(50),
    @Action    NVARCHAR(100),
    @VchNumber NVARCHAR(50)  = NULL,
    @Remark    NVARCHAR(MAX) = NULL,
    @Details   NVARCHAR(MAX) = NULL,
    @UserId    INT           = NULL,
    @UserName  NVARCHAR(150) = NULL,
    @CompanyId INT           = NULL,
    @IpAddress NVARCHAR(50)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO BillVerificationLog
        (Module, Action, VchNumber, Remark, Details, UserId, UserName, CompanyId, IpAddress, CreatedAt)
    VALUES
        (@Module, @Action, @VchNumber, @Remark, @Details, @UserId, @UserName, @CompanyId, @IpAddress, GETDATE());
END
GO
