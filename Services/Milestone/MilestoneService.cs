using Ideku.Data.Repositories;
using Ideku.Models.Entities;
using Ideku.ViewModels.Common;
using Ideku.ViewModels.Milestone;
using Ideku.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Ideku.Services.Milestone
{
    public class MilestoneService : IMilestoneService
    {
        private readonly IMilestoneRepository _milestoneRepository;
        private readonly IUserRepository _userRepository;

        public MilestoneService(
            IMilestoneRepository milestoneRepository,
            IUserRepository userRepository)
        {
            _milestoneRepository = milestoneRepository;
            _userRepository = userRepository;
        }

        public async Task<PagedResult<Models.Entities.Idea>> GetMilestoneEligibleIdeasAsync(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            var query = _milestoneRepository.GetIdeasWithMilestoneEligibility();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(i =>
                    i.IdeaCode.Contains(searchTerm) ||
                    i.IdeaName.Contains(searchTerm) ||
                    i.InitiatorUser.Name.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                query = query.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                query = query.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                query = query.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (selectedStage.HasValue)
            {
                query = query.Where(i => i.CurrentStage == selectedStage.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(i => i.CurrentStatus == selectedStatus);
            }

            return await query.ToPagedResultAsync(page, pageSize);
        }

        public async Task<Models.Entities.Idea?> GetMilestoneEligibleIdeaByIdAsync(long ideaId)
        {
            return await _milestoneRepository.GetIdeasWithMilestoneEligibility()
                .FirstOrDefaultAsync(i => i.Id == ideaId);
        }

        public async Task<IEnumerable<Models.Entities.Milestone>> GetMilestonesByIdeaIdAsync(long ideaId)
        {
            return await _milestoneRepository.GetMilestonesByIdeaIdAsync(ideaId);
        }

        public async Task<Models.Entities.Milestone?> GetMilestoneByIdAsync(long milestoneId)
        {
            return await _milestoneRepository.GetMilestoneByIdAsync(milestoneId);
        }

        public async Task<(bool Success, string Message, Models.Entities.Milestone? Milestone)> CreateMilestoneAsync(
            long ideaId,
            string title,
            string? note,
            DateTime startDate,
            DateTime endDate,
            string status,
            string creatorName,
            string creatorEmployeeId,
            List<long>? picUserIds = null)
        {
            // Validate idea eligibility
            if (!await _milestoneRepository.IsIdeaMilestoneEligibleAsync(ideaId))
            {
                return (false, "Idea is not eligible for milestone creation. Must be stage 2 or higher.", null);
            }

            // Validate dates
            var dateValidation = ValidateMilestoneDates(startDate, endDate);
            if (!dateValidation.IsValid)
            {
                return (false, dateValidation.Message, null);
            }

            // Create milestone
            var milestone = new Models.Entities.Milestone
            {
                IdeaId = ideaId,
                TitleMilestone = title,
                Note = note,
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                CreatorName = creatorName,
                CreatorEmployeeId = creatorEmployeeId
            };

            try
            {
                var createdMilestone = await _milestoneRepository.CreateMilestoneAsync(milestone);

                // Add PICs if provided
                if (picUserIds != null && picUserIds.Any())
                {
                    var pics = picUserIds.Select(userId => new MilestonePIC
                    {
                        MilestoneId = createdMilestone.Id,
                        UserId = userId
                    });

                    await _milestoneRepository.AddMilestonePICsAsync(pics);
                }

                return (true, "Milestone created successfully.", createdMilestone);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating milestone: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, Models.Entities.Milestone? Milestone)> UpdateMilestoneAsync(
            long milestoneId,
            string title,
            string? note,
            DateTime startDate,
            DateTime endDate,
            string status,
            List<long>? picUserIds = null)
        {
            var milestone = await _milestoneRepository.GetMilestoneByIdAsync(milestoneId);
            if (milestone == null)
            {
                return (false, "Milestone not found.", null);
            }

            // Validate dates (for update, we don't check if start date is in past since it's readonly)
            var dateValidation = ValidateMilestoneDatesForUpdate(startDate, endDate);
            if (!dateValidation.IsValid)
            {
                return (false, dateValidation.Message, null);
            }

            // Update milestone
            milestone.TitleMilestone = title;
            milestone.Note = note;
            milestone.StartDate = startDate;
            milestone.EndDate = endDate;
            milestone.Status = status;

            try
            {
                var updatedMilestone = await _milestoneRepository.UpdateMilestoneAsync(milestone);

                // Update PICs if provided
                if (picUserIds != null)
                {
                    var pics = picUserIds.Select(userId => new MilestonePIC
                    {
                        MilestoneId = milestoneId,
                        UserId = userId
                    });

                    await _milestoneRepository.UpdateMilestonePICsAsync(milestoneId, pics);
                }

                return (true, "Milestone updated successfully.", updatedMilestone);
            }
            catch (Exception ex)
            {
                return (false, $"Error updating milestone: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteMilestoneAsync(long milestoneId)
        {
            try
            {
                var success = await _milestoneRepository.DeleteMilestoneAsync(milestoneId);
                if (success)
                {
                    return (true, "Milestone deleted successfully.");
                }
                else
                {
                    return (false, "Milestone not found.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting milestone: {ex.Message}");
            }
        }

        public async Task<IEnumerable<ImplementatorForPICDto>> GetAvailablePICUsersAsync(long ideaId)
        {
            var idea = await _milestoneRepository.GetIdeasWithMilestoneEligibility()
                .Include(i => i.IdeaImplementators)
                    .ThenInclude(ii => ii.User)
                        .ThenInclude(u => u.Employee)
                .FirstOrDefaultAsync(i => i.Id == ideaId);

            if (idea == null) return new List<ImplementatorForPICDto>();

            return idea.IdeaImplementators.Select(ii => new ImplementatorForPICDto
            {
                Id = ii.User.Id,
                Name = ii.User.Name,
                Role = ii.Role,
                Employee = ii.User.Employee
            }).ToList();
        }

        public (bool IsValid, string Message) ValidateMilestoneDates(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                return (false, "End date cannot be before start date.");
            }

            // Allow past dates for start date (removed validation)

            return (true, string.Empty);
        }

        public (bool IsValid, string Message) ValidateMilestoneDatesForUpdate(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                return (false, "End date cannot be before start date.");
            }

            // No past date validation for updates since start date is readonly
            return (true, string.Empty);
        }

        public async Task<bool> CanUserManageMilestonesAsync(string username, long ideaId)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return false;

            var idea = await _milestoneRepository.GetIdeasWithMilestoneEligibility()
                .FirstOrDefaultAsync(i => i.Id == ideaId);

            if (idea == null) return false;

            // User can manage milestones if they are:
            // 1. The idea initiator
            // 2. Assigned as implementator
            // 3. Has admin/superuser role (you may need to adjust this based on your role system)
            return idea.InitiatorUserId == user.Id ||
                   idea.IdeaImplementators.Any(ii => ii.UserId == user.Id) ||
                   user.Role.RoleName == "Superuser" || // Adjust based on your role names
                   user.Role.RoleName == "Admin";
        }
    }
}