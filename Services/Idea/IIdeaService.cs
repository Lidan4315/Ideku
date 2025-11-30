using Ideku.ViewModels.Common;
using Ideku.Models.Entities;
using Ideku.Models.Statistics;

namespace Ideku.Services.Idea
{
    public interface IIdeaService
    {
        // PrepareCreateViewModelAsync REMOVED - Controller will handle ViewModel population
        Task<(bool Success, string Message, Models.Entities.Idea? CreatedIdea)> CreateIdeaAsync(Models.Entities.Idea idea, List<IFormFile>? files);
        Task<IQueryable<Models.Entities.Idea>> GetUserIdeasAsync(string username);
        Task<object?> GetEmployeeByBadgeNumberAsync(string badgeNumber);
        Task<IQueryable<Models.Entities.Idea>> GetAllIdeasQueryAsync(string username);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmployeeIdAsync(string employeeId);

        // Dashboard
        Task<DashboardData> GetDashboardDataAsync(string username, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetIdeasByStatusChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetIdeasByDivisionChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetIdeasByDepartmentChartAsync(string divisionId, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetIdeasByAllDepartmentsChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetInitiativeByStageAndDivisionChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<List<WLChartData>> GetWLChartDataAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<IQueryable<IdeaListItemDto>> GetIdeasListQueryAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<IQueryable<TeamRoleItemDto>> GetTeamRoleListQueryAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<IQueryable<ApprovalHistoryItemDto>> GetApprovalHistoryListQueryAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<IQueryable<IdeaCostSavingDto>> GetIdeaCostSavingListQueryAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);

        // Filters
        Task<List<int>> GetAvailableStagesAsync();
        Task<List<string>> GetAvailableStatusesAsync();

        // Validation
        Task<bool> IsIdeaNameExistsAsync(string ideaName, long? excludeIdeaId = null);

        // Edit & Delete
        Task<(bool Success, string Message)> UpdateIdeaAsync(Models.Entities.Idea idea, List<IFormFile>? newFiles, List<long>? attachmentIdsToDelete = null);
        Task<(bool Success, string Message)> SoftDeleteIdeaAsync(long ideaId, string username);

        // ========================================================================
        // Inactive Management (Auto-Rejected - 60 Days Without Approval)
        // ========================================================================
        Task<(bool Success, string Message)> ReactivateIdeaAsync(long ideaId, string activatedBy);
        Task SendReactivationEmailAsync(long ideaId);

        // ========================================================================
        // Rejected Management (Manually Rejected by Approver)
        // ========================================================================
        Task<(bool Success, string Message)> ReactivateRejectedIdeaAsync(long ideaId, string activatedBy);
        Task SendReactivateRejectedEmailAsync(long ideaId);
    }
}
