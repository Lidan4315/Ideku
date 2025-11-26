using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.ViewModels
{
    public class EditIdeaViewModel
    {
        // Idea ID (hidden field)
        public long Id { get; set; }

        // Initiator Profile Section (Read-only in edit mode)
        [Display(Name = "Badge Number")]
        public string BadgeNumber { get; set; } = string.Empty;

        [Display(Name = "Employee Name")]
        public string EmployeeName { get; set; } = string.Empty;

        [Display(Name = "Position")]
        public string Position { get; set; } = string.Empty;

        [Display(Name = "Division")]
        public string Division { get; set; } = string.Empty;

        [Display(Name = "Department")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Target Division & Department Section
        [Required(ErrorMessage = "Division is required")]
        [Display(Name = "To Division")]
        public string ToDivisionId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "To Department")]
        public string ToDepartmentId { get; set; } = string.Empty;

        // Category & Event Section
        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Event")]
        public int? EventId { get; set; }

        // Idea Details Section
        [Required(ErrorMessage = "Idea Name is required")]
        [StringLength(150, ErrorMessage = "Idea Name cannot exceed 150 characters")]
        [Display(Name = "Idea Name")]
        public string IdeaName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Idea Description is required")]
        [StringLength(2000, ErrorMessage = "Idea Description cannot exceed 2000 characters")]
        [Display(Name = "Idea Description")]
        public string IdeaDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Solution is required")]
        [StringLength(2000, ErrorMessage = "Solution cannot exceed 2000 characters")]
        [Display(Name = "Solution")]
        public string Solution { get; set; } = string.Empty;

        // Financial Impact Section
        [Required(ErrorMessage = "Saving Cost is required")]
        [Display(Name = "Saving Cost (USD)")]
        public long? SavingCost { get; set; }

        // Existing Attachments
        [Display(Name = "Existing Attachments")]
        public string ExistingAttachments { get; set; } = string.Empty;

        // New Attachments Section
        [Display(Name = "New Attachments")]
        public List<IFormFile> NewAttachmentFiles { get; set; } = new List<IFormFile>();

        // Dropdown Lists (populated by controller)
        public List<SelectListItem> DivisionList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DepartmentList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EventList { get; set; } = new List<SelectListItem>();

        // Hidden fields
        public string IdeaCode { get; set; } = string.Empty;
        public long InitiatorUserId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
    }
}
