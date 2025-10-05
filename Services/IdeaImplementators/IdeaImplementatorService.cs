using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Data.Repositories.IdeaImplementators;
using Ideku.Models.Entities;

namespace Ideku.Services.IdeaImplementators
{
    public class IdeaImplementatorService : IIdeaImplementatorService
    {
        private readonly IIdeaImplementatorRepository _implementatorRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<IdeaImplementatorService> _logger;

        public IdeaImplementatorService(
            IIdeaImplementatorRepository implementatorRepository,
            AppDbContext context,
            ILogger<IdeaImplementatorService> logger)
        {
            _implementatorRepository = implementatorRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> AssignImplementatorAsync(long ideaId, long userId, string role)
        {
            try
            {
                // Validate assignment
                var validation = await ValidateAssignmentAsync(ideaId, userId, role);
                if (!validation.IsValid)
                {
                    return (false, validation.Message);
                }

                // Create assignment
                var assignment = new IdeaImplementator
                {
                    IdeaId = ideaId,
                    UserId = userId,
                    Role = role,
                    CreatedAt = DateTime.Now
                };

                await _implementatorRepository.CreateAsync(assignment);

                _logger.LogInformation("User {UserId} assigned as {Role} to Idea {IdeaId}", userId, role, ideaId);
                return (true, $"User successfully assigned as {role}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning implementator: UserId={UserId}, IdeaId={IdeaId}, Role={Role}",
                    userId, ideaId, role);
                return (false, "An error occurred while assigning implementator");
            }
        }

        public async Task<(bool Success, string Message)> RemoveImplementatorAsync(long implementatorId)
        {
            try
            {
                var implementator = await _implementatorRepository.GetByIdAsync(implementatorId);
                if (implementator == null)
                {
                    return (false, "Implementator not found");
                }

                var success = await _implementatorRepository.RemoveAsync(implementatorId);
                if (!success)
                {
                    return (false, "Failed to remove implementator");
                }

                _logger.LogInformation("Implementator {ImplementatorId} removed from Idea {IdeaId}",
                    implementatorId, implementator.IdeaId);
                return (true, "Implementator removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing implementator: ImplementatorId={ImplementatorId}", implementatorId);
                return (false, "An error occurred while removing implementator");
            }
        }

        public async Task<IEnumerable<IdeaImplementator>> GetImplementatorsByIdeaIdAsync(long ideaId)
        {
            try
            {
                return await _implementatorRepository.GetByIdeaIdWithUserAsync(ideaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting implementators for Idea {IdeaId}", ideaId);
                return Enumerable.Empty<IdeaImplementator>();
            }
        }

        public async Task<IdeaImplementator?> GetLeaderByIdeaIdAsync(long ideaId)
        {
            try
            {
                return await _implementatorRepository.GetLeaderByIdeaIdAsync(ideaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leader for Idea {IdeaId}", ideaId);
                return null;
            }
        }

        public async Task<IEnumerable<IdeaImplementator>> GetMembersByIdeaIdAsync(long ideaId)
        {
            try
            {
                return await _implementatorRepository.GetMembersByIdeaIdAsync(ideaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members for Idea {IdeaId}", ideaId);
                return Enumerable.Empty<IdeaImplementator>();
            }
        }

        public async Task<IEnumerable<object>> GetAvailableUsersForAssignmentAsync(long ideaId)
        {
            try
            {
                _logger.LogInformation("Getting available users for Idea {IdeaId}", ideaId);

                // Get users who are not yet assigned to this idea
                var assignedUserIds = await _context.IdeaImplementators
                    .Where(ii => ii.IdeaId == ideaId)
                    .Select(ii => ii.UserId)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} assigned users for Idea {IdeaId}", assignedUserIds.Count, ideaId);

                // Simplified query first - just get basic user info
                var availableUsers = await _context.Users
                    .Include(u => u.Employee)
                    .Include(u => u.Role)
                    .Where(u => !assignedUserIds.Contains(u.Id))
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        employeeId = u.EmployeeId,
                        division = u.Employee != null ? u.Employee.DIVISION : "Unknown",
                        department = u.Employee != null ? u.Employee.DEPARTEMENT : "Unknown",
                        role = u.Role != null ? u.Role.RoleName : "Unknown",
                        displayText = $"{u.Name} ({u.EmployeeId})"
                    })
                    .OrderBy(u => u.name)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} available users for Idea {IdeaId}", availableUsers.Count, ideaId);

                return availableUsers.Cast<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available users for Idea {IdeaId}", ideaId);
                return Enumerable.Empty<object>();
            }
        }

        public async Task<(bool IsValid, string Message)> ValidateAssignmentAsync(long ideaId, long userId, string role)
        {
            try
            {
                // Check if role is valid
                if (role != "Leader" && role != "Member")
                {
                    return (false, "Invalid role. Must be 'Leader' or 'Member'");
                }

                // Check if idea exists
                var ideaExists = await _context.Ideas.AnyAsync(i => i.Id == ideaId);
                if (!ideaExists)
                {
                    return (false, "Idea not found");
                }

                // Check if user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return (false, "User not found");
                }

                // Check if user is already assigned to this idea
                var isAlreadyAssigned = await _implementatorRepository.IsUserAssignedToIdeaAsync(ideaId, userId);
                if (isAlreadyAssigned)
                {
                    return (false, "User is already assigned to this idea");
                }

                // Check if assigning as Leader but leader already exists
                if (role == "Leader")
                {
                    var hasLeader = await _implementatorRepository.HasLeaderAsync(ideaId);
                    if (hasLeader)
                    {
                        return (false, "This idea already has a Leader assigned");
                    }
                }

                return (true, "Assignment is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating assignment: UserId={UserId}, IdeaId={IdeaId}, Role={Role}",
                    userId, ideaId, role);
                return (false, "An error occurred during validation");
            }
        }
    }
}