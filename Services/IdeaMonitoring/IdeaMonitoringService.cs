using Ideku.Data.Repositories;
using Ideku.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ideku.Services.IdeaMonitoring
{
    public class IdeaMonitoringService : IIdeaMonitoringService
    {
        private readonly IIdeaMonitoringRepository _monitoringRepository;
        private readonly IIdeaRepository _ideaRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<IdeaMonitoringService> _logger;

        public IdeaMonitoringService(
            IIdeaMonitoringRepository monitoringRepository,
            IIdeaRepository ideaRepository,
            IUserRepository userRepository,
            ILogger<IdeaMonitoringService> logger)
        {
            _monitoringRepository = monitoringRepository;
            _ideaRepository = ideaRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, Models.Entities.IdeaMonitoring? Monitoring)> CreateMonitoringAsync(
            long ideaId,
            DateTime monthFrom,
            int durationMonths,
            string username)
        {
            try
            {
                // Validate idea exists
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    return (false, "Idea not found", null);
                }

                // Check if user has permission
                if (!await CanEditCostSavingsAsync(ideaId, username))
                {
                    return (false, "You don't have permission to create monitoring for this idea", null);
                }

                // Check if monitoring already exists for this idea
                var existingMonitorings = await _monitoringRepository.GetByIdeaIdAsync(ideaId);
                if (existingMonitorings.Any())
                {
                    return (false, "Cost Saving monitoring already exists for this idea", null);
                }

                // Validate duration
                if (durationMonths < 1 || durationMonths > 24)
                {
                    return (false, "Duration must be between 1 and 24 months", null);
                }

                // Generate monthly monitoring records (one record per month)
                var firstMonitoring = (Models.Entities.IdeaMonitoring?)null;

                for (int i = 0; i < durationMonths; i++)
                {
                    var currentMonth = monthFrom.AddMonths(i);
                    var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monitoring = new Models.Entities.IdeaMonitoring
                    {
                        IdeaId = ideaId,
                        MonitoringName = $"Cost Saving - {currentMonth:MMM yyyy}",
                        MonthFrom = monthStart,
                        MonthTo = monthEnd,
                        CostSavePlan = 0,
                        CostSaveActual = null,
                        CostSaveActualValidated = null,
                        CreatedAt = DateTime.Now
                    };

                    var created = await _monitoringRepository.CreateAsync(monitoring);

                    // Keep reference to first record for return value
                    if (i == 0)
                    {
                        firstMonitoring = created;
                    }
                }

                _logger.LogInformation("{Count} monthly monitoring records created for Idea {IdeaId} by user {Username}",
                    durationMonths, ideaId, username);

                return (true, $"{durationMonths} monthly monitoring records created successfully", firstMonitoring);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating monitoring for Idea {IdeaId}", ideaId);
                return (false, $"Error creating monitoring: {ex.Message}", null);
            }
        }

        public async Task<Models.Entities.IdeaMonitoring?> GetMonitoringByIdAsync(long id)
        {
            return await _monitoringRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Models.Entities.IdeaMonitoring>> GetMonitoringsByIdeaIdAsync(long ideaId)
        {
            return await _monitoringRepository.GetByIdeaIdAsync(ideaId);
        }

        public async Task<(bool Success, string Message)> UpdateCostSavingsAsync(
            long monitoringId,
            long costSavePlan,
            long? costSaveActual,
            string username)
        {
            try
            {
                var monitoring = await _monitoringRepository.GetByIdAsync(monitoringId);
                if (monitoring == null)
                {
                    return (false, "Monitoring not found");
                }

                // Check permission
                if (!await CanEditCostSavingsAsync(monitoring.IdeaId, username))
                {
                    return (false, "You don't have permission to edit this monitoring");
                }

                // Validate amounts
                if (costSavePlan < 0)
                {
                    return (false, "Cost Save Plan cannot be negative");
                }

                if (costSaveActual.HasValue && costSaveActual < 0)
                {
                    return (false, "Cost Save Actual cannot be negative");
                }

                // Update values
                monitoring.CostSavePlan = costSavePlan;
                monitoring.CostSaveActual = costSaveActual;

                await _monitoringRepository.UpdateAsync(monitoring);

                _logger.LogInformation("Monitoring {MonitoringId} updated by user {Username}", monitoringId, username);

                return (true, "Cost savings updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating monitoring {MonitoringId}", monitoringId);
                return (false, $"Error updating monitoring: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateCostSaveValidatedAsync(
            long monitoringId,
            long costSaveActualValidated,
            string username)
        {
            try
            {
                var monitoring = await _monitoringRepository.GetByIdAsync(monitoringId);
                if (monitoring == null)
                {
                    return (false, "Monitoring not found");
                }

                // Check permission (SCFO or SuperUser only)
                if (!await CanValidateCostSavingsAsync(username))
                {
                    return (false, "Only SCFO can validate cost savings");
                }

                // Validate amount
                if (costSaveActualValidated < 0)
                {
                    return (false, "Cost Save Actual Validated cannot be negative");
                }

                // Update validated value
                monitoring.CostSaveActualValidated = costSaveActualValidated;

                await _monitoringRepository.UpdateAsync(monitoring);

                _logger.LogInformation("Monitoring {MonitoringId} validated by SCFO {Username}", monitoringId, username);

                return (true, "Cost savings validated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating monitoring {MonitoringId}", monitoringId);
                return (false, $"Error validating monitoring: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteMonitoringAsync(long id, string username)
        {
            try
            {
                var monitoring = await _monitoringRepository.GetByIdAsync(id);
                if (monitoring == null)
                {
                    return (false, "Monitoring not found");
                }

                // Check permission
                if (!await CanEditCostSavingsAsync(monitoring.IdeaId, username))
                {
                    return (false, "You don't have permission to delete this monitoring");
                }

                var deleted = await _monitoringRepository.DeleteAsync(id);
                if (!deleted)
                {
                    return (false, "Failed to delete monitoring");
                }

                _logger.LogInformation("Monitoring {MonitoringId} deleted by user {Username}", id, username);

                return (true, "Monitoring deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting monitoring {MonitoringId}", id);
                return (false, $"Error deleting monitoring: {ex.Message}");
            }
        }

        public async Task<bool> CanEditCostSavingsAsync(long ideaId, string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return false;

            var currentRole = user.CurrentRole?.RoleName ?? user.Role?.RoleName;

            // Superuser can do everything
            if (currentRole == "Superuser")
            {
                return true;
            }

            // Workstream Leader can edit all ideas
            if (currentRole == "Workstream Leader")
            {
                return true;
            }

            // Check if user is Leader or Member of this idea
            var idea = await _ideaRepository.GetByIdAsync(ideaId);
            if (idea == null) return false;

            var implementators = idea.IdeaImplementators;
            var isImplementator = implementators.Any(i =>
                i.UserId == user.Id &&
                (i.Role == "Leader" || i.Role == "Member"));

            return isImplementator;
        }

        public async Task<bool> CanValidateCostSavingsAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return false;

            var currentRole = user.CurrentRole?.RoleName ?? user.Role?.RoleName;

            // Only SCFO or Superuser can validate
            return currentRole == "SCFO" || currentRole == "Superuser";
        }

        public async Task<(bool Success, string Message)> ExtendDurationAsync(long ideaId, int additionalMonths, string username)
        {
            try
            {
                // Validate idea exists
                var idea = await _ideaRepository.GetByIdAsync(ideaId);
                if (idea == null)
                {
                    return (false, "Idea not found");
                }

                // Check permission
                if (!await CanEditCostSavingsAsync(ideaId, username))
                {
                    return (false, "You don't have permission to extend monitoring for this idea");
                }

                // Get existing monitoring records
                var existingMonitorings = await _monitoringRepository.GetByIdeaIdAsync(ideaId);
                if (!existingMonitorings.Any())
                {
                    return (false, "No existing monitoring records found for this idea");
                }

                // Validate additional months
                if (additionalMonths < 1 || additionalMonths > 12)
                {
                    return (false, "Additional months must be between 1 and 12");
                }

                // Find the last month in current monitoring
                var lastMonitoring = existingMonitorings.OrderByDescending(m => m.MonthTo).First();
                var startMonth = lastMonitoring.MonthTo.AddDays(1); // Start from next day after last monitoring

                // Generate additional monthly monitoring records
                for (int i = 0; i < additionalMonths; i++)
                {
                    var currentMonth = startMonth.AddMonths(i);
                    var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monitoring = new Models.Entities.IdeaMonitoring
                    {
                        IdeaId = ideaId,
                        MonitoringName = $"Cost Saving - {currentMonth:MMM yyyy}",
                        MonthFrom = monthStart,
                        MonthTo = monthEnd,
                        CostSavePlan = 0,
                        CostSaveActual = null,
                        CostSaveActualValidated = null,
                        CreatedAt = DateTime.Now
                    };

                    await _monitoringRepository.CreateAsync(monitoring);
                }

                _logger.LogInformation("{Count} additional monthly monitoring records created for Idea {IdeaId} by user {Username}",
                    additionalMonths, ideaId, username);

                return (true, $"{additionalMonths} additional monthly monitoring records created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending monitoring duration for Idea {IdeaId}", ideaId);
                return (false, $"Error extending monitoring duration: {ex.Message}");
            }
        }
    }
}
