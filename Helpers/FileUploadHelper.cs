namespace Ideku.Helpers
{
    /// <summary>
    /// Helper class for file validation
    /// </summary>
    public static class FileUploadHelper
    {
        private static readonly string[] AllowedExtensions = new[]
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".jpeg", ".png"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB per file
        private const long MaxTotalFileSizeBytes = 10 * 1024 * 1024; // 10MB total

        /// <summary>
        /// Validates a collection of files for type and size constraints
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateFiles(List<IFormFile>? files)
        {
            if (files == null || !files.Any())
                return (true, string.Empty);

            long totalSize = 0;

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Validate file type
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(fileExtension))
                {
                    return (false, $"File type '{fileExtension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
                }

                // Validate individual file size
                if (file.Length > MaxFileSizeBytes)
                {
                    var fileSizeMB = (file.Length / (1024.0 * 1024.0)).ToString("F2");
                    return (false, $"File '{file.FileName}' ({fileSizeMB} MB) exceeds the maximum size of 10 MB per file");
                }

                // Accumulate total size
                totalSize += file.Length;
            }

            // Validate total size
            if (totalSize > MaxTotalFileSizeBytes)
            {
                var totalSizeMB = (totalSize / (1024.0 * 1024.0)).ToString("F2");
                var maxTotalMB = (MaxTotalFileSizeBytes / (1024.0 * 1024.0)).ToString("F0");
                return (false, $"Total file size ({totalSizeMB} MB) exceeds the maximum allowed total size of {maxTotalMB} MB");
            }

            return (true, string.Empty);
        }
    }
}
