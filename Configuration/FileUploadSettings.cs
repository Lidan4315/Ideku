namespace Ideku.Configuration
{
    /// Loaded from appsettings.json FileUploadSettings section
    public class FileUploadSettings
    {
        public int MaxFileSizeMB { get; set; } = 10;

        public int MaxTotalFileSizeMB { get; set; } = 10;

        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

        public long MaxFileSizeBytes => MaxFileSizeMB * 1024 * 1024;

        public long MaxTotalFileSizeBytes => MaxTotalFileSizeMB * 1024 * 1024;
    }
}
