using System;
using System.Security.Cryptography;

namespace TransportationsSystem.Helpers
{
    public static class SecurityHelper
    {
        public static string GenerateSalt(int size = 16)
        {
            var bytes = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        public static string HashPassword(string password, string salt, int iterations = 10000)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, saltBytes, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256))
            {
                var hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            var hashed = HashPassword(enteredPassword, storedSalt);
            return hashed == storedHash;
        }
    }
}
