using Ideku.Data;
using Ideku.Models;
using Ideku.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // <-- Tambahkan ini

namespace Ideku.Controllers
{
    [Authorize]
    public class IdeaController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IdeaController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // [GET] /Idea/Create : Menampilkan form dan mengirim data Divisi
        public async Task<IActionResult> Create()
        {
            var viewModel = new IdeaCreateViewModel();
            // Ambil semua divisi dan kirim ke view melalui ViewBag
            ViewBag.Divisions = await _context.Divisi.ToListAsync();
            ViewBag.Categories = await _context.Category.ToListAsync();
            ViewBag.Events = await _context.Event.ToListAsync();
            return View(viewModel);
        }

        // [POST] /Idea/Create : Memproses data dari form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IdeaCreateViewModel model)
        {
            var initiator = await _context.Employees.FindAsync(model.BadgeNumber);
            if (initiator == null)
            {
                ModelState.AddModelError("BadgeNumber", "Employee with this Badge Number not found.");
            }

            if (ModelState.IsValid)
            {
                string? uniqueFileName = null;

                if (model.AttachmentFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments");
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.AttachmentFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AttachmentFile.CopyToAsync(fileStream);
                    }
                }

                var newIdea = new Idea
                {
                    Initiator = initiator.Name,
                    IdeaName = model.IdeaName,
                    Division = model.Division,     
                    Department = model.Department,
                    IdeaIssueBackground = model.IdeaIssueBackground,
                    IdeaSolution = model.IdeaSolution,
                    SavingCost = model.SavingCost,
                    AttachmentFile = uniqueFileName,
                    SubmittedDate = DateTime.Now,
                    CurrentStatus = "Submitted",
                    CurrentStage = 0,
                    CategoryId = model.Category, // Asumsi model.Category menyimpan ID
                    EventId = model.Event   
                };

                _context.Ideas.Add(newIdea);
                await _context.SaveChangesAsync();

                // ↓↓↓ TAMBAHKAN BARIS INI UNTUK MENGATUR PESAN SUKSES ↓↓↓
                TempData["SuccessMessage"] = "Your new idea has been successfully submitted!";

                // Redirect ke halaman yang sama (form kosong) setelah berhasil
                return RedirectToAction("Create");
            }

            ViewBag.Divisions = await _context.Divisi.ToListAsync();
            ViewBag.Categories = await _context.Category.ToListAsync();
            ViewBag.Events = await _context.Event.ToListAsync();
            return View(model);
        }

        // API untuk auto-fill data initiator
        [HttpGet]
        public async Task<IActionResult> GetEmployeeData(string id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return Ok(employee);
        }

        // --- TAMBAHKAN METODE BARU UNTUK "MY IDEAS" ---
        public async Task<IActionResult> Index()
        {
            // 1. Ambil username dari pengguna yang sedang login
            var username = User.Identity.Name;

            // 2. Cari nama lengkap (initiator) berdasarkan username
            var initiatorName = await _context.Users
                                            .Where(u => u.Username == username)
                                            .Select(u => u.Name)
                                            .FirstOrDefaultAsync();

            // 3. Ambil semua ide yang initiator-nya cocok dengan nama lengkap pengguna
            var userIdeas = new List<Idea>();
            if (!string.IsNullOrEmpty(initiatorName))
            {
                userIdeas = await _context.Ideas
                                        .Where(i => i.Initiator == initiatorName)
                                        .OrderByDescending(i => i.SubmittedDate)
                                        .ToListAsync();
            }

            // 4. Kirim daftar ide yang sudah difilter ke view
            return View(userIdeas);
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByDivision(string divisionId)
        {
            if (string.IsNullOrEmpty(divisionId))
            {
                return Json(new List<Departement>()); // Kembalikan list kosong jika tidak ada id
            }

            var departments = await _context.Departement
                                            .Where(d => d.DivisiId == divisionId)
                                            .ToListAsync();
            return Json(departments);
        }
    }
}