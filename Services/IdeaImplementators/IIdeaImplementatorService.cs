using Ideku.Models.Entities;

namespace Ideku.Services.IdeaImplementators
{
    public interface IIdeaImplementatorService
    {
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
        /// Get available users for assignment (users not yet assigned to the idea)
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <returns>List of available users for dropdown</returns>
        Task<IEnumerable<object>> GetAvailableUsersForAssignmentAsync(long ideaId);

        /// <summary>
        /// Get all users (including those already assigned) for Edit Team modal
        /// </summary>
        /// <returns>List of all users for dropdown</returns>
        Task<IEnumerable<object>> GetAllUsersAsync();

        /// <summary>
        /// Validate if user can be assigned with specific role
        /// </summary>
        /// <param name="ideaId">Idea ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="role">Role to assign</param>
        /// <returns>Validation result with message</returns>
        Task<(bool IsValid, string Message)> ValidateAssignmentAsync(long ideaId, long userId, string role);

        /// <summary>
        /// Validate team composition meets minimum requirements
        /// </summary>
        /// <param name="leaderCount">Number of leaders</param>
        /// <param name="memberCount">Number of members</param>
        /// <returns>Validation result with message</returns>
        Task<(bool IsValid, string Message)> ValidateTeamCompositionAsync(int leaderCount, int memberCount);

        /// <summary>
        /// Assign multiple implementators to an idea in a single transaction
        /// Validates all business rules before assignment (all-or-nothing)
        /// </summary>
        /// <param name="username">Username of current user (for role-based validation)</param>
        /// <param name="ideaId">Idea ID</param>
        /// <param name="implementators">List of implementators with userId and role</param>
        /// <returns>Success status and message</returns>
        Task<(bool Success, string Message)> AssignMultipleImplementatorsAsync(string username, long ideaId, List<(long UserId, string Role)> implementators);

        /// <summary>
        /// Update team implementators atomically (remove old + add new in single transaction)
        /// Validates all business rules before making changes (all-or-nothing)
        /// </summary>
        /// <param name="username">Username of current user (for role-based validation)</param>
        /// <param name="ideaId">Idea ID</param>
        /// <param name="implementatorsToRemove">List of implementator IDs to remove</param>
        /// <param name="implementatorsToAdd">List of new implementators to add</param>
        /// <returns>Success status and message</returns>
        Task<(bool Success, string Message)> UpdateTeamImplementatorsAsync(
            string username,
            long ideaId,
            List<long> implementatorsToRemove,
            List<(long UserId, string Role)> implementatorsToAdd);
    }
}