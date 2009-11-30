namespace Signum.Excel.Schemas
{
    using Signum.Excel;
    using System;
    using System.Xml;

    public sealed class Schema : IWriter
    {
        private SchemaTypeCollection _types;

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Schema", Namespaces.Schema);
            if (this._types != null)
            {
                ((IWriter) this._types).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        public SchemaTypeCollection Types
        {
            get
            {
                if (this._types == null)
                {
                    this._types = new SchemaTypeCollection();
                }
                return this._types;
            }
            set
            {
                this._types= value;
            }
        }
    }
}

