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

        // ✅ Dashboard with statistics (Index page)
        public async Task<IActionResult> Index()
        {
            var stats = await _db.GetDashboardStatsAsync();
            return View(stats);
        }

        public async Task<IActionResult> Bookings()
        {
            var bookings = await _db.GetAllBookingsWithVehicleAsync();
            var drivers = await _db.GetAllDriversAsync();  // ✅ Get all drivers
    ViewBag.Drivers = drivers;
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

        // ✅ Assign driver to booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(int bookingId, int driverId)
        {
            var booking = await _db.GetBookingByIdAsync(bookingId);
            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Bookings");
            }

            if (booking.status != "APPROVED")
            {
                TempData["Error"] = "Only approved bookings can be assigned to drivers.";
                return RedirectToAction("Bookings");
            }

            var driver = await _db.GetUserByIdAsync(driverId);
            if (driver == null || driver.role != "DRIVER")
            {
                TempData["Error"] = "Invalid driver selected.";
                return RedirectToAction("Bookings");
            }

            var success = await _db.AssignDriverToBookingAsync(bookingId, driverId);

            if (success)
            {
                TempData["Success"] = $"Driver '{driver.full_name}' assigned to booking #{bookingId} successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to assign driver to booking.";
            }

            return RedirectToAction("Bookings");
        }

        // ✅ View to manage drivers
        public async Task<IActionResult> Drivers()
        {
            var drivers = await _db.GetAllDriversAsync();
            return View(drivers);
        }

        // ✅ View to manage all users
        public async Task<IActionResult> Users()
    {
var users = await _db.GetAllUsersAsync();
            return View(users);
        }

        // ✅ Delete user
   [HttpPost]
        [ValidateAntiForgeryToken]
 public async Task<IActionResult> DeleteUser(int userId)
        {
      var user = await _db.GetUserByIdAsync(userId);
        if (user == null)
            {
             TempData["Error"] = "User not found.";
       return RedirectToAction("Users");
        }

      if (user.role == "ADMIN")
            {
      TempData["Error"] = "Cannot delete admin users.";
      return RedirectToAction("Users");
            }

   var success = await _db.DeleteUserAsync(userId);
            if (success)
    {
              TempData["Success"] = $"User '{user.full_name}' deleted successfully.";
            }
    else
            {
                TempData["Error"] = "Failed to delete user.";
            }

  return RedirectToAction("Users");
    }

     // ✅ Update user role
        [HttpPost]
        [ValidateAntiForgeryToken]
   public async Task<IActionResult> UpdateUserRole(int userId, string newRole)
    {
            var user = await _db.GetUserByIdAsync(userId);
if (user == null)
      {
            TempData["Error"] = "User not found.";
      return RedirectToAction("Users");
      }

      if (!new[] { "USER", "DRIVER", "ADMIN" }.Contains(newRole))
   {
         TempData["Error"] = "Invalid role selected.";
 return RedirectToAction("Users");
}

 var success = await _db.UpdateUserRoleAsync(userId, newRole);
         if (success)
  {
     TempData["Success"] = $"User '{user.full_name}' role updated to {newRole}.";
   }
     else
          {
   TempData["Error"] = "Failed to update user role.";
     }

            return RedirectToAction("Users");
        }
    }
}