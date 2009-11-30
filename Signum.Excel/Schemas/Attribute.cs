namespace Signum.Excel.Schemas
{
    using Signum.Excel;
    using System;
    using System.Xml;

    public sealed class Attribute : IWriter
    {
        private string _type;

        public Attribute()
        {
        }

        public Attribute(string type)
        {
            this._type = type;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "attribute", Namespaces.Schema);
            if (this._type != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "type", Namespaces.Schema, this._type);
            }
            writer.WriteEndElement();
        }

        public string Type
        {
            get
            {
                return this._type;
            }
            set
            {
                this._type = value;
            }
        }
    }
}

