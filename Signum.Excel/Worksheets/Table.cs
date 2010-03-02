namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.ComponentModel;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class WorksheetTable : IWriter, IReader, IExpressionWriter
    {
        private ColumnCollection _columns;
        private float _defaultColumnWidth = 48f;
        private float _defaultRowHeight = 12.75f;
        //private int _expandedColumnCount = Namespaces.NullValue;
        //private int _expandedRowCount = Namespaces.NullValue;
        private int _fullColumns = Namespaces.NullValue;
        private int _fullRows = Namespaces.NullValue;
        private RowCollection _rows;
        private string _styleID;


        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<WorksheetTable>(new MemberBindingList<WorksheetTable>()
            {
                {12.75f,_defaultRowHeight, w=>w.DefaultRowHeight},
                {48f,_defaultColumnWidth,w=>w.DefaultColumnWidth},
                //{Namespaces.NullValue,_expandedColumnCount,w=>w.ExpandedColumnCount},
                //{Namespaces.NullValue,_expandedRowCount,w=>w.ExpandedRowCount},
                {Namespaces.NullValue,_fullColumns,w=>w.FullColumns},
                {Namespaces.NullValue,_fullRows,w=>w.FullRows},

                {null, _styleID,w=>w.StyleID},
                {_columns,w=>w.Columns},
                {_rows ,w=>w.Rows},
            }); 
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._defaultRowHeight = UtilXml.GetAttribute(element, "DefaultRowHeight", Namespaces.SpreadSheet, (float) 12.75f);
            this._defaultColumnWidth = UtilXml.GetAttribute(element, "DefaultColumnWidth", Namespaces.SpreadSheet, (float) 48f);
            //this._expandedColumnCount = UtilXml.GetAttribute(element, "ExpandedColumnCount", Namespaces.SpreadSheet, Namespaces.NullValue);
            //this._expandedRowCount = UtilXml.GetAttribute(element, "ExpandedRowCount", Namespaces.SpreadSheet, Namespaces.NullValue);
            this._fullColumns = UtilXml.GetAttribute(element, "FullColumns", Namespaces.Excel, Namespaces.NullValue);
            this._fullRows = UtilXml.GetAttribute(element, "FullRows", Namespaces.Excel, Namespaces.NullValue);
            this._fullRows = UtilXml.GetAttribute(element, "FullRows", Namespaces.Excel, Namespaces.NullValue);
            this._styleID = UtilXml.GetAttribute(element, "StyleID", Namespaces.SpreadSheet);
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if (element2 != null)
                {
                    if (Column.IsElement(element2))
                    {
                        Column column = new Column();
                        ((IReader) column).ReadXml(element2);
                        this.Columns.Add(column);
                        continue;
                    }
                    if (Row.IsElement(element2))
                    {
                        Row row = new Row();
                        ((IReader) row).ReadXml(element2);
                        this.Rows.Add(row);
                    }
                }
            }
            Columns.UpdateOffsets();
            Rows.UpdateOffsets(); 
        }

        public void WriteXml(XmlWriter writer)
        {
            PreWrite();

            writer.WriteStartElement(Namespaces.SchemaPrefix, "Table", Namespaces.SpreadSheet);
            if (this._defaultRowHeight != 12.75f)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "DefaultRowHeight", Namespaces.SpreadSheet, this._defaultRowHeight.ToString(CultureInfo.InvariantCulture));
            }
            if (this._defaultColumnWidth != 48f)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "DefaultColumnWidth", Namespaces.SpreadSheet, this._defaultColumnWidth.ToString(CultureInfo.InvariantCulture));
            }
            //if (this._expandedColumnCount != Namespaces.NullValue)
            //{
            //    writer.WriteAttributeString(Namespaces.SchemaPrefix, "ExpandedColumnCount", Namespaces.SpreadSheet, this._expandedColumnCount.ToString(CultureInfo.InvariantCulture));
            //}
            //if (this._expandedRowCount != Namespaces.NullValue)
            //{
            //    writer.WriteAttributeString(Namespaces.SchemaPrefix, "ExpandedRowCount", Namespaces.SpreadSheet, this._expandedRowCount.ToString(CultureInfo.InvariantCulture));
            //}
            if (this._fullColumns != Namespaces.NullValue)
            {
                writer.WriteAttributeString(Namespaces.ExcelPrefix, "FullColumns", Namespaces.Excel, this._fullColumns.ToString(CultureInfo.InvariantCulture));
            }
            if (this._fullRows != Namespaces.NullValue)
            {
                writer.WriteAttributeString(Namespaces.ExcelPrefix, "FullRows", Namespaces.Excel, this._fullRows.ToString(CultureInfo.InvariantCulture));
            }
            if (this._styleID != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "StyleID", Namespaces.SpreadSheet, this._styleID);
            }
            if (this._columns != null)
            {
                ((IWriter) this._columns).WriteXml(writer);
            }
            if (this._rows != null)
            {
                ((IWriter) this._rows).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        private void PreWrite()
        {
            _rows.PreWrite();
            if (this._columns != null)
            {
                //ExpandedColumnCount = Math.Max(ExpandedColumnCount, ); 
                _columns.UpdateIndices();
            }
            if (this._rows != null)
            {
                //ExpandedRowCount = 
                _rows.UpdateIndices();
            }
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Table", Namespaces.SpreadSheet);
        }

        public ColumnCollection Columns
        {
            get
            {
                if (this._columns == null)
                {
                    this._columns = new ColumnCollection();
                }
                return this._columns;
            }

            set
            {
                this._columns = value;
            }
        }

        public float DefaultColumnWidth
        {
            get
            {
                return this._defaultColumnWidth;
            }
            set
            {
                this._defaultColumnWidth = value;
            }
        }

        public float DefaultRowHeight
        {
            get
            {
                return this._defaultRowHeight;
            }
            set
            {
                this._defaultRowHeight = value;
            }
        }

        //public int ExpandedColumnCount
        //{
        //    get
        //    {
        //        return this._expandedColumnCount;
        //    }
        //    set
        //    {
        //        this._expandedColumnCount = value;
        //    }
        //}

        //public int ExpandedRowCount
        //{
        //    get
        //    {
        //        return this._expandedRowCount;
        //    }
        //    set
        //    {
        //        this._expandedRowCount = value;
        //    }
        //}

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int FullColumns
        {
            get
            {
                return this._fullColumns;
            }
            set
            {
                this._fullColumns = value;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int FullRows
        {
            get
            {
                return this._fullRows;
            }
            set
            {
                this._fullRows = value;
            }
        }

        public RowCollection Rows
        {
            get
            {
                if (this._rows == null)
                {
                    this._rows = new RowCollection();
                }
                return this._rows;
            }

            set
            {
                this._rows = value; 
            }
        }

        public string StyleID
        {
            get
            {
                return this._styleID;
            }
            set
            {
                this._styleID = value;
            }
        }
    }
}

