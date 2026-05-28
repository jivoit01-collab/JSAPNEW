using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using ServiceStack;
using System.Data;
using System.Text;
using Sap.Data.Hana;
using System.ComponentModel.Design;
using JSAPNEW.Data.Entities;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
//using static JSAPNEW.Models.Bom2Models;

namespace JSAPNEW.Services.Implementation
{
    public class Bom2Service : IBom2Service
    {
        private readonly IConfiguration _configuration;
        private readonly string _HanaconnectionString;
        private readonly string _connectionString;
        private readonly string _sapBaseUrl;
        private readonly string _activeEnv;
        private readonly string _sapUserName;
        private readonly string _sapPassword;
        private readonly string _sapOilCompanyDB;
        private readonly string _sapBevCompanyDB;
        private readonly string _sapMartCompanyDB;

        public Bom2Service(IConfiguration configuration)
        {
            _configuration = configuration;
            _HanaconnectionString = _configuration.GetConnectionString("HanaConnection");
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _activeEnv = configuration["ActiveEnvironment"];
            _sapBaseUrl = configuration["SapServiceLayer:BaseUrl"]
                ?? throw new ArgumentNullException("SapServiceLayer:BaseUrl not found in configuration.");
            _sapUserName = configuration["SapServiceLayer:UserName"]
                ?? throw new ArgumentNullException("SapServiceLayer:UserName not found in configuration.");
            _sapPassword = configuration["SapServiceLayer:Password"]
                ?? throw new ArgumentNullException("SapServiceLayer:Password not found in configuration.");
            _sapOilCompanyDB = configuration[$"SapServiceLayer:CompanyDB:{_activeEnv}:Oil"]
                ?? throw new ArgumentNullException($"SapServiceLayer:CompanyDB:{_activeEnv}:Oil not found.");
            _sapBevCompanyDB = configuration[$"SapServiceLayer:CompanyDB:{_activeEnv}:Beverages"]
                ?? throw new ArgumentNullException($"SapServiceLayer:CompanyDB:{_activeEnv}:Beverages not found.");
            _sapMartCompanyDB = configuration[$"SapServiceLayer:CompanyDB:{_activeEnv}:Mart"]
                ?? throw new ArgumentNullException($"SapServiceLayer:CompanyDB:{_activeEnv}:Mart not found.");
        }

        private SAPSessionModel _cachedOilSession;
        private SAPSessionModel _cachedBevSession;
        private SAPSessionModel _cachedMartSession;
        public async Task<SAPSessionModel> GetSAPSessionOilAsync()
        {
            if (_cachedOilSession != null && _cachedOilSession.Expiry > DateTime.Now)
            {
                return _cachedOilSession;
            }

            var login = new SAPLoginModel
            {
                UserName = _sapUserName,
                Password = _sapPassword,
                CompanyDB = _sapOilCompanyDB
            };

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_sapBaseUrl);

                var json = JsonConvert.SerializeObject(login);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("Login", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SAP login failed. Status: {response.StatusCode}, Details: {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);

                string sessionId = result.SessionId;
                var cookies = response.Headers.GetValues("Set-Cookie").ToList();

                string b1Session = cookies.FirstOrDefault(c => c.Contains("B1SESSION"))?.Split(';')[0];
                string routeId = cookies.FirstOrDefault(c => c.Contains("ROUTEID"))?.Split(';')[0];

                _cachedOilSession = new SAPSessionModel
                {
                    SessionId = sessionId,
                    RouteId = routeId,
                    B1Session = b1Session,
                    Expiry = DateTime.Now.AddMinutes(120)
                };

                return _cachedOilSession;
            }
        }
        public async Task<SAPSessionModel> GetSAPSessionBevAsync()
        {
            if (_cachedBevSession != null && _cachedBevSession.Expiry > DateTime.Now)
                return _cachedBevSession;

            var login = new SAPLoginModel
            {
                UserName = _sapUserName,
                Password = _sapPassword,
                CompanyDB = _sapBevCompanyDB
            };

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_sapBaseUrl);

                var json = JsonConvert.SerializeObject(login);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("Login", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SAP login failed. Status: {response.StatusCode}, Details: {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);

                string sessionId = result.SessionId;
                var cookies = response.Headers.GetValues("Set-Cookie").ToList();

                string b1Session = cookies.FirstOrDefault(c => c.Contains("B1SESSION"))?.Split(';')[0];
                string routeId = cookies.FirstOrDefault(c => c.Contains("ROUTEID"))?.Split(';')[0];

                _cachedBevSession = new SAPSessionModel
                {
                    SessionId = sessionId,
                    RouteId = routeId,
                    B1Session = b1Session,
                    Expiry = DateTime.Now.AddMinutes(120)
                };

                return _cachedBevSession;
            }
        }
        public async Task<SAPSessionModel> GetSAPSessionMartAsync()
        {
            if (_cachedMartSession != null && _cachedMartSession.Expiry > DateTime.Now)
                return _cachedMartSession;

            var login = new SAPLoginModel
            {
                UserName = _sapUserName,
                Password = _sapPassword,
                CompanyDB = _sapMartCompanyDB
            };

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_sapBaseUrl);

                var json = JsonConvert.SerializeObject(login);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("Login", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SAP login failed. Status: {response.StatusCode}, Details: {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);

                string sessionId = result.SessionId;
                var cookies = response.Headers.GetValues("Set-Cookie").ToList();

                string b1Session = cookies.FirstOrDefault(c => c.Contains("B1SESSION"))?.Split(';')[0];
                string routeId = cookies.FirstOrDefault(c => c.Contains("ROUTEID"))?.Split(';')[0];

                _cachedMartSession = new SAPSessionModel
                {
                    SessionId = sessionId,
                    RouteId = routeId,
                    B1Session = b1Session,
                    Expiry = DateTime.Now.AddMinutes(120)
                };

                return _cachedMartSession;
            }
        }
        public async Task<List<SapPendingInsertionModel>> GetPendingInsertionsAsync2(int bomid)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bomid", bomid);

                var sql = "CALL \"TEST_OIL_11FEB\".\"bomJsGetPendingApiInsertions\"(?)";

                var result = await connection.QueryAsync<SapPendingInsertionModel>(sql, parameters);
                return result.ToList();
            }
        }

        public async Task<List<SapBomSyncResult>> PostProductTreesToSAPAsync(List<SapPendingInsertionModel> pendingInsertions)
        {
            var results = new List<SapBomSyncResult>();
            var bomGroups = pendingInsertions.GroupBy(b => b.bomId);

            foreach (var group in bomGroups)
            {
                var bomList = group.ToList();
                var first = bomList.First();
                int company = first.bomCompany;

                SAPSessionModel session;

                if (company == 1)
                    session = await GetSAPSessionOilAsync();
                else if (company == 2)
                    session = await GetSAPSessionBevAsync();
                else if (company == 3)
                    session = await GetSAPSessionMartAsync();
                else
                {
                    string msg = $"Unsupported company: {company}";
                    results.Add(new SapBomSyncResult
                    {
                        BomId = first.bomId,
                        TreeCode = first.parentCode,
                        IsSuccess = false,
                        Message = msg
                    });
                    await UpdateBomStatusAsync(first.bomId, msg, false.ToString());
                    continue;
                }

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using var client = new HttpClient(handler);
               client.BaseAddress = new Uri(_sapBaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Cookie", $"{session.B1Session}; {session.RouteId}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var tree = new ProductTree
                {
                    TreeCode = first.parentCode,
                    TreeType = first.bomType == "PD" ? "iProductionTree" : "iSalesTree",
                    Quantity = first.bomQty,
                    Warehouse = first.headerWareHouse,
                    ProductDescription = first.parentName,
                    PriceList = -1,
                    ProductTreeLines = bomList.Select(c => new ProductTreeLine
                    {
                        ItemCode = c.componentCode,
                        ItemName = c.componentName,
                        Quantity = c.componentQty,
                        PriceList = -1,
                        Warehouse = c.componentWareHouse,
                        Currency = "INR",
                        ParentItem = c.parentCode,
                        ItemType = c.componentType == "IT" ? "pit_Item" : "pit_Resource",
                        IssueMethod = "im_Manual"
                    }).ToList()
                };

                try
                {
                    var json = JsonConvert.SerializeObject(tree);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("ProductTrees", content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    string message;

                    if (response.IsSuccessStatusCode)
                    {
                        message = "Code - 201 : Created";
                    }
                    else
                    {
                        message = ExtractSapErrorCodeAndMessage(responseBody);
                    }

                    results.Add(new SapBomSyncResult
                    {
                        BomId = first.bomId,
                        TreeCode = tree.TreeCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        Message = message
                    });

                    await UpdateBomStatusAsync(first.bomId, message, (response.IsSuccessStatusCode).ToString());
                }
                catch (Exception ex)
                {
                    string errMsg = $"-1000: {ex.Message}";
                    results.Add(new SapBomSyncResult
                    {
                        BomId = first.bomId,
                        TreeCode = tree.TreeCode,
                        IsSuccess = false,
                        Message = errMsg
                    });
                    await UpdateBomStatusAsync(first.bomId, errMsg, false.ToString());
                }
            }

            return results;
        }
        private async Task UpdateBomStatusAsync(int bomId, string apiMessage, string tag)
        {
            var sqlQuery = "EXEC [bom].[jsUpdateBomApiStatus] @bomId,@apiMessage,@tag";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(sqlQuery, new { bomId, apiMessage, tag });
            }
        }

        /*private async Task UpdateBomStatusAsync(int bomId, string message)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("p_bomId", bomId);
                parameters.Add("p_apiMessage", message.Length > 1000 ? message.Substring(0, 1000) : message);

                await connection.ExecuteAsync("CALL \"TEST_OIL_11FEB\".\"bomJsUpdateApiStatus\"(?, ?)", parameters);
            }
        }*/
        private string ExtractSapErrorCodeAndMessage(string responseJson)
        {
            try
            {
                var obj = JObject.Parse(responseJson);
                var code = obj["error"]?["code"]?.ToString();
                var value = obj["error"]?["message"]?["value"]?.ToString();

                return !string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(value)
                    ? $"Code - {code}:Message- {value}"
                    : "-5000: Unknown SAP error structure";
            }
            catch
            {
                return "-5001: Failed to parse SAP error response";
            }
        }
        public async Task<BomResponse2> BomApproveAsync(ApproveModel2 request)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("p_docId", request.p_docId);
                parameters.Add("p_company", request.p_company);
                parameters.Add("p_userId", request.p_userId);
                parameters.Add("p_remarks", request.p_remarks);
                parameters.Add("p_action", request.p_action);

                // Define p_message as an OUT parameter with the correct size and direction
                parameters.Add("p_message", dbType: DbType.String, size: 5000, direction: ParameterDirection.Output);

                // Use positional parameters `?` for the procedure call
                var result = await connection.ExecuteAsync(
                    "CALL \"TEST_OIL_11FEB\".\"bomJsApproveBom\"(?, ?, ?, ?, ?, ?)",
                    parameters);

                // Retrieve the output message and return it as a BomResponse
                var response = new BomResponse2
                {
                    p_message = parameters.Get<string>("p_message")  // Fetch the output value of p_message
                };

                return response;
            }
        }
        public async Task<IEnumerable<GetBomApprove2>> GetApprovedBOMsAsync(int userId, int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);
                parameters.Add("company", company);

                var result = await connection.QueryAsync<GetBomApprove2>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsGetApprovedBOMs\"(?,?)",
                    parameters
                );
                return result;
            }
        }
        public async Task<IEnumerable<TotalBomInsightsModel>> TotalBomInsightsAsync(int userId, int companyId)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);
                parameters.Add("companyId", companyId);

                var result = await connection.QueryAsync<TotalBomInsightsModel>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsGetBOMInsights\"(?, ?)",
                    parameters
                );

                return result;
            }
        }
        public async Task<IEnumerable<BomPendingRequest>> GetPendingBOMsAsync(int userId, int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);
                parameters.Add("company", company);

                var result = await connection.QueryAsync<BomPendingRequest>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsGetPendingBOMs\"(?, ?)",
                    parameters);
                return result;
            }
        }
        public async Task<IEnumerable<GetBomReject2>> 
            
            GetRejectedBOMsAsync(int userId, int company)
        {

            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);
                parameters.Add("company", company);

                var result = await connection.QueryAsync<GetBomReject2>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsGetRejectedBOMs\"(?, ?)",
                    parameters);
                return result;
            }
        }
        public async Task<IEnumerable<BomResponse>> BomRejectAsync(RejectModel2 request)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {

                var parameters = new DynamicParameters();
                parameters.Add("docId", request.docId);
                parameters.Add("company", request.company);
                parameters.Add("userId", request.userId);
                parameters.Add("remarks", request.remarks);
                parameters.Add("action", request.action);

                var result = await connection.QueryAsync<BomResponse>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsRejectBom\"(?,?,?,?,?)",
                    parameters);
                return result;
            }
        }
        public async Task<IEnumerable<BomFileModel>> GetBomFilesAsync(int bomId)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bomId", bomId);

                var result = await connection.QueryAsync<BomFileModel>(
                    "CALL \"TEST_OIL_11FEB\".\"jsGetBomFiles\"(?)",
                    parameters
                );

                return result;
            }

        }
        public async Task<IEnumerable<BomHeaderModel>> GetBomHeadersByIdsAsync(int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("company", company);

                var result = await connection.QueryAsync<BomHeaderModel>(
                    "CALL \"TEST_OIL_11FEB\".\"jsGetBomHeadersByIds\"(?)",
                    parameters);

                return result;
            }
        }
        public async Task<IEnumerable<TypeModel>> GetBomTypeAsync()
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var sql = "CALL \"TEST_OIL_11FEB\".\"jsGetBomType\"";

                var result = await connection.QueryAsync<TypeModel>(
                     sql
                 );
                return result;
            }
        }
        public async Task<IEnumerable<TypeModel>> GetChildTypeAsync()
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var sql = "CALL \"TEST_OIL_11FEB\".\"jsGetChildType\"";

                var result = await connection.QueryAsync<TypeModel>(
                     sql
                 );

                return result;
            }
        }
        public async Task<IEnumerable<TypeModel>> GetChildTypeById(int childId)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("childId", childId);
                var result = await connection.QueryAsync<TypeModel>(
                    "CALL \"TEST_OIL_11FEB\".\"jsGetChildTypeById\"(?)",
                    parameters);
                return result;
            }
        }
        public async Task<IEnumerable<BomMaterialModel>> BomMaterialAsync(int bomId, int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bomId", bomId);
                parameters.Add("company", company);

                var result = await connection.QueryAsync<BomMaterialModel>(
                     "CALL \"TEST_OIL_11FEB\".\"jsGetDetailOfBomMaterial\"(?,?)",
                    parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<BomResourceModel>> BomResourceAsync(int bomId, int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bomId", bomId);
                parameters.Add("company", company);

                var result = await connection.QueryAsync<BomResourceModel>(
                     "CALL \"TEST_OIL_11FEB\".\"jsGetDetailOfBomResource\"(?,?)",
                    parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<ItemModel>> GetFatherMaterialAsync(int company, string type)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("company", company);
                parameters.Add("type", type);

                var sql = "CALL \"TEST_OIL_11FEB\".\"jsGetFatherMaterial\"(?,?)";

                var result = await connection.QueryAsync<ItemModel>(
                    sql,
                    parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<FullHeaderDetailModel>> FullHeaderDetailAsync(int bomId, int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bomId", bomId);
                parameters.Add("company", company);

                var result = await connection.QueryAsync<FullHeaderDetailModel>(
                     "CALL \"TEST_OIL_11FEB\".\"jsGetFullDetailOfBomHeader\"(?,?)",
                    parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<MaterialModel>> GetMaterialAsync(string parentCode, int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();

                parameters.Add("parentCode", parentCode);
                parameters.Add("company", company);

                var result = await connection.QueryAsync<MaterialModel>(
                    "CALL \"TEST_OIL_11FEB\".\"jsGetMaterial\"(?, ?)",
                    parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<ResourcesModel>> GetResourcesAsync(int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {

                var parameters = new DynamicParameters();
                parameters.Add("company", company);

                var result = await connection.QueryAsync<ResourcesModel>(
                    "CALL \"TEST_OIL_11FEB\".\"jsGetResources\"(?)",
                    parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<WarehouseModel>> GetWarehouseAsync(int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("company", company);

                var result = await connection.QueryAsync<WarehouseModel>(
                    "CALL \"TEST_OIL_11FEB\".\"jsGetWareHouse\"(?)",
                    parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<BomResponse>> RemoveBomComponentAsync(RemoveBomComponentModel2 request)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bomComId", request.bomComId);
                parameters.Add("updatedBy", request.updatedBy);
                parameters.Add("updateFlag", request.updateFlag);

                var result = await connection.QueryAsync<BomResponse>(
                    "CALL \"TEST_OIL_11FEB\".\"jsRemoveBomComponent\"(?,?,?)",
                    parameters);

                return result; // This should return 1 on success
            }
        }
        public async Task<IEnumerable<BomResponse>> UpdateBomHeaderAsync(int bomId, int qty, string wareHouse, int updatedBy)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bomId", bomId);
                parameters.Add("qty", qty);
                parameters.Add("wareHouse", wareHouse);
                parameters.Add("updatedBy", updatedBy);

                var result = await connection.QueryAsync<BomResponse>(
                    "CALL \"TEST_OIL_11FEB\".\"jsUpdateBomHeader\"(?,?,?,?)",
                    parameters);

                return result;
            }
        }
        public async Task<IEnumerable<BomResponse>> CreateBomWithComponentsAsync(BomRequest2 request, List<IFormFile> files)
        {
            // Step 1: Validate Request
            if (string.IsNullOrWhiteSpace(request.parentCode) ||
                string.IsNullOrWhiteSpace(request.type) ||
                request.qty <= 0 ||
                request.company <= 0 ||
                request.createdBy <= 0 ||
                string.IsNullOrWhiteSpace(request.wareHouse) ||
                request.Components2 == null || !request.Components2.Any())
            {
                return new List<BomResponse> { new BomResponse { Message = "Invalid BOM request. All fields must be provided.", Success = false } };
            }

            // Step 2: Upload files (only if files are provided)
            List<BomFile2> uploadedFiles = new List<BomFile2>();
            if (files != null && files.Any())
            {
                string uploadFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads", "BOM");
                if (!Directory.Exists(uploadFolderPath)) Directory.CreateDirectory(uploadFolderPath);

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        string filePath = Path.Combine(uploadFolderPath, file.FileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        uploadedFiles.Add(new BomFile2
                        {
                            path = Path.Combine("Uploads", "BOM"),
                            fileName = file.FileName,
                            fileExt = Path.GetExtension(file.FileName),
                            company = request.company,
                            uploadedBy = request.createdBy,
                            fileSize = file.Length,
                            description = "Uploaded file"
                        });
                    }
                }
            }

            // Assign uploaded files to request (even if empty)
            request.Files2 = uploadedFiles;

            // Step 3: Save to HANA
            try
            {
                using (var connection = new HanaConnection(_HanaconnectionString))
                {
                    await connection.OpenAsync();

                    // ➤ Insert BOM and get bomId
                    var bomParams = new DynamicParameters();
                    bomParams.Add("parentCode", request.parentCode);
                    bomParams.Add("type", request.type);
                    bomParams.Add("qty", request.qty);
                    bomParams.Add("company", request.company);
                    bomParams.Add("createdBy", request.createdBy);
                    bomParams.Add("wareHouse", request.wareHouse);
                    bomParams.Add("newBomId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await connection.ExecuteAsync(
                        "CALL \"TEST_OIL_11FEB\".\"jsCreateBom\"(?, ?, ?, ?, ?, ?, ?)", bomParams);

                    int bomId = bomParams.Get<int>("newBomId");

                    request.Components2.ForEach(c => c.bomId = bomId);
                    request.Files2.ForEach(f => f.bomId = bomId);

                    // ➤ Insert Components
                    foreach (var comp in request.Components2)
                    {
                        if (string.IsNullOrWhiteSpace(comp.type) || string.IsNullOrWhiteSpace(comp.componentCode))
                            continue;

                        var compParams = new DynamicParameters();
                        compParams.Add("bomId", bomId);
                        compParams.Add("type", comp.type);
                        compParams.Add("componentCode", comp.componentCode);
                        compParams.Add("qty", comp.qty);
                        compParams.Add("company", comp.company);
                        compParams.Add("wareHouse", comp.wareHouse);
                        compParams.Add("updatedBy", comp.updatedBy);
                        compParams.Add("updateFlag", comp.updateFlag);

                        await connection.ExecuteAsync(
                            "CALL \"TEST_OIL_11FEB\".\"jsAddBomComponent\"(?, ?, ?, ?, ?, ?, ?, ?)", compParams);
                    }

                    // ➤ Insert Files only if any were uploaded
                    if (request.Files2 != null && request.Files2.Any())
                    {
                        foreach (var file in request.Files2)
                        {
                            var fileParams = new DynamicParameters();
                            fileParams.Add("path", file.path);
                            fileParams.Add("fileName", file.fileName);
                            fileParams.Add("fileExt", file.fileExt);
                            fileParams.Add("bomId", bomId);
                            fileParams.Add("company", file.company);
                            fileParams.Add("uploadedBy", file.uploadedBy);
                            fileParams.Add("fileSize", file.fileSize);
                            fileParams.Add("description", file.description);

                            await connection.ExecuteAsync(
                                "CALL \"TEST_OIL_11FEB\".\"jsAddBomFile\"(?, ?, ?, ?, ?, ?, ?, ?)", fileParams);
                        }
                    }

                    // ➤ Call jsAutoCreateBomWorkflow at the end
                    var workflowParams = new DynamicParameters();
                    workflowParams.Add("bomId", bomId);

                    await connection.ExecuteAsync(
                        "CALL \"TEST_OIL_11FEB\".\"jsAutoCreateBomWorkflow\"(?)", workflowParams);

                    // ➤ Return success response
                    return new List<BomResponse> {
                        new BomResponse {
                            Message = "BOM and components saved successfully." + (uploadedFiles.Any() ? " Files uploaded." : " No files uploaded.") + " Workflow triggered.",
                            Success = true
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return new List<BomResponse> { new BomResponse { Message = $"Error saving BOM: {ex.Message}", Success = false } };
            }
        }
        public async Task<IEnumerable<BomHeaderData>> BOMGetBomHeadersByIdsAsync(string IdsList, int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("IdsList", IdsList);
                parameters.Add("company", company);
                var result = await connection.QueryAsync<BomHeaderData>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsGetBomHeadersByIds\"(?, ?)",
                    parameters
                );
                return result;
            }
        }
        public async Task<IEnumerable<TotalBomDetails>> GetTotalBomInDetailsAsync(int userId, int company)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);
                parameters.Add("company", company);

                // Get Approved BOMs
                var approved = (await connection.QueryAsync<TotalBomDetails>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsGetApprovedBOMs\"(?, ?)", parameters))
                    .Where(b => b.bomId != 0) // <-- remove dummy rows
                    .Select(b => { b.status = "Approved"; b.flag = "N"; return b; });

                // Get Pending BOMs
                var pending = (await connection.QueryAsync<TotalBomDetails>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsGetPendingBOMs\"(?, ?)", parameters))
                    .Where(b => b.bomId != 0) // <-- remove dummy rows
                    .Select(b => { b.status = "Pending"; return b; });

                // Get Rejected BOMs
                var rejected = (await connection.QueryAsync<TotalBomDetails>(
                    "CALL \"TEST_OIL_11FEB\".\"bomjsGetRejectedBOMs\"(?, ?)", parameters))
                    .Where(b => b.bomId != 0) // <-- remove dummy rows
                    .Select(b => { b.status = "Rejected"; return b; });

                return approved.Concat(pending).Concat(rejected);
            }
        }
        public async Task<IEnumerable<ApprovalFlowRequest>> GetBomApprovalFlowAsync(int bomId)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bomId", bomId);
                var result = await connection.QueryAsync<ApprovalFlowRequest>(
                    "CALL \"TEST_OIL_11FEB\".\"jsGetBomApprovalFlow\"(?)",
                    parameters
                );
                return result;
            }

        }
    }
}
