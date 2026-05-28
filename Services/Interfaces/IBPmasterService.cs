using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Services.Interfaces
{
    public interface IBPmasterService
    {
        Task<BPMasterResponse> InsertBPMasterAsync(InsertBPMasterDataModel model);
        Task<ApproveOrRejectBpResponse> ApproveBPAsync(ApproveOrRejectBpRequest request);
        Task<ApproveOrRejectBpResponse> RejectBPAsync(ApproveOrRejectBpRequest request);
        Task<IEnumerable<ApprovedBpModel>> GetApprovedBPsAsync(int userId, int companyId, string month = null);
        Task<BPCountModel> GetBPCountsAsync(string month, int userId);
        Task<IEnumerable<PendingBpModel>> GetPendingBpAsync(int userId, int companyId, string month = null);
        Task<IEnumerable<RejectedBPModel>> GetRejectedBpAsync(int userId, int companyId, string month = null);
        Task<SingleBPDataModel> GetSingleBPDataAsync(int bpCode, IUrlHelper urlHelper);
        Task<IEnumerable<DistinctBankNameModel>> GetDistinctBankNameAsync(int company);
        Task<IEnumerable<SLPnameModel>> GetSLPnameAsync(int company);
        Task<IEnumerable<ChainModel>> GetChainAsync(int company, string BPType, string IsStaff);
        Task<IEnumerable<GetCountryModel>> GetCountryAsync(int company);
        Task<IEnumerable<GetMainGroup>> GetMaingroupAsync(int company, string BPType, string IsStaff);
        Task<IEnumerable<GetMSMEType>> GetMSMEtypeAsync(int company);
        Task<IEnumerable<GroupNameResponse>> GetGroupNameByBPTypeAsync(int company, string bpType,string isStaff);
        Task<IEnumerable<PaymentGroupModel>> GetDistinctPaymentGroupsAsync(int company);
        Task<IEnumerable<BPStateModel>> GetDistinctStatesAsync(int company, string CountryCode);
        Task<IEnumerable<BPGetCard>> BPGetCardInfoAsync(int company, string BPType, string IsStaff);
        Task<IEnumerable<UniquePANModel>> GetUniquePANsAsync(int company);
        Task<IEnumerable<GSTMismatchByStateModel>> GetGSTMismatchByStateAsync(int company, string stateCode);
        Task<IEnumerable<GetPricelist>> GetPricelistAsync(int company);
        Task<UidResponse> CheckAddressUidAsync(string addressUid);
        Task<UidResponse> CheckContactUidAsync(string contactUid);
        Task<IEnumerable<GetPanByBranch>> GetBpPANByBranchAsync(string Branch, int company);
        Task<SPAData> GetSPADataAsync(int masterId);
        Task<IEnumerable<MergeBpModel>> GetMergeBpModelAsync(int userId, int companyId, string month = null);
        Task<BPmasterModels> UpdateBPMasterAsync(BPMasterUpdateRequest request);
        Task<BPmasterModels> UpdateSapDataAsync(BpSapDataUpdateRequest model);
        Task<IEnumerable<BPinsightsModel>> GetBPInsightsAsync(int userId, int companyId, string? month);
        Task<IEnumerable<BPinsightsModel>> GetBPInsightsByCreatorAsync(int userId, int companyId, string? month);
        Task<IEnumerable<BPApprovalFlowModel>> GetBPApprovalFlowAsync(int flowId);
    }
}
