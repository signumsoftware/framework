namespace Signum.Excel
{
    using System;
    using System.Globalization;
    using System.Xml;

    public sealed class PTSource : IWriter
    {
        private int _cacheIndex = Namespaces.NullValue;
        private PTConsolidationReference _consolidationReference;
        private DateTime _refreshDate = DateTime.MinValue;
        private DateTime _refreshDateCopy = DateTime.MinValue;
        private string _refreshName;
        private int _versionLastRefresh = Namespaces.NullValue;

        
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PTSource", Namespaces.Excel);
            if (this._cacheIndex != Namespaces.NullValue)
            {
                writer.WriteElementString("CacheIndex", Namespaces.Excel, this._cacheIndex.ToString(CultureInfo.InvariantCulture));
            }
            if (this._versionLastRefresh != Namespaces.NullValue)
            {
                writer.WriteElementString("VersionLastRefresh", Namespaces.Excel, this._versionLastRefresh.ToString(CultureInfo.InvariantCulture));
            }
            if (this._refreshName != null)
            {
                writer.WriteElementString("RefreshName", Namespaces.Excel, this._refreshName);
            }
            if (this._refreshDate != DateTime.MinValue)
            {
                writer.WriteElementString("RefreshDate", Namespaces.Excel, this._refreshDate.ToString(Namespaces.SchemaPrefix, CultureInfo.InvariantCulture));
            }
            if (this._refreshDateCopy != DateTime.MinValue)
            {
                writer.WriteElementString("RefreshDateCopy", Namespaces.Excel, this._refreshDateCopy.ToString(Namespaces.SchemaPrefix, CultureInfo.InvariantCulture));
            }
            if (this._consolidationReference != null)
            {
                ((IWriter) this._consolidationReference).WriteXml(writer);
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

        public PTConsolidationReference ConsolidationReference
        {
            get
            {
                if (this._consolidationReference == null)
                {
                    this._consolidationReference = new PTConsolidationReference();
                }
                return this._consolidationReference;
            }
            set
            {
                this._consolidationReference = value;
            }
        }

        public DateTime RefreshDate
        {
            get
            {
                return this._refreshDate;
            }
            set
            {
                this._refreshDate = value;
            }
        }

        public DateTime RefreshDateCopy
        {
            get
            {
                return this._refreshDateCopy;
            }
            set
            {
                this._refreshDateCopy = value;
            }
        }

        public string RefreshName
        {
            get
            {
                return this._refreshName;
            }
            set
            {
                this._refreshName = value;
            }
        }

        public int VersionLastRefresh
        {
            get
            {
                return this._versionLastRefresh;
            }
            set
            {
                this._versionLastRefresh = value;
            }
        }
    }
}

