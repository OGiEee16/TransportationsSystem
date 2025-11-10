using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TransportationsSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        [Authorize(Roles = "ADMIN")]
        public IActionResult AdminDashboard()
        {
            ViewBag.Message = "Welcome, Admin!";
            return View();
        }

        [Authorize(Roles = "USER")]
        public IActionResult UserDashboard()
        {
            ViewBag.Message = "Welcome, User!";
            return View();
        }
    }
}
