using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Sap.Data.Hana;
using ServiceStack;
using System.ComponentModel.Design;
using System.Data;
using System.Net.Mail;
using TicketSystem.Models;

namespace JSAPNEW.Services.Implementation
{
    public class GIGOService : IGIGOService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly IBom2Service _bom2Service;
        public GIGOService(IConfiguration configuration, IBom2Service bom2Service)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _bom2Service = bom2Service;
        }

        public async Task<GateEntryResponse> InsertGateEntryAsync(GateEntryModel model)
        {
            var response = new GateEntryResponse();
            try
            {
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (var cmd = new SqlCommand("[giop].[InsertGateEntry]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@EntryType", model.EntryType);
                    cmd.Parameters.AddWithValue("@EntryDate", model.EntryDate);
                    cmd.Parameters.AddWithValue("@PartyID", model.PartyID);
                    cmd.Parameters.AddWithValue("@DocumentType", model.DocumentType);
                    cmd.Parameters.AddWithValue("@Remarks", model.Remarks);
                    cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);

                    var outputParam = new SqlParameter("@NewGateEntryID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.NewGateEntryID = (int?)outputParam.Value;
                    response.Message = "Gate entry inserted successfully.";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        public async Task<IEnumerable<gigoModels>> InsertAttachmentAsync(AddAttachmentModel model)
        {
            var resp = new List<gigoModels>();

            if (model.GateEntryID <= 0)
            {
                resp.Add(new gigoModels { Success = false, Message = "GateEntryID must be > 0." });
                return resp;
            }

            if (model.Attachments == null || model.Attachments.Count == 0)
            {
                resp.Add(new gigoModels { Success = false, Message = "No files to process." });
                return resp;
            }

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                // Create DataTable for table-valued parameter
                var attachmentTable = new DataTable();
                attachmentTable.Columns.Add("FileName", typeof(string));
                attachmentTable.Columns.Add("FileType", typeof(string));
                attachmentTable.Columns.Add("FileSize", typeof(long));
                attachmentTable.Columns.Add("UploadedBy", typeof(int));

                // Populate DataTable with file data
                foreach (var file in model.Attachments)
                {
                    attachmentTable.Rows.Add(
                        file.FileName,
                        file.FileType,
                        file.FileSize,
                        file.UploadedBy
                    );
                }

                // Call stored procedure with table-valued parameter
                using var cmd = new SqlCommand("giop.InsertAttachment", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add(new SqlParameter("@GateEntryID", model.GateEntryID));

                var attachmentParam = new SqlParameter("@Attachments", SqlDbType.Structured)
                {
                    TypeName = "giop.AttachmentItemType",
                    Value = attachmentTable
                };
                cmd.Parameters.Add(attachmentParam);

                await cmd.ExecuteNonQueryAsync();

                resp.Add(new gigoModels { Success = true, Message = "Attachments saved successfully." });
            }
            catch (SqlException ex)
            {
                resp.Add(new gigoModels { Success = false, Message = $"SQL error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                resp.Add(new gigoModels { Success = false, Message = $"Unexpected error: {ex.Message}" });
            }

            return resp;
        }
        public async Task<List<gigoModels>> InsertVehicleDetailAsync(IEnumerable<VehicleDetails> models)
        {
            var results = new List<gigoModels>();

            await using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var m in models)
                {
                    await using var cmd = new SqlCommand("[giop].[InsertVehicleDetails]", conn, (SqlTransaction)tx)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.Add("@GateEntryID", SqlDbType.Int).Value = m.GateEntryID;
                    cmd.Parameters.Add("@VehicleNo", SqlDbType.VarChar, 50).Value = (object?)m.VehicleNo ?? DBNull.Value;
                    cmd.Parameters.Add("@VRefNo", SqlDbType.VarChar, 100).Value = (object?)m.VRefNo ?? DBNull.Value;
                    cmd.Parameters.Add("@DriverName", SqlDbType.VarChar, 100).Value = (object?)m.DriverName ?? DBNull.Value;
                    cmd.Parameters.Add("@DriverNumber", SqlDbType.VarChar, 20).Value = (object?)m.DriverNumber ?? DBNull.Value;
                    cmd.Parameters.Add("@VendorBiltyNo", SqlDbType.VarChar, 50).Value = (object?)m.VendorBiltyNo ?? DBNull.Value;
                    cmd.Parameters.Add("@TransporterName", SqlDbType.VarChar, 100).Value = (object?)m.TransporterName ?? DBNull.Value;
                    cmd.Parameters.Add("@DocumentRemarks", SqlDbType.NVarChar, -1).Value = (object?)m.DocumentRemarks ?? DBNull.Value;
                    cmd.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = m.CreatedBy;

                    cmd.Parameters.Add("@FileName", SqlDbType.VarChar, 255).Value = (object?)m.FileName ?? DBNull.Value;
                    cmd.Parameters.Add("@FileType", SqlDbType.VarChar, 50).Value = (object?)m.FileType ?? DBNull.Value;
                    cmd.Parameters.Add("@FileSize", SqlDbType.Int).Value = m.FileSize;
                    cmd.Parameters.Add("@UploadedOn", SqlDbType.DateTime).Value = m.UploadedOn;

                    await cmd.ExecuteNonQueryAsync();

                    results.Add(new gigoModels
                    {
                        Success = true,
                        Message = "Vehicle detail inserted."
                    });
                }

                await tx.CommitAsync();
                return results;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new List<gigoModels>
        {
            new gigoModels { Success = false, Message = ex.Message }
        };
            }
        }

        public async Task<gigoModels> InsertVendorDocumentAsync(VendorDocument model)
        {
            var response = new gigoModels();
            try
            {
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (var cmd = new SqlCommand("[giop].[InsertVendorDocument]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@GateEntryID", model.GateEntryID);
                    var table = new DataTable();
                    table.Columns.Add("DocType", typeof(string));
                    table.Columns.Add("PONumber", typeof(string));
                    table.Columns.Add("ItemCode", typeof(string));
                    table.Columns.Add("POQty", typeof(decimal));
                    table.Columns.Add("ReceivedQty", typeof(decimal));
                    table.Columns.Add("OpenQty", typeof(decimal));
                    table.Columns.Add("Remarks", typeof(string));
                    table.Columns.Add("CreatedBy", typeof(int));
                    foreach (var item in model.Items)
                    {
                        table.Rows.Add(item.DocType, item.PONumber, item.ItemCode, item.POQty, item.ReceivedQty, item.OpenQty, item.Remarks, item.CreatedBy);
                    }
                    var itemsParam = new SqlParameter("@Items", SqlDbType.Structured);
                    {
                        itemsParam.TypeName = "giop.VendorDocumentItemType";
                        itemsParam.Value = table;
                    }
                    cmd.Parameters.Add(itemsParam);
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "Vendor document inserted successfully.";

                }
                ;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;


        }
        public async Task<GateEntryMasterResult> InsertGateEntryMasterAsync(GateEntryMasterRequest model)
        {
            var response = new GateEntryMasterResult();

            try
            {
                await using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                await using var transaction = await conn.BeginTransactionAsync();

                try
                {
                    // Step 1: Insert Gate Entry Master using existing procedure
                    int gateEntryId = 0;

                    await using (var cmd = new SqlCommand("[giop].[InsertGateEntry]", conn, (SqlTransaction)transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@EntryType", model.EntryType ?? "Vendor");
                        cmd.Parameters.AddWithValue("@EntryDate", model.EntryDate);
                        cmd.Parameters.AddWithValue("@PartyID", model.PartyID ?? "");
                        cmd.Parameters.AddWithValue("@DocumentType", model.DocumentType ?? "PO");
                        cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);

                        var outputParam = new SqlParameter("@NewGateEntryID", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputParam);

                        await cmd.ExecuteNonQueryAsync();
                        gateEntryId = (int)outputParam.Value;
                    }

                    Console.WriteLine($"✅ Gate Entry created with ID: {gateEntryId}");

                    // Step 1.5: Verify Gate Entry was created
                    await using (var verifyCmd = new SqlCommand("SELECT COUNT(*) FROM giop.jsGateEntry WHERE GateEntryID = @GateEntryID", conn, (SqlTransaction)transaction))
                    {
                        verifyCmd.Parameters.AddWithValue("@GateEntryID", gateEntryId);
                        var count = (int)await verifyCmd.ExecuteScalarAsync();
                        Console.WriteLine($"🔍 Gate Entry verification: {count} records found for ID {gateEntryId}");

                        if (count == 0)
                        {
                            throw new Exception($"Gate Entry ID {gateEntryId} was not found in giop.jsGateEntry table after insertion");
                        }
                    }

                    // FIXED Step 2: Insert Vehicle Details with ONLY Vehicle Attachment (if exists)
                    if (!string.IsNullOrEmpty(model.VehicleNo) || !string.IsNullOrEmpty(model.DriverName))
                    {
                        Console.WriteLine($"🚛 Starting Vehicle Details insertion...");

                        // FIXED: Create only ONE vehicle detail record, optionally with vehicle attachment
                        var vehicleDetail = new VehicleDetails
                        {
                            GateEntryID = gateEntryId,
                            VehicleNo = model.VehicleNo ?? "",
                            VRefNo = model.VRefNo ?? "",
                            DriverName = model.DriverName ?? "",
                            DriverNumber = model.DriverNumber ?? "",
                            VendorBiltyNo = model.VendorBiltyNo ?? "",
                            TransporterName = model.TransporterName ?? "",
                            DocumentRemarks = model.DocumentRemarks ?? "",
                            CreatedBy = model.CreatedBy,
                            UploadedOn = DateTime.UtcNow
                        };

                        // FIXED: Handle vehicle attachment separately (if exists)
                        if (model.VehicleAttachment != null)
                        {
                            Console.WriteLine($"🚛 Vehicle attachment found: {model.VehicleAttachment.FileName}");

                            // Extract file extension properly
                            string fileExtension = "";
                            if (!string.IsNullOrEmpty(model.VehicleAttachment.FileType))
                            {
                                if (model.VehicleAttachment.FileType.Contains("/"))
                                {
                                    // Convert MIME type to extension
                                    var mimeToExt = new Dictionary<string, string>
                            {
                                {"image/jpeg", "jpg"},
                                {"image/jpg", "jpg"},
                                {"image/png", "png"},
                                {"application/pdf", "pdf"},
                                {"application/msword", "doc"},
                                {"application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"}
                            };
                                    mimeToExt.TryGetValue(model.VehicleAttachment.FileType.ToLower(), out fileExtension);
                                }
                                else
                                {
                                    fileExtension = model.VehicleAttachment.FileType.TrimStart('.');
                                }
                            }

                            vehicleDetail.FileName = model.VehicleAttachment.FileName ?? "";
                            vehicleDetail.FileType = fileExtension ?? "";
                            vehicleDetail.FileSize = (int)Math.Min(model.VehicleAttachment.FileSize, int.MaxValue);

                            Console.WriteLine($"🚛 Vehicle attachment prepared: File={vehicleDetail.FileName}, Type={fileExtension}, Size={vehicleDetail.FileSize}");
                        }
                        else
                        {
                            Console.WriteLine($"🚛 No vehicle attachment provided");
                            vehicleDetail.FileName = "";
                            vehicleDetail.FileType = "";
                            vehicleDetail.FileSize = 0;
                        }

                        // Insert the single vehicle detail record
                        try
                        {
                            await using var vehicleCmd = new SqlCommand("[giop].[InsertVehicleDetails]", conn, (SqlTransaction)transaction)
                            {
                                CommandType = CommandType.StoredProcedure,
                                CommandTimeout = 30
                            };

                            vehicleCmd.Parameters.Add("@GateEntryID", SqlDbType.Int).Value = vehicleDetail.GateEntryID;
                            vehicleCmd.Parameters.Add("@VehicleNo", SqlDbType.VarChar, 50).Value = vehicleDetail.VehicleNo ?? "";
                            vehicleCmd.Parameters.Add("@VRefNo", SqlDbType.VarChar, 100).Value = vehicleDetail.VRefNo ?? "";
                            vehicleCmd.Parameters.Add("@DriverName", SqlDbType.VarChar, 100).Value = vehicleDetail.DriverName ?? "";
                            vehicleCmd.Parameters.Add("@DriverNumber", SqlDbType.VarChar, 20).Value = vehicleDetail.DriverNumber ?? "";
                            vehicleCmd.Parameters.Add("@VendorBiltyNo", SqlDbType.VarChar, 50).Value = vehicleDetail.VendorBiltyNo ?? "";
                            vehicleCmd.Parameters.Add("@TransporterName", SqlDbType.VarChar, 100).Value = vehicleDetail.TransporterName ?? "";
                            vehicleCmd.Parameters.Add("@DocumentRemarks", SqlDbType.NVarChar, -1).Value = vehicleDetail.DocumentRemarks ?? "";
                            vehicleCmd.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = vehicleDetail.CreatedBy;

                            // FIXED: Proper file parameter handling
                            vehicleCmd.Parameters.Add("@FileName", SqlDbType.VarChar, 255).Value =
                                string.IsNullOrEmpty(vehicleDetail.FileName) ? DBNull.Value : vehicleDetail.FileName;
                            vehicleCmd.Parameters.Add("@FileType", SqlDbType.VarChar, 50).Value =
                                string.IsNullOrEmpty(vehicleDetail.FileType) ? DBNull.Value : vehicleDetail.FileType;
                            vehicleCmd.Parameters.Add("@FileSize", SqlDbType.Int).Value = vehicleDetail.FileSize;
                            vehicleCmd.Parameters.Add("@UploadedOn", SqlDbType.DateTime).Value = vehicleDetail.UploadedOn;

                            Console.WriteLine($"🚛 Inserting vehicle detail:");
                            Console.WriteLine($"     GateEntryID: {vehicleDetail.GateEntryID}");
                            Console.WriteLine($"     VehicleNo: {vehicleDetail.VehicleNo}");
                            Console.WriteLine($"     DriverName: {vehicleDetail.DriverName}");
                            Console.WriteLine($"     FileName: {vehicleDetail.FileName}");
                            Console.WriteLine($"     FileType: {vehicleDetail.FileType}");
                            Console.WriteLine($"     FileSize: {vehicleDetail.FileSize}");

                            await vehicleCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"✅ Vehicle detail inserted successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error inserting vehicle detail: {ex.Message}");
                            throw new Exception($"Failed to insert vehicle detail: {ex.Message}");
                        }
                    }

                    // Step 3: Insert Vendor Documents if any
                    if (model.VendorDocuments?.Count > 0)
                    {
                        try
                        {
                            Console.WriteLine($"📋 Starting Vendor Documents insertion for {model.VendorDocuments.Count} documents...");

                            await using var vendorCmd = new SqlCommand("[giop].[InsertVendorDocument]", conn, (SqlTransaction)transaction)
                            {
                                CommandType = CommandType.StoredProcedure,
                                CommandTimeout = 30
                            };

                            vendorCmd.Parameters.AddWithValue("@GateEntryID", gateEntryId);

                            var vendorTable = new DataTable();
                            vendorTable.Columns.Add("DocType", typeof(string));
                            vendorTable.Columns.Add("PONumber", typeof(string));
                            vendorTable.Columns.Add("ItemCode", typeof(string));
                            vendorTable.Columns.Add("POQty", typeof(decimal));
                            vendorTable.Columns.Add("ReceivedQty", typeof(decimal));
                            vendorTable.Columns.Add("OpenQty", typeof(decimal));
                            vendorTable.Columns.Add("Remarks", typeof(string));
                            vendorTable.Columns.Add("CreatedBy", typeof(int));

                            foreach (var item in model.VendorDocuments)
                            {
                                Console.WriteLine($"📋 Adding vendor doc: {item.ItemCode}, PONumber={item.PONumber}, POQty={item.POQty}, ReceivedQty={item.ReceivedQty}");
                                vendorTable.Rows.Add(
                                    item.DocType ?? "Vendor",
                                    item.PONumber ?? "",
                                    item.ItemCode ?? "",
                                    item.POQty,
                                    item.ReceivedQty,
                                    item.OpenQty,
                                    item.Remarks ?? "",
                                    item.CreatedBy
                                );
                            }

                            var vendorParam = new SqlParameter("@Items", SqlDbType.Structured)
                            {
                                TypeName = "giop.VendorDocumentItemType",
                                Value = vendorTable
                            };
                            vendorCmd.Parameters.Add(vendorParam);

                            Console.WriteLine($"📋 Executing vendor document insertion with {vendorTable.Rows.Count} rows");
                            await vendorCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"✅ {model.VendorDocuments.Count} Vendor documents inserted successfully");
                        }
                        catch (SqlException sqlEx)
                        {
                            Console.WriteLine($"❌ SQL Error inserting Vendor documents: {sqlEx.Message}");
                            Console.WriteLine($"❌ SQL Error Number: {sqlEx.Number}, State: {sqlEx.State}, Severity: {sqlEx.Class}");
                            throw new Exception($"SQL Error inserting vendor documents: {sqlEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error inserting Vendor documents: {ex.Message}");
                            throw new Exception($"Failed to insert vendor documents: {ex.Message}");
                        }
                    }

                    // Step 4: Insert Customer Documents if any
                    if (model.CustomerDocuments?.Count > 0)
                    {
                        try
                        {
                            Console.WriteLine($"👥 Starting Customer Documents insertion for {model.CustomerDocuments.Count} documents...");

                            await using var customerCmd = new SqlCommand("[giop].[InsertCustomerDocument]", conn, (SqlTransaction)transaction)
                            {
                                CommandType = CommandType.StoredProcedure,
                                CommandTimeout = 30
                            };

                            customerCmd.Parameters.AddWithValue("@GateEntryID", gateEntryId);

                            var customerTable = new DataTable();
                            customerTable.Columns.Add("DocType", typeof(string));
                            customerTable.Columns.Add("DocId", typeof(string));
                            customerTable.Columns.Add("ItemCode", typeof(string));
                            customerTable.Columns.Add("ItemName", typeof(string));
                            customerTable.Columns.Add("Qty", typeof(decimal));
                            customerTable.Columns.Add("ReceivedQty", typeof(decimal));
                            customerTable.Columns.Add("Remarks", typeof(string));
                            customerTable.Columns.Add("CreatedBy", typeof(int));

                            foreach (var item in model.CustomerDocuments)
                            {
                                customerTable.Rows.Add(
                                    item.DocType ?? "Customer",
                                    item.DocId ?? "",
                                    item.ItemCode ?? "",
                                    item.Qty,
                                    item.ReceivedQty,
                                    item.Remarks ?? "",
                                    item.CreatedBy
                                );
                            }

                            var customerParam = new SqlParameter("@Items", SqlDbType.Structured)
                            {
                                TypeName = "giop.CustomerDocumentItemType",
                                Value = customerTable
                            };
                            customerCmd.Parameters.Add(customerParam);

                            await customerCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"✅ {model.CustomerDocuments.Count} Customer documents inserted successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error inserting Customer documents: {ex.Message}");
                            throw new Exception($"Failed to insert customer documents: {ex.Message}");
                        }
                    }

                    // Step 5: Insert BST Documents if any
                    if (model.BSTDocuments?.Count > 0)
                    {
                        try
                        {
                            Console.WriteLine($"🔄 Starting BST Documents insertion for {model.BSTDocuments.Count} documents...");

                            await using var bstCmd = new SqlCommand("[giop].[InsertBSTDocument]", conn, (SqlTransaction)transaction)
                            {
                                CommandType = CommandType.StoredProcedure,
                                CommandTimeout = 30
                            };

                            bstCmd.Parameters.AddWithValue("@GateEntryID", gateEntryId);

                            var bstTable = new DataTable();
                            bstTable.Columns.Add("DocType", typeof(string));
                            bstTable.Columns.Add("DocId", typeof(string));
                            bstTable.Columns.Add("ItemCode", typeof(string));
                            bstTable.Columns.Add("ItemName", typeof(string));
                            bstTable.Columns.Add("FromWarehouse", typeof(string));
                            bstTable.Columns.Add("ToWarehouse", typeof(string));
                            bstTable.Columns.Add("Qty", typeof(decimal));
                            bstTable.Columns.Add("QtyReceived", typeof(decimal));
                            bstTable.Columns.Add("Remarks", typeof(string));
                            bstTable.Columns.Add("CreatedBy", typeof(int));

                            foreach (var item in model.BSTDocuments)
                            {
                                bstTable.Rows.Add(
                                    item.DocType ?? "BST",
                                    item.DocId ?? "",
                                    item.ItemCode ?? "",
                                    item.FromWarehouse ?? "",
                                    item.ToWarehouse ?? "",
                                    item.Qty,
                                    item.QtyReceived,
                                    item.Remarks ?? "",
                                    item.CreatedBy
                                );
                            }

                            var bstParam = new SqlParameter("@Items", SqlDbType.Structured)
                            {
                                TypeName = "giop.BSTDocumentItemType",
                                Value = bstTable
                            };
                            bstCmd.Parameters.Add(bstParam);

                            await bstCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"✅ {model.BSTDocuments.Count} BST documents inserted successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error inserting BST documents: {ex.Message}");
                            throw new Exception($"Failed to insert BST documents: {ex.Message}");
                        }
                    }

                    // FIXED Step 6: Insert ONLY General Attachments (separate from vehicle)
                    if (model.GeneralAttachments?.Count > 0)
                    {
                        try
                        {
                            Console.WriteLine($"📎 Starting General Attachments insertion for {model.GeneralAttachments.Count} files...");

                            await using var attachCmd = new SqlCommand("giop.InsertAttachment", conn, (SqlTransaction)transaction)
                            {
                                CommandType = CommandType.StoredProcedure,
                                CommandTimeout = 30
                            };

                            attachCmd.Parameters.AddWithValue("@GateEntryID", gateEntryId);

                            var attachmentTable = new DataTable();
                            attachmentTable.Columns.Add("FileName", typeof(string));
                            attachmentTable.Columns.Add("FileType", typeof(string));
                            attachmentTable.Columns.Add("FileSize", typeof(long));
                            attachmentTable.Columns.Add("UploadedBy", typeof(int));

                            foreach (var attachment in model.GeneralAttachments)
                            {
                                Console.WriteLine($"📎 Adding general attachment: {attachment.FileName}");
                                attachmentTable.Rows.Add(
                                    attachment.FileName ?? "",
                                    attachment.FileType ?? "",
                                    attachment.FileSize,
                                    attachment.UploadedBy
                                );
                            }

                            var attachmentParam = new SqlParameter("@Attachments", SqlDbType.Structured)
                            {
                                TypeName = "giop.AttachmentItemType",
                                Value = attachmentTable
                            };
                            attachCmd.Parameters.Add(attachmentParam);

                            await attachCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"✅ {model.GeneralAttachments.Count} general attachments inserted successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error inserting General Attachments: {ex.Message}");
                            throw new Exception($"Failed to insert general attachments: {ex.Message}");
                        }
                    }

                    // Commit the transaction
                    await transaction.CommitAsync();

                    response.Success = true;
                    response.GateEntryID = gateEntryId;
                    response.Message = $"Gate entry master created successfully with ID: {gateEntryId}";

                    Console.WriteLine($"✅ TRANSACTION COMMITTED: Gate Entry Master saved with ID: {gateEntryId}");
                    Console.WriteLine($"   - Vehicle Details: {(!string.IsNullOrEmpty(model.VehicleNo) ? "✅" : "❌")} (Vehicle Attachment: {(model.VehicleAttachment != null ? "✅" : "❌")})");
                    Console.WriteLine($"   - Vendor Documents: {model.VendorDocuments?.Count ?? 0}");
                    Console.WriteLine($"   - Customer Documents: {model.CustomerDocuments?.Count ?? 0}");
                    Console.WriteLine($"   - BST Documents: {model.BSTDocuments?.Count ?? 0}");
                    Console.WriteLine($"   - General Attachments: {model.GeneralAttachments?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ TRANSACTION ROLLED BACK: {ex.Message}");
                    await transaction.RollbackAsync();
                    throw; // Re-throw to be caught by outer catch
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                Console.WriteLine($"❌ Error in InsertGateEntryMasterAsync: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
            }

            return response;
        }

        // Insert Customer Document method
        public async Task<gigoModels> InsertCustomerDocumentAsync(CustomerDocument model)
        {
            var response = new gigoModels();
            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand("[giop].[InsertCustomerDocument]", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@GateEntryID", model.GateEntryID);

                var table = new DataTable();
                table.Columns.Add("DocType", typeof(string));
                table.Columns.Add("DocId", typeof(string));
                table.Columns.Add("ItemCode", typeof(string));
                table.Columns.Add("ItemName", typeof(string));
                table.Columns.Add("Qty", typeof(decimal));
                table.Columns.Add("ReceivedQty", typeof(decimal));
                table.Columns.Add("Remarks", typeof(string));
                table.Columns.Add("CreatedBy", typeof(int));

                foreach (var item in model.Items)
                {
                    table.Rows.Add(item.DocType, item.DocId, item.ItemCode,
                                 item.Qty, item.ReceivedQty, item.Remarks, item.CreatedBy);
                }

                var itemsParam = new SqlParameter("@Items", SqlDbType.Structured)
                {
                    TypeName = "giop.CustomerDocumentItemType",
                    Value = table
                };
                cmd.Parameters.Add(itemsParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                response.Success = true;
                response.Message = "Customer document inserted successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        // Insert BST Document method  
        public async Task<gigoModels> InsertBSTDocumentAsync(BSTDocument model)
        {
            var response = new gigoModels();
            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand("[giop].[InsertBSTDocument]", conn);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@GateEntryID", model.GateEntryID);

                var table = new DataTable();
                table.Columns.Add("DocType", typeof(string));
                table.Columns.Add("DocId", typeof(string));
                table.Columns.Add("ItemCode", typeof(string));
                table.Columns.Add("ItemName", typeof(string));
                table.Columns.Add("FromWarehouse", typeof(string));
                table.Columns.Add("ToWarehouse", typeof(string));
                table.Columns.Add("Qty", typeof(decimal));
                table.Columns.Add("QtyReceived", typeof(decimal));
                table.Columns.Add("Remarks", typeof(string));
                table.Columns.Add("CreatedBy", typeof(int));

                foreach (var item in model.Items)
                {
                    table.Rows.Add(item.DocType, item.DocId, item.ItemCode,
                                 item.FromWarehouse, item.ToWarehouse, item.Qty,
                                 item.QtyReceived, item.Remarks, item.CreatedBy);
                }

                var itemsParam = new SqlParameter("@Items", SqlDbType.Structured)
                {
                    TypeName = "giop.BSTDocumentItemType",
                    Value = table
                };
                cmd.Parameters.Add(itemsParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                response.Success = true;
                response.Message = "BST document inserted successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<List<PurchaseOrderModel>> GetPODataAsync(int company, int docNum)
        {
            // Step 1: Get SAP session depending on company
            SAPSessionModel session;
            if (company == 1)
                session = await _bom2Service.GetSAPSessionOilAsync();
            else if (company == 2)
                session = await _bom2Service.GetSAPSessionBevAsync();
            else
                throw new ArgumentException("Invalid company value. Must be 1 (Oil) or 2 (Beverage).");

            // Step 2: Build filter parts
            var filterParts = new List<string>
            {
                "DocumentStatus eq 'bost_Open'",
                $"DocNum eq {docNum}"   // ✅ filter directly by DocNum
            };

            string filter = string.Join(" and ", filterParts);

            // Step 3: Build query string
            string query = $"PurchaseOrders?$select=DocEntry,DocNum,CardCode,CardName,DocumentStatus,DocDate,DocTotal,DocumentLines" +
                           $"&$filter={Uri.EscapeDataString(filter)}&$orderby=DocEntry";

            // Step 4: Call SAP Service Layer
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://103.89.44.112:50000/b1s/v1/");
            client.DefaultRequestHeaders.Add("Cookie", $"{session.B1Session}; {session.RouteId}");
            client.DefaultRequestHeaders.Add("B1S-PageSize", "10000");

            var response = await client.GetAsync(query);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();
            var sapData = JsonConvert.DeserializeObject<SapPurchaseOrderResponse>(json);

            // Step 5: Refine DocumentLines
            foreach (var po in sapData.Value)
            {
                po.DocumentLines = po.DocumentLines?.Select(line => new PurchaseOrderLineModel
                {
                    LineNum = line.LineNum,
                    ItemCode = line.ItemCode,
                    ItemDescription = line.ItemDescription,
                    Quantity = line.Quantity,
                    LineTotal = line.LineTotal,
                    OpenAmount = line.OpenAmount,
                    U_Remarks = line.U_Remarks,
                    RemainingOpenQuantity = line.RemainingOpenQuantity,
                    MeasureUnit = line.MeasureUnit
                }).ToList();
            }

            return sapData.Value;
        }
    }
}
