using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ideku.Services.FileAttachment;

namespace Ideku.Controllers
{
    /// <summary>
    /// Centralized controller for handling all file attachments
    /// Provides consistent URLs across all modules: /attached/view/{ideaId}/{filename}
    /// </summary>
    [Authorize]
    public class AttachedController : Controller
    {
        private readonly IFileAttachmentService _fileAttachmentService;
        private readonly ILogger<AttachedController> _logger;

        public AttachedController(
            IFileAttachmentService fileAttachmentService,
            ILogger<AttachedController> logger)
        {
            _fileAttachmentService = fileAttachmentService;
            _logger = logger;
        }

        /// <summary>
        /// View attachment file inline (for preview)
        /// Route: GET /attached/view/{ideaId}/{**filename}
        /// Example: /attached/view/123/document.pdf
        /// Note: {**filename} is a catch-all to handle filenames with dots and special chars
        /// </summary>
        [HttpGet("/attached/view/{ideaId}/{**filename}")]
        public async Task<IActionResult> View(long ideaId, string filename)
        {
            var result = await _fileAttachmentService.GetFileForViewAsync(ideaId, filename);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                _logger.LogWarning("File view failed for IdeaId {IdeaId}, Filename {Filename}: {Error}",
                    ideaId, filename, result.ErrorMessage);
                return NotFound(result.ErrorMessage);
            }

            return File(result.FileBytes!, result.ContentType!);
        }

        /// <summary>
        /// Download attachment file
        /// Route: GET /attached/download/{ideaId}/{**filename}
        /// Example: /attached/download/123/document.pdf
        /// Note: {**filename} is a catch-all to handle filenames with dots and special chars
        /// </summary>
        [HttpGet("/attached/download/{ideaId}/{**filename}")]
        public async Task<IActionResult> Download(long ideaId, string filename)
        {
            var result = await _fileAttachmentService.GetFileForDownloadAsync(ideaId, filename);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                _logger.LogWarning("File download failed for IdeaId {IdeaId}, Filename {Filename}: {Error}",
                    ideaId, filename, result.ErrorMessage);
                return NotFound(result.ErrorMessage);
            }

            return File(result.FileBytes!, result.ContentType!, result.Filename!);
        }
    }
}
