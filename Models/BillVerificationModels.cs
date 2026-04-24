namespace JSAPNEW.Models
{
    public class BillDetailDto
    {
        public string AccountName { get; set; }
        public object VchNumber { get; set; }
        public string VoucherDate { get; set; }
        public object BillAmount { get; set; }
        public string SupplierRef { get; set; }
        public string SupplierRefDate { get; set; }
        public string DueDate { get; set; }
        public string PaymentDate { get; set; }
        public string AttachmentPath { get; set; }
        public string Attachment { get; set; }  // "File" or "No File"
        public string MakerRemark { get; set; }
        public string CheckerRemark { get; set; }
        public string CheckerStatus { get; set; }
        public string MakerStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string SerialNumber { get; set; }
        public object TotalQuantity { get; set; }
        public object TotalItemValue { get; set; }
        public object TotalItems { get; set; }
    }

    public class InvoiceItemDto
    {
        public string ProductName { get; set; }
        public object Quantity { get; set; }
        public object Rate { get; set; }
        public object Tax { get; set; }
        public object Amount { get; set; }
        public string WarehouseName { get; set; }
        public string TaxName { get; set; }
        public object ItemValue { get; set; }
    }
}
public class BillSummaryDto
{
    public int TotalBills { get; set; }
    public int PendingMaker { get; set; }
    public int ApprovedChecker { get; set; }
    public int TotalPaid { get; set; }
}
