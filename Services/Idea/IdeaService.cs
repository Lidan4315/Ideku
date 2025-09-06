using Ideku.Data.Repositories;
using Ideku.ViewModels;
using Ideku.Models.Entities;
using Ideku.Services.WorkflowManagement;

namespace Ideku.Services.Idea
{
    public class IdeaService : IIdeaService
    {
        private readonly IIdeaRepository _ideaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILookupRepository _lookupRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IWorkflowManagementService _workflowManagementService;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IdeaService(
            IIdeaRepository ideaRepository,
            IUserRepository userRepository,
            ILookupRepository lookupRepository,
            IWorkflowRepository workflowRepository,
            IWorkflowManagementService workflowManagementService,
            IEmployeeRepository employeeRepository,
            IWebHostEnvironment webHostEnvironment)
        {
            _ideaRepository = ideaRepository;
            _userRepository = userRepository;
            _lookupRepository = lookupRepository;
            _workflowRepository = workflowRepository;
            _workflowManagementService = workflowManagementService;
            _employeeRepository = employeeRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<CreateIdeaViewModel> PrepareCreateViewModelAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new UnauthorizedAccessException("User not authenticated");

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            return new CreateIdeaViewModel
            {
                // Current user info (who is creating the idea, not necessarily the initiator)
                InitiatorUserId = user.Id,
                
                // Don't auto-populate initiator info - let user input badge number
                BadgeNumber = "",
                EmployeeName = "",
                Position = "",
                Email = "",
                EmployeeId = "",

                // Populate dropdown lists
                DivisionList = await _lookupRepository.GetDivisionsAsync(),
                CategoryList = await _lookupRepository.GetCategoriesAsync(),
                EventList = await _lookupRepository.GetEventsAsync(),
                DepartmentList = await _lookupRepository.GetDepartmentsByDivisionAsync("")
            };
        }

        public async Task<(bool Success, string Message, Models.Entities.Idea? CreatedIdea)> CreateIdeaAsync(CreateIdeaViewModel model, List<IFormFile>? files)
        {
            try
            {
                // Validate employee exists
                var employee = await _employeeRepository.GetByEmployeeIdAsync(model.BadgeNumber);
                if (employee == null)
                {
                    return (false, "Employee with badge number not found", null);
                }

                // Get or create User for the employee (idea initiator)
                var initiatorUser = await _userRepository.GetByEmployeeIdAsync(model.BadgeNumber);
                long initiatorUserId;
                
                if (initiatorUser == null)
                {
                    // For now, we'll use the current logged in user's ID as initiator
                    // In future, we might create user records automatically
                    initiatorUserId = model.InitiatorUserId;
                }
                else
                {
                    initiatorUserId = initiatorUser.Id;
                }

                // First save the idea with temporary file paths to get the ID and generate idea code
                // We'll handle file uploads after getting the idea code

                // Determine applicable workflow based on idea conditions
                var applicableWorkflow = await _workflowManagementService.GetApplicableWorkflowAsync(
                    model.CategoryId,
                    model.ToDivisionId,
                    model.ToDepartmentId,
                    model.SavingCost ?? 0,
                    model.EventId
                );

                if (applicableWorkflow == null)
                {
                    return (false, "No applicable workflow found for this idea. Please contact administrator.", null);
                }

                // Get workflow stages count for MaxStage
                var workflowWithStages = await _workflowManagementService.GetWorkflowByIdAsync(applicableWorkflow.Id);
                var maxStage = workflowWithStages?.WorkflowStages?.Count() ?? 0;

                // Create new Idea entity (without IdeaCode first)
                var idea = new Models.Entities.Idea
                {
                    InitiatorUserId = initiatorUserId,
                    ToDivisionId = model.ToDivisionId,
                    ToDepartmentId = model.ToDepartmentId,
                    CategoryId = model.CategoryId,
                    EventId = model.EventId,
                    IdeaName = model.IdeaName,
                    IdeaIssueBackground = model.IdeaDescription,
                    IdeaSolution = model.Solution,
                    SavingCost = model.SavingCost ?? 0,
                    AttachmentFiles = "", // Will be updated after file upload with proper naming
                    IdeaCode = "TMP", // Temporary code
                    WorkflowId = applicableWorkflow.Id, // Assign determined workflow
                    MaxStage = maxStage, // Set maximum stages for this workflow
                    CurrentStatus = "Waiting Approval S1",
                    CurrentStage = 0, // Start from stage 0
                    IsDeleted = false, // Default not deleted
                    SubmittedDate = DateTime.Now
                };

                // Save using repository to get the ID
                var createdIdea = await _ideaRepository.CreateAsync(idea);

                // Generate IdeaCode based on the actual ID
                var ideaCode = _ideaRepository.GenerateIdeaCodeFromId(createdIdea.Id);
                
                // Handle file uploads with proper naming now that we have idea code
                var attachmentPaths = await HandleFileUploadsWithNamingAsync(files, ideaCode, createdIdea.CurrentStage);
                
                // Update IdeaCode and AttachmentFiles
                await _ideaRepository.UpdateIdeaCodeAsync(createdIdea.Id, ideaCode);
                
                // Update attachment files with proper naming
                createdIdea.AttachmentFiles = string.Join(";", attachmentPaths);
                await _ideaRepository.UpdateAsync(createdIdea);

                // Note: No WorkflowHistory entry for initial submission
                // Submission is already tracked by Idea.SubmittedDate
                // WorkflowHistory will be created only for approval/rejection actions

                createdIdea.IdeaCode = ideaCode; // Update the object with the final code
                return (true, $"Idea '{model.IdeaName}' has been successfully submitted!", createdIdea);
            }
            catch (Exception ex)
            {
                return (false, $"Error saving idea: {ex.Message}", null);
            }
        }

        public async Task<IEnumerable<Models.Entities.Idea>> GetUserIdeasAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new UnauthorizedAccessException("User not authenticated");

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            return await _ideaRepository.GetByInitiatorAsync(user.Id);
        }

        public async Task<List<object>> GetDepartmentsByDivisionAsync(string divisionId)
        {
            var departments = await _lookupRepository.GetDepartmentsByDivisionAsync(divisionId);
            return departments.Select(d => new { value = d.Value, text = d.Text }).ToList<object>();
        }

        public async Task<object?> GetEmployeeByBadgeNumberAsync(string badgeNumber)
        {
            if (string.IsNullOrEmpty(badgeNumber))
                return null;

            var employee = await _employeeRepository.GetByEmployeeIdAsync(badgeNumber);
            if (employee == null)
                return null;

            return new
            {
                employeeId = employee.EMP_ID,
                name = employee.NAME,
                position = employee.POSITION_TITLE,
                email = employee.EMAIL,
                division = employee.DivisionNavigation?.NameDivision,
                department = employee.DepartmentNavigation?.NameDepartment
            };
        }

        #region Private Methods

        private async Task<List<string>> HandleFileUploadsWithNamingAsync(List<IFormFile>? files, string ideaCode, int currentStage)
        {
            var filePaths = new List<string>();

            if (files == null || !files.Any())
                return filePaths;

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "ideas");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Get existing files count for sequential numbering (ALL files from this idea)
            var ideaPattern = $"{ideaCode}_";
            var existingFiles = Directory.GetFiles(uploadsPath)
                .Where(f => Path.GetFileName(f).StartsWith(ideaPattern))
                .ToList();

            int fileCounter = existingFiles.Count + 1;
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        throw new InvalidOperationException($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
                    }

                    // Validate file size (max 10MB)
                    if (file.Length > 10 * 1024 * 1024)
                    {
                        throw new InvalidOperationException($"File {file.FileName} is too large. Maximum size is 10MB.");
                    }

                    // Generate filename with format: ideaCode_S(currentStage)_00X.extension
                    var fileName = $"{ideaCode}_S{currentStage}_{fileCounter:D3}{fileExtension}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Store relative path
                    filePaths.Add($"uploads/ideas/{fileName}");
                    fileCounter++;
                }
            }

            return filePaths;
        }

        // Keep the old method for backward compatibility if needed
        private async Task<List<string>> HandleFileUploadsAsync(List<IFormFile>? files)
        {
            var filePaths = new List<string>();

            if (files == null || !files.Any())
                return filePaths;

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "ideas");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    // Validate file type (optional)
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        throw new InvalidOperationException($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
                    }

                    // Validate file size (max 10MB)
                    if (file.Length > 10 * 1024 * 1024)
                    {
                        throw new InvalidOperationException($"File {file.FileName} is too large. Maximum size is 10MB.");
                    }

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Store relative path
                    filePaths.Add($"uploads/ideas/{fileName}");
                }
            }

            return filePaths;
        }

        #endregion
    }
}
