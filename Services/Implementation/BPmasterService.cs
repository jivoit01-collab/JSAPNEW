using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using Sap.Data.Hana;
using ServiceStack;
using JSAPNEW.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.Design;

namespace JSAPNEW.Services.Implementation
{
    public class BPmasterService : IBPmasterService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;

        public BPmasterService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            var activeEnv = configuration["ActiveEnvironment"];  // "Test" or "Live"
            _hanaSettings = configuration.GetSection($"HanaSettings:{activeEnv}")
                                         .Get<Dictionary<int, HanaCompanySettings>>();
        }
        public async Task<BPMasterResponse> InsertBPMasterAsync(InsertBPMasterDataModel model)
        {
            var response = new BPMasterResponse();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("[BP].[jsInsertBPMasterData]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Simple master parameters
                    cmd.Parameters.AddWithValue("@type", model.Type);
                    cmd.Parameters.AddWithValue("@isStaff", model.IsStaff);
                    cmd.Parameters.AddWithValue("@staffCode", (object?)model.StaffCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@company", model.Company);
                    cmd.Parameters.AddWithValue("@groupID", model.GroupID);
                    cmd.Parameters.AddWithValue("@mainGroupID", model.MainGroupID);
                    cmd.Parameters.AddWithValue("@chain", (object?)model.Chain ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@contactPerson", (object?)model.ContactPerson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mobileNo", (object?)model.MobileNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@paymentTermID", (object?)model.PaymentTermID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@creditLimit", (object?)model.CreditLimit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@priceList", (object?)model.PriceList ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@userId", model.UserId);
                    cmd.Parameters.AddWithValue("@companyByUser", model.CompanyByUser ?? "");

                    // Tax details
                    cmd.Parameters.AddWithValue("@buyerTANNo", (object?)model.BuyerTANNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@panNo", model.PanNo);
                    cmd.Parameters.AddWithValue("@fssaiNo", (object?)model.FssaiNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeNo", (object?)model.MsmeNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeType", (object?)model.MsmeType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeBusinessType", (object?)model.MsmeBusinessType ?? DBNull.Value);

                    // Bank details
                    cmd.Parameters.AddWithValue("@bankName", (object?)model.BankName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@accountNo", (object?)model.AccountNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ifscCode", (object?)model.IfscCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@bankCountryID", (object?)model.BankCountryID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@acctName", (object?)model.AcctName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@branch", (object?)model.Branch ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@swiftCode", (object?)model.SwiftCode ?? DBNull.Value);

                    // Table-valued parameters
                    var addressTable = model.Addresses?.Count > 0 ? ToAddressDataTable(model.Addresses) : ToAddressDataTable(new List<BPMasterAddress>());
                    var contactTable = model.Contacts?.Count > 0 ? ToContactDataTable(model.Contacts) : ToContactDataTable(new List<BPContactPerson>());
                    var attachmentTable = model.Attachments?.Count > 0 ? ToAttachmentDataTable(model.Attachments) : ToAttachmentDataTable(new List<BPAttachment>());

                    var addrParam = cmd.Parameters.AddWithValue("@addresses", addressTable);
                    addrParam.SqlDbType = SqlDbType.Structured;

                    var contactParam = cmd.Parameters.AddWithValue("@contacts", contactTable);
                    contactParam.SqlDbType = SqlDbType.Structured;

                    var attachParam = cmd.Parameters.AddWithValue("@attachments", attachmentTable);
                    attachParam.SqlDbType = SqlDbType.Structured;

                    // Output parameter
                    var outCode = new SqlParameter("@generatedCode", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outCode);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "BP Master inserted successfully.";
                    response.GeneratedCode = (int)(outCode.Value ?? 0);
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.GeneratedCode = 0;
            }

            return response;
        }
        private DataTable ToAddressDataTable(List<BPMasterAddress> list)
        {
            var table = new DataTable();
            table.Columns.Add("email", typeof(string));
            table.Columns.Add("addressType", typeof(string));
            table.Columns.Add("addressLine1", typeof(string));
            table.Columns.Add("addressLine2", typeof(string));
            table.Columns.Add("stateID", typeof(string));
            table.Columns.Add("cityID", typeof(string));
            table.Columns.Add("pincode", typeof(string));
            table.Columns.Add("countryID", typeof(string));
            table.Columns.Add("gstNo", typeof(string));
            table.Columns.Add("isDefault", typeof(bool));
            table.Columns.Add("addressUid", typeof(string));

            foreach (var item in list)
            {
                table.Rows.Add(item.Email, item.AddressType, item.AddressLine1, item.AddressLine2,
                               item.StateID, item.CityID, item.Pincode, item.CountryID,
                               item.GstNo, item.IsDefault, item.AddressUid);
            }

            return table;
        }
        private DataTable ToContactDataTable(List<BPContactPerson> list)
        {
            var table = new DataTable();
            table.Columns.Add("firstName", typeof(string));
            table.Columns.Add("lastName", typeof(string));
            table.Columns.Add("designation", typeof(string));
            table.Columns.Add("email", typeof(string));
            table.Columns.Add("phone", typeof(string));
            table.Columns.Add("telephone", typeof(string));
            table.Columns.Add("isPrimary", typeof(bool));
            table.Columns.Add("contactUid", typeof(string));

            foreach (var item in list)
            {
                table.Rows.Add(item.FirstName, item.LastName, item.Designation, item.Email,
                               item.Phone, item.Telephone, item.IsPrimary, item.ContactUid);
            }

            return table;
        }
        private DataTable ToAttachmentDataTable(List<BPAttachment> list)
        {
            var table = new DataTable();
            table.Columns.Add("fileName", typeof(string));
            table.Columns.Add("filePath", typeof(string));
            table.Columns.Add("fileSize", typeof(long));
            table.Columns.Add("contentType", typeof(string));
            table.Columns.Add("fileType", typeof(string)); // Add this column

            foreach (var item in list)
            {
                table.Rows.Add(item.FileName, item.FilePath, item.FileSize, item.ContentType, item.fileType);
            }

            return table;
        }

        public async Task<IEnumerable<DistinctBankNameModel>> GetDistinctBankNameAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {

                var query = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTBANKNAME\"()";

                var result = await connection.QueryAsync<DistinctBankNameModel>(query);
                return result;
            }
        }
        public async Task<IEnumerable<SLPnameModel>> GetSLPnameAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTBSNAME\"()";

                var result = await connection.QueryAsync<SLPnameModel>(
                     query
                 );

                return result;
            }
        }
        public async Task<IEnumerable<ChainModel>> GetChainAsync(int company, string BPType, string IsStaff)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("BPType", BPType);
                parameters.Add("IsStaff", IsStaff);

                var sql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTCHAIN\"(?,?)";

                var result = await connection.QueryAsync<ChainModel>(
                     sql, parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<GetCountryModel>> GetCountryAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var sql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTCOUNTRIES\"()";

                var result = await connection.QueryAsync<GetCountryModel>(
                     sql
                 );

                return result;
            }
        }
        public async Task<IEnumerable<GetMainGroup>> GetMaingroupAsync(int company, string BPType, string IsStaff)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("BPType", BPType);
                parameters.Add("IsStaff", IsStaff);
                var sql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTMAINGROUPS\"(?,?)";

                var result = await connection.QueryAsync<GetMainGroup>(
                     sql, parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<GetMSMEType>> GetMSMEtypeAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var sql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTMSMEBTYPE\"()";

                var result = await connection.QueryAsync<GetMSMEType>(
                     sql
                 );

                return result;
            }
        }
        public async Task<IEnumerable<GroupNameResponse>> GetGroupNameByBPTypeAsync(int company, string bpType, string isStaff)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("bpType", bpType);
                parameters.Add("isStaff",isStaff);

                var query = $"CALL \"{settings.Schema}\".\"BPGETGROUPNAMEBYBPTYPE\"(?,?)";

                var result = await connection.QueryAsync<GroupNameResponse>(query, parameters);
                return result;

            }
        }
        public async Task<IEnumerable<PaymentGroupModel>> GetDistinctPaymentGroupsAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                await connection.OpenAsync();
                string sql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTPYMNTGROUP\"()";

                var result = await connection.QueryAsync<PaymentGroupModel>(sql);
                return result;
            }
        }
        public async Task<IEnumerable<BPStateModel>> GetDistinctStatesAsync(int company, string CountryCode)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("CountryCode", CountryCode);

                string sql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTSTATE\"(?)";

                var result = await connection.QueryAsync<BPStateModel>(sql, parameters);
                return result;
            }
        }
        public async Task<IEnumerable<ApprovedBpModel>> GetApprovedBPsAsync(int userId, int companyId, string month = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", month);

                var result = await connection.QueryAsync<ApprovedBpModel>(
                    "[BP].[jsGetApprovedBP]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
        }
        public async Task<IEnumerable<PendingBpModel>> GetPendingBpAsync(int userId, int companyId, string month = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@companyId", companyId);
            parameters.Add("@month", month);

            var result = await connection.QueryAsync<PendingBpModel>(
                "[BP].[jsGetPendingBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
        public async Task<IEnumerable<RejectedBPModel>> GetRejectedBpAsync(int userId, int companyId, string month = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@companyId", companyId);
            parameters.Add("@month", month);

            var result = await connection.QueryAsync<RejectedBPModel>(
                "[BP].[jsGetRejectedBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
        public async Task<SingleBPDataModel> GetSingleBPDataAsync(int bpCode, IUrlHelper urlHelper)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var result = new SingleBPDataModel();

                using (var multi = await connection.QueryMultipleAsync(
                    "[BP].[jsGetSingleBPData]",
                    new { bpCode },
                    commandType: CommandType.StoredProcedure))
                {
                    result.Master = await multi.ReadFirstOrDefaultAsync<BP_Master>();
                    result.TaxDetails = await multi.ReadFirstOrDefaultAsync<BP_Tax>();
                    result.Addresses = (await multi.ReadAsync<BP_Address>()).ToList();
                    result.BankDetails = (await multi.ReadAsync<BP_Bank>()).ToList();
                    result.ContactPersons = (await multi.ReadAsync<BP_Contact>()).ToList();
                    result.Attachments = (await multi.ReadAsync<BP_Attachment>()).ToList();
                    // Add FileUrl to each attachment
                    foreach (var file in result.Attachments)
                    {
                        if (string.IsNullOrEmpty(file.FilePath) || string.IsNullOrEmpty(file.FileName))
                            continue;

                        string cleanFilePath = file.FilePath.Replace("\\", "/").Trim();
                        if (cleanFilePath.StartsWith("/"))
                            cleanFilePath = cleanFilePath.Substring(1);

                        string fullFileName = file.FileName.Trim();
                        string fileExt = Path.GetExtension(fullFileName)?.TrimStart('.').ToLower();
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);


                        file.FileUrl = urlHelper.Action("AdvanceDownloadFile", "File", new
                        {
                            filePath = cleanFilePath,
                            fileName = fileNameWithoutExt,
                            fileExt = fileExt
                        }, protocol: "http");
                    }
                }

                return result;
            }
        }

        public async Task<ApproveOrRejectBpResponse> ApproveBPAsync(ApproveOrRejectBpRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@flowid", request.FlowId);
            parameters.Add("@company", request.Company);
            parameters.Add("@userId", request.UserId);
            var result = await connection.QuerySingleAsync<ApproveOrRejectBpResponse>(
                "[BP].[jsApproveBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return result;
        }
        public async Task<ApproveOrRejectBpResponse> RejectBPAsync(ApproveOrRejectBpRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@flowid", request.FlowId);
            parameters.Add("@company", request.Company);
            parameters.Add("@userId", request.UserId);

            var result = await connection.QuerySingleAsync<ApproveOrRejectBpResponse>(
                "[BP].[jsRejectBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
        public async Task<IEnumerable<BPGetCard>> BPGetCardInfoAsync(int company, string BPType, string IsStaff)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("BPType", BPType);
                parameters.Add("IsStaff", IsStaff);

                var query = $"CALL \"{settings.Schema}\".\"BPGETCARDINFO\"(?,?)";

                var result = await connection.QueryAsync<BPGetCard>(query, parameters);
                return result;
            }
        }
        public async Task<IEnumerable<UniquePANModel>> GetUniquePANsAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"BPGETUNIQUEPANS\"()";

                var result = await connection.QueryAsync<UniquePANModel>(query);
                return result;
            }
        }
        public async Task<IEnumerable<GSTMismatchByStateModel>> GetGSTMismatchByStateAsync(int company, string stateCode)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("stateCode", stateCode);

                var query = $"CALL \"{settings.Schema}\".\"BPGETGSTMISMATCHBYSTATEV2\"(?)";

                var result = await connection.QueryAsync<GSTMismatchByStateModel>(query, parameters);
                return result;
            }
        }
        public async Task<BPCountModel> GetBPCountsAsync(string month, int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@month", month);    // Format: "MM-YYYY"
                parameters.Add("@userId", userId);

                var result = await connection.QueryFirstOrDefaultAsync<BPCountModel>(
                    "[BP].[jsGetBPCounts]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
        }
        public async Task<IEnumerable<GetPricelist>> GetPricelistAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"BpGetPriceList\"()";
                var result = await connection.QueryAsync<GetPricelist>(query);
                return result;
            }
        }

        public async Task<UidResponse> CheckAddressUidAsync(string addressUid)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@addressUid", addressUid);

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<UidResponse>(
                    "[BP].[jsGetAddressUid]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result ?? new UidResponse { Message = "Unknown response" };
            }
            catch (SqlException ex) when (ex.Number == 50002)
            {
                // Custom exception from THROW in SQL
                return new UidResponse { Message = ex.Message };
            }
        }
        public async Task<UidResponse> CheckContactUidAsync(string contactUid)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@contactUid", contactUid);

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<UidResponse>(
                    "[BP].[jsGetContactUid]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result ?? new UidResponse { Message = "Unknown response" };
            }
            catch (SqlException ex) when (ex.Number == 50001)
            {
                return new UidResponse { Message = ex.Message };
            }
        }

        public async Task<IEnumerable<GetPanByBranch>> GetBpPANByBranchAsync(string Branch, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("Branch", Branch);
                parameters.Add("company", company);

                var query = $"CALL \"{settings.Schema}\".\"BP_GET_PAN_BY_BRANCH_COMPANY\"(?,?)";
                var result = await connection.QueryAsync<GetPanByBranch>(query, parameters);
                return result;
            }
        }

        public async Task<SPAData> GetSPADataAsync(int masterId)
        {

            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@masterId", masterId);

                var result = await connection.QueryFirstOrDefaultAsync<SPAData>(
                    "[BP].[jsGetSPAData]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }

        }

        public async Task<IEnumerable<MergeBpModel>> GetMergeBpModelAsync(int userId, int companyId, string month = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var pendingBP = await connection.QueryAsync<MergeBpModel>(
                    "EXEC [BP].[jsGetPendingBP] @userId, @companyId, @month", new { userId, companyId, month });

                var approvedBP = await connection.QueryAsync<MergeBpModel>(
                    "EXEC [BP].[jsGetApprovedBP] @userId, @companyId, @month", new { userId, companyId, month });

                var rejectedBP = await connection.QueryAsync<MergeBpModel>(
                    "EXEC [BP].[jsGetRejectedBP] @userId, @companyId, @month", new { userId, companyId, month });

                var allBP = new List<MergeBpModel>();

                foreach (var Items in pendingBP)
                {
                    Items.status = "pending";
                    allBP.Add(Items);
                }
                foreach (var Items in approvedBP)
                {
                    Items.status = "approved";
                    allBP.Add(Items);
                }
                foreach (var Items in rejectedBP)
                {
                    Items.status = "rejected";
                    allBP.Add(Items);
                }
                return allBP;
            }
        }

        public async Task<BPmasterModels> UpdateBPMasterAsync(BPMasterUpdateRequest model)
        {
            var response = new BPmasterModels { Success = false };

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("[BP].[jsUpdateBPMasterData]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Simple master parameters
                    cmd.Parameters.AddWithValue("@Code", model.Code);
                    cmd.Parameters.AddWithValue("@type", model.Type);
                    cmd.Parameters.AddWithValue("@isStaff", model.IsStaff);
                    cmd.Parameters.AddWithValue("@staffCode", (object?)model.StaffCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@company", model.Company);
                    cmd.Parameters.AddWithValue("@groupID", model.GroupID);
                    cmd.Parameters.AddWithValue("@mainGroupID", model.MainGroupID);
                    cmd.Parameters.AddWithValue("@chain", (object?)model.Chain ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@contactPerson", (object?)model.ContactPerson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mobileNo", (object?)model.MobileNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@paymentTermID", (object?)model.PaymentTermID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@creditLimit", (object?)model.CreditLimit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@priceList", (object?)model.PriceList ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@userId", model.UserId);
                    cmd.Parameters.AddWithValue("@companyByUser", model.CompanyByUser ?? "");

                    // Tax details
                    cmd.Parameters.AddWithValue("@buyerTANNo", (object?)model.BuyerTANNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@panNo", model.PanNo);
                    cmd.Parameters.AddWithValue("@fssaiNo", (object?)model.FssaiNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeNo", (object?)model.MsmeNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeType", (object?)model.MsmeType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeBusinessType", (object?)model.MsmeBusinessType ?? DBNull.Value);
                    // Bank details
                    cmd.Parameters.AddWithValue("@bankName", (object?)model.BankName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@accountNo", (object?)model.AccountNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ifscCode", (object?)model.IfscCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@bankCountryID", (object?)model.BankCountryID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@acctName", (object?)model.AcctName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@branch", (object?)model.Branch ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@swiftCode", (object?)model.SwiftCode ?? DBNull.Value);

                    // Control flags
                    cmd.Parameters.Add(new SqlParameter("@updateAddresses", SqlDbType.Bit) { Value = model.UpdateAddresses });
                    cmd.Parameters.Add(new SqlParameter("@updateBankDetails", SqlDbType.Bit) { Value = model.UpdateBankDetails });
                    cmd.Parameters.Add(new SqlParameter("@updateContacts", SqlDbType.Bit) { Value = model.UpdateContacts });
                    cmd.Parameters.Add(new SqlParameter("@updateAttachments", SqlDbType.Bit) { Value = model.UpdateAttachments });

                    // Table-valued parameters
                    var addressTable = model.Addresses?.Count > 0 ? ToAddressDataTable(model.Addresses) : ToAddressDataTable(new List<BPMasterAddress>());
                    var contactTable = model.Contacts?.Count > 0 ? ToContactDataTable(model.Contacts) : ToContactDataTable(new List<BPContactPerson>());
                    var attachmentTable = model.Attachments?.Count > 0 ? ToAttachmentDataTable(model.Attachments) : ToAttachmentDataTable(new List<BPAttachment>());

                    var addrParam = cmd.Parameters.AddWithValue("@addresses", addressTable);
                    addrParam.SqlDbType = SqlDbType.Structured;

                    var contactParam = cmd.Parameters.AddWithValue("@contacts", contactTable);
                    contactParam.SqlDbType = SqlDbType.Structured;

                    var attachParam = cmd.Parameters.AddWithValue("@attachments", attachmentTable);
                    attachParam.SqlDbType = SqlDbType.Structured;

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "BP Master updated successfully.";
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }
        public async Task<BPmasterModels> UpdateSapDataAsync(BpSapDataUpdateRequest model)
        {
            var result = new BPmasterModels { Success = false };

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("[BP].[jsUpdateSAPData]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@masterId", model.MasterId);
                    cmd.Parameters.AddWithValue("@debPayAcct", model.DebPayAcct);
                    cmd.Parameters.AddWithValue("@wtLabel", model.WtLabel);
                    cmd.Parameters.AddWithValue("@series", model.Series);
                    cmd.Parameters.AddWithValue("@grpCode", model.GrpCode);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    result.Success = true;
                    result.Message = "SAP data updated successfully.";
                }
            }
            catch (SqlException ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            catch (System.Exception ex)
            {
                result.Success = false;
                result.Message = "Internal server error.";
            }

            return result;
        }

        public async Task<IEnumerable<BPinsightsModel>> GetBPInsightsAsync(int userId, int companyId, string? month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", month);

                var result = await connection.QueryAsync<BPinsightsModel>(
                    "[BP].[jsGetBPInsights]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
        }

        public async Task<IEnumerable<BPinsightsModel>> GetBPInsightsByCreatorAsync(int userId, int companyId, string? month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", month);

                var result = await connection.QueryAsync<BPinsightsModel>(
                    "[BP].[jsGetBPInsightsByCreator]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
        }

        public async Task<IEnumerable<BPApprovalFlowModel>> GetBPApprovalFlowAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", flowId);

                var result = await connection.QueryAsync<BPApprovalFlowModel>(
                    "[BP].[jsGetBPApprovalFlow]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
        }
    }
}
