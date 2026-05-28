using Azure.Core;
using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JSAPNEW.Services.Implementation
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public PaymentService(IConfiguration configuration, Interfaces.ITokenService tokenService)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<PaymentModel>> GetPendingPaymentsAsync(int userId, int company)
        {
            var sqlQuery = "EXEC pay.jsGetPendingPayments @userId, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<PaymentModel>(
                       sqlQuery,new { userId, company }
                 );
            }
        }

        public async Task<IEnumerable<PaymentModel>> GetApprovePaymentsAsync(int userId, int company)
        {
            var sqlQuery = "EXEC pay.jsGetApprovePayments @userId, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<PaymentModel>(
                   sqlQuery,new { userId, company }
                );
            }
        }

        public async Task<IEnumerable<PaymentModel>> GetRejectedPaymentsAsync(int userId, int company)
        {
            var sqlQuery = "EXEC pay.jsGetRejectedPayments @userId, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<PaymentModel>(
                    sqlQuery,new { userId, company }
                );
            }
        }

        public async Task<IEnumerable<PaymentModel>> GetAllPaymentsAsync(int userId, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Fetch data from the stored procedures
                var pendingPayments = await connection.QueryAsync<PaymentModel>(
                    "EXEC pay.jsGetPendingPayments @userId, @company",
                    new { userId, company });

                var approvedPayments = await connection.QueryAsync<PaymentModel>(
                    "EXEC pay.jsGetApprovePayments @userId, @company",
                    new { userId, company });

                var rejectedPayments = await connection.QueryAsync<PaymentModel>(
                    "EXEC pay.jsGetRejectedPayments @userId, @company",
                    new { userId, company });

                // Add status to each record
                foreach (var Payment in pendingPayments)
                {
                    Payment.Status = "Pending";
                }

                foreach (var Payment in approvedPayments)
                {
                    Payment.Status = "Approved";
                }

                foreach (var Payment in rejectedPayments)
                {
                    Payment.Status = "Rejected";
                }

                // Combine all records into one list
                var allPayments = pendingPayments.ToList();
                allPayments.AddRange(approvedPayments);
                allPayments.AddRange(rejectedPayments);

                return allPayments;
            }
        }

        public async Task<IEnumerable<PaymentDetailsModel>> GetPaymentDetailsAsync(int docEntry, int company)
        {
            var sqlQuery = "EXEC pay.jsGetPaymentDetails @docEntry, @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<PaymentDetailsModel>(
                    sqlQuery, new { docEntry, company }
                );
            }
        }
        public async Task<object> ApprovePaymentAsync(int paymentId, int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@paymentId", paymentId);
                parameters.Add("@userId", userId);
                //parameters.Add("@description", description);

                var result = await connection.ExecuteScalarAsync<int>("pay.jsPaymentApprove", parameters, commandType: CommandType.StoredProcedure);
                return result;
            }
        }

        public async Task<object> RejectPaymentAsync(int paymentId, int userId, string description)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@paymentId", paymentId);
                parameters.Add("@userId", userId);
                parameters.Add("@description", description);

                var result = await connection.ExecuteScalarAsync<int>("pay.jsPaymentReject", parameters, commandType: CommandType.StoredProcedure);
                
                return result;
            }
        }

        public async Task<IEnumerable<TotalPayInsightsModel>> GetPaymentInsightsAsync(int userId, int company)
        {
            var sqlQuery = "EXEC pay.jsGetPaymentInsights @userId , @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TotalPayInsightsModel>(
                    sqlQuery, new { userId, company }
                );
            }
        }
    }
}
