using Microsoft.AspNetCore.Mvc.Rendering;
using Ideku.Models.Entities;

namespace Ideku.ViewModels.LevelManagement
{
    public class LevelIndexViewModel
    {
        // Main data
        public IEnumerable<Level> Levels { get; set; } = new List<Level>();

        // For Add Level Modal
        public CreateLevelViewModel CreateLevelForm { get; set; } = new CreateLevelViewModel();

        // Dropdown data
        public List<SelectListItem> RoleList { get; set; } = new List<SelectListItem>();
    }
}