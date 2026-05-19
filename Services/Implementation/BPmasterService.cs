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
using System.Collections.Concurrent;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    public class BPmasterService : IBPmasterService
    {
        private readonly IConfiguration _configuration;
        private readonly IBPMasterSapService _bpMasterSapService;
        private readonly string _connectionString;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _approvalLocks = new();
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _sapPostLocks = new();

        public BPmasterService(IConfiguration configuration, IBPMasterSapService bpMasterSapService)
        {
            _configuration = configuration;
            _bpMasterSapService = bpMasterSapService;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            var activeEnv = configuration["ActiveEnvironment"];  // "Test" or "Live"
            _hanaSettings = configuration.GetSection($"HanaSettings:{activeEnv}")
                                         .Get<Dictionary<int, HanaCompanySettings>>();
        }

        private HanaCompanySettings GetHanaSettings(int company)
        {
            if (_hanaSettings == null || !_hanaSettings.TryGetValue(company, out var settings) || settings == null)
                throw new ArgumentException($"Invalid company ID: {company}");

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException($"HANA connection string is missing for company ID: {company}");

            if (string.IsNullOrWhiteSpace(settings.Schema))
                throw new InvalidOperationException($"HANA schema is missing for company ID: {company}");

            return settings;
        }

        private async Task<IEnumerable<T>> QueryHanaWithFallbackAsync<T>(
            int company,
            string optionName,
            string primarySql,
            object primaryParams = null,
            string fallbackSql = null,
            object fallbackParams = null,
            IEnumerable<T> staticFallback = null)
        {
            var settings = GetHanaSettings(company);

            try
            {
                using var connection = new HanaConnection(settings.ConnectionString);
                return await connection.QueryAsync<T>(primarySql, primaryParams);
            }
            catch (Exception primaryEx)
            {
                if (!string.IsNullOrWhiteSpace(fallbackSql))
                {
                    try
                    {
                        using var fallbackConnection = new HanaConnection(settings.ConnectionString);
                        return await fallbackConnection.QueryAsync<T>(fallbackSql, fallbackParams);
                    }
                    catch (Exception fallbackEx)
                    {
                        throw new InvalidOperationException(
                            $"{optionName} lookup failed. Primary error: {primaryEx.Message}. Fallback error: {fallbackEx.Message}",
                            fallbackEx);
                    }
                }

                if (staticFallback != null)
                    return staticFallback;

                throw new InvalidOperationException($"{optionName} lookup failed: {primaryEx.Message}", primaryEx);
            }
        }

        private static string NormalizeBpType(string bpType)
        {
            var value = (bpType ?? string.Empty).Trim().ToUpperInvariant();
            if (value == "V" || value == "S" || value == "SUPPLIER" || value == "VENDOR")
                return "V";

            return "C";
        }

        private static string NormalizeSapGroupType(string bpType)
        {
            return NormalizeBpType(bpType) == "V" ? "S" : "C";
        }

        private static string NormalizeIsStaff(string isStaff)
        {
            var value = (isStaff ?? string.Empty).Trim().ToLowerInvariant();
            return value is "1" or "true" or "yes" or "y" ? "true" : "false";
        }

        private static string NormalizeCountryCode(string countryCode)
        {
            var value = (countryCode ?? string.Empty).Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(value) ? "IN" : value;
        }

        private static string FirstText(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
        }

        private static List<ChainModel> NormalizeChains(IEnumerable<ChainModel> rows)
        {
            return (rows ?? Enumerable.Empty<ChainModel>())
                .Select(row =>
                {
                    var code = FirstText(row.Code, row.U_Chain, row.Name);
                    var name = FirstText(row.Name, row.U_Chain, code);
                    return new ChainModel { Code = code, Name = name, U_Chain = FirstText(row.U_Chain, code) };
                })
                .Where(row => !string.IsNullOrWhiteSpace(row.Code) || !string.IsNullOrWhiteSpace(row.Name))
                .ToList();
        }

        private static List<GetMainGroup> NormalizeMainGroups(IEnumerable<GetMainGroup> rows)
        {
            return (rows ?? Enumerable.Empty<GetMainGroup>())
                .Select(row =>
                {
                    var code = FirstText(row.Code, row.U_Main_Group, row.Name);
                    var name = FirstText(row.Name, row.U_Main_Group, code);
                    return new GetMainGroup { Code = code, Name = name, U_Main_Group = FirstText(row.U_Main_Group, code) };
                })
                .Where(row => !string.IsNullOrWhiteSpace(row.Code) || !string.IsNullOrWhiteSpace(row.Name))
                .ToList();
        }

        private static List<GroupNameResponse> NormalizeGroups(IEnumerable<GroupNameResponse> rows)
        {
            return (rows ?? Enumerable.Empty<GroupNameResponse>())
                .Select(row =>
                {
                    var code = FirstText(row.Code, row.GroupCode?.ToString());
                    var name = FirstText(row.Name, row.GroupName);
                    return new GroupNameResponse
                    {
                        GroupCode = row.GroupCode,
                        Code = code,
                        Name = name,
                        GroupName = FirstText(row.GroupName, name)
                    };
                })
                .Where(row => row.GroupCode.HasValue || !string.IsNullOrWhiteSpace(row.Code) || !string.IsNullOrWhiteSpace(row.GroupName))
                .ToList();
        }

        private static List<PaymentGroupModel> NormalizePaymentGroups(IEnumerable<PaymentGroupModel> rows)
        {
            return (rows ?? Enumerable.Empty<PaymentGroupModel>())
                .Select(row =>
                {
                    var code = FirstText(row.Code, row.GroupNum?.ToString());
                    var name = FirstText(row.Name, row.PymntGroup);
                    return new PaymentGroupModel
                    {
                        GroupNum = row.GroupNum,
                        Code = code,
                        Name = name,
                        PymntGroup = FirstText(row.PymntGroup, name)
                    };
                })
                .Where(row => row.GroupNum.HasValue || !string.IsNullOrWhiteSpace(row.Code) || !string.IsNullOrWhiteSpace(row.PymntGroup))
                .ToList();
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
            var settings = GetHanaSettings(company);

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTBANKNAME\"()";
            var fallbackSql = $@"
                SELECT
                    ""BankCode"",
                    ""BankName""
                FROM ""{settings.Schema}"".""ODSC""
                ORDER BY ""BankName""";

            return await QueryHanaWithFallbackAsync<DistinctBankNameModel>(
                company,
                "Bank options",
                primarySql,
                fallbackSql: fallbackSql);
        }
        public async Task<IEnumerable<SLPnameModel>> GetSLPnameAsync(int company)
        {
            var settings = GetHanaSettings(company);

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTBSNAME\"()";
            var fallbackSql = $@"
                SELECT ""SlpCode"", ""SlpName""
                FROM ""{settings.Schema}"".""OSLP""
                WHERE ""SlpCode"" > 0
                  AND IFNULL(""Locked"", 'N') = 'N'
                ORDER BY ""SlpName""";

            return await QueryHanaWithFallbackAsync<SLPnameModel>(
                company,
                "Sales employee options",
                primarySql,
                fallbackSql: fallbackSql);
        }
        public async Task<IEnumerable<ChainModel>> GetChainAsync(int company, string BPType, string IsStaff)
        {
            var settings = GetHanaSettings(company);
            var parameters = new DynamicParameters();
            parameters.Add("BPType", NormalizeBpType(BPType));
            parameters.Add("IsStaff", NormalizeIsStaff(IsStaff));

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTCHAIN\"(?,?)";
            var fallbackSql = $@"
                SELECT
                    ""Code"",
                    IFNULL(""Name"", ""Code"") AS ""Name"",
                    ""Code"" AS ""U_Chain""
                FROM ""{settings.Schema}"".""@CHAIN""
                ORDER BY ""Code""";

            var result = await QueryHanaWithFallbackAsync<ChainModel>(
                company,
                "Chain options",
                primarySql,
                parameters,
                fallbackSql);

            return NormalizeChains(result);
        }
        public async Task<IEnumerable<GetCountryModel>> GetCountryAsync(int company)
        {
            var settings = GetHanaSettings(company);

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTCOUNTRIES\"()";
            var fallbackSql = $@"
                SELECT ""Code"", ""Name""
                FROM ""{settings.Schema}"".""OCRY""
                ORDER BY ""Name""";

            return await QueryHanaWithFallbackAsync<GetCountryModel>(
                company,
                "Country options",
                primarySql,
                fallbackSql: fallbackSql);
        }
        public async Task<IEnumerable<GetMainGroup>> GetMaingroupAsync(int company, string BPType, string IsStaff)
        {
            var settings = GetHanaSettings(company);
            var parameters = new DynamicParameters();
            parameters.Add("BPType", NormalizeBpType(BPType));
            parameters.Add("IsStaff", NormalizeIsStaff(IsStaff));

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTMAINGROUPS\"(?,?)";
            var fallbackSql = $@"
                SELECT
                    ""Code"",
                    IFNULL(""Name"", ""Code"") AS ""Name"",
                    ""Code"" AS ""U_Main_Group""
                FROM ""{settings.Schema}"".""@MAIN_GROUP""
                ORDER BY ""Code""";

            var result = await QueryHanaWithFallbackAsync<GetMainGroup>(
                company,
                "Main group options",
                primarySql,
                parameters,
                fallbackSql);

            return NormalizeMainGroups(result);
        }
        public async Task<IEnumerable<GetMSMEType>> GetMSMEtypeAsync(int company)
        {
            var settings = GetHanaSettings(company);
            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTMSMEBTYPE\"()";
            var staticFallback = new[]
            {
                new GetMSMEType { U_MSME_BType = "Manufacturing" },
                new GetMSMEType { U_MSME_BType = "Service" },
                new GetMSMEType { U_MSME_BType = "Trading" },
                new GetMSMEType { U_MSME_BType = "Others" }
            };

            return await QueryHanaWithFallbackAsync<GetMSMEType>(
                company,
                "MSME business type options",
                primarySql,
                staticFallback: staticFallback);
        }
        public async Task<IEnumerable<GroupNameResponse>> GetGroupNameByBPTypeAsync(int company, string bpType, string isStaff)
        {
            var settings = GetHanaSettings(company);

            var parameters = new DynamicParameters();
            parameters.Add("bpType", NormalizeBpType(bpType));
            parameters.Add("isStaff", NormalizeIsStaff(isStaff));

            var fallbackParameters = new DynamicParameters();
            fallbackParameters.Add("GroupType", NormalizeSapGroupType(bpType));

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETGROUPNAMEBYBPTYPE\"(?,?)";
            var fallbackSql = $@"
                SELECT
                    ""GroupCode"",
                    CAST(""GroupCode"" AS NVARCHAR(20)) AS ""Code"",
                    ""GroupName"",
                    ""GroupName"" AS ""Name""
                FROM ""{settings.Schema}"".""OCRG""
                WHERE ""GroupType"" = ?
                ORDER BY ""GroupName""";

            var result = NormalizeGroups(await QueryHanaWithFallbackAsync<GroupNameResponse>(
                company,
                "BP group options",
                primarySql,
                parameters,
                fallbackSql,
                fallbackParameters));

            if (result.Count == 0 || result.All(row => !row.GroupCode.HasValue && string.IsNullOrWhiteSpace(row.Code)))
            {
                result = NormalizeGroups(await QueryHanaWithFallbackAsync<GroupNameResponse>(
                    company,
                    "BP group code options",
                    fallbackSql,
                    fallbackParameters));
            }

            return result;
        }
        public async Task<IEnumerable<PaymentGroupModel>> GetDistinctPaymentGroupsAsync(int company)
        {
            var settings = GetHanaSettings(company);

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTPYMNTGROUP\"()";
            var fallbackSql = $@"
                SELECT
                    ""GroupNum"",
                    CAST(""GroupNum"" AS NVARCHAR(20)) AS ""Code"",
                    ""PymntGroup"",
                    ""PymntGroup"" AS ""Name""
                FROM ""{settings.Schema}"".""OCTG""
                ORDER BY ""PymntGroup""";

            var result = NormalizePaymentGroups(await QueryHanaWithFallbackAsync<PaymentGroupModel>(
                company,
                "Payment term options",
                primarySql,
                fallbackSql: fallbackSql));

            if (result.Count == 0 || result.All(row => !row.GroupNum.HasValue && string.IsNullOrWhiteSpace(row.Code)))
            {
                result = NormalizePaymentGroups(await QueryHanaWithFallbackAsync<PaymentGroupModel>(
                    company,
                    "Payment term code options",
                    fallbackSql));
            }

            return result;
        }
        public async Task<IEnumerable<BPStateModel>> GetDistinctStatesAsync(int company, string CountryCode)
        {
            var settings = GetHanaSettings(company);
            var normalizedCountry = NormalizeCountryCode(CountryCode);

            var parameters = new DynamicParameters();
            parameters.Add("CountryCode", normalizedCountry);

            var fallbackParameters = new DynamicParameters();
            fallbackParameters.Add("Country", normalizedCountry);

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETDISTINCTSTATE\"(?)";
            var fallbackSql = $@"
                SELECT ""Code"", ""Name""
                FROM ""{settings.Schema}"".""OCST""
                WHERE ""Country"" = ?
                ORDER BY ""Name""";

            return await QueryHanaWithFallbackAsync<BPStateModel>(
                company,
                "State options",
                primarySql,
                parameters,
                fallbackSql,
                fallbackParameters);
        }
        public async Task<BPOptionsModel> GetOptionsAsync(int company, string bpType, string isStaff, string countryCode = "IN")
        {
            var options = new BPOptionsModel();
            var normalizedBpType = NormalizeBpType(bpType);
            var normalizedIsStaff = NormalizeIsStaff(isStaff);
            var normalizedCountryCode = NormalizeCountryCode(countryCode);

            async Task LoadAsync<T>(string key, Func<Task<IEnumerable<T>>> loader, Action<List<T>> assign)
            {
                try
                {
                    assign((await loader()).ToList());
                }
                catch (Exception ex)
                {
                    options.Errors[key] = ex.Message;
                    assign(new List<T>());
                }
            }

            await LoadAsync("banks", () => GetDistinctBankNameAsync(company), value => options.Banks = value);
            await LoadAsync("salesEmployees", () => GetSLPnameAsync(company), value => options.SalesEmployees = value);
            await LoadAsync("chains", () => GetChainAsync(company, normalizedBpType, normalizedIsStaff), value => options.Chains = value);
            await LoadAsync("countries", () => GetCountryAsync(company), value => options.Countries = value);
            await LoadAsync("mainGroups", () => GetMaingroupAsync(company, normalizedBpType, normalizedIsStaff), value => options.MainGroups = value);
            await LoadAsync("msmeBusinessTypes", () => GetMSMEtypeAsync(company), value => options.MsmeBusinessTypes = value);
            await LoadAsync("groups", () => GetGroupNameByBPTypeAsync(company, normalizedBpType, normalizedIsStaff), value => options.Groups = value);
            await LoadAsync("paymentTerms", () => GetDistinctPaymentGroupsAsync(company), value => options.PaymentTerms = value);
            await LoadAsync("states", () => GetDistinctStatesAsync(company, normalizedCountryCode), value => options.States = value);
            await LoadAsync("priceLists", () => GetPricelistAsync(company), value => options.PriceLists = value);
            await LoadAsync("uniquePANs", () => GetUniquePANsAsync(company), value => options.UniquePANs = value);
            await LoadAsync("existingCards", () => BPGetCardInfoAsync(company, normalizedBpType, normalizedIsStaff), value => options.ExistingCards = value);

            return options;
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
            var approvalLock = _approvalLocks.GetOrAdd(request.FlowId, _ => new SemaphoreSlim(1, 1));
            if (!await approvalLock.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = $"BP flow {request.FlowId} is already being approved. Please retry shortly.",
                    ApprovalStatus = "Blocked",
                    SapStatus = "Skipped - concurrent approval detected"
                };
            }

            try
            {
                var flow = await GetBpFlowRuntimeAsync(request.FlowId);
                if (flow == null)
                {
                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        ResultMessage = "BP workflow not found.",
                        ApprovalStatus = "Blocked",
                        SapStatus = "Skipped"
                    };
                }

                if (flow.Company != request.Company)
                {
                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        ResultMessage = "Access denied: BP belongs to a different company.",
                        ApprovalStatus = "Blocked",
                        SapStatus = "Skipped"
                    };
                }

                if (!flow.IsFinalStage || !string.Equals(request.Action ?? "Approve", "Approve", StringComparison.OrdinalIgnoreCase))
                {
                    var nonFinalResult = await ExecuteApproveProcedureAsync(request);
                    nonFinalResult.Success = true;
                    nonFinalResult.ApprovalStatus = "Advanced";
                    nonFinalResult.SapStatus = "Not final stage";
                    return nonFinalResult;
                }

                var authorizedForFinalStage = await IsUserAssignedToCurrentStageAsync(request.UserId, flow.CurrentStageId);
                if (!authorizedForFinalStage)
                {
                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        BPCode = flow.BpCode,
                        BPCompany = flow.Company,
                        ResultMessage = "User is not assigned to the final BP approval stage.",
                        ApprovalStatus = "Blocked",
                        SapStatus = "Skipped"
                    };
                }

                var sapLock = _sapPostLocks.GetOrAdd(flow.BpCode, _ => new SemaphoreSlim(1, 1));
                if (!await sapLock.WaitAsync(TimeSpan.FromSeconds(30)))
                {
                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        BPCode = flow.BpCode,
                        BPCompany = flow.Company,
                        ResultMessage = "Another request is already creating this BP in SAP. Please retry shortly.",
                        ApprovalStatus = "Blocked",
                        SapStatus = "Processing"
                    };
                }

                try
                {
                    var previousTag = await UpdateBpApiStatusAsync(flow.BpCode, "Processing SAP BP creation", "P", null, null, null, request.UserId);
                    if (string.Equals(previousTag.PreviousTag, "Y", StringComparison.OrdinalIgnoreCase))
                    {
                        var alreadySynced = await ExecuteApproveProcedureAsync(request);
                        alreadySynced.Success = true;
                        alreadySynced.ApprovalStatus = "Approved";
                        alreadySynced.SapStatus = "Already synced";
                        return alreadySynced;
                    }

                    if (string.Equals(previousTag.PreviousTag, "P", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ApproveOrRejectBpResponse
                        {
                            Success = false,
                            BPCode = flow.BpCode,
                            BPCompany = flow.Company,
                            ResultMessage = "SAP BP creation is already processing. Please retry shortly.",
                            ApprovalStatus = "Blocked",
                            SapStatus = "Processing"
                        };
                    }

                    var bpData = await GetSingleBPDataForSapAsync(flow.BpCode);
                    var sapData = await GetSPADataAsync(flow.BpCode);
                    var sapResult = await _bpMasterSapService.PostBusinessPartnerAsync(new BpSapPostRequest
                    {
                        FlowId = flow.FlowId,
                        BpCode = flow.BpCode,
                        Company = flow.Company,
                        UserId = request.UserId,
                        BpType = flow.BpType,
                        BpData = bpData,
                        SapData = sapData
                    });

                    if (!sapResult.Success)
                    {
                        await UpdateBpApiStatusAsync(flow.BpCode, sapResult.Message, "N", sapResult.CardCode, sapResult.AttachmentEntry, sapResult.PayloadHash, request.UserId);
                        return new ApproveOrRejectBpResponse
                        {
                            Success = false,
                            BPCode = flow.BpCode,
                            BPCompany = flow.Company,
                            ResultMessage = $"SAP BP creation failed: {sapResult.Message}",
                            ApprovalStatus = "Blocked",
                            SapStatus = "Failed",
                            SapCardCode = sapResult.CardCode,
                            AttachmentEntry = sapResult.AttachmentEntry,
                            PayloadHash = sapResult.PayloadHash
                        };
                    }

                    await UpdateBpApiStatusAsync(flow.BpCode, sapResult.Message, "Y", sapResult.CardCode, sapResult.AttachmentEntry, sapResult.PayloadHash, request.UserId);
                    var approved = await ExecuteApproveProcedureAsync(request);
                    approved.Success = true;
                    approved.BPCode = approved.BPCode == 0 ? flow.BpCode : approved.BPCode;
                    approved.BPCompany = approved.BPCompany == 0 ? flow.Company : approved.BPCompany;
                    approved.ApprovalStatus = "Approved";
                    approved.SapStatus = "Success";
                    approved.SapCardCode = sapResult.CardCode;
                    approved.AttachmentEntry = sapResult.AttachmentEntry;
                    approved.PayloadHash = sapResult.PayloadHash;
                    approved.ResultMessage = string.IsNullOrWhiteSpace(approved.ResultMessage)
                        ? sapResult.Message
                        : $"{approved.ResultMessage} {sapResult.Message}";
                    return approved;
                }
                finally
                {
                    sapLock.Release();
                }
            }
            finally
            {
                approvalLock.Release();
            }
        }

        public async Task<ApproveOrRejectBpResponse> RetrySapPostAsync(ApproveOrRejectBpRequest request)
        {
            var flow = await GetBpFlowRuntimeAsync(request.FlowId);
            if (flow == null)
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = "BP workflow not found.",
                    ApprovalStatus = "Blocked",
                    SapStatus = "Skipped"
                };
            }

            if (flow.Company != request.Company)
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    ResultMessage = "Access denied: BP belongs to a different company.",
                    ApprovalStatus = "Blocked",
                    SapStatus = "Skipped"
                };
            }

            if (!string.Equals(flow.FlowStatus, "P", StringComparison.OrdinalIgnoreCase))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    ResultMessage = "SAP retry is only allowed for pending BP workflows.",
                    ApprovalStatus = "Blocked",
                    SapStatus = "Skipped"
                };
            }

            if (!flow.IsFinalStage)
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    ResultMessage = "SAP retry is only allowed at the final BP approval stage.",
                    ApprovalStatus = "Blocked",
                    SapStatus = "Skipped - not final stage"
                };
            }

            var apiStatusTag = await GetBpApiStatusTagAsync(flow.BpCode);
            if (string.Equals(apiStatusTag, "P", StringComparison.OrdinalIgnoreCase))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    ResultMessage = "SAP BP creation is already processing. Please retry shortly.",
                    ApprovalStatus = "Blocked",
                    SapStatus = "Processing"
                };
            }

            if (string.IsNullOrWhiteSpace(apiStatusTag))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    ResultMessage = "No failed SAP BP posting exists for retry. Use normal final approval.",
                    ApprovalStatus = "Blocked",
                    SapStatus = "Not started"
                };
            }

            if (!string.Equals(apiStatusTag, "N", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(apiStatusTag, "Y", StringComparison.OrdinalIgnoreCase))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    ResultMessage = $"SAP retry is not allowed for current SAP status tag '{apiStatusTag}'.",
                    ApprovalStatus = "Blocked",
                    SapStatus = apiStatusTag
                };
            }

            request.Action = "Approve";
            return await ApproveBPAsync(request);
        }

        private async Task<ApproveOrRejectBpResponse> ExecuteApproveProcedureAsync(ApproveOrRejectBpRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@flowid", request.FlowId);
            parameters.Add("@company", request.Company);
            parameters.Add("@userId", request.UserId);
            parameters.Add("@remarks", request.Remarks ?? "");
            parameters.Add("@action", string.IsNullOrWhiteSpace(request.Action) ? "Approve" : request.Action);

            var result = await connection.QuerySingleAsync<ApproveOrRejectBpResponse>(
                "[BP].[jsApproveBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        private async Task<BpFlowRuntimeModel?> GetBpFlowRuntimeAsync(int flowId)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
SELECT
    f.id AS FlowId,
    f.bpCode AS BpCode,
    f.status AS FlowStatus,
    f.currentStage AS CurrentStage,
    f.totalStage AS TotalStage,
    f.currentStageId AS CurrentStageId,
    f.templateId AS TemplateId,
    m.company AS Company,
    m.type AS BpType
FROM BP.jsFlow f
INNER JOIN BP.jsMaster m ON m.code = f.bpCode
WHERE f.id = @flowId;";

            return await connection.QueryFirstOrDefaultAsync<BpFlowRuntimeModel>(sql, new { flowId });
        }

        private async Task<bool> IsUserAssignedToCurrentStageAsync(int userId, int stageId)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = "SELECT COUNT(1) FROM jsUserStage WHERE userId = @userId AND stageId = @stageId;";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { userId, stageId });
            return count > 0;
        }

        private async Task<SingleBPDataModel> GetSingleBPDataForSapAsync(int bpCode)
        {
            using var connection = new SqlConnection(_connectionString);
            using var multi = await connection.QueryMultipleAsync(
                "[BP].[jsGetSingleBPData]",
                new { bpCode },
                commandType: CommandType.StoredProcedure);

            return new SingleBPDataModel
            {
                Master = await multi.ReadFirstOrDefaultAsync<BP_Master>(),
                TaxDetails = await multi.ReadFirstOrDefaultAsync<BP_Tax>(),
                Addresses = (await multi.ReadAsync<BP_Address>()).ToList(),
                BankDetails = (await multi.ReadAsync<BP_Bank>()).ToList(),
                ContactPersons = (await multi.ReadAsync<BP_Contact>()).ToList(),
                Attachments = (await multi.ReadAsync<BP_Attachment>()).ToList()
            };
        }

        private async Task<string?> GetBpApiStatusTagAsync(int bpCode)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
SELECT TOP 1 apiStatusTag
FROM BP.jsSAPData
WHERE masterId = @bpCode
ORDER BY id DESC;";

            return await connection.QueryFirstOrDefaultAsync<string?>(sql, new { bpCode });
        }

        private async Task<BpApiStatusUpdateResult> UpdateBpApiStatusAsync(
            int bpCode,
            string apiMessage,
            string tag,
            string? sapCardCode,
            int? attachmentEntry,
            string? payloadHash,
            int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@bpCode", bpCode);
                parameters.Add("@apiMessage", TruncateForDb(apiMessage, 1000));
                parameters.Add("@tag", tag);
                parameters.Add("@sapCardCode", sapCardCode);
                parameters.Add("@attachmentEntry", attachmentEntry);
                parameters.Add("@payloadHash", payloadHash);
                parameters.Add("@userId", userId);

                var previousTag = await connection.QueryFirstOrDefaultAsync<string>(
                    "[BP].[jsUpdateBpApiStatus]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return new BpApiStatusUpdateResult
                {
                    PreviousTag = previousTag,
                    Message = "BP SAP API status updated."
                };
            }
            catch (SqlException ex) when (ex.Number == 2812 || ex.Message.Contains("jsUpdateBpApiStatus", StringComparison.OrdinalIgnoreCase))
            {
                return new BpApiStatusUpdateResult
                {
                    ProcedureAvailable = false,
                    PreviousTag = null,
                    Message = "BP.jsUpdateBpApiStatus is not deployed. Application locks are active, but cross-process SAP idempotency requires the SQL procedure."
                };
            }
        }

        private static string TruncateForDb(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        public async Task<ApproveOrRejectBpResponse> RejectBPAsync(ApproveOrRejectBpRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@flowid", request.FlowId);
            parameters.Add("@company", request.Company);
            parameters.Add("@userId", request.UserId);
            parameters.Add("@remarks", request.Remarks ?? "");
            parameters.Add("@action", string.IsNullOrWhiteSpace(request.Action) ? "Reject" : request.Action);

            var result = await connection.QuerySingleAsync<ApproveOrRejectBpResponse>(
                "[BP].[jsRejectBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            result.Success = true;
            result.ApprovalStatus = "Rejected";
            result.SapStatus = "Not applicable";
            return result;
        }
        public async Task<IEnumerable<BPGetCard>> BPGetCardInfoAsync(int company, string BPType, string IsStaff)
        {
            var settings = GetHanaSettings(company);

            var parameters = new DynamicParameters();
            parameters.Add("BPType", NormalizeBpType(BPType));
            parameters.Add("IsStaff", NormalizeIsStaff(IsStaff));

            var fallbackParameters = new DynamicParameters();
            fallbackParameters.Add("CardType", NormalizeBpType(BPType) == "V" ? "S" : "C");

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETCARDINFO\"(?,?)";
            var fallbackSql = $@"
                SELECT
                    c.""CardCode"",
                    c.""CardName"",
                    IFNULL(a.""Street"", '') AS ""Address"",
                    IFNULL(a.""State"", '') AS ""State"",
                    IFNULL(a.""GSTRegnNo"", '') AS ""GSTRegnNo""
                FROM ""{settings.Schema}"".""OCRD"" c
                LEFT JOIN ""{settings.Schema}"".""CRD1"" a
                    ON a.""CardCode"" = c.""CardCode""
                   AND a.""AdresType"" = 'B'
                WHERE c.""CardType"" = ?
                ORDER BY c.""CardName""";

            return await QueryHanaWithFallbackAsync<BPGetCard>(
                company,
                "Existing BP card options",
                primarySql,
                parameters,
                fallbackSql,
                fallbackParameters);
        }
        public async Task<IEnumerable<UniquePANModel>> GetUniquePANsAsync(int company)
        {
            var settings = GetHanaSettings(company);

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETUNIQUEPANS\"()";
            var fallbackSql = $@"
                SELECT DISTINCT
                    ""LicTradNum"" AS ""PAN_Number""
                FROM ""{settings.Schema}"".""OCRD""
                WHERE IFNULL(""LicTradNum"", '') <> ''
                ORDER BY ""LicTradNum""";

            return await QueryHanaWithFallbackAsync<UniquePANModel>(
                company,
                "PAN options",
                primarySql,
                fallbackSql: fallbackSql);
        }
        public async Task<IEnumerable<GSTMismatchByStateModel>> GetGSTMismatchByStateAsync(int company, string stateCode)
        {
            var settings = GetHanaSettings(company);
            var normalizedState = (stateCode ?? string.Empty).Trim().ToUpperInvariant();

            var parameters = new DynamicParameters();
            parameters.Add("stateCode", normalizedState);

            var fallbackParameters = new DynamicParameters();
            fallbackParameters.Add("StateCode1", normalizedState);
            fallbackParameters.Add("StateCode2", normalizedState);

            var primarySql = $"CALL \"{settings.Schema}\".\"BPGETGSTMISMATCHBYSTATEV2\"(?)";
            var fallbackSql = $@"
                SELECT
                    ""Code"",
                    ""Country"",
                    ""Name"",
                    ""Code"" AS ""GSTCode""
                FROM ""{settings.Schema}"".""OCST""
                WHERE ""Country"" = 'IN'
                  AND (UPPER(""Code"") = ? OR UPPER(""Name"") LIKE '%' || ? || '%')
                ORDER BY ""Name""";

            return await QueryHanaWithFallbackAsync<GSTMismatchByStateModel>(
                company,
                "GST state validation options",
                primarySql,
                parameters,
                fallbackSql,
                fallbackParameters);
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
            var settings = GetHanaSettings(company);
            var primarySql = $"CALL \"{settings.Schema}\".\"BpGetPriceList\"()";
            var fallbackSql = $@"
                SELECT
                    ""ListNum"",
                    CAST(""ListNum"" AS NVARCHAR(20)) AS ""Code"",
                    ""ListName"",
                    ""ListName"" AS ""Name""
                FROM ""{settings.Schema}"".""OPLN""
                ORDER BY ""ListName""";

            return await QueryHanaWithFallbackAsync<GetPricelist>(
                company,
                "Price list options",
                primarySql,
                fallbackSql: fallbackSql);
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
            var settings = GetHanaSettings(company);
            var normalizedBranch = (Branch ?? string.Empty).Trim().ToUpperInvariant();

            var parameters = new DynamicParameters();
            parameters.Add("Branch", Branch);
            parameters.Add("company", company);

            var fallbackParameters = new DynamicParameters();
            fallbackParameters.Add("Company", company);
            fallbackParameters.Add("BranchLabel", normalizedBranch);
            fallbackParameters.Add("BranchCity", normalizedBranch);
            fallbackParameters.Add("BranchState", normalizedBranch);

            var primarySql = $"CALL \"{settings.Schema}\".\"BP_GET_PAN_BY_BRANCH_COMPANY\"(?,?)";
            var fallbackSql = $@"
                SELECT DISTINCT
                    ? AS ""company"",
                    IFNULL(NULLIF(a.""City"", ''), ?) AS ""Branch"",
                    c.""LicTradNum"" AS ""PAN""
                FROM ""{settings.Schema}"".""OCRD"" c
                LEFT JOIN ""{settings.Schema}"".""CRD1"" a
                    ON a.""CardCode"" = c.""CardCode""
                WHERE IFNULL(c.""LicTradNum"", '') <> ''
                  AND (
                        UPPER(IFNULL(a.""City"", '')) LIKE '%' || ? || '%'
                     OR UPPER(IFNULL(a.""State"", '')) = ?
                  )
                ORDER BY ""PAN""";

            return await QueryHanaWithFallbackAsync<GetPanByBranch>(
                company,
                "PAN by branch lookup",
                primarySql,
                parameters,
                fallbackSql,
                fallbackParameters);
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
