using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public interface IMilestoneRepository
    {
        /// <summary>
        /// Gets IQueryable with all necessary includes for efficient pagination and filtering
        /// Only includes ideas with CurrentStage >= 2
        /// </summary>
        /// <returns>IQueryable of Ideas with S2+ stage and related entities included</returns>
        IQueryable<Idea> GetIdeasWithMilestoneEligibility();

        /// <summary>
        /// Get milestone by ID with all related entities
        /// </summary>
        /// <param name="id">Milestone ID</param>
        /// <returns>Milestone with related entities or null if not found</returns>
        Task<Milestone?> GetMilestoneByIdAsync(long id);

        /// <summary>
        /// Get all milestones for a specific idea
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>List of milestones for the idea</returns>
        Task<IEnumerable<Milestone>> GetMilestonesByIdeaIdAsync(long ideaId);

        /// <summary>
        /// Create a new milestone
        /// </summary>
        /// <param name="milestone">Milestone to create</param>
        /// <returns>Created milestone</returns>
        Task<Milestone> CreateMilestoneAsync(Milestone milestone);

        /// <summary>
        /// Update an existing milestone
        /// </summary>
        /// <param name="milestone">Milestone to update</param>
        /// <returns>Updated milestone</returns>
        Task<Milestone> UpdateMilestoneAsync(Milestone milestone);

        /// <summary>
        /// Delete a milestone
        /// </summary>
        /// <param name="id">Milestone ID to delete</param>
        /// <returns>True if successful, false if milestone not found</returns>
        Task<bool> DeleteMilestoneAsync(long id);

        /// <summary>
        /// Check if an idea is eligible for milestone creation (CurrentStage >= 2)
        /// </summary>
        /// <param name="ideaId">Idea ID to check</param>
        /// <returns>True if idea is S2+, false otherwise</returns>
        Task<bool> IsIdeaMilestoneEligibleAsync(long ideaId);

        /// <summary>
        /// Get milestone PICs for a specific milestone
        /// </summary>
        /// <param name="milestoneId">Milestone ID</param>
        /// <returns>List of MilestonePIC entities</returns>
        Task<IEnumerable<MilestonePIC>> GetMilestonePICsAsync(long milestoneId);

        /// <summary>
        /// Add PICs to a milestone
        /// </summary>
        /// <param name="milestonePICs">List of MilestonePIC entities to add</param>
        /// <returns>Created MilestonePIC entities</returns>
        Task<IEnumerable<MilestonePIC>> AddMilestonePICsAsync(IEnumerable<MilestonePIC> milestonePICs);

        /// <summary>
        /// Remove all PICs from a milestone and add new ones
        /// </summary>
        /// <param name="milestoneId">Milestone ID</param>
        /// <param name="newPICs">New list of MilestonePIC entities</param>
        /// <returns>Updated MilestonePIC entities</returns>
        Task<IEnumerable<MilestonePIC>> UpdateMilestonePICsAsync(long milestoneId, IEnumerable<MilestonePIC> newPICs);
    }
}