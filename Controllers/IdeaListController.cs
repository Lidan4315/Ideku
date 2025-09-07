using Ideku.Data.Repositories;
using Ideku.Services.Idea;
using Ideku.Services.IdeaRelation;
using Ideku.ViewModels.IdeaList;
using Ideku.Extensions;
using Ideku.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ideku.Controllers
{
    [Authorize]
    public class IdeaListController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly IUserRepository _userRepository;
        private readonly ILookupRepository _lookupRepository;
        private readonly IIdeaRelationService _ideaRelationService;
        private readonly ILogger<IdeaListController> _logger;

        public IdeaListController(
            IIdeaService ideaService, 
            IUserRepository userRepository, 
            ILookupRepository lookupRepository,
            IIdeaRelationService ideaRelationService,
            ILogger<IdeaListController> logger)
        {
            _ideaService = ideaService;
            _userRepository = userRepository;
            _lookupRepository = lookupRepository;
            _ideaRelationService = ideaRelationService;
            _logger = logger;
        }

        // GET: /IdeaList or /IdeaList/Index
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? selectedDivision = null,
            string? selectedDepartment = null,
            int? selectedCategory = null,
            int? selectedStage = null,
            string? selectedStatus = null)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Challenge();
            }

            // Validate and normalize pagination parameters
            pageSize = PaginationHelper.ValidatePageSize(pageSize);
            page = Math.Max(1, page);

            // Get user info for ViewBag
            var user = await _userRepository.GetByUsernameAsync(username);

            // Get ideas based on user role (superuser sees all, others filtered by role)
            var ideasQuery = await _ideaService.GetAllIdeasQueryAsync(username);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ideasQuery = ideasQuery.Where(i => 
                    i.IdeaName.Contains(searchTerm) || 
                    i.IdeaIssueBackground.Contains(searchTerm) ||
                    i.IdeaSolution.Contains(searchTerm) ||
                    i.InitiatorUser.Employee.NAME.Contains(searchTerm) ||
                    i.InitiatorUser.Employee.EMP_ID.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(selectedDivision))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDivisionId == selectedDivision);
            }

            if (!string.IsNullOrWhiteSpace(selectedDepartment))
            {
                ideasQuery = ideasQuery.Where(i => i.ToDepartmentId == selectedDepartment);
            }

            if (selectedCategory.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CategoryId == selectedCategory.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStatus == selectedStatus);
            }

            if (selectedStage.HasValue)
            {
                ideasQuery = ideasQuery.Where(i => i.CurrentStage == selectedStage.Value);
            }

            // Apply pagination - this executes the database queries
            var pagedResult = await ideasQuery.ToPagedResultAsync(page, pageSize);

            // Get lookup data for filters
            var divisions = await _lookupRepository.GetDivisionsAsync();
            var categories = await _lookupRepository.GetCategoriesAsync();
            
            var viewModel = new IdeaListViewModel
            {
                PagedIdeas = pagedResult,
                SearchTerm = searchTerm,
                SelectedDivision = selectedDivision,
                SelectedDepartment = selectedDepartment,
                SelectedCategory = selectedCategory,
                SelectedStage = selectedStage,
                SelectedStatus = selectedStatus
            };

            // Pass lookup data to view
            ViewBag.Divisions = divisions;
            ViewBag.Categories = categories;
            ViewBag.UserRole = user?.Role?.RoleName ?? "";

            return View(viewModel);
        }

    }
}