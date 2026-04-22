using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IAdminService
    {
        BillSummaryDto GetSummary();
        List<BillDetailDto> GetBillDetails(string accountName, string fromDate, string toDate);
        bool DeleteAttachment(int vchNumber, string wwwrootPath);
    }
}
