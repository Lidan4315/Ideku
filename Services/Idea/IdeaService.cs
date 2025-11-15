using Ideku.Data.Repositories;
using Ideku.ViewModels;
using Ideku.ViewModels.Common;
using Ideku.Models.Entities;
using Ideku.Models.Statistics;
using Ideku.Services.WorkflowManagement;
using Ideku.Services.Lookup;
using Ideku.Services.UserManagement;
using Ideku.Services.Workflow;
using Ideku.Extensions;
using Microsoft.EntityFrameworkCore;

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

        public IdeaService(
            IIdeaRepository ideaRepository,
            IUserRepository userRepository,
            ILookupService lookupService,
            IWorkflowRepository workflowRepository,
            IWorkflowManagementService workflowManagementService,
            IEmployeeRepository employeeRepository,
            IWebHostEnvironment webHostEnvironment,
            IUserManagementService userManagementService,
            IWorkflowService workflowService)
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
                DivisionList = await _lookupService.GetDivisionsAsync(),
                CategoryList = await _lookupService.GetCategoriesAsync(),
                EventList = await _lookupService.GetEventsAsync(),
                DepartmentList = await _lookupService.GetDepartmentsByDivisionAsync("")
            };
        }

        public async Task<(bool Success, string Message, Models.Entities.Idea? CreatedIdea)> CreateIdeaAsync(CreateIdeaViewModel model, List<IFormFile>? files)
        {
            try
            {
                // VALIDATE FILES FIRST - before creating idea in database
                var fileValidation = Helpers.FileUploadHelper.ValidateFiles(files);
                if (!fileValidation.IsValid)
                {
                    return (false, fileValidation.ErrorMessage, null);
                }

                // Validate Idea Name uniqueness
                var ideaNameExists = await _ideaRepository.IsIdeaNameExistsAsync(model.IdeaName);
                if (ideaNameExists)
                {
                    return (false, "An idea with this name already exists. Please use a different name.", null);
                }

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

        public async Task<IQueryable<Models.Entities.Idea>> GetUserIdeasAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new UnauthorizedAccessException("User not authenticated");

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            // Get base queryable with all necessary includes
            return _ideaRepository.GetQueryableWithIncludes()
                .Where(idea => idea.InitiatorUserId == user.Id)
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
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

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

        #endregion

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
                // Superuser can see ALL ideas regardless of status or ownership
                return baseQuery.OrderByDescending(idea => idea.SubmittedDate)
                    .ThenByDescending(idea => idea.Id);
            }
            else if (user.Role.RoleName == "Workstream Leader")
            {
                var userDivision = user.Employee?.DIVISION;
                var userDepartment = user.Employee?.DEPARTEMENT;

                if (string.IsNullOrEmpty(userDivision))
                {
                    return Enumerable.Empty<Models.Entities.Idea>().AsQueryable();
                }

                // Workstream Leader can see:
                // 1. Ideas for their division and department
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

            // For other roles, return empty for now (will be implemented later)
            return Enumerable.Empty<Models.Entities.Idea>().AsQueryable();
        }

        #endregion
        
        #region Additional Helper Methods
        
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username);
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
                Approved = activeIdeas.Count(i => i.CurrentStatus == "Approved"),
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

        public async Task<ViewModels.Common.PagedResult<IdeaListItemDto>> GetIdeasListPagedAsync(
            int page,
            int pageSize,
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

            var pagedResult = await orderedQuery.ToPagedResultAsync(page, pageSize);
            return pagedResult;
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

        public async Task<PagedResult<TeamRoleItemDto>> GetTeamRoleListPagedAsync(
            int page,
            int pageSize,
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

            var pagedResult = await orderedQuery.ToPagedResultAsync(page, pageSize);
            return pagedResult;
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

        public async Task<PagedResult<ApprovalHistoryItemDto>> GetApprovalHistoryListPagedAsync(
            int page, int pageSize,
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

            var pagedResult = await orderedQuery.ToPagedResultAsync(page, pageSize);
            return pagedResult;
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

        public async Task<PagedResult<IdeaCostSavingDto>> GetIdeaCostSavingListPagedAsync(
            int page,
            int pageSize,
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

            var pagedResult = await orderedQuery.ToPagedResultAsync(page, pageSize);
            return pagedResult;
        }

        public async Task<bool> IsIdeaNameExistsAsync(string ideaName, long? excludeIdeaId = null)
        {
            return await _ideaRepository.IsIdeaNameExistsAsync(ideaName, excludeIdeaId);
        }

        #endregion
    }
}
