namespace Ideku.ViewModels.Common
{
    /// Shared ViewModel for Idea Filter Form
    /// Used across Approval, IdeaList, Milestone, and MyIdeas pages
    public class IdeaFilterViewModel
    {
        // Search
        public string SearchTerm { get; set; } = string.Empty;
        public string SearchPlaceholder { get; set; } = "Search by Idea ID, Title, or Initiator...";

        // Filters
        public string SelectedDivision { get; set; } = string.Empty;
        public string SelectedDepartment { get; set; } = string.Empty;
        public int? SelectedCategory { get; set; }
        public int? SelectedStage { get; set; }
        public string SelectedStatus { get; set; } = string.Empty;

        // URLs
        public string ClearAllUrl { get; set; } = string.Empty;

        // Configuration - Which filters to show
        public bool ShowDivisionFilter { get; set; } = true;
        public bool ShowDepartmentFilter { get; set; } = true;
        public bool ShowCategoryFilter { get; set; } = true;
        public bool ShowStageFilter { get; set; } = true;
        public bool ShowStatusFilter { get; set; } = true;

        // Status Options (for custom status lists like Milestone page)
        public List<string>? StatusOptions { get; set; }

        // Available Stages (dynamically loaded from database)
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableStages { get; set; } = new();
    }
}
