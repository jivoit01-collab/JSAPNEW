using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IPaymentCheckerService
    {
        List<BillDetailDto> GetPaidBillDetails(DateTime? fromDate, DateTime? toDate, string accountName);
        List<InvoiceItemDto> GetInvoiceItemDetails(int vchNumber);
    }
}