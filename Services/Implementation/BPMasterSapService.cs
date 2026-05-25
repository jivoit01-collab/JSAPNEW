using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sap.Data.Hana;

namespace JSAPNEW.Services.Implementation
{
    public class BPMasterSapService : IBPMasterSapService
    {
        private readonly IConfiguration _configuration;
        private readonly IBom2Service _bom2Service;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BPMasterSapService> _logger;
        private readonly string _sapBaseUrl;
        private readonly string _connectionString;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;

        public BPMasterSapService(
            IConfiguration configuration,
            IBom2Service bom2Service,
            IWebHostEnvironment environment,
            ILogger<BPMasterSapService> logger)
        {
            _configuration = configuration;
            _bom2Service = bom2Service;
            _environment = environment;
            _logger = logger;
            _sapBaseUrl = configuration["SapServiceLayer:BaseUrl"]
                ?? throw new ArgumentNullException("SapServiceLayer:BaseUrl not found in configuration.");
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("DefaultConnection not found in configuration.");
            var activeEnv = configuration["ActiveEnvironment"];
            _hanaSettings = configuration.GetSection($"HanaSettings:{activeEnv}")
                .Get<Dictionary<int, HanaCompanySettings>>() ?? new Dictionary<int, HanaCompanySettings>();
        }

        public async Task<BpSapPostResult> PostBusinessPartnerAsync(BpSapPostRequest request, CancellationToken cancellationToken = default)
        {
            if (request.BpData?.Master == null)
                return new BpSapPostResult { Success = false, Message = "BP master data was not found for SAP posting." };

            var session = await GetSessionAsync(request.Company);
            var cardType = IsVendor(request.BpType) ? "cSupplier" : "cCustomer";
            var bpType = cardType == "cSupplier" ? "V" : "C";
            var accountResolution = await ResolveControlAccountAsync(request.Company, bpType, cancellationToken);
            if (!accountResolution.Success)
            {
                _logger.LogWarning(
                    "BP control account resolution failed. FlowId={FlowId}, BpCode={BpCode}, Company={Company}, BpType={BpType}, ErrorCode={ErrorCode}, Message={Message}",
                    request.FlowId,
                    request.BpCode,
                    request.Company,
                    bpType,
                    accountResolution.ErrorCode,
                    accountResolution.Message);

                return new BpSapPostResult
                {
                    Success = false,
                    Message = accountResolution.Message,
                    ErrorCode = accountResolution.ErrorCode,
                    CardCode = string.Empty,
                    CardType = cardType
                };
            }

            var accountValidation = await ValidateControlAccountAsync(request.Company, accountResolution.AccountCode, bpType, cancellationToken);
            _logger.LogInformation(
                "BP control account selected. FlowId={FlowId}, BpCode={BpCode}, Company={Company}, BpType={BpType}, AccountCode={AccountCode}, AccountName={AccountName}, Source={Source}, IsValid={IsValid}, GroupMask={GroupMask}, Postable={Postable}",
                request.FlowId,
                request.BpCode,
                request.Company,
                bpType,
                accountResolution.AccountCode,
                accountValidation.AccountName,
                accountResolution.Source,
                accountValidation.IsValid,
                accountValidation.GroupMask,
                accountValidation.Postable);

            if (!accountValidation.IsValid)
            {
                _logger.LogWarning(
                    "BP control account pre-validation failed; continuing to SAP Service Layer so SAP returns the authoritative error. FlowId={FlowId}, BpCode={BpCode}, Company={Company}, BpType={BpType}, AccountCode={AccountCode}, ErrorCode={ErrorCode}, Message={Message}",
                    request.FlowId,
                    request.BpCode,
                    request.Company,
                    bpType,
                    accountResolution.AccountCode,
                    accountValidation.ErrorCode,
                    accountValidation.Message);
            }

            var bankValidation = await ValidateVendorBankDetailsAsync(request.Company, request.BpData, cardType, cancellationToken);
            if (!bankValidation.IsValid)
            {
                _logger.LogWarning(
                    "BP vendor bank validation failed. FlowId={FlowId}, BpCode={BpCode}, Company={Company}, ErrorCode={ErrorCode}, Message={Message}",
                    request.FlowId,
                    request.BpCode,
                    request.Company,
                    bankValidation.ErrorCode,
                    bankValidation.Message);

                return new BpSapPostResult
                {
                    Success = false,
                    Message = bankValidation.Message,
                    ErrorCode = bankValidation.ErrorCode,
                    SapError = bankValidation.SapError ?? new BpSapErrorInfo
                    {
                        code = null,
                        message = bankValidation.Message
                    },
                    CardCode = string.Empty,
                    CardType = cardType,
                    RawResponse = bankValidation.RawResponse
                };
            }

            var prefix = ResolveCardCodePrefix(request, cardType);
            var cardCode = await GetNextCardCodeAsync(prefix, cardType, session, cancellationToken);
            var warnings = new List<string>();
            var attachmentEntry = await UploadAttachmentsAsync(request.BpData, session, warnings, cancellationToken);
            var controlAccountCode = accountValidation.IsValid
                ? accountValidation.AccountCode
                : accountResolution.AccountCode;
            var payload = BuildBusinessPartnerPayload(request, cardCode, cardType, controlAccountCode, attachmentEntry, bankValidation.BankCodeByInput, warnings);
            var payloadJson = payload.ToString(Formatting.None);
            var payloadHash = ComputeHash(payloadJson);

            _logger.LogInformation(
                "Posting BP to SAP. FlowId={FlowId}, BpCode={BpCode}, Company={Company}, UserId={UserId}, CardType={CardType}, CandidateCardCode={CandidateCardCode}, ControlAccount={ControlAccount}, AttachmentEntry={AttachmentEntry}, PayloadHash={PayloadHash}",
                request.FlowId,
                request.BpCode,
                request.Company,
                request.UserId,
                cardType,
                cardCode,
                controlAccountCode,
                attachmentEntry,
                payloadHash);

            _logger.LogDebug(
                "BP SAP payload. FlowId={FlowId}, BpCode={BpCode}, PayloadHash={PayloadHash}, Payload={Payload}",
                request.FlowId,
                request.BpCode,
                payloadHash,
                payloadJson);

            var response = await SendSapRequestAsync(HttpMethod.Post, "BusinessPartners", session, payloadJson, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var sapError = MapSapError(errorBody);

                _logger.LogWarning(
                    "SAP BP POST FAILED FlowId={FlowId} BpCode={BpCode} HttpStatus={HttpStatus} SapCode={SapCode} SapMessage={SapMessage} CandidateCardCode={CandidateCardCode} PayloadHash={PayloadHash} RawResponse={RawResponse}",
                    request.FlowId,
                    request.BpCode,
                    (int)response.StatusCode,
                    sapError.SapCode,
                    sapError.Message,
                    cardCode,
                    payloadHash,
                    sapError.RawResponse);

                return new BpSapPostResult
                {
                    Success = false,
                    Message = sapError.Message,
                    ErrorCode = sapError.ErrorCode,
                    SapError = sapError.ToResponseInfo(),
                    CardCode = string.Empty,
                    AttachmentEntry = attachmentEntry,
                    Payload = payload,
                    PayloadHash = payloadHash,
                    CardType = cardType,
                    RawResponse = errorBody
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var confirmedCardCode = ExtractConfirmedCardCode(responseBody);
            if (string.IsNullOrWhiteSpace(confirmedCardCode))
            {
                return new BpSapPostResult
                {
                    Success = false,
                    Message = "SAP BP creation response did not include confirmed CardCode.",
                    ErrorCode = "SAP_MISSING_CONFIRMED_CARD_CODE",
                    SapError = new BpSapErrorInfo
                    {
                        code = null,
                        message = "SAP BP creation response did not include confirmed CardCode."
                    },
                    CardCode = string.Empty,
                    AttachmentEntry = attachmentEntry,
                    Payload = payload,
                    PayloadHash = payloadHash,
                    CardType = cardType,
                    RawResponse = responseBody
                };
            }

            var message = $"SAP Business Partner created as {confirmedCardCode}.";

            if (warnings.Count > 0)
                message += " Warnings: " + string.Join(" ", warnings);

            return new BpSapPostResult
            {
                Success = true,
                Message = message,
                CardCode = confirmedCardCode,
                AttachmentEntry = attachmentEntry,
                Payload = payload,
                PayloadHash = payloadHash,
                CardType = cardType,
                RawResponse = responseBody
            };
        }

        private async Task<SAPSessionModel> GetSessionAsync(int company)
        {
            return company switch
            {
                1 => await _bom2Service.GetSAPSessionOilAsync(),
                2 => await _bom2Service.GetSAPSessionBevAsync(),
                3 => await _bom2Service.GetSAPSessionMartAsync(),
                _ => throw new InvalidOperationException($"Unsupported SAP company id: {company}")
            };
        }

        private HanaCompanySettings GetHanaSettings(int company)
        {
            if (_hanaSettings == null || !_hanaSettings.TryGetValue(company, out var settings) || settings == null)
                throw new InvalidOperationException($"Invalid SAP HANA company ID: {company}");

            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
                throw new InvalidOperationException($"SAP HANA connection string is missing for company ID: {company}");

            if (string.IsNullOrWhiteSpace(settings.Schema))
                throw new InvalidOperationException($"SAP HANA schema is missing for company ID: {company}");

            return settings;
        }

        private async Task<BpControlAccountResolution> ResolveControlAccountAsync(int companyId, string bpType, CancellationToken cancellationToken)
        {
            var normalizedBpType = NormalizeControlAccountBpType(bpType);
            var label = GetBpTypeLabel(normalizedBpType);

            var configRow = await GetControlAccountConfigRowAsync(companyId, normalizedBpType, cancellationToken);
            if (!string.IsNullOrWhiteSpace(configRow?.AccountCode))
            {
                return BpControlAccountResolution.Ok(
                    configRow.AccountCode,
                    configRow.AccountName,
                    "BP.jsSAPAccountConfig");
            }

            var fallbackAccount = GetFallbackControlAccount(companyId, normalizedBpType);
            if (!string.IsNullOrWhiteSpace(fallbackAccount))
            {
                return BpControlAccountResolution.Ok(
                    fallbackAccount,
                    string.Empty,
                    "appsettings:BPControlAccounts");
            }

            return BpControlAccountResolution.Fail(
                $"{label} control account configuration missing.",
                "CONTROL_ACCOUNT_NOT_CONFIGURED");
        }

        private async Task<BpControlAccountConfigRow?> GetControlAccountConfigRowAsync(int companyId, string bpType, CancellationToken cancellationToken)
        {
            const string sql = @"
IF OBJECT_ID(N'BP.jsSAPAccountConfig', N'U') IS NULL
BEGIN
    SELECT TOP 0
        CAST(NULL AS NVARCHAR(50)) AS AccountCode,
        CAST(NULL AS NVARCHAR(200)) AS AccountName;
    RETURN;
END;

SELECT TOP 1
    accountCode AS AccountCode,
    accountName AS AccountName
FROM BP.jsSAPAccountConfig
WHERE companyId = @CompanyId
  AND bpType = @BpType
  AND isActive = 1
ORDER BY id DESC;";

            try
            {
                using var connection = new SqlConnection(_connectionString);
                return await connection.QueryFirstOrDefaultAsync<BpControlAccountConfigRow>(
                    new CommandDefinition(
                        sql,
                        new { CompanyId = companyId, BpType = bpType },
                        cancellationToken: cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "BP control account SQL config lookup failed. Company={Company}, BpType={BpType}. Falling back to appsettings.",
                    companyId,
                    bpType);
                return null;
            }
        }

        private string GetFallbackControlAccount(int companyId, string bpType)
        {
            var key = bpType == "V" ? "Vendor" : "Customer";
            return FirstText(
                _configuration[$"BPControlAccounts:{companyId}:{key}"],
                _configuration[$"BPControlAccounts:{key}"]);
        }

        public async Task<BpControlAccountValidationResult> ValidateControlAccountAsync(
            int companyId,
            string accountCode,
            string bpType,
            CancellationToken cancellationToken = default)
        {
            var normalizedBpType = NormalizeControlAccountBpType(bpType);
            var label = GetBpTypeLabel(normalizedBpType);
            var expectedGroupMask = normalizedBpType == "V" ? 2 : 1;
            var invalidGroupCode = normalizedBpType == "V"
                ? "INVALID_VENDOR_CONTROL_ACCOUNT"
                : "INVALID_CUSTOMER_CONTROL_ACCOUNT";
            var invalidGroupMessage = normalizedBpType == "V"
                ? "Configured Vendor control account is not a valid liability account."
                : "Configured Customer control account is not a valid receivable account.";

            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return ControlAccountInvalid(
                    companyId,
                    normalizedBpType,
                    accountCode,
                    $"{label} control account configuration missing.",
                    "CONTROL_ACCOUNT_NOT_CONFIGURED");
            }

            try
            {
                var settings = GetHanaSettings(companyId);
                var sql = $@"
SELECT
    ""AcctCode"" AS ""AccountCode"",
    ""AcctName"" AS ""AccountName"",
    ""Postable"" AS ""Postable"",
    ""GroupMask"" AS ""GroupMask""
FROM ""{settings.Schema}"".""OACT""
WHERE ""AcctCode"" = ?
LIMIT 1";

                using var connection = new HanaConnection(settings.ConnectionString);
                var parameters = new DynamicParameters();
                parameters.Add("AccountCode", accountCode.Trim());

                var account = await connection.QueryFirstOrDefaultAsync<BpSapOactAccountRow>(
                    new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

                if (account == null || string.IsNullOrWhiteSpace(account.AccountCode))
                {
                    return ControlAccountInvalid(
                        companyId,
                        normalizedBpType,
                        accountCode,
                        $"Configured {label} control account was not found in SAP OACT.",
                        "CONTROL_ACCOUNT_NOT_FOUND_IN_SAP");
                }

                var postable = (account.Postable ?? string.Empty).Trim().ToUpperInvariant();
                if (postable != "Y")
                {
                    return ControlAccountInvalid(
                        companyId,
                        normalizedBpType,
                        account.AccountCode,
                        $"Configured {label} control account is not postable in SAP.",
                        "CONTROL_ACCOUNT_NOT_POSTABLE",
                        account);
                }

                if (account.GroupMask != expectedGroupMask)
                {
                    return ControlAccountInvalid(
                        companyId,
                        normalizedBpType,
                        account.AccountCode,
                        invalidGroupMessage,
                        invalidGroupCode,
                        account);
                }

                var result = new BpControlAccountValidationResult
                {
                    IsValid = true,
                    CompanyId = companyId,
                    BpType = normalizedBpType,
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    Postable = postable,
                    GroupMask = account.GroupMask,
                    Message = $"{label} control account is valid."
                };

                _logger.LogInformation(
                    "BP control account validation succeeded. Company={Company}, BpType={BpType}, AccountCode={AccountCode}, AccountName={AccountName}, GroupMask={GroupMask}, Postable={Postable}",
                    companyId,
                    normalizedBpType,
                    result.AccountCode,
                    result.AccountName,
                    result.GroupMask,
                    result.Postable);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "BP control account validation failed. Company={Company}, BpType={BpType}, AccountCode={AccountCode}",
                    companyId,
                    normalizedBpType,
                    accountCode);

                return ControlAccountInvalid(
                    companyId,
                    normalizedBpType,
                    accountCode,
                    $"{label} control account validation failed.",
                    "CONTROL_ACCOUNT_VALIDATION_FAILED");
            }
        }

        private BpControlAccountValidationResult ControlAccountInvalid(
            int companyId,
            string bpType,
            string accountCode,
            string message,
            string errorCode,
            BpSapOactAccountRow? account = null)
        {
            var result = new BpControlAccountValidationResult
            {
                IsValid = false,
                CompanyId = companyId,
                BpType = bpType,
                AccountCode = account?.AccountCode ?? accountCode,
                AccountName = account?.AccountName ?? string.Empty,
                Postable = account?.Postable ?? string.Empty,
                GroupMask = account?.GroupMask,
                Message = message,
                ErrorCode = errorCode
            };

            _logger.LogWarning(
                "BP control account validation failed. Company={Company}, BpType={BpType}, AccountCode={AccountCode}, AccountName={AccountName}, GroupMask={GroupMask}, Postable={Postable}, ErrorCode={ErrorCode}, Message={Message}",
                companyId,
                bpType,
                result.AccountCode,
                result.AccountName,
                result.GroupMask,
                result.Postable,
                errorCode,
                message);

            return result;
        }

        private async Task<BpBankValidationResult> ValidateVendorBankDetailsAsync(
            int companyId,
            SingleBPDataModel bp,
            string cardType,
            CancellationToken cancellationToken)
        {
            if (!string.Equals(cardType, "cSupplier", StringComparison.OrdinalIgnoreCase))
                return BpBankValidationResult.Ok();

            var banks = (bp.BankDetails ?? new List<BP_Bank>())
                .Where(b => !string.IsNullOrWhiteSpace(b.AccountNumber))
                .ToList();

            if (banks.Count == 0)
            {
                return BpBankValidationResult.Fail(
                    "At least one vendor bank account is required before SAP posting.",
                    "VENDOR_BANK_ACCOUNT_REQUIRED");
            }

            var resolvedBanks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var bank in banks)
            {
                var inputBank = (bank.BankName ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(inputBank))
                {
                    return BpBankValidationResult.Fail(
                        "Vendor bank name/code is required before SAP posting.",
                        "VENDOR_BANK_REQUIRED");
                }

                var accountNumber = (bank.AccountNumber ?? string.Empty).Trim();
                if (accountNumber.Length < 4 || accountNumber.Length > 30)
                {
                    return BpBankValidationResult.Fail(
                        "Vendor bank account number must be between 4 and 30 characters.",
                        "INVALID_BANK_ACCOUNT_NUMBER");
                }

                var ifsc = (bank.IfscCode ?? string.Empty).Trim().ToUpperInvariant();
                if (!Regex.IsMatch(ifsc, "^[A-Z]{4}0[A-Z0-9]{6}$"))
                {
                    return BpBankValidationResult.Fail(
                        "Vendor IFSC code is invalid.",
                        "INVALID_IFSC_CODE");
                }

                BpSapBankRow? sapBank;
                try
                {
                    sapBank = await ResolveSapBankAsync(companyId, inputBank, cancellationToken);
                }
                catch (Exception ex)
                {
                    var exactMessage = NormalizeSingleLine(ex.GetBaseException()?.Message ?? ex.Message);
                    _logger.LogWarning(
                        ex,
                        "SAP bank master pre-validation failed; continuing to SAP Service Layer so SAP returns the authoritative error. Company={Company}, InputBank={InputBank}, SapLookupError={SapLookupError}",
                        companyId,
                        inputBank,
                        exactMessage);

                    resolvedBanks[inputBank] = inputBank;
                    continue;
                }

                if (sapBank == null || string.IsNullOrWhiteSpace(sapBank.BankCode))
                {
                    _logger.LogWarning(
                        "SAP bank master pre-validation found no ODSC match; continuing to SAP Service Layer so SAP returns the authoritative error. Company={Company}, InputBank={InputBank}",
                        companyId,
                        inputBank);

                    resolvedBanks[inputBank] = inputBank;
                    continue;
                }

                resolvedBanks[inputBank] = sapBank.BankCode.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(bank.BankName))
                    resolvedBanks[bank.BankName.Trim()] = sapBank.BankCode.Trim().ToUpperInvariant();
                _logger.LogInformation(
                    "BP vendor bank validation succeeded. Company={Company}, InputBank={InputBank}, SapBankCode={SapBankCode}, SapBankName={SapBankName}, AccountNo={AccountNo}",
                    companyId,
                    inputBank,
                    sapBank.BankCode,
                    sapBank.BankName,
                    accountNumber);
            }

            return BpBankValidationResult.Ok(resolvedBanks);
        }

        private async Task<BpSapBankRow?> ResolveSapBankAsync(
            int companyId,
            string bankNameOrCode,
            CancellationToken cancellationToken)
        {
            var settings = GetHanaSettings(companyId);
            var lookup = bankNameOrCode.Trim().ToUpperInvariant();
            var sql = $@"
SELECT
    ""BankCode"" AS ""BankCode"",
    ""BankName"" AS ""BankName""
FROM ""{settings.Schema}"".""ODSC""
WHERE UPPER(""BankCode"") = ?
   OR UPPER(""BankName"") = ?
LIMIT 1";

            try
            {
                using var connection = new HanaConnection(settings.ConnectionString);
                var parameters = new DynamicParameters();
                parameters.Add("BankCode", lookup);
                parameters.Add("BankName", lookup);

                return await connection.QueryFirstOrDefaultAsync<BpSapBankRow>(
                    new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SAP bank lookup failed. Company={Company}, BankNameOrCode={BankNameOrCode}",
                    companyId,
                    bankNameOrCode);
                throw;
            }
        }

        private async Task<string> GetNextCardCodeAsync(string prefix, string cardType, SAPSessionModel session, CancellationToken cancellationToken)
        {
            var safePrefix = prefix.Replace("'", "''").Trim();
            var filter = $"startswith(CardCode,'{safePrefix}') and CardType eq '{cardType}'";
            var endpoint = $"BusinessPartners?$filter={Uri.EscapeDataString(filter)}&$select=CardCode&$orderby=CardCode desc&$top=1";
            var response = await SendSapRequestAsync(HttpMethod.Get, endpoint, session, null, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new BpSapException("Unable to generate SAP CardCode", MapSapError(body));

            var json = JObject.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var lastCode = json["value"]?.FirstOrDefault()?["CardCode"]?.ToString();
            if (string.IsNullOrWhiteSpace(lastCode) || !lastCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return prefix + "000001";

            var suffix = lastCode[prefix.Length..];
            var digits = Regex.Match(suffix, "\\d+").Value;
            if (!int.TryParse(digits, out var current))
                return prefix + "000001";

            var width = Math.Max(digits.Length, 6);
            return prefix + (current + 1).ToString().PadLeft(width, '0');
        }

        private JObject BuildBusinessPartnerPayload(
            BpSapPostRequest request,
            string cardCode,
            string cardType,
            string controlAccountCode,
            int? attachmentEntry,
            IReadOnlyDictionary<string, string> bankCodeByInput,
            List<string> warnings)
        {
            var bp = request.BpData;
            var master = bp.Master;
            var tax = bp.TaxDetails;
            var isVendor = cardType == "cSupplier";

            var payload = new JObject
            {
                ["CardCode"] = cardCode,
                ["CardName"] = master.Name,
                ["CardType"] = cardType,
                ["Currency"] = string.IsNullOrWhiteSpace(master.Currency) ? "INR" : master.Currency.Trim().ToUpperInvariant(),
                ["DebitorAccount"] = controlAccountCode.Trim()
            };

            var sapData = request.SapData;
            if (int.TryParse(sapData?.grpCode, out var groupCode) && groupCode > 0)
                payload["GroupCode"] = groupCode;
            if (!string.IsNullOrWhiteSpace(master.ForeignName))
                payload["CardForeignName"] = master.ForeignName.Trim();
            if (!string.IsNullOrWhiteSpace(master.MobileNumber))
                payload["Phone1"] = SanitizeMobile(master.MobileNumber);
            if (!string.IsNullOrWhiteSpace(master.EmailAddress))
                payload["EmailAddress"] = master.EmailAddress.Trim();
            if (!string.IsNullOrWhiteSpace(master.Remarks))
                payload["Notes"] = master.Remarks.Trim();
            if (attachmentEntry.HasValue)
                payload["AttachmentEntry"] = attachmentEntry.Value;
            if (!string.IsNullOrWhiteSpace(tax?.FssaiLicense))
                payload["U_Fssai"] = tax.FssaiLicense.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(tax?.Msme))
                payload["U_MSME"] = tax.Msme.Trim().ToUpperInvariant();

            var contacts = BuildContacts(bp);
            if (contacts.Count > 0)
                payload["ContactEmployees"] = contacts;

            var addresses = BuildAddresses(bp, warnings);
            if (addresses.Count > 0)
                payload["BPAddresses"] = addresses;

            var fiscalTax = BuildFiscalTax(bp, addresses);
            if (fiscalTax.Count > 0)
                payload["BPFiscalTaxIDCollection"] = fiscalTax;

            if (isVendor)
            {
                var banks = BuildBankAccounts(bp, bankCodeByInput, warnings);
                if (banks.Count > 0)
                    payload["BPBankAccounts"] = banks;
            }

            return payload;
        }

        private JArray BuildContacts(SingleBPDataModel bp)
        {
            var result = new JArray();
            foreach (var contact in bp.ContactPersons ?? new List<BP_Contact>())
            {
                var name = $"{contact.FirstName} {contact.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var obj = new JObject
                {
                    ["Name"] = Truncate(name, 50),
                    ["Active"] = "tYES"
                };

                AddIfNotBlank(obj, "FirstName", contact.FirstName, 50);
                AddIfNotBlank(obj, "LastName", contact.LastName, 50);
                AddIfNotBlank(obj, "Position", contact.Designation, 50);
                AddIfNotBlank(obj, "MobilePhone", SanitizeMobile(contact.MobileNumber), 20);
                AddIfNotBlank(obj, "Phone1", SanitizeMobile(contact.AlternateContact), 20);
                AddIfNotBlank(obj, "E_Mail", contact.EmailAddress, 100);

                result.Add(obj);
            }

            return result;
        }

        private JArray BuildAddresses(SingleBPDataModel bp, List<string> warnings)
        {
            var result = new JArray();
            var billTo = bp.BillingAddresses ?? new List<BP_Address>();
            var shipTo = bp.ShippingAddresses ?? new List<BP_Address>();

            foreach (var address in billTo)
                result.Add(BuildAddress(address, "bo_BillTo", bp.Master.Name));

            if (shipTo.Count == 0 && billTo.Count > 0)
                shipTo = billTo;

            foreach (var address in shipTo)
                result.Add(BuildAddress(address, "bo_ShipTo", bp.Master.Name));

            if (result.Count == 0)
                warnings.Add("No BP address rows were available for SAP payload.");

            return result;
        }

        private JObject BuildAddress(BP_Address address, string addressType, string cardName)
        {
            var addressName = !string.IsNullOrWhiteSpace(address.AddressName)
                ? address.AddressName
                : $"{Truncate(cardName, 25)}-{MapStateCode(address.State)}";

            var obj = new JObject
            {
                ["AddressName"] = Truncate(addressName, 50),
                ["AddressType"] = addressType,
                ["Country"] = MapCountry(address.Country)
            };

            AddIfNotBlank(obj, "Street", address.Street, 100);
            AddIfNotBlank(obj, "Block", address.BlockArea, 100);
            AddIfNotBlank(obj, "City", address.City, 100);
            AddIfNotBlank(obj, "ZipCode", address.PinCode, 20);
            AddIfNotBlank(obj, "State", MapStateCode(address.State), 10);

            var gstin = (address.Gstin ?? string.Empty).Trim().ToUpperInvariant();
            if (Regex.IsMatch(gstin, "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$"))
            {
                obj["GSTIN"] = gstin;
                obj["GstType"] = "gstRegularTDSISD";
            }

            return obj;
        }

        private JArray BuildFiscalTax(SingleBPDataModel bp, JArray addresses)
        {
            var result = new JArray();
            var pan = (bp.TaxDetails?.PanNumber ?? string.Empty).Trim().ToUpperInvariant();
            if (!Regex.IsMatch(pan, "^[A-Z]{5}[0-9]{4}[A-Z]$"))
                return result;

            var firstBillTo = addresses
                .OfType<JObject>()
                .FirstOrDefault(a => string.Equals(a["AddressType"]?.ToString(), "bo_BillTo", StringComparison.OrdinalIgnoreCase));
            var addressName = firstBillTo?["AddressName"]?.ToString();
            if (string.IsNullOrWhiteSpace(addressName))
                return result;

            result.Add(new JObject
            {
                ["Address"] = addressName,
                ["AddrType"] = "bo_BillTo",
                ["TaxId0"] = pan
            });

            return result;
        }

        private JArray BuildBankAccounts(
            SingleBPDataModel bp,
            IReadOnlyDictionary<string, string> bankCodeByInput,
            List<string> warnings)
        {
            var result = new JArray();
            foreach (var bank in bp.BankDetails ?? new List<BP_Bank>())
            {
                if (string.IsNullOrWhiteSpace(bank.AccountNumber))
                    continue;

                var inputBank = (bank.BankName ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(inputBank))
                {
                    warnings.Add($"Bank account {bank.AccountNumber} was skipped because bank name/code is missing.");
                    continue;
                }

                if (!bankCodeByInput.TryGetValue(inputBank, out var bankCode) || string.IsNullOrWhiteSpace(bankCode))
                {
                    warnings.Add($"Bank account {bank.AccountNumber} was skipped because bank {inputBank} was not validated in SAP.");
                    continue;
                }

                var ifsc = (bank.IfscCode ?? string.Empty).Trim().ToUpperInvariant();
                var obj = new JObject
                {
                    ["BankCode"] = bankCode.Trim().ToUpperInvariant(),
                    ["AccountNo"] = bank.AccountNumber.Trim(),
                    ["AccountName"] = Truncate(bp.Master.Name, 100)
                };

                AddIfNotBlank(obj, "Branch", bank.BranchName, 50);
                AddIfNotBlank(obj, "BICSwiftCode", ifsc, 50);
                AddIfNotBlank(obj, "UserNo1", ifsc, 50);
                AddIfNotBlank(obj, "IBAN", bank.SwiftCode, 34);

                result.Add(obj);
            }

            return result;
        }

        private async Task<int?> UploadAttachmentsAsync(SingleBPDataModel bp, SAPSessionModel session, List<string> warnings, CancellationToken cancellationToken)
        {
            var attachments = (bp.Attachments ?? new List<BP_Attachment>())
                .Where(a => !string.IsNullOrWhiteSpace(a.FileName) && !string.IsNullOrWhiteSpace(a.FilePath))
                .ToList();

            if (attachments.Count == 0)
                return null;

            var lines = new JArray();
            foreach (var attachment in attachments)
            {
                var sourcePath = ResolveSapAttachmentSourcePath(attachment);
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    warnings.Add($"Attachment {attachment.FileName} was skipped because the SAP source path could not be resolved.");
                    continue;
                }

                var extension = Path.GetExtension(attachment.FileName).TrimStart('.').ToLowerInvariant();
                var stem = Path.GetFileNameWithoutExtension(attachment.FileName);
                if (string.IsNullOrWhiteSpace(extension) || string.IsNullOrWhiteSpace(stem))
                    continue;

                lines.Add(new JObject
                {
                    ["FileName"] = stem,
                    ["FileExtension"] = extension,
                    ["SourcePath"] = sourcePath,
                    ["UserID"] = "1",
                    ["Override"] = "tYES"
                });
            }

            if (lines.Count == 0)
                return null;

            var payload = new JObject { ["Attachments2_Lines"] = lines };
            var response = await SendSapRequestAsync(HttpMethod.Post, "Attachments2", session, payload.ToString(Formatting.None), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var sapError = MapSapError(body);
                _logger.LogWarning(
                    "SAP ATTACHMENT POST FAILED HttpStatus={HttpStatus} SapCode={SapCode} SapMessage={SapMessage} RawResponse={RawResponse}",
                    (int)response.StatusCode,
                    sapError.SapCode,
                    sapError.Message,
                    sapError.RawResponse);

                throw new BpSapException("SAP Attachments2 upload failed", sapError);
            }

            var json = JObject.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            return json["AbsoluteEntry"]?.Value<int?>();
        }

        private string ResolveSapAttachmentSourcePath(BP_Attachment attachment)
        {
            var configuredPath = _configuration["SapServiceLayer:AttachmentSourcePath"]
                ?? Environment.GetEnvironmentVariable("SAP_ATTACHMENT_PATH");

            if (!string.IsNullOrWhiteSpace(configuredPath))
                return configuredPath.TrimEnd('\\', '/');

            var relativePath = attachment.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString())
                .TrimStart(Path.DirectorySeparatorChar);

            if (relativePath.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(_environment.ContentRootPath, relativePath);

            return Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), relativePath);
        }

        private async Task<HttpResponseMessage> SendSapRequestAsync(HttpMethod method, string endpoint, SAPSessionModel session, string? body, CancellationToken cancellationToken)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri(_sapBaseUrl.EndsWith("/") ? _sapBaseUrl : _sapBaseUrl + "/");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Cookie", $"{session.B1Session}; {session.RouteId}");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var request = new HttpRequestMessage(method, endpoint);
            if (body != null)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            return await client.SendAsync(request, cancellationToken);
        }

        private static string ResolveCardCodePrefix(BpSapPostRequest request, string cardType)
        {
            if (!string.IsNullOrWhiteSpace(request.CardCodePrefix))
                return request.CardCodePrefix.Trim().ToUpperInvariant();

            var series = request.SapData?.series;
            if (!string.IsNullOrWhiteSpace(series) && !series.All(char.IsDigit))
                return series.Trim().ToUpperInvariant();

            return cardType == "cSupplier" ? "VENDA" : "CUSTA";
        }

        private static bool IsVendor(string bpType)
        {
            return string.Equals(bpType, "V", StringComparison.OrdinalIgnoreCase)
                || string.Equals(bpType, "Vendor", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeControlAccountBpType(string bpType)
        {
            var value = (bpType ?? string.Empty).Trim().ToUpperInvariant();
            return value is "V" or "S" or "SUPPLIER" or "VENDOR" ? "V" : "C";
        }

        private static string GetBpTypeLabel(string bpType)
        {
            return NormalizeControlAccountBpType(bpType) == "V" ? "Vendor" : "Customer";
        }

        private static string FirstText(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
        }

        private static bool IsBillTo(BP_Address address)
        {
            var type = address.AddressType ?? string.Empty;
            return type.StartsWith("B", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Bill", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsShipTo(BP_Address address)
        {
            var type = address.AddressType ?? string.Empty;
            return type.StartsWith("S", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Ship", StringComparison.OrdinalIgnoreCase);
        }

        private static string MapCountry(string? country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return "IN";

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["India"] = "IN",
                ["United States"] = "US",
                ["United Kingdom"] = "GB",
                ["UAE"] = "AE",
                ["Singapore"] = "SG",
                ["Germany"] = "DE",
                ["Japan"] = "JP",
                ["Australia"] = "AU"
            };

            var trimmed = country.Trim();
            if (map.TryGetValue(trimmed, out var code))
                return code;

            return trimmed.Length <= 3 ? trimmed.ToUpperInvariant() : "IN";
        }

        private static string MapStateCode(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return string.Empty;

            var sapCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "AN","AP","AR","AS","BH","CH","CT","DD","DL","DN","GA","GJ","HP",
                "HR","JH","JK","KA","KL","LA","LD","MH","MN","MP","MZ","NL","OR",
                "PB","PY","RJ","SK","TG","TN","TR","UP","UT","WB"
            };

            var upper = state.Trim().ToUpperInvariant();
            if (upper.Length <= 3 && sapCodes.Contains(upper))
                return upper;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Andaman and Nicobar Islands"] = "AN",
                ["Andaman & Nicobar Islands"] = "AN",
                ["Andhra Pradesh"] = "AP",
                ["Arunachal Pradesh"] = "AR",
                ["Assam"] = "AS",
                ["Bihar"] = "BH",
                ["Chandigarh"] = "CH",
                ["Chhattisgarh"] = "CT",
                ["Delhi"] = "DL",
                ["Goa"] = "GA",
                ["Gujarat"] = "GJ",
                ["Haryana"] = "HR",
                ["Himachal Pradesh"] = "HP",
                ["Jammu & Kashmir"] = "JK",
                ["Jammu and Kashmir"] = "JK",
                ["Jharkhand"] = "JH",
                ["Karnataka"] = "KA",
                ["Kerala"] = "KL",
                ["Ladakh"] = "LA",
                ["Madhya Pradesh"] = "MP",
                ["Maharashtra"] = "MH",
                ["Manipur"] = "MN",
                ["Meghalaya"] = "ME",
                ["Mizoram"] = "MZ",
                ["Nagaland"] = "NL",
                ["Odisha"] = "OR",
                ["Orissa"] = "OR",
                ["Punjab"] = "PB",
                ["Rajasthan"] = "RJ",
                ["Sikkim"] = "SK",
                ["Tamil Nadu"] = "TN",
                ["Telangana"] = "TG",
                ["Tripura"] = "TR",
                ["Uttar Pradesh"] = "UP",
                ["Uttarakhand"] = "UT",
                ["West Bengal"] = "WB"
            };

            return map.TryGetValue(state.Trim(), out var code) ? code : string.Empty;
        }

        private static string SanitizeMobile(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var digits = Regex.Replace(raw, "\\D", string.Empty);
            if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
                return digits[2..];
            if (digits.Length == 13 && digits.StartsWith("091", StringComparison.Ordinal))
                return digits[3..];
            return digits.Length > 10 ? digits[^10..] : digits;
        }

        private static string Truncate(string? value, int length)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= length ? value : value[..length];
        }

        private static void AddIfNotBlank(JObject obj, string propertyName, string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            obj[propertyName] = Truncate(value.Trim(), maxLength);
        }

        private static string ComputeHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }

        private static string ExtractConfirmedCardCode(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
                return string.Empty;

            try
            {
                var obj = JObject.Parse(responseJson);
                return obj["CardCode"]?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static BpSapError MapSapError(string responseJson)
        {
            return BpSapErrorMapper.Map(responseJson);
        }

        private static string NormalizeSingleLine(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim(), "\\s+", " ");
        }

        private sealed class BpControlAccountConfigRow
        {
            public string AccountCode { get; set; } = string.Empty;
            public string AccountName { get; set; } = string.Empty;
        }

        private sealed class BpSapOactAccountRow
        {
            public string AccountCode { get; set; } = string.Empty;
            public string AccountName { get; set; } = string.Empty;
            public string Postable { get; set; } = string.Empty;
            public int? GroupMask { get; set; }
        }

        private sealed class BpSapBankRow
        {
            public string BankCode { get; set; } = string.Empty;
            public string BankName { get; set; } = string.Empty;
            public string CountryCode { get; set; } = string.Empty;
            public string SwiftNo { get; set; } = string.Empty;
        }

        private sealed class BpBankValidationResult
        {
            public bool IsValid { get; private init; }
            public string Message { get; private init; } = string.Empty;
            public string ErrorCode { get; private init; } = string.Empty;
            public BpSapErrorInfo? SapError { get; private init; }
            public string RawResponse { get; private init; } = string.Empty;
            public IReadOnlyDictionary<string, string> BankCodeByInput { get; private init; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public static BpBankValidationResult Ok(
                IReadOnlyDictionary<string, string>? bankCodeByInput = null)
            {
                return new BpBankValidationResult
                {
                    IsValid = true,
                    BankCodeByInput = bankCodeByInput ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };
            }

            public static BpBankValidationResult Fail(
                string message,
                string errorCode,
                string? sapMessage = null,
                string? rawResponse = null)
            {
                return new BpBankValidationResult
                {
                    IsValid = false,
                    Message = message,
                    ErrorCode = errorCode,
                    SapError = new BpSapErrorInfo
                    {
                        code = null,
                        message = sapMessage ?? message
                    },
                    RawResponse = rawResponse ?? string.Empty
                };
            }
        }

        private sealed class BpControlAccountResolution
        {
            public bool Success { get; private init; }
            public string AccountCode { get; private init; } = string.Empty;
            public string AccountName { get; private init; } = string.Empty;
            public string Source { get; private init; } = string.Empty;
            public string Message { get; private init; } = string.Empty;
            public string ErrorCode { get; private init; } = string.Empty;

            public static BpControlAccountResolution Ok(string accountCode, string accountName, string source)
            {
                return new BpControlAccountResolution
                {
                    Success = true,
                    AccountCode = accountCode.Trim(),
                    AccountName = accountName,
                    Source = source
                };
            }

            public static BpControlAccountResolution Fail(string message, string errorCode)
            {
                return new BpControlAccountResolution
                {
                    Success = false,
                    Message = message,
                    ErrorCode = errorCode
                };
            }
        }
    }
}
