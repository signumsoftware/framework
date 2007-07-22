using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.IO.Packaging;
using System.Xml.XPath;


namespace Framework.GestorInformes
{
    public class GestorWordOpenXML
    {

        #region Asociar y Combinar Content Controls

        #region Ejemplo de la definición XML de un ContentControl
        //ejemplo de la definición XML de un ContentControl:
        //<w:sdt>
        //  <w:sdtPr>
        //    <w:dataBinding w:prefixMappings="xmlns:ns0='http://signumsoftware.com/2007/pruebas'" w:xpath="/ns0:mapa[1]/ns0:usuario[1]/ns0:nombre[1]" w:storeItemID="{E4F644BB-68FA-484E-A24F-DBEE8774140B}" />
        //    <w:alias w:val="campo" />
        //    <w:tag w:val="@usuario.nombre" />
        //    <w:id w:val="4875501" />
        //    <w:lock w:val="contentLocked" />
        //    <w:placeholder>
        //      <w:docPart w:val="32865818745147CB9EA72204778AB2F8" />
        //    </w:placeholder>
        //    <w:showingPlcHdr />
        //    <w:text />
        //  </w:sdtPr>
        //  <w:sdtContent>
        //    <w:r>
        //      <w:t>nombre</w:t>
        //    </w:r>
        //  </w:sdtContent>
        //</w:sdt>        

        //seleccionamos todos los contentcontrols que hay en el documento
        //XmlNodeList xnlContentControls = doc.SelectNodes("//w:sdtPr", nsManager);
        #endregion


        /// <summary>
        /// Dado un documento XML, lo asocia y combina con un documento XML Data source (customPart)
        /// </summary>
        /// <param name="doc">El documento XML que se quiere asociar y combinar</param>
        /// <param name="docDataSource">El documento XML que contiene los datos a combinar 
        /// [Debe estar ya relleno con los datos]</param>
        /// <returns>El número de Asociaciones Incorrectas que se han encontrado en el documento</returns>
        public int AsociarYCombinarDocumento(ref XmlDocument doc, ref XmlDocument docDataSource)
        {
            //generamos las iteraciones del documento
            List<IteracionOXML> listaIteraciones = GenerarIteraciones(ref doc, ref docDataSource);

            //asociamos y combinamos el contenido del documento con el del docDataSource
            //creamos el namespace para ejecutar consultas XPath sobre el documento original
            NameTable nt = new NameTable();
            XmlNamespaceManager nsManager = new XmlNamespaceManager(nt);
            string wordNameSpace = doc.DocumentElement.NamespaceURI;
            nsManager.AddNamespace("w", wordNameSpace);

            //creamos el namespace para ejecutar consultas XPath sobre el documento Datasource
            NameTable ntDS = new NameTable();
            XmlNamespaceManager nsManagerDS = new XmlNamespaceManager(ntDS);
            string NameSpaceDS = docDataSource.DocumentElement.NamespaceURI;
            nsManagerDS.AddNamespace("m", NameSpaceDS);

            //seleccionamos el nodo del body
            XmlNode nodoACombinar = doc.SelectSingleNode("//w:body", nsManager);

            int numAsociacionesIncorrectas = 0;

            //llamamos al método que asocia y combina todo el documento recursivamente
            AsociadorDocumentos asociador = new AsociadorDocumentos();
            asociador.AsociarYCombinar(nodoACombinar, ref doc, ref docDataSource, ref nsManager, ref nsManagerDS, listaIteraciones, null, numAsociacionesIncorrectas);

            return numAsociacionesIncorrectas;
        }



        /// <summary>
        /// Dado un documento xml que contiene el Main (document.xml), y a partir de un documento
        /// que contiene la estructura de datos origen o custompart (item1.xml), se generan 
        /// las iteraciones asociadas a su elemento correspondiente.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="docDataSource"></param>
        private List<IteracionOXML> GenerarIteraciones(ref XmlDocument doc, ref XmlDocument docDataSource)
        {
            //la lista en la que se van a guardar todas las iteraciones que se van a generar
            List<IteracionOXML> listaIteraciones = new List<IteracionOXML>();

            NameTable nt = new NameTable();
            XmlNamespaceManager nsManager = new XmlNamespaceManager(nt);
            string wordNameSpace = doc.DocumentElement.NamespaceURI;
            nsManager.AddNamespace("w", wordNameSpace);

            //comprobamos si hay identificadores de iteración en alguno de los nodos del documento
            XmlNodeList nodosDefinicionIteracion = (doc.SelectNodes("//w:tag[starts-with(@w:val,'#')]", nsManager));
            bool buscarSiguienteIteracion = (nodosDefinicionIteracion.Count != 0);

            while (buscarSiguienteIteracion)
            {
                foreach (XmlNode nodoDefinicionIteracion in nodosDefinicionIteracion)
                {
                    //tenemos el nodo del control que indica el comienzo de la iteración
                    XmlNode nodoControladorIteracion = nodoDefinicionIteracion.ParentNode;

                    //llamamos al método que genera la iteración recursivamente
                    //y sustituye los nodos de definición por los nodos vinculados
                    listaIteraciones.Add(GenerarFragmentoIteracionDesdeContentControl(nodoControladorIteracion, nsManager, doc));

                    //sólo cogemos el 1º, ya que es posible que los demás estuviesen
                    //como subiteraciones en la inicial
                    break;
                }
                //volvemos a buscar a ver si hay más iteraciones en todo el documento
                nodosDefinicionIteracion = (doc.SelectNodes("//w:tag[starts-with(@w:val,'#')]", nsManager));
                buscarSiguienteIteracion = (nodosDefinicionIteracion.Count != 0);
            }

            //ahora generamos las subiteraciones que fuesen necesarias
            if (listaIteraciones != null && listaIteraciones.Count != 0)
            {
                foreach (IteracionOXML iteracion in listaIteraciones)
                {
                    //invocamos el método de generación de subiteraciones para las que están en el primer nivel
                    GenerarSubiteraciones(iteracion, ref nsManager, ref doc);
                }
            }

            //convertimos los nodos de referencia a IteracionOXML en nodos compatibles con el 
            //espacio de nombres del documento original
            doc.InnerXml = doc.InnerXml.Replace(@"<IteracionOXML", @"<w:IteracionOXML");
            doc.InnerXml = doc.InnerXml.Replace("GUID=", "w:GUID=");
            doc.InnerXml = doc.InnerXml.Replace("ElementoAsociado=", "w:ElementoAsociado=");

            return listaIteraciones;
        }

        /// <summary>
        /// Genera recursivamente las subiteraciones que haya dentro de una iteracionOXML, agregando el nodo
        /// de referencia a éstas en su fragmentoXML
        /// </summary>
        /// <param name="iteracion">La iteración en la que se quieren buscar/generar las subiteraciones</param>
        /// <param name="nsManager">El namespaceManager del documento original</param>
        /// <param name="doc">El documento original al que pertenece la iteración</param>
        private void GenerarSubiteraciones(IteracionOXML iteracion, ref XmlNamespaceManager nsManager, ref XmlDocument doc)
        {
            XmlNodeList nodosDefinicionIteracion = iteracion.FragmentoXML.SelectNodes("//w:tag[starts-with(@w:val,'#')]", nsManager);
            bool buscarSiguienteIteracion = (nodosDefinicionIteracion.Count != 0);

            while (buscarSiguienteIteracion)
            {
                foreach (XmlNode nodoDefinicionIteracion in nodosDefinicionIteracion)
                {
                    //tenemos el nodo del control que indica el comienzo de la iteración
                    XmlNode nodoControladorIteracion = nodoDefinicionIteracion.ParentNode;

                    //llamamos al método que genera la iteración recursivamente
                    //y sustituye los nodos de definición por los nodos vinculados
                    iteracion.Iteraciones.Add(GenerarFragmentoIteracionDesdeContentControl(nodoControladorIteracion, nsManager, doc));

                    //sólo cogemos el 1º, ya que es posible que los demás estuviesen
                    //como subiteraciones en la inicial
                    break;
                }
                //volvemos a buscar a ver si hay más iteraciones en todo el documento
                nodosDefinicionIteracion = (iteracion.FragmentoXML.SelectNodes("//w:tag[starts-with(@w:val,'#')]", nsManager));
                buscarSiguienteIteracion = (nodosDefinicionIteracion.Count != 0);
            }

            //convertimos los nodos de referencia a IteracionOXML en nodos compatibles con el 
            //espacio de nombres del documento original
            iteracion.FragmentoXML.InnerXml = iteracion.FragmentoXML.InnerXml.Replace(@"<IteracionOXML", @"<w:IteracionOXML");
            iteracion.FragmentoXML.InnerXml = iteracion.FragmentoXML.InnerXml.Replace("GUID=", "w:GUID=");
            iteracion.FragmentoXML.InnerXml = iteracion.FragmentoXML.InnerXml.Replace("ElementoAsociado=", "w:ElementoAsociado=");

            //ahora invocamos recursivamente este mismo método para generar las subiteraciones por cada una de las
            //iteraciones que contiene la actual
            foreach (IteracionOXML subiteracion in iteracion.Iteraciones)
            {
                GenerarSubiteraciones(subiteracion, ref nsManager, ref doc);
            }
        }

        /// <summary>
        /// Dado un nodo que es un contentcontrol de inicio iteración, 
        /// genera el fragmento iteracion correspondiente al mismo y lo incluye
        /// en el documento origen en sustitución de los nodos que lo definían
        /// </summary>
        /// <param name="nodoInicioIteracion">El nodo que define el comienzo de la iteración</param>
        /// <param name="nsManager">El NameSpacemanager necesario para ejecutar consultas xpath sobre el nodo</param>
        /// <param name="documentoOrigen">El documento en el que se va a sustituir la iteración</param>
        private IteracionOXML GenerarFragmentoIteracionDesdeContentControl(XmlNode nodoInicioIteracion, XmlNamespaceManager nsManager, XmlDocument documentoOrigen)
        {
            //definimos la asociación con la que estamos trabajando en este momento
            string asociacionActual = nodoInicioIteracion.SelectSingleNode(".//w:tag", nsManager).Attributes[0].Value;

            //creamos el objeto iteración en el que vamos  a guardar la iteración
            IteracionOXML miIteracion = new IteracionOXML(documentoOrigen, nsManager, asociacionActual);

            //subimos un nodo para obtener el w:sdt
            XmlNode nodoSdtControladorIteracion = nodoInicioIteracion.ParentNode;

            //a partir de aquí, recorremos los nodos siguientes hasta que lleguemos al fin de
            //la iteración (si no hay un nodo que determine el fin de la iteración, es un xml
            //incorrecto)
            XmlNode nodoActual = nodoSdtControladorIteracion.NextSibling;
            //si el siguiente está vacío, debemos subir hasta que haya un nodo en el que haya un siguiente
            if (nodoActual == null)
            {
                XmlNode npadre = nodoSdtControladorIteracion.ParentNode;
                nodoActual = npadre.NextSibling;
                while (nodoActual == null)
                {
                    npadre = npadre.ParentNode;
                    nodoActual = npadre.NextSibling;
                }
            }


            bool continuar = true;

            while (continuar)
            {
                XmlNode nodoSiguiente = null;
                switch (ExaminarNodoIteracion(nodoActual, nsManager, asociacionActual))
                {
                    case OperarEnIteracion.Agregar:
                    case OperarEnIteracion.GenerarSubIteracion:
                        //agregamos el nodo copiado al Fragmento de Iteración
                        XmlNode nodoClon = nodoActual.CloneNode(true);
                        if (miIteracion.FragmentoXML.HasChildNodes)
                        {
                            miIteracion.FragmentoXML.InsertAfter(nodoClon, miIteracion.FragmentoXML.LastChild);
                        }
                        else
                        {
                            miIteracion.FragmentoXML.AppendChild(nodoClon);
                        }
                        //eliminamos el nodo original del documento origen
                        nodoSiguiente = nodoActual.NextSibling;
                        nodoActual.ParentNode.RemoveChild(nodoActual);
                        break;

                    case OperarEnIteracion.Terminar:
                        //apuntamos que salimos
                        continuar = false;
                        //eliminamos el nodo de definición del fin de iteración
                        nodoSiguiente = null; // nodoActual.NextSibling;
                        nodoActual.ParentNode.RemoveChild(nodoActual);
                        //documentoOrigen.RemoveChild(nodoActual);
                        break;
                }

                //establecemos el último nodo que se ha procesado
                nodoActual = nodoSiguiente;
                //nodoActual = nodoActual.NextSibling;
            }

            //sustituimos en el contenedor de la iteración (el documento 
            //o el fragmento xml correspondiente) el nodo que definía 
            //el comienzo de la iteración (sdt) por el nodo que referencia a la iteraciónOXML
            nodoSdtControladorIteracion.ParentNode.ReplaceChild(miIteracion.GenerarNodoReferencia(), nodoSdtControladorIteracion);
            //documentoOrigen.ReplaceChild(miIteracion.GenerarNodoReferencia(), nodoSdtControladorIteracion);

            //devolvemos la iteraciónOXML
            return miIteracion;
        }


        /// <summary>
        /// Determina qué hay que hacer con un nodo determinado cuando
        /// se está generando un fragmento de iteración
        /// </summary>
        /// <param name="nodo">El nodo que se quiere examinar</param>
        /// <param name="xmlManager">El NameSpaceManager necesario para ejecutar xpaths sobre el nodo</param>
        /// <returns>Lo que hay que hacer respecto al fragmento de iteración (agregarlo a la iteración en curso, 
        /// terminar la operación en curso o iniciar una subiteración dentro de la que está en curso)</returns>
        private OperarEnIteracion ExaminarNodoIteracion(XmlNode nodo, XmlNamespaceManager xmlManager, string asociacionActual)
        {
            XmlNode tag = nodo.SelectSingleNode(".//w:tag", xmlManager);
            if (tag != null && tag.Attributes.Count > 0)
            {
                if (tag.Attributes[0].Value == "/" + asociacionActual)
                {
                    return OperarEnIteracion.Terminar;
                }
                else
                {
                    if (tag.Attributes[0].Value.StartsWith("#"))
                    {
                        return OperarEnIteracion.GenerarSubIteracion;
                    }
                }
            }
            return OperarEnIteracion.Agregar;
        }



        private enum OperarEnIteracion
        {
            Agregar,
            Terminar,
            GenerarSubIteracion
        };

        //Asociar y Combinar ContentControls
        #endregion


        #region Obtener y Salvar Partes de Documentos

        /// <summary>
        /// Devuelve el contenido XML del main (document.xml)
        /// </summary>
        /// <param name="fileName">El path completo del archivo .docx (c:\...\archivo.docx</param>
        /// <returns>Un XmlDocument con el contenido del archivo main</returns>
        public XmlDocument ObtenerMainDocument(string fileName)
        {
            // Given a file name, retrieve the officeDocument part.

            XmlDocument doc = null;

            const string documentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";


            //  Open the package with read/write access.
            using (Package myPackage = Package.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {

                //  Get the main document part (workbook.xml, document.xml, presentation.xml).
                foreach (System.IO.Packaging.PackageRelationship relationship in myPackage.GetRelationshipsByType(documentRelationshipType))
                {
                    //  There should only be one document part in the package. 
                    Uri documentUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), relationship.TargetUri);
                    PackagePart documentPart = myPackage.GetPart(documentUri);

                    doc = new XmlDocument();
                    doc.Load(documentPart.GetStream());

                    ////  Only one document part, so get out now.
                    break;
                }
            }
            return doc;
        }

        /// <summary>
        /// Guarda el contenido de un documento XML en un archivo main (sustituyendo lo que
        /// antes hubiera en él)
        /// </summary>
        /// <param name="fileName">El path completo del archivo .docx</param>
        /// <param name="contenidoXML">El XmlDocument que contiene el XML que se va a guardar</param>
        /// <returns></returns>
        public XmlDocument GuardarMainDocument(string fileName, ref XmlDocument contenidoXML)
        {
            // Given a file name, retrieve the officeDocument part.

            XmlDocument doc = null;

            const string documentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";


            //  Open the package with read/write access.
            using (Package myPackage = Package.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {

                //  Get the main document part (workbook.xml, document.xml, presentation.xml).
                foreach (System.IO.Packaging.PackageRelationship relationship in myPackage.GetRelationshipsByType(documentRelationshipType))
                {
                    //  There should only be one document part in the package. 
                    Uri documentUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), relationship.TargetUri);
                    PackagePart documentPart = myPackage.GetPart(documentUri);

                    doc = new XmlDocument();
                    doc.Load(documentPart.GetStream());

                    doc.InnerXml = contenidoXML.InnerXml;

                    //  Save the modified document back into its part. Not necessary 
                    //  unless you make changes to the document.
                    doc.Save(documentPart.GetStream(FileMode.Create, FileAccess.Write));

                    ////  Only one document part, so get out now.
                    break;
                }
            }
            return doc;
        }


        /// <summary>
        /// Modifica un customPart, cambiando el contenido que posea en ese momento
        /// por el que se pasa como parámetro
        /// </summary>
        /// <param name="NombreArchivo">El nombre del archivo .docx que se quiere modificar</param>
        /// <param name="pUriCustomPart">el Uri que contiene el customPart a sustituir (p ej http://signumsoftware.com/2007/pruebas)</param>
        /// <param name="pNombreCustomPart">el nombre del customPart que se va a modificar. Si es string.empty, se modifica por defecto
        /// 'item1.xml'</param>
        public XmlDocument ObtenerCustomPart(string NombreArchivo, Uri pUriCustomPart, string pNombreCustomPart)
        {
            //el docXML que vamos a devolver
            XmlDocument doc = null;

            //si viene vacío, trabajamos por defecto con custompart1
            if (string.IsNullOrEmpty(pNombreCustomPart))
            {
                pNombreCustomPart = "item1.xml";
            }

            const string documentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            //el reltype necesario para encontrar los customParts
            string custom_rel_type = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXml";

            //  Open the package with read/write access.
            using (Package myPackage = Package.Open(NombreArchivo, FileMode.Open, FileAccess.ReadWrite))
            {
                //almacenamos aquí el mainPart
                PackagePart documentPart = null;

                //obtenemos el mainPart del document
                //  Get the main document part (workbook.xml, document.xml, presentation.xml).
                foreach (System.IO.Packaging.PackageRelationship relationship in myPackage.GetRelationshipsByType(documentRelationshipType))
                {
                    //  There should only be one document part in the package. 
                    Uri documentUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), relationship.TargetUri);
                    documentPart = myPackage.GetPart(documentUri);

                    break;
                }

                //ahora buscamos entre los customParts que hay en el mainPart
                foreach (PackageRelationship packageRel in documentPart.GetRelationshipsByType(custom_rel_type))
                {
                    Uri uriCustomPart = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), packageRel.TargetUri);

                    //si el nombre del customPart coincide cn el que buscamos
                    //ejecutamos el cambio
                    if (uriCustomPart.ToString().Contains(pNombreCustomPart))
                    {
                        PackagePart micustomPart = myPackage.GetPart(uriCustomPart);

                        doc = new XmlDocument();
                        doc.Load(micustomPart.GetStream());

                        break;
                    }
                }

            }

            return doc;
        }


        /// <summary>
        /// Modifica un customPart, cambiando el contenido que posea en ese momento
        /// por el que se pasa como parámetro
        /// </summary>
        /// <param name="NombreArchivo">El nombre del archivo .docx que se quiere modificar</param>
        /// <param name="pUriCustomPart">El Uri que contiene el customPart a sustituir (p ej http://signumsoftware.com/2007/pruebas)</param>
        /// <param name="pNuevoCustomPartXML">El documento con el contenido XML que se quiere establecer como customPart</param>
        /// <param name="pNombreCustomPart">El nombre del customPart que se va a modificar. Si es string.empty, se modifica por defecto
        /// 'item1.xml'</param>
        public void ModificarCustomPart(string NombreArchivo, Uri pUriCustomPart, XmlDocument pNuevoCustomPartXML, string pNombreCustomPart)
        {
            //si viene vacío, trabajamos por defecto con custompart1
            if (string.IsNullOrEmpty(pNombreCustomPart))
            {
                pNombreCustomPart = "item1.xml";
            }

            const string documentRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

            //el reltype necesario para encontrar los customParts
            string custom_rel_type = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXml";

            //  Open the package with read/write access.
            using (Package myPackage = Package.Open(NombreArchivo, FileMode.Open, FileAccess.ReadWrite))
            {
                //almacenamos aquí el mainPart
                PackagePart documentPart = null;

                //obtenemos el mainPart del document
                //  Get the main document part (workbook.xml, document.xml, presentation.xml).
                foreach (System.IO.Packaging.PackageRelationship relationship in myPackage.GetRelationshipsByType(documentRelationshipType))
                {
                    //  There should only be one document part in the package. 
                    Uri documentUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), relationship.TargetUri);
                    documentPart = myPackage.GetPart(documentUri);

                    break;
                }

                //ahora buscamos entre los customParts que hay en el mainPart
                foreach (PackageRelationship packageRel in documentPart.GetRelationshipsByType(custom_rel_type))
                {
                    Uri uriCustomPart = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), packageRel.TargetUri);

                    //si el nombre del customPart coincide cn el que buscamos
                    //ejecutamos el cambio
                    if (uriCustomPart.ToString().Contains(pNombreCustomPart))
                    {
                        PackagePart micustomPart = myPackage.GetPart(uriCustomPart);

                        XmlDocument doc = new XmlDocument();
                        doc.Load(micustomPart.GetStream());

                        //establecemos como contenido el XML que
                        //nos han pasado como parámetro
                        doc.InnerXml = pNuevoCustomPartXML.InnerXml;

                        doc.Save(micustomPart.GetStream(FileMode.Create, FileAccess.Write));

                        break;
                    }
                }

            }
        }

        /// <summary>
        /// Modifica el customPart por defecto de la plantilla de impresión, sustituyendo
        /// el contenido que tenga por el del documento que se pasa como parámetro.
        /// </summary>
        /// <param name="NombreArchivo">El nombre del archivo .docx que se quiere modificar</param>
        /// <param name="NuevoCustomPart">El documento con el contenido XML que se quiere establecer como customPart</param>
        public void ModificarCustomPart(string NombreArchivo, XmlDocument NuevoCustomPart)
        {
            Uri miUri = new Uri("http://signumsoftware.com/2007/pruebas");
            string nombreCustomPart = "item1.xml";
            ModificarCustomPart(NombreArchivo, miUri, NuevoCustomPart, nombreCustomPart);
        }


        #endregion

        #region Crear Partes de Documentos



        private XmlNode GenerarNodoDesdeObjetos(object Objeto, ref XmlDocument doc)
        {
            ////obtenemos las propiedadses del objeto
            //System.Reflection.PropertyInfo[] Propiedades = Objeto.GetType().GetProperties();

            //XmlNode nodoObjeto = doc.CreateElement(Objeto.GetType().Name);

            //foreach (System.Reflection.PropertyInfo propiedad in Propiedades)
            //{
            //    //comprobamos si es una colección
            //    if (propiedad.GetType() is System.Collections.IEnumerable)
            //    {

            //    }
            //}



            XmlNode nUsuario;
            nUsuario = doc.CreateElement("usuario");


            string contenido = @"
            <nombre>Mapache</nombre>
            <apellidos>Marrón</apellidos>
            <deudas>
                <items>
                    <deuda>
                        <descripcion>Muerte del conductor</descripcion>
                        <importe>6.000,00</importe>
                        <intereses>
                            <items>
                                <interes>
                                    <tipo>12</tipo>
                                </interes>
                                <interes>
                                    <tipo>6</tipo>
                                </interes>
                            </items>
                            <promedio>9</promedio>
                        </intereses>
                    </deuda>
                    <deuda>
                        <descripcion>Muerte de 10 niños pequeños</descripcion>
                        <importe>600.000,00</importe>
                        <intereses>
                            <items>
                                <interes>
                                    <tipo>12</tipo>
                                </interes>
                            </items>
                            <promedio>12</promedio>
                        </intereses>
                    </deuda>
                </items>
            </deudas>
            ";
            nUsuario.InnerXml = contenido;



            return nUsuario;
        }

        #endregion

    }


    internal class AsociadorDocumentos
    {

        private XmlNode mUltimoNodoDataSource;
        private List<string> listaIDsAsignadas = new List<string>();
        //para saber si hay que saltar el siguiente nodo a procesar (debido a un
        //insertafter), esta lista contiene la firma (innerxml) del nodo que se ha
        //procesado
        List<string> mListaNodosCombinados = new List<string>();
        //bool mSaltarSiguiente = false;


        /// <summary>
        /// Asocia y combina los ContentControls del documento XML plantilla cargado en memoria con los elementos que hay en el
        /// documento DataSourceXML de forma recursiva, generando el contenido ya correctamente asociado en el documento XML
        /// </summary>
        /// <param name="nodoPadrePlantilla">El nodo del documento plantilla cuyos elementos se quieren asociar (el primero será el 'w:body')</param>
        /// <param name="doc">El documento XML plantilla que se quiere asociar</param>
        /// <param name="docDataSource">El documento XML que contiene los elementos con los que se van a asociar. Debe estar ya cargado con los datos</param>
        /// <param name="nsManagerOriginal">El NameSpaceManager para ejecutar consultas XPath sobre el documento XML plantilla</param>
        /// <param name="nsManagerDataSource">El NameSpaceManager para ejecutar consultas XPath sobre el documento DataSource XML</param>
        /// <param name="listaIteraciones">La lista de iteraciones asociadas con el documento plantilla</param>
        /// <param name="iteracionPadre">La iteración que precede al nodo que se está combinando (nodoPadrePlantilla), si procede</param>
        /// <param name="numeroAsociacionesIncorrectas">Devuelve el número de asociaciones mal formadas en el documento plantilla XML</param>
        public void AsociarYCombinar(XmlNode nodoPadrePlantilla, ref XmlDocument doc,
     ref XmlDocument docDataSource, ref XmlNamespaceManager nsManagerOriginal,
     ref XmlNamespaceManager nsManagerDataSource, List<IteracionOXML> listaIteraciones,
     IteracionOXML iteracionPadre, int numeroAsociacionesIncorrectas)
        {
            //buscamos nodos asociados y de referencia a IteracionOXML en cualquier nivel de profundidad dentro del nodo actual
            //para podarlo si no tiene ningún elemento que combinar
            if (nodoPadrePlantilla.HasChildNodes && nodoPadrePlantilla.SelectNodes(@".//w:tag[starts-with(@w:val,'@')]|.//w:IteracionOXML", nsManagerOriginal).Count != 0)
            {
                //si se ha anotado que hay que saltar este nodo no lo procesamos 
                if (!(mListaNodosCombinados.Contains(nodoPadrePlantilla.InnerXml)))
                {
                    //apuntamos el último nodo referenciado para este nodo
                    XmlNode ultimoNodoDataSourceLocal = mUltimoNodoDataSource;

                    //recorremos todos los nodos que contiene directamente el nodo padre 
                    foreach (XmlNode nodoACombinar in nodoPadrePlantilla.ChildNodes)
                    {

                        //volvemos a determinar el nodo al que está asociado este nodo padre
                        mUltimoNodoDataSource = ultimoNodoDataSourceLocal;

                        //si es un nodo de referencia a iteración
                        if (nodoACombinar.Name == "w:IteracionOXML")
                        {
                            //guardamos el valor del último nodoDatasource para poder
                            //Restablecerlo después de salir de la iteración
                            XmlNode nodoDataSourceOld = mUltimoNodoDataSource;

                            //obtenemos la iteración a la que está asociada
                            IteracionOXML iteracionActual = ObtenerIteracionAsociada(listaIteraciones, iteracionPadre, nodoACombinar);

                            //realizamos el proceso de asociación de la iteración
                            string asociacion = nodoACombinar.Attributes.GetNamedItem("w:ElementoAsociado").Value;
                            string asociacionSinSimbolos = asociacion.Replace("#", string.Empty);
                            //comprobamos si hay un objeto padre
                            string ObjetoPadre = string.Empty;
                            if (asociacionSinSimbolos.Contains("."))
                            {
                                ObjetoPadre = asociacionSinSimbolos.Substring(0, asociacionSinSimbolos.IndexOf("."));
                            }

                            //inicializamos la expresión de consulta
                            string XPExpression = string.Concat("m:", asociacion.Replace(".", "/m:").Replace("@", string.Empty).Replace("¬", string.Empty).Replace("#", string.Empty));

                            //ajustamos el Xpath para que busque en el root o a partir del objeto que contiene la propiedad
                            if (iteracionPadre == null || mUltimoNodoDataSource == null || string.IsNullOrEmpty(ObjetoPadre)) //mUltimoNodoDataSource.ParentNode.Name == ObjetoPadre)
                            {
                                //hay que buscar desde el root del documento datasource
                                XPExpression = string.Concat("/m:mapa/", XPExpression);
                            }
                            else
                            {
                                //hay que buscar a partir del último nodo datasource (representa el objeto que contiene la iteración)
                                XPExpression = XPExpression.Replace("m:" + ObjetoPadre + "/", @"./");
                            }


                            //obtenemos el "elemento asociado" a la iteración
                            //p ej: <deudas> -> el contenedor de la colección
                            //(el nodo que contiene los elementos de iteración 
                            //p ej: <deudas><items><deuda1/>...<deudan/><items/><deudas/>
                            XmlNode nodoContenedorIteracionDataSource = null;
                            if (iteracionPadre != null)
                            {
                                //hay que buscar dentro a partir del elemento de la iteración anterior
                                nodoContenedorIteracionDataSource = mUltimoNodoDataSource.SelectSingleNode(XPExpression, nsManagerDataSource);
                            }
                            else
                            {
                                //hay que buscar en el documento
                                nodoContenedorIteracionDataSource = docDataSource.SelectSingleNode(XPExpression, nsManagerDataSource);
                            }
                            if (nodoContenedorIteracionDataSource != null)
                            {
                                //establecemos el nodoDataSource de la colección a la iteración con la que estamos trabajando
                                iteracionActual.nodoAsociadoDataSource = nodoContenedorIteracionDataSource;

                                //obtenemos todos los elementos que se encuentren dentro del nodo items de la iteracion
                                XmlNodeList nodosItemsIteracion = nodoContenedorIteracionDataSource.SelectNodes("./m:items/*", nsManagerDataSource);

                                if (nodosItemsIteracion.Count != 0)
                                {
                                    foreach (XmlNode nodoItemDataSource in nodosItemsIteracion)
                                    {
                                        XmlNode nodoItemACombinar = iteracionActual.FragmentoXML.CloneNode(true);
                                        //asociamos el contenido del nodo que acabamos de insertar con su elemento correspondiente
                                        foreach (XmlNode nodo in nodoItemACombinar.ChildNodes)
                                        {
                                            //establecemos el último nodo datasource con el que se ha trabajado
                                            mUltimoNodoDataSource = nodoItemDataSource;
                                            AsociarYCombinar(nodo, ref doc, ref docDataSource, ref nsManagerOriginal, ref nsManagerDataSource, listaIteraciones, iteracionActual, numeroAsociacionesIncorrectas);
                                        }
                                        //insertamos el nodo que se debe combinar en el lugar que le corresponda
                                        //(no se puede encadenar un párrafo dentro de otro)
                                        InsertarContenidoIteracionCombinado(nsManagerOriginal, nsManagerDataSource, nodoACombinar, nodoItemACombinar);
                                    }
                                }
                            }

                            //ahora eliminamos el nodo de referencia de la iteraciónOXML
                            nodoACombinar.ParentNode.RemoveChild(nodoACombinar);

                            //restablecemos el ultimo nodo datasource al salir de la iteración
                            mUltimoNodoDataSource = nodoDataSourceOld;

                        }//fin 'es una iteración'
                        else
                        {
                            XmlNode nodoACombinarDataSource = null;
                            //si es un tag de un content control que define una asociación a un elemento
                            if (nodoACombinar.Name == "w:tag" && nodoACombinar.Attributes[0].Value.StartsWith("@"))
                            {
                                string asociacion, ObjetoPadre, XPExpression;

                                //obtenemos el tipo de asociación y definimos los objetos necesarios para el XPath
                                TipoAsociacion tasoc = GenerarObjetosXPath(nodoACombinar, mUltimoNodoDataSource, iteracionPadre, out asociacion, out ObjetoPadre, out XPExpression);


                                if (tasoc == TipoAsociacion.Incorrecto)
                                {
                                    //si estaba mal definida la asociación lo apuntamos y dejamos de trabajar con el nodo
                                    numeroAsociacionesIncorrectas++;
                                }
                                else
                                {
                                    //seleccionamos el nodo al que se debe asociar el nodo del doc original

                                    if (mUltimoNodoDataSource == null)
                                    {
                                        nodoACombinarDataSource = docDataSource.SelectSingleNode(XPExpression, nsManagerDataSource);
                                    }
                                    else
                                    {
                                        nodoACombinarDataSource = mUltimoNodoDataSource.SelectSingleNode(XPExpression, nsManagerDataSource);
                                    }
                                    if (nodoACombinarDataSource != null)
                                    {
                                        //establecemos cuál es el último nodo DataSource con el que se ha trabajado
                                        mUltimoNodoDataSource = nodoACombinarDataSource;

                                        //obtenemos el ContentControl al que pertenece el tag
                                        XmlNode ContentControl = nodoACombinar.ParentNode;

                                        //obtenemos el nodo que detalla la asociación con el elemento correspondiente
                                        //del DataSource
                                        XmlNode nodoDataBinding = ContentControl.SelectSingleNode("w:dataBinding", nsManagerOriginal);

                                        //si no exite el nodo de asociación con el elemento, lo creamos
                                        if (nodoDataBinding == null)
                                        {
                                            nodoDataBinding = doc.CreateElement("w:dataBinding");
                                            nodoDataBinding.Attributes.Append(doc.CreateAttribute("w:prefixMappings"));
                                            nodoDataBinding.Attributes.Append(doc.CreateAttribute("w:xpath"));
                                            nodoDataBinding.Attributes.Append(doc.CreateAttribute("w:storeItemID"));
                                            ContentControl.AppendChild(nodoDataBinding);
                                        }

                                        //enlazamos el nodo de asociación con el elemento que corresponde: el xmlns del mapa del datasource,
                                        //la ruta xPath del elemento en el datasource, y el GUID que tiene asociado el datasource en sus "props"
                                        nodoDataBinding.Attributes["w:prefixMappings"].Value = "xmlns:ns0='http://signumsoftware.com/2007/pruebas'";
                                        nodoDataBinding.Attributes["w:xpath"].Value = ObtenerPrefijoXPath(nodoACombinarDataSource, docDataSource, nsManagerDataSource);
                                        nodoDataBinding.Attributes["w:storeItemID"].Value = @"{28DAF809-0C14-44E3-8E79-B0949F72CA1C}";

                                        //cambiamos, si es necesario, los valores id y Placeholder del ContentControl
                                        XmlNode nodoID = ContentControl.SelectSingleNode("./w:id", nsManagerOriginal);
                                        XmlNode nodoPlaceHolder = ContentControl.SelectSingleNode("./w:placeholder", nsManagerOriginal);
                                        XmlNode nododocPart = nodoPlaceHolder.SelectSingleNode("./w:docPart", nsManagerOriginal);

                                        //comprobamos si existe más de un nodo con estos valores en el documento
                                        //if (doc.SelectNodes("//w:id[@w:val='" + nodoID.Attributes["w:val"].Value + "']", nsManagerOriginal).Count > 1)
                                        //{
                                        //hay que asignar un nuevo valor
                                        nodoID.Attributes["w:val"].Value = GenerarIDUnico(doc, nsManagerOriginal).ToString();
                                        //}

                                        //if (doc.SelectNodes("//w:placeholder/w:docPart[@w:val='" + nododocPart.Attributes[0].Value + "']", nsManagerOriginal).Count > 1)
                                        //{
                                        //hay que asignar un nuevo valor
                                        nododocPart.Attributes["w:val"].Value = System.Guid.NewGuid().ToString().Replace("-", string.Empty);
                                        //}

                                        //formateamos correctamente los valores XML para que se encuentren dentro del localnamespace
                                        FormatearXMLContentControlAsociado(ContentControl);

                                    }
                                }
                            }//fin 'es un tag asociación'
                            //ahora llamamos recursivamente a esta misma función para que se revisen cada uno de los nodos hijos
                            //del nodo actual
                            if (nodoACombinar.HasChildNodes)
                            {
                                AsociarYCombinar(nodoACombinar, ref doc, ref docDataSource, ref nsManagerOriginal, ref nsManagerDataSource, listaIteraciones, iteracionPadre, numeroAsociacionesIncorrectas);
                            }//fin llamada recursiva a este método

                        }//fin else 'no es una iteración'

                    }//fin recorrer todos los nodos del nodopadreplantilla

                }//fin comprobar si hay que saltar el nodopadreplantilla

            }//fin buscar nodos asociados y de iteración en nodopadreplantilla
        }

        /// <summary>
        /// Inserta el contenido ya asociado y combinado de una iteración en el lugar correspondiente
        /// del documento plantilla
        /// <remarks>No se puede insertar un párrafo dentro de otro, por lo que hay que comprobar si dentro de los
        /// nodos que se van a insertar hay algún párrafo, y si en el nodo en el que se debería insertar en la plantilla
        /// es o está dentro de uno</remarks>
        /// <param name="nsManagerOriginal">El NameSpaceManager para ejecutar xpath en el documento plantilla</param>
        /// <param name="nsManagerDataSource">El NameSpaceManager para ejecutar xpath en el documento DataSource</param>
        /// <param name="nodoACombinar">El nodo del documento plantilla en el que se va a insertar el contenido asociado de la iteración</param>
        /// <param name="nodoItemACombinar">El nodo XMLDocumentFragment que contiene la iteración asoicada y combinada</param>
        private void InsertarContenidoIteracionCombinado(XmlNamespaceManager nsManagerOriginal, XmlNamespaceManager nsManagerDataSource, XmlNode nodoACombinar, XmlNode nodoItemACombinar)
        {
            //---> lo hacemos con todo el contenido del fragmentoXML?? o sólo a partir del primer
            //párrafo que encontremos??

            //si el elemento en la plantilla es una celda de una tabla, lo insertamos normalmente
            if (nodoACombinar.ParentNode.Name == "w:tc" || nodoACombinar.ParentNode.Name == "w:body")
            {
                //insertamos el nodo que se tiene que combinar antes del nodo de referencia de la iteración
                nodoACombinar.ParentNode.InsertBefore(nodoItemACombinar, nodoACombinar);
            }
            else
            {
                if (nodoItemACombinar.SelectNodes("//w:p", nsManagerOriginal).Count != 0)
                {
                    //dentro del FragmentoXML hay un párrafo
                    if (nodoACombinar.SelectNodes("./ancestor::w:p", nsManagerOriginal).Count != 0)
                    {
                        //alguno de sus padres es un párrafo, así que no se puede embeber en él
                        //insertamos el nodo antes del primer párrafo que haya

                        //1º apuntamos en la lista la firma-innerxml de los nodos sdt
                        //como lo hemos añadido a continuación del párrafo de la iteración,
                        //apuntamos que hay que saltar el nodo que acabamos de combinar
                        XmlNodeList nodosContentControl = nodoItemACombinar.SelectNodes("//w:sdt", nsManagerOriginal);
                        foreach (XmlNode nodoSdt in nodosContentControl)
                        {
                            mListaNodosCombinados.Add(nodoSdt.InnerXml);
                        }
                        //mListaNodosCombinados.Add(nodoItemACombinar.InnerXml);

                        XmlNode nodoPrimerParrafo = nodoACombinar.SelectSingleNode("./ancestor::w:p[1]", nsManagerOriginal);
                        nodoPrimerParrafo.ParentNode.InsertAfter(nodoItemACombinar, nodoPrimerParrafo);
                        //mSaltarSiguiente = true;
                    }
                    else
                    {
                        //no hay ningún párrafo que vaya a contener el FragmentoXML
                        //insertamos el nodo que se tiene que combinar antes del nodo de referencia de la iteración
                        nodoACombinar.ParentNode.InsertBefore(nodoItemACombinar, nodoACombinar);
                    }
                }
                else
                {
                    //insertamos el nodo que se tiene que combinar antes del nodo de referencia de la iteración
                    nodoACombinar.ParentNode.InsertBefore(nodoItemACombinar, nodoACombinar);
                }
            }
        }


        /// <summary>
        /// Obtiene la iteración que está asociada al nodo de referencia a iteración correspondiente de la lista de iteraciones
        /// </summary>
        /// <param name="listaIteraciones">La lista que contiene todas las iteraciones definidas en el documento</param>
        /// <param name="iteracionPadre">La iteración padre de la actual (si corresponde)</param>
        /// <param name="nodoACombinar">El nodo de referencia a iteración</param>
        /// <returns>La IteracionOXML correspondiente al nodo de referencia a iteración</returns>
        private IteracionOXML ObtenerIteracionAsociada(List<IteracionOXML> listaIteraciones, IteracionOXML iteracionPadre, XmlNode nodoACombinar)
        {
            IteracionOXML iteracionActual = null;
            if (iteracionPadre != null)
            {
                foreach (IteracionOXML iter in iteracionPadre.Iteraciones)
                {
                    if (iter.ID.ToString() == nodoACombinar.Attributes.GetNamedItem("w:GUID").Value)
                    {
                        iteracionActual = iter;
                        break;
                    }
                }
            }
            else
            {
                foreach (IteracionOXML iter in listaIteraciones)
                {
                    if (iter.ID.ToString() == nodoACombinar.Attributes.GetNamedItem("w:GUID").Value)
                    {
                        iteracionActual = iter;
                        break;
                    }
                }
            }
            return iteracionActual;
        }


        /// <summary>
        /// Determina el nivel del elemento de asociación del nodo actual y genera los 
        /// objetos necesarios para ejecutar la búsqueda XPath para obtener el elemento del DataSource
        /// </summary>
        /// <param name="nodoACombinar">El nodo a asociar</param>
        /// <param name="ultimoNodoDataSource">El último nodo del DataSource</param>
        /// <param name="iteracionPadre">La iteración en la que se encuentra (null si no está en una iteración)</param>
        /// <param name="asociacion">La definición de la asociación del nodo (p ej: @#usuario.nombre)</param>
        /// <param name="ObjetoPadre">El Objeto al que pertenece la propiedad a la que se asocia 
        /// (p ej: de usuaio.nombre -> usuario)</param>
        /// <param name="XPExpression">La expresión de búsqueda para ejecutar el XPath</param>
        /// <returns>El Tipo de Asociación que define el nodo a combinar</returns>
        private TipoAsociacion GenerarObjetosXPath(XmlNode nodoACombinar, XmlNode ultimoNodoDataSource, IteracionOXML iteracionPadre, out string asociacion, out string ObjetoPadre, out string XPExpression)
        {
            //es un elemento asociado
            asociacion = nodoACombinar.Attributes[0].Value;

            //inicializamos la expresión de consulta
            string asociacionSinSimbolos = (asociacion.Replace("@", string.Empty).Replace("#", string.Empty).Replace("¬", string.Empty));

            ObjetoPadre = asociacionSinSimbolos.Substring(0, asociacionSinSimbolos.IndexOf("."));
            XPExpression = string.Concat("m:", asociacionSinSimbolos.Replace(".", "/m:"));
            if (mUltimoNodoDataSource == null)
            {
                //hay que buscar desde el root del documento datasource
                XPExpression = string.Concat("/m:mapa/", XPExpression);
            }

            int nivelesUp;
            TipoAsociacion tasoc = ExaminarAsociacion(asociacion, out nivelesUp);
            switch (tasoc)
            {
                case TipoAsociacion.NivelActual:
                    if (mUltimoNodoDataSource != null)
                    {
                        //quitamos el definidor de la propiedad, ya que es el propio nodo a partir del que
                        //se va a hacer la búsqueda (usuario.nombre -> nombre)
                        XPExpression = QuitarDefinidorDePropiedad(iteracionPadre, ObjetoPadre, XPExpression, tasoc);
                    }
                    break;
                case TipoAsociacion.SubirNivel:
                    if (mUltimoNodoDataSource == null)
                    {
                        //no puede definir que suba n niveles si no hay un nodo anterior
                        tasoc = TipoAsociacion.Incorrecto;
                        break;
                    }
                    //si estamos en una iteración, aumentamos en uno el escape para que salga de
                    //la colección "items"
                    XmlNode nodoActual = ultimoNodoDataSource;
                    for (int i = 0; i < nivelesUp; i++)
                    {
                        XPExpression = string.Concat("../", XPExpression);
                        nodoActual = nodoActual.ParentNode;
                        if (nodoActual.Name == "items")
                        {
                            XPExpression = string.Concat("../../", XPExpression);
                            nodoActual = nodoActual.ParentNode.ParentNode;
                        }
                    }
                    //quitamos el definidor de propiedad
                    XPExpression = QuitarDefinidorDePropiedad(iteracionPadre, ObjetoPadre, XPExpression, tasoc);
                    break;
                case TipoAsociacion.Incorrecto:
                    break;
                default:
                    break;
            }
            return tasoc;
        }


        /// <summary>
        /// quitamos el definidor de la propiedad, ya que es el propio nodo a partir del que
        ///se va a hacer la búsqueda (usuario.nombre -> nombre: hay que retorceder 1 nodo para llegar al objeto)
        /// </summary>
        /// <param name="iteracionPadre">la iteración OXML en la que estamos (si la hay)</param>
        /// <param name="ObjetoPadre">La definición de la propiedad (p ej: usuario)</param>
        /// <param name="XPExpression">La exprexión de xonsulta XPath que se va a normalizar</param>
        /// <returns></returns>
        private string QuitarDefinidorDePropiedad(IteracionOXML iteracionPadre, string ObjetoPadre, string XPExpression, TipoAsociacion tasoc)
        {
            switch (tasoc)
            {
                case TipoAsociacion.NivelActual:
                    if (iteracionPadre == null || mUltimoNodoDataSource.ParentNode.Name != "items")
                    {
                        XPExpression = XPExpression.Replace("m:" + ObjetoPadre + "/", "../");
                    }
                    else
                    {
                        XPExpression = XPExpression.Replace("m:" + ObjetoPadre + "/", "./");
                    }
                    break;
                case TipoAsociacion.SubirNivel:
                    //si ha subido niveles, nos ha dejado en el nivel justo de la propiedad
                    XPExpression = XPExpression.Replace("m:" + ObjetoPadre + "/", string.Empty);
                    break;
                case TipoAsociacion.Incorrecto:
                    break;
                default:
                    break;
            }
            return XPExpression;
        }


        /// <summary>
        /// Formatea correctamente el XML del ContentControl para que todos los nodos y atributos se encuentren
        /// dentro del namespace
        /// </summary>
        /// <param name="ContentControl"></param>
        private static void FormatearXMLContentControlAsociado(XmlNode ContentControl)
        {
            //los elementos del databinding
            ContentControl.InnerXml = ContentControl.InnerXml.Replace("<dataBinding", "<w:dataBinding");
            ContentControl.InnerXml = ContentControl.InnerXml.Replace(" prefixMappings", " w:prefixMappings");
            ContentControl.InnerXml = ContentControl.InnerXml.Replace(" xpath", " w:xpath");
            ContentControl.InnerXml = ContentControl.InnerXml.Replace(" storeItemID", " w:storeItemID");
            //los elementos del id
            ContentControl.InnerXml = ContentControl.InnerXml.Replace("<id", "<w:id");
            ContentControl.InnerXml = ContentControl.InnerXml.Replace(" val", " w:val");
            //los elementos del placeholder
            ContentControl.InnerXml = ContentControl.InnerXml.Replace("<placeholder", "<w:placeholder");
            ContentControl.InnerXml = ContentControl.InnerXml.Replace("<docPart", "<w:docPart");
        }


        private TipoAsociacion ExaminarAsociacion(string asociacion, out int nivelesASubir)
        {
            nivelesASubir = 0;
            if (asociacion.StartsWith("@"))
            {
                if (asociacion.StartsWith("@#"))
                {
                    //se trata de una asociación en el nivel actual
                    return TipoAsociacion.NivelActual;
                }
                else
                {
                    if (asociacion.StartsWith("@¬"))
                    {
                        nivelesASubir = ContarCaracteres(asociacion, "¬".ToCharArray()[0]);
                        return TipoAsociacion.SubirNivel;
                    }
                    //no es una deficnición de asociación válida
                    else return TipoAsociacion.Incorrecto;
                }
            }
            return TipoAsociacion.Incorrecto;
        }

        private enum TipoAsociacion
        {
            NivelActual,
            SubirNivel,
            Incorrecto
        };

        private int ContarCaracteres(string textoOrigen, char caracter)
        {
            int numero = 0;
            foreach (char car in textoOrigen)
            {
                if (car == caracter)
                {
                    numero++;
                }
            }
            return numero;
        }

        /// <summary>
        /// Genera un ID único para un ContentControl, comprobando que no está repetido
        /// en el documento XML de DataSource
        /// </summary>
        /// <param name="documentoOriginal">El documento XML main en el que se encuentran (o van a encotrar) los contentcontrols</param>
        /// <param name="nsManager">Un namespace formateado para poder ejecutar consultas 
        /// xpath sobre el documento XML</param>
        /// <returns>Un Integer que representa el ID único</returns>
        private int GenerarIDUnico(XmlDocument documentoOriginal, XmlNamespaceManager nsManager)
        {
            Random r = new Random(DateTime.Now.Millisecond);
            int ValorUnico = 0;
            bool valido = false;
            while (!valido)
            {
                ValorUnico = r.Next(1111111, 9999999);
                //comprobamos que no exista ya en el documento
                valido = (!listaIDsAsignadas.Contains(ValorUnico.ToString()));
                //valido = (documentoOriginal.SelectNodes(@"//w:id[@id='" + ValorUnico.ToString() + @"']", nsManager).Count == 0);
            }
            listaIDsAsignadas.Add(ValorUnico.ToString());
            return ValorUnico;
        }

        /// <summary>
        /// Devuelve el valor que se ha de incluir en el atributo w:xpath, determinando el
        /// número del elemento con el que se asociará el contenido
        /// </summary>
        /// <param name="nodoElemento">El nodo del elemento cuyo xpath se quiere obtener (p ej usuario)</param>
        /// <param name="documentoDataSource">El documento XML del custompart cargado con los datos</param>
        /// <param name="nsManagerDataSource">El XmlNameSpaceManager preparado para ejecutar consultas XPath</param>
        /// <returns>un stirng con el valor deseado (p ej usuario.persona.nombre)</returns>
        private string ObtenerPrefijoXPath(XmlNode nodoElemento, XmlDocument documentoDataSource, XmlNamespaceManager nsManagerDataSource)
        {
            //construimos el valor para el atributo xpath
            StringBuilder atributoXPath = new StringBuilder();
            XmlNode nodoactual = nodoElemento;
            while (nodoactual != null && nodoactual.NodeType.ToString() != "Document")
            {
                atributoXPath.Insert(0, @"]");
                atributoXPath.Insert(0, (nodoactual.SelectNodes(@"./preceding-sibling::m:" + nodoactual.Name, nsManagerDataSource).Count + 1).ToString());
                atributoXPath.Insert(0, @"[");
                atributoXPath.Insert(0, nodoactual.Name);
                atributoXPath.Insert(0, @"/ns0:");

                nodoactual = nodoactual.ParentNode;
            }
            return atributoXPath.ToString();
        }
    }
}