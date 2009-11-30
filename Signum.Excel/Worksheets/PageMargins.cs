namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Globalization;
    using System.Xml;
    using Signum.Utilities.ExpressionTrees;
    using System.Linq.Expressions;

    public sealed class PageMargins : IWriter, IReader, IExpressionWriter
    {
        private float _bottom = 1f;
        private float _left = 0.75f;
        private float _right = 0.75f;
        private float _top = 1f;

        public Expression CreateExpression()
        {
            return Linq.Expr<PageMargins>(() => new PageMargins
            {
                Bottom = _bottom,
                Left = _left,
                Right = _right,
                Top = _top
            }).Body.Collapse();
        }

        void IReader.ReadXml(XmlElement element)
        {
            if (!IsElement(element))
            {
                throw new ArgumentException("Invalid element", "element");
            }
            this._bottom = UtilXml.GetAttribute(element, "Bottom", Namespaces.Excel, (float) 1f);
            this._left = UtilXml.GetAttribute(element, "Left", Namespaces.Excel, (float) 0.75f);
            this._right = UtilXml.GetAttribute(element, "Right", Namespaces.Excel, (float) 0.75f);
            this._top = UtilXml.GetAttribute(element, "Top", Namespaces.Excel, (float) 1f);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Namespaces.ExcelPrefix, "PageMargins", Namespaces.Excel);
            writer.WriteAttributeString("Bottom", Namespaces.Excel, this._bottom.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Left", Namespaces.Excel, this._left.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Right", Namespaces.Excel, this._right.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Top", Namespaces.Excel, this._top.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        internal static bool IsElement(XmlElement element)
        {
            return UtilXml.IsElement(element, "PageMargins", Namespaces.Excel);
        }

        public float Bottom
        {
            get
            {
                return this._bottom;
            }
            set
            {
                this._bottom = value;
            }
        }

        public float Left
        {
            get
            {
                return this._left;
            }
            set
            {
                this._left = value;
            }
        }

        public float Right
        {
            get
            {
                return this._right;
            }
            set
            {
                this._right = value;
            }
        }

        public float Top
        {
            get
            {
                return this._top;
            }
            set
            {
                this._top = value;
            }
        }

    }
}

