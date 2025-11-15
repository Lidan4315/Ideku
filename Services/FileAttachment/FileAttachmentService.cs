using Ideku.Data.Repositories;

namespace Ideku.Services.FileAttachment
{
    public class FileAttachmentService : IFileAttachmentService
    {
        private readonly IIdeaRepository _ideaRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FileAttachmentService> _logger;

        public FileAttachmentService(
            IIdeaRepository ideaRepository,
            IWebHostEnvironment webHostEnvironment,
            ILogger<FileAttachmentService> logger)
        {
            _ideaRepository = ideaRepository;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<(byte[]? FileBytes, string? ContentType, string? ErrorMessage)> GetFileForViewAsync(long ideaId, string filename)
        {
            try
            {
                // Validate and get file path from database
                var (filePath, errorMessage) = await ValidateAndGetFilePathAsync(ideaId, filename);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return (null, null, errorMessage);
                }

                // Read file
                var fileBytes = await File.ReadAllBytesAsync(filePath!);
                var contentType = GetContentType(filePath!);

                return (fileBytes, contentType, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing attachment {Filename} for Idea {IdeaId}", filename, ideaId);
                return (null, null, "Error loading file");
            }
        }

        public async Task<(byte[]? FileBytes, string? ContentType, string? Filename, string? ErrorMessage)> GetFileForDownloadAsync(long ideaId, string filename)
        {
            try
            {
                // Validate and get file path from database
                var (filePath, errorMessage) = await ValidateAndGetFilePathAsync(ideaId, filename);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return (null, null, null, errorMessage);
                }

                // Read file
                var fileBytes = await File.ReadAllBytesAsync(filePath!);
                var contentType = GetContentType(filePath!);

                return (fileBytes, contentType, filename, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment {Filename} for Idea {IdeaId}", filename, ideaId);
                return (null, null, null, "Error downloading file");
            }
        }

        /// <summary>
        /// Validate that the file exists in the idea's AttachmentFiles and return the full path
        /// This ensures users can only access files that belong to the idea
        /// </summary>
        private async Task<(string? FilePath, string? ErrorMessage)> ValidateAndGetFilePathAsync(long ideaId, string filename)
        {
            // Sanitize filename to prevent path traversal attacks
            var sanitizedFilename = Path.GetFileName(filename);
            if (string.IsNullOrEmpty(sanitizedFilename))
            {
                return (null, "Invalid filename");
            }

            // Get idea from database
            var idea = await _ideaRepository.GetByIdAsync(ideaId);
            if (idea == null)
            {
                return (null, "Idea not found");
            }

            // Check if idea has attachments
            if (string.IsNullOrEmpty(idea.AttachmentFiles))
            {
                return (null, "No attachments found for this idea");
            }

            // Get all attachment file paths from database
            var attachmentFiles = idea.AttachmentFiles.Split(';', StringSplitOptions.RemoveEmptyEntries);

            // Find the file path that matches the filename
            // This ensures the file is actually associated with this idea in the database
            var relativeFilePath = attachmentFiles.FirstOrDefault(f => f.EndsWith(sanitizedFilename));

            if (string.IsNullOrEmpty(relativeFilePath))
            {
                _logger.LogWarning("File {Filename} not found in AttachmentFiles for Idea {IdeaId}", sanitizedFilename, ideaId);
                return (null, "File not found in idea attachments");
            }

            // Construct full physical path
            // Database stores: "uploads/ideas/filename.ext"
            // We need: "C:\path\to\wwwroot\uploads\ideas\filename.ext"
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativeFilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            // Verify file exists on disk
            if (!File.Exists(fullPath))
            {
                _logger.LogError("File {FilePath} (from DB: {RelativePath}) not found on disk for Idea {IdeaId}",
                    fullPath, relativeFilePath, ideaId);
                return (null, "File not found on server");
            }

            return (fullPath, null);
        }

        /// <summary>
        /// Get MIME content type based on file extension
        /// </summary>
        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}
