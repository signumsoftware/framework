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
    }

    public class CryptorEngine
    {
        private string key;
        //constructor
        public CryptorEngine()
        {
            /* Establecer una clave. La misma clave
               debe ser utilizada para descifrar
               los datos que son cifrados con esta clave.
               pueden ser los caracteres que uno desee*/
            //key = "ABCDEFGHIJKLMÑOPQRSTUVWXYZabcdefghijklmnñopqrstuvwxyz";
            key = "sfd5g4s5h4s5yj4r56u84ir8tyj2sfd2st1h86ty";
        }

        public void SetKey(string text)
        {
            key = text;
        }

        public string Encrypt(string text)
        {
            //arreglo de bytes donde guardaremos la llave
            byte[] keyArray;
            //arreglo de bytes donde guardaremos el texto
            //que vamos a encriptar
            byte[] toEncrypt =
            UTF8Encoding.UTF8.GetBytes(text);

            //se utilizan las clases de encriptación
            //provistas por el Framework
            //Algoritmo MD5
            MD5CryptoServiceProvider hashmd5 =
            new MD5CryptoServiceProvider();
            //se guarda la llave para que se le realice
            //hashing
            keyArray = hashmd5.ComputeHash(
            UTF8Encoding.UTF8.GetBytes(key));

            hashmd5.Clear();

            //Algoritmo 3DAS
            TripleDESCryptoServiceProvider tdes =
            new TripleDESCryptoServiceProvider();

            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            //se empieza con la transformación de la cadena
            ICryptoTransform cTransform =
            tdes.CreateEncryptor();

            //arreglo de bytes donde se guarda la
            //cadena cifrada
            byte[] result =
            cTransform.TransformFinalBlock(toEncrypt,
            0, toEncrypt.Length);

            tdes.Clear();

            //se regresa el resultado en forma de una cadena
            return Convert.ToBase64String(result,
                   0, result.Length);
        }
        public string Decrypt(string encryptText)
        {
            byte[] keyArray;
            //convierte el texto en una secuencia de bytes
            byte[] toDecrypt =
            Convert.FromBase64String(encryptText);

            //se llama a las clases que tienen los algoritmos
            //de encriptación se le aplica hashing
            //algoritmo MD5
            MD5CryptoServiceProvider hashmd5 =
            new MD5CryptoServiceProvider();

            keyArray = hashmd5.ComputeHash(
            UTF8Encoding.UTF8.GetBytes(key));

            hashmd5.Clear();

            TripleDESCryptoServiceProvider tdes =
            new TripleDESCryptoServiceProvider();

            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform =
             tdes.CreateDecryptor();

            byte[] resultArray =
            cTransform.TransformFinalBlock(toDecrypt,
            0, toDecrypt.Length);

            tdes.Clear();
            //se regresa en forma de cadena
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        public static CryptorEngine ce = new CryptorEngine();

        public static string Encrypts(string text)
        {
            return ce.Encrypt(text);
        }
        public static string Decrypts(string encryptText)
        {
            return ce.Decrypt(encryptText);
        }

    }
}