using Ideku.Models.Entities;

namespace Ideku.ViewModels.UserManagement
{
    /// <summary>
    /// ViewModel for User Details modal/page
    /// Contains comprehensive user information for display
    /// </summary>
    public class UserDetailsViewModel
    {
        /// <summary>
        /// Complete user entity with all related data
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// User dependency information for deletion safety
        /// </summary>
        public UserDependencyInfo DependencyInfo { get; set; } = new UserDependencyInfo();

        /// <summary>
        /// Formatted display properties for better UI presentation
        /// </summary>
        public UserDisplayInfo DisplayInfo { get; set; } = new UserDisplayInfo();
    }

    /// <summary>
    /// Helper class for formatted display information
    /// Makes it easier to display user data in a consistent format
    /// </summary>
    public class UserDisplayInfo
    {
        /// <summary>
        /// User status badge (Active/Inactive) based on employee status
        /// </summary>
        public string StatusBadge { get; set; } = string.Empty;

        /// <summary>
        /// Status color class for CSS styling
        /// </summary>
        public string StatusColorClass { get; set; } = string.Empty;

        /// <summary>
        /// Acting status display text
        /// </summary>
        public string ActingStatusText { get; set; } = string.Empty;

        /// <summary>
        /// Formatted creation date
        /// </summary>
        public string CreatedDateFormatted { get; set; } = string.Empty;

        /// <summary>
        /// Formatted last updated date
        /// </summary>
        public string UpdatedDateFormatted { get; set; } = string.Empty;

        /// <summary>
        /// Full employee information string for display
        /// Format: "John Doe (EMP001) - Manager IT"
        /// </summary>
        public string EmployeeFullInfo { get; set; } = string.Empty;

        /// <summary>
        /// Division and department combination
        /// Format: "IT Division - Development Department"
        /// </summary>
        public string LocationInfo { get; set; } = string.Empty;
    }

    /// <summary>
    /// User dependency information for deletion safety checks
    /// Reused from Service layer but with additional display properties
    /// </summary>
    public class UserDependencyInfo
    {
        public int IdeasCount { get; set; }
        public int WorkflowActionsCount { get; set; }
        public int MilestonesCount { get; set; }
        
        public int TotalDependencies => IdeasCount + WorkflowActionsCount + MilestonesCount;
        public bool CanDelete => TotalDependencies == 0;
        
        /// <summary>
        /// User-friendly dependency summary for display
        /// </summary>
        public string DependencySummary
        {
            get
            {
                if (CanDelete) return "This user can be safely deleted.";
                
                var dependencies = new List<string>();
                if (IdeasCount > 0) dependencies.Add($"{IdeasCount} idea{(IdeasCount > 1 ? "s" : "")}");
                if (WorkflowActionsCount > 0) dependencies.Add($"{WorkflowActionsCount} workflow action{(WorkflowActionsCount > 1 ? "s" : "")}");
                if (MilestonesCount > 0) dependencies.Add($"{MilestonesCount} milestone{(MilestonesCount > 1 ? "s" : "")}");
                
                return $"This user has created {string.Join(", ", dependencies)}. Please reassign these items before deletion.";
            }
        }

        /// <summary>
        /// CSS class for dependency warning styling
        /// </summary>
        public string WarningClass => CanDelete ? "text-success" : "text-warning";

        /// <summary>
        /// Icon class for dependency status
        /// </summary>
        public string IconClass => CanDelete ? "bi-check-circle" : "bi-exclamation-triangle";
    }
}