using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Resources;
using System.Reflection;

namespace Signum.Utilities
{
    public static class StreamExtensions
    {
        const int BufferSize = 32768;

        public static byte[] ReadAllBytes(this Stream str)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                str.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static void CopyTo(this Stream origin, Stream destiny)
        {
            byte[] buffer = new byte[BufferSize];
            while (true)
            {
                int read = origin.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                destiny.Write(buffer, 0, read);
            }
        }

        public static void WriteAllBytes(this Stream str, byte[] data)
        {
            str.Write(data, 0, data.Length); 
        }

        public static string ReadResourceStream(this Assembly assembly, string name)
        {
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    throw new MissingManifestResourceException("{0} not found on {1}".Formato(name, assembly));

                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();

            }
        }
    }
}
