/* =========================================================================
   GetInvoice  --  Checker page expand-row columns
   Database : FR8HODBNEW   (server 103.89.45.75)

   Kya badla:
     1. Result set 2 me `SaleRate` column add kiya (Checker table ka
        "Sale Rate" column isi se aata hai).
     2. Discount ke liye COALESCE lagaya -- B.DiscountPercent ke saath
        B.DiscPercent bhi check hota hai. (Neeche "DISCOUNT" note padhein.)

   Pehle ka original version: Database/GetInvoice_Original_Backup.sql

   -------------------------------------------------------------------------
   >>> SALE RATE -- YE EK LINE APKO CONFIRM KARNI HAI <<<

   Neeche marked line abhi ye hai:

        B.SellingRate                          AS SaleRate,

   Agar table dekhne ke baad koi dusra column sahi lage to sirf usko badlein:

        B.SellingRate        -- PurchaseDetail : is purchase line ka rate
                                (abhi ye "MRP" column me bhi dikh raha hai)
        PCM.SellingPrice     -- ProductChildMaster : product ka selling price
        PCM.MRP              -- ProductChildMaster : product ka MRP
        C.StandardSalePrice  -- ProductMaster (sample rows me 0 tha)
        C.MaxRetailPrice     -- ProductMaster (sample rows me 0 tha)
        C.Rate               -- ProductMaster (sample rows me 0 tha)

   Note: 87,631 purchase lines me se 17,145 me B.SellingRate aur PCM.MRP
   alag-alag hain, isliye ye choice matter karti hai.

   -------------------------------------------------------------------------
   >>> DISCOUNT -- ZAROORI NOTE <<<

   Discount galat isliye dikh raha hai kyunki PurchaseDetail me discount
   data hai hi nahi. Poore table pe check kiya gaya:

        Total rows                              87,631
        DiscPercent      <> 0                        0
        DiscountPercent  <> 0                        0
        DiscountAmount   <> 0                        0
        ItemValue <> Quantity x PurchaseCost         0

   Yani har row me discount 0 hai, aur ItemValue bhi exactly
   Quantity x PurchaseCost ke barabar hai -- kahin bhi discount deduct
   nahi ho raha. PurchaseHeader me bhi koi discount column nahi hai.

   Isliye neeche ka COALESCE sirf ek safety net hai. Column ko sahi
   value tab hi milegi jab purchase entry / SAP import discount ko
   PurchaseDetail me actually save karega. Agar aapke ERP me discount
   kisi aur table/column me aata hai to wo naam bata dijiye, main join
   laga dunga.
   ========================================================================= */

USE FR8HODBNEW;
GO

ALTER PROCEDURE [dbo].[GetInvoice]
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
    -- RESULT SET 1 : MAIN HEADER TABLE  (koi change nahi)
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
    -- RESULT SET 2 : PRODUCT DETAILS  (Checker expand-row)
    -- =============================================
    SELECT
        A.SerialNumber,                                 -- Serial No
        A.VchNumber,

        F.AccountName,

        B.ProductID,                                    -- Product ID  (UI: "Product ID")

        B.ProductChildID,

        C.ProductName,                                  -- Product

        PM.HSNSACID,                                    -- HSN/SAC     (UI: "HSN Code")

        B.Quantity,                                     -- Qty

        B.PurchaseCost,                                 -- Purchase Rate

        B.SellingRate,                                  -- MRP

        /* >>> SALE RATE : agar dusra column chahiye to sirf ye line badlein <<< */
        B.SellingRate            AS SaleRate,           -- UI: "Sale Rate"

        PCM.Margin,                                     -- Margin

        /* Discount : DiscountPercent khaali ho to DiscPercent try karo.
           (Filhaal dono hi poore table me 0 hain -- upar ka note padhein.) */
        COALESCE(NULLIF(B.DiscountPercent, 0), B.DiscPercent, 0)
                                 AS DiscountPercent,    -- Discount %

        B.DiscountAmount,                               -- Discount Amount

        E.TaxName,                                      -- Tax Name

        B.TaxRate,                                      -- Tax %

        B.TaxAmount,                                    -- Tax Amount

        B.ItemValue,                                    -- Amount

        D.WarehouseName                                 -- Warehouse

    FROM PurchaseHeader A
    INNER JOIN PurchaseDetail B
        ON A.SerialNumber   = B.SerialNumber
    INNER JOIN ProductMaster C
        ON C.ProductID      = B.ProductID
    LEFT JOIN ProductMaster PM
        ON PM.ProductID     = B.ProductID
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
GO
