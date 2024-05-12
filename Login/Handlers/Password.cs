using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;

namespace Login.Handlers
{
    public class Password
    {
        public static string hashPassword(string password)
        {
            string result = BCrypt.Net.BCrypt.EnhancedHashPassword(password, 12);
            return result;
        }
        
        public static bool controlHash(string password, string hash)
        {
            
            bool result = BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
            return result;
        }
    }
}
