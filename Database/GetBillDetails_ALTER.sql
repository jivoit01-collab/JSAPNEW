/* =====================================================================
   dbo.GetBillDetails  —  updated to return the new "who did it" columns.
   Run this in FR8HODBNEW (after the columns were added to AttachmentUpload).
   Only change vs original: added AU.* fields to the SELECT and GROUP BY.
   Aliases match what the app reads (CheckerDate / CheckerBy / CheckerName).
   ===================================================================== */
ALTER PROCEDURE [dbo].[GetBillDetails]
(
    @FromDate     DATE           = NULL,
    @ToDate       DATE           = NULL,
    @AccountName  NVARCHAR(200)  = NULL,
    @SerialNumber DECIMAL(18,4)  = NULL,
    @Status       NVARCHAR(50)   = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @StartDate DATE = '2026-04-01';

    -- Force FromDate to not go below 1 April 2026
    IF @FromDate IS NULL OR @FromDate < @StartDate
        SET @FromDate = @StartDate;

    SELECT
        A.SerialNumber,
        F.AccountName,
        A.AccountID,
        A.VchNumber,
        A.VoucherDate,
        A.BillAmount,
        MAX(A.SupplierRef)      AS SupplierRef,
        MAX(A.SupplierRefDate)  AS SupplierRefDate,
        MAX(A.DueDate)          AS DueDate,

        -- ✅ TOTALS
        SUM(B.Quantity)   AS TotalQuantity,
        SUM(B.ItemValue)  AS TotalItemValue,
        COUNT(B.ProductID) AS TotalItems,

        AU.AttachmentPath     AS AttachmentPath,
        AU.MakerRemark        AS MakerRemark,
        AU.Status             AS Status,
        AU.IsPaymentVerified  AS IsPaymentVerified,

        -- ✅ CHECKER DATA
        AU.CheckerStatus  AS CheckerStatus,
        AU.CheckerRemark  AS CheckerRemark,
        AU.CheckerDate    AS CheckerDate,

        -- ✅ WHO DID IT  (id + name captured at action time)
        AU.MakerBy        AS MakerBy,
        AU.MakerByName    AS MakerName,
        AU.CheckerBy      AS CheckerBy,
        AU.CheckerByName  AS CheckerName,
        AU.PaidBy         AS PaidBy,
        AU.PaidByName     AS PaidName,
        AU.VerifiedBy     AS VerifiedBy,
        AU.VerifiedByName AS VerifiedName,
        AU.VerifiedDate   AS VerifiedDate,

        CASE
            WHEN MAX(G.RefName) IS NULL THEN 'UnPaid'
            ELSE 'Paid'
        END AS PaymentStatus,

        MAX(G.RefDate) AS PaymentDate

    FROM PurchaseHeader A

    INNER JOIN PurchaseDetail B
        ON A.SerialNumber = B.SerialNumber          -- 🔥 IMPORTANT

    INNER JOIN AccountMaster F
        ON F.AccountID = A.AccountID

    OUTER APPLY (
        SELECT TOP 1 *
        FROM AttachmentUpload X
        WHERE X.VchNumber = A.VchNumber
        ORDER BY X.Id DESC
    ) AU

    LEFT JOIN RefMaster G
        ON G.RefName   = A.SupplierRef
        AND G.AccountID = A.AccountID
        AND G.ToBy      = 43

    WHERE
        (@SerialNumber IS NULL OR A.SerialNumber = @SerialNumber)
        AND A.VoucherDate >= @FromDate
        AND (@ToDate IS NULL OR A.VoucherDate < DATEADD(DAY, 1, @ToDate))
        AND (@AccountName IS NULL OR F.AccountName LIKE '%' + @AccountName + '%')
        AND
        (
            @Status IS NULL
            OR
            (
                @Status = 'Pending'
                AND
                (
                    AU.CheckerStatus IS NULL
                    OR LTRIM(RTRIM(AU.CheckerStatus)) = ''
                    OR AU.CheckerStatus = 'Pending'
                )
            )
            OR AU.CheckerStatus = @Status
        )

    GROUP BY
        A.SerialNumber,
        F.AccountName,
        A.AccountID,
        A.VchNumber,
        A.VoucherDate,
        A.BillAmount,
        AU.AttachmentPath,
        AU.MakerRemark,
        AU.Status,
        AU.CheckerStatus,
        AU.CheckerRemark,
        AU.CheckerDate,
        AU.MakerBy,
        AU.MakerByName,
        AU.CheckerBy,
        AU.CheckerByName,
        AU.PaidBy,
        AU.PaidByName,
        AU.VerifiedBy,
        AU.VerifiedByName,
        AU.VerifiedDate,
        AU.IsPaymentVerified

    ORDER BY A.VoucherDate DESC
END
