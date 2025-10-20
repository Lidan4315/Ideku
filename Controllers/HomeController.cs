using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ideku.Models;
using Ideku.Services.Idea;
using Ideku.Services.Lookup;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Ideku.Controllers;

[Authorize]
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
        string? savingCostRange = null)
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
                savingCostRange);

            // Get divisions for filter dropdown (reuse existing LookupService)
            ViewBag.Divisions = await _lookupService.GetDivisionsAsync();

            // Get available stages from database
            var stages = await _ideaService.GetAvailableStagesAsync();
            ViewBag.AvailableStages = stages.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = s.ToString(),
                Text = $"Stage S{s}"
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
        string? savingCostRange = null)
    {
        try
        {
            var data = await _ideaService.GetIdeasByStatusChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange);
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
        string? savingCostRange = null)
    {
        try
        {
            var data = await _ideaService.GetIdeasByDivisionChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange);
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
        string? savingCostRange = null)
    {
        try
        {
            var data = await _ideaService.GetIdeasByDepartmentChartAsync(divisionId, startDate, endDate, selectedDivision, selectedStage, savingCostRange);
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
        string? savingCostRange = null)
    {
        try
        {
            var data = await _ideaService.GetIdeasByAllDepartmentsChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange);
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
        string? savingCostRange = null)
    {
        try
        {
            var data = await _ideaService.GetInitiativeByStageAndDivisionChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Initiative by Stage and Division chart");
            return Json(new { success = false, message = "Error loading chart data" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardStatistics(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? selectedDivision = null,
        int? selectedStage = null,
        string? savingCostRange = null)
    {
        try
        {
            var username = User.Identity?.Name ?? "";
            var dashboardData = await _ideaService.GetDashboardDataAsync(username, startDate, endDate, selectedDivision, selectedStage, savingCostRange);
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
        string? savingCostRange = null)
    {
        var username = User.Identity?.Name ?? "";
        _logger.LogInformation("ExportDashboard started - User: {Username}, DateRange: {StartDate} to {EndDate}, Filters: Division={Division}, Stage={Stage}, SavingCost={SavingCost}",
            username, startDate, endDate, selectedDivision, selectedStage, savingCostRange);

        try
        {
            // Fetch all required data with filters
            var dashboardData = await _ideaService.GetDashboardDataAsync(username, startDate, endDate, selectedDivision, selectedStage, savingCostRange);
            var statusData = await _ideaService.GetIdeasByStatusChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange);
            var divisionData = await _ideaService.GetIdeasByDivisionChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange);
            var departmentData = await _ideaService.GetIdeasByAllDepartmentsChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange);
            var stageData = await _ideaService.GetInitiativeByStageAndDivisionChartAsync(startDate, endDate, selectedDivision, selectedStage, savingCostRange);

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

        // Auto-fit columns
        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        sheet.Column(1).Width = 25;
        sheet.Column(2).Width = 20;
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

        // Auto-fit columns
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
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

        // Auto-fit columns
        if (sheet.Dimension != null)
        {
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
