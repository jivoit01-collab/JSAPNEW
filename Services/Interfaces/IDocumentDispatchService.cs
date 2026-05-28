using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Services.Interfaces
{
    public interface IDocumentDispatchService
    {
        Task<IEnumerable<DocumentDispatchModels>> GetGRPOAsync();
        Task<IEnumerable<DocumentDispatchModels>> GetPOAsync();
        Task<IEnumerable<DocumentDispatchModels>> GetGoodReturnAsync();
        Task<IEnumerable<DocumentDispatchModels>> GetAPdraftAsync();
        Task<int> GetLastBundleIdAsync(int lastBundleId, string mode);
        Task<bool> SaveDocumentAttachmentsAsync(List<SaveDocumentAttachmentModel> attachments);
        Task<IEnumerable<DispatchResponse>> SaveDocumentAttachmentsInHanaAsync(HanaDocumentDispatchModels request);
        Task<IEnumerable<DocumentAttachmentModel>> GetDocumentByBundleIdAsync(string bundleId);
        Task<IEnumerable<DispatchResponse>> UpdateDocumentStatusAsync(List<UpdateDocumentModel> requests);
        Task<IEnumerable<DispatchResponse>> SaveBundleStatusModelAsync(SaveBundleStatusModel request);
        Task<IEnumerable<DocumentModel>> GetRejectedDocumentsAsync(int userId);
        Task<IEnumerable<DocumentModel>> GetUserDocumentsAsync(int userId);
        Task<IEnumerable<PendingDocumentModel>> GetRecieverPendingDataAsync(int company,string status);
        Task<IEnumerable<DocumentModel>> GetRecieverActionDataAsync(int company);
        Task<IEnumerable<RejectDocumentModel>> GetRejectedDataAsync(int userId);
        Task<IEnumerable<DispatchResponse>> UpdateNotRecievedStatusAsync(int id);
    }
}
