using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Web;

namespace GorillaGolfCommon.core
{
	/// <summary>
	/// Encryption package
	/// </summary>
	public class Pack
	{
		const string locker = "selkrnpx043m4$).";
		const string iv = "abcdefghijklmnop";

		private Pack()
		{
		}

		private static byte[] LockBytes()
		{
			return Encoding.UTF8.GetBytes(locker);
		}

		private static byte[] IVBytes()
		{
			return Encoding.UTF8.GetBytes(iv);
		}

		public static string PackIt(string indata)
		{
			byte[] encoded = ToMemory(indata, LockBytes(), IVBytes());
			return ToBase64(encoded);
		}

		public static string PackURL(string indata)
		{
			byte[] encoded = ToMemory(indata, LockBytes(), IVBytes());
			return HttpUtility.UrlEncode(ToBase64(encoded));
		}

		public static string UnpackIt(string indata)
		{
            if (String.IsNullOrEmpty(indata)) return "";

			byte[] encoded = FromBase64(indata);
			string decoded = FromMemory(encoded, LockBytes(), IVBytes());
			//-- remove any trailing nulls due to block size
			while (decoded.Length > 0 && decoded[decoded.Length - 1] == '\0') 
			{
				decoded = decoded.Remove(decoded.Length - 1, 1);
			}
			return decoded;
		}


		public static byte[] ToMemory(string Data,  byte[] Key, byte[] IV)
		{
			try
			{
				// Create a MemoryStream.
				MemoryStream mStream = new MemoryStream();

				// Create a CryptoStream using the MemoryStream 
				// and the passed key and initialization vector (IV).
				CryptoStream cStream = new CryptoStream(mStream, 
					new TripleDESCryptoServiceProvider().CreateEncryptor(Key, IV), 
					CryptoStreamMode.Write);

				// Convert the passed string to a byte array.
				byte[] toEncrypt = Encoding.UTF8.GetBytes(Data);

				// Write the byte array to the crypto stream and flush it.
				cStream.Write(toEncrypt, 0, toEncrypt.Length);
				cStream.FlushFinalBlock();
        
				// Get an array of bytes from the 
				// MemoryStream that holds the 
				// encrypted data.
				byte[] ret = mStream.ToArray();

				// Close the streams.
				cStream.Close();
				mStream.Close();

				// Return the encrypted buffer.
				return ret;
			}
			catch(CryptographicException)
			{
				return null;
			}

		}

		public static string FromMemory(byte[] Data,  byte[] Key, byte[] IV)
		{
			try
			{
				// Create a new MemoryStream using the passed 
				// array of encrypted data.
				MemoryStream msDecrypt = new MemoryStream(Data);

				// Create a CryptoStream using the MemoryStream 
				// and the passed key and initialization vector (IV).
				CryptoStream csDecrypt = new CryptoStream(msDecrypt, 
					new TripleDESCryptoServiceProvider().CreateDecryptor(Key, IV), 
					CryptoStreamMode.Read);

				// Create buffer to hold the decrypted data.
				byte[] fromEncrypt = new byte[Data.Length];

				// Read the decrypted data out of the crypto stream
				// and place it into the temporary buffer.
				csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);

				//Convert the buffer into a string and return it.
				return Encoding.UTF8.GetString(fromEncrypt);
			}
			catch(CryptographicException)
			{
				return null;
			}
		}

		public static string ToBase64(byte[] inputBytes)
		{
			return Convert.ToBase64String(inputBytes);
		}

		public static byte[] FromBase64(string indata)
		{
			return Convert.FromBase64String(indata);
		}

        // Convert a number from base10 to another base
        public static string FromBase10(long number, int target_base)
        {
            if (target_base < 2 || target_base > 36) return "";
            if (target_base == 10) return number.ToString();

            int n = target_base;
            long q = number;
            string rtn = "";

            while (q >= n)
            {
                long r = q % n;
                q = q / n;

                if (r < 10)
                    rtn = r.ToString() + rtn;
                else
                    rtn = Convert.ToChar(r + 55).ToString() + rtn;
            }

            if (q < 10)
                rtn = q.ToString() + rtn;
            else
                rtn = Convert.ToChar(q + 55).ToString() + rtn;

            return rtn;
        }

        // Convert from a number from a different base to base10
        public static long ToBase10(string number, int start_base)
        {
            if (start_base < 2 || start_base > 36) return 0;
            if (start_base == 10) return Convert.ToInt32(number);

            char[] chrs = number.ToCharArray();
            int m = chrs.Length - 1;
            int n = start_base;
            long rtn = 0;

            foreach (char c in chrs)
            {
                int x;
                if (char.IsNumber(c))
                    x = int.Parse(c.ToString());
                else
                    x = Convert.ToInt32(c) - 55;

                rtn += x * (Convert.ToInt64(Math.Pow(n, m)));

                m--;
            }

            return rtn;
        }

        public static string ToHex(byte[] data)
        {
            string hex = "";
            foreach (char ch in data)
            {
                hex += String.Format("{0:x2}", ((UInt16)ch));
            }
            return hex;
        }
	}
}
