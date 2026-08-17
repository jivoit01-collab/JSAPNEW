


CREATE PROCEDURE [dbo].[GetInvoice]
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
        A.DueDate,
          -- Due Date

        -- Attachment - File hai ya nahi
        CASE 
            WHEN AU.AttachmentPath IS NULL 
            THEN 'No File' ELSE 'File' 
        END                      AS Attachment,

        -- Maker Remark
        AU.MakerRemark,                                 -- Maker Remark

        -- Checker Remark
        AU.CheckerRemark,                               -- Checker Remark

        -- Checker Status
        AU.CheckerStatus,                               -- Checker Status

        -- Maker Status
        AU.Status                AS MakerStatus,        -- Maker Status

        -- Payment Status from RefMaster
        CASE 
            WHEN G.RefName IS NULL THEN 'UnPaid' 
            ELSE 'Paid' 
        END                      AS PaymentStatus,      -- Payment Status

        CASE 
            WHEN G.RefName IS NULL THEN NULL 
            ELSE G.RefDate 
        END                      AS PaymentDate,        -- Payment Date

        -- Totals for Maker Page
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
        ON AU.VchNumber     = A.VchNumber               -- ✅ Attachment join
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
    -- 
    -- =============================================
SELECT 
    A.SerialNumber,                                 -- Serial No
    A.VchNumber,

    F.AccountName,

    B.ProductID,

    B.ProductChildID,

    C.ProductName,                                 -- Product

    PM.HSNSACID,                                   -- HSN/SAC

    B.Quantity,                                    -- Qty

    B.PurchaseCost,                                -- Purchase Rate

    B.SellingRate,                                 -- MRP

    PCM.Margin,                                    -- Margin

    B.DiscountPercent,                             -- Discount %

    B.DiscountAmount,                              -- Discount Amount

    E.TaxName,                                     -- Tax Name

    B.TaxRate,                                     -- Tax %

    B.TaxAmount,                                   -- Tax Amount

    B.ItemValue,                                   -- Amount

    D.WarehouseName                                -- Warehouse
    FROM PurchaseHeader A
    INNER JOIN PurchaseDetail B 
        ON A.SerialNumber   = B.SerialNumber            -- ✅ Correct join
    INNER JOIN ProductMaster C 
        ON C.ProductID      = B.ProductID
        LEFT JOIN ProductMaster PM
    ON PM.ProductID = B.ProductID

LEFT JOIN ProductChildMaster PCM
    ON PCM.ProductChildID = B.ProductChildID
    INNER JOIN WarehouseMaster D 
        ON D.WarehouseID    = B.WarehouseID
    INNER JOIN TaxMaster E 
        ON E.TaxID          = B.TaxID
    INNER JOIN AccountMaster F 
        ON F.AccountID      = A.AccountID

    WHERE
        (@SerialNumber IS NULL OR A.SerialNumber    = @SerialNumber)
        AND (@AccountName  IS NULL OR F.AccountName LIKE '%' + @AccountName + '%')
        AND (@VchNumber    IS NULL OR A.VchNumber   = @VchNumber)
        AND (@FromDate     IS NULL OR A.VoucherDate >= @FromDate)
        AND (@ToDate       IS NULL OR A.VoucherDate <= @ToDate)

    ORDER BY A.VoucherDate DESC, C.ProductName;

END

