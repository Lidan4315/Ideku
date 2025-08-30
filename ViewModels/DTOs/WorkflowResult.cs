namespace Ideku.ViewModels.DTOs
{
    /// <summary>
    /// Result object for workflow operations
    /// </summary>
    public class WorkflowResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public long? IdeaId { get; set; }
        public int? NextStage { get; set; }
        public string? NextStatus { get; set; }

        /// <summary>
        /// Create successful result
        /// </summary>
        public static WorkflowResult Success(string message = "Operation completed successfully")
        {
            return new WorkflowResult 
            { 
                IsSuccess = true, 
                SuccessMessage = message 
            };
        }

        /// <summary>
        /// Create successful result with data
        /// </summary>
        public static WorkflowResult Success(long ideaId, int nextStage, string nextStatus, string message = "Approval processed successfully")
        {
            return new WorkflowResult
            {
                IsSuccess = true,
                SuccessMessage = message,
                IdeaId = ideaId,
                NextStage = nextStage,
                NextStatus = nextStatus
            };
        }

        /// <summary>
        /// Create failure result
        /// </summary>
        public static WorkflowResult Failure(string errorMessage)
        {
            return new WorkflowResult 
            { 
                IsSuccess = false, 
                ErrorMessage = errorMessage 
            };
        }
    }
}