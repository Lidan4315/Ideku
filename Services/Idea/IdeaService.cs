using Ideku.Data.Repositories;
using Ideku.Data.Context;
using Ideku.Models.Entities;
using Ideku.Models.Statistics;
using Ideku.Services.WorkflowManagement;
using Ideku.Services.Lookup;
using Ideku.Services.UserManagement;
using Ideku.Services.Workflow;
using Ideku.Services.Notification;
using Ideku.Services.FileUpload;
using Ideku.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ideku.Services.Idea
{
    public class IdeaService : IIdeaService
    {
        private readonly IIdeaRepository _ideaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILookupService _lookupService;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IWorkflowManagementService _workflowManagementService;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IUserManagementService _userManagementService;
        private readonly IWorkflowService _workflowService;
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<IdeaService> _logger;

        public IdeaService(
            IIdeaRepository ideaRepository,
            IUserRepository userRepository,
            ILookupService lookupService,
            IWorkflowRepository workflowRepository,
            IWorkflowManagementService workflowManagementService,
            IEmployeeRepository employeeRepository,
            IWebHostEnvironment webHostEnvironment,
            IUserManagementService userManagementService,
            IWorkflowService workflowService,
            AppDbContext context,
            INotificationService notificationService,
            IFileUploadService fileUploadService,
            ILogger<IdeaService> logger)
        {
            _ideaRepository = ideaRepository;
            _userRepository = userRepository;
            _lookupService = lookupService;
            _workflowRepository = workflowRepository;
            _workflowManagementService = workflowManagementService;
            _employeeRepository = employeeRepository;
            _webHostEnvironment = webHostEnvironment;
            _userManagementService = userManagementService;
            _workflowService = workflowService;
            _context = context;
            _notificationService = notificationService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // PrepareCreateViewModelAsync REMOVED - Controller now handles ViewModel population directly

        public async Task<(bool Success, string Message, Models.Entities.Idea? CreatedIdea)> CreateIdeaAsync(Models.Entities.Idea idea, List<IFormFile>? files)
        {
            try
            {
                // VALIDATE FILES FIRST - before creating idea in database
                var fileValidation = _fileUploadService.ValidateFiles(files);
                if (!fileValidation.IsValid)
                {
                    return (false, fileValidation.ErrorMessage, null);
                }

                // Validate Idea Name uniqueness
                var ideaNameExists = await _ideaRepository.IsIdeaNameExistsAsync(idea.IdeaName);
                if (ideaNameExists)
                {
                    return (false, "An idea with this name already exists. Please use a different name.", null);
                }

                // Validate InitiatorUser exists (strict validation - no fallback)
                var initiatorUser = await _userRepository.GetByEmployeeIdAsync(idea.InitiatorUserId); // Changed: use EmployeeId
                if (initiatorUser == null)
                {
                    return (false, "Initiator user not found in the system. Please contact administrator.", null);
                }

                // Determine applicable workflow based on idea conditions
                var applicableWorkflow = await _workflowManagementService.GetApplicableWorkflowAsync(
                    idea.CategoryId,
                    idea.ToDivisionId,
                    idea.ToDepartmentId,
                    idea.SavingCost,
                    idea.EventId
                );

                if (applicableWorkflow == null)
                {
                    return (false, "No applicable workflow found for this idea. Please contact administrator.", null);
                }

                // Get workflow stages count for MaxStage
                var workflowWithStages = await _workflowManagementService.GetWorkflowByIdAsync(applicableWorkflow.Id);
                var maxStage = workflowWithStages?.WorkflowStages?.Count() ?? 0;

                // Set workflow-related properties
                idea.WorkflowId = applicableWorkflow.Id;
                idea.CurrentStage = 0; // Stage 0 = belum masuk workflow, menunggu approval pertama
                idea.MaxStage = maxStage;
                idea.CurrentStatus = "Waiting Approval S1"; // Menunggu approval untuk masuk Stage 1
                idea.SubmittedDate = DateTime.Now;
                idea.IsDeleted = false;
                idea.IsRejected = false;

                // Idea entity is already populated by Controller
                // Set temporary values that will be updated after getting ID
                idea.AttachmentFiles = ""; // Will be updated after file upload with proper naming
                idea.IdeaCode = "TMP"; // Temporary code, will be generated based on ID

                // Save using repository to get the ID
                var createdIdea = await _ideaRepository.CreateAsync(idea);

                // Generate IdeaCode based on the actual ID
                var ideaCode = _ideaRepository.GenerateIdeaCodeFromId(createdIdea.Id);

                // Handle file uploads with proper naming now that we have idea code
                var existingFilesCount = _fileUploadService.GetExistingFilesCount(ideaCode, _webHostEnvironment.WebRootPath);
                var attachmentPaths = await _fileUploadService.HandleFileUploadsAsync(
                    files,
                    ideaCode,
                    _webHostEnvironment.WebRootPath,
                    stage: createdIdea.CurrentStage,
                    existingFilesCount: existingFilesCount
                );
                
                // Update IdeaCode and AttachmentFiles
                await _ideaRepository.UpdateIdeaCodeAsync(createdIdea.Id, ideaCode);
                
                // Update attachment files with proper naming
                createdIdea.AttachmentFiles = string.Join(";", attachmentPaths);
                await _ideaRepository.UpdateAsync(createdIdea);

                // Note: No WorkflowHistory entry for initial submission
                // Submission is already tracked by Idea.SubmittedDate
                // WorkflowHistory will be created only for approval/rejection actions

                createdIdea.IdeaCode = ideaCode; // Update the object with the final code
                return (true, $"Idea '{idea.IdeaName}' has been successfully submitted!", createdIdea);
            }
            catch (Exception ex)
            {
                return (false, $"Error saving idea: {ex.Message}", null);
            }
        }

        public async Task<IQueryable<Models.Entities.Idea>> GetUserIdeasAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new UnauthorizedAccessException("User not authenticated");

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            // Get base queryable with all necessary includes
            return _ideaRepository.GetQueryableWithIncludes()
                .Where(idea => idea.InitiatorUserId == user.EmployeeId) // Changed: compare with EmployeeId
                .OrderByDescending(idea => idea.SubmittedDate)
                .ThenByDescending(idea => idea.Id);
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

        #region Idea List Methods

        public async Task<IQueryable<Models.Entities.Idea>> GetAllIdeasQueryAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return Enumerable.Empty<Models.Entities.Idea>().AsQueryable();
            }

            // Get base queryable for ideas with all necessary includes
            var baseQuery = _ideaRepository.GetQueryableWithIncludes();

            // Apply role-based filtering
            if (user.Role.RoleName == "Superuser")
            {
                // Superuser can see ALL ideas
                return baseQuery.OrderByDescending(idea => idea.SubmittedDate)
                    .ThenByDescending(idea => idea.Id);
            }
            else
            {
                // All non-Superuser roles: filter by division and department
                // Access control (via ModuleAuthorize) determines which roles can access this data
                var userDivision = user.Employee?.DIVISION;
                var userDepartment = user.Employee?.DEPARTEMENT;

                if (string.IsNullOrEmpty(userDivision))
                {
                    return Enumerable.Empty<Models.Entities.Idea>().AsQueryable();
                }

                // Users can see:
                // 1. Ideas targeted to their division and department
                // 2. Ideas where their division is in RelatedDivisionsJson
                return baseQuery.Where(idea =>
                    // Ideas targeted to their division/department
                    (idea.ToDivisionId == userDivision &&
                     (string.IsNullOrEmpty(userDepartment) || idea.ToDepartmentId == userDepartment)) ||
                    // Ideas where their division is in RelatedDivisionsJson (stored as JSON string)
                    (idea.RelatedDivisionsJson != null && idea.RelatedDivisionsJson.Contains(userDivision))
                ).OrderByDescending(idea => idea.SubmittedDate)
                .ThenByDescending(idea => idea.Id);
            }
        }

        #endregion
        
        #region Additional Helper Methods
        
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username);
        }

        public async Task<User?> GetUserByEmployeeIdAsync(string employeeId)
        {
            return await _userRepository.GetByEmployeeIdAsync(employeeId);
        }

        #endregion

        #region Dashboard

        public async Task<DashboardData> GetDashboardDataAsync(string username, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null)
        {
            var allIdeasQuery = _ideaRepository.GetQueryableWithIncludes();
            var query = allIdeasQuery.Where(i => !i.IsDeleted);

            // Apply date filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(i => i.SubmittedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                // Include the entire end date (until 23:59:59)
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply Division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply Stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply Saving Cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                query = savingCostRange switch
                {
                    "lt20k" => query.Where(i => i.SavingCost < 20000),
                    "gte20k" => query.Where(i => i.SavingCost >= 20000),
                    _ => query
                };
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var activeIdeas = await query.ToListAsync();

            var actingStats = await _userManagementService.GetActingStatisticsAsync();

            return new DashboardData
            {
                TotalIdeas = activeIdeas.Count(),
                PendingApproval = activeIdeas.Count(i => i.CurrentStatus.Contains("Waiting Approval")),
                Completed = activeIdeas.Count(i => i.CurrentStatus == "Completed"),
                Rejected = activeIdeas.Count(i => i.CurrentStatus == "Rejected"),
                TotalSavingCost = activeIdeas.Sum(i => i.SavingCost),
                ValidatedSavingCost = activeIdeas.Sum(i => i.SavingCostValidated ?? 0),
                UrgentActingExpirations = actingStats.UrgentExpirations
            };
        }

        public async Task<object> GetIdeasByStatusChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null)
        {
            var allIdeasQuery = _ideaRepository.GetQueryableWithIncludes();
            var query = allIdeasQuery.Where(i => !i.IsDeleted);

            // Apply date filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(i => i.SubmittedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply Division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply Stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply Saving Cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                query = savingCostRange switch
                {
                    "lt20k" => query.Where(i => i.SavingCost < 20000),
                    "gte20k" => query.Where(i => i.SavingCost >= 20000),
                    _ => query
                };
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var activeIdeas = await query.ToListAsync();

            var stageGroups = activeIdeas
                .GroupBy(i => i.CurrentStage)
                .Select(g => new { Stage = g.Key, Count = g.Count() })
                .OrderBy(x => x.Stage)
                .ToList();

            return new
            {
                labels = stageGroups.Select(x => $"S{x.Stage}").ToArray(),
                datasets = new[] { new { data = stageGroups.Select(x => x.Count).ToArray() } }
            };
        }

        public async Task<object> GetIdeasByDivisionChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null)
        {
            var allIdeasQuery = _ideaRepository.GetQueryableWithIncludes();
            var query = allIdeasQuery.Where(i => !i.IsDeleted);

            // Apply date filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(i => i.SubmittedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply Division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply Stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply Saving Cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                query = savingCostRange switch
                {
                    "lt20k" => query.Where(i => i.SavingCost < 20000),
                    "gte20k" => query.Where(i => i.SavingCost >= 20000),
                    _ => query
                };
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var activeIdeas = await query.ToListAsync();

            var divisionGroups = activeIdeas
                .Where(i => i.TargetDivision != null)
                .GroupBy(i => new { i.TargetDivision!.Id, i.TargetDivision!.NameDivision })
                .Select(g => new { DivisionId = g.Key.Id, Division = g.Key.NameDivision, Count = g.Count() })
                .OrderBy(x => x.DivisionId)
                .ToList();

            return new
            {
                labels = divisionGroups.Select(x => x.Division).ToArray(),
                datasets = new[] { new { data = divisionGroups.Select(x => x.Count).ToArray() } },
                divisionIds = divisionGroups.Select(x => x.DivisionId).ToArray()
            };
        }

        public async Task<object> GetIdeasByDepartmentChartAsync(string divisionId, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null)
        {
            var allIdeasQuery = _ideaRepository.GetQueryableWithIncludes();
            var query = allIdeasQuery.Where(i => !i.IsDeleted);

            // Apply date filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(i => i.SubmittedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply Division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply Stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply Saving Cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                query = savingCostRange switch
                {
                    "lt20k" => query.Where(i => i.SavingCost < 20000),
                    "gte20k" => query.Where(i => i.SavingCost >= 20000),
                    _ => query
                };
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var activeIdeas = await query.ToListAsync();

            var division = activeIdeas
                .Where(i => i.TargetDivision != null && i.TargetDivision.Id == divisionId)
                .FirstOrDefault()?.TargetDivision;

            var departmentGroups = activeIdeas
                .Where(i => i.TargetDivision != null && i.TargetDivision.Id == divisionId && i.TargetDepartment != null)
                .GroupBy(i => i.TargetDepartment!.NameDepartment)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            return new
            {
                divisionName = division?.NameDivision ?? "",
                labels = departmentGroups.Select(x => x.Department).ToArray(),
                datasets = new[] { new { data = departmentGroups.Select(x => x.Count).ToArray() } }
            };
        }

        public async Task<object> GetIdeasByAllDepartmentsChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null)
        {
            var allIdeasQuery = _ideaRepository.GetQueryableWithIncludes();
            var query = allIdeasQuery.Where(i => !i.IsDeleted);

            // Apply date filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(i => i.SubmittedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply Division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply Stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply Saving Cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                query = savingCostRange switch
                {
                    "lt20k" => query.Where(i => i.SavingCost < 20000),
                    "gte20k" => query.Where(i => i.SavingCost >= 20000),
                    _ => query
                };
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var activeIdeas = await query.ToListAsync();

            var departmentGroups = activeIdeas
                .Where(i => i.TargetDepartment != null)
                .GroupBy(i => new { i.TargetDepartment!.Id, i.TargetDepartment!.NameDepartment })
                .Select(g => new { DepartmentId = g.Key.Id, Department = g.Key.NameDepartment, Count = g.Count() })
                .OrderBy(x => x.DepartmentId)
                .ToList();

            return new
            {
                labels = departmentGroups.Select(x => x.Department).ToArray(),
                datasets = new[] { new { data = departmentGroups.Select(x => x.Count).ToArray() } }
            };
        }

        public async Task<object> GetInitiativeByStageAndDivisionChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null)
        {
            var allIdeasQuery = _ideaRepository.GetQueryableWithIncludes();
            var query = allIdeasQuery.Where(i => !i.IsDeleted);

            // Apply date filter if provided
            if (startDate.HasValue)
            {
                query = query.Where(i => i.SubmittedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply Division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply Stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply Saving Cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                query = savingCostRange switch
                {
                    "lt20k" => query.Where(i => i.SavingCost < 20000),
                    "gte20k" => query.Where(i => i.SavingCost >= 20000),
                    _ => query
                };
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var allIdeas = await query.ToListAsync();

            // Get all unique stages, sorted
            var stages = allIdeas
                .Select(i => i.CurrentStage)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            // Get all unique divisions
            var divisions = allIdeas
                .Where(i => i.TargetDivision != null)
                .Select(i => new { i.TargetDivision!.Id, i.TargetDivision!.NameDivision })
                .Distinct()
                .OrderBy(d => d.Id)
                .ToList();

            // Group data by Stage and Division
            var groupedData = allIdeas
                .Where(i => i.TargetDivision != null)
                .GroupBy(i => new { i.CurrentStage, DivisionId = i.TargetDivision!.Id, DivisionName = i.TargetDivision!.NameDivision })
                .Select(g => new
                {
                    Stage = g.Key.CurrentStage,
                    DivisionId = g.Key.DivisionId,
                    DivisionName = g.Key.DivisionName,
                    Count = g.Count()
                })
                .ToList();

            // Prepare datasets for each division
            var datasets = divisions.Select(div => new
            {
                label = div.NameDivision,
                data = stages.Select(stage =>
                {
                    var item = groupedData.FirstOrDefault(g => g.Stage == stage && g.DivisionId == div.Id);
                    return item?.Count ?? 0;
                }).ToArray()
            }).ToList();

            return new
            {
                labels = stages.Select(s => $"S{s}").ToArray(), // S0, S1, S2, etc.
                datasets = datasets
            };
        }

        public async Task<List<WLChartData>> GetWLChartDataAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? selectedDivision = null,
            int? selectedStage = null,
            string? savingCostRange = null,
            string? initiatorName = null,
            string? initiatorBadgeNumber = null,
            string? ideaId = null,
            string? initiatorDivision = null,
            string? selectedStatus = null)
        {
            var workstreamLeaders = await _userRepository.GetAllUsersWithDetailsAsync();
            workstreamLeaders = workstreamLeaders
                .Where(u => u.Role.RoleName == "Workstream Leader")
                .Where(u => u.Employee.EMP_STATUS == "Active")
                .ToList();

            var chartData = new List<WLChartData>();
            var allIdeasQuery = _ideaRepository.GetQueryableWithIncludes();

            foreach (var wl in workstreamLeaders)
            {
                var query = allIdeasQuery
                    .Where(i => !i.IsDeleted)
                    .Where(i => i.ToDivisionId == wl.Employee.DIVISION)
                    .Where(i => i.ToDepartmentId == wl.Employee.DEPARTEMENT);

                if (startDate.HasValue)
                    query = query.Where(i => i.SubmittedDate >= startDate.Value);

                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(i => i.SubmittedDate <= endOfDay);
                }

                if (!string.IsNullOrEmpty(selectedDivision))
                    query = query.Where(i => i.ToDivisionId == selectedDivision);

                if (selectedStage.HasValue)
                    query = query.Where(i => i.CurrentStage == selectedStage.Value);

                if (!string.IsNullOrEmpty(savingCostRange))
                {
                    query = savingCostRange switch
                    {
                        "lt20k" => query.Where(i => i.SavingCost < 20000),
                        "gte20k" => query.Where(i => i.SavingCost >= 20000),
                        _ => query
                    };
                }

                // Apply Initiator Name filter
                if (!string.IsNullOrWhiteSpace(initiatorName))
                {
                    query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
                }

                // Apply Initiator Badge Number filter
                if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
                {
                    query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
                }

                // Apply Idea Id filter
                if (!string.IsNullOrWhiteSpace(ideaId))
                {
                    query = query.Where(i => i.IdeaCode.Contains(ideaId));
                }

                // Apply Initiator Division filter
                if (!string.IsNullOrWhiteSpace(initiatorDivision))
                {
                    query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
                }

                // Apply Status filter
                if (!string.IsNullOrWhiteSpace(selectedStatus))
                {
                    query = query.Where(i => i.CurrentStatus == selectedStatus);
                }

                var ideasByStage = await query
                    .GroupBy(i => i.CurrentStage)
                    .Select(g => new { Stage = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Stage)
                    .ToListAsync();

                chartData.Add(new WLChartData
                {
                    UserId = wl.Id,
                    UserName = wl.Employee.NAME,
                    EmployeeId = wl.EmployeeId,
                    Division = wl.Employee.DivisionNavigation?.NameDivision ?? wl.Employee.DIVISION,
                    DepartmentId = wl.Employee.DEPARTEMENT,
                    Department = wl.Employee.DepartmentNavigation?.NameDepartment ?? wl.Employee.DEPARTEMENT,
                    IdeasByStage = ideasByStage.ToDictionary(x => $"S{x.Stage}", x => x.Count),
                    TotalIdeas = ideasByStage.Sum(x => x.Count)
                });
            }

            return chartData
                .OrderBy(x => x.DepartmentId)
                .ThenBy(x => x.EmployeeId)
                .ToList();
        }

        public async Task<List<IdeaListItemDto>> GetIdeasListAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? selectedDivision = null,
            int? selectedStage = null,
            string? savingCostRange = null,
            string? initiatorName = null,
            string? initiatorBadgeNumber = null,
            string? ideaId = null,
            string? initiatorDivision = null,
            string? selectedStatus = null)
        {
            var query = _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted);

            if (startDate.HasValue)
            {
                var startOfDay = startDate.Value.Date;
                query = query.Where(i => i.SubmittedDate >= startOfDay);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                if (savingCostRange == "lt20k")
                    query = query.Where(i => i.SavingCost < 20000);
                else if (savingCostRange == "gte20k")
                    query = query.Where(i => i.SavingCost >= 20000);
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var ideas = await query
                .OrderByDescending(i => i.SubmittedDate)
                .Select(i => new IdeaListItemDto
                {
                    IdeaNumber = i.IdeaCode,
                    IdeaStatus = i.CurrentStatus,
                    CurrentStage = "S" + i.CurrentStage,
                    SubmissionDate = i.SubmittedDate,
                    LastUpdatedDays = (int)(DateTime.Now - (i.UpdatedDate ?? i.SubmittedDate)).TotalDays,
                    IdeaFlowValidated = i.SavingCostValidated == null
                        ? "not_validated"
                        : (i.SavingCostValidated.Value >= 20000 ? "more_than_20" : "less_than_20"),
                    InitiatorBN = i.InitiatorUser.Employee.EMP_ID,
                    InitiatorName = i.InitiatorUser.Employee.NAME,
                    InitiatorDivision = i.InitiatorUser.Employee.DivisionNavigation.NameDivision,
                    ImplementOnDivision = i.TargetDivision.NameDivision,
                    ImplementOnDepartment = i.TargetDepartment.NameDepartment,
                    IdeaTitle = i.IdeaName
                })
                .ToListAsync();

            return ideas;
        }

        public async Task<IQueryable<IdeaListItemDto>> GetIdeasListQueryAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? selectedDivision = null,
            int? selectedStage = null,
            string? savingCostRange = null,
            string? initiatorName = null,
            string? initiatorBadgeNumber = null,
            string? ideaId = null,
            string? initiatorDivision = null,
            string? selectedStatus = null)
        {
            var query = _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted);

            if (startDate.HasValue)
            {
                var startOfDay = startDate.Value.Date;
                query = query.Where(i => i.SubmittedDate >= startOfDay);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                if (savingCostRange == "lt20k")
                    query = query.Where(i => i.SavingCost < 20000);
                else if (savingCostRange == "gte20k")
                    query = query.Where(i => i.SavingCost >= 20000);
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var orderedQuery = query
                .OrderByDescending(i => i.SubmittedDate)
                .Select(i => new IdeaListItemDto
                {
                    IdeaNumber = i.IdeaCode,
                    IdeaStatus = i.CurrentStatus,
                    CurrentStage = "S" + i.CurrentStage,
                    SubmissionDate = i.SubmittedDate,
                    LastUpdatedDays = (int)(DateTime.Now - (i.UpdatedDate ?? i.SubmittedDate)).TotalDays,
                    IdeaFlowValidated = i.SavingCostValidated == null
                        ? "not_validated"
                        : (i.SavingCostValidated.Value >= 20000 ? "more_than_20" : "less_than_20"),
                    InitiatorBN = i.InitiatorUser.Employee.EMP_ID,
                    InitiatorName = i.InitiatorUser.Employee.NAME,
                    InitiatorDivision = i.InitiatorUser.Employee.DivisionNavigation.NameDivision,
                    ImplementOnDivision = i.TargetDivision.NameDivision,
                    ImplementOnDepartment = i.TargetDepartment.NameDepartment,
                    IdeaTitle = i.IdeaName
                });

            return await Task.FromResult(orderedQuery);
        }

        public async Task<List<int>> GetAvailableStagesAsync()
        {
            // Get the highest MaxStage from all workflows in the system
            var maxStageQuery = await _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted)
                .Select(i => i.MaxStage)
                .ToListAsync();

            // If no ideas exist yet, return empty list (dropdown will be empty)gsese
            if (!maxStageQuery.Any())
            {
                return new List<int>();
            }

            // Get the highest MaxStage value
            var maxStageFromWorkflows = maxStageQuery.Max();

            // Generate list from 0 to maxStage (e.g., if maxStage is 8, returns [0,1,2,3,4,5,6,7,8])
            var stages = Enumerable.Range(0, maxStageFromWorkflows + 1).ToList();

            return stages;
        }

        public async Task<List<string>> GetAvailableStatusesAsync()
        {
            // Get all distinct CurrentStatus values from ideas that are not deleted
            var statuses = await _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted && !string.IsNullOrWhiteSpace(i.CurrentStatus))
                .Select(i => i.CurrentStatus)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return statuses;
        }

        public async Task<List<TeamRoleItemDto>> GetTeamRoleListAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? selectedDivision = null,
            int? selectedStage = null,
            string? savingCostRange = null,
            string? initiatorName = null,
            string? initiatorBadgeNumber = null,
            string? ideaId = null,
            string? initiatorDivision = null,
            string? selectedStatus = null)
        {
            var query = _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted);

            // Apply date filters
            if (startDate.HasValue)
            {
                var startOfDay = startDate.Value.Date;
                query = query.Where(i => i.SubmittedDate >= startOfDay);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply saving cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                if (savingCostRange == "lt20k")
                    query = query.Where(i => i.SavingCost < 20000);
                else if (savingCostRange == "gte20k")
                    query = query.Where(i => i.SavingCost >= 20000);
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Join with IdeaImplementators and get team role data
            var teamRoleData = await query
                .SelectMany(i => i.IdeaImplementators, (idea, implementator) => new
                {
                    Idea = idea,
                    Implementator = implementator
                })
                .OrderBy(x => x.Idea.IdeaCode)
                .ThenBy(x => x.Implementator.Role == "Leader" ? 0 : 1)
                .ThenBy(x => x.Implementator.User.EmployeeId)
                .Select(x => new TeamRoleItemDto
                {
                    EmployeeName = x.Implementator.User.Employee.NAME,
                    EmployeeBN = x.Implementator.User.EmployeeId,
                    TeamRole = x.Implementator.Role,
                    IdeaCode = x.Idea.IdeaCode
                })
                .ToListAsync();

            return teamRoleData;
        }

        public async Task<IQueryable<TeamRoleItemDto>> GetTeamRoleListQueryAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? selectedDivision = null,
            int? selectedStage = null,
            string? savingCostRange = null,
            string? initiatorName = null,
            string? initiatorBadgeNumber = null,
            string? ideaId = null,
            string? initiatorDivision = null,
            string? selectedStatus = null)
        {
            var query = _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted);

            // Apply date filters
            if (startDate.HasValue)
            {
                var startOfDay = startDate.Value.Date;
                query = query.Where(i => i.SubmittedDate >= startOfDay);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply saving cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                if (savingCostRange == "lt20k")
                    query = query.Where(i => i.SavingCost < 20000);
                else if (savingCostRange == "gte20k")
                    query = query.Where(i => i.SavingCost >= 20000);
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Join with IdeaImplementators and get team role data with pagination
            var orderedQuery = query
                .SelectMany(i => i.IdeaImplementators, (idea, implementator) => new
                {
                    Idea = idea,
                    Implementator = implementator
                })
                .OrderBy(x => x.Idea.IdeaCode)
                .ThenBy(x => x.Implementator.Role == "Leader" ? 0 : 1)
                .ThenBy(x => x.Implementator.User.EmployeeId)
                .Select(x => new TeamRoleItemDto
                {
                    EmployeeName = x.Implementator.User.Employee.NAME,
                    EmployeeBN = x.Implementator.User.EmployeeId,
                    TeamRole = x.Implementator.Role,
                    IdeaCode = x.Idea.IdeaCode
                });

            return await Task.FromResult(orderedQuery);
        }

        public async Task<List<ApprovalHistoryItemDto>> GetApprovalHistoryListAsync(
            DateTime? startDate = null, DateTime? endDate = null,
            string? selectedDivision = null, int? selectedStage = null,
            string? savingCostRange = null, string? initiatorName = null,
            string? initiatorBadgeNumber = null, string? ideaId = null,
            string? initiatorDivision = null, string? selectedStatus = null)
        {
            var query = _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted);

            // Apply date filters
            if (startDate.HasValue)
            {
                var startOfDay = startDate.Value.Date;
                query = query.Where(i => i.SubmittedDate >= startOfDay);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply division filter
            if (!string.IsNullOrEmpty(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply saving cost filter
            if (!string.IsNullOrEmpty(savingCostRange))
            {
                if (savingCostRange == "lt20k")
                {
                    query = query.Where(i => i.SavingCost < 20000);
                }
                else if (savingCostRange == "gte20k")
                {
                    query = query.Where(i => i.SavingCost >= 20000);
                }
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Join with WorkflowHistory to get approval history
            var approvalHistoryQuery = query
                .SelectMany(i => i.WorkflowHistories, (idea, workflowHistory) => new
                {
                    Idea = idea,
                    WorkflowHistory = workflowHistory
                })
                .OrderBy(x => x.Idea.IdeaCode)
                .ThenBy(x => x.WorkflowHistory.Timestamp)
                .Select(x => new ApprovalHistoryItemDto
                {
                    IdeaNumber = x.Idea.IdeaCode,
                    ApprovalId = x.WorkflowHistory.Id,
                    IdeaStatus = x.Idea.CurrentStatus,
                    CurrentStage = "S" + x.Idea.CurrentStage,
                    StageSequence = x.WorkflowHistory.ToStage ?? 0,
                    ApprovalDate = x.WorkflowHistory.Timestamp,
                    Approver = x.WorkflowHistory.ActorUser.Employee != null
                        ? x.WorkflowHistory.ActorUser.Employee.NAME
                        : "N/A",
                    LatestUpdateDate = x.Idea.UpdatedDate ?? x.Idea.SubmittedDate,
                    LastUpdatedDays = (int)(DateTime.Now - (x.Idea.UpdatedDate ?? x.Idea.SubmittedDate)).TotalDays,
                    ImplementedDivision = x.Idea.TargetDivision.NameDivision,
                    ImplementedDepartment = x.Idea.TargetDepartment.NameDepartment
                });

            return await approvalHistoryQuery.ToListAsync();
        }

        public async Task<IQueryable<ApprovalHistoryItemDto>> GetApprovalHistoryListQueryAsync(
            DateTime? startDate = null, DateTime? endDate = null,
            string? selectedDivision = null, int? selectedStage = null,
            string? savingCostRange = null, string? initiatorName = null,
            string? initiatorBadgeNumber = null, string? ideaId = null,
            string? initiatorDivision = null, string? selectedStatus = null)
        {
            var query = _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted);

            // Apply date filters
            if (startDate.HasValue)
            {
                var startOfDay = startDate.Value.Date;
                query = query.Where(i => i.SubmittedDate >= startOfDay);
            }
            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply division filter
            if (!string.IsNullOrEmpty(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply saving cost filter
            if (!string.IsNullOrEmpty(savingCostRange))
            {
                if (savingCostRange == "lt20k")
                {
                    query = query.Where(i => i.SavingCost < 20000);
                }
                else if (savingCostRange == "gte20k")
                {
                    query = query.Where(i => i.SavingCost >= 20000);
                }
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Join with WorkflowHistory to get approval history with pagination
            var orderedQuery = query
                .SelectMany(i => i.WorkflowHistories, (idea, workflowHistory) => new
                {
                    Idea = idea,
                    WorkflowHistory = workflowHistory
                })
                .OrderBy(x => x.Idea.IdeaCode)
                .ThenBy(x => x.WorkflowHistory.Timestamp)
                .Select(x => new ApprovalHistoryItemDto
                {
                    IdeaNumber = x.Idea.IdeaCode,
                    ApprovalId = x.WorkflowHistory.Id,
                    IdeaStatus = x.Idea.CurrentStatus,
                    CurrentStage = "S" + x.Idea.CurrentStage,
                    StageSequence = x.WorkflowHistory.ToStage ?? 0,
                    ApprovalDate = x.WorkflowHistory.Timestamp,
                    Approver = x.WorkflowHistory.ActorUser.Employee != null
                        ? x.WorkflowHistory.ActorUser.Employee.NAME
                        : "N/A",
                    LatestUpdateDate = x.Idea.UpdatedDate ?? x.Idea.SubmittedDate,
                    LastUpdatedDays = (int)(DateTime.Now - (x.Idea.UpdatedDate ?? x.Idea.SubmittedDate)).TotalDays,
                    ImplementedDivision = x.Idea.TargetDivision.NameDivision,
                    ImplementedDepartment = x.Idea.TargetDepartment.NameDepartment
                });

            return await Task.FromResult(orderedQuery);
        }

        public async Task<List<IdeaCostSavingDto>> GetIdeaCostSavingListAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? selectedDivision = null,
            int? selectedStage = null,
            string? savingCostRange = null,
            string? initiatorName = null,
            string? initiatorBadgeNumber = null,
            string? ideaId = null,
            string? initiatorDivision = null,
            string? selectedStatus = null)
        {
            var query = _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted);

            // Apply date filters
            if (startDate.HasValue)
            {
                var startOfDay = startDate.Value.Date;
                query = query.Where(i => i.SubmittedDate >= startOfDay);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply saving cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                if (savingCostRange == "lt20k")
                    query = query.Where(i => i.SavingCost < 20000);
                else if (savingCostRange == "gte20k")
                    query = query.Where(i => i.SavingCost >= 20000);
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var costSavingData = await query
                .OrderByDescending(i => i.SubmittedDate)
                .Select(i => new IdeaCostSavingDto
                {
                    IdeaId = i.IdeaCode,
                    SavingCostValidated = i.SavingCostValidated ?? i.SavingCost,
                    IdeaCategory = i.Category.CategoryName ?? "N/A",
                    CurrentStage = "S" + i.CurrentStage,
                    IdeaFlowValidated = i.SavingCostValidated == null ? "not_validated" :
                                       (i.SavingCostValidated.Value >= 20000 ? "more_than_20" : "less_than_20")
                })
                .ToListAsync();

            return costSavingData;
        }

        public async Task<IQueryable<IdeaCostSavingDto>> GetIdeaCostSavingListQueryAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? selectedDivision = null,
            int? selectedStage = null,
            string? savingCostRange = null,
            string? initiatorName = null,
            string? initiatorBadgeNumber = null,
            string? ideaId = null,
            string? initiatorDivision = null,
            string? selectedStatus = null)
        {
            var query = _ideaRepository.GetQueryableWithIncludes()
                .Where(i => !i.IsDeleted);

            // Apply date filters
            if (startDate.HasValue)
            {
                var startOfDay = startDate.Value.Date;
                query = query.Where(i => i.SubmittedDate >= startOfDay);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.SubmittedDate <= endOfDay);
            }

            // Apply division filter
            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            // Apply stage filter
            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply saving cost filter
            if (!string.IsNullOrWhiteSpace(savingCostRange))
            {
                if (savingCostRange == "lt20k")
                    query = query.Where(i => i.SavingCost < 20000);
                else if (savingCostRange == "gte20k")
                    query = query.Where(i => i.SavingCost >= 20000);
            }

            // Apply Initiator Name filter
            if (!string.IsNullOrWhiteSpace(initiatorName))
            {
                query = query.Where(i => i.InitiatorUser.Employee.NAME.Contains(initiatorName));
            }

            // Apply Initiator Badge Number filter
            if (!string.IsNullOrWhiteSpace(initiatorBadgeNumber))
            {
                query = query.Where(i => i.InitiatorUser.Employee.EMP_ID.Contains(initiatorBadgeNumber));
            }

            // Apply Idea Id filter
            if (!string.IsNullOrWhiteSpace(ideaId))
            {
                query = query.Where(i => i.IdeaCode.Contains(ideaId));
            }

            // Apply Initiator Division filter
            if (!string.IsNullOrWhiteSpace(initiatorDivision))
            {
                query = query.Where(i => i.InitiatorUser.Employee.DIVISION == initiatorDivision);
            }

            // Apply Status filter
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            var orderedQuery = query
                .OrderByDescending(i => i.SubmittedDate)
                .Select(i => new IdeaCostSavingDto
                {
                    IdeaId = i.IdeaCode,
                    SavingCostValidated = i.SavingCostValidated ?? i.SavingCost,
                    IdeaCategory = i.Category.CategoryName ?? "N/A",
                    CurrentStage = "S" + i.CurrentStage,
                    IdeaFlowValidated = i.SavingCostValidated == null ? "not_validated" :
                                       (i.SavingCostValidated.Value >= 20000 ? "more_than_20" : "less_than_20")
                });

            return await Task.FromResult(orderedQuery);
        }

        public async Task<bool> IsIdeaNameExistsAsync(string ideaName, long? excludeIdeaId = null)
        {
            return await _ideaRepository.IsIdeaNameExistsAsync(ideaName, excludeIdeaId);
        }

        #endregion

        #region Edit & Delete

        /// <summary>
        /// Update existing idea with new data
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateIdeaAsync(Models.Entities.Idea updatedIdea, List<IFormFile>? newFiles, List<long>? attachmentIdsToDelete = null)
        {
            try
            {
                // Get existing idea
                var idea = await _ideaRepository.GetByIdAsync(updatedIdea.Id);
                if (idea == null)
                {
                    return (false, "Idea not found.");
                }

                // Check if idea is already deleted
                if (idea.IsDeleted)
                {
                    return (false, "Cannot edit deleted idea.");
                }

                // VALIDATE FILES FIRST if new files are uploaded
                if (newFiles != null && newFiles.Any())
                {
                    var fileValidation = _fileUploadService.ValidateFiles(newFiles);
                    if (!fileValidation.IsValid)
                    {
                        return (false, fileValidation.ErrorMessage);
                    }
                }

                // Validate Idea Name uniqueness (exclude current idea)
                var ideaNameExists = await _ideaRepository.IsIdeaNameExistsAsync(updatedIdea.IdeaName, updatedIdea.Id);
                if (ideaNameExists)
                {
                    return (false, "An idea with this name already exists. Please use a different name.");
                }

                // Update idea properties from updatedIdea entity
                idea.ToDivisionId = updatedIdea.ToDivisionId;
                idea.ToDepartmentId = updatedIdea.ToDepartmentId;
                idea.CategoryId = updatedIdea.CategoryId;
                idea.EventId = updatedIdea.EventId;
                idea.IdeaName = updatedIdea.IdeaName;
                idea.IdeaIssueBackground = updatedIdea.IdeaIssueBackground;
                idea.IdeaSolution = updatedIdea.IdeaSolution;
                idea.SavingCost = updatedIdea.SavingCost;
                idea.UpdatedDate = DateTime.Now;

                // Handle new file uploads if any
                if (newFiles != null && newFiles.Any())
                {
                    var existingFilesCount = _fileUploadService.GetExistingFilesCount(idea.IdeaCode, _webHostEnvironment.WebRootPath);
                    var newAttachmentPaths = await _fileUploadService.HandleFileUploadsAsync(
                        newFiles,
                        idea.IdeaCode,
                        _webHostEnvironment.WebRootPath,
                        stage: idea.CurrentStage,
                        existingFilesCount: existingFilesCount
                    );

                    // Append new files to existing attachments
                    var existingFiles = string.IsNullOrEmpty(idea.AttachmentFiles)
                        ? new List<string>()
                        : idea.AttachmentFiles.Split(';').ToList();

                    existingFiles.AddRange(newAttachmentPaths);
                    idea.AttachmentFiles = string.Join(";", existingFiles);
                }

                // Save changes
                await _ideaRepository.UpdateAsync(idea);

                return (true, $"Idea '{updatedIdea.IdeaName}' has been successfully updated!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating idea {IdeaId}", updatedIdea.Id);
                return (false, $"Error updating idea: {ex.Message}");
            }
        }

        /// <summary>
        /// Soft delete idea by setting IsDeleted to true
        /// </summary>
        public async Task<(bool Success, string Message)> SoftDeleteIdeaAsync(long ideaId, string username)
        {
            try
            {
                // Get the idea
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    return (false, "Idea not found.");
                }

                // Check if already deleted
                if (idea.IsDeleted)
                {
                    return (false, "Idea is already deleted.");
                }

                // Get current user for authorization check
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Authorization: Only allow initiator or Superuser to delete
                if (idea.InitiatorUserId != user.EmployeeId && user.Role.RoleName != "Superuser") // Changed: compare with EmployeeId
                {
                    return (false, "You are not authorized to delete this idea.");
                }

                // Perform soft delete
                var deleteResult = await _ideaRepository.SoftDeleteAsync(ideaId);
                if (!deleteResult)
                {
                    return (false, "Failed to delete idea.");
                }

                _logger.LogInformation("Idea {IdeaCode} soft deleted by user {Username}", idea.IdeaCode, username);
                return (true, $"Idea '{idea.IdeaCode}' has been successfully deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting idea {IdeaId}", ideaId);
                return (false, $"Error deleting idea: {ex.Message}");
            }
        }

        #endregion

        #region Inactive Management

        /// <summary>
        /// Reactivate inactive idea and send notification to approvers (Superuser only)
        /// </summary>
        public async Task<(bool Success, string Message)> ReactivateIdeaAsync(long ideaId, string activatedBy)
        {
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.WorkflowHistories)
                    .Include(i => i.Workflow)
                    .Include(i => i.InitiatorUser)
                        .ThenInclude(u => u.Employee)
                    .Include(i => i.TargetDivision)
                    .Include(i => i.TargetDepartment)
                    .Include(i => i.Category)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);

                if (idea == null)
                {
                    return (false, "Idea not found.");
                }

                if (!idea.IsRejected || idea.CurrentStatus != "Inactive")
                {
                    return (false, "Only inactive ideas can be reactivated.");
                }

                // ========================================
                // STEP 1: Cari status sebelumnya dari WorkflowHistory
                // ========================================
                var lastAutoReject = idea.WorkflowHistories
                    .Where(h => h.Action == "Auto-Rejected")
                    .OrderByDescending(h => h.Timestamp)
                    .FirstOrDefault();

                string previousStatus = "Waiting Approval S1"; // Default fallback

                if (lastAutoReject?.Comments != null)
                {
                    // Extract dari comment: "Previous status: Waiting Approval S2"
                    var match = System.Text.RegularExpressions.Regex.Match(
                        lastAutoReject.Comments,
                        @"Previous status: (.+)$"
                    );
                    if (match.Success)
                    {
                        previousStatus = match.Groups[1].Value;
                    }
                }
                else
                {
                    // Fallback: reconstruct dari CurrentStage
                    if (idea.CurrentStage == 0)
                    {
                        previousStatus = "Waiting Approval S1";
                    }
                    else if (idea.CurrentStage >= idea.MaxStage)
                    {
                        previousStatus = "Completed"; // Edge case
                    }
                    else
                    {
                        previousStatus = $"Waiting Approval S{idea.CurrentStage + 1}";
                    }
                }

                // ========================================
                // STEP 2: REACTIVATE idea
                // ========================================
                idea.IsRejected = false;
                idea.CurrentStatus = previousStatus; // Restore status sebelumnya
                idea.RejectedReason = null;
                idea.UpdatedDate = DateTime.Now; // Reset 60 day counter
                idea.CompletedDate = null;

                // ========================================
                // STEP 3: Log ke WorkflowHistory
                // ========================================
                var activator = await _userRepository.GetByUsernameAsync(activatedBy);
                if (activator != null)
                {
                    var history = new WorkflowHistory
                    {
                        IdeaId = ideaId,
                        ActorUserId = activator.Id,
                        FromStage = idea.CurrentStage,
                        ToStage = idea.CurrentStage,
                        Action = "Reactivated",
                        Comments = $"Idea reactivated by {activator.Employee?.NAME} ({activator.EmployeeId})",
                        Timestamp = DateTime.Now
                    };
                    _context.WorkflowHistories.Add(history);
                }

                await _context.SaveChangesAsync();

                // Email will be sent in background by controller
                return (true, $"✅ Idea {idea.IdeaCode} successfully reactivated!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating idea {IdeaId}", ideaId);
                return (false, $"Error reactivating idea: {ex.Message}");
            }
        }

        #endregion

        #region Rejected Management

        /// <summary>
        /// Reactivate rejected idea (manually rejected by approver) - Superuser only
        /// </summary>
        public async Task<(bool Success, string Message)> ReactivateRejectedIdeaAsync(long ideaId, string activatedBy)
        {
            try
            {
                var idea = await _context.Ideas
                    .Include(i => i.WorkflowHistories)
                    .Include(i => i.Workflow)
                    .Include(i => i.InitiatorUser)
                        .ThenInclude(u => u.Employee)
                    .Include(i => i.TargetDivision)
                    .Include(i => i.TargetDepartment)
                    .Include(i => i.Category)
                    .FirstOrDefaultAsync(i => i.Id == ideaId);

                if (idea == null)
                {
                    return (false, "Idea not found.");
                }

                // Validate: Only manually rejected ideas can be reactivated via this method
                if (!idea.IsRejected || !idea.CurrentStatus.StartsWith("Rejected S"))
                {
                    return (false, "Only manually rejected ideas can be reactivated via this action.");
                }

                // ========================================
                // STEP 1: Extract stage number from rejection status
                // ========================================
                // CurrentStatus format: "Rejected S0", "Rejected S1", "Rejected S2", etc.
                // We need to restore to "Waiting Approval S{X}" at the SAME stage
                var stageMatch = System.Text.RegularExpressions.Regex.Match(
                    idea.CurrentStatus,
                    @"Rejected S(\d+)"
                );

                int rejectedStage = idea.CurrentStage; // Fallback to current stage
                if (stageMatch.Success && int.TryParse(stageMatch.Groups[1].Value, out int extractedStage))
                {
                    rejectedStage = extractedStage;
                }

                // ========================================
                // STEP 2: Determine target status based on stage
                // ========================================
                string targetStatus;

                if (rejectedStage == 0)
                {
                    // Rejected at S0 → Restore to "Waiting Approval S1"
                    targetStatus = "Waiting Approval S1";
                }
                else if (rejectedStage == 1)
                {
                    // Rejected at S1 → Check if team is assigned
                    var hasLeader = idea.IdeaImplementators.Any(ii => ii.Role == "Leader");
                    var hasMember = idea.IdeaImplementators.Any(ii => ii.Role == "Member");

                    if (hasLeader && hasMember)
                    {
                        // Team already assigned → "Waiting Approval S2"
                        targetStatus = "Waiting Approval S2";
                    }
                    else
                    {
                        // No team → "Waiting Team Assignment"
                        targetStatus = "Waiting Team Assignment";
                    }
                }
                else if (rejectedStage == 2)
                {
                    // Rejected at S2 → Check if milestone is created
                    if (idea.IsMilestoneCreated)
                    {
                        // Milestone exists → "Waiting Approval S3"
                        targetStatus = $"Waiting Approval S{rejectedStage + 1}";
                    }
                    else
                    {
                        // No milestone → "Waiting Milestone Creation"
                        targetStatus = "Waiting Milestone Creation";
                    }
                }
                else
                {
                    // For other stages → "Waiting Approval S{X+1}"
                    targetStatus = $"Waiting Approval S{rejectedStage + 1}";
                }

                // ========================================
                // STEP 3: REACTIVATE idea
                // ========================================
                idea.IsRejected = false;
                idea.CurrentStatus = targetStatus; // Restore to approval status
                idea.CurrentStage = rejectedStage; // Ensure stage is correct
                idea.RejectedReason = null;
                idea.UpdatedDate = DateTime.Now; // Reset 60 day counter
                idea.CompletedDate = null;

                // ========================================
                // STEP 4: Log ke WorkflowHistory
                // ========================================
                var activator = await _userRepository.GetByUsernameAsync(activatedBy);
                if (activator != null)
                {
                    var history = new WorkflowHistory
                    {
                        IdeaId = ideaId,
                        ActorUserId = activator.Id,
                        FromStage = rejectedStage,
                        ToStage = rejectedStage, // Same stage
                        Action = "Reactivated",
                        Comments = $"Rejected idea reactivated by {activator.Employee?.NAME} ({activator.EmployeeId})",
                        Timestamp = DateTime.Now
                    };
                    _context.WorkflowHistories.Add(history);
                }

                await _context.SaveChangesAsync();

                // Email will be sent in background by controller
                return (true, $"✅ Idea {idea.IdeaCode} successfully reactivated!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating rejected idea {IdeaId}", ideaId);
                return (false, $"Error reactivating idea: {ex.Message}");
            }
        }

        #endregion
    }
}
