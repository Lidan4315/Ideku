using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Data.Repositories;
using Ideku.Data.Repositories.IdeaImplementators;
using Ideku.Models.Entities;

namespace Ideku.Services.IdeaImplementators
{
    public class IdeaImplementatorService : IIdeaImplementatorService
    {
        private readonly IIdeaImplementatorRepository _implementatorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IIdeaRepository _ideaRepository;
        private readonly AppDbContext _context;
        private readonly ILogger<IdeaImplementatorService> _logger;

        public IdeaImplementatorService(
            IIdeaImplementatorRepository implementatorRepository,
            IUserRepository userRepository,
            IIdeaRepository ideaRepository,
            AppDbContext context,
            ILogger<IdeaImplementatorService> logger)
        {
            _implementatorRepository = implementatorRepository;
            _userRepository = userRepository;
            _ideaRepository = ideaRepository;
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

                // Check if this is the first implementator assigned and update status if needed
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea != null && idea.CurrentStage == 1 && idea.CurrentStatus == "Waiting Team Assignment")
                {
                    // Check if this is indeed the first implementator
                    var implementatorCount = await _context.IdeaImplementators.CountAsync(ii => ii.IdeaId == ideaId);
                    if (implementatorCount == 1) // Just assigned the first one
                    {
                        idea.CurrentStatus = "Waiting Approval S2";
                        idea.UpdatedDate = DateTime.Now;
                        await _ideaRepository.UpdateAsync(idea);
                        _logger.LogInformation("Idea {IdeaId} status changed to 'Waiting Approval S2' after first team assignment", ideaId);
                    }
                }

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

        public async Task<IEnumerable<object>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Getting all users");

                // Get all users without filtering
                var allUsers = await _context.Users
                    .Include(u => u.Employee)
                    .Include(u => u.Role)
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

                _logger.LogInformation("Found {Count} total users", allUsers.Count);

                return allUsers.Cast<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
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

                // Check if assigning as Member and limit is reached
                if (role == "Member")
                {
                    var currentMemberCount = await _implementatorRepository.GetMemberCountAsync(ideaId);

                    // Check if limit is 5 (will be checked later if user is not superuser)
                    // This is just a soft check, actual enforcement is in controller
                    if (currentMemberCount >= 5)
                    {
                        // Don't block here, let controller check if user is superuser
                        _logger.LogInformation("Member count is {Count}, may exceed limit for non-superuser", currentMemberCount);
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

        public Task<(bool IsValid, string Message)> ValidateTeamCompositionAsync(int leaderCount, int memberCount)
        {
            // Minimum requirement: 1 Leader + 1 Member
            if (leaderCount < 1)
            {
                return Task.FromResult((false, "Please assign a Leader to the team."));
            }

            if (memberCount < 1)
            {
                return Task.FromResult((false, "Please assign at least one Member to the team."));
            }

            return Task.FromResult((true, "Team composition is valid."));
        }

        public async Task<(bool Success, string Message)> AssignMultipleImplementatorsAsync(
            string username,
            long ideaId,
            List<(long UserId, string Role)> implementators)
        {
            try
            {
                // Get current user for role-based validation
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Get current implementators
                var currentImplementators = await GetImplementatorsByIdeaIdAsync(ideaId);
                var currentLeaderCount = currentImplementators.Count(i => i.Role == "Leader");
                var currentMemberCount = currentImplementators.Count(i => i.Role == "Member");

                // Count new implementators by role
                var newLeaders = implementators.Where(i => i.Role == "Leader").ToList();
                var newMembers = implementators.Where(i => i.Role == "Member").ToList();

                // Calculate total after assignment
                var totalLeaders = currentLeaderCount + newLeaders.Count;
                var totalMembers = currentMemberCount + newMembers.Count;

                // Validation 1: Check minimum team composition (1 Leader + 1 Member)
                var compositionValidation = await ValidateTeamCompositionAsync(totalLeaders, totalMembers);
                if (!compositionValidation.IsValid)
                {
                    return (false, compositionValidation.Message);
                }

                // Validation 2: Check leader limit (max 1 leader total)
                if (totalLeaders > 1)
                {
                    return (false, "Only one Leader can be assigned per idea.");
                }

                // Validation 3: Check member limit for Workstream Leader (max 5 members)
                if (user.Role.RoleName == "Workstream Leader")
                {
                    if (totalMembers > 5)
                    {
                        return (false, "Maximum limit of 5 members has been reached.");
                    }
                }
                // Superuser has no member limit

                // All validations passed - assign all implementators
                foreach (var impl in implementators)
                {
                    var result = await AssignImplementatorAsync(ideaId, impl.UserId, impl.Role);
                    if (!result.Success)
                    {
                        // If any assignment fails, return error
                        return (false, result.Message);
                    }
                }

                return (true, "Team has been assembled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning multiple implementators: IdeaId={IdeaId}", ideaId);
                return (false, "An error occurred while assigning implementators.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateTeamImplementatorsAsync(
            string username,
            long ideaId,
            List<long> implementatorsToRemove,
            List<(long UserId, string Role)> implementatorsToAdd)
        {
            try
            {
                // Get current user for role-based validation
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Get current implementators after removal
                var currentImplementators = await GetImplementatorsByIdeaIdAsync(ideaId);
                var remainingImplementators = currentImplementators
                    .Where(i => !implementatorsToRemove.Contains(i.Id))
                    .ToList();

                var currentLeaderCount = remainingImplementators.Count(i => i.Role == "Leader");
                var currentMemberCount = remainingImplementators.Count(i => i.Role == "Member");

                // Count new implementators by role
                var newLeaders = implementatorsToAdd.Where(i => i.Role == "Leader").ToList();
                var newMembers = implementatorsToAdd.Where(i => i.Role == "Member").ToList();

                // Calculate total after update
                var totalLeaders = currentLeaderCount + newLeaders.Count;
                var totalMembers = currentMemberCount + newMembers.Count;

                // Validation 1: Check minimum team composition (1 Leader + 1 Member)
                var compositionValidation = await ValidateTeamCompositionAsync(totalLeaders, totalMembers);
                if (!compositionValidation.IsValid)
                {
                    return (false, compositionValidation.Message);
                }

                // Validation 2: Check leader limit (max 1 leader total after update)
                if (totalLeaders > 1)
                {
                    return (false, "Only one Leader can be assigned per idea.");
                }

                // Validation 3: Check member limit for Workstream Leader (max 5 members after update)
                if (user.Role.RoleName == "Workstream Leader")
                {
                    if (totalMembers > 5)
                    {
                        return (false, "Maximum limit of 5 members has been reached.");
                    }
                }
                // Superuser has no member limit

                // All validations passed - remove old implementators
                foreach (var implementatorId in implementatorsToRemove)
                {
                    var removeResult = await RemoveImplementatorAsync(implementatorId);
                    if (!removeResult.Success)
                    {
                        return (false, $"Failed to remove implementator: {removeResult.Message}");
                    }
                }

                // Add new implementators
                foreach (var impl in implementatorsToAdd)
                {
                    var assignResult = await AssignImplementatorAsync(ideaId, impl.UserId, impl.Role);
                    if (!assignResult.Success)
                    {
                        return (false, $"Failed to add implementator: {assignResult.Message}");
                    }
                }

                return (true, "Team assembly has been updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team implementators: IdeaId={IdeaId}", ideaId);
                return (false, "An error occurred while updating team implementators.");
            }
        }
    }
}