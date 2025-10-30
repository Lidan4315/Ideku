using Ideku.Models.Entities;

namespace Ideku.Services.IdeaMonitoring
{
    public interface IIdeaMonitoringService
    {
        /// <summary>
        /// Create a new monitoring record for an idea
        /// </summary>
        Task<(bool Success, string Message, Models.Entities.IdeaMonitoring? Monitoring)> CreateMonitoringAsync(long ideaId, DateTime monthFrom, int durationMonths, string username);

        /// <summary>
        /// Get monitoring by ID
        /// </summary>
        Task<Models.Entities.IdeaMonitoring?> GetMonitoringByIdAsync(long id);

        /// <summary>
        /// Get all monitoring records for a specific idea
        /// </summary>
        Task<IEnumerable<Models.Entities.IdeaMonitoring>> GetMonitoringsByIdeaIdAsync(long ideaId);

        /// <summary>
        /// Update CostSavePlan and CostSaveActual (for Leader/Member/Workstream Leader)
        /// </summary>
        Task<(bool Success, string Message)> UpdateCostSavingsAsync(long monitoringId, long? costSavePlan, long? costSaveActual, string username);

        /// <summary>
        /// Update CostSaveActualValidated (for SCFO only)
        /// </summary>
        Task<(bool Success, string Message)> UpdateCostSaveValidatedAsync(long monitoringId, long costSaveActualValidated, string username);

        /// <summary>
        /// Delete monitoring record
        /// </summary>
        Task<(bool Success, string Message)> DeleteMonitoringAsync(long id, string username);

        /// <summary>
        /// Check if user can edit CostSavePlan/CostSaveActual
        /// </summary>
        Task<bool> CanEditCostSavingsAsync(long ideaId, string username);

        /// <summary>
        /// Check if user can edit CostSaveActualValidated (SCFO or SuperUser)
        /// </summary>
        Task<bool> CanValidateCostSavingsAsync(string username);

        /// <summary>
        /// Extend monitoring duration by adding additional months
        /// </summary>
        Task<(bool Success, string Message)> ExtendDurationAsync(long ideaId, int additionalMonths, string username);
    }
}
