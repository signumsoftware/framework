using System.IO;
using System.Security.Cryptography;

namespace Signum.Security;

public delegate byte[] HashPasswordDelegate(string usernameForSalt, string password) ;
public delegate List<byte[]> HashPasswordAlternativesDelegate(string usernameForSalt, string password);

public static class PasswordEncoding
{

    public static HashPasswordDelegate HashPassword = (usernameForSalt, originalPassword) => PBKDF2Hash(originalPassword, usernameForSalt);
    public static HashPasswordAlternativesDelegate HashPasswordAlternatives = (usernameForSalt, originalPassword) => new List<byte[]> 
    {
        MD5Hash(originalPassword) // Backwards compatibility only
    };

    public static int PBKDF2Iterations { get; set; } = 100000;
    public static byte[] PBKDF2Hash(string password, string salt, int iterations = -1)
    {
        if (iterations == -1)
            iterations = PBKDF2Iterations;

        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            Encoding.UTF8.GetBytes(salt),
            iterations,
            HashAlgorithmName.SHA256,
            32 // 256 bits
        );
    }

    //Obsolete, for backwards compatibility only. Do not use for new passwords
    static byte[] MD5Hash(string saltedPassword)
    {
        byte[] originalBytes = Encoding.Default.GetBytes(saltedPassword);
        byte[] encodedBytes = MD5.Create().ComputeHash(originalBytes);
        return encodedBytes;
    }
}

public class CryptorEngine
{
    public static string CalculateMD5Hash(byte[] inputBytes)
    {
        MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(inputBytes);

        return ConvertToHex(hash);
    }

    public static string CalculateMD5Hash(Stream stream)
    {
        MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(stream);

        return ConvertToHex(hash);
    }

    private static string ConvertToHex(byte[] hash)
    {
        // step 2, convert byte array to hex string
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2"));
        }
        return sb.ToString();
    }
}
