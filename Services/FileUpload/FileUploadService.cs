using Ideku.Configuration;
using Microsoft.Extensions.Options;

namespace Ideku.Services.FileUpload
{
    // Service for file upload operations with configurable validation rules
    public class FileUploadService : IFileUploadService
    {
        private readonly FileUploadSettings _settings;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            IOptions<FileUploadSettings> settings,
            ILogger<FileUploadService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        // Validates a collection of files for type and size constraints
        public (bool IsValid, string ErrorMessage) ValidateFiles(List<IFormFile>? files, long existingFilesTotalSize = 0)
        {
            if (files == null || !files.Any())
                return (true, string.Empty);

            long newFilesSize = 0;

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Validate file type
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_settings.AllowedExtensions.Contains(fileExtension))
                {
                    var allowedTypes = string.Join(", ", _settings.AllowedExtensions);
                    return (false, $"File type '{fileExtension}' is not allowed. Allowed types: {allowedTypes}");
                }

                // Validate individual file size
                if (file.Length > _settings.MaxFileSizeBytes)
                {
                    var fileSizeMB = (file.Length / (1024.0 * 1024.0)).ToString("F2");
                    return (false, $"File '{file.FileName}' ({fileSizeMB} MB) exceeds the maximum size of {_settings.MaxFileSizeMB} MB per file");
                }

                // Accumulate new files size
                newFilesSize += file.Length;
            }

            // Validate total size INCLUDING existing files
            var totalSize = existingFilesTotalSize + newFilesSize;
            if (totalSize > _settings.MaxTotalFileSizeBytes)
            {
                var totalSizeMB = (totalSize / (1024.0 * 1024.0)).ToString("F2");
                var existingSizeMB = (existingFilesTotalSize / (1024.0 * 1024.0)).ToString("F2");
                var newSizeMB = (newFilesSize / (1024.0 * 1024.0)).ToString("F2");

                if (existingFilesTotalSize > 0)
                {
                    return (false, $"Total file size would be {totalSizeMB} MB (existing: {existingSizeMB} MB + new: {newSizeMB} MB), which exceeds the maximum allowed total size of {_settings.MaxTotalFileSizeMB} MB");
                }
                else
                {
                    return (false, $"Total file size ({totalSizeMB} MB) exceeds the maximum allowed total size of {_settings.MaxTotalFileSizeMB} MB");
                }
            }

            return (true, string.Empty);
        }

        // Calculates total size of existing files from attachment file paths
        public long CalculateExistingFilesSize(string? attachmentFilesPath, string webRootPath)
        {
            if (string.IsNullOrEmpty(attachmentFilesPath))
                return 0;

            var filePaths = attachmentFilesPath.Split(';', StringSplitOptions.RemoveEmptyEntries);
            long totalSize = 0;

            foreach (var relativeFilePath in filePaths)
            {
                var fullPath = Path.Combine(webRootPath, relativeFilePath);
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    totalSize += fileInfo.Length;
                }
            }

            _logger.LogDebug("Calculated existing files size: {TotalSize} bytes from {FileCount} files",
                totalSize, filePaths.Length);

            return totalSize;
        }

        // Uploads files with naming convention: {ideaCode}_S{stage}_{counter}.ext or {ideaCode}_M{counter}.ext
        public async Task<List<string>> HandleFileUploadsAsync(
            List<IFormFile>? files,
            string ideaCode,
            string webRootPath,
            int? stage = null,
            int existingFilesCount = 0)
        {
            var filePaths = new List<string>();

            if (files == null || !files.Any())
                return filePaths;

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(webRootPath, "uploads", "ideas");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                _logger.LogInformation("Created uploads directory: {UploadsPath}", uploadsPath);
            }

            int fileCounter = existingFilesCount + 1;

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    // Generate filename based on context
                    string fileName;
                    if (stage.HasValue)
                    {
                        // Idea creation/approval: IdeaCode_S{stage}_{counter}.ext
                        fileName = $"{ideaCode}_S{stage.Value}_{fileCounter:D3}{fileExtension}";
                    }
                    else
                    {
                        // Monitoring: IdeaCode_M{counter}.ext
                        fileName = $"{ideaCode}_M{fileCounter:D3}{fileExtension}";
                    }

                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Store relative path
                    filePaths.Add($"uploads/ideas/{fileName}");
                    fileCounter++;

                    _logger.LogInformation("Uploaded file {FileName} for idea {IdeaCode}", fileName, ideaCode);
                }
            }

            return filePaths;
        }

        // Get count of existing files for an idea (for sequential numbering)
        public int GetExistingFilesCount(string ideaCode, string webRootPath)
        {
            var uploadsPath = Path.Combine(webRootPath, "uploads", "ideas");
            if (!Directory.Exists(uploadsPath))
            {
                return 0;
            }

            var ideaPattern = $"{ideaCode}_";
            var existingFiles = Directory.GetFiles(uploadsPath)
                .Where(f => Path.GetFileName(f).StartsWith(ideaPattern))
                .ToList();

            return existingFiles.Count;
        }
    }
}
