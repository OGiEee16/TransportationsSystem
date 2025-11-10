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
    }


}
