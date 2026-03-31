using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace JSAPNEW.Models
{
    public class RealiseReportModels
    {
        public DateTime DocDate { get; set; }
        public string U_Main_Group { get; set; }
        public string U_Chain { get; set; }
        public string State { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string SKU { get; set; }
        public string ItmsGrpNam { get; set; }
        public int ItmsGrpCod { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string SalPackUn { get; set; }
        public string Quantity { get; set; }
        public string Liter { get; set; }
        public string LineTotal { get; set; }
        public string Realise { get; set; }
        public string SchemeQty { get; set; }
        public string SchemeSaleAmt { get; set; }
        public string SchemeAmt { get; set; }
        public string COGS { get; set; }
        public int TransId { get; set; }
        public string VARIETY { get; set; }
        public string Type { get; set; }
        public string Brand { get; set; }
        public string Location { get; set; }
        public string Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public string Box { get; set; }

        [JsonProperty("Liter(Y/N)")]
        public string LiterYN { get; set; }
        public string PACKTYPE { get; set; }
    }

    public class Variety
    {
        public string PrcCode { get; set; }
        public string PrcName { get; set; }
    }
    public class Brand
    {
        public string U_Brand { get; set; }
    }
    public class ApprovalStatusReportResult
    {
        public IEnumerable<ExpensesModels> Advance { get; set; }
        public IEnumerable<BomByUserIdModel> BOMs { get; set; }
        public IEnumerable<GetItemByIdModel> Items { get; set; }
    }

    public class BudgetByCompanyListModel
    {
        public int BudgetId { get; set; }
        public int ObjType { get; set; }
        public string Company { get; set; }
        public int CompanyId { get; set; }
        public int DocEntry { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime DocDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrentMonth { get; set; }
        public string BudgetOwner { get; set; }
        public string OwnerCode { get; set; }
        public string ApproverName { get; set; }
        public string ApprovalCode { get; set; }
    }

    public class BudgetByCompanyModel
    {
        public List<BudgetByCompanyListModel> budgets { get; set; }
    }
}
