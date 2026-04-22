using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IInvoicePaymentService
    {
        List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, decimal? serialNumber);
        List<InvoiceItemDto> GetInvoiceItemDetails(int vchNumber);
    }
}
