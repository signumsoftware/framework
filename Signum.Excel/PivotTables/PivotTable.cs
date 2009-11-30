namespace Signum.Excel
{
    using System;
    using System.Xml;

    public sealed class PivotTable : IWriter
    {
        private string _defaultVersion;
        private string _location;
        private string _name;
        private PivotFieldCollection _pivotFields;
        private PTLineItemCollection _pTLinesItems;
        private PTSource _pTSource;
        private string _versionLastUpdate;

        
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PivotTable", Namespaces.Excel);
            if (this._name != null)
            {
                writer.WriteElementString("Name", Namespaces.Excel, this._name);
            }
            writer.WriteStartElement(Namespaces.ExcelPrefix, "ImmediateItemsOnDrop", Namespaces.Excel);
            writer.WriteEndElement();
            writer.WriteStartElement(Namespaces.ExcelPrefix, "ShowPageMultipleItemLabel", Namespaces.Excel);
            writer.WriteEndElement();
            if (this._location != null)
            {
                writer.WriteElementString("Location", Namespaces.Excel, this._location);
            }
            if (this._defaultVersion != null)
            {
                writer.WriteElementString("DefaultVersion", Namespaces.Excel, this._defaultVersion);
            }
            if (this._versionLastUpdate != null)
            {
                writer.WriteElementString("VersionLastUpdate", Namespaces.Excel, this._versionLastUpdate);
            }
            if (this._pivotFields != null)
            {
                ((IWriter) this._pivotFields).WriteXml(writer);
            }
            if (this._pTLinesItems != null)
            {
                ((IWriter) this._pTLinesItems).WriteXml(writer);
            }
            if (this._pTSource != null)
            {
                ((IWriter) this._pTSource).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        public string DefaultVersion
        {
            get
            {
                return this._defaultVersion;
            }
            set
            {
                this._defaultVersion = value;
            }
        }

        public PTLineItemCollection LineItems
        {
            get
            {
                if (this._pTLinesItems == null)
                {
                    this._pTLinesItems = new PTLineItemCollection();
                }
                return this._pTLinesItems;
            }

            set
            {
                this._pTLinesItems= value;
            }
        }

        public string Location
        {
            get
            {
                return this._location;
            }
            set
            {
                this._location = value;
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

        public PivotFieldCollection PivotFields
        {
            get
            {
                if (this._pivotFields == null)
                {
                    this._pivotFields = new PivotFieldCollection();
                }
                return this._pivotFields;
            }
            set
            {
                this._pivotFields= value;
            }
        }

        public PTSource Source
        {
            get
            {
                if (this._pTSource == null)
                {
                    this._pTSource = new PTSource();
                }
                return this._pTSource;
            }
            set
            {
                this._pTSource= value;
            }
        }

        public string VersionLastUpdate
        {
            get
            {
                return this._versionLastUpdate;
            }
            set
            {
                this._versionLastUpdate = value;
            }
        }
    }
}

