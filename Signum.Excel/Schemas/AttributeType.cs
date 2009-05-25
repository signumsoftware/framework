namespace Signum.Excel.Schemas
{
    using Signum.Excel;
    using System;
    using System.Xml;

    public sealed class AttributeType : SchemaType, IWriter
    {
        private string _dataType;
        private string _name;
        private string _rsname;

        public AttributeType()
        {
        }

        public AttributeType(string name, string rowsetName, string dataType)
        {
            this._name = name;
            this._rsname = rowsetName;
            this._dataType = dataType;
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "AttributeType", Namespaces.Schema);
            if (this._name != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "name", Namespaces.Schema, this._name);
            }
            if (this._rsname != null)
            {
                writer.WriteAttributeString(Namespaces.RowsetPrefix, "name", Namespaces.Rowset, this._rsname);
            }
            if (this._dataType != null)
            {
                writer.WriteStartElement(Namespaces.DataTypePrefix, "datatype", Namespaces.DataType);
                writer.WriteAttributeString(Namespaces.DataTypePrefix, "type", Namespaces.DataType, this._dataType);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public string DataType
        {
            get
            {
                return this._dataType;
            }
            set
            {
                this._dataType = value;
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

        public string RowsetName
        {
            get
            {
                return this._rsname;
            }
            set
            {
                this._rsname = value;
            }
        }
    }
}

