using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Models.Entities;

namespace Ideku.ViewModels.WorkflowManagement
{
    public class WorkflowDetailsViewModel
    {
        // Main Workflow Data
        public Workflow Workflow { get; set; } = null!;

        // Dropdown Lists for Add Stage Form
        public List<SelectListItem> LevelList { get; set; } = new List<SelectListItem>();

        // Dropdown Lists for Add Condition Form
        public List<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DivisionList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DepartmentList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EventList { get; set; } = new List<SelectListItem>();

        // Add Stage Form Data
        public WorkflowStageViewModel AddStageForm { get; set; } = new WorkflowStageViewModel();

        // Add Condition Form Data
        public WorkflowConditionViewModel AddConditionForm { get; set; } = new WorkflowConditionViewModel();

        // Predefined Options
        public List<SelectListItem> ConditionTypeList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "SAVING_COST", Text = "Saving Cost" },
            new SelectListItem { Value = "CATEGORY", Text = "Category" },
            new SelectListItem { Value = "DIVISION", Text = "Division" },
            new SelectListItem { Value = "DEPARTMENT", Text = "Department" },
            new SelectListItem { Value = "EVENT", Text = "Event" }
        };

        public List<SelectListItem> OperatorList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = ">=", Text = "Greater than or equal (>=)" },
            new SelectListItem { Value = "<=", Text = "Less than or equal (<=)" },
            new SelectListItem { Value = "=", Text = "Equal (=)" },
            new SelectListItem { Value = "!=", Text = "Not equal (!=)" },
            new SelectListItem { Value = "IN", Text = "In list (IN)" },
            new SelectListItem { Value = "NOT_IN", Text = "Not in list (NOT_IN)" }
        };
    }
}