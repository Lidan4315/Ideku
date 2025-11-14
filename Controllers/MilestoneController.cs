using Ideku.Services.Milestone;
using Ideku.Services.Lookup;
using Ideku.Services.Idea;
using Ideku.Services.Workflow;
using Ideku.ViewModels.Milestone;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Ideku.Helpers;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Ideku.Controllers
{
    [Authorize]
    [ModuleAuthorize("milestone")]
    public class MilestoneController : Controller
    {
        private readonly IMilestoneService _milestoneService;
        private readonly ILookupService _lookupService;
        private readonly IIdeaService _ideaService;
        private readonly IWorkflowService _workflowService;
        private readonly ILogger<MilestoneController> _logger;

        public MilestoneController(
            IMilestoneService milestoneService,
            ILookupService lookupService,
            IIdeaService ideaService,
            IWorkflowService workflowService,
            ILogger<MilestoneController> logger)
        {
            _milestoneService = milestoneService;
            _lookupService = lookupService;
            _ideaService = ideaService;
            _workflowService = workflowService;
            _logger = logger;
        }

        // GET: /Milestone or /Milestone/Index
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
            try
            {
                var pagedIdeas = await _milestoneService.GetMilestoneEligibleIdeasAsync(
                    page, pageSize, searchTerm, selectedDivision, selectedDepartment, selectedCategory, selectedStage, selectedStatus);

                // Get lookup data for filters
                var divisions = await _lookupService.GetDivisionsAsync();
                var categories = await _lookupService.GetCategoriesAsync();

                // Get available stages from database (only S2 and above for milestone-eligible ideas)
                var stages = await _ideaService.GetAvailableStagesAsync();
                var milestoneStages = stages.Where(s => s >= 2).ToList();

                var viewModel = new MilestoneListViewModel
                {
                    PagedIdeas = pagedIdeas,
                    SearchTerm = searchTerm,
                    SelectedDivision = selectedDivision,
                    SelectedDepartment = selectedDepartment,
                    SelectedCategory = selectedCategory,
                    SelectedStage = selectedStage,
                    SelectedStatus = selectedStatus,
                    AvailableStages = milestoneStages.Select(s => new SelectListItem
                    {
                        Value = s.ToString(),
                        Text = $"Stage S{s}"
                    }).ToList()
                };

                // Pass lookup data to view
                ViewBag.Divisions = divisions;
                ViewBag.Categories = categories;

                if (!string.IsNullOrWhiteSpace(selectedDivision))
                {
                    ViewBag.Departments = await _lookupService.GetDepartmentsByDivisionAsync(selectedDivision);
                }

                // Get unique statuses for filter
                ViewBag.Statuses = pagedIdeas.Items
                    .Select(i => i.CurrentStatus)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading milestone list");
                TempData["ErrorMessage"] = "An error occurred while loading the milestone list.";
                return View(new MilestoneListViewModel());
            }
        }

        // GET: /Milestone/Detail/{ideaId}
        public async Task<IActionResult> Detail(long ideaId)
        {
            try
            {
                var idea = await _milestoneService.GetMilestoneEligibleIdeaByIdAsync(ideaId);
                if (idea == null)
                {
                    TempData["ErrorMessage"] = "Idea not found or not eligible for milestone management.";
                    return RedirectToAction(nameof(Index));
                }

                var milestones = await _milestoneService.GetMilestonesByIdeaIdAsync(ideaId);
                var availablePICUsers = await _milestoneService.GetAvailablePICUsersAsync(ideaId);

                var viewModel = new MilestoneDetailViewModel
                {
                    Idea = idea,
                    Milestones = milestones,
                    AvailablePICUsers = availablePICUsers,
                    IsEligibleForMilestones = true // Already filtered in service
                };

                _logger.LogInformation("[MilestoneDetail] IdeaId={IdeaId}, CurrentStage={Stage}, IsMilestoneCreated={IsMilestoneCreated}, CurrentStatus={Status}",
                    ideaId, idea.CurrentStage, idea.IsMilestoneCreated, idea.CurrentStatus);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading milestone detail for idea {IdeaId}", ideaId);
                TempData["ErrorMessage"] = "An error occurred while loading the milestone details.";
                return RedirectToAction(nameof(Index));
            }
        }


        // AJAX: Create milestone from modal
        [HttpPost]
        public async Task<IActionResult> CreateMilestone(
            long ideaId,
            string title,
            string? note,
            DateTime startDate,
            DateTime endDate,
            string status,
            string creatorName,
            string creatorEmployeeId,
            List<long>? selectedPICUserIds = null)
        {
            try
            {
                var result = await _milestoneService.CreateMilestoneAsync(
                    ideaId, title, note, startDate, endDate, status, creatorName, creatorEmployeeId, selectedPICUserIds);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating milestone via AJAX for idea {IdeaId}", ideaId);
                return Json(new { success = false, message = "An error occurred while creating the milestone." });
            }
        }

        // POST: /Milestone/UpdateMilestone (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateMilestone([FromBody] UpdateMilestoneRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided." });
                }

                // Authorization handled by ModuleAuthorize attribute
                var result = await _milestoneService.UpdateMilestoneAsync(
                    request.MilestoneId,
                    request.Title,
                    request.Note,
                    request.StartDate,
                    request.EndDate,
                    request.Status,
                    request.SelectedPICUserIds);

                if (result.Success)
                {
                    return Json(new { success = true, message = "Milestone updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating milestone {MilestoneId}", request.MilestoneId);
                return Json(new { success = false, message = "An error occurred while updating the milestone." });
            }
        }

        // POST: /Milestone/DeleteMilestone (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteMilestone(long milestoneId)
        {
            try
            {
                // Authorization handled by ModuleAuthorize attribute
                var result = await _milestoneService.DeleteMilestoneAsync(milestoneId);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting milestone {MilestoneId}", milestoneId);
                return Json(new { success = false, message = "An error occurred while deleting the milestone." });
            }
        }

        // POST: /Milestone/SendToStage3Approval
        [HttpPost]
        [ModuleAuthorize("milestone_send_approval")]
        public async Task<IActionResult> SendToStage3Approval(long ideaId)
        {
            try
            {
                // Authorization handled by ModuleAuthorize attribute

                // Get idea
                var idea = await _milestoneService.GetMilestoneEligibleIdeaByIdAsync(ideaId);
                if (idea == null)
                {
                    return Json(new { success = false, message = "Idea not found." });
                }

                // Validate: must be Stage 2
                if (idea.CurrentStage != 2)
                {
                    return Json(new { success = false, message = "Only Stage 2 ideas can be sent to Stage 3 approval." });
                }

                // Validate: must have milestone created
                if (!idea.IsMilestoneCreated)
                {
                    return Json(new { success = false, message = "Please create at least one milestone before requesting Stage 3 approval." });
                }

                // Validate: not already approved
                if (idea.CurrentStatus == "Approved")
                {
                    return Json(new { success = false, message = "This idea has already been fully approved." });
                }

                // Submit to next stage approval (S3)
                var result = await _workflowService.SubmitForNextStageApprovalAsync(ideaId, User.Identity!.Name!);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Idea {IdeaId} sent to Stage 3 approval by {Username}", ideaId, User.Identity!.Name);
                    return Json(new { success = true, message = result.SuccessMessage ?? "Idea has been successfully sent to Stage 3 approvers for review." });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage ?? "Failed to send to Stage 3 approval." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending idea {IdeaId} to Stage 3 approval", ideaId);
                return Json(new { success = false, message = "An error occurred while sending to Stage 3 approval." });
            }
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
            try
            {
                // Validate pagination parameters
                pageSize = Math.Max(5, Math.Min(100, pageSize));
                page = Math.Max(1, page);

                // Get milestone eligible ideas with filters
                var pagedResult = await _milestoneService.GetMilestoneEligibleIdeasAsync(
                    page, pageSize, searchTerm, selectedDivision, selectedDepartment, selectedCategory, selectedStage, selectedStatus);

                // Return JSON with paginated results
                return Json(new
                {
                    success = true,
                    ideas = pagedResult.Items.Select(i => new
                    {
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
                        implementators = i.IdeaImplementators.Select(impl => new
                        {
                            name = impl.User.Name,
                            role = impl.Role
                        }).ToList(),
                        submittedDate = i.SubmittedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        detailUrl = Url.Action("Detail", new { ideaId = i.Id })
                    }),
                    pagination = new
                    {
                        currentPage = pagedResult.Page,
                        pageSize = pagedResult.PageSize,
                        totalCount = pagedResult.TotalCount,
                        totalItems = pagedResult.TotalCount,
                        totalPages = pagedResult.TotalPages,
                        hasPreviousPage = pagedResult.HasPrevious,
                        hasNextPage = pagedResult.HasNext,
                        hasPrevious = pagedResult.HasPrevious,
                        hasNext = pagedResult.HasNext,
                        firstItemIndex = pagedResult.FirstItemIndex,
                        lastItemIndex = pagedResult.LastItemIndex
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering milestone ideas");
                return Json(new { success = false, message = "Error loading data" });
            }
        }

        // AJAX: Get departments by division
        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByDivision(string divisionId)
        {
            try
            {
                var departments = await _lookupService.GetDepartmentsByDivisionForAjaxAsync(divisionId);
                return Json(new { success = true, departments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments for division {DivisionId}", divisionId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Export Milestone to Excel
        [HttpGet]
        [Route("Milestone/ExportToExcel")]
        public async Task<IActionResult> ExportToExcel(
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            var username = User.Identity?.Name ?? "";
            _logger.LogInformation("ExportMilestone started - User: {Username}, Filters: SearchTerm={SearchTerm}, Division={Division}, Department={Department}, Category={Category}, Stage={Stage}, Status={Status}",
                username, searchTerm, selectedDivision, selectedDepartment, selectedCategory, selectedStage, selectedStatus);

            try
            {
                // Get base queryable (same as Index method)
                var ideasQuery = await _milestoneService.GetMilestoneEligibleIdeasAsync(
                    1, int.MaxValue, searchTerm, selectedDivision, selectedDepartment, selectedCategory, selectedStage, selectedStatus);

                // Get all filtered results (no pagination)
                var ideas = ideasQuery.Items.ToList();

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
                var worksheet = package.Workbook.Worksheets.Add("Milestone");

                // Row 1: Filter Summary
                CreateFilterSummary(worksheet, searchTerm, divisionName, selectedDepartment, categoryName, selectedStage, selectedStatus);

                // Row 3: Headers
                CreateMilestoneHeaders(worksheet);

                // Row 4+: Data
                PopulateMilestoneData(worksheet, ideas);

                // Generate file
                var fileName = $"IdeKU_Milestone_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var fileBytes = package.GetAsByteArray();

                _logger.LogInformation("ExportMilestone completed - User: {Username}, File: {FileName}, TotalRecords: {Count}, Size: {Size} bytes",
                    username, fileName, ideas.Count, fileBytes.Length);

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportMilestone failed - User: {Username}", username);
                TempData["ErrorMessage"] = $"Error exporting milestone data: {ex.Message}";
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
                ? $"Milestone | Export Date: {DateTime.Now:yyyy-MM-dd} | Filters: {string.Join(", ", filters)}"
                : $"Milestone | Export Date: {DateTime.Now:yyyy-MM-dd} | Filters: None (All Data)";

            sheet.Cells["A1:L1"].Merge = true;
            sheet.Cells["A1"].Value = filterText;
            sheet.Cells["A1"].Style.Font.Size = 11;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            sheet.Row(1).Height = 25;
        }

        // Helper: Create column headers in row 3
        private void CreateMilestoneHeaders(ExcelWorksheet sheet)
        {
            var headers = new[]
            {
                "Idea ID",
                "Idea Title",
                "Initiator",
                "Implementor",
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
        private void PopulateMilestoneData(ExcelWorksheet sheet, List<Models.Entities.Idea> ideas)
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

                // Column 4: Implementor(s) - Multi-valued with line breaks
                var implementors = idea.IdeaImplementators?
                    .Select(imp => $"{imp.User?.Name ?? "Unknown"} ({imp.Role ?? "Member"})")
                    .ToList() ?? new List<string>();

                if (implementors.Any())
                {
                    sheet.Cells[row, 4].Value = string.Join("\n", implementors);
                    sheet.Cells[row, 4].Style.WrapText = true;
                }
                else
                {
                    sheet.Cells[row, 4].Value = "";
                }

                // Column 5: Division
                sheet.Cells[row, 5].Value = idea.TargetDivision?.NameDivision ?? "";

                // Column 6: Department
                sheet.Cells[row, 6].Value = idea.TargetDepartment?.NameDepartment ?? "";

                // Column 7: Category
                sheet.Cells[row, 7].Value = idea.Category?.CategoryName ?? "";

                // Column 8: Event (optional)
                sheet.Cells[row, 8].Value = idea.Event?.EventName ?? "";

                // Column 9: Stage
                sheet.Cells[row, 9].Value = $"S{idea.CurrentStage}";
                sheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Column 10: Saving Cost (currency format)
                sheet.Cells[row, 10].Value = idea.SavingCost;
                sheet.Cells[row, 10].Style.Numberformat.Format = "$#,##0";

                // Column 11: Status
                sheet.Cells[row, 11].Value = idea.CurrentStatus ?? "";

                // Column 12: Submitted Date (date format)
                sheet.Cells[row, 12].Value = idea.SubmittedDate;
                sheet.Cells[row, 12].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";

                // Apply borders to all cells in this row
                for (int col = 1; col <= 12; col++)
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
            sheet.Column(4).Width = Math.Max(sheet.Column(4).Width, 30);  // Implementor
            sheet.Column(10).Width = Math.Max(sheet.Column(10).Width, 15); // Saving Cost
        }
    }

    // Request model for AJAX UpdateMilestone
    public class UpdateMilestoneRequest
    {
        public long MilestoneId { get; set; }
        public long IdeaId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<long> SelectedPICUserIds { get; set; } = new();
    }
}