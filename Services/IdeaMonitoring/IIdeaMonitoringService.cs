using Ideku.Models.Entities;

namespace Ideku.Services.IdeaMonitoring
{
    public interface IIdeaMonitoringService
    {
        // Create a new monitoring record for an idea
        Task<(bool Success, string Message, Models.Entities.IdeaMonitoring? Monitoring)> CreateMonitoringAsync(long ideaId, DateTime monthFrom, int durationMonths, string username);

        // Get monitoring by ID
        Task<Models.Entities.IdeaMonitoring?> GetMonitoringByIdAsync(long id);

        // Get all monitoring records for a specific idea
        Task<IEnumerable<Models.Entities.IdeaMonitoring>> GetMonitoringsByIdeaIdAsync(long ideaId);

        // Update CostSavePlan and CostSaveActual (for Leader/Member/Workstream Leader)
        Task<(bool Success, string Message)> UpdateCostSavingsAsync(long monitoringId, long? costSavePlan, long? costSaveActual, string username);

        // Update CostSaveActualValidated (for SCFO only)
        Task<(bool Success, string Message)> UpdateCostSaveValidatedAsync(long monitoringId, long costSaveActualValidated, string username);

        // Delete monitoring record
        Task<(bool Success, string Message)> DeleteMonitoringAsync(long id, string username);

        // Check if user can edit CostSavePlan/CostSaveActual
        Task<bool> CanEditCostSavingsAsync(long ideaId, string username);

        // Check if user can edit CostSaveActualValidated (SCFO or SuperUser)
        Task<bool> CanValidateCostSavingsAsync(string username);

        // Extend monitoring duration by adding additional months
        Task<(bool Success, string Message)> ExtendDurationAsync(long ideaId, int additionalMonths, string username);

        /// Upload monitoring supporting documents (attachments) for an idea
        /// Files will be named with "M" prefix: {ideaCode}_M{counter}.ext
        Task<(bool Success, string Message)> UploadMonitoringAttachmentsAsync(long ideaId, List<IFormFile> files, string username);

        /// Add a new KPI monitoring based on existing monthly cost saving monitoring duration
        /// The new KPI will automatically use the same MonthFrom and MonthTo as the existing monitoring
        Task<(bool Success, string Message)> AddKpiMonitoringAsync(long ideaId, string kpiName, string measurementUnit, string username);
    }
}
