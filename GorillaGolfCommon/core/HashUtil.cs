using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace GorillaGolfCommon.core
{
    public sealed class HashUtil
    {
        public static string HashPassword(string passwordPlaintext)
        {
            byte[] passwordPlaintextBytes = Encoding.UTF8.GetBytes(passwordPlaintext);
            byte[] passwordHashedBytes = new SHA1CryptoServiceProvider().ComputeHash(passwordPlaintextBytes);
            return Convert.ToBase64String(passwordHashedBytes);
        }
    }
}
