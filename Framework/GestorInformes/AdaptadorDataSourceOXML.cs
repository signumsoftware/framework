using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Xml;

namespace Framework.GestorInformes
{
    public static class AdaptadorDataSourceOXML
    {

        /// <summary>
        /// Devuelve un Documento XML DataSource correctamente formado 
        /// con el elemento root (mapa) creado
        /// </summary>
        /// <returns>El documento XML DataSource correctamente formado con el nodo prinicpal o root (mapa)</returns>
        public static XmlDocument GenerarDocumentoDataSource()
        {
            return GenerarDocumentoBase();
        }

        /// <summary>
        /// Genera un esquema del documento XML DataSource mostrando todos los elementos que
        /// éste tendrá, pero sin insertar su contenido
        /// </summary>
        /// <param name="pDataset">El Dataset que contiene las tablas y los datarelations cuyo contenido se quiere
        /// reflejar en el esquema</param>
        /// <param name="TablasPrincipales">Una lista con los nombres de la/s tabla/s que deben mostrarse 
        /// como elementos que dependen directamente del root (aquellas que no son tablas dependientes). 
        /// Si se pasa como null, todas las tablas lo harán.</param>
        /// <returns>El documento XML DataSource con el esquema XML de los datos correctamente cargado</returns>
        public static XmlDocument GenerarEsquemaXML(DataSet pDataset, List<string> TablasPrincipales)
        {
            XmlDocument doc = GenerarDocumentoBase();
            return GenerarEsquemaXML(pDataset, TablasPrincipales, doc);
        }

        /// <summary>
        /// Genera un esquema del documento XML DataSource mostrando todos los elementos que
        /// éste tendrá, pero sin insertar su contenido
        /// </summary>
        /// <param name="pDataset">El Dataset que contiene las tablas y los datarelations cuyo contenido se quiere
        /// reflejar en el esquema</param>
        /// <param name="TablasPrincipales">Una lista con los nombres de la/s tabla/s que deben mostrarse 
        /// como elementos que dependen directamente del root (aquellas que no son tablas dependientes). 
        /// Si se pasa como null, todas las tablas lo harán.</param>
        /// <param name="pDocumentoDataSource">El documento XML DataSource en el que se quiere cargar el esquema XML</param>
        /// <returns>El documento XML DataSource con el esquema XML de los datos correctamente cargado</returns>
        public static XmlDocument GenerarEsquemaXML(DataSet pDataset, List<string> TablasPrincipales, XmlDocument pDocumentoDataSource)
        {
            //prepara la lista con las tablas principales (las que deben colgar del nodo root)
            TablasPrincipales = EstablecerTablasPrincipales(pDataset, TablasPrincipales);

            //creamos el namespace para ejecutar consultas XPath sobre el documento Datasource
            XmlNode nodoRoot = ObtenerNodoRoot(pDocumentoDataSource);

            //obtenemos la agrupación de DataRelations
            Dictionary<string, List<System.Data.DataRelation>> relacionesAgrupadas = AgruparDataRelations(pDataset);

            //generamos el esquema para cada una de las tablas principales
            foreach (string tabla in TablasPrincipales)
            {
                XmlNode nodoTabla = pDocumentoDataSource.CreateElement("Col" + tabla);
                XmlNode nodoIteracion = pDocumentoDataSource.CreateElement("items");

                nodoIteracion.AppendChild(GenerarNodoTablaEsquemaXML(pDataset.Tables[tabla], tabla, pDataset, pDocumentoDataSource, relacionesAgrupadas));

                nodoTabla.AppendChild(nodoIteracion);
                nodoRoot.AppendChild(nodoTabla);
            }

            EliminarNamespacesVacios(pDocumentoDataSource);

            return pDocumentoDataSource;
        }

        private static XmlNode GenerarNodoTablaEsquemaXML(DataTable tabla, string pElementoActual, DataSet pDataSet, XmlDocument pDocumentoDataSource, Dictionary<string, List<DataRelation>> pRelacionesAgrupadas)
        {
            XmlNode nodoFila = pDocumentoDataSource.CreateElement(pElementoActual);

            foreach (DataColumn columna in tabla.Columns)
            {
                XmlNode nodoPropiedad = pDocumentoDataSource.CreateElement(columna.ColumnName);
                nodoFila.AppendChild(nodoPropiedad);
            }

            if (pDataSet.Relations.Count != 0)
            {
                List<DataRelation> relaciones = null;
                if (pRelacionesAgrupadas.TryGetValue(tabla.TableName, out relaciones))
                {
                    foreach (DataRelation relacion in relaciones)
                    {
                        XmlNode nodoElemento = pDocumentoDataSource.CreateElement("Col" + relacion.RelationName);
                        XmlNode nodoIteracion = pDocumentoDataSource.CreateElement("items");
                        nodoIteracion.AppendChild(GenerarNodoTablaEsquemaXML(relacion.ChildTable, relacion.RelationName, pDataSet, pDocumentoDataSource, pRelacionesAgrupadas));
                        nodoElemento.AppendChild(nodoIteracion);
                        nodoFila.AppendChild(nodoElemento);
                    }
                }
            }

            return nodoFila;
        }

        /// <summary>
        /// Genera el contenido XML a partir de un dataset y lo inserta en el nodo root (mapa) del 
        /// documento DataSource XML
        /// </summary>
        /// <param name="pDataSet">El Dataset que contiene las tablas y los datarelations cuyo contenido se quiere
        /// insertar en el documento DataSource</param>
        /// <param name="TablasPrincipales">Una lista con los nombres de la/s tabla/s que deben mostrarse 
        /// como elementos que dependen directamente del root (aquellas que no son tablas dependientes). 
        /// Si se pasa como null, todas las tablas lo harán.</param>
        /// <returns>El documento XML DataSource correctamente cargado</returns>
        public static XmlDocument GenerarContenidoDataSourceXML(DataSet pDataSet, List<string> TablasPrincipales)
        {
            //crea automáticamente el documento datasource
            XmlDocument doc = GenerarDocumentoBase();

            return GenerarContenidoDataSourceXML(pDataSet, TablasPrincipales, doc);
        }

        /// <summary>
        /// Genera el contenido XML a partir de un dataset y lo inserta en el nodo root (mapa) del 
        /// documento DataSource XML
        /// </summary>
        /// <param name="pDataSet">El Dataset que contiene las tablas y los datarelations cuyo contenido se quiere
        /// insertar en el documento DataSource</param>
        /// <param name="TablasPrincipales">Una lista con los nombres de la/s tabla/s que deben mostrarse 
        /// como elementos que dependen directamente del root (aquellas que no son tablas dependientes). 
        /// Si se pasa como null, todas las tablas lo harán.</param>
        /// <param name="pDocumentoDataSource">El documento XML DataSource en el que se quiere insertar el 
        /// contenido generado</param>
        /// <returns>El documento XML DataSource correctamente cargado</returns>
        public static XmlDocument GenerarContenidoDataSourceXML(DataSet pDataSet, List<string> TablasPrincipales, XmlDocument pDocumentoDataSource)
        {
            //prepara la lista con las tablas principales (las que deben colgar del nodo root)
            TablasPrincipales = EstablecerTablasPrincipales(pDataSet, TablasPrincipales);

            //creamos el namespace para ejecutar consultas XPath sobre el documento Datasource
            XmlNode nodoRoot = ObtenerNodoRoot(pDocumentoDataSource);

            //obtenemos la agrupación de DataRelations
            Dictionary<string, List<System.Data.DataRelation>> relacionesAgrupadas = AgruparDataRelations(pDataSet);

            //generamos el contenido para cada una de las tablas marcadas como principales
            foreach (string tablaPrincipal in TablasPrincipales)
            {
                XmlNode nodoTabla;
                //bool esColeccion = (pDataSet.Tables[tablaPrincipal].Rows.Count > 1);

                //if (esColeccion)
                //{
                nodoTabla = pDocumentoDataSource.CreateElement("Col" + tablaPrincipal);
                XmlNode nodoIteracion = pDocumentoDataSource.CreateElement("items");
                foreach (System.Data.DataRow row in pDataSet.Tables[tablaPrincipal].Rows)
                {
                    nodoIteracion.AppendChild(GenerarNodoFila(pDataSet, pDocumentoDataSource, tablaPrincipal, row, relacionesAgrupadas));
                }
                nodoTabla.AppendChild(nodoIteracion);
                //}
                //else
                //{
                //    nodoTabla = pDocumentoDataSource.CreateElement(tablaPrincipal);
                //    foreach (System.Data.DataRow row in pDataSet.Tables[tablaPrincipal].Rows)
                //    {
                //        nodoTabla.AppendChild(GenerarNodoFila(pDataSet, pDocumentoDataSource, tablaPrincipal, row, relacionesAgrupadas));
                //    }
                //}

                //agregamos al mapa el nodo generado
                nodoRoot.AppendChild(nodoTabla);
            }

            ////el root el 1º
            //pDocumentoDataSource.AppendChild(nodoRoot);

            //eliminamos los elementos que contienen un Uri vacío, ya que sólo debe existir el del root
            EliminarNamespacesVacios(pDocumentoDataSource);

            return pDocumentoDataSource;
        }


        /// <summary>
        /// Elimina todos los namespace vacíos (xmnls='') que haya en el documento, ya que
        /// el único válido es el del nodo root, que contiene el URI correcto del documentoDatasource
        /// </summary>
        /// <param name="pDocumentoDataSource">El documento XML DataSource en el que se quieren eliminar los
        /// namespaces vacíos.</param>
        private static void EliminarNamespacesVacios(XmlDocument pDocumentoDataSource)
        {
            pDocumentoDataSource.InnerXml = pDocumentoDataSource.InnerXml.Replace(" xmlns=\"\"", string.Empty);
        }


        /// <summary>
        /// Devuelve un diccionario (clave-valor) de listas de datarelations agrupando estos en función
        /// de la ParentTable a la que asocian
        /// </summary>
        /// <param name="pDataSet">El dataset que contiene las tablas y los DataRelations</param>
        /// <returns>Un dictionary de string/list of (datarelation)</returns>
        private static Dictionary<string, List<System.Data.DataRelation>> AgruparDataRelations(DataSet pDataSet)
        {
            Dictionary<string, List<System.Data.DataRelation>> relacionesAgrupadas = new Dictionary<string, List<System.Data.DataRelation>>();

            if (pDataSet.Relations.Count != 0)
            {
                foreach (System.Data.DataRelation relacion in pDataSet.Relations)
                {
                    //buscamos una entrada por el nombre del parenttable
                    //y añadimos las entradas correspondientes
                    string nombretabla = relacion.ParentTable.TableName;

                    List<DataRelation> especificacion = null;

                    if (relacionesAgrupadas.ContainsKey(nombretabla))
                    {
                        especificacion = relacionesAgrupadas[nombretabla];
                    }
                    else
                    {
                        especificacion = new List<System.Data.DataRelation>();
                        relacionesAgrupadas.Add(nombretabla, especificacion);
                    }
                    //ahora agregamos el nombre de la relación/propiedad junto con la relación
                    especificacion.Add(relacion);
                }
            }
            return relacionesAgrupadas;
        }


        /// <summary>
        /// Devuelve el nodo root o principal (mapa) del documento XML DataSource
        /// </summary>
        /// <param name="pDocumentoDataSource">El documento XML DataSource correctamente formado
        /// cuyo nodo se quiere obtener</param>
        /// <returns></returns>
        private static XmlNode ObtenerNodoRoot(XmlDocument pDocumentoDataSource)
        {
            NameTable ntDS = new NameTable();
            XmlNamespaceManager nsManagerDS = new XmlNamespaceManager(ntDS);
            string NameSpaceDS = pDocumentoDataSource.DocumentElement.NamespaceURI;
            nsManagerDS.AddNamespace("m", NameSpaceDS);

            XmlNode nodoRoot = pDocumentoDataSource.SelectSingleNode("//m:mapa", nsManagerDS);
            return nodoRoot;
        }


        private static List<string> EstablecerTablasPrincipales(DataSet pDataSet, List<string> TablasPrincipales)
        {
            //si no han pasado un list con las tablas principales, todas las
            //tablas del dataset lo son
            if (TablasPrincipales == null || TablasPrincipales.Count == 0)
            {
                TablasPrincipales = new List<string>();
                foreach (System.Data.DataTable tabla in pDataSet.Tables)
                {
                    TablasPrincipales.Add(tabla.TableName);
                }
            }
            return TablasPrincipales;
        }


        /// <summary>
        /// Genera recursivamente el nodo del documento DataSource XML para un datarow determinado, incluyendo
        /// los nodos de aquellas filas que dependan de él
        /// </summary>
        /// <param name="pDataSet">El DataSet al que pertenece el datarow</param>
        /// <param name="doc">El documento XML DataSource en el que se insertará el nodo generado</param>
        /// <param name="ElementoActual">El nombre que recibirá el elemento o nodo generado</param>
        /// <param name="row">El datarow cuyo contenido se quiere generar</param>
        /// <param name="pRelacionsAgrupadas">Par que contiene en formato clave/valor los DataRelation que contiene el Dataset</param>
        /// <returns>El nodo XML correctamente formado con el contenido del datarow en forma de elementos (subnodos)</returns>
        private static XmlNode GenerarNodoFila(DataSet pDataSet, XmlDocument doc, string ElementoActual, System.Data.DataRow row, Dictionary<string, List<System.Data.DataRelation>> pRelacionsAgrupadas)
        {
            XmlNode nodoFila = doc.CreateElement(ElementoActual);

            //generamos un nodo por cada una de las columnas que haya en la fila
            foreach (DataColumn col in row.Table.Columns)
            {
                XmlNode nodoPropiedad = doc.CreateElement(col.ColumnName);
                nodoPropiedad.InnerText = row[col.ColumnName].ToString();
                nodoFila.AppendChild(nodoPropiedad);
            }

            //comprobamos si hay filas dependientes de esta
            if (pDataSet.Relations.Count != 0)
            {
                List<DataRelation> relacionesTablaActual = null;
                if (pRelacionsAgrupadas.TryGetValue(row.Table.TableName, out relacionesTablaActual))
                {
                    foreach (DataRelation relacion in relacionesTablaActual)
                    {
                        //creamos el nodo con el nombre que se ha especificado para la propiedad (relación)

                        XmlNode nodoColElementos = doc.CreateElement("Col" + relacion.RelationName);
                        XmlNode nodoItems = doc.CreateElement("items");
                        DataRow[] childrows = row.GetChildRows(relacion);
                        foreach (DataRow childrow in childrows)
                        {
                            nodoItems.AppendChild(GenerarNodoFila(pDataSet, doc, relacion.RelationName, childrow, pRelacionsAgrupadas));
                        }
                        nodoColElementos.AppendChild(nodoItems);
                        //agregamos el nodo con los elementos relacionados como una propiedad
                        nodoFila.AppendChild(nodoColElementos);
                    }
                }
            }

            return nodoFila;
        }


        public static XmlDocument GenerarContenidoDataSourceXML(string pRutaEnsamblado)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Genera el Documento XML Data Source Base para poder trabajar con una plantilla de documento
        /// </summary>
        /// <returns>El XML Document correctamente formado y con el elemento principal o root (mapa)</returns>
        private static XmlDocument GenerarDocumentoBase()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            doc.AppendChild(decl);

            //nodo root
            XmlNode nodo;

            //nodo root
            nodo = doc.CreateElement("mapa", "http://signumsoftware.com/2007/pruebas");

            //el root el 1º
            doc.AppendChild(nodo);

            //eliminamos los elementos que contienen un Uri vacío, ya que sólo debe existir el del root
            doc.InnerXml = doc.InnerXml.Replace(" xmlns=\"\"", string.Empty);

            return doc;
        }

    }

}


