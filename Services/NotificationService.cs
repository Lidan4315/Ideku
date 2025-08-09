using Ideku.Models;
using Ideku.Models.Entities;
using Ideku.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Ideku.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IEmailService emailService, AppDbContext context, ILogger<NotificationService> logger)
        {
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        public async Task NotifyIdeaSubmitted(Idea idea)
        {
            try
            {
                // Get approvers based on target division/department
                var approvers = await GetApproversForIdea(idea);
                
                var emailMessages = new List<EmailMessage>();
                
                foreach (var approver in approvers)
                {
                    var emailMessage = new EmailMessage
                    {
                        To = approver.Employee.EMAIL,
                        Subject = $"New Idea Submission: {idea.IdeaName}",
                        Body = GenerateIdeaSubmittedEmailBody(idea, approver),
                        IsHtml = true
                    };
                    emailMessages.Add(emailMessage);
                }

                if (emailMessages.Any())
                {
                    await _emailService.SendBulkEmailAsync(emailMessages);
                    _logger.LogInformation("Sent idea submission notifications for Idea ID: {IdeaId}", idea.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea submission notifications for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyIdeaApproved(Idea idea, User approver)
        {
            try
            {
                // Notify initiator
                var initiatorEmail = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"Idea Approved: {idea.IdeaName}",
                    Body = GenerateIdeaApprovedEmailBody(idea, approver),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(initiatorEmail);
                _logger.LogInformation("Sent idea approval notification for Idea ID: {IdeaId}", idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea approval notification for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyIdeaRejected(Idea idea, User rejector, string reason)
        {
            try
            {
                // Notify initiator
                var initiatorEmail = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"Idea Rejected: {idea.IdeaName}",
                    Body = GenerateIdeaRejectedEmailBody(idea, rejector, reason),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(initiatorEmail);
                _logger.LogInformation("Sent idea rejection notification for Idea ID: {IdeaId}", idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea rejection notification for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyIdeaCompleted(Idea idea)
        {
            try
            {
                // Notify initiator and stakeholders
                var emailMessage = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"Idea Completed: {idea.IdeaName}",
                    Body = GenerateIdeaCompletedEmailBody(idea),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                _logger.LogInformation("Sent idea completion notification for Idea ID: {IdeaId}", idea.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea completion notification for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyMilestoneCreated(Milestone milestone)
        {
            try
            {
                // Notify idea initiator
                var emailMessage = new EmailMessage
                {
                    To = milestone.Idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"New Milestone Created: {milestone.Idea.IdeaName}",
                    Body = GenerateMilestoneCreatedEmailBody(milestone),
                    IsHtml = true
                };

                await _emailService.SendEmailAsync(emailMessage);
                _logger.LogInformation("Sent milestone creation notification for Milestone ID: {MilestoneId}", milestone.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send milestone creation notification for Milestone ID: {MilestoneId}", milestone.Id);
            }
        }

        private async Task<List<User>> GetApproversForIdea(Idea idea)
        {
            // Simple logic: get users with specific roles in target division/department
            // You can customize this based on your approval workflow
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .Where(u => u.Employee.DIVISION == idea.ToDivisionId && 
                           u.Role.RoleName.Contains("Manager")) // Adjust role criteria
                .ToListAsync();
        }

        private string GenerateIdeaSubmittedEmailBody(Idea idea, User approver)
        {
            return $@"
            <h2>New Idea Submission</h2>
            <p>Dear {approver.Name},</p>
            <p>A new idea has been submitted and requires your review:</p>
            <ul>
                <li><strong>Idea:</strong> {idea.IdeaName}</li>
                <li><strong>Code:</strong> {idea.IdeaCode}</li>
                <li><strong>Initiator:</strong> {idea.InitiatorUser.Name}</li>
                <li><strong>Category:</strong> {idea.Category.CategoryName}</li>
                <li><strong>Estimated Saving:</strong> {idea.SavingCost:C}</li>
            </ul>
            <p><strong>Background:</strong></p>
            <p>{idea.IdeaIssueBackground}</p>
            <p><strong>Proposed Solution:</strong></p>
            <p>{idea.IdeaSolution}</p>
            <p>Please log in to the system to review and approve this idea.</p>
            <p>Best regards,<br>Ideku System</p>";
        }

        private string GenerateIdeaApprovedEmailBody(Idea idea, User approver)
        {
            return $@"
            <h2>Idea Approved</h2>
            <p>Dear {idea.InitiatorUser.Name},</p>
            <p>Great news! Your idea has been approved:</p>
            <ul>
                <li><strong>Idea:</strong> {idea.IdeaName}</li>
                <li><strong>Code:</strong> {idea.IdeaCode}</li>
                <li><strong>Approved by:</strong> {approver.Name}</li>
                <li><strong>Current Status:</strong> {idea.CurrentStatus}</li>
            </ul>
            <p>The implementation process will begin soon.</p>
            <p>Best regards,<br>Ideku System</p>";
        }

        private string GenerateIdeaRejectedEmailBody(Idea idea, User rejector, string reason)
        {
            return $@"
            <h2>Idea Rejected</h2>
            <p>Dear {idea.InitiatorUser.Name},</p>
            <p>We regret to inform you that your idea has been rejected:</p>
            <ul>
                <li><strong>Idea:</strong> {idea.IdeaName}</li>
                <li><strong>Code:</strong> {idea.IdeaCode}</li>
                <li><strong>Rejected by:</strong> {rejector.Name}</li>
                <li><strong>Reason:</strong> {reason}</li>
            </ul>
            <p>You may revise and resubmit your idea addressing the concerns mentioned above.</p>
            <p>Best regards,<br>Ideku System</p>";
        }

        private string GenerateIdeaCompletedEmailBody(Idea idea)
        {
            return $@"
            <h2>Idea Implementation Completed</h2>
            <p>Dear {idea.InitiatorUser.Name},</p>
            <p>Congratulations! Your idea has been successfully implemented:</p>
            <ul>
                <li><strong>Idea:</strong> {idea.IdeaName}</li>
                <li><strong>Code:</strong> {idea.IdeaCode}</li>
                <li><strong>Completed Date:</strong> {idea.CompletedDate:dd/MM/yyyy}</li>
                <li><strong>Final Saving Cost:</strong> {idea.SavingCostVaidated:C}</li>
            </ul>
            <p>Thank you for your valuable contribution to our organization!</p>
            <p>Best regards,<br>Ideku System</p>";
        }

        private string GenerateMilestoneCreatedEmailBody(Milestone milestone)
        {
            return $@"
            <h2>New Milestone Created</h2>
            <p>Dear {milestone.Idea.InitiatorUser.Name},</p>
            <p>A new milestone has been created for your idea:</p>
            <ul>
                <li><strong>Idea:</strong> {milestone.Idea.IdeaName}</li>
                <li><strong>Milestone Status:</strong> {milestone.Status}</li>
                <li><strong>Start Date:</strong> {milestone.StartDate:dd/MM/yyyy}</li>
                <li><strong>End Date:</strong> {milestone.EndDate:dd/MM/yyyy}</li>
                <li><strong>Created by:</strong> {milestone.CreatorUser.Name}</li>
            </ul>
            <p><strong>Note:</strong> {milestone.Note}</p>
            <p>Best regards,<br>Ideku System</p>";
        }
    }
}