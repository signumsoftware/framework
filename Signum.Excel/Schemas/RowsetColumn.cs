namespace Signum.Excel.Schemas
{
    using Signum.Excel;
    using System;
    using System.Xml;

    public sealed class RowsetColumn : IWriter
    {
        private string _name;
        private string _value;

        public RowsetColumn(string name, string value)
        {
            this._name = name;
            this._value = value;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(Namespaces.RowsetSchemaPrefix, this._name, Namespaces.RowsetSchema, this._value);
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

        public string Value
        {
            get
            {
                return this._value;
            }
            set
            {
                this._value = value;
            }
        }
    }
}

