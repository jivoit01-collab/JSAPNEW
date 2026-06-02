using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Data.SqlClient;
using Sap.Data.Hana;
using ServiceStack;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    public class QcService : IQcService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        public QcService(IConfiguration configuration, INotificationService notificationService, IUserService userService)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            var activeEnv = configuration["ActiveEnvironment"];  // "Test" or "Live"
            _hanaSettings = configuration.GetSection($"HanaSettings:{activeEnv}")
                                         .Get<Dictionary<int, HanaCompanySettings>>();
            _notificationService = notificationService;
            _userService = userService;
        }

        public async Task<CreateFormResponse> CreateFormAsync(CreateFormRequest request)
        {
            var response = new CreateFormResponse();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[qc].[CreateForm]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Basic Parameters
                    command.Parameters.AddWithValue("@formNumber", request.FormNumber);
                    command.Parameters.AddWithValue("@formDate", (object?)request.FormDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@status", request.Status);
                    command.Parameters.AddWithValue("@remarks", (object?)request.Remarks ?? DBNull.Value);
                    command.Parameters.AddWithValue("@createdBy", request.CreatedBy);

                    // Quality Setting Parameters
                    command.Parameters.AddWithValue("@qualityCheckMin", request.QualityCheckMin);
                    command.Parameters.AddWithValue("@qualityCheckMax", request.QualityCheckMax);
                    command.Parameters.AddWithValue("@minValueToPassQC", (object?)request.MinValueToPassQC ?? DBNull.Value);
                    command.Parameters.AddWithValue("@randomBoxCheck", request.RandomBoxCheck);

                    // Documents Table-Valued Parameter
                    var tvp = new DataTable();
                    tvp.Columns.Add("documentTypeName", typeof(string));
                    tvp.Columns.Add("hanaId", typeof(int));
                    tvp.Columns.Add("isMandatory", typeof(bool));
                    tvp.Columns.Add("documentPath", typeof(string));

                    foreach (var doc in request.Documents)
                    {
                        tvp.Rows.Add(doc.DocumentTypeName, doc.HanaId, doc.IsMandatory, doc.DocumentPath);
                    }

                    var tvpParam = command.Parameters.AddWithValue("@documents", tvp);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "qc.DocumentTableType";

                    // Output Parameter
                    var outputParam = new SqlParameter("@newFormId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "Form created successfully.";
                    response.NewFormId = Convert.ToInt32(outputParam.Value);
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating form: " + ex.Message;
                response.NewFormId = 0;
            }
            return response;
        }

        /*public async Task<CreateFormResponse> CreateFormAsync(CreateFormRequest request)
        {
            var response = new CreateFormResponse();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[qc].[CreateForm]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Basic Parameters
                    command.Parameters.AddWithValue("@formNumber", request.FormNumber);
                    command.Parameters.AddWithValue("@formDate", (object?)request.FormDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@status", request.Status);
                    command.Parameters.AddWithValue("@remarks", (object?)request.Remarks ?? DBNull.Value);
                    command.Parameters.AddWithValue("@createdBy", request.CreatedBy);

                    // Quality Setting Parameters
                    command.Parameters.AddWithValue("@qualityCheckMin", request.QualityCheckMin);
                    command.Parameters.AddWithValue("@qualityCheckMax", request.QualityCheckMax);
                    command.Parameters.AddWithValue("@minValueToPassQC", (object?)request.MinValueToPassQC ?? DBNull.Value);
                    command.Parameters.AddWithValue("@randomBoxCheck", request.RandomBoxCheck);

                    // Documents Table-Valued Parameter
                    var tvp = new DataTable();
                    tvp.Columns.Add("documentTypeName", typeof(string));
                    tvp.Columns.Add("hanaId", typeof(int));
                    tvp.Columns.Add("isMandatory", typeof(bool));
                    tvp.Columns.Add("documentPath", typeof(string));

                    foreach (var doc in request.Documents)
                        tvp.Rows.Add(doc.DocumentTypeName, doc.HanaId, doc.IsMandatory, doc.DocumentPath);

                    var tvpParam = command.Parameters.AddWithValue("@documents", tvp);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "qc.DocumentTableType";

                    // Output Parameter
                    var outputParam = new SqlParameter("@newFormId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    int newFormId = Convert.ToInt32(outputParam.Value);
                    response.NewFormId = newFormId;
                    response.Success = true;
                    response.Message = "Form created successfully.";

                    // ✅ STEP 2️⃣: Get user list for notification
                    var stageUsers = await GetQcCurrentUsersSendNotificationAsync(newFormId);

                    // ✅ STEP 3️⃣: Send notifications (only once per token)
                    if (stageUsers != null && stageUsers.Any())
                    {
                        var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var notifiedUsers = new HashSet<int>();
                        var notificationLog = new StringBuilder();

                        foreach (var userGroup in stageUsers.GroupBy(u => u.UserId))
                        {
                            int userId = userGroup.Key;
                            if (notifiedUsers.Contains(userId))
                                continue;

                            // Get all FCM tokens for this user
                            var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);
                            if (fcmTokens == null || fcmTokens.Count == 0)
                            {
                                notificationLog.AppendLine($"⚠️ No FCM token for user {userId}.");
                                continue;
                            }

                            var uniqueTokens = fcmTokens
                                .Select(t => t.fcmToken?.Trim())
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            string title = "New Quality Check Form";
                            string body = $"A new Quality Check Form (ID: {newFormId}) has been created and awaits your action.";

                            var data = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() },
                        { "formId", newFormId.ToString() },
                        { "screen", "qc-form" }
                    };

                            foreach (var token in uniqueTokens)
                            {
                                if (sentTokens.Contains(token))
                                    continue;

                                await _notificationService.SendPushNotificationAsync(title, body, token, data);
                                sentTokens.Add(token);
                            }

                            notificationLog.AppendLine($"✅ Sent to user {userId} ({uniqueTokens.Count} device(s)).");
                            notifiedUsers.Add(userId);
                        }

                        // Optional: Log notification results
                        Console.WriteLine(notificationLog.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating form: " + ex.Message;
                response.NewFormId = 0;
            }

            return response;
        }
*/
        public async Task<CreateParameterResponse> CreateParameterAsync(CreateParameterRequest request)
        {
            var response = new CreateParameterResponse();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[qc].[CreateParameter]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Input parameters
                    command.Parameters.AddWithValue("@parameterName", request.ParameterName);
                    command.Parameters.AddWithValue("@formId", request.FormId);
                    command.Parameters.AddWithValue("@displayOrder", request.DisplayOrder);
                    command.Parameters.AddWithValue("@isActive", request.IsActive);

                    // Output parameter
                    var outputParam = new SqlParameter("@newParameterId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "Parameter created successfully.";
                    response.newParameterId = Convert.ToInt32(outputParam.Value);
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = "SQL Error: " + ex.Message;
                response.newParameterId = null;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
                response.newParameterId = null;
            }

            return response;
        }

        public async Task<CreateSubParameterResponse> CreateSubParameterAsync(CreateSubParameterRequest request)
        {
            var response = new CreateSubParameterResponse();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[qc].[CreateSubParameter]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Input parameters
                    command.Parameters.AddWithValue("@parameterId", request.ParameterId);
                    command.Parameters.AddWithValue("@subParameterName", request.SubParameterName);
                    command.Parameters.AddWithValue("@isImageMandatory", request.IsImageMandatory);
                    command.Parameters.AddWithValue("@displayOrder", request.DisplayOrder);
                    command.Parameters.AddWithValue("@isActive", request.IsActive);

                    // Output parameter
                    var outputParam = new SqlParameter("@newSubParameterId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "Sub-parameter created successfully.";
                    response.NewSubParameterId = Convert.ToInt32(outputParam.Value);
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = "SQL Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }

            return response;
        }

        public async Task<GetFormDataUsingDocEntryResponse> GetFormDataUsingDocEntryAsync(int docEntry, int company, IUrlHelper urlHelper)
        {
            var response = new GetFormDataUsingDocEntryResponse();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var multi = await connection.QueryMultipleAsync("[qc].[GetFormDataUsingDocEntry]",
                    new { docEntry }, commandType: CommandType.StoredProcedure))
                {
                    response.ItemData = (await multi.ReadAsync<ItemDataModel>()).AsList();
                    response.Form = await multi.ReadFirstOrDefaultAsync<FormModel>();
                    response.QualitySettings = (await multi.ReadAsync<QualitySettingModel>()).AsList();
                    response.Documents = (await multi.ReadAsync<QCDocumentModel>()).AsList();
                    response.Parameters = (await multi.ReadAsync<ParameterDataModel>()).AsList();

                    var subParameters = (await multi.ReadAsync<SubParameterDataModel>()).AsList();

                    // ✅ Handle SubParameter Download URLs
                    foreach (var subParam in subParameters)
                    {
                        if (string.IsNullOrEmpty(subParam.ImagePath))
                            continue;

                        string cleanFilePath = subParam.ImagePath.Replace("\\", "/").Trim();
                        if (cleanFilePath.StartsWith("/"))
                            cleanFilePath = cleanFilePath.Substring(1);

                        string fileName = Path.GetFileName(cleanFilePath);
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string fileExt = Path.GetExtension(fileName)?.TrimStart('.') ?? "";
                        string folderPath = Path.GetDirectoryName(cleanFilePath)?.Replace("\\", "/");

                        subParam.DownloadUrl = urlHelper.Action("AdvanceDownloadFile", "File", new
                        {
                            filePath = folderPath,
                            fileName = fileNameWithoutExt,
                            fileExt = fileExt
                        }, protocol: "http");
                    }

                    response.SubParameters = subParameters;

                    // ✅ Enrich ALL ItemData entries with HANA data
                    if (response.ItemData != null && response.ItemData.Any())
                    {
                        foreach (var item in response.ItemData)
                        {
                            try
                            {
                                var hanaList = await GetProductionDataUsingLineAsync(item.DocEntry, item.DocumentId, item.LineNum, company);
                                var hana = hanaList.FirstOrDefault();

                                if (hana != null)
                                {
                                    item.ItemCode ??= hana.ItemCode;
                                    item.ItemName ??= hana.ItemName;
                                    item.Status ??= hana.status;
                                    item.ObjType ??= hana.objType;
                                    item.PlannedQty ??= hana.plannedQty;
                                    item.Warehouse ??= hana.Warehouse;
                                    item.Type ??= hana.Type;
                                    item.BaseQty ??= hana.BaseQty;
                                    item.ItemType = hana.ItemType;
                                    item.Box ??= hana.Box;
                                    item.Litre ??= hana.Litre;
                                    item.Date ??= hana.Date;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ HANA enrichment failed for DocEntry {item.DocEntry}, LineNum {item.LineNum}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            return response;
        }

        public async Task<GetFormStructureResponse> GetFormStructureAsync(int formId)
        {
            var response = new GetFormStructureResponse();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var multi = await connection.QueryMultipleAsync("[qc].[GetFormStructure]",
                    new { formId }, commandType: CommandType.StoredProcedure))
                {
                    response.Form = await multi.ReadFirstOrDefaultAsync<FormModel>();
                    response.QualitySettings = (await multi.ReadAsync<QualitySettingModel>()).AsList();
                    response.Documents = (await multi.ReadAsync<QCDocumentModel>()).AsList();
                    response.Parameters = (await multi.ReadAsync<FormParameterModel>()).AsList();
                }
            }

            return response;
        }

        public async Task<GetFormStructureResponse> GetFormStructureAsyncV2()
        {
            var response = new GetFormStructureResponse();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var multi = await connection.QueryMultipleAsync("[qc].[GetFormStructureV2]",
                    commandType: CommandType.StoredProcedure))
                {
                    response.Form = await multi.ReadFirstOrDefaultAsync<FormModel>();
                    response.QualitySettings = (await multi.ReadAsync<QualitySettingModel>()).AsList();
                    response.Documents = (await multi.ReadAsync<QCDocumentModel>()).AsList();
                    response.Parameters = (await multi.ReadAsync<FormParameterModel>()).AsList();
                }
            }

            return response;
        }

        public async Task<List<FormModel>> GetFormUsingCreatedByAsync(string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);

            var result = await connection.QueryAsync<FormModel>(
                "[qc].[GetFormUsingCreatedBy]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }
        public async Task<QcResponse> InsertItemDataAsync(InsertItemDataRequest request)
        {
            var response = new QcResponse();

            if (request?.ItemDataList == null || request.ItemDataList.Count == 0)
            {
                response.Success = false;
                response.Message = "ItemDataList cannot be empty.";
                return response;
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[qc].[InsertItemData]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // TVP parameter
                    var dt = ConvertToDataTable(request.ItemDataList);
                    var tvpParam = command.Parameters.AddWithValue("@ItemDataList", dt);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "qc.ItemDataType";

                    // formId parameter
                    command.Parameters.AddWithValue("@formId", request.FormId);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "Item data inserted successfully.";
                }
            }
            catch (SqlException sqlEx)
            {
                response.Success = false;
                response.Message = $"SQL Error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        // Private helper: convert list to DataTable for TVP
        private DataTable ConvertToDataTable(List<ItemDataInsertModel> items)
        {
            var dt = new DataTable();
            dt.Columns.Add("docEntry", typeof(int));
            dt.Columns.Add("documentId", typeof(int));
            dt.Columns.Add("lineNum", typeof(int));
            dt.Columns.Add("DocNum", typeof(int));
            dt.Columns.Add("result", typeof(bool));

            foreach (var item in items)
            {
                dt.Rows.Add(item.DocEntry, item.DocumentId, item.LineNum, item.DocNum, item.Result);
            }

            return dt;
        }
        public async Task<QcResponse> InsertItemParameterDataAsync(InsertItemParameterDataRequest request)
        {
            var response = new QcResponse();

            if (request?.ItemParamList == null || request.ItemParamList.Count == 0)
            {
                response.Success = false;
                response.Message = "ItemParamList cannot be empty.";
                return response;
            }

            try
            {
                var dt = ConvertToDataTable(request.ItemParamList);

                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[qc].[InsertItemParameterData]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    var tvpParam = command.Parameters.AddWithValue("@ItemParamList", dt);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "qc.ItemParameterDataType";

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "Item parameter data inserted successfully.";
                }
            }
            catch (SqlException sqlEx)
            {
                response.Success = false;
                response.Message = $"SQL Error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }
        // 🔹 Private helper: convert list to DataTable
        private DataTable ConvertToDataTable(List<ItemParameterDataModel> items)
        {
            var dt = new DataTable();
            dt.Columns.Add("itemDataId", typeof(int));
            dt.Columns.Add("parameterId", typeof(int));
            dt.Columns.Add("value", typeof(decimal));
            dt.Columns.Add("remark", typeof(string));

            foreach (var item in items)
            {
                dt.Rows.Add(item.ItemDataId, item.ParameterId, item.Value, item.Remark);
            }

            return dt;
        }
        public async Task<QcResponse> SaveSubParameterDataAsync(List<SubParameterRequest> subParameters, IFormFileCollection files)
        {
            var response = new QcResponse();

            try
            {
                string uploadFolder = Path.Combine("wwwroot", "Uploads", "QC");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                int fileIndex = 0;

                // Assign image paths where HasFile = true
                foreach (var sub in subParameters)
                {
                    if (sub.HasFile && fileIndex < files.Count)
                    {
                        var file = files[fileIndex++];
                        var ext = Path.GetExtension(file.FileName);
                        var newFileName = $"{Guid.NewGuid()}{ext}";
                        var savePath = Path.Combine(uploadFolder, newFileName);

                        using (var stream = new FileStream(savePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        sub.ImagePath = $"/Uploads/QC/{newFileName}";
                    }
                }

                // Convert to DataTable
                var dt = new DataTable();
                dt.Columns.Add("ItemDataId", typeof(int));
                dt.Columns.Add("ParameterId", typeof(int));
                dt.Columns.Add("SubParameterId", typeof(int));
                dt.Columns.Add("ImagePath", typeof(string));
                dt.Columns.Add("Values", typeof(decimal));
                dt.Columns.Add("Remark", typeof(string));

                foreach (var s in subParameters)
                {
                    dt.Rows.Add(s.ItemDataId, s.ParameterId, s.SubParameterId, s.ImagePath ?? string.Empty, s.Values, s.Remark);
                }

                // Insert into DB via stored procedure
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[qc].[InsertItemSubParameterData]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    var tvp = command.Parameters.AddWithValue("@SubParamList", dt);
                    tvp.SqlDbType = SqlDbType.Structured;
                    tvp.TypeName = "qc.ItemSubParameterDataType";

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }

                response.Success = true;
                response.Message = "Item sub-parameter data inserted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }

            return response;
        }
        public async Task<IEnumerable<ProductionDocNumModel>> GetProductionDocNumAsync(int company, int docId)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                string procedureName;

                // ✅ Map docId to specific stored procedure
                switch (docId)
                {
                    case 202: // Production
                        procedureName = "GetProductionDocNum";
                        break;
                    case 15: // Delivery
                        procedureName = "GetDeliveryDocNum";
                        break;
                    case 13: // A/R Invoice
                        procedureName = "GetARInvoiceDocNum";
                        break;
                    case 20: // GRPO
                        procedureName = "GetGRPODocNum";
                        break;
                    case 18: // A/P Invoice
                        procedureName = "GetAPInvoiceDocNum";
                        break;
                    case 7: // Stock Transfer
                        procedureName = "GetStockTransferDocNum";
                        break;
                    case 59: // Receipt from Production
                        procedureName = "GetReceiptFromProductionDocNum";
                        break;
                    default:
                        throw new ArgumentException($"Unsupported DocId: {docId}");
                }

                // ✅ Build dynamic SQL to call corresponding procedure
                var sql = $"CALL \"{settings.Schema}\".\"{procedureName}\"()";

                var result = await connection.QueryAsync<ProductionDocNumModel>(sql);

                return result;
            }
        }
        public async Task<IEnumerable<QcProductionDataModel>> GetProductionDataAsync(int DocNum, int docId, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                string procedureName;

                // ✅ Map docId to specific stored procedure
                switch (docId)
                {
                    case 202: // Production
                        procedureName = "GetProductionData";
                        break;
                    case 59: // Receipt from Production
                        procedureName = "GetReceiptFromProductionData";
                        break;
                    default:
                        throw new ArgumentException($"Unsupported DocId: {docId}");
                }

                var parameters = new DynamicParameters();
                parameters.Add("DocNum", DocNum);

                //var query = $"CALL \"{settings.Schema}\".\"GetProductionData\"(?)";
                var query = $"CALL \"{settings.Schema}\".\"{procedureName}\"(?)";

                var result = await connection.QueryAsync<QcProductionDataModel>(query, parameters);
                return result;
            }

        }
        public async Task<IEnumerable<DocumentInsightModel>> GetDocumentInsightsAsync(int userId, int companyId, string month)
        {
            var sqlQuery = "EXEC  [qc].[jsGetDocumentInsight] @userId, @companyId, @month";
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(); // Ensure the connection is opened asynchronously
                return await connection.QueryAsync<DocumentInsightModel>(
                    sqlQuery,
                    new { userId, companyId, month }
                );
            }
        }
        private string GetDocumentNameById(int documentId)
        {
            switch (documentId)
            {
                case 202:
                    return "Production";
                case 15:
                    return "Delivery";
                case 13:
                    return "A/R Invoice";
                case 20:
                    return "GRPO";
                case 18:
                    return "A/P Invoice";
                case 7:
                    return "Stock Transfer";
                case 59:
                    return "Receipt from Production";
                default:
                    return "Unknown";
            }
        }
        public async Task<List<QCPendingDocumentModel>> GetPendingDocumentsAsync(int userId, int companyId, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var result = await connection.QueryAsync<QCPendingDocumentModel>(
                    "[qc].[jsGetPendingDocuments]",
                    new { userId, companyId, month },
                    commandType: System.Data.CommandType.StoredProcedure
                );

                var list = result.AsList();

                // apply document name
                foreach (var doc in list)
                {
                    doc.DocumentName = GetDocumentNameById(doc.DocumentId);
                }

                return list;
            }
        }

        public async Task<List<QCApprovedDocumentModel>> GetApprovedDocumentsAsync(int userId, int companyId, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var result = await connection.QueryAsync<QCApprovedDocumentModel>(
                    "[qc].[jsGetApprovedDocuments]",
                    new { userId, companyId, month },
                    commandType: System.Data.CommandType.StoredProcedure
                );

                var list = result.AsList();

                // apply document name
                foreach (var doc in list)
                {
                    doc.DocumentName = GetDocumentNameById(doc.DocumentId);
                }

                return list;
            }
        }
        public async Task<List<QCRejectedDocumentModel>> GetRejectedDocumentsAsync(int userId, int companyId, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var result = await connection.QueryAsync<QCRejectedDocumentModel>(
                    "[qc].[jsGetRejectedDocuments]",
                    new { userId, companyId, month },
                    commandType: System.Data.CommandType.StoredProcedure
                );

                var list = result.AsList();

                // apply document name
                foreach (var doc in list)
                {
                    doc.DocumentName = GetDocumentNameById(doc.DocumentId);
                }

                return list;
            }
        }

        public async Task<List<QcAllDocumentModel>> GetAllQcDocumentAsync(int userId, int companyId, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Fetch all three lists
                var pendingQC = await connection.QueryAsync<QcAllDocumentModel>(
                    "EXEC [qc].[jsGetPendingDocuments] @userId, @companyId, @month",
                    new { userId, companyId, month });

                var approvedQC = await connection.QueryAsync<QcAllDocumentModel>(
                    "EXEC [qc].[jsGetApprovedDocuments] @userId, @companyId, @month",
                    new { userId, companyId, month });

                var rejectedQC = await connection.QueryAsync<QcAllDocumentModel>(
                    "EXEC [qc].[jsGetRejectedDocuments] @userId, @companyId, @month",
                    new { userId, companyId, month });

                var allQC = new List<QcAllDocumentModel>();

                // Combine and assign status + document name
                foreach (var item in pendingQC)
                {
                    item.status = "Pending";
                    item.DocumentName = GetDocumentNameById(item.DocumentId);
                    allQC.Add(item);
                }

                foreach (var item in approvedQC)
                {
                    item.status = "Approved";
                    item.DocumentName = GetDocumentNameById(item.DocumentId);
                    allQC.Add(item);
                }

                foreach (var item in rejectedQC)
                {
                    item.status = "Rejected";
                    item.DocumentName = GetDocumentNameById(item.DocumentId);
                    allQC.Add(item);
                }

                return allQC;
            }
        }

        public async Task<IEnumerable<QCApprovalFlowModel>> GetQCApprovalFlowAsync(int flowId)
        {
            var sqlQuery = "EXEC [qc].[jsGetQCApprovalFlow] @flowId";
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(); // Ensure the connection is opened asynchronously
                return await connection.QueryAsync<QCApprovalFlowModel>(
                    sqlQuery,
                    new { flowId }
                );
            }
        }

        /*public async Task<QcResponse> ApproveDocumentAsync(QcApprovalRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@flowId", request.FlowId);
                parameters.Add("@company", request.Company);
                parameters.Add("@userId", request.UserId);
                parameters.Add("@remarks", string.IsNullOrEmpty(request.Remarks) ? " " : request.Remarks);

                // This will throw exception if SP fails
                var result = await connection.QueryFirstOrDefaultAsync(
                    "[qc].[jsApproveDocument]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return new QcResponse
                {
                    Success = true,
                    Message = result?.ResultMessage ?? "Action completed successfully"
                };
            }
        }*/

        public async Task<QcResponse> ApproveDocumentAsync(QcApprovalRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    var resultMessages = new List<string>();
                    var allNotificationModels = new List<UserIdsForNotificationModel>();

                    using (SqlCommand cmd = new SqlCommand("[qc].[jsApproveDocument]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@flowId", request.FlowId);
                        cmd.Parameters.AddWithValue("@company", request.Company);
                        cmd.Parameters.AddWithValue("@userId", request.UserId);
                        cmd.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(request.Remarks) ? " " : request.Remarks);

                        var result = await cmd.ExecuteScalarAsync();
                        resultMessages.Add(result?.ToString() ?? $"Approved Document of FlowId {request.FlowId}");


                    }
                    var notifications = await GetQcUserIdsSendNotificatiosAsync(request.FlowId);
                    if (notifications != null)
                        allNotificationModels.AddRange(notifications);

                    // ✅ FIX 1: Deduplicate notification models (same userId multiple times)
                    allNotificationModels = allNotificationModels
                        .Where(m => !string.IsNullOrWhiteSpace(m.userIdsToApprove))
                        .GroupBy(m => m.userIdsToApprove)
                        .Select(g => g.First())
                        .ToList();

                    // ✅ FIX 2: Get unique user IDs
                    var uniqueUserIds = new HashSet<int>(
                        allNotificationModels
                            .SelectMany(m => m.userIdsToApprove.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            .Select(s => int.Parse(s.Trim()))
                    );

                    string notificationTitle = "You have received a new QC document for review";
                    string notificationBody = $"A new Quality Check document (Flow ID: {request.FlowId}) forwarded to you for further action.";

                    var data = new Dictionary<string, string>
                    {
                        { "screen", "details" },
                        { "company", request.Company.ToString() },
                        { "flowId", request.FlowId.ToString() }
                    };

                    // ✅ Track sent tokens to avoid duplicates
                    var sentTokens = new HashSet<string>();

                    foreach (var userId in uniqueUserIds)
                    {
                        var fcmTokenList = await _notificationService.GetUserFcmTokenAsync(userId);
                        if (fcmTokenList == null || fcmTokenList.Count == 0)
                            continue;

                        foreach (var token in fcmTokenList)
                        {
                            if (string.IsNullOrWhiteSpace(token.fcmToken))
                                continue;

                            if (sentTokens.Contains(token.fcmToken))
                                continue;

                            await _notificationService.SendPushNotificationAsync(
                                notificationTitle,
                                notificationBody,
                                token.fcmToken,
                                data
                            );

                            sentTokens.Add(token.fcmToken);
                        }

                        // ✅ Insert database notification once per user
                        await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                        {
                            userId = userId,
                            title = "QC Document",
                            message = notificationBody,
                            pageId = 6,
                            data = $"Flow ID: {request.FlowId}",
                            BudgetId = request.FlowId
                        });
                    }

                    return new QcResponse
                    {
                        Success = true,
                        Message = string.Join(" | ", resultMessages)
                    };

                }
            }
            catch (SqlException ex)
            {
                return new QcResponse { Success = false, Message = $"SQL Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new QcResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<QcResponse> RejectDocumentAsync(QcRejectRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@flowId", request.FlowId);
                parameters.Add("@company", request.Company);
                parameters.Add("@userId", request.UserId);
                parameters.Add("@remarks", request.Remarks);

                // This will throw exception if SP fails
                var result = await connection.QueryFirstOrDefaultAsync(
                    "[qc].[jsRejectDocument]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return new QcResponse
                {
                    Success = true,
                    Message = result?.ResultMessage ?? "Action completed successfully"
                };
            }
        }

        public async Task<IEnumerable<FormsWithUsersModel>> GetFormsWithUsersAsync()
        {
            var sqlQuery = "EXEC  [qc].[GetFormsWithUsers]";
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(); // Ensure the connection is opened asynchronously
                return await connection.QueryAsync<FormsWithUsersModel>(
                    sqlQuery
                );
            }
        }

        public async Task<IEnumerable<GetItemDataIdModel>> GetItemDataIdAsync(int docEntry, int lineNum)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@docEntry", docEntry);
                    parameters.Add("@lineNum", lineNum);

                    var result = await conn.QueryAsync<GetItemDataIdModel>("[qc].[GetItemDataId]", parameters, commandType: CommandType.StoredProcedure);

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }

        public async Task<IEnumerable<UserIdsForNotificationModel>> GetQcUserIdsSendNotificatiosAsync(int flowId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@userDocumentId", flowId);
                    var result = await conn.QueryAsync<UserIdsForNotificationModel>("[qc].[jsQCNotify]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }

        public async Task<Response> SendPendingQcCountNotificationAsync()
        {
            var responseMessage = new StringBuilder();
            bool overallSuccess = true;
            bool foundAnyPending = false;

            try
            {
                var activeUsers = await _userService.GetActiveUser();
                if (activeUsers == null || !activeUsers.Any())
                    return new Response { Success = false, Message = "No active users found." };

                // ✅ Track which tokens we've already sent to (to prevent duplicates in this request)
                var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Track which users we've already processed
                HashSet<int> notifiedUsers = new HashSet<int>();

                foreach (var user in activeUsers)
                {
                    int userId = user.userId;

                    // Skip if we've already notified this user
                    if (notifiedUsers.Contains(userId))
                        continue;

                    int companyId = user.company;
                    string month = DateTime.Now.ToString("MM-yyyy");

                    // fetch all counts for this single user
                    var counts = await GetDocumentInsightsAsync(userId, companyId, month);
                    if (counts == null || !counts.Any())
                    {
                        responseMessage.AppendLine($"No document counts for user {userId}.");
                        continue;
                    }

                    // total up ALL pendings for this user
                    int totalPending = counts.Sum(c => c.totalPending);
                    if (totalPending <= 0)
                        continue;   // nothing to send

                    foundAnyPending = true;

                    // ✅ Get list of FCM tokens
                    var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);

                    if (fcmTokens == null || fcmTokens.Count == 0)
                    {
                        responseMessage.AppendLine($"No FCM token for user {userId}.");
                        overallSuccess = false;
                        continue;
                    }

                    // build notification data
                    string title = $"You have {totalPending} " +
                                   (totalPending == 1 ? "Quality Check pending request" : "Quality Check pending requests");
                    string body = "Kindly Approve.";

                    var data = new Dictionary<string, string>
                    {
                        { "userId",  userId.ToString() },
                        { "company", companyId.ToString() },
                        { "screen",  "pending" }
                    };

                    // ✅ Send push notification to all tokens (but only once per token)
                    int tokensSent = 0;
                    foreach (var token in fcmTokens)
                    {
                        var normalizedToken = token.fcmToken?.Trim();

                        if (string.IsNullOrWhiteSpace(normalizedToken))
                            continue;

                        // ✅ Check if already sent to this token
                        if (sentTokens.Contains(normalizedToken))
                        {
                            Console.WriteLine($"⏭️ Skipping duplicate token for userId {userId}");
                            continue;
                        }

                        await _notificationService.SendPushNotificationAsync(
                            title,
                            body,
                            normalizedToken,
                            data
                        );

                        // ✅ Mark this token as sent
                        sentTokens.Add(normalizedToken);
                        tokensSent++;
                    }

                    if (tokensSent > 0)
                    {
                        // Add to our tracking set after successful notification
                        notifiedUsers.Add(userId);
                        responseMessage.AppendLine($"Notification sent to user {userId} ({tokensSent} device(s)).");
                    }
                    else
                    {
                        responseMessage.AppendLine($"No valid/unique tokens for user {userId}.");
                    }
                }

                if (!foundAnyPending)
                    return new Response
                    {
                        Success = true,
                        Message = "No pending requests for any active user."
                    };

                return new Response
                {
                    Success = overallSuccess,
                    Message = responseMessage.ToString().Trim()
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<IEnumerable<QcProductionDataModel>> GetProductionDataUsingLineAsync(int DocEntry, int docId, int LineNum, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                string procedureName;
                switch (docId)
                {
                    case 202: // Production
                        procedureName = "GetProductionDataUsingLine";
                        break;
                    case 59: // Receipt from Production
                        procedureName = "GetReceiptFromProductionDataUsingLine";
                        break;
                    default:
                        throw new ArgumentException($"Unsupported DocId: {docId}");
                }

                var parameters = new DynamicParameters();
                parameters.Add("DocEntry", DocEntry);
                parameters.Add("LineNumParam", LineNum);

                //var query = $"CALL \"{settings.Schema}\".\"GetProductionDataUsingLine\"(?,?)";
                var query = $"CALL \"{settings.Schema}\".\"{procedureName}\"(?,?)";

                var result = await connection.QueryAsync<QcProductionDataModel>(query, parameters);
                return result;
            }

        }

        public async Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetQcCurrentUsersSendNotificationAsync(int userDocumentId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@userDocumentId", userDocumentId);
                    var result = await conn.QueryAsync<AfterCreatedRequestSendNotificationToUser>("[qc].[GetUsersInCurrentStage]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }

        public async Task<UpdateFormResponse> UpdateFormAsync(UpdateFormModel request)
        {
            var response = new UpdateFormResponse();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[qc].[UpdateForm]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // ====== Basic Form Parameters ======
                    command.Parameters.AddWithValue("@oldFormId", request.OldFormId);
                    command.Parameters.AddWithValue("@newFormNumber", (object?)request.NewFormNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@formDate", (object?)request.FormDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@status", (object?)request.Status ?? DBNull.Value);
                    command.Parameters.AddWithValue("@remarks", (object?)request.Remarks ?? DBNull.Value);
                    command.Parameters.AddWithValue("@createdBy", request.CreatedBy);

                    // ====== Quality Check Fields ======
                    command.Parameters.AddWithValue("@qualityCheckMin", (object?)request.QualityCheckMin ?? DBNull.Value);
                    command.Parameters.AddWithValue("@qualityCheckMax", (object?)request.QualityCheckMax ?? DBNull.Value);
                    command.Parameters.AddWithValue("@minValueToPassQC", (object?)request.MinValueToPassQC ?? DBNull.Value);
                    command.Parameters.AddWithValue("@randomBoxCheck", (object?)request.RandomBoxCheck ?? DBNull.Value);

                    // ====== Documents as Table-Valued Parameter ======
                    var documentTable = new DataTable();
                    documentTable.Columns.Add("documentTypeName", typeof(string));
                    documentTable.Columns.Add("hanaId", typeof(int));
                    documentTable.Columns.Add("isMandatory", typeof(bool));
                    documentTable.Columns.Add("documentPath", typeof(string));

                    if (request.Documents != null && request.Documents.Any())
                    {
                        foreach (var doc in request.Documents)
                        {
                            documentTable.Rows.Add(
                                doc.DocumentTypeName,
                                doc.HanaId,
                                doc.IsMandatory,
                                string.IsNullOrEmpty(doc.DocumentPath) ? DBNull.Value : (object)doc.DocumentPath
                            );
                        }
                    }

                    var tvpParam = command.Parameters.AddWithValue("@documents", documentTable);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "qc.DocumentTableType";

                    // ====== Output Parameter ======
                    var newFormIdParam = new SqlParameter("@newFormId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(newFormIdParam);

                    // ====== Execute Procedure ======
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    // ====== Read Output ======
                    response.NewFormId = newFormIdParam.Value != DBNull.Value
                        ? Convert.ToInt32(newFormIdParam.Value)
                        : 0;

                    response.Success = response.NewFormId > 0;
                    response.Message = response.Success
                        ? $"Form updated successfully. New Form ID: {response.NewFormId}"
                        : "Form update failed.";
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = $"SQL Error: {ex.Message}";
                response.NewFormId = 0;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Unexpected Error: {ex.Message}";
                response.NewFormId = 0;
            }

            return response;
        }

        /* public async Task<UpdateFormResponse> UpdateFormWithParametersAsync(UpdateFormModel request)
         {
             var response = new UpdateFormResponse();

             try
             {
                 // 1️⃣ First, update form using your existing UpdateFormAsync
                 var formResponse = await UpdateFormAsync(request);

                 if (!formResponse.Success || formResponse.NewFormId == 0)
                     return new UpdateFormResponse
                     {
                         Success = false,
                         Message = "Form update failed.",
                         NewFormId = 0
                     };

                 int newFormId = formResponse.NewFormId;

                 // 2️⃣ Next, insert Parameters and SubParameters
                 if (request.Parameters != null && request.Parameters.Any())
                 {
                     foreach (var parameter in request.Parameters)
                     {
                         // Create Parameter
                         var paramRequest = new CreateParameterRequest
                         {
                             ParameterName = parameter.ParameterName,
                             FormId = newFormId,
                             DisplayOrder = parameter.DisplayOrder,
                             IsActive = parameter.IsActive
                         };

                         var paramResponse = await CreateParameterAsync(paramRequest);
                         if (!paramResponse.Success || paramResponse.newParameterId == null)
                             throw new Exception($"Failed to create parameter: {parameter.ParameterName}");

                         int newParameterId = paramResponse.newParameterId.Value;

                         // Create SubParameters
                         if (parameter.SubParameters != null && parameter.SubParameters.Any())
                         {
                             foreach (var sub in parameter.SubParameters)
                             {
                                 var subRequest = new CreateSubParameterRequest
                                 {
                                     ParameterId = newParameterId,
                                     SubParameterName = sub.SubParameterName,
                                     IsImageMandatory = sub.IsImageMandatory,
                                     DisplayOrder = sub.DisplayOrder,
                                     IsActive = sub.IsActive
                                 };

                                 var subResponse = await CreateSubParameterAsync(subRequest);
                                 if (!subResponse.Success)
                                     throw new Exception($"Failed to create subparameter: {sub.SubParameterName}");
                             }
                         }
                     }
                 }

                 response.Success = true;
                 response.Message = "Form, Parameters, and SubParameters updated successfully.";
                 response.NewFormId = newFormId;
             }
             catch (Exception ex)
             {
                 response.Success = false;
                 response.Message = $"Error updating form with parameters: {ex.Message}";
                 response.NewFormId = 0;
             }

             return response;
         }
 */
    }
}
