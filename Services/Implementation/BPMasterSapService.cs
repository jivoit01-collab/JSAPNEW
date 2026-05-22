using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JSAPNEW.Services.Implementation
{
    public class BPMasterSapService : IBPMasterSapService
    {
        private readonly IConfiguration _configuration;
        private readonly IBom2Service _bom2Service;
        private readonly IWebHostEnvironment _environment;
        private readonly string _sapBaseUrl;

        public BPMasterSapService(IConfiguration configuration, IBom2Service bom2Service, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _bom2Service = bom2Service;
            _environment = environment;
            _sapBaseUrl = configuration["SapServiceLayer:BaseUrl"]
                ?? throw new ArgumentNullException("SapServiceLayer:BaseUrl not found in configuration.");
        }

        public async Task<BpSapPostResult> PostBusinessPartnerAsync(BpSapPostRequest request, CancellationToken cancellationToken = default)
        {
            if (request.BpData?.Master == null)
                return new BpSapPostResult { Success = false, Message = "BP master data was not found for SAP posting." };

            var session = await GetSessionAsync(request.Company);
            var cardType = IsVendor(request.BpType) ? "cSupplier" : "cCustomer";
            var prefix = ResolveCardCodePrefix(request, cardType);
            var cardCode = await GetNextCardCodeAsync(prefix, cardType, session, cancellationToken);
            var warnings = new List<string>();
            var attachmentEntry = await UploadAttachmentsAsync(request.BpData, session, warnings, cancellationToken);
            var payload = BuildBusinessPartnerPayload(request, cardCode, cardType, attachmentEntry, warnings);
            var payloadJson = payload.ToString(Formatting.None);

            var response = await SendSapRequestAsync(HttpMethod.Post, "BusinessPartners", session, payloadJson, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new BpSapPostResult
                {
                    Success = false,
                    Message = ExtractSapError(errorBody),
                    CardCode = cardCode,
                    AttachmentEntry = attachmentEntry,
                    Payload = payload,
                    PayloadHash = ComputeHash(payloadJson),
                    CardType = cardType,
                    RawResponse = errorBody
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = string.IsNullOrWhiteSpace(responseBody)
                ? $"SAP Business Partner created as {cardCode}."
                : $"SAP Business Partner created as {cardCode}.";

            if (warnings.Count > 0)
                message += " Warnings: " + string.Join(" ", warnings);

            return new BpSapPostResult
            {
                Success = true,
                Message = message,
                CardCode = cardCode,
                AttachmentEntry = attachmentEntry,
                Payload = payload,
                PayloadHash = ComputeHash(payloadJson),
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

        private async Task<string> GetNextCardCodeAsync(string prefix, string cardType, SAPSessionModel session, CancellationToken cancellationToken)
        {
            var safePrefix = prefix.Replace("'", "''").Trim();
            var filter = $"startswith(CardCode,'{safePrefix}') and CardType eq '{cardType}'";
            var endpoint = $"BusinessPartners?$filter={Uri.EscapeDataString(filter)}&$select=CardCode&$orderby=CardCode desc&$top=1";
            var response = await SendSapRequestAsync(HttpMethod.Get, endpoint, session, null, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Unable to generate SAP CardCode: " + ExtractSapError(body));

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
            int? attachmentEntry,
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
                ["Currency"] = string.IsNullOrWhiteSpace(master.Currency) ? "INR" : master.Currency.Trim().ToUpperInvariant()
            };

            if (!string.IsNullOrWhiteSpace(master.ForeignName))
                payload["CardForeignName"] = master.ForeignName.Trim();
            if (!string.IsNullOrWhiteSpace(master.MobileNumber))
                payload["Phone1"] = SanitizeMobile(master.MobileNumber);
            if (!string.IsNullOrWhiteSpace(master.EmailAddress))
                payload["EmailAddress"] = master.EmailAddress.Trim();
            if (!string.IsNullOrWhiteSpace(master.Remarks))
                payload["Notes"] = master.Remarks.Trim();
            if (!string.IsNullOrWhiteSpace(master.TypeOfBusiness))
                payload["U_TypeOfBusiness"] = master.TypeOfBusiness.Trim();
            if (!string.IsNullOrWhiteSpace(master.Industry))
                payload["U_Industry"] = master.Industry.Trim();
            if (attachmentEntry.HasValue)
                payload["AttachmentEntry"] = attachmentEntry.Value;
            if (!string.IsNullOrWhiteSpace(tax?.FssaiLicense))
                payload["U_Fssai"] = tax.FssaiLicense.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(tax?.Msme))
                payload["U_MSME"] = tax.Msme.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(tax?.Tan))
                payload["U_TAN"] = tax.Tan.Trim().ToUpperInvariant();

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
                var banks = BuildBankAccounts(bp, warnings);
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

                result.Add(new JObject
                {
                    ["Name"] = Truncate(name, 50),
                    ["FirstName"] = contact.FirstName ?? string.Empty,
                    ["LastName"] = contact.LastName ?? string.Empty,
                    ["Position"] = contact.Designation ?? string.Empty,
                    ["MobilePhone"] = SanitizeMobile(contact.MobileNumber),
                    ["Phone1"] = SanitizeMobile(contact.AlternateContact),
                    ["E_Mail"] = contact.EmailAddress ?? string.Empty,
                    ["Active"] = "tYES"
                });
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
                ["Street"] = Truncate(address.Street, 100),
                ["Block"] = Truncate(address.BlockArea, 100),
                ["City"] = Truncate(address.City, 100),
                ["ZipCode"] = Truncate(address.PinCode, 20),
                ["State"] = MapStateCode(address.State),
                ["Country"] = MapCountry(address.Country)
            };

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

        private JArray BuildBankAccounts(SingleBPDataModel bp, List<string> warnings)
        {
            var result = new JArray();
            foreach (var bank in bp.BankDetails ?? new List<BP_Bank>())
            {
                if (string.IsNullOrWhiteSpace(bank.AccountNumber))
                    continue;

                var bankCode = bank.BankName;
                if (string.IsNullOrWhiteSpace(bankCode))
                {
                    warnings.Add($"Bank account {bank.AccountNumber} was skipped because bank name/code is missing.");
                    continue;
                }

                var ifsc = (bank.IfscCode ?? string.Empty).Trim().ToUpperInvariant();
                result.Add(new JObject
                {
                    ["BankCode"] = bankCode.Trim(),
                    ["AccountNo"] = bank.AccountNumber.Trim(),
                    ["Branch"] = Truncate(bank.BranchName, 50),
                    ["AccountName"] = Truncate(bp.Master.Name, 100),
                    ["BICSwiftCode"] = Truncate(ifsc, 50),
                    ["UserNo1"] = Truncate(ifsc, 50),
                    ["IBAN"] = Truncate(bank.SwiftCode, 34)
                });
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
                throw new InvalidOperationException("SAP Attachments2 upload failed: " + ExtractSapError(body));

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

        private static string ComputeHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }

        private static string ExtractSapError(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
                return "SAP returned an empty error response.";

            try
            {
                var obj = JObject.Parse(responseJson);
                var code = obj["error"]?["code"]?.ToString();
                var value = obj["error"]?["message"]?["value"]?.ToString()
                    ?? obj["error"]?["message"]?.ToString();

                if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(value))
                    return $"{value} (SAP Error Code: {code})";

                return obj.ToString(Formatting.None);
            }
            catch
            {
                return responseJson;
            }
        }
    }
}
