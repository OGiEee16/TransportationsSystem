using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TransportationsSystem.Data;
using TransportationsSystem.Models;
using TransportationsSystem.Services;

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
        public async Task<IActionResult> Create(int vehicleId, string origin, string destination, string schedule, 
            string tripType, int tripDays, decimal paymentAmount)
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

            // Validate payment amount
            if (paymentAmount <= 0)
            {
                ViewBag.Error = "Invalid payment amount calculated.";
                ViewBag.Vehicles = await _db.GetAllVehiclesAsync();
                return View();
            }

            // Validate trip days for day trips
            if (tripType == "DAY_TRIP" && (tripDays < 1 || tripDays > 30))
            {
                ViewBag.Error = "Invalid number of days for day trip. Must be between 1 and 30 days.";
                ViewBag.Vehicles = await _db.GetAllVehiclesAsync();
                return View();
            }

            // Recalculate payment on server side to prevent tampering
            decimal calculatedAmount = PaymentCalculator.CalculatePayment(origin, destination, tripType, tripDays);
            
            // Allow small discrepancy due to rounding
            if (Math.Abs(calculatedAmount - paymentAmount) > 1)
            {
                paymentAmount = calculatedAmount; // Use server-calculated amount
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
                created_at = DateTime.Now,
                trip_type = tripType,
                trip_days = tripDays,
                payment_amount = paymentAmount,
                payment_status = "UNPAID"
            };

            await _db.CreateBookingAsync(booking);
            
            TempData["Success"] = $"Booking created successfully! Payment amount: PHP {paymentAmount:N2}. " +
                                  "Please prepare payment. The driver will be notified once your booking is approved.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int bookingId)
        {
            var username = User.Identity?.Name;
            var user = await _db.GetUserByUsernameAsync(username);
            if (user == null) return RedirectToAction("Login", "Account");

            var success = await _db.MarkBookingAsPaidAsync(bookingId, user.id);
          
            if (success)
            {
                TempData["Success"] = "Payment confirmed! Driver has been notified.";
            }
            else
            {
                TempData["Error"] = "Unable to confirm payment. Please contact support.";
            }

            return RedirectToAction("MyBookings");
        }
    }
}
