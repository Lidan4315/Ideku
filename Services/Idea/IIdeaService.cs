using Ideku.ViewModels;
using Ideku.ViewModels.Common;
using Ideku.Models.Entities;
using Ideku.Models.Statistics;

namespace Ideku.Services.Idea
{
    public interface IIdeaService
    {
        Task<CreateIdeaViewModel> PrepareCreateViewModelAsync(string username);
        Task<(bool Success, string Message, Models.Entities.Idea? CreatedIdea)> CreateIdeaAsync(CreateIdeaViewModel model, List<IFormFile>? files);
        Task<IQueryable<Models.Entities.Idea>> GetUserIdeasAsync(string username);
        Task<object?> GetEmployeeByBadgeNumberAsync(string badgeNumber);
        Task<IQueryable<Models.Entities.Idea>> GetAllIdeasQueryAsync(string username);
        Task<User?> GetUserByUsernameAsync(string username);

        // Dashboard
        Task<DashboardData> GetDashboardDataAsync(string username, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetIdeasByStatusChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetIdeasByDivisionChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetIdeasByDepartmentChartAsync(string divisionId, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetIdeasByAllDepartmentsChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<object> GetInitiativeByStageAndDivisionChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<List<WLChartData>> GetWLChartDataAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<List<IdeaListItemDto>> GetIdeasListAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<PagedResult<IdeaListItemDto>> GetIdeasListPagedAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<List<TeamRoleItemDto>> GetTeamRoleListAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<PagedResult<TeamRoleItemDto>> GetTeamRoleListPagedAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<List<ApprovalHistoryItemDto>> GetApprovalHistoryListAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<PagedResult<ApprovalHistoryItemDto>> GetApprovalHistoryListPagedAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<List<IdeaCostSavingDto>> GetIdeaCostSavingListAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);
        Task<PagedResult<IdeaCostSavingDto>> GetIdeaCostSavingListPagedAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null, string? selectedStatus = null);

        // Filters
        Task<List<int>> GetAvailableStagesAsync();
        Task<List<string>> GetAvailableStatusesAsync();

        // Validation
        Task<bool> IsIdeaNameExistsAsync(string ideaName, long? excludeIdeaId = null);

        // Edit & Delete
        Task<(bool Success, string Message)> UpdateIdeaAsync(EditIdeaViewModel model, List<IFormFile>? newFiles);
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
