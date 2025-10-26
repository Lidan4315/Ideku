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

        /// <summary>
        /// Get all milestones for a specific idea
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>List of milestones</returns>
        Task<IEnumerable<Models.Entities.Milestone>> GetMilestonesByIdeaIdAsync(long ideaId);

        /// <summary>
        /// Get milestone by ID with all related data
        /// </summary>
        /// <param name="milestoneId">Milestone ID</param>
        /// <returns>Milestone with related entities</returns>
        Task<Models.Entities.Milestone?> GetMilestoneByIdAsync(long milestoneId);

        /// <summary>
        /// Create a new milestone for an idea
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <param name="title">Milestone title</param>
        /// <param name="note">Milestone description/note</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="status">Milestone status</param>
        /// <param name="creatorName">Creator name</param>
        /// <param name="creatorEmployeeId">Creator employee ID</param>
        /// <param name="picUserIds">List of user IDs to assign as PICs</param>
        /// <returns>Success result with created milestone or error message</returns>
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

        /// <summary>
        /// Update an existing milestone
        /// </summary>
        /// <param name="milestoneId">Milestone ID</param>
        /// <param name="title">Updated title</param>
        /// <param name="note">Updated note</param>
        /// <param name="startDate">Updated start date</param>
        /// <param name="endDate">Updated end date</param>
        /// <param name="status">Updated status</param>
        /// <param name="picUserIds">Updated list of PIC user IDs</param>
        /// <returns>Success result with updated milestone or error message</returns>
        Task<(bool Success, string Message, Models.Entities.Milestone? Milestone)> UpdateMilestoneAsync(
            long milestoneId,
            string title,
            string? note,
            DateTime startDate,
            DateTime endDate,
            string status,
            List<long>? picUserIds = null);

        /// <summary>
        /// Delete a milestone
        /// </summary>
        /// <param name="milestoneId">Milestone ID</param>
        /// <returns>Success result with message</returns>
        Task<(bool Success, string Message)> DeleteMilestoneAsync(long milestoneId);

        /// <summary>
        /// Get available implementators for PIC assignment from idea implementators with their roles
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>List of implementators with role information</returns>
        Task<IEnumerable<ImplementatorForPICDto>> GetAvailablePICUsersAsync(long ideaId);

        /// <summary>
        /// Validate milestone dates and business rules
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Validation result with message</returns>
        (bool IsValid, string Message) ValidateMilestoneDates(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Check if user can manage milestones for a specific idea
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>True if user can manage milestones</returns>
        Task<bool> CanUserManageMilestonesAsync(string username, long ideaId);
    }
}