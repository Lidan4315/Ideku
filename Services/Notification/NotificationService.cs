using Ideku.Models;
using Ideku.Models.Entities;
using Ideku.Data.Context;
using Ideku.Services.Email;
using Ideku.Services.WorkflowManagement;
using Ideku.Services.ApprovalToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ideku.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly IWorkflowManagementService _workflowManagementService;
        private readonly IApprovalTokenService _approvalTokenService;
        private readonly ILogger<NotificationService> _logger;
        private readonly EmailSettings _emailSettings;

        public NotificationService(
            IEmailService emailService,
            AppDbContext context,
            IWorkflowManagementService workflowManagementService,
            IApprovalTokenService approvalTokenService,
            ILogger<NotificationService> logger,
            IOptions<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _context = context;
            _workflowManagementService = workflowManagementService;
            _approvalTokenService = approvalTokenService;
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        public async Task NotifyIdeaSubmitted(Models.Entities.Idea idea, List<User> approvers)
        {
            try
            {
                _logger.LogInformation("Sending idea submitted notification for idea {IdeaId} to {ApproverCount} approvers",
                    idea.Id, approvers.Count);

                if (!approvers.Any())
                {
                    _logger.LogWarning("No approvers provided for idea {IdeaId} at stage {CurrentStage}",
                        idea.Id, idea.CurrentStage);
                    return;
                }

                // Load workflow history for Historical Approval table
                var workflowHistory = await _context.WorkflowHistories
                    .Include(wh => wh.ActorUser)
                        .ThenInclude(u => u.Employee)
                    .Where(wh => wh.IdeaId == idea.Id && wh.Action == "Approved")
                    .OrderBy(wh => wh.Timestamp)
                    .ToListAsync();

                var emailMessages = new List<EmailMessage>();

                foreach (var approver in approvers)
                {
                    var approveToken = _approvalTokenService.GenerateToken(idea.Id, approver.Id, "Approve", idea.CurrentStage);
                    var rejectToken = _approvalTokenService.GenerateToken(idea.Id, approver.Id, "Reject", idea.CurrentStage);

                    var emailMessage = new EmailMessage
                    {
                        To = approver.Employee.EMAIL,
                        Subject = $"Ideku Approval {idea.IdeaName} | {idea.IdeaCode}",
                        Body = GenerateIdeaSubmittedEmailBody(idea, approver, approveToken, rejectToken, workflowHistory),
                        IsHtml = true
                    };
                    emailMessages.Add(emailMessage);
                }

                if (emailMessages.Any())
                {
                    await _emailService.SendBulkEmailAsync(emailMessages);
                    _logger.LogInformation("Sent idea submission notifications for Idea ID: {IdeaId} to {ApproverCount} approvers",
                        idea.Id, approvers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send idea submission notifications for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyIdeaApproved(Models.Entities.Idea idea, User approver)
        {
            try
            {
                // Load workflow history for Historical Approval table
                var workflowHistory = await _context.WorkflowHistories
                    .Include(wh => wh.ActorUser)
                        .ThenInclude(u => u.Employee)
                    .Where(wh => wh.IdeaId == idea.Id && wh.Action == "Approved")
                    .OrderBy(wh => wh.Timestamp)
                    .ToListAsync();

                // Notify initiator
                var initiatorEmail = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"Ideku Approved {idea.IdeaName} | {idea.IdeaCode}",
                    Body = GenerateIdeaApprovedEmailBody(idea, approver, workflowHistory),
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

        public async Task NotifyIdeaRejected(Models.Entities.Idea idea, User rejector, string reason)
        {
            try
            {
                // Load ALL workflow history (Approved AND Rejected)
                var workflowHistory = await _context.WorkflowHistories
                    .Include(wh => wh.ActorUser)
                        .ThenInclude(u => u.Employee)
                    .Where(wh => wh.IdeaId == idea.Id && (wh.Action == "Approved" || wh.Action == "Rejected"))
                    .OrderBy(wh => wh.Timestamp)
                    .ToListAsync();

                // Notify initiator
                var initiatorEmail = new EmailMessage
                {
                    To = idea.InitiatorUser.Employee.EMAIL,
                    Subject = $"Ideku Rejected {idea.IdeaName} | {idea.IdeaCode}",
                    Body = GenerateIdeaRejectedEmailBody(idea, rejector, reason, workflowHistory),
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

        public async Task NotifyMilestoneCreated(Models.Entities.Milestone milestone)
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


        private string GenerateIdeaSubmittedEmailBody(Models.Entities.Idea idea, User approver, string approveToken, string rejectToken, List<WorkflowHistory> workflowHistory)
        {
            var approvalUrl = $"{_emailSettings.BaseUrl}/Approval/Review/{idea.Id}";
            var approveViaEmailUrl = $"{_emailSettings.BaseUrl}/Approval/ApproveViaEmail?token={approveToken}";
            var rejectViaEmailUrl = $"{_emailSettings.BaseUrl}/Approval/RejectViaEmail?token={rejectToken}";

            // Build Historical Approval table rows
            var historyRows = new System.Text.StringBuilder();
            var rowIndex = 0;
            foreach (var history in workflowHistory)
            {
                var stage = $"S{history.ToStage}";
                var approvalDate = history.Timestamp.ToString("dd. MMM yyyy HH:mm:ss");
                var approverName = history.ActorUser?.Employee?.NAME ?? "Unknown";

                // Alternating background: row 0=white, row 1=gray, row 2=white, etc
                var bgColor = (rowIndex % 2 == 0) ? "white" : "#f0f0f0";

                historyRows.Append($@"
                <tr style='background-color: {bgColor};'>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{stage}</td>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{approvalDate}</td>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{approverName}</td>
                </tr>");

                rowIndex++;
            }

            // Get submitter info
            var submitterName = idea.InitiatorUser?.Employee?.NAME ?? idea.InitiatorUser?.Name ?? "Unknown";
            var submitterPosition = idea.InitiatorUser?.Employee?.POSITION_TITLE ?? "Unknown";
            var stageNumber = idea.CurrentStage + 1;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .email-wrapper {{ max-width: 750px; margin: 20px auto; background-color: white; border: 3px solid #333; }}
        .header {{ background-color: #c62828; padding: 20px; text-align: left; }}
        .header-title {{ font-size: 16px; font-weight: bold; color: #000; }}
        .content {{ padding: 25px; line-height: 1.5; color: #000; background-color: #e7f3ff; font-size: 14px; }}
        .section-title {{ font-weight: bold; margin-top: 15px; margin-bottom: 5px; color: #000; font-size: 14px; }}
        .blue-text {{ color: #0066cc; }}
        .blue-bold {{ color: #0066cc; font-weight: bold; }}
        .history-table {{ width: 100%; border-collapse: collapse; margin: 15px 0; border: 3px solid white; }}
        .history-table th {{ background-color: #4a90e2; color: white; border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px; font-weight: bold; }}
        .history-table td {{ border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px; color: #000; }}
        .history-table tbody tr:nth-child(odd) {{ background-color: white; }}
        .history-table tbody tr:nth-child(even) {{ background-color: #f0f0f0; }}
        .button-container {{ text-align: left; margin: 20px 0 10px 0; page-break-inside: avoid; }}
        .btn {{ display: inline-block; padding: 6px 30px; text-decoration: none; border-radius: 6px; margin: 0 10px 0 0; font-size: 15px; color: white !important; font-weight: 500; min-width: 100px; text-align: center; }}
        .btn-review {{ background-color: #007bff; }}
        .btn-approve {{ background-color: #17a2b8; }}
        .btn-reject {{ background-color: #e74c3c; }}
        .footer-text {{ margin-top: 15px; padding-top: 15px; font-size: 14px; color: #000; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'>
            <div class='header-title'>#{idea.IdeaCode} ""{idea.IdeaName}""</div>
        </div>

        <div class='content'>
            <p style='margin: 0 0 5px 0;'>Dear <span class='blue-text'>{approver.Employee.NAME}</span>,</p>
            <p style='margin: 0 0 15px 0;' class='blue-text'>A S{stageNumber} Approval Request for the <strong>#{idea.IdeaCode}</strong> <strong>""{idea.IdeaName}""</strong> requires your review and approval.</p>

            <div class='section-title'>Summary:</div>
            <div class='blue-text' style='margin-bottom: 15px;'>{idea.IdeaIssueBackground}</div>

            <div class='section-title'>Details:</div>
            <div style='margin: 5px 0 15px 20px;'>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Requested By:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>{submitterName} ({submitterPosition})</span>
                </div>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Expected Solution:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>{idea.IdeaSolution}</span>
                </div>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Estimated Saving Cost:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>${idea.SavingCost:N2}</span>
                </div>
            </div>

            <div class='section-title'>Historical Approval</div>
            <table class='history-table'>
                <thead>
                    <tr>
                        <th>Stage</th>
                        <th>Approval Date</th>
                        <th>Approver</th>
                    </tr>
                </thead>
                <tbody>
                    {historyRows}
                </tbody>
            </table>

            <p style='margin: 20px 0 15px 0;'>Please review the detailed request below and take the necessary action using the provided links.</p>

            <div class='button-container'>
                <a href='{approvalUrl}' class='btn btn-review'>Review</a>
                <a href='{approveViaEmailUrl}' class='btn btn-approve'>Approve</a>
                <a href='{rejectViaEmailUrl}' class='btn btn-reject'>Reject</a>
            </div>

            <div class='footer-text'>
                <p style='margin: 0 0 10px 0;'>This notification was generated automatically by the <span class='blue-text'>IdeKU Notification System</span>. For further information, please contact <span class='blue-text'>BI Department</span> on <span class='blue-text'>ext 1156</span>.</p>
                <p style='margin: 0;'>Best regards,<br><span class='blue-text'>IdeKU</span> Notification</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateIdeaApprovedEmailBody(Models.Entities.Idea idea, User approver, List<WorkflowHistory> workflowHistory)
        {
            var ideaUrl = $"{_emailSettings.BaseUrl}/Idea/Details/{idea.Id}";

            // Build Historical Approval table rows
            var historyRows = new System.Text.StringBuilder();
            var rowIndex = 0;
            foreach (var history in workflowHistory)
            {
                var stage = $"S{history.ToStage}";
                var approvalDate = history.Timestamp.ToString("dd. MMM yyyy HH:mm:ss");
                var historyApproverName = history.ActorUser?.Employee?.NAME ?? "Unknown";

                // Alternating background: row 0=white, row 1=gray, row 2=white, etc
                var bgColor = (rowIndex % 2 == 0) ? "white" : "#f0f0f0";

                historyRows.Append($@"
                <tr style='background-color: {bgColor};'>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{stage}</td>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{approvalDate}</td>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{historyApproverName}</td>
                </tr>");

                rowIndex++;
            }

            // Get initiator info
            var initiatorName = idea.InitiatorUser?.Employee?.NAME ?? idea.InitiatorUser?.Name ?? "Unknown";
            var initiatorPosition = idea.InitiatorUser?.Employee?.POSITION_TITLE ?? "Unknown";
            var stageNumber = idea.CurrentStage;
            var currentApproverName = approver?.Employee?.NAME ?? approver?.Name ?? "Unknown";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .email-wrapper {{ max-width: 750px; margin: 20px auto; background-color: white; border: 3px solid #333; }}
        .header {{ background-color: #c62828; padding: 20px; text-align: left; }}
        .header-title {{ font-size: 16px; font-weight: bold; color: #000; }}
        .content {{ padding: 25px; line-height: 1.5; color: #000; background-color: #e7f3ff; font-size: 14px; }}
        .section-title {{ font-weight: bold; margin-top: 15px; margin-bottom: 5px; color: #000; font-size: 14px; }}
        .blue-text {{ color: #0066cc; }}
        .blue-bold {{ color: #0066cc; font-weight: bold; }}
        .history-table {{ width: 100%; border-collapse: collapse; margin: 15px 0; border: 3px solid white; }}
        .history-table th {{ background-color: #4a90e2; color: white; border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px; font-weight: bold; }}
        .history-table td {{ border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px; color: #000; }}
        .button-container {{ text-align: left; margin: 20px 0 10px 0; page-break-inside: avoid; }}
        .btn {{ display: inline-block; padding: 6px 30px; text-decoration: none; border-radius: 6px; margin: 0 10px 0 0; font-size: 15px; color: white !important; font-weight: 500; min-width: 100px; text-align: center; }}
        .btn-view {{ background-color: #007bff; }}
        .footer-text {{ margin-top: 15px; padding-top: 15px; font-size: 14px; color: #000; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'>
            <div class='header-title'>#{idea.IdeaCode} ""{idea.IdeaName}""</div>
        </div>

        <div class='content'>
            <p style='margin: 0 0 5px 0;'>Dear <span class='blue-text'>{initiatorName}</span>,</p>
            <p style='margin: 0 0 15px 0;' class='blue-text'>Congratulations! Your idea <strong>#{idea.IdeaCode}</strong> <strong>""{idea.IdeaName}""</strong> has been approved at Stage <strong>S{stageNumber}</strong> by <strong>{currentApproverName}</strong>.</p>

            <div class='section-title'>Summary:</div>
            <div class='blue-text' style='margin-bottom: 15px;'>{idea.IdeaIssueBackground}</div>

            <div class='section-title'>Details:</div>
            <div style='margin: 5px 0 15px 20px;'>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Requested By:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>{initiatorName} ({initiatorPosition})</span>
                </div>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Expected Solution:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>{idea.IdeaSolution}</span>
                </div>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Estimated Saving Cost:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>${idea.SavingCost:N2}</span>
                </div>
            </div>

            <div class='section-title'>Historical Approval</div>
            <table class='history-table'>
                <thead>
                    <tr>
                        <th>Stage</th>
                        <th>Approval Date</th>
                        <th>Approver</th>
                    </tr>
                </thead>
                <tbody>
                    {historyRows}
                </tbody>
            </table>

            <p style='margin: 20px 0 15px 0;'>Your idea is now progressing to the next stage of the approval process. You will receive updates as it moves through the system.</p>

            <div class='button-container'>
                <a href='{ideaUrl}' class='btn btn-view'>View Idea Status</a>
            </div>

            <div class='footer-text'>
                <p style='margin: 0 0 10px 0;'>This notification was generated automatically by the <span class='blue-text'>IdeKU Notification System</span>. For further information, please contact <span class='blue-text'>BI Department</span> on <span class='blue-text'>ext 1156</span>.</p>
                <p style='margin: 0;'>Best regards,<br><span class='blue-text'>IdeKU</span> Notification</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateIdeaRejectedEmailBody(Models.Entities.Idea idea, User rejector, string reason, List<WorkflowHistory> workflowHistory)
        {
            var ideaUrl = $"{_emailSettings.BaseUrl}/Idea/Details/{idea.Id}";

            // Build Historical Approval table rows with Action column
            var historyRows = new System.Text.StringBuilder();
            var rowIndex = 0;
            foreach (var history in workflowHistory)
            {
                // For Approved: use ToStage (successfully moved to that stage)
                // For Rejected: use FromStage (stayed at current stage, failed to move up)
                var historyStageNumber = history.Action == "Approved" ? history.ToStage : history.FromStage;
                var stage = $"S{historyStageNumber}";
                var approvalDate = history.Timestamp.ToString("dd. MMM yyyy HH:mm:ss");
                var historyApproverName = history.ActorUser?.Employee?.NAME ?? "Unknown";
                var action = history.Action; // "Approved" or "Rejected"

                // Alternating background: row 0=white, row 1=gray, row 2=white, etc
                var bgColor = (rowIndex % 2 == 0) ? "white" : "#f0f0f0";

                historyRows.Append($@"
                <tr style='background-color: {bgColor};'>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{stage}</td>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{approvalDate}</td>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{historyApproverName}</td>
                    <td style='border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px;'>{action}</td>
                </tr>");

                rowIndex++;
            }

            // Get initiator info
            var initiatorName = idea.InitiatorUser?.Employee?.NAME ?? idea.InitiatorUser?.Name ?? "Unknown";
            var initiatorPosition = idea.InitiatorUser?.Employee?.POSITION_TITLE ?? "Unknown";
            var stageNumber = idea.CurrentStage;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; }}
        .email-wrapper {{ max-width: 750px; margin: 20px auto; background-color: white; border: 3px solid #333; }}
        .header {{ background-color: #c62828; padding: 20px; text-align: left; }}
        .header-title {{ font-size: 16px; font-weight: bold; color: #000; }}
        .content {{ padding: 25px; line-height: 1.5; color: #000; background-color: #e7f3ff; font-size: 14px; }}
        .section-title {{ font-weight: bold; margin-top: 15px; margin-bottom: 5px; color: #000; font-size: 14px; }}
        .blue-text {{ color: #0066cc; }}
        .blue-bold {{ color: #0066cc; font-weight: bold; }}
        .history-table {{ width: 100%; border-collapse: collapse; margin: 15px 0; border: 3px solid white; }}
        .history-table th {{ background-color: #4a90e2; color: white; border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px; font-weight: bold; }}
        .history-table td {{ border: 3px solid white; padding: 5px 10px; text-align: center; font-size: 13px; color: #000; }}
        .button-container {{ text-align: left; margin: 20px 0 10px 0; page-break-inside: avoid; }}
        .btn {{ display: inline-block; padding: 6px 30px; text-decoration: none; border-radius: 6px; margin: 0 10px 0 0; font-size: 15px; color: white !important; font-weight: 500; min-width: 100px; text-align: center; }}
        .btn-view {{ background-color: #007bff; }}
        .footer-text {{ margin-top: 15px; padding-top: 15px; font-size: 14px; color: #000; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'>
            <div class='header-title'>#{idea.IdeaCode} ""{idea.IdeaName}""</div>
        </div>

        <div class='content'>
            <p style='margin: 0 0 5px 0;'>Dear <span class='blue-text'>{initiatorName}</span>,</p>
            <p style='margin: 0 0 15px 0;' class='blue-text'>We regret to inform you that your idea <strong>#{idea.IdeaCode}</strong> <strong>""{idea.IdeaName}""</strong> has been rejected at Stage <strong>S{stageNumber}</strong>.</p>

            <div class='section-title'>Summary:</div>
            <div class='blue-text' style='margin-bottom: 15px;'>{idea.IdeaIssueBackground}</div>

            <div class='section-title'>Details:</div>
            <div style='margin: 5px 0 15px 20px;'>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Requested By:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>{initiatorName} ({initiatorPosition})</span>
                </div>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Expected Solution:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>{idea.IdeaSolution}</span>
                </div>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Estimated Saving Cost:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>${idea.SavingCost:N2}</span>
                </div>
                <div style='margin: 5px 0; display: flex; align-items: flex-start;'>
                    <span class='blue-bold' style='flex-shrink: 0; white-space: nowrap;'>• Rejection Reason:</span>
                    <span class='blue-text' style='margin-left: 5px; flex: 1;'>{reason}</span>
                </div>
            </div>

            <div class='section-title'>Historical Approval</div>
            <table class='history-table'>
                <thead>
                    <tr>
                        <th>Stage</th>
                        <th>Approval Date</th>
                        <th>Approver</th>
                        <th>Action</th>
                    </tr>
                </thead>
                <tbody>
                    {historyRows}
                </tbody>
            </table>

            <p style='margin: 20px 0 15px 0;'>You may revise and resubmit your idea addressing the concerns mentioned above. Please feel free to contact the reviewer for clarification if needed.</p>

            <div class='button-container'>
                <a href='{ideaUrl}' class='btn btn-view'>View Idea Status</a>
            </div>

            <div class='footer-text'>
                <p style='margin: 0 0 10px 0;'>This notification was generated automatically by the <span class='blue-text'>IdeKU Notification System</span>. For further information, please contact <span class='blue-text'>BI Department</span> on <span class='blue-text'>ext 1156</span>.</p>
                <p style='margin: 0;'>Best regards,<br><span class='blue-text'>IdeKU</span> Notification</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateMilestoneCreatedEmailBody(Models.Entities.Milestone milestone)
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
                <li><strong>Created by:</strong> {milestone.CreatorName}</li>
            </ul>
            <p><strong>Note:</strong> {milestone.Note}</p>
            <p>Best regards,<br>Ideku System</p>";
        }

        public async Task NotifyWorkstreamLeadersAsync(Models.Entities.Idea idea, List<User> workstreamLeaders)
        {
            try
            {
                if (!workstreamLeaders.Any())
                {
                    _logger.LogInformation("No workstream leaders to notify for idea {IdeaId}", idea.Id);
                    return;
                }

                var emailMessages = new List<EmailMessage>();

                foreach (var workstreamLeader in workstreamLeaders)
                {
                    var emailMessage = new EmailMessage
                    {
                        To = workstreamLeader.Employee.EMAIL,
                        Subject = $"[Ideku] Idea Approved - {idea.IdeaName} (Related to {workstreamLeader.Employee.DivisionNavigation?.NameDivision})",
                        Body = GenerateWorkstreamLeaderNotificationEmailBody(idea, workstreamLeader),
                        IsHtml = true
                    };
                    emailMessages.Add(emailMessage);
                }

                if (emailMessages.Any())
                {
                    await _emailService.SendBulkEmailAsync(emailMessages);
                    _logger.LogInformation("Sent workstream leader notifications for Idea ID: {IdeaId} to {Count} leaders", 
                        idea.Id, emailMessages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send workstream leader notifications for Idea ID: {IdeaId}", idea.Id);
            }
        }

        public async Task NotifyMilestoneCreationRequiredAsync(Models.Entities.Idea idea, List<User> implementators)
        {
            try
            {
                if (!implementators.Any())
                {
                    _logger.LogWarning("No implementators to notify for idea {IdeaId}", idea.Id);
                    return;
                }

                var emailMessages = new List<EmailMessage>();

                foreach (var implementator in implementators)
                {
                    var emailMessage = new EmailMessage
                    {
                        To = implementator.Employee.EMAIL,
                        Subject = $"[Action Required] Create Milestone for Idea {idea.IdeaCode} - {idea.IdeaName}",
                        Body = GenerateMilestoneCreationRequiredEmailBody(idea, implementator),
                        IsHtml = true
                    };
                    emailMessages.Add(emailMessage);
                }

                if (emailMessages.Any())
                {
                    await _emailService.SendBulkEmailAsync(emailMessages);
                    _logger.LogInformation("Sent milestone creation required notifications for Idea ID: {IdeaId} to {Count} implementators",
                        idea.Id, emailMessages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send milestone creation required notifications for Idea ID: {IdeaId}", idea.Id);
            }
        }

        private string GenerateMilestoneCreationRequiredEmailBody(Models.Entities.Idea idea, User workstreamLeader)
        {
            var milestoneUrl = $"{_emailSettings.BaseUrl}/Milestone/Detail/{idea.Id}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ width: 90%; max-width: 1200px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #f59e0b, #d97706); color: white; padding: 25px; border-radius: 8px 8px 0 0; margin: -40px -40px 30px -40px; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .idea-details {{ background-color: #f8f9fa; padding: 25px; border-radius: 6px; margin: 20px 0; }}
        .warning-box {{ background-color: #fff3cd; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #f59e0b; }}
        .action-button {{ display: inline-block; background-color: #f59e0b; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-size: 16px; font-weight: bold; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666; }}
        .checklist {{ background-color: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0; }}
        .checklist li {{ margin-bottom: 10px; }}
        @media screen and (max-width: 768px) {{
            .container {{ max-width: 95%; padding: 20px; }}
            .header {{ margin: -20px -20px 30px -20px; padding: 20px; }}
            .header h1 {{ font-size: 24px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Action Required: Create Milestone</h1>
        </div>

        <div class='content'>
            <p>Hello {workstreamLeader.Name},</p>

            <p>An idea targeted to your department has been approved to <strong>Stage 2</strong> and requires milestone planning.</p>

            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Idea Code:</strong> {idea.IdeaCode}</p>
                <p><strong>Initiator:</strong> {idea.InitiatorUser?.Name}</p>
                <p><strong>Current Stage:</strong> Stage {idea.CurrentStage}</p>
                <p><strong>Target Division:</strong> {idea.TargetDivision?.NameDivision}</p>
                <p><strong>Target Department:</strong> {idea.TargetDepartment?.NameDepartment}</p>
                <p><strong>Saving Cost:</strong> {idea.SavingCost:C}</p>
            </div>

            <div class='warning-box'>
                <h4>📋 Next Steps Required</h4>
                <p><strong>As the Workstream Leader of this department, you need to create a milestone plan for this idea to proceed to Stage 3 approval.</strong></p>
                <p>The milestone should outline the implementation timeline and deliverables.</p>
            </div>

            <div class='checklist'>
                <h4>What You Need to Do:</h4>
                <ol>
                    <li>Click the button below to access the Milestone Management page</li>
                    <li>Review the idea details and implementation plan</li>
                    <li>Create at least one milestone with:
                        <ul>
                            <li>Milestone title and description</li>
                            <li>Start and end dates</li>
                            <li>Assign Person In Charge (PIC) from the implementators</li>
                        </ul>
                    </li>
                    <li>Once milestone is created, click ""Send to Approval S3"" button to request Stage 3 approval</li>
                </ol>
            </div>

            <a href='{milestoneUrl}' class='action-button' style='color: white !important;'>Create Milestone Now</a>

            <p>If you cannot click the button above, copy and paste this URL into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{milestoneUrl}</p>

            <p><strong>Important:</strong> Stage 3 approval can only be requested after at least one milestone has been created.</p>

            <p>If you have questions about milestone creation, please contact the Innovation Team or refer to the system documentation.</p>

            <p>Best regards,<br>The Ideku Team</p>
        </div>

        <div class='footer'>
            <p>This is an automated message from the Ideku Idea Management System. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateWorkstreamLeaderNotificationEmailBody(Models.Entities.Idea idea, User workstreamLeader)
        {
            var ideaUrl = $"{_emailSettings.BaseUrl}/Idea/Details/{idea.Id}";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ width: 90%; max-width: 1200px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #17a2b8, #138496); color: white; padding: 25px; border-radius: 8px 8px 0 0; margin: -40px -40px 30px -40px; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .idea-details {{ background-color: #f8f9fa; padding: 25px; border-radius: 6px; margin: 20px 0; }}
        .action-button {{ display: inline-block; background-color: #17a2b8; color: white; padding: 15px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-size: 16px; }}
        .background-section {{ background-color: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #6c757d; }}
        .solution-section {{ background-color: #e8f5e8; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #28a745; }}
        @media screen and (max-width: 768px) {{ 
            .container {{ max-width: 95%; padding: 20px; }} 
            .header {{ margin: -20px -20px 30px -20px; padding: 20px; }}
            .header h1 {{ font-size: 24px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💡 Related Idea Approved</h1>
        </div>
        
        <div class='content'>
            <p>Dear {workstreamLeader.Name},</p>
            
            <p>An idea has been approved and your division (<strong>{workstreamLeader.Employee.DivisionNavigation?.NameDivision}</strong>) has been marked as related for potential collaboration.</p>
            
            <div class='idea-details'>
                <h3>{idea.IdeaName}</h3>
                <p><strong>Idea Code:</strong> {idea.IdeaCode}</p>
                <p><strong>Initiator:</strong> {idea.InitiatorUser?.Name}</p>
                <p><strong>Target Division:</strong> {idea.TargetDivision?.NameDivision}</p>
                <p><strong>Category:</strong> {idea.Category?.CategoryName}</p>
                <p><strong>Validated Saving Cost:</strong> {idea.SavingCostValidated:C}</p>
                <p><strong>Current Status:</strong> {idea.CurrentStatus}</p>
            </div>
            
            <div class='background-section'>
                <h4>Idea Description</h4>
                <p>{idea.IdeaIssueBackground}</p>
            </div>
            
            <div class='solution-section'>
                <h4>Idea Solution</h4>
                <p>{idea.IdeaSolution}</p>
            </div>
            
            <p>As the Workstream Leader for <strong>{workstreamLeader.Employee.DivisionNavigation?.NameDivision}</strong>, your division may be involved in the implementation process. Please review the details and prepare for potential collaboration.</p>
            
            <a href='{ideaUrl}' class='action-button' style='color: white !important;'>View Idea Details</a>
            
            <p>If you have questions about this idea or need clarification on your division's involvement, please contact the idea initiator.</p>
            
            <p>Best regards,<br>The Ideku Team</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}