using System;
using System.Text;
using System.Security.Cryptography;

namespace Signum.Services
{
    public static class Security
    {
        public static Func<string, byte[]> EncodePassword = (string originalPassword) => MD5Hash(originalPassword);

        public static byte[] MD5Hash(string saltedPassword)
        {
            byte[] originalBytes = ASCIIEncoding.Default.GetBytes(saltedPassword);
            byte[] encodedBytes = new MD5CryptoServiceProvider().ComputeHash(originalBytes);
            return encodedBytes;
        }

        public static string GetSHA1(string str)
        {
            SHA1 sha1 = SHA1Managed.Create();
            ASCIIEncoding encoding = new ASCIIEncoding();
            StringBuilder sb = new StringBuilder();
            byte[] stream = sha1.ComputeHash(encoding.GetBytes(str));
            for (int i = 0; i < stream.Length; i++)
                sb.AppendFormat("{0:x2}", stream[i]);
            return sb.ToString();
        }
    }

    public class CryptorEngine
    {
        private string key;

        public CryptorEngine(string key)
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


        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input

            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            return CalculateMD5Hash(inputBytes);
        }

        public static string CalculateMD5Hash(byte[] inputBytes)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

    }
}
