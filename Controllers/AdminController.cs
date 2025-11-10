using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TransportationsSystem.Data;

namespace TransportationsSystem.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly MySqlHelper _db;

        public AdminController(IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("DefaultConnection");
            _db = new MySqlHelper(conn);
        }

        public IActionResult Index()
        {
            ViewBag.Message = "Welcome to Admin Dashboard";
            return View();
        }

        public async Task<IActionResult> Bookings()
        {
            var bookings = await _db.GetAllBookingsWithVehicleAsync();
            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int id, string actionType)
        {
            var booking = await _db.GetBookingByIdAsync(id);
            if (booking == null) return NotFound();

            var action = (actionType ?? "").Trim().ToLowerInvariant();
            if (action == "approve")
            {
                var vehicle = await _db.GetVehicleByIdAsync(booking.vehicle_id);
                if (vehicle == null) { TempData["Error"] = "Vehicle not found."; return RedirectToAction("Bookings"); }

                var approvedCount = await _db.CountApprovedBookingsForVehicleAtScheduleAsync(booking.vehicle_id, booking.schedule);
                if (approvedCount >= vehicle.capacity)
                {
                    TempData["Error"] = $"Cannot approve booking #{id}: vehicle '{vehicle.name}' is already full at that schedule.";
                    return RedirectToAction("Bookings");
                }

                await _db.UpdateBookingStatusAsync(id, "APPROVED");
                TempData["Success"] = $"Booking #{id} approved successfully.";
                return RedirectToAction("Bookings");
            }
            else if (action == "reject")
            {
                await _db.UpdateBookingStatusAsync(id, "REJECTED");
                TempData["Success"] = $"Booking #{id} rejected.";
                return RedirectToAction("Bookings");
            }

            TempData["Error"] = "Unknown action.";
            return RedirectToAction("Bookings");
        }
    }
}
        