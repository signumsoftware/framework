using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;


namespace Framework.GestorInformes
{
    public class IteracionOXML
    {
        //campos
        public XmlDocumentFragment FragmentoXML;
        public XmlDocument DocumentoOrigen;
        public XmlNamespaceManager nsManager;
        public List<IteracionOXML> Iteraciones;
        private Guid mID;
        private string mElementoAsociado;
        /// <summary>
        /// El nodo del docDataSource al que se ha asociado esta iteración
        /// </summary>
        public XmlNode nodoAsociadoDataSource;

        //constructor
        public IteracionOXML(XmlDocument pDocumentoOrigen, XmlNamespaceManager pnsManager, string pElementoAsociado)
        {
            this.DocumentoOrigen = pDocumentoOrigen;
            this.nsManager = pnsManager;
            this.Iteraciones = new List<IteracionOXML>();
            this.FragmentoXML = DocumentoOrigen.CreateDocumentFragment();
            this.mID = Guid.NewGuid();
            this.mElementoAsociado = pElementoAsociado;
        }

        //propiedades
        public Guid ID
        {
            get { return mID; }
        }

        public string ElementoAsociado
        {
            get { return mElementoAsociado; }
        }

        //métodos
        public XmlNode GenerarNodoReferencia()
        {
            //XmlNode nodo = DocumentoOrigen.CreateElement("w", "IteracionOXML", DocumentoOrigen.GetNamespaceOfPrefix("w"));
            //nodo.Attributes.Append(DocumentoOrigen.CreateAttribute("GUID"));
            //nodo.Attributes.Append(DocumentoOrigen.CreateAttribute("ElementoAsociado"));
            //nodo.Attributes["GUID"].Value = mID.ToString();
            //nodo.Attributes["ElementoAsociado"].Value = mElementoAsociado;
            XmlNode nodo = DocumentoOrigen.CreateElement("w", "IteracionOXML", nsManager.LookupNamespace("w"));
            XmlAttribute aGUID = DocumentoOrigen.CreateAttribute("w", "GUID", nsManager.LookupNamespace("w"));
            aGUID.Value = mID.ToString();
            nodo.Attributes.Append(aGUID);
            XmlAttribute aEA = DocumentoOrigen.CreateAttribute("w", "ElementoAsociado", nsManager.LookupNamespace("w"));
            aEA.Value = mElementoAsociado;
            nodo.Attributes.Append(aEA);

            return nodo;
        }
    }
}