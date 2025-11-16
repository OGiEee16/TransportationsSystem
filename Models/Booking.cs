using System;

namespace TransportationsSystem.Models
{
    public class Booking
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int vehicle_id { get; set; }
        public string vehicle_name { get; set; } = "";
        public int capacity { get; set; }
        public string origin { get; set; } = "";
        public string destination { get; set; } = "";
        public DateTime schedule { get; set; }
        public string status { get; set; } = "";
        public DateTime created_at { get; set; }
        public int? driver_id { get; set; }  // ✅ Driver assignment
        public string? driver_name { get; set; }  // ✅ For display purposes
        public string? user_full_name { get; set; }  // ✅ For driver view display
        public string? driver_status { get; set; }  // ✅ PENDING_DRIVER_RESPONSE, ACCEPTED, DECLINED
        
        // ✅ Payment fields
        public decimal payment_amount { get; set; }  // ✅ Calculated payment amount
        public string payment_status { get; set; } = "UNPAID";  // ✅ UNPAID, PAID
        public string trip_type { get; set; } = "ONE_WAY";  // ✅ ONE_WAY, ROUND_TRIP, DAY_TRIP
        public int trip_days { get; set; } = 1;  // ✅ Number of days for day trip
    }


}
