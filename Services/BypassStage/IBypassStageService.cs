namespace Ideku.Services.BypassStage
{
    public interface IBypassStageService
    {
        Task<(bool Success, string Message, string? NewStatus)> BypassStageAsync(
            long ideaId,
            int targetStage,
            string reason,
            string username);
    }
}
