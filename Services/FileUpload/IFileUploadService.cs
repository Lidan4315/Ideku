namespace Ideku.Services.FileUpload
{
    public interface IFileUploadService
    {
        (bool IsValid, string ErrorMessage) ValidateFiles(List<IFormFile>? files, long existingFilesTotalSize = 0);

        long CalculateExistingFilesSize(string? attachmentFilesPath, string webRootPath);

        Task<List<string>> HandleFileUploadsAsync(
            List<IFormFile>? files,
            string ideaCode,
            string webRootPath,
            int? stage = null,
            int existingFilesCount = 0);

        int GetExistingFilesCount(string ideaCode, string webRootPath);
    }
}
