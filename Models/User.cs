using System;

namespace TransportationsSystem.Models
{
    public class User
    {
        public int id { get; set; }
        public string full_name { get; set; } = "";
        public string username { get; set; } = "";
        public string email { get; set; } = "";
        public string password_hash { get; set; } = "";
        public string salt { get; set; } = "";
        public string role { get; set; }
    }
}
