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
        Task<DashboardData> GetDashboardDataAsync(string username, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<object> GetIdeasByStatusChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<object> GetIdeasByDivisionChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<object> GetIdeasByDepartmentChartAsync(string divisionId, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<object> GetIdeasByAllDepartmentsChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<object> GetInitiativeByStageAndDivisionChartAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<List<WLChartData>> GetWLChartDataAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<List<IdeaListItemDto>> GetIdeasListAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<PagedResult<IdeaListItemDto>> GetIdeasListPagedAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<List<TeamRoleItemDto>> GetTeamRoleListAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<PagedResult<TeamRoleItemDto>> GetTeamRoleListPagedAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<List<ApprovalHistoryItemDto>> GetApprovalHistoryListAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<PagedResult<ApprovalHistoryItemDto>> GetApprovalHistoryListPagedAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<List<IdeaCostSavingDto>> GetIdeaCostSavingListAsync(DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);
        Task<PagedResult<IdeaCostSavingDto>> GetIdeaCostSavingListPagedAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null, string? selectedDivision = null, int? selectedStage = null, string? savingCostRange = null, string? initiatorName = null, string? initiatorBadgeNumber = null, string? ideaId = null, string? initiatorDivision = null);

        // Filters
        Task<List<int>> GetAvailableStagesAsync();
    }
}
