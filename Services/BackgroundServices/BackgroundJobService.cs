using Microsoft.Extensions.Logging;

namespace Ideku.Services.BackgroundServices
{
    /// Service untuk menjalankan background jobs dengan proper scope management
    /// dan centralized error handling
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BackgroundJobService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// Execute action in background dengan automatic scope management
        public void ExecuteInBackground(string jobName, Func<IServiceProvider, Task> action)
        {
            // Fire-and-forget dengan Task.Run
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background job: {JobName}", jobName);

                    // Create new scope untuk background task
                    // Scope ini independent dari HTTP request scope
                    using var scope = _serviceScopeFactory.CreateScope();
                    var serviceProvider = scope.ServiceProvider;

                    // Execute the action dengan scoped service provider
                    await action(serviceProvider);

                    _logger.LogInformation("Background job completed successfully: {JobName}", jobName);
                }
                catch (Exception ex)
                {
                    // Log error tapi tidak throw - background job tidak boleh crash aplikasi
                    _logger.LogError(ex, "Background job failed: {JobName} - {ErrorMessage}",
                        jobName, ex.Message);

                    // Future enhancement: Add to retry queue
                    // await AddToRetryQueueAsync(jobName, action, ex);
                }
            });
        }
    }
}
