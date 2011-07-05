using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Signum.Utilities
{
    public static class Serialization
    {
        public static byte[] ToBytes(object graph)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, graph);
                return ms.ToArray();
            }
        }

        public static void ToFile(object graph, string fileName)
        {
            using (FileStream fs = File.OpenWrite(fileName))
            {
                new BinaryFormatter().Serialize(fs, graph);
            }
        }

        public static object FromBytes(byte[] array)
        {
            using (MemoryStream ms = new MemoryStream(array))
            {
                return new BinaryFormatter().Deserialize(ms);
            }
        }

        public static object FromFile(string fileName)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                return new BinaryFormatter().Deserialize(fs);
            }
        }
    }
}
