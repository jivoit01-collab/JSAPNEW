using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;
using ServiceStack;

namespace JSAPNEW.Services.Interfaces
{
    public interface IQcService
    {
        Task<CreateFormResponse> CreateFormAsync(CreateFormRequest request);
        Task<CreateParameterResponse> CreateParameterAsync(CreateParameterRequest request);
        Task<CreateSubParameterResponse> CreateSubParameterAsync(CreateSubParameterRequest request);
        Task<GetFormDataUsingDocEntryResponse> GetFormDataUsingDocEntryAsync(int docEntry, int company, IUrlHelper urlHelper);
        Task<GetFormStructureResponse> GetFormStructureAsync(int formId);
        Task<GetFormStructureResponse> GetFormStructureAsyncV2();
        Task<List<FormModel>> GetFormUsingCreatedByAsync(string userId);
        Task<QcResponse> InsertItemDataAsync(InsertItemDataRequest request);
        Task<QcResponse> InsertItemParameterDataAsync(InsertItemParameterDataRequest request);
        Task<QcResponse> SaveSubParameterDataAsync(List<SubParameterRequest> subParameters, IFormFileCollection files);
        Task<IEnumerable<ProductionDocNumModel>> GetProductionDocNumAsync(int company, int DocId);
        Task<IEnumerable<QcProductionDataModel>> GetProductionDataAsync(int DocNum, int docId, int company);
        Task<IEnumerable<DocumentInsightModel>> GetDocumentInsightsAsync(int userId, int companyId, string month);
        Task<List<QCPendingDocumentModel>> GetPendingDocumentsAsync(int userId, int companyId, string month);
        Task<List<QCApprovedDocumentModel>> GetApprovedDocumentsAsync(int userId, int companyId, string month);
        Task<List<QCRejectedDocumentModel>> GetRejectedDocumentsAsync(int userId, int companyId, string month);
        Task<List<QcAllDocumentModel>> GetAllQcDocumentAsync(int userId, int companyId, string month);
        Task<IEnumerable<QCApprovalFlowModel>> GetQCApprovalFlowAsync(int flowId);
        Task<QcResponse> ApproveDocumentAsync(QcApprovalRequest request);
        Task<QcResponse> RejectDocumentAsync(QcRejectRequest request);
        Task<IEnumerable<FormsWithUsersModel>> GetFormsWithUsersAsync();
        Task<IEnumerable<GetItemDataIdModel>> GetItemDataIdAsync(int docEntry, int lineNum);
        Task<IEnumerable<UserIdsForNotificationModel>> GetQcUserIdsSendNotificatiosAsync(int userDocumentId);
        Task<Response> SendPendingQcCountNotificationAsync();
        Task<IEnumerable<QcProductionDataModel>> GetProductionDataUsingLineAsync(int DocEntry, int docId, int LineNum, int company);
        Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetQcCurrentUsersSendNotificationAsync(int userDocumentId);
        Task<UpdateFormResponse> UpdateFormAsync(UpdateFormModel model);
        //Task<UpdateFormResponse> UpdateFormWithParametersAsync(UpdateFormModel request);
    }
}
