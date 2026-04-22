using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface ICheckerService
    {
        List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName);
        List<InvoiceItemDto> GetInvoiceItemDetails(decimal serialNumber);
        void UpdateCheckerStatus(int vchNumber, string status, string remark);
    }
}

