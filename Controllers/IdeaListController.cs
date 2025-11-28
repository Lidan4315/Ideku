using Ideku.Data.Repositories;
using Ideku.Services.Idea;
using Ideku.Services.IdeaRelation;
using Ideku.Services.Lookup;
using Ideku.Services.IdeaImplementators;
using Ideku.Services.Workflow;
using Ideku.Services.Notification;
using Ideku.ViewModels.IdeaList;
using Ideku.Extensions;
using Ideku.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("idea_list")]
    public class IdeaListController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly IUserRepository _userRepository;
        private readonly ILookupService _lookupService;
        private readonly IIdeaRelationService _ideaRelationService;
        private readonly IIdeaImplementatorService _implementatorService;
        private readonly ILogger<IdeaListController> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public IdeaListController(
            IIdeaService ideaService,
            IUserRepository userRepository,
            ILookupService lookupService,
            IIdeaRelationService ideaRelationService,
            IIdeaImplementatorService implementatorService,
            ILogger<IdeaListController> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _ideaService = ideaService;
            _userRepository = userRepository;
            _lookupService = lookupService;
            _ideaRelationService = ideaRelationService;
            _implementatorService = implementatorService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        // GET: /IdeaList or /IdeaList/Index
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Validate and normalize pagination parameters
            pageSize = PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            // Get user info for ViewBag
            var user = await _userRepository.GetByUsernameAsync(username);

            // Get ideas based on user role (superuser sees all, others filtered by role)
            var ideasQuery = await _ideaService.GetAllIdeasQueryAsync(username);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideasQuery = ideasQuery.Where(i => 
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm) ||
                    i.InitiatorUser.Name.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
            }

            if (selectedStage.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply pagination - this executes the database queries
            var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

            // Get lookup data for filters
            var divisions = await _lookupService.GetDivisionsAsync();
            var categories = await _lookupService.GetCategoriesAsync();

            // Get available stages from database
            var stages = await _ideaService.GetAvailableStagesAsync();

            var statuses = await _ideaService.GetAvailableStatusesAsync();

            var viewModel = new IdeaListViewModel
            {
                PagedIdeas = pagedResult,
                SearchTerm = searchTerm,
                SelectedDivision = selectedDivision,
                SelectedDepartment = selectedDepartment,
                SelectedCategory = selectedCategory,
                SelectedStage = selectedStage,
                SelectedStatus = selectedStatus,
                AvailableStages = stages.Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = $"Stage S{s}"
                }).ToList(),
                StatusOptions = statuses
            };

            // Pass lookup data to view
            ViewBag.Divisions = divisions;
            ViewBag.Categories = categories;
            ViewBag.UserRole = user?.Role?.RoleName ?? "";

            return View(viewModel);
        }

        // AJAX endpoint for real-time filtering
        [HttpGet]
        public async Task<IActionResult> FilterAllIdeas(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Validate pagination parameters
            pageSize = PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            // Get base queryable with role-based filtering
            var ideasQuery = await _ideaService.GetAllIdeasQueryAsync(username);

            // Apply filters (same logic as Index method)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideasQuery = ideasQuery.Where(i => 
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm) ||
                    i.InitiatorUser.Name.Contains(searchTerm)
                );
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (selectedStage.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStage == selectedStage.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
            }

            // Apply pagination
            var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);
            
            // Return JSON with paginated results
            return Json(new { 
                success = true, 
                ideas = pagedResult.Items.Select(i => new {
                    ideaCode = i.IdeaCode,
                    ideaName = i.IdeaName,
                    initiatorName = i.InitiatorUser?.Employee?.NAME,
                    initiatorBadge = i.InitiatorUser?.Employee?.EMP_ID,
                    divisionName = i.TargetDivision?.NameDivision,
                    departmentName = i.TargetDepartment?.NameDepartment,
                    categoryName = i.Category?.CategoryName,
                    eventName = i.Event?.EventName,
                    currentStage = i.CurrentStage,
                    savingCost = i.SavingCost,
                    savingCostValidated = i.SavingCostValidated,
                    currentStatus = i.CurrentStatus,
                    submittedDate = i.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    detailUrl = Url.Action("Detail", new { id = i.Id })
                }),
                pagination = new {
                    currentPage = pagedResult.Page,
                    pageSize = pagedResult.PageSize,
                    totalCount = pagedResult.TotalCount,
                    totalPages = pagedResult.TotalPages,
                    hasPrevious = pagedResult.HasPrevious,
                    hasNext = pagedResult.HasNext,
                    firstItemIndex = pagedResult.FirstItemIndex,
                    lastItemIndex = pagedResult.LastItemIndex
                }
            });
        }

        // AJAX endpoint for cascading dropdown
        [HttpGet]
        public async Task<JsonResult> GetDepartmentsByDivision(string divisionId)
        {
            if (string.IsNullOrWhiteSpace(divisionId))
            {
                return Json(new { success = true, departments = new List<object>() });
            }

            try
            {
                // Use real data from LookupService (returns List<object>)
                var departments = await _lookupService.GetDepartmentsByDivisionForAjaxAsync(divisionId);

                return Json(new { success = true, departments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: IdeaList/Details/{id}
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                // Get idea details
                var ideasQuery = await _ideaService.GetAllIdeasQueryAsync(User.Identity?.Name ?? "");
                var idea = await ideasQuery
                    .Where(i => i.Id == id)
                    .FirstOrDefaultAsync();

                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found";
                    return RedirectToAction("Index");
                }

                // Get implementators for the idea
                var implementators = await _implementatorService.GetImplementatorsByIdeaIdAsync(id);

                // Get available users for dropdown (server-side) - for Add Implementator modal
                var availableUsers = await _implementatorService.GetAvailableUsersForAssignmentAsync(id);

                // Get ALL users for Edit Team modal (including already assigned)
                var allUsers = await _implementatorService.GetAllUsersAsync();

                // Get user role for view (untuk show/hide reactivate button)
                var user = await _userRepository.GetByUsernameAsync(User.Identity?.Name ?? "");
                ViewBag.UserRole = user?.Role?.RoleName ?? "";

                // ========================================================================
                // CASE 1: Inactive Idea (Auto-Rejected - 60 Days Without Approval)
                // ========================================================================
                ViewBag.IsInactive = idea.IsRejected && idea.CurrentStatus == "Inactive";
                ViewBag.ShowReactivateButton = (user?.Role?.RoleName == "Superuser" && ViewBag.IsInactive);

                // ========================================================================
                // CASE 2: Rejected Idea (Manually Rejected by Approver)
                // ========================================================================
                ViewBag.IsRejected = idea.IsRejected && idea.CurrentStatus.StartsWith("Rejected S");
                ViewBag.ShowReactivateRejectedButton = (user?.Role?.RoleName == "Superuser" && ViewBag.IsRejected);

                // Pass dropdown data to view for edit modal
                ViewBag.Divisions = await _lookupService.GetDivisionsAsync();
                ViewBag.Categories = await _lookupService.GetCategoriesAsync();
                ViewBag.Events = await _lookupService.GetEventsAsync();

                var viewModel = new IdeaDetailViewModel
                {
                    Idea = idea,
                    Implementators = implementators.ToList(),
                    AvailableUsers = availableUsers.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = GetUserProperty(u, "id")?.ToString(),
                        Text = GetUserProperty(u, "displayText")?.ToString()
                    }).ToList(),
                    AllUsers = allUsers.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = GetUserProperty(u, "id")?.ToString(),
                        Text = GetUserProperty(u, "displayText")?.ToString()
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading idea detail: {IdeaId}", id);
                TempData["ErrorMessage"] = "Error loading idea details";
                return RedirectToAction("Index");
            }
        }

        // AJAX: Get implementators for an idea
        [HttpGet]
        public async Task<JsonResult> GetImplementators(long ideaId)
        {
            try
            {
                var implementators = await _implementatorService.GetImplementatorsByIdeaIdAsync(ideaId);

                var result = implementators.Select(i => new
                {
                    id = i.Id,
                    userId = i.UserId,
                    role = i.Role,
                    userName = i.User?.Name,
                    employeeId = i.User?.EmployeeId,
                    division = i.User?.Employee?.DivisionNavigation?.NameDivision,
                    department = i.User?.Employee?.DepartmentNavigation?.NameDepartment,
                    assignedDate = i.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting implementators for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "Error loading implementators" });
            }
        }

        // AJAX: Get available users for assignment
        [HttpGet]
        public async Task<JsonResult> GetAvailableUsers(long ideaId)
        {
            try
            {
                var availableUsers = await _implementatorService.GetAvailableUsersForAssignmentAsync(ideaId);
                return Json(new { success = true, data = availableUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available users for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "Error loading available users" });
            }
        }

        // AJAX: Assign multiple implementators in a single transaction
        [HttpPost]
        public async Task<JsonResult> AssignMultipleImplementators([FromBody] AssignMultipleImplementatorsRequest request)
        {
            try
            {
                if (request == null || request.Implementators == null || !request.Implementators.Any())
                {
                    return Json(new { success = false, message = "No implementators provided." });
                }

                // Convert DTO to tuple list
                var implementators = request.Implementators
                    .Select(i => (i.UserId, i.Role))
                    .ToList();

                var result = await _implementatorService.AssignMultipleImplementatorsAsync(
                    User.Identity!.Name!,
                    request.IdeaId,
                    implementators);

                if (result.Success)
                {
                    // Send email notifications in background (non-blocking)
                    SendTeamAssignmentEmailInBackground(request.IdeaId);

                    _logger.LogInformation("Successfully assigned {Count} implementators to idea {IdeaId} by {Username}",
                        implementators.Count, request.IdeaId, User.Identity!.Name);
                }

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning multiple implementators to idea {IdeaId}", request?.IdeaId);
                return Json(new { success = false, message = "An error occurred while assigning implementators." });
            }
        }

        // AJAX: Update team implementators (remove + add in single transaction)
        [HttpPost]
        public async Task<JsonResult> UpdateTeamImplementators([FromBody] UpdateTeamImplementatorsRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "Invalid request." });
                }

                // Convert DTO to tuple list
                var implementatorsToAdd = request.ImplementatorsToAdd
                    .Select(i => (i.UserId, i.Role))
                    .ToList();

                var result = await _implementatorService.UpdateTeamImplementatorsAsync(
                    User.Identity!.Name!,
                    request.IdeaId,
                    request.ImplementatorsToRemove,
                    implementatorsToAdd);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully updated team for idea {IdeaId}: removed {RemovedCount}, added {AddedCount} by {Username}",
                        request.IdeaId, request.ImplementatorsToRemove.Count, implementatorsToAdd.Count, User.Identity!.Name);
                }

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team implementators for idea {IdeaId}", request?.IdeaId);
                return Json(new { success = false, message = "An error occurred while updating team implementators." });
            }
        }

        // Helper method to get property from anonymous object
        private object? GetUserProperty(object user, string propertyName)
        {
            var type = user.GetType();
            var property = type.GetProperty(propertyName);
            return property?.GetValue(user);
        }

        // Send email notification in background after team assignment
        private void SendTeamAssignmentEmailInBackground(long ideaId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background team assignment email process for idea {IdeaId}", ideaId);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowService>();
                    var ideaRepository = scope.ServiceProvider.GetRequiredService<IIdeaRepository>();

                    var idea = await ideaRepository.GetByIdAsync(ideaId);
                    if (idea == null)
                    {
                        _logger.LogError("Idea {IdeaId} not found in background email task", ideaId);
                        return;
                    }

                    // Send email if status is "Waiting Approval S2" at Stage 1
                    // This covers: team assigned at S0 then approved to S1, OR team assigned directly at S1
                    if (idea.CurrentStage == 1 && idea.CurrentStatus == "Waiting Approval S2")
                    {
                        var approvers = await workflowService.GetApproversForNextStageAsync(idea);
                        if (!approvers.Any())
                        {
                            _logger.LogWarning("No approvers found for idea {IdeaId} in background email task", ideaId);
                            return;
                        }

                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        await notificationService.NotifyIdeaSubmitted(idea, approvers);

                        _logger.LogInformation("Background team assignment email sent successfully for idea {IdeaId} to {ApproverCount} approvers",
                            ideaId, approvers.Count);
                    }
                    else
                    {
                        _logger.LogInformation("No email sent for idea {IdeaId} - Stage: {Stage}, Status: {Status}",
                            ideaId, idea.CurrentStage, idea.CurrentStatus);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background team assignment email for idea {IdeaId}", ideaId);
                }
            });
        }

        // Export IdeaList to Excel
        [HttpGet]
        [Route("IdeaList/ExportToExcel")]
        public async Task<IActionResult> ExportToExcel(
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            var username = User.Identity?.Name ?? "";
            _logger.LogInformation("ExportIdeaList started - User: {Username}, Filters: SearchTerm={SearchTerm}, Division={Division}, Department={Department}, Category={Category}, Stage={Stage}, Status={Status}",
                username, searchTerm, selectedDivision, selectedDepartment, selectedCategory, selectedStage, selectedStatus);

            try
            {
                // Get base queryable with role-based filtering (same as Index method)
                var ideasQuery = await _ideaService.GetAllIdeasQueryAsync(username);

                // Apply all filters (same logic as Index and FilterAllIdeas)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    ideasQuery = ideasQuery.Where(i =>
                        i.IdeaCode.Contains(searchTerm) ||
                        i.IdeaName.Contains(searchTerm) ||
                        i.InitiatorUser.Name.Contains(searchTerm));
                }

                if (!string.IsNullOrWhiteSpace(selectedDivision))
                {
                    ideasQuery = ideasQuery.Where(i => i.ToDivisionId == selectedDivision);
                }

                if (!string.IsNullOrWhiteSpace(selectedDepartment))
                {
                    ideasQuery = ideasQuery.Where(i => i.ToDepartmentId == selectedDepartment);
                }

                if (selectedCategory.HasValue)
                {
                    ideasQuery = ideasQuery.Where(i => i.CategoryId == selectedCategory.Value);
                }

                if (selectedStage.HasValue)
                {
                    ideasQuery = ideasQuery.Where(i => i.CurrentStage == selectedStage.Value);
                }

                if (!string.IsNullOrWhiteSpace(selectedStatus))
                {
                    ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
                }

                // Execute query - get ALL matching results (no pagination)
                var ideas = await ideasQuery.ToListAsync();

                // Get lookup data for filter summary display
                var categoryName = "";
                if (selectedCategory.HasValue)
                {
                    var categories = await _lookupService.GetCategoriesAsync();
                    var category = categories.FirstOrDefault(c => c.Value == selectedCategory.Value.ToString());
                    categoryName = category?.Text ?? "";
                }

                var divisionName = "";
                if (!string.IsNullOrWhiteSpace(selectedDivision))
                {
                    var divisions = await _lookupService.GetDivisionsAsync();
                    var division = divisions.FirstOrDefault(d => d.Value == selectedDivision);
                    divisionName = division?.Text ?? "";
                }

                // EPPlus 7: Set license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();

                // Create worksheet
                var worksheet = package.Workbook.Worksheets.Add("Idea List");

                // Row 1: Filter Summary
                CreateFilterSummary(worksheet, searchTerm, divisionName, selectedDepartment, categoryName, selectedStage, selectedStatus);

                // Row 3: Headers
                CreateIdeaListHeaders(worksheet);

                // Row 4+: Data
                PopulateIdeaListData(worksheet, ideas);

                // Generate file
                var fileName = $"IdeKU_IdeaList_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var fileBytes = package.GetAsByteArray();

                _logger.LogInformation("ExportIdeaList completed - User: {Username}, File: {FileName}, TotalRecords: {Count}, Size: {Size} bytes",
                    username, fileName, ideas.Count, fileBytes.Length);

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportIdeaList failed - User: {Username}", username);
                TempData["ErrorMessage"] = $"Error exporting idea list: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper: Create filter summary in row 1
        private void CreateFilterSummary(ExcelWorksheet sheet, string? searchTerm, string? divisionName,
            string? departmentId, string? categoryName, int? stage, string? status)
        {
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                filters.Add($"Search: {searchTerm}");

            if (!string.IsNullOrWhiteSpace(divisionName))
                filters.Add($"Division: {divisionName}");

            if (!string.IsNullOrWhiteSpace(departmentId))
                filters.Add($"Department: {departmentId}");

            if (!string.IsNullOrWhiteSpace(categoryName))
                filters.Add($"Category: {categoryName}");

            if (stage.HasValue)
                filters.Add($"Stage: S{stage.Value}");

            if (!string.IsNullOrWhiteSpace(status))
                filters.Add($"Status: {status}");

            var filterText = filters.Any()
                ? $"Idea List | Export Date: {DateTime.Now:yyyy-MM-dd} | Filters: {string.Join(", ", filters)}"
                : $"Idea List | Export Date: {DateTime.Now:yyyy-MM-dd} | Filters: None (All Data)";

            sheet.Cells["A1:K1"].Merge = true;
            sheet.Cells["A1"].Value = filterText;
            sheet.Cells["A1"].Style.Font.Size = 11;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet.Row(1).Height = 25;
        }

        // Helper: Create column headers in row 3
        private void CreateIdeaListHeaders(ExcelWorksheet sheet)
        {
            var headers = new[]
            {
                "Idea ID",
                "Idea Title",
                "Initiator",
                "Division",
                "Department",
                "Category",
                "Event",
                "Stage",
                "Saving Cost",
                "Status",
                "Submitted Date"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[3, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            sheet.Row(3).Height = 20;
        }

        // Helper: Populate data rows starting from row 4
        private void PopulateIdeaListData(ExcelWorksheet sheet, List<Models.Entities.Idea> ideas)
        {
            if (!ideas.Any())
            {
                sheet.Cells["A4"].Value = "No data available";
                return;
            }

            int row = 4;
            foreach (var idea in ideas)
            {
                // Column 1: Idea ID
                sheet.Cells[row, 1].Value = idea.IdeaCode ?? "";

                // Column 2: Idea Title
                sheet.Cells[row, 2].Value = idea.IdeaName ?? "";
                sheet.Cells[row, 2].Style.WrapText = true;

                // Column 3: Initiator
                sheet.Cells[row, 3].Value = idea.InitiatorUser?.Employee?.NAME ?? "";

                // Column 4: Division
                sheet.Cells[row, 4].Value = idea.TargetDivision?.NameDivision ?? "";

                // Column 5: Department
                sheet.Cells[row, 5].Value = idea.TargetDepartment?.NameDepartment ?? "";

                // Column 6: Category
                sheet.Cells[row, 6].Value = idea.Category?.CategoryName ?? "";

                // Column 7: Event (optional)
                sheet.Cells[row, 7].Value = idea.Event?.EventName ?? "";

                // Column 8: Stage
                sheet.Cells[row, 8].Value = $"S{idea.CurrentStage}";
                sheet.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Column 9: Saving Cost (currency format)
                sheet.Cells[row, 9].Value = idea.SavingCost;
                sheet.Cells[row, 9].Style.Numberformat.Format = "$#,##0";

                // Column 10: Status
                sheet.Cells[row, 10].Value = idea.CurrentStatus ?? "";

                // Column 11: Submitted Date (date format)
                sheet.Cells[row, 11].Value = idea.SubmittedDate;
                sheet.Cells[row, 11].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";

                // Apply borders to all cells in this row
                for (int col = 1; col <= 11; col++)
                {
                    sheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    sheet.Cells[row, col].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                }

                row++;
            }

            // Auto-fit columns
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            // Set minimum column widths for better readability
            sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 15);  // Idea ID
            sheet.Column(2).Width = Math.Max(sheet.Column(2).Width, 40);  // Idea Title
            sheet.Column(3).Width = Math.Max(sheet.Column(3).Width, 20);  // Initiator
            sheet.Column(9).Width = Math.Max(sheet.Column(9).Width, 15);  // Saving Cost
        }

        /// <summary>
        /// Reactivate inactive idea (Superuser only)
        /// POST: /IdeaList/ReactivateIdea
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateIdea(long ideaId)
        {
            try
            {
                // Check if user is superuser
                var username = User.Identity?.Name ?? "";
                var user = await _userRepository.GetByUsernameAsync(username);

                if (user?.Role?.RoleName != "Superuser")
                {
                    TempData["ErrorMessage"] = "Only Superuser can reactivate inactive ideas.";
                    return RedirectToAction("Detail", new { id = ideaId });
                }

                // Call service to reactivate
                var result = await _ideaService.ReactivateIdeaAsync(ideaId, username);

                if (result.Success)
                {
                    // Send email notifications in background (non-blocking)
                    SendReactivationEmailInBackground(ideaId);

                    TempData["SuccessMessage"] = result.Message;
                    _logger.LogInformation("Idea {IdeaId} reactivated by {Username}", ideaId, username);
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    _logger.LogWarning("Failed to reactivate idea {IdeaId}: {Message}", ideaId, result.Message);
                }

                return RedirectToAction("Detail", new { id = ideaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating idea {IdeaId}", ideaId);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Detail", new { id = ideaId });
            }
        }

        private void SendReactivationEmailInBackground(long ideaId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background reactivation email process for idea {IdeaId}", ideaId);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var ideaService = scope.ServiceProvider.GetRequiredService<IIdeaService>();

                    await ideaService.SendReactivationEmailAsync(ideaId);

                    _logger.LogInformation("Background reactivation email sent successfully for idea {IdeaId}", ideaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background reactivation email for idea {IdeaId}", ideaId);
                }
            });
        }

        /// <summary>
        /// Reactivate rejected idea (manually rejected by approver) - Superuser only
        /// POST: /IdeaList/ReactivateRejectedIdea
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateRejectedIdea(long ideaId)
        {
            try
            {
                // Check if user is superuser
                var username = User.Identity?.Name ?? "";
                var user = await _userRepository.GetByUsernameAsync(username);

                if (user?.Role?.RoleName != "Superuser")
                {
                    TempData["ErrorMessage"] = "Only Superuser can reactivate rejected ideas.";
                    return RedirectToAction("Details", new { id = ideaId });
                }

                // Call service to reactivate rejected idea
                var result = await _ideaService.ReactivateRejectedIdeaAsync(ideaId, username);

                if (result.Success)
                {
                    // Send email notifications in background (non-blocking)
                    SendReactivateRejectedEmailInBackground(ideaId);

                    TempData["SuccessMessage"] = result.Message;
                    _logger.LogInformation("Rejected idea {IdeaId} reactivated by {Username}", ideaId, username);
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    _logger.LogWarning("Failed to reactivate rejected idea {IdeaId}: {Message}", ideaId, result.Message);
                }

                return RedirectToAction("Details", new { id = ideaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating rejected idea {IdeaId}", ideaId);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", new { id = ideaId });
            }
        }

        private void SendReactivateRejectedEmailInBackground(long ideaId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background reactivation email process for rejected idea {IdeaId}", ideaId);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var ideaService = scope.ServiceProvider.GetRequiredService<IIdeaService>();

                    await ideaService.SendReactivateRejectedEmailAsync(ideaId);

                    _logger.LogInformation("Background reactivation email sent successfully for rejected idea {IdeaId}", ideaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send background reactivation email for rejected idea {IdeaId}", ideaId);
                }
            });
        }

    }
}