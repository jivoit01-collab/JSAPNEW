using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IMakerService
    {
        List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, decimal? serialNumber);
        List<string> GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate);
        List<InvoiceItemDto> GetInvoiceItemDetails(int vchNumber);
    }
}
