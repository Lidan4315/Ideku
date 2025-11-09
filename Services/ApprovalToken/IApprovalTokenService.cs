namespace Ideku.Services.ApprovalToken
{
    public interface IApprovalTokenService
    {
        string GenerateToken(long ideaId, long approverId, string action, int stage);
        (bool IsValid, long IdeaId, long ApproverId, string Action, int Stage, string ErrorMessage) ValidateAndDecryptToken(string token);
    }
}
