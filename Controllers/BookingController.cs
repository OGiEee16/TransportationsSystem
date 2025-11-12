using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TransportationsSystem.Data;
using TransportationsSystem.Models;

namespace TransportationsSystem.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly MySqlHelper _db;
        public BookingController(IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("DefaultConnection");
            _db = new MySqlHelper(conn);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vehicles = await _db.GetAllVehiclesAsync();
            ViewBag.Vehicles = vehicles;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int vehicleId, string origin, string destination, string schedule)
        {
            if (!DateTime.TryParse(schedule, out var scheduleDt))
            {
                ViewBag.Error = "Invalid schedule datetime.";
                ViewBag.Vehicles = await _db.GetAllVehiclesAsync();
                return View();
            }

            var username = User.Identity?.Name;
            var user = await _db.GetUserByUsernameAsync(username);
            if (user == null) return RedirectToAction("Login", "Account");

            var vehicle = await _db.GetVehicleByIdAsync(vehicleId);
            if (vehicle == null)
            {
                ViewBag.Error = "Selected vehicle not found.";
                ViewBag.Vehicles = await _db.GetAllVehiclesAsync();
                return View();
            }

            var currentBookings = await _db.CountBookingsForVehicleAtScheduleAsync(vehicleId, scheduleDt);
            if (currentBookings >= vehicle.capacity)
            {
                ViewBag.Error = $"No available seats on {vehicle.name} at that schedule.";
                ViewBag.Vehicles = await _db.GetAllVehiclesAsync();
                return View();
            }

            var booking = new Booking
            {
                user_id = user.id,
                vehicle_id = vehicleId,
                vehicle_name = vehicle.name,
                capacity = vehicle.capacity,
                origin = origin,
                destination = destination,
                schedule = scheduleDt,
                status = "PENDING",
                created_at = DateTime.Now  // ✅ Set created_at timestamp
            };


            await _db.CreateBookingAsync(booking);
            TempData["Success"] = "Booking created successfully and is pending approval.";
            return RedirectToAction("MyBookings");
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var username = User.Identity?.Name;
            var user = await _db.GetUserByUsernameAsync(username);
            if (user == null) return RedirectToAction("Login", "Account");
            var bookings = await _db.GetBookingsByUserAsync(user.id);
            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var username = User.Identity?.Name;
            var user = await _db.GetUserByUsernameAsync(username);
            if (user == null) return RedirectToAction("Login", "Account");

            var success = await _db.CancelBookingAsync(bookingId, user.id);
          
            if (success)
            {
                TempData["Success"] = "Booking cancelled successfully.";
            }
            else
            {
                TempData["Error"] = "Unable to cancel booking. Only pending bookings can be cancelled.";
            }

            return RedirectToAction("MyBookings");
        }
    }
}
