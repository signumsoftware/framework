namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using System.Linq.Expressions;

    public sealed class Alignment : IWriter, IReader, IExpressionWriter
    {
        private HorizontalAlignment _horizontal;
        private int _indent;
        private ReadingOrder _readingOrder;
        private int _rotate;
        private bool _shrinkToFit;
        private VerticalAlignment _vertical;
        private bool _verticalText;
        private bool _wrapText;

        public Expression CreateExpression()
        {
            return UtilExpression.MemberInit<Alignment>(new MemberBindingList<Alignment>()
            {
                {HorizontalAlignment.Automatic,_horizontal, w=>w.Horizontal},
                {0, _indent, w=>w.Indent},
                {0,_rotate, w=>w.Rotate},
                {false,_shrinkToFit, w=>w.ShrinkToFit},
                {VerticalAlignment.Automatic,_vertical, w=>w.Vertical},
                {false,_verticalText, w=>w.VerticalText},
                {false,_wrapText, w=>w.WrapText},
                {ReadingOrder.NotSet,_readingOrder, w=>w.ReadingOrder},
            }).Collapse(); 
        }


        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            string attribute = element.GetAttribute("Horizontal", Namespaces.SpreadSheet);
            if ((attribute != null) && (attribute.Length != 0))
            {
                this._horizontal = (HorizontalAlignment) Enum.Parse(typeof(HorizontalAlignment), attribute, true);
            }
            this._indent = UtilXml.GetAttribute(element, "Indent", Namespaces.SpreadSheet, 0);
            this._rotate = UtilXml.GetAttribute(element, "Rotate", Namespaces.SpreadSheet, 0);
            this._shrinkToFit = UtilXml.GetAttribute(element, "ShrinkToFit", Namespaces.SpreadSheet, false);
            attribute = element.GetAttribute("Vertical", Namespaces.SpreadSheet);
            if ((attribute != null) && (attribute.Length != 0))
            {
                this._vertical = (VerticalAlignment) Enum.Parse(typeof(VerticalAlignment), attribute, true);
            }
            attribute = element.GetAttribute("ReadingOrder", Namespaces.SpreadSheet);
            if ((attribute != null) && (attribute.Length != 0))
            {
                this._readingOrder = (ReadingOrder) Enum.Parse(typeof(ReadingOrder), attribute, true);
            }
            this._verticalText = element.GetAttribute("VerticalText", Namespaces.SpreadSheet) == "1";
            this._wrapText = element.GetAttribute("WrapText", Namespaces.SpreadSheet) == "1";
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.SchemaPrefix, "Alignment", Namespaces.SpreadSheet);
            if (this._horizontal != HorizontalAlignment.Automatic)
            {
                writer.WriteAttributeString("Horizontal", Namespaces.SpreadSheet, this._horizontal.ToString());
            }
            if (this._indent != 0)
            {
                writer.WriteAttributeString("Indent", Namespaces.SpreadSheet, this._indent.ToString(CultureInfo.InvariantCulture));
            }
            if (this._rotate != 0)
            {
                writer.WriteAttributeString("Rotate", Namespaces.SpreadSheet, this._rotate.ToString(CultureInfo.InvariantCulture));
            }
            if (this._shrinkToFit)
            {
                writer.WriteAttributeString("ShrinkToFit", Namespaces.SpreadSheet, "1");
            }
            if (this._vertical != VerticalAlignment.Automatic)
            {
                writer.WriteAttributeString("Vertical", Namespaces.SpreadSheet, this._vertical.ToString());
            }
            if (this._verticalText)
            {
                writer.WriteAttributeString("VerticalText", Namespaces.SpreadSheet, "1");
            }
            if (this._wrapText)
            {
                writer.WriteAttributeString("WrapText", Namespaces.SpreadSheet, "1");
            }
            if (this._readingOrder != ReadingOrder.NotSet)
            {
                writer.WriteAttributeString("ReadingOrder", Namespaces.SpreadSheet, this._readingOrder.ToString());
            }
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "Alignment", Namespaces.SpreadSheet);
        }

        public HorizontalAlignment Horizontal
        {
            get
            {
                return this._horizontal;
            }
            set
            {
                this._horizontal = value;
            }
        }

        public int Indent
        {
            get
            {
                return this._indent;
            }
            set
            {
                this._indent = value;
            }
        }

        public ReadingOrder ReadingOrder
        {
            get
            {
                return this._readingOrder;
            }
            set
            {
                this._readingOrder = value;
            }
        }

        public int Rotate
        {
            get
            {
                return this._rotate;
            }
            set
            {
                this._rotate = value;
            }
        }

        public bool ShrinkToFit
        {
            get
            {
                return this._shrinkToFit;
            }
            set
            {
                this._shrinkToFit = value;
            }
        }

        public VerticalAlignment Vertical
        {
            get
            {
                return this._vertical;
            }
            set
            {
                this._vertical = value;
            }
        }

        public bool VerticalText
        {
            get
            {
                return this._verticalText;
            }
            set
            {
                this._verticalText = value;
            }
        }

        public bool WrapText
        {
            get
            {
                return this._wrapText;
            }
            set
            {
                this._wrapText = value;
            }
        }
    }
}

