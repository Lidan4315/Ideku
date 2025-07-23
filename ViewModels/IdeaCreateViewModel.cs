using System.ComponentModel.DataAnnotations;

namespace Ideku.ViewModels
{
    public class IdeaCreateViewModel
    {
        [Required(ErrorMessage = "Badge Number is required.")]
        [Display(Name = "Badge Number")]
        public string BadgeNumber { get; set; }

        public string? InitiatorName { get; set; }
        public string? InitiatorEmail { get; set; }

        [Required(ErrorMessage = "Idea Name is required.")]
        [Display(Name = "Idea Name")]
        [MaxLength(256)]
        public string IdeaName { get; set; }

        [Required(ErrorMessage = "Please select a Division.")]
        [Display(Name = "To Division")]
        public string Division { get; set; }

        [Required(ErrorMessage = "Please select a Department.")]
        [Display(Name = "To Department")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Please select a Category.")]
        [Display(Name = "Category")]
        public int? Category { get; set; } // Ubah menjadi int?

        [Required(ErrorMessage = "Please select an Event.")]
        [Display(Name = "Event")]
        public int? Event { get; set; } // Ubah menjadi int?

        [Required(ErrorMessage = "Idea Description is required.")]
        [Display(Name = "Idea Description")]
        public string IdeaIssueBackground { get; set; }

        [Required(ErrorMessage = "Expected Solution is required.")]
        [Display(Name = "Expected Solution")]
        public string IdeaSolution { get; set; }

        [Required(ErrorMessage = "Saving Cost is required.")]
        [Display(Name = "Saving Cost")]
        public decimal? SavingCost { get; set; }

        [Required(ErrorMessage = "Please attach a file.")]
        [Display(Name = "Attachment")]
        public IFormFile AttachmentFile { get; set; }
    }
}