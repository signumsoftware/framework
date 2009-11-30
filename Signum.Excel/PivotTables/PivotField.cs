namespace Signum.Excel
{
    using System;
    using System.Globalization;
    using System.Xml;

    public sealed class PivotField : IWriter
    {
        private string _dataField;
        private DataType _dataType;
        private PTFunction _function;
        private string _name;
        private PivotFieldOrientation _orientation = PivotFieldOrientation.NotSet;
        private string _parentField;
        private PivotItemCollection _pivotItems;
        private int _position = Namespaces.NullValue;

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PivotField", Namespaces.Excel);
            if (this._dataField != null)
            {
                writer.WriteElementString("DataField", Namespaces.Excel, this._dataField);
            }
            if (this._name != null)
            {
                writer.WriteElementString("Name", Namespaces.Excel, this._name);
            }
            if (this._parentField != null)
            {
                writer.WriteElementString("ParentField", Namespaces.Excel, this._parentField);
            }
            if (this._dataType != DataType.NotSet)
            {
                writer.WriteElementString("DataType", Namespaces.Excel, this._dataType.ToString());
            }
            if (this._function != PTFunction.NotSet)
            {
                writer.WriteElementString("Function", Namespaces.Excel, this._function.ToString());
            }
            if (this._position != Namespaces.NullValue)
            {
                writer.WriteElementString("Position", Namespaces.Excel, this._position.ToString(CultureInfo.InvariantCulture));
            }
            if (this._orientation != PivotFieldOrientation.NotSet)
            {
                writer.WriteElementString("Orientation", Namespaces.Excel, this._orientation.ToString());
            }
            if (this._pivotItems != null)
            {
                ((IWriter) this._pivotItems).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        public string DataField
        {
            get
            {
                return this._dataField;
            }
            set
            {
                this._dataField = value;
            }
        }

        public DataType DataType
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

        public PTFunction Function
        {
            get
            {
                return this._function;
            }
            set
            {
                this._function = value;
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

        public PivotFieldOrientation Orientation
        {
            get
            {
                return this._orientation;
            }
            set
            {
                this._orientation = value;
            }
        }

        public string ParentField
        {
            get
            {
                return this._parentField;
            }
            set
            {
                this._parentField = value;
            }
        }

        public PivotItemCollection PivotItems
        {
            get
            {
                if (this._pivotItems == null)
                {
                    this._pivotItems = new PivotItemCollection();
                }
                return this._pivotItems;
            }

            set
            {
                this._pivotItems= value;
            }
        }

        public int Position
        {
            get
            {
                return this._position;
            }
            set
            {
                this._position = value;
            }
        }
    }
}

