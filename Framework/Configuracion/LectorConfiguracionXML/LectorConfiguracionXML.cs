using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Collections;
using System.Xml.XPath;

namespace Framework.Configuracion
{
    /// <summary>
    /// Clase para leer archivos de configuración XML asociados a un ensamblado y que no se encuentran
    /// en el config de la aplicación principal
    /// </summary>
    public static class LectorConfiguracionXML
    {
        /// <summary>
        /// Devuelve un hashtable de string/string con los valores 
        /// que se encuentran dentro de configuración.
        /// </summary>
        /// <param name="NombreArchivo">El nombre del archivo XML de configuración.</param>
        /// <returns>Un dccionario((string,string) con los valores de configuración en formato clave/valor.</returns>
        public static Dictionary<string, string> LeerConfiguracion(string NombreArchivo)
        {
            //comprobamos que existe el archivo de configuración en la ruta base
            if (!System.IO.File.Exists(NombreArchivo))
            {
                //no se encuentra el archivo en el directorio por defecto, quizá oprque se está
                //ejecutando en un test o algún otro tipo de ejecutable que distorsiona el path
                NombreArchivo = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\" + NombreArchivo;
                if (!System.IO.File.Exists(NombreArchivo))
                {
                    throw new System.IO.FileNotFoundException("No se ha encontrado el archivo de configuración " + NombreArchivo);
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(NombreArchivo);

            //creamos el namespace para ejecutar consultas XPath sobre el documento Datasource
            NameTable ntDS = new NameTable();
            XmlNamespaceManager nsManagerDS = new XmlNamespaceManager(ntDS);
            string NameSpaceDS = doc.DocumentElement.NamespaceURI;
            nsManagerDS.AddNamespace("s", NameSpaceDS);

            XmlNodeList propiedades = doc.SelectNodes(@"//s:propiedad", nsManagerDS);
            Dictionary<string, string> ht = new Dictionary<string, string>();
            foreach (XmlNode nodo in propiedades)
            {
                string key = nodo.Attributes.GetNamedItem("nombre").Value;
                string valor = nodo.Attributes.GetNamedItem("valor").Value;
                ht.Add(key, valor);
            }

            return ht;
        }
    }
}
