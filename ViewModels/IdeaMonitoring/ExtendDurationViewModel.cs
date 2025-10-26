using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels.IdeaMonitoring
{
    public class ExtendDurationViewModel
    {
        [Required]
        public long IdeaId { get; set; }

        [Required]
        [Range(1, 12, ErrorMessage = "Additional months must be between 1 and 12")]
        public int AdditionalMonths { get; set; }
    }
}
