using Ideku.Data;
using Ideku.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ideku.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // [GET] /Account/Login : Menampilkan halaman form login
        public IActionResult Login()
        {
            return View();
        }

        // [POST] /Account/Login : Memproses data yang dikirim dari form
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Cari user di database berdasarkan username
            var user = await _context.Users
                                     .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user != null)
            {
                // Jika user ditemukan, buat "tiket" (claims)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("FullName", user.Name), // Contoh claim tambahan
                    // Anda bisa menambahkan Role di sini jika perlu
                };

                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Buat cookie login untuk menandai user sudah terautentikasi
                await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal);

                return RedirectToAction("Index", "Home"); // Arahkan ke halaman utama
            }

            // Jika user tidak ditemukan, tampilkan pesan error
            ModelState.AddModelError("", "Invalid username.");
            return View(model);
            
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login", "Account"); // Arahkan langsung ke halaman Login
        }
    }
}