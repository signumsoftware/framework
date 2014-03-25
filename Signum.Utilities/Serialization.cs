using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;

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



        //SOAP
        public static string ToString(object graph)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new SoapFormatter().Serialize(ms, graph);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static void ToStringFile(object graph, string fileName)
        {
            using (FileStream fs = File.OpenWrite(fileName))
            {
                new SoapFormatter().Serialize(fs, graph);
            }
        }

        public static object FromString(string str)
        {
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                return new SoapFormatter().Deserialize(ms);
            }
        }

        public static object FromStringFile(string fileName)
        {
            using (FileStream fs = File.OpenRead(fileName))
            {
                return new SoapFormatter().Deserialize(fs);
            }
        }
    }
}
