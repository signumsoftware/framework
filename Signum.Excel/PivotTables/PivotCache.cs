namespace Signum.Excel
{
    using Signum.Excel.Schemas;
    using System;
    using System.Globalization;
    using System.Xml;

    public sealed class PivotCache : IWriter
    {
        private int _cacheIndex = Namespaces.NullValue;
        private RowsetData _rowsetData;
        private Schema _schema;

      

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PivotCache", Namespaces.Excel);
            if (this._cacheIndex != Namespaces.NullValue)
            {
                writer.WriteElementString("CacheIndex", Namespaces.Excel, this._cacheIndex.ToString(CultureInfo.InvariantCulture));
            }
            if (this._schema != null)
            {
                ((IWriter) this._schema).WriteXml(writer);
            }
            if (this._rowsetData != null)
            {
                ((IWriter) this._rowsetData).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        public int CacheIndex
        {
            get
            {
                return this._cacheIndex;
            }
            set
            {
                this._cacheIndex = value;
            }
        }

        public RowsetData Data
        {
            get
            {
                if (this._rowsetData == null)
                {
                    this._rowsetData = new RowsetData();
                }
                return this._rowsetData;
            }
            set
            {
                this._rowsetData = value;
            }
        }

        public Schema Schema
        {
            get
            {
                if (this._schema == null)
                {
                    this._schema = new Schema();
                }
                return this._schema;
            }
            set
            {
                this._schema= value;
            }
        }
    }
}

