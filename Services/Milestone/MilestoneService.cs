using Ideku.Data.Repositories;
using Ideku.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ideku.Services.Milestone
{
    public class MilestoneService : IMilestoneService
    {
        private readonly IMilestoneRepository _milestoneRepository;
        private readonly IUserRepository _userRepository;
        private readonly IIdeaRepository _ideaRepository;

        public MilestoneService(
            IMilestoneRepository milestoneRepository,
            IUserRepository userRepository,
            IIdeaRepository ideaRepository)
        {
            _milestoneRepository = milestoneRepository;
            _userRepository = userRepository;
            _ideaRepository = ideaRepository;
        }

        public async Task<IQueryable<Models.Entities.Idea>> GetMilestoneEligibleIdeasQueryAsync(
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

            return await Task.FromResult(query);
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

                // Set IsMilestoneCreated flag and update status on the idea
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea != null)
                {
                    bool needsUpdate = false;

                    // Set milestone created flag if not already set
                    if (!idea.IsMilestoneCreated)
                    {
                        idea.IsMilestoneCreated = true;
                        needsUpdate = true;
                    }

                    // Change status from "Waiting Milestone Creation" to "Waiting Approval S3"
                    if (idea.CurrentStage == 2 && idea.CurrentStatus == "Waiting Milestone Creation")
                    {
                        idea.CurrentStatus = "Waiting Approval S3";
                        idea.UpdatedDate = DateTime.Now;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        await _ideaRepository.UpdateAsync(idea);
                    }
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

            // Validate dates
            var dateValidation = ValidateMilestoneDates(startDate, endDate);
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

        public async Task<IEnumerable<IdeaImplementator>> GetAvailablePICUsersAsync(long ideaId)
        {
            var idea = await _milestoneRepository.GetIdeaWithImplementatorsAsync(ideaId);

            if (idea == null) return new List<IdeaImplementator>();

            return idea.IdeaImplementators.ToList();
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
    }
}