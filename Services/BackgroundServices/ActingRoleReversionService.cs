using Ideku.Services.UserManagement;

namespace Ideku.Services.BackgroundServices
{
    /// <summary>
    /// Background service yang otomatis merevert expired acting users
    /// Berjalan setiap jam untuk check dan revert users yang acting period sudah habis
    /// </summary>
    public class ActingRoleReversionService : BackgroundService
    {
        private readonly ILogger<ActingRoleReversionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check setiap 1 jam

        public ActingRoleReversionService(
            ILogger<ActingRoleReversionService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Main execution method yang berjalan di background
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Acting Role Reversion Service started.");

            // Wait sedikit saat startup agar aplikasi fully loaded
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredActingUsersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Acting Role Reversion Service execution");
                }

                // Wait untuk interval berikutnya
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Acting Role Reversion Service stopped.");
        }

        /// <summary>
        /// Process semua expired acting users dan revert mereka
        /// </summary>
        private async Task ProcessExpiredActingUsersAsync()
        {
            try
            {
                // Buat scope baru untuk DI karena ini background service (Singleton)
                // sedangkan UserManagementService adalah Scoped
                using var scope = _serviceProvider.CreateScope();
                var userManagementService = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

                // Process expired acting users
                var result = await userManagementService.ProcessExpiredActingUsersAsync();

                if (result.ProcessedCount > 0)
                {
                    _logger.LogInformation(
                        "Processed {ProcessedCount} expired acting users. Messages: {Messages}",
                        result.ProcessedCount,
                        string.Join("; ", result.Messages)
                    );
                }
                else
                {
                    _logger.LogDebug("No expired acting users found to process.");
                }

                // Log error messages jika ada
                var errorMessages = result.Messages.Where(m => m.Contains("Error") || m.Contains("Warning")).ToList();
                if (errorMessages.Any())
                {
                    _logger.LogWarning("Some issues during processing: {ErrorMessages}", string.Join("; ", errorMessages));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired acting users");
            }
        }

        /// <summary>
        /// Called when service is stopping
        /// </summary>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Acting Role Reversion Service is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}