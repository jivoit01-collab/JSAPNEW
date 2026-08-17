/* =========================================================================
   GetInvoice2  --  Bill Verification expand-row (Checker / Payment) columns
   Database : FR8HODBNEW

   Fixes vs the version that errored on /BillVerificationWeb/CheckerPage:

   1. ProductDetail join was:
          INNER JOIN ProductDetail PD ON PD.ProductID = PM.ProductID
      Two problems:
        a) INNER JOIN dropped every purchase line whose product had no
           ProductDetail row  ->  items disappeared and totals were wrong.
        b) ProductDetail has more than one row per ProductID, so each
           purchase line was duplicated once per detail row  ->  duplicate
           items and inflated Qty / Tax / Amount totals.
      HSNCode is now pulled with OUTER APPLY (SELECT TOP 1 ...), which
      returns exactly one HSN per line and never drops a line.

   2. Removed the redundant `LEFT JOIN ProductMaster PM` -- it only existed
      to reach ProductDetail; ProductMaster is already joined as C.

   NOTE: if HSN actually varies per child (not per product), add
         `AND pd.ProductChildID = B.ProductChildID` inside the OUTER APPLY.
   ========================================================================= */

CREATE OR ALTER PROCEDURE [dbo].[GetInvoice2]
(
    @SerialNumber  DECIMAL(18,4) = NULL,
    @AccountName   NVARCHAR(200) = NULL,
    @VchNumber     DECIMAL       = NULL,
    @FromDate      DATE          = NULL,
    @ToDate        DATE          = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    -- =============================================
    -- RESULT SET 1 : MAIN HEADER TABLE
    -- =============================================
    SELECT
        A.SerialNumber,
        F.AccountName,                                  -- Account
        A.VchNumber,                                    -- Voucher
        A.VoucherDate,                                  -- Date
        A.BillAmount,                                   -- Amount
        A.SupplierRef,                                  -- Supplier Ref
        A.SupplierRefDate        AS RefDate,            -- Ref Date
        A.DueDate,                                      -- Due Date

        CASE
            WHEN AU.AttachmentPath IS NULL
            THEN 'No File' ELSE 'File'
        END                      AS Attachment,

        AU.MakerRemark,                                 -- Maker Remark
        AU.CheckerRemark,                               -- Checker Remark
        AU.CheckerStatus,                               -- Checker Status
        AU.Status                AS MakerStatus,        -- Maker Status

        CASE
            WHEN G.RefName IS NULL THEN 'UnPaid'
            ELSE 'Paid'
        END                      AS PaymentStatus,      -- Payment Status

        CASE
            WHEN G.RefName IS NULL THEN NULL
            ELSE G.RefDate
        END                      AS PaymentDate,        -- Payment Date

        A.QtyTotal               AS TotalQty,           -- Total Qty
        A.SubTotal               AS TotalAmount,        -- Total Amount
        (
            SELECT COUNT(*)
            FROM PurchaseDetail PD
            WHERE PD.SerialNumber = A.SerialNumber
        )                        AS TotalItems          -- Total Items

    FROM PurchaseHeader A
    INNER JOIN AccountMaster F
        ON F.AccountID      = A.AccountID
    LEFT JOIN AttachmentUpload AU
        ON AU.VchNumber     = A.VchNumber
    LEFT JOIN RefMaster G
        ON G.RefName        = A.SupplierRef
        AND G.AccountID     = A.AccountID
        AND G.ToBy          = 43

    WHERE
        (@SerialNumber IS NULL OR A.SerialNumber    = @SerialNumber)
        AND (@AccountName  IS NULL OR F.AccountName LIKE '%' + @AccountName + '%')
        AND (@VchNumber    IS NULL OR A.VchNumber   = @VchNumber)
        AND (@FromDate     IS NULL OR A.VoucherDate >= @FromDate)
        AND (@ToDate       IS NULL OR A.VoucherDate <= @ToDate)

    ORDER BY A.VoucherDate DESC;


    -- =============================================
    -- RESULT SET 2 : PRODUCT DETAILS
    -- =============================================
    SELECT
        A.SerialNumber,                                 -- Serial No
        A.VchNumber,
        F.AccountName,
        B.ProductID,
        B.ProductChildID,
        C.ProductName,                                  -- Product
        PD.HSNCode,                                     -- HSN Code (one per product)
        B.Quantity,                                     -- Qty
        B.PurchaseCost,                                 -- Purchase Rate
        B.SellingRate,                                  -- Selling Rate
        PCM.Margin,                                     -- Margin
        B.DiscountPercent,                              -- Discount %
        B.DiscountAmount,                               -- Discount Amount
        E.TaxName,                                      -- Tax Name
        B.TaxRate,                                      -- Tax %
        B.TaxAmount,                                    -- Tax Amount
        B.ItemValue,                                    -- Amount
        D.WarehouseName,                                -- Warehouse
        PCM.MinusPercent,
        PCM.PlusPercent,
        PCM.MRP

    FROM PurchaseHeader A
    INNER JOIN PurchaseDetail B
        ON A.SerialNumber   = B.SerialNumber
    INNER JOIN ProductMaster C
        ON C.ProductID      = B.ProductID
    LEFT JOIN ProductChildMaster PCM
        ON PCM.ProductChildID = B.ProductChildID
    INNER JOIN WarehouseMaster D
        ON D.WarehouseID    = B.WarehouseID
    INNER JOIN TaxMaster E
        ON E.TaxID          = B.TaxID
    INNER JOIN AccountMaster F
        ON F.AccountID      = A.AccountID
    OUTER APPLY (
        SELECT TOP (1) pd.HSNCode
        FROM ProductDetail pd
        WHERE pd.ProductID = B.ProductID
    ) PD

    WHERE
        (@SerialNumber IS NULL OR A.SerialNumber    = @SerialNumber)
        AND (@AccountName  IS NULL OR F.AccountName LIKE '%' + @AccountName + '%')
        AND (@VchNumber    IS NULL OR A.VchNumber   = @VchNumber)
        AND (@FromDate     IS NULL OR A.VoucherDate >= @FromDate)
        AND (@ToDate       IS NULL OR A.VoucherDate <= @ToDate)

    ORDER BY A.VoucherDate DESC, C.ProductName;

END
GO
