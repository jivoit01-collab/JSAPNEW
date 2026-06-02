using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Services.Implementation
{
    public class DocumentHubService : IDocumentHubService
    {
        private static readonly object StoreLock = new();
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".xlsx", ".pptx", ".png", ".jpg", ".jpeg", ".csv"
        };

        private static readonly List<DocumentHubFolderDto> Folders = new();
        private static readonly List<DocumentHubFileDto> Files = new();
        private static readonly List<DocumentHubVersionDto> Versions = new();
        private static readonly List<DocumentHubActivityDto> Activities = new();
        private static int _nextFolderId = 1;
        private static int _nextFileId = 1;
        private static int _nextVersionId = 1;
        private static int _nextActivityId = 1;
        private static bool _isSeeded;

        private readonly IWebHostEnvironment _environment;

        public DocumentHubService(IConfiguration config, IWebHostEnvironment environment)
        {
            _environment = environment;
            SeedIfNeeded();
        }

        public Task<DocumentHubPermissionDto> GetPermissionsAsync(int userId, int companyId)
        {
            return Task.FromResult(new DocumentHubPermissionDto
            {
                CanView = userId > 0,
                CanUpload = userId > 0,
                CanDelete = userId > 0,
                CanManageFolders = userId > 0
            });
        }

        public Task<DocumentHubSnapshotDto> GetSnapshotAsync(int? folderId, string? filter, string? search)
        {
            lock (StoreLock)
            {
                var normalizedFilter = (filter ?? string.Empty).Trim().ToLowerInvariant();
                var normalizedSearch = (search ?? string.Empty).Trim();

                var visibleFolders = Folders.Where(x => !x.IsDeleted).ToList();
                var visibleFiles = Files.Where(x => !x.IsDeleted).ToList();

                var childFolders = visibleFolders
                    .Where(x => folderId == null ? x.ParentFolderId == null : x.ParentFolderId == folderId)
                    .ToList();

                var files = folderId == null
                    ? new List<DocumentHubFileDto>()
                    : visibleFiles.Where(x => x.FolderId == folderId).ToList();

                if (normalizedFilter == "recent")
                {
                    childFolders = new List<DocumentHubFolderDto>();
                    files = visibleFiles.Where(x => x.UploadedDate >= DateTime.Now.AddDays(-7)).ToList();
                }
                else if (normalizedFilter == "favorites")
                {
                    childFolders = new List<DocumentHubFolderDto>();
                    files = visibleFiles.Where(x => x.IsFavorite).ToList();
                }

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    childFolders = visibleFolders
                        .Where(x => Matches(x.FolderName, normalizedSearch) || Matches(x.Department, normalizedSearch))
                        .ToList();

                    files = visibleFiles
                        .Where(x => Matches(x.FileName, normalizedSearch) || Matches(x.FileType, normalizedSearch) || Matches(x.UploadedBy, normalizedSearch))
                        .ToList();
                }

                HydrateFolderCounts(childFolders, visibleFolders, visibleFiles);

                var snapshot = new DocumentHubSnapshotDto
                {
                    Folders = childFolders.OrderBy(x => x.FolderName).Select(CloneFolder).ToList(),
                    Files = files.OrderByDescending(x => x.UploadedDate).Select(CloneFile).ToList(),
                    Breadcrumb = GetBreadcrumb(folderId, visibleFolders),
                    Stats = BuildStats(visibleFolders, visibleFiles)
                };

                return Task.FromResult(snapshot);
            }
        }

        public Task<List<DocumentHubFolderDto>> GetFoldersAsync()
        {
            lock (StoreLock)
            {
                var visibleFolders = Folders.Where(x => !x.IsDeleted).ToList();
                var visibleFiles = Files.Where(x => !x.IsDeleted).ToList();
                HydrateFolderCounts(visibleFolders, visibleFolders, visibleFiles);
                return Task.FromResult(visibleFolders.OrderBy(x => x.ParentFolderId.HasValue).ThenBy(x => x.FolderName).Select(CloneFolder).ToList());
            }
        }

        public Task<DocumentHubFolderDto?> CreateFolderAsync(DocumentHubFolderRequest request, int userId, string userName)
        {
            lock (StoreLock)
            {
                var folderName = NormalizeName(request.FolderName);
                if (string.IsNullOrWhiteSpace(folderName))
                    return Task.FromResult<DocumentHubFolderDto?>(null);

                var parent = request.ParentFolderId.HasValue
                    ? Folders.FirstOrDefault(x => x.FolderId == request.ParentFolderId && !x.IsDeleted)
                    : null;

                var folder = new DocumentHubFolderDto
                {
                    FolderId = _nextFolderId++,
                    ParentFolderId = request.ParentFolderId,
                    FolderName = folderName,
                    Department = request.Department ?? parent?.Department ?? parent?.FolderName ?? folderName,
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };

                Folders.Add(folder);
                AddActivity(null, folder.FolderId, userId, userName, "Folder Created", folder.FolderName);
                return Task.FromResult<DocumentHubFolderDto?>(CloneFolder(folder));
            }
        }

        public Task<bool> RenameFolderAsync(int folderId, string folderName, int userId, string userName)
        {
            lock (StoreLock)
            {
                var folder = Folders.FirstOrDefault(x => x.FolderId == folderId && !x.IsDeleted);
                var cleanName = NormalizeName(folderName);
                if (folder == null || string.IsNullOrWhiteSpace(cleanName))
                    return Task.FromResult(false);

                var oldName = folder.FolderName;
                folder.FolderName = cleanName;
                folder.LastUpdated = DateTime.Now;
                AddActivity(null, folder.FolderId, userId, userName, "Folder Renamed", $"{oldName} to {cleanName}");
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteFolderAsync(int folderId, int userId, string userName)
        {
            lock (StoreLock)
            {
                var folder = Folders.FirstOrDefault(x => x.FolderId == folderId && !x.IsDeleted);
                if (folder == null)
                    return Task.FromResult(false);

                var folderIds = GetDescendantFolderIds(folderId).Append(folderId).ToHashSet();
                foreach (var item in Folders.Where(x => folderIds.Contains(x.FolderId)))
                    item.IsDeleted = true;

                foreach (var file in Files.Where(x => folderIds.Contains(x.FolderId)))
                    file.IsDeleted = true;

                AddActivity(null, folderId, userId, userName, "Folder Deleted", folder.FolderName);
                return Task.FromResult(true);
            }
        }

        public async Task<DocumentHubUploadResultDto> UploadFilesAsync(DocumentHubUploadRequest request, int userId, string userName)
        {
            var result = new DocumentHubUploadResultDto { Success = true, Message = "Files uploaded successfully." };
            if (request.Files.Count == 0)
                return new DocumentHubUploadResultDto { Success = false, Message = "No files were selected." };

            Directory.CreateDirectory(GetUploadRoot());

            foreach (var formFile in request.Files)
            {
                var originalName = Path.GetFileName(formFile.FileName);
                var extension = Path.GetExtension(originalName);
                if (!AllowedExtensions.Contains(extension))
                {
                    result.Conflicts.Add($"{originalName}: unsupported file type.");
                    continue;
                }

                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(GetUploadRoot(), storedFileName);
                await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
                    await formFile.CopyToAsync(stream);

                lock (StoreLock)
                {
                    var existing = Files.FirstOrDefault(x => !x.IsDeleted && x.FolderId == request.FolderId && x.FileName.Equals(originalName, StringComparison.OrdinalIgnoreCase));
                    if (existing != null && request.ConflictAction.Equals("ask", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Conflicts.Add(originalName);
                        result.Success = false;
                        result.Message = "One or more files already exist.";
                        continue;
                    }

                    var version = existing == null ? 1 : existing.VersionNumber + 1;
                    var contentType = string.IsNullOrWhiteSpace(formFile.ContentType) ? GetContentType(extension) : formFile.ContentType;

                    if (existing != null)
                    {
                        existing.StoredFileName = storedFileName;
                        existing.ContentType = contentType;
                        existing.FileSize = formFile.Length;
                        existing.UploadedBy = userName;
                        existing.UploadedByUserId = userId;
                        existing.UploadedDate = DateTime.Now;
                        existing.VersionNumber = request.ConflictAction.Equals("replace", StringComparison.OrdinalIgnoreCase) ? existing.VersionNumber : version;
                        existing.IsConfidential = request.IsConfidential;
                        existing.FileUrl = $"/uploads/documenthub/{storedFileName}";
                        AddVersion(existing.FileId, existing.VersionNumber, storedFileName, formFile.Length, userId, userName);
                        TouchFolder(existing.FolderId);
                        AddActivity(existing.FileId, existing.FolderId, userId, userName, request.ConflictAction.Equals("replace", StringComparison.OrdinalIgnoreCase) ? "File Replaced" : "File Uploaded", originalName);
                        result.Files.Add(CloneFile(existing));
                    }
                    else
                    {
                        var file = new DocumentHubFileDto
                        {
                            FileId = _nextFileId++,
                            FolderId = request.FolderId,
                            FileName = originalName,
                            StoredFileName = storedFileName,
                            ContentType = contentType,
                            FileType = extension.TrimStart('.').ToUpperInvariant(),
                            UploadedBy = userName,
                            UploadedByUserId = userId,
                            UploadedDate = DateTime.Now,
                            FileSize = formFile.Length,
                            VersionNumber = 1,
                            IsConfidential = request.IsConfidential,
                            IsFavorite = false,
                            FileUrl = $"/uploads/documenthub/{storedFileName}"
                        };

                        Files.Add(file);
                        AddVersion(file.FileId, 1, storedFileName, formFile.Length, userId, userName);
                        TouchFolder(file.FolderId);
                        AddActivity(file.FileId, file.FolderId, userId, userName, "File Uploaded", file.FileName);
                        result.Files.Add(CloneFile(file));
                    }
                }
            }

            if (!result.Files.Any() && result.Conflicts.Any())
                result.Success = false;

            return result;
        }

        public Task<DocumentHubFileDto?> GetFileAsync(int fileId)
        {
            lock (StoreLock)
            {
                return Task.FromResult(Files.Where(x => !x.IsDeleted).Select(CloneFile).FirstOrDefault(x => x.FileId == fileId));
            }
        }

        public Task<bool> RenameFileAsync(int fileId, string fileName, int userId, string userName)
        {
            lock (StoreLock)
            {
                var file = Files.FirstOrDefault(x => x.FileId == fileId && !x.IsDeleted);
                var cleanName = NormalizeFileName(fileName);
                if (file == null || string.IsNullOrWhiteSpace(cleanName))
                    return Task.FromResult(false);

                var oldName = file.FileName;
                file.FileName = cleanName;
                file.FileType = Path.GetExtension(cleanName).TrimStart('.').ToUpperInvariant();
                file.UploadedDate = DateTime.Now;
                TouchFolder(file.FolderId);
                AddActivity(file.FileId, file.FolderId, userId, userName, "File Renamed", $"{oldName} to {cleanName}");
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteFileAsync(int fileId, int userId, string userName)
        {
            lock (StoreLock)
            {
                var file = Files.FirstOrDefault(x => x.FileId == fileId && !x.IsDeleted);
                if (file == null)
                    return Task.FromResult(false);

                file.IsDeleted = true;
                TouchFolder(file.FolderId);
                AddActivity(file.FileId, file.FolderId, userId, userName, "File Deleted", file.FileName);
                return Task.FromResult(true);
            }
        }

        public Task<List<DocumentHubVersionDto>> GetVersionHistoryAsync(int fileId)
        {
            lock (StoreLock)
            {
                return Task.FromResult(Versions.Where(x => x.FileId == fileId).OrderByDescending(x => x.VersionNumber).Select(CloneVersion).ToList());
            }
        }

        public Task<List<DocumentHubActivityDto>> GetActivityLogAsync(int? fileId = null)
        {
            lock (StoreLock)
            {
                var rows = Activities
                    .Where(x => fileId == null || x.FileId == fileId)
                    .OrderByDescending(x => x.ActivityDate)
                    .Take(100)
                    .Select(CloneActivity)
                    .ToList();

                return Task.FromResult(rows);
            }
        }

        public Task<bool> ValidateConfidentialAccessAsync(int fileId, int userId, string userName, string? usernameConfirmation, string? pin)
        {
            var isValid = !string.IsNullOrWhiteSpace(usernameConfirmation) && usernameConfirmation.Trim().Equals(userName, StringComparison.OrdinalIgnoreCase);
            isValid = isValid || (pin ?? string.Empty).Trim() == "1234";

            lock (StoreLock)
                AddActivity(fileId, null, userId, userName, isValid ? "Confidential Access Granted" : "Confidential Access Failed", $"File #{fileId}");

            return Task.FromResult(isValid);
        }

        public Task LogActivityAsync(int? fileId, int? folderId, int userId, string userName, string action, string? details = null)
        {
            lock (StoreLock)
                AddActivity(fileId, folderId, userId, userName, action, details);

            return Task.CompletedTask;
        }

        private static void SeedIfNeeded()
        {
            lock (StoreLock)
            {
                if (_isSeeded)
                    return;

                var invoice = AddFolder(null, "Invoice Department", "Invoice Department", DateTime.Now.AddDays(-6));
                var payment = AddFolder(null, "Payment Department", "Payment Department", DateTime.Now.AddDays(-4));
                var accounts = AddFolder(null, "Accounts", "Accounts", DateTime.Now.AddDays(-5));
                var audit = AddFolder(null, "Audit", "Audit", DateTime.Now.AddDays(-2));
                var shared = AddFolder(null, "Shared Documents", "Shared Documents", DateTime.Now.AddDays(-1));

                var invoiceSops = AddFolder(invoice.FolderId, "Invoice SOPs", "Invoice Department", DateTime.Now.AddDays(-1));
                var vendorFormats = AddFolder(invoice.FolderId, "Vendor Formats", "Invoice Department", DateTime.Now.AddDays(-2));
                var gstGuidelines = AddFolder(accounts.FolderId, "GST Guidelines", "Accounts", DateTime.Now.AddDays(-3));
                var bankFormats = AddFolder(payment.FolderId, "Bank Formats", "Payment Department", DateTime.Now.AddDays(-1));
                var auditReports = AddFolder(audit.FolderId, "Audit Reports", "Audit", DateTime.Now.AddHours(-18));

                AddFile(invoiceSops.FolderId, "Invoice_SOP_v3.pdf", "PDF", 1864200, "Priya Sharma", DateTime.Now.AddHours(-8), 3, true, true);
                AddFile(vendorFormats.FolderId, "Vendor_Master.xlsx", "XLSX", 842120, "Rohit Mehta", DateTime.Now.AddDays(-2), 1, false, true);
                AddFile(gstGuidelines.FolderId, "GST_Guidelines.pdf", "PDF", 1298890, "Accounts Team", DateTime.Now.AddDays(-3), 2, false, false);
                AddFile(bankFormats.FolderId, "Payment_Policy.docx", "DOCX", 523440, "Payment Desk", DateTime.Now.AddDays(-1), 4, false, false);
                AddFile(auditReports.FolderId, "Audit_Report_Q1.pdf", "PDF", 2391000, "Audit Team", DateTime.Now.AddHours(-18), 1, true, false);
                AddFile(shared.FolderId, "Reference_Checklist.csv", "CSV", 42040, "System", DateTime.Now.AddDays(-5), 1, false, false);

                AddActivity(null, invoice.FolderId, 0, "System", "Folder Created", "Invoice Department");
                AddActivity(null, payment.FolderId, 0, "System", "Folder Created", "Payment Department");
                AddActivity(null, shared.FolderId, 0, "System", "Folder Created", "Shared Documents");
                AddActivity(1, invoiceSops.FolderId, 0, "Priya Sharma", "File Uploaded", "Invoice_SOP_v3.pdf");
                AddActivity(5, auditReports.FolderId, 0, "Audit Team", "File Uploaded", "Audit_Report_Q1.pdf");

                HydrateFolderCounts(Folders, Folders, Files);
                _isSeeded = true;
            }
        }

        private static DocumentHubFolderDto AddFolder(int? parentFolderId, string folderName, string department, DateTime updated)
        {
            var folder = new DocumentHubFolderDto
            {
                FolderId = _nextFolderId++,
                ParentFolderId = parentFolderId,
                FolderName = folderName,
                Department = department,
                CreatedDate = updated.AddDays(-5),
                LastUpdated = updated
            };
            Folders.Add(folder);
            return folder;
        }

        private static void AddFile(int folderId, string fileName, string fileType, long fileSize, string uploadedBy, DateTime uploadedDate, int version, bool confidential, bool favorite)
        {
            var fileId = _nextFileId++;
            var extension = "." + fileType.ToLowerInvariant();
            var file = new DocumentHubFileDto
            {
                FileId = fileId,
                FolderId = folderId,
                FileName = fileName,
                StoredFileName = $"mock-{fileId}{extension}",
                ContentType = GetContentType(extension),
                FileType = fileType,
                UploadedBy = uploadedBy,
                UploadedByUserId = 0,
                UploadedDate = uploadedDate,
                FileSize = fileSize,
                VersionNumber = version,
                IsConfidential = confidential,
                IsFavorite = favorite,
                FileUrl = $"/DocumentHub/Preview?fileId={fileId}"
            };

            Files.Add(file);
            for (var i = 1; i <= version; i++)
                AddVersion(fileId, i, file.StoredFileName, fileSize - ((version - i) * 12400), 0, uploadedBy);
        }

        private static void AddVersion(int fileId, int versionNumber, string storedFileName, long fileSize, int userId, string userName)
        {
            Versions.Add(new DocumentHubVersionDto
            {
                VersionId = _nextVersionId++,
                FileId = fileId,
                VersionNumber = versionNumber,
                StoredFileName = storedFileName,
                UploadedBy = userName,
                UploadedDate = DateTime.Now.AddDays(-(versionNumber == 1 ? 5 : 0)),
                FileSize = Math.Max(fileSize, 0)
            });
        }

        private static void AddActivity(int? fileId, int? folderId, int userId, string userName, string action, string? details)
        {
            Activities.Add(new DocumentHubActivityDto
            {
                ActivityId = _nextActivityId++,
                FileId = fileId,
                FolderId = folderId,
                UserName = string.IsNullOrWhiteSpace(userName) ? "System" : userName,
                ActivityDate = DateTime.Now,
                Action = action,
                Details = details
            });
        }

        private static List<int> GetDescendantFolderIds(int parentFolderId)
        {
            var directChildren = Folders.Where(x => x.ParentFolderId == parentFolderId && !x.IsDeleted).Select(x => x.FolderId).ToList();
            return directChildren.Concat(directChildren.SelectMany(GetDescendantFolderIds)).ToList();
        }

        private static List<DocumentHubBreadcrumbDto> GetBreadcrumb(int? folderId, List<DocumentHubFolderDto> visibleFolders)
        {
            var crumbs = new List<DocumentHubBreadcrumbDto> { new() { FolderId = null, FolderName = "Document Hub" } };
            if (folderId == null)
                return crumbs;

            var path = new Stack<DocumentHubBreadcrumbDto>();
            var current = visibleFolders.FirstOrDefault(x => x.FolderId == folderId);
            while (current != null)
            {
                path.Push(new DocumentHubBreadcrumbDto { FolderId = current.FolderId, FolderName = current.FolderName });
                current = current.ParentFolderId.HasValue ? visibleFolders.FirstOrDefault(x => x.FolderId == current.ParentFolderId) : null;
            }

            crumbs.AddRange(path);
            return crumbs;
        }

        private static DocumentHubStatsDto BuildStats(List<DocumentHubFolderDto> visibleFolders, List<DocumentHubFileDto> visibleFiles)
        {
            return new DocumentHubStatsDto
            {
                TotalFiles = visibleFiles.Count,
                TotalFolders = visibleFolders.Count,
                RecentFiles = visibleFiles.Count(x => x.UploadedDate >= DateTime.Now.AddDays(-7)),
                FavoriteFiles = visibleFiles.Count(x => x.IsFavorite)
            };
        }

        private static void HydrateFolderCounts(IEnumerable<DocumentHubFolderDto> targetFolders, List<DocumentHubFolderDto> visibleFolders, List<DocumentHubFileDto> visibleFiles)
        {
            foreach (var folder in targetFolders)
            {
                folder.ChildFolderCount = visibleFolders.Count(x => x.ParentFolderId == folder.FolderId);
                var scopedFolderIds = GetDescendantFolderIds(folder.FolderId).Append(folder.FolderId).ToHashSet();
                folder.FileCount = visibleFiles.Count(x => scopedFolderIds.Contains(x.FolderId));
                folder.ItemCount = folder.ChildFolderCount + folder.FileCount;
                var lastFileDate = visibleFiles.Where(x => scopedFolderIds.Contains(x.FolderId)).Select(x => (DateTime?)x.UploadedDate).Max();
                folder.LastUpdated = new[] { folder.LastUpdated, lastFileDate }.Where(x => x.HasValue).Max();
            }
        }

        private static void TouchFolder(int folderId)
        {
            var folder = Folders.FirstOrDefault(x => x.FolderId == folderId);
            if (folder != null)
                folder.LastUpdated = DateTime.Now;
        }

        private string GetUploadRoot()
        {
            var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                : _environment.WebRootPath;

            return Path.Combine(webRoot, "uploads", "documenthub");
        }

        private static bool Matches(string? value, string search) => !string.IsNullOrWhiteSpace(value) && value.Contains(search, StringComparison.OrdinalIgnoreCase);

        private static string NormalizeName(string value) => (value ?? string.Empty).Trim().Replace("  ", " ");

        private static string NormalizeFileName(string value) => Path.GetFileName((value ?? string.Empty).Trim());

        private static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }

        private static DocumentHubFolderDto CloneFolder(DocumentHubFolderDto folder) => new()
        {
            FolderId = folder.FolderId,
            ParentFolderId = folder.ParentFolderId,
            FolderName = folder.FolderName,
            Department = folder.Department,
            ItemCount = folder.ItemCount,
            ChildFolderCount = folder.ChildFolderCount,
            FileCount = folder.FileCount,
            CreatedDate = folder.CreatedDate,
            LastUpdated = folder.LastUpdated,
            IsDeleted = folder.IsDeleted
        };

        private static DocumentHubFileDto CloneFile(DocumentHubFileDto file) => new()
        {
            FileId = file.FileId,
            FolderId = file.FolderId,
            FileName = file.FileName,
            StoredFileName = file.StoredFileName,
            ContentType = file.ContentType,
            FileType = file.FileType,
            UploadedBy = file.UploadedBy,
            UploadedByUserId = file.UploadedByUserId,
            UploadedDate = file.UploadedDate,
            FileSize = file.FileSize,
            VersionNumber = file.VersionNumber,
            IsConfidential = file.IsConfidential,
            IsFavorite = file.IsFavorite,
            FileUrl = file.FileUrl,
            IsDeleted = file.IsDeleted
        };

        private static DocumentHubVersionDto CloneVersion(DocumentHubVersionDto version) => new()
        {
            VersionId = version.VersionId,
            FileId = version.FileId,
            VersionNumber = version.VersionNumber,
            StoredFileName = version.StoredFileName,
            UploadedBy = version.UploadedBy,
            UploadedDate = version.UploadedDate,
            FileSize = version.FileSize
        };

        private static DocumentHubActivityDto CloneActivity(DocumentHubActivityDto activity) => new()
        {
            ActivityId = activity.ActivityId,
            FileId = activity.FileId,
            FolderId = activity.FolderId,
            UserName = activity.UserName,
            ActivityDate = activity.ActivityDate,
            Action = activity.Action,
            Details = activity.Details
        };
    }
}
