using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Services
{
    public interface IBillVerificationService 
    {
        //for maker 

        List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, decimal? serialNumber);
        List<string> GetAccountSuggestions(string term, DateTime? fromDate, DateTime? toDate);
        List<InvoiceItemDto> GetInvoiceItemDetails(int vchNumber);


        //for checker
        List<BillDetailDto> GetBillDetails(DateTime? fromDate, DateTime? toDate, string accountName, string status);
        List<InvoiceItemDto> GetInvoiceItemDetails(decimal serialNumber);
        void UpdateCheckerStatus(int vchNumber, string status, string remark, int? userId, string userName);

        // for invoice payment
        List<BillDetailDto> GetBillDetailsIP(DateTime? fromDate, DateTime? toDate, string accountName, decimal? serialNumber);
        List<InvoiceItemDto> GetInvoiceItemDetailsIP(int vchNumber);
        bool MarkAsPaid(decimal vchNumber, int? userId, string userName);
        bool RejectByPaymentMaker(decimal vchNumber, string remark, int? userId, string userName);

        // for invoice payment checker

        List<BillDetailDto> GetPaidBillDetails(DateTime? fromDate, DateTime? toDate, string accountName);
        List<InvoiceItemDto> GetInvoiceItemDetailsPC(int vchNumber);
        void MarkVerified(int vchNumber, int? userId, string userName);
        bool RejectPayment(int vchNumber, string remark, int? userId, string userName);

        //for admin
        BillSummaryDto GetSummary();
        List<BillDetailDto> GetBillDetails(string accountName, string fromDate, string toDate);
        bool DeleteAttachment(int vchNumber, string wwwrootPath);
    }
}
