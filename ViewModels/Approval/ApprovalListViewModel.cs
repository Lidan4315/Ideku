using Ideku.Models.Entities;
using System.Collections.Generic;

namespace Ideku.ViewModels.Approval
{
    public class ApprovalListViewModel
    {
        public IEnumerable<Idea> IdeasForApproval { get; set; } = new List<Idea>();
        
        // Filter Properties
        public string? SearchTerm { get; set; }
        public string? SelectedDivision { get; set; }
        public string? SelectedDepartment { get; set; }
        public int? SelectedCategory { get; set; }
        public int? SelectedStage { get; set; }
        public string? SelectedStatus { get; set; }
    }
}
