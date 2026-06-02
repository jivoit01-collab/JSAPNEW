using System.Data;
using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Any;
using Sap.Data.Hana;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JSAPNEW.Services.Implementation
{
    public class ReportsService : IReportsService
    {
        private readonly IConfiguration _configuration;
        private readonly string _HanaconnectionString;

        private readonly string _sapBaseUrl;

        public ReportsService(IConfiguration configuration)
        {
            _configuration = configuration;
            _sapBaseUrl = _configuration.GetValue<string>("SapServiceLayerUrl");
            _HanaconnectionString = _configuration.GetConnectionString("LiveHanaConnection");

        }
        public async Task<IEnumerable<RealiseReportModels>> GetRealiseReportAsync(DateTime FROMDATE, DateTime TODATE)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {

                var parameters = new DynamicParameters();
                parameters.Add("FROMDATE", FROMDATE);
                parameters.Add("TODATE", TODATE);

                var result = await connection.QueryAsync<RealiseReportModels>(
                    "CALL \"JIVO_OIL_HANADB\".\"REPORT_SALES_ANALYSIS\"(?,?)",
                    parameters);
                return result;
            }
        }
        public async Task<IEnumerable<Variety>> GetVarietyAsync()
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var result = await connection.QueryAsync<Variety>(
                    "CALL \"JIVO_OIL_HANADB\".\"SELECT_VARIETY\"()");
                return result;
            }
        }
        public async Task<IEnumerable<Brand>> GetBrandAsync()
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var result = await connection.QueryAsync<Brand>(
                    "CALL \"JIVO_OIL_HANADB\".\"SELECT_U_BRAND\"()");
                return result;
            }
        }

        public async Task<ApprovalStatusReportResult> GetApprovalStatusReportAsync(int userId, int company, string month)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var advanceReport = await connection.QueryAsync<ExpensesModels>(
                    "EXEC [adv].[jsGetExpenseByUserId] @userId, @company, @month",
                    new { userId, company, month });

                var bomReport = await connection.QueryAsync<BomByUserIdModel>(
                    "EXEC [bom].[jsGetBomByUserId] @userId, @company, @month",
                    new { userId, company, month });

                var ItemReport = await connection.QueryAsync<GetItemByIdModel>(
                    "EXEC [imc].[jsGetItemByUserId] @userId, @company, @month",
                    new { userId, company, month });

                return new ApprovalStatusReportResult
                {
                    Advance = advanceReport,
                    BOMs = bomReport,
                    Items = ItemReport
                };
            }
        }

        public async Task<BudgetByCompanyModel> GetBudgetByCompanyAsync(
           int company,
           int docEntry,
           string cardName,
           string month,
           string status)
        {
            try
            {
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@company", company);
                    parameters.Add("@docEntry", docEntry == 0 ? (int?)null : docEntry);
                    parameters.Add("@cardName", string.IsNullOrWhiteSpace(cardName) ? null : cardName);
                    parameters.Add("@month", string.IsNullOrWhiteSpace(month) ? null : month);
                    parameters.Add("@status", string.IsNullOrWhiteSpace(status) ? null : status);

                    var budgets = await conn.QueryAsync<BudgetByCompanyListModel>(
                        "[bud].[jsSearchBudgetsByCompany]",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var result = new BudgetByCompanyModel
                    {
                        budgets = budgets.ToList()
                    };

                    return result;
                }
            }
            catch (SqlException sqlEx)
            {
                throw new Exception("Database error occurred while retrieving budget data", sqlEx);
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting budget data by company", ex);
            }
        }
    }
}
