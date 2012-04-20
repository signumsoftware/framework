namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Row : Indexed, IWriter, IReader, IExpressionWriter
    {
        private bool _autoFitHeight = true;
        private CellCollection _cells;
        private int _height = Namespaces.NullValue;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Row>(new MemberBindingList<Row>()
            {
                {0, _offset, a=>a.Offset},
                {Namespaces.NullValue,_height,a=>a.Height},
                {true,_autoFitHeight,a=>a.AutoFitHeight },
                {_cells,a=>a.Cells},
            }); 
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._index = UtilXml.GetAttribute(element, "Index", Namespaces.SpreadSheet, 0);
            this._height = UtilXml.GetAttribute(element, "Height", Namespaces.SpreadSheet, Namespaces.NullValue);
            this._autoFitHeight = UtilXml.GetAttribute(element, "AutoFitHeight", Namespaces.SpreadSheet, true);
            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement element2 = node as XmlElement;
                if ((element2 != null) && Cell.IsElement(element2))
                {
                    Cell cell = new Cell();
                    ((IReader) cell).ReadXml(element2);
                    this.Cells.Add(cell);
                }
            }
            Cells.UpdateOffsets(); 
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Row", Namespaces.SpreadSheet);
            if (this._index > 0)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Index", Namespaces.SpreadSheet, this._index.ToString(CultureInfo.InvariantCulture));
            }
            if (this._height != Namespaces.NullValue)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Height", Namespaces.SpreadSheet, this._height.ToString(CultureInfo.InvariantCulture));
            }
            if (!this._autoFitHeight)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "AutoFitHeight", Namespaces.SpreadSheet, "0");
            }
            if (this._cells != null)
            {
                ((IWriter) this._cells).WriteXml(writer);
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Row", Namespaces.SpreadSheet);
        }

        public bool AutoFitHeight
        {
            get
            {
                return this._autoFitHeight;
            }
            set
            {
                this._autoFitHeight = value;
            }
        }

        public CellCollection Cells
        {
            get
            {
                if (this._cells == null)
                {
                    this._cells = new CellCollection();
                }
                return this._cells;
            }

            set
            {
                this._cells = value; 
            }
        }

        public int? Height
        {
            get
            {
                return _height == Namespaces.NullValue ? null: (int?)_height;
            }
            set
            {
                this._height = value ?? Namespaces.NullValue;
            }
        }

        public bool PageBreak { get; set; }
    }
}

