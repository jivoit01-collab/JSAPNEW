using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using Sap.Data.Hana;
using ServiceStack;
using JSAPNEW.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.Design;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace JSAPNEW.Services.Implementation
{
    public class BPmasterService : IBPmasterService
    {
        private readonly IConfiguration _configuration;
        private readonly IBPMasterSapService _bpMasterSapService;
        private readonly ILogger<BPmasterService> _logger;
        private readonly string _connectionString;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _approvalLocks = new();
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _sapPostLocks = new();

        public BPmasterService(
            IConfiguration configuration,
            IBPMasterSapService bpMasterSapService,
            ILogger<BPmasterService> logger)
        {
            _configuration = configuration;
            _bpMasterSapService = bpMasterSapService;
            _logger = logger;
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

        private static string? NormalizeMonthFilter(string? month)
        {
            if (string.IsNullOrWhiteSpace(month))
                return null;

            var value = month.Trim();

            if (Regex.IsMatch(value, @"^\d{2}-\d{4}$"))
                return value;

            if (Regex.IsMatch(value, @"^\d{4}-\d{2}$"))
                return $"{value.Substring(5, 2)}-{value.Substring(0, 4)}";

            if (Regex.IsMatch(value, @"^\d{1,2}$") && int.TryParse(value, out var monthNumber) && monthNumber >= 1 && monthNumber <= 12)
                return $"{monthNumber:00}-{DateTime.Now.Year}";

            return value;
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

        private static string ResolveBpType(InsertBPMasterDataModel model)
        {
            var type = (model.Type ?? string.Empty).Trim();
            if (type.Equals("Vendor", StringComparison.OrdinalIgnoreCase)
                || type.Equals("V", StringComparison.OrdinalIgnoreCase)
                || type.Equals("Supplier", StringComparison.OrdinalIgnoreCase))
            {
                return "V";
            }

            if (type.Equals("Customer", StringComparison.OrdinalIgnoreCase)
                || type.Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                return "C";
            }

            if (!string.IsNullOrWhiteSpace(model.VendorType))
                return "V";

            return NormalizeBpType(FirstText(model.CustomerType, model.VendorType, "C"));
        }

        private static int ResolveCompanyId(InsertBPMasterDataModel model)
        {
            if (model.CompanyId > 0)
                return model.CompanyId;

            var company = (model.Company ?? string.Empty).Trim();
            if (int.TryParse(company, out var numericCompany) && numericCompany > 0)
                return numericCompany;

            return company.ToUpperInvariant() switch
            {
                "JIVO_OIL_HANADB" or "JIVO OIL" or "OIL" => 1,
                "JIVO_BEVERAGES_HANADB" or "JIVO BEVERAGES" or "BEVERAGES" => 2,
                "JIVO_MART_HANADB" or "JIVO MART" or "MART" => 3,
                _ => 0
            };
        }

        private static string ResolveCompanyByUser(InsertBPMasterDataModel model)
        {
            return FirstText(model.CompanyByUser, model.Company);
        }

        private static string ResolveCompanyName(InsertBPMasterDataModel model)
        {
            return FirstText(model.CompanyName, model.CardName);
        }

        private static string ResolveForeignName(InsertBPMasterDataModel model)
        {
            return FirstText(model.ForeignName, model.ForeignTradeName);
        }

        private static string ResolveIndustry(InsertBPMasterDataModel model)
        {
            return FirstText(model.Industry, model.IndustrySector);
        }

        private static string ResolveDesignation(InsertBPMasterDataModel model)
        {
            return FirstText(model.Designation, model.Title, model.ContactTitle);
        }

        private static string ResolveFirstName(InsertBPMasterDataModel model)
        {
            return FirstText(model.FirstName, model.ContactFirst);
        }

        private static string ResolveLastName(InsertBPMasterDataModel model)
        {
            return FirstText(model.LastName, model.ContactLast);
        }

        private static string ResolveMobile(InsertBPMasterDataModel model)
        {
            return FirstText(model.MobileNumber, model.Mobile, model.ContactMobile);
        }

        private static string ResolveEmail(InsertBPMasterDataModel model)
        {
            return FirstText(model.EmailAddress, model.Email);
        }

        private static string ResolveAlternateEmail(InsertBPMasterDataModel model)
        {
            return FirstText(model.AlternateEmail, model.ContactEmail);
        }

        private static string ResolveAlternateContact(InsertBPMasterDataModel model)
        {
            return FirstText(model.AlternateContact, model.AltContact);
        }

        private static string ResolvePan(InsertBPMasterDataModel model)
        {
            return FirstText(model.PanNumber, model.Pan);
        }

        private static string ResolveFssai(InsertBPMasterDataModel model)
        {
            return FirstText(model.FssaiLicense, model.FssaiNo);
        }

        private static string ResolveMsme(InsertBPMasterDataModel model)
        {
            if (model.HasMsme)
                return FirstText(model.MsmeNo, model.Msme);

            return FirstText(model.Msme, model.MsmeNo);
        }

        private static string ResolveCurrency(InsertBPMasterDataModel model)
        {
            var currency = FirstText(model.Currency, "INR").Trim();
            return currency switch
            {
                "Indian Rupee" => "INR",
                "US Dollar" => "USD",
                "Euro" => "EUR",
                "British Pound" => "GBP",
                "UAE Dirham" => "AED",
                _ => currency.ToUpperInvariant()
            };
        }

        private static string SanitizeMobileForValidation(string mobile)
        {
            var digits = Regex.Replace(mobile ?? string.Empty, "\\D", string.Empty);
            if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
                return digits[2..];
            if (digits.Length == 13 && digits.StartsWith("091", StringComparison.Ordinal))
                return digits[3..];
            return digits.Length > 10 ? digits[^10..] : digits;
        }

        private static string GetGstStateCode(string gstin)
        {
            var normalized = (gstin ?? string.Empty).Trim().ToUpperInvariant();
            return normalized.Length >= 2 ? normalized[..2] : string.Empty;
        }

        private static string MapIndianStateToGstCode(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return string.Empty;

            var value = state.Trim().ToUpperInvariant();
            if (Regex.IsMatch(value, "^\\d{2}$"))
                return value;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Jammu & Kashmir"] = "01",
                ["Jammu and Kashmir"] = "01",
                ["Himachal Pradesh"] = "02",
                ["Punjab"] = "03",
                ["Chandigarh"] = "04",
                ["Uttarakhand"] = "05",
                ["Uttaranchal"] = "05",
                ["Haryana"] = "06",
                ["Delhi"] = "07",
                ["Rajasthan"] = "08",
                ["Uttar Pradesh"] = "09",
                ["Bihar"] = "10",
                ["Sikkim"] = "11",
                ["Arunachal Pradesh"] = "12",
                ["Nagaland"] = "13",
                ["Manipur"] = "14",
                ["Mizoram"] = "15",
                ["Tripura"] = "16",
                ["Meghalaya"] = "17",
                ["Assam"] = "18",
                ["West Bengal"] = "19",
                ["Jharkhand"] = "20",
                ["Odisha"] = "21",
                ["Orissa"] = "21",
                ["Chhattisgarh"] = "22",
                ["Madhya Pradesh"] = "23",
                ["Gujarat"] = "24",
                ["Dadra and Nagar Haveli and Daman and Diu"] = "26",
                ["Dadra & Nagar Haveli"] = "26",
                ["Daman & Diu"] = "26",
                ["Maharashtra"] = "27",
                ["Andhra Pradesh"] = "37",
                ["Karnataka"] = "29",
                ["Goa"] = "30",
                ["Lakshadweep"] = "31",
                ["Kerala"] = "32",
                ["Tamil Nadu"] = "33",
                ["Puducherry"] = "34",
                ["Pondicherry"] = "34",
                ["Andaman and Nicobar Islands"] = "35",
                ["Andaman & Nicobar Islands"] = "35",
                ["Telangana"] = "36",
                ["Ladakh"] = "38"
            };

            return map.TryGetValue(state.Trim(), out var code) ? code : string.Empty;
        }

        private static bool GstStateMatchesAddress(string gstin, BPMasterAddress address)
        {
            if (string.IsNullOrWhiteSpace(gstin))
                return true;

            var gstStateCode = GetGstStateCode(gstin);
            var addressStateCode = MapIndianStateToGstCode(address.State);
            return string.IsNullOrWhiteSpace(gstStateCode)
                || string.IsNullOrWhiteSpace(addressStateCode)
                || string.Equals(gstStateCode, addressStateCode, StringComparison.OrdinalIgnoreCase);
        }

        private static BPBankAccount? ResolvePrimaryBank(InsertBPMasterDataModel model)
        {
            var account = (model.BankAccounts ?? new List<BPBankAccount>())
                .Where(b => !string.IsNullOrWhiteSpace(b.AccNo))
                .OrderByDescending(b => b.IsPrimary)
                .FirstOrDefault();

            return account;
        }

        private static string ResolveBankName(BPBankAccount? bank)
        {
            return bank == null ? string.Empty : FirstText(bank.BankCode, bank.MgrBankCode, bank.BankName);
        }

        private static string ResolveBankBranch(BPBankAccount? bank)
        {
            return bank == null ? string.Empty : bank.Branch;
        }

        private static string ResolveBankAccountNo(BPBankAccount? bank)
        {
            return bank == null ? string.Empty : bank.AccNo;
        }

        private static string ResolveBankIfsc(BPBankAccount? bank)
        {
            return bank == null ? string.Empty : bank.Ifsc;
        }

        private static List<BPMasterAddress> BuildAddressRows(InsertBPMasterDataModel model)
        {
            var rows = new List<BPMasterAddress>();

            void AddRows(IEnumerable<BPMasterAddress>? source, string addressType)
            {
                foreach (var address in source ?? Enumerable.Empty<BPMasterAddress>())
                {
                    rows.Add(new BPMasterAddress
                    {
                        AddressType = FirstText(address.AddressType, addressType),
                        Street = address.Street,
                        BlockArea = FirstText(address.BlockArea, address.Block),
                        State = address.State,
                        City = address.City,
                        PinCode = FirstText(address.PinCode, address.Zip),
                        Country = address.Country,
                        Gstin = FirstText(address.Gstin, model.Gstin),
                        AddressName = FirstText(address.AddressName, address.AddrName)
                    });
                }
            }

            var billAddresses = model.BillingAddresses?.Count > 0
                ? model.BillingAddresses
                : model.AllBillAddresses;
            var shipAddresses = model.ShippingAddresses?.Count > 0
                ? model.ShippingAddresses
                : model.AllShipAddresses;

            if ((billAddresses == null || billAddresses.Count == 0)
                && !string.IsNullOrWhiteSpace(model.BillStreet))
            {
                billAddresses = new List<BPMasterAddress>
                {
                    new()
                    {
                        AddressName = model.BillAddressName,
                        Street = model.BillStreet,
                        BlockArea = model.BillBlock,
                        City = model.BillCity,
                        PinCode = model.BillZip,
                        State = model.BillState,
                        Country = FirstText(model.BillCountry, "India"),
                        Gstin = model.Gstin
                    }
                };
            }

            if ((shipAddresses == null || shipAddresses.Count == 0)
                && !model.SameAsBill
                && !string.IsNullOrWhiteSpace(model.ShipStreet))
            {
                shipAddresses = new List<BPMasterAddress>
                {
                    new()
                    {
                        AddressName = model.ShipAddressName,
                        Street = model.ShipStreet,
                        BlockArea = model.ShipBlock,
                        City = model.ShipCity,
                        PinCode = model.ShipZip,
                        State = model.ShipState,
                        Country = FirstText(model.ShipCountry, "India"),
                        Gstin = string.Empty
                    }
                };
            }

            AddRows(billAddresses, "B");

            if ((shipAddresses == null || shipAddresses.Count == 0) && model.SameAsBill && billAddresses?.Count > 0)
                shipAddresses = billAddresses;

            AddRows(shipAddresses, "S");
            return rows;
        }

        private static List<BPContactPerson> BuildContactRows(InsertBPMasterDataModel model)
        {
            var firstName = ResolveFirstName(model);
            var lastName = ResolveLastName(model);
            var email = ResolveEmail(model);

            if (string.IsNullOrWhiteSpace(firstName)
                && string.IsNullOrWhiteSpace(lastName)
                && string.IsNullOrWhiteSpace(ResolveMobile(model))
                && string.IsNullOrWhiteSpace(email))
            {
                return new List<BPContactPerson>();
            }

            return new List<BPContactPerson>
            {
                new()
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Designation = ResolveDesignation(model),
                    MobileNumber = ResolveMobile(model),
                    AlternateContact = ResolveAlternateContact(model),
                    EmailAddress = email,
                    AlternateEmail = ResolveAlternateEmail(model)
                }
            };
        }

        private static string ValidateBpRequest(InsertBPMasterDataModel model)
        {
            if (model == null)
                return "BP request body is required.";

            var bpType = ResolveBpType(model);
            if (bpType is not ("C" or "V"))
                return "BP type must be Customer or Vendor.";

            if (ResolveCompanyId(model) <= 0)
                return "Company is required.";

            if (string.IsNullOrWhiteSpace(ResolveCompanyName(model)))
                return "Company name is required.";

            if (string.IsNullOrWhiteSpace(ResolveIndustry(model)))
                return "Industry is required.";

            if (string.IsNullOrWhiteSpace(ResolveFirstName(model)))
                return "Contact first name is required.";

            if (string.IsNullOrWhiteSpace(ResolveLastName(model)))
                return "Contact last name is required.";

            var pan = ResolvePan(model).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(pan))
                return "PAN is required.";

            if (!Regex.IsMatch(pan, "^[A-Z]{5}[0-9]{4}[A-Z]$"))
                return "PAN format is invalid.";

            var currency = ResolveCurrency(model);
            if (!Regex.IsMatch(currency, "^[A-Z]{3}$"))
                return "Currency must be a valid three-letter code.";

            var mobile = ResolveMobile(model);
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return "Mobile number is required.";
            }

            var sanitizedMobile = SanitizeMobileForValidation(mobile);
            if (!Regex.IsMatch(sanitizedMobile, "^[6-9][0-9]{9}$"))
                return "Mobile number format is invalid.";

            var email = ResolveEmail(model);
            if (string.IsNullOrWhiteSpace(email))
                return "Email address is required.";

            if (!Regex.IsMatch(email.Trim(), "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$"))
            {
                return "Email address format is invalid.";
            }

            var alternateEmail = ResolveAlternateEmail(model);
            if (!string.IsNullOrWhiteSpace(alternateEmail)
                && !Regex.IsMatch(alternateEmail.Trim(), "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$"))
            {
                return "Alternate email address format is invalid.";
            }

            var gstin = model.Gstin?.Trim().ToUpperInvariant() ?? string.Empty;
            if (bpType == "V" && string.IsNullOrWhiteSpace(gstin))
                return "GSTIN is required for vendors.";

            if (bpType == "C"
                && string.Equals(model.CustomerType, "B2B", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(gstin))
            {
                return "GSTIN is required for B2B customers.";
            }

            if (!string.IsNullOrWhiteSpace(gstin)
                && !Regex.IsMatch(gstin, "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$"))
            {
                return "GSTIN format is invalid.";
            }

            var addresses = BuildAddressRows(model);
            var billingAddresses = addresses.Where(a => a.AddressType.StartsWith("B", StringComparison.OrdinalIgnoreCase)).ToList();
            if (billingAddresses.Count == 0)
                return "At least one billing address is required.";

            foreach (var address in billingAddresses)
            {
                if (string.IsNullOrWhiteSpace(address.Street))
                    return "Billing address street is required.";

                if (string.IsNullOrWhiteSpace(address.City))
                    return "Billing address city is required.";

                if (string.IsNullOrWhiteSpace(address.State))
                    return "Billing address state is required.";

                var addressGstin = FirstText(address.Gstin, gstin);
                if (!GstStateMatchesAddress(addressGstin, address))
                    return "GSTIN state code does not match the billing address state.";
            }

            foreach (var address in addresses)
            {
                if (!string.IsNullOrWhiteSpace(address.Gstin)
                    && !Regex.IsMatch(address.Gstin.Trim().ToUpperInvariant(), "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$"))
                {
                    return "Address GSTIN format is invalid.";
                }

                if (!GstStateMatchesAddress(address.Gstin, address))
                    return "Address GSTIN state code does not match the selected state.";
            }

            if (model.HasMsme)
            {
                var msmeNo = ResolveMsme(model).Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(msmeNo))
                    return "MSME/Udyam registration number is required when MSME is enabled.";

                if (!Regex.IsMatch(msmeNo, "^UDYAM-[A-Z]{2}-\\d{2}-\\d{7}$"))
                    return "MSME/Udyam registration number format is invalid.";

                if (bpType == "V" && string.IsNullOrWhiteSpace(model.MsmeType))
                    return "MSME type is required for MSME vendors.";

                if (bpType == "V" && string.IsNullOrWhiteSpace(model.MsmeBType))
                    return "MSME business type is required for MSME vendors.";
            }

            var primaryBank = ResolvePrimaryBank(model);
            if (bpType == "V")
            {
                if (primaryBank == null)
                    return "At least one vendor bank account is required.";

                if (string.IsNullOrWhiteSpace(ResolveBankName(primaryBank)))
                    return "Bank name/code is required for vendor bank account.";

                if (string.IsNullOrWhiteSpace(ResolveBankAccountNo(primaryBank)))
                    return "Bank account number is required for vendor bank account.";

                var ifsc = ResolveBankIfsc(primaryBank);
                if (string.IsNullOrWhiteSpace(ifsc))
                    return "IFSC code is required for vendor bank account.";

                if (!Regex.IsMatch(ifsc.Trim().ToUpperInvariant(), "^[A-Z]{4}0[A-Z0-9]{6}$"))
                {
                    return "IFSC code format is invalid.";
                }
            }

            foreach (var attachment in model.Attachments ?? new List<BPAttachment>())
            {
                if (string.IsNullOrWhiteSpace(attachment.FileName)
                    || string.IsNullOrWhiteSpace(attachment.FilePath)
                    || attachment.FileSize < 0)
                {
                    return "Attachment metadata is invalid.";
                }
            }

            return string.Empty;
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

        private static BP_Master BuildListMaster(BpListModel item)
        {
            return new BP_Master
            {
                Code = item.Code,
                Type = item.Type ?? string.Empty,
                IsStaff = item.IsStaff,
                Name = item.CompanyName ?? string.Empty,
                ForeignName = item.ForeignName ?? string.Empty,
                TypeOfBusiness = item.TypeOfBusiness ?? string.Empty,
                Industry = item.Industry ?? string.Empty,
                FirstName = item.FirstName ?? string.Empty,
                LastName = item.LastName ?? string.Empty,
                Designation = item.Designation ?? string.Empty,
                MobileNumber = item.MobileNumber ?? string.Empty,
                EmailAddress = item.EmailAddress ?? string.Empty,
                AlternateEmail = item.AlternateEmail ?? string.Empty,
                Currency = string.IsNullOrWhiteSpace(item.Currency) ? "INR" : item.Currency,
                Remarks = item.Remarks ?? string.Empty,
                company = item.CompanyId,
                flowId = item.flowId
            };
        }

        private async Task EnrichBpListDetailsAsync<T>(SqlConnection connection, IReadOnlyCollection<T> rows)
            where T : BpListModel
        {
            if (rows == null || rows.Count == 0)
                return;

            var codes = rows.Select(r => r.Code).Where(code => code > 0).Distinct().ToArray();
            if (codes.Length == 0)
                return;

            var masterRows = (await connection.QueryAsync<BpMasterDetailRow>(
                @"SELECT
                    code,
                    companyByUser
                  FROM BP.jsMaster
                  WHERE code IN @Codes",
                new { Codes = codes })).ToDictionary(row => row.Code);

            var taxRows = (await connection.QueryAsync<BpTaxDetailRow>(
                @"SELECT
                    code,
                    buyerTANNo AS tan,
                    panNo AS panNumber,
                    fssaiNo AS fssaiLicense,
                    msmeNo AS msme,
                    msmeType,
                    msmeBType,
                    gstin
                  FROM BP.jsTaxDetails
                  WHERE code IN @Codes",
                new { Codes = codes })).ToDictionary(row => row.Code);

            var addressRows = (await connection.QueryAsync<BpAddressDetailRow>(
                @"SELECT
                    code,
                    addressType,
                    addressLine1 AS street,
                    addressLine2 AS blockArea,
                    stateID AS state,
                    cityID AS city,
                    pincode AS pinCode,
                    countryID AS country,
                    gstNo AS gstin,
                    addressName
                  FROM BP.jsMasterAddress
                  WHERE code IN @Codes",
                new { Codes = codes })).ToList();

            var bankRows = (await connection.QueryAsync<BpBankDetailRow>(
                @"SELECT
                    code,
                    name AS bankName,
                    branch AS branchName,
                    accountNo AS accountNumber,
                    ifscCode,
                    swiftCode,
                    accountType
                  FROM BP.jsBankDetails
                  WHERE code IN @Codes",
                new { Codes = codes })).ToList();

            var contactRows = (await connection.QueryAsync<BpContactDetailRow>(
                @"SELECT
                    code,
                    firstName,
                    lastName,
                    designation,
                    emailAddress,
                    alternateEmail,
                    mobileNumber,
                    alternateContact
                  FROM BP.jsContactPersons
                  WHERE code IN @Codes",
                new { Codes = codes })).ToList();

            var attachmentRows = (await connection.QueryAsync<BpAttachmentDetailRow>(
                @"SELECT
                    code,
                    fileName,
                    filePath,
                    fileSize,
                    contentType,
                    fileType
                  FROM BP.jsAttachments
                  WHERE code IN @Codes",
                new { Codes = codes })).ToList();

            var addressesByCode = addressRows.GroupBy(row => row.Code).ToDictionary(group => group.Key, group => group.Cast<BP_Address>().ToList());
            var banksByCode = bankRows.GroupBy(row => row.Code).ToDictionary(group => group.Key, group => group.Cast<BP_Bank>().ToList());
            var contactsByCode = contactRows.GroupBy(row => row.Code).ToDictionary(group => group.Key, group => group.Cast<BP_Contact>().ToList());
            var attachmentsByCode = attachmentRows.GroupBy(row => row.Code).ToDictionary(group => group.Key, group => group.Cast<BP_Attachment>().ToList());

            foreach (var row in rows)
            {
                row.Master = BuildListMaster(row);
                if (masterRows.TryGetValue(row.Code, out var master))
                    row.Master.CompanyByUser = master.CompanyByUser ?? string.Empty;

                row.TaxDetails = taxRows.TryGetValue(row.Code, out var tax) ? tax : new BP_Tax();

                var addresses = addressesByCode.TryGetValue(row.Code, out var bpAddresses)
                    ? bpAddresses
                    : new List<BP_Address>();

                row.BillingAddresses = addresses.Where(IsBillTo).ToList();
                row.ShippingAddresses = addresses.Where(IsShipTo).ToList();
                row.BankDetails = banksByCode.TryGetValue(row.Code, out var banks) ? banks : new List<BP_Bank>();
                row.ContactPersons = contactsByCode.TryGetValue(row.Code, out var contacts) ? contacts : new List<BP_Contact>();
                row.Attachments = attachmentsByCode.TryGetValue(row.Code, out var attachments) ? attachments : new List<BP_Attachment>();
            }
        }

        public async Task<BPMasterResponse> InsertBPMasterAsync(InsertBPMasterDataModel model)
        {
            var response = new BPMasterResponse();
            var validationError = ValidateBpRequest(model);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                response.Success = false;
                response.Message = validationError;
                response.GeneratedCode = 0;
                return response;
            }

            try
            {
                var companyId = ResolveCompanyId(model);
                var primaryBank = ResolvePrimaryBank(model);

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("[BP].[jsInsertBPMasterData]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@type", ResolveBpType(model));
                    cmd.Parameters.AddWithValue("@isStaff", model.IsStaff);
                    cmd.Parameters.AddWithValue("@name", ResolveCompanyName(model));
                    cmd.Parameters.AddWithValue("@company", companyId);
                    cmd.Parameters.AddWithValue("@foreignName", (object?)ResolveForeignName(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@typeOfBusiness", (object?)model.TypeOfBusiness ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@industry", (object?)ResolveIndustry(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@firstName", (object?)ResolveFirstName(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@lastName", (object?)ResolveLastName(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@designation", (object?)ResolveDesignation(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mobileNo", (object?)ResolveMobile(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@emailAddress", (object?)ResolveEmail(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@alternateEmail", (object?)ResolveAlternateEmail(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@currency", (object?)ResolveCurrency(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@remarks", (object?)model.Remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@userId", model.UserId);
                    cmd.Parameters.AddWithValue("@companyByUser", ResolveCompanyByUser(model));

                    cmd.Parameters.AddWithValue("@tan", (object?)model.Tan ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@panNo", ResolvePan(model));
                    cmd.Parameters.AddWithValue("@fssaiNo", (object?)ResolveFssai(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeNo", (object?)ResolveMsme(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeType", (object?)model.MsmeType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeBType", (object?)model.MsmeBType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gstin", (object?)model.Gstin ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@bankName", (object?)ResolveBankName(primaryBank) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@branchName", (object?)ResolveBankBranch(primaryBank) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@accountNo", (object?)ResolveBankAccountNo(primaryBank) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ifscCode", (object?)ResolveBankIfsc(primaryBank) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@swiftCode", (object?)primaryBank?.SwiftCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@accountType", (object?)primaryBank?.AccountType ?? DBNull.Value);

                    var addressTable = ToAddressDataTable(BuildAddressRows(model));
                    var contactTable = ToContactDataTable(BuildContactRows(model));
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

                if (response.GeneratedCode > 0)
                    await EnsureInitialPendingFlowStatusAsync(response.GeneratedCode, model.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "BP Master insert failed. Company={Company}, UserId={UserId}, BpType={BpType}",
                    model.CompanyId,
                    model.UserId,
                    ResolveBpType(model));
                    response.Success = false;
                    response.Message = GetBpInsertFailureMessage(ex);
                    response.GeneratedCode = 0;
                }

            return response;
        }

        private static string GetBpInsertFailureMessage(Exception ex)
        {
            if (ex is SqlException sqlException)
            {
                var businessError = sqlException.Errors
                    .Cast<SqlError>()
                    .FirstOrDefault(error => error.Number is >= 50001 and <= 50020);

                if (businessError != null && !string.IsNullOrWhiteSpace(businessError.Message))
                    return businessError.Message;
            }

            return "BP Master insert failed.";
        }

        private async Task EnsureInitialPendingFlowStatusAsync(int bpCode, int creatorUserId)
        {
            const string sql = @"
;WITH FlowRows AS
(
    SELECT
        f.id AS FlowId,
        f.currentStageId AS StageId,
        f.templateId AS TemplateId
    FROM BP.jsFlow AS f
    WHERE f.bpCode = @BpCode
      AND f.status = 'P'
),
ApproverRows AS
(
    SELECT DISTINCT
        fr.FlowId,
        fr.StageId,
        fr.TemplateId,
        COALESCE(us.userId, @CreatorUserId) AS UserId
    FROM FlowRows AS fr
    LEFT JOIN dbo.jsUserStage AS us
        ON us.stageId = fr.StageId
       AND ISNULL(us.status, 1) = 1
)
INSERT INTO BP.jsFlowStatus
(
    flowId,
    status,
    stageId,
    templateId,
    userId,
    createdOn,
    description
)
SELECT
    ar.FlowId,
    'P',
    ar.StageId,
    ar.TemplateId,
    ar.UserId,
    GETDATE(),
    'Pending'
FROM ApproverRows AS ar
WHERE ar.UserId IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM BP.jsFlowStatus AS fs
      WHERE fs.flowId = ar.FlowId
        AND fs.status = 'P'
        AND fs.stageId = ar.StageId
        AND fs.templateId = ar.TemplateId
        AND fs.userId = ar.UserId
  );";

            using var connection = new SqlConnection(_connectionString);
            var inserted = await connection.ExecuteAsync(sql, new { BpCode = bpCode, CreatorUserId = creatorUserId });

            _logger.LogInformation(
                "BP initial pending flow-status rows ensured. BpCode={BpCode}, CreatorUserId={CreatorUserId}, RowsInserted={RowsInserted}",
                bpCode,
                creatorUserId,
                inserted);
        }

        private DataTable ToAddressDataTable(List<BPMasterAddress> list)
        {
            var table = new DataTable();
            table.Columns.Add("addressType", typeof(string));
            table.Columns.Add("street", typeof(string));
            table.Columns.Add("blockArea", typeof(string));
            table.Columns.Add("state", typeof(string));
            table.Columns.Add("city", typeof(string));
            table.Columns.Add("pinCode", typeof(string));
            table.Columns.Add("country", typeof(string));
            table.Columns.Add("gstin", typeof(string));
            table.Columns.Add("addressName", typeof(string));

            foreach (var item in list)
            {
                table.Rows.Add(item.AddressType, item.Street, item.BlockArea, item.State,
                               item.City, item.PinCode, item.Country, item.Gstin,
                               item.AddressName);
            }

            return table;
        }
        private DataTable ToContactDataTable(List<BPContactPerson> list)
        {
            var table = new DataTable();
            table.Columns.Add("firstName", typeof(string));
            table.Columns.Add("lastName", typeof(string));
            table.Columns.Add("designation", typeof(string));
            table.Columns.Add("emailAddress", typeof(string));
            table.Columns.Add("alternateEmail", typeof(string));
            table.Columns.Add("mobileNumber", typeof(string));
            table.Columns.Add("alternateContact", typeof(string));

            foreach (var item in list)
            {
                table.Rows.Add(item.FirstName, item.LastName, item.Designation, item.EmailAddress,
                               item.AlternateEmail, item.MobileNumber, item.AlternateContact);
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
            await LoadAsync("countries", () => GetCountryAsync(company), value => options.Countries = value);
            await LoadAsync("states", () => GetDistinctStatesAsync(company, normalizedCountryCode), value => options.States = value);
            await LoadAsync("uniquePANs", () => GetUniquePANsAsync(company), value => options.UniquePANs = value);

            return options;
        }
        public async Task<IEnumerable<ApprovedBpModel>> GetApprovedBPsAsync(int userId, int companyId, string month = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var normalizedMonth = NormalizeMonthFilter(month);
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", normalizedMonth);

                var result = (await connection.QueryAsync<ApprovedBpModel>(
                    "[BP].[jsGetApprovedBP]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                foreach (var row in result)
                    row.status = "approved";

                await EnrichBpListDetailsAsync(connection, result);
                return result;
            }
        }
        public async Task<IEnumerable<PendingBpModel>> GetPendingBpAsync(int userId, int companyId, string month = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var normalizedMonth = NormalizeMonthFilter(month);
            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@companyId", companyId);
            parameters.Add("@month", normalizedMonth);

            var result = (await connection.QueryAsync<PendingBpModel>(
                "[BP].[jsGetPendingBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            )).ToList();

            foreach (var row in result)
                row.status = "pending";

            await EnrichBpListDetailsAsync(connection, result);
            return result;
        }
        public async Task<IEnumerable<RejectedBPModel>> GetRejectedBpAsync(int userId, int companyId, string month = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var normalizedMonth = NormalizeMonthFilter(month);
            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@companyId", companyId);
            parameters.Add("@month", normalizedMonth);

            var result = (await connection.QueryAsync<RejectedBPModel>(
                "[BP].[jsGetRejectedBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            )).ToList();

            foreach (var row in result)
                row.status = "rejected";

            await EnrichBpListDetailsAsync(connection, result);
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
                    var addresses = (await multi.ReadAsync<BP_Address>()).ToList();
                    result.BillingAddresses = addresses.Where(IsBillTo).ToList();
                    result.ShippingAddresses = addresses.Where(IsShipTo).ToList();
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
                    ErrorCode = "BP_CONCURRENT_APPROVAL",
                    FlowId = request.FlowId
                };
            }

            try
            {
                var flow = await GetBpFlowRuntimeAsync(request.FlowId);
                if (flow == null)
                {
                    _logger.LogWarning(
                        "BP approval failed before workflow load. FlowId={FlowId}, Company={Company}, UserId={UserId}, Action={Action}",
                        request.FlowId,
                        request.Company,
                        request.UserId,
                        request.Action);

                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        ResultMessage = "BP workflow not found.",
                        ErrorCode = "BP_WORKFLOW_NOT_FOUND",
                        FlowId = request.FlowId
                    };
                }

                var debugSnapshot = await GetApprovalDebugSnapshotAsync(request.FlowId, request.UserId);
                LogApprovalDebugSnapshot(request, flow, debugSnapshot);

                if (flow.Company != request.Company)
                {
                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        ResultMessage = "Access denied: BP belongs to a different company.",
                        ErrorCode = "BP_COMPANY_MISMATCH",
                        FlowId = flow.FlowId,
                        BPCode = flow.BpCode,
                        BPCompany = flow.Company
                    };
                }

                if (!string.Equals(flow.FlowStatus, "P", StringComparison.OrdinalIgnoreCase))
                {
                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        ResultMessage = string.Equals(flow.FlowStatus, "A", StringComparison.OrdinalIgnoreCase)
                            ? "BP workflow is already approved."
                            : "BP workflow is not pending approval.",
                        ErrorCode = string.Equals(flow.FlowStatus, "A", StringComparison.OrdinalIgnoreCase)
                            ? "BP_ALREADY_APPROVED"
                            : "BP_WORKFLOW_NOT_PENDING",
                        FlowId = flow.FlowId,
                        BPCode = flow.BpCode,
                        BPCompany = flow.Company,
                        ApprovalStatus = flow.FlowStatus
                    };
                }

                if (!flow.IsFinalStage || !string.Equals(request.Action ?? "Approve", "Approve", StringComparison.OrdinalIgnoreCase))
                {
                    var nonFinalResult = await ExecuteApproveProcedureAsync(request);
                    if (!nonFinalResult.Success)
                        return nonFinalResult;

                    nonFinalResult.ResultMessage = "BP moved to next approval stage.";
                    nonFinalResult.FlowId = flow.FlowId;
                    nonFinalResult.BPCode = nonFinalResult.BPCode == 0 ? flow.BpCode : nonFinalResult.BPCode;
                    nonFinalResult.BPCompany = nonFinalResult.BPCompany == 0 ? flow.Company : nonFinalResult.BPCompany;
                    nonFinalResult.ApprovalStatus = "Advanced";
                    nonFinalResult.SapStatus = "Skipped";
                    return nonFinalResult;
                }

                var authorizedForFinalStage = await IsUserAssignedToCurrentStageAsync(request.UserId, flow.CurrentStageId);
                if (!authorizedForFinalStage)
                {
                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        ResultMessage = "User is not assigned to the final BP approval stage.",
                        ErrorCode = "BP_FINAL_STAGE_UNAUTHORIZED",
                        FlowId = flow.FlowId,
                        BPCode = flow.BpCode,
                        BPCompany = flow.Company
                    };
                }

                var sapLock = _sapPostLocks.GetOrAdd(flow.BpCode, _ => new SemaphoreSlim(1, 1));
                if (!await sapLock.WaitAsync(TimeSpan.FromSeconds(30)))
                {
                    return new ApproveOrRejectBpResponse
                    {
                        Success = false,
                        ResultMessage = "Another request is already creating this BP in SAP. Please retry shortly.",
                        ErrorCode = "BP_SAP_POST_IN_PROGRESS",
                        FlowId = flow.FlowId,
                        BPCode = flow.BpCode,
                        BPCompany = flow.Company,
                        SapStatus = "Processing"
                    };
                }

                try
                {
                    var previousTag = await UpdateBpApiStatusAsync(flow.BpCode, "Processing SAP BP creation", "P", null, null, null, request.UserId);
                    if (string.Equals(previousTag.PreviousTag, "Y", StringComparison.OrdinalIgnoreCase))
                    {
                        var alreadySynced = await ExecuteApproveProcedureAsync(request);
                        if (!alreadySynced.Success)
                            return alreadySynced;

                        alreadySynced.ResultMessage = "BP approved and activated successfully.";
                        alreadySynced.FlowId = flow.FlowId;
                        alreadySynced.BPCode = alreadySynced.BPCode == 0 ? flow.BpCode : alreadySynced.BPCode;
                        alreadySynced.BPCompany = alreadySynced.BPCompany == 0 ? flow.Company : alreadySynced.BPCompany;
                        alreadySynced.ApprovalStatus = "Approved";
                        alreadySynced.SapStatus = "Already synced";
                        return alreadySynced;
                    }

                    if (string.Equals(previousTag.PreviousTag, "P", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ApproveOrRejectBpResponse
                        {
                            Success = false,
                            ResultMessage = "SAP BP creation is already processing. Please retry shortly.",
                            ErrorCode = "BP_SAP_POST_IN_PROGRESS",
                            FlowId = flow.FlowId,
                            BPCode = flow.BpCode,
                            BPCompany = flow.Company,
                            SapStatus = "Processing"
                        };
                    }

                    BpSapPostResult sapResult;
                    try
                    {
                        var bpData = await GetSingleBPDataForSapAsync(flow.BpCode);
                        var sapData = await GetSPADataAsync(flow.BpCode);
                        sapResult = await _bpMasterSapService.PostBusinessPartnerAsync(new BpSapPostRequest
                        {
                            FlowId = flow.FlowId,
                            BpCode = flow.BpCode,
                            Company = flow.Company,
                            UserId = request.UserId,
                            BpType = flow.BpType,
                            BpData = bpData,
                            SapData = sapData
                        });
                    }
                    catch (SqlException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        var mappedSapError = BpSapErrorMapper.ExtractSapError(ex);

                        _logger.LogWarning(
                            ex,
                            "SAP BP POST FAILED FlowId={FlowId} BpCode={BpCode} SapCode={SapCode} SapMessage={SapMessage} RawResponse={RawResponse}",
                            flow.FlowId,
                            flow.BpCode,
                            mappedSapError.SapCode,
                            mappedSapError.Message,
                            mappedSapError.RawResponse);

                        await UpdateBpApiStatusAsync(flow.BpCode, mappedSapError.Message, "N", null, null, null, request.UserId);
                        return new ApproveOrRejectBpResponse
                        {
                            Success = false,
                            ResultMessage = mappedSapError.Message,
                            ErrorCode = mappedSapError.ErrorCode,
                            SapError = mappedSapError.ToResponseInfo(),
                            FlowId = flow.FlowId,
                            BPCode = flow.BpCode,
                            BPCompany = flow.Company,
                            ApprovalStatus = "Blocked",
                            SapStatus = "Failed"
                        };
                    }

                    if (!sapResult.Success)
                    {
                        await UpdateBpApiStatusAsync(flow.BpCode, sapResult.Message, "N", null, sapResult.AttachmentEntry, sapResult.PayloadHash, request.UserId);
                        var resultMessage = FormatBpSapFailureMessage(sapResult);
                        _logger.LogWarning(
                            "SAP BP POST FAILED FlowId={FlowId} BpCode={BpCode} SapCode={SapCode} SapMessage={SapMessage} RawResponse={RawResponse}",
                            flow.FlowId,
                            flow.BpCode,
                            sapResult.SapError?.code,
                            sapResult.SapError?.message ?? sapResult.Message,
                            sapResult.RawResponse);

                        return new ApproveOrRejectBpResponse
                        {
                            Success = false,
                            ResultMessage = resultMessage,
                            ErrorCode = string.IsNullOrWhiteSpace(sapResult.ErrorCode) ? "SAP_POST_FAILED" : sapResult.ErrorCode,
                            SapError = sapResult.SapError,
                            FlowId = flow.FlowId,
                            BPCode = flow.BpCode,
                            BPCompany = flow.Company,
                            ApprovalStatus = "Blocked",
                            SapStatus = "Failed",
                            AttachmentEntry = sapResult.AttachmentEntry,
                            PayloadHash = sapResult.PayloadHash
                        };
                    }

                    await UpdateBpApiStatusAsync(flow.BpCode, sapResult.Message, "Y", sapResult.CardCode, sapResult.AttachmentEntry, sapResult.PayloadHash, request.UserId);
                    var approved = await ExecuteApproveProcedureAsync(request);
                    if (!approved.Success)
                        return approved;

                    approved.ResultMessage = "BP approved and activated successfully.";
                    approved.FlowId = flow.FlowId;
                    approved.BPCode = approved.BPCode == 0 ? flow.BpCode : approved.BPCode;
                    approved.BPCompany = approved.BPCompany == 0 ? flow.Company : approved.BPCompany;
                    approved.ApprovalStatus = "Approved";
                    approved.SapStatus = "Success";
                    approved.SapCardCode = sapResult.CardCode;
                    approved.AttachmentEntry = sapResult.AttachmentEntry;
                    approved.PayloadHash = sapResult.PayloadHash;
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
                    ErrorCode = "BP_WORKFLOW_NOT_FOUND",
                    FlowId = request.FlowId
                };
            }

            if (flow.Company != request.Company)
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = "Access denied: BP belongs to a different company.",
                    ErrorCode = "BP_COMPANY_MISMATCH",
                    FlowId = flow.FlowId,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company
                };
            }

            if (!string.Equals(flow.FlowStatus, "P", StringComparison.OrdinalIgnoreCase))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = "SAP retry is only allowed for pending BP workflows.",
                    ErrorCode = "BP_RETRY_NOT_PENDING",
                    FlowId = flow.FlowId,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company
                };
            }

            if (!flow.IsFinalStage)
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = "SAP retry is only allowed at the final BP approval stage.",
                    ErrorCode = "BP_RETRY_NOT_FINAL_STAGE",
                    FlowId = flow.FlowId,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company
                };
            }

            var apiStatusTag = await GetBpApiStatusTagAsync(flow.BpCode);
            if (string.Equals(apiStatusTag, "P", StringComparison.OrdinalIgnoreCase))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = "SAP BP creation is already processing. Please retry shortly.",
                    ErrorCode = "BP_SAP_POST_IN_PROGRESS",
                    FlowId = flow.FlowId,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    SapStatus = "Processing"
                };
            }

            if (string.IsNullOrWhiteSpace(apiStatusTag))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = "No failed SAP BP posting exists for retry. Use normal final approval.",
                    ErrorCode = "BP_RETRY_NOT_STARTED",
                    FlowId = flow.FlowId,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company
                };
            }

            if (string.Equals(apiStatusTag, "Y", StringComparison.OrdinalIgnoreCase))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = "SAP retry is blocked because this BP is already posted successfully.",
                    ErrorCode = "BP_RETRY_ALREADY_POSTED",
                    FlowId = flow.FlowId,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    SapStatus = "Success"
                };
            }

            if (!string.Equals(apiStatusTag, "N", StringComparison.OrdinalIgnoreCase))
            {
                return new ApproveOrRejectBpResponse
                {
                    Success = false,
                    ResultMessage = $"SAP retry is not allowed for current SAP status tag '{apiStatusTag}'.",
                    ErrorCode = "BP_RETRY_INVALID_STATUS",
                    FlowId = flow.FlowId,
                    BPCode = flow.BpCode,
                    BPCompany = flow.Company,
                    SapStatus = apiStatusTag ?? string.Empty
                };
            }

            _logger.LogInformation(
                "Retrying BP SAP post. FlowId={FlowId}, BpCode={BpCode}, Company={Company}, UserId={UserId}, ApiStatusTag={ApiStatusTag}",
                flow.FlowId,
                flow.BpCode,
                flow.Company,
                request.UserId,
                apiStatusTag);

            request.Action = "Approve";
            return await ApproveBPAsync(request);
        }

        private async Task<ApproveOrRejectBpResponse> ExecuteApproveProcedureAsync(ApproveOrRejectBpRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@flowId", request.FlowId);
            parameters.Add("@company", request.Company);
            parameters.Add("@userId", request.UserId);
            parameters.Add("@remarks", request.Remarks ?? "");
            parameters.Add("@action", string.IsNullOrWhiteSpace(request.Action) ? "Approve" : request.Action);

            _logger.LogInformation(
                "Executing BP approval SQL: EXEC [BP].[jsApproveBP] @flowId={FlowId}, @company={Company}, @userId={UserId}, @remarks={Remarks}, @action={Action}",
                request.FlowId,
                request.Company,
                request.UserId,
                request.Remarks ?? "",
                string.IsNullOrWhiteSpace(request.Action) ? "Approve" : request.Action);

            var result = await connection.QuerySingleAsync<ApproveOrRejectBpResponse>(
                "[BP].[jsApproveBP]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        private async Task<BpApprovalDebugSnapshot?> GetApprovalDebugSnapshotAsync(int flowId, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
SELECT
    f.id AS FlowId,
    f.bpCode AS BpCode,
    f.templateId AS TemplateId,
    f.currentStage AS CurrentStage,
    f.totalStage AS TotalStage,
    f.currentStageId AS CurrentStageId,
    f.status AS FlowStatus,
    m.company AS Company,
    s.stage AS MatchedStage,
    s.approvalId AS ApprovalId,
    st.templateId AS MatchedStageTemplateId,
    st.priority AS MatchedStagePriority,
    ta.templateId AS MatchedTemplateApprovalTemplateId,
    ta.approvalId AS MatchedTemplateApprovalId,
    us.userId AS MatchedApproverUserId,
    us.status AS MatchedApproverStatus
FROM BP.jsFlow AS f
INNER JOIN BP.jsMaster AS m ON m.code = f.bpCode
LEFT JOIN dbo.jsStage AS s ON s.id = f.currentStageId
OUTER APPLY
(
    SELECT TOP (1) stageId, templateId, priority
    FROM dbo.jsStageTemplate
    WHERE stageId = f.currentStageId
       OR templateId = f.templateId
    ORDER BY
        CASE WHEN stageId = f.currentStageId THEN 0 ELSE 1 END,
        priority
) AS st
LEFT JOIN dbo.jsTemplateApproval AS ta
    ON ta.templateId = f.templateId
   AND ta.approvalId = s.approvalId
LEFT JOIN dbo.jsUserStage AS us
    ON us.stageId = f.currentStageId
   AND us.userId = @userId
   AND ISNULL(us.status, 1) = 1
WHERE f.id = @flowId;";

            return await connection.QueryFirstOrDefaultAsync<BpApprovalDebugSnapshot>(sql, new { flowId, userId });
        }

        private void LogApprovalDebugSnapshot(
            ApproveOrRejectBpRequest request,
            BpFlowRuntimeModel flow,
            BpApprovalDebugSnapshot? snapshot)
        {
            if (snapshot == null)
            {
                _logger.LogWarning(
                    "BP approval debug snapshot not found. FlowId={FlowId}, TemplateId={TemplateId}, CurrentStageId={CurrentStageId}, CurrentStage={CurrentStage}, TotalStage={TotalStage}, UserId={UserId}",
                    flow.FlowId,
                    flow.TemplateId,
                    flow.CurrentStageId,
                    flow.CurrentStage,
                    flow.TotalStage,
                    request.UserId);
                return;
            }

            _logger.LogInformation(
                "BP approval debug snapshot. FlowId={FlowId}, BpCode={BpCode}, TemplateId={TemplateId}, CurrentStage={CurrentStage}, TotalStage={TotalStage}, CurrentStageId={CurrentStageId}, FlowStatus={FlowStatus}, ApprovalId={ApprovalId}, MatchedStage={MatchedStage}, MatchedStageTemplateId={MatchedStageTemplateId}, MatchedStagePriority={MatchedStagePriority}, MatchedTemplateApprovalTemplateId={MatchedTemplateApprovalTemplateId}, MatchedTemplateApprovalId={MatchedTemplateApprovalId}, MatchedApproverUserId={MatchedApproverUserId}, MatchedApproverStatus={MatchedApproverStatus}, Company={Company}, RequestCompany={RequestCompany}, RequestUserId={RequestUserId}, RequestAction={RequestAction}",
                snapshot.FlowId,
                snapshot.BpCode,
                snapshot.TemplateId,
                snapshot.CurrentStage,
                snapshot.TotalStage,
                snapshot.CurrentStageId,
                snapshot.FlowStatus,
                snapshot.ApprovalId,
                snapshot.MatchedStage,
                snapshot.MatchedStageTemplateId,
                snapshot.MatchedStagePriority,
                snapshot.MatchedTemplateApprovalTemplateId,
                snapshot.MatchedTemplateApprovalId,
                snapshot.MatchedApproverUserId,
                snapshot.MatchedApproverStatus,
                snapshot.Company,
                request.Company,
                request.UserId,
                request.Action);
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
            const string sql = "SELECT COUNT(1) FROM dbo.jsUserStage WHERE userId = @userId AND stageId = @stageId AND ISNULL(status, 1) = 1;";
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

            var master = await multi.ReadFirstOrDefaultAsync<BP_Master>();
            var taxDetails = await multi.ReadFirstOrDefaultAsync<BP_Tax>();
            var addresses = (await multi.ReadAsync<BP_Address>()).ToList();

            return new SingleBPDataModel
            {
                Master = master ?? new BP_Master(),
                TaxDetails = taxDetails ?? new BP_Tax(),
                BillingAddresses = addresses.Where(IsBillTo).ToList(),
                ShippingAddresses = addresses.Where(IsShipTo).ToList(),
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

        private static string TruncateForDb(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private static string FormatBpSapFailureMessage(BpSapPostResult sapResult)
        {
            return sapResult.Message;
        }

        private static bool IsControlAccountConfigurationError(string? errorCode)
        {
            return errorCode is
                "CONTROL_ACCOUNT_NOT_CONFIGURED" or
                "CONTROL_ACCOUNT_NOT_FOUND_IN_SAP" or
                "CONTROL_ACCOUNT_NOT_POSTABLE" or
                "INVALID_VENDOR_CONTROL_ACCOUNT" or
                "INVALID_CUSTOMER_CONTROL_ACCOUNT" or
                "CONTROL_ACCOUNT_VALIDATION_FAILED";
        }

        private static string GetExactErrorMessage(Exception ex)
        {
            return ex.GetBaseException()?.Message ?? ex.Message;
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

            if (!result.Success)
                return result;

            result.ResultMessage = "BP rejected successfully.";
            result.FlowId = request.FlowId;
            result.ApprovalStatus = "Rejected";
            result.SapStatus = "Skipped";
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
        public async Task<BPCountModel> GetBPCountsAsync(string month, int userId, int companyId = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var normalizedMonth = NormalizeMonthFilter(month);
                var parameters = new DynamicParameters();
                parameters.Add("@month", normalizedMonth);    // Format: "MM-YYYY"
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);

                try
                {
                    var result = await connection.QueryFirstOrDefaultAsync<BPCountModel>(
                        "[BP].[jsGetBPCounts]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return result ?? new BPCountModel();
                }
                catch (SqlException ex) when (ex.Number == 8144)
                {
                    _logger.LogWarning(
                        ex,
                        "BP.jsGetBPCounts does not yet accept @companyId. Falling back to user/month counts. UserId={UserId}, CompanyId={CompanyId}, Month={Month}",
                        userId,
                        companyId,
                        normalizedMonth);

                    var fallbackParameters = new DynamicParameters();
                    fallbackParameters.Add("@month", normalizedMonth);
                    fallbackParameters.Add("@userId", userId);

                    var fallbackResult = await connection.QueryFirstOrDefaultAsync<BPCountModel>(
                        "[BP].[jsGetBPCounts]",
                        fallbackParameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return fallbackResult ?? new BPCountModel();
                }
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
                var normalizedMonth = NormalizeMonthFilter(month);
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", normalizedMonth);

                var pendingBP = await connection.QueryAsync<MergeBpModel>(
                    "[BP].[jsGetPendingBP]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var approvedBP = await connection.QueryAsync<MergeBpModel>(
                    "[BP].[jsGetApprovedBP]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var rejectedBP = await connection.QueryAsync<MergeBpModel>(
                    "[BP].[jsGetRejectedBP]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

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

                await EnrichBpListDetailsAsync(connection, allBP);
                return allBP;
            }
        }

        public async Task<BPmasterModels> UpdateBPMasterAsync(BPMasterUpdateRequest model)
        {
            var response = new BPmasterModels { Success = false };
            var validationError = ValidateBpRequest(model);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                response.Message = validationError;
                return response;
            }

            try
            {
                var companyId = ResolveCompanyId(model);
                var primaryBank = ResolvePrimaryBank(model);

                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("[BP].[jsUpdateBPMasterData]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Code", model.Code);
                    cmd.Parameters.AddWithValue("@type", ResolveBpType(model));
                    cmd.Parameters.AddWithValue("@isStaff", model.IsStaff);
                    cmd.Parameters.AddWithValue("@name", ResolveCompanyName(model));
                    cmd.Parameters.AddWithValue("@company", companyId);
                    cmd.Parameters.AddWithValue("@foreignName", (object?)ResolveForeignName(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@typeOfBusiness", (object?)model.TypeOfBusiness ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@industry", (object?)ResolveIndustry(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@firstName", (object?)ResolveFirstName(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@lastName", (object?)ResolveLastName(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@designation", (object?)ResolveDesignation(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mobileNo", (object?)ResolveMobile(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@emailAddress", (object?)ResolveEmail(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@alternateEmail", (object?)ResolveAlternateEmail(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@currency", (object?)ResolveCurrency(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@remarks", (object?)model.Remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@userId", model.UserId);
                    cmd.Parameters.AddWithValue("@companyByUser", ResolveCompanyByUser(model));

                    cmd.Parameters.AddWithValue("@tan", (object?)model.Tan ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@panNo", ResolvePan(model));
                    cmd.Parameters.AddWithValue("@fssaiNo", (object?)ResolveFssai(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeNo", (object?)ResolveMsme(model) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeType", (object?)model.MsmeType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@msmeBType", (object?)model.MsmeBType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gstin", (object?)model.Gstin ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@bankName", (object?)ResolveBankName(primaryBank) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@branchName", (object?)ResolveBankBranch(primaryBank) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@accountNo", (object?)ResolveBankAccountNo(primaryBank) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ifscCode", (object?)ResolveBankIfsc(primaryBank) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@swiftCode", (object?)primaryBank?.SwiftCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@accountType", (object?)primaryBank?.AccountType ?? DBNull.Value);

                    // Control flags
                    cmd.Parameters.Add(new SqlParameter("@updateAddresses", SqlDbType.Bit) { Value = model.UpdateAddresses });
                    cmd.Parameters.Add(new SqlParameter("@updateBankDetails", SqlDbType.Bit) { Value = model.UpdateBankDetails });
                    cmd.Parameters.Add(new SqlParameter("@updateContacts", SqlDbType.Bit) { Value = model.UpdateContacts });
                    cmd.Parameters.Add(new SqlParameter("@updateAttachments", SqlDbType.Bit) { Value = model.UpdateAttachments });

                    var addressTable = ToAddressDataTable(BuildAddressRows(model));
                    var contactTable = ToContactDataTable(BuildContactRows(model));
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
                _logger.LogError(
                    ex,
                    "BP Master update SQL failed. BpCode={BpCode}, Company={Company}, UserId={UserId}",
                    model.Code,
                    model.CompanyId,
                    model.UserId);
                response.Success = false;
                response.Message = "BP Master update failed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "BP Master update failed. BpCode={BpCode}, Company={Company}, UserId={UserId}",
                    model.Code,
                    model.CompanyId,
                    model.UserId);
                response.Success = false;
                response.Message = "BP Master update failed.";
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
                _logger.LogError(
                    ex,
                    "BP SAP metadata update SQL failed. MasterId={MasterId}",
                    model.MasterId);
                result.Success = false;
                result.Message = "SAP metadata update failed.";
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
                var normalizedMonth = NormalizeMonthFilter(month);
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", normalizedMonth);

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
                var normalizedMonth = NormalizeMonthFilter(month);
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", normalizedMonth);

                var result = await connection.QueryAsync<BPinsightsModel>(
                    "[BP].[jsGetBPInsights]",
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

        private sealed class BpApprovalDebugSnapshot
        {
            public int FlowId { get; set; }
            public int BpCode { get; set; }
            public int TemplateId { get; set; }
            public int CurrentStage { get; set; }
            public int TotalStage { get; set; }
            public int CurrentStageId { get; set; }
            public string FlowStatus { get; set; } = string.Empty;
            public int Company { get; set; }
            public string MatchedStage { get; set; } = string.Empty;
            public int? ApprovalId { get; set; }
            public int? MatchedStageTemplateId { get; set; }
            public int? MatchedStagePriority { get; set; }
            public int? MatchedTemplateApprovalTemplateId { get; set; }
            public int? MatchedTemplateApprovalId { get; set; }
            public int? MatchedApproverUserId { get; set; }
            public int? MatchedApproverStatus { get; set; }
        }

        private sealed class BpMasterDetailRow
        {
            public int Code { get; set; }
            public string CompanyByUser { get; set; } = string.Empty;
        }

        private sealed class BpTaxDetailRow : BP_Tax
        {
            public int Code { get; set; }
        }

        private sealed class BpAddressDetailRow : BP_Address
        {
            public int Code { get; set; }
        }

        private sealed class BpBankDetailRow : BP_Bank
        {
            public int Code { get; set; }
        }

        private sealed class BpContactDetailRow : BP_Contact
        {
            public int Code { get; set; }
        }

        private sealed class BpAttachmentDetailRow : BP_Attachment
        {
            public int Code { get; set; }
        }
    }
}
