using System;
using System.Security.Cryptography;
using System.Text;

namespace Metamatic
{
    public static class IdsGenerator
    {
        public static string GenerateGuid()
        {
            return Guid.NewGuid().ToString();
        }
        public static string GenerateShortGuid(int length = 8)
        {
            if (length <= 0 || length > 32)
                throw new ArgumentException("Length must be between 1 and 32");

            Guid guid = Guid.NewGuid();

            // Создаем MD5 хеш от Guid
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(guid.ToByteArray());
                return ByteArrayToString(hashBytes).Substring(0, length);
            }
        }
        
        private static string ByteArrayToString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

    }
}