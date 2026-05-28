using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentModel>> GetPendingPaymentsAsync(int userId, int company);
        Task<IEnumerable<PaymentModel>> GetApprovePaymentsAsync(int userId, int company);
        Task<IEnumerable<PaymentModel>> GetRejectedPaymentsAsync(int userId, int company);
        Task<IEnumerable<PaymentModel>> GetAllPaymentsAsync(int userId, int company);
        Task<IEnumerable<PaymentDetailsModel>> GetPaymentDetailsAsync(int docEntry, int company);
        Task<object> ApprovePaymentAsync(int paymentId, int userId);
        Task<object> RejectPaymentAsync(int paymentId, int userId, string description);
        Task<IEnumerable<TotalPayInsightsModel>> GetPaymentInsightsAsync(int userId, int company);
    }
}