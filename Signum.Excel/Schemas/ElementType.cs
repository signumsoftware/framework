namespace Signum.Excel.Schemas
{
    using Signum.Excel;
    using System;
    using System.Globalization;
    using System.Xml;

    public sealed class ElementType : SchemaType, IWriter
    {
        private AttributeCollection _attributes;
        private SchemaContent _content;
        private string _name;

        public override  void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "ElementType", Namespaces.Schema);
            if (this._name != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "name", Namespaces.Schema, this._name);
            }
            if (this._content != SchemaContent.NotSet)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "content", Namespaces.Schema, this._content.ToString());
            }
            if (this._attributes != null)
            {
                ((IWriter) this._attributes).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        public AttributeCollection Attributes
        {
            get
            {
                if (this._attributes == null)
                {
                    this._attributes = new AttributeCollection();
                }
                return this._attributes;
            }
            set
            {
                this._attributes = new AttributeCollection();
            }
        }

        public SchemaContent Content
        {
            get
            {
                return this._content;
            }
            set
            {
                this._content = value;
            }
        }

        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }
    }
}

