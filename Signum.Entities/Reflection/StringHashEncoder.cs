using System.Text;

namespace Signum.Entities.Reflection
{
    public static class StringHashEncoder
    {
        public const int HashSize = 7;
        public static string ChopHash(string str, int maxLength)
        {
            if (str.Length > maxLength)
                return str.Substring(0, maxLength - HashSize) + Codify(str.Substring(maxLength - HashSize));

            return str;
        }

        static readonly string letters = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        public static string Codify(string str)
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

        public static int GetHashCode32(string value)
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

}
