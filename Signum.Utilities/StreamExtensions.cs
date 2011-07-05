using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
    }
}
