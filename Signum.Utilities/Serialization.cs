using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Signum.Utilities
{
    public static class Serialization
    {
        //Binary
        public static byte[] ToBytes(object graph)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, graph);
                return ms.ToArray();
            }
        }

        public static void ToBinaryFile(object graph, string fileName)
        {
            using (FileStream fs = File.OpenWrite(fileName))
            {
                new BinaryFormatter().Serialize(fs, graph);
            }
        }

        public static object FromBytes(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return new BinaryFormatter().Deserialize(ms);
            }
        }

        public static object FromBinaryFile(string fileName)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                return new BinaryFormatter().Deserialize(fs);
            }
        }
    }
}
