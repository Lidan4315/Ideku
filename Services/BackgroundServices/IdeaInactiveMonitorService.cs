using Ideku.Data.Context;
using Ideku.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ideku.Services.BackgroundServices
{
    /// <summary>
    /// Background service untuk otomatis meng-inactive idea yang lebih dari 60 hari
    /// Berjalan setiap hari pada jam 2 pagi
    /// </summary>
    public class IdeaInactiveMonitorService : BackgroundService
    {
        private readonly ILogger<IdeaInactiveMonitorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Setiap 24 jam
        private readonly TimeSpan _scheduledTime = new TimeSpan(2, 0, 0); // 02:00 AM

        public IdeaInactiveMonitorService(
            ILogger<IdeaInactiveMonitorService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Idea Inactive Monitor Service started.");

            // Initial delay saat startup (5 menit)
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Tunggu hingga scheduled time (02:00 AM)
                    await WaitUntilScheduledTime(stoppingToken);

                    // Process inactive ideas
                    await ProcessAutoRejectIdeasAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Idea Inactive Monitor Service execution");
                }

                // Wait untuk hari berikutnya
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Idea Inactive Monitor Service stopped.");
        }

        private async Task WaitUntilScheduledTime(CancellationToken stoppingToken)
        {
            var now = DateTime.Now;
            var scheduledDateTime = now.Date.Add(_scheduledTime);

            // Jika sudah lewat scheduled time hari ini, schedule untuk besok
            if (now > scheduledDateTime)
            {
                scheduledDateTime = scheduledDateTime.AddDays(1);
            }

            var delay = scheduledDateTime - now;

            _logger.LogInformation(
                "Next scheduled run at {ScheduledTime}. Waiting {DelayMinutes} minutes.",
                scheduledDateTime,
                delay.TotalMinutes);

            await Task.Delay(delay, stoppingToken);
        }

        private async Task ProcessAutoRejectIdeasAsync()
        {
            var processedCount = 0;
            var messages = new List<string>();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Query ideas yang eligible untuk auto-reject
                var eligibleIdeas = await context.Ideas
                    .Where(i => !i.IsRejected
                        && !i.IsDeleted
                        && i.CurrentStatus.Contains("Waiting Approval"))
                    .ToListAsync();

                _logger.LogInformation("Found {Count} ideas in 'Waiting Approval' status to check", eligibleIdeas.Count);

                foreach (var idea in eligibleIdeas)
                {
                    // Tentukan tanggal referensi untuk hitungan 60 hari
                    DateTime referenceDate;
                    string reasonDetail;

                    if (idea.CurrentStage == 0)
                    {
                        // S0: Hitung dari SubmittedDate
                        referenceDate = idea.SubmittedDate;
                        reasonDetail = "since submission";
                    }
                    else
                    {
                        // S1, S2, S3, dst: Hitung dari UpdatedDate
                        if (!idea.UpdatedDate.HasValue)
                        {
                            // Safety fallback: jika UpdatedDate null, gunakan SubmittedDate
                            referenceDate = idea.SubmittedDate;
                            reasonDetail = "since submission";
                        }
                        else
                        {
                            referenceDate = idea.UpdatedDate.Value;
                            reasonDetail = "since last approval";
                        }
                    }

                    // Hitung hari sejak referenceDate
                    int daysSince = (DateTime.Now - referenceDate).Days;

                    if (daysSince > 60)
                    {
                        // Simpan CurrentStatus sebelumnya untuk reactivation nanti
                        var previousStatus = idea.CurrentStatus;

                        // AUTO-REJECT
                        idea.IsRejected = true;
                        idea.CurrentStatus = "Inactive";
                        idea.RejectedReason = $"Auto-rejected: No approval for 60 days {reasonDetail}";
                        idea.UpdatedDate = DateTime.Now;
                        idea.CompletedDate = DateTime.Now;

                        // Log ke WorkflowHistory
                        var history = new WorkflowHistory
                        {
                            IdeaId = idea.Id,
                            ActorUserId = 1, // System user ID (adjust as needed)
                            FromStage = idea.CurrentStage,
                            ToStage = null,
                            Action = "Auto-Rejected",
                            Comments = $"Automatically rejected after 60 days without approval",
                            Timestamp = DateTime.Now
                        };
                        context.WorkflowHistories.Add(history);

                        processedCount++;
                        messages.Add($"Idea {idea.IdeaCode} auto-rejected ({daysSince} days idle at {previousStatus})");

                        _logger.LogInformation(
                            "Auto-rejected idea {IdeaCode} (ID: {IdeaId}) - {DaysSince} days idle. Previous status: {PreviousStatus}",
                            idea.IdeaCode, idea.Id, daysSince, previousStatus);
                    }
                }

                if (processedCount > 0)
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation(
                        "Successfully auto-rejected {ProcessedCount} ideas. Details: {Details}",
                        processedCount,
                        string.Join("; ", messages));
                }
                else
                {
                    _logger.LogDebug("No ideas found eligible for auto-reject (60+ days idle).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing auto-reject ideas");
                messages.Add($"Error: {ex.Message}");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Idea Inactive Monitor Service is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
