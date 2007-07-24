using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.IO;
using System.Collections;
using SerializadorTexto.Atributos;
using SerializadorTexto.Atributos.Incontextual;

namespace SerializadorTexto
{
    public static class Serializador
    {
        public static T AbrirArchivo<T>(string file)
        {
            ArchivoTextoAttribute ata = Reflector.DameAtributoUnico<ArchivoTextoAttribute>(typeof(T));

            if (ata == null)
                throw new ApplicationException(typeof(T).Name + " no tiene el atributo ArchivoTexto o ArchivoTextoIncontextual");

            using (FileStream fs = File.OpenRead(file))
            {
                using (StreamReader sr = new StreamReader(fs, ata.Encoding))
                {
                    return Serializador.DeserializarArchivo<T>(sr, ata);
                }
            }
        }


        public static T AbrirBytes<T>(byte[] datos)
        {
            ArchivoTextoAttribute ata = Reflector.DameAtributoUnico<ArchivoTextoAttribute>(typeof(T));

            if (ata == null)
                throw new ApplicationException(typeof(T).Name + " no tiene el atributo ArchivoTexto o ArchivoTextoIncontextual");

            using (MemoryStream ms = new MemoryStream(datos))
            {
                using (StreamReader sr = new StreamReader(ms, ata.Encoding))
                {
                    return Serializador.DeserializarArchivo<T>(sr, ata);
                }
            }
        }


        private static T DeserializarArchivo<T>(StreamReader s, ArchivoTextoAttribute ata)
        {
            if (ata is ArchivoTextoIncontextualAttribute)
                return (T)SerializadorIncontextual.DeserializarArchivo<T>(s);
            else
                return (T)SerializadorRegular.DeserializarArchivo(s, typeof(T));
        }

        public static T DeserializarLinea<T>(string s)
        {
            return (T)SerializadorLineas.DeserializarLinea(s, typeof(T), new ArchivoTextoAttribute(s.Length));
        }





        public static void GuardarArchivo<T>(T t, string file)
        {
            ArchivoTextoAttribute ata = Reflector.DameAtributoUnico<ArchivoTextoAttribute>(typeof(T));

            if (ata == null)
                throw new ApplicationException(typeof(T).Name + " no tiene el atributo ArchivoTexto o ArchivoTextoIncontextual");

            using (FileStream fs = File.Create(file))
            {
                using (StreamWriter sw = new StreamWriter(fs, ata.Encoding))
                {
                    Serializador.SerializarArchivo<T>(t, sw, ata);
                }
            }
        }

        public static byte[] GuardarBytes<T>(T t)
        {
            ArchivoTextoAttribute ata = Reflector.DameAtributoUnico<ArchivoTextoAttribute>(typeof(T));

            if (ata == null)
                throw new ApplicationException(typeof(T).Name + " no tiene el atributo ArchivoTexto o ArchivoTextoIncontextual");

            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms, ata.Encoding))
                {
                    Serializador.SerializarArchivo<T>(t, sw, ata);
                }
                return ms.ToArray();
            }
        }

        private static void SerializarArchivo<T>(T o, StreamWriter sw, ArchivoTextoAttribute ata)
        {
            if (ata is ArchivoTextoIncontextualAttribute)
                SerializadorIncontextual.SerializarArchivo(o, typeof(T), sw);
            else
                SerializadorRegular.SerializarArchivo(o, typeof(T), sw);
        }

        public static string SerializarLinea<T>(T t)
        {
            return SerializadorLineas.SerializarLinea(t, typeof(T), new ArchivoTextoAttribute(0));
        }

        public static string SerializarLinea(object o)
        {
            return SerializadorLineas.SerializarLinea(o, o.GetType(), new ArchivoTextoAttribute(0));
        }

        public static string SerializarLinea(object o, Type type)
        {
            return SerializadorLineas.SerializarLinea(o, type, new ArchivoTextoAttribute(0));
        }

    }

}
