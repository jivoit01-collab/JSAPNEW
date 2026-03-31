using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IReportsService
    {
        Task<IEnumerable<RealiseReportModels>> GetRealiseReportAsync(DateTime FROMDATE, DateTime TODATE);
        Task<IEnumerable<Variety>> GetVarietyAsync();
        Task<IEnumerable<Brand>> GetBrandAsync();
        Task<ApprovalStatusReportResult> GetApprovalStatusReportAsync(int userId, int company, string month);
        Task<BudgetByCompanyModel> GetBudgetByCompanyAsync(int company,int docEntry,string cardName,string month,string status);
    }
}
