using Ideku.Models.Entities;

namespace Ideku.Services.IdeaImplementators
{
    public interface IIdeaImplementatorService
    {
        /// <summary>
        /// Assign user as implementator to an idea
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <param name="userId">User ID to assign</param>
        /// <param name="role">Role: "Leader" or "Member"</param>
        /// <returns>Success status and message</returns>
        Task<(bool Success, string Message)> AssignImplementatorAsync(long ideaId, long userId, string role);

        /// <summary>
        /// Remove implementator from idea
        /// </summary>
        /// <param name="implementatorId">IdeaImplementator ID to remove</param>
        /// <returns>Success status and message</returns>
        Task<(bool Success, string Message)> RemoveImplementatorAsync(long implementatorId);

        /// <summary>
        /// Get all implementators for an idea with user details
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>List of implementators with user information</returns>
        Task<IEnumerable<IdeaImplementator>> GetImplementatorsByIdeaIdAsync(long ideaId);

        /// <summary>
        /// Get leader for an idea
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>Leader implementator or null if no leader assigned</returns>
        Task<IdeaImplementator?> GetLeaderByIdeaIdAsync(long ideaId);

        /// <summary>
        /// Get members for an idea
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>List of member implementators</returns>
        Task<IEnumerable<IdeaImplementator>> GetMembersByIdeaIdAsync(long ideaId);

        /// <summary>
        /// Get available users for assignment (users not yet assigned to the idea)
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>List of available users for dropdown</returns>
        Task<IEnumerable<object>> GetAvailableUsersForAssignmentAsync(long ideaId);

        /// <summary>
        /// Validate if user can be assigned with specific role
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="role">Role to assign</param>
        /// <returns>Validation result with message</returns>
        Task<(bool IsValid, string Message)> ValidateAssignmentAsync(long ideaId, long userId, string role);
    }
}