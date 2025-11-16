using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TransportationsSystem.Data;

namespace TransportationsSystem.Controllers
{
    [Authorize(Roles = "DRIVER")]
    public class DriverController : Controller
    {
        private readonly MySqlHelper _db;

   public DriverController(IConfiguration configuration)
        {
         var conn = configuration.GetConnectionString("DefaultConnection");
         _db = new MySqlHelper(conn);
      }

   // Driver Dashboard - View assigned destinations
        public async Task<IActionResult> Index()
    {
         var username = User.Identity?.Name;
       var driver = await _db.GetUserByUsernameAsync(username);
   
          if (driver == null)
        return RedirectToAction("Login", "Account");

   var assignedBookings = await _db.GetBookingsByDriverAsync(driver.id);
      
            // Get revenue statistics
            var revenueStats = await _db.GetDriverRevenueStatsAsync(driver.id);
            
            ViewBag.DriverName = driver.full_name;
            ViewBag.DriverId = driver.id;
            ViewBag.TotalEarnings = revenueStats.ContainsKey("TotalEarnings") ? revenueStats["TotalEarnings"] : 0;
            ViewBag.PendingEarnings = revenueStats.ContainsKey("PendingEarnings") ? revenueStats["PendingEarnings"] : 0;
            ViewBag.MonthlyEarnings = revenueStats.ContainsKey("MonthlyEarnings") ? revenueStats["MonthlyEarnings"] : 0;
            ViewBag.CompletedTrips = revenueStats.ContainsKey("CompletedTrips") ? (int)revenueStats["CompletedTrips"] : 0;
            
   return View(assignedBookings);
        }

        // ? My Trips - Comprehensive trip management
        public async Task<IActionResult> MyTrips()
        {
            var username = User.Identity?.Name;
            var driver = await _db.GetUserByUsernameAsync(username);
  
        if (driver == null)
    return RedirectToAction("Login", "Account");

       var assignedBookings = await _db.GetBookingsByDriverAsync(driver.id);
         
            // Get revenue statistics
            var revenueStats = await _db.GetDriverRevenueStatsAsync(driver.id);
            
   ViewBag.DriverName = driver.full_name;
ViewBag.DriverId = driver.id;
            ViewBag.TotalEarnings = revenueStats.ContainsKey("TotalEarnings") ? revenueStats["TotalEarnings"] : 0;
            ViewBag.PendingEarnings = revenueStats.ContainsKey("PendingEarnings") ? revenueStats["PendingEarnings"] : 0;
            ViewBag.MonthlyEarnings = revenueStats.ContainsKey("MonthlyEarnings") ? revenueStats["MonthlyEarnings"] : 0;
            ViewBag.CompletedTrips = revenueStats.ContainsKey("CompletedTrips") ? (int)revenueStats["CompletedTrips"] : 0;
            
            return View(assignedBookings);
}

        // ? Accept assignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptAssignment(int bookingId)
        {
            var username = User.Identity?.Name;
   var driver = await _db.GetUserByUsernameAsync(username);
            
     if (driver == null)
       return RedirectToAction("Login", "Account");

            var booking = await _db.GetBookingByIdAsync(bookingId);
            if (booking == null || booking.driver_id != driver.id)
       {
       TempData["Error"] = "Booking not found or not assigned to you.";
       return RedirectToAction("MyTrips");
            }

            var success = await _db.AcceptAssignmentAsync(bookingId, driver.id);
    
            if (success)
      {
  TempData["Success"] = $"You have accepted the trip to {booking.destination}!";
  }
   else
    {
      TempData["Error"] = "Failed to accept assignment.";
            }

            return RedirectToAction("MyTrips");
        }

   // ? Decline assignment
      [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineAssignment(int bookingId)
        {
  var username = User.Identity?.Name;
            var driver = await _db.GetUserByUsernameAsync(username);
    
  if (driver == null)
     return RedirectToAction("Login", "Account");

     var booking = await _db.GetBookingByIdAsync(bookingId);
       if (booking == null || booking.driver_id != driver.id)
            {
      TempData["Error"] = "Booking not found or not assigned to you.";
   return RedirectToAction("MyTrips");
            }

        var success = await _db.DeclineAssignmentAsync(bookingId, driver.id);
            
            if (success)
        {
        TempData["Success"] = $"You have declined the trip to {booking.destination}. It will be reassigned.";
            }
            else
       {
    TempData["Error"] = "Failed to decline assignment.";
       }

          return RedirectToAction("MyTrips");
        }

        // ? Confirm payment received from passenger
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int bookingId)
        {
            var username = User.Identity?.Name;
            var driver = await _db.GetUserByUsernameAsync(username);
            
            if (driver == null)
                return RedirectToAction("Login", "Account");

            var booking = await _db.GetBookingByIdAsync(bookingId);
            if (booking == null || booking.driver_id != driver.id)
            {
                TempData["Error"] = "Booking not found or not assigned to you.";
                return RedirectToAction("MyTrips");
            }

            var success = await _db.ConfirmPaymentByDriverAsync(bookingId, driver.id);
            
            if (success)
            {
                TempData["Success"] = $"Payment confirmed! PHP {booking.payment_amount:N2} received for trip to {booking.destination}.";
            }
            else
            {
                TempData["Error"] = "Failed to confirm payment. The booking might already be marked as paid.";
            }

            return RedirectToAction("MyTrips");
        }
    }
}
