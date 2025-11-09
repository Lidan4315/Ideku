using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ideku.Models;
using Ideku.Models.Statistics;
using Ideku.Services.Idea;
using Ideku.Services.Lookup;
using Ideku.Helpers;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Ideku.Controllers;

[ModuleAuthorize("dashboard")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IIdeaService _ideaService;
    private readonly ILookupService _lookupService;

    // Excel sheet formatting constants
    private const int EXCEL_TITLE_ROW = 1;
    private const int EXCEL_HEADER_ROW = 3;
    private const int EXCEL_DATA_START_ROW = 4;
    private const string EXCEL_TITLE_MERGE_RANGE = "A1:D1";
    private const string EXCEL_HEADER_RANGE = "A3:B3";
    private const int EXCEL_TITLE_FONT_SIZE = 14;
    private const int EXCEL_SUMMARY_TITLE_FONT_SIZE = 16;

    public HomeController(ILogger<HomeController> logger, IIdeaService ideaService, ILookupService lookupService)
    {
        _logger = logger;
        _ideaService = ideaService;
        _lookupService = lookupService;
    }

    public async Task<IActionResult> Index(
        string? selectedDivision = null,
        int? selectedStage = null,
        string? savingCostRange = null,
        string? initiatorName = null,
        string? initiatorBadgeNumber = null,
        string? ideaId = null,
        string? initiatorDivision = null,
        string? selectedStatus = null)
    {
        try
        {
            var username = User.Identity?.Name ?? "";
            var dashboardData = await _ideaService.GetDashboardDataAsync(
                username,
                null,
                null,
                selectedDivision,
                selectedStage,
                savingCostRange,
                initiatorName,
                initiatorBadgeNumber,
                ideaId,
                initiatorDivision,
                selectedStatus);

            // Get divisions for filter dropdown (reuse existing LookupService)
            ViewBag.Divisions = await _lookupService.GetDivisionsAsync();

            // Get available stages from database
            var stages = await _ideaService.GetAvailableStagesAsync();
            ViewBag.AvailableStages = stages.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = s.ToString(),
                Text = $"Stage S{s}"
            }).ToList();

            // Get available statuses from database
            var statuses = await _ideaService.GetAvailableStatusesAsync();
            ViewBag.AvailableStatuses = statuses.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = s,
                Text = s
            }).ToList();

            // Store filter values for view
            ViewBag.SelectedDivision = selectedDivision;
            ViewBag.SelectedStage = selectedStage;
            ViewBag.SavingCostRange = savingCostRange;

            return View(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            TempData["ErrorMessage"] = "Error loading dashboard data";
            return View();
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetIdeasByStatusChart(
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
        try
        {
            var data = await _ideaService.GetIdeasByStatusChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Ideas by Status chart");
            return Json(new { success = false, message = "Error loading chart data" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetIdeasByDivisionChart(
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
        try
        {
            var data = await _ideaService.GetIdeasByDivisionChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Ideas by Division chart");
            return Json(new { success = false, message = "Error loading chart data" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetIdeasByDepartmentChart(
        string divisionId,
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
        try
        {
            var data = await _ideaService.GetIdeasByDepartmentChartAsync(divisionId, startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Ideas by Department chart");
            return Json(new { success = false, message = "Error loading chart data" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetIdeasByAllDepartmentsChart(
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
        try
        {
            var data = await _ideaService.GetIdeasByAllDepartmentsChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Ideas by All Departments chart");
            return Json(new { success = false, message = "Error loading chart data" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetInitiativeByStageAndDivisionChart(
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
        try
        {
            var data = await _ideaService.GetInitiativeByStageAndDivisionChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Initiative by Stage and Division chart");
            return Json(new { success = false, message = "Error loading chart data" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetWLChart(
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
        try
        {
            var data = await _ideaService.GetWLChartDataAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading WL chart");
            return Json(new { success = false, message = "Error loading chart data" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetIdeasList(
        int page = 1,
        int pageSize = 10,
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
        try
        {
            // Validate pagination parameters
            pageSize = Helpers.PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            var pagedResult = await _ideaService.GetIdeasListPagedAsync(
                page, pageSize, startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);

            return Json(new {
                success = true,
                data = pagedResult.Items,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ideas list");
            return Json(new { success = false, message = "Error loading ideas list" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTeamRoleList(
        int page = 1,
        int pageSize = 10,
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
        try
        {
            pageSize = Helpers.PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            var pagedResult = await _ideaService.GetTeamRoleListPagedAsync(
                page, pageSize, startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);

            return Json(new
            {
                success = true,
                data = pagedResult.Items,
                pagination = new
                {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading team role list");
            return Json(new { success = false, message = "Error loading team role list" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetApprovalHistoryList(
        int page = 1, int pageSize = 10,
        DateTime? startDate = null, DateTime? endDate = null,
        string? selectedDivision = null, int? selectedStage = null,
        string? savingCostRange = null, string? initiatorName = null,
        string? initiatorBadgeNumber = null,
        string? ideaId = null,
        string? initiatorDivision = null,
        string? selectedStatus = null)
    {
        try
        {
            pageSize = Helpers.PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            var pagedResult = await _ideaService.GetApprovalHistoryListPagedAsync(
                page, pageSize, startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);

            return Json(new
            {
                success = true,
                data = pagedResult.Items,
                pagination = new
                {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading approval history list");
            return Json(new { success = false, message = "Error loading approval history list" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetIdeaCostSavingList(
        int page = 1,
        int pageSize = 10,
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
        try
        {
            pageSize = Helpers.PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            var pagedResult = await _ideaService.GetIdeaCostSavingListPagedAsync(
                page, pageSize, startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);

            return Json(new
            {
                success = true,
                data = pagedResult.Items,
                pagination = new
                {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading idea cost saving list");
            return Json(new { success = false, message = "Error loading idea cost saving list" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardStatistics(
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
        try
        {
            var username = User.Identity?.Name ?? "";
            var dashboardData = await _ideaService.GetDashboardDataAsync(username, startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            return Json(new {
                success = true,
                totalIdeas = dashboardData.TotalIdeas,
                totalSavingCost = dashboardData.TotalSavingCost,
                validatedSavingCost = dashboardData.ValidatedSavingCost
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard statistics");
            return Json(new { success = false, message = "Error loading statistics" });
        }
    }

    [HttpGet]
    [Route("Home/ExportDashboard")]
    public async Task<IActionResult> ExportDashboard(
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
        var username = User.Identity?.Name ?? "";
        _logger.LogInformation("ExportDashboard started - User: {Username}, DateRange: {StartDate} to {EndDate}, Filters: Division={Division}, Stage={Stage}, SavingCost={SavingCost}",
            username, startDate, endDate, selectedDivision, selectedStage, savingCostRange);

        try
        {
            // Fetch all required data with filters
            var dashboardData = await _ideaService.GetDashboardDataAsync(username, startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var statusData = await _ideaService.GetIdeasByStatusChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var divisionData = await _ideaService.GetIdeasByDivisionChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var departmentData = await _ideaService.GetIdeasByAllDepartmentsChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var stageData = await _ideaService.GetInitiativeByStageAndDivisionChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var wlData = await _ideaService.GetWLChartDataAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var ideasListData = await _ideaService.GetIdeasListAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var teamRoleData = await _ideaService.GetTeamRoleListAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var ideaCostSavingData = await _ideaService.GetIdeaCostSavingListAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);
            var approvalHistoryData = await _ideaService.GetApprovalHistoryListAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange, initiatorName, initiatorBadgeNumber, ideaId, initiatorDivision, selectedStatus);

            // EPPlus 7: Set license context for non-commercial use
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();

            // Create all sheets in dashboard order
            var summarySheet = package.Workbook.Worksheets.Add("Summary");
            CreateSummarySheet(summarySheet, dashboardData, startDate, endDate);

            var divisionSheet = package.Workbook.Worksheets.Add("Count of Initiative by Division");
            CreateSimpleDataSheet(divisionSheet, divisionData, "Count of Initiative by Division", "Division");

            var departmentSheet = package.Workbook.Worksheets.Add("Count of Initiative by Department");
            CreateSimpleDataSheet(departmentSheet, departmentData, "Count of Initiative by Department", "Department");

            var stageSheet = package.Workbook.Worksheets.Add("Initiative by Stage and Division");
            CreateStageSheet(stageSheet, stageData);

            var statusSheet = package.Workbook.Worksheets.Add("Count of Initiative by Stage");
            CreateSimpleDataSheet(statusSheet, statusData, "Count of Initiative by Stage", "Stage");

            var wlSheet = package.Workbook.Worksheets.Add("Ideas by Workstream Leader");
            CreateWLSheet(wlSheet, wlData);

            var ideasListSheet = package.Workbook.Worksheets.Add("Ideas List");
            CreateIdeasListSheet(ideasListSheet, ideasListData);

            var teamRoleSheet = package.Workbook.Worksheets.Add("Team Role");
            CreateTeamRoleSheet(teamRoleSheet, teamRoleData);

            var ideaCostSavingSheet = package.Workbook.Worksheets.Add("Idea Cost Saving");
            CreateIdeaCostSavingSheet(ideaCostSavingSheet, ideaCostSavingData);

            var approvalHistorySheet = package.Workbook.Worksheets.Add("Approval History");
            CreateApprovalHistorySheet(approvalHistorySheet, approvalHistoryData);

            // Generate file
            var fileName = $"Dashboard_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var fileBytes = package.GetAsByteArray();

            _logger.LogInformation("ExportDashboard completed - User: {Username}, File: {FileName}, Size: {Size} bytes",
                username, fileName, fileBytes.Length);

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportDashboard failed - User: {Username}", username);
            TempData["ErrorMessage"] = $"Error exporting dashboard data: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    private void CreateSummarySheet(ExcelWorksheet sheet, dynamic dashboardData, DateTime? startDate, DateTime? endDate)
    {
        if (dashboardData == null)
        {
            sheet.Cells[$"A{EXCEL_DATA_START_ROW}"].Value = "No data available";
            return;
        }

        // Title
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Value = "Dashboard Summary Report";
        sheet.Cells[EXCEL_TITLE_MERGE_RANGE].Merge = true;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Size = EXCEL_SUMMARY_TITLE_FONT_SIZE;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Bold = true;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Date Range
        sheet.Cells["A2"].Value = "Period:";
        sheet.Cells["B2"].Value = startDate.HasValue && endDate.HasValue
            ? $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
            : "All Time";
        sheet.Cells["A2"].Style.Font.Bold = true;

        // Statistics Header
        sheet.Cells[$"A{EXCEL_DATA_START_ROW}"].Value = "Metric";
        sheet.Cells[$"B{EXCEL_DATA_START_ROW}"].Value = "Value";
        sheet.Cells[$"A{EXCEL_DATA_START_ROW}:B{EXCEL_DATA_START_ROW}"].Style.Font.Bold = true;
        sheet.Cells[$"A{EXCEL_DATA_START_ROW}:B{EXCEL_DATA_START_ROW}"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells[$"A{EXCEL_DATA_START_ROW}:B{EXCEL_DATA_START_ROW}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

        // Statistics Data
        int row = EXCEL_DATA_START_ROW + 1;
        sheet.Cells[$"A{row}"].Value = "Total Ideas";
        sheet.Cells[$"B{row}"].Value = dashboardData.TotalIdeas;

        row++;
        sheet.Cells[$"A{row}"].Value = "Total Saving Cost";
        sheet.Cells[$"B{row}"].Value = dashboardData.TotalSavingCost;
        sheet.Cells[$"B{row}"].Style.Numberformat.Format = "#,##0";

        row++;
        sheet.Cells[$"A{row}"].Value = "Validated Saving Cost";
        sheet.Cells[$"B{row}"].Value = dashboardData.ValidatedSavingCost;
        sheet.Cells[$"B{row}"].Style.Numberformat.Format = "#,##0";

        // Auto-fit columns to content
        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

        // Add borders to data section
        var dataRange = sheet.Cells[EXCEL_DATA_START_ROW, 1, row, 2];
        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    }

    /// <summary>
    /// Generic method to create simple data sheets (Stage, Division, Department)
    /// with title, header, and two-column data (label and count)
    /// </summary>
    private void CreateSimpleDataSheet(ExcelWorksheet sheet, dynamic data, string title, string labelHeader)
    {
        // Null check
        if (data == null || data.labels == null || data.datasets == null)
        {
            sheet.Cells[$"A{EXCEL_DATA_START_ROW}"].Value = "No data available";
            return;
        }

        // Title
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Value = title;
        sheet.Cells[EXCEL_TITLE_MERGE_RANGE].Merge = true;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Size = EXCEL_TITLE_FONT_SIZE;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Bold = true;

        // Header
        sheet.Cells[$"A{EXCEL_HEADER_ROW}"].Value = labelHeader;
        sheet.Cells[$"B{EXCEL_HEADER_ROW}"].Value = "Count";
        sheet.Cells[EXCEL_HEADER_RANGE].Style.Font.Bold = true;
        sheet.Cells[EXCEL_HEADER_RANGE].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells[EXCEL_HEADER_RANGE].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

        // Data
        int row = EXCEL_DATA_START_ROW;
        try
        {
            string[] labels = data.labels;
            int[] counts = data.datasets[0].data;

            for (int i = 0; i < labels.Length; i++)
            {
                sheet.Cells[$"A{row}"].Value = labels[i];
                sheet.Cells[$"B{row}"].Value = counts[i];
                row++;
            }
        }
        catch (Exception ex)
        {
            sheet.Cells[$"A{EXCEL_DATA_START_ROW}"].Value = $"Error: {ex.Message}";
        }

        // Auto-fit columns to content
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        // Add borders to all cells
        if (row > EXCEL_DATA_START_ROW)
        {
            var allCells = sheet.Cells[EXCEL_HEADER_ROW, 1, row - 1, 2];
            allCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }

    /// <summary>
    /// Create complex matrix sheet for Initiative by Stage and Division
    /// with dynamic columns based on divisions
    /// </summary>
    private void CreateStageSheet(ExcelWorksheet sheet, dynamic stageData)
    {
        // Null check
        if (stageData == null || stageData.labels == null || stageData.datasets == null)
        {
            sheet.Cells[$"A{EXCEL_DATA_START_ROW}"].Value = "No data available";
            return;
        }

        // Title
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Value = "Initiative by Stage and Division";
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Size = EXCEL_TITLE_FONT_SIZE;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Bold = true;

        try
        {
            string[] labels = stageData.labels;
            var datasets = stageData.datasets;

            // Create headers (Stage + each Division)
            sheet.Cells[EXCEL_HEADER_ROW, 1].Value = "Stage";
            int col = 2;
            foreach (var dataset in datasets)
            {
                string label = dataset.label;
                sheet.Cells[EXCEL_HEADER_ROW, col].Value = label;
                col++;
            }

            // Merge title across all columns (now we know total column count)
            int totalColumns = col - 1;
            sheet.Cells[1, 1, 1, totalColumns].Merge = true;

            // Style headers
            var headerRange = sheet.Cells[EXCEL_HEADER_ROW, 1, EXCEL_HEADER_ROW, col - 1];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            // Fill data rows
            int row = EXCEL_DATA_START_ROW;
            for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
            {
                sheet.Cells[row, 1].Value = labels[labelIndex];
                col = 2;
                foreach (var dataset in datasets)
                {
                    int[] data = dataset.data;
                    sheet.Cells[row, col].Value = data[labelIndex];
                    col++;
                }
                row++;
            }
        }
        catch (Exception ex)
        {
            sheet.Cells[$"A{EXCEL_DATA_START_ROW}"].Value = $"Error: {ex.Message}";
        }

        // Auto-fit columns to content
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            // Add borders to all data cells
            var allCells = sheet.Cells[sheet.Dimension.Address];
            allCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }

    /// <summary>
    /// Create WL sheet with WL data showing ideas by stage
    /// </summary>
    private void CreateWLSheet(ExcelWorksheet sheet, List<WLChartData> wlData)
    {
        // Title
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Value = "Ideas by Workstream Leader";
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Size = EXCEL_TITLE_FONT_SIZE;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Bold = true;

        // Find max stage dynamically
        int maxStage = 0;
        foreach (var wl in wlData)
        {
            foreach (var stageKey in wl.IdeasByStage.Keys)
            {
                if (int.TryParse(stageKey.Replace("S", ""), out int stageNum))
                {
                    if (stageNum > maxStage)
                    {
                        maxStage = stageNum;
                    }
                }
            }
        }

        // Headers
        int col = 1;
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Workstream Leader";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Employee ID";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Division";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Department";

        // Dynamic stage headers (S0, S1, ... S{maxStage})
        for (int stage = 0; stage <= maxStage; stage++)
        {
            sheet.Cells[EXCEL_HEADER_ROW, col++].Value = $"S{stage}";
        }

        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Total";

        // Calculate total columns and merge title
        int totalColumns = col - 1;
        sheet.Cells[EXCEL_TITLE_ROW, 1, EXCEL_TITLE_ROW, totalColumns].Merge = true;

        // Style headers
        var headerRange = sheet.Cells[EXCEL_HEADER_ROW, 1, EXCEL_HEADER_ROW, totalColumns];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

        // Data
        int row = EXCEL_DATA_START_ROW;
        foreach (var wl in wlData)
        {
            col = 1;
            sheet.Cells[row, col++].Value = wl.UserName;
            sheet.Cells[row, col++].Value = wl.EmployeeId;
            sheet.Cells[row, col++].Value = wl.Division;
            sheet.Cells[row, col++].Value = wl.Department;

            // Stage counts (dynamic from 0 to maxStage)
            for (int stage = 0; stage <= maxStage; stage++)
            {
                string stageKey = $"S{stage}";
                int count = wl.IdeasByStage.ContainsKey(stageKey) ? wl.IdeasByStage[stageKey] : 0;
                sheet.Cells[row, col++].Value = count;
            }

            sheet.Cells[row, col++].Value = wl.TotalIdeas;
            row++;
        }

        // Auto-fit columns to content
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        // Add borders to all cells
        if (sheet.Dimension != null)
        {
            var allCells = sheet.Cells[EXCEL_HEADER_ROW, 1, row - 1, totalColumns];
            allCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }

    private void CreateIdeasListSheet(ExcelWorksheet sheet, List<IdeaListItemDto> ideasListData)
    {
        // Title
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Value = "Ideas List";
        sheet.Cells["A1:L1"].Merge = true;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Size = EXCEL_TITLE_FONT_SIZE;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Bold = true;

        // Headers
        int col = 1;
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Idea ID";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Status";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Initiator B/N";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Initiator Name";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Initiator Division";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Implement on Division";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Implement on Department";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Idea Title";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Current Stage";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Submission Date";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Last Updated (Days)";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Idea Flow Validated";

        // Style headers
        var headerRange = sheet.Cells[EXCEL_HEADER_ROW, 1, EXCEL_HEADER_ROW, col - 1];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

        // Data
        int row = EXCEL_DATA_START_ROW;
        foreach (var idea in ideasListData)
        {
            col = 1;
            sheet.Cells[row, col++].Value = idea.IdeaNumber;
            sheet.Cells[row, col++].Value = idea.IdeaStatus;
            sheet.Cells[row, col++].Value = idea.InitiatorBN;
            sheet.Cells[row, col++].Value = idea.InitiatorName;
            sheet.Cells[row, col++].Value = idea.InitiatorDivision;
            sheet.Cells[row, col++].Value = idea.ImplementOnDivision;
            sheet.Cells[row, col++].Value = idea.ImplementOnDepartment;
            sheet.Cells[row, col++].Value = idea.IdeaTitle;
            sheet.Cells[row, col++].Value = idea.CurrentStage;
            sheet.Cells[row, col++].Value = idea.SubmissionDate.ToString("dd/MM/yyyy HH:mm");
            sheet.Cells[row, col++].Value = idea.LastUpdatedDays;

            // Idea Flow Validated - convert to display text
            string flowText = idea.IdeaFlowValidated switch
            {
                "more_than_20" => "More than $20k",
                "less_than_20" => "Less than $20k",
                "not_validated" => "Not Validated",
                _ => idea.IdeaFlowValidated
            };
            sheet.Cells[row, col++].Value = flowText;

            row++;
        }

        // Auto-fit columns 1-7 to content
        if (sheet.Dimension != null)
        {
            // Auto-fit all columns first
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            // Set fixed width for Idea Title column (column 8) so text wrapping works effectively
            sheet.Column(8).Width = 60;
        }

        // Enable text wrapping for Idea Title column
        if (sheet.Dimension != null)
        {
            var dataRange = sheet.Cells[EXCEL_DATA_START_ROW, 1, row - 1, 12];
            dataRange.Style.WrapText = true;
            dataRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
        }

        // Add borders to all cells
        if (sheet.Dimension != null)
        {
            var allCells = sheet.Cells[EXCEL_HEADER_ROW, 1, row - 1, 12];
            allCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }

    private void CreateTeamRoleSheet(ExcelWorksheet sheet, List<TeamRoleItemDto> teamRoleData)
    {
        // Title
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Value = "Team Role";
        sheet.Cells["A1:C1"].Merge = true;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Size = EXCEL_TITLE_FONT_SIZE;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Bold = true;

        // Headers - 3 columns
        int col = 1;
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Employee BN";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Team Role";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Idea ID";

        // Style headers
        var headerRange = sheet.Cells[EXCEL_HEADER_ROW, 1, EXCEL_HEADER_ROW, col - 1];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

        // Data
        int row = EXCEL_DATA_START_ROW;
        foreach (var item in teamRoleData)
        {
            col = 1;
            sheet.Cells[row, col++].Value = item.EmployeeBN;
            sheet.Cells[row, col++].Value = item.TeamRole;
            sheet.Cells[row, col++].Value = item.IdeaCode;
            row++;
        }

        // Auto-fit columns to content
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        // Add borders to all cells
        if (sheet.Dimension != null)
        {
            var allCells = sheet.Cells[EXCEL_HEADER_ROW, 1, row - 1, 3];
            allCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }

    private void CreateIdeaCostSavingSheet(ExcelWorksheet sheet, List<IdeaCostSavingDto> ideaCostSavingData)
    {
        // Title
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Value = "Idea Cost Saving";
        sheet.Cells["A1:E1"].Merge = true;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Size = EXCEL_TITLE_FONT_SIZE;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Bold = true;

        // Headers - 5 columns
        int col = 1;
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Idea Id";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "SavingCostValidated";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Idea Category";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Current Stage";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "ideaFlowValidated";

        // Style headers
        var headerRange = sheet.Cells[EXCEL_HEADER_ROW, 1, EXCEL_HEADER_ROW, col - 1];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

        // Data
        int row = EXCEL_DATA_START_ROW;
        foreach (var item in ideaCostSavingData)
        {
            col = 1;
            sheet.Cells[row, col++].Value = item.IdeaId;
            sheet.Cells[row, col++].Value = item.SavingCostValidated;
            sheet.Cells[row, col++].Value = item.IdeaCategory;
            sheet.Cells[row, col++].Value = item.CurrentStage;
            sheet.Cells[row, col++].Value = item.IdeaFlowValidated;
            row++;
        }

        // Format SavingCostValidated column as currency
        if (sheet.Dimension != null && row > EXCEL_DATA_START_ROW)
        {
            var savingCostColumn = sheet.Cells[EXCEL_DATA_START_ROW, 2, row - 1, 2];
            savingCostColumn.Style.Numberformat.Format = "$#,##0";
        }

        // Auto-fit columns to content
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        // Add borders to all cells
        if (sheet.Dimension != null)
        {
            var allCells = sheet.Cells[EXCEL_HEADER_ROW, 1, row - 1, 5];
            allCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }

    private void CreateApprovalHistorySheet(ExcelWorksheet sheet, List<ApprovalHistoryItemDto> approvalHistoryData)
    {
        // Title
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Value = "Approval History for IdeKU";
        sheet.Cells["A1:K1"].Merge = true;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Size = EXCEL_TITLE_FONT_SIZE;
        sheet.Cells[$"A{EXCEL_TITLE_ROW}"].Style.Font.Bold = true;

        // Headers - 11 columns
        int col = 1;
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Idea Number";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Approval ID";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Idea Status";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Current Stage";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Stage Name";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Approval Date";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Approver";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Latest Update Date";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Last Updated (Days)";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Implemented Division";
        sheet.Cells[EXCEL_HEADER_ROW, col++].Value = "Implemented Department";

        // Style headers
        var headerRange = sheet.Cells[EXCEL_HEADER_ROW, 1, EXCEL_HEADER_ROW, col - 1];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

        // Data
        int row = EXCEL_DATA_START_ROW;
        foreach (var item in approvalHistoryData)
        {
            col = 1;
            sheet.Cells[row, col++].Value = item.IdeaNumber;
            sheet.Cells[row, col++].Value = item.ApprovalId;
            sheet.Cells[row, col++].Value = item.IdeaStatus;
            sheet.Cells[row, col++].Value = item.CurrentStage;
            sheet.Cells[row, col++].Value = item.StageSequence;
            sheet.Cells[row, col++].Value = item.ApprovalDate;
            sheet.Cells[row, col++].Value = item.Approver;
            sheet.Cells[row, col++].Value = item.LatestUpdateDate;
            sheet.Cells[row, col++].Value = item.LastUpdatedDays;
            sheet.Cells[row, col++].Value = item.ImplementedDivision;
            sheet.Cells[row, col++].Value = item.ImplementedDepartment;

            // Format date columns
            sheet.Cells[row, 6].Style.Numberformat.Format = "m/d/yyyy h:mm:ss AM/PM";
            if (item.LatestUpdateDate != null)
            {
                sheet.Cells[row, 8].Style.Numberformat.Format = "m/d/yyyy h:mm:ss AM/PM";
            }

            row++;
        }

        // Auto-fit columns to content
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        // Add borders to all cells
        if (sheet.Dimension != null)
        {
            var allCells = sheet.Cells[EXCEL_HEADER_ROW, 1, row - 1, 11];
            allCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            allCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
