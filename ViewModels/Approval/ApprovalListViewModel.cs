using Ideku.Models.Entities;
using System.Collections.Generic;

namespace Ideku.ViewModels.Approval
{
    public class ApprovalListViewModel
    {
        public IEnumerable<Idea> IdeasForApproval { get; set; } = new List<Idea>();
    }
}
