namespace Signum.Excel
{
    using System;
    using System.Xml;

    public sealed class PTConsolidationReference : IWriter
    {
        private string _fileName;
        private string _reference;

        
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "ConsolidationReference", Namespaces.Excel);
            if (this._fileName != null)
            {
                writer.WriteElementString("FileName", Namespaces.Excel, this._fileName);
            }
            if (this._reference != null)
            {
                writer.WriteElementString("Reference", Namespaces.Excel, this._reference);
            }
            writer.WriteEndElement();
        }

        public string FileName
        {
            get
            {
                return this._fileName;
            }
            set
            {
                this._fileName = value;
            }
        }

        public string Reference
        {
            get
            {
                return this._reference;
            }
            set
            {
                this._reference = value;
            }
        }
    }
}

