using System;
using System.Security.Cryptography;
using System.Text;

namespace MenuPlanner.Services
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Хэширует пароль с солью (SHA256)
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var bytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Генерирует случайную соль
        /// </summary>
        public static string GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[32];
                rng.GetBytes(salt);
                return Convert.ToBase64String(salt);
            }
        }

        /// <summary>
        /// Проверяет пароль
        /// </summary>
        public static bool VerifyPassword(string password, string salt, string storedHash)
        {
            var hash = HashPassword(password, salt);
            return hash == storedHash;
        }
    }
}