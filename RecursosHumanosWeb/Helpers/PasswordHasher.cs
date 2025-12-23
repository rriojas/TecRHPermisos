using System.Security.Cryptography;
using System.Text;

namespace RecursosHumanosWeb.Helpers
{
    public static class PasswordHasher
    {
        // Hash a password using SHA256 and return lowercase hex string
        public static string HashSha256(string input)
        {
            if (input == null) return string.Empty;

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);

            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("X2")); // Uppercase hex to match SQL CONVERT(..., 2)

            return sb.ToString();
        }
    }
}
