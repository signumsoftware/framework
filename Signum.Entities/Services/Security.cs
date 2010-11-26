using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Signum.Services
{
    public static class Security
    {
        static MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
        public static string EncodePassword(string originalPassword)
        {
            //Instantiate MD5CryptoServiceProvider, get bytes for original password and compute hash (encoded password)
            byte[] originalBytes = ASCIIEncoding.Default.GetBytes(originalPassword);
            byte[] encodedBytes = provider.ComputeHash(originalBytes);
            //Convert encoded bytes back to a 'readable' string
            return BitConverter.ToString(encodedBytes);
        }


        static readonly string letters = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        public static string EncodeCleanUnsafe(string str)
        {
            int hash = GetHashCode32(str);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 32; i += 5)
            {
                sb.Append(letters[hash & 31]);
                hash >>= 5;
            }

            return sb.ToString();
        }

        static int GetHashCode32(string value)
        {
            int num = 0x15051505;
            int num2 = num;
            for (int i = 0; i < value.Length; i++)
            {
                if ((i & 0x1) == 0)
                    num = (((num << 5) + num) + (num >> 0x1b)) ^ value[i];
                else
                    num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ value[i];
            }

            return (num + (num2 * 0x5d588b65));
        }

    }

    public class CryptorEngine
    {
        private string key;

        public CryptorEngine(string key) //"sfd5g4s5h4s5yj4r56u84ir8tyj2sfd2st1h86ty"
        {
            this.key = key;
        }

        byte[] GetMD5KeyHash()
        {
            using (MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider())
            {
                return hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            }
        }

        TripleDESCryptoServiceProvider TripleDesCryptoService()
        {
            return new TripleDESCryptoServiceProvider()
            {
                Key = GetMD5KeyHash(),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7,
            };
        }

        public string Encrypt(string text)
        {
            using (TripleDESCryptoServiceProvider tdes = TripleDesCryptoService())
            {
                byte[] toEncrypt = UTF8Encoding.UTF8.GetBytes(text);

                byte[] result = tdes.CreateEncryptor().TransformFinalBlock(toEncrypt, 0, toEncrypt.Length);

                return Convert.ToBase64String(result, 0, result.Length);
            }
        }

        public string Decrypt(string encrypted)
        {
            using (TripleDESCryptoServiceProvider tdes = TripleDesCryptoService())
            {
                byte[] toDecrypt = Convert.FromBase64String(encrypted);

                byte[] resultArray = tdes.CreateDecryptor().TransformFinalBlock(toDecrypt, 0, toDecrypt.Length);

                return UTF8Encoding.UTF8.GetString(resultArray);
            }
        }
    }
}