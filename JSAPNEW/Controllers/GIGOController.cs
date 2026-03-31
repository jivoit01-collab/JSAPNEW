using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Renci.SshNet.Messages.Authentication;
using ServiceStack.Text;
using System.Data;
using System.Text.Json;
using TicketSystem.Models;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GIGOController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IGIGOService _gigoService;
        private readonly ILogger<GIGOController> _logger;
        public GIGOController(IConfiguration configuration, IGIGOService gigoService, ILogger<GIGOController> logger)
        {
            _configuration = configuration;
            _gigoService = gigoService;
            _logger = logger;
        }
        [HttpPost("InsertGateEntry")]
        public async Task<IActionResult> InsertGateEntry(GateEntryModel model)
        {
            if (model == null)
            {
                return BadRequest("Invalid gate entry model.");
            }
            try
            {
                var response = await _gigoService.InsertGateEntryAsync(model);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(new gigoModels
                    {
                        Success = false,
                        Message = "Error: " + response.Message
                    });

                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error inserting gate entry.");
                return StatusCode(500, "Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting gate entry.");
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpPost("InsertBSTDocument")]
        public async Task<IActionResult> InsertBSTDocument(BSTDocument model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return BadRequest("Invalid BST document model.");
            }
            try
            {
                var response = await _gigoService.InsertBSTDocumentAsync(model);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(new gigoModels
                    {
                        Success = false,
                        Message = "Error: " + response.Message
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error inserting BST document.");
                return StatusCode(500, "Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting BST document.");
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpPost("InsertCustomerDocument")]
        public async Task<IActionResult> InsertCustomerDocument(CustomerDocument model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return BadRequest("Invalid Customer Document request");
            }
            try
            {
                var response = await _gigoService.InsertCustomerDocumentAsync(model);
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(new gigoModels
                    {
                        Success = false,
                        Message = "Error:" + response.Message
                    });
                }
            }

            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error inserting Customer document.");
                return StatusCode(500, "Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting Customer document.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("InsertAttachment")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<IEnumerable<gigoModels>>> InsertAttachment()
        {
            try
            {
                var form = await Request.ReadFormAsync();

                // Simple validation
                if (!int.TryParse(form["GateEntryID"], out int gateEntryId) || gateEntryId <= 0)
                    return BadRequest(new gigoModels { Success = false, Message = "Invalid GateEntryID" });

                if (!int.TryParse(form["UploadedBy"], out int uploadedBy) || uploadedBy <= 0)
                    return BadRequest(new gigoModels { Success = false, Message = "Invalid UploadedBy" });

                if (form.Files.Count == 0)
                    return BadRequest(new gigoModels { Success = false, Message = "No files uploaded" });

                // Create model and save files
                var model = new AddAttachmentModel
                {
                    GateEntryID = gateEntryId,
                    UploadedBy = uploadedBy,
                    Attachments = new List<AttachmentItem>()
                };

                var uploadFolder = Path.Combine("wwwroot", "Uploads", "GIGO");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                foreach (var file in form.Files)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName);
                        var newFileName = $"{Guid.NewGuid()}{ext}";
                        var savePath = Path.Combine(uploadFolder, newFileName);

                        using var stream = new FileStream(savePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        model.Attachments.Add(new AttachmentItem
                        {
                            FileName = newFileName,
                            //FilePath = "/Uploads/Ticket",
                            FileType = ext.Replace(".", ""),
                            FileSize = file.Length,
                            UploadedBy = uploadedBy
                        });
                    }
                }

                var result = await _gigoService.InsertAttachmentAsync(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new gigoModels { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("InsertVehicleDetails")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<List<gigoModels>>> InsertVehicleDetails()
        {
            try
            {
                var form = await Request.ReadFormAsync();

                if (!int.TryParse(form["GateEntryID"], out var gateEntryId) || gateEntryId <= 0)
                    return BadRequest(new gigoModels { Success = false, Message = "Invalid GateEntryID" });

                if (form.Files is null || form.Files.Count == 0)
                    return BadRequest(new gigoModels { Success = false, Message = "No files uploaded" });

                int createdBy = 0;
                if (!int.TryParse(User?.Claims?.FirstOrDefault(c => c.Type == "userId")?.Value, out createdBy))
                    int.TryParse(form["CreatedBy"], out createdBy);

                var common = new VehicleDetails
                {
                    GateEntryID = gateEntryId,
                    VehicleNo = form["VehicleNo"],
                    VRefNo = form["VRefNo"],
                    DriverName = form["DriverName"],
                    DriverNumber = form["DriverNumber"],
                    VendorBiltyNo = form["VendorBiltyNo"],
                    TransporterName = form["TransporterName"],
                    DocumentRemarks = form["DocumentRemarks"],
                    CreatedBy = createdBy
                };

                var uploadFolder = Path.Combine("wwwroot", "Uploads", "GIGO");
                Directory.CreateDirectory(uploadFolder);

                var toInsert = new List<VehicleDetails>();

                foreach (var file in form.Files)
                {
                    if (file.Length <= 0) continue;

                    // Basic guardrails (optional)
                    // if (file.Length > 20 * 1024 * 1024) return BadRequest(new gigoModels { Success=false, Message="File too large" });

                    var ext = Path.GetExtension(file.FileName);   // ".pdf"
                    var safeExt = (ext ?? string.Empty).ToLowerInvariant();
                    var newName = $"{Guid.NewGuid()}{safeExt}";
                    var fullPath = Path.Combine(uploadFolder, newName);

                    await using (var stream = System.IO.File.Create(fullPath))
                        await file.CopyToAsync(stream);

                    toInsert.Add(new VehicleDetails
                    {
                        GateEntryID = common.GateEntryID,
                        VehicleNo = common.VehicleNo,
                        VRefNo = common.VRefNo,
                        DriverName = common.DriverName,
                        DriverNumber = common.DriverNumber,
                        VendorBiltyNo = common.VendorBiltyNo,
                        TransporterName = common.TransporterName,
                        DocumentRemarks = common.DocumentRemarks,
                        CreatedBy = common.CreatedBy,

                        FileName = newName,
                        FileType = safeExt.TrimStart('.'),
                        FileSize = file.Length > int.MaxValue ? int.MaxValue : (int)file.Length,
                        UploadedOn = DateTime.UtcNow
                    });
                }

                if (toInsert.Count == 0)
                    return BadRequest(new gigoModels { Success = false, Message = "All uploaded files were empty." });

                var result = await _gigoService.InsertVehicleDetailAsync(toInsert);
                return Ok(result); // List<gigoModels>
            }
            catch (Exception ex)
            {
                return BadRequest(new gigoModels { Success = false, Message = ex.Message });
            }
        }


        [HttpPost("InsertVendorDocument")]
        public async Task<IActionResult> InsertVendorDocument([FromBody] VendorDocument model)
        {
            if (model == null)
            {
                return BadRequest("Invalid vendor document request");
            }
            try
            {
                var response = await _gigoService.InsertVendorDocumentAsync(model);
                if (response.Success) { return Ok(response); }
                else
                {
                    return BadRequest(new gigoModels
                    {
                        Success = false,
                        Message = "Error" + response.Message
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error inserting vendor document.");
                return StatusCode(500, "Database error occurred.");
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting vendor Details");
                return StatusCode(500, "Internal server error");

            }

        }

        [HttpPost("InsertGateEntryMaster")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> InsertGateEntryMaster()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var requestJson = form["gateEntry"];
                if (string.IsNullOrWhiteSpace(requestJson))
                {
                    return BadRequest(new gigoModels
                    {
                        Success = false,
                        Message = "Missing gate entry JSON data"
                    });
                }

                Console.WriteLine($"📥 Received JSON: {requestJson}");

                // ✅ Use your existing JsonConvert approach
                var model = JsonConvert.DeserializeObject<GateEntryMasterRequest>(requestJson);

                Console.WriteLine("🔍 CONTROLLER DETAILED DEBUG:");
                Console.WriteLine($"Vehicle No: {model.VehicleNo}");
                Console.WriteLine($"Driver Name: {model.DriverName}");
                Console.WriteLine($"Total FormData Files: {form.Files.Count}");

                // Log all received files for debugging
                foreach (var file in form.Files)
                {
                    Console.WriteLine($"📁 Received file - Name: {file.Name}, FileName: {file.FileName}, Size: {file.Length}");
                }

                // ✅ FIXED: Handle Vehicle Attachment SEPARATELY
                VehicleAttachmentInfo? vehicleAttachment = null;
                var vehicleFile = form.Files.FirstOrDefault(f => f.Name == "vehicle_attachment");

                if (vehicleFile != null && vehicleFile.Length > 0)
                {
                    Console.WriteLine($"🚛 VEHICLE FILE FOUND: {vehicleFile.FileName} ({vehicleFile.Length} bytes)");

                    try
                    {
                        var uploadPath = Path.Combine("wwwroot", "Uploads", "GIGO", "Vehicle");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        var ext = Path.GetExtension(vehicleFile.FileName);
                        var newVehicleFileName = $"{Guid.NewGuid()}{ext}";
                        var vehicleSavePath = Path.Combine(uploadPath, newVehicleFileName);

                        using var vehicleStream = new FileStream(vehicleSavePath, FileMode.Create);
                        await vehicleFile.CopyToAsync(vehicleStream);

                        vehicleAttachment = new VehicleAttachmentInfo
                        {
                            FileName = newVehicleFileName,
                            FileType = ext.Replace(".", ""),
                            FileSize = vehicleFile.Length,
                            UploadedBy = model.CreatedBy,
                            AttachmentCategory = "VEHICLE_DOCUMENT"
                        };

                        Console.WriteLine($"🚛 Vehicle attachment saved: {newVehicleFileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error saving vehicle file: {ex.Message}");
                        return StatusCode(500, new gigoModels
                        {
                            Success = false,
                            Message = $"Failed to save vehicle attachment: {ex.Message}"
                        });
                    }
                }
                else
                {
                    Console.WriteLine("❌ No vehicle attachment received");
                }

                // ✅ FIXED: Handle General Attachments SEPARATELY
                var generalAttachments = new List<GeneralAttachmentInfo>();
                var generalFiles = form.Files.Where(f => f.Name.StartsWith("general_attachment_")).ToList();

                if (generalFiles.Count > 0)
                {
                    Console.WriteLine($"📎 GENERAL FILES FOUND: {generalFiles.Count} files");

                    try
                    {
                        var uploadPath = Path.Combine("wwwroot", "Uploads", "GIGO", "General");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        for (int i = 0; i < generalFiles.Count; i++)
                        {
                            var file = generalFiles[i];
                            Console.WriteLine($"📎 Processing general file {i + 1}: {file.FileName} ({file.Length} bytes)");

                            var ext = Path.GetExtension(file.FileName);
                            var newGeneralFileName = $"{Guid.NewGuid()}{ext}";
                            var generalSavePath = Path.Combine(uploadPath, newGeneralFileName);

                            using var generalStream = new FileStream(generalSavePath, FileMode.Create);
                            await file.CopyToAsync(generalStream);

                            var generalAttachment = new GeneralAttachmentInfo
                            {
                                FileName = newGeneralFileName,
                                FileType = ext.Replace(".", ""),
                                FileSize = file.Length,
                                UploadedBy = model.CreatedBy,
                                AttachmentCategory = "GENERAL_DOCUMENT",
                                Index = i
                            };

                            generalAttachments.Add(generalAttachment);
                            Console.WriteLine($"📎 General attachment {i + 1} saved: {newGeneralFileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error saving general files: {ex.Message}");
                        return StatusCode(500, new gigoModels
                        {
                            Success = false,
                            Message = $"Failed to save general attachments: {ex.Message}"
                        });
                    }
                }
                else
                {
                    Console.WriteLine("❌ No general attachments received");
                }

                // ✅ FIXED: Set the correct properties on model
                model.VehicleAttachment = vehicleAttachment;
                model.GeneralAttachments = generalAttachments.Count > 0 ? generalAttachments : null;

                // ✅ IMPORTANT: Don't set old Attachments property
                // model.Attachments = null; // This is not used anymore

                Console.WriteLine("📊 FINAL CONTROLLER DATA SUMMARY:");
                Console.WriteLine($"   - Vehicle Attachment: {(model.VehicleAttachment != null ? $"✅ {model.VehicleAttachment.FileName}" : "❌ None")}");
                Console.WriteLine($"   - General Attachments: {model.GeneralAttachments?.Count ?? 0}");
                if (model.GeneralAttachments?.Count > 0)
                {
                    for (int i = 0; i < model.GeneralAttachments.Count; i++)
                    {
                        Console.WriteLine($"     {i + 1}. {model.GeneralAttachments[i].FileName}");
                    }
                }
                Console.WriteLine($"   - VendorDocuments: {model.VendorDocuments?.Count ?? 0}");
                Console.WriteLine($"   - CustomerDocuments: {model.CustomerDocuments?.Count ?? 0}");
                Console.WriteLine($"   - BSTDocuments: {model.BSTDocuments?.Count ?? 0}");

                // Call the service
                var result = await _gigoService.InsertGateEntryMasterAsync(model);

                if (result.Success)
                {
                    Console.WriteLine($"✅ CONTROLLER SUCCESS: Gate entry saved with ID {result.GateEntryID}");
                    Console.WriteLine($"   Final Summary:");
                    Console.WriteLine($"   - Vehicle table entries: {(model.VehicleAttachment != null ? 1 : 0)}");
                    Console.WriteLine($"   - General attachment table entries: {model.GeneralAttachments?.Count ?? 0}");

                    return Ok(result);
                }
                else
                {
                    Console.WriteLine($"❌ CONTROLLER FAILED: Service returned failure: {result.Message}");
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CONTROLLER ERROR: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");

                return StatusCode(500, new gigoModels
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        [HttpGet("GetPOBasedOnDocNum")]
        public async Task<IActionResult> GetPOData(int company, int docNum)
        {
            if (company <= 0)
                return BadRequest(new GetApiResponse<object>
                {
                    Success = false,
                    Message = "Company is required."
                });

            if (docNum <= 0)
                return BadRequest(new GetApiResponse<object>
                {
                    Success = false,
                    Message = "DocNum is required."
                });

            try
            {
                // 1. Fetch SAP POs
                var sapPoList = await _gigoService.GetPODataAsync(company, docNum);

                if (sapPoList == null || !sapPoList.Any())
                {
                    return NotFound(new GetApiResponse<object>
                    {
                        Success = false,
                        Message = $"No Purchase Orders found for DocNum {docNum} in company {company}."
                    });
                }

                return Ok(new GetApiResponse<List<PurchaseOrderModel>>
                {
                    Success = true,
                    Message = "Purchase Orders fetched successfully.",
                    Data = sapPoList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new GetApiResponse<object>
                {
                    Success = false,
                    Message = $"Error fetching Purchase Orders: {ex.Message}"
                });
            }
        }
    }
}
