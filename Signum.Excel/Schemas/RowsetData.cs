namespace Signum.Excel.Schemas
{
    using Signum.Excel;
    using System;
    using System.Xml;

    public sealed class RowsetData : IWriter
    {
        private RowsetRowCollection _rows;

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.RowsetPrefix, "data", Namespaces.Rowset);
            if (this._rows != null)
            {
                ((IWriter) this._rows).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        public RowsetRowCollection Rows
        {
            get
            {
                if (this._rows == null)
                {
                    this._rows = new RowsetRowCollection();
                }
                return this._rows;
            }
            set
            {
                this._rows= value;
            }
        }
    }
}

