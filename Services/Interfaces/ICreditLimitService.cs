using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Services.Interfaces
{
    public interface ICreditLimitService
    {
        Task<OpenCslmResponse> OpenCslmAsync(OpenCslmRequest request);
        Task<IEnumerable<GetCustomerCardModel>> GetCustomerCardsAsync(int company);
        Task<CreateDocumentResult> CreateDocumentAsync(CreateDocumentDto request);
        Task<IEnumerable<ApprovedDocumentDto>> GetApprovedDocumentsAsync(CLDocumentRequest request);
        Task<IEnumerable<PendingDocumentDto>> GetPendingDocumentsAsync(CLDocumentRequest request);
        Task<IEnumerable<RejectedDocumentDto>> GetRejectedDocumentsAsync(CLDocumentRequest request);
        Task<IEnumerable<AllDocumentDto>> GetAllDocumentsAsync(CLDocumentRequest request);
        Task<IEnumerable<CreditDocumentInsightResponse>> GetCreditDocumentInsightAsync(CLDocumentRequest request);
        Task<IEnumerable<UserDocumentInsightsResponse>> GetUserDocumentInsightsAsync(UserDocumentInsightsRequest request);
        Task<IEnumerable<DocumentDetailDto>> GetDocumentDetailAsync(int documentId);
        Task<IEnumerable<CreditLimitApprovalFlowDto>> GetApprovalFlowAsync(long flowId);
        Task<IEnumerable<UserDocumentDto>> GetUserDocumentsAsync(UserDocumentRequest request);
        Task<CreditLimitApiResponse> ApproveDocumentAsync(ApproveDocumentRequest request);
        Task<CreditLimitApiResponse> RejectDocumentAsync(RejectDocumentRequest request);
        Task<IEnumerable<DocumentDetailDto>> GetDocumentDetailUsingFlowIdAsync(int flowId);
        Task<FlowStatusRequest> GetFlowStatusAsync(int flowId);
        Task<CreditLimitApiResponse> UpdateHanaStatusAsync(CreditLimitUpdateHanaStatus request);
        Task<string> UpdateCreditLimitAsync(int flowId);
        Task<IEnumerable<UserIdsForNotificationModel>> GetCLUserIdsSendNotificatiosAsync(int flowId);
        Task<CreditLimitApiResponse> SendPendingCLCountNotificationAsync();
        Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetCurrentUsersSendNotificationAsync(int userDocumentId);
        Task<CreateDocumentResultV2> CreateDocumentWithAttachmentAsyncV2(CreateDocumentDtoV2 request, IFormFile attachment);
        Task<CreditDocumentDetailModel> GetCreditDocumentDetailAsyncV2(int documentId, IUrlHelper urlHelper);

    }
}
