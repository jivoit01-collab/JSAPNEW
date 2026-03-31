using System.ComponentModel.DataAnnotations;
namespace JSAPNEW.Models
{
    public class PaymentModel
    {
        public string BRANCH { get; set; }
        public int DocEntry { get; set; }
        public int TransId { get; set; }
        public string CardCode { get; set; }
        public string TargetPath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string CardName { get; set; }
        public int DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public string DocType { get; set; }
        public decimal DocTotal { get; set; }
        public string ACCOUNT { get; set; }
        public string IFSC { get; set; }
        public string Status { get; set; }
    }

    public class TotalPayInsightsModel
    {
        public int PendingPayments { get; set; }
        public int ApprovedPayments { get; set; }
        public int RejectedPayments { get; set; }
        public int TotalPayments => PendingPayments + ApprovedPayments + RejectedPayments;
    }

    public class PaymentDetailsModel
    {
        public string Type { get; set; }
        public string Branch { get; set; }
        public int DocEntry { get; set; }
        public string CardName{ get; set; }
        public string PaymentMode { get; set; }
        public string Comments { get; set; }
        public string NUMATCARD { get; set; }
        public int DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public string DocType { get; set; }
        public decimal DocTotal { get; set; }
        public string DueDate { get; set; }
        public string TOTALPAYMENT { get; set; }
        public string TOTAL { get; set; }
        public string trgtPath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
    }
}
