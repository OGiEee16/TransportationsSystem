using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TransportationsSystem.Data;
using TransportationsSystem.Models;
using System.Linq;
using System.Collections.Generic;

namespace TransportationsSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly TransportationContext _context;

        public AccountController(TransportationContext context)
        {
            _context = context;
        }

        // ================== REGISTER ==================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            if (string.IsNullOrWhiteSpace(model.username) || string.IsNullOrWhiteSpace(model.password_hash))
            {
                ViewBag.Error = "Username and password are required.";
                return View(model);
            }

            // Check if username exists
            var existingUser = _context.Users.FirstOrDefault(u => u.username == model.username);
            if (existingUser != null)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            // Create new user
            var user = new User
            {
                full_name = model.full_name ?? "",
                username = model.username,
                email = model.email ?? "",
                password_hash = model.password_hash, // plain for now (you can hash later)
                role = model.role ?? "USER",
                salt = ""
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Redirect to login after successful registration
            return RedirectToAction("Login", "Account");
        }

        // ================== LOGIN ==================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.username == username && u.password_hash == password);

            if (user == null)
            {
                ViewBag.Error = "Invalid login credentials.";
                return View();
            }

            // ✅ Create login session (cookie)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.username),
                new Claim(ClaimTypes.Role, user.role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // ✅ Redirect based on role
            if (user.role == "ADMIN")
                return RedirectToAction("Bookings", "Admin");
            else if (user.role == "DRIVER")  // ✅ Driver redirect
                return RedirectToAction("Index", "Driver");
            else
                return RedirectToAction("Create", "Booking");
        }

        // ================== LOGOUT ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
