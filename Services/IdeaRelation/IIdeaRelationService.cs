using Ideku.Models.Entities;

namespace Ideku.Services.IdeaRelation
{
    /// <summary>
    /// Service for managing idea relations with divisions
    /// Handles business logic for related divisions feature
    /// </summary>
    public interface IIdeaRelationService
    {
        /// <summary>
        /// Get available divisions for selection (excluding idea's target division)
        /// </summary>
        /// <param name="ideaId">ID of the idea</param>
        /// <returns>List of available divisions for selection</returns>
        Task<List<Division>> GetAvailableDivisionsAsync(long ideaId);

        /// <summary>
        /// Get workstream leaders from specified divisions
        /// </summary>
        /// <param name="divisionIds">List of division IDs</param>
        /// <returns>List of workstream leaders</returns>
        Task<List<User>> GetWorkstreamLeadersAsync(List<string> divisionIds);

        /// <summary>
        /// Update idea's related divisions in database
        /// </summary>
        /// <param name="ideaId">ID of the idea</param>
        /// <param name="divisionIds">List of division IDs to associate</param>
        /// <returns>Task representing the operation</returns>
        Task UpdateIdeaRelatedDivisionsAsync(long ideaId, List<string> divisionIds);

        /// <summary>
        /// Send notification emails to workstream leaders of related divisions
        /// </summary>
        /// <param name="idea">The approved idea</param>
        /// <param name="divisionIds">List of division IDs to notify</param>
        /// <returns>Task representing the operation</returns>
        Task NotifyRelatedDivisionsAsync(Models.Entities.Idea idea, List<string> divisionIds);

        /// <summary>
        /// Get divisions that are currently related to an idea
        /// </summary>
        /// <param name="ideaId">ID of the idea</param>
        /// <returns>List of related divisions</returns>
        Task<List<Division>> GetRelatedDivisionsAsync(long ideaId);
    }
}