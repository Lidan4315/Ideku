using Ideku.Models.Entities;
using Ideku.ViewModels.Common;
using Ideku.ViewModels.Milestone;

namespace Ideku.Services.Milestone
{
    public interface IMilestoneService
    {
        /// <summary>
        /// Get paginated ideas that are eligible for milestone management (S2+)
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="searchTerm">Search term for filtering</param>
        /// <param name="selectedDivision">Division filter</param>
        /// <param name="selectedDepartment">Department filter</param>
        /// <param name="selectedCategory">Category filter</param>
        /// <param name="selectedStage">Stage filter</param>
        /// <param name="selectedStatus">Status filter</param>
        /// <returns>Paginated result of eligible ideas</returns>
        Task<PagedResult<Models.Entities.Idea>> GetMilestoneEligibleIdeasAsync(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null);

        /// <summary>
        /// Get idea by ID with milestone eligibility check
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>Idea if eligible for milestones, null otherwise</returns>
        Task<Models.Entities.Idea?> GetMilestoneEligibleIdeaByIdAsync(long ideaId);

        Task<IEnumerable<Models.Entities.Milestone>> GetMilestonesByIdeaIdAsync(long ideaId);

        Task<Models.Entities.Milestone?> GetMilestoneByIdAsync(long milestoneId);

        Task<(bool Success, string Message, Models.Entities.Milestone? Milestone)> CreateMilestoneAsync(
            long ideaId,
            string title,
            string? note,
            DateTime startDate,
            DateTime endDate,
            string status,
            string creatorName,
            string creatorEmployeeId,
            List<long>? picUserIds = null);

        Task<(bool Success, string Message, Models.Entities.Milestone? Milestone)> UpdateMilestoneAsync(
            long milestoneId,
            string title,
            string? note,
            DateTime startDate,
            DateTime endDate,
            string status,
            List<long>? picUserIds = null);

        Task<(bool Success, string Message)> DeleteMilestoneAsync(long milestoneId);

        Task<IEnumerable<ImplementatorForPICDto>> GetAvailablePICUsersAsync(long ideaId);

        (bool IsValid, string Message) ValidateMilestoneDates(DateTime startDate, DateTime endDate);
    }
}