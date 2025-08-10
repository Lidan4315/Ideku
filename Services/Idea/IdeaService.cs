using Ideku.Data.Repositories;
using Ideku.ViewModels;
using Ideku.Models.Entities;

namespace Ideku.Services.Idea
{
    public class IdeaService : IIdeaService
    {
        private readonly IIdeaRepository _ideaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILookupRepository _lookupRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IdeaService(
            IIdeaRepository ideaRepository,
            IUserRepository userRepository,
            ILookupRepository lookupRepository,
            IWorkflowRepository workflowRepository,
            IEmployeeRepository employeeRepository,
            IWebHostEnvironment webHostEnvironment)
        {
            _ideaRepository = ideaRepository;
            _userRepository = userRepository;
            _lookupRepository = lookupRepository;
            _workflowRepository = workflowRepository;
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

        public async Task<(bool Success, string Message, string? IdeaCode)> CreateIdeaAsync(CreateIdeaViewModel model, List<IFormFile>? files)
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
                var initiatorUser = await _userRepository.GetByUsernameAsync(model.BadgeNumber);
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

                // Handle file uploads
                var attachmentPaths = await HandleFileUploadsAsync(files);

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
                    SavingCost = model.SavingCost,
                    AttachmentFiles = string.Join(";", attachmentPaths),
                    IdeaCode = "TMP", // Temporary code
                    Workflow = "Draft", // Initial workflow status
                    CurrentStatus = "Submitted",
                    CurrentStage = 0, // Initial stage for new submission
                    SubmittedDate = DateTime.Now
                };

                // Save using repository to get the ID
                var createdIdea = await _ideaRepository.CreateAsync(idea);

                // Generate IdeaCode based on the actual ID
                var ideaCode = _ideaRepository.GenerateIdeaCodeFromId(createdIdea.Id);
                
                // Update IdeaCode
                await _ideaRepository.UpdateIdeaCodeAsync(createdIdea.Id, ideaCode);

                // Create initial workflow history
                var workflowHistory = new WorkflowHistory
                {
                    IdeaId = createdIdea.Id,
                    ActorUserId = model.InitiatorUserId,
                    FromStage = 0, // Initial stage
                    ToStage = 1,   // First stage after submission
                    Action = "Submitted",
                    Comments = "Idea submitted for review",
                    Timestamp = DateTime.Now
                };

                await _workflowRepository.CreateAsync(workflowHistory);

                return (true, $"Idea '{model.IdeaName}' has been successfully submitted!", ideaCode);
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
                division = employee.DIVISION,
                department = employee.DEPARTEMENT
            };
        }

        #region Private Methods

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