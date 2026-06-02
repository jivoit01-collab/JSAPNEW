using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Sap.Data.Hana;
using ServiceStack;
using System.Data;

namespace JSAPNEW.Services.Implementation
{
    public class DocumentDispatchService : IDocumentDispatchService
    {
        private readonly IConfiguration _configuration;
        private readonly string _HanaconnectionString;
        private readonly string _connectionString;
        public DocumentDispatchService(IConfiguration configuration)
        {
            _configuration = configuration;
            _HanaconnectionString = _configuration.GetConnectionString("LiveHanaConnection");
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<DocumentDispatchModels>> GetGRPOAsync()
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var sql = "CALL \"JIVO_OIL_HANADB\".\"GET_AVAILABLE_GRPO\"()";

                var result = await connection.QueryAsync<DocumentDispatchModels>(sql);
                return result;
            }
        }

        public async Task<IEnumerable<DocumentDispatchModels>> GetPOAsync()
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var sql = "CALL \"JIVO_OIL_HANADB\".\"GET_AVAILABLE_PO\"()";

                var result = await connection.QueryAsync<DocumentDispatchModels>(sql);
                return result;
            }
        }

        public async Task<IEnumerable<DocumentDispatchModels>> GetGoodReturnAsync()
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var sql = "CALL \"JIVO_OIL_HANADB\".\"GET_AVAILABLE_GR\"()";

                var result = await connection.QueryAsync<DocumentDispatchModels>(sql);
                return result;
            }
        }
        public async Task<IEnumerable<DocumentDispatchModels>> GetAPdraftAsync()
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var sql = "CALL \"JIVO_OIL_HANADB\".\"GET_AVAILABLE_APDRAFT\"()";

                var result = await connection.QueryAsync<DocumentDispatchModels>(sql);
                return result;
            }
        }
        public async Task<IEnumerable<DispatchResponse>> SaveDocumentAttachmentsInHanaAsync(HanaDocumentDispatchModels request)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("p_docId", request.p_docId);
                parameters.Add("p_docType", request.p_docType);
                if (request.p_branch == "OIL")
                {
                    parameters.Add("p_branch", "JIVO_OIL_HANADB");
                }
                else
                {
                    parameters.Add("p_branch", "JIVO_BEVERAGES_HANADB");
                }

                var result = await connection.QueryAsync<DispatchResponse>(
                    "CALL \"JIVO_OIL_HANADB\".\"PRC_INS_jsDocumentAttachments\"(?,?,?)",
                    parameters);

                return result;
            }
        }
        public async Task<int> GetLastBundleIdAsync(int lastBundleId, string mode)
        {
            var sqlQuery = "EXEC [dds].[UpdateLastBundleId] @lastBundleId, @mode";

            using (var connection = new SqlConnection(_connectionString))
            {
                if (mode == "select")
                {
                    // In select mode, we don't need to send lastBundleId
                    return await connection.QuerySingleAsync<int>(
                        "EXEC [dds].[UpdateLastBundleId] NULL, @mode",
                        new { mode }
                    );
                }
                else
                {
                    // For insert or update mode
                    await connection.ExecuteAsync(sqlQuery, new { lastBundleId, mode });
                    return lastBundleId;
                }
            }
        }
        public async Task<bool> SaveDocumentAttachmentsAsync(List<SaveDocumentAttachmentModel> attachments)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var table = new DataTable();
            table.Columns.Add("bundleId", typeof(string));
            table.Columns.Add("attachment", typeof(string));
            table.Columns.Add("docId", typeof(string));
            table.Columns.Add("docType", typeof(string));
            table.Columns.Add("branch", typeof(string));
            table.Columns.Add("createdBy", typeof(int));
            table.Columns.Add("createdOn", typeof(DateTime));
            table.Columns.Add("status", typeof(string));

            foreach (var item in attachments)
            {
                table.Rows.Add(
                    item.BundleId,
                    item.Attachment,
                    item.DocId,
                    item.DocType,
                    item.Branch,
                    item.CreatedBy,
                    item.CreatedOn,
                    "S"
                );
            }

            var parameters = new DynamicParameters();
            parameters.Add("@Rows", table.AsTableValuedParameter("dds.DocumentAttachmentTVP"));

            await connection.ExecuteAsync("[dds].[SaveDocumentAttachments]", parameters, commandType: CommandType.StoredProcedure);

            return true;
        }

        public async Task<IEnumerable<DocumentAttachmentModel>> GetDocumentByBundleIdAsync(string bundleId)
        {
            var sqlQuery = "EXEC [dds].[GetDocumentAttachmentsByBundleId] @bundleId";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<DocumentAttachmentModel>(
                    sqlQuery,
                    new { bundleId }
                );
            }
        }
        public async Task<IEnumerable<DispatchResponse>> UpdateDocumentStatusAsync(List<UpdateDocumentModel> requests)
        {
            var resultList = new List<DispatchResponse>();

            using (var connection = new SqlConnection(_connectionString))
            {
                foreach (var request in requests)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("id", request.id);
                    parameters.Add("status", request.status);
                    parameters.Add("receivedBy", request.receivedBy);
                    parameters.Add("docId", request.docId);
                    parameters.Add("rejectedReason", string.IsNullOrWhiteSpace(request.rejectedReason) ? null : request.rejectedReason);
                    parameters.Add("BundleStatus", string.IsNullOrWhiteSpace(request.BundleStatus) ? null : request.BundleStatus);

                    var result = await connection.QueryAsync<DispatchResponse>(
                        "EXEC [dds].[UpdateDocumentStatus] @id, @status, @receivedBy, @docId, @rejectedReason, @BundleStatus",
                        parameters
                    );

                    resultList.AddRange(result);
                }
            }

            return resultList;
        }
        public async Task<IEnumerable<DispatchResponse>> SaveBundleStatusModelAsync(SaveBundleStatusModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bundleId", request.bundleId);
                parameters.Add("status", request.status);
                parameters.Add("createdBy", request.createdBy);
                var result = await connection.QueryAsync<DispatchResponse>(
                    "EXEC [dds].[SaveBundleStatus] @bundleId,@status, @createdBy",
                    parameters
                );
                return result;
            }
        }

        public async Task<IEnumerable<DocumentModel>> GetRejectedDocumentsAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);

                var result = await connection.QueryAsync<DocumentModel>(
                    "EXEC [dds].[GetRejectDoc] @userId",
                    parameters
                );
                return result;
            }
        }

        public async Task<IEnumerable<DocumentModel>> GetUserDocumentsAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);

                var result = await connection.QueryAsync<DocumentModel>(
                    "EXEC [dds].[GetUserDocumentAttachments] @userId",
                    parameters
                );
                return result;
            }
        }

        public async Task<IEnumerable<PendingDocumentModel>> GetRecieverPendingDataAsync(int company, string status)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                var branch = company == 1 ? "OIL" : "BEVERAGES";

                parameters.Add("branch", branch);
                parameters.Add("status", status);
                var result = await connection.QueryAsync<PendingDocumentModel>(
                    "EXEC [dds].[GetPendingData] @branch, @status",
                    parameters
                );
                return result;
            }
        }

        public async Task<IEnumerable<DocumentModel>> GetRecieverActionDataAsync(int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                var branch = company == 1 ? "OIL" : "BEVERAGES";
                parameters.Add("branch", branch);
                var result = await connection.QueryAsync<DocumentModel>(
                    "EXEC [dds].[GetRecieverActionData] @branch",
                    parameters
                );
                return result;
            }
        }

        public async Task<IEnumerable<RejectDocumentModel>> GetRejectedDataAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);
                var result = await connection.QueryAsync<RejectDocumentModel>(
                    "EXEC [dds].[GetPendingDocumentAttachmentsByUser] @userId",
                    parameters
                );
                return result;
            }
        }

        public async Task<IEnumerable<DispatchResponse>> UpdateNotRecievedStatusAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("id", id);
                var result = await connection.QueryAsync<DispatchResponse>(
                    "EXEC [dds].[UpdateAndInsertDocumentAttachment] @id",
                    parameters
                );
                return result;
            }
        }
    }
}