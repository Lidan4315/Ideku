namespace Ideku.Services.FileAttachment
{
    public interface IFileAttachmentService
    {
        // Get file for viewing (inline preview)
        Task<(byte[]? FileBytes, string? ContentType, string? ErrorMessage)> GetFileForViewAsync(long ideaId, string filename);

        // Get file for downloading
        Task<(byte[]? FileBytes, string? ContentType, string? Filename, string? ErrorMessage)> GetFileForDownloadAsync(long ideaId, string filename);
    }
}
