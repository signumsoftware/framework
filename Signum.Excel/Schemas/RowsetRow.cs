namespace Signum.Excel.Schemas
{
    using Signum.Excel;
    using System;
    using System.Xml;

    public sealed class RowsetRow : IWriter
    {
        private RowsetColumnCollection _columns;

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.RowsetSchemaPrefix, "row", Namespaces.RowsetSchema);
            if (this._columns != null)
            {
                ((IWriter) this._columns).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        public RowsetColumnCollection Columns
        {
            get
            {
                if (this._columns == null)
                {
                    this._columns = new RowsetColumnCollection();
                }
                return this._columns;
            }
            set
            {
                this._columns= value;
            }
        }
    }
}

