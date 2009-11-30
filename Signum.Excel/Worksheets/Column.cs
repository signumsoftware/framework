namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;
    using Signum.Utilities.ExpressionTrees;

    public sealed class Column : Indexed, IWriter, IReader, IExpressionWriter
    {
        private bool _autoFitWidth;
        private bool _hidden;
        private string _styleID;
        private int _span = Namespaces.NullValue;
        private int _width = Namespaces.NullValue;

        public Column() { }

        public Column(int width)
        {
            this._width = width;
        }

        public Column(string styleID)
        {
            this._styleID = styleID; 
        }

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Column>(() => new Column(CSharpRenderer.Literal<string>(_styleID)), new MemberBindingList<Column>()
            {
                {0,_offset,a=>a.Offset},
                {Namespaces.NullValue,_width,a=>a.Width},
                {false,_hidden,a=>a.Hidden},
                {false,_autoFitWidth,a=>a.AutoFitWidth},
                {Namespaces.NullValue,_span,a=>a.Span},
            }); 
        }
    
        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._index = UtilXml.GetAttribute(element, "Index", Namespaces.SpreadSheet, 0);
            this._span = UtilXml.GetAttribute(element, "Span", Namespaces.SpreadSheet, Namespaces.NullValue);
            this._width = UtilXml.GetAttribute(element, "Width", Namespaces.SpreadSheet, Namespaces.NullValue);
            this._hidden = UtilXml.GetAttribute(element, "Hidden", Namespaces.SpreadSheet, false);
            this._autoFitWidth = UtilXml.GetAttribute(element, "AutoFitWidth", Namespaces.SpreadSheet, false);
            this._styleID = UtilXml.GetAttribute(element, "StyleID", Namespaces.SpreadSheet);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Column", Namespaces.SpreadSheet);
            if (this._index != 0)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Index", Namespaces.SpreadSheet, this._index.ToString(CultureInfo.InvariantCulture));
            }
            if (this._width != Namespaces.NullValue)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Width", Namespaces.SpreadSheet, this._width.ToString(CultureInfo.InvariantCulture));
            }
            if (this._hidden)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Hidden", Namespaces.SpreadSheet, "1");
            }
            if (this._autoFitWidth)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "AutoFitWidth", Namespaces.SpreadSheet, "1");
            }
            if (this._styleID != null)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "StyleID", Namespaces.SpreadSheet, this._styleID);
            }
            if (this._span != Namespaces.NullValue)
            {
                writer.WriteAttributeString(Namespaces.SchemaPrefix, "Span", Namespaces.SpreadSheet, this._span.ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Column", Namespaces.SpreadSheet);
        }

        public bool AutoFitWidth
        {
            get
            {
                return this._autoFitWidth;
            }
            set
            {
                this._autoFitWidth = value;
            }
        }

        public bool Hidden
        {
            get
            {
                return this._hidden;
            }
            set
            {
                this._hidden = value;
            }
        }

        public int Span
        {
            get
            {
                if (this._span == Namespaces.NullValue)
                {
                    return 0;
                }
                return this._span;
            }
            set
            {
                this._span = value;
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

        public int? Width
        {
            get
            {
                return this._width == Namespaces.NullValue? (int?)null: this._width;
            }
            set
            {
                this._width = value ?? Namespaces.NullValue;
            }
        }
    }
}

