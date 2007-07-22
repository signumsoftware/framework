using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;

using Framework.DatosNegocio;

using Framework.Mensajeria.GestorMails.Properties;

namespace Framework.Mensajeria.GestorMails.DN
{
    [Serializable]
    public abstract class MensajeDN
    {
        public abstract string Body { get; }
        public abstract string Subject { get; }
        public abstract bool IsHtml { get; }



        public static string MultiReemplazoOrdenado(string orig, Dictionary<string, string> reemplazos)
        {
            StringBuilder sb = new StringBuilder(orig.Length);

            int pos1 = 0, pos2 = 0;

            foreach (KeyValuePair<string, string> kvp in reemplazos)
            {
                string etiqueta = kvp.Key;
                string reemplazo = kvp.Value;
                pos1 = orig.IndexOf(etiqueta, pos2);
                if (pos1 == -1)
                    throw new ApplicationExceptionDN(etiqueta + Resources.EtiquetaNoEncontrada);
                sb.Append(orig.Substring(pos2, pos1 - pos2));
                pos2 = pos1 + etiqueta.Length;
                sb.Append(reemplazo);
            }

            sb.Append(orig.Substring(pos2, orig.Length - pos2));
            return sb.ToString();
        }

        public static string MultiReemplazo(string orig, Dictionary<string, string> reemplazos)
        {
            string result = orig;
            foreach (KeyValuePair<string, string> kvp in reemplazos)
            {
                result = result.Replace(kvp.Key, kvp.Value);
            }
            return result;
        }

        public static string ToXml(MensajeDN mensaje)
        {
            SoapFormatter formatter = new SoapFormatter();
            MemoryStream memoryStream = new MemoryStream();
            formatter.Serialize(memoryStream, mensaje);
            string result = Encoding.ASCII.GetString(memoryStream.GetBuffer());
            return result;
        }

        public static MensajeDN FromXml(string xml)
        {
            SoapFormatter formatter = new SoapFormatter();
            byte[] bytes = Encoding.ASCII.GetBytes(xml);
            MemoryStream memoryStream = new MemoryStream(bytes);
            object o = formatter.Deserialize(memoryStream);
            MensajeDN result = o as MensajeDN;
            return result;
        }
    }
}
