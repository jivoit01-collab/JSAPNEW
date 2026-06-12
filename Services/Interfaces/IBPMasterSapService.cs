using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IBPMasterSapService
    {
        Task<BpSapPostResult> PostBusinessPartnerAsync(BpSapPostRequest request, CancellationToken cancellationToken = default);
        Task<BpControlAccountValidationResult> ValidateControlAccountAsync(int companyId, string accountCode, string bpType, CancellationToken cancellationToken = default);
        Task<IEnumerable<GroupNameResponse>> GetBusinessPartnerGroupsAsync(int companyId, string bpType, CancellationToken cancellationToken = default);
        Task<IEnumerable<DistinctBankNameModel>> GetBankCodesAsync(int companyId, string countryCode = "IN", CancellationToken cancellationToken = default);
    }
}
